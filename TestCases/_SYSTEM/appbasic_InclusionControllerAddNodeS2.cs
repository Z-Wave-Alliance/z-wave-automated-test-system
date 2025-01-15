// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Test of inclusion controller CC")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.ControllerStaticLib)]
    public class appbasic_InclusionControllerAddNodeS2 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias PrimarySISController = new Alias(1);
            Alias InclusionController = new Alias(2);
            Alias ControllerAsNode = new Alias(3);

            Alias[] ALL = new Alias[] { PrimarySISController, InclusionController, ControllerAsNode };

            #region S2
            ParallelService.Reset(ALL);

            ApplicationService.AddSecurity(PrimarySISController, SecurityVersions.S2);
            ApplicationService.AddSecurity(InclusionController, SecurityVersions.S2);
            ApplicationService.AddSecurity(ControllerAsNode, SecurityVersions.S2);
            
            Service.ApplicationNodeInformation(PrimarySISController, true, (byte)GenericTypes.STATIC_CONTROLLER,
                (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_SECURITY_2.ID });
            Service.ApplicationNodeInformation(InclusionController, true, (byte)GenericTypes.STATIC_CONTROLLER,
                (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_SECURITY_2.ID });
            Service.ApplicationNodeInformation(ControllerAsNode, true, (byte)GenericTypes.STATIC_CONTROLLER,
                (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_SECURITY_2.ID });

            Service.SetSUCNodeID(PrimarySISController, PrimarySISController);

            var includeRes = Service.IncludeNode(PrimarySISController, InclusionController);
            AssertIsTrue(includeRes);
            AssertIsTrue(SubstituteStatuses.Done == includeRes.AddNodeResult.SubstituteStatus);

            includeRes = Service.IncludeNode(InclusionController, ControllerAsNode);
            AssertIsTrue(includeRes);
            AssertIsTrue(includeRes.SetLearnMode.SubstituteStatus == SubstituteStatuses.Done);
            #endregion

        }

        //protected override void ExecuteTest()
        //{
        //    Alias SIS = new Alias(1);
        //    Alias Inclusion = new Alias(2);
        //    Alias Slave = new Alias(3);

        //    ParallelService.Reset(SIS, Inclusion, Slave);
        //    ApplicationService.AddSecurity(SIS, SecurityVersions.S0 | SecurityVersions.S2);
        //    ApplicationService.AddSecurity(Inclusion, SecurityVersions.S0 | SecurityVersions.S2);
        //    ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(SIS);

        //    Service.SetSUCNodeID(SIS, SIS);
        //    var inclNode = Service.IncludeNode(SIS, Inclusion);

        //    byte dutNodeId = 1;
        //    var zatsSlaveNodeId = 3;

        //    var ztInitiate = ZnifferService.ExpectData(60000).SetUpWatch(inclNode.AddNodeResult.Node, dutNodeId, new COMMAND_CLASS_INCLUSION_CONTROLLER.INITIATE());
        //    var ztComplete = ZnifferService.ExpectData(60000).SetUpWatch(dutNodeId, inclNode.AddNodeResult.Node, new COMMAND_CLASS_INCLUSION_CONTROLLER.COMPLETE());

        //    Service.IncludeNode(Inclusion, Slave);

        //    var zrInitiate = ztInitiate.WaitCompletedSignal();
        //    var zrComplete = ztComplete.WaitCompletedSignal();
        //    if (zrInitiate && zrInitiate.Frames.Count > 0)
        //    {
        //        var initiateCmd = (COMMAND_CLASS_INCLUSION_CONTROLLER.INITIATE)zrInitiate.Frames[0].OriginalPayload;

        //        Inconclusive(initiateCmd.nodeId.Value == zatsSlaveNodeId,
        //            "- Inclusion Controller Initiate Command contains Node ID = " + zatsSlaveNodeId + ".",
        //            "- Inclusion Controller Initiate Command did NOT contain Node ID = " + zatsSlaveNodeId + "!");

        //        Inconclusive(initiateCmd.stepId.Value == 0x01, // Step ID: 0x01 = Proxy Inclusion
        //            "- Inclusion Controller Initiate Command contains Step ID = 0x01 (Proxy Inclusion).",
        //            "- Inclusion Controller Initiate Command did NOT contain Step ID = 0x01 (Proxy Inclusion)!");
        //    }

        //    if (zrComplete && zrComplete.Frames.Count > 0)
        //    {
        //        var completeCmd = (COMMAND_CLASS_INCLUSION_CONTROLLER.COMPLETE)zrComplete.Frames[0].OriginalPayload;

        //        Inconclusive(completeCmd.stepId.Value == 0x01, // Step ID: 0x01 = Proxy Inclusion
        //            "- Inclusion Controller Complete Command contains Step ID = 0x01 (Proxy Inclusion).",
        //            "- Inclusion Controller Complete Command did NOT contain Step ID = 0x01 (Proxy Inclusion)!");

        //        Inconclusive(completeCmd.status.Value == 0x01, // Status: 0x01 = Step OK
        //            "- Inclusion Controller Complete Command contains Status = 0x01 (Step OK).",
        //            "- Inclusion Controller Complete Command did NOT contain Status = 0x01 (Step OK)!");
        //    }

        //}
    }
}