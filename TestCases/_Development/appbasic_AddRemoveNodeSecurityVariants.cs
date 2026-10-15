// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Security 2 Controller Side Inclusion Workflow Test case")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    [DeviceMapping(3, LibTypes.ControllerStaticLib)]
    public class appbasic_AddRemoveNodeSecurityVariants : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias PrimaryCtrl = new Alias(1);
            Alias Slave = new Alias(2);
            Alias SecondaryCtrl = new Alias(3);
            Alias[] ALL = new Alias[] { PrimaryCtrl, Slave, SecondaryCtrl };

            ParallelService.Reset(ALL);

            Service.ApplicationNodeInformation(Slave, true, (byte)GenericTypes.SWITCH_MULTILEVEL, (byte)SpecificTypes.NOT_USED, COMMAND_CLASS_SECURITY_2.ID);
            Service.ApplicationNodeInformation(SecondaryCtrl, true, (byte)GenericTypes.STATIC_CONTROLLER, 
                (byte)SpecificTypes.PC_CONTROLLER, COMMAND_CLASS_SECURITY_2.ID);
            
            ApplicationService.AddOrModifySecurity(PrimaryCtrl, SecurityVersions.S2 | SecurityVersions.S0, true, new byte[] {
                COMMAND_CLASS_SECURITY_2.ID, COMMAND_CLASS_SECURITY.ID});
            ApplicationService.AddOrModifySecurity(SecondaryCtrl, SecurityVersions.S2 | SecurityVersions.S0, true, new byte[] {
                COMMAND_CLASS_SECURITY_2.ID, COMMAND_CLASS_SECURITY.ID});

            #region Slave S2 Secondary S2
            if (true)
            {
                var dsk = Service.GetDsk(Slave, 2);
                ApplicationService.SecurityTestSettings.IncludeSettings.JoinDSK = dsk;
                var addToken = AsyncService.AddNode(PrimaryCtrl, Modes.NodeAny, 15000);
                var learnToken = AsyncService.SetLearnMode(Slave);
                var addRes = addToken.WaitCompletedSignal();
                var learnRes = learnToken.WaitCompletedSignal();
                AssertIsTrue(addRes);
                AssertIsTrue(learnRes);
                AssertIsTrue(addRes.SubstituteStatus == SubstituteStatuses.Done);
                
                Service.SendData(PrimaryCtrl, Slave, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(PrimaryCtrl, Slave, new COMMAND_CLASS_BASIC.BASIC_SET());

                dsk = Service.GetDsk(SecondaryCtrl, 2);
                ApplicationService.SecurityTestSettings.IncludeSettings.JoinDSK = dsk;
                addToken = AsyncService.AddNode(PrimaryCtrl, Modes.NodeAny, 15000);
                learnToken = AsyncService.SetLearnMode(SecondaryCtrl);
                addRes = addToken.WaitCompletedSignal();
                learnRes = learnToken.WaitCompletedSignal();
                AssertIsTrue(addRes);
                AssertIsTrue(learnRes);
                AssertIsTrue(addRes.SubstituteStatus == SubstituteStatuses.Done);
                AssertIsTrue(learnRes.SubstituteStatus == SubstituteStatuses.Done);

                Service.SendData(PrimaryCtrl, SecondaryCtrl, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(PrimaryCtrl, SecondaryCtrl, new COMMAND_CLASS_BASIC.BASIC_SET());

            }
            #endregion

            #region Slave S2 Secondary S0
            if (true)
            {
                Service.ApplicationNodeInformation(SecondaryCtrl, true, (byte)GenericTypes.STATIC_CONTROLLER, 
                    (byte)SpecificTypes.PC_CONTROLLER, COMMAND_CLASS_SECURITY.ID);
                ApplicationService.AddOrModifySecurity(SecondaryCtrl, SecurityVersions.S0, true, new byte[] {
                 COMMAND_CLASS_SECURITY.ID});

                ParallelService.Reset(ALL);

                var addToken = AsyncService.AddNode(PrimaryCtrl, Modes.NodeAny, 15000);
                var learnToken = AsyncService.SetLearnMode(Slave);
                var addRes = addToken.WaitCompletedSignal();
                var learnRes = learnToken.WaitCompletedSignal();
                AssertIsTrue(addRes);
                AssertIsTrue(learnRes);
                AssertIsTrue(addRes.SubstituteStatus == SubstituteStatuses.Done);

                Service.SendData(PrimaryCtrl, Slave, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(PrimaryCtrl, Slave, new COMMAND_CLASS_BASIC.BASIC_SET());

                addToken = AsyncService.AddNode(PrimaryCtrl, Modes.NodeAny, 15000);
                learnToken = AsyncService.SetLearnMode(SecondaryCtrl);
                addRes = addToken.WaitCompletedSignal();
                learnRes = learnToken.WaitCompletedSignal();
                AssertIsTrue(addRes);
                AssertIsTrue(learnRes);
                AssertIsTrue(addRes.SubstituteStatus == SubstituteStatuses.Done);
                AssertIsTrue(learnRes.SubstituteStatus == SubstituteStatuses.Done);

                Service.SendData(PrimaryCtrl, SecondaryCtrl, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(PrimaryCtrl, SecondaryCtrl, new COMMAND_CLASS_BASIC.BASIC_SET());

            }
            #endregion
        }
    }
}