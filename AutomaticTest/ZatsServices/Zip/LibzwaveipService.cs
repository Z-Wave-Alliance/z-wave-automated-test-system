// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using Utils.UI.Enums;
using System;
using System.Net;
using ZWave.ZipApplication.Devices;
using System.IO;
using System.Threading;
using ZWave;
using ZWave.BasicApplication.Devices;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;

namespace Zats.Services
{
    public class LibzwaveipService : ServiceBase, ILibzwaveipService
    {
        Dictionary<int, ActionToken> testResults = new Dictionary<int, ActionToken>();
        readonly IPAddress PortalDefaultAddress = IPAddress.Parse("fd00:1111::10");

        private const int INCLUSION_TIMEOUT_MS = 61000; // 30 sec -> 61 sec //HVA

        List<string> receiveStr = new List<string> { };
        List<string> receiveAddNodeStr = new List<string> { };
        List<string> receiveListener = new List<string> { };
        List<byte[]> myByteList = new List<byte[]> { };
        string zipUnsolicitedDestinationIp6 = null;
        List<string> discoveredList = new List<string> { };

        bool isListening = false;
        String gwIp6Address = null;
        string strLine = "";
        bool clientIsStart = false;
        string SysLog = "SysLog: ";

        Task myTask;
        Action myListenerAction;

        public IProvisioningList ProvisioningList
        {
            get { return ZipManager.ProvisioningList; }
        }
        private ZipManager ZipManager { get; set; }
        private DeviceManager _deviceManager;

        public LibzwaveipService(ZipManager zipManager, DeviceManager deviceManager)
        {
            ZipManager = zipManager;
            _deviceManager = deviceManager;
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            ZipManager.Log(text, logLevel);
        }

        #region Classic Network management
        public ReciveDataPacket SetDefaultGw(Alias ZipController)
        {
            receiveStr.Clear();
            List<string> default_set_status = new List<string>
            {
                "DEFAULT_SET_DONE",
                "DEFAULT_SET_BUSY"};

            if (!clientIsStart)
            {
                RunReferenceClient(ZipController);
                Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
            }
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.GWSetDefault.Command);
            Thread.Sleep(15000);
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
            Thread.Sleep(2000);

            while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
            {
                Log("Reader: " + strLine, LogLevels.Information);
                receiveStr.Add(strLine);
                if (strLine.Contains(default_set_status[0]))
                {
                    Log(default_set_status[0], LogLevels.Ok);
                }
                else if (strLine.Contains(default_set_status[1]))
                {
                    Log(default_set_status[1], LogLevels.Error);
                }
            }
            discoveredList = List(ZipController);
            Thread.Sleep(3000);
            return ReciveData(new COMMAND_CLASS_NETWORK_MANAGEMENT_BASIC_V2.DEFAULT_SET_COMPLETE());
        }
        public ReciveDataPacket AddNodeWithDSK(Alias ZipController, Alias JoinNode)
        {
            receiveStr.Clear();
            List<string> node_add_status = new List<string>()
            {
                "NODE_ADD_STATUS_DONE",
                "NODE_ADD_STATUS_FAILED",
                "NODE_ADD_STATUS_SECURITY_FAILED" };

            if (!clientIsStart)
            {
                RunReferenceClient(ZipController);
                Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
            }

            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshAddNodeCMD.AddNode.Command);
            Thread.Sleep(3000);

            byte[] JoinNodeDSK = _deviceManager.GetDSK(JoinNode, 2);
            string JoinNodeStrDSK = ConvertDSK(JoinNodeDSK);

            Device node = _deviceManager.GetDevice(JoinNode);

            ZWave.Enums.LearnModes lmode = ZWave.Enums.LearnModes.LearnModeClassic;
            if ((lmode & ZWave.Enums.LearnModes.LearnModeSmartStart) != ZWave.Enums.LearnModes.LearnModeSmartStart)
            {
                _deviceManager.DisableSmartStart(JoinNode);
            }
            ActionToken token = node.SetLearnMode(lmode, false, INCLUSION_TIMEOUT_MS, (x) => _deviceManager.EnableSmartStart(JoinNode));
            Thread.Sleep(5000);

