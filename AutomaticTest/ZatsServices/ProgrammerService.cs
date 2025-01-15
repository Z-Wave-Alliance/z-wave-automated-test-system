// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Utils;
using Utils.UI.Enums;
using ZWave.Enums;

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

        private BootloaderPath GetBootloaderPath(DevKits devKit, string[] regexFilters, BootloaderPath ret, DeviceHostClient deviceClient, string[] fileNamePatterns, string encryptKeyFileName, string signKeyFileName)
        {
            string path = null;
            foreach (var fileNamePattern in fileNamePatterns)
            {
                path = DeviceManager.FileSystemManager.GetLatestFilePath(fileNamePattern, devKit, regexFilters);
                if (path != null && File.Exists(path))
                {
                    break;
                }
            }

            if (path != null && File.Exists(path))
            {
                ret = new BootloaderPath
                {
                    EncryptKey = Path.Combine(Path.GetDirectoryName(path), encryptKeyFileName),
                    SignKey = Path.Combine(Path.GetDirectoryName(path), signKeyFileName),
                    Bootloader = path
                };
                if (!File.Exists(ret.EncryptKey))
                {
                    var location = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources");
                    ret.EncryptKey = Path.Combine(location, encryptKeyFileName);
                }
                if (!File.Exists(ret.SignKey))
                {
                    var location = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources");
                    ret.SignKey = Path.Combine(location, signKeyFileName);
                }
            }
            else
            {
                AssertionException.Throw($"Bootloader file for {deviceClient.Host.ChipSeries} not found");
            }

            return ret;
        }

        private BootloaderPath GetOTABootloaderPath(Alias deviceNumber, DevKits devKit, params string[] regexFilters)
        {
            BootloaderPath ret = null;
            var deviceClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            if (deviceClient.Host.ChipSeries.Contains("xG28"))
            {
                ret = GetBootloaderPath(devKit, regexFilters, ret, deviceClient, new[] { $"ota-*{deviceClient.Host.ChipSeries.Replace("xG28_", "")}*.s37", "OTA-bootloader-fg13-combined.s37" },
                    DeviceManager.SampleEncryptKeyFileName, DeviceManager.SampleSignKeyFileName);
                return ret;
            }
            else
            {
                ret = GetBootloaderPath(devKit, regexFilters, ret, deviceClient, new[] { $"ota-*{deviceClient.Host.ChipSeries}*.s37", "OTA-bootloader-fg13-combined.s37" },
                    DeviceManager.SampleEncryptKeyFileName, DeviceManager.SampleSignKeyFileName);
                return ret;
            }
                
        }

        private BootloaderPath GetOTWBootloaderPath(Alias deviceNumber, bool isSerialApiController, DevKits devKit, params string[] regexFilters)
        {
            BootloaderPath ret = null;
            var deviceClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            if (deviceClient.Host.ChipSeries.Contains("ZG14"))
            {
                ret = GetBootloaderPath(devKit, regexFilters, ret, deviceClient, new[] { $"otw-*{deviceClient.Host.ChipSeries}*.s37", "OTW-bootloader-fg14-combined.s37" },
                    DeviceManager.ControllerEncryptKeyFileName, DeviceManager.ControllerSignKeyFileName);
            }
            else if (deviceClient.Host.ChipSeries.Contains("xG28"))
            {
                ret = GetBootloaderPath(devKit, regexFilters, ret, deviceClient, new[] { $"otw-*{deviceClient.Host.ChipSeries.Replace("xG28_", "")}*.s37", "OTW-bootloader-fg14-combined.s37" },
                    DeviceManager.ControllerEncryptKeyFileName, DeviceManager.ControllerSignKeyFileName);
            }
            else if (isSerialApiController && !deviceClient.Host.ChipSeries.Contains("ZGM13") && !deviceClient.Host.ChipSeries.Contains("ZG13") && !deviceClient.Host.ChipSeries.Contains("RZ13"))
            {
                ret = GetBootloaderPath(devKit, regexFilters, ret, deviceClient, new[] { $"otw-*{deviceClient.Host.ChipSeries}*.s37", "OTW-bootloader-fg13-combined.s37" },
                    DeviceManager.ControllerEncryptKeyFileName, DeviceManager.ControllerSignKeyFileName);
            }
            else
            {
                ret = GetBootloaderPath(devKit, regexFilters, ret, deviceClient, new[] { $"otw-*{deviceClient.Host.ChipSeries}*.s37", "OTW-bootloader-fg13-combined.s37" },
                    DeviceManager.SampleEncryptKeyFileName, DeviceManager.SampleSignKeyFileName);
            }
            return ret;
        }

        private BootloaderPath GetBootloaderKeysPath(Alias deviceNumber, bool isSerialApiController, DevKits devKit, params string[] regexFilters)
        {
            BootloaderPath ret = null;
            var deviceClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            if (isSerialApiController && !deviceClient.Host.ChipSeries.Contains("ZGM13") && !deviceClient.Host.ChipSeries.Contains("ZG13") && !deviceClient.Host.ChipSeries.Contains("RZ13"))
            {
                ret = DeviceManager.GetBootloaderKeysPath(DeviceManager.ControllerEncryptKeyFileName, DeviceManager.ControllerSignKeyFileName);
            }
            else
            {
                ret = DeviceManager.GetBootloaderKeysPath(DeviceManager.SampleEncryptKeyFileName, DeviceManager.SampleSignKeyFileName);
            }
            return ret;
        }

        public int ProgramOTABootloader(Alias deviceNumber)
        {
            int ret = -1;
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            var bootloader = GetOTABootloaderPath(deviceNumber, DevKits.Default);
            ret = DeviceManager.ProgramBootLoader(deviceHostClient, new string[] { bootloader.EncryptKey, bootloader.SignKey }, bootloader.Bootloader);
            return ret;
        }

        public int ProgramOTWBootloader(Alias deviceNumber)
        {
            int ret = -1;
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            var bootloader = GetOTWBootloaderPath(deviceNumber, true, DevKits.Default);
            ret = DeviceManager.ProgramBootLoader(deviceHostClient, new string[] { bootloader.EncryptKey, bootloader.SignKey }, bootloader.Bootloader);
            return ret;
        }

        public int ProgramSampleWithBootloaderKeys(Alias deviceNumber, string filePath)
        {
            int ret = -1;
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            var bootloader = GetBootloaderKeysPath(deviceNumber, filePath.Contains("SerialAPI_Controller"), DevKits.Default);
            ret = DeviceManager.ProgramSampleWithBootLoaderKeys(deviceHostClient, DeviceManager.GetRegion(deviceHostClient.Frequency), filePath, new string[] { bootloader.EncryptKey, bootloader.SignKey });
            UpdateDeviceHostClientAfterProgramSample(deviceNumber, deviceHostClient);
            return ret;
        }

        public int ProgramSampleWithOTABootloaderImageAndKeys(Alias deviceNumber, string filePath)
        {
            int ret = -1;
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            var bootloader = GetOTABootloaderPath(deviceNumber, DevKits.Default);
            ret = DeviceManager.ProgramSampleWithBootLoaderImageAndKeys(deviceHostClient, DeviceManager.GetRegion(deviceHostClient.Frequency), filePath, new string[] { bootloader.EncryptKey, bootloader.SignKey }, bootloader.Bootloader);
            UpdateDeviceHostClientAfterProgramSample(deviceNumber, deviceHostClient);
            return ret;
        }

        public int ProgramSampleWithOTWBootloaderImageAndKeys(Alias deviceNumber, string filePath)
        {
            int ret = -1;
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            var bootloader = GetOTWBootloaderPath(deviceNumber, filePath.Contains("SerialAPI_Controller"), DevKits.Default);
            ret = DeviceManager.ProgramSampleWithBootLoaderImageAndKeys(deviceHostClient, DeviceManager.GetRegion(deviceHostClient.Frequency), filePath, new string[] { bootloader.EncryptKey, bootloader.SignKey }, bootloader.Bootloader);
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

        private string GblCreateWithCustomKeys(string hexFilePath)
        {
            string gblFilePath = Path.GetFileName(hexFilePath).Replace(".hex", ".gbl");
            gblFilePath = Path.Combine(Path.GetTempPath(), gblFilePath);
            return DeviceManager.GblCreateCustom(hexFilePath, gblFilePath);
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
