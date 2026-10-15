// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using Utils;
using Utils.Events;
using ZWave.Enums;
using ZWave.Layers;
using PrimS.Telnet;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace Zats.Services
{
    public class TelnetTransportLayer : ITransportLayer
    {
        public bool SuppressDebugOutput { get; set; }

        public event EventHandler<EventArgs<DataChunk>> DataTransmitted;

        public ITransportClient CreateClient(ushort sessionId)
        {
            var ret = new TelnetTransportClient(dataChunk => DataTransmitted?.Invoke(this, new EventArgs<DataChunk>(dataChunk)))
            {
                SuppressDebugOutput = SuppressDebugOutput,
                SessionId = sessionId
            };
            return ret;
        }
    }

    public class TelnetTransportClient : TransportClientBase
    {
        private Client _telnetClient;
        public override bool IsOpen => _telnetClient != null ? _telnetClient.IsConnected : false;

        public override event Action<ITransportClient> Connected;
        public override event Action<ITransportClient> Disconnected;
        private readonly Action<DataChunk> _transmitCallback;
        public TelnetTransportClient(Action<DataChunk> transmitCallback)
        {
            _transmitCallback = transmitCallback;
        }

        volatile bool _isClosing = false;
        private Task _readingTask = null;
        protected override CommunicationStatuses InnerConnect(IDataSource dataSource)
        {
            CommunicationStatuses ret = CommunicationStatuses.Failed;
            if (dataSource is SocketDataSource sds)
            {
                _telnetClient = new Client(sds.SourceName, sds.Port, new CancellationToken());
                if (_telnetClient.IsConnected)
                {
                    ret = CommunicationStatuses.Done;
                    _isClosing = false;
                    Connected?.Invoke(this);
                    _readingTask = new Task(() =>
                    {
                        _readingSignal.Set();
                        while (!_isClosing)
                        {
                            string s = null;
                            lock (_locker)
                            {
                                if (!_isClosing && _telnetClient != null && _telnetClient.IsConnected)
                                {
                                    s = _telnetClient.ReadAsync().Result;
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                var data = _encoding.GetBytes(s);
                                if (!SuppressDebugOutput)
                                {
                                    $"{SessionId:X2} {ApiType} {sds.SourceName} << {s}"._DLOG();
                                }
                                DataChunk dc = new DataChunk(data, SessionId, false, ApiType);
                                _transmitCallback?.Invoke(dc);
                                ReceiveDataCallback?.Invoke(dc, false);
                            }
                        }
                    });
                    _readingTask.Start();
                    _readingSignal.WaitOne();
                    $"{SessionId:X2} {ApiType} {sds.SourceName}@{3}:{sds.Port} {ret}"._DLOG();
                }
                else
                {
                    _telnetClient.Dispose();
                    _telnetClient = null;
                }
            }
            return ret;
        }

        protected override void InnerDisconnect()
        {
            _isClosing = true;
            if (_readingTask != null)
            {
                _readingTask.Wait();
            }
            Disconnected?.Invoke(this);
            if (DataSource is SocketDataSource sds)
            {
                $"{SessionId:X2} {ApiType} {sds.SourceName}@{3}:{sds.Port}"._DLOG();
            }
            _telnetClient.Dispose();
            _telnetClient = null;
            _readingSignal.Reset();
        }

        protected override void InnerDispose()
        {
            InnerDisconnect();
        }

        private Encoding _encoding = Encoding.ASCII;
        ManualResetEvent _readingSignal = new ManualResetEvent(false);
        private object _locker = new object();
        protected override int InnerWriteData(byte[] data)
        {
            if (!SuppressDebugOutput)
            {
                $"{SessionId:X2} {ApiType} {DataSource?.SourceName} >> {_encoding.GetString(data)}"._DLOG();
            }
            var dc = new DataChunk(data, SessionId, true, ApiType);
            _transmitCallback?.Invoke(dc);
            lock (_locker)
            {
                _telnetClient.WriteAsync(_encoding.GetString(data)).Wait();
            }
            return data.Length;
        }
    }
}
