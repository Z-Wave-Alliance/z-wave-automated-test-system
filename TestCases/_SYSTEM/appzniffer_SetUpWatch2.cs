// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetUpWatch
    -Watch")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.ZnifferPTI, InterfaceType = InterfaceTypes.TCP, RfRegion = RfRegions.US_LR)]
    public class appzniffer_SetUpWatch2 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias Zniffer = new Alias(3);
            Alias[] ALL = new Alias[] { Primary, Secondary };

            ReportService.LogText("Start");


            Service.ApplicationNodeInformation(Primary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_BASIC.ID });
            Service.ApplicationNodeInformation(Secondary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_BASIC.ID });


            ParallelService.SetDefault(ALL);

            var nid = Service.IncludeNode(Primary, Secondary).AddNodeResult.Node;
            AssertIsTrue(nid == 2, "Controller added to the network NID:" + nid.ToString(), "Unable to add: Controller");

            ZnifferToken.ExpectData zt = ZnifferService.ExpectData(Zniffer, 5000);
            zt.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_GET(), null, false);
            Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
            AssertIsTrue(zt.WaitCompletedSignal(), "1. step");

            using (ZnifferService.Settings.IsExpectingFailure.Set())
            {
                using (ZnifferService.ExpectData(5000).SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_GET(), HeaderTypes.BROADCAST, false))
                {
                    Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
                }
            }

            using (ZnifferService.ExpectData(5000).SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_GET(), HeaderTypes.SINGLECAST, false))
            {
                using (ZnifferService.ExpectData(5000).SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_REPORT(), HeaderTypes.SINGLECAST, false))
                {
                    Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
                    Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT());
                }
            }

            using (ZnifferService.Settings.IsExpectingFailure.Set())
            {
                using (ZnifferService.ExpectData(5000).
                    SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_GET(), HeaderTypes.SINGLECAST, false).
                    SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_REPORT(), HeaderTypes.SINGLECAST, false))
                {
                    Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
                }
            }

            using (ZnifferService.ExpectData(5000).
                SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_GET(), HeaderTypes.SINGLECAST, false).
                SetUpWatch(null, null, null, new COMMAND_CLASS_BASIC.BASIC_REPORT(), HeaderTypes.SINGLECAST, false, false, 2))
            {
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT());
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            }

            for (int i = 0; i < 0; i++)
            {
                ZnifferToken.CollectData zct1 = ZnifferService.CollectData().
                    SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_GET(), HeaderTypes.SINGLECAST, false).
                    SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_REPORT(), HeaderTypes.SINGLECAST, false);
                ZnifferToken.CollectData zct2 = ZnifferService.CollectData().
                    SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_GET(), HeaderTypes.SINGLECAST, false);
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT());
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT());
                ZnifferResult.CollectData zcr1 = zct1.WaitCompletedSignal();
                ZnifferResult.CollectData zcr2 = zct2.WaitCompletedSignal();
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT());
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT());
                Inconclusive(zcr1.Frames.Count == 5, "frame count");
                Inconclusive(zcr2.Frames.Count == 3, "frame count");
            }
        }
    }
}