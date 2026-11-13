// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Linq;
using Utils.UI.Enums;
using ZWave;
using System.Threading;
using ZWave.BasicApplication.Devices;
using ZWave.Devices;
using Zats.Services.Enums;

namespace Zats.Services
{
    public class AsyncService : ServiceBase, IAsyncService
    {
        private readonly TransmitOptions TX_OPTIONS = TransmitOptions.Ack | TransmitOptions.AutoRoute | TransmitOptions.Explore;
        private readonly TransmitSecurityOptions TX_SEC_OPTIONS = TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY;
        private readonly int REQUEST_TIMEOUT_MS = 10000;
        private readonly int INCLUSION_TIMEOUT_MS = 65000; // 65 sec
        private DeviceManager DeviceManager { get; set; }

        public AsyncService(DeviceManager deviceManager)
        {
            DeviceManager = deviceManager;
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            DeviceManager.Log(text, logLevel);
        }

        /// <summary>
        /// Starting AddNode operation.
        /// </summary>
        /// <param name="controllerNumber"></param>
        /// <returns></returns>
        public BasicToken.AddNode AddNode(Alias controllerNumber)
        {
            return AddNode(controllerNumber, Modes.NodeAny, INCLUSION_TIMEOUT_MS);
        }

        /// <summary>
        /// Starting AddNode operation.
        /// </summary>
        /// <param name="controllerNumber"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public BasicToken.AddNode AddNode(Alias controllerNumber, Modes mode)
        {
            return AddNode(controllerNumber, mode, INCLUSION_TIMEOUT_MS);
        }

        /// <summary>
        /// Starting AddNode operation.
        /// </summary>
        /// <param name="controllerNumber"></param>
        /// <param name="mode"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        public BasicToken.AddNode AddNode(Alias controllerNumber, Modes mode, int timeoutMs)
        {
            var dsk = DeviceManager.IncludeSettings.JoinDSK;
            return AddNodeWithDSK(controllerNumber, mode, dsk, timeoutMs);
        }

        public BasicToken.AddNode AddNode(Alias controllerNumber, Modes mode, bool isModeStopEnabled, int timeoutMs)
        {
            Modes modeFx = mode == Modes.NodeAnyS2 ? Modes.NodeAny : mode;
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            ActionToken token = controller.AddNodeToNetwork((ZWave.Enums.Modes)((byte)modeFx), isModeStopEnabled, timeoutMs,
                (action) =>
                {
                    DeviceManager.IncludeSettings.Clear();
                });
            return new BasicActionToken.AddNode(token);
        }

        public BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, byte[] dsk)
        {
            return AddNodeWithDSK(controllerNumber, Modes.NodeAny, dsk, INCLUSION_TIMEOUT_MS);
        }

