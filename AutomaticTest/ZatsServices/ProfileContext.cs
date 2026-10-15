// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using System;
using System.Linq;
using ZWave.BasicApplication.Devices;

namespace Zats.Services
{
    public class ProfileContext
    {
        public DeviceManager Manager { get; set; }
        public string Frequency { get; set; }
        public string DutName { get; set; }
        public ProfileContext(DeviceManager manager)
        {
            Manager = manager;
        }

        public string GetHostChipSeries(Alias deviceNumber)
        {
            var device = Manager.GetDeviceHostClient(deviceNumber);
            return device.Host.ChipSeries;
        }

        public AliasGroup GetAliasGroup(byte group)
        {
            AliasGroup ret = new AliasGroup(group, Manager.Devices.Keys.Where(x => x.Group == group).ToArray());
            return ret;
        }

        public bool IsZW080x(Alias deviceNumber)
        {
            return Manager.IsZW080x(deviceNumber);
        }

        public bool IsZW070x(Alias deviceNumber)
        {
            return Manager.IsZW070x(deviceNumber);
        }

        public bool IsZW030x(Alias deviceNumber)
        {
            return Manager.IsZW030x(deviceNumber);
        }

        public bool IsLongRange(Alias deviceNumber)
        {
            bool ret = false;
            var device = Manager.GetDeviceHostClient(deviceNumber);
            ret = device.IsLongRange;
            return ret;
        }

        public bool IsNodeIdBaseType2(Alias deviceNumber)
        {
            bool ret = false;
            var device = Manager.GetDeviceHostClient(deviceNumber);
            if (device.SClient as Device != null)
            {
                ret = (device.SClient as Device).Network.IsNodeIdBaseTypeLR;
            }
            return ret;
        }

        public bool IsZW050x(Alias deviceNumber)
        {
            return Manager.IsZW050x(deviceNumber);
        }

        public ChannelConfigs ChannelConfig
        {
            get
            {
                ChannelConfigs ret = ChannelConfigs.Ch12;
                if (Is3Channel())
                {
                    ret = ChannelConfigs.Ch3;
                }
                else if (RfRegionsExtensions.IsLongRangeRfRegion(Frequency))
                {
                    ret = ChannelConfigs.ChLR;
                }
                return ret;
            }
        }

        private bool Is3Channel()
        {
            return new[]
            {
                "KR",
                "JP",
                "TF_835_3CH",
                "TF_840_3CH",
                "TF_850_3CH",
                "TF_932_3CH",
                "TF_940_3CH",
                }.Contains(Frequency);
        }

        public string GetDataSource(Alias deviceNumber)
        {
            var device = Manager.GetDeviceHostClient(deviceNumber);
            return device.SClient?.DataSource?.SourceName;
        }

        public LibTypes GetLibrary(Alias deviceNumber)
        {
            var device = Manager.GetDeviceHostClient(deviceNumber);
            return (LibTypes)device.Library;
        }
    }
}
