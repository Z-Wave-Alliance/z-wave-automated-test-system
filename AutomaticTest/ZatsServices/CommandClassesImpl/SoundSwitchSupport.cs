// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Utils;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;
using Utils.UI.Enums;

// ToDo: Supervision Report Status 'Working' for Tone Play Set?

namespace Zats.Services
{
    public class SoundSwitchSupport : CommandClassSupport
    {
        private SoundSwitchSupportData SoundSwitchSupportData { get; set; }

        public readonly string CommandClassName = "Sound Switch CC";
        public string EndPointName = "";
        private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly BackgroundWorker TonePlayTimer = new BackgroundWorker();
        private string TonePlayTimerName = "";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        public ushort RemainingPlayTime = 0;

        public SoundSwitchSupport()
            : this(new SoundSwitchSupportData() { })
        { }

        public SoundSwitchSupport(SoundSwitchSupportData soundSwitchSupportData)
            : base(COMMAND_CLASS_SOUND_SWITCH.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 2;
            SoundSwitchSupportData = soundSwitchSupportData;
            SoundSwitchSupportData.SupportClassObject = this;
            LogLevel = SoundSwitchSupportData.LogLevel;

            if (SoundSwitchSupportData.SupportedTones == 0)
                SoundSwitchSupportData.SupportedTones = 1;
            InitializeTones();
            SoundSwitchSupportData.PlayingTone = 0x00;
            SoundSwitchSupportData.CurrentConfiguredVolume = SoundSwitchSupportData.Volume;
            SoundSwitchSupportData.LastConfiguredVolume = SoundSwitchSupportData.CurrentConfiguredVolume;

            TonePlayTimer.DoWork += new DoWorkEventHandler(TonePlayTimer_DoWork);
            TonePlayTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Timer_RunWorkerCompleted);
            TonePlayTimer.WorkerSupportsCancellation = true;
            TonePlayTimer.WorkerReportsProgress = false;
        }

        protected override void OnHandledDelayed(DataReceivedUnit ou)
        {
            ou.SetNextActionItems();
            if (DstNode.EndPointId == ReceivedAchData.DstNode.EndPointId)
            {
                var sendDstNode = ReceivedAchData.DstNode;
                var sendSrcNode = ReceivedAchData.SrcNode;
                byte[] command = ReceivedAchData.Command;
                var scheme = (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme;
                bool isResponseAllowed = IsResponseAllowed(Network, command, scheme, Secure);

                EndPointName = ReceivedAchData.DstNode.EndPointId == 0 ? " @ Root" : string.Format(" @ EP {0}", ReceivedAchData.DstNode.EndPointId);
                TonePlayTimerName = string.Format("{0}{1}: TonePlay Simulation Timer", CommandClassName, EndPointName);
                ResultForSv = SvResultNames.Undefined;
                if (isResponseAllowed && command != null && command.Length > 1 && SoundSwitchSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONES_NUMBER_GET.ID)
                    {
                        rpt = PrepareToneNumberReport();
                    }
                    else if (command[1] == COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_CONFIGURATION_GET.ID)
                    {
                        rpt = PrepareConfigurationReport();
                    }
                    else if (command[1] == COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_CONFIGURATION_SET.ID)
                    {
                        ResultForSv = HandleConfigurationSet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONE_INFO_GET.ID)
                    {
                        rpt = PrepareToneInfoReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONE_PLAY_GET.ID)
                    {
                        rpt = PrepareTonePlayReport();
                    }
                    else if (command[1] == COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONE_PLAY_SET.ID)
                    {
                        ResultForSv = HandleTonePlaySet(command);
                    }

                    if (rpt != null)
                    {
                        ApiOperation sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command);
                }
            }
        }

        private void InitializeTones()
        {
            for (byte i = 0x01; i <= SoundSwitchSupportData.SupportedTones; i++)
            {
                ushort duration = (ushort) (i + 5);
                if (SoundSwitchSupportData.SupportedTonesDurations.Length >= i)
                    duration = SoundSwitchSupportData.SupportedTonesDurations[i - 1];
                SoundSwitchSupportData.Tones.Add(new SoundSwitchTone(i, new byte[] { (byte)(duration / 256), (byte)(duration % 256) }, string.Format("Tone{0:000}", i)));
            }
        }

