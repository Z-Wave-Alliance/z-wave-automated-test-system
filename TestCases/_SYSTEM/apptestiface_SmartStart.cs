// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [TestInterfaceDeviceMapping(2, LibTypes.SwitchOnOff)]
    public class apptestiface_SmartStart : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias DUT = new Alias(2);
            //Alias PTI = new Alias(3);

            Service.Reset(Primary);

            var res = Service.SerialApiSetNodeIdBaseType(Primary, 2);
            AssertIsTrue(res && (res.ByteArray[1] > 0));

            //var t = ZnifferService.ExpectData(PTI, new CmdClassFilter()
            //        .Payload(new COMMAND_CLASS_BASIC.BASIC_SET())
            //        .HeaderExclude(HeaderTypes.SINGLECAST)
            //        .S2Extension(SecurityS2ExtensionTypes.Span)
            //        .Source(2).Dest(1)
            //    .ExcludeFilter()
            //        .Payload(new COMMAND_CLASS_BASIC.BASIC_SET())
            //        .Header(HeaderTypes.SINGLECAST).Hide()
            //        .S2Extension(SecurityS2ExtensionTypes.Span)
            //        .Dest(1)
            //        .Src(2), 3000);

            //var res = t.WaitCompletedSignal();

            //ZnifferService.ExpectData(PTI);
            //AsyncService.ExpectData(Primary, null, )


            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);
            AssertIsTrue(TestInterfaceService.ExcludeNode(Primary, DUT));

           

            ApplicationService.Provisioning.Add(Primary, DUT, SecuritySchemeSet.ALL, Modes.NodeOptionLongRange);
            var addToken = AsyncService.ExpectSmartStartAdd(Primary, 60000);
            TestInterfaceService.SoftReset(DUT);
            var addRes = addToken.WaitCompletedSignal();
            AssertIsTrue(addRes);
            AssertIsTrue(addRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done);
        }
    }
}
