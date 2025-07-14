// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using Utils;
using ZWave;
using ZWave.Enums;

namespace Zats.Services
{
    public class ProgrammingGammaProvider : ProgrammingProviderBase, IProgrammingProvider
    {
        public ProgrammingGammaProvider(string appPath, Action<bool, string> logAction)
            : base(appPath, logAction) { }

        public int EraseSample(DeviceHostClient deviceHostClient)
        {
            int ret = 0;
            var devicePort = GetHostClientPortWithArg(deviceHostClient);
            var manipulatorPort = GetHostManipulatorPortWithArg(deviceHostClient);
            var deviceId = getDeviceIdentifierWithArg(deviceHostClient.Host);
            string args;
            if (manipulatorPort != null)
            {
                args = $"device reset {manipulatorPort}";
                ret = ProgrammerAction(args, "Reset", 30000);
            }
            for (int i = 0; i < 3; i++)
            {
                args = $"device masserase {devicePort} {deviceId}";
                ret = ProgrammerAction(args, "Erase Flash", 30000);
                if (ret != 0)
                {
                    args = $"device recover {devicePort}";
                    ret = ProgrammerAction(args, "Recover Bricked Device", 30000);
                }
                else
                {
                    break;
                }
            }
            return ret;
        }

        private void AwakeDeviceWithTestInterface(DeviceHostClient deviceHostClient)
        {
            if (deviceHostClient.PClient != null)
            {
                bool isConnected = deviceHostClient.PClient.TransportClient.IsOpen;
                if (!isConnected)
                {
                    deviceHostClient.PClient.Connect();
                }
                var res = deviceHostClient.PClient.Request("EF 00 73 02 01 01".GetBytes(),
                    new[] { ByteIndex.AnyValue, new ByteIndex((byte)FrameTypes.Response), new ByteIndex(0x73) }, 1000);
                _logAction(res, "Attempt to awake device with Learn Mode button");

                if (!isConnected)
                {
                    deviceHostClient.PClient.Disconnect();
                }
            }
        }

        public int RenewDsk(DeviceHostClient deviceHostClient)
        {
            int ret = 0;
            string args;
            var devicePort = GetHostClientPortWithArg(deviceHostClient);
            var deviceId = getDeviceIdentifierWithArg(deviceHostClient.Host);
            args = $"device pageerase --region @lockbits {devicePort} {deviceId}";
            ret = ProgrammerAction(args, "Erase DSK", 30000, out string output);
            Thread.Sleep(10000); // wait for adapter restore 
            args = $"adapter reset {devicePort}";
            ret = ProgrammerAction(args, "Reset Adapter", 30000, out output);
            Thread.Sleep(10000); // wait for adapter restore 
            return ret;
        }

