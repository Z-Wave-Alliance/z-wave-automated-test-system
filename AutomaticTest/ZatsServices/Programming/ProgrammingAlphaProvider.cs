// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.IO;
using System.Linq;
using System.Threading;
using Utils;
using Zats.Services.Constants;
using ZWave.Enums;
using ZWave.Operations;

namespace Zats.Services
{
    public class ProgrammingAlphaProvider : ProgrammingProviderBase, IProgrammingProvider
    {
        public ProgrammingAlphaProvider(string appPath, Action<bool, string> logAction)
            : base(appPath, logAction) { }

        private void CloseAndOpen(DeviceHostClient deviceHostClient, Action action)
        {
            bool wasSClientOpen = false;
            bool wasPClientOpen = false;
            if (deviceHostClient.SClient != null && deviceHostClient.SClient.TransportClient.IsOpen)
            {
                wasSClientOpen = true;
                deviceHostClient.SClient.Disconnect();
            }
            if (deviceHostClient.PClient != null && deviceHostClient.PClient.TransportClient.IsOpen)
            {
                wasPClientOpen = true;
                deviceHostClient.PClient.Disconnect();
            }
            if (wasSClientOpen || wasPClientOpen)
            {
                Thread.Sleep(500);
            }
            action();
            if (wasSClientOpen)
            {
                deviceHostClient.SClient.Connect();
            }
            if (wasPClientOpen)
            {
                deviceHostClient.PClient.Connect();
            }
        }

        public int EraseSample(DeviceHostClient deviceHostClient)
        {
            int ret = 0;
            var port = deviceHostClient.Host.PName;
            var args = $"-c {port} -less -e";
            CloseAndOpen(deviceHostClient, () =>
            {
                for (int i = 0; i < 3; i++)
                {
                    ret = ProgrammerAction(args, "Erase Flash", 30000);
                    if (ret == 0)
                    {
                        break;
                    }
                }
            });
            return ret;
        }

        public byte[] GetDsk(DeviceHostClient deviceHostClient)
        {
            throw new NotImplementedException();
        }

        public byte[] GetPrk(DeviceHostClient deviceHostClient)
        {
            throw new NotImplementedException();
        }

        public string GetPowerMeasure(DeviceHostClient deviceHostClient, int windowLenght)
        {
            throw new NotImplementedException();
        }

        public bool HasFirmware(DeviceHostClient deviceHostClient)
        {
            bool ret = false;
            int attempts = 0;
            for (int i = 0; i < 3; i++)// in case seqNo is already tagged send with next seqNo
            {
                deviceHostClient.PClient.TransportClient.WriteData(new byte[8].Select(x => (byte)0x06).ToArray());
                deviceHostClient.PClient.TransportClient.WriteData(new byte[8]);
                ret = deviceHostClient.ProgRequest(ProgrammerCommandTypes.GetSoftwareVersion);
                if (ret)
                {
                    break;
                }
                else
                {
                    deviceHostClient.PClient.Disconnect();
                    deviceHostClient.PClient.Connect();
                    attempts++;
                }
            }
            LogProgrammerAction(ret, $"GetFirmwareVersion attempts:{attempts} port:{deviceHostClient.PClient.DataSource.SourceName}");
            return ret;
        }

        public string GetChipSeries(DeviceHostClient deviceHostClient)
        {
            string ret = null;
            RequestResult chipRet = null;
            using (deviceHostClient.ProgMode())
            {
                chipRet = deviceHostClient.ProgRequest(ProgrammerCommandTypes.ReadSignatureByte, 0x00);
                if (chipRet && chipRet.Payload != null && chipRet.Payload.Length > 6
                    && chipRet.Payload[4] == 0x7F && chipRet.Payload[5] == 0x1F)
                {
                    if (chipRet.Payload[6] == 0x04)
                    {
                        ret = ChipSeries.ZW050x.ToString();
                    }
                    else if (chipRet.Payload[6] == 0x00 && chipRet.Payload[7] >= 0x06)
                    {
                        ret = ChipSeries.ZW030x.ToString();
                    }
                    else if (chipRet.Payload[6] == 0x00 && chipRet.Payload[7] < 0x06)
                    {
                        ret = ChipSeries.ZW020x.ToString();
                    }
                }
            }
            deviceHostClient.Host.ChipSeries = ret?.ToString();
            LogProgrammerAction(chipRet, $"GetChipSeries chip:{ret} port:{deviceHostClient.PClient.DataSource.SourceName}");
            return ret;
        }

