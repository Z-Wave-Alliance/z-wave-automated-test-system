// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Net.Http;
using System.Text;
using Utils;
using Utils.Events;
using ZWave.Enums;
using ZWave.Layers;

namespace Zats.Services.MeshNetworkRacks
{
    public class MeshRouterTransportClient : ITransportClient
    {
        private HttpClient _httpClient = new HttpClient();
        private Action<EventArgs<DataChunk>> _transmittCallback;

        public ushort SessionId { get; set; }
        public ApiTypes ApiType { get; set; }

        private SocketDataSource _dataSource;
        public IDataSource DataSource
        {
            get { return _dataSource; }
            set { _dataSource = value as SocketDataSource; }
        }

        public bool IsOpen => true;

        public Action<DataChunk, bool> ReceiveDataCallback { get; set; }
        public bool SuppressDebugOutput { get; set; }

        public event Action<ITransportClient> Connected;
        public event Action<ITransportClient> Disconnected;

        public MeshRouterTransportClient(Action<EventArgs<DataChunk>> transmittCallback)
        {
            _transmittCallback = transmittCallback;
        }

        public CommunicationStatuses Connect(IDataSource dataSource)
        {
            DataSource = dataSource;
            return Connect();
        }

        public CommunicationStatuses Connect()
        {
            if (DataSource != null)
            {
                Connected?.Invoke(this);
                return CommunicationStatuses.Done;
            }
            return CommunicationStatuses.Failed;
        }

        public void Disconnect()
        {
            DataSource = null;
            Disconnected?.Invoke(this);
        }

        public void Dispose()
        {
        }

        public int WriteData(byte[] data)
        {
            var queryString = $"http://{_dataSource.SourceName}:{_dataSource.Port}/{Encoding.ASCII.GetString(data)}";
            _transmittCallback?.Invoke(new EventArgs<DataChunk>(new DataChunk(data, SessionId, true, ApiType)));
            var res = _httpClient.GetAsync(queryString).Result;
            if (res.IsSuccessStatusCode)
            {
                // Response.
                var responseBytes = res.Content.ReadAsByteArrayAsync().Result;
                var receivedDataChunk = new DataChunk(responseBytes, SessionId, false, ApiType);
                ReceiveDataCallback?.Invoke(receivedDataChunk, false);
                _transmittCallback?.Invoke(new EventArgs<DataChunk>(receivedDataChunk));
                return data.Length;
            }
            return -1;
        }
    }
}
