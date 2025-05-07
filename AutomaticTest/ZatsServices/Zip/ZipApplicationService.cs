using System.Collections.Generic;
using Utils.UI.Enums;
using System.Net;
using System.Net.Sockets;
using ZWave;
using ZWave.ZipApplication.Devices;
using ZWave.ZipApplication.Operations;
using System;
using System.Threading;
using System.Linq;

namespace Zats.Services
{
    public class ZipApplicationService : ServiceBase, IZipApplicationService
    {
        Dictionary<int, ActionToken> testResults = new Dictionary<int, ActionToken>();
        readonly IPAddress PortalDefaultAddress = IPAddress.Parse("fd00:1111::10");
        readonly int RECONNET_ATTEMPT_COUNT = 1;

        private string gwHostLogin = "pi:raspberry";
        private string gwHostExecuteGW = "sudo /etc/init.d/zipgateway restart";
        private string gwHostProtocol = "sftp";
        private string gwHostWorkspace = "/home/pi/";
        private string gwHostStart = "sudo /etc/init.d/zipgateway start";
        private string gwHostStop = "sudo /etc/init.d/zipgateway stop";
        private string gwHostSUDO = "sudo";

        public IProvisioningList ProvisioningList 
        {
            get { return ZipManager.ProvisioningList; }
            
        }
        private ZipManager ZipManager { get; set; }

        public ZipApplicationService(ZipManager zipManager)
        {
            ZipManager = zipManager;
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            ZipManager.Log(text, logLevel);
        }

        public void StartBasicTest(Alias virtualDeviceNumber)
        {
            ZipDevice device = ZipManager.GetDevice(virtualDeviceNumber);
            StartTrafficTest(virtualDeviceNumber, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT(), 100, 20000);
        }

        public void StartBasicTest(Alias virtualDeviceNumber, int intervalMs)
        {
            ZipDevice device = ZipManager.GetDevice(virtualDeviceNumber);
            StartTrafficTest(virtualDeviceNumber, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT(), intervalMs, 20000);
        }

        public ZipResult.Test GetBasicTestResult(Alias virtualDeviceNumber)
        {
            return GetTrafficTestResult(virtualDeviceNumber);
        }

        public ZipResult.Test StopBasicTest(Alias virtualDeviceNumber)
        {
            return StopTrafficTest(virtualDeviceNumber);
        }

        public ZipResult.Test StopBasicTestNoResult(Alias virtualDeviceNumber)
        {
            return StopTrafficTestNoResult(virtualDeviceNumber);
        }

        public void StartTrafficTest(Alias virtualDeviceNumber, byte[] cmd, int intervalMs)
        {
            StartTrafficTest(virtualDeviceNumber, cmd, null, intervalMs, 0);
        }

        public void StartTrafficTest(Alias virtualDeviceNumber, byte[] cmd, byte[] expectedCmd, int intervalMs, int timeoutMs)
        {
            ZipDevice device = ZipManager.GetDevice(virtualDeviceNumber);
            ActionToken token = device.NoiseData(null, cmd, expectedCmd, intervalMs, timeoutMs);

            if (testResults.ContainsKey(virtualDeviceNumber.Value))
            {
                testResults[virtualDeviceNumber.Value].SetCompleted();
                testResults.Remove(virtualDeviceNumber.Value);
            }
            testResults.Add(virtualDeviceNumber.Value, token);
        }

        public ZipResult.Test GetTrafficTestResult(Alias virtualDeviceNumber)
        {
            ZipResult.Test ret = new ZipResult.Test();
            if (testResults.ContainsKey(virtualDeviceNumber.Value))
            {
                NoiseDataResult res = (NoiseDataResult)testResults[virtualDeviceNumber.Value].Result;
                ret.Total = res.TotalCount;
                ret.Fails = res.FailCount;
            }
            return ret;
        }

        public ZipResult.Test StopTrafficTest(Alias virtualDeviceNumber)
        {
            ZipResult.Test ret = new ZipResult.Test();
            if (testResults.ContainsKey(virtualDeviceNumber.Value))
            {
                testResults[virtualDeviceNumber.Value].SetCompleted();
                NoiseDataResult res = (NoiseDataResult)testResults[virtualDeviceNumber.Value].WaitCompletedSignal();
                ret.Total = res.TotalCount;
                ret.Fails = res.FailCount;
            }
            return ret;
        }

        public ZipResult.Test StopTrafficTestNoResult(Alias virtualDeviceNumber)
        {
            ZipResult.Test ret = new ZipResult.Test();
            if (testResults.ContainsKey(virtualDeviceNumber.Value))
            {
                testResults[virtualDeviceNumber.Value].SetCompleted();
            }
            return null;
        }

        public void AddUnsolResponse(Alias UnsoldeviceNumber, byte[] headerExtension, byte[] cmd, byte[] expectedCmd)
        {
            ZipDevice zipdevice = ZipManager.GetDevice(UnsoldeviceNumber);
            zipdevice.ResponseData(null, cmd, expectedCmd);
        }

