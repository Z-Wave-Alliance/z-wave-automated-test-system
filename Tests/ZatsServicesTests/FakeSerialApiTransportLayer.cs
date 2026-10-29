// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Utils.Events;
using ZWave.BasicApplication.EmulatedLink;
using ZWave.Layers;

namespace ZatsServicesTests
{
    /// <summary>
    /// A Serial API chip for unit tests. It acknowledges every frame, records the requests of the host, answers the reads of the
    /// NodeId and of the keys from its own state, and lets a test send frames the chip sends on its own.
    /// </summary>
    internal class FakeSerialApiTransportLayer : ITransportLayer
    {
        public const byte CMD_APPL_NODE_INFORMATION = 0x03;
        public const byte CMD_SOFT_RESET = 0x08;
        public const byte CMD_STARTED = 0x0A;
        public const byte CMD_SETUP = 0x0B;
        public const byte CMD_APPL_NODE_INFORMATION_CMD_CLASSES = 0x0C;
        public const byte CMD_MEMORY_GET_ID = 0x20;
        public const byte CMD_SET_LEARN_MODE = 0x50;
        public const byte CMD_SECURITY_SETUP = 0x9C;

        private const byte SOF = 0x01;
        private const byte ACK = 0x06;
        private const byte REQUEST = 0x00;
        private const byte RESPONSE = 0x01;

        private readonly ConcurrentDictionary<ushort, BlockingCollection<byte[]>> _toHost =
            new ConcurrentDictionary<ushort, BlockingCollection<byte[]>>();

        private readonly List<byte[]> _requests = new List<byte[]>();

        public event EventHandler<EventArgs<DataChunk>> DataTransmitted;

        public bool SuppressDebugOutput { get; set; }

        /// <summary>
        /// The NodeId the chip reports.
        /// </summary>
        public byte NodeId { get; set; }

        /// <summary>
        /// The keys the chip reports, as the bit mask of Security Setup Get Security Keys.
        /// </summary>
        public byte SecurityKeysMask { get; set; }

        public ITransportClient CreateClient(ushort sessionId)
        {
            _toHost.TryAdd(sessionId, new BlockingCollection<byte[]>());
            return new BasicLinkTransportClient(
                dataChunk => DataTransmitted?.Invoke(this, new EventArgs<DataChunk>(dataChunk)), WriteData, ReadData, RemoveClient)
            {
                SuppressDebugOutput = true,
                SessionId = sessionId
            };
        }

        /// <summary>
        /// Returns the requests of the host with the given command, each from its command byte on.
        /// </summary>
        public byte[][] GetRequests(byte command)
        {
            lock (_requests)
            {
                return _requests.Where(x => x[0] == command).ToArray();
            }
        }

        /// <summary>
        /// Forgets the requests recorded so far.
        /// </summary>
        public void ClearRequests()
        {
            lock (_requests)
            {
                _requests.Clear();
            }
        }

        /// <summary>
        /// Sends a request frame from the chip to the host, given from its command byte on.
        /// </summary>
        public void SendRequest(ushort sessionId, params byte[] commandAndParameters)
        {
            _toHost[sessionId].Add(CreateFrame(new[] { REQUEST }.Concat(commandAndParameters).ToArray()));
        }

        private void RemoveClient(ushort sessionId)
        {
            if (_toHost.TryRemove(sessionId, out BlockingCollection<byte[]> toHost))
            {
                toHost.Dispose();
            }
        }

        private int ReadData(ushort sessionId, byte[] buffer, CancellationToken cancellationToken)
        {
            if (!_toHost.TryGetValue(sessionId, out BlockingCollection<byte[]> toHost))
            {
                return 0;
            }

            byte[] data = toHost.Take(cancellationToken);
            Array.Copy(data, buffer, data.Length);

            return data.Length;
        }

        private int WriteData(ushort sessionId, byte[] data)
        {
            if (data == null || data.Length < 4 || data[0] != SOF || !_toHost.TryGetValue(sessionId, out BlockingCollection<byte[]> toHost))
            {
                return data?.Length ?? 0;
            }

            toHost.Add(new[] { ACK });
            if (data[2] != REQUEST)
            {
                return data.Length;
            }

            // SOF | length | type | command | parameters | checksum
            byte[] request = data.Skip(3).Take(data.Length - 4).ToArray();
            lock (_requests)
            {
                _requests.Add(request);
            }

            switch (request[0])
            {
                case CMD_MEMORY_GET_ID:
                    toHost.Add(CreateFrame(new byte[] { RESPONSE, CMD_MEMORY_GET_ID, 0xC0, 0x01, 0x02, 0x03, NodeId }));
                    break;
                case CMD_SECURITY_SETUP:
                    toHost.Add(CreateFrame(new byte[] { RESPONSE, CMD_SECURITY_SETUP, 0x00, 0x01, SecurityKeysMask }));
                    break;
                case CMD_SETUP:
                    toHost.Add(CreateFrame(new byte[] { RESPONSE, CMD_SETUP, request[1], 0x01 }));
                    break;
                case CMD_SOFT_RESET:
                    toHost.Add(CreateFrame(new byte[] { REQUEST, CMD_STARTED, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }));
                    break;
            }

            return data.Length;
        }

        private static byte[] CreateFrame(byte[] typeCommandAndParameters)
        {
            byte length = (byte)(typeCommandAndParameters.Length + 1);
            byte checksum = (byte)(0xFF ^ length);
            foreach (byte b in typeCommandAndParameters)
            {
                checksum ^= b;
            }

            return new[] { SOF, length }.Concat(typeCommandAndParameters).Concat(new[] { checksum }).ToArray();
        }
    }
}
