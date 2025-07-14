// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
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
#if NETCOREAPP
using ZWave.Layers.Application;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;
#endif

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

        public bool InitDevice(string port, string setRfRegion)
        {
            if (string.IsNullOrEmpty(setRfRegion))
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

                var zniffer = ZnifferApplicationLayer.CreateZniffer();
                var tmp = zniffer.TransportClient.SuppressDebugOutput;
                zniffer.TransportClient.SuppressDebugOutput = false;
                var supportedRegions = StartZniffer(zniffer, port);
                byte rfRegionCode = 0;
                if (supportedRegions != null)
                {
                    var znifferAppVersion = zniffer.Version.Split('.');
                    var majorVersion = znifferAppVersion.Length > 0 ? int.Parse(znifferAppVersion[0]) : 0;
                    var minorVersion = znifferAppVersion.Length > 1 ? int.Parse(znifferAppVersion[1]) : zniffer.Revision;
                    var isNew = zniffer.ChipType >= ChipTypes.ZW080x; // alt.
                    if (majorVersion < 10) // Old Zniffers (<700) doesn't support Long Range.
                    {
                        // WARNING: a workaround that allows ZATS to work with in classic mode even in case of set LR Region.
                        setRfRegion = setRfRegion.Replace("_LR", ""); // Remove LR from set-value to find correct region
                                                                      // from available list below and apply it for non LR tests.
                    }
                    else if (minorVersion > 21)
                    {
                        //not supported unhandled message!
                        zniffer.GetLRFRegions();
                        zniffer.GetLRConfigs();
                    }

                    // Find corresponding region code from from supported values to set it up.
                    foreach (var region in supportedRegions)
                    {
                        if (region.Value.Name.StartsWith(setRfRegion)) //Start with because result could be like: "EU, 868.42 MHz" for 500s instead of full comparison
                        {
                            rfRegionCode = region.Key;
                            zniffer.RfRegionName = region.Value.Name ?? ""; //setRfRegion
                            break;
                        }
                    }
                    zniffer.Frequency = rfRegionCode;
                    zniffer.SetFrequency(rfRegionCode);
                    AppendFrequencyHeaders(supportedRegions);
                    zniffer.Start();
                    ret = true;
                }
                zniffer.TransportClient.SuppressDebugOutput = tmp;
                Zniffer = zniffer;
            }
            return ret;
        }

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

        /// <summary>
        /// DO WE NEED THIS method as ref = 0
        /// </summary>
        /// <param name="frequency"></param>
        /// <param name="excludedPorts"></param>
        /// <returns></returns>
        public bool DiscoverAndInit(string frequency, string[] excludedPorts)
        {
            bool ret = false;
#if NETCOREAPP
            var portsToCheckList = new List<string>();

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                portsToCheckList.AddRange(System.IO.Ports.SerialPort.GetPortNames());
            }
            else
            {
                portsToCheckList.Add("z1"); // basictrans32 library will enumerate all connected devices by itself.
            }

            portsToCheckList.RemoveAll(var => excludedPorts.Any(port => port == var));
            var znifferPorts = new ConcurrentBag<string>();
            Parallel.ForEach(portsToCheckList, port =>
            {
                var zniffer = ZnifferApplicationLayer.CreateZniffer();
                if (zniffer.Connect(new SerialPortDataSource { SourceName = port, BaudRate = BaudRates.Rate_115200 }) == CommunicationStatuses.Done)
                {
                    var verRes = zniffer.GetVersion();
                    if (verRes)
                    {
                        znifferPorts.Add(port);
                    }
                    zniffer.Dispose();
                }
            });

            if (znifferPorts.Count == 0)
            {
                Parallel.ForEach(portsToCheckList, port =>
                {
                    var zniffer = ZnifferApplicationLayer.CreateZniffer();
                    if (zniffer.Connect(new SerialPortDataSource { SourceName = port, BaudRate = BaudRates.Rate_230400 }) == CommunicationStatuses.Done)
                    {
                        var verRes = zniffer.GetVersion();
                        if (verRes)
                        {
                            znifferPorts.Add(port);
                        }
                        zniffer.Dispose();
                    }
                });
            }
            if (znifferPorts.Count > 0)
            {
                string port;
                znifferPorts.TryTake(out port);
                ret = InitDevice(port, frequency);
            }
            else
            {
                Zniffer = ZnifferApplicationLayer.CreateZniffer();
                //Zniffer.DataSource = new SerialPortDataSource { SourceName = "Not Found!" };
                //Port = Zniffer.DataSource.SourceName;
            }
#endif
            return ret;
        }
    }
}
