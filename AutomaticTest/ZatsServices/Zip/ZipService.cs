// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Utils;
using Utils.UI.Enums;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.Devices;
using ZWave.Enums;
using ZWave.ZipApplication;
using ZWave.ZipApplication.Devices;
using ZWave.ZipApplication.Operations;


namespace Zats.Services
{
    public class ZipService : ServiceBase, IZipService
    {
        private const int INCLUSION_TIMEOUT_MS = 61000; // 30 sec -> 61 sec //HVA
        private const int SET_DEFAULT_TIMEOUT_MS = 65000; // 37 sec
        private const int GET_NODE_LIST_TIMEOUT_MS = 15000; // 15 sec
        private const int COMMAND_TIMEOUT_MS = 10000; // 10 sec
        private const int RECONNET_ATTEMPT_COUNT = 2;
        private ZipManager _zipManager;
        private DeviceManager _deviceManager;

        public ZipService(ZipManager zipManager, DeviceManager deviceManager)
        {
            _zipManager = zipManager;
            _deviceManager = deviceManager;
        }

        readonly ZWave.Enums.TransmitOptions TXO = ZWave.Enums.TransmitOptions.TransmitOptionAcknowledge |
           ZWave.Enums.TransmitOptions.TransmitOptionAutoRoute |
           ZWave.Enums.TransmitOptions.TransmitOptionExplore;

        public ServiceSettings Settings
        {
            get
            {
                return _zipManager.Settings;
            }
            set
            {
                _zipManager.Settings = value;
            }
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            _deviceManager.Log(text, logLevel);
        }

        private byte mSeqNo = 0;
        private byte GetNextSeqNo()
        {
            if (mSeqNo < 0xFF)
                mSeqNo++;
            else
                mSeqNo = 0;
            return mSeqNo;
        }

        public ZipResult.Ping Ping(IPAddress ipAddress, int count, int timeoutMs, int intervalMs)
        {
            return _zipManager.Ping(ipAddress, count, timeoutMs, intervalMs);
        }

        public ZipResult.Ping Ping(IPAddress ipAddress)
        {
            return Ping(ipAddress, 1, 250, 0);
        }

        public ZipResult.Ping Ping(Alias zipDeviceNumber)
        {
            ZipDevice device = _zipManager.GetDevice(zipDeviceNumber);
            return Ping(device.IpAddress);
        }

        public void Reconnect(Alias deviceNumber)
        {
            _zipManager.ReconnectVirtualDevice(deviceNumber);
        }

        public void Disconnect(Alias deviceNumber)
        {
            _zipManager.DisconnectDevice(deviceNumber);
        }

        public void ReconnectZipController(Alias zipController)
        {
            //_zipManager = new ZipManager();
            _zipManager.ReconnectZipController(zipController);
        }

        public void ReconnectUnsolicited(Alias unsolicitedNumber)
        {
            _zipManager.ReconnectUnsolicited(unsolicitedNumber);
        }

        public ZipResult.GetId GetId(Alias zipDeviceNumber)
        {
            ZipDevice zipController = _zipManager.GetDevice(zipDeviceNumber);
            ActionResult res = zipController.GetId();
            return new ZipResult.GetId((GetIdResult)res);
        }

        public ZipResult.GetId GetId(Alias zipDeviceNumber, Alias deviceNumber)
        {
            ZipDevice zipController = _zipManager.GetDevice(zipDeviceNumber);
            ZipDevice device = _zipManager.GetDevice(deviceNumber);
            ActionResult res = zipController.GetId(device.IpAddress);
            return new ZipResult.GetId((GetIdResult)res);
        }

        public ZipResult.GetIpAddress GetIpAddress(Alias zipControllerNumber, Alias deviceNumber)
        {
            ZipDevice controller = _zipManager.GetDevice(zipControllerNumber);
            bool useDtls1 = controller.TransportClient is Dtls1ClientTransportClient;
            ZipDevice device = null;
            try
            {
                device = _zipManager.GetDevice(deviceNumber);
            }
            catch (Exception ex)
            {
                $"{ex.Message}"._DLOG();
            }
            if (device != null)
            {
                GetIpAddressResult IpResult = new GetIpAddressResult();
                IpResult.HomeId = device.HomeId;
                IpResult.IpAddress = device.IpAddress;
                IpResult.NodeId = (byte)device.Id;
                ZipResult.GetIpAddress ret = new ZipResult.GetIpAddress(IpResult);
                return ret;
            }
            else
            {
                Device nodeDevice = _deviceManager.GetDevice(deviceNumber);
                nodeDevice.MemoryGetId();
                GetIpAddressResult res = controller.GetIpAddress(nodeDevice.Network.NodeTag);
                ZipResult.GetIpAddress ret = new ZipResult.GetIpAddress(res);
                return ret;
            }
        }

        public ZipResult.GetIpAddress GetIpAddress(Alias zipControllerNumber, Node node)
        {
            ZipDevice controller = _zipManager.GetDevice(zipControllerNumber);
            bool useDtls1 = controller.TransportClient is Dtls1ClientTransportClient;
            GetIpAddressResult res = controller.GetIpAddress(node);
            ZipResult.GetIpAddress ret = new ZipResult.GetIpAddress(res);
            return ret;
        }

        public string GetConnectionArgs(Alias zipControllerNumber)
        {
            ZipDevice controller = _zipManager.GetDevice(zipControllerNumber);
            return controller.DataSource.Args;
        }

        public string GetPskValue(Alias zipControllerNumber)
        {
            string ret = null;
            string pskArg = GetConnectionArgs(zipControllerNumber);
            var splittedTokens = pskArg.Split(' ');
            var enumerator = splittedTokens.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (((string)enumerator.Current) == "-psk" && enumerator.MoveNext())
                {
                    ret = (string)enumerator.Current;
                }
            }

            return ret;
        }

        public bool IsPortaConnection(Alias zipControllerNumber)
        {
            bool ret = false;
            ZipDevice controller = _zipManager.GetDevice(zipControllerNumber);
            if (controller.DataSource.Args == "")
            {
                ret = true;
            }

            return ret;
        }

        public void UnregisterZipDevice(Alias deviceNumber)
        {
            _zipManager.FinalizeVirtualDevice(deviceNumber);
        }

        public void RegisterZipDevice(Alias zipControllerNumber, Alias deviceNumber, Node node)
        {
            ZipDevice controller = _zipManager.GetDevice(zipControllerNumber);
            bool useDtls1 = controller.TransportClient is Dtls1ClientTransportClient;
            GetIpAddressResult res = controller.GetIpAddress(node);
            _zipManager.FinalizeVirtualDevice(deviceNumber);
            try
            {
                _zipManager.NewVirtualDevice(deviceNumber, res.IpAddress, useDtls1);
            }
            catch (Exception ex)
            {
                $"{ex.Message}"._DLOG();
                Console.WriteLine("Failed to register device");
            }
        }

