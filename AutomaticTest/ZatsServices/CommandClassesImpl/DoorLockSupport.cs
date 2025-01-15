// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using System.ComponentModel;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Enums;
using ZWave.Devices;
using Utils;
using System.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Utils.UI.Enums;
using ZWave;
using ZWave.BasicApplication;

namespace Zats.Services
{
    // ToDo: Supervision Report Status 'Working'
    // ToDo (nice to have only): Support for InsideDoorHandlesState / OutsideDoorHandlesState
    //      See explanation from SiLabs spec team 2020-Jan-23
    // Interaction with UserCode CC?

    public class DoorLockSupport : CommandClassSupport
    {
        private DoorLockSupportData DoorLockSupportData { get; set; }

        public readonly string CommandClassName = "Door Lock CC";
        public string EndPointName = "";
        private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly BackgroundWorker LockTimeoutTimer = new BackgroundWorker();
        private readonly BackgroundWorker AutoRelockTimer = new BackgroundWorker();
        private readonly BackgroundWorker HoldAndReleaseTimer = new BackgroundWorker();
        private readonly BackgroundWorker DurationTimer = new BackgroundWorker();
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);
        private string LockTimeoutTimerName = "";
        private string AutoRelockTimerName = "";
        private string HoldAndReleaseTimerName = "";
        private string DurationTimerName = "";

        public byte RemainingDuration = 0x00;
        private bool AutoRelockIsActive = false;

        public DoorLockSupport()
            : this(new DoorLockSupportData() { })
        { }

