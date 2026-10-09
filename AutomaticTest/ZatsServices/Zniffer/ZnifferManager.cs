// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using System;
using System.Collections.Generic;
using System.Net;
using ZWave;
using ZWave.Enums;
using ZWave.Layers;
using ZWave.Layers.Transport;
using ZWave.Xml.Application;
using ZWave.Xml.FrameHeader;
using ZWave.ZnifferApplication;
using ZWave.ZnifferApplication.Devices;
using ZWave.ZnifferApplication.Enums;
using ZWave.Layers.Application;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;

namespace Zats.Services
{
    public class ZnifferManager : DeviceManagerBase
    {
        public ZnifferApplicationLayer ZnifferApplicationLayer { get; set; }
        public ZnifferApplicationLayer SnifferPtiApplicationLayer { get; set; }
        public ISnifferDevice Zniffer { get; set; }
        public IApplicationClient ZnifferAppClient { get { return Zniffer as IApplicationClient; } }
        public ZWaveDefinition ZWaveDefinition { get; set; }
        public FrameDefinition FrameDefinition { get; private set; }
        public string Port { get; set; }

        public ZnifferManager(FrameDefinition frameDefinition)
            : base()
        {
            IsSuppressLogDebug = true;
            FrameDefinition = frameDefinition;
            ZnifferApplicationLayer = new ZnifferApplicationLayer(SessionLayer, new ZnifferFrameLayer(FrameDefinition), new SerialPortTransportLayer() { SuppressDebugOutput = true });
            SnifferPtiApplicationLayer = new ZnifferApplicationLayer(SessionLayer, new SnifferPtiFrameLayer(FrameDefinition), new TcpClientTransportLayer() { SuppressDebugOutput = true });
        }

        public bool InitDevice(string port, bool isVirtual, string rfRegion, LongRangeChannels lrChannel)
        {
            if (string.IsNullOrEmpty(rfRegion))
                return false;

            bool ret = false;
            Port = port;
            if (IPAddress.TryParse(port, out IPAddress ptiSource))
            {
                Zniffer = SnifferPtiApplicationLayer.CreateSnifferPti();
                var res = ZnifferAppClient.Connect(new SocketDataSource(port, 4905));
                ret = res == CommunicationStatuses.Done;
                Zniffer.Frequencies = Array.Empty<byte>();
            }
            else
            {
                Zniffer zniffer = ZnifferApplicationLayer.CreateZniffer();
                bool tmp = zniffer.TransportClient.SuppressDebugOutput;
                zniffer.TransportClient.SuppressDebugOutput = false;
                Dictionary<byte, RFrequency> supportedFrequencies = StartZniffer(zniffer, port);

                if (supportedFrequencies != null)
                {
                    if (RfRegionsExtensions.IsLongRangeRfRegion(rfRegion))
                    {
                        TryParseZnifferVersion(zniffer, out int majorVersion, out int minorVersion);

                        if (!IsRfRegionSupportedByZniffer(rfRegion, isVirtual, zniffer.ChipType, majorVersion, minorVersion))
                        {
                            // WARNING: Workaround that allows Zniffer to work in classic mode even in case of setting an LR region.
                            // Find corresponding region from available list below and apply it for non-LR tests.
                            rfRegion = RfRegionsExtensions.GetCorrespondingClassicRfRegion(rfRegion).ToString();
                        }
                        else
                        {
                            // Not supported unhandled message!
                            zniffer.GetLRFRegions();
                            zniffer.GetLRConfigs();
                        }
                    }

                    if (FindFrequencyRegionCodeForZniffer(rfRegion, supportedFrequencies, out byte freqCode, out RFrequency freq))
                    {
                        zniffer.RfRegionName = freq?.Name ?? "";
                        zniffer.Frequency = freqCode;
                        zniffer.SetFrequency(freqCode);
                        AppendFrequencyHeaders(supportedFrequencies);

                        if (RfRegionsExtensions.IsLongRangeRfRegion(rfRegion))
                        {
                            byte lrChannelConfigCode = (byte)GetDefaultZnifferLRChannelConfig(lrChannel);
                            zniffer.LrConfig = lrChannelConfigCode;
                            zniffer.SetLRChannelConfig(lrChannelConfigCode);
                        }

                        ret = zniffer.Start();
                    }
                }
                zniffer.TransportClient.SuppressDebugOutput = tmp;
                Zniffer = zniffer;
            }
            return ret;
        }