        public int ProgramSample(DeviceHostClient deviceHostClient, ZWaveRegions? region, string samplePath, bool isAddS2Keypair, byte[] prk, byte[] puk)
        {
            int ret = 0;
            deviceHostClient.SClient?.Disconnect();
            deviceHostClient.PClient.Disconnect();

            ret = ProgramAlphaSample(deviceHostClient.Host.PName, samplePath, isAddS2Keypair, prk, puk);
            if (ret == 0)
            {
                Thread.Sleep(1000);
            }
            if (ret == 0 && deviceHostClient.Host.ChipSeries == ChipSeries.ZW050x.ToString())
            {
                EraseNvm(deviceHostClient.Host.PName, 1);
            }
            else
            {
                var file = Path.Combine(Path.GetDirectoryName(samplePath), "extern_eep.hex");
                if (File.Exists(file))
                {
                    ret = ProgramNvm(deviceHostClient.Host.PName, file);
                }
            }
            deviceHostClient.PClient.Connect();

            if (ret == 0)
            {
                Thread.Sleep(7000);
            }

            deviceHostClient.SClient?.Connect();
            return ret;
        }

        public int BackUpSample(DeviceHostClient deviceHostClient, string backUpPath)
        {
            int ret = 0;
            deviceHostClient.SClient?.Disconnect();
            deviceHostClient.PClient.Disconnect();

            var port = deviceHostClient.Host.PName;
            var args = string.Format($"-c {port} -pr {backUpPath}");
            ProgrammerAction(args, "BackUp", 4 * 60 * 1000, out string output);

            deviceHostClient.SClient?.Connect();

            return ret;
        }

        public int ResetSample(DeviceHostClient deviceHostClient)
        {
            using (deviceHostClient.ProgMode())
            {
                Thread.Sleep(100);
            }
            Thread.Sleep(5000);
            deviceHostClient.SClient?.Disconnect();
            LogProgrammerAction(true, $"Reset port:{deviceHostClient.Host.PName}");
            deviceHostClient.SClient?.Connect();

            return 0;
        }

        private int ProgramNvm(string port, string fileName)
        {
            int ret = 1;
            var args = string.Format(@"-c {0} -pe ""{1}"" -less", port, fileName);
            for (int i = 0; i < 4; i++)
            {
                ret = ProgrammerAction(args, "Program Nvm/Eeprom", 60000);
                if (ret == 0)
                {
                    break;
                }
            }
            return ret;
        }

        private int EraseNvm(string port, int pagesCount)
        {
            int ret = 1;
            //var args = $"-c {port} -pe {pagesCount}";
            var args = $"-c {port} -pe 1";
            for (int i = 0; i < 4; i++)
            {
                ret = ProgrammerAction(args, "Erase Nvm/Eeprom", 160000);
                if (ret == 0)
                {
                    break;
                }
            }
            return ret;
        }

        private int BackUpAlphaSample(string port, string backUpPath)
        {
            int ret = 1;

            var args = $@"c {port} -r ""{backUpPath}""";

            ret = ProgrammerAction(args, "Back Up NVM", 300000);

            return ret;
        }

        private int ProgramAlphaSample(string port, string samplePath, bool isAddS2Keypair, byte[] prk, byte[] puk)
        {
            int ret = 1;
            var args = $@"-c {port} -p ""{samplePath}"" -less";
            if (isAddS2Keypair)
            {
                if (prk != null && puk != null && prk.Length == 32 && puk.Length == 32)
                {
                    args += $@" -pks {Tools.GetHexShort(prk)} {Tools.GetHexShort(puk)}";
                }
                else
                {
                    args += " -pks";
                }
            }

            for (int i = 0; i < 4; i++)
            {
                ret = ProgrammerAction(args, "Program Flash", 300000);
                if (ret == 0)
                {
                    break;
                }
            }
            return ret;
        }

        public int ProgramBootLoader(DeviceHostClient deviceHostClient, string[] tokenPaths, string bootLoaderPath)
        {
            throw new NotImplementedException();
        }