            while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
            {
                Console.WriteLine("Reader: " + strLine);
                if (strLine.Contains("Enter 'grantkeys' to accept or 'abortkeys' to cancel"))
                {
                    ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshAddNodeCMD.GrantKeys("87").Command);
                    Log(SysLog + "Libzwaveip Selected GrantKeys option.", LogLevels.Text);
                    Thread.Sleep(5000);

                    while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
                    {
                        Console.WriteLine("Reader: " + strLine);
                        if (strLine.Contains("Please approve by typing 'acceptdsk 12345'"))
                        {
                            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshAddNodeCMD.AcceptDSK.Command + JoinNodeStrDSK);
                            Log(SysLog + "send join node DSK to lib Client.", LogLevels.Text);
                        }
                    }
                }
            }
            Thread.Sleep(25000);

            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
            Thread.Sleep(2000);

            while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
            {
                //Console.WriteLine("Reader: " + strLine);
                Log("Reader: " + strLine, LogLevels.Information);
                receiveStr.Add(strLine);
                if (strLine.Contains(node_add_status[0]))
                {
                    Log(node_add_status[0], LogLevels.Ok);
                }
                else if (strLine.Contains(node_add_status[1]))
                {
                    Log(node_add_status[1], LogLevels.Fail);
                }
                else if (strLine.Contains(node_add_status[2]))
                {
                    Log(node_add_status[2], LogLevels.Fail);
                }
                else
                {

                }
            }
            Thread.Sleep(3000);

            discoveredList = List(ZipController);
            return ReciveData(new COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V3.NODE_ADD_STATUS());
        }
        public ReciveDataPacket AddNodeWithGrantkeys(Alias ZipController, Alias JoinNode, GrantKeyType[] grantkeys)
        {
            receiveStr.Clear();
            string myGrantkeys = "";
            List<string> node_add_status = new List<string>()
            {
                "NODE_ADD_STATUS_DONE",
                "NODE_ADD_STATUS_FAILED",
                "NODE_ADD_STATUS_SECURITY_FAILED" };         

            if (!clientIsStart)
            {
                RunReferenceClient(ZipController);
                Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
            }

            myGrantkeys = GetStringGrantkeys(grantkeys);

            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshAddNodeCMD.AddNode.Command);
            Thread.Sleep(3000);

            byte[] JoinNodeDSK = _deviceManager.GetDSK(JoinNode, 2);
            string JoinNodeStrDSK = ConvertDSK(JoinNodeDSK);

            Device node = _deviceManager.GetDevice(JoinNode);

            ZWave.Enums.LearnModes lmode = ZWave.Enums.LearnModes.LearnModeClassic;
            if ((lmode & ZWave.Enums.LearnModes.LearnModeSmartStart) != ZWave.Enums.LearnModes.LearnModeSmartStart)
            {
                _deviceManager.DisableSmartStart(JoinNode);
            }
            ActionToken token = node.SetLearnMode(lmode, false, INCLUSION_TIMEOUT_MS, (x) => _deviceManager.EnableSmartStart(JoinNode));
            Thread.Sleep(5000);

            while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
            {
                Console.WriteLine("Reader: " + strLine);
                if (strLine.Contains("Enter 'grantkeys' to accept or 'abortkeys' to cancel"))
                {
                    ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshAddNodeCMD.GrantKeys(myGrantkeys).Command);
                    Log(SysLog + "Libzwaveip Selected GrantKeys option.", LogLevels.Text);
                    Thread.Sleep(5000);

                    while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
                    {
                        Console.WriteLine("Reader: " + strLine);
                        if (strLine.Contains("Please approve by typing 'acceptdsk 12345'"))
                        {
                            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshAddNodeCMD.AcceptDSK.Command + JoinNodeStrDSK);
                            Log(SysLog + "send join node DSK to lib Client.", LogLevels.Text);
                        }
                    }
                }
            }
            Thread.Sleep(25000);

            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
            Thread.Sleep(2000);

            while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
            {
                //Console.WriteLine("Reader: " + strLine);
                Log("Reader: " + strLine, LogLevels.Information);
                receiveStr.Add(strLine);
                if (strLine.Contains(node_add_status[0]))
                {
                    Log(node_add_status[0], LogLevels.Ok);
                }
                else if (strLine.Contains(node_add_status[1]))
                {
                    Log(node_add_status[1], LogLevels.Fail);
                }
                else if (strLine.Contains(node_add_status[2]))
                {
                    Log(node_add_status[2], LogLevels.Fail);
                }
                else
                {

                }
            }
            Thread.Sleep(3000);

            discoveredList = List(ZipController);
            return ReciveData(new COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V3.NODE_ADD_STATUS());
        }
        public ReciveDataPacket AddNodeNonSecurity(Alias ZipController, Alias JoinNode)
        {
            receiveStr.Clear();
            List<string> node_add_status = new List<string>()
            {
                "NODE_ADD_STATUS_DONE",
                "NODE_ADD_STATUS_FAILED",
                "NODE_ADD_STATUS_SECURITY_FAILED" };

            if (!clientIsStart)
            {
                RunReferenceClient(ZipController);
                Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
            }

            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshAddNodeCMD.AddNode.Command);
            Thread.Sleep(3000);

            byte[] JoinNodeDSK = _deviceManager.GetDSK(JoinNode, 2);
            string JoinNodeStrDSK = ConvertDSK(JoinNodeDSK);

            Device node = _deviceManager.GetDevice(JoinNode);

            ZWave.Enums.LearnModes lmode = ZWave.Enums.LearnModes.LearnModeClassic;
            if ((lmode & ZWave.Enums.LearnModes.LearnModeSmartStart) != ZWave.Enums.LearnModes.LearnModeSmartStart)
            {
                _deviceManager.DisableSmartStart(JoinNode);
            }
            ActionToken token = node.SetLearnMode(lmode, false, INCLUSION_TIMEOUT_MS, (x) => _deviceManager.EnableSmartStart(JoinNode));
            Thread.Sleep(5000);

            while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
            {
                Console.WriteLine("Reader: " + strLine);
                if (strLine.Contains("Enter 'grantkeys' to accept or 'abortkeys' to cancel"))
                {
                    ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshAddNodeCMD.AbortKeys.Command);
                    Log(SysLog + "Libzwaveip Selected GrantKeys option.", LogLevels.Text);
                    Thread.Sleep(5000);                    
                }
            }
            Thread.Sleep(25000);

            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
            Thread.Sleep(2000);

            while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
            {
                //Console.WriteLine("Reader: " + strLine);
                Log("Reader: " + strLine, LogLevels.Information);
                receiveStr.Add(strLine);
                if (strLine.Contains(node_add_status[0]))
                {
                    Log(node_add_status[0], LogLevels.Ok);
                }
                else if (strLine.Contains(node_add_status[1]))
                {
                    Log(node_add_status[1], LogLevels.Fail);
                }
                else if (strLine.Contains(node_add_status[2]))
                {
                    Log(node_add_status[2], LogLevels.Fail);
                }
                else
                {

                }
            }
            Thread.Sleep(3000);

            discoveredList = List(ZipController);
            return ReciveData(new COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V3.NODE_ADD_STATUS());
        }
        public ReciveDataPacket AddNodeStop(Alias ZipController)
        {
            receiveStr.Clear();
            if (!clientIsStart)
            {
                RunReferenceClient(ZipController);
                Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
            }

            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshAddNodeCMD.AddNode.Command);
            Thread.Sleep(3000);
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshAddNodeCMD.AddNodeStop.Command);
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
            Thread.Sleep(2000);

            while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
            {
                Log("Reader: " + strLine, LogLevels.Information);
                receiveStr.Add(strLine);
            }
            Thread.Sleep(3000);
            return ReciveData(new COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V3.NODE_ADD_STATUS());
        }
        public void AddNodeNonSecurity() { }
        public void Learnmode(Alias ZipController)
        {
            receiveStr.Clear();
            List<string> Learn_mode_status = new List<string>
            {
                "LEARN_MODE_DONE",
                "LEARN_MODE_FAILED_TIMEOUT",
                "LEARN_MODE_FAILED",
                "LEARN_MODE_SECURITY_FAILED"
            };
            if (!clientIsStart)
            {
                RunReferenceClient(ZipController);
                Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
            }

            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshLearnModeCMD.SetLearnMode.Command);
            Thread.Sleep(50000);
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
            Thread.Sleep(2000);

            while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
            {
                Log("Reader: " + strLine, LogLevels.Information);
                receiveStr.Add(strLine);
                if (strLine.Contains(Learn_mode_status[0]))
                {
                    Log(Learn_mode_status[0], LogLevels.Ok);
                }
                else if (strLine.Contains(Learn_mode_status[1]))
                {
                    Log(Learn_mode_status[1], LogLevels.Fail);
                }
                else if (strLine.Contains(Learn_mode_status[2]))
                {
                    Log(Learn_mode_status[2], LogLevels.Fail);
                }
                else if (strLine.Contains(Learn_mode_status[3]))
                {
                    Log(Learn_mode_status[3], LogLevels.Fail);
                }
                else
                {

                }
            }
            Thread.Sleep(3000);
        }
        public void LearnmodeStop(Alias ZipController)
        {
            receiveStr.Clear();
            if (!clientIsStart)
            {
                RunReferenceClient(ZipController);
                Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
            }

            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshLearnModeCMD.SetLearnMode.Command);
            Thread.Sleep(5000);
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshLearnModeCMD.SetLearnModeStop.Command);
            Thread.Sleep(2000);
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
            Thread.Sleep(2000);

            while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
            {
                Log("Reader: " + strLine, LogLevels.Information);
                receiveStr.Add(strLine);
            }
            Thread.Sleep(3000);
        }
        public void Identify() { }
        public void LifeLine(Alias ZipController, byte ReportDeviceNodeId, byte ReciveDeviceNodeId)
        {
            receiveStr.Clear();
            string targetDeviceName = "";
            receiveStr.Clear();

            targetDeviceName = FindDeviceName(discoveredList, ReportDeviceNodeId);

            if (targetDeviceName == null)
            {
                Log("Target Device Name is null, Client Closed", LogLevels.Fail);
                Thread.Sleep(3000);
            }
            else
            {
                if (!clientIsStart)
                {
                    RunReferenceClient(ZipController);
                    Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
                }
                ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshLifeLineCMD.LifeLine(targetDeviceName, ReciveDeviceNodeId.ToString("X2")).Command);
                Thread.Sleep(5000);
                ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
                Thread.Sleep(2000);

                while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
                {
                    Console.WriteLine("Reader: " + strLine);
                    receiveStr.Add(strLine);
                }
                Thread.Sleep(3000);
            }
        }
        public List<string> List(Alias ZipContoller)
        {
            bool contentIsFind = false;
            discoveredList.Clear();

            if (!clientIsStart)
            {
                RunReferenceClient(ZipContoller);
                Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
            }

            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshListCMD.DiscoverList.Command);
            Thread.Sleep(5000);
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
            Thread.Sleep(2000);

            while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
            {
                Console.WriteLine("Reader: " + strLine);
                if (strLine.Contains("List of discovered Z/IP services"))
                {
                    contentIsFind = true;
                }
                if (contentIsFind == true)
                {
                    if (!strLine.Contains("(ZIP)") && !strLine.Contains("List of discovered Z/IP services") && !strLine.Contains("pi@raspberrypi")
                        && strLine != "")
                    {
                        discoveredList.Add(strLine);
                    }
                }
            }
            contentIsFind = false;
            Thread.Sleep(3000);

            if (discoveredList != null)
            {
                Log(SysLog + "List Report Printed.", LogLevels.Ok);
            }
            else
            {
                Log(SysLog + "List Report doesn't Printed.", LogLevels.Fail);
            }

            return discoveredList;
        }
        public void NodeInfo(Alias ZipController, byte ControllerNodeId, byte TargetNodeId)
        {
            string ControllerDeviceName = "";
            List<string> discoveredList = new List<string>() { };
            receiveStr.Clear();

            discoveredList = List(ZipController);
            ControllerDeviceName = FindDeviceName(discoveredList, ControllerNodeId);

            if (ControllerDeviceName == null)
            {
                Log("Target Device Name is null, Client Closed", LogLevels.Fail);
                Thread.Sleep(3000);
            }
            else
            {
                if (!clientIsStart)
                {
                    RunReferenceClient(ZipController);
                    Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
                }

                ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshNodeInfo.NodeInfo(ControllerDeviceName, TargetNodeId).Command);
                Thread.Sleep(10000);
                ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
                Thread.Sleep(2000);

                while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
                {
                    Console.WriteLine("Reader: " + strLine);
                    receiveStr.Add(strLine);
                }
                Thread.Sleep(3000);
            }
        }
        public ReciveDataPacket RemoveNode(Alias ZipController, Alias Device)
        {
            receiveStr.Clear();
            List<string> node_remove_status = new List<string>()
            {
                "NODE_REMOVE_STATUS_DONE",
                "NODE_REMOVE_STATUS_FAILED"};

            if (!clientIsStart)
            {
                RunReferenceClient(ZipController);
                Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
            }

            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshRemoveNodeCMD.RemoveNode.Command);
            Thread.Sleep(3000);

            Device node = _deviceManager.GetDevice(Device);

            ZWave.Enums.LearnModes lmode = ZWave.Enums.LearnModes.LearnModeClassic;
            if ((lmode & ZWave.Enums.LearnModes.LearnModeSmartStart) != ZWave.Enums.LearnModes.LearnModeSmartStart)
            {
                _deviceManager.DisableSmartStart(Device);
            }
            ActionToken token = node.SetLearnMode(lmode, false, INCLUSION_TIMEOUT_MS, (x) => _deviceManager.EnableSmartStart(Device));
            Thread.Sleep(5000);

            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
            Thread.Sleep(2000);

            while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
            {
                Console.WriteLine("Reader: " + strLine);
                receiveStr.Add(strLine);

                if (strLine.Contains(node_remove_status[0]))
                {
                    Log(node_remove_status[0], LogLevels.Ok);
                }
                else if (strLine.Contains(node_remove_status[1]))
                {
                    Log(node_remove_status[1], LogLevels.Fail);
                }
                else
                {

                }
            }
            Thread.Sleep(3000);
            return ReciveData(new COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V3.NODE_REMOVE_STATUS());
        }
        public ReciveDataPacket RemoveNodeStop(Alias ZipController)
        {
            receiveStr.Clear();
            if (!clientIsStart)
            {
                RunReferenceClient(ZipController);
                Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
            }

            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshRemoveNodeCMD.RemoveNode.Command);
            Thread.Sleep(3000);
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshRemoveNodeCMD.RemoveNodeStop.Command);
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
            Thread.Sleep(2000);

            while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
            {
                Console.WriteLine("Reader: " + strLine);
                receiveStr.Add(strLine);
            }
            Thread.Sleep(3000);
            return ReciveData(new COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V3.NODE_REMOVE_STATUS());
        }
        #endregion

        #region SmartStart Node Provisioning list management
        public List<string> Pl_List(Alias ZipController)
        {
            receiveStr.Clear();
            if (!clientIsStart)
            {
                RunReferenceClient(ZipController);
                Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
            }
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshSmartStartCMD.Pl_List.Command);
            Thread.Sleep(15000);
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
            Thread.Sleep(2000);

            while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
            {
                Console.WriteLine("Reader: " + strLine);
                receiveStr.Add(strLine);
            }
            Thread.Sleep(3000);

            if (receiveStr.Contains(""))
            {

            }
            return FindPlListStr(receiveStr);
        }
        public void Pl_Add_DSK(Alias ZipController, byte[] JoinDsk, string name, string location, string bootmode, string smartstart)
        {
            if (!clientIsStart)
            {
                RunReferenceClient(ZipController);
                Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
            }

            string DskStr = ConvertPlDSK(JoinDsk);
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshSmartStartCMD.Pl_Add_Dsk(DskStr, name, location, bootmode, smartstart).Command);
            Thread.Sleep(5000);
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
            Thread.Sleep(2000);

            while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
            {
                Console.WriteLine("Reader: " + strLine);
            }
            Thread.Sleep(3000);
        }
        public void Pl_Add_DSK(Alias ZipController, byte[] JoinDsk)
        {
            if (!clientIsStart)
            {
                RunReferenceClient(ZipController);
                Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
            }

            string DskStr = ConvertPlDSK(JoinDsk);
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshSmartStartCMD.Pl_Add_Dsk(DskStr).Command);
            Thread.Sleep(5000);
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
            Thread.Sleep(2000);

            while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
            {
                Console.WriteLine("Reader: " + strLine);
            }
            Thread.Sleep(3000);
        }
        public void Pl_Remove_DSK(Alias ZipController, byte[] JoinDsk)
        {
            if (!clientIsStart)
            {
                RunReferenceClient(ZipController);
                Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
            }

            string DskStr = ConvertPlDSK(JoinDsk);

            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshSmartStartCMD.Pl_Remove_Dsk(DskStr).Command);
            Thread.Sleep(5000);
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
            Thread.Sleep(2000);

            while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
            {
                Console.WriteLine("Reader: " + strLine);
            }
            Thread.Sleep(3000);
        }
        public void Pl_Reset(Alias ZipController)
        {
            if (!clientIsStart)
            {
                RunReferenceClient(ZipController);
                Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
            }
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshSmartStartCMD.Pl_Reset.Command);
            Thread.Sleep(15000);
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
            Thread.Sleep(2000);

            while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
            {
                Console.WriteLine("Reader: " + strLine);
            }
            Thread.Sleep(3000);
        }
        #endregion

        #region Sending Data
        public List<string> SendCommand(Alias ZipController, byte TargetNodeId, string CommandClass, string Command)
        {
            string targetDeviceName = "";
            receiveStr.Clear();

            discoveredList = List(ZipController);
            targetDeviceName = FindDeviceName(discoveredList, TargetNodeId);

            if (targetDeviceName == null)
            {
                Log("Target Device Name is null, Client Closed", LogLevels.Fail);
                Thread.Sleep(3000);
            }
            else
            {
                if (!clientIsStart)
                {
                    RunReferenceClient(ZipController);
                    Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
                }

                ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshSendCMD.SendData(targetDeviceName, CommandClass, Command).Command);
                Thread.Sleep(15000);

                ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
                Thread.Sleep(2000);

                while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
                {
                    Console.WriteLine("Reader: " + strLine);
                    receiveStr.Add(strLine);
                }
                Thread.Sleep(3000);
            }
            return receiveStr;
        }
        public List<string> SendCommand(string Data)
        {
            receiveStr.Clear();
            ZipManager._libZwaveIpTransportClient.InnerWriteData(Data);

            while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
            {
                Console.WriteLine("Reader: " + strLine);
                receiveStr.Add(strLine);
            }
            return receiveStr;
        }
        public void Hexsend(Alias ZipController, byte TargetNodeId, byte[] SendDataPacket)
        {
            receiveStr.Clear();
            string targetDeviceAddress = "";
            string sendDataStr = "";

            discoveredList = List(ZipController);
            targetDeviceAddress = FindDeviceAddress(discoveredList, TargetNodeId);
            sendDataStr = GetDataStr(SendDataPacket);

            if (targetDeviceAddress == null)
            {
                Log("Target Device Name is null, Client Closed", LogLevels.Fail);
                Thread.Sleep(3000);
            }
            else
            {
                if (!clientIsStart)
                {
                    RunReferenceClient(ZipController);
                    Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
                }

                ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshSendCMD.HexSendData(targetDeviceAddress, sendDataStr).Command);
                Thread.Sleep(15000);

                ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
                Thread.Sleep(2000);

                while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
                {
                    Console.WriteLine("Reader: " + strLine);
                    receiveStr.Add(strLine);
                }
                Thread.Sleep(3000);
            }
        }
        public ReciveDataPacket ReciveData(byte[] ExpectData)
        {
            ReciveDataPacket myRecive = new ReciveDataPacket(); 
            bool hasExpectData = false;
            List<string> receiveData = receiveStr;
            byte[] ReceiveBytes = new byte[] { };
            ReceiveBytes = FindReceiveBytes(receiveData, ExpectData);
            if (ReceiveBytes.Count()>1)
            {
                if (ReceiveBytes[1] == ExpectData[1])
                {
                    hasExpectData = true;
                }
            }
            
            myRecive.ExpectData = ReceiveBytes;
            myRecive.hasExpectData = hasExpectData;
            return myRecive;
        }        
        #endregion

        private string GetStringGrantkeys(GrantKeyType[] grantkeysArray)
        {   
            int sumKeys = 0;            
            foreach (var key in grantkeysArray)
            {
                sumKeys = sumKeys + ((byte)key);
            }

            return sumKeys.ToString("X2") ;
        }
        public string SetUnsoliToGwCfg(Alias ZipController)
        {
            if (zipUnsolicitedDestinationIp6 != null)
            {
                SetUnsoliToGwCfg(ZipController, zipUnsolicitedDestinationIp6);
            }
            else
            {
                Thread.Sleep(10000);
                SetUnsoliToGwCfg(ZipController, zipUnsolicitedDestinationIp6);
            }
            return zipUnsolicitedDestinationIp6;
        }
        public void StartReferenceListener(Alias ZipController, int TimeOut)
        {
            myListenerAction = () => RunReferenceListener(ZipController, TimeOut);
            myTask = new Task(myListenerAction);
            myTask.Start();
        }
        public void StopReferenceListener()
        {
            if (myTask.IsCompleted)
            {
                myTask.Dispose();
            }
            myTask.Wait();
        }
        public List<byte[]> GetListenerData()
        {
            while (myByteList.Count() == 0)
            {
                Thread.Sleep(5000);
            }
            return myByteList;
        }
        private void RunReferenceListener(Alias ZipController, int DelayTime)
        {
            receiveListener.Clear();
            receiveStr.Clear();
            myByteList.Clear();
            ZipManager._LibZwaveIpListenerTransportClient.InnerWriteData((string)SshBasicCMD.GetBrLanIp.Command);
            Thread.Sleep(5000);
            while ((strLine = ZipManager._LibZwaveIpListenerTransportClient.reader.ReadLine()) != null)
            {
                //Console.WriteLine("Reader: " + strLine);
                Log("Reader: " + strLine, LogLevels.Information);
                receiveStr.Add(strLine);
            }
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.MoveToLibPath.Command);
            Thread.Sleep(200);
            zipUnsolicitedDestinationIp6 = FindGlobalIp6(receiveStr);
            ZipManager._LibZwaveIpListenerTransportClient.InnerWriteData((string)SshBasicCMD.RunLibzwaveLisener(zipUnsolicitedDestinationIp6).Command);

            Thread.Sleep(DelayTime);

            while ((strLine = ZipManager._LibZwaveIpListenerTransportClient.reader.ReadLine()) != null)
            {
                Log("Reader: " + strLine, LogLevels.Information);
                receiveListener.Add(strLine);
            }
            isListening = true;
            myByteList = FindReceiveLisenerBytes(receiveListener);
            //return isListening;
        }
        private String GwIp6Address(Alias ZipController)
        {
            ZipDevice controller = ZipManager.GetDevice(ZipController);
            IPAddress GwAddress = IPAddress.Parse(controller.DataSource.SourceName);
            gwIp6Address = GwAddress.ToString();
            return gwIp6Address;
        }
        private bool RunReferenceClient(Alias ZipController)
        {
            bool clientIsStart = false;
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.RemoveLogFile.Command);
            Thread.Sleep(200);
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.MoveToLibPath.Command);
            Thread.Sleep(200);
            if (gwIp6Address == null)
            {
                gwIp6Address = GwIp6Address(ZipController);
                Log(SysLog + "Get GW IPv6 Address: " + gwIp6Address, LogLevels.Text);
            }
            ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.RunLibzwaveClient(gwIp6Address).Command);
            Thread.Sleep(200);
            clientIsStart = true;
            return clientIsStart;
        }
        private string ConvertDSK(byte[] DskByteArray)
        {
            string strDsk1 = BitConverter.ToString(DskByteArray);
            string repStr = strDsk1.Replace("-", "");
            int decValue = int.Parse(repStr, System.Globalization.NumberStyles.HexNumber);
            string ConverDec = Convert.ToDecimal(decValue).ToString();
            return ConverDec;
        }
        private string ConvertPlDSK(byte[] DskByteArray)
        {
            //string strDsk1 = BitConverter.ToString(DskByteArray);
            string strDsk = "";
            List<string> dskByteList = new List<string>() { };

            for (int i = 0; i < DskByteArray.Length - 1; i += 2)
            {
                var dskInt = (DskByteArray[i] << 8 | DskByteArray[i + 1]);
                string dskDec = Convert.ToDecimal(dskInt).ToString();
                dskByteList.Add(dskDec);
                dskByteList.Add("-");
            }

            dskByteList.RemoveAt(dskByteList.Count - 1);

            foreach (var str in dskByteList)
            {
                strDsk += str;
            }
            return strDsk;
        }
        private string GetUntilOrEmpty(string text, string stopAt = "]")
        {
            text = text.Substring(1);
            if (!String.IsNullOrWhiteSpace(text))
            {
                int charLocation = text.IndexOf(stopAt, StringComparison.Ordinal);

                if (charLocation > 0)
                {
                    return text.Substring(0, charLocation) + "]";
                }
            }
            return String.Empty;
        }
        private string GetIp4(string text)
        {            
            text = text.Substring(text.LastIndexOf(':') + 1);
            return text;
        }
        public static string GetDataStr(byte[] SendDataPacket)
        {
            string sendDataStr = BitConverter.ToString(SendDataPacket);

            StringBuilder sb = new StringBuilder();
            foreach (char c in sendDataStr)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
        private string FindDeviceAddress(List<string> deviceName, int deviceID)
        {
            string TargetName = "";
            var regexPattern = @"\b-\S*" + deviceID.ToString() + @"-\b";
            bool isFind = false;

            foreach (var name in deviceName)
            {
                if (Regex.IsMatch(name, regexPattern))
                {
                    TargetName = name;
                    isFind = true;
                }
            }
            if (isFind == false)
            {
                TargetName = null;
                Log("Can't find expect Target Name in List", LogLevels.Error);
            }
            TargetName = GetIp4(TargetName);

            return TargetName;
        }
        private string FindDeviceName(List<string> deviceName, int deviceID)
        {
            string TargetName = "";
            var regexPattern = @"\b-\S*" + deviceID.ToString() + @"-\b";
            bool isFind = false;

            foreach (var name in deviceName)
            {
                if (Regex.IsMatch(name, regexPattern))
                {
                    TargetName = name;
                    isFind = true;
                }
            }
            if (isFind == false)
            {
                TargetName = null;
                Log("Can't find expect Target Name in List", LogLevels.Error);
            }
            TargetName = GetUntilOrEmpty(TargetName);

            return TargetName;
        }
        private List<string> FindPlListStr(List<string> ReceiveStrings)
        {
            List<string> myPlListStr = new List<string>();
            bool isHere = false;

            foreach (var receiveStr in ReceiveStrings)
            {
                if (receiveStr.Contains(Pl_List_Class.Title2))
                {
                    myPlListStr.Add(Pl_List_Class.Title2);                    
                }
                else if (receiveStr.Contains(Pl_List_Class.Title1))
                {
                    isHere = true;
                }

                if (isHere == true)
                {
                    myPlListStr.Add(receiveStr);
                }
            }
           
            return myPlListStr;    
        }
        private byte[] FindReceiveBytes(List<string> ReceiveStrings, byte[] TargetCommandClass)
        {
            string regPattern = @"bytestream:.(.*)";
            byte[] myBytes = new byte[] { };
            string commandCheck = "";
            commandCheck = TargetCommandClass[0].ToString("X2").ToLower() + " " + TargetCommandClass[1].ToString("X2").ToLower();

            foreach (var receiveStr in ReceiveStrings)
            {
                if (receiveStr.Contains("bytestream") && receiveStr.Contains(commandCheck.Substring(0, 4)))
                {
                    var catchString = Regex.Match(receiveStr, regPattern);
                    string matchValue = catchString.Groups[1].Value;

                    matchValue = matchValue.Replace(" ", string.Empty);
                    int NumberChars = matchValue.Length;
                    myBytes = new byte[NumberChars / 2];

                    for (int i = 0; i < NumberChars; i += 2)
                    {
                        myBytes[i / 2] = Convert.ToByte(matchValue.Substring(i, 2), 16);
                    }
                }
            }
            return myBytes;
        }
        private List<byte[]> FindReceiveLisenerBytes(List<string> ReceiveStrings)
        {
            string regPattern = @"bytestream:.(.*)";
            byte[] myBytes = new byte[] { };
            List<byte[]> myBytesArray = new List<byte[]> { };

            foreach (var receiveStr in ReceiveStrings)
            {
                if (receiveStr.Contains("bytestream"))
                {
                    var catchString = Regex.Match(receiveStr, regPattern);
                    string matchValue = catchString.Groups[1].Value;

                    matchValue = matchValue.Replace(" ", string.Empty);
                    int NumberChars = matchValue.Length;
                    myBytes = new byte[NumberChars / 2];

                    for (int i = 0; i < NumberChars; i += 2)
                    {
                        myBytes[i / 2] = Convert.ToByte(matchValue.Substring(i, 2), 16);
                    }
                    myBytesArray.Add(myBytes);
                }
            }
            return myBytesArray;
        }
        private string FindGlobalIp6(List<string> ReceiveStrings)
        {
            string globalIp6 = "";
            string tmpStr = "";
            string regPattern = @"inet6 (.*)\/";
            foreach (var str in ReceiveStrings)
            {
                if (str.Contains("inet6"))
                {
                    tmpStr = str;
                }
            }
            var catchString = Regex.Match(tmpStr, regPattern);
            string matchValue = catchString.Groups[1].Value;
            return globalIp6 = matchValue;
        }
        private void SetUnsoliToGwCfg(Alias ZipController, string ZipUnsoliDestinationIp)
        {
            string targetDeviceName = "";
            targetDeviceName = FindDeviceName(discoveredList, 01);
            Thread.Sleep(5000);

            if (targetDeviceName == null)
            {
                Log("Target Device Name is null, Client Closed", LogLevels.Fail);
                Thread.Sleep(3000);
            }
            else
            {
                receiveListener.Clear();
                if (!clientIsStart)
                {
                    RunReferenceClient(ZipController);
                    Log(SysLog + "Excute RunReferenceClient function.", LogLevels.Text);
                }
                ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.SetGwUnsoliCfg(targetDeviceName, ZipUnsoliDestinationIp).Command);
                Thread.Sleep(15000);
                ZipManager._libZwaveIpTransportClient.InnerWriteData((string)SshBasicCMD.ExitReferenceClient.Command);
                Thread.Sleep(2000);

                while ((strLine = ZipManager._libZwaveIpTransportClient.reader.ReadLine()) != null)
                {
                    Log("Reader: " + strLine, LogLevels.Information);
                    receiveStr.Add(strLine);
                }
                Thread.Sleep(3000);
            }            
        }
    }

    public class Pl_List_Class
    {
        public const string Title1 = "SmartStart Node provisioning list:";
        public const string Title2 = "The SmartStart Node provisioning list is empty";

        public string dsk { get; set; }
        public string Bootstrapping_mode { get; set; }
        public string SmartStart_inclusion_setting { get; set; }
        public string Network_status { get; set; }
        public string Node_name { get; set; }
        public string Node_location { get; set; }

    }
    public class ReciveDataPacket
    {
        #region Private Variables
        private byte[] bytesPacket;
        private bool hasPacket;
        #endregion
        public byte[] ExpectData
        {
            get { return bytesPacket; }
            set { bytesPacket = value; }
        }
        public bool hasExpectData
        {
            get { return hasPacket; }
            set { hasPacket = value; }
        }
    }
}