        private byte[] PrepareToneNumberReport()
        {
            COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONES_NUMBER_REPORT rpt = new COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONES_NUMBER_REPORT()
            {
                supportedTones = SoundSwitchSupportData.SupportedTones
            };
            return rpt;
        }

        private byte[] PrepareToneInfoReport(byte[] command)
        {
            COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONE_INFO_GET soundSwitchToneInfoGetCmd = command;
            COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONE_INFO_REPORT rpt = new COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONE_INFO_REPORT() { };

            SoundSwitchTone requestedTone = SoundSwitchSupportData.Tones.Find(tone => tone.toneId == soundSwitchToneInfoGetCmd.toneIdentifier);
            if (requestedTone != null)
            {
                rpt.toneIdentifier = requestedTone.toneId;
                rpt.toneDuration = requestedTone.duration;
                rpt.nameLength = (byte)Encoding.UTF8.GetByteCount(requestedTone.name);
                rpt.name = Encoding.UTF8.GetBytes(requestedTone.name);
            }
            else
            {
                rpt.toneIdentifier = soundSwitchToneInfoGetCmd.toneIdentifier;
                rpt.toneDuration = new byte[] { 0x00, 0x00 };
                rpt.nameLength = 0;
                rpt.name = new byte[] { };
            }
            return rpt;
        }

        private byte[] PrepareConfigurationReport()
        {
            COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_CONFIGURATION_REPORT rpt = new COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_CONFIGURATION_REPORT()
            {
                defaultToneIdentifer = SoundSwitchSupportData.DefaultToneId,
                volume = SoundSwitchSupportData.CurrentConfiguredVolume
            };
            return rpt;
        }

        public byte[] PrepareTonePlayReport()
        {
            if (Version == 1)
            {
                COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONE_PLAY_REPORT rpt = new COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONE_PLAY_REPORT() { };
                if (SoundSwitchSupportData.PlayingVolume == 0x00)
                    rpt.toneIdentifier = 0x00;
                else
                    rpt.toneIdentifier = SoundSwitchSupportData.PlayingTone;
                return rpt;
            }
            if (Version == 2)
            {
                COMMAND_CLASS_SOUND_SWITCH_V2.SOUND_SWITCH_TONE_PLAY_REPORT rpt = new COMMAND_CLASS_SOUND_SWITCH_V2.SOUND_SWITCH_TONE_PLAY_REPORT() { };
                if (SoundSwitchSupportData.PlayingVolume == 0x00)
                    rpt.toneIdentifier = 0x00;
                else
                    rpt.toneIdentifier = SoundSwitchSupportData.PlayingTone;
                rpt.playCommandToneVolume = SoundSwitchSupportData.PlayingVolume;
                return rpt;
            }
            return null;
        }
        private SvResultNames HandleConfigurationSet(byte[] command)
        {
            COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_CONFIGURATION_SET soundSwitchConfigSetCmd = command;
            SvResultNames success = SvResultNames.Successful;

            if (soundSwitchConfigSetCmd.defaultToneIdentifier <= SoundSwitchSupportData.SupportedTones)
            {
                if (soundSwitchConfigSetCmd.defaultToneIdentifier != 0x00)
                {
                    SoundSwitchSupportData.DefaultToneId = soundSwitchConfigSetCmd.defaultToneIdentifier;
                }

                if (soundSwitchConfigSetCmd.volume <= 0x64)
                {
                    SoundSwitchSupportData.LastConfiguredVolume = SoundSwitchSupportData.CurrentConfiguredVolume;
                    SoundSwitchSupportData.CurrentConfiguredVolume = soundSwitchConfigSetCmd.volume;
                }
                else if (soundSwitchConfigSetCmd.volume == 0xFF)
                {
                    if (SoundSwitchSupportData.CurrentConfiguredVolume == 0x00)
                        SoundSwitchSupportData.CurrentConfiguredVolume = SoundSwitchSupportData.LastConfiguredVolume;
                }
                else
                {
                    success = SvResultNames.Failed;
                }
            }
            else
            {
                success = SvResultNames.Failed;
            }
            return success;
        }

