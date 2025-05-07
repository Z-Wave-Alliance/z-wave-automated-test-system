using System;
using System.Collections.Generic;
using System.Linq;
using Utils;
using Utils.Threading;
using ZWave.BasicApplication.Operations;
using ZWave.Xml.Application;
using static Zats.Services.BasicActionResult;

namespace Zats.Services
{
    public static class PCControllerActionResult
    {
        public class Void : BasicResult.Void
        {
            public Void(bool isCompleted)
            {
                Init(isCompleted, new ConcurrentList<ZWave.TraceLogItem>());//value.IsStateCompleted, value.TraceLog);
            }
        }

        public class MemoryGetId : BasicResult.MemoryGetId
        {
            public MemoryGetId(MemoryGetIdResult value)
            {
                Init(true, new ConcurrentList<ZWave.TraceLogItem>());//value.IsStateCompleted, value.TraceLog);
                Node = value.Node;
                NodeId = (byte)value.Node.Id;
                HomeId = value.HomeId;
            }
        }

        public class GetVersion : BasicResult.GetVersion
        {
            public GetVersion(VersionResult value) : base(value)
            {
                Init(true, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }

        public class AddNodeWithLearnMode : BasicResult.AddNodeWithLearnMode
        {
            public AddNodeWithLearnMode(UIInstance uiInstance, NodeInfo nodeInfo, bool isEndDeviceAdded, bool IsNwi, bool isReplaced)
            {
                var operation = IsNwi ? uiInstance.FindLastOperation("Add Node completed") : uiInstance.FindLastOperation("Add Node");
                if (isReplaced)
                {
                    operation = uiInstance.FindLastOperation("Replace Failed Node completed");
                }
                var operationCompleted = !string.IsNullOrEmpty(operation) && !operation.ToLower().Contains("cancelled");
                if (nodeInfo != null)
                {
                    NodeId = nodeInfo.Id;
                    Node = new Node(nodeInfo.Id, nodeInfo.EndPointId);
                    bool securityS0 = nodeInfo.Name.Contains("[S0]");
                    bool securityS2 = nodeInfo.Name.Contains("[S2]");
                    SubstituteStatus = (securityS0 || securityS2) ? SubstituteStatuses.Done : SubstituteStatuses.None;
                    AddRemoveNodeStatus = nodeInfo.AddRemoveNodeStatus;
                }
                SubstituteStatus = operation.ToLower().Contains("(security failed)") ? SubstituteStatuses.Failed : SubstituteStatus;
                AddRemoveNodeStatus = isReplaced ? AddRemoveNodeStatuses.Replaced : AddRemoveNodeStatus;
                IsEndDevice = isEndDeviceAdded;

                Init(operationCompleted, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }

        public class AddNodeWithLearnModeNWI : BasicResult.AddNodeWithLearnMode
        {
            public AddNodeWithLearnModeNWI(UIInstance uiInstance, NodeInfo nodeInfo, bool isEndDeviceAdded)
            {
                const int LOG_RECORD_COLUMN = 1;
                const int MIN_RECORDS_IN_LOG = 4;
                bool completed = false;
                using (var logDlg = uiInstance.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Log",
                    () => uiInstance.Automation.NavigateTo(PCControllerIdMap.MainWindow.ShowLogBtn).Click(),
                    null))
                {
                    var logListView = logDlg.DialogView.NavigateTo(PCControllerIdMap.LogView.LogLv);
                    int logRecordsCount = logListView.GetGridRowCount();
                    for (int i = logRecordsCount - 1; i >= 0; i--)
                    {
                        var record = logListView.GetValueFromGrid(i, LOG_RECORD_COLUMN);
                        if (record.ToLower().Contains("add node cancelled"))
                        {
                            if (i >= MIN_RECORDS_IN_LOG - 1)
                            {
                                if (logListView.GetValueFromGrid(i - 1, LOG_RECORD_COLUMN).ToLower().Contains("add node started") &&
                                    logListView.GetValueFromGrid(i - 2, LOG_RECORD_COLUMN).ToLower().Contains("add node completed") &&
                                    logListView.GetValueFromGrid(i - 3, LOG_RECORD_COLUMN).ToLower().Contains("add node started"))
                                {
                                    completed = true;
                                }
                                break;
                            }
                        }
                    }
                }
                if (completed)
                {
                    NodeId = nodeInfo.Id;
                    Node = new Node(nodeInfo.Id, nodeInfo.EndPointId);
                    bool securityS0 = nodeInfo.Name.Contains("[S0]");
                    bool securityS2 = nodeInfo.Name.Contains("[S2]");
                    SubstituteStatus = (securityS0 || securityS2) ? SubstituteStatuses.Done : SubstituteStatuses.Failed;
                }
                IsEndDevice = isEndDeviceAdded;
                Init(completed, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }

        public class LearnModeResult : BasicResult.SetLearnMode
        {
            public LearnModeResult(UIInstance uiInstance, NodeInfo nodeInfo)
            {
                var operation = uiInstance.FindLastOperation("learn mode"); //completed
                if (string.IsNullOrEmpty(operation))
                {
                    operation = uiInstance.FindLastOperation("learning mode started");
                }
                var operationCompleted = !string.IsNullOrEmpty(operation) && nodeInfo != null;
                if (operationCompleted)
                {
                    if (!operation.ToLower().Contains("security failed"))
                    {
                        SubstituteStatus = SubstituteStatuses.Done;
                    }
                    else
                    {
                        SubstituteStatus = SubstituteStatuses.Failed;
                    }
                    NodeId = nodeInfo.Id;
                    Node = new Node(nodeInfo.Id, nodeInfo.EndPointId);
                    SetLearnModeResult setLearnModeResult = new SetLearnModeResult();
                    setLearnModeResult.SubstituteStatus = ZWave.Enums.SubstituteStatuses.Done;
                    setLearnModeResult.Node = new ZWave.Devices.NodeTag(NodeId);
                }
                Init(operationCompleted, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }

        public class IncludeNodeWithLearnMode : BasicResult.IncludeNodeWithLearnMode
        {
            public IncludeNodeWithLearnMode(BasicResult.AddNodeWithLearnMode addNodeResult)
            {
                AddNodeResult = addNodeResult;
                SetLearnMode = addNodeResult.SetLearnMode;
                Init(addNodeResult, new ConcurrentList<ZWave.TraceLogItem>());
            }

            public IncludeNodeWithLearnMode(UIInstance uiInstance, NodeInfo nodeInfo)
            {
                AddRemoveNodeResult addRemoveNodeResult = new AddRemoveNodeResult();
                SetLearnModeResult setLearnModeResult = new SetLearnModeResult();

                var operation = uiInstance.FindLastOperation("Add Node");
                var operationCompleted = !string.IsNullOrEmpty(operation)/* && !operation.ToLower().Contains("cancelled")*/;
                if (operationCompleted)
                {
                    if (nodeInfo != null)
                    {
                        addRemoveNodeResult.Node = new ZWave.Devices.NodeTag(nodeInfo.Id);
                        //addRemoveNodeResult.Id = nodeInfo.Id;
                        bool securityS0 = nodeInfo.Name.Contains("[S0]");
                        bool securityS2 = nodeInfo.Name.Contains("[S2]");
                        addRemoveNodeResult.SubstituteStatus = (securityS0 || securityS2) ? ZWave.Enums.SubstituteStatuses.Done : ZWave.Enums.SubstituteStatuses.None;
                    }
                    addRemoveNodeResult.SubstituteStatus = operation.ToLower().Contains("(security failed)") ? ZWave.Enums.SubstituteStatuses.Failed : addRemoveNodeResult.SubstituteStatus;
                    AddNodeResult = new BasicActionResult.AddNodeWithLearnMode(addRemoveNodeResult, setLearnModeResult);
                }
                Init(operationCompleted, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }

        public class AddNode : BasicResult.AddNode
        {
            public AddNode(UIInstance uiInstance, NodeInfo nodeInfo, bool isReplaced = false)
            {
                var operation = uiInstance.FindLastOperation("Add Node");
                if (isReplaced)
                {
                    operation = uiInstance.FindLastOperation("Replace Failed Node completed");
                }
                var operationCompleted = !string.IsNullOrEmpty(operation)/* && !operation.ToLower().Contains("cancelled")*/;
                if (operationCompleted)
                {
                    if (nodeInfo != null)
                    {
                        NodeId = nodeInfo.Id;
                        Node = new Node(nodeInfo.Id, nodeInfo.EndPointId);
                        bool securityS0 = nodeInfo.Name.Contains("[S0]");
                        bool securityS2 = nodeInfo.Name.Contains("[S2]");
                        SubstituteStatus = (securityS0 || securityS2) ? SubstituteStatuses.Done : SubstituteStatuses.None;
                        AddRemoveNodeStatus = nodeInfo.AddRemoveNodeStatus;
                    }
                    SubstituteStatus = operation.ToLower().Contains("(security failed)") ? SubstituteStatuses.Failed : SubstituteStatus;
                    AddRemoveNodeStatus = isReplaced ? AddRemoveNodeStatuses.Replaced : AddRemoveNodeStatus;
                }
                IsEndDevice = true;

                Init(operationCompleted, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }

        public class IncludeNode : BasicResult.IncludeNode
        {
            public IncludeNode(AddNode addNodeRes, BasicResult.SendData sendData)
            {
                AddNodeResult = addNodeRes;
                SetWakeUpInterval = sendData;
                Init(addNodeRes, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }

        public class RemoveNode : BasicResult.RemoveNode
        {
            public RemoveNode(UIInstance uiInstance)
            {
                var operation = uiInstance.FindLastOperation("Remove Node");
                var completed = !string.IsNullOrEmpty(operation);
                if (!completed)
                {
                    operation = uiInstance.FindLastOperation("Remove Failed Node");
                }
                completed = !string.IsNullOrEmpty(operation) && operation.ToLower().Contains("completed");
                Init(completed, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }

        public class RemoveNodeNWE : BasicResult.RemoveNode
        {
            public RemoveNodeNWE(UIInstance uiInstance)
            {
                const int LOG_RECORD_COLUMN = 1;
                const int MIN_RECORDS_IN_LOG = 4;
                bool completed = false;
                using (var logDlg = uiInstance.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Log",
                    () => uiInstance.Automation.NavigateTo(PCControllerIdMap.MainWindow.ShowLogBtn).Click(),
                    null))
                {
                    var logListView = logDlg.DialogView.NavigateTo(PCControllerIdMap.LogView.LogLv);
                    int logRecordsCount = logListView.GetGridRowCount();
                    for (int i = logRecordsCount - 1; i >= 0; i--)
                    {
                        var record = logListView.GetValueFromGrid(i, LOG_RECORD_COLUMN);
                        if (record.ToLower().Contains("remove node cancelled"))
                        {
                            if (i >= MIN_RECORDS_IN_LOG - 1)
                            {
                                if (logListView.GetValueFromGrid(i - 1, LOG_RECORD_COLUMN).ToLower().Contains("remove node started") &&
                                    logListView.GetValueFromGrid(i - 2, LOG_RECORD_COLUMN).ToLower().Contains("remove node completed") &&
                                    logListView.GetValueFromGrid(i - 3, LOG_RECORD_COLUMN).ToLower().Contains("remove node started"))
                                {
                                    completed = true;
                                }
                                break;
                            }
                        }
                    }
                }
                Init(completed, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }

        public class SendData : BasicResult.SendData
        {
            public SendData(UIInstance uiInstance)
            {
                var operation = uiInstance.FindLastOperation("Send");
                var completed = !string.IsNullOrEmpty(operation) && operation.ToLower().Contains("completed");
                Init(completed, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }

        public class SendDataNop : BasicResult.SendData
        {
            public SendDataNop(UIInstance uiInstance)
            {
                var operation = uiInstance.FindLastOperation("Send NOP");
                var completed = !string.IsNullOrEmpty(operation) && operation.ToLower().Contains("completed");
                Init(completed, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }

        public class SendDataNopEnqueue : BasicResult.SendData
        {
            public SendDataNopEnqueue(UIInstance uiInstance)
            {
                var operation = uiInstance.FindLastOperationCommandQueue("Send NOP");
                var completed = !string.IsNullOrEmpty(operation);
                Init(completed, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }

        public class RequestData : BasicResult.RequestData
        {
            public RequestData(UIInstance uiInstance, ZWaveDefinition zWaveDefinition, byte nodeId, byte[] expectedCommand)
            {
                if (uiInstance == null)
                {
                    Init(false, new ConcurrentList<ZWave.TraceLogItem>());
                    return;
                }
                string expectedCommandName = zWaveDefinition.ParseApplicationStringName(expectedCommand);
                string logRecord = uiInstance.FindLastOperation(expectedCommandName);
                var completed = VerifyCommand(zWaveDefinition, logRecord, expectedCommand);
                Init(completed, new ConcurrentList<ZWave.TraceLogItem>());
                List<byte> payload = new List<byte>();
                payload.AddRange(expectedCommand.Take(2));
                var ix = logRecord.IndexOf(" + ");
                if (ix > 0)
                {
                    var logData = logRecord.Remove(0, logRecord.IndexOf(" + ") + 2);
                    var data = Tools.GetBytes(logData);
                    payload.AddRange(data);
                }
                Command = payload.ToArray();
                Node = new Node(nodeId);
            }

            private bool VerifyCommand(ZWaveDefinition zWaveDefinition, string logRecord, byte[] command)
            {
                bool ret = false;
                var logRecordParser = new LogRecordParser(zWaveDefinition);
                try
                {
                    var res = logRecordParser.Parse(logRecord);
                    var cmdName = res.CommandName;
                    var expectedCmdName = zWaveDefinition.ParseApplicationStringName(command);
                    RxSubstituteInfo = new SubstitutionInfo();
                    RxSubstituteInfo.SubstituteStatus = res.SecurityScheme != SecuritySchemes.NONE ? SubstituteStatuses.Done : SubstituteStatuses.Failed;
                    RxSecurityScheme = res.SecurityScheme;
                    ret = string.Compare(expectedCmdName, cmdName, true) == 0;
                }
                catch (System.ArgumentException)
                {
                    RxSubstituteInfo = null;
                    ret = false;
                }
                return ret;
            }
        }

        public class ExpectData : BasicResult.ExpectData
        {
            public ExpectData(UIInstance uiInstance, ZWaveDefinition zWaveDefinition, byte[] expectedCommand, int numberOfBytesToCompare)
            {
                if (uiInstance == null)
                {
                    Init(false, new ConcurrentList<ZWave.TraceLogItem>());
                    return;
                }

                var expectedCmdName = zWaveDefinition.ParseApplicationStringName(expectedCommand);
                if (numberOfBytesToCompare > 2)
                {
                    expectedCmdName = string.Format("{0}({1}) + {2}",
                        expectedCmdName,
                        expectedCommand.Take(2).ToArray().ToHexString(),
                        expectedCommand.Skip(2).ToArray().ToHexString());
                }
                var operation = uiInstance.FindRecordFromLogView(true,
                    (record) =>
                    {
                        bool ret = false;
                        if (record.Contains("Rx") && record.ToLower().Contains(expectedCmdName.ToLower()))
                        {
                            ret = true;
                        }
                        return ret;
                    });
                var isStateCompleted = !string.IsNullOrEmpty(operation) && operation.Contains(expectedCmdName);
                if (isStateCompleted)
                {
                    var logRecordParser = new LogRecordParser(zWaveDefinition);
                    LogRecordParser.LogRecordParserResult res = new LogRecordParser.LogRecordParserResult(new byte[0], string.Empty, new byte[0], SecuritySchemes.NONE);
                    try
                    {
                        res = logRecordParser.Parse(operation);
                    }
                    catch (ArgumentException aex)
                    {
                        $"{aex.Message}"._DLOG();
                        isStateCompleted = false;
                    }
                    if (isStateCompleted)
                    {
                        RxSubstituteInfo = new SubstitutionInfo();
                        SecurityScheme = res.SecurityScheme;
                        RxSubstituteInfo.SubstituteStatus = res.SecurityScheme != SecuritySchemes.NONE ? SubstituteStatuses.Done : SubstituteStatuses.Failed;
                        if (expectedCommand.Length > 2 && numberOfBytesToCompare > 2)
                        {
                            isStateCompleted = expectedCommand.Skip(2).Take(numberOfBytesToCompare - 2).SequenceEqual(res.Payload);
                        }
                        Command = res.Payload == null ? res.Command : res.Command.Concat(res.Payload).ToArray();
                    }
                }
                Init(isStateCompleted, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }

        public class ExpectDataWithS2Extension : BasicResult.ExpectData
        {
            public ExpectDataWithS2Extension(UIInstance uiInstance, ZWaveDefinition zWaveDefinition, SecurityS2ExtensionTypes[] expectedS2ExtensionTypes)
            {
                if (uiInstance == null)
                {
                    Init(false, new ConcurrentList<ZWave.TraceLogItem>());
                    return;
                }
                byte[] expectedS2Msg = new byte[] { 0x9F, 0x03 };
                var expectedCmdName = zWaveDefinition.ParseApplicationStringName(expectedS2Msg);

                var operation = uiInstance.FindRecordFromLogView(true,
                (record) =>
                {
                    bool ret = false;
                    if (record.Contains("Rx") && record.ToLower().Contains(expectedCmdName.ToLower()))
                    {
                        var logRecordParser = new LogRecordParser(zWaveDefinition);
                        LogRecordParser.LogRecordParserResult res = new LogRecordParser.LogRecordParserResult(new byte[0], string.Empty, new byte[0], SecuritySchemes.NONE);
                        List<ExtenssionInfo> resExtensions = new List<ExtenssionInfo>();
                        try
                        {
                            res = logRecordParser.Parse(record);
                            var s2Msg = (COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION)res.Command.Concat(res.Payload).ToArray();
                            if (s2Msg.properties1.extension > 0 && s2Msg.vg1.Any())
                            {
                                foreach (var foundExt in s2Msg.vg1)
                                {
                                    var extInfo = new ExtenssionInfo
                                    {
                                        extension = foundExt.extension,
                                        extensionLength = foundExt.extensionLength,
                                        properties1 = foundExt.properties1,
                                        EncryptedWithScheme = res.SecurityScheme
                                    };
                                    resExtensions.Add(extInfo);
                                }
                                if (expectedS2ExtensionTypes != null && expectedS2ExtensionTypes.Length > 0)
                                {
                                    ret = expectedS2ExtensionTypes.All(x => resExtensions.Any(y => y.properties1.type == (byte)x));
                                }
                                else
                                {
                                    ret = true;
                                }
                            }
                        }
                        catch (ArgumentException aex)
                        {
                            $"{aex.Message}"._DLOG();
                        }
                            RxSubstituteInfo = new SubstitutionInfo();
                            SecurityScheme = res.SecurityScheme;
                            RxSubstituteInfo.SubstituteStatus = res.SecurityScheme != SecuritySchemes.NONE ? SubstituteStatuses.Done : SubstituteStatuses.Failed;
                            Command = res.Payload == null ? res.Command : res.Command.Concat(res.Payload).ToArray();
                            Extensions = resExtensions;
                    }
                    return ret;
                });

                var isStateCompleted = !string.IsNullOrEmpty(operation);
                Init(isStateCompleted, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }


        public class RequestNodeInfo : BasicResult.RequestNodeInfo
        {
            public RequestNodeInfo(UIInstance uiInstance, ZWaveDefinition zWaveDefinition)
            {
                if (uiInstance == null)
                {
                    Init(false, new ConcurrentList<ZWave.TraceLogItem>());
                    return;
                }

                var operation = uiInstance.FindLastOperation("Node Info");
                var completed = !string.IsNullOrEmpty(operation) && operation.ToLower().Contains("completed");
                if (completed)
                {
                    var expectedCmdNameS2 = zWaveDefinition.ParseApplicationStringName(new COMMAND_CLASS_SECURITY_2.SECURITY_2_COMMANDS_SUPPORTED_REPORT());
                    var supportedCCS2 = uiInstance.FindLastOperation(expectedCmdNameS2);
                    completed = !string.IsNullOrEmpty(supportedCCS2) && supportedCCS2.Contains(expectedCmdNameS2);
                    if (completed)
                    {
                        var logRecordParser = new LogRecordParser(zWaveDefinition);
                        var res = logRecordParser.Parse(supportedCCS2);
                        NodeInfo = res.Payload;
                    }
                    else
                    {
                        var expectedCmdNameS0 = zWaveDefinition.ParseApplicationStringName(new COMMAND_CLASS_SECURITY.SECURITY_COMMANDS_SUPPORTED_REPORT());
                        var supportedCCS0 = uiInstance.FindLastOperation(expectedCmdNameS0);
                        completed = !string.IsNullOrEmpty(supportedCCS0) && supportedCCS0.Contains(expectedCmdNameS0);
                        if (completed)
                        {
                            var logRecordParser = new LogRecordParser(zWaveDefinition);
                            var res = logRecordParser.Parse(supportedCCS0);
                            NodeInfo = res.Payload;
                        }
                    }
                }
                Init(completed, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }

        public class GetNodeInfo : BasicResult.GetNodeInfo
        {
            public GetNodeInfo(UIInstance uiInstance, ZWaveDefinition zWaveDefinition)
            {
                if (uiInstance == null)
                {
                    Init(false, new ConcurrentList<ZWave.TraceLogItem>());
                    return;
                }

                var operation = uiInstance.FindLastOperation("Node Info");
                var completed = !string.IsNullOrEmpty(operation) && operation.ToLower().Contains("received");
                if (completed)
                {
                    var expectedCmdNameS2 = zWaveDefinition.ParseApplicationStringName(new COMMAND_CLASS_SECURITY_2.SECURITY_2_COMMANDS_SUPPORTED_REPORT());
                    var supportedCCS2 = uiInstance.FindLastOperation(expectedCmdNameS2);
                    completed = !string.IsNullOrEmpty(supportedCCS2) && supportedCCS2.Contains(expectedCmdNameS2);
                    if (completed)
                    {
                        var logRecordParser = new LogRecordParser(zWaveDefinition);
                        var res = logRecordParser.Parse(supportedCCS2);
                        NodeInfo = res.Payload;
                    }
                    else
                    {
                        var expectedCmdNameS0 = zWaveDefinition.ParseApplicationStringName(new COMMAND_CLASS_SECURITY.SECURITY_COMMANDS_SUPPORTED_REPORT());
                        var supportedCCS0 = uiInstance.FindLastOperation(expectedCmdNameS0);
                        completed = !string.IsNullOrEmpty(supportedCCS0) && supportedCCS0.Contains(expectedCmdNameS0);
                        if (completed)
                        {
                            var logRecordParser = new LogRecordParser(zWaveDefinition);
                            var res = logRecordParser.Parse(supportedCCS0);
                            NodeInfo = res.Payload;
                        }
                    }
                }
                Init(completed, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }

        public class SetSucNodeId : BasicResult.SetSucNodeId
        {
            public SetSucNodeId(UIInstance uiInstance)
            {
                if (uiInstance == null)
                {
                    Init(false, new ConcurrentList<ZWave.TraceLogItem>());
                    return;
                }

                var operation = uiInstance.FindLastOperation("Set SIS");
                var completed = !string.IsNullOrEmpty(operation) && operation.ToLower().Contains("completed");
                if (completed)
                {
                    this.RetVal = SetSucReturnValues.SucSetSucceeded;
                }
                Init(completed, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }

        public class IsFailedNode : BasicResult.IsFailedNode
        {
            public IsFailedNode(UIInstance uiInstance)
            {
                if (uiInstance == null)
                {
                    Init(false, new ConcurrentList<ZWave.TraceLogItem>());
                    return;
                }

                var operation = uiInstance.FindLastOperation("Is Failed");
                var completed = !string.IsNullOrEmpty(operation) && operation.ToLower().Contains("completed");
                if (completed)
                {
                    operation = uiInstance.FindLastOperation("status - is ");
                    this.RetValue = operation.Contains("is not failed") ? false : true;
                }
                Init(completed, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }

        public class RequestNodeNeighborUpdate : BasicResult.RequestNodeNeighborUpdate
        {
            public RequestNodeNeighborUpdate(UIInstance uiInstance, ZWaveDefinition zWaveDefinition)
            {
                if (uiInstance == null)
                {
                    Init(false, new ConcurrentList<ZWave.TraceLogItem>());
                    return;
                }

                var operation = uiInstance.FindLastOperation("Request Neighbors Update");
                var completed = !string.IsNullOrEmpty(operation) && operation.ToLower().Contains("done");
                NeighborUpdateStatus = completed ? RequestNeighborUpdateStatuses.Done : RequestNeighborUpdateStatuses.Failed;
                Init(completed, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }


        public class SetPriorityRoute : BasicResult.SetPriorityRoute
        {
            public SetPriorityRoute(UIInstance uiInstance) :
                base(new SetPriorityRouteResult())
            {
                var operation = uiInstance.FindLastOperation("Set Priority Route");
                var completed = !string.IsNullOrEmpty(operation) && operation.ToLower().Contains("completed");
                Init(completed, new ConcurrentList<ZWave.TraceLogItem>());
            }
        }

        public class SetDefaultZip : ZipResult.SetDefault
        {
            public SetDefaultZip() :
                base(new ZWave.ZipApplication.Operations.SetDefaultResult())
            {
            }
        }

        public class AddNodeZip : ZipResult.AddNode
        {
            public AddNodeZip() :
                base(new ZWave.ZipApplication.Operations.AddNodeResult())
            {
            }
        }

        public class LearnModeZip : ZipResult.LearnMode
        {
            public LearnModeZip() :
                base(new ZWave.ZipApplication.Operations.SetLearnModeResult())
            {
            }
        }

        public class RemoveNodeZip : ZipResult.RemoveNode
        {
            public RemoveNodeZip() :
                base(new ZWave.ZipApplication.Operations.RemoveNodeResult())
            {
            }
        }

        public class ExpectSmartStartAdd : BasicResult.ExpectSmartStartAdd
        {
            public ExpectSmartStartAdd(AddNode value)
            {
                if (value != null)
                {
                    Init(value, value.ElapsedMs, new ConcurrentList<ZWave.TraceLogItem>());
                    AddNodeResult = value;
                }
            }
        }
    }
}
