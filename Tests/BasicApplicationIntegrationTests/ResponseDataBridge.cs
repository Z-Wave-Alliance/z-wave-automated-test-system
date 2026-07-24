// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZWave.BasicApplication.Enums;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using NUnit.Framework;
using ZWave.Layers;
using ZWave;
using ZWave.Enums;
using ZWave.Layers.Session;
using System.Threading;
using ZWave.BasicApplication.Operations;
using ZWave.Layers.Transport;
using ZWave.Devices;

namespace BasicApplicationIntegrationTests
{
    [TestFixture]
    public class ResponseDataBridge : BasicTestsBase
    {
        byte[] cmd_get = new byte[] { 0x20, 0x02 };
        byte[] cmd_report = new byte[] { 0x20, 0x03, 0x55 };
        int POW = 10;
        int REQUEST_DATA_TIMEOUT = 3000;

        /// <summary>
        /// Builds a responder that only answers a cmd_get addressed to <paramref name="dstNode"/>.
        /// </summary>
        /// <remarks>
        /// Device.ResponseData has no overload taking both a callback and a node, so the
        /// operation is built here to reach DstNode. That is the node the Bridge Application
        /// Command Handler reports as the destination, so it selects which of the bridge's
        /// virtual nodes the request was addressed to. The reply is issued from that same
        /// node, because ResponseDataOperation answers with a Bridge Controller Node Send
        /// Data whose source is the destination of the request.
        /// </remarks>
        private ResponseDataOperation CreateResponder(Device device, NodeTag dstNode, ResponseDataDelegate receiveCallback)
        {
            return new ResponseDataOperation(device.Network, receiveCallback, TXO, NodeTag.Empty, cmd_get[0], cmd_get[1])
            {
                DstNode = dstNode
            };
        }

        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (BridgeController bridge = al.CreateBridgeController())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    bridge.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                    VersionResult res = ctrl.GetVersion();
                    Assert.IsTrue(res.State == ActionStates.Completed);
                    Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || res.Library == Libraries.InstallerLib || res.Library == Libraries.ControllerPortableLib || res.Library == Libraries.ControllerBridgeLib);
                    res = bridge.GetVersion();
                    Assert.IsTrue(res.State == ActionStates.Completed);
                    Assert.IsTrue(res.Library == Libraries.ControllerBridgeLib);
                    ActionResult ares = ctrl.SetDefault();
                    Assert.IsTrue(ares.State == ActionStates.Completed);
                    ares = bridge.SetDefault();
                    Assert.IsTrue(ares.State == ActionStates.Completed);

                    ActionToken addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                    ActionToken learnToken = bridge.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);
                    Assert.IsTrue(addToken.WaitCompletedSignal().IsStateCompleted);
                    Assert.IsTrue(learnToken.WaitCompletedSignal().IsStateCompleted);

                    addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                    learnToken = bridge.SetVirtualDeviceLearnMode(new NodeTag(0), VirtualDeviceLearnModes.Enable, INCLUSION_TIMEOUT, null);
                    Assert.IsTrue(addToken.WaitCompletedSignal().IsStateCompleted);
                    Assert.IsTrue(learnToken.WaitCompletedSignal().IsStateCompleted);

                }
            }
        }

        [Test]
        public void TestSync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (BridgeController bridge = al.CreateBridgeController())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    bridge.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                    bridge.ResponseData(cmd_report, TXO, cmd_get);

                    for (int i = 0; i < POW; i++)
                    {
                        RequestDataResult res = ctrl.RequestData(new NodeTag(0x02), cmd_get, TXO, cmd_report, REQUEST_DATA_TIMEOUT);
                        Assert.AreEqual(ActionStates.Completed, res.State);
                    }
                }
            }
        }

        [Test]
        public void TestSlaveSync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (BridgeController bridge = al.CreateBridgeController())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    bridge.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                    bridge.ResponseData(new NodeTag(0x03), cmd_report, TXO, cmd_get);

                    for (int i = 0; i < POW; i++)
                    {
                        RequestDataResult res = ctrl.RequestData(new NodeTag(0x03), cmd_get, TXO, cmd_report, REQUEST_DATA_TIMEOUT);
                        Assert.AreEqual(ActionStates.Completed, res.State);
                    }
                }
            }
        }

        [Test]
        public void TestCallback()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (BridgeController slave = al.CreateBridgeController())
                {
                    byte counter = 0;
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));

                    slave.ResponseData((x, a, b, c) =>
                    {
                        cmd_report[2] = counter;
                        return cmd_report;
                    }, TXO, cmd_get);

                    for (int i = 0; i < POW; i++)
                    {
                        RequestDataResult res = ctrl.RequestData(new NodeTag(0x02), cmd_get, TXO, cmd_report, REQUEST_DATA_TIMEOUT);
                        Assert.AreEqual(ActionStates.Completed, res.State);
                        Assert.AreEqual(counter, res.Command[2]);
                        counter++;
                    }
                }
            }
        }

        [Test]
        public void TestSlaveCallback()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (BridgeController slave = al.CreateBridgeController())
                {
                    byte counter = 0;
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));

                    slave.ExecuteAsync(CreateResponder(slave, new NodeTag(0x03), (x, a, b, c) =>
                    {
                        cmd_report[2] = counter;
                        return cmd_report;
                    }), null);

                    for (int i = 0; i < POW; i++)
                    {
                        RequestDataResult res = ctrl.RequestData(new NodeTag(0x03), cmd_get, TXO, cmd_report, REQUEST_DATA_TIMEOUT);
                        Assert.AreEqual(ActionStates.Completed, res.State);
                        Assert.AreEqual(counter, res.Command[2]);
                        counter++;
                    }
                }
            }
        }
    }
}
