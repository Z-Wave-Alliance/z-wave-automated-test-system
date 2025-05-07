using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Utils;
using Utils.UI.Enums;
using ZWave;
using ZWave.ZipApplication.Devices;
using System.Linq;

namespace Zats.Services
{
    public class ZipAsyncService : ServiceBase, IZipAsyncService
    {
        private readonly TransmitOptions TX_OPTIONS = TransmitOptions.Ack | TransmitOptions.AutoRoute | TransmitOptions.Explore;
        private DeviceManager _deviceManager;
        readonly ZWave.Enums.TransmitOptions TXO = ZWave.Enums.TransmitOptions.TransmitOptionAcknowledge |
          ZWave.Enums.TransmitOptions.TransmitOptionAutoRoute |
          ZWave.Enums.TransmitOptions.TransmitOptionExplore;
        private readonly SecuritySchemes[] DefaultGrantedKeys = new SecuritySchemes[] { SecuritySchemes.S0,
            SecuritySchemes.S2_UNAUTHENTICATED, SecuritySchemes.S2_AUTHENTICATED, SecuritySchemes.S2_ACCESS};

        private ZipManager ZipManager { get; set; }

        public ZipAsyncService(ZipManager zipManager, DeviceManager deviceManager)
        {
            ZipManager = zipManager;
            _deviceManager = deviceManager;
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            ZipManager.Log(text, logLevel);
        }

        public void StartPing(IPAddress ipAddress, int timeoutMs, int intervalMs)
        {
            ZipManager.StartPing(ipAddress, int.MaxValue, timeoutMs, intervalMs);
        }

        public void StartPing(Alias zipDeviceNumber, int timeoutMs, int intervalMs)
        {
            ZipDevice device = ZipManager.GetDevice(zipDeviceNumber);
            if (device != null)
                StartPing(device.IpAddress, timeoutMs, intervalMs);
        }

        public ZipResult.Ping StopPing(IPAddress ipAddress)
        {
            return ZipManager.StopPing(ipAddress);
        }

        public ZipResult.Ping StopPing(Alias zipDeviceNumber)
        {
            ZipDevice device = ZipManager.GetDevice(zipDeviceNumber);
            if (device != null)
                return StopPing(device.IpAddress);
            else
                return new ZipResult.Ping();
        }

        public void ResetAllPing()
        {
            ZipManager.ResetAllPing();
        }

        public bool WaitForPingStart(IPAddress ipAddress, int timeoutMs)
        {
            return ZipManager.WaitForPingStart(ipAddress, timeoutMs);
        }

        public bool WaitForPingStart(Alias zipDeviceNumber, int timeoutMs)
        {
            ZipDevice device = ZipManager.GetDevice(zipDeviceNumber);
            if (device != null)
                return WaitForPingStart(device.IpAddress, timeoutMs);
            else
                return false;
        }

        public ZipToken.ExpectData ExpectData(Alias zipSenderNumber, byte[] expectedPacket, int timeoutMs)
        {
            ZipDevice node = ZipManager.GetDevice(zipSenderNumber);
            return new ZipToken.ExpectData(node.ExpectData(expectedPacket, timeoutMs, null));
        }

        public ZipToken.ExpectData ExpectData(Alias zipSenderNumber, byte[] expectedPacket, int timeoutMs, int hitCount)
        {
            ZipDevice node = ZipManager.GetDevice(zipSenderNumber);
            return new ZipToken.ExpectData(node.ExpectData(expectedPacket, timeoutMs, hitCount, null));
        }

        public bool WaitCompletedSignal(_ActionToken token)
        {
            return token.Token.WaitCompletedSignal().IsStateCompleted;
        }

        public ZipToken.AddNode AddNode(Alias zipControllerNumber, int timeoutMs)
        {
            return AddNode(zipControllerNumber, Modes.NodeAnyS2, timeoutMs);
        }

        public ZipToken.AddNode AddNode(Alias zipControllerNumber, Modes mode, int timeoutMs)
        {
            return AddNodeWithDSK(zipControllerNumber, mode, null, timeoutMs);
        }

        public ZipToken.AddNode AddNode(Alias zipControllerNumber, Modes mode, int timeoutMs,
            TransmitOptions Txo)
        {
            return AddNodeWithDSK(zipControllerNumber, mode, null, timeoutMs, Txo);
        }

