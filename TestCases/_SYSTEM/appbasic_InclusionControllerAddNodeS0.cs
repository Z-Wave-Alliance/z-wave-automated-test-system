// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Test of inclusion controller CC")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.ControllerStaticLib)]
    public class appbasic_InclusionControllerAddNodeS0 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias PrimarySISController = new Alias(1);
            Alias InclusionController = new Alias(2);
            Alias ControllerAsNode = new Alias(3);

            Alias[] ALL = new Alias[] { PrimarySISController, InclusionController, ControllerAsNode };

            #region S0
            ParallelService.Reset(ALL);

            ApplicationService.AddSecurity(PrimarySISController, SecurityVersions.S0);
            ApplicationService.AddSecurity(InclusionController, SecurityVersions.S0);
            ApplicationService.AddSecurity(ControllerAsNode, SecurityVersions.S0);

            Service.ApplicationNodeInformation(PrimarySISController, true, (byte)GenericTypes.STATIC_CONTROLLER,
                (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_SECURITY.ID });
            Service.ApplicationNodeInformation(InclusionController, true, (byte)GenericTypes.STATIC_CONTROLLER,
                (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_SECURITY.ID });
            Service.ApplicationNodeInformation(ControllerAsNode, true, (byte)GenericTypes.STATIC_CONTROLLER,
                (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_SECURITY.ID });

            Service.SetSUCNodeID(PrimarySISController, PrimarySISController);

            var includeRes = Service.IncludeNode(PrimarySISController, InclusionController);
            AssertIsTrue(includeRes);
            AssertIsTrue(SubstituteStatuses.Done == includeRes.AddNodeResult.SubstituteStatus);

            //Update SucNodeId in Network
            Service.GetSucNodeId(InclusionController);

            includeRes = Service.IncludeNode(InclusionController, ControllerAsNode);
            AssertIsTrue(includeRes);
            AssertIsTrue(includeRes.SetLearnMode.SubstituteStatus == SubstituteStatuses.Done);
            #endregion
        }
    }
}