        public void RegisterZipDevice(Alias zipControllerNumber, Alias deviceNumber, IPAddress deviceIpAddress)
        {
            ZipDevice controller = _zipManager.GetDevice(zipControllerNumber);
            bool useDtls1 = controller.TransportClient is Dtls1ClientTransportClient;
            _zipManager.FinalizeVirtualDevice(deviceNumber);
            try
            {
                _zipManager.NewVirtualDevice(deviceNumber, deviceIpAddress, useDtls1);
            }
            catch (Exception ex)
            {
                $"{ex.Message}"._DLOG();
                Console.WriteLine("Failed to register device");
            }
        }

        public void RegisterUnsolicitedZipDevice(Alias deviceNumber, IPEndPoint unsolEndPoint)
        {
            _zipManager.FinalizeVirtualDevice(deviceNumber);
            bool isDtls = !(unsolEndPoint.Port == 4123);

            if (isDtls)
            {
                _zipManager.NewUnsolicitedDevice(deviceNumber, unsolEndPoint, isDtls);
            }
            else
            {
                _zipManager.NewUnsolicitedDevice(deviceNumber, unsolEndPoint, isDtls);
            }
        }

        public void RegisterUnsolicitedZipDevice(Alias ZipController, Alias UnsoldeviceNumber, Alias deviceNumber)
        {
            _zipManager.FinalizeVirtualDevice(UnsoldeviceNumber);
            ZipController controller = (ZipController)_zipManager.GetDevice(ZipController);
            bool isDtls = controller.TransportClient is Dtls1ClientTransportClient;
            int PortNum = isDtls ? 41230 : 4123;
            IPEndPoint unsolEndPoint = new IPEndPoint(GetIpAddress(ZipController, deviceNumber).IpAddress, PortNum);
            if (isDtls)
            {
                _zipManager.NewVirtualDevice(UnsoldeviceNumber, unsolEndPoint.Address, isDtls, true);
            }
            else
            {
                _zipManager.NewUnsolicitedDevice(UnsoldeviceNumber, unsolEndPoint, isDtls);
            }
        }

        public void AddSecondaryUnsolicitedListener(Alias zipControllerNumber, int port)
        {
            string pskValue = GetPskValue(zipControllerNumber);
            _zipManager.RegisterSecondaryUnsolicitedListener(zipControllerNumber, port, pskValue);
        }

        public void CloseSecondaryUnsolicitedListener()
        {
            _zipManager.CloseSecondaryUnsolicitedListener();
        }

        public ZipResult.GetNodeList GetNodeList(Alias zipControllerNumber)
        {
            ZipController controller = (ZipController)_zipManager.GetDevice(zipControllerNumber);
            GetNodeListResult res = controller.GetNodeList(GET_NODE_LIST_TIMEOUT_MS);
            return new ZipResult.GetNodeList(res);
        }

        public ZipResult.AddNode AddNode(Alias zipControllerNumber, Alias deviceNumber)
        {
            return AddNodeWithDSK(zipControllerNumber, deviceNumber, Modes.NodeAnyS2, null, INCLUSION_TIMEOUT_MS);
        }

        public ZipResult.AddNode AddNode(Alias zipControllerNumber, Alias deviceNumber, Modes mode, int timeoutMs)
        {
            return AddNodeWithDSK(zipControllerNumber, deviceNumber, mode, null, timeoutMs);
        }

        public ZipResult.AddNode AddNode(Alias zipControllerNumber, Alias deviceNumber, Modes mode, SecuritySchemes[] GrantSchemes, int timeoutMs)
        {
            return AddNodeWithDSK(zipControllerNumber, deviceNumber, mode, null, GrantSchemes, timeoutMs);
        }

