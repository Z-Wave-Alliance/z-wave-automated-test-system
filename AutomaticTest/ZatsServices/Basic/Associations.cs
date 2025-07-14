// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using Utils;
using Utils.UI.Bind;
using Utils.UI.Wrappers;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using ZWave.BasicApplication.Tasks;
using ZWave.Configuration;
using ZWave.Devices;
using System.Linq;
using System.Collections;
using ZWave;
using ZWave.Xml.Application;
using System.Collections.ObjectModel;

namespace Zats.Services
{
    public class Associations : IAssociations
    {
        const int DEFAULT_REQ_TM = 5000;
        public ZWave.Enums.TransmitOptions TX_OPTIONS = ZWave.Enums.TransmitOptions.TransmitOptionAcknowledge | ZWave.Enums.TransmitOptions.TransmitOptionAutoRoute | ZWave.Enums.TransmitOptions.TransmitOptionExplore;
        private DeviceManager DeviceManager { get; set; }
        public Associations(DeviceManager deviceManager)
        {
            DeviceManager = deviceManager;
        }

        public byte[] AssociationGetCommandList(Alias controllerNumber, Node associativeDevice, byte groupId)
        {
            Device device = DeviceManager.GetDevice(controllerNumber);

            COMMAND_CLASS_ASSOCIATION_GRP_INFO.ASSOCIATION_GROUP_COMMAND_LIST_GET cmd = new COMMAND_CLASS_ASSOCIATION_GRP_INFO.ASSOCIATION_GROUP_COMMAND_LIST_GET();
            COMMAND_CLASS_ASSOCIATION_GRP_INFO.ASSOCIATION_GROUP_COMMAND_LIST_REPORT rpt = new COMMAND_CLASS_ASSOCIATION_GRP_INFO.ASSOCIATION_GROUP_COMMAND_LIST_REPORT();
            cmd.groupingIdentifier = groupId;
            RequestDataResult result = device.RequestData(associativeDevice, cmd, TX_OPTIONS, rpt, DEFAULT_REQ_TM);

            rpt = result.Command;
            byte[] ret = new byte[rpt.command.Count];

            if (result)
            {
                ret = rpt.command.ToArray();
            }
            else
            {
                ret = null;
            }

            return ret;
        }
        public List<string> AssociationGetGroupInfo(Alias controllerNumber, Node associativeDevice, byte groupId)
        {
            Device device = DeviceManager.GetDevice(controllerNumber);

            List<string> ret = new List<string>();

            COMMAND_CLASS_ASSOCIATION_GRP_INFO.ASSOCIATION_GROUP_INFO_GET cmd = new COMMAND_CLASS_ASSOCIATION_GRP_INFO.ASSOCIATION_GROUP_INFO_GET();
            COMMAND_CLASS_ASSOCIATION_GRP_INFO.ASSOCIATION_GROUP_INFO_REPORT rpt = new COMMAND_CLASS_ASSOCIATION_GRP_INFO.ASSOCIATION_GROUP_INFO_REPORT();
            cmd.groupingIdentifier = groupId;
            RequestDataResult result = device.RequestData(associativeDevice, cmd, TX_OPTIONS, rpt, DEFAULT_REQ_TM);
            if (result)
            {
                rpt = result.Command;
                DeviceManager.ZWaveDefinition.ParseApplicationObject(rpt, out CommandClassValue[] ccv);
                if (ccv != null && ccv.Length > 0)
                {
                    var pVal = ccv[0].CommandValue.ParamValues;
                    if (pVal.Count > 1)
                    {
                        string profile1 = pVal[1].InnerValues.Where(x => x.ParamDefinitionText == "Profile1").FirstOrDefault().TextValue;
                        string profile2 = pVal[1].InnerValues.Where(x => x.ParamDefinitionText == "Profile2").FirstOrDefault().TextValue;

                        ret = new List<string> { profile1, profile2 };
                    }
                }
            }
            return ret;
        }

