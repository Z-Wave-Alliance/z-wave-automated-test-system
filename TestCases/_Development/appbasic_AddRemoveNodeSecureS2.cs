// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;
using System.Linq;
using System;

namespace ZatsTests
{
    [ZatsTestCase(@"Security 2 Controller Side Inclusion Workflow Test case")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    [DeviceMapping(3, LibTypes.ControllerStaticLib)]
    public class appbasic_AddRemoveNodeSecureS2 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias PrimaryCtrl = new Alias(1);
            Alias Slave = new Alias(2);
            Alias SecondaryCtrl = new Alias(3);
            Alias[] ALL = new Alias[] { PrimaryCtrl, Slave, SecondaryCtrl };

            ParallelService.Reset(ALL);

            Service.ApplicationNodeInformation(Slave, true, (byte)GenericTypes.SWITCH_MULTILEVEL, (byte)SpecificTypes.NOT_USED, COMMAND_CLASS_SECURITY_2.ID);
            Service.ApplicationNodeInformationCmdClasses(Slave,
                new byte[] { COMMAND_CLASS_SECURITY_2.ID },
                new byte[] { COMMAND_CLASS_SECURITY_2.ID },
                new byte[] { COMMAND_CLASS_BASIC.ID });
            ApplicationService.AddBasic(Slave);

            Service.ApplicationNodeInformation(SecondaryCtrl, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, COMMAND_CLASS_SECURITY_2.ID);
            ApplicationService.AddBasic(SecondaryCtrl);
            
            ApplicationService.AddSecurity(PrimaryCtrl, SecurityVersions.S2);
            ApplicationService.AddSecurity(SecondaryCtrl, SecurityVersions.S2);
            
            #region Slave
            if (true)
            {
                //var inclusion = Service.IncludeNode(PrimaryCtrl, Slave, Modes.NodeAny, LearnModes.LearnModeClassic, 120000);
                //AssertIsTrue(inclusion, "Included Slave");
                //or:
                var dsk = Service.GetDsk(Slave, 2);
                ApplicationService.SecurityTestSettings.IncludeSettings.JoinDSK = dsk;
                ApplicationService.SecurityTestSettings.IncludeSettings.GrantedSchemes = new SecuritySchemes[0];
                var addToken = AsyncService.AddNodeWithDSK(PrimaryCtrl, dsk);
                var learnToken = AsyncService.SetLearnMode(Slave);
                var addRes = addToken.WaitCompletedSignal();
                var learnRes = learnToken.WaitCompletedSignal();
                AssertIsTrue(addRes);
                AssertIsTrue(learnRes);
                AssertIsTrue(addRes.SubstituteStatus == SubstituteStatuses.Done);

                Service.SendData(PrimaryCtrl, Slave, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(PrimaryCtrl, Slave, new COMMAND_CLASS_BASIC.BASIC_SET());
            }
            #endregion


            #region Controller
            if (true)
            {
                //var inclusion = Service.AddNode(PrimaryCtrl, SecondaryCtrl, Modes.NodeAny, LearnModes.LearnModeClassic, 120000);
                //AssertIsTrue(inclusion, "Included Secondary");
                //or:

                var senderPublicKey = ApplicationService.SecurityTestSettings.GetControllerPublicKey(SecondaryCtrl);
                ApplicationService.SecurityTestSettings.IncludeSettings.JoinDSK = senderPublicKey.Take(2).ToArray(); //by default
                var addToken = AsyncService.AddNode(PrimaryCtrl, Modes.NodeAny, 15000);
                var learnToken = AsyncService.SetLearnMode(SecondaryCtrl);
                var addRes = addToken.WaitCompletedSignal();
                var learnRes = learnToken.WaitCompletedSignal();
                AssertIsTrue(addRes);
                AssertIsTrue(learnRes);
                AssertIsTrue(addRes.SubstituteStatus == SubstituteStatuses.Done);
                AssertIsTrue(learnRes.SubstituteStatus == SubstituteStatuses.Done);

                Service.SendData(PrimaryCtrl, SecondaryCtrl, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(PrimaryCtrl, SecondaryCtrl, new COMMAND_CLASS_BASIC.BASIC_SET());
            }
            #endregion

            var ex = Service.RemoveNode(PrimaryCtrl, Slave, Modes.NodeAny, LearnModes.LearnModeClassic, 30000);
            AssertIsTrue(ex, "Excluded Slave");

            var exclusion = Service.RemoveNode(PrimaryCtrl, SecondaryCtrl, Modes.NodeAny, LearnModes.LearnModeClassic, 120000);
            AssertIsTrue(exclusion, "Excluded Secondary");

        }
    }
}