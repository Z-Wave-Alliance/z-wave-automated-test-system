// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Linq;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;
using ZWave.Enums;
using ZWave.Layers;
using ZWave.ZipApplication;
using Assert = NUnit.Framework.Assert;
using Moq;
using Utils;

namespace ZatsServicesUnitTests
{
    [TestFixture]
    public class Dtls1ClientTransportClientTests
    {
        private SocketDataSource _dataSource;
        private ITransportClient _dtls1TransportClient;
        private byte[] _data = new byte[] { 0x0, 0x1, 0x2 };
        private const int _sessionId = 0x79;
        private readonly ApiTypes _apiType = ApiTypes.Zip;
        private DataChunk _transmitedDataChunk;
        private DataChunk _receivedDataChunk;

        [SetUp]
        public void SetUp()
        {
            _dataSource = new SocketDataSource("1.1.1.1", 41230, "-psk 1234567890");
            _dtls1TransportClient = new Dtls1ClientTransportClient(OnDataTransmited)
            {
                DataSource = _dataSource
            };
            _transmitedDataChunk = null;
            _receivedDataChunk = null;
        }

        [TearDown]
        public void TearDown()
        {
            _dtls1TransportClient.Dispose();
        }

        [Test]
        public void Connect_DtlsClientCannotEstablishConnection_CommunicationStatusIsBusy()
        {
            // Arrange.
            var dtlsClientMock = new Mock<IDtlsClient>();
            dtlsClientMock.SetupGet(x => x.IsConnected).Returns(false);
            ((Dtls1ClientTransportClient)_dtls1TransportClient).DtlsClient = dtlsClientMock.Object;

            // Act.
            var status = _dtls1TransportClient.Connect(_dataSource);

            // Assert.
            Assert.IsTrue(status == CommunicationStatuses.Busy);
        }

        [Test]
        public void Connect_DtlsClientConnectedSuccessfuly_DtlsClientConnectCalled()
        {
            // Arrange.
            var dtlsClientMock = new Mock<IDtlsClient>();
            dtlsClientMock.Setup(x => x.Receive(It.IsAny<byte[]>())).Returns(0);
            dtlsClientMock.Setup(x => x.Connect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ushort>()))
                .Callback(() => dtlsClientMock.SetupGet(x => x.IsConnected).Returns(true));
            ((Dtls1ClientTransportClient)_dtls1TransportClient).DtlsClient = dtlsClientMock.Object;

            // Act.
            var status = _dtls1TransportClient.Connect();

            // Assert.
            dtlsClientMock.Verify(x => x.Connect(
                It.IsAny<string>(),
                It.Is<string>(b => b == _dataSource.SourceName),
                It.Is<ushort>(c => c == _dataSource.Port)));
            Assert.AreEqual(CommunicationStatuses.Done, status);
        }

        [Test]
        public void Connect_Reconnection_DtlsClientClosedBeforeNewConnection()
        {
            // Arrange.
            var dtlsClientMock = new Mock<IDtlsClient>();
            dtlsClientMock.Setup(x => x.Receive(It.IsAny<byte[]>())).Returns(0);
            dtlsClientMock.SetupGet(x => x.IsConnected).Returns(true);
            ((Dtls1ClientTransportClient)_dtls1TransportClient).DtlsClient = dtlsClientMock.Object;

            // Act.
            var status = _dtls1TransportClient.Connect(); // Connected successfuly.
            Assert.AreEqual(CommunicationStatuses.Done, status);
            status = _dtls1TransportClient.Connect(); // Reconnect.

            // Assert.
            dtlsClientMock.Verify(x => x.Close(), Times.Once);
            Assert.AreEqual(CommunicationStatuses.Done, status);
        }

        [Test]
        public void Connect_DtlsClientThrowsIOException_CommunicationStatusIsBusy()
        {
            // Arrange.
            var dtlsClientMock = new Mock<IDtlsClient>();
            dtlsClientMock.Setup(x => x.Connect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ushort>()))
                .Throws(new IOException());

            ((Dtls1ClientTransportClient)_dtls1TransportClient).DtlsClient = dtlsClientMock.Object;

            // Act.
            var status = _dtls1TransportClient.Connect(_dataSource);

