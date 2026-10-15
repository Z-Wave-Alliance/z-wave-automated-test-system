// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"SetDefault, Smart Start/Remove slave", Tags.SYS, Tags.PCController)]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    [RailDeviceMapping(4, "N1", ChipSeries = ChipSeries.ZW070x)]
    public class apprail_SmartStartAddRemoveNodeCallbacks : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Bridge = new Alias(2);
            Alias Slave = new Alias(3);
            Alias Rail = new Alias(4);
            ReportService.LogText("Start");
            ApplicationService.SecurityTestSettings.IncludeSettings.TimeoutMs = 30000;
            ParallelService.Reset(Primary, Bridge, Slave);
            var memRes = Service.MemoryGetId(Primary);
            Service.IncludeNode(Primary, Bridge);
            byte nextNodeId = 3;
            for (int i = 0; i < 20; i++)
            {
                Service.AddNodeVirtual(Primary, Bridge);
                nextNodeId++;
            }

            RailService.Reset(Rail);
            RailService.DeviceSetup(Rail, Context.Frequency, 30, 50, RailChannels.Ch1);
            RailService.SetRx(Rail, false);

            Delay(10000);

            var dsk = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            ApplicationService.Provisioning.Add(Primary, dsk, SecuritySchemeSet.ALL, Modes.NodeOptionNetworkWide);
            var addToken = AsyncService.ExpectSmartStartAdd(Primary, 300000);

            //Setup fake send data
            var smartHomeId = new byte[] { 0xC9, 0x0A, 0x0B, 0x0C };
            var smartPrimeCmd = new ZWAVE_CMD_CLASS.SMART_START_PRIME()
            {
                properties1 = 0xD3,
                properties2 = 0x9C,
                properties3 = 0x01,
                genericDeviceClass = 0x11,
                specificDeviceClass = 0x00,
                commandClasses = new byte[] { 0x20 }
            };
            var smartPrimeFrame = RailService.PrepareFrame(smartHomeId, 0x00, 0xFF, smartPrimeCmd, RailChannels.Ch1,
                (byte)HeadTypesMPDU.Explorer, 0x0F);
            RailService.SetTxLength(Rail, smartPrimeFrame.Length);
            RailService.SetTxPayload(Rail, smartPrimeFrame);
            RailService.SetTx(Rail, 1);
            RailService.SetRx(Rail, false);

            Delay(500, "wait for controller starts inclusion");

            var smartIncludeCmd = new ZWAVE_CMD_CLASS.SMART_START_INCLUDE()
            {
                properties1 = 0xD3,
                properties2 = 0x9C,
                properties3 = 0x01,
                genericDeviceClass = 0x11,
                specificDeviceClass = 0x00,
                commandClasses = new byte[] { 0x20 }
            };
            var smartIncludeFrame = RailService.PrepareFrame(smartHomeId, 0x00, 0xFF, smartIncludeCmd, RailChannels.Ch1, (byte)HeadTypesMPDU.Explorer, 0x0F);
            RailService.SetTxLength(Rail, smartIncludeFrame.Length);
            RailService.SetTxPayload(Rail, smartIncludeFrame);
            RailService.SetTx(Rail, 1);
            RailService.SetRx(Rail, false);

            //AsyncService.WaitNodeStatus(Primary, 1000, NodeStatuses.NodeFound);

            var ackFrame = RailService.PrepareFrame(memRes.HomeId, nextNodeId, 0x01, null, RailChannels.Ch1, (byte)HeadTypesMPDU.Ack, 0x83);
            RailService.SetTxLength(Rail, ackFrame.Length);
            RailService.SetTxPayload(Rail, ackFrame);
            RailService.SetTxDelay(Rail, 50);
            RailService.SetTx(Rail, 4);
            RailService.SetRx(Rail, false);


            var addRes = addToken.WaitCompletedSignal();

            ApplicationService.Provisioning.Add(Primary, Slave, SecuritySchemeSet.ALL, Modes.NodeOptionNetworkWide);
            addToken = AsyncService.ExpectSmartStartAdd(Primary, 300000);
            var learnRes = Service.SetLearnMode(Slave, LearnModes.LearnModeSmartStart | LearnModes.LearnModeNetworkMask, 50000);
            addRes = addToken.WaitCompletedSignal();
            if (addRes)
            {
                ReportService.LogText($"Slave added to the network NID: {addRes.AddNodeResult.Node.ToString()}");
            }

            Delay(4 * 60000, "wait after collback");
        }
    }
}