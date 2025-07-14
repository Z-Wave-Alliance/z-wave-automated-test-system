// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using Utils.Events;
using ZWave.Layers;

namespace Zats.Services.MeshNetworkRacks
{
    public class MeshRouterTransportLayer : ITransportLayer
    {
        public bool SuppressDebugOutput { get; set; }

        public event EventHandler<EventArgs<DataChunk>> DataTransmitted;

        public ITransportClient CreateClient(ushort sessionId)
        {
            return new MeshRouterTransportClient(evArgs => DataTransmitted?.Invoke(this, evArgs))
            {
                SessionId = sessionId
            };
        }
    }
}
