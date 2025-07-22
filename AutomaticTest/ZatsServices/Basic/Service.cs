// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils;
using Utils.UI.Enums;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using ZWave.BasicApplication.Tasks;
using ZWave.Devices;
using ZWave.Enums;
using ZWave.Layers;
using ZWave.Layers.Session;
using ZWave.Layers.Transport;

namespace Zats.Services
{
    public class Service : ServiceBase, IService
    {
        private const int REQUEST_TIMEOUT_MS = 10000; // 10 sec
        private const int INCLUSION_TIMEOUT_MS = 35000; // 35 sec timeout as S2 inclusion timeout is 30-32sec 

        private readonly TransmitOptions TX_OPTIONS = TransmitOptions.Ack | TransmitOptions.AutoRoute | TransmitOptions.Explore;
        private readonly TransmitSecurityOptions TX_SEC_OPTIONS = TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY;

        private DeviceManager DeviceManager { get; set; }

        public ISendDataSettings SendDataSettings
        {
            get
            {
                return DeviceManager.SendDataSettings;
            }
        }

        public Service(DeviceManager deviceManager)
        {
            DeviceManager = deviceManager;
        }

        public ServiceSettings Settings
        {
            get
            {
                return DeviceManager.Settings;
            }
            set
            {
                DeviceManager.Settings = value;
            }
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            DeviceManager.Log(text, logLevel);
        }

        public byte[] GetCsa(Alias controllerNumber, int numberOfBytes)
        {
            return DeviceManager.GetCSA(controllerNumber, numberOfBytes);
        }

        public BasicResult.ReturnValue SerialApiSetNodeIdBaseType(Alias deviceNumber, byte version)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            var res = (ReturnValueResult)device.SerialApiSetNodeIdBaseType(version);
            return new BasicActionResult.ReturnValue(res);
        }

        public BasicResult.SetLearnMode SetVirtualDeviceLearnMode(Alias bridgeNumber, Node node, VirtualDeviceLearnModes mode)
        {
            BridgeController controller = (BridgeController)DeviceManager.GetDevice(bridgeNumber);
            DeviceManager.DisableSmartStart(bridgeNumber);
            SetLearnModeResult res = controller.SetVirtualDeviceLearnMode(node, (ZWave.BasicApplication.Enums.VirtualDeviceLearnModes)mode, INCLUSION_TIMEOUT_MS);
            DeviceManager.EnableSmartStart(bridgeNumber);
            return new BasicActionResult.SetLearnMode(res);
        }

        public BasicResult.AddNode AddNodeVirtual(Alias controllerNumber, Alias bridgeNumber)
        {
            return AddNodeVirtual(controllerNumber, bridgeNumber, INCLUSION_TIMEOUT_MS);
        }

        public BasicResult.AddNode AddNodeVirtual(Alias controllerNumber, Alias bridgeNumber, int timeoutMs)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            BridgeController bridge = (BridgeController)DeviceManager.GetDevice(bridgeNumber);

            if (DeviceManager.IncludeSettings.JoinDSK == null)
            {
                DeviceManager.IncludeSettings.JoinDSK = DeviceManager.GetDSK(bridgeNumber, 2);
            }
            if (DeviceManager.IncludeSettings.JoinCSA == null)
            {
                DeviceManager.IncludeSettings.JoinCSA = DeviceManager.GetCSA(controllerNumber, 4);
            }

            DeviceManager.DisableSmartStart(controllerNumber);
            ActionToken addRemoveToken = controller.AddNodeToNetwork(ZWave.Enums.Modes.NodeAny, timeoutMs, null);
            controller.WaitNodeStatusSignal(ZWave.BasicApplication.Enums.NodeStatuses.LearnReady, timeoutMs);
            Thread.Sleep(1000);
            SetVirtualDeviceLearnMode(bridgeNumber, 0x00, VirtualDeviceLearnModes.Enable);

            AddRemoveNodeResult addRemoveResult = (AddRemoveNodeResult)addRemoveToken.WaitCompletedSignal();
            SetVirtualDeviceLearnMode(bridgeNumber, 0x00, VirtualDeviceLearnModes.Disable);

