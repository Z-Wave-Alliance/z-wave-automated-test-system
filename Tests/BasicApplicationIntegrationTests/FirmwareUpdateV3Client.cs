// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Enums;
using ZWave.Layers;
using ZWave.Layers.Session;
using ZWave.Layers.Transport;

namespace BasicApplicationIntegrationTests
{
    //[TestFixture]
    public class FirmwareUpdateV3Client : BasicTestsBase
    {
        string COM_DUT = "COM51"; // slave
        int TASK_TIMEOUT = 60000*100;

        //[SetUp]
        public void SetUp()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Slave slave = al.CreateSlave())
            {
                slave.Connect(new SerialPortDataSource(COM_DUT, BaudRates.Rate_115200));
                VersionResult res = slave.GetVersion();
                Assert.IsTrue(res.State == ActionStates.Completed);
                Assert.IsTrue(res.Library == Libraries.SlaveEnhancedLib || res.Library == Libraries.SlaveLib || res.Library == Libraries.SlaveRoutingLib);
            }
        }

        //[Test]
        public void LearnMode()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Slave slave = al.CreateSlave())
            {
                slave.Connect(new SerialPortDataSource(COM_DUT, BaudRates.Rate_115200));
                slave.ApplicationNodeInformation(true, 0x11, 0x00, new byte[] 
                { 
                    COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.ID
                });

                ActionToken learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);
                Assert.IsTrue(learnToken.WaitCompletedSignal());
            }
        }

        byte[] MANUFACTURER_ID = new byte[] { 0x01, 0x02 };

        //[Test]
        public void FirmwareUpdateTask()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Slave slave = al.CreateSlave())
            {
                slave.Connect(new SerialPortDataSource(COM_DUT, BaudRates.Rate_115200));

                #region FIRMWARE_MD_GET -> FIRMWARE_MD_REPORT
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_MD_REPORT mdReport = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_MD_REPORT();
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_MD_REPORT.TVG1 vg1 = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_MD_REPORT.TVG1();
                vg1.firmwareId = new byte[] { 0x00, 0x00 };
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_MD_REPORT.TVG1 vg2 = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_MD_REPORT.TVG1();
                vg2.firmwareId = new byte[] { 0x00, 0x01 };
                mdReport.vg1 = new List<COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_MD_REPORT.TVG1>();
                mdReport.vg1.Add(vg1);
                mdReport.vg1.Add(vg2);

                mdReport.manufacturerId = MANUFACTURER_ID;
                mdReport.numberOfFirmwareTargets = 2;
                mdReport.maxFragmentSize = new byte[] { 0x00, 0x40 };
                mdReport.firmwareUpgradable = 1;
                mdReport.firmware0Id = new byte[] { 0x00, 0x01 };
                mdReport.firmware0Checksum = new byte[] { 0x00, 0x01 };
                
                
                slave.ResponseData(mdReport, TXO, new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_MD_GET());
                #endregion

                #region VERSION_COMMAND_CLASS_GET -> VERSION_COMMAND_CLASS_REPORT
                COMMAND_CLASS_VERSION_V2.VERSION_COMMAND_CLASS_REPORT cmdClassReport = new COMMAND_CLASS_VERSION_V2.VERSION_COMMAND_CLASS_REPORT();
                cmdClassReport.requestedCommandClass = COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.ID;
                cmdClassReport.commandClassVersion = 0x03;

                slave.ResponseData(cmdClassReport, TXO, new COMMAND_CLASS_VERSION_V2.VERSION_COMMAND_CLASS_GET());
                #endregion

                #region FIRMWARE_UPDATE_MD_REQUEST_GET -> FIRMWARE_UPDATE_MD_REQUEST_REPORT
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_REQUEST_REPORT updReqReport = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_REQUEST_REPORT();
                updReqReport.status = 0xFF;

                ExpectDataResult expRes = slave.ExpectData(new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_REQUEST_GET(), TASK_TIMEOUT);
                Assert.AreEqual(ActionStates.Completed, expRes.State);

                ActionResult sendRes = slave.SendData(expRes.SrcNodeId, updReqReport, TXO);
                Assert.AreEqual(ActionStates.Completed, sendRes.State);
                #endregion

                int reportNumber = 1;
                bool done = false;
                while (true)
                {
                    COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_GET request = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_GET();
                    request.numberOfReports = 1;
                    request.properties1.reportNumber1 = (byte)(reportNumber >> 7);
                    request.reportNumber2 = (byte)reportNumber;
                    RequestDataResult reqRes = slave.RequestData(expRes.SrcNodeId, request, TXO, new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_REPORT(), 20000);
                    if (reqRes.State == ActionStates.Completed)
                    {
                        COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_REPORT reqCmd = reqRes.Command;


                        if (reqCmd.properties1.last > 0)
                        {
                            done = true;
                            break;
                        }
                        reportNumber++;
                    }
                    else
                        break;
                }

                slave.SendData(expRes.SrcNodeId, new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_STATUS_REPORT()
                {
                    status = 0xFF,
                    waittime = new byte[] { 0x00, 0x14 }
                }, TXO);
                slave.SetRFReceiveMode(0x00);
                Thread.Sleep(40000);
                slave.SetRFReceiveMode(0x01);
                Assert.IsTrue(done);

            }
        }
    }
}