            // Assert.
            Assert.IsTrue(status == CommunicationStatuses.Busy);
        }

        [Test]
        public void Connect_DtlsClientConnectedSuccessfuly_SubscribedForDtlsClientEvents()
        {
            // Arrange.
            var dtlsClientMock = new Mock<IDtlsClient>();
            dtlsClientMock.Setup(x => x.Receive(It.IsAny<byte[]>())).Returns(0);
            dtlsClientMock.Setup(x => x.Connect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ushort>()))
                .Callback(() =>
                {
                    dtlsClientMock.SetupGet(x => x.IsConnected).Returns(true);
                    dtlsClientMock.Raise(x => x.Connected += null, dtlsClientMock.Object);
                });

            bool isConnected = false;
            bool isDisconnected = false;
            ((Dtls1ClientTransportClient)_dtls1TransportClient).DtlsClient = dtlsClientMock.Object;
            _dtls1TransportClient.Connected +=
                new Action<ITransportClient>(
                    client =>
                    {
                        isConnected = true;
                    });

            _dtls1TransportClient.Disconnected +=
                new Action<ITransportClient>(
                    client =>
                    {
                        isDisconnected = true;
                    });
            // Act.
            var status = _dtls1TransportClient.Connect();
            _dtls1TransportClient.Disconnect();

            // Assert.
            Assert.IsTrue(isConnected);
            Assert.IsTrue(isDisconnected);
        }

        [Test]
        public void Connect_DtlsClientConnectionFailed_UnsubscribedFromDtlsClientEvents()
        {
            // Arrange.
            const int CONNECTION_ATTEMPTS = 5;
            var dtlsClientMock = new Mock<IDtlsClient>();
            dtlsClientMock.SetupGet(x => x.IsConnected).Returns(false);
            dtlsClientMock.Setup(x => x.Connect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ushort>()))
                .Returns(false);

            var dtlsTransportClient = (Dtls1ClientTransportClient)_dtls1TransportClient;
            dtlsTransportClient.ConnectionAttempts = CONNECTION_ATTEMPTS;
            dtlsTransportClient.DtlsClient = dtlsClientMock.Object;
            bool isConnected = false;
            bool isDisconnected = false;
            _dtls1TransportClient.Connected +=
                new Action<ITransportClient>(
                    client =>
                    {
                        isConnected = true;
                    });

            _dtls1TransportClient.Disconnected +=
                new Action<ITransportClient>(
                    client =>
                    {
                        isDisconnected = true;
                    });

            // Act.
            var status = _dtls1TransportClient.Connect();
            _dtls1TransportClient.Disconnect();

            // Assert.
            dtlsClientMock.Verify(x => x.Connect(It.IsAny<string>(), It.Is<string>(b => b == _dataSource.SourceName), It.Is<ushort>(c => c == _dataSource.Port)), Times.Exactly(CONNECTION_ATTEMPTS));
            Assert.IsFalse(isConnected);
            Assert.IsFalse(isDisconnected);
        }

        [Test]
        public void Disconnect_DtlsClientConnectedSuccessfuly_UnsubscribedFromDtlsClientEvents()
        {
            // Arrange.
            var dtlsClientMock = new Mock<IDtlsClient>();
            dtlsClientMock.Setup(x => x.Receive(It.IsAny<byte[]>())).Returns(0);
            dtlsClientMock.Setup(x => x.Connect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ushort>()))
              .Callback(() =>
              {
                  dtlsClientMock.SetupGet(x => x.IsConnected).Returns(true);
                  dtlsClientMock.Raise(x => x.Connected += null, dtlsClientMock.Object);
              });

            ((Dtls1ClientTransportClient)_dtls1TransportClient).DtlsClient = dtlsClientMock.Object;
            bool isConnected = false;
            bool isDisconnected = false;
            _dtls1TransportClient.Connected +=
                new Action<ITransportClient>(
                    client =>
                    {
                        isConnected = true;
                    });
            _dtls1TransportClient.Disconnected +=
                new Action<ITransportClient>(
                    client =>
                    {
                        isDisconnected = true;
                    });

            // Act.
            var status = _dtls1TransportClient.Connect(); // Connected successfuly.
            Assert.AreEqual(CommunicationStatuses.Done, status);
            Assert.IsTrue(isConnected);

            _dtls1TransportClient.Disconnect();
            Assert.IsTrue(isDisconnected);
        }

        [Test]
        public void WriteData_DtlsClientThrowsSocketException_HandleItAndReturnsMinusOne()
        {
            // Arrange.
            var dtlsClientMock = new Mock<IDtlsClient>();
            dtlsClientMock.Setup(x => x.Receive(It.IsAny<byte[]>())).Returns(0);
            dtlsClientMock.SetupGet(x => x.IsConnected).Returns(true);

            dtlsClientMock.Setup(x => x.Send(It.IsAny<byte[]>())).Throws(new SocketException());
            ((Dtls1ClientTransportClient)_dtls1TransportClient).DtlsClient = dtlsClientMock.Object;

            // Act.
            var status = _dtls1TransportClient.Connect(); // Connected successfuly.
            Assert.AreEqual(CommunicationStatuses.Done, status);
            var written = _dtls1TransportClient.WriteData(_data);

            // Assert.
            Assert.AreEqual(written, -1);
        }

        [Test]
        public void WriteData_DtlsClientThrowsObjectDisposedException_HandleItAndReturnsMinusOne()
        {
            // Arrange.
            var dtlsClientMock = new Mock<IDtlsClient>();
            dtlsClientMock.Setup(x => x.Receive(It.IsAny<byte[]>())).Returns(0);
            dtlsClientMock.SetupGet(x => x.IsConnected).Returns(true);

            dtlsClientMock.Setup(x => x.Send(It.IsAny<byte[]>())).Throws(new ObjectDisposedException("dtlsClientMock"));
            ((Dtls1ClientTransportClient)_dtls1TransportClient).DtlsClient = dtlsClientMock.Object;

            // Act.
            _dtls1TransportClient.Connect();
            var written = _dtls1TransportClient.WriteData(_data);

            // Assert.
            Assert.AreEqual(written, -1);
        }

        [Test]
        public void WriteData_DtlsClientConnectedSuccessfuly_DataTransmitedToCallback()
        {
            // Arrange.
            var dtlsClientMock = new Mock<IDtlsClient>();
            dtlsClientMock.Setup(x => x.Receive(It.IsAny<byte[]>())).Returns(0);
            dtlsClientMock.SetupGet(x => x.IsConnected).Returns(true);

            dtlsClientMock.Setup(x => x.Send(It.IsAny<byte[]>())).Returns<byte[]>(x => x.Length);

            ((Dtls1ClientTransportClient)_dtls1TransportClient).DtlsClient = dtlsClientMock.Object;
            _dtls1TransportClient.SessionId = _sessionId;
            _dtls1TransportClient.ApiType = _apiType;

            // Act.
            var status = _dtls1TransportClient.Connect(); // Connected successfuly.
            Assert.AreEqual(CommunicationStatuses.Done, status);
            int written = _dtls1TransportClient.WriteData(_data);

            // Assert.
            Assert.AreEqual(written, _data.Length);
            Assert.AreEqual(written, _transmitedDataChunk.DataBufferLength);
            Assert.AreEqual(_sessionId, _transmitedDataChunk.SessionId);
            Assert.AreEqual(_apiType, _transmitedDataChunk.ApiType);
            Assert.IsTrue(_data.SequenceEqual(_transmitedDataChunk.GetDataBuffer()));
        }

        [Test]
        public void WriteData_DtlsClientConnectedSuccessfuly_DtlsClientSendsData()
        {
            // Arrange.
            var dtlsClientMock = new Mock<IDtlsClient>();
            dtlsClientMock.Setup(x => x.Receive(It.IsAny<byte[]>())).Returns(0);
            dtlsClientMock.SetupGet(x => x.IsConnected).Returns(true);
            ((Dtls1ClientTransportClient)_dtls1TransportClient).DtlsClient = dtlsClientMock.Object;

            // Act.
            var status = _dtls1TransportClient.Connect(); // Connected successfuly.
            Assert.AreEqual(CommunicationStatuses.Done, status);
            int written = _dtls1TransportClient.WriteData(_data);

            // Assert.
            dtlsClientMock.Verify(x => x.Send(It.Is<byte[]>(a => a == _data)));
        }

        [Test]
        public void ReceiveData_DtlsClientConnectedSuccessfuly_DtlsClientReceivesData()
        {
            // Arrange.
            var dtlsClientMock = new Mock<IDtlsClient>();
            dtlsClientMock.Setup(x => x.Receive(It.IsAny<byte[]>())).Returns(0);
            dtlsClientMock.SetupGet(x => x.IsConnected).Returns(true);
            ((Dtls1ClientTransportClient)_dtls1TransportClient).DtlsClient = dtlsClientMock.Object;

            // Act.
            var status = _dtls1TransportClient.Connect(); // Connected successfuly.
            Assert.AreEqual(CommunicationStatuses.Done, status);
            Thread.Sleep(300);
            _dtls1TransportClient.Disconnect();

            // Assert.
            dtlsClientMock.Verify(x => x.Receive(It.IsAny<byte[]>()));
        }

        [Test]
        public void ReceiveData_DtlsClientConnectedSuccessfuly_DataTransmitedToReceivedCallback()
        {
            // Arrange.
            var dtlsClientMock = new Mock<IDtlsClient>();
            dtlsClientMock.SetupGet(x => x.IsConnected).Returns(true);
            dtlsClientMock.Setup(x => x.Receive(It.IsAny<byte[]>())).Returns<byte[]>(a =>
            {
                Array.Copy(_data, a, _data.Length);
                return _data.Length;
            });

            ((Dtls1ClientTransportClient)_dtls1TransportClient).DtlsClient = dtlsClientMock.Object;
            _dtls1TransportClient.SessionId = _sessionId;
            _dtls1TransportClient.ApiType = _apiType;
            _dtls1TransportClient.ReceiveDataCallback += OnDataReceived;

            // Act.
            var status = _dtls1TransportClient.Connect(); // Connected successfuly.
            Assert.AreEqual(CommunicationStatuses.Done, status);
            Thread.Sleep(300);
            _dtls1TransportClient.Disconnect();

            // Assert.
            Assert.AreEqual(_data.Length, _receivedDataChunk.DataBufferLength);
            Assert.AreEqual(_apiType, _receivedDataChunk.ApiType);
            Assert.AreEqual(_sessionId, _receivedDataChunk.SessionId);
            Assert.IsTrue(_data.SequenceEqual(_receivedDataChunk.GetDataBuffer()));

            _dtls1TransportClient.ReceiveDataCallback -= OnDataReceived;
        }

        private void OnDataTransmited(DataChunk dataChunk)
        {
            _transmitedDataChunk = dataChunk;
        }

        private void OnDataReceived(DataChunk dataChunk, bool val)
        {
            _receivedDataChunk = dataChunk;
        }
    }
}
