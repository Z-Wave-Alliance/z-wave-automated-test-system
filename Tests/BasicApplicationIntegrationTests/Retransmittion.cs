// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using NUnit.Framework;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.Layers;
using ZWave.Enums;
using ZWave;
using ZWave.BasicApplication.Enums;
using ZWave.BasicApplication.Operations;
using ZWave.Layers.Session;
using ZWave.Layers.Transport;
using System.Threading;
using Utils.Threading;

namespace BasicApplicationIntegrationTests
{
    [TestFixture]
    public class Retransmittion : BasicTestsBase
    {
        int POW = 3;

        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                VersionResult res = ctrl.GetVersion();
                Assert.AreEqual(ActionStates.Completed, res.State);
                Assert.Contains(res.Library, new Libraries[] { Libraries.ControllerStaticLib, Libraries.InstallerLib, Libraries.ControllerPortableLib, Libraries.ControllerBridgeLib });
                ActionResult ares = ctrl.SetDefault();
                Assert.AreEqual(ActionStates.Completed, ares.State);
            }
        }

        [Test]
        public void TestRetransmittionOneThread()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                for (int i = 0; i < POW; i++)
                {
                    var token = ctrl.SessionClient.ExecuteAsync(new RetransmittedOperation(7000));
                    ActionResult res = token.WaitCompletedSignal();
                    Assert.AreEqual(ActionStates.Failed, res.State);
                }
            }
        }

        [Test]
        public void TestRetransmittionExpired()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                for (int i = 0; i < POW; i++)
                {
                    ActionResult res = ctrl.SessionClient.ExecuteAsync(new RetransmittedOperation(1000)).WaitCompletedSignal();
                    Assert.AreEqual(ActionStates.Failed, res.State);
                }
            }
        }

        [Test]
        public void TestRetransmittionManyThread()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                Thread[] threads = new Thread[POW];
                for (int j = 0; j < POW; j++)
                {
                    threads[j] = new Thread(() =>
                        {
                            for (int i = 0; i < POW; i++)
                            {
                                ActionResult res = ctrl.SessionClient.ExecuteAsync(new RetransmittedOperation(7000)).WaitCompletedSignal();
                                Assert.AreEqual(ActionStates.Failed, res.State);
                            }
                        });
                    threads[j].Start();
                }

                for (int j = 0; j < POW; j++)
                {
                    threads[j].Join();
                }
            }
        }
    }

    public class RetransmittedOperation : ApiOperation
    {
        public int TimeoutMs { get; set; }
        public RetransmittedOperation(int timeoutMs)
            : base(true, (CommandTypes)0x00, false)
        {
            TimeoutMs = timeoutMs;
        }

        ApiMessage message;
        protected override void CreateWorkflow()
        {
            ActionUnits.Add(new StartActionUnit(SetStateCompleting, TimeoutMs, message));
        }

        protected override void CreateInstance()
        {
            message = new ApiMessage(SerialApiCommands[0], new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            });
        }

        protected override ActionResult CreateOperationResult()
        {
            return new ActionResult();
        }
    }
}
