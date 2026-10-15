// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Verify that SendData callback received after call SendDataAbort")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(4, LibTypes.EndDeviceLib)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class appbasic_SendDataAbort : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Bridge = new Alias(2);
            Alias EDev = new Alias(4);

            ReportService.LogText("Start");
            ParallelService.Reset(Primary, Bridge, EDev);

            var addRes = Service.AddNode(Primary, Bridge);
            AssertIsTrue(addRes, "Include portable controller node");
            var addEDevRes = Service.AddNode(Primary, EDev);
            AssertIsTrue(addEDevRes, "Include end device node");
            byte[] payload = new byte[] { 32, 1, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6 };

            var sendRes = Service.SendData(Primary, EDev, payload, TransmitOptions.Ack | TransmitOptions.AutoRoute | TransmitOptions.Explore);
            AssertIsTrue(sendRes);
            var memRes = Service.MemoryGetId(Primary);

            Service.SetRFReceiveMode(EDev, false);
            ComplianceService.SendDataSettings.SetCallbackWaitTimeout(Primary, 3000);
            sendRes = ComplianceService.SendData(Primary, addEDevRes.Node, payload, TransmitOptions.Ack | TransmitOptions.AutoRoute | TransmitOptions.Explore);


            ComplianceService.SendDataSettings.SetCallbackWaitTimeout(Primary, 3000);
            sendRes = ComplianceService.SendDataBridge(Primary, memRes.Node, addEDevRes.Node, payload, TransmitOptions.Ack | TransmitOptions.AutoRoute | TransmitOptions.Explore);
            AssertIsTrue(sendRes);
            Delay(120000, "wait 2 min");
        }
    }
}