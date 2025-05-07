// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("appbasic_SetSleepMode_FLiRS")]
    [DeviceMapping(1, LibTypes.ControllerPortableLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    public class appbasic_SetSleepMode_FLiRS : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Controller = new Alias(1);
            Alias Node = new Alias(2);

            Alias[] ALL = { Controller, Node };

            ReportService.LogText("Started");
            ParallelService.Reset(ALL);

            ReportService.LogText("Set Node to be FLIRS node");
            Service.ApplicationNodeInformation(Node, DeviceOptions.FreqListeningMode250ms,
                (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });

            Service.SetDefault(Node);

            ReportService.LogText("Add Node to the Controller");
            BasicResult.AddNode addRes = Service.AddNode(Controller, Node); ;
            AssertIsTrue(addRes, "Successfully added Node", "Unable to add Node");

            byte[] payload = new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF };

            ReportService.LogText("Send data to always listening mode");
            ZnifferToken.ExpectData zt = ZnifferService.ExpectData(5000);
            zt.SetUpWatch(0x01, 0x02, payload, null, true);
            Service.SendData(Controller, Node, payload);
            ZnifferResult.ExpectData zr = zt.WaitCompletedSignal();
            AssertIsTrue(zr, "ACK detected", "ACK not detected");

            ReportService.LogText("Set Node in frequently listening mode");
            Service.SetSleepMode(Node, SleepModes.FrequentlyListening, 0);
            
            ReportService.LogText("Send data to frequently listening Node");
            zt = ZnifferService.ExpectData(5000);
            zt.SetUpWatch(null, HeaderTypes.WAKEUP_BEAM, false);
            Service.SendData(Controller, Node, payload);
            zr = zt.WaitCompletedSignal();
            AssertIsTrue(zr, "WAKEUP_BEAM detected", "WAKEUP_BEAM not detected");

        }
    }
}