// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Net;
using ZWave.Layers;
using ZWave.ZipApplication;

namespace Zats.Services
{
    public class Dtls1Listener
    {
        public IPEndPoint LocalEP { get; set; }
        public Dtls1ClientTransportListener Listener { get; set; }
        public Queue<ReceivedDataArgs> ReceivedData { get; set; }
        public string PskKeyArg { get; set; }
        public int Available { get; set; }
        private readonly object _dtlsListenerLock = new object();

        public Dtls1Listener(IPEndPoint localEP, string pskKeyArg)
        {
            LocalEP = localEP;
            Listener = new Dtls1ClientTransportListener();
            ReceivedData = new Queue<ReceivedDataArgs>();
            PskKeyArg = pskKeyArg;
            Init();
        }

        public void Init()
        {
            Listener.Listen(new Dtls1StartListenParams
            {
                PortNo = (ushort)LocalEP.Port,
                PskKey = PskKeyArg
            });
            Listener.DataReceived += new Action<ReceivedDataArgs>(Listener_DataReceived);
        }

        void Listener_DataReceived(ReceivedDataArgs receivedDataArgs)
        {
            lock (_dtlsListenerLock)
            {
                ReceivedData.Enqueue(receivedDataArgs);
                Available++;
            }
        }

        public byte[] Receive(ref IPEndPoint unsolEndPoint)
        {
            byte[] ret = null;
            lock (_dtlsListenerLock)
            {
                var item = ReceivedData.Dequeue();
                ret = item.Data;
                var tempIp = IPAddress.Parse(item.SourceName);
                unsolEndPoint.Address = tempIp;
                unsolEndPoint.Port = item.SourcePort;
                Available--;
            }
            return ret;
        }

        public void Close()
        {
            Listener.DataReceived -= Listener_DataReceived;
            Listener.Stop();
        }
    }
}
