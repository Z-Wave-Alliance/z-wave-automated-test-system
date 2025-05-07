// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetDefault
    -Add controller. 
    -SendData
    -Multi Security Keys")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, "m3", LibTypes.EndDeviceLib)]
    public class appzniffer_AllPossibleWatchesWithCountersSecureS2 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            #region Aliases
            Alias Primary = new Alias(1, "Primary");
            Alias Slave = new Alias(2, "Slave");
            Alias[] ALL = new Alias[] { Primary, Slave };
            #endregion

            #region Reset
            ParallelService.Reset(ALL);
            #endregion

            #region Add Security support
            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            #endregion

            ApplicationService.AddBasic(Slave);

            ReportService.LogText("Add Slave");
            ZnifferToken.ExpectData zte = ZnifferService.ExpectData(20000);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.KEX_GET(), null, null, false, false);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.KEX_REPORT(), null, null, false, false);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.KEX_SET(), null, null, false, false);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT(), null, null, false, 2, false);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), null, null, false, 6, false);
            Service.AddNode(Primary, Slave);
            Assert.IsTrue(zte.WaitCompletedSignal());

            ReportService.LogTitle("Different Zniffer watch combinations");

            var networkKeyS2Last = new byte[16];
            networkKeyS2Last[0] = 0xFF;
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, networkKeyS2Last, SecuritySchemes.S2_AUTHENTICATED);
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Primary, 0x02);
            ReportService.LogText("1 default and correct key ");
            ZnifferService.SetSecurityNetworkKeyS2(networkKeyS2Last);
            zte = ZnifferService.ExpectData(2000);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), null, null, false, null);
            var requestRes = Service.RequestData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Assert.IsTrue(zte.WaitCompletedSignal());

            ReportService.LogText("1.2 default and wrong key ");
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Primary, 0x02);
            ZnifferService.SetSecurityNetworkKeyS2(new byte[16]);
            zte = ZnifferService.ExpectData(2000);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), null, null, false, null);
            requestRes = Service.RequestData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Assert.IsTrue(zte.WaitCompletedSignal());

            ReportService.LogText("2 true and correct key");
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Primary, 0x02);
            ZnifferService.SetSecurityNetworkKeyS2(networkKeyS2Last);
            zte = ZnifferService.ExpectData(2000);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), null, null, false, true);
            requestRes = Service.RequestData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Assert.IsFalse(zte.WaitCompletedSignal());

            ReportService.LogText("2.2 true and wrong key");
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Primary, 0x02);
            ZnifferService.SetSecurityNetworkKeyS2(new byte[16]);
            zte = ZnifferService.ExpectData(2000);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), null, null, false, true);
            requestRes = Service.RequestData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Assert.IsFalse(zte.WaitCompletedSignal());

            ReportService.LogText("3 false and correct key non secure watch");
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Primary, 0x02);
            ZnifferService.SetSecurityNetworkKeyS2(networkKeyS2Last);
            zte = ZnifferService.ExpectData(2000);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_BASIC.BASIC_GET(), null, null, false, false);
            requestRes = Service.RequestData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Assert.IsFalse(zte.WaitCompletedSignal());

            ReportService.LogText("3.2 true and correct key non secure watch");
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Primary, 0x02);
            ZnifferService.SetSecurityNetworkKeyS2(networkKeyS2Last);
            zte = ZnifferService.ExpectData(2000);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_BASIC.BASIC_GET(), null, null, false, true);
            requestRes = Service.RequestData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Assert.IsTrue(zte.WaitCompletedSignal());

            ReportService.LogText("4 false and correct key");
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Primary, 0x02);
            ZnifferService.SetSecurityNetworkKeyS2(networkKeyS2Last);
            zte = ZnifferService.ExpectData(2000);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), null, null, false, false);
            requestRes = Service.RequestData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Assert.IsTrue(zte.WaitCompletedSignal());

            ReportService.LogText("4.2 false and wrong key");
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Primary, 0x02);
            ZnifferService.SetSecurityNetworkKeyS2(networkKeyS2Last);
            zte = ZnifferService.ExpectData(2000);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), null, null, false, false);
            requestRes = Service.RequestData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Assert.IsTrue(zte.WaitCompletedSignal());

            
        }
    }
}