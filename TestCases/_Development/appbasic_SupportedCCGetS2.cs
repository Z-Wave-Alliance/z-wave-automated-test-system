// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Security 2 Controller Side Inclusion Workflow Test case")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    public class appbasic_SupportedCCGetS2 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias PrimaryCtrl = new Alias(1);
            Alias Slave = new Alias(2);
            Alias[] ALL = new Alias[] { PrimaryCtrl, Slave,  };

            ParallelService.Reset(ALL);

            Service.ApplicationNodeInformation(Slave, true, (byte)GenericTypes.SWITCH_MULTILEVEL, (byte)SpecificTypes.NOT_USED, COMMAND_CLASS_SECURITY_2.ID);
            Service.ApplicationNodeInformationCmdClasses(Slave, new byte[] { 1, 2, 3, 4, 5 }, new byte[] { 6, 7, 8, 9, 10 }, new byte[] { 11, 12, 13, 14, 15 });
            ApplicationService.AddSecurity(PrimaryCtrl, SecurityVersions.S2);

            #region Slave
            if (true)
            {
                var addToken = AsyncService.AddNodeWithDSK(PrimaryCtrl, Modes.NodeAny, 62398);
                var learnToken = AsyncService.SetLearnMode(Slave);
                var addRes = addToken.WaitCompletedSignal();
                var learnRes = learnToken.WaitCompletedSignal();
                AssertIsTrue(addRes);
                AssertIsTrue(learnRes);
                AssertIsTrue(addRes.SubstituteStatus == SubstituteStatuses.Done);

                Service.SendDataEx(PrimaryCtrl, Slave, new COMMAND_CLASS_SECURITY_2.SECURITY_2_COMMANDS_SUPPORTED_GET(), SecuritySchemes.S2_ACCESS);
                Delay(1000);
                Service.SendDataEx(PrimaryCtrl, Slave, new COMMAND_CLASS_SECURITY_2.SECURITY_2_COMMANDS_SUPPORTED_GET(), SecuritySchemes.S2_AUTHENTICATED);
                Delay(1000);    
                Service.SendDataEx(PrimaryCtrl, Slave, new COMMAND_CLASS_SECURITY_2.SECURITY_2_COMMANDS_SUPPORTED_GET(), SecuritySchemes.S2_UNAUTHENTICATED);
            }
            #endregion
        }
    }
}