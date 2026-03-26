// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using System;
using System.IO;
using System.Text.RegularExpressions;
using Utils;
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

        /// <summary>
        /// Executes programmer application CLI command directly on the specified device.
        /// </summary>
        /// <param name="deviceNumber">Device</param>
        /// <param name="args">CLI arguments to pass to the programmer application. Should not include device-specific arguments.</param>
        /// <param name="timeoutMs">Timeout in milliseconds for the programming operation. Default is 30000ms (30 seconds).</param>
        /// <returns>
        /// Exit code from the programmer application: 0 - Success.
        /// </returns>
        public int RunProgrammerProcess(Alias deviceNumber, string args, int timeoutMs = 30000)
        {
            var ret = DeviceManager.RunProgrammerWithArgs(deviceNumber, args, timeoutMs);
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

        private BootloaderPath GetOTABootloaderPath(Alias deviceNumber, string specificBootloaderName = null, params string[] regexFilters)
        {
            var deviceClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            var cs = deviceClient.Host.ChipSeries.Replace("xG28_", "").Replace("ZGM230S_", "");
            // TODO: move to FileSystemManager
            var bootloaderSearchPattern = !string.IsNullOrEmpty(specificBootloaderName) ?
                new[] { $"*{specificBootloaderName}*{cs}*.s37" } :
                new[]
                {
                    $"*ota*{cs}*.s37",
                    "OTA-bootloader-fg13-combined.s37"
                };

            return DeviceManager.FileSystemManager.GetBootloaderPath(deviceClient, bootloaderSearchPattern, regexFilters);
        }

        private BootloaderPath GetOTWBootloaderPath(Alias deviceNumber, string specificBootloaderName = null, params string[] regexFilters)
        {
            BootloaderPath ret = null;
            var deviceClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            // TODO: move to FileSystemManager
            string[] bootloaderSearchPattern;
            var brdId = deviceClient.Host.ChipSeries.Replace("xG28_", "").Replace("ZGM230S_", "");
            if (!string.IsNullOrEmpty(specificBootloaderName))
            {
                bootloaderSearchPattern = new[] { $"*{specificBootloaderName}*{brdId}*.s37" };
            }
            else if (deviceClient.Host.ChipSeries.Contains("ZG14") || deviceClient.Host.ChipSeries.Contains("xG28"))
            {
                bootloaderSearchPattern = new[] { $"otw*{brdId}*.s37", "OTW-bootloader-fg14-combined.s37" };
            }
            else
            {
                bootloaderSearchPattern = new[] { $"otw*{brdId}*.s37", "OTW-bootloader-fg13-combined.s37" };
            }

            ret = DeviceManager.FileSystemManager.GetBootloaderPath(deviceClient, bootloaderSearchPattern, regexFilters);
            return ret;
        }

        private BootloaderPath GetBootloaderPath(Alias deviceNumber, bool isOTW = false, string specificBootloaderName = null, params string[] regexFilters)
        {
            BootloaderPath ret = null;
            var deviceClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            var bootloader = isOTW ?
                GetOTWBootloaderPath(deviceNumber, specificBootloaderName, regexFilters) :
                GetOTABootloaderPath(deviceNumber, specificBootloaderName, regexFilters);

            if (!string.IsNullOrWhiteSpace(specificBootloaderName) &&
                string.IsNullOrEmpty(bootloader.Bootloader))
            {
                throw new ArgumentException($"Specific Bootloader not found for the next filter: {specificBootloaderName}");
            }

            ret = bootloader;
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
            var bootloader = GetOTWBootloaderPath(deviceNumber);
            ret = DeviceManager.ProgramBootLoader(deviceHostClient, bootloader);
            return ret;
        }

        //TODO: Rename to ProgramSample and remove duplications 'ProgramSampleWithOTWBootloaderImageAndKeys' (refactor)
        public int ProgramSampleWithBootloaderKeys(Alias deviceNumber, string filePath)
        {
            var isOTW = DeviceManager.GetDeviceHostClient(deviceNumber).Library.IsSerialApi();
            return ProgramSampleWithOTWBootloaderImageAndKeys(deviceNumber, filePath, isOTW);
        }

        //TODO: Rename to ProgramSample and remove duplications 'ProgramSampleWithOTWBootloaderImageAndKeys' (refactor)
        public int ProgramSampleWithOTABootloaderImageAndKeys(Alias deviceNumber, string filePath, bool isOTW = false, string specificBootloaderName = null)
        {
            return ProgramSampleWithOTWBootloaderImageAndKeys(deviceNumber, filePath, false, specificBootloaderName);
        }

        //TODO: remove duplicate (see: refactor)
        public int ProgramSampleWithOTWBootloaderImageAndKeys(Alias deviceNumber, string filePath, bool isOTW = true, string specificBootloaderName = null)
        {
            int ret = -1;
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            var bootloader = GetBootloaderPath(deviceNumber, isOTW, specificBootloaderName);

            ret = DeviceManager.ProgramSampleWithBootLoaderImageAndKeys(deviceHostClient, DeviceManager.ConvertToZWaveRegion(deviceHostClient.Frequency), filePath, bootloader);
            UpdateDeviceHostClientAfterProgramSample(deviceNumber, deviceHostClient);
            return ret;
        }

        private void UpdateDeviceHostClientAfterProgramSample(Alias deviceNumber, DeviceHostClient deviceHostClient, string bootloaderFilter = null)
        {
            System.Threading.Thread.Sleep(1000);
            DeviceManager.LoadDSK(deviceHostClient);
            //DeviceManager.InitDeviceHostClient(deviceHostClient); >> PresetSerialAPIDevice ?
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
                        if (Enum.TryParse(deviceHostClient.Frequency, out ZWave.BasicApplication.Enums.RfRegions rfRegion))
                        {
                            device.SetRfRegion(rfRegion);
                            device.SoftReset(); // After serialAPI Setup required! impacts on max Payload size:
                        }
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

            DeviceManager.ProgramSample(deviceHostClient, DeviceManager.ConvertToZWaveRegion(deviceHostClient.Frequency), filePath, prk, puk);
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

        /// <summary>
        /// Retrieve Z-Wave region from device using programmer.
        /// </summary>
        public byte[] ReadDSK(Alias deviceNumber)
        {
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            return DeviceManager.ReadDSK(deviceHostClient);
        }

        /// <summary>
        /// Retrieve Z-Wave region from device using programmer.
        /// </summary>
        public ZWave.Enums.ZWaveRegions ReadZwRegion(Alias deviceNumber)
        {
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            return DeviceManager.ReadZwRegion(deviceHostClient);
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
                Completed = true;
                AveragedOver = ParseInt(value, @"Averaged over\s+(\d+)\s*ms");
                Current = ParseFloat(value, @"Current\s*\[mA\]\s*:\s*([\d.]+)");
                Power = ParseFloat(value, @"Power\s*\[mW\]\s*:\s*([\d.]+)");
                Voltage = ParseFloat(value, @"Voltage\s*\[V\]\s*:\s*([\d.]+)");
            }
        }

        private static int ParseInt(string input, string pattern)
        {
            var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out int result) ? result : 0;
        }

        private static float ParseFloat(string input, string pattern)
        {
            var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
            return match.Success && float.TryParse(match.Groups[1].Value, out float result) ? result : 0;
        }
    }

}