        public ZipToken.AddNode AddNode(Alias zipControllerNumber, Modes mode, SecuritySchemes[] GrantSecurities, int timeoutMs)
        {
            return AddNodeWithDSK(zipControllerNumber, mode, null, GrantSecurities, timeoutMs);
        }      

        public ZipToken.AddNode AddNodeWithDSK(Alias zipControllerNumber, Modes mode, int dsk, int timeoutMs)
        {
            byte[] dskInner = null;
            try
            {
                dskInner = new byte[2] { (byte)(dsk >> 8), (byte)dsk };
            }
            catch (Exception)
            {
            }
            return AddNodeWithDSK(zipControllerNumber, mode, dskInner, timeoutMs);
        }

        public ZipToken.AddNode AddNodeWithDSK(Alias zipControllerNumber, Modes mode, byte[] dsk, int timeoutMs)
        {
            ZipController controller = (ZipController)ZipManager.GetDevice(zipControllerNumber);
            //Setting DSK
            if (_deviceManager.IncludeSettings.GrantedSchemes == null)
            {
                _deviceManager.IncludeSettings.GrantedSchemes = DefaultGrantedKeys;
            }
            SetTestInclusionValues(dsk,
                _deviceManager.IncludeSettings.JoinDSK,
                !_deviceManager.IncludeSettings.CancelGrantedSchemes,
                _deviceManager.IncludeSettings.GrantedSchemes);

            return AddNodeWithDSK(zipControllerNumber, mode, dsk, _deviceManager.IncludeSettings.GrantedSchemes, timeoutMs);
        }

        public ZipToken.AddNode AddNodeWithDSK(Alias zipControllerNumber, Modes mode, byte[] dsk, int timeoutMs,
            TransmitOptions Txo)
        {
            ZipController controller = (ZipController)ZipManager.GetDevice(zipControllerNumber);
            //Setting DSK
            if (_deviceManager.IncludeSettings.GrantedSchemes == null)
            {
                _deviceManager.IncludeSettings.GrantedSchemes = DefaultGrantedKeys;
            }
            SetTestInclusionValues(dsk,
                _deviceManager.IncludeSettings.JoinDSK,
                !_deviceManager.IncludeSettings.CancelGrantedSchemes,
                _deviceManager.IncludeSettings.GrantedSchemes);

            return AddNodeWithDSK(zipControllerNumber, mode, 
                dsk, _deviceManager.IncludeSettings.GrantedSchemes, timeoutMs, Txo);
        }

        public ZipToken.AddNode AddNodeWithDSK(Alias zipControllerNumber, Modes mode, byte[] dsk, SecuritySchemes[] GrantSecurities, int timeoutMs )
        {
            ZipController controller = (ZipController)ZipManager.GetDevice(zipControllerNumber);
            //Setting DSK
            if (GrantSecurities == null || GrantSecurities.Length == 0x00)
            {
                _deviceManager.IncludeSettings.GrantedSchemes = DefaultGrantedKeys;
            }
            else
            {
                _deviceManager.IncludeSettings.GrantedSchemes = GrantSecurities;
            }
            SetTestInclusionValues(dsk,
                _deviceManager.IncludeSettings.JoinDSK,
                !_deviceManager.IncludeSettings.CancelGrantedSchemes,
                _deviceManager.IncludeSettings.GrantedSchemes);

            ActionToken token = controller.AddNodeToNetwork((ZWave.Enums.Modes)((byte)mode), TXO, timeoutMs, null);
            return new ZipToken.AddNode(token);
        }

        public ZipToken.AddNode AddNodeWithDSK(Alias zipControllerNumber, 
            Modes mode, byte[] dsk, SecuritySchemes[] GrantSecurities, int timeoutMs, TransmitOptions Txo)
        {
            ZipController controller = (ZipController)ZipManager.GetDevice(zipControllerNumber);
            //Setting DSK
            if (GrantSecurities == null || GrantSecurities.Length == 0x00)
            {
                _deviceManager.IncludeSettings.GrantedSchemes = DefaultGrantedKeys;
            }
            else
            {
                _deviceManager.IncludeSettings.GrantedSchemes = GrantSecurities;
            }
            SetTestInclusionValues(dsk,
                _deviceManager.IncludeSettings.JoinDSK,
                !_deviceManager.IncludeSettings.CancelGrantedSchemes,
                _deviceManager.IncludeSettings.GrantedSchemes);

            ActionToken token = controller.AddNodeToNetwork((ZWave.Enums.Modes)((byte)mode), 
                (ZWave.Enums.TransmitOptions)Txo, timeoutMs, null);
            return new ZipToken.AddNode(token);
        }

