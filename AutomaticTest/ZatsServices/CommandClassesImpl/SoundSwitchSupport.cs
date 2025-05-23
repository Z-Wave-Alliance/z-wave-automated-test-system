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
using Newtonsoft.Json;
using Zats.Services.CommandClassesImpl;
using static UicApplication.Clusters.DoorLockCluster.ATT_SoundVolume.Restriction;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;

// ToDo: Supervision Report Status 'Working' for Tone Play Set?

namespace Zats.Services
{
    [CommandClassName("Sound Switch Command Class")]
    [CommandClassId(COMMAND_CLASS_SOUND_SWITCH.ID)]
    [MinVersion(1)]
    [MaxVersion(2)]
    public class SoundSwitchSupport : CommandClassSupport
    {
        private SoundSwitchSupportData SoundSwitchSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return SoundSwitchSupportData; }
            set { SoundSwitchSupportData = (SoundSwitchSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly BackgroundWorker TonePlayTimer = new BackgroundWorker();
        private string TonePlayTimerName = "";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        public ushort RemainingPlayTime = 0;

        private NodeTag CmdSrcNode = NodeTag.Empty;

        public SoundSwitchSupport()
            : this(new SoundSwitchSupportData() { })
        { }

        public SoundSwitchSupport(SoundSwitchSupportData soundSwitchSupportData)
            : base(COMMAND_CLASS_SOUND_SWITCH.ID)
        {
            SoundSwitchSupportData = soundSwitchSupportData;
            SoundSwitchSupportData.SupportClassObject = this;

            SoundSwitchSupportData.PlayingTone = 0x00;
            SoundSwitchSupportData.CurrentConfiguredVolume = SoundSwitchSupportData.Volume;
            SoundSwitchSupportData.LastConfiguredVolume = SoundSwitchSupportData.CurrentConfiguredVolume;

            TonePlayTimer.DoWork += new DoWorkEventHandler(TonePlayTimerDoWork);
            TonePlayTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(TonePlayTimerRunWorkerCompleted);
            TonePlayTimer.WorkerSupportsCancellation = true;
            TonePlayTimer.WorkerReportsProgress = false;
        }

        public override void StartEmulation()
        {
            SoundSwitchSupportData.SupportClassObject = this;

            SoundSwitchSupportData.PlayingTone = 0x00;
            SoundSwitchSupportData.CurrentConfiguredVolume = SoundSwitchSupportData.Volume;
            SoundSwitchSupportData.LastConfiguredVolume = SoundSwitchSupportData.CurrentConfiguredVolume;
        }

        protected override void OnHandledDelayed(DataReceivedUnit ou)
        {
            ou.SetNextActionItems();

            if (MatchesDestinationEndPoint())
            {
                var sendDstNode = new NodeTag(ReceivedAchData.DstNode.Id, DstNode.EndPointId);
                var sendSrcNode = ReceivedAchData.SrcNode;
                byte[] command = ReceivedAchData.Command;
                var scheme = (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme;
                bool isHandlingAllowed = IsHandlingAllowed(Network, command, scheme, Secure);

                ResultForSv = SvResultNames.Undefined;
                if (isHandlingAllowed && command != null && command.Length > 1 && SoundSwitchSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONES_NUMBER_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareToneNumberReport();
                    }
                    else if (command[1] == COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_CONFIGURATION_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareConfigurationReport();
                    }
                    else if (command[1] == COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_CONFIGURATION_SET.ID)
                    {
                        ResultForSv = HandleConfigurationSetCmd(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONE_INFO_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareToneInfoReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONE_PLAY_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareTonePlayReport();
                    }
                    else if (command[1] == COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONE_PLAY_SET.ID)
                    {
                        ResultForSv = HandleTonePlaySetCmd(command, sendSrcNode);
                    }

                    if (rpt != null && IsAnsweringAllowed())
                    {
                        ApiOperation sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command, sendDstNode.EndPointId);
                }
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

            SoundSwitchTone requestedTone = SoundSwitchSupportData.Tones.Find(tone => tone.ToneId == soundSwitchToneInfoGetCmd.toneIdentifier);
            if (requestedTone != null)
            {
                rpt.toneIdentifier = requestedTone.ToneId;
                rpt.toneDuration = requestedTone.Duration.ToByteArray();
                rpt.nameLength = (byte)Encoding.UTF8.GetByteCount(requestedTone.Name);
                rpt.name = Encoding.UTF8.GetBytes(requestedTone.Name);
            }
            else
            {
                // CC:0079.01.03.11.006
                // A node receiving this command for an unsupported Tone Identifier or for Tone Identifier 0x00 MUST
                // return a zero duration and a zero length name.
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
                {
                    rpt.toneIdentifier = 0x00;
                }
                else
                {
                    rpt.toneIdentifier = SoundSwitchSupportData.PlayingTone;
                }
                return rpt;
            }
            if (Version == 2)
            {
                COMMAND_CLASS_SOUND_SWITCH_V2.SOUND_SWITCH_TONE_PLAY_REPORT rpt = new COMMAND_CLASS_SOUND_SWITCH_V2.SOUND_SWITCH_TONE_PLAY_REPORT() { };
                if (SoundSwitchSupportData.PlayingVolume == 0x00)
                {
                    rpt.toneIdentifier = 0x00;
                }
                else
                {
                    rpt.toneIdentifier = SoundSwitchSupportData.PlayingTone;
                }
                rpt.playCommandToneVolume = SoundSwitchSupportData.PlayingVolume;
                return rpt;
            }
            return null;
        }

        private SvResultNames HandleConfigurationSet(byte newDefaultToneIdentifier, byte newVolume, bool uiUpdate, NodeTag srcNode)
        {
            SvResultNames success = SvResultNames.Successful;

            if (newDefaultToneIdentifier <= SoundSwitchSupportData.SupportedTones)
            {
                if (newDefaultToneIdentifier != 0x00)
                {
                    SoundSwitchSupportData.DefaultToneId = newDefaultToneIdentifier;
                }

                if (newVolume <= 0x64)
                {
                    SoundSwitchSupportData.LastConfiguredVolume = SoundSwitchSupportData.CurrentConfiguredVolume;
                    SoundSwitchSupportData.CurrentConfiguredVolume = newVolume;
                }
                else if (newVolume == 0xFF)
                {
                    if (SoundSwitchSupportData.CurrentConfiguredVolume == 0x00)
                    {
                        SoundSwitchSupportData.CurrentConfiguredVolume = SoundSwitchSupportData.LastConfiguredVolume;
                    }
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

            if (success == SvResultNames.Successful)
            {
                if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                {
                    SendUnsolicitedReport(PrepareConfigurationReport(), srcNode);
                }
                if (uiUpdate)
                {
                    SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                }
            }
            return success;
        }

        private SvResultNames HandleConfigurationSetCmd(byte[] command, NodeTag srcNode)
        {
            COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_CONFIGURATION_SET soundSwitchConfigSetCmd = command;
            return HandleConfigurationSet(soundSwitchConfigSetCmd.defaultToneIdentifier, soundSwitchConfigSetCmd.volume, true, srcNode);
        }

        public SvResultNames HandleTonePlaySet(byte toneIdentifier, byte playCommandToneVolume, NodeTag srcNode)
        {
            CmdSrcNode = srcNode;
            SvResultNames success = SvResultNames.Successful;
            TonePlayTimerName = string.Format("{0} CC @ {1}: TonePlay Simulation Timer", CommandClassShortName, EndPointName);

            TonePlayTimerCancel();

            if (toneIdentifier == 0x00)
            {
                SoundSwitchSupportData.PlayingTone = 0x00;
                SoundSwitchSupportData.PlayingVolume = 0x00;
            }
            else
            {
                SoundSwitchTone requestedTone = SoundSwitchSupportData.Tones.Find(tone => tone.ToneId == toneIdentifier);
                if (requestedTone != null)
                {
                    SoundSwitchSupportData.PlayingTone = requestedTone.ToneId;
                }
                else
                {
                    SoundSwitchSupportData.PlayingTone = SoundSwitchSupportData.DefaultToneId;
                    if (toneIdentifier != 0xFF)
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
                    if (playCommandToneVolume == 0x00)
                    {
                        SoundSwitchSupportData.PlayingVolume = SoundSwitchSupportData.CurrentConfiguredVolume;
                    }
                    else if (playCommandToneVolume == 0xFF)
                    {
                        if (SoundSwitchSupportData.CurrentConfiguredVolume == 0x00)
                        {
                            SoundSwitchSupportData.PlayingVolume = SoundSwitchSupportData.LastConfiguredVolume;
                        }
                        else
                        {
                            SoundSwitchSupportData.PlayingVolume = SoundSwitchSupportData.CurrentConfiguredVolume;
                        }
                    }
                    else if (playCommandToneVolume <= 0x64)
                    {
                        SoundSwitchSupportData.PlayingVolume = playCommandToneVolume;
                    }
                }
                TonePlayTimerStart();
            }
            return success;
        }

        public SvResultNames HandleTonePlaySetCmd(byte[] command, NodeTag srcNode)
        {
            SvResultNames success = SvResultNames.Successful;

            if (Version == 1)
            {
                COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONE_PLAY_SET soundSwitchTonePlaySetCmd = command;
                return HandleTonePlaySet(soundSwitchTonePlaySetCmd.toneIdentifier, 0, srcNode);
            }
            if (Version == 2)
            {
                COMMAND_CLASS_SOUND_SWITCH_V2.SOUND_SWITCH_TONE_PLAY_SET soundSwitchTonePlaySetV2Cmd = command;
                return HandleTonePlaySet(soundSwitchTonePlaySetV2Cmd.toneIdentifier, soundSwitchTonePlaySetV2Cmd.playCommandToneVolume, srcNode);
            }
            return success;
        }

        #region TonePlay Timer
        private void TonePlayTimerStart()
        {
            SoundSwitchSupportData.CancelPendingTonePlaySet = TonePlayTimerReset;
            TonePlayTimer.RunWorkerAsync();
        }
        private void TonePlayTimerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            SoundSwitchTone requestedTone = SoundSwitchSupportData.Tones.Find(tone => tone.ToneId == SoundSwitchSupportData.PlayingTone);

            if (requestedTone != null)
            {
                if (requestedTone.Duration > 0)
                {
                    if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        //SupportDelegates?.DeviceRestart(this);
                        SendUnsolicitedReport(PrepareTonePlayReport(), CmdSrcNode);
                        //SupportDelegates?.DeviceSleep(this, true);
                    }
                    SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state

                    ReportOperationProgress(TonePlayTimerName, ReportTimerStateNames.Started, requestedTone.Duration);
                    for (RemainingPlayTime = requestedTone.Duration; RemainingPlayTime > 0; RemainingPlayTime--)
                    {
                        Thread.Sleep(1000);
                        if (Result.State == ActionStates.Cancelled)
                        {
                            TonePlayTimerCancel();
                        }
                        if (bw.CancellationPending)
                        {
                            ReportOperationProgress(TonePlayTimerName, ReportTimerStateNames.CancelledRemainSec, RemainingPlayTime);
                            e.Cancel = true;
                            break;
                        }
                    }
                    if (!e.Cancel)
                    {
                        ReportOperationProgress(TonePlayTimerName, ReportTimerStateNames.ExpiredSec, requestedTone.Duration);
                    }
                    RemainingPlayTime = 0;
                    SoundSwitchSupportData.PlayingTone = 0x00;
                    SoundSwitchSupportData.PlayingVolume = 0x00;

                    if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        DeviceRestart(CmdSrcNode);
                        SendUnsolicitedReport(PrepareTonePlayReport(), CmdSrcNode);
                        SupportDelegates?.DeviceSleep(this, ReportWakeupTimeout);
                    }
                    SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                }
            }
        }
        private void TonePlayTimerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }
        private void TonePlayTimerCancel()
        {
            SoundSwitchSupportData.CancelPendingTonePlaySet?.Invoke();
            SoundSwitchSupportData.CancelPendingTonePlaySet = null;
        }

        // This method MUST only be called via <CommandClass>SupportData.CancelPending<CommandName>.Invoke();
        private void TonePlayTimerReset()
        {
            if (TonePlayTimer.IsBusy)
            {
                ReportOperationProgress(TonePlayTimerName, ReportTimerStateNames.Reset, 0);
                TonePlayTimer.CancelAsync();
                _resetEvent.WaitOne();
            }
        }
        #endregion // TonePlay Timer

        private void ReportOperationProgress(string timerName, ReportTimerStateNames timerState, int seconds)
        {
            switch (timerState)
            { 
                case ReportTimerStateNames.Started: CommandClassSupportUtils.ReportAddLog(SoundSwitchSupportData.LogLevel, "{0} started: {1} seconds. Tone Id: {2}", timerName, seconds, SoundSwitchSupportData.PlayingTone); break;
                case ReportTimerStateNames.ExpiredSec: CommandClassSupportUtils.ReportTimerState(SoundSwitchSupportData.LogLevel, timerName, timerState, seconds); break;
                case ReportTimerStateNames.CancelledRemainSec: CommandClassSupportUtils.ReportTimerState(SoundSwitchSupportData.LogLevel, timerName, timerState, seconds); break;
                case ReportTimerStateNames.Reset: CommandClassSupportUtils.ReportTimerState(SoundSwitchSupportData.LogLevel, timerName, timerState, seconds); break;
            }
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            {
                if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != CommandClassId)
                {
                    supervisionReport.status = (byte) SvStatusNames.NoSupport;
                    return;
                }

                supervisionReport.status = (byte) SvStatusNames.Fail;  // FAIL as default
                switch (supervisionGet.encapsulatedCommand[1])
                {
                    case COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_CONFIGURATION_SET.ID:
                        if (ResultForSv == SvResultNames.Successful)
                        {
                            supervisionReport.status = (byte)SvStatusNames.Success;
                        }
                        break;
                    case COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONE_PLAY_SET.ID:
                        if (ResultForSv == SvResultNames.Successful)
                        {
                            supervisionReport.status = (byte)SvStatusNames.Success;
                        }
                        break;
                }
                ResultForSv = SvResultNames.Undefined;
            }
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is SoundSwitchSupportData newData)
            {
                bool identicalTones = newData.Tones.Count == SoundSwitchSupportData.Tones.Count;
                if (identicalTones)
                {
                    foreach (SoundSwitchTone newTone in newData.Tones)
                    {
                        if (SoundSwitchSupportData.Tones.Find(x => x.ToneId == newTone.ToneId) is SoundSwitchTone currentTone)
                        {
                            if (currentTone.Duration != newTone.Duration
                             || currentTone.Name != newTone.Name)
                            {
                                identicalTones = false;
                            }
                        }
                        else
                        {
                            identicalTones = false;
                        }
                        if (!identicalTones)
                        {
                            break;
                        }
                    }
                }
                if (!identicalTones)
                {
                    SoundSwitchSupportData.Tones = newData.Tones;
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareToneNumberReport(), NodeTag.Empty);
                    }
                }

