// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.IO;
using System.Linq;
using Utils;
using Utils.UI.Enums;

namespace Zats.Services
{
    public class ProgrammerService
    {
        private DeviceManager DeviceManager { get; set; }
        public ProgrammerService(DeviceManager deviceManager)
        {
            DeviceManager = deviceManager;
        }

        public string GetDevicePort(Alias deviceNumber)
        {
            string ret = string.Empty;
            var device = DeviceManager.GetDevice(deviceNumber);
            if (device != null)
                ret = device.DataSource.SourceName;
            return ret;
        }

        public int RunProgrammerProcess(Alias deviceNumber, string args)
        {
            int ret = 0;
            var deviceClientHost = DeviceManager.GetDeviceHostClient(deviceNumber);
            var runArgs = $"-c {deviceClientHost.Host.PName} {args}";
            DeviceManager.Log("Wait for background programming process to complete...", LogLevels.Information);
            ret = DeviceManager.RunProgrammerWithArgs(deviceClientHost, runArgs);
            return ret;
        }

        public void LoadDSK(Alias deviceNumber)
        {
            DeviceManager.LoadDSK(deviceNumber);
        }

        public byte[] GetPRK(Alias deviceNumber)
        {
            return DeviceManager.GetPRK(deviceNumber);
        }


        public string GetSecurityStatusInfo(Alias deviceNumber)
        {
            return DeviceManager.GetSecurityStatusInfo(deviceNumber);
        }

        private BootloaderPath GetOTABootloaderPath(Alias deviceNumber, params string[] regexFilters)
        {
            var deviceClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            var cs = deviceClient.Host.ChipSeries.Replace("xG28_", "").Replace("ZGM230S_", "");
            var bootLoaderSearchPatern = new[]
            {
                $"*ota-*{cs}*.s37",
                "OTA-bootloader-fg13-combined.s37"
            };

            return DeviceManager.FileSystemManager.
                GetBootloaderPath(deviceClient, bootLoaderSearchPatern, regexFilters);
        }

        private BootloaderPath GetOTWBootloaderPath(Alias deviceNumber, bool isSerialApiController, params string[] regexFilters)
        {
            BootloaderPath ret = null;
            var deviceClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            string[] bl_names;
            if (deviceClient.Host.ChipSeries.Contains("ZG14"))
            {
                bl_names = new[] { $"otw-*{deviceClient.Host.ChipSeries}*.s37", "OTW-bootloader-fg14-combined.s37" };
            }
            else if (deviceClient.Host.ChipSeries.Contains("xG28"))
            {
                bl_names = new[] { $"otw-*{deviceClient.Host.ChipSeries.Replace("xG28_", "")}*.s37", "OTW-bootloader-fg14-combined.s37" };
            }
            else if (isSerialApiController && !deviceClient.Host.ChipSeries.Contains("ZGM13") && !deviceClient.Host.ChipSeries.Contains("ZG13") && !deviceClient.Host.ChipSeries.Contains("RZ13"))
            {
                bl_names = new[] { $"otw-*{deviceClient.Host.ChipSeries}*.s37", "OTW-bootloader-fg13-combined.s37" };
            }
            else
            {
                bl_names = new[] { $"otw-*{deviceClient.Host.ChipSeries}*.s37", "OTW-bootloader-fg13-combined.s37" };
            }
            ret = DeviceManager.FileSystemManager.GetBootloaderPath(deviceClient, bl_names, regexFilters);
            return ret;
        }

        private BootloaderPath GetBootloaderKeysPath(Alias deviceNumber, bool isSerialApiController, params string[] regexFilters)
        {
            BootloaderPath ret = null;
            var deviceClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            ret = DeviceManager.FileSystemManager.GetBootloaderPath(deviceClient, null, regexFilters);
            return ret;
        }

        public int ProgramOTABootloader(Alias deviceNumber)
        {
            int ret = -1;
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            var bootloader = GetOTABootloaderPath(deviceNumber);
            ret = DeviceManager.ProgramBootLoader(deviceHostClient, bootloader);
            return ret;
        }