        public DoorLockSupport(DoorLockSupportData doorLockSupportData)
            : base(COMMAND_CLASS_DOOR_LOCK.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 4;
            DoorLockSupportData = doorLockSupportData;
            DoorLockSupportData.SupportClassObject = this;
            LogLevel = DoorLockSupportData.LogLevel;

            if (!doorLockSupportData.SupportedOperationTypesList.Contains(DoorLockOperationTypes.ConstantOperation))
            {
                doorLockSupportData.SupportedOperationTypesList.Add(DoorLockOperationTypes.ConstantOperation);   // Constant Operation (0x01) is always required
            }
            DoorLockSupportData.DoorConditionDoor = (byte) (doorLockSupportData.InitialDoorCondition & 0x01);
            DoorLockSupportData.DoorConditionBolt  = (byte) (doorLockSupportData.InitialDoorCondition & 0x02);
            DoorLockSupportData.DoorConditionLatch = (byte) (doorLockSupportData.InitialDoorCondition & 0x04);
            DoorLockSupportData.CurrentDoorLockMode = doorLockSupportData.InitialDoorLockMode;
            DoorLockSupportData.TargetDoorLockMode = doorLockSupportData.InitialDoorLockMode;

            LockTimeoutTimer.DoWork += new DoWorkEventHandler(LockTimeoutTimer_DoWork);
            LockTimeoutTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Timer_RunWorkerCompleted);
            LockTimeoutTimer.WorkerSupportsCancellation = true;
            LockTimeoutTimer.WorkerReportsProgress = false;
            AutoRelockTimer.DoWork += new DoWorkEventHandler(AutoRelockTimer_DoWork);
            AutoRelockTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Timer_RunWorkerCompleted);
            AutoRelockTimer.WorkerSupportsCancellation = true;
            AutoRelockTimer.WorkerReportsProgress = false;
            HoldAndReleaseTimer.DoWork += new DoWorkEventHandler(HoldAndReleaseTimer_DoWork);
            HoldAndReleaseTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Timer_RunWorkerCompleted);
            HoldAndReleaseTimer.WorkerSupportsCancellation = true;
            HoldAndReleaseTimer.WorkerReportsProgress = false;
            DurationTimer.DoWork += new DoWorkEventHandler(DurationTimer_DoWork);
            DurationTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Timer_RunWorkerCompleted);
            DurationTimer.WorkerSupportsCancellation = true;
            DurationTimer.WorkerReportsProgress = false;
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
                LockTimeoutTimerName = string.Format("{0}{1}: LockTimeout Timer", CommandClassName, EndPointName);
                AutoRelockTimerName = string.Format("{0}{1}: AutoRelock Timer", CommandClassName, EndPointName);
                HoldAndReleaseTimerName = string.Format("{0}{1}: HoldAndRelease Timer", CommandClassName, EndPointName);
                DurationTimerName = string.Format("{0}{1}: Duration Simulation Timer", CommandClassName, EndPointName);
                ResultForSv = SvResultNames.Undefined;
                if (isResponseAllowed && command != null && command.Length > 1 && DoorLockSupportData != null)
                {
                    byte[] rpt = null;
                    // ** Configuration **
                    if (command[1] == COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_CONFIGURATION_GET.ID)
                    {
                        rpt = PrepareConfigurationReport();
                    }
                    else if (command[1] == COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_CONFIGURATION_SET.ID)
                    {
                        ResultForSv = HandleConfigurationSet(command);
                    }
                    // ** Capabilities **
                    else if (command[1] == COMMAND_CLASS_DOOR_LOCK_V4.DOOR_LOCK_CAPABILITIES_GET.ID)
                    {
                        rpt = PrepareCapabilitiesReport();
                    }
                    // ** Operation **
                    else if (command[1] == COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_GET.ID)
                    {
                        rpt = PrepareOperationReport();
                    }
                    else if (command[1] == COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_SET.ID)
                    {
                        ResultForSv = HandleOperationSet(command);
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

        private byte[] PrepareOperationReport()
        {
            if (Version == 3 || Version == 4)
            {
                COMMAND_CLASS_DOOR_LOCK_V3.DOOR_LOCK_OPERATION_REPORT rpt = new COMMAND_CLASS_DOOR_LOCK_V3.DOOR_LOCK_OPERATION_REPORT()
                {
                    properties1 = new COMMAND_CLASS_DOOR_LOCK_V3.DOOR_LOCK_OPERATION_REPORT.Tproperties1()
                    {
                        outsideDoorHandlesMode = DoorLockSupportData.OutsideDoorHandlesState,
                        insideDoorHandlesMode = DoorLockSupportData.InsideDoorHandlesState
                    },
                    doorCondition = DoorLockSupportData.DoorCondition,
                    currentDoorLockMode = (byte) DoorLockSupportData.CurrentDoorLockMode,
                    targetDoorLockMode = (byte) DoorLockSupportData.TargetDoorLockMode,
                    duration = RemainingDuration
                };
                rpt.remainingLockTimeMinutes = 0xFE;
                rpt.remainingLockTimeSeconds = 0xFE;
                if (DoorLockSupportData.AutoRelockReportRemainingTime
                 && AutoRelockIsActive
                 && Version == 4)
                {
                    rpt.remainingLockTimeMinutes = DoorLockSupportData.RemainingLockTimeOutMinutes;
                    rpt.remainingLockTimeSeconds = DoorLockSupportData.RemainingLockTimeOutSeconds;
                }
                if (DoorLockSupportData.OperationType == DoorLockOperationTypes.TimedOperation)
                {
                   if (DoorLockSupportData.CurrentDoorLockMode == DoorLockModes.UnsecuredWithTimeout
                    || DoorLockSupportData.CurrentDoorLockMode == DoorLockModes.UnsecuredForInsideHandlesWithTimeout
                    || DoorLockSupportData.CurrentDoorLockMode == DoorLockModes.UnsecuredForOutsideHandlesWithTimeout
                    || Version == 3)
                    {
                        rpt.remainingLockTimeMinutes = DoorLockSupportData.RemainingLockTimeOutMinutes;
                        rpt.remainingLockTimeSeconds = DoorLockSupportData.RemainingLockTimeOutSeconds;
                    }
                }
                return rpt;
            }
            if (Version == 1 || Version == 2)
            {
                COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_REPORT rpt = new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_REPORT()
                {
                    doorLockMode = (byte) DoorLockSupportData.CurrentDoorLockMode,
                    properties1 = new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_REPORT.Tproperties1()
                    {
                        outsideDoorHandlesMode = DoorLockSupportData.OutsideDoorHandlesState,
                        insideDoorHandlesMode = DoorLockSupportData.InsideDoorHandlesState
                    },
                    doorCondition = DoorLockSupportData.DoorCondition
                };
                rpt.remainingLockTimeMinutes = 0xFE;
                rpt.remainingLockTimeSeconds = 0xFE;
                if (DoorLockSupportData.OperationType == DoorLockOperationTypes.TimedOperation)
                {
                    rpt.remainingLockTimeMinutes = DoorLockSupportData.RemainingLockTimeOutMinutes;
                    rpt.remainingLockTimeSeconds = DoorLockSupportData.RemainingLockTimeOutSeconds;
                }
                return rpt;
            }
            return null;
        }

        private byte[] PrepareCapabilitiesReport()
        {
            if (Version == 4)
            {
                COMMAND_CLASS_DOOR_LOCK_V4.DOOR_LOCK_CAPABILITIES_REPORT rpt = new COMMAND_CLASS_DOOR_LOCK_V4.DOOR_LOCK_CAPABILITIES_REPORT()
                {
                    properties1 = new COMMAND_CLASS_DOOR_LOCK_V4.DOOR_LOCK_CAPABILITIES_REPORT.Tproperties1() { },
                    supportedDoorLockModeListLength = (byte)DoorLockSupportData.SupportedDoorLockModes.Count,
                    supportedDoorLockMode = DoorLockSupportData.SupportedDoorLockModes.ConvertAll(value => (byte)value),
                    properties2 = new COMMAND_CLASS_DOOR_LOCK_V4.DOOR_LOCK_CAPABILITIES_REPORT.Tproperties2()
                    {
                        supportedInsideHandleModesBitmask = DoorLockSupportData.SupportedInsideHandleModesBitMask,
                        supportedOutsideHandleModesBitmask = DoorLockSupportData.SupportedOutsideHandleModesBitMask
                    },
                    supportedDoorComponents = DoorLockSupportData.SupportedDoorComponents,
                    properties3 = new COMMAND_CLASS_DOOR_LOCK_V4.DOOR_LOCK_CAPABILITIES_REPORT.Tproperties3()
                    {
                        ars = (byte) (DoorLockSupportData.AutoRelockSupport ? 0x01 : 0x00),
                        hrs = (byte) (DoorLockSupportData.HoldAndReleaseSupport ? 0x01 : 0x00),
                        tas = (byte) (DoorLockSupportData.TwistAssistSupport ? 0x01 : 0x00),
                        btbs = (byte) (DoorLockSupportData.BlockToBlockSupport ? 0x01 : 0x00),
                    }
                };
                rpt.supportedOperationTypeBitMask = CommandClassSupportUtils.CreateBitmaskFromList(DoorLockSupportData.SupportedOperationTypesList, 0);   // Bit 0 represents type 0
                rpt.properties1.supportedOperationTypeBitMaskLength = (byte)rpt.supportedOperationTypeBitMask.Count;
                return rpt;
            }
            return null;
        }

        private byte[] PrepareConfigurationReport()
        {
            if (Version <= 3)
            {
                COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_CONFIGURATION_REPORT rpt = new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_CONFIGURATION_REPORT() { };
                rpt.operationType = (byte) DoorLockSupportData.OperationType;
                rpt.lockTimeoutMinutes = DoorLockSupportData.LockTimeOutMinutes;
                rpt.lockTimeoutSeconds = DoorLockSupportData.LockTimeOutSeconds;
                rpt.properties1.insideDoorHandlesEnabled = DoorLockSupportData.InsideDoorHandlesState;
                rpt.properties1.outsideDoorHandlesEnabled = DoorLockSupportData.OutsideDoorHandlesState;
                return rpt;
            }
            if (Version == 4)
            {
                COMMAND_CLASS_DOOR_LOCK_V4.DOOR_LOCK_CONFIGURATION_REPORT rpt = new COMMAND_CLASS_DOOR_LOCK_V4.DOOR_LOCK_CONFIGURATION_REPORT() { };
                rpt.operationType = (byte) DoorLockSupportData.OperationType;
                rpt.lockTimeoutMinutes = DoorLockSupportData.LockTimeOutMinutes;
                rpt.lockTimeoutSeconds = DoorLockSupportData.LockTimeOutSeconds;
                rpt.autoRelockTime = DoorLockSupportData.AutoRelockTime;
                rpt.holdAndReleaseTime = DoorLockSupportData.HoldAndReleaseTime;
                rpt.properties1.insideDoorHandlesEnabled = DoorLockSupportData.InsideDoorHandlesState;
                rpt.properties1.outsideDoorHandlesEnabled = DoorLockSupportData.OutsideDoorHandlesState;
                rpt.properties2.btb = DoorLockSupportData.BlockToBlock;
                rpt.properties2.ta = DoorLockSupportData.TwistAssist;
                return rpt;
            }
            return null;
        }

        private SvResultNames HandleConfigurationSet(byte[] command)
        {
            COMMAND_CLASS_DOOR_LOCK_V4.DOOR_LOCK_CONFIGURATION_SET doorLockConfigurationSetCmd = command;
            SvResultNames success = SvResultNames.Successful;

            if (Version <= 3)
            {
                if (doorLockConfigurationSetCmd.operationType == (byte) DoorLockOperationTypes.ConstantOperation
                 || doorLockConfigurationSetCmd.operationType == (byte) DoorLockOperationTypes.TimedOperation)
                {
                    DoorLockSupportData.OperationType = (DoorLockOperationTypes) (byte) doorLockConfigurationSetCmd.operationType;
                }
                else
                {
                    success = SvResultNames.Failed;
                }
            }
            else
            {
                if ((doorLockConfigurationSetCmd.operationType == (byte) DoorLockOperationTypes.ConstantOperation && DoorLockSupportData.SupportedOperationTypesList.Contains(DoorLockOperationTypes.ConstantOperation))
                 || (doorLockConfigurationSetCmd.operationType == (byte) DoorLockOperationTypes.TimedOperation && DoorLockSupportData.SupportedOperationTypesList.Contains(DoorLockOperationTypes.TimedOperation)))
                {
                    DoorLockSupportData.OperationType = (DoorLockOperationTypes) (byte) doorLockConfigurationSetCmd.operationType;
                }
                else
                {
                    success = SvResultNames.Failed;
                }
            }
            // Ignore non-supported handles (set to 0=disabled)
            DoorLockSupportData.OutsideDoorHandlesState = (byte) (doorLockConfigurationSetCmd.properties1.outsideDoorHandlesEnabled & DoorLockSupportData.SupportedOutsideHandleModesBitMask);
            DoorLockSupportData.InsideDoorHandlesState = (byte) (doorLockConfigurationSetCmd.properties1.insideDoorHandlesEnabled & DoorLockSupportData.SupportedInsideHandleModesBitMask);

            if (DoorLockSupportData.OperationType == DoorLockOperationTypes.TimedOperation) // Operation Type from incoming command is already validated above
            {
                if (doorLockConfigurationSetCmd.lockTimeoutMinutes <= 0xFC)
                {
                    DoorLockSupportData.LockTimeOutMinutes = doorLockConfigurationSetCmd.lockTimeoutMinutes;
                }
                else
                {
                    success = SvResultNames.Failed;
                }
                if (doorLockConfigurationSetCmd.lockTimeoutSeconds <= 0x3B)
                {
                    DoorLockSupportData.LockTimeOutSeconds = doorLockConfigurationSetCmd.lockTimeoutSeconds;
                }
                else
                {
                    success = SvResultNames.Failed;
                }
            }
            if (DoorLockSupportData.OperationType == DoorLockOperationTypes.ConstantOperation)
            {
                DoorLockSupportData.LockTimeOutMinutes = 0xFE;
                DoorLockSupportData.LockTimeOutSeconds = 0xFE;
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "Configuration V1-4 Set: OperationType=0x{0:X2}, LockTimeoutMin={1}, LockTimeoutSec={2}",
                                 (byte) DoorLockSupportData.OperationType, DoorLockSupportData.LockTimeOutMinutes, DoorLockSupportData.LockTimeOutSeconds);

            if (Version >= 4)
            {
                if (DoorLockSupportData.AutoRelockSupport)
                    DoorLockSupportData.AutoRelockTime = doorLockConfigurationSetCmd.autoRelockTime;
                if (DoorLockSupportData.HoldAndReleaseSupport)
                    DoorLockSupportData.HoldAndReleaseTime = doorLockConfigurationSetCmd.holdAndReleaseTime;
                if (DoorLockSupportData.BlockToBlockSupport)
                    DoorLockSupportData.BlockToBlock = doorLockConfigurationSetCmd.properties2.btb;
                if (DoorLockSupportData.TwistAssistSupport)
                    DoorLockSupportData.TwistAssist = doorLockConfigurationSetCmd.properties2.ta;
                CommandClassSupportUtils.ReportAddLog(LogLevel, "Configuration V4+ Set: AutoRelockTime={0} sec, HoldAndReleaseTime={1} sec, BlockToBlock={2}, TwistAssist={3}",
                        BitConverter.ToUInt16(DoorLockSupportData.AutoRelockTime.Reverse().ToArray(), 0),
                        BitConverter.ToUInt16(DoorLockSupportData.HoldAndReleaseTime.Reverse().ToArray(), 0),
                        DoorLockSupportData.BlockToBlock, DoorLockSupportData.TwistAssist);
            }
            return success;
        }

        public SvResultNames HandleOperationSet(byte[] command)
        {
            COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_SET doorLockOperationSetCmd = command;
            DoorLockModes requestedDoorLockMode = (DoorLockModes) (byte) doorLockOperationSetCmd.doorLockMode;
            SvResultNames success = SvResultNames.Failed;

            if (DoorLockSupportData.SupportedDoorLockModes.Contains(requestedDoorLockMode))
            {
                if (requestedDoorLockMode == DoorLockModes.UnsecuredWithTimeout
                 || requestedDoorLockMode == DoorLockModes.UnsecuredForInsideHandlesWithTimeout
                 || requestedDoorLockMode == DoorLockModes.UnsecuredForOutsideHandlesWithTimeout)
                {
                    if (DoorLockSupportData.OperationType == DoorLockOperationTypes.TimedOperation)
                    {
                        DoorLockSupportData.TargetDoorLockMode = requestedDoorLockMode;
                        if (DoorLockSupportData.CurrentDoorLockMode == DoorLockModes.Secured)
                        {
                            ResetDurationTimer();
                            ReportOperationSet();
                            DurationTimer.RunWorkerAsync();
                        }
                        else
                        {
                            ReportOperationSet();
                            DoorLockSupportData.CurrentDoorLockMode = DoorLockSupportData.TargetDoorLockMode;
                        }
                        DoorLockSupportData.DoorConditionBolt = 0x02;
                        if (DoorLockSupportData.LockTimeOutMinutes != 0xFE && DoorLockSupportData.LockTimeOutSeconds != 0xFE)
                        {
                            if (DoorLockSupportData.LockTimeOutMinutes + DoorLockSupportData.LockTimeOutSeconds != 0)
                            {
                                ResetAllTimers();   // Cancel running Unsecured Timer Tasks
                                LockTimeoutTimer.RunWorkerAsync();
                            }
                        }
                        success = SvResultNames.Successful;
                    }
                }
                else
                if (requestedDoorLockMode == DoorLockModes.Unsecured
                 || requestedDoorLockMode == DoorLockModes.UnsecuredForInsideHandles
                 || requestedDoorLockMode == DoorLockModes.UnsecuredForOutsideHandles)
                {
                    ResetAllTimers();   // Cancel running Unsecured Timer Tasks
                    DoorLockSupportData.TargetDoorLockMode = requestedDoorLockMode;
                    if (DoorLockSupportData.CurrentDoorLockMode == DoorLockModes.Secured)
                    {
                        ResetDurationTimer();
                        ReportOperationSet();
                        DurationTimer.RunWorkerAsync();
                    }
                    else
                    {
                        ReportOperationSet();
                        DoorLockSupportData.CurrentDoorLockMode = DoorLockSupportData.TargetDoorLockMode;
                    }
                    DoorLockSupportData.DoorConditionBolt = 0x02;

                    // AutoRelock (V4+) is supported for Constant Operation Type only, as discussed with SiLabs Spec Team 2020-Feb-06:
                    // If the door lock is actuated locally or in constant operation, Auto-Relock must kick in.
                    // If the door lock runs in Timed Operation, Auto-Relock must not do anything.
                    if (DoorLockSupportData.OperationType == DoorLockOperationTypes.ConstantOperation
                        && Version >= 4
                        && DoorLockSupportData.AutoRelockSupport)
                    {
                        ushort autoRelockTime = BitConverter.ToUInt16(DoorLockSupportData.AutoRelockTime.Reverse().ToArray(), 0);
                        if (autoRelockTime != 0)
                        {
                            ResetAllTimers();   // Cancel running Unsecured Timer Tasks
                            AutoRelockTimer.RunWorkerAsync();
                        }
                    }
                    if (Version >= 4 && DoorLockSupportData.HoldAndReleaseSupport)
                    {
                        ushort holdAndReleaseTime = BitConverter.ToUInt16(DoorLockSupportData.HoldAndReleaseTime.Reverse().ToArray(), 0);
                        if (holdAndReleaseTime != 0)
                        {
                            //ResetAllTimers();   // Cancel running Unsecured Timer Tasks
                            if (HoldAndReleaseTimer.IsBusy)
                            {
                                HoldAndReleaseTimer.CancelAsync();
                                _resetEvent.WaitOne();
                            }
                            HoldAndReleaseTimer.RunWorkerAsync();
                            DoorLockSupportData.DoorConditionLatch = 0x00; // Latch open
                        }
                    }
                    success = SvResultNames.Successful;
                }
                else if (requestedDoorLockMode == DoorLockModes.Secured)
                {
                    DoorLockSupportData.TargetDoorLockMode = requestedDoorLockMode;
                    if (DoorLockSupportData.CurrentDoorLockMode != DoorLockModes.Secured)
                    {
                        ResetAllTimers();   // Cancel running Unsecured Timer Tasks
                        ResetDurationTimer();
                        ReportOperationSet();
                        DurationTimer.RunWorkerAsync();
                        DoorLockSupportData.DoorConditionBolt = 0x00;
                    }
                    else
                    {
                        ReportOperationSet();
                    }
                    success = SvResultNames.Successful;
                }
            }
            return success;
        }

        private void LockTimeoutTimer_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            int unlockSeconds = DoorLockSupportData.LockTimeOutMinutes * 60 + DoorLockSupportData.LockTimeOutSeconds;

            ReportOperationProgress(LockTimeoutTimerName, ReportTimerStateNames.Started, unlockSeconds);
            for (int seconds = unlockSeconds - 1; seconds >= 0; seconds--)
            {
                DoorLockSupportData.RemainingLockTimeOutMinutes = (byte) (seconds / 60);
                DoorLockSupportData.RemainingLockTimeOutSeconds = (byte) (seconds % 60);
                Thread.Sleep(1000);
                if (bw.CancellationPending)
                {
                    ReportOperationProgress(LockTimeoutTimerName, ReportTimerStateNames.CancelledElapsedSec, DoorLockSupportData.RemainingLockTimeOutMinutes * 60 + DoorLockSupportData.RemainingLockTimeOutSeconds);
                    e.Cancel = true;
                    break;
                }
            }
            DoorLockSupportData.RemainingLockTimeOutMinutes = 0;
            DoorLockSupportData.RemainingLockTimeOutSeconds = 0;
            // DoorLockSupportData.CurrentDoorLockMode = DoorLockModes.Secured; // will be set after DurationTimer is expired
            DoorLockSupportData.DoorConditionBolt = 0x00;
            if (!e.Cancel)
            {
                ResetDurationTimer();
                DoorLockSupportData.TargetDoorLockMode = DoorLockModes.Secured;
                ReportOperationProgress(LockTimeoutTimerName, ReportTimerStateNames.ExpiredSec, unlockSeconds);
                DurationTimer.RunWorkerAsync();
            }
        }
        private void AutoRelockTimer_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            ushort autoRelockTime = BitConverter.ToUInt16(DoorLockSupportData.AutoRelockTime.Reverse().ToArray(), 0);

            AutoRelockIsActive = true;
            ReportOperationProgress(AutoRelockTimerName, ReportTimerStateNames.Started, autoRelockTime);
            for (int seconds = autoRelockTime; seconds > 0; seconds--)
            {
                if (DoorLockSupportData.AutoRelockReportRemainingTime)
                {
                    DoorLockSupportData.RemainingLockTimeOutMinutes = (byte) (seconds / 60);
                    DoorLockSupportData.RemainingLockTimeOutSeconds = (byte) (seconds % 60);
                }
                Thread.Sleep(1000);
                if (bw.CancellationPending)
                {
                    ReportOperationProgress(AutoRelockTimerName, ReportTimerStateNames.CancelledElapsedSec, seconds);
                    e.Cancel = true;
                    break;
                }
            }
            if (DoorLockSupportData.AutoRelockReportRemainingTime)
            {
                DoorLockSupportData.RemainingLockTimeOutMinutes = 0;
                DoorLockSupportData.RemainingLockTimeOutSeconds = 0;
            }
            AutoRelockIsActive = false;
            DoorLockSupportData.DoorConditionBolt = 0x00;
            // DoorLockSupportData.CurrentDoorLockMode = DoorLockModes.Secured; // will be set after DurationTimer is expired
            if (!e.Cancel)
            {
                ResetDurationTimer();
                DoorLockSupportData.TargetDoorLockMode = DoorLockModes.Secured;
                ReportOperationProgress(AutoRelockTimerName, ReportTimerStateNames.ExpiredSec, autoRelockTime);
                CommandClassSupportUtils.ReportAddLog(LogLevel, "Door will be secured, bolt will be locked.");
                DurationTimer.RunWorkerAsync();
            }
        }
        private void HoldAndReleaseTimer_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            ushort holdAndReleaseTime = BitConverter.ToUInt16(DoorLockSupportData.HoldAndReleaseTime.Reverse().ToArray(), 0);

            ReportOperationProgress(HoldAndReleaseTimerName, ReportTimerStateNames.Started, holdAndReleaseTime);
            for (int seconds = holdAndReleaseTime; seconds > 0; seconds--)
            {
                Thread.Sleep(1000);
                if (bw.CancellationPending)
                {
                    ReportOperationProgress(HoldAndReleaseTimerName, ReportTimerStateNames.CancelledElapsedSec, seconds);
                    e.Cancel = true;
                    break;
                }
            }
            // Target/CurrentDoorLockMode will not change after HoldAndRelease Timer is expired
            DoorLockSupportData.DoorConditionLatch = 0x04;
            if (!e.Cancel)
            {
                ReportOperationProgress(HoldAndReleaseTimerName, ReportTimerStateNames.ExpiredSec, holdAndReleaseTime);
            }
        }
        private void DurationTimer_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            if (DoorLockSupportData.SimulatedDuration > 0)
            {
                ReportOperationProgress(DurationTimerName, ReportTimerStateNames.Started, DoorLockSupportData.SimulatedDuration);
                for (RemainingDuration = DoorLockSupportData.SimulatedDuration; RemainingDuration > 0; RemainingDuration--)
                {
                    Thread.Sleep(1000);
                    if (bw.CancellationPending)
                    {
                        ReportOperationProgress(DurationTimerName, ReportTimerStateNames.CancelledElapsedSec, RemainingDuration);
                        e.Cancel = true;
                        break;
                    }
                }
                if (!e.Cancel)
                {
                    ReportOperationProgress(DurationTimerName, ReportTimerStateNames.ExpiredSec, DoorLockSupportData.SimulatedDuration);
                }
            }
            RemainingDuration = 0;
            DoorLockSupportData.CurrentDoorLockMode = DoorLockSupportData.TargetDoorLockMode;
            ReportOperationFinished();
        }
        private void Timer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }
        private void ResetAllTimers ()
        {
            if (LockTimeoutTimer.IsBusy)
            {
                LockTimeoutTimer.CancelAsync();
                _resetEvent.WaitOne();
                ReportOperationProgress(LockTimeoutTimerName, ReportTimerStateNames.Reset, 0);
            }
            if (AutoRelockTimer.IsBusy)
            {
                AutoRelockTimer.CancelAsync();
                _resetEvent.WaitOne();
                ReportOperationProgress(AutoRelockTimerName, ReportTimerStateNames.Reset, 0);
            }
            if (HoldAndReleaseTimer.IsBusy)
            {
                HoldAndReleaseTimer.CancelAsync();
                _resetEvent.WaitOne();
                ReportOperationProgress(HoldAndReleaseTimerName, ReportTimerStateNames.Reset, 0);
            }
        }
        private void ResetDurationTimer()
        {
            if (DurationTimer.IsBusy)
            {
                DurationTimer.CancelAsync();
                _resetEvent.WaitOne();
                ReportOperationProgress(DurationTimerName, ReportTimerStateNames.Reset, 0);
            }
        }

        private void ReportOperationSet()
        {
            if (DoorLockSupportData.TargetDoorLockMode == DoorLockModes.Secured)
            {
                if (DoorLockSupportData.CurrentDoorLockMode != DoorLockSupportData.TargetDoorLockMode)
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "TagetDoorLockMode set to 0x{0:X2}. Door will be secured, Bolt will be locked.", (byte) DoorLockSupportData.TargetDoorLockMode);
                else
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "DoorLockMode stays 0x{0:X2}. Door stays secured, Bolt stays locked.", (byte) DoorLockSupportData.CurrentDoorLockMode);
            }
            if (DoorLockSupportData.TargetDoorLockMode == DoorLockModes.Unsecured
             || DoorLockSupportData.TargetDoorLockMode == DoorLockModes.UnsecuredForInsideHandles
             || DoorLockSupportData.TargetDoorLockMode == DoorLockModes.UnsecuredForOutsideHandles)
            {
                if (DoorLockSupportData.CurrentDoorLockMode == DoorLockModes.Secured)
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "TargetDoorLockMode set to 0x{0:X2} (constant). Door will be unsecured, bolt will be unlocked.", (byte) DoorLockSupportData.TargetDoorLockMode);
                else if (DoorLockSupportData.CurrentDoorLockMode != DoorLockSupportData.TargetDoorLockMode)
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "DoorLockMode is set to 0x{0:X2} (constant). Door stays unsecured, bolt stays unlocked.", (byte) DoorLockSupportData.TargetDoorLockMode);
                else
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "DoorLockMode stays 0x{0:X2} (constant). Door stays unsecured, Bolt stays unlocked.", (byte) DoorLockSupportData.CurrentDoorLockMode);
            }
            if (DoorLockSupportData.TargetDoorLockMode == DoorLockModes.UnsecuredWithTimeout
             || DoorLockSupportData.TargetDoorLockMode == DoorLockModes.UnsecuredForInsideHandlesWithTimeout
             || DoorLockSupportData.TargetDoorLockMode == DoorLockModes.UnsecuredForOutsideHandlesWithTimeout)
            {
                if (DoorLockSupportData.CurrentDoorLockMode == DoorLockModes.Secured)
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "TargetDoorLockMode set to 0x{0:X2} (timed). Door will be unsecured, bolt will be unlocked.", (byte) DoorLockSupportData.TargetDoorLockMode);
                else if (DoorLockSupportData.CurrentDoorLockMode != DoorLockSupportData.TargetDoorLockMode)
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "DoorLockMode is set to 0x{0:X2} (timed). Door stays unsecured, bolt stays unlocked.", (byte) DoorLockSupportData.TargetDoorLockMode);
                else
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "DoorLockMode stays 0x{0:X2} (timed). Door stays unsecured, bolt stays unlocked.", (byte) DoorLockSupportData.CurrentDoorLockMode);
            }
        }
        private void ReportOperationProgress(string timerName, ReportTimerStateNames timerState, int timerValue)
        {
            CommandClassSupportUtils.ReportTimerState(LogLevel, timerName, timerState, timerValue);
        }
        private void ReportOperationFinished()
        {
            if (DoorLockSupportData.CurrentDoorLockMode == DoorLockModes.Secured)
                CommandClassSupportUtils.ReportAddLog(LogLevel, "Door is now secured, Bolt is now locked.");
            else
                CommandClassSupportUtils.ReportAddLog(LogLevel, "Door is now unsecured, bolt is now unlocked.");
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
                    case COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_CONFIGURATION_SET.ID:
                        if (ResultForSv == SvResultNames.Successful)
                            supervisionReport.status = (byte) SvStatusNames.Success;
                        break;
                    case COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_SET.ID:
                        if (ResultForSv == SvResultNames.Successful)
                            supervisionReport.status = (byte) SvStatusNames.Success;
                        break;
                }
                ResultForSv = SvResultNames.Undefined;
            }
        }
    }

    public class DoorLockSupportData : IBasicMapping
    {
        // Public Internals
        public int LogLevel { get; set; } = 0;
        public byte SimulatedDuration { get; set; } = 0x00; // 0...255 seconds
        // Operation Get
        // Initial Door Condition. Internal Default: 0x01 (Closed, Bolt Locked, Latch Open = Secured)
        // Bit 0: 0 = Door Open; 1 = Door Closed
        // Bit 1: 0 = Bolt Locked (out, extended)[=Door Secured]; 1 = Bolt Unlocked (in, retracted)
        // Bit 2: 0 = Latch Open (out, engaged, extended)[=Door Closed]; 1 = Latch Closed (in, disengaged, retracted)
        public byte InitialDoorCondition { get; set; } = 0x01;
        // Initial Door Lock Mode. Internal Default: 0xFF (Secured)
        // ATTENTION: InitialCoorCondition and InitialDoorLockMode MUST match!
        public DoorLockModes InitialDoorLockMode { get; set; } = DoorLockModes.Secured;

        // Internals accessible from TestCases
        // Door Lock Modes:
        // 0x00 = Door Unsecured; 0x01 = Door Unsecured with timeout;
        // 0x10 = Door Unsecured for inside  Door Handles; 0x11 = Door Unsecured for inside  Door Handles with timeout;
        // 0x20 = Door Unsecured for outside Door Handles; 0x21 = Door Unsecured for outside Door Handles with timeout;
        // 0xFF = Door Secured
        public DoorLockModes CurrentDoorLockMode { get; set; } = DoorLockModes.Unsecured;
        public DoorLockModes TargetDoorLockMode { get; set; } = DoorLockModes.Unsecured;

        // Operation Types: Bit 0 = Reserved; Bit 1 = Constant Operation (0x01); Bit 2 = Timed Operation (0x02)
        public DoorLockOperationTypes OperationType { get; set; } = DoorLockOperationTypes.ConstantOperation;

        public byte DoorConditionDoor { get; set; } = 0x01;  // Bit 0: 0 = Door Open; 1 = Door Closed
        public byte DoorConditionBolt { get; set; } = 0x02;  // Bit 1: 0 = Bolt Locked (out, extended)[=Door Closed]; 1 = Bolt Unlocked (in, retracted)
        public byte DoorConditionLatch { get; set; } = 0x04; // Bit 2: 0 = Latch Open (out, engaged, extended)[=Door Closed]; 1 = Latch Closed (in, disengaged, retracted)
        public byte DoorCondition { get { return (byte)(DoorConditionDoor + DoorConditionBolt + DoorConditionLatch); } set => DoorCondition = value; }
        public byte OutsideDoorHandlesState { get; set; } = 0x0F;
        public byte InsideDoorHandlesState { get; set; } = 0x0F;
        public byte LockTimeOutMinutes { get; set; } = 0x00;
        public byte LockTimeOutSeconds { get; set; } = 0x00;
        public byte RemainingLockTimeOutMinutes { get; set; } = 0x00;
        public byte RemainingLockTimeOutSeconds { get; set; } = 0x00;
        public byte[] AutoRelockTime { get; set; } = new byte[] { 0x00, 0x00 };
        public byte[] HoldAndReleaseTime { get; set; } = new byte[] { 0x00, 0x00 };
        public byte BlockToBlock { get; set; } = 0x01;
        public byte TwistAssist { get; set; } = 0x01;

        // Capabilities Report V4+
        // Operation Types: Bit 0 = Reserved; Bit 1 = Constant Operation (0x01); Bit 2 = Timed Operation (0x02)
        public List<DoorLockOperationTypes> SupportedOperationTypesList { get; set; } = new List<DoorLockOperationTypes>() 
        {
            DoorLockOperationTypes.ConstantOperation, 
            DoorLockOperationTypes.TimedOperation, 
        };
        // Door Lock Modes:
        // 0x00 = Door Unsecured; 0x01 = Door Unsecured with timeout;
        // 0x10 = Door Unsecured for inside Door Handles; 0x11 = Door Unsecured for inside Door Handles with timeout;
        // 0x20 = Door Unsecured for outside Door Handles; 0x21 = Door Unsecured for outside Door Handles with timeout;
        // 0xFF = Door Secured
        public List<DoorLockModes> SupportedDoorLockModes { get; set; } = new List<DoorLockModes>()
        { 
            DoorLockModes.Unsecured, DoorLockModes.UnsecuredWithTimeout, 
            DoorLockModes.UnsecuredForInsideHandles, DoorLockModes.UnsecuredForInsideHandlesWithTimeout, 
            DoorLockModes.UnsecuredForOutsideHandles, DoorLockModes.UnsecuredForOutsideHandlesWithTimeout, 
            DoorLockModes.Secured
        };
        public byte SupportedOutsideHandleModesBitMask { get; set; } = 0x0F;// Outside Handles 1+2+3+4
        public byte SupportedInsideHandleModesBitMask { get; set; } = 0x0F; // Inside Handles 1+2+3+4
        public byte SupportedDoorComponents { get; set; } = 0x06;           // Bit 0 = Door; Bit 1 = Bolt; Bit 2 = Latch
        public bool AutoRelockSupport { get; set; } = true;
        public bool AutoRelockReportRemainingTime { get; set; } = false;
        public bool HoldAndReleaseSupport { get; set; } = true;
        public bool TwistAssistSupport { get; set; } = true;
        public bool BlockToBlockSupport { get; set; } = true;

        // Basic CC Mapping
        public DoorLockSupport SupportClassObject;
        bool IBasicMapping.BasicSet(byte value)
        {
            if (SupportClassObject.Version <= 4)
            {
                COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_SET setCmd = new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_SET() { };
                setCmd.doorLockMode = value;
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} accepted.", SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
                return SupportClassObject.HandleOperationSet(setCmd) == SvResultNames.Successful;
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} ignored.", SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
            return false;
        }
        COMMAND_CLASS_BASIC_V2.BASIC_REPORT IBasicMapping.BasicV2Get()
        {
            COMMAND_CLASS_BASIC_V2.BASIC_REPORT rpt = new COMMAND_CLASS_BASIC_V2.BASIC_REPORT() { };
            rpt.currentValue = (byte) (CurrentDoorLockMode == 0x00 ? 0x00 : 0xFF);
            rpt.targetValue = rpt.currentValue;
            rpt.duration = SupportClassObject.RemainingDuration;
            return rpt;
        }
    }

    public enum DoorLockModes : byte
    {
        Unsecured = 0x00,
        UnsecuredWithTimeout = 0x01,
        UnsecuredForInsideHandles = 0x10,
        UnsecuredForInsideHandlesWithTimeout = 0x11,
        UnsecuredForOutsideHandles = 0x20,
        UnsecuredForOutsideHandlesWithTimeout = 0x21,
        Secured = 0xFF
    }

    [Flags]
    public enum DoorLockOperationTypes : byte
    {
        ConstantOperation = 1,
        TimedOperation = 2,
    }
}
