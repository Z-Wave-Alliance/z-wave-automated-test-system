// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Collections.Generic;
using System.Linq;
using Utils.UI.Enums;
using Zats.Services.Constants;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using ZWave.BasicApplication.Tasks;
using ZWave.TextApplication;

namespace Zats.Services
{
    public class TestInterfaceService : ServiceBase, ITestInterfaceService
    {
        private const int INCLUSION_TIMEOUT_MS = 65000;
        private const int INCLUSION_NODE_FOUND_TIMEOUT_MS = 15000;
        private const int INCLUSION_ADDING_REMOVING_TIMEOUT_MS = 5000;
        private const int INCLUSION_LEARN_READY_TIMEOUT_MS = 15000;
        private const int INCLUSION_MAX_TIMOUT_MS = 250000;

        public HMIActions HMIActions { get; }
        private IEmulatedDeviceManager EmulatedDeviceManager { get; set; }
        private DeviceManager DeviceManager { get; set; }
        private DebugDeviceManager DebugDeviceManager { get; set; }

        public TestInterfaceService(IEmulatedDeviceManager emulatedDeviceManager,
            DeviceManager deviceManager, DebugDeviceManager debugDeviceManager)
        {
            HMIActions = new HMIActions(deviceManager);
            EmulatedDeviceManager = emulatedDeviceManager;
            DeviceManager = deviceManager;
            DebugDeviceManager = debugDeviceManager;
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

        public void SetLearnMode(Alias deviceNumber)
        {
            HMIActions.Sample_LearnMode(deviceNumber);
        }

        public void ButtonSinglePress_UserInput(Alias deviceNumber, byte ButtonNumber)
        {
            TestInterfaceDevice device = DeviceManager.GetTIDevice(deviceNumber);
            device.TestInterfaceSendData(TestInterfaceConstants.ButtonActionTemplates.ToggleButton(ButtonNumber), TestInterfaceConstants.ButtonOperations.DefaultTimeoutMs);
        }

        //public BasicResult.ReturnValue GetLedState_UserAction(Alias deviceNumber)
        //{
        //    Device device = DeviceManager.GetTIDevice(deviceNumber);
        //    var ret = device.TestInterfaceSendData2(new byte[] { (byte)ProgrammerCommandTypes.GetLedState }, 1000);
        //    if (ret && ret.ByteArray != null && ret.ByteArray.Length > 0)
        //    {
        //        ret.ByteArray[0] = (byte)~ret.ByteArray[0];
        //        if (ledsMask != null)
        //        {
        //            ret.ByteArray[0] = (byte)(ret.ByteArray[0] & ledsMask.Value);
        //        }
        //        return ret.ByteValue == ledsMask;
        //    }
        //    return new BasicActionResult.ReturnValue(ret);
        //}

        public LibTypes GetLibrary(Alias deviceNumber)
        {
            TestInterfaceDevice device = DeviceManager.GetTIDevice(deviceNumber);
            return (LibTypes)device.Library;
        }

        public byte[] GetDSK(Alias deviceNumber, int NumBytes)
        {
            byte[] Dsk = new byte[NumBytes];
            Dsk = DeviceManager.GetDSK(deviceNumber, NumBytes);
            return Dsk;
        }

        public BasicResult.AddNode AddNode(Alias controllerNumber, Alias testInterfaceNumber, Alias deviceDebugNumber = new Alias())
        {
            if (DeviceManager.IncludeSettings.JoinDSK == null)
            {
                DeviceManager.IncludeSettings.JoinDSK = GetDSK(testInterfaceNumber, 2);
            }
            if (DeviceManager.IncludeSettings.JoinCSA == null)
            {
                DeviceManager.IncludeSettings.JoinCSA = DeviceManager.GetCSA(controllerNumber, 4);
            }

            DeviceManager.DisableSmartStart(controllerNumber);
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            TestInterfaceDevice device = DeviceManager.GetTIDevice(testInterfaceNumber);
            TextDevice deviceDebugPort = DebugDeviceManager.GetDevice(deviceDebugNumber);
            DeviceManager.ReportTestFramesAndExtensionOverrides(controllerNumber);
            var addToken = controller.AddNodeToNetwork((ZWave.Enums.Modes)Modes.NodeAny, INCLUSION_MAX_TIMOUT_MS, null);
            controller.WaitNodeStatusSignal(ZWave.BasicApplication.Enums.NodeStatuses.LearnReady, INCLUSION_LEARN_READY_TIMEOUT_MS);

            int attempts = 6;
            var IsNodeFoundReceived = false;
            while (attempts > 0 && !IsNodeFoundReceived)
            {
                attempts--;
                SetLearnMode(testInterfaceNumber);
                IsNodeFoundReceived = controller.WaitNodeStatusSignal(ZWave.BasicApplication.Enums.NodeStatuses.NodeFound, INCLUSION_NODE_FOUND_TIMEOUT_MS);
            }
            if (deviceDebugPort != null)
            {
                const int KEXREPORT_TIMEOUT = 21000;
                const int PUBLICKEY_TIMEOUT = INCLUSION_MAX_TIMOUT_MS;
                const int INPUT_TIMEOUT = 10000;
                const byte CSA_REQUESTED = 0x01;
                var etRes = controller.ExpectData(new COMMAND_CLASS_SECURITY_2.KEX_REPORT(), KEXREPORT_TIMEOUT);
                if (etRes)
                {
                    COMMAND_CLASS_SECURITY_2.KEX_REPORT kexRep = etRes.Command;
                    if (kexRep.properties1.requestCsa == CSA_REQUESTED)
                    {
                        etRes = controller.ExpectData(new COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT(), PUBLICKEY_TIMEOUT);
                        var d_channel = D_CH.a;
                        var d_data = D_CMD.Csa;
                        int part1 = DeviceManager.IncludeSettings.JoinCSA.Take(2).ToArray().ToNumber();
                        int part2 = DeviceManager.IncludeSettings.JoinCSA.Skip(2).ToArray().ToNumber();
                        var csa = string.Format("{0}{1}", part1.ToString("00000"), part2.ToString("00000"));
                        var csaData = string.Format("{0} {1} {2}\r", d_channel.Channel, d_data.Command, csa);
                        //Wait Request from debug port:
                        var recievedResult = deviceDebugPort.ReceiveData(INPUT_TIMEOUT);
                        // Answer with CSA
                        if (recievedResult)
                        {
                            var requestResult = deviceDebugPort.Request(csaData, INPUT_TIMEOUT);
                        }
                    }
                }
            }

            var addRes = (AddRemoveNodeResult)addToken.WaitCompletedSignal();
            if (addRes.SubstituteStatus != ZWave.Enums.SubstituteStatuses.None)
            {
                DeviceManager.LogNetworkKeys(controllerNumber, addRes.Node);
            }
            DeviceManager.EnableSmartStart(controllerNumber);
            DeviceManager.IncludeSettings.Clear();
            return new BasicActionResult.AddNode(addRes);
        }

        public BasicResult.IncludeNode IncludeNode(Alias controllerNumber, Alias testInterfaceNumber, Alias deviceDebugNumber = new Alias())
        {
            BasicResult.IncludeNode ret = null;
            var controller = DeviceManager.GetDevice(controllerNumber);
            controller.Network.InclusionControllerInitiateRequestTimeoutMs = DeviceManager.IncludeSettings.InclusionControllerInitiateRequestTimeoutMs;
            if (!DeviceManager.IsOptionDisabled(ZWaveSet.SmartStart, controllerNumber) &&
                controller.Network.IsNodeIdBaseTypeLR &&
                DeviceManager.GetDeviceHostClient(controllerNumber).IsLongRange &&
                DeviceManager.GetDeviceHostClient(testInterfaceNumber).IsLongRange)
            {
                ret = IncludeNodeSmartStartLR(controllerNumber, testInterfaceNumber);
            }
            else
            {
                ret = IncludeNodeClassic(controllerNumber, testInterfaceNumber, deviceDebugNumber);
            }
            controller.Network.InclusionControllerInitiateRequestTimeoutMs = null;
            return ret;
        }

        private BasicResult.IncludeNode IncludeNodeSmartStartLR(Alias controllerNumber, Alias testInterfaceNumber)
        {
            DeviceManager.DisableSmartStart(controllerNumber);
            DeviceManager.CancelSmartStartSupport(controllerNumber);
            DeviceManager.EnableSmartStart(controllerNumber);
            DeviceManager.RunSmartStartSupport(controllerNumber);

            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            controller.WaitNodeStatusSignal(ZWave.BasicApplication.Enums.NodeStatuses.LearnReady, 2000);

            var timeoutMs = DeviceManager.IncludeSettings.TimeoutMs ?? 300000;
            var schemes = DeviceManager.IncludeSettings.GrantedSchemes != null && DeviceManager.IncludeSettings.GrantedSchemes.Length != 0 ?
                DeviceManager.IncludeSettings.GrantedSchemes :
                SecuritySchemeSet.ALL;
            var modes = DeviceManager.IncludeSettings.SkipSmartStartLR ?
                Modes.NodeOptionNetworkWide :
                Modes.NodeOptionLongRange | Modes.NodeOptionNetworkWide;
            var Provisioning = new ProvisioningService(DeviceManager);
            Provisioning.Add(controllerNumber, testInterfaceNumber, schemes, modes);
            var addToken = new BasicActionToken.ExpectSmartStartAdd(DeviceManager.GetSmartStartToken(timeoutMs));
            SoftReset(testInterfaceNumber);
            //var deviceHostClient = DeviceManager.GetDeviceHostClient(testInterfaceNumber);
            //deviceHostClient.SClient.Disconnect();
            //deviceHostClient.SClient.Connect();
            var addNodeRes = (InclusionResult)addToken.Token.WaitCompletedSignal();
            var addRes = new BasicActionResult.IncludeNode(addNodeRes);
            DeviceManager.IncludeSettings.Clear();
            Provisioning.Remove(controllerNumber, testInterfaceNumber);
            return addRes;
        }

        private BasicResult.IncludeNode IncludeNodeClassic(Alias controllerNumber, Alias testInterfaceNumber, Alias deviceDebugNumber)
        {
            if (DeviceManager.IncludeSettings.JoinDSK == null)
            {
                DeviceManager.IncludeSettings.JoinDSK = GetDSK(testInterfaceNumber, 2);
            }
            if (DeviceManager.IncludeSettings.JoinCSA == null)
            {
                DeviceManager.IncludeSettings.JoinCSA = DeviceManager.GetCSA(controllerNumber, 4);
            }
            var timeoutMs = DeviceManager.IncludeSettings.TimeoutMs ?? INCLUSION_MAX_TIMOUT_MS;
            var mode = (ZWave.Enums.Modes)DeviceManager.IncludeSettings.IncludeMode;

            DeviceManager.DisableSmartStart(controllerNumber);
            DeviceManager.ReportTestFramesAndExtensionOverrides(controllerNumber);

            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            TestInterfaceDevice device = DeviceManager.GetTIDevice(testInterfaceNumber);
            TextDevice deviceDebugPort = DebugDeviceManager.GetDevice(deviceDebugNumber);

            var includeToken = controller.IncludeNode(mode, timeoutMs, null);
            controller.WaitNodeStatusSignal(ZWave.BasicApplication.Enums.NodeStatuses.LearnReady, INCLUSION_LEARN_READY_TIMEOUT_MS);

            int attempts = 6;
            var IsNodeFoundReceived = false;
            while (attempts > 0 && !IsNodeFoundReceived)
            {
                attempts--;
                SetLearnMode(testInterfaceNumber);
                IsNodeFoundReceived = controller.WaitNodeStatusSignal(ZWave.BasicApplication.Enums.NodeStatuses.NodeFound, INCLUSION_NODE_FOUND_TIMEOUT_MS);
            }
            if (deviceDebugPort != null)
            {
                const int KEXREPORT_TIMEOUT = 21000;
                const int PUBLICKEY_TIMEOUT = 250000;
                const int INPUT_TIMEOUT = 10000;
                const byte CSA_REQUESTED = 0x01;
                var etRes = controller.ExpectData(new COMMAND_CLASS_SECURITY_2.KEX_REPORT(), KEXREPORT_TIMEOUT);
                if (etRes)
                {
                    COMMAND_CLASS_SECURITY_2.KEX_REPORT kexRep = etRes.Command;
                    if (kexRep.properties1.requestCsa == CSA_REQUESTED)
                    {
                        etRes = controller.ExpectData(new COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT(), PUBLICKEY_TIMEOUT);
                        var d_channel = D_CH.a;
                        var d_data = D_CMD.Csa;
                        int part1 = DeviceManager.IncludeSettings.JoinCSA.Take(2).ToArray().ToNumber();
                        int part2 = DeviceManager.IncludeSettings.JoinCSA.Skip(2).ToArray().ToNumber();
                        var csa = string.Format("{0}{1}", part1.ToString("00000"), part2.ToString("00000"));
                        var csaData = string.Format("{0} {1} {2}\r", d_channel.Channel, d_data.Command, csa);
                        //Wait Request from debug port:
                        var recievedResult = deviceDebugPort.ReceiveData(INPUT_TIMEOUT);
                        // Answer with CSA
                        if (recievedResult)
                        {
                            var requestResult = deviceDebugPort.Request(csaData, INPUT_TIMEOUT);
                        }
                    }
                }
            }

            var addRes = (InclusionResult)includeToken.WaitCompletedSignal();
            if (addRes.AddRemoveNode.SubstituteStatus != ZWave.Enums.SubstituteStatuses.None)
            {
                DeviceManager.LogNetworkKeys(controllerNumber, addRes.AddRemoveNode.Node);
            }
            DeviceManager.EnableSmartStart(controllerNumber);
            DeviceManager.IncludeSettings.Clear();
            return new BasicActionResult.IncludeNode(addRes);
        }

        public BasicResult.RemoveNode ExcludeNode(Alias controllerNumber, Alias testInterfaceNumber)
        {
            return ExcludeNode(controllerNumber, testInterfaceNumber, Modes.NodeAny);
        }
        public BasicResult.RemoveNode ExcludeNode(Alias controllerNumber, Alias testInterfaceNumber, Modes mode)
        {
            DeviceManager.DisableSmartStart(controllerNumber);
            DeviceManager.ResetSample(DeviceManager.GetDeviceHostClient(testInterfaceNumber));
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);

            var removeToken = controller.ExcludeNode((ZWave.Enums.Modes)mode, INCLUSION_TIMEOUT_MS, null);
            controller.WaitNodeStatusSignal(ZWave.BasicApplication.Enums.NodeStatuses.LearnReady, INCLUSION_LEARN_READY_TIMEOUT_MS);

            int attempts = 4;
            var IsAddingRemovingEndDeviceReceived = false;
            var expNS = ZWave.BasicApplication.Enums.NodeStatuses.NodeFound;
            while (attempts > 0 && !IsAddingRemovingEndDeviceReceived)
            {
                attempts--;
                SetLearnMode(testInterfaceNumber);
                IsAddingRemovingEndDeviceReceived = controller.WaitNodeStatusSignal(expNS, INCLUSION_ADDING_REMOVING_TIMEOUT_MS);
            }
            var removeRes = (ExclusionResult)removeToken.WaitCompletedSignal();
            if (!removeRes)
            {
                removeToken = controller.ExcludeNode(ZWave.Enums.Modes.NodeOptionNetworkWide | (ZWave.Enums.Modes)mode, INCLUSION_TIMEOUT_MS, null);
                controller.WaitNodeStatusSignal(ZWave.BasicApplication.Enums.NodeStatuses.LearnReady, INCLUSION_LEARN_READY_TIMEOUT_MS);

                attempts = 4;
                IsAddingRemovingEndDeviceReceived = false;
                while (attempts > 0 && !IsAddingRemovingEndDeviceReceived)
                {
                    attempts--;
                    DeviceManager.ResetSample(DeviceManager.GetDeviceHostClient(testInterfaceNumber));
                    SetLearnMode(testInterfaceNumber);
                    IsAddingRemovingEndDeviceReceived = controller.WaitNodeStatusSignal(expNS, INCLUSION_ADDING_REMOVING_TIMEOUT_MS);
                }
                removeRes = (ExclusionResult)removeToken.WaitCompletedSignal();
            }
            DeviceManager.EnableSmartStart(controllerNumber);
            return new BasicActionResult.RemoveNode(removeRes.AddRemoveNode);
        }