        public int ProgramOTWBootloader(Alias deviceNumber)
        {
            int ret = -1;
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            var bootloader = GetOTWBootloaderPath(deviceNumber, true);
            ret = DeviceManager.ProgramBootLoader(deviceHostClient, bootloader);
            return ret;
        }

        public int ProgramSampleWithBootloaderKeys(Alias deviceNumber, string filePath)
        {
            int ret = -1;
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            var bootloader = GetBootloaderKeysPath(deviceNumber, filePath.Contains("SerialAPI_Controller"));
            ret = DeviceManager.ProgramSampleWithBootLoaderKeys(deviceHostClient, DeviceManager.GetRegion(deviceHostClient.Frequency), filePath, bootloader);
            UpdateDeviceHostClientAfterProgramSample(deviceNumber, deviceHostClient);
            return ret;
        }

        public int ProgramSampleWithOTABootloaderImageAndKeys(Alias deviceNumber, string filePath)
        {
            int ret = -1;
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            var tempBasePath = DeviceManager.FileSystemManager.BasePath;
            var srcRoot = Directory.GetParent(filePath);
            DeviceManager.FileSystemManager.BasePath = string.Concat(srcRoot, ";", tempBasePath);
            var bootloader = GetOTABootloaderPath(deviceNumber);
            DeviceManager.FileSystemManager.BasePath = tempBasePath;
            ret = DeviceManager.ProgramSampleWithBootLoaderImageAndKeys(deviceHostClient, DeviceManager.GetRegion(deviceHostClient.Frequency), filePath, bootloader);
            UpdateDeviceHostClientAfterProgramSample(deviceNumber, deviceHostClient);
            return ret;
        }

        public int ProgramSampleWithOTWBootloaderImageAndKeys(Alias deviceNumber, string filePath)
        {
            int ret = -1;
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            var tempBasePath = DeviceManager.FileSystemManager.BasePath;
            var srcRoot = Directory.GetParent(filePath);
            DeviceManager.FileSystemManager.BasePath = string.Concat(srcRoot, ";", tempBasePath);
            var bootloader = GetOTWBootloaderPath(deviceNumber, filePath.Contains("SerialAPI_Controller"));
            DeviceManager.FileSystemManager.BasePath = tempBasePath;
            ret = DeviceManager.ProgramSampleWithBootLoaderImageAndKeys(deviceHostClient, DeviceManager.GetRegion(deviceHostClient.Frequency), filePath, bootloader);
            UpdateDeviceHostClientAfterProgramSample(deviceNumber, deviceHostClient);
            return ret;
        }

        private void UpdateDeviceHostClientAfterProgramSample(Alias deviceNumber, DeviceHostClient deviceHostClient)
        {
            System.Threading.Thread.Sleep(1000);
            DeviceManager.LoadDSK(deviceHostClient);
            var device = DeviceManager.GetDevice(deviceNumber);
            if (device != null)
            {
                device.GetVersion();
                if (DeviceManager.IsSerialApi(device.Library))
                {
                    if (!DeviceManager.IsOptionDisabled(ZWaveSet.NodeIdBase2, deviceNumber))
                    {
                        device.SerialApiSetNodeIdBaseType(2);
                    }
                    if (device.Network.IsSupportedSerialApiCommand((byte)ZWave.BasicApplication.Enums.CommandTypes.CmdSerialApiSetup))
                    {
                        var rfregion = (ZWave.BasicApplication.Enums.RfRegions)(Enum.Parse(typeof(ZWave.BasicApplication.Enums.RfRegions), deviceHostClient.Frequency));
                        device.SetRfRegion(rfregion);
                        device.SoftReset(); // After serialAPI Setup required! impacts on max Payload size:
                        device.GetMaxPayloadSize();
                        device.GetMaxLRPayloadSize();
                    }
                }
            }
        }

        public void ProgramSample(Alias deviceNumber, string filePath)
        {
            ProgramSample(deviceNumber, null, null, filePath);
        }

        public void ProgramSample(Alias deviceNumber, string publicKey, string privateKey, string filePath)
        {
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);

