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
using Newtonsoft.Json;
using Zats.Services.CommandClassesImpl;

namespace Zats.Services
{
    [CommandClassName("Door Lock Command Class")]
    [CommandClassId(COMMAND_CLASS_DOOR_LOCK.ID)]
    [MinVersion(1)]
    [MaxVersion(4)]

    // ToDo: Supervision Report Status 'Working'
    // ToDo (nice to have only): Support for InsideDoorHandlesState / OutsideDoorHandlesState
    //      See explanation from SiLabs spec team 2020-Jan-23
    // Interaction with UserCode CC?

    public class DoorLockSupport : CommandClassSupport
    {
        private DoorLockSupportData DoorLockSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return DoorLockSupportData; }
            set { DoorLockSupportData = (DoorLockSupportData)value; }
        }

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

        private NodeTag CmdSrcNode = NodeTag.Empty;

        public DoorLockSupport()
            : this(new DoorLockSupportData() { })
        { }

        public DoorLockSupport(DoorLockSupportData doorLockSupportData)
            : base(COMMAND_CLASS_DOOR_LOCK.ID)
        {
            DoorLockSupportData = doorLockSupportData;
            DoorLockSupportData.SupportClassObject = this;

            if (!doorLockSupportData.SupportedOperationTypesList.Contains(DoorLockOperationTypes.ConstantOperation))
            {
                doorLockSupportData.SupportedOperationTypesList.Add(DoorLockOperationTypes.ConstantOperation);   // Constant Operation (0x01) is always required
            }

            LockTimeoutTimer.DoWork += new DoWorkEventHandler(LockTimeoutTimerDoWork);
            LockTimeoutTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(TimerRunWorkerCompleted);
            LockTimeoutTimer.WorkerSupportsCancellation = true;
            LockTimeoutTimer.WorkerReportsProgress = false;
            AutoRelockTimer.DoWork += new DoWorkEventHandler(AutoRelockTimerDoWork);
            AutoRelockTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(TimerRunWorkerCompleted);
            AutoRelockTimer.WorkerSupportsCancellation = true;
            AutoRelockTimer.WorkerReportsProgress = false;
            HoldAndReleaseTimer.DoWork += new DoWorkEventHandler(HoldAndReleaseTimerDoWork);
            HoldAndReleaseTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(TimerRunWorkerCompleted);
            HoldAndReleaseTimer.WorkerSupportsCancellation = true;
            HoldAndReleaseTimer.WorkerReportsProgress = false;
            DurationTimer.DoWork += new DoWorkEventHandler(DurationTimerDoWork);
            DurationTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(TimerRunWorkerCompleted);
            DurationTimer.WorkerSupportsCancellation = true;
            DurationTimer.WorkerReportsProgress = false;
        }

        public override void StartEmulation()
        {
            DoorLockSupportData.SupportClassObject = this;

            if (!DoorLockSupportData.SupportedOperationTypesList.Contains(DoorLockOperationTypes.ConstantOperation))
            {
                DoorLockSupportData.SupportedOperationTypesList.Add(DoorLockOperationTypes.ConstantOperation);   // Constant Operation (0x01) is always required
            }
            DoorLockSupportData.DoorConditionDoor = (byte)(DoorLockSupportData.InitialDoorCondition & 0x01);
            DoorLockSupportData.DoorConditionBolt = (byte)(DoorLockSupportData.InitialDoorCondition & 0x02);
            DoorLockSupportData.DoorConditionLatch = (byte)(DoorLockSupportData.InitialDoorCondition & 0x04);
            DoorLockSupportData.CurrentDoorLockMode = DoorLockSupportData.InitialDoorLockMode;
            DoorLockSupportData.TargetDoorLockMode = DoorLockSupportData.InitialDoorLockMode;
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
                if (isHandlingAllowed && command != null && command.Length > 1 && DoorLockSupportData != null)
                {
                    byte[] rpt = null;
                    // ** Configuration **
                    if (command[1] == COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_CONFIGURATION_GET.ID)
                    {
                        rpt = PrepareConfigurationReport();
                    }
                    else if (command[1] == COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_CONFIGURATION_SET.ID)
                    {
                        ResultForSv = HandleConfigurationSetCmd(command, sendSrcNode);
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
                        ResultForSv = HandleOperationSetCmd(command, sendSrcNode);
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

        private byte[] PrepareOperationReport()
        {
            if (Version == 3 || Version == 4)
            {
                COMMAND_CLASS_DOOR_LOCK_V3.DOOR_LOCK_OPERATION_REPORT rpt = new COMMAND_CLASS_DOOR_LOCK_V3.DOOR_LOCK_OPERATION_REPORT()
                {
                    properties1 = new COMMAND_CLASS_DOOR_LOCK_V3.DOOR_LOCK_OPERATION_REPORT.Tproperties1()
                    {
                        outsideDoorHandlesMode = DoorLockSupportData.OutsideDoorHandlesEnabled,
                        insideDoorHandlesMode = DoorLockSupportData.InsideDoorHandlesEnabled
                    },
                    doorCondition = DoorLockSupportData.DoorCondition,
                    currentDoorLockMode = (byte)DoorLockSupportData.CurrentDoorLockMode,
                    targetDoorLockMode = (byte)DoorLockSupportData.TargetDoorLockMode,
                    duration = RemainingDuration
                };
                rpt.remainingLockTimeMinutes = 0xFE;
                rpt.remainingLockTimeSeconds = 0xFE;
                //rpt.lockTimeoutMinutes = 0xFE;
                //rpt.lockTimeoutSeconds = 0xFE;
                if (DoorLockSupportData.AutoRelockReportRemainingTime
                 && AutoRelockIsActive
                 && Version == 4)
                {
                    rpt.remainingLockTimeMinutes = DoorLockSupportData.RemainingLockTimeOutMinutes;
                    rpt.remainingLockTimeSeconds = DoorLockSupportData.RemainingLockTimeOutSeconds;
                    //rpt.lockTimeoutMinutes = DoorLockSupportData.RemainingLockTimeOutMinutes;
                    //rpt.lockTimeoutSeconds = DoorLockSupportData.RemainingLockTimeOutSeconds;
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
                        //rpt.lockTimeoutMinutes = DoorLockSupportData.RemainingLockTimeOutMinutes;
                        //rpt.lockTimeoutSeconds = DoorLockSupportData.RemainingLockTimeOutSeconds;
                    }
                }
                return rpt;
            }
            if (Version == 1 || Version == 2)
            {
                COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_REPORT rpt = new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_REPORT()
                {
                    doorLockMode = (byte)DoorLockSupportData.CurrentDoorLockMode,
                    properties1 = new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_REPORT.Tproperties1()
                    {
                        outsideDoorHandlesMode = DoorLockSupportData.OutsideDoorHandlesEnabled,
                        insideDoorHandlesMode = DoorLockSupportData.InsideDoorHandlesEnabled
                    },
                    doorCondition = DoorLockSupportData.DoorCondition
                };
                rpt.remainingLockTimeMinutes = 0xFE;
                rpt.remainingLockTimeSeconds = 0xFE;
                //rpt.lockTimeoutMinutes = 0xFE;
                //rpt.lockTimeoutSeconds = 0xFE;
                if (DoorLockSupportData.OperationType == DoorLockOperationTypes.TimedOperation)
                {
                    rpt.remainingLockTimeMinutes = DoorLockSupportData.RemainingLockTimeOutMinutes;
                    rpt.remainingLockTimeSeconds = DoorLockSupportData.RemainingLockTimeOutSeconds;
                    //rpt.lockTimeoutMinutes = DoorLockSupportData.RemainingLockTimeOutMinutes;
                    //rpt.lockTimeoutSeconds = DoorLockSupportData.RemainingLockTimeOutSeconds;
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
                        ars = (byte)(DoorLockSupportData.AutoRelockSupport ? 0x01 : 0x00),
                        hrs = (byte)(DoorLockSupportData.HoldAndReleaseSupport ? 0x01 : 0x00),
                        tas = (byte)(DoorLockSupportData.TwistAssistSupport ? 0x01 : 0x00),
                        btbs = (byte)(DoorLockSupportData.BlockToBlockSupport ? 0x01 : 0x00),
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
                rpt.operationType = (byte)DoorLockSupportData.OperationType;
                rpt.lockTimeoutMinutes = DoorLockSupportData.LockTimeOutMinutes;
                rpt.lockTimeoutSeconds = DoorLockSupportData.LockTimeOutSeconds;
                rpt.properties1.insideDoorHandlesEnabled = DoorLockSupportData.InsideDoorHandlesEnabled;
                rpt.properties1.outsideDoorHandlesEnabled = DoorLockSupportData.OutsideDoorHandlesEnabled;
                //rpt.properties1.insideDoorHandlesState = DoorLockSupportData.InsideDoorHandlesState;
                //rpt.properties1.outsideDoorHandlesState = DoorLockSupportData.OutsideDoorHandlesState;
                return rpt;
            }
            if (Version == 4)
            {
                COMMAND_CLASS_DOOR_LOCK_V4.DOOR_LOCK_CONFIGURATION_REPORT rpt = new COMMAND_CLASS_DOOR_LOCK_V4.DOOR_LOCK_CONFIGURATION_REPORT() { };
                rpt.operationType = (byte)DoorLockSupportData.OperationType;
                rpt.lockTimeoutMinutes = DoorLockSupportData.LockTimeOutMinutes;
                rpt.lockTimeoutSeconds = DoorLockSupportData.LockTimeOutSeconds;
                rpt.autoRelockTime = CommandClassSupportUtils.UInt64ToByteField(DoorLockSupportData.AutoRelockTime, 2);
                rpt.holdAndReleaseTime = CommandClassSupportUtils.UInt64ToByteField(DoorLockSupportData.HoldAndReleaseTime, 2);
                rpt.properties1.insideDoorHandlesEnabled = DoorLockSupportData.InsideDoorHandlesEnabled;
                rpt.properties1.outsideDoorHandlesEnabled = DoorLockSupportData.OutsideDoorHandlesEnabled;
                //rpt.properties1.insideDoorHandlesState = DoorLockSupportData.InsideDoorHandlesEnabled;
                //rpt.properties1.outsideDoorHandlesState = DoorLockSupportData.OutsideDoorHandlesEnabled;
                rpt.properties2.btb = DoorLockSupportData.BlockToBlock;
                rpt.properties2.ta = DoorLockSupportData.TwistAssist;
                return rpt;
            }
            return null;
        }

        private SvResultNames HandleConfigurationSet(DoorLockOperationTypes newOperationType,
                                                     byte newOutsideDoorHandlesState,
                                                     byte newInsideDoorHandlesState,
                                                     byte newLockTimeOutMinutes,
                                                     byte newLockTimeOutSeconds,
                                                     ushort newAutoRelockTime,
                                                     ushort newHoldAndReleaseTime,
                                                     byte newBlockToBlock,
                                                     byte newTwistAssist,
                                                     bool uiUpdate,
                                                     NodeTag srcNode)
        {
            CmdSrcNode = srcNode;
            SvResultNames success = SvResultNames.Successful;

            if (Version <= 3)
            {
                if (newOperationType == DoorLockOperationTypes.ConstantOperation
                 || newOperationType == DoorLockOperationTypes.TimedOperation)
                {
                    DoorLockSupportData.OperationType = (DoorLockOperationTypes)newOperationType;
                }
                else
                {
                    success = SvResultNames.Failed;
                }
            }
            else
            {
                if ((newOperationType == DoorLockOperationTypes.ConstantOperation && DoorLockSupportData.SupportedOperationTypesList.Contains(DoorLockOperationTypes.ConstantOperation))
                 || (newOperationType == DoorLockOperationTypes.TimedOperation && DoorLockSupportData.SupportedOperationTypesList.Contains(DoorLockOperationTypes.TimedOperation)))
                {
                    DoorLockSupportData.OperationType = newOperationType;
                }
                else
                {
                    success = SvResultNames.Failed;
                }
            }
            // Ignore non-supported handles (set to 0=disabled)
            DoorLockSupportData.OutsideDoorHandlesEnabled = (byte)(newOutsideDoorHandlesState & DoorLockSupportData.SupportedOutsideHandleModesBitMask);
            DoorLockSupportData.InsideDoorHandlesEnabled = (byte)(newInsideDoorHandlesState & DoorLockSupportData.SupportedInsideHandleModesBitMask);

            if (DoorLockSupportData.OperationType == DoorLockOperationTypes.TimedOperation) // Operation Type from incoming command/update is already validated above
            {
                if (newLockTimeOutMinutes <= 0xFC)
                {
                    DoorLockSupportData.LockTimeOutMinutes = newLockTimeOutMinutes;
                }
                else
                {
                    success = SvResultNames.Failed;
                }
                if (newLockTimeOutSeconds <= 0x3B)
                {
                    DoorLockSupportData.LockTimeOutSeconds = newLockTimeOutSeconds;
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
            CommandClassSupportUtils.ReportAddLog(DoorLockSupportData.LogLevel,
                        "Configuration V1-4 Set: OperationType=0x{0:X2}, LockTimeoutMin={1}, LockTimeoutSec={2}",
                        (byte)DoorLockSupportData.OperationType, DoorLockSupportData.LockTimeOutMinutes,
                        DoorLockSupportData.LockTimeOutSeconds);

            if (Version >= 4)
            {
                if (DoorLockSupportData.AutoRelockSupport)
                {
                    DoorLockSupportData.AutoRelockTime = newAutoRelockTime;
                }
                else
                {
                    if (newAutoRelockTime != 0)
                    {
                        success = SvResultNames.Failed;
                    }
                }
                if (DoorLockSupportData.HoldAndReleaseSupport)
                {
                    DoorLockSupportData.HoldAndReleaseTime = newHoldAndReleaseTime;
                }
                else
                {
                    if (newHoldAndReleaseTime != 0)
                    {
                        success = SvResultNames.Failed;
                    }
                }
                if (DoorLockSupportData.BlockToBlockSupport)
                {
                    DoorLockSupportData.BlockToBlock = newBlockToBlock;
                }
                else
                {
                    if (newBlockToBlock != 0)
                    {
                        success = SvResultNames.Failed;
                    }
                }
                if (DoorLockSupportData.TwistAssistSupport)
                {
                    DoorLockSupportData.TwistAssist = newTwistAssist;
                }
                else
                {
                    if (newTwistAssist != 0)
                    {
                        success = SvResultNames.Failed;
                    }
                }
                CommandClassSupportUtils.ReportAddLog(DoorLockSupportData.LogLevel,
                            "Configuration V4+ Set: AutoRelockTime={0} sec, HoldAndReleaseTime={1} sec, BlockToBlock={2}, TwistAssist={3}",
                            DoorLockSupportData.AutoRelockTime,
                            DoorLockSupportData.HoldAndReleaseTime,
                            DoorLockSupportData.BlockToBlock, DoorLockSupportData.TwistAssist);
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
            COMMAND_CLASS_DOOR_LOCK_V4.DOOR_LOCK_CONFIGURATION_SET doorLockConfigurationSetCmd = command;
            ushort autoRelockTime = doorLockConfigurationSetCmd.autoRelockTime.Length > 0?BitConverter.ToUInt16(doorLockConfigurationSetCmd.autoRelockTime.Reverse().ToArray(), 0):(ushort)0;
            ushort holdAndReleaseTime = doorLockConfigurationSetCmd.holdAndReleaseTime.Length > 0?BitConverter.ToUInt16(doorLockConfigurationSetCmd.holdAndReleaseTime.Reverse().ToArray(), 0):(ushort)0;
            return HandleConfigurationSet(
                            (DoorLockOperationTypes) (byte) doorLockConfigurationSetCmd.operationType,
                            doorLockConfigurationSetCmd.properties1.outsideDoorHandlesEnabled,
                            doorLockConfigurationSetCmd.properties1.insideDoorHandlesEnabled,
                            //doorLockConfigurationSetCmd.properties1.outsideDoorHandlesState,
                            //doorLockConfigurationSetCmd.properties1.insideDoorHandlesState,
                            doorLockConfigurationSetCmd.lockTimeoutMinutes,
                            doorLockConfigurationSetCmd.lockTimeoutSeconds,
                            autoRelockTime,
                            holdAndReleaseTime,
                            doorLockConfigurationSetCmd.properties2.btb,
                            doorLockConfigurationSetCmd.properties2.ta, 
                            false,
                            srcNode);
        }

        public SvResultNames HandleOperationSet(DoorLockModes requestedDoorLockMode, bool uiUpdate, NodeTag srcNode)
        {
            CmdSrcNode = srcNode;
            SvResultNames success = SvResultNames.Failed;
            bool changedWithoutTimer = false;

            LockTimeoutTimerName = string.Format("{0} @ {1}: LockTimeout Timer", CommandClassShortName, EndPointName);
            AutoRelockTimerName = string.Format("{0} @ {1}: AutoRelock Timer", CommandClassShortName, EndPointName);
            HoldAndReleaseTimerName = string.Format("{0} @ {1}: HoldAndRelease Timer", CommandClassShortName, EndPointName);
            DurationTimerName = string.Format("{0} @ {1}: Duration Simulation Timer", CommandClassShortName, EndPointName);

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
                            DurationTimerCancel();
                            ReportOperationSet();
                            DurationTimerStart();
                        }
                        else
                        {
                            changedWithoutTimer = requestedDoorLockMode != DoorLockSupportData.CurrentDoorLockMode;
                            ReportOperationSet();
                            DoorLockSupportData.CurrentDoorLockMode = DoorLockSupportData.TargetDoorLockMode;
                        }
                        DoorLockSupportData.DoorConditionBolt = 0x02;
                        if (DoorLockSupportData.LockTimeOutMinutes != 0xFE && DoorLockSupportData.LockTimeOutSeconds != 0xFE)
                        {
                            if (DoorLockSupportData.LockTimeOutMinutes + DoorLockSupportData.LockTimeOutSeconds != 0)
                            {
                                changedWithoutTimer = false;
                                UnsecuredTimersCancel();  // Cancel running 'Door Lock Unsecured' Timer Tasks
                                LockTimerStart();
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
                    UnsecuredTimersCancel();  // Cancel running 'Door Lock Unsecured' Timer Tasks
                    DoorLockSupportData.TargetDoorLockMode = requestedDoorLockMode;
                    if (DoorLockSupportData.CurrentDoorLockMode == DoorLockModes.Secured)
                    {
                        DurationTimerReset();
                        ReportOperationSet();
                        DurationTimerStart();
                    }
                    else
                    {
                        changedWithoutTimer = requestedDoorLockMode != DoorLockSupportData.CurrentDoorLockMode;
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
                        if (DoorLockSupportData.AutoRelockTime != 0)
                        {
                            changedWithoutTimer = false;
                            UnsecuredTimersCancel();  // Cancel running 'Door Lock Unsecured' Timer Tasks
                            AutoRelockTimerStart();
                        }
                    }
                    if (Version >= 4 && DoorLockSupportData.HoldAndReleaseSupport)
                    {
                        if (DoorLockSupportData.HoldAndReleaseTime != 0)
                        {
                            //UnsecuredTimersCancel();   // Cancel running 'Door Lock Unsecured' Timer Tasks
                            if (HoldAndReleaseTimer.IsBusy)
                            {
                                HoldAndReleaseTimer.CancelAsync();
                                _resetEvent.WaitOne();
                            }
                            HoldAndReleaseTimerStart();
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
                        UnsecuredTimersCancel();  // Cancel running 'Door Lock Unsecured' Timer Tasks
                        DurationTimerReset();
                        ReportOperationSet();
                        DurationTimerStart();
                        DoorLockSupportData.DoorConditionBolt = 0x00;
                    }
                    else
                    {
                        changedWithoutTimer = requestedDoorLockMode != DoorLockSupportData.CurrentDoorLockMode;
                        ReportOperationSet();
                    }
                    success = SvResultNames.Successful;
                }
            }

            if (changedWithoutTimer)
            {
                if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                {
                    SendUnsolicitedReport(PrepareOperationReport(), srcNode);
                }
                if (uiUpdate)
                {
                    SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) for state changes without starting a timer
                }
            }

            return success;
        }
        public SvResultNames HandleOperationSetCmd(byte[] command, NodeTag srcNode)
        {
            COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_SET doorLockOperationSetCmd = command;
            DoorLockModes requestedDoorLockMode = (DoorLockModes)(byte)doorLockOperationSetCmd.doorLockMode;
            return HandleOperationSet(requestedDoorLockMode, true, srcNode);
        }

        #region Door Lock Timers
        private void DurationTimerStart()
        {
            DoorLockSupportData.CancelPendingDurationTimer = DurationTimerReset;
            DurationTimer.RunWorkerAsync();
        }
        private void LockTimerStart()
        {
            DoorLockSupportData.CancelPendingLockTimer = UnsecuredTimersReset;
            LockTimeoutTimer.RunWorkerAsync();
        }
        private void AutoRelockTimerStart()
        {
            DoorLockSupportData.CancelPendingAutoRelockTimer = UnsecuredTimersReset;
            AutoRelockTimer.RunWorkerAsync();
        }
        private void HoldAndReleaseTimerStart()
        {
            DoorLockSupportData.CancelPendingHoldAndReleaseTimer = UnsecuredTimersReset;
            HoldAndReleaseTimer.RunWorkerAsync();
        }

        private void LockTimeoutTimerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            int unlockSeconds = DoorLockSupportData.LockTimeOutMinutes * 60 + DoorLockSupportData.LockTimeOutSeconds;

            ReportOperationProgress(LockTimeoutTimerName, ReportTimerStateNames.Started, unlockSeconds);
            for (int seconds = unlockSeconds - 1; seconds >= 0; seconds--)
            {
                DoorLockSupportData.RemainingLockTimeOutMinutes = (byte)(seconds / 60);
                DoorLockSupportData.RemainingLockTimeOutSeconds = (byte)(seconds % 60);
                Thread.Sleep(1000);
                if (Result.State == ActionStates.Cancelled)
                {
                    UnsecuredTimersCancel();
                }
                if (bw.CancellationPending)
                {
                    ReportOperationProgress(LockTimeoutTimerName, ReportTimerStateNames.CancelledElapsedSec, DoorLockSupportData.RemainingLockTimeOutMinutes * 60 + DoorLockSupportData.RemainingLockTimeOutSeconds);
                    e.Cancel = true;
                    break;
                }
            }
            DoorLockSupportData.RemainingLockTimeOutMinutes = 0;
            DoorLockSupportData.RemainingLockTimeOutSeconds = 0;
            // DoorLockSupportData.CurrentDoorLockMode = DoorLockModes.Secured; // will be set only after DurationTimer is expired
            DoorLockSupportData.DoorConditionBolt = 0x00;
            if (!e.Cancel)
            {
                DurationTimerReset();
                DoorLockSupportData.TargetDoorLockMode = DoorLockModes.Secured;
                ReportOperationProgress(LockTimeoutTimerName, ReportTimerStateNames.ExpiredSec, unlockSeconds);
                DurationTimerStart();
            }
        }
        private void AutoRelockTimerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            ushort autoRelockTime = DoorLockSupportData.AutoRelockTime;

            AutoRelockIsActive = true;
            ReportOperationProgress(AutoRelockTimerName, ReportTimerStateNames.Started, autoRelockTime);
            for (int seconds = autoRelockTime; seconds > 0; seconds--)
            {
                if (DoorLockSupportData.AutoRelockReportRemainingTime)
                {
                    DoorLockSupportData.RemainingLockTimeOutMinutes = (byte)(seconds / 60);
                    DoorLockSupportData.RemainingLockTimeOutSeconds = (byte)(seconds % 60);
                }
                Thread.Sleep(1000);
                if (Result.State == ActionStates.Cancelled)
                {
                    UnsecuredTimersCancel();
                }
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
            // DoorLockSupportData.CurrentDoorLockMode = DoorLockModes.Secured; // will be set only after DurationTimer is expired
            if (!e.Cancel)
            {
                DurationTimerCancel();
                DoorLockSupportData.TargetDoorLockMode = DoorLockModes.Secured;
                ReportOperationProgress(AutoRelockTimerName, ReportTimerStateNames.ExpiredSec, autoRelockTime);
                CommandClassSupportUtils.ReportAddLog(DoorLockSupportData.LogLevel, "Door will be secured, bolt will be locked.");
                DurationTimerStart();
            }
        }
        private void HoldAndReleaseTimerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            ushort holdAndReleaseTime = DoorLockSupportData.HoldAndReleaseTime;

            ReportOperationProgress(HoldAndReleaseTimerName, ReportTimerStateNames.Started, holdAndReleaseTime);
            for (int seconds = holdAndReleaseTime; seconds > 0; seconds--)
            {
                Thread.Sleep(1000);
                if (Result.State == ActionStates.Cancelled)
                {
                    UnsecuredTimersCancel();
                }
                if (bw.CancellationPending)
                {
                    ReportOperationProgress(HoldAndReleaseTimerName, ReportTimerStateNames.CancelledElapsedSec, seconds);
                    e.Cancel = true;
                    break;
                }
            }
            // TargetDoorLockMode / CurrentDoorLockMode will not change after HoldAndRelease Timer is expired
            DoorLockSupportData.DoorConditionLatch = 0x04;
            if (!e.Cancel)
            {
                ReportOperationProgress(HoldAndReleaseTimerName, ReportTimerStateNames.ExpiredSec, holdAndReleaseTime);
                if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                {
                    DeviceRestart(CmdSrcNode);
                    SendUnsolicitedReport(PrepareOperationReport(), CmdSrcNode);
                    SupportDelegates?.DeviceSleep(this, ReportWakeupTimeout);
                }
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
            }
        }
        private void DurationTimerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            if (DoorLockSupportData.SimulatedDuration > 0)
            {
                ReportOperationProgress(DurationTimerName, ReportTimerStateNames.Started, DoorLockSupportData.SimulatedDuration);
                for (RemainingDuration = DoorLockSupportData.SimulatedDuration; RemainingDuration > 0; RemainingDuration--)
                {
                    Thread.Sleep(1000);
                    if (Result.State == ActionStates.Cancelled)
                    {
                        DurationTimerCancel();
                    }
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

            if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
            {
                DeviceRestart(CmdSrcNode);
                SendUnsolicitedReport(PrepareOperationReport(), CmdSrcNode);
                SupportDelegates?.DeviceSleep(this, ReportWakeupTimeout);
            }
            SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
        }
        private void TimerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }
        private void UnsecuredTimersCancel()
        {
            DoorLockSupportData.CancelPendingLockTimer?.Invoke();
            DoorLockSupportData.CancelPendingLockTimer = null;
            DoorLockSupportData.CancelPendingAutoRelockTimer?.Invoke();
            DoorLockSupportData.CancelPendingAutoRelockTimer = null;
            DoorLockSupportData.CancelPendingHoldAndReleaseTimer?.Invoke();
            DoorLockSupportData.CancelPendingHoldAndReleaseTimer = null;
        }
        private void UnsecuredTimersReset()
        {
            if (LockTimeoutTimer.IsBusy)
            {
                ReportOperationProgress(LockTimeoutTimerName, ReportTimerStateNames.Reset, 0);
                LockTimeoutTimer.CancelAsync();
                _resetEvent.WaitOne();
            }
            if (AutoRelockTimer.IsBusy)
            {
                ReportOperationProgress(AutoRelockTimerName, ReportTimerStateNames.Reset, 0);
                AutoRelockTimer.CancelAsync();
                _resetEvent.WaitOne();
            }
            if (HoldAndReleaseTimer.IsBusy)
            {
                ReportOperationProgress(HoldAndReleaseTimerName, ReportTimerStateNames.Reset, 0);
                HoldAndReleaseTimer.CancelAsync();
                _resetEvent.WaitOne();
            }
        }
        private void DurationTimerCancel()
        {
            DoorLockSupportData.CancelPendingDurationTimer?.Invoke();
            DoorLockSupportData.CancelPendingDurationTimer = null;
        }
        // This method MUST only be called via <CommandClass>SupportData.CancelPending<CommandName>.Invoke();
        private void DurationTimerReset()
        {
            if (DurationTimer.IsBusy)
            {
                DurationTimer.CancelAsync();
                _resetEvent.WaitOne();
                ReportOperationProgress(DurationTimerName, ReportTimerStateNames.Reset, 0);
            }
        }
        #endregion // Door Lock Timers

        #region Console Reporting
        private void ReportOperationSet()
        {
            if (DoorLockSupportData.TargetDoorLockMode == DoorLockModes.Secured)
            {
                if (DoorLockSupportData.CurrentDoorLockMode != DoorLockSupportData.TargetDoorLockMode)
                {
                    CommandClassSupportUtils.ReportAddLog(DoorLockSupportData.LogLevel,
                                "TagetDoorLockMode set to 0x{0:X2}. Door will be secured, Bolt will be locked.",
                                (byte)DoorLockSupportData.TargetDoorLockMode);
                }
                else
                {
                    CommandClassSupportUtils.ReportAddLog(DoorLockSupportData.LogLevel,
                                "DoorLockMode stays 0x{0:X2}. Door stays secured, Bolt stays locked.",
                                (byte) DoorLockSupportData.CurrentDoorLockMode);
                }
            }
            if (DoorLockSupportData.TargetDoorLockMode == DoorLockModes.Unsecured
             || DoorLockSupportData.TargetDoorLockMode == DoorLockModes.UnsecuredForInsideHandles
             || DoorLockSupportData.TargetDoorLockMode == DoorLockModes.UnsecuredForOutsideHandles)
            {
                if (DoorLockSupportData.CurrentDoorLockMode == DoorLockModes.Secured)
                {
                    CommandClassSupportUtils.ReportAddLog(DoorLockSupportData.LogLevel,
                                "TargetDoorLockMode set to 0x{0:X2} (constant). Door will be unsecured, bolt will be unlocked.",
                                (byte) DoorLockSupportData.TargetDoorLockMode);
                }
                else if (DoorLockSupportData.CurrentDoorLockMode != DoorLockSupportData.TargetDoorLockMode)
                {
                    CommandClassSupportUtils.ReportAddLog(DoorLockSupportData.LogLevel,
                                "DoorLockMode is set to 0x{0:X2} (constant). Door stays unsecured, bolt stays unlocked.",
                                (byte) DoorLockSupportData.TargetDoorLockMode);
                }
                else
                {
                    CommandClassSupportUtils.ReportAddLog(DoorLockSupportData.LogLevel,
                                "DoorLockMode stays 0x{0:X2} (constant). Door stays unsecured, Bolt stays unlocked.",
                                (byte) DoorLockSupportData.CurrentDoorLockMode);
                }
            }
            if (DoorLockSupportData.TargetDoorLockMode == DoorLockModes.UnsecuredWithTimeout
             || DoorLockSupportData.TargetDoorLockMode == DoorLockModes.UnsecuredForInsideHandlesWithTimeout
             || DoorLockSupportData.TargetDoorLockMode == DoorLockModes.UnsecuredForOutsideHandlesWithTimeout)
            {
                if (DoorLockSupportData.CurrentDoorLockMode == DoorLockModes.Secured)
                {
                    CommandClassSupportUtils.ReportAddLog(DoorLockSupportData.LogLevel,
                                "TargetDoorLockMode set to 0x{0:X2} (timed). Door will be unsecured, bolt will be unlocked.",
                                (byte) DoorLockSupportData.TargetDoorLockMode);
                }
                else if (DoorLockSupportData.CurrentDoorLockMode != DoorLockSupportData.TargetDoorLockMode)
                {
                    CommandClassSupportUtils.ReportAddLog(DoorLockSupportData.LogLevel,
                                "DoorLockMode is set to 0x{0:X2} (timed). Door stays unsecured, bolt stays unlocked.",
                                (byte) DoorLockSupportData.TargetDoorLockMode);
                }
                else
                {
                    CommandClassSupportUtils.ReportAddLog(DoorLockSupportData.LogLevel,
                                "DoorLockMode stays 0x{0:X2} (timed). Door stays unsecured, bolt stays unlocked.",
                                (byte) DoorLockSupportData.CurrentDoorLockMode);
                }
            }
        }
        private void ReportOperationProgress(string timerName, ReportTimerStateNames timerState, int timerValue)
        {
            CommandClassSupportUtils.ReportTimerState(DoorLockSupportData.LogLevel, timerName, timerState, timerValue);
        }
        private void ReportOperationFinished()
        {
            if (DoorLockSupportData.CurrentDoorLockMode == DoorLockModes.Secured)
            {
                CommandClassSupportUtils.ReportAddLog(DoorLockSupportData.LogLevel, "Door is now secured, Bolt is now locked.");
            }
            else
            {
                CommandClassSupportUtils.ReportAddLog(DoorLockSupportData.LogLevel, "Door is now unsecured, bolt is now unlocked.");
            }
        }
        #endregion // Console Reporting

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet,
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            {
                if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != CommandClassId)
                {
                    supervisionReport.status = (byte)SvStatusNames.NoSupport;
                    return;
                }

                supervisionReport.status = (byte)SvStatusNames.Fail;  // FAIL as default
                switch (supervisionGet.encapsulatedCommand[1])
                {
                    case COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_CONFIGURATION_SET.ID:
                        if (ResultForSv == SvResultNames.Successful)
                        {
                            supervisionReport.status = (byte)SvStatusNames.Success;
                        }
                        break;
                    case COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_SET.ID:
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
            if (data is DoorLockSupportData newData)
            {
                bool identicalConfiguration = true;
                if (newData.OperationType != DoorLockSupportData.OperationType
                 || newData.OutsideDoorHandlesEnabled != DoorLockSupportData.OutsideDoorHandlesEnabled
                 || newData.InsideDoorHandlesEnabled != DoorLockSupportData.InsideDoorHandlesEnabled
                 || newData.LockTimeOutMinutes != DoorLockSupportData.LockTimeOutMinutes
                 || newData.LockTimeOutSeconds != DoorLockSupportData.LockTimeOutSeconds
                 || newData.AutoRelockTime != DoorLockSupportData.AutoRelockTime
                 || newData.HoldAndReleaseTime != DoorLockSupportData.HoldAndReleaseTime
                 || newData.BlockToBlock != DoorLockSupportData.BlockToBlock
                 || newData.TwistAssist != DoorLockSupportData.TwistAssist)
                {
                    identicalConfiguration = false;
                }

                bool identicalCapabilities = true;
                if (DoorLockSupportData.SupportedOutsideHandleModesBitMask != newData.SupportedOutsideHandleModesBitMask
                 || DoorLockSupportData.SupportedInsideHandleModesBitMask != newData.SupportedInsideHandleModesBitMask
                 || DoorLockSupportData.SupportedDoorComponents != newData.SupportedDoorComponents
                 || DoorLockSupportData.AutoRelockSupport != newData.AutoRelockSupport
                 || DoorLockSupportData.AutoRelockReportRemainingTime != newData.AutoRelockReportRemainingTime
                 || DoorLockSupportData.HoldAndReleaseSupport != newData.HoldAndReleaseSupport
                 || DoorLockSupportData.BlockToBlockSupport != newData.BlockToBlockSupport
                 || DoorLockSupportData.TwistAssistSupport != newData.TwistAssistSupport
                   )
                {
                    identicalCapabilities = false;
                }

                bool identicalOperationTypes = CommandClassSupportUtils.ListElementsEqual(DoorLockSupportData.SupportedOperationTypesList, newData.SupportedOperationTypesList);

                bool identicalDoorLockModes  = CommandClassSupportUtils.ListElementsEqual(DoorLockSupportData.SupportedDoorLockModes, newData.SupportedDoorLockModes);

                if (!identicalConfiguration)
                {
                    HandleConfigurationSet(newData.OperationType,
                            newData.OutsideDoorHandlesEnabled, newData.InsideDoorHandlesEnabled,
                            newData.LockTimeOutMinutes, newData.LockTimeOutSeconds, newData.AutoRelockTime, 
                            newData.HoldAndReleaseTime, newData.BlockToBlock, newData.TwistAssist, 
                            false, NodeTag.Empty);
                }

                if (!identicalCapabilities
                 || !identicalOperationTypes
                 || !identicalDoorLockModes)
                {
                    DoorLockSupportData.SupportedOperationTypesList = newData.SupportedOperationTypesList;
                    DoorLockSupportData.SupportedDoorLockModes = newData.SupportedDoorLockModes;
                    DoorLockSupportData.SupportedOutsideHandleModesBitMask = newData.SupportedOutsideHandleModesBitMask;
                    DoorLockSupportData.SupportedInsideHandleModesBitMask = newData.SupportedInsideHandleModesBitMask;
                    DoorLockSupportData.SupportedDoorComponents = newData.SupportedDoorComponents;
                    DoorLockSupportData.AutoRelockSupport = newData.AutoRelockSupport;
                    DoorLockSupportData.AutoRelockReportRemainingTime = newData.AutoRelockReportRemainingTime;
                    DoorLockSupportData.HoldAndReleaseSupport = newData.HoldAndReleaseSupport;
                    DoorLockSupportData.BlockToBlockSupport = newData.BlockToBlockSupport;
                    DoorLockSupportData.TwistAssistSupport = newData.TwistAssistSupport;
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareCapabilitiesReport(), NodeTag.Empty);
                    }

                }
                if (DoorLockSupportData.CurrentDoorLockMode != newData.CurrentDoorLockMode
                 || DoorLockSupportData.TargetDoorLockMode != newData.TargetDoorLockMode
                 || DoorLockSupportData.DoorConditionDoor != newData.DoorConditionDoor
                 || DoorLockSupportData.DoorConditionBolt != newData.DoorConditionBolt
                 || DoorLockSupportData.DoorConditionLatch != newData.DoorConditionLatch
                 || DoorLockSupportData.RemainingLockTimeOutMinutes != newData.RemainingLockTimeOutMinutes
                 || DoorLockSupportData.RemainingLockTimeOutSeconds != newData.RemainingLockTimeOutSeconds
                    )
                {
                    DoorLockSupportData.CurrentDoorLockMode = newData.CurrentDoorLockMode;
                 // DoorLockSupportData.TargetDoorLockMode = newData.TargetDoorLockMode; // DoorLockMode is changed via HandleOperationSet()
                    DoorLockSupportData.DoorConditionDoor = newData.DoorConditionDoor;
                    DoorLockSupportData.DoorConditionBolt = newData.DoorConditionBolt;
                    DoorLockSupportData.DoorConditionLatch = newData.DoorConditionLatch;
                    DoorLockSupportData.RemainingLockTimeOutMinutes = newData.RemainingLockTimeOutMinutes;
                    DoorLockSupportData.RemainingLockTimeOutSeconds = newData.RemainingLockTimeOutSeconds;

                    if (DoorLockSupportData.TargetDoorLockMode != newData.TargetDoorLockMode)
                    {
                        HandleOperationSet(newData.TargetDoorLockMode, false, NodeTag.Empty);
                    }
                }
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) for current state
            }
        }
    }

    public class DoorLockSupportData : CommandClassSupportData, IBasicMapping
    {
        // Public Internals
        [JsonIgnore]
        public CancelPendingOperationDelegate CancelPendingDurationTimer = null;
        [JsonIgnore]
        public CancelPendingOperationDelegate CancelPendingLockTimer = null;
        [JsonIgnore]
        public CancelPendingOperationDelegate CancelPendingAutoRelockTimer = null;
        [JsonIgnore]
        public CancelPendingOperationDelegate CancelPendingHoldAndReleaseTimer = null;

        public byte SimulatedDuration { get; set; } = 0x00; // 0...255 seconds
        // Internals for Operation
        // Initial Door Condition. Internal Default: 0x01 (Closed, Bolt Locked, Latch Open = Secured)
        // Bit 0: 0 = Door Open; 1 = Door Closed
        // Bit 1: 0 = Bolt Locked (out, extended)[=Door Secured]; 1 = Bolt Unlocked (in, retracted)[=Door can be opened]
        // Bit 2: 0 = Latch Open (out, engaged, extended)[=Door Closed]; 1 = Latch Closed (in, disengaged, retracted)[=Door can be opened]
        public byte InitialDoorCondition { get; set; } = 0x01;
        // Initial Door Lock Mode. Internal Default: 0xFF (Secured)
        // ATTENTION: InitialCoorCondition and InitialDoorLockMode MUST match!
        public DoorLockModes InitialDoorLockMode { get; set; } = DoorLockModes.Secured;

        // Door Lock Modes:
        // 0x00 = Door Unsecured; 0x01 = Door Unsecured with timeout;
        // 0x10 = Door Unsecured for inside  Door Handles; 0x11 = Door Unsecured for inside  Door Handles with timeout;
        // 0x20 = Door Unsecured for outside Door Handles; 0x21 = Door Unsecured for outside Door Handles with timeout;
        // 0xFF = Door Secured

        // Operation Report
        // Remark: The Duration field of the report is not accessible from Testcases or EDE.
        [ParameterSeparator(1, "Door Lock Operation Report")]
        [XmlDataParameter(1, 0x62, 0x03, new byte[] { 0x00 }, EditType.Config/* | EditType.Edit*/)]
        public DoorLockModes CurrentDoorLockMode { get; set; } = DoorLockModes.Secured;

        [XmlDataParameter(3, 0x62, 0x03, new byte[] { 0x05 }, EditType.Config | EditType.Edit)]
        public DoorLockModes TargetDoorLockMode { get; set; } = DoorLockModes.Secured;

        [JsonIgnore]
        public byte DoorConditionDoor { get; set; } = 0x01;  // Bit 0: 0 = Door Open; 1 = Door Closed
        [JsonIgnore]
        public byte DoorConditionBolt { get; set; } = 0x00;  // Bit 1: 0 = Bolt Locked (out, extended)[=Door Closed/Secured]; 1 = Bolt Unlocked (in, retracted)[=Door can be opened]
        [JsonIgnore]
        public byte DoorConditionLatch { get; set; } = 0x00; // Bit 2: 0 = Latch Open (out, engaged, extended)[=Door Closed]; 1 = Latch Closed (in, disengaged, retracted)[=Door can be opened]

        [XmlDataParameter(1, 0x62, 0x03, new byte[] { 0x02 }, EditType.Config | EditType.Edit)]
        public byte DoorCondition {
            get
            {
                return (byte)(DoorConditionDoor + DoorConditionBolt + DoorConditionLatch);
            }
            set
            {
                DoorConditionDoor = (byte)(value & 0x01);
                DoorConditionBolt = (byte)(value & 0x02);
                DoorConditionLatch = (byte)(value & 0x04);
            }
        }

        [XmlDataParameter(3, 0x62, 0x03, new byte[] { 0x03 }, EditType.Config | EditType.Edit)]
        public byte RemainingLockTimeOutMinutes { get; set; } = 0x00;

        [XmlDataParameter(3, 0x62, 0x03, new byte[] { 0x04 }, EditType.Config | EditType.Edit)]
        public byte RemainingLockTimeOutSeconds { get; set; } = 0x00;

        // Configuration Report

        // Operation Types: Bit 0 = Reserved; Bit 1 = Constant Operation (0x01); Bit 2 = Timed Operation (0x02)
        [ParameterSeparator(1, "Door Lock Configuration Report")]
        [XmlDataParameter(1, 0x62, 0x06, new byte[] { 0x00 }, EditType.Config | EditType.Edit)]
        public DoorLockOperationTypes OperationType { get; set; } = DoorLockOperationTypes.ConstantOperation;

        [XmlDataParameter(1, 0x62, 0x06, new byte[] { 0x01, 0x00 }, EditType.Config | EditType.Edit)]
        public byte OutsideDoorHandlesEnabled { get; set; } = 0x0F;

        [XmlDataParameter(1, 0x62, 0x06, new byte[] { 0x01, 0x01 }, EditType.Config | EditType.Edit)]
        public byte InsideDoorHandlesEnabled { get; set; } = 0x0F;

        [XmlDataParameter(1, 0x62, 0x06, new byte[] { 0x02 }, EditType.Config | EditType.Edit)]
        public byte LockTimeOutMinutes { get; set; } = 0x00;

        [XmlDataParameter(1, 0x62, 0x06, new byte[] { 0x03 }, EditType.Config | EditType.Edit)]
        public byte LockTimeOutSeconds { get; set; } = 0x00;

        [XmlDataParameter(4, 0x62, 0x06, new byte[] { 0x04 }, EditType.Config | EditType.Edit)]
        public ushort AutoRelockTime { get; set; } = 0;

        [XmlDataParameter(4, 0x62, 0x06, new byte[] { 0x05 }, EditType.Config | EditType.Edit)]
        public ushort HoldAndReleaseTime { get; set; } = 0;

        [XmlDataParameter(4, 0x62, 0x06, new byte[] { 0x06, 00 }, EditType.Config | EditType.Edit)]
        public byte BlockToBlock { get; set; } = 0x01;

        [XmlDataParameter(4, 0x62, 0x06, new byte[] { 0x06, 0x01 }, EditType.Config | EditType.Edit)]
        public byte TwistAssist { get; set; } = 0x01;

        // Capabilities Report V4+
        // Operation Types: Bit 0 = Reserved; Bit 1 = Constant Operation (0x01); Bit 2 = Timed Operation (0x02)
        // Internal Default: none
        // Possible values: DoorLockOperationTypes.ConstantOperation, DoorLockOperationTypes.TimedOperation
        [ParameterSeparator(4, "Door Lock Capabilities Report")]
        [CustomDataParameter(4, "Supported Operation Type", UiControl.ListCheckBox, InputType.Enum, EditType.Config)]
        public List<DoorLockOperationTypes> SupportedOperationTypesList { get; set; } = new List<DoorLockOperationTypes>();

        // Door Lock Modes:
        // 0x00 = Door Unsecured; 0x01 = Door Unsecured with timeout;
        // 0x10 = Door Unsecured for inside Door Handles; 0x11 = Door Unsecured for inside Door Handles with timeout;
        // 0x20 = Door Unsecured for outside Door Handles; 0x21 = Door Unsecured for outside Door Handles with timeout;
        // 0xFF = Door Secured
        // Internal Default: none
        // Possible values: DoorLockModes.Unsecured, DoorLockModes.UnsecuredWithTimeout,
        // DoorLockModes.UnsecuredForInsideHandles, DoorLockModes.UnsecuredForInsideHandlesWithTimeout,
        // DoorLockModes.UnsecuredForOutsideHandles, DoorLockModes.UnsecuredForOutsideHandlesWithTimeout, 
        // DoorLockModes.Secured
        [CustomDataParameter(4, "Supported Door Lock Mode", UiControl.ListCheckBox, InputType.Enum, EditType.Config)]
        public List<DoorLockModes> SupportedDoorLockModes { get; set; } = new List<DoorLockModes>();

        [XmlDataParameter(4, 0x62, 0x08, new byte[] { 0x04, 0x00 }, EditType.Config)]
        public byte SupportedInsideHandleModesBitMask { get; set; } = 0x0F; // Inside Handles 1+2+3+4

        [XmlDataParameter(4, 0x62, 0x08, new byte[] { 0x04, 0x01 }, EditType.Config)]
        public byte SupportedOutsideHandleModesBitMask { get; set; } = 0x0F;// Outside Handles 1+2+3+4

        [XmlDataParameter(4, 0x62, 0x08, new byte[] { 0x05 }, EditType.Config)]
        public byte SupportedDoorComponents { get; set; } = 0x06;           // Bit 0 = Door; Bit 1 = Bolt; Bit 2 = Latch

        [XmlDataParameter(4, 0x62, 0x08, new byte[] { 0x06, 0x00 }, EditType.Config)]
        public bool BlockToBlockSupport { get; set; } = true;

        [XmlDataParameter(4, 0x62, 0x08, new byte[] { 0x06, 0x01 }, EditType.Config)]
        public bool TwistAssistSupport { get; set; } = true;

        [XmlDataParameter(4, 0x62, 0x08, new byte[] { 0x06, 0x02 }, EditType.Config)]
        public bool HoldAndReleaseSupport { get; set; } = true;

        [XmlDataParameter(4, 0x62, 0x08, new byte[] { 0x06, 0x03 }, EditType.Config)]
        public bool AutoRelockSupport { get; set; } = true;

        public bool AutoRelockReportRemainingTime { get; set; } = false;

        // Basic CC Mapping
        [JsonIgnore]
        public DoorLockSupport SupportClassObject;
        bool IBasicMapping.BasicSet(byte value, NodeTag srcNode)
        {
            if (SupportClassObject.Version <= 4)
            {
                COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_SET setCmd = new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_SET() { };
                setCmd.doorLockMode = value;
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} CC @ {1}: Basic Set with value 0x{2:X2} accepted.", 
                            SupportClassObject.CommandClassShortName, SupportClassObject.EndPointName, value);
                return SupportClassObject.HandleOperationSetCmd(setCmd, srcNode) == SvResultNames.Successful;
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} CC @ {1}: Basic Set with value 0x{2:X2} ignored.", 
                        SupportClassObject.CommandClassShortName, SupportClassObject.EndPointName, value);
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
        [Description("0x00 Unsecured")]
        Unsecured = 0x00,
        [Description("0x01 Unsecured with timeout")]
        UnsecuredWithTimeout = 0x01,
        [Description("0x10 Unsecured for inside handles")]
        UnsecuredForInsideHandles = 0x10,
        [Description("0x11 Unsecured for inside handles with timeout")]
        UnsecuredForInsideHandlesWithTimeout = 0x11,
        [Description("0x20 Unsecured for outside handles")]
        UnsecuredForOutsideHandles = 0x20,
        [Description("0x21 Unsecured for outside handles with timeout")]
        UnsecuredForOutsideHandlesWithTimeout = 0x21,
        [Description("0xFF Secured")]
        Secured = 0xFF
    }

    [Flags]
    public enum DoorLockOperationTypes : byte
    {
        [Description("0x01 Constant operation")]
        ConstantOperation = 1,
        [Description("0x02 Timed operation")]
        TimedOperation = 2,
    }

    public static class DoorLockUtils
    {
        public static bool IsUnlockingType(this DoorLockModes mode)
        {
            return mode != DoorLockModes.Secured;
        }

        public static bool IsLockingType(this DoorLockModes mode)
        {
            return !mode.IsUnlockingType();
        }
    }

}