        public ZipToken.Void AddUnsolResponse(Alias UnsoldeviceNumber, byte[] cmd, byte[] expectedCmd)
        {
            ZipDevice zipdevice = ZipManager.GetDevice(UnsoldeviceNumber);
            ActionToken ResponseToken = zipdevice.ResponseData(null, cmd, expectedCmd);
            return new ZipToken.Void(ResponseToken);
        }

        public void AddUnsolResponse(Alias UnsoldeviceNumber, byte[] headerExtension, byte[] cmd, byte[] expectedCmd, int NumOfBytesToCompare)
        {
            ZipDevice zipdevice = ZipManager.GetDevice(UnsoldeviceNumber);
            zipdevice.ResponseData(null, cmd, expectedCmd, NumOfBytesToCompare);
        }

        public ZipToken.Void AddUnsolResponse(Alias UnsoldeviceNumber, byte[] headerExtension, Func<byte[], byte[], byte[]> responseCallBack,
            byte[] expectedCmd)
        {
            ZipDevice zipdevice = ZipManager.GetDevice(UnsoldeviceNumber);
            ActionToken ResponseToken = zipdevice.ResponseData(headerExtension, responseCallBack, expectedCmd);
            return new ZipToken.Void(ResponseToken);
        }

        public ZipToken.Void AddUnsolResponse(Alias UnsoldeviceNumber, bool isNoAck, byte[] headerExtension, Func<byte[], byte[], byte[]> responseCallBack,
            byte[] expectedCmd)
        {
            ZipDevice zipdevice = ZipManager.GetDevice(UnsoldeviceNumber);
            ActionToken ResponseToken = zipdevice.ResponseData(headerExtension, responseCallBack, expectedCmd, isNoAck);
            return new ZipToken.Void(ResponseToken);
        }

        private static IPEndPoint QueryRoutingInterface(
          Socket socket,
          IPEndPoint remoteEndPoint)
        {
            SocketAddress address = remoteEndPoint.Serialize();

            byte[] remoteAddrBytes = new byte[address.Size];
            for (int i = 0; i < address.Size; i++)
            {
                remoteAddrBytes[i] = address[i];
            }

            byte[] outBytes = new byte[remoteAddrBytes.Length];
            try
            {
                socket.IOControl(
                            IOControlCode.RoutingInterfaceQuery,
                            remoteAddrBytes,
                            outBytes);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.InnerException);
            }

            for (int i = 0; i < address.Size; i++)
            {
                address[i] = outBytes[i];
            }

            EndPoint ep = remoteEndPoint.Create(address);
            return (IPEndPoint)ep;
        }

        public bool AutoConfigUnsolicitedDestination(Alias ZipDeviceNumber, Alias ZipUnsolDevice)
        {
            #region Ip Discovery service

            bool IsSuccess = false;
            byte[] unsIPv6addr = null;
            
            ZipDevice controller = ZipManager.GetDevice(ZipDeviceNumber);

            //The IP address of the local PC's interface connected to Z/IP GW
            //GetIpAddressResult res = controller.GetIpAddress(0x01);
            //ZipResult.GetIpAddress GwAddresInfo = new ZipResult.GetIpAddress(res);
            IPAddress GwAddress = IPAddress.Parse(controller.DataSource.SourceName);

            Socket socket = null;

            if (GwAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                socket = new Socket(
                    AddressFamily.InterNetworkV6,
                    SocketType.Dgram,
                    ProtocolType.Udp);
            }
            else
            {
                socket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Dgram,
                    ProtocolType.Udp);
            }

            IPEndPoint RemoteEndPoint = new IPEndPoint(GwAddress, controller.Port);
            
            IPEndPoint localEndPoint = QueryRoutingInterface(socket, RemoteEndPoint);

            unsIPv6addr = localEndPoint.Address.GetAddressBytes();

            #endregion

            #region Unsolicited Ip Destination Setup

            //Also Verify that is correctly setted up

            ushort newUnsolPort = (ushort)controller.Port;
            byte[] newUnsPort = BitConverter.GetBytes(newUnsolPort);
            Array.Reverse(newUnsPort);
            IPEndPoint unsolEndPoint = new IPEndPoint(IPAddress.IPv6Any, newUnsolPort);
            byte[] AckUdpPack = new byte[] { 0x23, 0x02, 0x40, 0, 0, 0, 0 };

            //Setting a Unsolicited Destination

