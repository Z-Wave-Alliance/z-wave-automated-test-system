// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetDefault
    -Add controller. 
    -Add slave
    -SendData
    -Stress test")]
    [DeviceMapping(1, LibTypes.Controller)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    public class stressbasic_SendData : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias EndDevice = new Alias(2);

            ReportService.LogText("Start");

            Service.ApplicationNodeInformation(Primary, true, (byte)GenericTypes.STATIC_CONTROLLER, SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY_2.ID });
            Service.ApplicationNodeInformation(EndDevice, true, (byte)GenericTypes.SWITCH_BINARY, SpecificTypes.NOT_USED, new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY_2.ID });
            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);
            ApplicationService.AddBasicEx(Primary);
            ApplicationService.AddBasicEx(EndDevice);
            Service.SetDefault(Primary);

            Service.IncludeNode(Primary, EndDevice);

            //ReportService.LogTitle("StartTrafficToNodeIdTest SendData from Controller to EndDevice");
            //ApplicationService.StartTrafficToNodeIdTest(Primary, 0x02, new COMMAND_CLASS_BASIC.BASIC_SET(), 100);
            //for (int i = 0; i < 10; i++)
            //{
            //    Delay(1000);
            //    BasicResult.Test res = ApplicationService.GetTrafficTestResult(Primary, 0x02);
            //    ReportService.LogTitle(string.Format("after 6 sec: {0}({1}), {2}%", res.Total, res.Fails, res.SuccessPercentRate));
            //}
            //BasicResult.Test resTotal = ApplicationService.StopTrafficTest(Primary, 0x02);
            //ReportService.LogTitle(string.Format("Total: {0}({1}), {2}%", resTotal.Total, resTotal.Fails, resTotal.SuccessPercentRate));

            //ReportService.LogTitle("StartTrafficToNodeIdTest RequestData from Controller to EndDevice");
            //ApplicationService.StartTrafficToNodeIdTest(Primary, 0x02, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT(), 100, 100);
            //for (int i = 0; i < 10; i++)
            //{
            //    Delay(1000);
            //    BasicResult.Test res = ApplicationService.GetTrafficTestResult(Primary, 0x02);
            //    ReportService.LogTitle(string.Format("after 6 sec: {0}({1}), {2}%", res.Total, res.Fails, res.SuccessPercentRate));
            //}
            //resTotal = ApplicationService.StopTrafficTest(Primary, 0x02);
            //ReportService.LogTitle(string.Format("Total: {0}({1}), {2}%", resTotal.Total, resTotal.Fails, resTotal.SuccessPercentRate));

            ReportService.LogTitle("StartTrafficToNodeIdTest SendData from EndDevice to Controller");
            ApplicationService.StartTrafficFromEndDeviceToNodeIdTest(EndDevice, 0x01, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT(), 100, 100, SecuritySchemes.S2_ACCESS);
            for (int i = 0; i < 10; i++)
            {
                Delay(1000);
                BasicResult.Test res = ApplicationService.GetTrafficTestResult(EndDevice, 0x01);
                ReportService.LogTitle(string.Format("after 6 sec: {0}({1}), {2}%", res.Total, res.Fails, res.SuccessPercentRate));
            }
            var resTotal = ApplicationService.StopTrafficTest(EndDevice, 0x01);
            ReportService.LogTitle(string.Format("Total: {0}({1}), {2}%", resTotal.Total, resTotal.Fails, resTotal.SuccessPercentRate));

            //ReportService.LogTitle("NoiseData from EndDevice to Controller (with expected report)");
            //var noiseToken = AsyncService.NoiseData(EndDevice, 1, new COMMAND_CLASS_BASIC.BASIC_GET(), TX_OPTIONS, new COMMAND_CLASS_BASIC.BASIC_REPORT(), x =>
            // {

            // }, 100, 100);

            //Delay(10000);

            //AsyncService.CancelToken(EndDevice, noiseToken);
            //var noiseRes = noiseToken.WaitCompletedSignal();
        }
    }
}