                if (SoundSwitchSupportData.Volume != newData.Volume
                 || SoundSwitchSupportData.DefaultToneId != newData.DefaultToneId)
                {
                    HandleConfigurationSet(newData.DefaultToneId, newData.Volume, false, NodeTag.Empty);
                }

                if (newData.PlayingTone == 0x00                                     // V1+ Cancel current tone
                 || newData.PlayingTone != SoundSwitchSupportData.PlayingTone       // V1+
                 || newData.PlayingVolume != SoundSwitchSupportData.PlayingVolume)  // V2+
                {
                    HandleTonePlaySet(newData.PlayingTone, newData.PlayingVolume, NodeTag.Empty);
                }

                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class SoundSwitchTone : INestedItem
    {
        [CustomDataParameter(1, "Tone Id", UiControl.TextBox, InputType.Int_1_254, EditType.Readonly)]
        public byte ToneId { get; set; }
        [CustomDataParameter(1, "Tone Duration", UiControl.TextBox, InputType.UShort, EditType.Config)]
        public ushort Duration { get; set; }
        [CustomDataParameter(1, "Name", UiControl.TextBox, InputType.TextUTF8, EditType.Config)]
        public string Name { get; set; }

        // only used for EDE UI. see interface for useage notes.
        [JsonIgnore]
        public int NestedId
        {
            get => ToneId;
            set
            {
                ToneId = (byte)value;
            }
        }

        public SoundSwitchTone(byte toneId, ushort duration, string name)
        {
            ToneId = toneId;
            Duration = duration;
            Name = name;
        }
    }