        public ZipToken.RemoveNode RemoveNode(Alias zipControllerNumber, Modes mode, int timeoutMs)
        {
            ZipController controller = (ZipController)ZipManager.GetDevice(zipControllerNumber);
            ActionToken token = controller.RemoveNodeFromNetwork((ZWave.Enums.Modes)((byte)mode), timeoutMs, null);
            return new ZipToken.RemoveNode(token);
        }

        public ZipToken.LearnMode SetLearnMode(Alias zipControllerNumber, LearnModes mode, int timeoutMs)
        {
            Thread.Sleep(5000);
            ZipController controller = (ZipController)ZipManager.GetDevice(zipControllerNumber);
            ActionToken token = controller.SetLearnMode((ZWave.Enums.LearnModes)((byte)mode), timeoutMs, null);
            return new ZipToken.LearnMode(token);
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
            ZipManager.DskValue = dsk;
            ZipManager.CsaValue = csa;
            ZipManager.IsKexReportConfirmed = isKexReportConfirmed;
            ZipManager.GrantedSchemes = grantedSchemes;
        }

        /// <summary>
        /// Cancells async operation
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="token">token of the started async operation</param>
        public void CancelToken(Alias zipReceiverNumber, _ActionToken token)
        {
            ZipDevice device = ZipManager.GetDevice(zipReceiverNumber);
            device.Cancel(token.Token);
        }

        public ZipToken.SendData SendData(Alias zipReceiverNumber, byte[] packet, int timeout, bool isNoAck)
        {
            ZipDevice zipDevice = ZipManager.GetDevice(zipReceiverNumber);
            ActionToken token;
            token = null;
            try
            {
                token = zipDevice.SendData(null, packet, !isNoAck, timeout, null, isNoAck);
            }
            catch (Exception)
            {

            }
            return new ZipToken.SendData(token);
        }

        public ZipToken.SendData SendData(Alias zipReceiverNumber, byte[] packet)
        {
            ZipDevice zipDevice = ZipManager.GetDevice(zipReceiverNumber);
            ActionToken token;
            token = null;
            try
            {
                token = zipDevice.SendData(null, packet, true, 1000, null);
            }
            catch (Exception)
            {

            }
            return new ZipToken.SendData(token);
        }
        public ZipToken.SendData SendData(Alias zipReceiverNumber, byte[] packet, int timeout)
        {
            ZipDevice zipDevice = ZipManager.GetDevice(zipReceiverNumber);
            ActionToken token;
            token = null;
            try
            {
                //token = zipDevice.SendData(null, packet, null);
                token = zipDevice.SendData(null, packet, true, timeout, null);
            }
            catch (Exception)
            {

            }
            return new ZipToken.SendData(token);
        }
        public DateTime SendDataTimeStamp(Alias zipReceiverNumber, byte[] packet, int timeout)
        {
            ZipDevice zipDevice = ZipManager.GetDevice(zipReceiverNumber);
            ActionToken token = zipDevice.SendData(null, packet, true, timeout, null);
            return token.WaitCompletedSignal().StartTimestamp;
        }
        public void CancelTokenArray(Alias ZipUnsol, ZipToken.Void[] tokenArray)
        {
            foreach (var token in tokenArray)
            {
                CancelToken(ZipUnsol, token);
            }
        }
         
