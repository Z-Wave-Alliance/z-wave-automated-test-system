// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils.Events;
using ZWave;
using ZWave.Enums;
using ZWave.Layers;
using ZWave.Layers.Application;
using ZWave.Layers.Session;
using ZWave.Layers.Transport;
using ZWave.TextApplication;

namespace Zats.Services
{
    public class InstrumentsManager : DeviceManagerBase
    {
        public event EventHandler<EventArgs<SessionAttachment>> OnConnected;
        public event EventHandler<EventArgs<DataChunk>> OnDataTransmitted;
        private ZatsInstruments zatsInstruments;
        private TcpClientTransportLayer _tcpClientTransportLayer;
        private TelnetTransportLayer _telnetTransportLayer;
        private SessionLayer _sessionLayer;
        private TextFrameLayer _textFrameLayer;
        public InstrumentsManager(ZatsInstruments zatsInstruments)
        {
            this.zatsInstruments = zatsInstruments;
            _sessionLayer = new SessionLayer();
            _textFrameLayer = new TextFrameLayer();
            _tcpClientTransportLayer = new TcpClientTransportLayer();
            _tcpClientTransportLayer.DataTransmitted += DataTransmitted;
            _telnetTransportLayer = new TelnetTransportLayer();
            _telnetTransportLayer.DataTransmitted += DataTransmitted;
        }

        private void DataTransmitted(object sender, EventArgs<DataChunk> e)
        {
            OnDataTransmitted?.Invoke(sender, e);
        }

        public bool InitDevice(KeyValuePair<Alias, InstrumentMappingAttribute> item)
        {
            bool ret = false;
            var ins = zatsInstruments.Instruments.FirstOrDefault(x => (string.IsNullOrWhiteSpace(item.Value.Alias) || x.Alias == item.Value.Alias) && x.Type == item.Value.Type);
            if (ins != null)
            {
                TextDevice instrumentClient = null;
                switch (item.Value.Type)
                {
                    case InstrumentTypes.AttenuatorRudat:
                        instrumentClient = new TextApplicationLayer(ApiTypes.AttenuatorRudatInstrument, _sessionLayer, _textFrameLayer, _telnetTransportLayer).CreateTextDevice();
                        instrumentClient.VersionRequest = "M";
                        instrumentClient.RequestEndOfLine = "\r";
                        instrumentClient.ResponseEndOfLine = Environment.NewLine;
                        instrumentClient.DataSource = new SocketDataSource(ins.SName, 9001);
                        break;
                    case InstrumentTypes.PsuE364x:
                        instrumentClient = new TextApplicationLayer(ApiTypes.PsuE364xInstrument, _sessionLayer, _textFrameLayer, _tcpClientTransportLayer).CreateTextDevice();
                        instrumentClient.ResetOutBufferRequests = new[]
                        {
                            "SYSTem:REMote","*RST", "*CLS",
                            "INSTrument:NSELect 1", "CURRent 0.030", "VOLTage 3.30", "OUTPut ON",
                            "INSTrument:NSELect 2", "CURRent 0.030", "VOLTage 3.30", "OUTPut ON",
                        };
                        instrumentClient.RequestEndOfLine = Environment.NewLine;
                        instrumentClient.ResponseEndOfLine = Environment.NewLine;
                        instrumentClient.VersionRequest = "*IDN?";
                        instrumentClient.DataSource = new SocketDataSource(ins.SName, 9001);
                        break;
                    case InstrumentTypes.Dmm7510:
                        instrumentClient = new TextApplicationLayer(ApiTypes.Dmm7510Instrument, _sessionLayer, _textFrameLayer, _tcpClientTransportLayer).CreateTextDevice();
                        instrumentClient.RequestEndOfLine = Environment.NewLine;
                        instrumentClient.ResponseEndOfLine = Environment.NewLine;
                        instrumentClient.VersionRequest = "*IDN?";
                        instrumentClient.DataSource = new SocketDataSource(ins.SName, 5025);
                        break;
                    default:
                        break;
                }
                if (instrumentClient != null)
                {
                    OnConnected?.Invoke(this, new EventArgs<SessionAttachment>(new SessionAttachment
                    {
                        //ApiType = (byte)ApiTypes.OtherInstrument,
                        SessionId = instrumentClient.SessionId,
                        Name = instrumentClient.DataSource.SourceName
                    }));
                    _instruments.AddOrUpdate(item.Key, instrumentClient, (k, v) => instrumentClient);
                    instrumentClient.Connect();
                    for (int i = 0; i < 3; i++)
                    {
                        instrumentClient.Initialize();
                        instrumentClient.GetVersion();
                        ret = instrumentClient.Version != null;
                        if (ret)
                        {
                            break;
                        }
                    }
                    instrumentClient.Disconnect();
                }
            }
            return ret;
        }

        private ConcurrentDictionary<Alias, TextDevice> _instruments = new ConcurrentDictionary<Alias, TextDevice>();
        public TextDevice GetInstrument(Alias deviceAlias)
        {
            if (_instruments.TryGetValue(deviceAlias, out TextDevice ret))
            {
                return ret;
            }
            else
                throw new ServiceException($"can't find instrument {deviceAlias}");
        }

        public void ConnectDevices()
        {
            Parallel.ForEach(_instruments, item =>
            {
                if (item.Value is TextDevice ac)
                {
                    var instrumentClient = item.Value;
                    if (instrumentClient.ApiType == ApiTypes.PsuE364xInstrument)
                    {
                        instrumentClient.Connect();
                        instrumentClient.RequestLine("APPLy?", 1000);
                        instrumentClient.RequestLine("MEASure:CURRent?", 1000);
                        instrumentClient.RequestLine("MEASure:VOLTage?", 1000);
                        instrumentClient.Disconnect();
                    }
                }
            });
        }
    }
}