    public class SoundSwitchSupportData : CommandClassSupportData, IBasicMapping, INodeItem
    {
        // Internals accessible from TestCases or EDE
        // Defined Tones
        // Internal Default: none
        [NestedControl(1, "Tones")]
        public List<SoundSwitchTone> Tones { get; set; } = new List<SoundSwitchTone>();
        
        public byte LastConfiguredVolume { get; set; } = 50;
        public byte CurrentConfiguredVolume { get; set; } = 50;

        [JsonIgnore]
        public CancelPendingOperationDelegate CancelPendingTonePlaySet = null;
        [JsonIgnore]
        public IEnumerable<INestedItem> ChildItems => Tones;

        // V1+ Tones
        public byte SupportedTones { get { return (byte) (Tones == null ? 0 : Tones.Count); } }

        // V1+ Configuration Set
        [ParameterSeparator(1, "Sound Switch Configuration Report")]
        [CustomDataParameter(1, "Volume", UiControl.TextBox, InputType.Int_0_100_and_255, EditType.Config | EditType.Edit)]
        public byte Volume { get; set; } = 50;

        [CustomDataParameter(1, "Default Tone Id", UiControl.TextBox, InputType.Int_0_254, EditType.Config | EditType.Edit)]
        public byte DefaultToneId { get; set; } = 1;