        public byte[] AssociationGetCommandList(Alias controllerNumber, byte associativeDeviceId, byte endpointId, byte groupId)
        {
            Device device = DeviceManager.GetDevice(controllerNumber);
            byte[] ret = new byte[] { };

            COMMAND_CLASS_ASSOCIATION_GRP_INFO.ASSOCIATION_GROUP_COMMAND_LIST_GET cmd = new COMMAND_CLASS_ASSOCIATION_GRP_INFO.ASSOCIATION_GROUP_COMMAND_LIST_GET();
            COMMAND_CLASS_ASSOCIATION_GRP_INFO.ASSOCIATION_GROUP_COMMAND_LIST_REPORT rpt = new COMMAND_CLASS_ASSOCIATION_GRP_INFO.ASSOCIATION_GROUP_COMMAND_LIST_REPORT();
            cmd.groupingIdentifier = groupId;
            RequestDataResult result = device.RequestData(new NodeTag(associativeDeviceId),
                EncapData(cmd, endpointId), TX_OPTIONS, new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP(), DEFAULT_REQ_TM);

            if (result)
            {
                COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP encaped = result.Command;

                var rep = new List<byte>();
                rep.Add(encaped.commandClass);
                rep.Add(encaped.command);
                rep.AddRange(encaped.parameter);
                ret = rep.ToArray();
            }
            return ret;
        }
        public List<string> AssociationGetGroupInfo(Alias controllerNumber, byte associativeDeviceId, byte endpointId, byte groupId)
        {
            Device device = DeviceManager.GetDevice(controllerNumber);

            List<string> ret = new List<string>();

            COMMAND_CLASS_ASSOCIATION_GRP_INFO.ASSOCIATION_GROUP_INFO_GET cmd = new COMMAND_CLASS_ASSOCIATION_GRP_INFO.ASSOCIATION_GROUP_INFO_GET();
            COMMAND_CLASS_ASSOCIATION_GRP_INFO.ASSOCIATION_GROUP_INFO_REPORT rpt = new COMMAND_CLASS_ASSOCIATION_GRP_INFO.ASSOCIATION_GROUP_INFO_REPORT();
            cmd.groupingIdentifier = groupId;
            RequestDataResult result = device.RequestData(new NodeTag(associativeDeviceId),
                EncapData(cmd, endpointId), TX_OPTIONS, new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP(), DEFAULT_REQ_TM);
            if (result)
            {
                COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP encaped = result.Command;
                var rep = new List<byte>();
                rep.Add(encaped.commandClass);
                rep.Add(encaped.command);
                rep.AddRange(encaped.parameter);
                rpt = rep.ToArray();
                DeviceManager.ZWaveDefinition.ParseApplicationObject(rpt, out CommandClassValue[] ccv);
                if (ccv != null && ccv.Length > 0)
                {
                    var pVal = ccv[0].CommandValue.ParamValues;
                    if (pVal.Count > 1)
                    {
                        string profile1 = pVal[1].InnerValues.Where(x => x.ParamDefinitionText == "Profile1").FirstOrDefault().TextValue;
                        string profile2 = pVal[1].InnerValues.Where(x => x.ParamDefinitionText == "Profile2").FirstOrDefault().TextValue;

                        ret = new List<string> { profile1, profile2 };
                    }
                }
            }
            return ret;
        }
        public BasicActionResult.AssociationGroupsResult AssociationGroupingsGet(Alias controllerNumber, Node associativeDevice)
        {
            Device device = DeviceManager.GetDevice(controllerNumber);

            BasicActionResult.AssociationGroupsResult groupResult = null;

            bool IsCommandClassSupported = device.Network.HasCommandClass(associativeDevice, COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID);

            if (IsCommandClassSupported)
            {
                RequestDataResult result = device.RequestData(associativeDevice,
                    new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GROUPINGS_GET(), TX_OPTIONS,
                    new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GROUPINGS_REPORT(), DEFAULT_REQ_TM);
                if (result)
                {
                    COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GROUPINGS_REPORT rpt = result.Command;
                    groupResult = new BasicActionResult.AssociationGroupsResult();
                    //groupResult.DeviceId = associativeDeviceId;
                    groupResult.Groups = CreateGroups(rpt.supportedGroupings);
                    for (int i = 0; i < groupResult.Groups.Count(); i++)
                    {
                        var groupId = groupResult.Groups[i].GroupId;
                        groupResult.Groups[i] = AssociationGet(controllerNumber, associativeDevice, groupId);
                        groupResult.Groups[i].Profile = AssociationGetGroupInfo(controllerNumber, associativeDevice, groupId);
                        groupResult.Groups[i].CommandClasses = AssociationGetCommandList(controllerNumber, associativeDevice, groupId);
                    }
                }
            }
            else
            {
                RequestDataResult result = device.RequestData(associativeDevice,
                    new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GROUPINGS_GET(), TX_OPTIONS,
                    new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GROUPINGS_REPORT(), DEFAULT_REQ_TM);
                if (result)
                {
                    COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GROUPINGS_REPORT rpt = result.Command;
                    groupResult = new BasicActionResult.AssociationGroupsResult();
                    //groupResult.DeviceId = associativeDeviceId;
                    groupResult.Groups = CreateGroups(rpt.supportedGroupings);
                    for (int i = 0; i < groupResult.Groups.Count(); i++)
                    {
                        var groupId = groupResult.Groups[i].GroupId;
                        groupResult.Groups[i] = AssociationGet(controllerNumber, associativeDevice, groupId);
                        groupResult.Groups[i].Profile = AssociationGetGroupInfo(controllerNumber, associativeDevice, groupId);
                        groupResult.Groups[i].CommandClasses = AssociationGetCommandList(controllerNumber, associativeDevice, groupId);
                    }
                }
            }

            return groupResult;
        }
        private List<AssociationGroup> CreateGroups(byte supportedGroupings)
        {
            List<AssociationGroup> groups = new List<AssociationGroup>();
            for (byte i = 0; i < supportedGroupings; i++)
            {
                groups.Add(new AssociationGroup { GroupId = (byte)(i + 1) });
            }
            return groups;
        }

