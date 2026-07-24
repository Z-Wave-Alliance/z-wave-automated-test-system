// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;
using Utils;
using ZWave.ZipApplication;

namespace ZipTransportIntegrationTests
{
    internal class ListenerReceivedData
    {
        public string Address { get; set; }
        public ushort PortNo { get; set; }
        public byte[] RawData { get; set; }
    }

    [TestFixture]
    public class DtlsClientTests
    {
        private const int Timeout = 10000;
        private string _address;
        private ushort PortNo;
        private string PskKey;
        private ListenerReceivedData _listenerReceivedData;
        private byte[] _dataToSend = new byte[] { 0x0, 0x01, 0x02 };
        private byte[] _dataToReceive = new byte[] { 0x05, 0x04, 0x03, 0x02, 0x01 };
        private byte[] _receivedData;
        private IDtlsClient _dtlsClient;
        private IDtlsListener _dtlsListener;
        private ManualResetEvent _waitForDataEvent;
        private Thread _clientReceiveThread;

        public IPAddress GetFirstLocalIPAddress(AddressFamily addressFamily)
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == addressFamily)
                {
                    return ip;
                }
            }
            return null;
        }

        public IPAddress[] GetLocalIPAddresses(AddressFamily addressFamily)
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList.Where(ip => ip.AddressFamily == addressFamily).ToArray();
        }

        public DtlsClientTests()
        {
            Assert.NotNull(ConfigLoader.SourcesProfile.ZipSource);
            PortNo = (ushort)ConfigLoader.SourcesProfile.ZipSource.DtlsPort;
            PskKey = ConfigLoader.SourcesProfile.ZipSource.Psk;
        }

        private void ListenerStarted_ClientReady(Action action, string ipAddress)
        {
            try
            {
                _dtlsListener = new DtlsListener();
                _dtlsListener.DataReceived += OnListenerDataReceived;
                int errorCode = _dtlsListener.Start(PskKey, ipAddress, PortNo);
                Assert.AreEqual(0, errorCode);
                _dtlsClient = new DtlsClient();

                // Invoke test action.
                action.Invoke();
            }
            finally
            {
                _dtlsClient.Close();
                _dtlsListener.DataReceived -= OnListenerDataReceived;
                _dtlsListener.Stop();
            }
        }

        private void OnListenerDataReceived(byte[] data, string address, ushort portNo)
        {
            _listenerReceivedData = new ListenerReceivedData
            {
                Address = address,
                PortNo = portNo,
                RawData = data
            };
            _waitForDataEvent.Set();
        }

        private void ClientReceiveThread()
        {
            var data = new byte[8];

            var received = _dtlsClient.Receive(data);
            if (received > 0)
            {
                _receivedData = new byte[received];
                Array.Copy(data, _receivedData, received);
            }
        }

        [SetUp]
        public void SetUp()
        {
            _waitForDataEvent = new ManualResetEvent(false);
            _clientReceiveThread = new Thread(ClientReceiveThread);
            _listenerReceivedData = null;
        }

        [TearDown]
        public void TearDown()
        {
            _clientReceiveThread = null;
            _waitForDataEvent.Close();
            _waitForDataEvent = null;
        }

        [Test]
        public void Connect_ServerExists_Ipv6_ReturnsTrue()
        {
            _address = GetFirstLocalIPAddress(AddressFamily.InterNetworkV6).ToString();
            ListenerStarted_ClientReady(() =>
                {
                    var result = _dtlsClient.Connect(PskKey, _address.ToString(), PortNo);
                    Assert.IsTrue(result);
                    Assert.IsTrue(_dtlsClient.IsConnected);
                }, _address);
        }

        [Test]
        public void Connect_ServerExists_Ipv4_ReturnsTrue()
        {
            _address = GetFirstLocalIPAddress(AddressFamily.InterNetwork).ToString();
            ListenerStarted_ClientReady(() =>
            {
                var result = _dtlsClient.Connect(PskKey, _address.ToString(), PortNo);
                Assert.IsTrue(result);
                Assert.IsTrue(_dtlsClient.IsConnected);
            }, _address);
        }

        [Test]
        public void Connect_ServerExists_Ipv6_ClientRaisesConnectedEvent()
        {
            _address = GetFirstLocalIPAddress(AddressFamily.InterNetworkV6).ToString();
            ListenerStarted_ClientReady(() =>
            {
                bool isConnected = false;
                _dtlsClient.Connected += (client) =>
                    {
                        isConnected = true;
                        _waitForDataEvent.Set();
                    };
                var result = _dtlsClient.Connect(PskKey, _address.ToString(), PortNo);
                _waitForDataEvent.WaitOne(Timeout);
                Assert.IsTrue(isConnected);
            }, _address);
        }

        [Test]
        public void Connect_ServerExists_Ipv4_ClientRaisesConnectedEvent()
        {
            _address = GetFirstLocalIPAddress(AddressFamily.InterNetwork).ToString();
            ListenerStarted_ClientReady(() =>
            {
                bool isConnected = false;
                _dtlsClient.Connected += (client) =>
                {
                    isConnected = true;
                    _waitForDataEvent.Set();
                };
                var result = _dtlsClient.Connect(PskKey, _address.ToString(), PortNo);
                _waitForDataEvent.WaitOne(Timeout);
                Assert.IsTrue(isConnected);
            }, _address);
        }

        [Test]
        public void Connect_ServerExists_Ipv6_ListenerRaisesConnectionCreatedEvent()
        {
            _address = GetFirstLocalIPAddress(AddressFamily.InterNetworkV6).ToString();
            ListenerStarted_ClientReady(() =>
            {
                bool connectionCreated = false;
                _dtlsListener.ClientConnected += (address, port) =>
                    {
                        connectionCreated = true;
                        _waitForDataEvent.Set();
                    };
                _dtlsClient.Connect(PskKey, _address.ToString(), PortNo);
                _waitForDataEvent.WaitOne(Timeout);
                Assert.IsTrue(connectionCreated);
            }, _address);
        }

        [Test]
        public void Connect_ServerExists_Ipv4_ListenerRaisesConnectionCreatedEvent()
        {
            _address = GetFirstLocalIPAddress(AddressFamily.InterNetwork).ToString();
            ListenerStarted_ClientReady(() =>
            {
                bool connectionCreated = false;
                _dtlsListener.ClientConnected += (address, port) =>
                {
                    connectionCreated = true;
                    _waitForDataEvent.Set();
                };
                _dtlsClient.Connect(PskKey, _address.ToString(), PortNo);
                _waitForDataEvent.WaitOne(Timeout);
                Assert.IsTrue(connectionCreated);
            }, _address);
        }

        [Test]
        public void Close_ServerExists_Ipv6_ListenerRaisesConnectionClosedEvent()
        {
            _address = GetFirstLocalIPAddress(AddressFamily.InterNetworkV6).ToString();
            ListenerStarted_ClientReady(() =>
            {
                _dtlsListener.DataReceived += OnListenerDataReceived;
                bool connectionClosed = false;
                _dtlsListener.ClientClosed += (address, port) =>
                {
                    connectionClosed = true;
                    _waitForDataEvent.Set();
                };
                _dtlsClient.Connect(PskKey, _address.ToString(), PortNo);
                _dtlsClient.Close();
                _waitForDataEvent.WaitOne(Timeout);
                Assert.IsTrue(connectionClosed);
            }, _address);
        }

        [Test]
        public void Close_ServerExists_Ipv4_ListenerRaisesConnectionClosedEvent()
        {
            _address = GetFirstLocalIPAddress(AddressFamily.InterNetwork).ToString();
            ListenerStarted_ClientReady(() =>
            {
                bool connectionClosed = false;
                _dtlsListener.ClientClosed += (address, port) =>
                {
                    connectionClosed = true;
                    _waitForDataEvent.Set();
                };
                _dtlsClient.Connect(PskKey, _address.ToString(), PortNo);
                _dtlsClient.Close();
                _waitForDataEvent.WaitOne(Timeout);
                Assert.IsTrue(connectionClosed);
            }, _address);
        }

        [Test]
        public void Close_ServerExists_Ipv6_ClientRaisesClosedEvent()
        {
            _address = GetFirstLocalIPAddress(AddressFamily.InterNetworkV6).ToString();
            ListenerStarted_ClientReady(() =>
            {
                bool connectionClosed = false;
                _dtlsClient.Closed += (client) =>
                {
                    connectionClosed = true;
                    _waitForDataEvent.Set();
                };
                _dtlsClient.Connect(PskKey, _address.ToString(), PortNo);
                _dtlsClient.Close();
                _waitForDataEvent.WaitOne(Timeout);
                Assert.IsTrue(connectionClosed);
            }, _address);
        }

        [Test]
        public void Close_ServerExists_Ipv4_ClientRaisesClosedEvent()
        {
            _address = GetFirstLocalIPAddress(AddressFamily.InterNetwork).ToString();
            ListenerStarted_ClientReady(() =>
            {
                bool connectionClosed = false;
                _dtlsClient.Closed += (client) =>
                {
                    connectionClosed = true;
                    _waitForDataEvent.Set();
                };
                _dtlsClient.Connect(PskKey, _address.ToString(), PortNo);
                _dtlsClient.Close();
                _waitForDataEvent.WaitOne(Timeout);
                Assert.IsTrue(connectionClosed);
            }, _address);
        }

        [Test]
        public void Send_ServerExists_Ipv6_ListenerReceivedDataAndSenderAddress()
        {
            _address = GetFirstLocalIPAddress(AddressFamily.InterNetworkV6).ToString();
            ListenerStarted_ClientReady(() =>
            {
                var result = _dtlsClient.Connect(PskKey, _address.ToString(), PortNo);
                if (_dtlsClient.IsConnected)
                {
                    _dtlsClient.Send(_dataToSend);
                    _waitForDataEvent.WaitOne(Timeout);
                    Assert.IsTrue(_dataToSend.SequenceEqual(_listenerReceivedData.RawData));
                    Assert.AreEqual(0, string.Compare(_address.ToString(), _listenerReceivedData.Address));
                }
            }, _address);
        }

        [Test]
        public void Send_ServerExists_Ipv4_ListenerReceivedDataAndSenderAddress()
        {
            _address = GetFirstLocalIPAddress(AddressFamily.InterNetwork).ToString();
            ListenerStarted_ClientReady(() =>
            {
                var result = _dtlsClient.Connect(PskKey, _address.ToString(), PortNo);
                if (_dtlsClient.IsConnected)
                {
                    _dtlsClient.Send(_dataToSend);
                    _waitForDataEvent.WaitOne(Timeout);
                    Assert.IsTrue(_dataToSend.SequenceEqual(_listenerReceivedData.RawData));
                    IPAddress addressFromListener;
                    Assert.IsTrue(IPAddress.TryParse(_listenerReceivedData.Address, out addressFromListener));
                    var mappedAddress = Tools.MapToIPv4(addressFromListener);
                    Assert.AreEqual(0, string.Compare(_address.ToString(), mappedAddress.ToString()));
                }
            }, _address);
        }

        [Test]
        public void Receive_ServerResponse_Ipv6_ReceiveCorrectData()
        {
            _address = GetFirstLocalIPAddress(AddressFamily.InterNetworkV6).ToString();
            ListenerStarted_ClientReady(() =>
            {
                var result = _dtlsClient.Connect(PskKey, _address.ToString(), PortNo);
                if (_dtlsClient.IsConnected)
                {
                    _dtlsClient.Send(_dataToSend);
                    _waitForDataEvent.WaitOne(Timeout);
                    _clientReceiveThread.Start();
                    int sent = _dtlsListener.ResponseTo(_dataToReceive, _listenerReceivedData.Address, _listenerReceivedData.PortNo);
                    Assert.AreEqual(_dataToReceive.Length, sent);
                    _clientReceiveThread.Join();
                    Assert.IsTrue(_receivedData.SequenceEqual(_dataToReceive));
                }
            }, _address);
        }

        [Test]
        public void Receive_ServerResponse_Ipv4_ReceiveCorrectData()
        {
            _address = GetFirstLocalIPAddress(AddressFamily.InterNetwork).ToString();
            ListenerStarted_ClientReady(() =>
            {
                var result = _dtlsClient.Connect(PskKey, _address.ToString(), PortNo);
                if (_dtlsClient.IsConnected)
                {
                    _dtlsClient.Send(_dataToSend);
                    _waitForDataEvent.WaitOne(Timeout);
                    _clientReceiveThread.Start();
                    int sent = _dtlsListener.ResponseTo(_dataToReceive, _listenerReceivedData.Address, _listenerReceivedData.PortNo);
                    Assert.AreEqual(_dataToReceive.Length, sent);
                    _clientReceiveThread.Join();
                    Assert.IsTrue(_receivedData.SequenceEqual(_dataToReceive));
                }
            }, _address);
        }

        [Test]
        public void Receive_ServerCloseConnection_Ipv6_ClientRaisesClosedEvent()
        {
            _address = GetFirstLocalIPAddress(AddressFamily.InterNetworkV6).ToString();
            ListenerStarted_ClientReady(() =>
            {
                bool connectionClosed = false;
                _dtlsClient.Closed += (client) =>
                {
                    connectionClosed = true;
                    _waitForDataEvent.Set();
                };
                _dtlsClient.Connect(PskKey, _address.ToString(), PortNo);
                _clientReceiveThread.Start();
                _dtlsListener.Stop();
                _clientReceiveThread.Join();
                _waitForDataEvent.WaitOne(Timeout);
                Assert.IsTrue(connectionClosed);
            }, _address);
        }

        [Test]
        public void Receive_ServerCloseConnection_Ipv4_ClientRaisesClosedEvent()
        {
            _address = GetFirstLocalIPAddress(AddressFamily.InterNetwork).ToString();
            ListenerStarted_ClientReady(() =>
            {
                bool connectionClosed = false;
                _dtlsClient.Closed += (client) =>
                {
                    connectionClosed = true;
                    _waitForDataEvent.Set();
                };
                _dtlsClient.Connect(PskKey, _address.ToString(), PortNo);
                _clientReceiveThread.Start();
                _dtlsListener.Stop();
                _clientReceiveThread.Join();
                _waitForDataEvent.WaitOne(Timeout);
                Assert.IsTrue(connectionClosed);
            }, _address);
        }

        private byte[] _dataOnFirstListener;
        private byte[] _dataOnSecondListener;
        private ManualResetEvent[] _dataReceivedEvents;
        [TestCase(AddressFamily.InterNetwork)]
        [TestCase(AddressFamily.InterNetworkV6)]
        public void SendFromTwoClients_TwoListenersStarted_ListenerReceivedDataAndSendersAddress(AddressFamily af)
        {
            _dataReceivedEvents = new ManualResetEvent[] { new ManualResetEvent(false), new ManualResetEvent(false) };
            var dataFirst = new byte[] { 0x00, 0x01, 0x03, 0x05, 0x07 };
            var dataSecond = new byte[] { 0x00, 0x02, 0x04, 0x06, 0x08 };
            var address = GetFirstLocalIPAddress(af).ToString();
            ushort portFirstNo = 41231;
            ushort portSecondNo = 41232;
            DtlsListener listenerFirst = null;
            DtlsClient clientFirst = null;
            DtlsListener listenerSecond = null;
            DtlsClient clientSecond = null;
            try
            {
                listenerFirst = new DtlsListener();
                listenerFirst.DataReceived += ListenerFirst_DataReceived;
                clientFirst = new DtlsClient();
                listenerSecond = new DtlsListener();
                listenerSecond.DataReceived += ListenerSecond_DataReceived;
                clientSecond = new DtlsClient();

                Assert.AreEqual(0, listenerFirst.Start(PskKey, address, portFirstNo));
                Assert.AreEqual(0, listenerSecond.Start(PskKey, address, portSecondNo));

                Assert.IsTrue(clientFirst.Connect(PskKey, address, portFirstNo));
                Assert.IsTrue(clientSecond.Connect(PskKey, address, portSecondNo));

                Assert.AreEqual(dataFirst.Length, clientFirst.Send(dataFirst));
                Assert.AreEqual(dataSecond.Length, clientSecond.Send(dataSecond));

                WaitHandle.WaitAll(_dataReceivedEvents);

                Assert.IsNotNull(_dataOnFirstListener);
                Assert.IsTrue(_dataOnFirstListener.SequenceEqual(dataFirst));
                Assert.IsNotNull(_dataOnSecondListener);
                Assert.IsTrue(_dataOnSecondListener.SequenceEqual(dataSecond));
            }
            finally
            {
                clientFirst.Close();
                listenerFirst.DataReceived -= ListenerFirst_DataReceived;
                listenerFirst.Stop();
                clientSecond.Close();
                listenerSecond.DataReceived -= ListenerSecond_DataReceived;
                listenerSecond.Stop();
                foreach (var drEvent in _dataReceivedEvents)
                {
                    drEvent.Close();
                }
                _dataReceivedEvents = null;
            }
        }

        void ListenerFirst_DataReceived(byte[] arg1, string arg2, ushort arg3)
        {
            _dataOnFirstListener = new byte[arg1.Length];
            Array.Copy(arg1, _dataOnFirstListener, arg1.Length);
            _dataReceivedEvents[0].Set();
        }

        void ListenerSecond_DataReceived(byte[] arg1, string arg2, ushort arg3)
        {
            _dataOnSecondListener = new byte[arg1.Length];
            Array.Copy(arg1, _dataOnSecondListener, arg1.Length);
            _dataReceivedEvents[1].Set();
        }

        //private ManualResetEvent[] _dataReceivedEvents;
        internal class RunClientData
        {
            public string localAddress;
            public ushort localPortNo;
            public string destAddress;
            public ushort destPortNo;
            public byte[] data;
        }

        [TestCase(AddressFamily.InterNetwork)]
        [TestCase(AddressFamily.InterNetworkV6)]
        [Ignore("Disabled test.")]
        public void TwoClientsSendFromOneAddress_OneListenersStarted_BothListenersReceivedData(AddressFamily af)
        {
            _dataReceivedEvents = new ManualResetEvent[] { new ManualResetEvent(false), new ManualResetEvent(false) };
            var dataFirst = new byte[] { 0x00, 0x01, 0x03, 0x05, 0x07 };
            var dataSecond = new byte[] { 0x00, 0x02, 0x04, 0x06, 0x08 };
            var address = GetFirstLocalIPAddress(af).ToString();
            DtlsListener listenerFirst = null;
            //DtlsClient clientFirst = null;
            DtlsListener listenerSecond = null;
            //DtlsClient clientSecond = null;
            try
            {
                listenerFirst = new DtlsListener();
                listenerFirst.DataReceived += ListenerFirst_DataReceived;
                //clientFirst = new DtlsClient();
                listenerSecond = new DtlsListener();
                listenerSecond.DataReceived += ListenerSecond_DataReceived;
                //clientSecond = new DtlsClient();

                var firstClientThread = new Thread(RunClient);
                var secondClientThread = new Thread(RunClient);
                firstClientThread.Start(new RunClientData { localAddress = "172.27.22.10", localPortNo = 41230, destAddress = "172.27.22.5", destPortNo = 41230, data = dataFirst });
                secondClientThread.Start(new RunClientData { localAddress = "172.27.22.10", localPortNo = 41230, destAddress = "172.27.22.5", destPortNo = 41231, data = dataSecond });
                firstClientThread.Join();
                secondClientThread.Join();

                //Assert.AreEqual(0, listenerFirst.Start(PskKey, portFirstNo));
                //Assert.AreEqual(0, listenerSecond.Start(PskKey, portSecondNo));

                //Assert.IsTrue(clientFirst.Connect(PskKey, address, portFirstNo));
                //Assert.IsTrue(clientSecond.Connect(PskKey, address, portSecondNo));

                //Assert.AreEqual(dataFirst.Length, clientFirst.Send(dataFirst));
                //Assert.AreEqual(dataSecond.Length, clientSecond.Send(dataSecond));

                //WaitHandle.WaitAll(_dataReceivedEvents);

                //Assert.IsNotNull(_dataOnFirstListener);
                //Assert.IsTrue(_dataOnFirstListener.SequenceEqual(dataFirst));
                //Assert.IsNotNull(_dataOnSecondListener);
                //Assert.IsTrue(_dataOnSecondListener.SequenceEqual(dataSecond));
            }
            finally
            {
                //clientFirst.Close();
                listenerFirst.DataReceived -= ListenerFirst_DataReceived;
                listenerFirst.Stop();
                //clientSecond.Close();
                listenerSecond.DataReceived -= ListenerSecond_DataReceived;
                listenerSecond.Stop();
                foreach (var drEvent in _dataReceivedEvents)
                {
                    drEvent.Close();
                }
                _dataReceivedEvents = null;
            }
        }

        private void RunClient(object data)
        {
            var rcd = (RunClientData)data;
            IDtlsClient client = null;
            try
            {
                client = new DtlsClient();
                if (client.Connect(PskKey, rcd.localAddress, rcd.localPortNo, rcd.destAddress, rcd.destPortNo))
                {
                    client.Send(rcd.data);
                }
            }
            finally
            {
                client.Close();
            }
        }
    }
}