        public int ProgramSampleWithBootLoaderKeys(DeviceHostClient deviceHostClient, ZWaveRegions? region, string samplePath, string[] tokenPaths)
        {
            throw new NotImplementedException();
        }

        public int ProgramSampleWithBootLoaderImageAndKeys(DeviceHostClient deviceHostClient, ZWaveRegions? region, string samplePath, string[] tokenPaths, string bootLoaderPath)
        {
            throw new NotImplementedException();
        }

        public int SetHomeId(DeviceHostClient deviceHostClient, byte[] homeId)
        {
            int ret = 0;
            using (deviceHostClient.ProgMode())
            {
                using (deviceHostClient.NvmMode())
                {
                    deviceHostClient.ProgRequest(ProgrammerCommandTypes.StoreHomeId, 0x00, homeId[0], homeId[1], homeId[2], homeId[3]);
                }
            }
            return ret;
        }

        public int Run(DeviceHostClient deviceHostClient, string args)
        {
            int ret = 0;
            if (HasFirmware(deviceHostClient))
            {
                deviceHostClient.SClient?.Disconnect();
                deviceHostClient.PClient.Disconnect();
                ret = ProgrammerAction(args, "Run", 300000);
                Thread.Sleep(8000); // startup
                deviceHostClient.SClient?.Connect();
                deviceHostClient.PClient.Connect();
            }
            return ret;
        }

        public int GblCreate(string hexFilePath, string gblFilePath, string signKeyFilePath, string encryptKeyFilePath)
        {
            throw new NotImplementedException();
        }

        public string GetVersion()
        {
            throw new NotImplementedException();
        }

        public int RenewDsk(DeviceHostClient deviceHostClient)
        {
            throw new NotImplementedException();
        }

        public string GetSecurityStatusInfo(DeviceHostClient deviceHostClient)
        {
            throw new NotImplementedException();
        }

        public ZWaveRegions GetZWaveRegion(DeviceHostClient deviceHostClient)
        {
            throw new NotImplementedException();
        }
    }

    internal static class ClientExtensions
    {
        public static ProgModeDispather ProgMode(this DeviceHostClient client)
        {
            return new ProgModeDispather(client);
        }

        public static NvmModeDispather NvmMode(this DeviceHostClient client)
        {
            return new NvmModeDispather(client);
        }

        public static RequestResult ProgRequest(this DeviceHostClient client, ProgrammerCommandTypes cmd, params byte[] payload)
        {
            int timeout = 1000;
            switch (cmd)
            {
                case ProgrammerCommandTypes.GetSoftwareVersion:
                    timeout = 50;
                    break;
                default:
                    break;
            }
            var data = new byte[] { 0xEF, 0x00, (byte)cmd }.Concat(payload ?? new byte[0]).ToArray();
            var mask = new ByteIndex[] { ByteIndex.AnyValue, 0x01, (byte)cmd };
            return client.PClient.Request(data, mask, timeout);
        }
    }

    internal abstract class BaseDispatcher : IDisposable
    {
        protected DeviceHostClient _client;
        protected bool _result;
        public BaseDispatcher(DeviceHostClient client)
        {
            _client = client;
        }

        public abstract void Begin();
        public abstract void End();

        public void Dispose()
        {
            End();
        }

        public static implicit operator bool(BaseDispatcher dispather)
        {
            return dispather._result;
        }
    }

    internal class ProgModeDispather : BaseDispatcher
    {
        public ProgModeDispather(DeviceHostClient client)
            : base(client)
        {
            Begin();
        }

        public override void Begin()
        {
            _result = _client.ProgRequest(ProgrammerCommandTypes.ProgEnable);
        }

        public override void End()
        {
            _client.ProgRequest(ProgrammerCommandTypes.ProgDisable);
        }
    }

    internal class NvmModeDispather : BaseDispatcher
    {
        public NvmModeDispather(DeviceHostClient client)
              : base(client)
        {
            Begin();
        }

        public override void Begin()
        {
            _result = _client.ProgRequest(ProgrammerCommandTypes.ToggleEeprom, 0x01, (byte)_client.PClient.ChipType);
        }

        public override void End()
        {
            _client.ProgRequest(ProgrammerCommandTypes.ToggleEeprom, 0x00, (byte)_client.PClient.ChipType);
        }
    }
}
