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
    public class appzniffer_SetUpWatchSecureS2 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            #region Aliases
            Alias Primary = new Alias(1, "Primary");
            Alias Secondary = new Alias(2, "Secondary");
            Alias[] ALL = new Alias[] { Primary, Secondary };
            #endregion

            #region Reset
            ParallelService.Reset(ALL);
            #endregion

            #region Add Security support
            ApplicationService.AddSecurity(Primary, SecurityVersions.S2, new byte[] { COMMAND_CLASS_SECURITY_2.ID});
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S2, new byte[] { COMMAND_CLASS_SECURITY_2.ID });
            ParallelService.Reset(ALL);
            #endregion

            ApplicationService.SecurityTestSettings.NetworkKeyChanged(Primary, ZnifferService.SetSecurityNetworkKey);

            var ztc = ZnifferService.CollectData();
            ztc.SetUpWatch(null, null, null, null, null, null, false, false);
            var addRes = Service.IncludeNode(Primary, Secondary);
            ztc.WaitCompletedSignal();
            Assert.IsTrue(addRes);
            Assert.IsTrue(addRes.SetLearnMode);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.AddNodeResult.SubstituteStatus);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SetLearnMode.SubstituteStatus);

            var zt = ZnifferService.ExpectData(2000);
            zt.SetUpWatchS2(null, null, null, new COMMAND_CLASS_BASIC.BASIC_GET(), 0x01, false, 1);
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Primary, addRes.AddNodeResult.Node);
            Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
            Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
            var zr = zt.WaitCompletedSignal();
            AssertIsTrue(zr, "0. step");
            AssertIsTrue(zr.Frames.Count == 1, "frame with span count :" + zr.Frames.Count);

            zt = ZnifferService.ExpectData(2000);
            zt.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_GET(), null, false);
            Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
            zr = zt.WaitCompletedSignal();
            AssertIsTrue(zr, "1. step");
            AssertIsTrue(zr.Frames.Count == 1, "frame count :" + zr.Frames.Count);
            AssertIsTrue(zr.Frames[0].OriginalPayload[0] == COMMAND_CLASS_BASIC.ID);
            AssertIsTrue(zr.Frames[0].Payload[0] == COMMAND_CLASS_SECURITY_2.ID);

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


            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Primary, 2);
            zt = ZnifferService.ExpectData(2000);
            zt.SetUpWatchS2(1, 2, null, null, (byte)SecurityS2ExtensionTypes.Span, null, 1);
            Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
            zr = zt.WaitCompletedSignal();
            AssertIsTrue(zr, "7. step");
            AssertIsTrue(zr.Frames.Count == 1, "frame count :" + zr.Frames.Count);
            AssertIsTrue(zr.Frames[0].OriginalPayload[0] == COMMAND_CLASS_BASIC.ID);
            AssertIsTrue(zr.Frames[0].Payload[0] == COMMAND_CLASS_SECURITY_2.ID);
        }
    }
}