        public SvResultNames HandleTonePlaySet(byte[] command)
        {
            COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONE_PLAY_SET soundSwitchTonePlaySetCmd = command;
            SvResultNames success = SvResultNames.Successful;

            TonePlayTimer_Reset();
            if (soundSwitchTonePlaySetCmd.toneIdentifier == 0x00)
            {
                SoundSwitchSupportData.PlayingTone = 0x00;
                SoundSwitchSupportData.PlayingVolume = 0x00;
            }
            else
            {
                SoundSwitchTone requestedTone = SoundSwitchSupportData.Tones.Find(tone => tone.toneId == soundSwitchTonePlaySetCmd.toneIdentifier);
                if (requestedTone != null)
                {
                    SoundSwitchSupportData.PlayingTone = requestedTone.toneId;
                }
                else
                {
                    SoundSwitchSupportData.PlayingTone = SoundSwitchSupportData.DefaultToneId;
                    if (soundSwitchTonePlaySetCmd.toneIdentifier != 0xFF)
                    {
                        success = SvResultNames.Failed;
                    }
                }
                if (Version == 1)
                { 
                    SoundSwitchSupportData.PlayingVolume = SoundSwitchSupportData.CurrentConfiguredVolume;
                }
                if (Version == 2)
                {
                    COMMAND_CLASS_SOUND_SWITCH_V2.SOUND_SWITCH_TONE_PLAY_SET soundSwitchTonePlaySetV2Cmd = command;
                    if (soundSwitchTonePlaySetV2Cmd.playCommandToneVolume == 0x00)
                    {
                        SoundSwitchSupportData.PlayingVolume = SoundSwitchSupportData.CurrentConfiguredVolume;
                    }
                    else if (soundSwitchTonePlaySetV2Cmd.playCommandToneVolume == 0xFF)
                    {
                        if (SoundSwitchSupportData.CurrentConfiguredVolume == 0x00)
                            SoundSwitchSupportData.PlayingVolume = SoundSwitchSupportData.LastConfiguredVolume;
                        else
                            SoundSwitchSupportData.PlayingVolume = SoundSwitchSupportData.CurrentConfiguredVolume;
                    }
                    else if (soundSwitchTonePlaySetV2Cmd.playCommandToneVolume <= 0x64)
                    {
                        SoundSwitchSupportData.PlayingVolume = soundSwitchTonePlaySetV2Cmd.playCommandToneVolume;
                    }
                }
                TonePlayTimer.RunWorkerAsync();
            }

            return success;
        }

        private void TonePlayTimer_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            SoundSwitchTone requestedTone = SoundSwitchSupportData.Tones.Find(tone => tone.toneId == SoundSwitchSupportData.PlayingTone);
            ushort tonePlayTime = BitConverter.ToUInt16(requestedTone.duration.Reverse().ToArray(), 0);