        public BasicResult.Void AssociationCreate(Alias controllerNumber, Node deviceId, byte groupId, Node destNode, bool IsAssignReturnRoutes = false)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            ActionToken mToken = null;
            var destnodes = new List<NodeTag>();
            destnodes.Add(destNode);
            var result = SendAssociationCreate(controllerNumber, deviceId, groupId, destnodes);
            if (result == true)
            {
                AssociationGet(controllerNumber, deviceId, groupId);
                if (IsAssignReturnRoutes)
                {
                    DeleteReturnRoute(controllerNumber, deviceId);
                    AssignReturnRoute(controllerNumber, deviceId, new Node[] { destNode }, out mToken);
                }
            }
            return new BasicResult.Void();
        }

        public BasicResult.AssociationGetGroups GetGroups(Alias controllerNumber, Node deviceId)
        {
            BasicActionResult.AssociationGroupsResult res = null;
            if (deviceId.EndPointId == 0)
            {
                res = AssociationGroupingsGet(controllerNumber, deviceId);
            }
            else
            {
                res = AssociationGroupingsGet(controllerNumber, (byte)deviceId.Id, deviceId.EndPointId);
            }
            return new BasicActionResult.AssociationGroupCollection(res);
        }

        public BasicResult.AssociationGetNodes GetNodes(Alias controllerNumber, Node associativeDevice, byte groupId)
        {
            BasicActionResult.AssociationNodesResult ret = new BasicActionResult.AssociationNodesResult();
            AssociationGroup group = null;

            if (associativeDevice.EndPointId == 0)
            {
                group = AssociationGet(controllerNumber, associativeDevice, groupId);
            }
            else
            {
                group = AssociationGet(controllerNumber, associativeDevice, associativeDevice.EndPointId, groupId);
            }
            if (group != null)
            {
                ret.Nodes = group.Nodes;
            }
            return new BasicActionResult.AssociationNodeCollection(ret);
        }