        public byte FirmwareUpdateV5(Alias deviceNumber,Alias enddevice, Node node, string filePath,COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT fmRep)
        {
            byte activation = 0;
            ushort fragmentSize = 0;
            ZipController controller = (ZipController)ZipManager.GetDevice(deviceNumber);
            byte ret = 0x00;
            AutoResetEvent completedFirmwareUpdateSignal = new AutoResetEvent(false);            

            if (File.Exists(filePath))
            {
                byte[] firmwareData = null;
                try
                {
                    firmwareData = HexFileHelper.GetBytes(HexFileHelper.ReadIntelHexFile(filePath, 0xFF), 0xFF);
                }
                catch (FileFormatException)
                {
                }
                if (firmwareData == null)
                {
                    firmwareData = File.ReadAllBytes(filePath);
                }
                var firmwareChecksum = Tools.CalculateCrc16Array(firmwareData);
                byte[] frSize = new byte[] { (byte)(fragmentSize >> 8), (byte)fragmentSize };// default 40
                                
                if (fmRep != null)
                {
                    var fwReport = fmRep;
                    if (fragmentSize == 0 || fragmentSize > Tools.GetInt32(fwReport.maxFragmentSize))
                    {
                        frSize = fwReport.maxFragmentSize;
                    }
                    List<byte[]> fwData = Tools.ArraySplit(firmwareData, Tools.GetInt32(frSize));
                    Log("Number of FW fragments: " + fwData.Count, LogLevels.Information);
                    ActionToken aToken = null;
                    ret = InnerFirmwareUpdateV5(controller, node, fwData, firmwareChecksum, fwReport.firmware0Id, fwReport.manufacturerId, 0x00, frSize, activation, fwReport.hardwareVersion, out aToken, enddevice);
                    COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_GET fu_Md_Get = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_GET();
                    COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REPORT fu_Md_Rep = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REPORT();
                    if (ret == 0xFF)
                    {
                        for (int i = 0; i < fwData.Count; i++)
                        {
                            var IsMdGet = ExpectData(enddevice, fu_Md_Get, 10000);
                            if (IsMdGet.WaitCompletedSignal())
                            {
                                fu_Md_Get = IsMdGet.WaitCompletedSignal().ReceivedData;
                                List<byte[]> rep = FwuResponseDataHandler(fwData, fu_Md_Get, out fu_Md_Get);
                                fu_Md_Rep = rep.SelectMany(a => a).ToArray();
                                SendData(enddevice, fu_Md_Rep);
                                IsMdGet.Dispose();
                            }
                            else if (!IsMdGet.WaitCompletedSignal())
                            {
                                IsMdGet = ExpectData(enddevice, fu_Md_Get, 10000);
                                fu_Md_Get = IsMdGet.WaitCompletedSignal().ReceivedData;
                                List<byte[]> rep = FwuResponseDataHandler(fwData, fu_Md_Get, out fu_Md_Get);
                                fu_Md_Rep = rep.SelectMany(a => a).ToArray();
                                SendData(enddevice, fu_Md_Rep);
                                IsMdGet.Dispose();
                            }
                            else if (i == 6002 && !IsMdGet.WaitCompletedSignal())
                            {
                                IsMdGet = ExpectData(enddevice, fu_Md_Get, 10000);
                                fu_Md_Get = IsMdGet.WaitCompletedSignal().ReceivedData;
                                List<byte[]> rep = FwuResponseDataHandler(fwData, fu_Md_Get, out fu_Md_Get);
                                fu_Md_Rep = rep.SelectMany(a => a).ToArray();
                                SendData(enddevice, fu_Md_Rep);
                                IsMdGet.Dispose();
                            }
                            else if(i == 6003 && !IsMdGet.WaitCompletedSignal())
                            {
                                IsMdGet = ExpectData(enddevice, fu_Md_Get, 10000);
                                fu_Md_Get = IsMdGet.WaitCompletedSignal().ReceivedData;
                                List<byte[]> rep = FwuResponseDataHandler(fwData, fu_Md_Get, out fu_Md_Get);
                                fu_Md_Rep = rep.SelectMany(a => a).ToArray();
                                SendData(enddevice, fu_Md_Rep);
                                IsMdGet.Dispose();
                            }
                            else if (i == 6004 && !IsMdGet.WaitCompletedSignal())
                            {
                                IsMdGet = ExpectData(enddevice, fu_Md_Get, 10000);
                                fu_Md_Get = IsMdGet.WaitCompletedSignal().ReceivedData;
                                List<byte[]> rep = FwuResponseDataHandler(fwData, fu_Md_Get, out fu_Md_Get);
                                fu_Md_Rep = rep.SelectMany(a => a).ToArray();
                                SendData(enddevice, fu_Md_Rep);
                                IsMdGet.Dispose();
                            }
                            else if (i == 6005 && !IsMdGet.WaitCompletedSignal())
                            {
                                IsMdGet = ExpectData(enddevice, fu_Md_Get, 10000);
                                fu_Md_Get = IsMdGet.WaitCompletedSignal().ReceivedData;
                                List<byte[]> rep = FwuResponseDataHandler(fwData, fu_Md_Get, out fu_Md_Get);
                                fu_Md_Rep = rep.SelectMany(a => a).ToArray();
                                SendData(enddevice, fu_Md_Rep);
                                IsMdGet.Dispose();
                            }
                            else
                            {
                                Console.WriteLine("error, ZGW doesn't get any Firmware Update Md Get from End device!!!");
                            }
                        }

                        completedFirmwareUpdateSignal.Reset();
                        completedFirmwareUpdateSignal.WaitOne(100);
                    }
                    controller.Cancel(aToken);
                }
            }
            else
            {
                Log("File with firmware was not found", LogLevels.Error);
            }
            return ret;
        }
        private byte InnerFirmwareUpdateV5(ZipController controller, Node node, List<byte[]> fwData, byte[] fwChecksum, byte[] fwId, byte[] mfId, byte fwTarget, byte[] fragmentSize, byte activation, byte hardwareVersion, out ActionToken token, Alias enddevice)
        {       
            COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_GET fwuMDRequestGetData = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_GET();
            fwuMDRequestGetData.checksum = fwChecksum;
            fwuMDRequestGetData.firmwareId = fwId;
            fwuMDRequestGetData.manufacturerId = mfId;
            fwuMDRequestGetData.firmwareTarget = fwTarget;
            fwuMDRequestGetData.fragmentSize = fragmentSize;
            fwuMDRequestGetData.properties1 = activation;
            fwuMDRequestGetData.hardwareVersion = hardwareVersion;

            COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_REPORT fwuMDRequestReportData = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_REPORT();
            //Register ResponseData
            COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_GET fwuMDGetData = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_GET(); //response from the node

            //Watching Fu_MD_Get and sending back the Fu_MD_Report 
            token = controller.ResponseMultiData(null, (t, h, d) => FwuResponseDataHandler(fwData, d, out fwuMDGetData), new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_GET());
                       
            //Start Update
            return MDRequestGet(enddevice, node, fwuMDRequestGetData, fwuMDRequestReportData, controller);
        }
        private List<byte[]> FwuResponseDataHandler(List<byte[]> fwData, byte[] x, out COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_GET fwuMDGetData)
        {
            fwuMDGetData = x;
            List<byte[]> fwuMDReportData = new List<byte[]>();
            short firmwareDataIndex = 0;
            for (byte i = 0; i < fwuMDGetData.numberOfReports; i++)
            {
                firmwareDataIndex = (short)(((fwuMDGetData.properties1.reportNumber1 << 8) + fwuMDGetData.reportNumber2) + i);
                if (firmwareDataIndex > 0 && fwData.Count > (firmwareDataIndex - 1))
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

                    ushort crc = Utils.Tools.CalculateCrc16(dataToSend);

                    dataToSend.Add((byte)(crc >> 8));
                    dataToSend.Add((byte)crc);

                    fwuMDReportData.Add(dataToSend.ToArray());
                }
            }
            return fwuMDReportData;
        }

        private byte MDRequestGet(Alias enddevice, Node node, byte[] fwuMDRequestGetData, byte[] fwuMDRequestReportData, ZipController controller)
        {
            //this is output the final OTA update status back to the test case
            byte ret = 0;

            //var res = controller.RequestData(null, fwuMDRequestGetData, fwuMDRequestReportData, 3000);
            var res = ExpectData(enddevice, fwuMDRequestReportData,3000);
            SendData(enddevice, fwuMDRequestGetData);

            if (res.WaitCompletedSignal() == true && res.WaitCompletedSignal().ReceivedData != null)
            {
                var repData = (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V2.FIRMWARE_UPDATE_MD_REQUEST_REPORT)res.WaitCompletedSignal().ReceivedData;
                ret = repData.status;
            }
            return ret;
        }
    }
}