            byte[] Ipv4onIpv6format = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xff, 0xff,
                        unsIPv6addr[0], unsIPv6addr[1], unsIPv6addr[2], unsIPv6addr[3]};

            if (GwAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                SendData(ZipDeviceNumber, null, new COMMAND_CLASS_ZIP_GATEWAY.UNSOLICITED_DESTINATION_SET()
                {
                    unsolicitedIpv6Destination = unsIPv6addr,
                    unsolicitedDestinationPort = newUnsPort
                }, false, 1000);
            }
            else
            {
                SendData(ZipDeviceNumber, null, new COMMAND_CLASS_ZIP_GATEWAY.UNSOLICITED_DESTINATION_SET()
                {
                    unsolicitedIpv6Destination = Ipv4onIpv6format,
                    unsolicitedDestinationPort = newUnsPort
                }, false, 1000);
            }

            //Verifing new Unsolicited IPv6 address for Z/IP GW
            ZipToken.ExpectData token = new ZipToken.ExpectData(controller.ExpectData(
                new COMMAND_CLASS_ZIP_GATEWAY.UNSOLICITED_DESTINATION_REPORT(), 5000, null));
            SendData(ZipDeviceNumber, null, new COMMAND_CLASS_ZIP_GATEWAY.UNSOLICITED_DESTINATION_GET(), false, 1000);
            ZipResult.ExpectData expectedResult = token.WaitCompletedSignal();
            if (expectedResult)
            {
                COMMAND_CLASS_ZIP_GATEWAY.UNSOLICITED_DESTINATION_REPORT result = expectedResult.ReceivedData;

                if (GwAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    if (result.unsolicitedIpv6Destination.SequenceEqual(unsIPv6addr) &&
                   result.unsolicitedDestinationPort.SequenceEqual(newUnsPort))
                    {
                        IsSuccess = true;
                        unsolEndPoint = new IPEndPoint(controller.IpAddress, controller.Port);
                        bool isDtls = !(unsolEndPoint.Port == 4123);

                        //ZipManager.NewVirtualDevice(ZipUnsolDevice, unsolEndPoint, isDtls);                        
                        ZipManager.NewUnsolicitedDevice(ZipUnsolDevice, unsolEndPoint.Address, isDtls);
                        ZipManager.CreateUnsolicitedClient(unsolEndPoint.Address.ToString(), (ushort)unsolEndPoint.Port);
                        //ZipManager.RegisterUnsolicitedListener(ZipUnsolDevice, unsolEndPoint.Port, 
                        //    "123456789012345678901234567890AA", unsolEndPoint.Address);
                    }
                }
                else
                {
                    if (result.unsolicitedIpv6Destination.SequenceEqual(Ipv4onIpv6format) &&
                   result.unsolicitedDestinationPort.SequenceEqual(newUnsPort))
                    {
                        IsSuccess = true;
                        unsolEndPoint = new IPEndPoint(controller.IpAddress, controller.Port);
                        bool isDtls = !(unsolEndPoint.Port == 4123);
                        //ZipManager.NewVirtualDevice(ZipUnsolDevice, unsolEndPoint, isDtls);
                        ZipManager.NewUnsolicitedDevice(ZipUnsolDevice, unsolEndPoint.Address, isDtls);
                        ZipManager.CreateUnsolicitedClient(unsolEndPoint.Address.ToString(), (ushort)unsolEndPoint.Port);
                    }
                }
            }

            return IsSuccess;

            #endregion
        }

        public ZipToken.Void[] AddUnsolInclusionSupport(Alias ZipUnsol, byte[] dsk)
        {
            ZipToken.Void returnToken_Keys = AddUnsolResponse(ZipUnsol, null, (x, y) =>
            {

                COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V3.NODE_ADD_KEYS_REPORT keysReport =
                (COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V3.NODE_ADD_KEYS_REPORT)y;

                COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V3.NODE_ADD_KEYS_SET keysSet =
                    new COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V3.NODE_ADD_KEYS_SET();

                keysSet.grantedKeys = keysReport.requestedKeys;
                keysSet.properties1.accept = 0x01;

                return keysSet;
            }, new COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V3.NODE_ADD_KEYS_REPORT());

            ZipToken.Void returnToken_Dsk = AddUnsolResponse(ZipUnsol, null, (x, y) =>
            {

                COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V3.NODE_ADD_DSK_REPORT dskReport =
                (COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V3.NODE_ADD_DSK_REPORT)y;

                COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V3.NODE_ADD_DSK_SET dskSet =
                    new COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V3.NODE_ADD_DSK_SET();

                dskSet.inputDsk = new List<byte> { dsk[0], dsk[1] };
                dskSet.properties1.accept = 0x01;
                dskSet.properties1.inputDskLength = 0x02;

                return dskSet;
            }, new COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V3.NODE_ADD_DSK_REPORT());

            return new ZipToken.Void[] { returnToken_Keys, returnToken_Dsk };
        }        

        private ZipResult.SendData SendData(Alias zipReceiverNumber, byte[] headerExtension, byte[] packet, bool isValidateAckSeqNo, int timeout)
        {
            ZipDevice zipDevice = ZipManager.GetDevice(zipReceiverNumber);

            int attemptNo = 0;
            SendDataResult sendAction() => zipDevice.SendData(headerExtension, packet, isValidateAckSeqNo, timeout);

            SendDataResult actionRes = null;
            bool actionResSucceeded = false;
            do
            {
                attemptNo++;
                actionRes = sendAction();
                actionResSucceeded = actionRes && actionRes.IsAckReceived;
                if (!actionResSucceeded)
                {
                    Log(string.Format(
                            "{0}, device alias: {1}, reconnection attempt: {2}",
                            !actionRes ? "Connection fault" : "Send data ACK timeout",
                            zipReceiverNumber.ToString(),
                            attemptNo),
                        LogLevels.Warning);
                    ZipManager.ReconnectVirtualDevice(zipReceiverNumber);
                }
            }
            while (!actionResSucceeded && attemptNo < RECONNET_ATTEMPT_COUNT);

            return new ZipResult.SendData(actionRes);
        }

        public void GetFileFromGwHost(string fileName, string zipHostAddress, string downloadPath)
        {
            string command = " /nointeractiveinput /command \"open "+gwHostProtocol+"://"+gwHostLogin+"@" + 
                zipHostAddress + "/\"  \"get "+ gwHostWorkspace + fileName + " " + downloadPath + "\"  \"exit\"";

            System.Diagnostics.Process cmd = new System.Diagnostics.Process();
            cmd.StartInfo.FileName = "\"C:\\Program Files (x86)\\WinSCP\\WinSCP.com\"";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;
            cmd.Start();
            Console.WriteLine(cmd.StandardOutput.ReadToEnd());
            cmd.WaitForExit();
            cmd.StandardInput.Close();
            cmd.Close();
        }
        public void SetFileFromGwHost(string zipHostAddress, string downloadPath)
        {
            string command = " /nointeractiveinput /command \"open "+gwHostProtocol+"://"+gwHostLogin+"@" + zipHostAddress + 
                "/\"  \"put " + " " + downloadPath + " " + gwHostWorkspace + "\"  \"exit\"";

            System.Diagnostics.Process cmd = new System.Diagnostics.Process();
            cmd.StartInfo.FileName = "\"C:\\Program Files (x86)\\WinSCP\\WinSCP.com\"";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;
            cmd.Start();
            Console.WriteLine(cmd.StandardOutput.ReadToEnd());
            cmd.WaitForExit();
            cmd.StandardInput.Close();
            cmd.Close();
        }
        public void UploadNvmToGwHost(string backUpPath, string startingVersion, 
            string endingVersion, string zipHostAddress, string hexFileName, string jsonFileName)
        {
            string debug1 = "\"call echo ----------TRANSFORMING THE HEX FILE IN TO A BINARY FILE---------- \" ";
            string debug2 = "\"call echo ----------EXPORTING THE OLD " + startingVersion + " TO " + jsonFileName + " ----------\" ";
            string debug3 = "\"call echo ----------IMPORTING THE " + jsonFileName + " TO " + endingVersion + " ----------\" ";
            string debug4 = "\"call echo ---------- WRITTING THE UPGRADED NVM TO GW MODULE ----------\" ";

            string command = " /nointeractiveinput /command \"open "+gwHostProtocol+"://"+gwHostLogin+"@" + zipHostAddress + 
                "/\" \"call "+gwHostStop+ "\" " +
                " \"put " + backUpPath + " "+ gwHostWorkspace + hexFileName + "\" "+
                debug1+
                " \"call "+ gwHostSUDO +" objcopy -I ihex -O binary " + gwHostWorkspace + hexFileName + " "+ gwHostWorkspace + "backUpBig\" "+
                debug2 +
                " \"call " + gwHostSUDO + " dd if=" + gwHostWorkspace + "backUpBig "+ " of=" + gwHostWorkspace + "backUp " + " bs=1024 count=16\" " +
                " \"call " + gwHostSUDO + " sleep 50\" " +
                //"  \"call " + gwHostSUDO + " /usr/local/bin/zw_nvm_converter -e " + startingVersion + " "+gwHostWorkspace+ "backUp " + gwHostWorkspace + 
                //jsonFileName + "\" "
                //+ debug3 +
                //" \"call " + gwHostSUDO + " /usr/local/bin/zw_nvm_converter -i " + endingVersion + " " + gwHostWorkspace + jsonFileName + " " + gwHostWorkspace + "UpgradedNVM\" " +
                //debug4+
                //" \"call " + gwHostSUDO + " zw_programmer -s /dev/ttyACM0 -w " + gwHostWorkspace + "UpgradedNVM\" " +
                //" \"call " + gwHostSUDO + " sleep 20\" " +
                " \"exit\" ";
            ReportService.AddLog(command, LogLevels.Information);            

            System.Diagnostics.Process cmd = new System.Diagnostics.Process();
            cmd.StartInfo.FileName = "\"C:\\Program Files (x86)\\WinSCP\\WinSCP.com\"";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;
            cmd.Start();
            ReportService.AddLog(cmd.StandardOutput.ReadToEnd(), LogLevels.Information);
            cmd.WaitForExit();
            cmd.StandardInput.Close();
            cmd.Close();
        }

        public void UploadFileToGwHost(string filePath, string zipHostAddress)
        {
            string command = " /nointeractiveinput /command \"open " + gwHostProtocol + "://" + gwHostLogin + "@" + zipHostAddress +
                "/\" " +
                " \"put " + filePath + " " + gwHostWorkspace + "backup.json" + "\" " +
                " \"exit\" ";
            ReportService.AddLog(command, LogLevels.Information);

            System.Diagnostics.Process cmd = new System.Diagnostics.Process();
            cmd.StartInfo.FileName = "\"C:\\Program Files (x86)\\WinSCP\\WinSCP.com\"";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;
            cmd.Start();
            ReportService.AddLog(cmd.StandardOutput.ReadToEnd(), LogLevels.Information);
            cmd.WaitForExit();
            cmd.StandardInput.Close();
            cmd.Close();
        }

        public void UploadNvmToGwHostWithSmallBinary(string backUpPath, string startingVersion,
            string endingVersion, string zipHostAddress, string hexFileName, string jsonFileName)
        {
            string debug1 = "\"call echo ----------TRANSFORMING THE HEX FILE IN TO A BINARY FILE---------- \" ";
            string debug2 = "\"call echo ----------EXPORTING THE OLD " + startingVersion + " TO " + jsonFileName + " ----------\" ";
            string debug3 = "\"call echo ----------IMPORTING THE " + jsonFileName + " TO " + endingVersion + " ----------\" ";
            string debug4 = "\"call echo ---------- WRITTING THE UPGRADED NVM TO GW MODULE ----------\" ";

            string command = " /nointeractiveinput /command \"open " + gwHostProtocol + "://" + gwHostLogin + "@" + zipHostAddress +
                "/\" \"call " + gwHostStop + "\" " +
                "  \"call " + gwHostSUDO + " /usr/local/bin/zw_nvm_converter -e " + startingVersion + " " + gwHostWorkspace + "backUp " + gwHostWorkspace +
                jsonFileName + "\" "
                + debug3 +
                " \"call " + gwHostSUDO + " /usr/local/bin/zw_nvm_converter -i " + endingVersion + " " + gwHostWorkspace + jsonFileName + " " + gwHostWorkspace + "UpgradedNVM\" " +
                debug4 +
                " \"call " + gwHostSUDO + " /usr/local/bin/zw_programmer -s /dev/ttyACM0 -w " + gwHostWorkspace + "UpgradedNVM\" " +
                " \"call " + gwHostSUDO + " sleep 20\" " +
                " \"exit\" ";
            ReportService.AddLog(command, LogLevels.Information);

            System.Diagnostics.Process cmd = new System.Diagnostics.Process();
            cmd.StartInfo.FileName = "\"C:\\Program Files (x86)\\WinSCP\\WinSCP.com\"";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;
            cmd.Start();
            ReportService.AddLog(cmd.StandardOutput.ReadToEnd(), LogLevels.Information);
            cmd.WaitForExit();
            cmd.StandardInput.Close();
            cmd.Close();
        }

        public void ImportGW( string target_firmware, string protocol_name, string zipHostAddress, string jsonFileName)
        {
            Console.WriteLine("------Copy GWJson.json and protocol firmware to your raspberry /home/pi------");
            //string debug1 = "\"call echo ----------Import GW file---------- \" ";  

            string command = " /nointeractiveinput /command \"open "+gwHostProtocol+"://"+gwHostLogin+"@" + zipHostAddress +
                "/\" \"call "+gwHostStop+ "\" " +
                //"\"call sudo /usr/local/bin/zgw_import.sh -s\" \"exit\"";
                "\"call " + gwHostSUDO + " /usr/local/bin/zgw_import.sh -s /dev/ttyACM0 -f " + gwHostWorkspace + target_firmware + " -p " + 
                protocol_name + " -j " + gwHostWorkspace + jsonFileName + "\" " +
                "\"call " + gwHostSUDO + " sleep 20\"  \"exit\"";

            Console.WriteLine(command);
            //" \"call sudo /usr/local/bin/zgw_import.sh -s /dev/ttyACM0 -f /home/pi/" + target_firmware + " -p " + protocol_name + " -j /home/pi/ " + jsonFileName;
            //" \"call sudo sleep 20\" \"call sudo /etc/init.d/zipgateway restart\" \"exit\"";            

            ReportService.AddLog(command, LogLevels.Information);

            System.Diagnostics.Process cmd = new System.Diagnostics.Process();
            cmd.StartInfo.FileName = "\"C:\\Program Files (x86)\\WinSCP\\WinSCP.com\"";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;

            ZipManager.MessageBox.Show("change to a new bridge device and press any key to continue...", "User input", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);

            //cmd.StartInfo.Arguments = command2;
            cmd.Start();
            Console.WriteLine(cmd.StandardOutput.ReadToEnd());
            ReportService.AddLog(cmd.StandardOutput.ReadToEnd(), LogLevels.Information);
            cmd.WaitForExit();
            cmd.StandardInput.Close();
            cmd.Close();
        }
        public void InstallGW( string Target_File_Name, string zipHostAddress, string Target_Path)
        {
            gwHostLogin = "pi:raspberry";
            gwHostExecuteGW = "sudo /etc/init.d/zipgateway restart";
            gwHostProtocol = "sftp";
            gwHostWorkspace = "/home/pi/";
            gwHostStart = "sudo /etc/init.d/zipgateway start";
            gwHostStop = "sudo /etc/init.d/zipgateway stop";
            gwHostSUDO = "sudo";

            //string command = " /nointeractiveinput /command \"open " + gwHostProtocol + "://" + gwHostLogin + "@" + zipHostAddress +
            //    "/\" \"call " + gwHostStop + "\" " +
            //    " \"put " + Target_Path + " " + gwHostWorkspace + Target_File_Name + "\" " +               
            //    " \"call " + " cd " + gwHostWorkspace + "\" " +
            //    " \"call " + gwHostSUDO + " dpkg --purge zipgateway\" " +
            //    " \"call " + gwHostSUDO + " sleep 50\" " +
            //    " \"call " + gwHostSUDO + " dpkg -i "+ Target_File_Name + "\" " +
            //    " \"exit\" ";
            string command = " /nointeractiveinput " +
                " /command \"option batch abort\" \"option confirm off\" \"open " + gwHostProtocol + "://" + gwHostLogin + "@" + zipHostAddress +
                "/\" \"call " + gwHostStop + "\" " +
                " \"put " + Target_Path + " " + gwHostWorkspace + Target_File_Name + "\" " +
                " \"call " + " cd " + gwHostWorkspace + "\" " +
                " \"call " + gwHostSUDO + " dpkg -i " + Target_File_Name + "\" " +
                " \"exit\" ";

            ReportService.AddLog(command, LogLevels.Information);

            System.Diagnostics.Process cmd = new System.Diagnostics.Process();
            cmd.StartInfo.FileName = "\"C:\\Program Files (x86)\\WinSCP\\WinSCP.com\"";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;
            cmd.Start();
            ReportService.AddLog(cmd.StandardOutput.ReadToEnd(), LogLevels.Information);
            cmd.WaitForExit();
            cmd.StandardInput.Close();
            cmd.Close();
        }
        public void EepromBackup(string zipHostAddress, string GW_Json_Name)
        {
            Console.WriteLine("------Copy GWJson.json to your raspberry /home/pi------");
            //string debug1 = "\"call echo ----------Import GW file---------- \" ";  

            string command = " /nointeractiveinput /command \"open "+gwHostProtocol+"://"+gwHostLogin+"@" + zipHostAddress +
                "/\" \"call "+gwHostStop+"\" " +
                //"\"call sudo /usr/local/bin/zgw_import.sh -s\" \"exit\"";
                "\"call " + gwHostSUDO + " /usr/local/bin/zgw_restore -s /dev/ttyACM0 -j " + gwHostWorkspace + GW_Json_Name + "\" " +
                "\"call " + gwHostSUDO + " sleep 20\"  \"exit\"";

            Console.WriteLine(command);
            //" \"call sudo /usr/local/bin/zgw_import.sh -s /dev/ttyACM0 -f /home/pi/" + target_firmware + " -p " + protocol_name + " -j /home/pi/ " + jsonFileName;
            //" \"call sudo sleep 20\" \"call sudo /etc/init.d/zipgateway restart\" \"exit\"";            

            ReportService.AddLog(command, LogLevels.Information);

            System.Diagnostics.Process cmd = new System.Diagnostics.Process();
            cmd.StartInfo.FileName = "\"C:\\Program Files (x86)\\WinSCP\\WinSCP.com\"";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;

            //MessageBox.Show("change to a new bridge device and press any key to continue...", "User input", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);

            //cmd.StartInfo.Arguments = command2;
            cmd.Start();
            Console.WriteLine(cmd.StandardOutput.ReadToEnd());
            ReportService.AddLog(cmd.StandardOutput.ReadToEnd(), LogLevels.Information);
            cmd.WaitForExit();
            cmd.StandardInput.Close();
            cmd.Close();
        }
        public void RestartPiGW(string zipHostAddress)
        {
            string command = " /nointeractiveinput /command \"open "+gwHostProtocol+"://"+gwHostLogin+"@" + zipHostAddress +
                "/\" \"call "+gwHostStop+ "\" " +
                "\"call " + gwHostSUDO + " sleep 20\" \"call "+gwHostExecuteGW+"\" \"exit\"";

            Console.WriteLine(command);

            System.Diagnostics.Process cmd = new System.Diagnostics.Process();
            cmd.StartInfo.FileName = "\"C:\\Program Files (x86)\\WinSCP\\WinSCP.com\"";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;
            //MessageBox.Show("change to a new bridge device and press any key to continue...", "User input", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);

            cmd.Start();
            Console.WriteLine(cmd.StandardOutput.ReadToEnd());
            cmd.WaitForExit();
            cmd.StandardInput.Close();
            cmd.Close();
        }
        public void StopGW(string zipHostAddress)
        {
            string command = " /nointeractiveinput /command \"open "+gwHostProtocol+"://"+gwHostLogin+"@" + zipHostAddress +
                "/\" \"call " +gwHostStop+"\" " +
                "\"exit\"";

            Console.WriteLine(command);

            System.Diagnostics.Process cmd = new System.Diagnostics.Process();
            cmd.StartInfo.FileName = "\"C:\\Program Files (x86)\\WinSCP\\WinSCP.com\"";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;
            //MessageBox.Show("change to a new bridge device and press any key to continue...", "User input", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);

            cmd.Start();
            Console.WriteLine(cmd.StandardOutput.ReadToEnd());
            cmd.WaitForExit();
            cmd.StandardInput.Close();
            cmd.Close();
        }
        public void ProgramGWmodule(string zipHostAddress, string HexOrGblPath)
        {
            string[] pathSplit = HexOrGblPath.Split( new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
            string hexFileName = pathSplit[pathSplit.Length - 1];
            string command = " /nointeractiveinput /command \"open "+gwHostProtocol+"://"+gwHostLogin+"@" + 
                zipHostAddress + "/ -timeout=60" +
                "\" \"put " + HexOrGblPath + " "+ gwHostWorkspace + hexFileName +
                "\" \"call "+gwHostStop+ "\" \"call " + gwHostSUDO + " sleep 5\" " +
                "\"call "+gwHostSUDO+" /usr/local/bin/zw_programmer -s /dev/ttyACM0 -p " + gwHostWorkspace + hexFileName + " \" " +
                "\"call " + gwHostSUDO + " sleep 10\" \"call "+gwHostExecuteGW+"\" " +
                "\"call " + gwHostSUDO + " rm "+gwHostWorkspace+"*.hex\" " +
                "\"exit\"";

            Console.WriteLine(command);

            System.Diagnostics.Process cmd = new System.Diagnostics.Process();
            cmd.StartInfo.FileName = "\"C:\\Program Files (x86)\\WinSCP\\WinSCP.com\"";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;            

            cmd.Start();
            Console.WriteLine(cmd.StandardOutput.ReadToEnd());
            cmd.WaitForExit();
            cmd.StandardInput.Close();
            cmd.Close();
        }
        public void ProgramGWmodule700(string HostAddress_700, string HexPath, string CommanderPath)
        {
            string[] pathSplit = HexPath.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
            string hexFileName = pathSplit[pathSplit.Length - 1];
            string command = " flash --masserase " + HexPath + " --ip " + HostAddress_700;
            //string command = " flash " + HexPath + " --ip " + HostAddress_700;

            Console.WriteLine(command);

            System.Diagnostics.Process cmd = new System.Diagnostics.Process();
            cmd.StartInfo.FileName = "\""+CommanderPath+"\"";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;

            cmd.Start();
            Console.WriteLine(cmd.StandardOutput.ReadToEnd());
            cmd.WaitForExit();
            cmd.StandardInput.Close();
            cmd.Close();
        }
        public void BackUpGw(string zipHostAddress)
        {
            string command = " /nointeractiveinput /command \"open "+gwHostProtocol+"://"+gwHostLogin+"@" + zipHostAddress +
                "/\" "+
                "\"call "+gwHostSUDO+" /usr/local/bin/zgw_backup.sh -b "+gwHostWorkspace+"backup.zip\" " +
                "\"call "+gwHostSUDO+ " echo Return value was $?\" " +
                " \"exit\"";

            Console.WriteLine(command);

            System.Diagnostics.Process cmd = new System.Diagnostics.Process();
            cmd.StartInfo.FileName = "\"C:\\Program Files (x86)\\WinSCP\\WinSCP.com\"";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;

            cmd.Start();
            Console.WriteLine(cmd.StandardOutput.ReadToEnd());
            cmd.WaitForExit();
            cmd.StandardInput.Close();
            cmd.Close();
        }
        public void RemoveFileFromGwHost(string zipHostAddress, string fileName)
        {
            string command = " /nointeractiveinput /command \"open " + gwHostProtocol + "://" + gwHostLogin + "@" + zipHostAddress +
                "/\" \"call " + gwHostSUDO + " rm " + gwHostWorkspace + fileName + "\"" +
                " \"exit\"";

            Console.WriteLine(command);

            System.Diagnostics.Process cmd = new System.Diagnostics.Process();
            cmd.StartInfo.FileName = "\"C:\\Program Files (x86)\\WinSCP\\WinSCP.com\"";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;

            cmd.Start();
            Console.WriteLine(cmd.StandardOutput.ReadToEnd());
            cmd.WaitForExit();
            cmd.StandardInput.Close();
            cmd.Close();
        }
        public void RestoreGw(string zipHostAddress)
        {
            string command = " /nointeractiveinput /command \"open " + gwHostProtocol + "://" + gwHostLogin + "@" + zipHostAddress +
                "/\" \"call "+gwHostStop+"\"" +
                " \"call  " + gwHostSUDO + " sleep 10\"" +
                " \"call " +gwHostSUDO+" /usr/local/bin/zgw_recover.sh -b "+gwHostWorkspace+"backup.zip -s /dev/ttyACM0\"" +
                " \"call " + gwHostSUDO + " echo Return value was $?\" " +
                " \"call  " + gwHostSUDO +" sleep 10\"" +
                " \"call "+gwHostExecuteGW+"\""+
                " \"exit\"";

            Console.WriteLine(command);

            System.Diagnostics.Process cmd = new System.Diagnostics.Process();
            cmd.StartInfo.FileName = "\"C:\\Program Files (x86)\\WinSCP\\WinSCP.com\"";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;

            cmd.Start();
            Console.WriteLine(cmd.StandardOutput.ReadToEnd());
            cmd.WaitForExit();
            cmd.StandardInput.Close();
            cmd.Close();
        }
        public void StartGW(string zipHostAddress)
        {
            string command = " /nointeractiveinput /command \"open " + gwHostProtocol + "://" + gwHostLogin + "@" + zipHostAddress +
                "/\" \"call "+gwHostExecuteGW+"\" " +
                "\"exit\"";

            Console.WriteLine(command);

            System.Diagnostics.Process cmd = new System.Diagnostics.Process();
            cmd.StartInfo.FileName = "\"C:\\Program Files (x86)\\WinSCP\\WinSCP.com\"";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;
            //MessageBox.Show("change to a new bridge device and press any key to continue...", "User input", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);

            cmd.Start();
            Console.WriteLine(cmd.StandardOutput.ReadToEnd());
            cmd.WaitForExit();
            cmd.StandardInput.Close();
            cmd.Close();
        }
        public void RemoveGwEEPROM(string zipHostAddress)
        {
            string command = " /nointeractiveinput /command \"open " + gwHostProtocol + "://" + gwHostLogin + "@" + zipHostAddress +
                "/\" \"call " + gwHostSUDO + " cd /usr/local/var/lib/zipgateway " + "\" " +
                " \"call  " + gwHostSUDO +" rm eeprom.dat"+  "\" " +
                "\"exit\"";

            Console.WriteLine(command);

            System.Diagnostics.Process cmd = new System.Diagnostics.Process();
            cmd.StartInfo.FileName = "\"C:\\Program Files (x86)\\WinSCP\\WinSCP.com\"";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;
            //MessageBox.Show("change to a new bridge device and press any key to continue...", "User input", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);

            cmd.Start();
            Console.WriteLine(cmd.StandardOutput.ReadToEnd());
            cmd.WaitForExit();
            cmd.StandardInput.Close();
            cmd.Close();
        }
        public void GetDnsGW(string savePath)
        {
            string command = "-B _z-wave._udp > " + savePath;
            System.Diagnostics.Process cmd = new System.Diagnostics.Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;

            cmd.Start();

            Thread.Sleep(5000);

            cmd.Close();
        }        
        public void CheckGwHost(string zipHostAddress)
        {
            string command = " /nointeractiveinput /command \"open scp://root:1234@" + zipHostAddress +
                "/\" \"call uname --m\" " +
                "\"exit\"";

            Console.WriteLine(command);

            System.Diagnostics.Process cmd = new System.Diagnostics.Process();
            cmd.StartInfo.FileName = "\"C:\\Program Files (x86)\\WinSCP\\WinSCP.com\"";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;
            //MessageBox.Show("change to a new bridge device and press any key to continue...", "User input", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);

            cmd.Start();
            string standardOutput = cmd.StandardOutput.ReadToEnd();
            if(standardOutput.Contains("mips"))
            {
                gwHostLogin = "root:1234";                
                gwHostProtocol = "scp";
                gwHostWorkspace = "/root/";
                gwHostExecuteGW = "/root/start_gw.sh";
                gwHostStop = "/root/stop_gw.sh";
                gwHostStart = "/root/start_gw.sh";
                gwHostSUDO = "";
            }
            Console.WriteLine(standardOutput);
            cmd.WaitForExit();
            cmd.StandardInput.Close();
            cmd.Close();
        }
        
    }
}