        public BasicResult.Void RemoveNodeId(Alias controllerNumber, Node deviceId, byte groupId, Node destNodeId, bool IsAssignReturnRoutes = false)
        {
            bool result = false;
            ActionToken mToken = null;
            var destnodes = new List<NodeTag>();
            destnodes.Add(destNodeId);
            result = AssociationRemove(controllerNumber, deviceId, groupId, destnodes);
            if (result)
            {
                AssociationGet(controllerNumber, deviceId, groupId);
                if (IsAssignReturnRoutes)
                {
                    DeleteReturnRoute(controllerNumber, deviceId.Id);
                    AssignReturnRoute(controllerNumber, deviceId, new Node[] { destNodeId }, out mToken);
                }
            }
            return new BasicResult.Void();
        }
        private BasicActionResult.AssociationGroupsResult AssociationGroupingsGet(Alias controllerNumber, byte associativeDeviceId, byte endpointId)
        {
            Device device = DeviceManager.GetDevice(controllerNumber);

            BasicActionResult.AssociationGroupsResult groupResult = null;

            var associativeDevice = new NodeTag(associativeDeviceId);
            bool IsCommandClassSupported = device.Network.HasCommandClass(associativeDevice, COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID)
                && device.Network.HasCommandClass(associativeDevice, COMMAND_CLASS_MULTI_CHANNEL_V4.ID);


            if (IsCommandClassSupported)
            {
                RequestDataResult result = device.RequestData(new NodeTag(associativeDeviceId),
                    EncapData(new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GROUPINGS_GET(), endpointId), TX_OPTIONS,
                    new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP(), DEFAULT_REQ_TM);
                if (result)
                {
                    COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP encaped = result.Command;
                    var rep = new List<byte>();
                    rep.Add(encaped.commandClass);
                    rep.Add(encaped.command);
                    rep.AddRange(encaped.parameter);
                    COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GROUPINGS_REPORT rpt = rep.ToArray();

                    groupResult = new BasicActionResult.AssociationGroupsResult();
                    groupResult.Groups = CreateGroups(rpt.supportedGroupings);

                    for (int i = 0; i < groupResult.Groups.Count(); i++)
                    {
                        groupResult.Groups[i] = AssociationGet(controllerNumber, associativeDeviceId, endpointId, groupResult.Groups[i].GroupId);
                        groupResult.Groups[i].Profile = AssociationGetGroupInfo(controllerNumber, associativeDeviceId, groupResult.Groups[i].GroupId);
                        groupResult.Groups[i].CommandClasses = AssociationGetCommandList(controllerNumber, associativeDeviceId, groupResult.Groups[i].GroupId);
                    }
                }
            }
            return groupResult;
        }