            byte[] prk = null;
            byte[] puk = null;
            try
            {
                prk = Tools.GetBytes(privateKey);
                puk = Tools.GetBytes(publicKey);
                if (prk != null && prk.Length != 32 && puk != null && puk.Length != 32)
                {
                    prk = null;
                    puk = null;
                }
            }
            catch { }

            DeviceManager.ProgramSample(deviceHostClient, DeviceManager.GetRegion(deviceHostClient.Frequency), filePath, prk, puk);
            UpdateDeviceHostClientAfterProgramSample(deviceNumber, deviceHostClient);
        }
        public void RenewDsk(Alias deviceNumber)
        {
            var dhc = DeviceManager.GetDeviceHostClient(deviceNumber);

            DeviceManager.RenewDsk(dhc);
            DeviceManager.LoadDSK(dhc);
            var device = DeviceManager.GetDevice(deviceNumber);
            if (device != null)
            {
                device.GetVersion();
            }
        }

        private string GblCreateWithCustomKeys(string hexFilePath, bool isSeries2 = false)
        {
            string gblFilePath = Path.GetFileName(hexFilePath).Replace(".hex", ".gbl");
            gblFilePath = Path.Combine(Path.GetTempPath(), gblFilePath);
            return DeviceManager.GblCreateCustom(hexFilePath, gblFilePath, null, isSeries2);
        }

        public int SetHomeId(Alias deviceNumber, byte[] homeId)
        {
            int ret = 0;
            var deviceClientHost = DeviceManager.GetDeviceHostClient(deviceNumber);
            ret = DeviceManager.SetHomeId(deviceClientHost, homeId);
            return ret;
        }

        public PowerMeasureResult GetPowerMeasure(Alias deviceNumber, int windowLenght)
        {
            string ret = string.Empty;
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            ret = DeviceManager.GetPowerMeasure(deviceHostClient, windowLenght);
            return new PowerMeasureResult(ret);
        }

        public BasicResult.Void ResetSample(Alias deviceNumber)
        {
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            DeviceManager.ResetSample(deviceHostClient);

            return null;
        }

        /// <summary>
        /// Read NVM3 to specific file and retrive Application Framework API Version
        /// </summary>
        /// <param name="deviceHostClient">Device host information.</param>
        /// <returns>Z-Wave Application Framework API Version from NVM3: [APP_VERSION, APP_REVISION, APP_PATCH]</returns>
        public byte[] GetApplicationVersion(Alias deviceNumber)
        {
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            return DeviceManager.GetZafApiVersion(deviceHostClient);
        }
    }

    public class PowerMeasureResult
    {
        public bool Completed { get; set; }
        public string RetVal { get; set; }
        public int AveragedOver { get; set; }
        public float Current { get; set; }
        public float Power { get; set; }
        public float Voltage { get; set; }

        public PowerMeasureResult(string value)
        {
            RetVal = value;
            Completed = false;
            if (!string.IsNullOrEmpty(value))
            {
                var rows = value.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                if (rows.Length == 5 && rows.Last().Contains("DONE"))
                {
                    System.IFormatProvider numform = new System.Globalization.CultureInfo("en-US");

                    var s = rows[0].Replace("Averaged over", "").Replace("ms:", "").Trim();
                    AveragedOver = int.TryParse(s, System.Globalization.NumberStyles.AllowDecimalPoint, numform, out int r) ? r : 0;

                    s = rows[1].ToLower().Replace("current", "").Replace("[ma]", "").Replace(":", "").Trim();
                    Current = float.TryParse(s, System.Globalization.NumberStyles.AllowDecimalPoint, numform, out float f) ? f : 0;

                    s = rows[2].ToLower().Replace("power", "").Replace("[mw]", "").Replace(":", "").Trim();
                    Power = float.TryParse(s, System.Globalization.NumberStyles.AllowDecimalPoint, numform, out f) ? f : 0;

                    s = rows[3].ToLower().Replace("voltage", "").Replace("[v]", "").Replace(":", "").Trim();
                    Voltage = float.TryParse(s, System.Globalization.NumberStyles.AllowDecimalPoint, numform, out f) ? f : 0;

                    Completed = true;
                }
            }

        }
    }

}
