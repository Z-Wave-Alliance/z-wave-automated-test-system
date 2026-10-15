// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
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
        private readonly Action<EventArgs<DataChunk>> _transmittCallback;

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

        /// <summary>
        /// Write data to the mesh network via HTTP GET request. 
        /// </summary>
        /// <param name="data">Data to write.</param>
        /// <returns>Length of written data in the success case, otherwise -1.</returns>
        public int WriteData(byte[] data)
        {
            var queryString = $"http://{_dataSource.SourceName}:{_dataSource.Port}/{Encoding.ASCII.GetString(data)}";

            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(2);
                _transmittCallback?.Invoke(new EventArgs<DataChunk>(new DataChunk(data, SessionId, true, ApiType)));
                var resp = httpClient.GetAsync(queryString).GetAwaiter().GetResult();
                var res = resp.EnsureSuccessStatusCode();
                if (res.IsSuccessStatusCode)
                {
                    // Response.
                    var response = res.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                    var responseBytes = resp.EnsureSuccessStatusCode().Content.ReadAsByteArrayAsync().Result;
                    var receivedDataChunk = new DataChunk(responseBytes, SessionId, false, ApiType);
                    ReceiveDataCallback?.Invoke(receivedDataChunk, false);
                    _transmittCallback?.Invoke(new EventArgs<DataChunk>(receivedDataChunk));
                    return data.Length;
                }
            }
            return -1;
        }
    }
}