        private AssociationGroup AssociationGet(Alias controllerNumber, Node associativeDevice, byte groupId)
        {
            AssociationGroup associativeGroup = null;

            Device device = DeviceManager.GetDevice(controllerNumber);

            bool IsCommandClassSupported = device.Network.HasCommandClass(associativeDevice, COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID);
            if (IsCommandClassSupported)
            {
                COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GET cmd = new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GET();
                cmd.groupingIdentifier = groupId;
                RequestDataResult result = device.RequestData(associativeDevice,
                    cmd, TX_OPTIONS,
                    new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REPORT(), DEFAULT_REQ_TM);
                if (result)
                {
                    COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REPORT rpt = result.Command;
                    associativeGroup = new AssociationGroup() { GroupId = groupId };
                    IList<ushort> nodeIds = rpt.nodeId.Select(x => GetNodePairId(x, 0)).ToList();
                    nodeIds.AddRange(rpt.vg.Select(x => GetNodePairId(x.multiChannelNodeId, x.properties1.endPoint)).ToArray());
                    associativeGroup.Nodes = new List<ushort>(nodeIds);
                }
            }
            else
            {
                COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GET cmd = new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GET();
                cmd.groupingIdentifier = groupId;
                RequestDataResult result = device.RequestData(associativeDevice,
                    cmd, TX_OPTIONS,
                    new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REPORT(), DEFAULT_REQ_TM);
                if (result)
                {
                    COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REPORT rpt = result.Command;
                    associativeGroup = new AssociationGroup() { GroupId = groupId };
                    IList<ushort> nodeIds = rpt.nodeid.Select(x => GetNodePairId(x, 0)).ToList();
                    associativeGroup.Nodes = new List<ushort>(nodeIds);
                }
            }
            return associativeGroup;
        }
        private AssociationGroup AssociationGet(Alias controllerNumber, Node associativeDevice, byte endpointId, byte groupId)
        {
            AssociationGroup associativeGroup = new AssociationGroup();

            Device device = DeviceManager.GetDevice(controllerNumber);

            bool IsCommandClassSupported = device.Network.HasCommandClass(associativeDevice, COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID)
                && device.Network.HasCommandClass(associativeDevice, COMMAND_CLASS_MULTI_CHANNEL_V4.ID);

            if (IsCommandClassSupported)
            {
                COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REPORT rpt = new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REPORT();
                COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GET cmd = new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GET();
                cmd.groupingIdentifier = groupId;
                RequestDataResult result = device.RequestData(associativeDevice,
                    EncapData(cmd, endpointId), TX_OPTIONS,
                    new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP(), DEFAULT_REQ_TM);
                if (result)
                {
                    COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP encaped = result.Command;
                    var rep = new List<byte>();
                    rep.Add(encaped.commandClass);
                    rep.Add(encaped.command);
                    rep.AddRange(encaped.parameter);
                    rpt = rep.ToArray();

                    IList<ushort> nodeIds = rpt.nodeId.Select(x => GetNodePairId(x, 0)).ToList();
                    nodeIds.AddRange(rpt.vg.Select(x => GetNodePairId(x.multiChannelNodeId, x.properties1.endPoint)).ToArray());
                    associativeGroup.Nodes = new List<ushort>(nodeIds);
                    associativeGroup.GroupId = groupId;
                }
            }
            return associativeGroup;
        }
        private byte[] EncapData(byte[] data, byte destinationEndPoint)
        {
            if (destinationEndPoint > 0)
            {
                COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP multiChannelCmd = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP();
                multiChannelCmd.commandClass = data[0];
                multiChannelCmd.command = data[1];
                multiChannelCmd.parameter = new List<byte>();
                for (int i = 2; i < data.Length; i++)
                {
                    multiChannelCmd.parameter.Add(data[i]);
                }
                multiChannelCmd.properties1.res = 0;
                multiChannelCmd.properties1.sourceEndPoint = 0;
                multiChannelCmd.properties2.bitAddress = 0;
                multiChannelCmd.properties2.destinationEndPoint = destinationEndPoint;
                data = multiChannelCmd;
            }
            return data;
        }
        private ushort GetNodePairId(byte nodeId, byte endPointId)
        {
            return (ushort)((endPointId << 8) + nodeId);
        }
        private bool AssociationRemove(Alias controllerNumber, NodeTag associativeDevice, byte groupId, IEnumerable<NodeTag> nodeIds)
        {
            bool ret = false;
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            bool IsCommandClassSupported = controller.Network.HasCommandClass(associativeDevice, COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID);
            if (IsCommandClassSupported)
            {
                COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REMOVE cmd = new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REMOVE();
                cmd.groupingIdentifier = groupId;
                cmd.nodeId = nodeIds.Where(x => x.EndPointId == 0).Select(x => (byte)x.Id).ToList();
                foreach (var item in nodeIds.Where(x => x.EndPointId > 0))
                {
                    var prop = new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REMOVE.TVG.Tproperties1();
                    prop.endPoint = item.EndPointId;
                    var tvg = new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REMOVE.TVG();
                    tvg.multiChannelNodeId = (byte)item.Id;
                    tvg.properties1 = prop;
                    cmd.vg.Add(tvg);
                }
                byte[] data = associativeDevice.EndPointId == 0 ? (byte[])cmd : EncapData(cmd, associativeDevice.EndPointId);
                if (controller.SendData(associativeDevice, data, TX_OPTIONS))
                {
                    ret = true;
                }
            }
            else
            {
                COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REMOVE cmd = new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REMOVE();
                cmd.groupingIdentifier = groupId;
                cmd.nodeId = nodeIds.Select(x => (byte)x.Id).Distinct().ToList();
                if (controller.SendData(associativeDevice, cmd, TX_OPTIONS))
                {
                    ret = true;
                }
            }
            return ret;
        }