        // V1+ Tone Play Set
        [ParameterSeparator(1, "Sound Switch Tone Play Report")]
        [CustomDataParameter(1, "Play Tone Id", UiControl.TextBox, InputType.Int_0_255, EditType.Config | EditType.Edit)]
        public byte PlayingTone { get; set; } = 0x00;

        // V2+ Tone Play Set
        [CustomDataParameter(2, "Play Tone Volume", UiControl.TextBox, InputType.Int_0_100_and_255, EditType.Config | EditType.Edit)]
        public byte PlayingVolume { get; set; } = 50;

        // Basic CC Mapping
        [JsonIgnore]
        public SoundSwitchSupport SupportClassObject;
        bool IBasicMapping.BasicSet(byte value, NodeTag srcNode)
        {
            if (SupportClassObject.Version == 1)
            {
                COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONE_PLAY_SET tonePlaySetCmd = new COMMAND_CLASS_SOUND_SWITCH.SOUND_SWITCH_TONE_PLAY_SET() { };
                tonePlaySetCmd.toneIdentifier = value;
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} {1}: Basic Set with value 0x{2:X2} accepted.", 
                            SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
                return SupportClassObject.HandleTonePlaySetCmd(tonePlaySetCmd, srcNode) == SvResultNames.Successful ? true : false;
            }
            if (SupportClassObject.Version == 2)
            {
                COMMAND_CLASS_SOUND_SWITCH_V2.SOUND_SWITCH_TONE_PLAY_SET tonePlaySetCmd = new COMMAND_CLASS_SOUND_SWITCH_V2.SOUND_SWITCH_TONE_PLAY_SET() { };
                tonePlaySetCmd.toneIdentifier = value;
                tonePlaySetCmd.playCommandToneVolume = 0;   // current volume
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} {1}: Basic Set with value 0x{2:X2} accepted.", 
                            SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
                return SupportClassObject.HandleTonePlaySetCmd(tonePlaySetCmd, srcNode) == SvResultNames.Successful ? true : false;
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} {1}: Basic Set with value 0x{2:X2} ignored.", 
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