            if (tonePlayTime > 0)
            {
                ReportOperationProgress(TonePlayTimerName, ReportTimerStateNames.Started, tonePlayTime);
                for (RemainingPlayTime = tonePlayTime; RemainingPlayTime > 0; RemainingPlayTime--)
                {
                    Thread.Sleep(1000);
                    if (bw.CancellationPending)
                    {
                        ReportOperationProgress(TonePlayTimerName, ReportTimerStateNames.CancelledRemainSec, RemainingPlayTime);
                        e.Cancel = true;
                        break;
                    }
                }
                if (!e.Cancel)
                {
                    ReportOperationProgress(TonePlayTimerName, ReportTimerStateNames.ExpiredSec, tonePlayTime);
                }
                RemainingPlayTime = 0;
                SoundSwitchSupportData.PlayingTone = 0x00;
                SoundSwitchSupportData.PlayingVolume = 0x00;
            }
        }
        private void Timer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }
        private void TonePlayTimer_Reset()
        {
            if (TonePlayTimer.IsBusy)
            {
                TonePlayTimer.CancelAsync();
                _resetEvent.WaitOne();
                ReportOperationProgress(TonePlayTimerName, ReportTimerStateNames.Reset, 0);
            }
        }
        private void ReportOperationProgress(string timerName, ReportTimerStateNames timerState, int seconds)
        {
            switch (timerState)
            { 
                case ReportTimerStateNames.Started: CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} started: {1} seconds. Tone Id: {2}", timerName, seconds, SoundSwitchSupportData.PlayingTone); break;
                case ReportTimerStateNames.ExpiredSec: CommandClassSupportUtils.ReportTimerState(LogLevel, timerName, timerState, seconds); break;
                case ReportTimerStateNames.CancelledRemainSec: CommandClassSupportUtils.ReportTimerState(LogLevel, timerName, timerState, seconds); break;
                case ReportTimerStateNames.Reset: CommandClassSupportUtils.ReportTimerState(LogLevel, timerName, timerState, seconds); break;
            }
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            {
                if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != Id)
                {
                    supervisionReport.status = (byte) SvStatusNames.NoSupport;
                    return;
                }

                supervisionReport.status = (byte) SvStatusNames.Fail;  // FAIL as default
                switch (supervisionGet.encapsulatedCommand[1])
                {
                    case COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_CONFIGURATION_SET.ID:
                        if (ResultForSv == SvResultNames.Successful)
                            supervisionReport.status = (byte) SvStatusNames.Success;
                        break;
                    case COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONE_PLAY_SET.ID:
                        if (ResultForSv == SvResultNames.Successful)
                            supervisionReport.status = (byte) SvStatusNames.Success;
                        break;
                }
                ResultForSv = SvResultNames.Undefined;
            }
        }
    }

    public class SoundSwitchTone
    {
        public byte toneId { get; }
        public byte[] duration { get; }
        public string name { get; }

        public SoundSwitchTone(byte ToneId, byte[] Duration, string Name)
        {
            toneId = ToneId;
            duration = Duration;
            name = Name;
        }
    }

    public class SoundSwitchSupportData : IBasicMapping
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        public List<SoundSwitchTone> Tones { get; set; } = new List<SoundSwitchTone>();
        public byte LastConfiguredVolume { get; set; } = 50;
        public byte CurrentConfiguredVolume { get; set; } = 50;
        public byte PlayingVolume { get; set; } = 50;
        public byte PlayingTone { get; set; } = 0x00;

        // V1+ Tones Number
        public byte SupportedTones { get; set; } = 5;
        // V1+ Tones Durations.
        // Length SHOULD match SupportedTones, unavailable durations are set to (ToneId + 5) internally.
        public ushort[] SupportedTonesDurations { get; set; } = new ushort[5] { 6, 7, 8, 9, 10 };
        // V1+ Configuration Set
        public byte Volume { get; set; } = 50;
        public byte DefaultToneId { get; set; } = 1;

        // Basic CC Mapping
        public SoundSwitchSupport SupportClassObject;
        bool IBasicMapping.BasicSet(byte value)
        {
            if (SupportClassObject.Version == 1)
            {
                COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONE_PLAY_SET tonePlaySetCmd = new COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONE_PLAY_SET() { };
                tonePlaySetCmd.toneIdentifier = value;
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} accepted.", 
                            SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
                return SupportClassObject.HandleTonePlaySet(tonePlaySetCmd) == SvResultNames.Successful ? true : false;
            }
            if (SupportClassObject.Version == 2)
            {
                COMMAND_CLASS_SOUND_SWITCH_V2.SOUND_SWITCH_TONE_PLAY_SET tonePlaySetCmd = new COMMAND_CLASS_SOUND_SWITCH_V2.SOUND_SWITCH_TONE_PLAY_SET() { };
                tonePlaySetCmd.toneIdentifier = value;
                tonePlaySetCmd.playCommandToneVolume = 0;   // current volume
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} accepted.", 
                            SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
                return SupportClassObject.HandleTonePlaySet(tonePlaySetCmd) == SvResultNames.Successful ? true : false;
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} ignored.", 
                        SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
            return false;
        }
        COMMAND_CLASS_BASIC_V2.BASIC_REPORT IBasicMapping.BasicV2Get()
        {
            COMMAND_CLASS_BASIC_V2.BASIC_REPORT rpt = new COMMAND_CLASS_BASIC_V2.BASIC_REPORT() { };
            rpt.currentValue = (byte) (PlayingVolume == 0x00 ? 0x00 : PlayingTone);
            rpt.targetValue = rpt.currentValue;
            rpt.duration = (byte) SupportClassObject.RemainingPlayTime;
            return rpt;
        }
    }
}
