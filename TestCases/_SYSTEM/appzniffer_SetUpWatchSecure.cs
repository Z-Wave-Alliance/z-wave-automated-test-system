// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetUpWatch Secure
    -Watch")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    public class appzniffer_SetUpWatchSecure : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias[] ALL = new Alias[] { Primary, Secondary };

            ReportService.LogText("Start");

            Service.ApplicationNodeInformation(Primary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_BASIC.ID });
            Service.ApplicationNodeInformation(Secondary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_BASIC.ID });

            ParallelService.SetDefault(ALL);

            Service.ApplicationNodeInformation(Primary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_BASIC.ID });
            Service.ApplicationNodeInformation(Secondary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_BASIC.ID });
            ApplicationService.AddSecurity(Primary, SecurityVersions.S0);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S0);
            ZnifferService.SetSecurityNetworkKey(ApplicationService.GetSecurityNetworkKey(Primary));
            int nid = Service.AddNode(Primary, Secondary).NodeId;
            AssertIsTrue(nid == 2, "Controller added to the network NID:" + nid.ToString(), "Unable to add: Controller");

            for (int i = 0; i < 2000; i++)
            {
                ZnifferToken.ExpectData zt = ZnifferService.ExpectData(5000);
                zt.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_GET(), null, false);
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
                ZnifferResult.ExpectData zr = zt.WaitCompletedSignal();
                AssertIsTrue(zr, "1. step");
                AssertIsTrue(zr.Frames.Count == 1, "frame count");


                zt = ZnifferService.ExpectData(5000);
                zt.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_GET(), HeaderTypes.BROADCAST, false);
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
                using (ZnifferService.Settings.IsExpectingFailure.Set())
                {
                    zr = zt.WaitCompletedSignal();
                }
                AssertIsTrue(!zr, "2. step");
                AssertIsTrue(zr.Frames.Count == 0, "frame count");


                zt = ZnifferService.ExpectData(5000);
                zt.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_GET(), HeaderTypes.SINGLECAST, false);
                zt.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_REPORT(), HeaderTypes.SINGLECAST, false);
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT());
                zr = zt.WaitCompletedSignal();
                AssertIsTrue(zr, "3. step");
                AssertIsTrue(zr.Frames.Count == 2, "frame count");


                zt = ZnifferService.ExpectData(5000);
                zt.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_GET(), HeaderTypes.SINGLECAST, false);
                zt.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_REPORT(), HeaderTypes.SINGLECAST, false);
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
                using (ZnifferService.Settings.IsExpectingFailure.Set())
                {
                    zr = zt.WaitCompletedSignal();
                }
                AssertIsTrue(!zr, "4. step");
                AssertIsTrue(zr.Frames.Count == 1, "frame count");

                zt = ZnifferService.ExpectData(5000);
                zt.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_GET(), HeaderTypes.SINGLECAST, false);
                zt.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_REPORT(), HeaderTypes.SINGLECAST, false);
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT());
                zr = zt.WaitCompletedSignal();
                AssertIsTrue(zr, "5. step");
                AssertIsTrue(zr.Frames.Count == 2, "frame count");

                ZnifferToken.CollectData zct = ZnifferService.CollectData(2000);
                zct.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_GET(), HeaderTypes.SINGLECAST, false);
                zct.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_REPORT(), HeaderTypes.SINGLECAST, false);
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT());
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT());

                ZnifferResult.CollectData zcr = zct.WaitCompletedSignal();
                ReportService.LogOk("6. step");
                IList<ZnifferDataItem> result = zcr.Frames;
                AssertIsTrue(result.Count == 5, "frame count");
            }
        }
    }
}