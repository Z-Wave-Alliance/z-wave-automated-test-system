// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetDefault
    -Add controller. 
    -Add slave by command")]
    [ZipDeviceMapping(1, "z1")]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    public class appzip_AddRemoveNodeSecure : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias ZipPrimary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias Slave = new Alias(3, "sdf");
            Alias[] ALL = new Alias[] { Secondary, Slave };

            ReportService.LogText("Start");
            ReportService.LogText(Slave);

            Service.ApplicationNodeInformation(Secondary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_CONTROLLER_REPLICATION.ID, COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID, 
                    COMMAND_CLASS_SECURITY.ID });
            Service.ApplicationNodeInformation(Slave, true, (byte)GenericTypes.SWITCH_MULTILEVEL, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID, 
                    COMMAND_CLASS_SECURITY.ID });

            ParallelService.SetDefault(ALL);
            ZipService.SetDefault(ZipPrimary);

            ApplicationService.AddSecurity(Secondary, SecurityVersions.S0);
            ApplicationService.AddSecurity(Slave, SecurityVersions.S0);
            
            ZipResult.AddNode addRes = ZipService.AddNode(ZipPrimary, Secondary);
            AssertIsTrue(addRes, "Controller added to the network NID:" + addRes.NodeId.ToString(), "Unable to add: Controller");

            byte[] NetKey = ApplicationService.GetSecurityNetworkKey(Secondary);
            ZnifferService.SetSecurityNetworkKey(NetKey);
            ZnifferToken.ExpectData zt = ZnifferService.ExpectData(61000);
            zt.SetUpWatch(new COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION.NODE_ADD_STATUS(), HeaderTypes.ALL_SINGLECAST, false);

            BasicToken.ExpectData expectToken = AsyncService.ExpectData(Secondary, new COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION.NODE_ADD_STATUS());
            ComplianceService.SendData(Secondary, 0x01, new COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION.NODE_ADD()
            {
                seqNo = 0x01,
                mode = (byte)Modes.NodeAny
            });
            
            BasicToken.SetLearnMode learnToken = AsyncService.SetLearnMode(Slave);
            BasicResult.SetLearnMode learnRes = learnToken.WaitCompletedSignal();

            ZnifferResult.ExpectData zr = zt.WaitCompletedSignal();
            BasicResult.ExpectData expectResult = expectToken.WaitCompletedSignal();
            AssertIsTrue(expectResult, "ADD_STATUS callback received", "ADD_STATUS callback missing");
            AssertIsTrue(zr, "ADD_STATUS received", "ADD_STATUS missing");
        }
    }
}