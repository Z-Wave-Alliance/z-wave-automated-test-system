// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Security 2 Inclusion test using Inclusion Task")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    [DeviceMapping(3, LibTypes.ControllerStaticLib)]
    public class appbasic_IncludeNodeSecureS2 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias PrimaryCtrl = new Alias(1);
            Alias Slave = new Alias(2);
            Alias SecondaryCtrl = new Alias(3);
            Alias[] ALL = new Alias[] { PrimaryCtrl, Slave, SecondaryCtrl };

            ParallelService.Reset(ALL);

            Service.ApplicationNodeInformation(Slave, true, (byte)GenericTypes.SWITCH_MULTILEVEL, (byte)SpecificTypes.NOT_USED, COMMAND_CLASS_SECURITY_2.ID);
            Service.ApplicationNodeInformation(SecondaryCtrl, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, COMMAND_CLASS_SECURITY_2.ID);

            ApplicationService.AddSecurity(PrimaryCtrl, SecurityVersions.S2);
            ApplicationService.AddSecurity(SecondaryCtrl, SecurityVersions.S2);

            #region Slave
            if (true)
            {
                var addRes = Service.IncludeNode(PrimaryCtrl, Slave);
                AssertIsTrue(addRes);
                AssertIsTrue(addRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done);

                Service.SendData(PrimaryCtrl, Slave, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(PrimaryCtrl, Slave, new COMMAND_CLASS_BASIC.BASIC_SET());
            }
            #endregion


            #region Controller
            if (true)
            {
                var addRes = Service.IncludeNode(PrimaryCtrl, SecondaryCtrl);
                AssertIsTrue(addRes);
                AssertIsTrue(addRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done);

                Service.SendData(PrimaryCtrl, SecondaryCtrl, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(PrimaryCtrl, SecondaryCtrl, new COMMAND_CLASS_BASIC.BASIC_SET());
            }
            #endregion

        }
    }
}