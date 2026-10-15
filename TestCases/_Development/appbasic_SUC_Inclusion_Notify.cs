// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Show that SUC notify delay is needed")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.ControllerStaticLib)]
    [DeviceMapping(4, LibTypes.EndDeviceLib)]
    public class appbasic_SUC_Inclusion_Notify : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1, "Primary");
            Alias SUC = new Alias(2, "SUC");
            Alias SecondaryStatic = new Alias(3, "Slave");
            Alias Slave2 = new Alias(4, "Slave2");

            ReportService.LogText("Start");
            ParallelService.Reset(Primary, SUC, SecondaryStatic, Slave2);

            BasicResult.AddNode addResult = Service.AddNode(Primary, SUC);
            AssertIsTrue(addResult, "SUC node added sucessfuly", "SUC node not added sucessfuly");

            ZnifferToken.ExpectData zt = ZnifferService.ExpectData(5000);
            zt.SetUpWatch(new ZWAVE_CMD_CLASS.CMD_SET_SUC_ACK() { result = 0x80, properties1 = 0x00 }, HeaderTypes.SINGLECAST, true);
            Service.SetSUCNodeID(Primary, SUC, true, 0x00, true);
            ZnifferResult.ExpectData zr = zt.WaitCompletedSignal();
            AssertIsTrue(zr, "Set SUC Ack frame detected", "Set SUC Ack frame not detected");

            Service.AddNode(Primary, SecondaryStatic);

            Delay(2000); // wait SUC notify

            Service.AddNode(Primary, Slave2);
        }
    }
}