        private string GetAdapterInfo(DeviceHostClient deviceHostClient)
        {
            string ret = null;
            var devicePort = GetHostClientPortWithArg(deviceHostClient);
            var args = $"adapter probe --boards {devicePort}";

            string output = string.Empty;
            for (int i = 0; i < 3; i++)
            {
                int chipRet = ProgrammerAction(args, "Get Adapter Info", 30000, out output);
                if (chipRet == 0)
                {
                    break;
                }
            }
            try
            {
                if (!string.IsNullOrEmpty(output))
                {
                    var seriesPattern = @"^Name\s+:\s+(?:EFR32)?(\w+).+Board\s+Part Number\s+:\s+(\w+).*$";
                    var seriesPatternWithoutBoard = @"^Name\s+:\s+(?:EFR32)?(\w+).+\nPart Number\s+:\s+(\w+).*$";
                    var seriesMatch = Regex.Match(output, seriesPattern, RegexOptions.Multiline);
                    var seriesMatchWithoutBoard = Regex.Match(output, seriesPatternWithoutBoard, RegexOptions.Multiline);
                    if (seriesMatch != null && seriesMatch.Success && seriesMatch.Groups.Count > 2)
                    {
                        ret = $"{seriesMatch.Groups[1].Value}_{seriesMatch.Groups[2].Value}";
                        deviceHostClient.Host.ChipSeries = ret;
                    }
                    else if (seriesMatchWithoutBoard != null && seriesMatchWithoutBoard.Success && seriesMatchWithoutBoard.Groups.Count > 2)
                    {
                        ret = $"{seriesMatchWithoutBoard.Groups[1].Value}_{seriesMatchWithoutBoard.Groups[2].Value}";
                        deviceHostClient.Host.ChipSeries = ret;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Message._DLOG();
            }
            return ret;
        }

        public string GetChipSeries(DeviceHostClient deviceHostClient)
        {
            return GetAdapterInfo(deviceHostClient);
        }

        public ZWaveRegions GetZWaveRegion(DeviceHostClient deviceHostClient)
        {
            ZWaveRegions zwRegion = ZWaveRegions.REGION_UNDEFINED;
            var devicePort = GetHostClientPortWithArg(deviceHostClient);
            var deviceId = getDeviceIdentifierWithArg(deviceHostClient.Host);
            var args = $"tokendump --tokengroup znet --token MFG_ZWAVE_COUNTRY_FREQ {devicePort} {deviceId}";
            int ret = 0;
            string output = null;
            for (int i = 0; i < 3; i++)
            {
                ret = ProgrammerAction(args, "Get Z-Wave Region", 30000, out output);
                if (ret != 0)
                {
                    AwakeDeviceWithTestInterface(deviceHostClient);
                }
                else
                {
                    break;
                }
            }

            try
            {
                string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                if (lines != null)
                {
                    var zwRegionLine = lines.FirstOrDefault(x => x.StartsWith("MFG_ZWAVE_COUNTRY_FREQ"))?.Replace("MFG_ZWAVE_COUNTRY_FREQ", "").Replace(" ", "").Replace(":", "");
                    if (zwRegionLine != null && zwRegionLine.StartsWith("0x") || zwRegionLine.StartsWith("0X"))
                    {
                        byte zwRegionVal = (int)ZWaveRegions.REGION_UNDEFINED;
                        if (byte.TryParse(zwRegionLine.Substring(2), NumberStyles.HexNumber, null, out zwRegionVal))
                        {
                            zwRegion = (ZWaveRegions)zwRegionVal;
                            if (zwRegion != ZWaveRegions.REGION_UNDEFINED && zwRegion != ZWaveRegions.REGION_DEFAULT)
                            {
                                LogProgrammerAction(true, $"ZWAVE_COUNTRY_FREQ: {zwRegion}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Message._DLOG();
            }
            return zwRegion;
        }

        public byte[] GetDsk(DeviceHostClient deviceHostClient)
        {
            List<byte> dskValue = new List<byte>();
            var devicePort = GetHostClientPortWithArg(deviceHostClient);
            var deviceId = getDeviceIdentifierWithArg(deviceHostClient.Host);
            var args = $"tokendump --tokengroup znet --token MFG_ZW_PUK {devicePort} {deviceId}";
            int ret = 0;
            string output = null;
            for (int i = 0; i < 3; i++)
            {
                ret = ProgrammerAction(args, "Get DSK", 30000, out output);
                if (ret != 0)
                {
                    AwakeDeviceWithTestInterface(deviceHostClient);
                }
                else
                {
                    // Retry if a invalid PUK is found
                    // PUK's are generated at boot time and if the boot takes
                    // longer than expected, we'll get a false negative result
                    if(!output.Contains("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"))
                    {
                        break;
                    }
                    else
                    {
                        // Give device 0.5 seconds more to boot
                        Thread.Sleep(500);
                    }
                }
            }

            try
            {
                string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                if (lines != null)
                {
                    var pukLine = lines.FirstOrDefault(x => x.StartsWith("MFG_ZW_PUK"))?.Replace("MFG_ZW_PUK", "").Replace(" ", "").Replace(":", "");
                    if (pukLine != null && pukLine.All(x => Uri.IsHexDigit(x)))
                    {
                        dskValue.AddRange(pukLine.GetBytes());
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Message._DLOG();
            }
            var dsk = dskValue.ToArray();
            if (dsk != null && dsk.Length >= 16)
            {
                string dskStr = Tools.GetInt32(dsk.Skip(0).Take(2).ToArray()).ToString("00000");
                dskStr += "-" + Tools.GetInt32(dsk.Skip(2).Take(2).ToArray()).ToString("00000");
                dskStr += "-" + Tools.GetInt32(dsk.Skip(4).Take(2).ToArray()).ToString("00000");
                dskStr += "-" + Tools.GetInt32(dsk.Skip(6).Take(2).ToArray()).ToString("00000");
                dskStr += "-" + Tools.GetInt32(dsk.Skip(8).Take(2).ToArray()).ToString("00000");
                dskStr += "-" + Tools.GetInt32(dsk.Skip(10).Take(2).ToArray()).ToString("00000");
                dskStr += "-" + Tools.GetInt32(dsk.Skip(12).Take(2).ToArray()).ToString("00000");
                dskStr += "-" + Tools.GetInt32(dsk.Skip(14).Take(2).ToArray()).ToString("00000");
                LogProgrammerAction(true, $"PUK: {dsk.GetHex(false)}");
                LogProgrammerAction(true, $"DSK: {dskStr} {devicePort}");
            }
            return dskValue.ToArray();
        }

        public byte[] GetPrk(DeviceHostClient deviceHostClient)
        {
            List<byte> prkValue = new List<byte>();
            var devicePort = GetHostClientPortWithArg(deviceHostClient);
            var deviceId = getDeviceIdentifierWithArg(deviceHostClient.Host);
            var args = $"tokendump --tokengroup znet --token MFG_ZW_PRK --token MFG_ZW_PUK {devicePort} {deviceId}";

            int ret = ProgrammerAction(args, "Get PRK", 30000, out string output);
            try
            {
                string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                if (lines != null)
                {
                    var prkLine = lines.FirstOrDefault(x => x.StartsWith("MFG_ZW_PRK"))?.Replace("MFG_ZW_PRK", "").Replace(" ", "").Replace(":", "");
                    var pukLine = lines.FirstOrDefault(x => x.StartsWith("MFG_ZW_PUK"))?.Replace("MFG_ZW_PUK", "").Replace(" ", "").Replace(":", "");
                    if (prkLine != null && prkLine.All(x => Uri.IsHexDigit(x)))
                    {
                        prkValue.AddRange(prkLine.GetBytes());
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Message._DLOG();
            }
            var prk = prkValue.ToArray();
            if (prk != null && prk.Length >= 32)
            {
                string prkStr = prkValue.GetHex(false);
                LogProgrammerAction(true, $"PRK: {prkStr}");
            }
            return prkValue.ToArray();
        }

        public string GetPowerMeasure(DeviceHostClient deviceHostClient, int windowLenght)
        {
            var devicePort = GetHostClientPortWithArg(deviceHostClient);
            var args = string.Format($"aem measure {devicePort} --windowlength {windowLenght}");
            int ret = ProgrammerAction(args, "Get Power Measure", 30000, out string output);
            return output;
        }

        public bool HasFirmware(DeviceHostClient deviceHostClient)
        {
            throw new NotImplementedException();
        }

        private int ProgramSampleWithBootLoaderImageAndKeysInternal(DeviceHostClient deviceHostClient, ZWaveRegions? region, string samplePath, string[] tokenPaths, string bootLoaderPath)
        {
            int ret = 0;
            var devicePort = GetHostClientPortWithArg(deviceHostClient);
            var deviceId = getDeviceIdentifierWithArg(deviceHostClient.Host);
            string args, output;
            for (int i = 0; i < 3; i++)
            {
                args = string.Format($"device masserase {devicePort} {deviceId}");
                ret = ProgrammerAction(args, "Erase Flash", 30000, out output);

                if (ret == 0)
                {
                    args = string.Format($"device reset {devicePort} {deviceId}");
                    ret = ProgrammerAction(args, "Reset", 30000, out output);
                }

                if (ret == 0 && new[] { "ZGM13", "ZG13", "RZ13", "ZG14" }.Any(x => deviceHostClient.Host.ChipSeries.Contains(x)) && bootLoaderPath != null)
                {
                    args = string.Format($"device pageerase --region @bootloader {devicePort} {deviceId}");
                    ProgrammerAction(args, "Erase Bootloader", 30000, out output);
                }

                if (ret == 0 && new[] { "ZGM13", "ZG13", "RZ13", "ZG14" }.Any(x => deviceHostClient.Host.ChipSeries.Contains(x)))
                {
                    args = string.Format($"device pageerase --region @lockbits {devicePort} {deviceId}");
                    ProgrammerAction(args, "Erase Lockbits", 30000, out output);
                }

                if (ret == 0)
                {
                    args = string.Format($"device pageerase --region @userdata {devicePort} {deviceId}");
                    ProgrammerAction(args, "Erase User Data", 30000, out output);
                }

                if (ret == 0 && samplePath != null)
                {
                    args = string.Format($"flash \"{samplePath}\" --address 0x0 {devicePort} {deviceId}");
                    ret = ProgrammerAction(args, "Program Flash", 60000, out output);
                }

                if (ret == 0 && region != null)
                {
                    args = string.Format($"flash --tokengroup znet --token MFG_ZWAVE_COUNTRY_FREQ:0x{(byte)region:X2} {devicePort} {deviceId}");
                    ret = ProgrammerAction(args, $"Set Z-Wave Region {region}=0x{(byte)region:X2}", 30000, out output);
                }

                if (ret == 0 && bootLoaderPath != null)
                {
                    args = string.Format($"flash \"{bootLoaderPath}\" {devicePort} {deviceId}");
                    ret = ProgrammerAction(args, "Program Bootloader", 30000, out output);
                }

                if (ret == 0 && tokenPaths != null && tokenPaths.Length > 0)
                {
                    var tokenfiles = tokenPaths.Select(x => $"--tokenfile \"{x}\"").Aggregate((x, y) => $"{x} {y}");
                    args = string.Format($"flash --tokengroup znet {tokenfiles} {devicePort} {deviceId}");
                    ret = ProgrammerAction(args, "Program Keys", 30000, out output);
                }

                if (ret != 0)
                {
                    args = $"device recover {devicePort}";
                    ret = ProgrammerAction(args, "Recover Bricked Device", 30000, out output);
                }
                else
                {
                    break;
                }
            }

            if (ret == 0 && samplePath != null)
            {
                Thread.Sleep(500);
                var sclient = deviceHostClient.SClient;
                int delay = 2000;
                args = string.Format($"device reset {devicePort} {deviceId}");
                if (sclient != null && sclient.TransportClient.IsOpen
                    && Path.GetFileName(samplePath).IndexOf("serialapi", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var serialApiExpectToken = deviceHostClient.SClient.Expect(new ByteIndex[] { 0x00, 0x0A }, delay, null);
                    ProgrammerAction(args, "Reset", 30000, out output);
                    var serialApiExpectRes = serialApiExpectToken.WaitCompletedSignal();
                    if (serialApiExpectRes)
                    {
                        LogProgrammerAction(serialApiExpectRes, $"Serial Api Started");
                    }
                    else
                    {
                        LogProgrammerAction(true, $"No Serial Api Started after {delay} at {deviceHostClient.Host.SName} ({deviceHostClient.Host.JName})");
                    }
                }
                else
                {
                    ProgrammerAction(args, "Reset", 30000, out output);
                }
                Thread.Sleep(500);
            }
            return ret;
        }

        public int ProgramBootLoader(DeviceHostClient deviceHostClient, string[] tokenPaths, string bootLoaderPath)
        {
            return ProgramSampleWithBootLoaderImageAndKeysInternal(deviceHostClient, ZWaveRegions.REGION_DEFAULT, null, tokenPaths, bootLoaderPath);
        }

        public int ProgramSample(DeviceHostClient deviceHostClient, ZWaveRegions? region, string samplePath, bool isAddS2Keypair, byte[] prk, byte[] puk)
        {
            return ProgramSampleWithBootLoaderImageAndKeysInternal(deviceHostClient, region, samplePath, null, null);
        }

        public int ProgramSampleWithBootLoaderKeys(DeviceHostClient deviceHostClient, ZWaveRegions? region, string samplePath, string[] tokenPaths)
        {
            return ProgramSampleWithBootLoaderImageAndKeysInternal(deviceHostClient, region, samplePath, tokenPaths, null);
        }

        public int ProgramSampleWithBootLoaderImageAndKeys(DeviceHostClient deviceHostClient, ZWaveRegions? region, string samplePath, string[] tokenPaths, string bootLoaderPath)
        {
            return ProgramSampleWithBootLoaderImageAndKeysInternal(deviceHostClient, region, samplePath, tokenPaths, bootLoaderPath);
        }

        public int BackUpSample(DeviceHostClient deviceHostClient, string backUpPath)
        {
            int ret = 0;
            var devicePort = GetHostClientPortWithArg(deviceHostClient);
            var args = string.Format($" {devicePort} -pr {backUpPath}");
            ProgrammerAction(args, "BackUp", 4 * 60 * 1000, out string output);

            return ret;
        }

        public int ResetSample(DeviceHostClient deviceHostClient)
        {
            int ret = 0;
            var devicePort = GetHostClientPortWithArg(deviceHostClient);
            var manipulatorPort = GetHostManipulatorPortWithArg(deviceHostClient);
            string args;
            if (manipulatorPort != null)
            {
                args = $"device reset {manipulatorPort}";
                ret = ProgrammerAction(args, "Reset", 30000);
            }
            args = $"device reset {devicePort}";
            ActionToken token = null;
            var sclient = deviceHostClient.SClient;
            if (sclient != null && sclient.TransportClient.IsOpen && IsSerialApi(deviceHostClient.Library))
            {
                token = deviceHostClient.SClient.Expect(new ByteIndex[] { 0x00, 0x0A }, 5000, null);
            }
            ret = ProgrammerAction(args, "Reset", 5000);
            if (ret == 0)
            {
                if (deviceHostClient != null && deviceHostClient.SClient != null && token != null)
                {
                    var resttt = token.WaitCompletedSignal();
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
            else
            {
                if (deviceHostClient != null && deviceHostClient.SClient != null && token != null)
                {
                    deviceHostClient.SClient.Cancel(token);
                    token.WaitCompletedSignal();
                }
            }
            return ret;
        }

        public int SetHomeId(DeviceHostClient deviceHostClient, byte[] homeId)
        {
            throw new NotImplementedException();
        }

        private int UnlockDebugAccess(DeviceHostClient deviceHostClient)
        {
            var devicePort = GetHostClientPortWithArg(deviceHostClient);
            var deviceId = getDeviceIdentifierWithArg(deviceHostClient.Host);
            var args = $"device {deviceId} lock {devicePort} --debug disable";
            int ret = ProgrammerAction(args, "Unlock Debug Access", 30000, out string output);
            return ret;
        }

        public static bool IsSerialApi(Libraries library)
        {
            return new[]
            {
                Libraries.ControllerBridgeLib,
                Libraries.ControllerPortableLib,
                Libraries.ControllerStaticLib,
                Libraries.InstallerLib,
                Libraries.EndDeviceLib,
                Libraries.EndDeviceLegacyLib,
                Libraries.EndDeviceLegacyRoutingLib,
                Libraries.TestController
            }.Contains(library);
        }

        public int Run(DeviceHostClient deviceClientHost, string args)
        {
            throw new NotImplementedException();
        }

        public int GblCreate(string hexFilePath, string gblFilePath, string signKeyFilePath, string encryptKeyFilePath)
        {
            var args = $"gbl create \"{gblFilePath}\" --app \"{hexFilePath}\" --sign {signKeyFilePath} --encrypt {encryptKeyFilePath} --compress lz4";
            int ret = ProgrammerAction(args, "Create .gbl File", 30000, out string output);
            return ret;
        }

        private string GetHostClientPortWithArg(DeviceHostClient deviceHostClient)
        {
            string devicePort;
            if (IPAddress.TryParse(deviceHostClient.Host.SName, out var ipAddress))
            {
                devicePort = $"--ip {deviceHostClient.Host.SName}";
            }
            else
            {
                devicePort = $"--serialno {deviceHostClient.Host.JName}";
            }

            return devicePort;
        }

        private string GetHostManipulatorPortWithArg(DeviceHostClient deviceHostClient)
        {
            string devicePort = null;
            if (IPAddress.TryParse(deviceHostClient.Host.PName, out var ipAddress))
            {
                devicePort = $"--ip {deviceHostClient.Host.PName}";
            }
            return devicePort;
        }

        public string GetVersion()
        {
            ProgrammerAction("--version", string.Empty, 6000, out string versionOutput);
            return versionOutput;
        }

        public string GetSecurityStatusInfo(DeviceHostClient deviceHostClient)
        {
            var ret = string.Empty;
            var devicePort = GetHostClientPortWithArg(deviceHostClient);
            var deviceId = getDeviceIdentifierWithArg(deviceHostClient.Host);
            var args = $"security status {deviceId} {devicePort}";

            string output = string.Empty;
            for (int i = 0; i < 3; i++)
            {
                int chipRet = ProgrammerAction(args, "Get Security Status Info", 30000, out output);
                if (chipRet == 0)
                {
                    ret = output;
                    break;
                }
            }
            return ret;
        }

        private string getDeviceIdentifierWithArg(DeviceHost deviceHost) {
            if (deviceHost == null) {
                return "--device Cortex-M4";
            }
            
            switch(deviceHost.ChipSeries) {
                case "ZGM230_BRD2603A":
                    return "--device ZGM230SB27HGN3";
                default:
                    return "--device Cortex-M4";
            }
        }
    }
}