            DeviceManager.EnableSmartStart(controllerNumber);
            DeviceManager.IncludeSettings.Clear();
            return new BasicActionResult.AddNode(addRemoveResult);
        }

        public BasicResult.AddNodeWithLearnMode AddNode(Alias controllerNumber, Alias nodeNumber, int timeoutMs)
        {
            return AddNode(controllerNumber, nodeNumber, Modes.NodeAny, timeoutMs);
        }

        public BasicResult.AddNodeWithLearnMode AddNodeWithDSK(Alias controllerNumber, Alias nodeNumber, byte[] dsk)
        {
            return AddNodeWithDSK(controllerNumber, nodeNumber, dsk, INCLUSION_TIMEOUT_MS);
        }

        public BasicResult.AddNodeWithLearnMode AddNodeWithDSK(Alias controllerNumber, Alias nodeNumber, byte[] dsk, int timeoutMs)
        {
            return AddNodeWithDSK(controllerNumber, nodeNumber, Modes.NodeAny, LearnModes.LearnModeClassic, dsk, INCLUSION_TIMEOUT_MS);
        }

        public BasicResult.AddNodeWithLearnMode AddNodeWithDSK(Alias controllerNumber, Alias nodeNumber, Modes addMode, LearnModes learnMode, byte[] dsk, int timeoutMs)
        {
            var addNodeResult = AddNode(controllerNumber, nodeNumber, addMode, learnMode, timeoutMs);
            return addNodeResult;
        }

        public BasicResult.AddNodeWithLearnMode AddNodeWithDSK(Alias controllerNumber, Alias nodeNumber, int dsk)
        {
            return AddNodeWithDSK(controllerNumber, nodeNumber, dsk, INCLUSION_TIMEOUT_MS);
        }

        public BasicResult.AddNodeWithLearnMode AddNodeWithDSK(Alias controllerNumber, Alias nodeNumber, int dsk, int timeoutMs)
        {
            return AddNodeWithDSK(controllerNumber, nodeNumber, Modes.NodeAny, LearnModes.LearnModeClassic, dsk, INCLUSION_TIMEOUT_MS);
        }

        public BasicResult.AddNodeWithLearnMode AddNodeWithDSK(Alias controllerNumber, Alias nodeNumber, Modes addMode, LearnModes learnMode, int dsk, int timeoutMs)
        {
            byte[] dskInner = null;
            try
            {
                dskInner = new byte[2] { (byte)(dsk >> 8), (byte)dsk };
            }
            catch (Exception)
            {
            }
            return AddNodeWithDSK(controllerNumber, nodeNumber, addMode, learnMode, dskInner, timeoutMs);
        }

        private byte[] ConvertHexStringToHexArray(string hexString)
        {
            //Tools.GetBytes(hexString);
            string hexString_fix = hexString.Replace(" ", String.Empty);
            return Enumerable.Range(0, hexString_fix.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(hexString_fix.Substring(x, 2), 16))
                     .ToArray(); ;
        }

        public BasicResult.AddNodeWithLearnMode AddNode(Alias controllerNumber, Alias nodeNumber, Modes addMode, int timeoutMs)
        {
            return AddNode(controllerNumber, nodeNumber, addMode, LearnModes.LearnModeClassic, timeoutMs);
        }

        public BasicResult.AddNodeWithLearnMode AddNode(Alias controllerNumber, Alias nodeNumber, Modes addMode, LearnModes learnMode, int timeoutMs)
        {
            if (DeviceManager.IncludeSettings.JoinDSK == null)
            {
                DeviceManager.IncludeSettings.JoinDSK = DeviceManager.GetDSK(nodeNumber, 2);
            }

            DeviceManager.DisableSmartStart(controllerNumber);

            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            DeviceManager.ReportTestFramesAndExtensionOverrides(controllerNumber);
            Modes addModeFx = addMode == Modes.NodeAnyS2 ? Modes.NodeAny : addMode;

            ActionToken addRemoveToken = controller.AddNodeToNetwork((ZWave.Enums.Modes)Modes.NodeAny, timeoutMs, (x) => DeviceManager.EnableSmartStart(controllerNumber));

            BasicResult.SetLearnMode learnRes = null;
            controller.WaitNodeStatusSignal(ZWave.BasicApplication.Enums.NodeStatuses.LearnReady, timeoutMs);
            learnRes = SetLearnMode(nodeNumber, learnMode, timeoutMs);
            AddRemoveNodeResult addRemoveResult = (AddRemoveNodeResult)addRemoveToken.WaitCompletedSignal();
            var ret = new BasicActionResult.AddNodeWithLearnMode(addRemoveResult, null);
            ret.SetLearnMode = learnRes;
            if (ret.SubstituteStatus != SubstituteStatuses.None)
            {
                DeviceManager.LogNetworkKeys(controllerNumber, addRemoveResult.Node);
            }
            DeviceManager.EnableSmartStart(controllerNumber);
            DeviceManager.IncludeSettings.Clear();
            return ret;
        }

        public BasicResult.AddNodeWithLearnMode AddNode(Alias controllerNumber, Alias nodeNumber)
        {
            return AddNode(controllerNumber, nodeNumber, INCLUSION_TIMEOUT_MS);
        }

        public byte[] GetInclusionRequestHomeId(Alias deviceNumber)
        {
            var dsk = GetDsk(deviceNumber, 16);
            var homeId = new byte[4];
            homeId[0] = (byte)(dsk[8] | 0xC0);
            homeId[1] = dsk[9];
            homeId[2] = dsk[10];
            homeId[3] = (byte)(dsk[11] & 0xFE);
            return homeId;
        }

        public byte[] GetDsk(Alias deviceNumber, int NumBytes)
        {
            byte[] Dsk = new byte[NumBytes];
            Device device = DeviceManager.GetDevice(deviceNumber);
            if (device != null && (device.Library == Libraries.ControllerBridgeLib || device.Library == Libraries.ControllerPortableLib ||
                device.Library == Libraries.ControllerStaticLib || device.Library == Libraries.EndDeviceSysTestLib))
            {
                SecurityManager securityManager = (SecurityManager)device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
                Dsk = securityManager.SecurityManagerInfo.GetJoinPublicKeyS2().Take(NumBytes).ToArray();
            }
            else
            {
                Dsk = DeviceManager.GetDSK(deviceNumber, NumBytes);
            }

            return Dsk;
        }

        public string GetDskAsString(Alias deviceNumber)
        {
            byte[] dsk = GetDsk(deviceNumber, 16);
            string deviceDsk_str = "";
            for (int indy = 0; indy < dsk.Length; indy = indy + 2)
            {
                deviceDsk_str += BitConverter.ToUInt16(new byte[] { dsk[indy],
                dsk[indy+1]}.Reverse().ToArray(), 0).ToString() + "-";
            }
            deviceDsk_str = deviceDsk_str.TrimEnd('-');
            return deviceDsk_str;
        }

        public byte[] GetDskFromEmbeded(Alias deviceNumber, int NumBytes)
        {
            return GetDsk(deviceNumber, NumBytes);
        }

        public int AddNodes(Alias controllerNumber, int timeoutMs, Alias[] nodeNumbers)
        {
            int ret = 0;
            if (nodeNumbers != null && nodeNumbers.Length > 0)
            {
                foreach (var item in nodeNumbers)
                {
                    var addRes = AddNode(controllerNumber, item, timeoutMs);
                    if (addRes)
                        ret++;
                }
            }
            return ret;
        }

        public int AddNodes(Alias controllerNumber, Alias[] nodeNumbers)
        {
            return AddNodes(controllerNumber, INCLUSION_TIMEOUT_MS, nodeNumbers);
        }

        public int AddNodesWithDSK(Alias controllerNumber, int timeoutMs, Alias[] nodeNumbers, byte[] dsk)
        {
            int ret = 0;
            if (nodeNumbers != null && nodeNumbers.Length > 0)
            {
                foreach (var item in nodeNumbers)
                {
                    var addRes = AddNodeWithDSK(controllerNumber, item, dsk, timeoutMs);
                    if (addRes)
                        ret++;
                }
            }
            return ret;
        }

        public int AddNodesWithDSK(Alias controllerNumber, int timeoutMs, Alias[] nodeNumbers, int dsk)
        {
            byte[] dskInner = null;
            try
            {
                dskInner = new byte[2] { (byte)(dsk >> 8), (byte)dsk };
            }
            catch (Exception)
            {
            }
            return AddNodesWithDSK(controllerNumber, timeoutMs, nodeNumbers, dskInner);
        }

        public int AddNodesWithDSK(Alias controllerNumber, Alias[] nodeNumbers, byte[] dsk)
        {
            return AddNodesWithDSK(controllerNumber, INCLUSION_TIMEOUT_MS, nodeNumbers, dsk);
        }

        public int AddNodesWithDSK(Alias controllerNumber, Alias[] nodeNumbers, int dsk)
        {
            return AddNodesWithDSK(controllerNumber, INCLUSION_TIMEOUT_MS, nodeNumbers, dsk);
        }

        private List<BasicResult.AddNodeWithLearnMode> AddNodesWithDSK(Alias controllerNumber, Alias[] nodeNumbers, Modes addMode, LearnModes learnMode, byte[] dsk, int timeoutMs)
        {
            var ret = new List<BasicResult.AddNodeWithLearnMode>();
            foreach (var item in nodeNumbers)
            {
                var addRes = AddNodeWithDSK(controllerNumber, item, addMode, learnMode, dsk, timeoutMs);
                ret.Add(addRes);

            }
            return ret;
        }

        public List<BasicResult.AddNodeWithLearnMode> AddNodesWithDSK(Alias controllerNumber, Alias[] nodeNumbers, Modes addMode, LearnModes learnMode, int dsk, int timeoutMs)
        {
            byte[] dskInner = null;
            try
            {
                dskInner = new byte[2] { (byte)(dsk >> 8), (byte)dsk };
            }
            catch (Exception)
            {
            }
            return AddNodesWithDSK(controllerNumber, nodeNumbers, addMode, learnMode, dskInner, timeoutMs);
        }

        public BasicResult.IncludeNodeWithLearnMode IncludeNode(Alias controllerNumber, Alias nodeNumber)
        {
            return IncludeNode(controllerNumber, nodeNumber, INCLUSION_TIMEOUT_MS);
        }

        public BasicResult.IncludeNodeWithLearnMode IncludeNode(Alias controllerNumber, Alias nodeNumber, int timeoutMs)
        {
            return IncludeNode(controllerNumber, nodeNumber, Modes.NodeAny, timeoutMs);
        }

        public BasicResult.IncludeNodeWithLearnMode IncludeNode(Alias controllerNumber, Alias nodeNumber, Modes addMode, int timeoutMs)
        {
            return IncludeNode(controllerNumber, nodeNumber, addMode, LearnModes.LearnModeClassic, timeoutMs);
        }

        public BasicResult.IncludeNodeWithLearnMode IncludeNode(Alias controllerNumber, Alias nodeNumber, Modes addMode, LearnModes learnMode, int timeoutMs)
        {
            BasicResult.IncludeNodeWithLearnMode ret = null;
            var controller = DeviceManager.GetDevice(controllerNumber);
            controller.Network.InclusionControllerInitiateRequestTimeoutMs = DeviceManager.IncludeSettings.InclusionControllerInitiateRequestTimeoutMs;
            var lib = TypeLibrary(nodeNumber).Library;
            var isEndDevice = lib == LibTypes.EndDeviceLib || lib == LibTypes.EndDeviceLegacyRoutingLib || lib == LibTypes.EndDeviceLegacyLib;
            DeviceManager.ReportTestFramesAndExtensionOverrides(controllerNumber);
            if (!DeviceManager.IsOptionDisabled(ZWaveSet.SmartStart, controllerNumber) &&
                isEndDevice &&
                controller.Network.IsNodeIdBaseTypeLR &&
                DeviceManager.GetDeviceHostClient(controllerNumber).IsLongRange &&
                DeviceManager.GetDevice(nodeNumber).Network.IsNodeIdBaseTypeLR &&
                DeviceManager.GetDeviceHostClient(nodeNumber).IsLongRange)
            {
                ret = IncludeNodeSmartStartLR(controllerNumber, nodeNumber, timeoutMs);
            }
            else
            {
                ret = IncludeNodeClassic(controllerNumber, nodeNumber, addMode, learnMode, timeoutMs);
            }
            controller.Network.InclusionControllerInitiateRequestTimeoutMs = null;
            return ret;
        }

        private BasicResult.IncludeNodeWithLearnMode IncludeNodeSmartStartLR(Alias controllerNumber, Alias nodeNumber, int timeoutMs)
        {
            DeviceManager.DisableSmartStart(controllerNumber);
            DeviceManager.CancelSmartStartSupport(controllerNumber);
            DeviceManager.EnableSmartStart(controllerNumber);
            DeviceManager.RunSmartStartSupport(controllerNumber);

            //Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            //controller.WaitNodeStatusSignal(ZWave.BasicApplication.Enums.NodeStatuses.LearnReady, timeoutMs);

            var schemes = DeviceManager.IncludeSettings.GrantedSchemes != null && DeviceManager.IncludeSettings.GrantedSchemes.Length != 0 ?
                DeviceManager.IncludeSettings.GrantedSchemes :
                SecuritySchemeSet.ALL;
            var modes = DeviceManager.IncludeSettings.SkipSmartStartLR ?
                Modes.NodeOptionNetworkWide :
                Modes.NodeOptionLongRange | Modes.NodeOptionNetworkWide;
            var Provisioning = new ProvisioningService(DeviceManager);
            Provisioning.Add(controllerNumber, nodeNumber, schemes, modes);

            var addToken = new BasicActionToken.ExpectSmartStartAdd(DeviceManager.GetSmartStartToken(timeoutMs));
            var learnRes = SetLearnMode(nodeNumber, LearnModes.LearnModeSmartStart | LearnModes.LearnModeNetworkMask);
            var addNodeRes = (InclusionResult)addToken.Token.WaitCompletedSignal();
            var addRes = new BasicActionResult.IncludeNodeWithLearnMode(addNodeRes);
            if (!learnRes)
            {
                MemoryGetId(nodeNumber);
            }
            addRes.SetLearnMode = learnRes;
            DeviceManager.IncludeSettings.Clear();
            Provisioning.Remove(controllerNumber, nodeNumber);
            return addRes;
        }

        private BasicResult.IncludeNodeWithLearnMode IncludeNodeClassic(Alias controllerNumber, Alias nodeNumber, Modes addMode, LearnModes learnMode, int timeoutMs)
        {
            if (DeviceManager.IncludeSettings.JoinDSK == null)
            {
                DeviceManager.IncludeSettings.JoinDSK = DeviceManager.GetDSK(nodeNumber, 2);
            }
            if (DeviceManager.IncludeSettings.JoinCSA == null)
            {
                DeviceManager.IncludeSettings.JoinCSA = DeviceManager.GetCSA(controllerNumber, 4);
            }

            DeviceManager.DisableSmartStart(controllerNumber);

            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            DeviceManager.ReportTestFramesAndExtensionOverrides(controllerNumber);
            Modes addModeFx = addMode == Modes.NodeAnyS2 ? Modes.NodeAny : addMode;

            var setupNodeLifelineSettings = (ZWave.BasicApplication.Enums.SetupNodeLifelineSettings)DeviceManager.IncludeSettings.SetupNodeLifelineSettings;
            ActionToken includeToken = controller.IncludeNode((ZWave.Enums.Modes)addModeFx, timeoutMs, null, setupNodeLifelineSettings: setupNodeLifelineSettings);

            BasicResult.SetLearnMode learnRes = null;
            controller.WaitNodeStatusSignal(ZWave.BasicApplication.Enums.NodeStatuses.LearnReady, timeoutMs);
            Thread.Sleep(1000);
            learnRes = SetLearnMode(nodeNumber, learnMode, timeoutMs);
            InclusionResult includeResult = (InclusionResult)includeToken.WaitCompletedSignal();
            var ret = new BasicActionResult.IncludeNodeWithLearnMode(includeResult);
            ret.SetLearnMode = learnRes;
            if (DeviceManager.GetDevice(nodeNumber) is Controller)
            {
                GetSucNodeId(nodeNumber);
            }
            if (ret.AddNodeResult.SubstituteStatus != SubstituteStatuses.None)
            {
                DeviceManager.LogNetworkKeys(controllerNumber, includeResult.AddRemoveNode.Node);
            }
            DeviceManager.EnableSmartStart(controllerNumber);
            DeviceManager.IncludeSettings.Clear();
            return ret;
        }

        public BasicResult.RemoveNode RemoveNode(Alias controllerNumber, Alias nodeNumber, int timeoutMs)
        {
            return RemoveNode(controllerNumber, nodeNumber, Modes.NodeAny, LearnModes.LearnModeClassic, timeoutMs);
        }

        public BasicResult.RemoveNode RemoveNode(Alias controllerNumber, Alias nodeNumber, Modes removeMode, LearnModes learnMode, int timeoutMs)
        {
            Modes removeModeFx = removeMode == Modes.NodeAnyS2 ? Modes.NodeAny : removeMode;
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            DeviceManager.DisableSmartStart(controllerNumber);
            ActionToken addRemoveToken = controller.RemoveNodeFromNetwork((ZWave.Enums.Modes)removeModeFx, timeoutMs, (x) => DeviceManager.EnableSmartStart(controllerNumber));
            SetLearnMode(nodeNumber, learnMode, timeoutMs);
            AddRemoveNodeResult res = (AddRemoveNodeResult)addRemoveToken.WaitCompletedSignal();
            return new BasicActionResult.RemoveNode(res);
        }

        public BasicResult.RemoveNode RemoveNode(Alias controllerNumber, Alias nodeNumber)
        {
            return RemoveNode(controllerNumber, nodeNumber, INCLUSION_TIMEOUT_MS);
        }

        public int RemoveNodes(Alias controllerNumber, int timeoutMs, params Alias[] nodeNumbers)
        {
            int ret = 0;
            if (nodeNumbers != null && nodeNumbers.Length > 0)
            {
                foreach (var item in nodeNumbers)
                {
                    BasicResult.RemoveNode node = RemoveNode(controllerNumber, item, timeoutMs);
                    if (node)
                        ret++;
                }
            }
            return ret;
        }

        public int RemoveNodes(Alias controllerNumber, params Alias[] nodeNumbers)
        {
            return RemoveNodes(controllerNumber, INCLUSION_TIMEOUT_MS, nodeNumbers);
        }

        public BasicResult.RemoveNode ExcludeNode(Alias controllerNumber, Alias nodeNumber)
        {
            return ExcludeNode(controllerNumber, nodeNumber, INCLUSION_TIMEOUT_MS);
        }

        public BasicResult.RemoveNode ExcludeNode(Alias controllerNumber, Alias nodeNumber, int timeoutMs)
        {
            return ExcludeNode(controllerNumber, nodeNumber, Modes.NodeAny, timeoutMs);
        }

        public BasicResult.RemoveNode ExcludeNode(Alias controllerNumber, Alias nodeNumber, Modes removeMode, int timeoutMs)
        {
            Device device = DeviceManager.GetDevice(nodeNumber);
            var learnMode = device.Network.IsLongRangeEnabled(device.Network.NodeTag) ?
                LearnModes.LearnModeNWE | LearnModes.LearnModeNetworkMask :
                LearnModes.LearnModeClassic;
            return ExcludeNode(controllerNumber, nodeNumber, removeMode, learnMode, timeoutMs);
        }

        public BasicResult.RemoveNode ExcludeNode(Alias controllerNumber, Alias nodeNumber, Modes removeMode, LearnModes learnMode, int timeoutMs)
        {
            DeviceManager.DisableSmartStart(controllerNumber);
            Modes removeModeFx = removeMode == Modes.NodeAnyS2 ? Modes.NodeAny : removeMode;
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            ActionToken excludeToken = controller.ExcludeNode((ZWave.Enums.Modes)removeModeFx, timeoutMs, null);
            SetLearnMode(nodeNumber, learnMode, timeoutMs);
            ExclusionResult res = (ExclusionResult)excludeToken.WaitCompletedSignal();
            DeviceManager.EnableSmartStart(controllerNumber);
            return new BasicActionResult.RemoveNode(res.AddRemoveNode);
        }

        public BasicResult.RemoveNode RemoveNodeIdFromNetwork(Alias controllerNumber, Alias nodeNumber, Modes removeMode, LearnModes learnMode, int timeoutMs)
        {
            Modes removeModeFx = removeMode == Modes.NodeAnyS2 ? Modes.NodeAny : removeMode;
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device deviceNode = DeviceManager.GetDevice(nodeNumber);
            DeviceManager.DisableSmartStart(controllerNumber);
            AddRemoveNodeResult removeNode = controller.RemoveNodeIdFromNetwork((ZWave.Enums.Modes)removeModeFx, deviceNode.Network.NodeTag, timeoutMs);
            SetLearnMode(nodeNumber, learnMode, timeoutMs);
            return new BasicActionResult.RemoveNode(removeNode);
        }

        public BasicResult.AddNode CreateNewPrimary(Alias controllerNumber, Alias newPrimaryControllerNumber)
        {
            return CreateNewPrimary(controllerNumber, newPrimaryControllerNumber, INCLUSION_TIMEOUT_MS);
        }

        public BasicResult.AddNode CreateNewPrimary(Alias controllerNumber, Alias newPrimaryControllerNumber, int timeoutMs)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            ActionToken addRemoveToken = controller.CreateNewPrimary(ZWave.BasicApplication.Enums.CreatePrimaryModes.Start, timeoutMs, null);
            SetLearnMode(newPrimaryControllerNumber, LearnModes.LearnModeClassic, timeoutMs);
            AddRemoveNodeResult res = (AddRemoveNodeResult)addRemoveToken.WaitCompletedSignal();
            return new BasicActionResult.AddNode(res);
        }

        /// <summary>
        /// Consequentially calls SetDefault(), SoftReset(), MemoryGetId()
        /// </summary>
        /// <param name="deviceNumber"></param>
        public void Reset(Alias deviceNumber)
        {
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            DeviceManager.Reset(deviceHostClient, deviceNumber);
        }

        public bool Reconnect(Alias deviceNumber)
        {
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            bool ret = DeviceManager.Reconnect(deviceHostClient);
            return ret;
        }

        public bool Disconnect(Alias deviceNumber)
        {
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            bool ret = DeviceManager.Disconnect(deviceHostClient);
            return ret;
        }

        public bool Connect(Alias deviceNumber)
        {
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            bool ret = DeviceManager.Connect(deviceHostClient);
            return ret;
        }

        public BasicResult.Void SetDefault(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.SetDefault();
            device.MemoryGetId();
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void SoftReset(Alias deviceNumber)
        {
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            return DeviceManager.SoftReset(deviceHostClient, deviceNumber);
        }

        public BasicResult.SerialApiGetCapabilities SerialApiGetCapabilities(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            SerialApiGetCapabilitiesResult res = device.SerialApiGetCapabilities();
            return new BasicResult.SerialApiGetCapabilities(res);
        }

        public BasicResult.MemoryGetId MemoryGetId(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            MemoryGetIdResult res = device.MemoryGetId();
            return new BasicActionResult.MemoryGetId(res);
        }

        public BasicResult.Void EnableSUC(Alias deviceNumber, bool isEnable, byte capabilities)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(deviceNumber);
            ActionResult res = controller.EnableSUC(isEnable, capabilities);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.SetSucNodeId SetSUCNodeID(Alias controllerNumber, Alias controllerSucNumber)
        {
            return SetSUCNodeID(controllerNumber, controllerSucNumber, true, 1, true);
        }

        public BasicResult.SetSucNodeId SetSUCNodeID(Alias controllerNumber, Alias controllerSucNumber, bool isEnable, byte capabilities, bool isLowPower)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Controller suc = (Controller)DeviceManager.GetDevice(controllerSucNumber);
            SetSucNodeIdResult res = controller.SetSucNodeID(suc.Network.NodeTag, isEnable, isLowPower, capabilities);
            controller.GetSucNodeId();
            return new BasicActionResult.SetSucNodeId(res);
        }

        public BasicResult.Transmit SendNodeInfo(Alias deviceNumber, Alias receiverNumber, TransmitOptions txOptions)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            Device receiver = DeviceManager.GetDevice(receiverNumber);
            TransmitResult res = device.SendNodeInformation(receiver.Network.NodeTag, (ZWave.Enums.TransmitOptions)txOptions);
            return new BasicActionResult.Transmit(res);
        }

        public BasicResult.Transmit SendNop(Alias senderNumber, Alias receiverNumber, TransmitOptions txOptions)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            Device receiver = DeviceManager.GetDevice(receiverNumber);
            TransmitResult res = sender.SendData(new NodeTag(receiver.Id, receiverNumber.EndPointId), new byte[] { 0 }, (ZWave.Enums.TransmitOptions)txOptions);
            return new BasicActionResult.Transmit(res);
        }

        public BasicResult.Transmit EnqueueSendNop(Alias senderNumber, Alias receiverNumber, TransmitOptions txOptions)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            Device receiver = DeviceManager.GetDevice(receiverNumber);
            TransmitResult res = sender.SendData(new NodeTag(receiver.Id, receiverNumber.EndPointId), new byte[] { 0 }, (ZWave.Enums.TransmitOptions)txOptions);
            return new BasicActionResult.Transmit(res);
        }

        public BasicResult.Transmit SendBroadCastNodeInfo(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            TransmitResult res = device.SendNodeInformation(new NodeTag(0xFF), ZWave.Enums.TransmitOptions.TransmitOptionNone);
            return new BasicActionResult.Transmit(res);
        }

        public BasicResult.Transmit SendBroadCastNodeInfo(Alias deviceNumber, TransmitOptions txOptions)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            TransmitResult res = device.SendNodeInformation(new NodeTag(0xFF), (ZWave.Enums.TransmitOptions)txOptions);
            return new BasicActionResult.Transmit(res);
        }

        public BasicResult.GetProtocolInfo GetProtocolInfo(Alias controllerNumber, Alias requestedDeviceNumber)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device requestedDevice = DeviceManager.GetDevice(requestedDeviceNumber);
            GetNodeProtocolInfoResult res = controller.GetProtocolInfo(requestedDevice.Network.NodeTag);
            return new BasicResult.GetProtocolInfo(res);
        }

        public BasicResult.SendData SendData(Alias senderNumber, Alias receiverNumber, byte[] data)
        {
            return SendData(senderNumber, receiverNumber, data, TX_OPTIONS);
        }

        public BasicResult.SendData SendData(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            Device receiver = DeviceManager.GetDevice(receiverNumber);
            SendDataResult res = sender.SendData(new NodeTag(receiver.Id, receiverNumber.EndPointId), data, (ZWave.Enums.TransmitOptions)txOptions);
            return new BasicActionResult.SendData(res);
        }

        public BasicResult.SendData SendDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, SecuritySchemes scheme)
        {
            return SendDataEx(senderNumber, receiverNumber, data, TX_OPTIONS, TX_SEC_OPTIONS, scheme, TransmitOptions2.NONE);
        }

        public BasicResult.SendData SendDataEx(Alias senderNumber, Alias receiverNumber, byte[] data,
            TransmitOptions txOptions, SecuritySchemes scheme)
        {
            return SendDataEx(
                senderNumber,
                receiverNumber,
                data,
                txOptions,
                TX_SEC_OPTIONS,
                scheme,
                TransmitOptions2.NONE);
        }

        public BasicResult.SendData SendDataEx(Alias senderNumber, Alias receiverNumber, byte[] data,
            TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            Device receiver = DeviceManager.GetDevice(receiverNumber);
            SendDataResult res = sender.SendDataEx(
                new NodeTag(receiver.Id, receiverNumber.EndPointId),
                data,
                (ZWave.Enums.TransmitOptions)txOptions,
                (ZWave.Enums.TransmitSecurityOptions)txSecOptions,
                (ZWave.Enums.SecuritySchemes)scheme,
                (ZWave.Enums.TransmitOptions2)txOptions2);
            return new BasicActionResult.SendData(res);
        }

        private volatile byte supervisionSessionId = 0;
        public BasicResult.Transmit SendDataMultiEx(Alias senderNumber, Alias[] receiverNumbers, byte groupId, byte[] data, SecuritySchemes scheme)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            TransmitResult res = sender.SendDataMultiEx(data, (ZWave.Enums.TransmitOptions)TX_OPTIONS, (ZWave.Enums.SecuritySchemes)scheme, groupId);
            if (!SendDataSettings.GetSupressMulticastFollowUp(senderNumber))
            {
                var txSOptions = TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY | TransmitSecurityOptions.S2_TXOPTION_FIRST_SINGLECAST_FOLLOWUP | TransmitSecurityOptions.S2_TXOPTION_SINGLECAST_FOLLOWUP;
                foreach (var node in receiverNumbers)
                {
                    var svget = new COMMAND_CLASS_SUPERVISION.SUPERVISION_GET();
                    svget.encapsulatedCommand = data;
                    svget.encapsulatedCommandLength = (byte)data.Length;
                    svget.properties1.sessionId = ++supervisionSessionId;
                    RequestDataEx(
                        senderNumber,
                        node,
                        svget,
                        TX_OPTIONS,
                        txSOptions,
                        scheme,
                        new COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT(),
                        2000);
                    txSOptions = TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY | TransmitSecurityOptions.S2_TXOPTION_SINGLECAST_FOLLOWUP;
                }
            }
            return new BasicActionResult.Transmit(res);
        }

        public BasicResult.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, byte[] expectedData)
        {
            return RequestData(senderNumber, receiverNumber, data, TX_OPTIONS, expectedData, 10000);
        }

        public BasicResult.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, byte[] expectedData, int timeoutMs)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            Device receiver = DeviceManager.GetDevice(receiverNumber);
            RequestDataResult res = sender.RequestData(new NodeTag(receiver.Id, receiverNumber.EndPointId), data, (ZWave.Enums.TransmitOptions)txOptions, expectedData, timeoutMs);
            BasicResult.RequestData ret = new BasicActionResult.RequestData(res);
            return ret;
        }

        public BasicResult.RequestData RequestDataBridge(Alias senderNumber, Alias receiverNumber, Node srcNode, byte[] data, TransmitOptions txOptions, byte[] expectedData, int timeoutMs)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            Device receiver = DeviceManager.GetDevice(receiverNumber);
            RequestDataResult res = sender.RequestData(srcNode, new NodeTag(receiver.Id, receiverNumber.EndPointId), data, (ZWave.Enums.TransmitOptions)txOptions, expectedData, timeoutMs);
            BasicResult.RequestData ret = new BasicActionResult.RequestData(res);
            return ret;
        }

        public BasicResult.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, SecuritySchemes scheme, byte[] expectedData)
        {
            return RequestDataEx(senderNumber, receiverNumber, data, TX_OPTIONS, TX_SEC_OPTIONS, scheme, expectedData, 10000);
        }

        public BasicResult.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, byte[] expectedData, int timeoutMs)
        {
            return RequestDataEx(senderNumber, receiverNumber, data, txOptions, txSecOptions, scheme, TransmitOptions2.NONE, expectedData, timeoutMs);
        }

        public BasicResult.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2, byte[] expectedData, int timeoutMs)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            Device receiver = DeviceManager.GetDevice(receiverNumber);
            RequestDataResult res = sender.RequestDataEx(new NodeTag(receiver.Id, receiverNumber.EndPointId), data, (ZWave.Enums.TransmitOptions)txOptions, (ZWave.Enums.TransmitSecurityOptions)txSecOptions, (ZWave.Enums.SecuritySchemes)scheme, (ZWave.Enums.TransmitOptions2)txOptions2, expectedData, timeoutMs);
            BasicResult.RequestData ret = new BasicActionResult.RequestData(res);
            return ret;
        }

        /// <summary>
        /// Waits expected data
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="expectedData"></param>
        /// <returns></returns>
        public BasicResult.ExpectData ExpectData(Alias deviceNumber, byte[] expectedData)
        {
            return ExpectData(deviceNumber, expectedData, REQUEST_TIMEOUT_MS);
        }

        /// <summary>
        /// Waits expected data
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="expectedData"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        public BasicResult.ExpectData ExpectData(Alias deviceNumber, byte[] expectedData, int timeoutMs)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ExpectDataResult result = device.ExpectData(expectedData, timeoutMs);
            return new BasicActionResult.ExpectData(result);
        }

        public BasicResult.Transmit SendDataMulti(Alias senderNumber, Alias[] receiverNumbers, byte[] data, TransmitOptions txOptions)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            NodeTag[] receiverNodeIds = new NodeTag[receiverNumbers.Length];
            for (int i = 0; i < receiverNumbers.Length; i++)
            {
                receiverNodeIds[i] = DeviceManager.GetDevice(receiverNumbers[i]).Network.NodeTag;
            }
            TransmitResult res = sender.SendDataMulti(receiverNodeIds, data, (ZWave.Enums.TransmitOptions)txOptions);
            return new BasicActionResult.Transmit(res);
        }

        public BasicResult.Void SetPromiscuousMode(Alias deviceNumber, bool state)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.SetPromiscuousMode(state);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode, bool isNonSecure, int timeoutMs)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ZWave.Enums.LearnModes lmode = (ZWave.Enums.LearnModes)(byte)mode;
            DeviceManager.DisableSmartStart(deviceNumber);
            SetLearnModeResult res = device.SetLearnMode(lmode, isNonSecure, timeoutMs);
            DeviceManager.EnableSmartStart(deviceNumber);
            if (res.SubstituteStatus != ZWave.Enums.SubstituteStatuses.Done)
            {
                var version = device.Version.Replace("Z-Wave ", string.Empty);
                var major = version.Split('.')[0];
                byte majorVal = 0;
                if (device.ChipType == ZWave.Enums.ChipTypes.ZW050x)
                {
                    if (byte.TryParse(major, out majorVal) && majorVal >= 5 &&
                        (device.Library == ZWave.Enums.Libraries.EndDeviceLib ||
                        device.Library == ZWave.Enums.Libraries.EndDeviceLegacyRoutingLib))
                    {
                        Thread.Sleep(11000);
                    }
                }
            }
            return new BasicActionResult.SetLearnMode(res);
        }

        public BasicResult.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode, int timeoutMs)
        {
            return SetLearnMode(deviceNumber, mode, false, timeoutMs);
        }

        public BasicResult.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode)
        {
            return SetLearnMode(deviceNumber, mode, INCLUSION_TIMEOUT_MS);
        }

        public BasicResult.AreNodesNeighbours AreNodesNeighbours(Alias controllerNumber, Alias nodeNumber1, Alias nodeNumber2)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device nodeDevice1 = DeviceManager.GetDevice(nodeNumber1);
            Device nodeDevice2 = DeviceManager.GetDevice(nodeNumber2);
            ReturnValueResult res = controller.AreNodesNeighbours(nodeDevice1.Network.NodeTag, nodeDevice2.Network.NodeTag);
            return new BasicResult.AreNodesNeighbours(res);
        }

        public BasicResult.AssignReturnRoute AssignReturnRoute(Alias controllerNumber, Alias srcNode, Alias destNode)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device nodeDevice1 = DeviceManager.GetDevice(srcNode);
            Device nodeDevice2 = DeviceManager.GetDevice(destNode);
            AssignReturnRouteResult res = controller.AssignReturnRoute(nodeDevice1.Network.NodeTag, nodeDevice2.Network.NodeTag, out ActionToken token);
            return new BasicResult.AssignReturnRoute(res);
        }

        public BasicResult.AssignReturnRoute AssignPriorityReturnRoute(Alias controllerNumber, Alias srcNode, Alias destNode,
            Node priorityRoute0, Node priorityRoute1, Node priorityRoute2, Node priorityRoute3, byte routespeed)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device nodeDevice1 = DeviceManager.GetDevice(srcNode);
            Device nodeDevice2 = DeviceManager.GetDevice(destNode);
            AssignReturnRouteResult res = controller.AssignPriorityReturnRoute(nodeDevice1.Network.NodeTag, nodeDevice2.Network.NodeTag,
                priorityRoute0, priorityRoute1, priorityRoute2, priorityRoute3, routespeed, out ActionToken token);
            return new BasicResult.AssignReturnRoute(res);
        }


        public BasicResult.Void AssignSucReturnRoute(Alias controllerNumber, Alias nodeNumber)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device nodeDevice = DeviceManager.GetDevice(nodeNumber);
            ActionResult res = controller.AssignSucReturnRoute(nodeDevice.Network.NodeTag, out ActionToken token);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void AssignPrioritySucReturnRoute(Alias controllerNumber, Alias nodeNumber, Node repeater0, Node repeater1, Node repeater2, Node repeater3, byte routespeed)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device nodeDevice = DeviceManager.GetDevice(nodeNumber);
            ActionResult res = controller.AssignPrioritySucReturnRoute(nodeDevice.Network.NodeTag, repeater0, repeater1, repeater2, repeater3, routespeed, out ActionToken token);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void DeleteReturnRoute(Alias controllerNumber, Alias nodeNumber)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device nodeDevice = DeviceManager.GetDevice(nodeNumber);
            ActionResult res = controller.DeleteReturnRoute(nodeDevice.Network.NodeTag, out ActionToken token);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void DeleteSucReturnRoute(Alias controllerNumber, Alias nodeNumber)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device nodeDevice = DeviceManager.GetDevice(nodeNumber);
            ActionResult res = controller.DeleteSucReturnRoute(nodeDevice.Network.NodeTag, out ActionToken token);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void ExploreRequestInclusion(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.ExploreRequestInclusion();
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void ExploreRequestExclusion(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.ExploreRequestExclusion();
            return new BasicActionResult.Void(res);
        }

        public BasicResult.GetControllerCapabilities GetControllerCapabilities(Alias controllerNumber)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            ActionResult res = controller.GetControllerCapabilities();
            return new BasicResult.GetControllerCapabilities((GetControllerCapabilitiesResult)res);
        }

        public BasicResult.GetNeighborCount GetNeighborCount(Alias controllerNumber, Alias nodeNumber)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device nodeDevice = DeviceManager.GetDevice(nodeNumber);
            GetNeighborCountResult res = controller.GetNeighborCount(nodeDevice.Network.NodeTag);
            return new BasicResult.GetNeighborCount(res);
        }

        public BasicResult.GetProtocolStatus GetProtocolStatus(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            GetProtocolStatusResult res = device.GetProtocolStatus();
            return new BasicResult.GetProtocolStatus(res);
        }

        public BasicResult.GetRandomWord GetRandomWord(Alias deviceNumber, byte count)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            GetRandomWordResult res = device.GetRandomWord(count);
            return new BasicResult.GetRandomWord(res);
        }

        public BasicResult.GetRoutingInfo GetRoutingInfo(Alias controllerNumber, Alias nodeNumber, byte removeBad, byte removeNonReps)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device nodeDevice = DeviceManager.GetDevice(nodeNumber);
            GetRoutingInfoResult res = controller.GetRoutingInfo(nodeDevice.Network.NodeTag, removeBad, removeNonReps);
            return new BasicResult.GetRoutingInfo(res);
        }

        public BasicResult.GetSucNodeId GetSucNodeId(Alias controllerNumber)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            GetSucNodeIdResult res = controller.GetSucNodeId();
            return new BasicResult.GetSucNodeId(res);
        }

        public BasicResult.GetTransmitCount GetTransmitCount(Alias installerNumber)
        {
            InstallerController installer = (InstallerController)DeviceManager.GetDevice(installerNumber);
            GetTransmitCountResult res = installer.GetTransmitCount();
            return new BasicResult.GetTransmitCount(res);
        }

        public BasicResult.GetVirtualNodes GetVirtualNodes(Alias bridgeNumber)
        {
            BridgeController bridge = (BridgeController)DeviceManager.GetDevice(bridgeNumber);
            GetVirtualNodesResult res = bridge.GetVirtualNodes();
            return new BasicResult.GetVirtualNodes(res);
        }

        public string GetDevicePort(Alias deviceNumber)
        {
            string ret = string.Empty;
            var device = DeviceManager.GetDevice(deviceNumber);
            if (device != null)
                ret = device.DataSource.SourceName;
            return ret;
        }

        public BasicResult.IsFailedNode IsFailedNode(Alias controllerNumber, Alias nodeNumber)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device nodeDevice = DeviceManager.GetDevice(nodeNumber);
            IsFailedNodeResult res = controller.IsFailedNode(nodeDevice.Network.NodeTag);
            return new BasicActionResult.IsFailedNode(res);
        }

        public BasicResult.IsNodeWithinDirectRange IsNodeWithinDirectRange(Alias endDeviceNumber, Alias nodeNumber)
        {
            EndDevice endDevice = (EndDevice)DeviceManager.GetDevice(endDeviceNumber);
            Device nodeDevice = DeviceManager.GetDevice(nodeNumber);
            IsNodeWithinDirectRangeResult res = endDevice.IsNodeWithinDirectRange((byte)nodeDevice.Id);
            return new BasicResult.IsNodeWithinDirectRange(res);
        }

        public BasicResult.IsVirtualNode IsVirtualNode(Alias bridgeNumber, byte nodeId)
        {
            BridgeController bridge = (BridgeController)DeviceManager.GetDevice(bridgeNumber);
            IsVirtualNodeResult res = bridge.IsVirtualNode(new NodeTag(nodeId));
            return new BasicResult.IsVirtualNode(res);
        }

        public BasicResult.MemoryGetBuffer MemoryGetBuffer(Alias deviceNumber, ushort offset, byte length)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            MemoryGetBufferResult res = device.MemoryGetBuffer(offset, length);
            return new BasicResult.MemoryGetBuffer(res);
        }

        public BasicResult.MemoryGetByte MemoryGetByte(Alias deviceNumber, ushort offset)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            MemoryGetByteResult res = device.MemoryGetByte(offset);
            return new BasicResult.MemoryGetByte(res);
        }

        public BasicResult.Void MemoryPutBuffer(Alias deviceNumber, ushort offset, ushort length, byte[] data)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.MemoryPutBuffer(offset, length, data);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void MemoryPutByte(Alias deviceNumber, ushort offset, byte data)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.MemoryPutByte(offset, data);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.RandomByte RandomByte(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            RandomResult res = device.RandomByte();
            return new BasicResult.RandomByte(res);
        }

        public BasicResult.Void RediscoveryNeeded(Alias endDeviceNumber, Alias nodeNumber)
        {
            EndDevice endDevice = (EndDevice)DeviceManager.GetDevice(endDeviceNumber);
            Device nodeDevice = DeviceManager.GetDevice(nodeNumber);
            ActionResult res = endDevice.RediscoveryNeeded((byte)nodeDevice.Id);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void RediscoveryNeededBroadcast(Alias endDeviceNumber)
        {
            EndDevice endDevice = (EndDevice)DeviceManager.GetDevice(endDeviceNumber);
            ActionResult res = endDevice.RediscoveryNeeded(0xFF);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void RemoveFailedNodeId(Alias controllerNumber, Alias nodeNumber)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device nodeDevice = DeviceManager.GetDevice(nodeNumber);
            DeviceManager.DisableSmartStart(controllerNumber);
            ActionResult res = controller.RemoveFailedNodeId(nodeDevice.Network.NodeTag);
            DeviceManager.EnableSmartStart(controllerNumber);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void ReplaceFailedNode(Alias controllerNumber, Alias nodeNumber)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device nodeDevice = DeviceManager.GetDevice(nodeNumber);
            DeviceManager.DisableSmartStart(controllerNumber);
            var res = controller.ReplaceFailedNode(nodeDevice.Network.NodeTag);
            DeviceManager.EnableSmartStart(controllerNumber);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void ReplicationReceiveComplete(Alias controllerNumber)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            ActionResult res = controller.ReplicationReceiveComplete();
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Transmit ReplicationSend(Alias controllerNumber, Alias nodeNumber, byte[] data, TransmitOptions txOptions)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device node = DeviceManager.GetDevice(nodeNumber);
            TransmitResult res = controller.ReplicationSend(node.Network.NodeTag, data, (ZWave.Enums.TransmitOptions)txOptions);
            return new BasicActionResult.Transmit(res);
        }

        public BasicResult.RequestNetworkUpdate RequestNetworkUpdate(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            var res = device.RequestNetworkUpdate();
            return new BasicActionResult.RequestNetworkUpdate(res);
        }

        public BasicResult.Void RequestNewRouteDestinations(Alias endDeviceNumber, byte[] destList)
        {
            EndDevice endDevice = (EndDevice)DeviceManager.GetDevice(endDeviceNumber);
            ActionResult res = endDevice.RequestNewRouteDestinations(destList);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.RequestNodeInfo RequestNodeInfo(Alias deviceNumber, Alias nodeNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            Device nodeDevice = DeviceManager.GetDevice(nodeNumber);
            RequestNodeInfoResult res = device.RequestNodeInfo(nodeDevice.Network.NodeTag);
            return new BasicActionResult.RequestNodeInfo(res);
        }

        public BasicResult.GetNodeInfo GetNodeInfo(Alias deviceNumber, Alias nodeNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            Device nodeDevice = DeviceManager.GetDevice(nodeNumber);
            var res = device.NodeInfo(nodeDevice.Network.NodeTag, out ActionToken token);
            return new BasicActionResult.GetNodeInfo(res);
        }

        public BasicResult.RequestNodeNeighborUpdate RequestNodeNeighborUpdate(Alias controllerNumber, Alias nodeNumber)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device node = DeviceManager.GetDevice(nodeNumber);
            RequestNodeNeighborUpdateResult res = controller.RequestNodeNeighborUpdate(node.Network.NodeTag, 180000);
            return new BasicActionResult.RequestNodeNeighborUpdate(res);
        }

        public BasicResult.RequestNodeNeighborUpdate RequestNodeNeighborDiscovery(Alias controllerNumber, Alias nodeNumber, byte nodeType)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device node = DeviceManager.GetDevice(nodeNumber);
            RequestNodeNeighborUpdateResult res = controller.RequestNodeNeighborDiscovery(node.Network.NodeTag, nodeType, 180000);
            return new BasicActionResult.RequestNodeNeighborUpdate(res);
        }

        public BasicResult.SerialApiGetInitData SerialApiGetInitData(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            SerialApiGetInitDataResult res = device.SerialApiGetInitData();
            return new BasicResult.SerialApiGetInitData(res);
        }

        public BasicResult.SerialApiGetLRNodes SerialApiGetLRNodes(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            SerialApiGetLRNodesResult res = device.SerialApiGetLRNodes();
            return new BasicResult.SerialApiGetLRNodes(res);
        }

        public BasicResult.Void ResetTransmitCount(Alias installerNumber)
        {
            InstallerController installer = (InstallerController)DeviceManager.GetDevice(installerNumber);
            ActionResult res = installer.ResetTransmitCount();
            return new BasicActionResult.Void(res);
        }

        public BasicResult.RFPowerLevelGet RFPowerLevelGet(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            RFPowerLevelGetResult res = device.RFPowerLevelGet();
            return new BasicResult.RFPowerLevelGet(res);
        }

        public BasicResult.Void RFPowerlevelRediscoverySet(Alias deviceNumber, byte powerLevel)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.RFPowerlevelRediscoverySet(powerLevel);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void RFPowerLevelSet(Alias deviceNumber, byte powerLevel)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.RFPowerLevelSet(powerLevel);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void SendDataAbort(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.SendDataAbort();
            return new BasicActionResult.Void(res);
        }

        public BasicResult.SendData SendDataBridge(Alias bridgeNumber, Node srcNode, Alias destNodeNumber, byte[] data, TransmitOptions txOptions)
        {
            BridgeController bridge = (BridgeController)DeviceManager.GetDevice(bridgeNumber);
            Device destNode = DeviceManager.GetDevice(destNodeNumber);
            SendDataResult res = bridge.SendDataBridge(srcNode, new NodeTag(destNode.Id, destNodeNumber.EndPointId), data, (ZWave.Enums.TransmitOptions)txOptions);
            return new BasicActionResult.SendData(res);
        }

        public BasicResult.SendData SendDataBridge(Alias bridgeNumber, Node srcNode, Node destNode, byte[] data, TransmitOptions txOptions)
        {
            BridgeController bridge = (BridgeController)DeviceManager.GetDevice(bridgeNumber);
            SendDataResult res = bridge.SendDataBridge(srcNode, destNode, data, (ZWave.Enums.TransmitOptions)txOptions);
            return new BasicActionResult.SendData(res);
        }

        public BasicResult.Transmit SendDataMetaBridge(Alias bridgeNumber, Node srcNode, Alias destNodeNumber, byte[] data, TransmitOptions txOptions)
        {
            BridgeController bridge = (BridgeController)DeviceManager.GetDevice(bridgeNumber);
            Device destNode = DeviceManager.GetDevice(destNodeNumber);
            TransmitResult res = bridge.SendDataMetaBridge(srcNode, new NodeTag(destNode.Id, destNodeNumber.EndPointId), data, (ZWave.Enums.TransmitOptions)txOptions);
            return new BasicActionResult.Transmit(res);
        }

        public BasicResult.Transmit SendDataMultiBridge(Alias bridgeNumber, Node srcNode, Alias[] destNodeNumbers, byte[] data, TransmitOptions txOptions)
        {
            BridgeController bridge = (BridgeController)DeviceManager.GetDevice(bridgeNumber);
            byte[] receiverNodeIds = new byte[destNodeNumbers.Length];
            for (int i = 0; i < destNodeNumbers.Length; i++)
            {
                receiverNodeIds[i] = (byte)DeviceManager.GetDevice(destNodeNumbers[i]).Id;
            }
            TransmitResult res = bridge.SendDataMultiBridge(srcNode, receiverNodeIds, data, (ZWave.Enums.TransmitOptions)txOptions);
            return new BasicActionResult.Transmit(res);
        }

        public BasicResult.Transmit SendVirtualDeviceNodeInformation(Alias bridgeNumber, Node srcNode, Alias destNodeNumber, TransmitOptions txOptions)
        {
            BridgeController bridge = (BridgeController)DeviceManager.GetDevice(bridgeNumber);
            Device destNode = DeviceManager.GetDevice(destNodeNumber);
            TransmitResult res = bridge.SendVirtualDeviceNodeInformation(srcNode, destNode.Network.NodeTag, (ZWave.Enums.TransmitOptions)txOptions);
            return new BasicActionResult.Transmit(res);
        }

        public BasicResult.Transmit SendSucId(Alias controllerNumber, Alias nodeNumber, TransmitOptions txOptions)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device node = DeviceManager.GetDevice(nodeNumber);
            TransmitResult res = controller.SendSucId(node.Network.NodeTag, (ZWave.Enums.TransmitOptions)txOptions);
            return new BasicActionResult.Transmit(res);
        }

        public BasicResult.Transmit SendTestFrame(Alias deviceNumber, Alias nodeNumber, byte powerLevel)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            Device destDevice = DeviceManager.GetDevice(nodeNumber);
            TransmitResult res = device.SendTestFrame(destDevice.Network.NodeTag, powerLevel);
            return new BasicActionResult.Transmit(res);
        }

        public BasicResult.Void ApplicationNodeInformationCmdClasses(Alias deviceNumber, byte[] nodeParameters, byte[] unsecureNodeParameters, byte[] secureNodeParameters)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.ApplicationNodeInformationCmdClasses(nodeParameters, unsecureNodeParameters, secureNodeParameters);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void ApplicationNodeInformation(Alias deviceNumber, params byte[] cmdClasses)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            var nif = device.Network.GetNodeInfo();
            ActionResult res = device.ApplicationNodeInformation(nif.DeviceOptions, nif.Generic, nif.Specific, cmdClasses);
            device.Network.SetApplicationNodeInformation(nif.DeviceOptions, nif.Generic, nif.Specific, cmdClasses);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void ApplicationNodeInformation(Alias deviceNumber, bool isListening, byte generic, byte specific, params byte[] cmdClasses)
        {
            var opt = (ZWave.Enums.DeviceOptions)(isListening ? DeviceOptions.Listening : DeviceOptions.NoneListening);
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.ApplicationNodeInformation(opt, generic, specific, cmdClasses);
            device.Network.SetApplicationNodeInformation(opt, generic, specific, cmdClasses);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void ApplicationNodeInformation(Alias deviceNumber, DeviceOptions deviceOptions, byte generic, byte specific, params byte[] cmdClasses)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.ApplicationNodeInformation((ZWave.Enums.DeviceOptions)deviceOptions, generic, specific, cmdClasses);
            device.Network.SetApplicationNodeInformation((ZWave.Enums.DeviceOptions)deviceOptions, generic, specific, cmdClasses);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void ApplicationUserInput(Alias deviceNumber, byte[] userInputId, byte[] userInputLocal)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.ApplicationUserInput(userInputId, userInputLocal);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void ApplicationGetUserInput(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.ApplicationGetUserInput();
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void ApplicationVirtualDeviceNodeInformation(Alias bridgeNumber, bool isListening, byte generic, byte specific, params byte[] cmdClasses)
        {
            BridgeController bridge = (BridgeController)DeviceManager.GetDevice(bridgeNumber);
            ActionResult res = bridge.ApplicationVirtualDeviceNodeInformation(isListening, generic, specific, cmdClasses);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void SetExtIntLevel(Alias deviceNumber, byte intSrc, bool triggerLevel)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.SetExtIntLevel(intSrc, triggerLevel);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void SetRoutingInfo(Alias controllerNumber, Alias nodeNumber, byte[] nodeMask)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device node = DeviceManager.GetDevice(nodeNumber);
            ActionResult res = controller.SetRoutingInfo(node.Network.NodeTag, nodeMask);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void SetRoutingInfo(Alias controllerNumber, Node node, byte[] nodeMask)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            ActionResult res = controller.SetRoutingInfo(node, nodeMask);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void SetRoutingMAX(Alias deviceNumber, byte maxRouteTries)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.SetRoutingMAX(maxRouteTries);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void SetSleepMode(Alias deviceNumber, SleepModes mode)
        {
            return SetSleepMode(deviceNumber, mode, 0);
        }

        public BasicResult.Void SetSleepMode(Alias deviceNumber, SleepModes mode, byte intEnable)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.SetSleepMode((ZWave.BasicApplication.Enums.SleepModes)mode, intEnable);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void SetWutTimeout(Alias deviceNumber, byte timeoutSec)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.SetWutTimeout(timeoutSec);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void PowerMgmtStayAwake(Alias deviceNumber, byte powerLockType, int powerTimeout)
        {
            return PowerMgmtStayAwake(deviceNumber, powerLockType, powerTimeout, 0);
        }

        public BasicResult.Void PowerMgmtStayAwake(Alias deviceNumber, byte powerLockType, int awakeTimeoutMs, int wakeupTimeoutMs)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.PowerMgmtStayAwake(powerLockType, awakeTimeoutMs, wakeupTimeoutMs);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void PowerMgmtCancel(Alias deviceNumber, byte powerLockType)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.PowerMgmtCancel(powerLockType);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void SetMaxInclusionRequestInterval(Alias deviceNumber, byte requestintervalSec)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.SetMaxInclusionRequestIntervals(requestintervalSec);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void StoreHomeId(Alias installerNumber, byte[] homeId, byte nodeId)
        {
            InstallerController installer = (InstallerController)DeviceManager.GetDevice(installerNumber);
            ActionResult res = installer.StoreHomeId(homeId, nodeId);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void StoreNodeInfo(Alias installerNumber, byte nodeId, byte[] nodeInfo)
        {
            InstallerController installer = (InstallerController)DeviceManager.GetDevice(installerNumber);
            ActionResult res = installer.StoreNodeInfo(nodeId, nodeInfo);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.TypeLibrary TypeLibrary(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            TypeLibraryResult res = device.GetTypeLibrary();
            return new BasicResult.TypeLibrary(res);
        }

        public BasicResult.Void WatchDogDisable(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.WatchDogDisable();
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void WatchDogEnable(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.WatchDogEnable();
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void WatchDogKick(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.WatchDogKick();
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void WatchDogStart(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.WatchDogStart();
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void WatchDogStop(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.WatchDogStop();
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void SerialApiTest(Alias deviceNumber, byte testCmd, ushort testDelay, byte testPayloadLength, ushort testCount, TransmitOptions txOptions, bool isStopOnErrors, params Alias[] nodeNumbers)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            List<NodeTag> nodeIds = new List<NodeTag>();
            foreach (var item in nodeNumbers)
            {
                Device node = DeviceManager.GetDevice(item);
                nodeIds.Add(node.Network.NodeTag);
            }
            SerialApiTestResult res = device.SerialApiTest(null, testCmd, testDelay, testPayloadLength, testCount, (ZWave.Enums.TransmitOptions)txOptions, nodeIds.ToArray(), isStopOnErrors);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.ReturnValue SetRFReceiveMode(Alias deviceNumber, bool mode)
        {
            //TODO: remvoe this workaround after software fix released.
            if (!mode)
            {
                Thread.Sleep(2000); // SWPROT-6181
            }
            Device device = DeviceManager.GetDevice(deviceNumber);
            var nif = device.Network.GetNodeInfo();
            var cmdClasses = device.Network.GetCommandClasses();
            if (!mode)
            {
                if (!nif.IsEmpty && (nif.Security & 0x20) != 0 || (nif.Security & 0x40) != 0)
                {
                    device.ApplicationNodeInformation(ZWave.Enums.DeviceOptions.Listening, nif.Generic, nif.Specific, cmdClasses);
                    SoftReset(deviceNumber);
                }
            }
            var res = device.SetRFReceiveMode((byte)(mode ? 0x01 : 0x00));
            if (mode)
            {
                if ((nif.Security & 0x20) != 0 || (nif.Security & 0x40) != 0)
                {
                    device.ApplicationNodeInformation(nif.DeviceOptions, nif.Generic, nif.Specific, cmdClasses);
                    SoftReset(deviceNumber);
                }
            }

            return new BasicActionResult.ReturnValue(res);
        }

        public BasicResult.Void SetMaxInclusionRequestIntervals(Alias deviceNumber, byte requestintervalSec)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.SetMaxInclusionRequestIntervals(requestintervalSec);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.AddNode ControllerChange(Alias controllerNumber, ControllerChangeModes mode)
        {
            Controller device = (Controller)DeviceManager.GetDevice(controllerNumber);
            DeviceManager.DisableSmartStart(controllerNumber);
            AddRemoveNodeResult res = device.ControllerChange((ZWave.Enums.ControllerChangeModes)mode, 10000);
            DeviceManager.EnableSmartStart(controllerNumber);
            return new BasicActionResult.AddNode(res);
        }

        public BasicResult.AddNodeWithLearnMode ControllerChange(Alias primaryControllerNumber, Alias newPrimaryControllerNumber)
        {
            return ControllerChange(primaryControllerNumber, newPrimaryControllerNumber, INCLUSION_TIMEOUT_MS);
        }

        public BasicResult.AddNodeWithLearnMode ControllerChange(Alias primaryControllerNumber, Alias newPrimaryControllerNumber, int timeoutMs)
        {
            Controller primaryController = (Controller)DeviceManager.GetDevice(primaryControllerNumber);
            Controller newPrimaryController = (Controller)DeviceManager.GetDevice(primaryControllerNumber);
            DeviceManager.DisableSmartStart(primaryControllerNumber);
            ActionToken addRemoveToken = primaryController.ControllerChange(ZWave.Enums.ControllerChangeModes.Start, timeoutMs, (x) => DeviceManager.EnableSmartStart(primaryControllerNumber));
            BasicResult.SetLearnMode learnRes = null;
            primaryController.WaitNodeStatusSignal(ZWave.BasicApplication.Enums.NodeStatuses.LearnReady, timeoutMs);
            learnRes = SetLearnMode(newPrimaryControllerNumber, LearnModes.LearnModeClassic, timeoutMs);
            AddRemoveNodeResult addRemoveResult = (AddRemoveNodeResult)addRemoveToken.WaitCompletedSignal();
            var ret = new BasicActionResult.AddNodeWithLearnMode(addRemoveResult, null);
            ret.SetLearnMode = learnRes;
            return ret;
        }

        public BasicResult.SetPriorityRoute SetPriorityRoute(Alias controllerNumber, Node node, Node repeater0, Node repeater1, Node repeater2, Node repeater3, byte routespeed)
        {
            Controller device = (Controller)DeviceManager.GetDevice(controllerNumber);
            SetPriorityRouteResult res = device.SetPriorityRoute(node, repeater0, repeater1, repeater2, repeater3, routespeed);
            return new BasicResult.SetPriorityRoute(res);
        }

        public BasicResult.SetPriorityRoute SetPriorityRoute(Alias controllerNumber, Alias nodeNumber, Node repeater0, Node repeater1, Node repeater2, Node repeater3, byte routespeed)
        {
            Controller device = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device node = DeviceManager.GetDevice(nodeNumber);
            SetPriorityRouteResult res = device.SetPriorityRoute(node.Network.NodeTag, repeater0, repeater1, repeater2, repeater3, routespeed);
            return new BasicResult.SetPriorityRoute(res);
        }

        public BasicResult.GetPriorityRoute GetPriorityRoute(Alias controllerNumber, Alias nodeNumber)
        {
            Controller device = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device node = DeviceManager.GetDevice(nodeNumber);
            GetPriorityRouteResult res = device.GetPriorityRoute(node.Network.NodeTag);
            return new BasicResult.GetPriorityRoute(res);
        }


        public BasicResult.NvmBackupRestoreOpen NvmBackupRestoreOpen(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            NvmBackupRestoreOpenResult res = device.NvmBackupRestoreOpen();
            return new BasicResult.NvmBackupRestoreOpen(res);
        }

        public BasicResult.Void NvmBackupRestoreClose(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.NvmBackupRestoreClose();
            return new BasicActionResult.Void(res);
        }

        public BasicResult.NvmBackupRestoreRead NvmBackupRestoreRead(Alias deviceNumber, byte length, int offset)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            NvmBackupRestoreReadResult res = device.NvmBackupRestoreRead(length, (ushort)offset);
            return new BasicResult.NvmBackupRestoreRead(res);
        }

        public BasicResult.NvmBackupRestoreWrite NvmBackupRestoreWrite(Alias deviceNumber, byte length, int offset, byte[] data)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            NvmBackupRestoreWriteResult res = device.NvmBackupRestoreWrite(length, offset, data);
            return new BasicResult.NvmBackupRestoreWrite(res);
        }


        public bool NVMBackupWriteSession(Alias controller, IList<byte> data, int offset = 0, byte packetSize = 64)
        {
            var res = false;
            if (NvmBackupRestoreOpen(controller))
            {
                List<byte[]> nvmData = Tools.ArraySplit(data.ToArray(), packetSize);
                for (int i = 0; i < nvmData.Count; i++)
                {
                    var nvmWriteRes = NvmBackupRestoreWrite(controller, packetSize, offset, nvmData[i]);
                    offset += packetSize;
                    if (nvmWriteRes.Status == NvmBackupRestoreStatuses.Error)
                    {
                        break;
                    }
                    else if (nvmWriteRes.Status == NvmBackupRestoreStatuses.EOF ||
                            nvmWriteRes.Status == NvmBackupRestoreStatuses.OK)
                    {
                        res = true;
                    }
                }
                NvmBackupRestoreClose(controller);

                SoftReset(controller);
            }
            return res;
        }

        public IList<byte> NVMBackupReadSession(Alias controller, int offset = 0, byte packetSize = 64, bool tillEOF = true)
        {
            var nvmReadData = new List<byte>();
            var openRes = NvmBackupRestoreOpen(controller);
            if (openRes)
            {
                Log($"NVM Size = {openRes.NvmSize}", LogLevels.Information); //Expceted for diagnostic openRes.NvmSize > offset + packetSize;
                bool done = false;
                while (!done)
                {
                    var readResult = NvmBackupRestoreRead(controller, packetSize, offset);
                    if (readResult && readResult.Data != null)
                    {
                        nvmReadData.AddRange(readResult.Data);
                        offset += packetSize;
                        if (!tillEOF ||
                            (tillEOF &&
                            readResult.Status == NvmBackupRestoreStatuses.EOF ||
                            readResult.Status == NvmBackupRestoreStatuses.Error))
                        {
                            done = true;
                        }
                    }
                    else
                    {
                        done = true;
                    }
                }
                NvmBackupRestoreClose(controller);
            }
            return nvmReadData;
        }


        public BasicResult.FirmwareUpdateNvmInit FirmwareUpdateNvmInit(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            FirmwareUpdateNvmInitResult res = device.FirmwareUpdateNvmInit();
            return new BasicActionResult.FirmwareUpdateNvmInit(res);
        }

        public BasicResult.FirmwareUpdateNvmSetNewImage FirmwareUpdateNvmSetNewImage(Alias deviceNumber, bool isSet)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            FirmwareUpdateNvmSetNewImageResult res = device.FirmwareUpdateNvmSetNewImage(isSet);
            return new BasicActionResult.FirmwareUpdateNvmSetNewImage(res);
        }

        public BasicResult.FirmwareUpdateNvmGetNewImage FirmwareUpdateNvmGetNewImage(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            FirmwareUpdateNvmGetNewImageResult res = device.FirmwareUpdateNvmGetNewImage();
            return new BasicActionResult.FirmwareUpdateNvmGetNewImage(res);
        }

        public BasicResult.FirmwareUpdateNvmUpdateCrc16 FirmwareUpdateNvmUpdateCrc16(Alias deviceNumber, int offset, ushort length, ushort seed)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            FirmwareUpdateNvmUpdateCrc16Result res = device.FirmwareUpdateNvmUpdateCrc16(offset, length, seed);
            return new BasicActionResult.FirmwareUpdateNvmUpdateCrc16(res);
        }

        public BasicResult.FirmwareUpdateNvmIsValidCrc16 FirmwareUpdateNvmIsValidCrc16(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            FirmwareUpdateNvmIsValidCrc16Result res = device.FirmwareUpdateNvmIsValidCrc16();
            return new BasicActionResult.FirmwareUpdateNvmIsValidCrc16(res);
        }

        public BasicResult.FirmwareUpdateNvmWrite FirmwareUpdateNvmWrite(Alias deviceNumber, ushort blockSize, string filePath)
        {
            if (File.Exists(filePath))
            {
                var fwFileExt = Path.GetExtension(filePath);
                bool isOTA = fwFileExt == ".ota" || fwFileExt == ".ota";
                var firmwareData = HexFileHelper.GetBytes(HexFileHelper.ReadIntelHexFile(filePath, 0xFF, out int startAddress), 0xFF);
                var firmwareChecksum = Tools.CalculateCrc16Array(firmwareData);
                "CRC16 initital:{0}, startAddress:{1}"._DLOG(firmwareChecksum.ToHexString(), startAddress);
                Device device = DeviceManager.GetDevice(deviceNumber);
                int offset = 0;
                bool isOk = true;
                while (offset < firmwareData.Length && isOk)
                {
                    int delta = firmwareData.Length - offset;
                    ushort length = (ushort)(delta > blockSize ? blockSize : delta);
                    byte[] data = firmwareData.Skip(offset).Take(length).ToArray();
                    if (data.Any(x => x != 0xFF))
                    {
                        FirmwareUpdateNvmWriteResult res = device.FirmwareUpdateNvmWrite(offset, length, data);
                        isOk = res && res.IsOk;
                    }
                    else
                    {
                        // need to write empty bytes to update valid CRC16
                        device.FirmwareUpdateNvmWrite(offset, length, data);
                    }
                    offset += blockSize;
                }
                return new BasicActionResult.FirmwareUpdateNvmWrite(isOk);
            }
            else
                return new BasicActionResult.FirmwareUpdateNvmWrite(false);
        }

        public BasicResult.FirmwareUpdateNvmWrite FirmwareUpdateNvmWrite(Alias deviceNumber, int offset, ushort length, byte[] buffer)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            FirmwareUpdateNvmWriteResult res = device.FirmwareUpdateNvmWrite(offset, length, buffer);
            return new BasicActionResult.FirmwareUpdateNvmWrite(res);
        }

        public BasicResult.FirmwareUpdateSendXModem FirmwareUpdateSendXModem(Alias deviceNumber, string fwFileName)
        {
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            var dataSource = deviceHostClient.SClient.DataSource;
            deviceHostClient.SClient.Disconnect();
            var appLayer = new XModemApplicationLayer(new SessionLayer(), new XModemFrameLayer(), new SerialPortTransportLayer());
            if (deviceHostClient.SClient.DataSource is SocketDataSource)
            {
                appLayer = new XModemApplicationLayer(new SessionLayer(), new XModemFrameLayer(), new TcpClientTransportLayer());
            }
            bool ret = false;
            var initFrame = new byte[] { 0x01, 0x03, 0x00, 0x27, 0xdb };
            using (var xModemDevice = appLayer.CreateXModem())
            {
                if (xModemDevice.Connect(dataSource) == CommunicationStatuses.Done)
                {
                    xModemDevice.TransportClient.WriteData(initFrame);
                    if (xModemDevice.WaitUpdateSessionReady(5000))
                    {
                        var hexData = File.ReadAllBytes(fwFileName);
                        List<byte[]> firmwareData = Tools.ArraySplit(hexData, ZWave.BasicApplication.XModemDataFrame.PayloadSize);
                        foreach (var packetData in firmwareData)
                        {
                            if (!xModemDevice.Send(packetData))
                            {
                                break;
                            }
                        }
                        ret = xModemDevice.ConfirmUpdate(5000);
                        if (ret)
                        {
                            Task.Delay(1000).Wait(); // Wait for Serial API app run.
                        }
                        else
                        {
                            //LogError(xModemDevice.ErrorMessage);
                        }
                    }
                }

            }
            deviceHostClient.SClient.Connect();
            if (deviceHostClient.SClient is Device)
            {
                if (!DeviceManager.IsOptionDisabled(ZWaveSet.NodeIdBase2, deviceNumber))
                {
                    ((Device)deviceHostClient.SClient).SerialApiSetNodeIdBaseType(2);
                }
            }
            return new BasicActionResult.FirmwareUpdateSendXModem(ret);
        }

        public BasicResult.GetVersion GetVersion(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            VersionResult res = device.GetVersion();
            return new BasicResult.GetVersion(res);
        }

        public BasicResult.GetSecurityKeys GetSecurityKeys(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            GetSecurityKeysResult res = device.GetSecurityKeys();
            return new BasicResult.GetSecurityKeys(res);
        }

        public BasicResult.GetSecurityS2PublicDSK GetSecurityS2PublicDSK(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            var res = device.GetSecurityS2PublicDSK();
            return new BasicResult.GetSecurityS2PublicDSK(res);
        }

        public BasicResult.ReturnValue SetSecurityInclusionRequestedKeys(Alias deviceNumber, SecuritySchemes[] securitySchemes)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ReturnValueResult res = device.SetSecurityInclusionRequestedKeys((securitySchemes ?? new SecuritySchemes[0])
                .Select(x => (ZWave.Enums.SecuritySchemes)x).ToArray());
            return new BasicActionResult.ReturnValue(res);
        }

        public BasicResult.ReturnValue SetSecurityInclusionRequestedAuthentication(Alias deviceNumber, SecurityAuthentications securityAuthentication)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ReturnValueResult res = device.SetSecurityInclusionRequestedAuthentication((ZWave.Enums.SecurityAuthentications)securityAuthentication);
            return new BasicActionResult.ReturnValue(res);
        }

        public BasicResult.ReturnValue SetListenBeforeTalkThreshold(Alias deviceNumber, byte channel, byte threshold)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ReturnValueResult res = device.SetListenBeforeTalkThreshold(channel, threshold);
            return new BasicActionResult.ReturnValue(res);
        }

        public void BackUpNVM(Alias deviceNumber, string backUpPath)
        {
            var dhc = DeviceManager.GetDeviceHostClient(deviceNumber);

            DeviceManager.BackUpSample(dhc, backUpPath);
            Thread.Sleep(1000);
            var device = DeviceManager.GetDevice(deviceNumber);
            if (device != null)
            {
                device.GetVersion();
            }
        }

        public bool UserActionRequired(string ActionDescription, int timeoutMilliseconds)
        {
            ReportService.AddLog($"User action reqiured: {ActionDescription}", LogLevels.Text);
            var ret = false;
            var cts = new CancellationTokenSource(timeoutMilliseconds);
            var userActionPipeClientStream = new NamedPipeClientStream(".", "UAR_A3B0F183-AFF3-420B-956C-424742A6F143", PipeDirection.InOut);
            try
            {
                userActionPipeClientStream.Connect(timeoutMilliseconds);
                var writeBytes = new List<byte>(Encoding.UTF8.GetBytes(ActionDescription));
                writeBytes.InsertRange(0, new byte[] { 0x75, 0x72 }); // ur
                userActionPipeClientStream.Write(writeBytes.ToArray(), 0, writeBytes.Count);
                userActionPipeClientStream.Flush();
                while (!cts.Token.IsCancellationRequested)
                {
                    var buffer = new byte[1024];
                    int readBytesLength = userActionPipeClientStream.Read(buffer, 0, buffer.Length);
                    if (readBytesLength >= 2)
                    {
                        if (buffer.StartsWithArray(new byte[] { 0x75, 0x61 })) // ua
                        {
                            ReportService.AddLog("User action applied", LogLevels.Ok);
                            ret = true;
                            break;
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (IOException)
            {
            }
            catch (TimeoutException)
            {
            }
            finally
            {
                userActionPipeClientStream.Dispose();
                cts.Dispose();
            }
            return ret;
        }

        public bool UserActionRequired(string ActionDescription)
        {

            Console.WriteLine($"in_req<<:{ActionDescription}");
            const string respApplied = "in_appl>>";
            bool ret = false;
            var response = Console.ReadLine();
            if (respApplied == response)
            {
                ReportService.AddLog("User action applied", LogLevels.Ok);
                ret = true;
            }
            else
            {
                ReportService.AddLog(response, LogLevels.Warning);
            }
            return ret;
        }

        public BasicResult.Void WriteZW(Alias deviceNumber, string strData)
        {
            var data = strData.GetBytes();
            return WriteZW(deviceNumber, data);
        }

        public BasicResult.Void WriteZW(Alias deviceNumber, byte[] data)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.WriteZW(data);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.ExpectZW RequestZW(Alias deviceNumber, string strData, string filter, int timeoutMs)
        {
            var data = strData.GetBytes();
            return RequestZW(deviceNumber, data, filter, timeoutMs);
        }

        public BasicResult.ExpectZW RequestZW(Alias deviceNumber, byte[] data, string filter, int timeoutMs)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            var tokens = filter.Split(' ');
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
            var res = device.RequestZW(data, mask.ToArray(), timeoutMs);
            return new BasicActionResult.ExpectZW(res);
        }

        public BasicResult.MaxPayloadSize GetMaxPayloadSize(Alias controllerNumber)
        {
            Device device = DeviceManager.GetDevice(controllerNumber);
            var res = device.GetMaxPayloadSize();
            return new BasicActionResult.MaxPayloadSize(res); ;
        }

        public BasicResult.MaxPayloadSize GetMaxLRPayloadSize(Alias controllerNumber)
        {
            Device device = DeviceManager.GetDevice(controllerNumber);
            var res = device.GetMaxLRPayloadSize();
            return new BasicActionResult.MaxPayloadSize(res); ;
        }

        public void SetLedState(Alias deviceNumber, byte ledMask)
        {
            var device = DeviceManager.GetDevice(deviceNumber);

            device.WriteZW("00 E5 01".GetBytes()); Thread.Sleep(100);
            device.WriteZW("00 E6 05 05 04".GetBytes()); Thread.Sleep(100);
            device.WriteZW("00 E6 01 05 04 00".GetBytes()); Thread.Sleep(100);
            device.WriteZW("00 E6 05 05 05".GetBytes()); Thread.Sleep(100);
            device.WriteZW("00 E6 01 05 05 00".GetBytes()); Thread.Sleep(100);
            device.WriteZW("00 E6 05 00 05".GetBytes()); Thread.Sleep(100);
            device.WriteZW("00 E6 01 00 05 00".GetBytes()); Thread.Sleep(100);
            device.WriteZW("00 E6 05 03 0D".GetBytes()); Thread.Sleep(100);
            device.WriteZW("00 E6 01 03 0D 00".GetBytes()); Thread.Sleep(100);
            device.WriteZW("00 E6 05 03 0E".GetBytes()); Thread.Sleep(100);
            device.WriteZW("00 E6 01 03 0E 00".GetBytes()); Thread.Sleep(100);
            device.WriteZW("00 E6 05 03 0F".GetBytes()); Thread.Sleep(100);
            device.WriteZW("00 E6 01 03 0F 00".GetBytes()); Thread.Sleep(100);
            var bits = (BitArray8)ledMask;
            if (bits[0])
            {
                device.WriteZW("00 E6 01 05 04 01".GetBytes()); Thread.Sleep(100);
            }
            if (bits[1])
            {
                device.WriteZW("00 E6 01 05 05 01".GetBytes()); Thread.Sleep(100);
            }
            if (bits[2])
            {
                device.WriteZW("00 E6 01 00 05 01".GetBytes()); Thread.Sleep(100);
            }
            if (bits[3])
            {
                device.WriteZW("00 E6 01 03 0D 01".GetBytes()); Thread.Sleep(100);
            }
            if (bits[4])
            {
                device.WriteZW("00 E6 01 03 0E 01".GetBytes()); Thread.Sleep(100);
            }
            if (bits[5])
            {
                device.WriteZW("00 E6 01 03 0F 01".GetBytes()); Thread.Sleep(100);
            }
        }

        public BasicResult.ClearNetworkStats ClearNetworkStats(Alias deviceNumber)
        {
            var device = DeviceManager.GetDevice(deviceNumber);
            var res = device.ClearNetworkStats();
            return new BasicActionResult.ClearNetworkStats(res);
        }

        public BasicResult.GetNetworkStats GetNetworkStats(Alias deviceNumber)
        {
            var device = DeviceManager.GetDevice(deviceNumber);
            var res = device.GetNetworkStats();
            return new BasicActionResult.GetNetworkStats(res);
        }

        public BasicResult.GetTxTimer GetTxTimer(Alias deviceNumber)
        {
            var device = DeviceManager.GetDevice(deviceNumber);
            var res = device.GetTxTimer();
            return new BasicActionResult.GetTxTimer(res);
        }

        public BasicResult.Void ClearTxTimers(Alias deviceNumber)
        {
            var device = DeviceManager.GetDevice(deviceNumber);
            device.ClearTxTimers();
            return new BasicResult.Void();
        }

        public BasicResult.Void SendDataForced(Alias nodeNumber, ChannelConfigs chConfig, TransmitRfOptions txRfOptions, byte[] homeId,
            byte srcNodeId, byte headerType, bool speedModified, bool ack, byte seqNo, byte[] destination, byte[] data)
        {
            var device = DeviceManager.GetDevice(nodeNumber);
            List<byte> buffer = new List<byte>();
            buffer.Add((byte)FrameTypes.Request);
            buffer.Add(0xF0);
            buffer.Add(0x01);
            buffer.Add(0x00);
            buffer.Add((byte)txRfOptions);
            if (chConfig == ChannelConfigs.ChLR || txRfOptions == TransmitRfOptions.RfOptionSpeedLR100k)
            {
                var len = 9 + destination.Length + data.Length + 4;
                buffer.Add((byte)len);
                buffer.AddRange(homeId);
                var srcNodeId1 = (byte)(srcNodeId >> 4);
                var srcNodeId2 = (byte)(srcNodeId & 0x0F);
                var dst1 = destination != null && destination.Length > 0 ? destination[0] : (byte)0;
                var dst2 = destination != null && destination.Length > 1 ? destination[1] : (byte)0;
                var semiByte = (byte)((srcNodeId2 << 4) | (dst1 & 0x0F));
                //buffer.Add(srcNodeId);
                buffer.Add(srcNodeId1);
                buffer.Add(semiByte);
                buffer.Add(dst2);
                buffer.Add((byte)len); //Att!
                buffer.Add((byte)((headerType & 0x0F) + (speedModified ? (1 << 4) : 0) + (ack ? (1 << 7) : 0)));
                buffer.Add(seqNo);
                buffer.Add(0x00); //noise floor
                buffer.Add(0x00); //ext1
            }
            else if (chConfig == ChannelConfigs.Ch3)
            {
                var len = 9 + destination.Length + data.Length;
                buffer.Add((byte)len);
                buffer.AddRange(homeId);
                buffer.Add(srcNodeId);
                buffer.Add((byte)((headerType & 0x0F) + (speedModified ? (1 << 4) : 0) + (ack ? (1 << 7) : 0)));
                buffer.Add(0x00);
                len += txRfOptions > TransmitRfOptions.RfOptionSpeed40k ? 2 : 1;
                buffer.Add((byte)len);
                buffer.Add(seqNo);
                buffer.AddRange(destination);
            }
            else
            {
                var len = 8 + destination.Length + data.Length;
                buffer.Add((byte)len);
                buffer.AddRange(homeId);
                buffer.Add(srcNodeId);
                buffer.Add((byte)((headerType & 0x0F) + (speedModified ? (1 << 4) : 0) + (ack ? (1 << 6) : 0)));
                buffer.Add((byte)(seqNo & 0x0F));
                len += txRfOptions > TransmitRfOptions.RfOptionSpeed40k ? 2 : 1;
                buffer.Add((byte)len);
                buffer.AddRange(destination);
            }
            buffer.AddRange(data);
            ActionResult res = device.RequestZW(buffer.ToArray(), new[] { new ByteIndex(0x01), new ByteIndex(0xF0), }, 1000);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void SendDataForced(Alias nodeNumber, ChannelConfigs chConfig, TransmitRfOptions txRfOptions, byte[] homeId,
           byte srcNodeId, byte headerType, bool speedModified, bool ack, byte seqNo, byte destination, byte[] data)
        {
            var device = DeviceManager.GetDevice(nodeNumber);
            List<byte> buffer = new List<byte>();
            buffer.Add((byte)FrameTypes.Request);
            buffer.Add(0xF0);
            buffer.Add(0x01);
            buffer.Add(0x00);
            buffer.Add((byte)txRfOptions);
            if (chConfig == ChannelConfigs.ChLR)
            {
                throw new NotSupportedException("Long Range channel configuration not supported");
            }
            else if (chConfig == ChannelConfigs.Ch3)
            {
                var len = 10 + data.Length;
                buffer.Add((byte)len);
                buffer.AddRange(homeId);
                buffer.Add(srcNodeId);
                buffer.Add((byte)((headerType & 0x0F) + (speedModified ? (1 << 4) : 0) + (ack ? (1 << 7) : 0)));
                buffer.Add(0x00);
                len += txRfOptions > TransmitRfOptions.RfOptionSpeed40k ? 2 : 1;
                buffer.Add((byte)len);
                buffer.Add(seqNo);
            }
            else
            {
                var len = 9 + data.Length;
                buffer.Add((byte)len);
                buffer.AddRange(homeId);
                buffer.Add(srcNodeId);
                buffer.Add((byte)((headerType & 0x0F) + (speedModified ? (1 << 4) : 0) + (ack ? (1 << 6) : 0)));
                buffer.Add((byte)(seqNo & 0x0F));
                len += txRfOptions > TransmitRfOptions.RfOptionSpeed40k ? 2 : 1;
                buffer.Add((byte)len);
            }
            buffer.Add(destination);
            buffer.AddRange(data);
            ActionResult res = device.RequestZW(buffer.ToArray(), new[] { new ByteIndex(0x01), new ByteIndex(0xF0), }, 1000);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void SendDataForced(Alias nodeNumber, TransmitRfOptions txRfOptions,
            byte[] homeId, byte srcNodeId, byte properties1, byte properties2, byte[] destination, byte[] data)
        {
            var device = DeviceManager.GetDevice(nodeNumber);
            var len = 8 + destination.Length + data.Length;
            List<byte> buffer = new List<byte>();
            buffer.Add((byte)FrameTypes.Request);
            buffer.Add(0xF0);
            buffer.Add(0x01);
            buffer.Add(0x00);
            buffer.Add((byte)txRfOptions);
            buffer.Add((byte)len);
            buffer.AddRange(homeId);
            buffer.Add(srcNodeId);
            buffer.Add(properties1);
            buffer.Add(properties2);
            len += txRfOptions > TransmitRfOptions.RfOptionSpeed40k ? 2 : 1;
            buffer.Add((byte)len);
            buffer.AddRange(destination);
            buffer.AddRange(data);
            ActionResult res = device.RequestZW(buffer.ToArray(), new[] { new ByteIndex(0x01), new ByteIndex(0xF0), }, 1000);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void DeleteFirstItemFromCommandQueue(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void ClearCommandQueue(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.ReturnValue InitiateShutdown(Alias controllerNumber)
        {
            var device = DeviceManager.GetDevice(controllerNumber);
            return new BasicActionResult.ReturnValue(device.InitiateShutdown());
        }

        public BasicResult.Void SetRfRegion(Alias deviceNumber, RfRegions rfRegion)
        {
            var device = DeviceManager.GetDevice(deviceNumber);
            return new BasicActionResult.Void(device.SetRfRegion(rfRegion.ToRfRegion()));
        }

        public BasicResult.GetRfRegion GetRfRegion(Alias deviceNumber)
        {
            var device = DeviceManager.GetDevice(deviceNumber);
            return new BasicActionResult.GetRfRegion(device.GetRfRegion());
        }

        public BasicResult.Void SetSecurityBypass(Alias deviceNumber, bool isBypass)
        {
            var device = DeviceManager.GetDevice(deviceNumber);
            var token = device.SessionClient.ExecuteAsync(new SetSecurityBypassOperation((byte)(isBypass ? 0x01 : 0x00)));
            return new BasicActionResult.Void(token.WaitCompletedSignal());
        }

        public BasicResult.DefaultTxPowerLevelGet GetDefaultTxPowerLevel(Alias deviceNumber)
        {
            var device = DeviceManager.GetDevice(deviceNumber);
            var res = device.GetDefaultTxPowerLevel();
            return new BasicResult.DefaultTxPowerLevelGet(res);
        }

        public BasicResult.DefaultTxPowerLevelGet GetDefaultTxPowerLevel16Bit(Alias deviceNumber)
        {
            var device = DeviceManager.GetDevice(deviceNumber);
            var res = device.GetDefaultTxPowerLevel16Bit();
            return new BasicResult.DefaultTxPowerLevelGet(res);
        }

        public BasicResult.Void SetDefaultTxPowerLevel(Alias deviceNumber, sbyte normalTxPower, sbyte measured0dBmPower)
        {
            var device = DeviceManager.GetDevice(deviceNumber);
            var res = device.SetDefaultTxPowerLevel(normalTxPower, measured0dBmPower);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void SetDefaultTxPowerLevel16Bit(Alias deviceNumber, short normalTxPower, short measured0dBmPower)
        {
            var device = DeviceManager.GetDevice(deviceNumber);
            var res = device.SetDefaultTxPowerLevel16Bit(normalTxPower, measured0dBmPower);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.GetLRChannel GetLRChannel(Alias deviceNumber)
        {
            var device = DeviceManager.GetDevice(deviceNumber);
            var res = device.GetLRChannel();
            return new BasicResult.GetLRChannel(res);
        }

        public BasicResult.Void SetLRChannel(Alias deviceNumber, LongRangeChannels channel)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.SetLRChannel((ZWave.BasicApplication.Enums.LongRangeChannels)channel);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.GetDcdcMode GetDcdcMode(Alias deviceNumber)
        {
            var device = DeviceManager.GetDevice(deviceNumber);
            var res = device.GetDcdcMode();
            return new BasicResult.GetDcdcMode(res);
        }

        public BasicResult.Void SetDcdcMode(Alias deviceNumber, DcdcModes dcdcMode)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.SetDcdcMode((ZWave.BasicApplication.Enums.DcdcModes)dcdcMode);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.GetBackgroundRssi GetBackgroundRSSI(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            var res = device.GetBackgroundRSSI();
            return new BasicActionResult.GetBackgroundRssi(res); ;
        }

        public BasicResult.GetRadioPTI GetRadioPTI(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            var res = device.IsRadioPTI();
            return new BasicResult.GetRadioPTI(res);
        }

        public BasicResult.Void EnableRadioPTI(Alias deviceNumber, bool isEnabled)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionResult res = device.EnableRadioPTI(isEnabled);
            SoftReset(deviceNumber);

            var dhc = DeviceManager.GetDeviceHostClient(deviceNumber);
            if (dhc != null)
            {
                dhc.ZClient?.Dispose();
                dhc.ZClient = DeviceManager.SnifferPtiApplicationLayer.CreateSnifferPti();
                dhc.ZClient.Connect(new SocketDataSource(dhc.SClient.TransportClient.DataSource.SourceName, 4905));
            }
            else
            {
                //Fail?
            }
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Void SetSupervisionDelay(Alias deviceNumber, SupervisionReportStatuses status, int delay)
        {
            var device = DeviceManager.GetDevice(deviceNumber);
            device.Network.SupervisionReportStatusResponse = (ZWave.Enums.SupervisionReportStatuses)status;
            device.Network.SupervisionDuration = delay;
            if (delay > 0)
            {
                device.Network.SupervisionMoreStatusUpdates = 1;
            }
            return new BasicResult.Void();
        }

        public BasicResult.Void SetMaxLrTxPower(Alias controllerNumber, MaxLrTxPower value)
        {
            var device = DeviceManager.GetDevice(controllerNumber);
            var res = device.SetMaxLrTxPower((short)value);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.GetMaxLrTxPower GetMaxLrTxPower(Alias controllerNumber)
        {
            var device = DeviceManager.GetDevice(controllerNumber);
            var res = device.GetMaxLrTxPower();
            return new BasicResult.GetMaxLrTxPower(res);
        }
    }
}

