// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.Layers.Session;
using ZWave.Layers.Transport;
using ZWave.Layers;
using ZWave.Enums;
using ZWave.BasicApplication.Operations;
using ZWave;
using Utils;
using System.Threading;
using ZWave.Layers.Application;
using ZWave.TextApplication;

namespace BasicApplicationIntegrationTests
{
    [TestFixture]
    public class GetVersion : BasicTestsBase
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
            TextApplicationLayer tl = new TextApplicationLayer(new SessionLayer(), new TextFrameLayer(), new TextTransportLayer());
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            for (int i = 0; i < POW; i++)
            {
                using (TextDevice txtd = tl.CreateTextDevice())
                {
                    using (Controller ctrl = al.CreateController())
                    {
                        txtd.Connect(new TextDataSource(null, null));
                        txtd.Send("Hello world!");
                        ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                        for (int j = 0; j < POW; j++)
                        {
                            VersionResult res = ctrl.GetVersion();
                            Assert.AreEqual(ActionStates.Completed, res.State);
                        }
                    }
                }
            }
        }
    }
}
