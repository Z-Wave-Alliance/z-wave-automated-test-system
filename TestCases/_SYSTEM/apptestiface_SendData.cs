// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Multilevel Switch Start/Stop Level Change. Levels decreasing")]
    [DeviceMapping(1, LibTypes.Controller)]
    [TestInterfaceDeviceMapping(2, LibTypes.PowerStrip)]
    public class apptestiface_SendData : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Controller = new Alias(1);
            Alias DUT = new Alias(2);

            Service.SetDefault(Controller);
            ApplicationService.AddSecurity(Controller, SecurityVersions.S2);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Controller);

            ReportService.LogTitle("1.Add the DUT to the Controller�s S2 network");

            AssertIsTrue(TestInterfaceService.ExcludeNode(Controller, DUT), "Successful exclusion of the DUT", "The DUT exclusion fails");
            var dutInclR = TestInterfaceService.IncludeNode(Controller, DUT).AddNodeResult;
            AssertIsTrue(dutInclR.SubstituteStatus == SubstituteStatuses.Done, "Successful S2 inclusion of the DUT", "The DUT S2 inclusion fails");

            ComplianceService.RequestNodeInfo(Controller, dutInclR.NodeId);

            ComplianceService.SendData(Controller, dutInclR.NodeId, new COMMAND_CLASS_VERSION_V3.VERSION_GET(), TransmitOptions.Ack);

            ReportService.LogTitle("2. Send Multilevel Switch Set command to the DUT setting the level to 100%");
            ComplianceService.SendData(Controller, dutInclR.NodeId, new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP()
            {
                commandClass = COMMAND_CLASS_SWITCH_MULTILEVEL_V4.ID,
                command = COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_SET.ID,
                parameter = new List<byte>() { 0x63, 0x00 },
                properties1 = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP.Tproperties1() { sourceEndPoint = 0x00 },
                properties2 = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP.Tproperties2() { destinationEndPoint = 0x02 }
            });

            var responseT = AsyncService.ExpectData(Controller, new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP());
            ComplianceService.SendData(Controller, dutInclR.NodeId, new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP()
            {
                commandClass = COMMAND_CLASS_SWITCH_MULTILEVEL_V4.ID,
                command = COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_GET.ID,
                properties1 = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP.Tproperties1() { sourceEndPoint = 0x00 },
                properties2 = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP.Tproperties2() { destinationEndPoint = 0x02 }
            });
            var responseR = responseT.WaitCompletedSignal();

            AssertIsTrue(responseR && 
                ((COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP)responseR.Command).commandClass==COMMAND_CLASS_SWITCH_MULTILEVEL_V4.ID &&
                ((COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP)responseR.Command).command==COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_REPORT.ID, "Multilevel Switch Report is received", "There is no Multilevel Switch Report");
            AssertIsTrue(((COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP)responseR.Command).parameter[0] == 0x63, "Multilevel Switch is set to 100%", "Multilevel Switch has a wrong value");

            ReportService.LogTitle("3. Send Multilevel Switch Start Level Change command to the DUT with Start_Level=0x00, Up/Down=1, Ignore_Start_Level=1 and Duration of 30 sec");
            ComplianceService.SendData(Controller, dutInclR.NodeId, new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP()
            {
                commandClass = COMMAND_CLASS_SWITCH_MULTILEVEL_V4.ID,
                command = COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_START_LEVEL_CHANGE.ID,
                parameter = new List<byte>() { 0xc0, 0x00, 0x1e },
                properties1 = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP.Tproperties1() { sourceEndPoint = 0x00 },
                properties2 = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP.Tproperties2() { destinationEndPoint = 0x02 }
            });

            /*
                new COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_START_LEVEL_CHANGE()
            {
                dimmingDuration = 30,
                startLevel = 0x63,
                properties1 = new COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_START_LEVEL_CHANGE.Tproperties1() { ignoreStartLevel = 0x01, upDown = 0x01 }
            });
            */
            ReportService.LogTitle("4. Delay 3 sec");
            Delay(3000);

            responseT = AsyncService.ExpectData(Controller, new COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_REPORT());
            ComplianceService.SendData(Controller, dutInclR.NodeId, new COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_GET());
            responseR = responseT.WaitCompletedSignal();

            AssertIsTrue(responseR, "Multilevel Switch Report is received", "There is no Multilevel Switch Report");
            AssertIsTrue(((COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_REPORT)responseR.Command).currentValue < 0x63 && ((COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_REPORT)responseR.Command).targetValue == 0x00, "Multilevel Switch value decreased", "Multilevel Switch has a wrong value");


            ReportService.LogTitle("5. Send Multilevel Switch Stop Level Change to the DUT");
            ComplianceService.SendData(Controller, dutInclR.NodeId, new COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_STOP_LEVEL_CHANGE());

            ReportService.LogTitle("6. Send Multilevel Switch Get to the DUT");
            responseT = AsyncService.ExpectData(Controller, new COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_REPORT());
            ComplianceService.SendData(Controller, dutInclR.NodeId, new COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_GET());
            responseR = responseT.WaitCompletedSignal();

            byte levelAtStop = ((COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_REPORT)responseR.Command).currentValue;

            AssertIsTrue(responseR, "Multilevel Switch Report is received", "There is no Multilevel Switch Report");
            AssertIsTrue(levelAtStop < 0x63 && ((COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_REPORT)responseR.Command).targetValue == levelAtStop, "Multilevel Switch value decreased", "Multilevel Switch has a wrong value");

            ReportService.LogTitle("7.Delay 3 sec");
            Delay(3000);

            ReportService.LogTitle("8. Send Multilevel Switch Get to the DUT");
            responseT = AsyncService.ExpectData(Controller, new COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_REPORT());
            ComplianceService.SendData(Controller, dutInclR.NodeId, new COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_GET());
            responseR = responseT.WaitCompletedSignal();


            AssertIsTrue(responseR, "Multilevel Switch Report is received", "There is no Multilevel Switch Report");
            AssertIsTrue(((COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_REPORT)responseR.Command).currentValue == levelAtStop && ((COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_REPORT)responseR.Command).targetValue == levelAtStop, "Multilevel Switch value decreased", "Multilevel Switch has a wrong value");

        }
    }
}