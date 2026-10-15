// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ZWave.Layers.Transport;
using ZWave.Layers;
using ZWave.BasicApplication;
using ZWave.Layers.Session;
using ZWave.Enums;
using ZWave.BasicApplication.Operations;
using ZWave.BasicApplication.Devices;
using ZWave;
using System.Threading;

namespace BasicApplicationIntegrationTests
{
    [TestFixture]
    public class SetDefault : BasicTestsBase
    {
        int POW = 5;

        [SetUp]
        public void SetUp()
        {
            using (SerialPortTransportClient tc = new SerialPortTransportClient(null))
            {
                tc.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                tc.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));
            }
        }

        [Test]
        public void TestSync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            for (int i = 0; i < POW; i++)
            {
                using (Controller ctrl = al.CreateController())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    for (int j = 0; j < POW; j++)
                    {
                        ActionResult res = ctrl.SetDefault();
                        Assert.AreEqual(ActionStates.Completed, res.State);
                    }
                }
            }
        }
    }
}