        private BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, Modes mode, byte[] dsk)
        {
            return AddNodeWithDSK(controllerNumber, mode, dsk, INCLUSION_TIMEOUT_MS);
        }

        public BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, Modes mode, byte[] dsk, int timeoutMs)
        {
            return AddNodeWithDSK(controllerNumber, mode, dsk, DeviceManager.IncludeSettings.GrantedSchemes, INCLUSION_TIMEOUT_MS);
        }

        public BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, Modes mode, int dsk, SecuritySchemes[] grantSchemes, int timeoutMs)
        {
            return AddNodeWithDSK(controllerNumber, mode, new byte[] { (byte)(dsk >> 8), (byte)(dsk & byte.MaxValue) }, grantSchemes, timeoutMs);
        }

        public BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, Modes mode, byte[] dsk, SecuritySchemes[] grantSchemes, int timeoutMs)
        {
            DeviceManager.IncludeSettings.JoinDSK = dsk;
            DeviceManager.IncludeSettings.GrantedSchemes = grantSchemes;

            Modes modeFx = mode == Modes.NodeAnyS2 ? Modes.NodeAny : mode;
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);

            DeviceManager.DisableSmartStart(controllerNumber);

            ActionToken token = controller.AddNodeToNetwork((ZWave.Enums.Modes)((byte)modeFx), timeoutMs,
                (action) =>
                {
                    DeviceManager.EnableSmartStart(controllerNumber);
                    DeviceManager.IncludeSettings.Clear();
                });

            return new BasicActionToken.AddNode(token);
        }

        public BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, Modes mode, int dsk)
        {
            return AddNodeWithDSK(controllerNumber, mode, dsk, INCLUSION_TIMEOUT_MS);
        }

        public BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, int dsk)
        {
            return AddNodeWithDSK(controllerNumber, Modes.NodeAny, dsk, INCLUSION_TIMEOUT_MS);
        }

        public BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, Modes mode, int dsk, int timeoutMs)
        {
            byte[] innerDsk = null;
            try
            {
                innerDsk = new byte[] { (byte)(dsk >> 8), (byte)dsk };
            }
            catch (InvalidCastException)
            {
            }
            return AddNodeWithDSK(controllerNumber, mode, innerDsk, timeoutMs);
        }

        /// <summary>
        /// Starting RemoveNode operation
        /// </summary>
        /// <param name="controllerNumber"></param>
        /// <returns></returns>
        public BasicToken.RemoveNode RemoveNode(Alias controllerNumber)
        {
            return RemoveNode(controllerNumber, Modes.NodeAny, INCLUSION_TIMEOUT_MS);
        }

        /// <summary>
        /// Starting RemoveNode operation
        /// </summary>
        /// <param name="controllerNumber"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public BasicToken.RemoveNode RemoveNode(Alias controllerNumber, Modes mode)
        {
            return RemoveNode(controllerNumber, mode, INCLUSION_TIMEOUT_MS);
        }

        /// <summary>
        /// Starting RemoveNode operation
        /// </summary>
        /// <param name="controllerNumber"></param>
        /// <param name="mode"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        public BasicToken.RemoveNode RemoveNode(Alias controllerNumber, Modes mode, int timeoutMs)
        {
            Modes modeFx = mode == Modes.NodeAnyS2 ? Modes.NodeAny : mode;
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            DeviceManager.DisableSmartStart(controllerNumber);
            ActionToken token = controller.RemoveNodeFromNetwork((ZWave.Enums.Modes)modeFx, timeoutMs, (x) => DeviceManager.EnableSmartStart(controllerNumber));
            return new BasicActionToken.RemoveNode(token);
        }

        /// <summary>
        /// Starting RemoveNode operation
        /// </summary>
        /// <param name="controllerNumber"></param>
        /// <param name="mode"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        public BasicToken.RemoveNode RemoveNodeIdFromNetwork(Alias controllerNumber, Node nodeNumber, Modes mode, int timeoutMs)
        {
            Modes modeFx = mode == Modes.NodeAnyS2 ? Modes.NodeAny : mode;
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            DeviceManager.DisableSmartStart(controllerNumber);
            ActionToken token = controller.RemoveNodeIdFromNetwork((ZWave.Enums.Modes)((byte)modeFx), nodeNumber, timeoutMs, (x) => DeviceManager.EnableSmartStart(controllerNumber));
            return new BasicActionToken.RemoveNode(token);
        }

        /// <summary>
        /// Starting ControllerChange operation
        /// </summary>
        /// <param name="controllerNumber"></param>
        /// <param name="mode"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        public BasicToken.AddNode ControllerChange(Alias controllerNumber, ControllerChangeModes mode, int timeoutMs)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            DeviceManager.DisableSmartStart(controllerNumber);
            ActionToken token = controller.ControllerChange((ZWave.Enums.ControllerChangeModes)((byte)mode), timeoutMs, (x) => DeviceManager.EnableSmartStart(controllerNumber));
            return new BasicActionToken.AddNode(token);
        }

        public BasicToken.AddNode ControllerChange(Alias controllerNumber, ControllerChangeModes mode, bool isModeStopEnabled, int timeoutMs)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            ActionToken token = controller.ControllerChange((ZWave.Enums.ControllerChangeModes)mode, isModeStopEnabled, timeoutMs, null);
            return new BasicActionToken.AddNode(token);
        }

        /// <summary>
        /// Starting SetLearnMode operation
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <returns></returns>
        public BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber)
        {
            return SetLearnMode(deviceNumber, LearnModes.LearnModeClassic, INCLUSION_TIMEOUT_MS);
        }

        /// <summary>
        /// Starting SetLearnMode operation
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode)
        {
            return SetLearnMode(deviceNumber, mode, INCLUSION_TIMEOUT_MS);
        }

        /// <summary>
        /// Starting SetLearnMode operation
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="mode"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        public BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode, int timeoutMs)
        {
            return SetLearnMode(deviceNumber, mode, false, timeoutMs);
        }

        /// <summary>
        /// Starting SetLearnMode operation
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="mode"></param>
        /// <param name="isNonSecure"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        public BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode, bool isNonSecure, int timeoutMs)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ZWave.Enums.LearnModes lmode = (ZWave.Enums.LearnModes)(byte)mode;
            if ((lmode & ZWave.Enums.LearnModes.LearnModeSmartStart) != ZWave.Enums.LearnModes.LearnModeSmartStart)
            {
                DeviceManager.DisableSmartStart(deviceNumber);
            }
            // The NIF of the new inclusion state is written when the operation completes, see DeviceManager.
            DeviceManager.SetNodeInformationForLearnMode(deviceNumber);
            ActionToken token = device.SetLearnMode(lmode, isNonSecure, timeoutMs,
                (x) =>
                {
                    DeviceManager.EnableSmartStart(deviceNumber);
                });
            return new BasicActionToken.SetLearnMode(token);
        }

        public BasicToken.SetLearnMode WaitForLearnMode(Alias deviceNumber, int timeoutMs)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);

            ActionToken token = device.WaitForLearnMode(timeoutMs,
                (x) =>
                {
                    DeviceManager.EnableSmartStart(deviceNumber);
                });
            return new BasicActionToken.SetLearnMode(token);
        }

        /// <summary>
        /// Blocks test execution untill expected status received. 
        /// Raise RequestTimeoutException if timeout expired.
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="timeoutMs"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool WaitNodeStatus(Alias deviceNumber, int timeoutMs, NodeStatuses status)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            return device.WaitNodeStatusSignal((ZWave.BasicApplication.Enums.NodeStatuses)status, timeoutMs);
        }

        /// <summary>
        /// Blocks test execution untill expected status received. Raise RequestTimeoutException if timeout expired.
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="timeoutMs"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool WaitAssignStatus(Alias deviceNumber, int timeoutMs, AssignStatuses status)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            return device.WaitAssignStatusSignal((ZWave.BasicApplication.Enums.AssignStatuses)status, timeoutMs);
        }

        /// <summary>
        /// Waits untill async operation completed
        /// </summary>
        /// <param name="token">token of the started async operation</param>
        /// <returns></returns>
        public bool WaitCompletedSignal(_ActionToken token)
        {
            return token.Token.WaitCompletedSignal().IsStateCompleted;
        }

        /// <summary>
        /// Cancells async operation
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="token">token of the started async operation</param>
        public void CancelToken(Alias deviceNumber, _ActionToken token)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            device.Cancel(token.Token);
        }

        /// <summary>
        /// Starting SendData operation
        /// </summary>
        /// <param name="senderNumber"></param>
        /// <param name="receiverNumber"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public BasicToken.SendData SendData(Alias senderNumber, Alias receiverNumber, byte[] data)
        {
            return SendData(senderNumber, receiverNumber, data, TX_OPTIONS);
        }

        /// <summary>
        /// Starting SendData operation
        /// </summary>
        /// <param name="senderNumber"></param>
        /// <param name="receiverNumber"></param>
        /// <param name="data"></param>
        /// <param name="txOptions"></param>
        /// <returns></returns>
        public BasicToken.SendData SendData(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            Device receiver = DeviceManager.GetDevice(receiverNumber);
            ActionToken token = sender.SendData(new NodeTag(receiver.Id, receiverNumber.EndPointId), data, (ZWave.Enums.TransmitOptions)txOptions, null, null);
            return new BasicToken.SendData(token);
        }

        public BasicToken.SendData SendData(Alias senderNumber, Node receiver, byte[] data, TransmitOptions txOptions)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            ActionToken token = sender.SendData(receiver, data, (ZWave.Enums.TransmitOptions)txOptions, null, null);
            return new BasicToken.SendData(token);
        }

        public BasicToken.SendData SendDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, SecuritySchemes scheme)
        {
            return SendDataEx(senderNumber, receiverNumber, data, TX_OPTIONS, TX_SEC_OPTIONS, scheme);
        }

        public BasicToken.SendData SendDataEx(Alias senderNumber, Node receiver, byte[] data, SecuritySchemes scheme)
        {
            return SendDataEx(senderNumber, receiver, data, TX_OPTIONS, TX_SEC_OPTIONS, scheme);
        }

        public BasicToken.SendData SendDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme)
        {
            return SendDataEx(senderNumber, receiverNumber, data, txOptions, txSecOptions, scheme, TransmitOptions2.NONE);
        }

        public BasicToken.SendData SendDataEx(Alias senderNumber, Node receiver, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme)
        {
            return SendDataEx(senderNumber, receiver, data, txOptions, txSecOptions, scheme, TransmitOptions2.NONE);
        }

        public BasicToken.SendData SendDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            Device receiver = DeviceManager.GetDevice(receiverNumber);
            ActionToken token = sender.SendDataEx(new NodeTag(receiver.Id, receiverNumber.EndPointId), data, (ZWave.Enums.TransmitOptions)txOptions, (ZWave.Enums.TransmitSecurityOptions)txSecOptions, (ZWave.Enums.SecuritySchemes)scheme, (ZWave.Enums.TransmitOptions2)txOptions2, null);
            return new BasicToken.SendData(token);
        }

        public BasicToken.SendData SendDataEx(Alias senderNumber, Node receiver, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            if (sender == null)
            {
                return null;
            }
            else
            {
                ActionToken token = sender.SendDataEx(receiver, data,
                    (ZWave.Enums.TransmitOptions)txOptions, (ZWave.Enums.TransmitSecurityOptions)txSecOptions,
                    (ZWave.Enums.SecuritySchemes)scheme, (ZWave.Enums.TransmitOptions2)txOptions2, null);
                return new BasicToken.SendData(token);
            }
        }

        public BasicToken.SendData SendDataMultiEx(Alias senderNumber, byte[] data, SecuritySchemes scheme, byte groupId)
        {
            return SendDataMultiEx(senderNumber, data, TX_OPTIONS, scheme, groupId);
        }

        public BasicToken.SendData SendDataMultiEx(Alias senderNumber, byte[] data, TransmitOptions txOptions, SecuritySchemes scheme, byte groupId)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            ActionToken token = sender.SendDataMultiEx(
                data,
                (ZWave.Enums.TransmitOptions)txOptions,
                (ZWave.Enums.SecuritySchemes)scheme,
                groupId,
                null);
            return new BasicToken.SendData(token);
        }

        public BasicToken.SendData SendDataMulti(Alias senderNumber, Node[] nodes, byte[] data, SubstituteFlags Flags)
        {
            SubstituteSettings Settings = new SubstituteSettings((ZWave.Enums.SubstituteFlags)Flags, 55);
            Device sender = DeviceManager.GetDevice(senderNumber);
            ActionToken token = sender.SendDataMulti(
                nodes.Select(x => (NodeTag)x).ToArray(),
                data,
                (ZWave.Enums.TransmitOptions)TX_OPTIONS,
                Settings,
                null
                );
            return new BasicToken.SendData(token);
        }

        /// <summary>
        /// Starting RequestNetworkUpdate operation
        /// </summary>
        /// <param name="senderNumber"></param>
        /// <returns></returns>
        public BasicToken.RequestNetworkUpdate RequestNetworkUpdate(Alias senderNumber)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            ActionToken token = sender.RequestNetworkUpdate(null);
            return new BasicToken.RequestNetworkUpdate(token);
        }

        /// <summary>
        /// Get the neighbors from the specified node.
        /// </summary>
        /// <param name="controllerNumber"></param>
        /// <param name="nodeNumber"></param>
        /// <returns></returns>
        public BasicToken.RequestNodeNeighborUpdate RequestNodeNeighborUpdate(Alias controllerNumber, Alias nodeNumber)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device nodeDevice = DeviceManager.GetDevice(nodeNumber);
            ActionToken token = controller.RequestNodeNeighborUpdate(nodeDevice.Network.NodeTag, 30000, null);
            return new BasicToken.RequestNodeNeighborUpdate(token);
        }

        /// <summary>
        /// Starting Request Node Information operation
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="nodeNumberID"></param>
        /// <returns></returns>
        public BasicToken.RequestNodeInfo RequestNodeInfo(Alias deviceNumber, Node nodeNumberID)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionToken token = device.RequestNodeInfo(nodeNumberID, null);
            return new BasicToken.RequestNodeInfo(token);
        }

        /// <summary>
        /// Starting SendData operation and waits expected data
        /// </summary>
        /// <param name="senderNumber"></param>
        /// <param name="receiverNumber"></param>
        /// <param name="data"></param>
        /// <param name="expectedData"></param>
        /// <returns></returns>
        public BasicToken.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, byte[] expectedData)
        {
            return RequestData(senderNumber, receiverNumber, data, TX_OPTIONS, expectedData, REQUEST_TIMEOUT_MS);
        }

        /// <summary>
        /// Starting SendData operation and waits expected data
        /// </summary>
        /// <param name="senderNumber"></param>
        /// <param name="receiverNumber"></param>
        /// <param name="data"></param>
        /// <param name="expectedData"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        public BasicToken.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, byte[] expectedData, int timeoutMs)
        {
            return RequestData(senderNumber, receiverNumber, data, TX_OPTIONS, expectedData, timeoutMs);
        }

        /// <summary>
        /// Starting SendData operation and waits expected data
        /// </summary>
        /// <param name="senderNumber"></param>
        /// <param name="receiverNumber"></param>
        /// <param name="data"></param>
        /// <param name="txOptions"></param>
        /// <param name="expectedData"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        public BasicToken.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, byte[] expectedData, int timeoutMs)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            Device receiver = DeviceManager.GetDevice(receiverNumber);
            ActionToken token = sender.RequestData(new NodeTag(receiver.Id, receiverNumber.EndPointId), data, (ZWave.Enums.TransmitOptions)txOptions, expectedData, timeoutMs, null);
            BasicToken.RequestData ret = new BasicToken.RequestData(token);
            return ret;
        }

        public BasicToken.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, SecuritySchemes scheme, byte[] expectedData)
        {
            return RequestDataEx(senderNumber, receiverNumber, data, TX_OPTIONS, TX_SEC_OPTIONS, scheme, expectedData, REQUEST_TIMEOUT_MS);
        }

        public BasicToken.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, SecuritySchemes scheme, byte[] expectedData, int timeoutMs)
        {
            return RequestDataEx(senderNumber, receiverNumber, data, TX_OPTIONS, TX_SEC_OPTIONS, scheme, expectedData, timeoutMs);
        }

        public BasicToken.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data,
            TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, byte[] expectedData, int timeoutMs)
        {
            return RequestDataEx(
                senderNumber,
                receiverNumber,
                data,
                txOptions,
                txSecOptions,
                scheme,
                TransmitOptions2.NONE,
                expectedData,
                timeoutMs);
        }

        public BasicToken.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data,
            TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2, byte[] expectedData, int timeoutMs)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            Device receiver = DeviceManager.GetDevice(receiverNumber);
            ActionToken token = sender.RequestDataEx(
                new NodeTag(receiver.Id, receiverNumber.EndPointId),
                data,
                (ZWave.Enums.TransmitOptions)txOptions,
                (ZWave.Enums.TransmitSecurityOptions)txSecOptions,
                (ZWave.Enums.SecuritySchemes)scheme,
                (ZWave.Enums.TransmitOptions2)txOptions2,
                expectedData,
                timeoutMs,
                null);
            BasicToken.RequestData ret = new BasicToken.RequestData(token);
            return ret;
        }

        /// <summary>
        /// Waits expected data
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="expectedData"></param>
        /// <returns></returns>
        public BasicToken.ExpectData ExpectData(Alias deviceNumber, byte[] expectedData)
        {
            return ExpectData(deviceNumber, expectedData, REQUEST_TIMEOUT_MS);
        }

        public BasicToken.ExpectData ExpectData(Alias deviceNumber, byte[] expectedData, int timeoutMs)
        {
            return ExpectData(deviceNumber, expectedData, 2, timeoutMs);
        }

        /// <summary>
        /// Waits expected data
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="expectedData"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        public BasicToken.ExpectData ExpectData(Alias deviceNumber, byte[] expectedData, int numberOfBytesToCompare, int timeoutMs)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            if (device == null)
                return null;
            else
            {
                ActionToken token = device.ExpectData(expectedData, numberOfBytesToCompare, timeoutMs, null);
                return new BasicActionToken.ExpectData(token);
            }
        }

        public BasicToken.ExpectData ExpectData(Alias deviceNumber, Node srcNode, byte[] expectedData, int timeoutMs)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            if (device == null)
                return null;
            else
            {
                ActionToken token = device.ExpectData(srcNode, expectedData, timeoutMs, null);
                return new BasicActionToken.ExpectData(token);
            }
        }

        public BasicToken.ExpectData ExpectData(Alias deviceNumber, Node srcNode, byte[] expectedData, SecurityS2ExtensionTypes[] securityS2ExtensionType, int timeoutMs)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            if (device == null)
                return null;
            else
            {
                ZWave.Security.ExtensionTypes[] extensionTypes = null;
                if (securityS2ExtensionType != null)
                {
                    extensionTypes = securityS2ExtensionType.Select(x => (ZWave.Security.ExtensionTypes)x).ToArray();
                }
                ActionToken token = device.ExpectData(srcNode, expectedData ?? new byte[0], extensionTypes, timeoutMs, null);
                return new BasicActionToken.ExpectData(token);
            }
        }

        public BasicToken.ExpectControllerUpdate ExpectControllerUpdate(Alias controllerNumber, ControllerUpdateStatuses updateStatus, int timeoutMs)
        {
            Device device = DeviceManager.GetDevice(controllerNumber);
            ActionToken token = device.ExpectControllerUpdate((ZWave.BasicApplication.Enums.ControllerUpdateStatuses)updateStatus, timeoutMs, null);
            return new BasicToken.ExpectControllerUpdate(token);
        }

        public BasicToken.ListenControllerUpdate ListenControllerUpdate(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            var listenControllerUpdateToken = new BasicActionToken.ListenControllerUpdate();
            var token = device.HandleControllerUpdate(listenControllerUpdateToken.ListenControllerUpdateDelegate);
            listenControllerUpdateToken.InitToken(token);
            return listenControllerUpdateToken;
        }

        public BasicToken.ListenData ListenData(Alias deviceNumber)
        {
            return ListenData(deviceNumber, null);
        }

        public BasicToken.ListenData ListenData(Alias deviceNumber, byte[] compareData)
        {
            Utils.ByteIndex[] cod = null;
            if (compareData != null && compareData.Length > 1)
            {
                cod = new Utils.ByteIndex[] { compareData[0], compareData[1] };
            }
            Device device = DeviceManager.GetDevice(deviceNumber);
            var listenDataToken = new BasicActionToken.ListenData();
            var token = device.ListenData(listenDataToken.ListenDataDelegate, cod);
            listenDataToken.InitToken(token);
            return listenDataToken;
        }

        public BasicToken.ExpectSmartStartAdd ExpectSmartStartAdd(Alias controllerNumber, int timeoutMs)
        {
            return new BasicActionToken.ExpectSmartStartAdd(DeviceManager.GetSmartStartToken(timeoutMs));
        }

        public BasicToken.Void SetRFReceiveMode(Alias DeviceNumber, bool mode)
        {
            Device deviceNumber = DeviceManager.GetDevice(DeviceNumber);
            byte RfMode = (byte)((mode == true) ? 0x01 : 0x00);
            ActionToken token = deviceNumber.SetRFReceiveMode(RfMode, null);
            return new BasicActionToken.Void(token);
        }

        public BasicToken.Includenode IncludeNode(Alias controllerNumber, byte[] dsk, Modes addMode, int timeoutMs, SetupNodeLifelineSettings setupNodeLifelineSettings = SetupNodeLifelineSettings.Default)
        {
            DeviceManager.IncludeSettings.JoinDSK = dsk;
            DeviceManager.ReportTestFramesAndExtensionOverrides(controllerNumber);
            Modes addModeFx = addMode == Modes.NodeAnyS2 ? Modes.NodeAny : addMode;
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);

            DeviceManager.DisableSmartStart(controllerNumber);

            ActionToken includeToken = controller.IncludeNode((ZWave.Enums.Modes)addModeFx, timeoutMs, (action) =>
            {
                DeviceManager.EnableSmartStart(controllerNumber);
                DeviceManager.IncludeSettings.Clear();
            }, (ZWave.BasicApplication.Enums.SetupNodeLifelineSettings)setupNodeLifelineSettings);

            return new BasicActionToken.IncludeNode(includeToken);
        }

        public BasicToken.ExpectZW ExpectZW(Alias deviceNumber, IEnumerable<ByteData[]> filter, int timeoutMs)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            List<Utils.ByteIndex[]> masks = new List<Utils.ByteIndex[]>();
            foreach (var f in filter)
            {
                Utils.ByteIndex[] mask = new Utils.ByteIndex[f.Length];
                for (int j = 0; j < f.Length; j++)
                {
                    mask[j] = new Utils.ByteIndex(f[j].Value, f[j].MaskInData, f[j].Presence);
                }
                masks.Add(mask);
            }
            ActionToken token = device.ExpectZW(masks, timeoutMs, null);
            return new BasicActionToken.ExpectZW(token);
        }

        public BasicToken.ExpectZW ExpectZW(Alias deviceNumber, string filter, int timeoutMs)
        {
            return ExpectZW(deviceNumber, new[] { filter }, timeoutMs);
        }

        public BasicToken.ExpectZW ExpectZW(Alias deviceNumber, IEnumerable<string> filter, int timeoutMs)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            List<Utils.ByteIndex[]> masks = new List<Utils.ByteIndex[]>();
            foreach (var str in filter)
            {
                var tokens = str.Split(' ');
                List<Utils.ByteIndex> mask = new List<Utils.ByteIndex>();
                foreach (var t in tokens)
                {
                    byte b = 0;
                    if (t == "?")
                    {
                        mask.Add(Utils.ByteIndex.AnyValue);
                    }
                    else if (Byte.TryParse(t, System.Globalization.NumberStyles.HexNumber, null, out b))
                    {
                        mask.Add(new Utils.ByteIndex(b));
                    }
                }
                masks.Add(mask.ToArray());
            }
            ActionToken token = device.ExpectZW(masks, timeoutMs, null);
            return new BasicActionToken.ExpectZW(token);
        }

        public BasicToken.ExpectZW ExpectZW(Alias deviceNumber, ByteData[] filter, int timeoutMs)
        {
            return ExpectZW(deviceNumber, new[] { filter }, timeoutMs);
        }

        public BasicToken.AddNode ReplaceFailedNode(Alias controllerNumber, Node Node, byte[] dsk, Modes addMode, int timeoutMs)
        {
            DeviceManager.IncludeSettings.JoinDSK = dsk;

            Modes addModeFx = addMode == Modes.NodeAnyS2 ? Modes.NodeAny : addMode;

            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            DeviceManager.DisableSmartStart(controllerNumber);

            ActionResult RemoveRes = controller.RemoveFailedNodeId(Node);

            while (!RemoveRes.IsStateCompleted)
            {
                Thread.Sleep(333);
            }

            ActionToken res = controller.ReplaceFailedNode(Node, (action) =>
            {
                DeviceManager.EnableSmartStart(controllerNumber);
                DeviceManager.IncludeSettings.Clear();
            });
            DeviceManager.EnableSmartStart(controllerNumber);
            return new BasicActionToken.AddNode(res);
        }

        public BasicToken.Void NoiseData(Alias deviceNumber, Node node, byte[] data, TransmitOptions txOptions, Action<BasicResult.SendData> sendCallback, int interval, int timeout)
        {
            return NoiseData(deviceNumber, new[] { node }, data, txOptions, sendCallback, interval, timeout);
        }

        public BasicToken.Void NoiseData(Alias deviceNumber, Node node, byte[] data, TransmitOptions txOptions, byte[] expectData, Action<BasicResult.RequestData> requestCallback, int interval, int timeout)
        {
            return NoiseData(deviceNumber, new[] { node }, data, txOptions, expectData, requestCallback, interval, timeout);
        }

        public BasicToken.Void NoiseData(Alias deviceNumber, Node[] nodes, byte[] data, TransmitOptions txOptions, Action<BasicResult.SendData> sendCallback, int interval, int timeout)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            var token = device.NoiseData(nodes.Select(x => (NodeTag)x).ToArray(), data, (ZWave.Enums.TransmitOptions)txOptions,
                (x) => sendCallback?.Invoke(new BasicActionResult.SendData(x)), interval, timeout);
            return new BasicActionToken.Void(token);
        }

        public BasicToken.Void NoiseData(Alias deviceNumber, Node[] nodes, byte[] data, TransmitOptions txOptions, byte[] expectData, Action<BasicResult.RequestData> requestCallback, int interval, int timeout)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            var token = device.NoiseData(nodes.Select(x => (NodeTag)x).ToArray(), data, (ZWave.Enums.TransmitOptions)txOptions, expectData,
                (x) => requestCallback?.Invoke(new BasicActionResult.RequestData(x)), interval, timeout);
            return new BasicActionToken.Void(token);
        }
        public BasicToken.Void NoiseDataEx(Alias deviceNumber, Node[] nodes, byte[] data, TransmitOptions txOptions, byte[] expectData,
            Action<BasicResult.RequestData> requestCallback, int interval, int timeout, SecuritySchemes secSch, bool isPoisson)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            var token = device.NoiseDataEx(nodes.Select(x => (NodeTag)x).ToArray(), data, (ZWave.Enums.TransmitOptions)txOptions, expectData, interval, timeout,
               (ZWave.Enums.SecuritySchemes)(secSch), ZWave.Enums.TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY, ZWave.Enums.TransmitOptions2.TRANSMIT_OPTION_2_TRANSPORT_SERVICE, isPoisson);
            return new BasicActionToken.Void(token);
        }
    }
}