        private bool DeleteReturnRoute(Alias controllerNumber, Node srcNodeId)
        {
            return DeleteReturnRoute(controllerNumber, srcNodeId, false, out ActionToken token);
        }
        private bool DeleteReturnRoute(Alias controllerNumber, Node srcNode, bool isSUCReturnRoute, out ActionToken token)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);

            bool ret = false;

            ActionResult result = null;

            if (isSUCReturnRoute)
            {
                result = controller.DeleteSucReturnRoute(srcNode, out token);
            }
            else
            {
                result = controller.DeleteReturnRoute(srcNode, out token);
            }
            if (result)
            {
                ret = true;
            }
            else
            {
                //MainVM.LogFail("Return Route for Node {0} delete failed".FormatStr(srcNodeId));
            }
            return ret;
        }
        private bool AssignReturnRoute(Alias controllerNumber, Node srcNodeId, Node[] destNodeIds, out ActionToken token)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);

            bool ret = false;

            token = null;

            if (destNodeIds != null && destNodeIds.Count() > 0)
            {
                foreach (var destNodeId in destNodeIds)
                {
                    var result = controller.AssignReturnRoute(srcNodeId, destNodeId, out token);
                    if (result)
                    {
                        ret = true;
                    }
                    else
                    {
                    }
                }
            }
            return ret;
        }
        private bool SendAssociationCreate(Alias controllerNumber, NodeTag associativeDevice, byte groupId, IEnumerable<NodeTag> nodeIds)
        {
            bool ret = false;
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);

            bool IsCommandClassSupported = controller.Network.HasCommandClass(associativeDevice, COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID);
            if (IsCommandClassSupported)
            {
                COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_SET cmd = new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_SET();
                cmd.groupingIdentifier = groupId;
                cmd.nodeId = nodeIds.Where(x => x.EndPointId == 0).Select(x => (byte)x.Id).ToList();
                foreach (var item in nodeIds.Where(x => x.EndPointId > 0))
                {
                    var prop = new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_SET.TVG.Tproperties1();
                    prop.endPoint = item.EndPointId;
                    var tvg = new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_SET.TVG();
                    tvg.multiChannelNodeId = (byte)item.Id;
                    tvg.properties1 = prop;
                    cmd.vg.Add(tvg);
                }
                byte[] data = associativeDevice.EndPointId == 0 ? (byte[])cmd : EncapData(cmd, associativeDevice.EndPointId);
                if (controller.SendData(associativeDevice, data, TX_OPTIONS))
                {
                    ret = true;
                }
            }
            else
            {
                COMMAND_CLASS_ASSOCIATION.ASSOCIATION_SET cmd = new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_SET();
                cmd.groupingIdentifier = groupId;
                cmd.nodeId = nodeIds.Select(x => (byte)x.Id).Distinct().ToList();
                if (controller.SendData(associativeDevice, cmd, TX_OPTIONS))
                {
                    ret = true;
                }
            }
            return ret;
        }
        private bool SendMultiAssociationCreate(Alias controllerNumber, byte associativeNodeId, byte associativeEndPointId, byte groupId, byte[] destNodeIds)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);

            bool ret = false;

            var associativeDevice = new NodeTag(associativeNodeId);
            bool IsCommandClassSupported = controller.Network.HasCommandClass(associativeDevice, COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID) &&
                controller.Network.HasCommandClass(associativeDevice, COMMAND_CLASS_MULTI_CHANNEL_V4.ID);

            if (IsCommandClassSupported)
            {
                var pairs = destNodeIds.Select(x => Tools.GetBytes(x)).ToArray();
                COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_SET cmd = new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_SET();
                cmd.groupingIdentifier = groupId;
                cmd.nodeId = pairs.Where(x => x[0] == 0).Select(x => x[1]).ToList();
                foreach (var item in pairs.Where(x => x[0] > 0))
                {
                    var prop = new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_SET.TVG.Tproperties1();
                    prop.endPoint = item[0];
                    var tvg = new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_SET.TVG();
                    tvg.multiChannelNodeId = item[1];
                    tvg.properties1 = prop;
                    cmd.vg.Add(tvg);
                }
                if (controller.SendData(associativeDevice, EncapData(cmd, associativeEndPointId), TX_OPTIONS))
                {
                    ret = true;
                }
            }
            return ret;
        }
        public BasicResult.Void DisableSupport(Alias controllerNumber, ActionToken token)
        {
            Device device = DeviceManager.GetDevice(controllerNumber);
            device.Cancel(token);
            return new BasicResult.Void();
        }

        public BasicResult.Void EnableSupport(Alias[] controlles)
        {
            DeviceManager.EnableAssociations(controlles);
            return new BasicResult.Void();
        }

    }
    public class AssociationGroup
    {
        public byte GroupId { get; set; }
        public List<ushort> Nodes { get; set; }
        public byte[] CommandClasses { get; set; }
        public List<string> Profile { get; set; }
    }
    public class AssociationNodes
    {
        public List<ushort> Nodes { get; set; }
    }
}