        public BasicResult.IncludeNodeWithLearnMode ReplaceFailedNode(Alias controllerNumber, Node node, Alias testInterfaceNumber)
        {
            if (DeviceManager.IncludeSettings.JoinDSK == null)
            {
                DeviceManager.IncludeSettings.JoinDSK = GetDSK(testInterfaceNumber, 2);
            }
            DeviceManager.DisableSmartStart(controllerNumber);
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            DeviceManager.ReportTestFramesAndExtensionOverrides(controllerNumber);

            var replaceToken = controller.ReplaceFailedNode(node, (x) => DeviceManager.EnableSmartStart(controllerNumber));
            controller.WaitNodeStatusSignal(ZWave.BasicApplication.Enums.NodeStatuses.LearnReady, 10000);

            int attempts = 6;
            var IsNodeFoundReceived = false;
            while (attempts > 0 && !IsNodeFoundReceived)
            {
                attempts--;
                SetLearnMode(testInterfaceNumber);
                IsNodeFoundReceived = controller.WaitNodeStatusSignal(ZWave.BasicApplication.Enums.NodeStatuses.NodeFound, INCLUSION_NODE_FOUND_TIMEOUT_MS);
            }

            var addRes = (InclusionResult)replaceToken.WaitCompletedSignal();
            if (addRes.AddRemoveNode.SubstituteStatus != ZWave.Enums.SubstituteStatuses.None)
            {
                DeviceManager.LogNetworkKeys(controllerNumber, addRes.AddRemoveNode.Node);
            }
            DeviceManager.EnableSmartStart(controllerNumber);
            DeviceManager.IncludeSettings.Clear();
            return new BasicActionResult.IncludeNodeWithLearnMode(addRes);
        }

        /// <summary>
        /// Power cycle
        /// </summary>
        /// <param name="deviceNumber"></param>
        public void SoftReset(Alias deviceNumber)
        {
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            DeviceManager.ResetSample(deviceHostClient);
        }

        public void SoftReset(Alias[] deviceNumbers)
        {
            List<DeviceHostClient> hosts = new List<DeviceHostClient>();
            foreach (var item in deviceNumbers)
            {
                hosts.Add(DeviceManager.GetDeviceHostClient(item));
            }
            DeviceManager.ResetSamples(hosts);
        }

        public int ProgramBootLoader(Alias deviceNumber, BootloaderPath bootloaderPath)
        {
            int ret = -1;
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            bool is700 = deviceHostClient.HasJName;
            if (is700)
            {
                ret = DeviceManager.ProgramBootLoader(deviceHostClient, bootloaderPath);
            }
            return ret;
        }



#if NETCOREAPP
        public void SendNodeInfo(Alias deviceNumber, byte destNodeId, TransmitOptions txOptions)
        {
            EmulatedDeviceManager.SendRequest(deviceNumber, System.Reflection.MethodBase.GetCurrentMethod().Name, destNodeId, txOptions);
        }
#endif
    }
}