        public ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber)
        {
            return AddNodeWithDSK(zipControllerNumber, deviceNumber, Modes.NodeAnyS2, INCLUSION_TIMEOUT_MS);
        }

        public ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber, int dsk)
        {
            return AddNodeWithDSK(zipControllerNumber, deviceNumber, Modes.NodeAnyS2, dsk, INCLUSION_TIMEOUT_MS);
        }

        public ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber, byte[] dsk)
        {
            return AddNodeWithDSK(zipControllerNumber, deviceNumber, Modes.NodeAnyS2, dsk, INCLUSION_TIMEOUT_MS);
        }

        public ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber, Modes mode, int dsk, int timeoutMs)
        {
            byte[] dskInner = null;
            try
            {
                dskInner = new byte[2] { (byte)(dsk >> 8), (byte)dsk };
            }
            catch (Exception)
            {
            }
            return AddNodeWithDSK(zipControllerNumber, deviceNumber, Modes.NodeAnyS2, dskInner, INCLUSION_TIMEOUT_MS);
        }

        public ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber, Modes mode, byte[] dsk, SecuritySchemes[] GrantSchemes, int timeoutMs)
        {
            return IncludeNodeClassic(zipControllerNumber, deviceNumber, mode, dsk, GrantSchemes, timeoutMs);
        }

        public ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber, Modes mode, byte[] dsk, int timeoutMs)
        {
            return AddNodeWithDSK(zipControllerNumber, deviceNumber, mode, dsk, null, timeoutMs);
        }

        public ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber, Modes mode, int timeoutMs)
        {
            return AddNodeWithDSK(zipControllerNumber, deviceNumber, mode, null, null, timeoutMs);
        }

        private ZipResult.AddNode IncludeNodeClassic(Alias zipControllerNumber, Alias deviceNumber, Modes mode, byte[] dsk, SecuritySchemes[] GrantSchemes, int timeoutMs)
        {
            ZipController controller = (ZipController)_zipManager.GetDevice(zipControllerNumber);
            bool useDtls1 = controller.TransportClient is Dtls1ClientTransportClient;

            Device node = _deviceManager.GetDevice(deviceNumber);

            //Setting DSK
            if (GrantSchemes.IsNullOrEmpty())
            {
                _zipManager.GrantedSchemes = new SecuritySchemes[] { SecuritySchemes.S0, SecuritySchemes.S2_ACCESS, SecuritySchemes.S2_AUTHENTICATED, SecuritySchemes.S2_UNAUTHENTICATED };
                _deviceManager.IncludeSettings.GrantedSchemes = new SecuritySchemes[] { SecuritySchemes.S0, SecuritySchemes.S2_ACCESS, SecuritySchemes.S2_AUTHENTICATED, SecuritySchemes.S2_UNAUTHENTICATED };
            }
            else
            {
                _deviceManager.IncludeSettings.GrantedSchemes = GrantSchemes;
            }
            if (_deviceManager.IncludeSettings.JoinDSK == null)
            {
                _deviceManager.IncludeSettings.JoinDSK = dsk.IsNullOrEmpty() ? _deviceManager.GetDSK(deviceNumber, 2) : dsk;
            }
            SetTestInclusionValues(_deviceManager.IncludeSettings.JoinDSK,
                _deviceManager.IncludeSettings.JoinDSK,
                !_deviceManager.IncludeSettings.CancelGrantedSchemes,
                _deviceManager.IncludeSettings.GrantedSchemes);

            ActionToken addToken = controller.AddNodeToNetwork((ZWave.Enums.Modes)((byte)mode), TXO, timeoutMs, null);
            Thread.Sleep(1000);
            ZWave.BasicApplication.Operations.SetLearnModeResult learnRes = node.SetLearnMode(
                ZWave.Enums.LearnModes.LearnModeClassic,
                INCLUSION_TIMEOUT_MS);
            AddNodeResult res = (AddNodeResult)addToken.WaitCompletedSignal();

            ZipResult.AddNode ret = new ZipResult.AddNode(res);
            _deviceManager.IncludeSettings.Clear();
            if (res.IsStateCompleted)
            {
                controller.LastCommandStatus = res.Status;
                if (res.Status == 0x07)
                {
                    Log("Add Node Status Fail", LogLevels.Error);
                }
                else
                {
                    Thread.Sleep(500);
                    GetIpAddressResult ipRes = controller.GetIpAddress(res.Node);
                    _zipManager.FinalizeVirtualDevice(deviceNumber);
                    _zipManager.NewVirtualDevice(deviceNumber, ipRes.IpAddress, useDtls1);
                }
            }
            return ret;
        }

        public ZipResult.AddNode IncludeNode(Alias zipControllerNumber, Alias zipControllerUnsolicitedNumber, Alias deviceNumber, Modes mode, SecuritySchemes[] GrantSchemes, int timeoutMs)
        {
            var device = _deviceManager.GetDevice(deviceNumber);
            var res = device.GetTypeLibrary();
            var lib = new BasicResult.TypeLibrary(res).Library;
            var controller = _zipManager.GetDevice(zipControllerNumber);
            var isEndDevice = lib == LibTypes.EndDeviceSysTestLib || lib == LibTypes.EndDeviceLib || lib == LibTypes.EndDeviceLegacyRoutingLib || lib == LibTypes.EndDeviceLegacyLib;
            Modes addModeFx = mode == Modes.NodeAnyS2 ? Modes.NodeAny : mode;
            var devHostClient = _deviceManager.GetDeviceHostClient(deviceNumber);
            if (isEndDevice && _deviceManager.GetDeviceHostClient(deviceNumber).IsLongRange)
            {
                return IncludeSmartStartDevice(zipControllerNumber, zipControllerUnsolicitedNumber, deviceNumber, timeoutMs, true);
            }
            else
            {
                return IncludeNodeClassic(zipControllerNumber, deviceNumber, addModeFx, null, GrantSchemes, timeoutMs);
            }
        }

        public ZipResult.AddNode IncludeNode(Alias zipControllerNumber, Alias zipControllerUnsolicitedNumber, Alias deviceNumber, Modes mode, int timeoutMs)
        {
            return IncludeNode(zipControllerNumber, zipControllerUnsolicitedNumber, deviceNumber, mode, null, timeoutMs);
        }

        public ZipResult.AddNode IncludeNode(Alias zipControllerNumber, Alias zipControllerUnsolicitedNumber, Alias deviceNumber, int timeoutMs)
        {
            return IncludeNode(zipControllerNumber, zipControllerUnsolicitedNumber, deviceNumber, Modes.NodeAnyS2, timeoutMs);
        }

        public ZipResult.AddNode IncludeNode(Alias zipControllerNumber, Alias zipControllerUnsolicitedNumber, Alias deviceNumber)
        {
            return IncludeNode(zipControllerNumber, zipControllerUnsolicitedNumber, deviceNumber, INCLUSION_TIMEOUT_MS);
        }

        public ZipResult.RemoveNode ExcludeNode(Alias zipControllerNumber, Alias deviceNumber, int timeoutMs)
        {
            var device = _deviceManager.GetDevice(deviceNumber);
            var res = device.GetTypeLibrary();
            var lib = new BasicResult.TypeLibrary(res).Library;
            var controller = _zipManager.GetDevice(zipControllerNumber);
            var isEndDevice = lib == LibTypes.EndDeviceSysTestLib || lib == LibTypes.EndDeviceLib || lib == LibTypes.EndDeviceLegacyRoutingLib || lib == LibTypes.EndDeviceLegacyLib;
            if (isEndDevice &&
                device.Network.IsLongRangeEnabled(device.Network.NodeTag))
            {
                return ExcludeSmartStartDevice(zipControllerNumber, deviceNumber, timeoutMs);
            }
            else
            {
                return RemoveNode(zipControllerNumber, deviceNumber, Modes.NodeAny, timeoutMs);
            }
        }

        public ZipResult.RemoveNode ExcludeNode(Alias zipControllerNumber, Alias deviceNumber)
        {
            return ExcludeNode(zipControllerNumber, deviceNumber, INCLUSION_TIMEOUT_MS);
        }

        public ZipResult.AddNode AddNodeWithCsaDSK(Alias zipControllerNumber, Alias secondaryControllerNumber, int[] zipDsk)
        {
            return AddNodeWithCsaDSK(zipControllerNumber, secondaryControllerNumber, Modes.NodeAnyS2, zipDsk, INCLUSION_TIMEOUT_MS);
        }

        public ZipResult.AddNode AddNodeWithCsaDSK(Alias zipControllerNumber, Alias secondaryControllerNumber, byte[] zipDsk)
        {
            return AddNodeWithCsaDSK(zipControllerNumber, secondaryControllerNumber, Modes.NodeAnyS2, zipDsk, INCLUSION_TIMEOUT_MS);
        }

        public ZipResult.AddNode AddNodeWithCsaDSK(Alias zipControllerNumber, Alias secondaryControllerNumber, Modes mode, int[] zipDsk, int timeoutMs)
        {
            byte[] dskInner = null;
            try
            {
                dskInner = new byte[4] { (byte)(zipDsk[0] >> 8), (byte)zipDsk[0], (byte)(zipDsk[1] >> 8), (byte)zipDsk[1] };
            }
            catch (Exception)
            {
            }
            return AddNodeWithCsaDSK(zipControllerNumber, secondaryControllerNumber, Modes.NodeAnyS2, dskInner, INCLUSION_TIMEOUT_MS);
        }

        public ZipResult.AddNode AddNodeWithCsaDSK(Alias zipControllerNumber, Alias secondaryControllerNumber, Modes mode, byte[] zipDsk, int timeoutMs)
        {
            if (_deviceManager.IncludeSettings.GrantedSchemes == null)
            {
                _deviceManager.IncludeSettings.GrantedSchemes = new SecuritySchemes[] { SecuritySchemes.S0, SecuritySchemes.S2_ACCESS, SecuritySchemes.S2_AUTHENTICATED, SecuritySchemes.S2_UNAUTHENTICATED };

            }

            ZipController controller = (ZipController)_zipManager.GetDevice(zipControllerNumber);
            bool useDtls1 = controller.TransportClient is Dtls1ClientTransportClient;

            Controller secondaryController = (Controller)_deviceManager.GetDevice(secondaryControllerNumber);
            var sm = (SecurityManager)(secondaryController.SessionClient.GetSubstituteManager(typeof(SecurityManager)));
            OverrideDSKVerificationOnReceiverCallbackForSecurityManager(zipDsk, sm);
            sm.SecurityManagerInfo.TestEnableClientSideAuthS2 = true;

            //Setting DSK
            SetTestInclusionValues(new byte[] { }, zipDsk, true, _deviceManager.IncludeSettings.GrantedSchemes);

            ActionToken addToken = controller.AddNodeToNetwork((ZWave.Enums.Modes)((byte)mode), TXO, timeoutMs, null);
            Thread.Sleep(1000);
            ZWave.BasicApplication.Operations.SetLearnModeResult learnRes = secondaryController.SetLearnMode(
                ZWave.Enums.LearnModes.LearnModeClassic,
                INCLUSION_TIMEOUT_MS);
            AddNodeResult res = (AddNodeResult)addToken.WaitCompletedSignal();

            ZipResult.AddNode ret = new ZipResult.AddNode(res);

            if (res.IsStateCompleted)
            {
                controller.LastCommandStatus = res.Status;
                if (res.Status == 0x07)
                {
                    Log("Add Node Status Fail", LogLevels.Error);
                }
                else
                {
                    Thread.Sleep(500);
                    GetIpAddressResult ipRes = controller.GetIpAddress(res.Node);
                    _zipManager.FinalizeVirtualDevice(secondaryControllerNumber);
                    _zipManager.NewVirtualDevice(secondaryControllerNumber, ipRes.IpAddress, useDtls1);
                }
            }

            sm.SecurityManagerInfo.DSKVerificationOnReceiverCallback = null;
            sm.SecurityManagerInfo.TestEnableClientSideAuthS2 = false;

            return ret;
        }

        public ZipResult.AddNode AddNodeWithCsaDSK(Alias zipControllerNumber, Alias secondaryControllerNumber, Modes mode, byte[] zipDsk, SecuritySchemes[] GrantSchemes, int timeoutMs)
        {
            if (GrantSchemes.IsNullOrEmpty())
            {
                _deviceManager.IncludeSettings.GrantedSchemes = new SecuritySchemes[] { SecuritySchemes.S0, SecuritySchemes.S2_ACCESS, SecuritySchemes.S2_AUTHENTICATED, SecuritySchemes.S2_UNAUTHENTICATED };

            }
            else
            {
                _deviceManager.IncludeSettings.GrantedSchemes = GrantSchemes;
            }

            ZipController controller = (ZipController)_zipManager.GetDevice(zipControllerNumber);
            bool useDtls1 = controller.TransportClient is Dtls1ClientTransportClient;

            Controller secondaryController = (Controller)_deviceManager.GetDevice(secondaryControllerNumber);
            var sm = (SecurityManager)(secondaryController.SessionClient.GetSubstituteManager(typeof(SecurityManager)));
            OverrideDSKVerificationOnReceiverCallbackForSecurityManager(zipDsk, sm);
            sm.SecurityManagerInfo.TestEnableClientSideAuthS2 = true;

            //Setting DSK
            SetTestInclusionValues(new byte[] { }, zipDsk, true, _deviceManager.IncludeSettings.GrantedSchemes);

            ActionToken addToken = controller.AddNodeToNetwork((ZWave.Enums.Modes)((byte)mode), TXO, timeoutMs, null);
            Thread.Sleep(1000);
            ZWave.BasicApplication.Operations.SetLearnModeResult learnRes = secondaryController.SetLearnMode(
                ZWave.Enums.LearnModes.LearnModeClassic,
                INCLUSION_TIMEOUT_MS);
            AddNodeResult res = (AddNodeResult)addToken.WaitCompletedSignal();

            ZipResult.AddNode ret = new ZipResult.AddNode(res);

            if (res.IsStateCompleted)
            {
                controller.LastCommandStatus = res.Status;
                if (res.Status == 0x07)
                {
                    Log("Add Node Status Fail", LogLevels.Error);
                }
                else
                {
                    Thread.Sleep(500);
                    GetIpAddressResult ipRes = controller.GetIpAddress(res.Node);
                    _zipManager.FinalizeVirtualDevice(secondaryControllerNumber);
                    _zipManager.NewVirtualDevice(secondaryControllerNumber, ipRes.IpAddress, useDtls1);
                }
            }

            sm.SecurityManagerInfo.DSKVerificationOnReceiverCallback = null;
            sm.SecurityManagerInfo.TestEnableClientSideAuthS2 = false;

            return ret;
        }

        public ZipResult.AddNode IncludeSmartStartDevice(Alias zipControllerNumber, Alias zipControllerUnsolicitedNumber, Alias endDevice, int timeoutMs, bool isLongRange)
        {
            byte[] nodeAddStatusFilter = null;
            if (isLongRange == true)
            {
                nodeAddStatusFilter = new COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V4.EXTENDED_NODE_ADD_STATUS();
            }
            else
            {
                nodeAddStatusFilter = new COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V4.NODE_ADD_STATUS();
            }

            ZipController controller = (ZipController)_zipManager.GetDevice(zipControllerNumber);
            ZipDevice unsolicitedController = _zipManager.GetDevice(zipControllerUnsolicitedNumber);
            Device device = _deviceManager.GetDevice(endDevice);
            var sm = (SecurityManager)(device.SessionClient.GetSubstituteManager(typeof(SecurityManager)));

            var dskRes = _deviceManager.GetDSK(endDevice, 16);
            ActionToken expAddStatusToken = unsolicitedController.ExpectData(nodeAddStatusFilter, timeoutMs, null);

            SetProvisioningList(zipControllerNumber, new List<byte[]>() { dskRes }, isLongRange);
            ActionToken learnRes = device.ExecuteAsync(new ZWave.BasicApplication.Operations.SetLearnModeEndDeviceOperation(
                    device.Network, ZWave.Enums.LearnModes.LearnModeSmartStart | ZWave.Enums.LearnModes.NetworkMask,
                device.SetAssignStatusSignal, timeoutMs), null);
            //ActionToken learnRes = device.SetLearnMode(ZWave.Enums.LearnModes.LearnModeSmartStart | ZWave.Enums.LearnModes.NetworkMask, false, timeoutMs, null);

            byte status = 0x06;
            var learnModeResult = (ZWave.BasicApplication.Operations.SetLearnModeResult)learnRes.WaitCompletedSignal();
            if (learnModeResult.SubstituteStatus == ZWave.Enums.SubstituteStatuses.Failed)
            {
                device.SetDefault();
                status = 0x09;
            }

            var addStatusExp = new ZipToken.ExpectData(expAddStatusToken);
            var addStatusRes = addStatusExp.WaitCompletedSignal();
            _deviceManager.IncludeSettings.Clear();
            if (addStatusRes)
            {
                var res = GetSmartStartResult(addStatusRes.ReceivedData, isLongRange);
                RegisterZipDevice(zipControllerNumber, endDevice, res.Node);
                return res;
            }
            else if (learnModeResult)
            {
                Thread.Sleep(10000);
                RegisterUnsolicitedZipDevice(zipControllerNumber, zipControllerUnsolicitedNumber, endDevice);
                var getIpRes = GetIpAddress(zipControllerNumber, zipControllerUnsolicitedNumber);
                RegisterZipDevice(zipControllerNumber, endDevice, getIpRes.IpAddress);
                var res = new AddNodeResult()
                {
                    Status = status,
                    Node = learnModeResult.Node
                };
                return new ZipResult.AddNode(res, true);
            }
            else
            {
                Log("Inclusion status not received", LogLevels.Information);
                status = 0x07;
            }
            return new ZipResult.AddNode(new AddNodeResult() { Status = status }, false); // Empty result.
        }

        public ZipResult.RemoveNode ExcludeSmartStartDevice(Alias zipControllerNumber, Alias endDevice, int timeoutMs)
        {
            return RemoveNode(zipControllerNumber, endDevice, Modes.NodeAny, ZWave.Enums.LearnModes.LearnModeNWE | ZWave.Enums.LearnModes.NetworkMask, timeoutMs);
        }

        private ZipResult.AddNode GetSmartStartResult(byte[] nodeStatusFrame, bool isLongRange)
        {
            var result = new AddNodeResult();
            bool isStateCompleted = false;
            if (isLongRange && nodeStatusFrame[1] == COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V4.EXTENDED_NODE_ADD_STATUS.ID)
            {
                var packet = (COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V4.EXTENDED_NODE_ADD_STATUS)nodeStatusFrame;
                result.Node = new NodeTag((ushort)((packet.assignedNodeid[0] << 8) + packet.assignedNodeid[1]));
                result.NodeInfo = new NodeInfo
                {
                    Capability = packet.properties1,
                    Security = packet.properties2,
                    Basic = packet.basicDeviceClass,
                    Generic = packet.genericDeviceClass,
                    Specific = packet.specificDeviceClass
                };
                if (packet.commandClass != null)
                {
                    ZWave.Xml.Application.ZWaveDefinition.TryParseCommandClassRef(packet.commandClass, out byte[] commandClasses, out byte[] secureCommandClasses);
                    result.CommandClasses = commandClasses;
                    result.SecureCommandClasses = secureCommandClasses;
                }
                result.Status = packet.status;
                Log("Inclusion status: " + result.Status, LogLevels.Information);
                NetworkKeyS2Flags grantedKeys = (NetworkKeyS2Flags)packet.grantedKeys.Value;
                var securitySchemes = new List<ZWave.Enums.SecuritySchemes>();
                if (grantedKeys.HasFlag(NetworkKeyS2Flags.S2Class2))
                {
                    securitySchemes.Add(ConvertToSecurityScheme(NetworkKeyS2Flags.S2Class2));
                }
                if (grantedKeys.HasFlag(NetworkKeyS2Flags.S2Class1))
                {
                    securitySchemes.Add(ConvertToSecurityScheme(NetworkKeyS2Flags.S2Class1));
                }
                if (grantedKeys.HasFlag(NetworkKeyS2Flags.S2Class0))
                {
                    securitySchemes.Add(ConvertToSecurityScheme(NetworkKeyS2Flags.S2Class0));
                }
                if (grantedKeys.HasFlag(NetworkKeyS2Flags.S0))
                {
                    securitySchemes.Add(ConvertToSecurityScheme(NetworkKeyS2Flags.S0));
                }
                result.SecuritySchemes = securitySchemes.ToArray();
                isStateCompleted = true;
            }
            else if (nodeStatusFrame[1] == COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V4.NODE_ADD_STATUS.ID)
            {
                var packet = (COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V4.NODE_ADD_STATUS)nodeStatusFrame;
                result.Status = packet.status;
                result.Node = new NodeTag(packet.newNodeId);
                result.NodeInfo = new NodeInfo
                {
                    Capability = packet.properties1,
                    Security = packet.properties2,
                    Basic = packet.basicDeviceClass,
                    Generic = packet.genericDeviceClass,
                    Specific = packet.specificDeviceClass
                };
                if (packet.commandClass != null)
                {
                    ZWave.Xml.Application.ZWaveDefinition.TryParseCommandClassRef(packet.commandClass, out byte[] commandClasses, out byte[] secureCommandClasses);
                    result.CommandClasses = commandClasses;
                    result.SecureCommandClasses = secureCommandClasses;
                }
                isStateCompleted = true;
            }
            return new ZipResult.AddNode(result, isStateCompleted);
        }

        private ZWave.Enums.SecuritySchemes ConvertToSecurityScheme(NetworkKeyS2Flags verifyKey)
        {
            ZWave.Enums.SecuritySchemes ret = ZWave.Enums.SecuritySchemes.NONE;
            switch (verifyKey)
            {
                case NetworkKeyS2Flags.S2Class0:
                    ret = ZWave.Enums.SecuritySchemes.S2_UNAUTHENTICATED;
                    break;
                case NetworkKeyS2Flags.S2Class1:
                    ret = ZWave.Enums.SecuritySchemes.S2_AUTHENTICATED;
                    break;
                case NetworkKeyS2Flags.S2Class2:
                    ret = ZWave.Enums.SecuritySchemes.S2_ACCESS;
                    break;
                case NetworkKeyS2Flags.S0:
                    ret = ZWave.Enums.SecuritySchemes.S0;
                    break;
            }
            return ret;
        }

        private void OverrideDSKVerificationOnReceiverCallbackForSecurityManager(byte[] dsk, SecurityManager sm)
        {
            sm.SecurityManagerInfo.DSKVerificationOnReceiverCallback = () =>
            {
                return dsk;
            };
        }

        public ZipResult.LearnMode LearnMode(Alias zipControllerNumber)
        {
            return LearnMode(zipControllerNumber, LearnModes.LearnModeClassic, INCLUSION_TIMEOUT_MS);
        }

        public ZipResult.LearnMode LearnMode(Alias zipControllerNumber, LearnModes mode, int timeoutMs)
        {
            ZipController controller = (ZipController)_zipManager.GetDevice(zipControllerNumber);
            SetLearnModeResult res = controller.SetLearnMode((ZWave.Enums.LearnModes)((byte)mode), timeoutMs);
            ZipResult.LearnMode ret = new ZipResult.LearnMode(res);
            if (res.IsStateCompleted)
            {
                _zipManager.FinalizeAllVirtualDevices();
                controller.LastCommandStatus = res.Status;
                if (res.Status == 0x07)
                    Log("Learn Mode Status Fail: 0x" + res.Status.ToString("X2"), LogLevels.Error);
            }
            else
                Log("Learn Mode Status Fail: 0x" + res.Status.ToString("X2"), LogLevels.Error);
            return ret;
        }

        public ZipResult.RemoveNode RemoveNode(Alias zipControllerNumber, Alias deviceNumber)
        {
            return RemoveNode(zipControllerNumber, deviceNumber, Modes.NodeAny, INCLUSION_TIMEOUT_MS);
        }

        public ZipResult.RemoveNode RemoveNode(Alias zipControllerNumber, Alias deviceNumber, Modes mode, int timeoutMs)
        {
            return RemoveNode(zipControllerNumber, deviceNumber, mode, ZWave.Enums.LearnModes.LearnModeClassic, timeoutMs);
        }

        public ZipResult.RemoveNode RemoveNode(Alias zipControllerNumber, Alias deviceNumber, Modes mode, ZWave.Enums.LearnModes learnMode, int timeoutMs)
        {
            ZipController controller = (ZipController)_zipManager.GetDevice(zipControllerNumber);
            Device node = _deviceManager.GetDevice(deviceNumber);
            ActionToken removeToken = controller.RemoveNodeFromNetwork((ZWave.Enums.Modes)((byte)mode), timeoutMs, null);
            ZWave.BasicApplication.Operations.SetLearnModeResult learnRes = node.SetLearnMode(learnMode, INCLUSION_TIMEOUT_MS);
            RemoveNodeResult res = (RemoveNodeResult)removeToken.WaitCompletedSignal();
            ZipResult.RemoveNode ret = new ZipResult.RemoveNode(res);
            if (res.IsStateCompleted)
            {
                controller.LastCommandStatus = res.Status;
                if (res.Status == 0x07)
                    Log("Remove Node Status Fail", LogLevels.Error);
                else
                    _zipManager.FinalizeVirtualDevice(deviceNumber);
            }
            else
                Log("Remove Node Status Fail", LogLevels.Error);
            return ret;
        }

        public ZipResult.SetDefault SetDefault(Alias zipDeviceNumber)
        {
            DeleteProvisioningList(zipDeviceNumber);
            Thread.Sleep(2000);
            return SetDefault(zipDeviceNumber, SET_DEFAULT_TIMEOUT_MS);
        }

        public ZipResult.SetDefault SetDefault(Alias zipDeviceNumber, int timeoutMs)
        {
            _zipManager.FinalizeAllVirtualDevices();
            ZipDevice controller = _zipManager.GetDevice(zipDeviceNumber);
            SetDefaultResult res = controller.SetDefault(timeoutMs);
            ZipResult.SetDefault ret = new ZipResult.SetDefault(res);
            if (res.IsStateCompleted)
            {
                controller.LastCommandStatus = res.Status;
                if (res.Status == 0x07)
                {
                    Log("Set Default Status Fail - Z/IP GW is busy. Waiting 4!!!! minutes!", LogLevels.Error);
                    Thread.Sleep(4 * 60 * 1000);
                }
            }
            else
            {
                Log("Set Default Status Fail - Z/IP GW is busy. Waiting 4!!!! minutes!", LogLevels.Error);
                Thread.Sleep(4 * 60 * 1000);
            }
            return ret;
        }

        public ZipResult.SendData SendData(Alias zipReceiverNumber, byte[] packet)
        {
            return SendData(zipReceiverNumber, packet, true);
        }

        public ZipResult.SendData SendData(Alias zipReceiverNumber, byte[] packet, bool isValidateAckSeqNo)
        {
            return SendData(zipReceiverNumber, null, packet, isValidateAckSeqNo);
        }

        public ZipResult.SendData SendData(Alias zipReceiverNumber, byte[] headerExtension, byte[] packet, bool isValidateAckSeqNo)
        {
            return SendData(zipReceiverNumber, headerExtension, packet, isValidateAckSeqNo, 1000);
        }

        public ZipResult.SendData SendDataIPMulticast(Alias FirstNodeAlias, byte HeaderLengthByte,
            COMMAND_CLASS_ZIP_V5.COMMAND_ZIP_PACKET.PacketOption OptionsDataBytes, byte[] ZWaveCommandBytes)
        {
            COMMAND_CLASS_ZIP_V5.COMMAND_ZIP_PACKET Client_ZP_CMD = new COMMAND_CLASS_ZIP_V5.COMMAND_ZIP_PACKET();
            //Control Panel 1 
            Client_ZP_CMD.properties1.ackRequest = 0x01;
            Client_ZP_CMD.properties1.ackResponse = 0x00;
            Client_ZP_CMD.properties1.nackResponse = 0x00;
            Client_ZP_CMD.properties1.nackWaiting = 0x00;
            Client_ZP_CMD.properties1.nackQueueFull = 0x00;
            Client_ZP_CMD.properties1.nackOptionError = 0x00;
            //Control Panel 2
            Client_ZP_CMD.properties2.headerExtIncluded = 0x01;
            Client_ZP_CMD.properties2.zWaveCmdIncluded = 0x01;
            Client_ZP_CMD.properties2.moreInformation = 0x00;
            Client_ZP_CMD.properties2.secureOrigin = 0x01;
            //Sequent Number
            Client_ZP_CMD.seqNo = 0x03;
            //End Points
            Client_ZP_CMD.properties3.reserved3 = 0x00;
            Client_ZP_CMD.properties3.sourceEndPoint = 0x00;
            Client_ZP_CMD.properties4.destinationEndPoint = 0x00;
            Client_ZP_CMD.properties4.bitAddress = 0x00;
            //HeaderLength
            Client_ZP_CMD.headerLength = HeaderLengthByte;
            //Options
            Client_ZP_CMD.PacketOptions.Add(OptionsDataBytes);
            //ZWave Command Class
            foreach (var CMDbyte in ZWaveCommandBytes)
            {
                Client_ZP_CMD.zWaveCommand.Add(CMDbyte);
            }
            return SendData(FirstNodeAlias, Client_ZP_CMD, false);
        }

        public ZipResult.SendData SendData(Alias zipReceiverNumber, byte[] headerExtension, byte[] packet, bool isValidateAckSeqNo, int timeout)
        {
            ZipDevice zipDevice = _zipManager.GetDevice(zipReceiverNumber);

            int attemptNo = 0;
            SendDataResult sendAction() => zipDevice.SendData(headerExtension, packet, isValidateAckSeqNo, timeout);

            SendDataResult actionRes = null;
            bool actionResSucceeded = false;
            do
            {
                attemptNo++;
                actionRes = sendAction();
                actionResSucceeded = actionRes && actionRes.IsAckReceived;
                //Thread.Sleep
                if (!actionResSucceeded)
                {
                    Log(string.Format(
                            "{0}, device alias: {1}, reconnection attempt: {2}",
                            !actionRes ? "Connection fault" : "Send data ACK timeout",
                            zipReceiverNumber.ToString(),
                            attemptNo),
                        LogLevels.Warning);
                    Reconnect(zipReceiverNumber);
                }
            }
            while (!actionResSucceeded && attemptNo < RECONNET_ATTEMPT_COUNT);

            return new ZipResult.SendData(actionRes);
        }

        public ZipResult.RequestData RequestData(Alias zipDeviceNumber, byte[] data, byte[] expectedData, int timeoutMs)
        {
            return RequestData(zipDeviceNumber, null, data, expectedData, timeoutMs);
        }

        public ZipResult.RequestData RequestData(Alias zipDeviceNumber, byte[] headerExtension, byte[] data, byte[] expectedData, int timeoutMs)
        {
            ZipDevice zipDevice = _zipManager.GetDevice(zipDeviceNumber);
            RequestDataResult res = zipDevice.RequestData(headerExtension, data, expectedData, timeoutMs);
            ZipResult.RequestData ret = new ZipResult.RequestData(res);
            return ret;
        }

        public byte[] PutIPv4inIPv6Format(byte[] ipV4addr)
        {
            byte[] ipV6addr = new byte[16];
            if (ipV4addr.Length == 4)
            {
                ipV6addr[10] = 0xFF;
                ipV6addr[11] = 0xFF;
                Array.Copy(ipV4addr, 0, ipV6addr, 12, 4);
            }
            else
                ipV6addr = ipV4addr;
            return ipV6addr;
        }

        public void SetTestInclusionValues(byte[] dsk)
        {
            SetTestInclusionValues(dsk, null);
        }

        public void SetTestInclusionValues(byte[] dsk, byte[] csa)
        {
            SetTestInclusionValues(dsk, csa, true);
        }

        public void SetTestInclusionValues(byte[] dsk, byte[] csa, bool isKexReportConfirmed)
        {
            SetTestInclusionValues(dsk, csa, isKexReportConfirmed, null);
        }
        public void SetTestInclusionValues(byte[] dsk, byte[] csa, bool isKexReportConfirmed, SecuritySchemes[] grantedSchemes)
        {
            _zipManager.DskValue = dsk;
            _zipManager.CsaValue = csa;
            _zipManager.IsKexReportConfirmed = isKexReportConfirmed;
            _zipManager.GrantedSchemes = grantedSchemes;
        }

        SortedList<int, byte[]> downloadFirmware = new SortedList<int, byte[]>();
        BitArray expectPackets = null;
        bool isDownloadCompleted = false;
        bool isDownloadCancelled = false;
        readonly byte downloadNumberOfReports = 255;
        private List<byte[]> OnDownloadReceived(ZipController controller, ActionToken token, byte[] header, byte[] payload)
        {
            List<byte[]> ret = null;
            if (payload != null)
            {
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REPORT cmd = payload;
                int reportNumber = (cmd.properties1.reportNumber1 << 8) + cmd.reportNumber2;
                expectPackets[(reportNumber - 1) % downloadNumberOfReports] = true;
                bool isAllReceived = true;
                for (int i = 0; i < expectPackets.Count; i++)
                {
                    if (!expectPackets[i])
                    {
                        isAllReceived = false;
                        break;
                    }
                }
                if (cmd.data != null)
                {
                    if (downloadFirmware.ContainsKey(reportNumber))
                    {
                        downloadFirmware[reportNumber] = cmd.data.ToArray();
                    }
                    else
                    {
                        downloadFirmware.Add(reportNumber, cmd.data.ToArray());
                    }
                }

                if (cmd.properties1.last > 0)
                {
                    isDownloadCompleted = true;
                    controller.Cancel(token);
                }
                else if (isAllReceived)
                {
                    controller.Cancel(token);
                }
            }
            return ret;
        }
        public ZipResult.FirmwareBackup FirmwareBackupV5(Alias zipControllerNumber, int firmwareTarget, int timeoutMs)
        {
            return FirmwareBackupV5(zipControllerNumber, null, firmwareTarget, timeoutMs);
        }
        public ZipResult.FirmwareBackup FirmwareBackupV5(Alias zipControllerNumber, byte[] headerExtension, int firmwareTarget, int timeoutMs)
        {
            ZipResult.FirmwareBackup ret = new ZipResult.FirmwareBackup();
            ZipController controller = (ZipController)_zipManager.GetDevice(zipControllerNumber);
            var fwuGetData = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_GET();
            var fwuReportData = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT();
            var mdGetRes = controller.RequestData(headerExtension, fwuGetData, fwuReportData, 3000);
            if (mdGetRes)
            {
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT cfwuReportData = mdGetRes.ReceivedData;
                var checksum = cfwuReportData.firmware0Checksum;
                var manufacturerID = cfwuReportData.manufacturerId;
                var firmwareID = cfwuReportData.firmware0Id;
                var isFirmwareUpgradable = (cfwuReportData.firmwareUpgradable == 0x00) ? false : true;
                var maxFragmentSize = Tools.GetInt32(cfwuReportData.maxFragmentSize);
                var fragmentSize = Tools.GetInt32(cfwuReportData.maxFragmentSize);
                var hardwareVersion = cfwuReportData.hardwareVersion;
                var numberOfFirmwareTargets = cfwuReportData.numberOfFirmwareTargets;

                SortedList<int, byte[]> firmwareTargets = new SortedList<int, byte[]>();
                firmwareTargets.Add(0, cfwuReportData.firmware0Id);
                if (cfwuReportData.vg1 != null)
                {
                    foreach (var item in cfwuReportData.vg1)
                    {
                        if (item.firmwareId != null)
                        {
                            firmwareTargets.Add(firmwareTargets.Count, item.firmwareId);
                        }
                    }
                }

                var getCmd = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_PREPARE_GET();
                getCmd.manufacturerId = manufacturerID;
                getCmd.firmwareId = firmwareTargets[firmwareTarget];
                getCmd.firmwareTarget = (byte)firmwareTarget;
                getCmd.fragmentSize = Tools.GetBytes((ushort)fragmentSize);
                getCmd.hardwareVersion = hardwareVersion;

                byte[] expectCmd = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_PREPARE_REPORT();
                var res = controller.RequestData(headerExtension, getCmd, expectCmd, 3000);
                if (res)
                {
                    COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_PREPARE_REPORT reportCmd = res.ReceivedData;
                    ret.PrepareStatus = reportCmd.status;
                    if (reportCmd.status == 0xFF)
                    {
                        isDownloadCompleted = false;
                        isDownloadCancelled = false;
                        int iteration = 0;
                        while (!isDownloadCompleted && !isDownloadCancelled)
                        {
                            var get2Cmd = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_GET();
                            get2Cmd.numberOfReports = downloadNumberOfReports;
                            int reportNumber = iteration * downloadNumberOfReports + 1;
                            get2Cmd.properties1.reportNumber1 = (byte)(reportNumber >> 8);
                            get2Cmd.reportNumber2 = (byte)reportNumber;
                            expectPackets = new BitArray(downloadNumberOfReports);
                            var token = controller.ResponseMultiData(headerExtension, (t, h, d) => OnDownloadReceived(controller, t, h, d), new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REPORT());
                            var res2 = controller.SendData(headerExtension, get2Cmd);
                            if (res2)
                            {
                                token.WaitCompletedSignal();
                                if (isDownloadCompleted && downloadFirmware != null)
                                {
                                    List<byte> tmp = new List<byte>();
                                    foreach (var item in downloadFirmware)
                                    {
                                        tmp.AddRange(item.Value);
                                    }
                                    ret.Data = tmp.ToArray();
                                }
                            }
                            iteration++;
                        }
                    }
                }
            }

            return ret;
        }
        private List<byte[]> OnRequestReceived(ZipController controller, List<byte[]> fwData, ActionToken token, byte[] header, byte[] payload)
        {
            List<byte[]> ret = null;
            if (payload != null)
            {
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_GET fwuMDGetData = payload;
                List<byte[]> fwuMDReportData = new List<byte[]>();
                short firmwareDataIndex = 0;
                for (byte i = 0; i < fwuMDGetData.numberOfReports; i++)
                {
                    firmwareDataIndex = (short)(((fwuMDGetData.properties1.reportNumber1 << 8) + fwuMDGetData.reportNumber2) + i);
                    if (fwData.Count > (firmwareDataIndex - 1))
                    {
                        List<byte> dataToSend = new List<byte>(fwData[firmwareDataIndex - 1]);
                        dataToSend.Insert(0, COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.ID);
                        dataToSend.Insert(1, COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REPORT.ID);
                        if (fwData.Count <= firmwareDataIndex)
                        {
                            dataToSend.Insert(2, (byte)((firmwareDataIndex >> 8) | 0x80));
                        }
                        else
                        {
                            dataToSend.Insert(2, (byte)(firmwareDataIndex >> 8));
                        }
                        dataToSend.Insert(3, (byte)firmwareDataIndex);

                        short crc = Utils.Tools.CalculateCrc16(dataToSend);

                        dataToSend.Add((byte)(crc >> 8));
                        dataToSend.Add((byte)crc);

                        fwuMDReportData.Add(dataToSend.ToArray());
                    }
                }
                return fwuMDReportData;
            }
            return ret;
        }
        public ZipResult.FirmwareUpdate FirmwareUpdateV5(Alias zipControllerNumber, byte[] data, int firmwareTarget, int timeoutMs)
        {
            return FirmwareUpdateV5(zipControllerNumber, null, data, firmwareTarget, false, timeoutMs);
        }
        public ZipResult.FirmwareUpdate FirmwareUpdateV5(Alias zipControllerNumber, byte[] headerExtension, byte[] data, int firmwareTarget, bool hasActivation, int timeoutMs)
        {
            ZipResult.FirmwareUpdate ret = new ZipResult.FirmwareUpdate();
            ZipController controller = (ZipController)_zipManager.GetDevice(zipControllerNumber);
            var fwuGetData = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_GET();
            var fwuReportData = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT();
            var mdGetRes = controller.RequestData(headerExtension, fwuGetData, fwuReportData, 3000);
            if (mdGetRes)
            {
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT cfwuReportData = mdGetRes.ReceivedData;
                var checksum = cfwuReportData.firmware0Checksum;
                var manufacturerID = cfwuReportData.manufacturerId;
                var firmwareID = cfwuReportData.firmware0Id;
                var isFirmwareUpgradable = (cfwuReportData.firmwareUpgradable == 0x00) ? false : true;
                var maxFragmentSize = Tools.GetInt32(cfwuReportData.maxFragmentSize);
                var fragmentSize = Tools.GetInt32(cfwuReportData.maxFragmentSize);
                var hardwareVersion = cfwuReportData.hardwareVersion;
                var numberOfFirmwareTargets = cfwuReportData.numberOfFirmwareTargets;

                SortedList<int, byte[]> firmwareTargets = new SortedList<int, byte[]>();
                firmwareTargets.Add(0, cfwuReportData.firmware0Id);
                if (cfwuReportData.vg1 != null)
                {
                    foreach (var item in cfwuReportData.vg1)
                    {
                        if (item.firmwareId != null)
                        {
                            firmwareTargets.Add(firmwareTargets.Count, item.firmwareId);
                        }
                    }
                }
                List<byte[]> fwData = Tools.ArraySplit(data, fragmentSize);

                //register status listener

                var statusToken = controller.ExpectData(new COMMAND_CLASS_FIRMWARE_UPDATE_MD.FIRMWARE_UPDATE_MD_STATUS_REPORT(), timeoutMs, null);

                // register response
                var responseToken = controller.ResponseMultiData(
                           headerExtension,
                           (t, h, d) => OnRequestReceived(controller, fwData, t, h, d),
                           new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_GET());

                // start request
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_GET fwuMDRequestGetData = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_GET();
                fwuMDRequestGetData.checksum = Utils.Tools.CalculateCrc16Array(data);
                fwuMDRequestGetData.firmwareId = firmwareTargets[firmwareTarget];
                fwuMDRequestGetData.manufacturerId = manufacturerID;
                fwuMDRequestGetData.firmwareTarget = (byte)firmwareTarget;
                fwuMDRequestGetData.fragmentSize = Tools.GetBytes((ushort)fragmentSize);
                fwuMDRequestGetData.properties1.activation = (byte)(hasActivation ? 1 : 0);
                fwuMDRequestGetData.hardwareVersion = hardwareVersion;
                var reqRes = controller.RequestData(headerExtension, fwuMDRequestGetData, new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_REPORT(), 3000);
                if (reqRes && reqRes.ReceivedData != null)
                {
                    COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_REPORT rep = reqRes.ReceivedData;
                    ret.RequestStatus = rep.status;
                }

                var statusRes = (ExpectDataResult)statusToken.WaitCompletedSignal();
                controller.Cancel(responseToken);
                if (statusRes)
                {
                    COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_STATUS_REPORT repCmd = statusRes.ReceivedData;
                    ret.ReportStatus = repCmd.status;
                }
            }
            return ret;
        }
        public void StopCallbacksListeners(Alias zipControllerNumber)
        {
            ZipController zipController = (ZipController)_zipManager.GetDevice(zipControllerNumber);
            _zipManager.StopCallbacksListeners(zipController);
        }
        public bool IsZipConnected(Alias zipControllerNumber)
        {
            ZipController zipControler = (ZipController)_zipManager.GetDevice(zipControllerNumber);
            bool ret = zipControler.TransportClient.IsOpen;
            return ret;
        }
        public void DeleteProvisioningList(Alias zipControllerNumber)
        {
            var device = _zipManager.GetDevice(zipControllerNumber);
            COMMAND_CLASS_PROVISIONING_LIST.PROVISION_DELETE ProvisioningListDelete = new COMMAND_CLASS_PROVISIONING_LIST.PROVISION_DELETE();
            //Clear provisioning list
            device.SendData(null, new COMMAND_CLASS_PROVISIONING_LIST.PROVISION_DELETE() { });
        }
        public void SetProvisioningList(Alias zipControllerNumber, List<byte[]> Dsk, bool isLongRange)
        {
            var device = _zipManager.GetDevice(zipControllerNumber);
            COMMAND_CLASS_PROVISIONING_LIST.PROVISION_SET ProvisioningListSet = new COMMAND_CLASS_PROVISIONING_LIST.PROVISION_SET();

            foreach (byte[] devicekey in Dsk)
            {
                ProvisioningListSet.Dsk = new List<byte>(devicekey);
                ProvisioningListSet.DskLength = (byte)devicekey.Length;
                if (isLongRange)
                {
                    ProvisioningListSet.metadataExtension.Add(new COMMAND_CLASS_PROVISIONING_LIST.PROVISION_SET.MetaDataExtension()
                    {
                        metaDataType = { Type = (byte)PLMetaData.BootstrappingMode, Critical = 0x01 },
                        Length = 0x01,
                        Value = new List<byte>() { 0x02 }
                    });
                }
                device.SendData(null, ProvisioningListSet);
                Thread.Sleep(300);
            }
        }
    }
}