        public static bool TryParseZnifferVersion(Zniffer zniffer, out int majorVersion, out int minorVersion)
        {
            majorVersion = 0;
            minorVersion = 0;

            if (zniffer == null || string.IsNullOrEmpty(zniffer.Version))
            {
                return false;
            }

            try
            {
                string[] znifferVersion = zniffer.Version.Split(new char[] { '.', ' ' });
                majorVersion = znifferVersion.Length > 0 ? int.Parse(znifferVersion[0]) : 0;
                minorVersion = znifferVersion.Length > 1 ? int.Parse(znifferVersion[1]) : zniffer.Revision;
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Old Zniffers (older than 700) do not support LR in general.
        /// Older FWs do not support specific LR regions.
        /// Classic regions are supported.
        /// </summary>
        public static bool IsRfRegionSupportedByZniffer(string rfRegionStr, bool isVirtual, 
            ChipTypes znifferChipType, int znifferMajorVersion, int znifferMinorVersion)
        {
            if (string.IsNullOrEmpty(rfRegionStr) || !Enum.TryParse(rfRegionStr, out RfRegions rfRegion))
            {
                return false;
            }

            if (isVirtual)
            {
                // Consider all virtual zniffers as supporting all RF regions.
                return true;
            }

            switch (znifferChipType)
            {
                case ChipTypes.ZW050x:
                case ChipTypes.ZW070x:
                case ChipTypes.ZW080x:
                    {
                        // Silicon Labs 500, 700 and 800 series devices

                        if (!RfRegionsExtensions.IsLongRangeRfRegion(rfRegionStr))
                        {
                            // All classic Z-Wave RF regions are supported.
                            return true;
                        }

                        // Long Range RF Regions

                        if (znifferChipType == ChipTypes.ZW050x)
                        {
                            return false;
                        }

                        switch (rfRegion)
                        {
                            case RfRegions.US_LR:
                                return znifferMajorVersion > 10 || (znifferMajorVersion == 10 && znifferMinorVersion >= 15);
                            case RfRegions.EU_LR:
                                // Silabs Z-Wave SDK 7.22 (Zniffer v10.22) did already support EU_LR but on now obsolete frequencies only.
                                return znifferMajorVersion > 10 || (znifferMajorVersion == 10 && znifferMinorVersion >= 23);
                            default:
                                return false;
                        }
                    }
                default:
                    return false;
            }
        }

        public static bool FindFrequencyRegionCodeForZniffer(string rfRegion, Dictionary<byte, RFrequency> supportedFrequencies, out byte freqCode, out RFrequency freq)
        {
            // Find corresponding region code from from supported values to set it up.

            freqCode = 0; // EU fallback
            try
            {
                freq = supportedFrequencies[freqCode];
            }
            catch
            {
                freq = new RFrequency(0, "EU");
            }

            if (supportedFrequencies == null || supportedFrequencies.Count == 0)
            {
                return false;
            }

            // Try to find the exact region string

            foreach (KeyValuePair<byte, RFrequency> suppFreq in supportedFrequencies)
            {
                if (suppFreq.Value.Name.Equals(rfRegion)) // "Equals" because we try to find the exactly matching region first.
                {
                    freqCode = suppFreq.Key;
                    freq = suppFreq.Value;
                    return true;
                }
            }

            // If the exact region string was not found, try to find the region string that starts with the region name.

            foreach (KeyValuePair<byte, RFrequency> suppFreq in supportedFrequencies)
            {
                if (suppFreq.Value.Name.StartsWith(rfRegion)) // "Start with" because result could be like "EU, 868.42 MHz" for 500 series
                                                              // instead of exact comparison string
                {
                    freqCode = suppFreq.Key;
                    freq = suppFreq.Value;
                    return true;
                }
            }

            return false;
        }

        public static ZnifferChannelConfig GetDefaultZnifferLRChannelConfig(LongRangeChannels lrChannel)
        {
            switch (lrChannel)
            {
                case LongRangeChannels.ChannelA:
                    return ZnifferChannelConfig.ClassicChannelsAndLRChannelA;
                case LongRangeChannels.ChannelB:
                    return ZnifferChannelConfig.ClassicChannelsAndLRChannelB;
                default:
                    return ZnifferChannelConfig.ClassicChannelsAndLRChannelA;
            }
        }

        public static string GetZnifferChannelConfigDescription(ZnifferChannelConfig channelConfig)
        {
            return ZnifferChannelConfigurations[channelConfig];
        }

        /// <summary>
        /// Zniffer channel configurations and its descriptions
        /// </summary>
        private static readonly Dictionary<ZnifferChannelConfig, string> ZnifferChannelConfigurations = new Dictionary<ZnifferChannelConfig, string>
        {
            { ZnifferChannelConfig.ClassicChannelsAndLRChannelA, "all classic channels and LR channel A" },
            { ZnifferChannelConfig.ClassicChannelsAndLRChannelB, "all classic channels and LR channel B" },
            { ZnifferChannelConfig.LRChannelsAAndBOnly, "LR channels A and B only, no classic channels" }
        };

        public void AppendFrequencyHeaders(Dictionary<byte, RFrequency> freqs)
        {
            if (freqs != null && FrameDefinition != null && FrameDefinition.FrequencyHeaders != null)
            {
                foreach (var item in freqs)
                {
                    byte baseHeader = 0;
                    if (item.Value.Channels == 3)
                        baseHeader = 1;
                    if (FrameDefinition.FrequencyHeaders.ContainsKey(item.Key))
                        FrameDefinition.FrequencyHeaders[item.Key] = baseHeader;
                    else
                        FrameDefinition.FrequencyHeaders.Add(item.Key, baseHeader);
                }
            }
        }

        public void FinalizeDevice()
        {
            if (ZnifferAppClient != null)
            {
                ZnifferAppClient.Dispose();
            }
        }

        private Dictionary<byte, RFrequency> StartZniffer(Zniffer zniffer, string port)
        {
            Dictionary<byte, RFrequency> ret = null;
            if (port == null)
                return ret;
            bool versionDetected = false;
            zniffer.Frequency = 0;
            zniffer.Frequencies = null;
            if (zniffer.Connect(new SerialPortDataSource(port, BaudRates.Rate_230400)) == CommunicationStatuses.Done)
            {
                for (int i = 0; i < 2; i++)
                {
                    versionDetected = zniffer.GetVersion().IsStateCompleted;
                    if (versionDetected)
                        break;
                }
            }
            if (!versionDetected)
            {
                if (zniffer.Connect(new SerialPortDataSource(port, BaudRates.Rate_115200)) == CommunicationStatuses.Done)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        versionDetected = zniffer.GetVersion().IsStateCompleted;
                        if (versionDetected)
                            break;
                    }
                }
            }
            if (versionDetected)
            {
                for (int i = 0; i < 2; i++)
                {
                    ret = zniffer.GetFrequencies();
                    if (ret != null && ret.Count > 1)
                        break;
                }

            }
            return ret;
        }
    }
}
