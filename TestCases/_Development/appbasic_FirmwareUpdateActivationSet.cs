// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"FirmwareUpdate with Activation")]
    [DeviceMapping(1, LibTypes.ControllerBridgeLib)]
    [TestInterfaceDeviceMapping(2, LibTypes.SwitchOnOff)]

    public class appbasic_FirmwareUpdateActivationSet : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Controller = new Alias(1);
            Alias Slave = new Alias(2);

            Alias[] All = new Alias[] {Controller};

            bool is700 = false;

            ParallelService.SetDefault(All);

            ApplicationService.AddSecurity(Controller, SecurityVersions.S0 | SecurityVersions.S2);

            Service.ApplicationNodeInformation(Controller, true, (byte) GenericTypes.STATIC_CONTROLLER,
                (byte) SpecificTypes.PC_CONTROLLER,
                new byte[]
                {
                    COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_CONTROLLER_REPLICATION.ID,
                    COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID,
                    COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID, COMMAND_CLASS_BASIC.ID,
                    COMMAND_CLASS_FIRMWARE_UPDATE_MD.ID
                });

            TestInterfaceService.ExcludeNode(Controller, Slave);
            var incRes = TestInterfaceService.IncludeNode(Controller, Slave).AddNodeResult;
            AssertIsTrue(incRes.SubstituteStatus == SubstituteStatuses.Done, "Suuccessful inclusion", "Inclusion was not successful");
            Delay(5000);

            ///
            /// @"C:\work\DevKits\release29\Gecko.Release\Apps\SwitchOnOff\ZW_SwitchOnOff_7.11.0_29_ZGM130S_REGION_US_v255.gbl"
            /// @"C:\work\DevKits\6_81_05\build167\SwitchOnOff_slave_enhanced_232_OTA_ZW050x_TI\SwitchOnOff_slave_enhanced_232_OTA_ZW050x_US_TI.otz"
            ///
            
            var updateFilePath =
                @"C:\work\DevKits\6_81_05\build167\SwitchOnOff_slave_enhanced_232_OTA_ZW050x_TI\SwitchOnOff_slave_enhanced_232_OTA_ZW050x_US_TI.otz";

            var updateRes = ComplianceService.FirmwareUpdateV5(Controller, incRes.NodeId, updateFilePath, 0x01);

            if (is700)
            {
                AssertIsTrue(updateRes == 0x03, "Update succesfull");
            }
            else
            {
                AssertIsTrue(updateRes == 0xFF, "Update succesfull");
            }

            var activateRes = ComplianceService.ActivteFirmwareUpdate(Controller, incRes.NodeId, updateFilePath);

            string msg = "";
            if (activateRes == 0xFF)
            {
                msg = string.Format("Received status (0x{0}): Firmware update completed successfully", activateRes.ToString("X2"));
            }
            else if (activateRes == 0x00)
            {
                msg = string.Format("Received status (0x{0}): Invalid combination", activateRes.ToString("X2"));
            }
            else if (activateRes == 0x01)
            {
                msg = string.Format("Received status (0x{0}): Error activating the firmware", activateRes.ToString("X2"));
            }

            AssertIsTrue(activateRes == 0xFF); // Here assert on expected status of activation.
        }
    }
}