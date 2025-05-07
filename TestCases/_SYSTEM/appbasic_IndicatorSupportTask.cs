// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using System.Linq;
using Zats.Services;

namespace TestCmdPrompt
{
    [ZatsTestCase("Indicator Support task using S2 security")]
    [DeviceMapping(1, LibTypes.Controller)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    class appbasic_IndicatorSupportTask : ZatsCodeTestCase
    {
        Alias controller = new Alias(1);
        Alias zwaveEndDevice = new Alias(2);
        protected override void ExecuteTest()
        {
            Alias[] ALL = new Alias[] { zwaveEndDevice, controller };
            ParallelService.Reset(ALL);

            #region 1.- Configure the Slave as a multichannel switch.

            ApplicationService.AddSecurity(controller, SecurityVersions.S2);
            ApplicationService.AddSecurity(zwaveEndDevice, SecurityVersions.S2);

            var indicatorSupportData = new IndicatorSupportData();
            indicatorSupportData.AddIndicator((byte)IndicatorIdNames.NodeIdentify, 0x01, 0x00);
            indicatorSupportData.AddIndicator((byte)IndicatorIdNames.Busy, 0x01, 0x00);
            indicatorSupportData.AddIndicator((byte)IndicatorIdNames.Armed, 0x01, 0x00);
            indicatorSupportData.AddIndicator((byte)IndicatorIdNames.AlarmingMedical, 0x01, 0x00);

            var indicatorSupport = new IndicatorSupport(indicatorSupportData) { Version = 4, Secure = true };

            var responseTokens = ApplicationService.SetApplicationCmd(zwaveEndDevice, true, true, LibTypes.SwitchOnOff);

            ApplicationService.AddCommandClassSupport(zwaveEndDevice, 0, indicatorSupport);
            #endregion

            Service.IncludeNode(controller, zwaveEndDevice);

            var expectedIndicators = indicatorSupportData.Indicators.ToDictionary(x => x.Id, x => false);
            AssertIsTrue(VerifyAllSupportedInidcatorsAreReported(expectedIndicators),
                "All expected indicators were reported correctly",
                $"IndicatorId='{string.Join(";", expectedIndicators.Where(x => !x.Value).Select(x => x.Key))}' were not reported");


            foreach (var indicator in indicatorSupportData.Indicators)
            {
                var indicatorGetCmd = new COMMAND_CLASS_INDICATOR_V4.INDICATOR_GET();
                indicatorGetCmd.indicatorId = indicator.Id;

                var indicatorReportCmd = new COMMAND_CLASS_INDICATOR_V4.INDICATOR_REPORT();

                var indicatorRequest = Service.RequestData(controller, zwaveEndDevice, indicatorGetCmd, TransmitOptions.Ack, indicatorReportCmd, 10000);

                if (indicatorRequest)
                {
                    var requestDataResponse = (COMMAND_CLASS_INDICATOR_V4.INDICATOR_REPORT)indicatorRequest.Command;

                    AssertIsTrue(requestDataResponse.properties1.indicatorObjectCount > 0 &&
                        requestDataResponse.vg1[0].indicatorId == indicator.Id,
                    $"Correct Indicator Id:{indicator.Id} is recieved inside INDICATOR_REPORT encap frame", $"Requested Indicator Id:{indicator.Id} is not found inside INDICATOR_REPORT encap frame");
                }
            }
        }

        private bool VerifyAllSupportedInidcatorsAreReported(Dictionary<byte, bool> expectedIndicators)
        {
            var indicatorSupportedReportCmd = new COMMAND_CLASS_INDICATOR_V4.INDICATOR_SUPPORTED_REPORT();
            var indicatorSupportedGetCmd = new COMMAND_CLASS_INDICATOR_V4.INDICATOR_SUPPORTED_GET();
            byte nextIndicatorId = 0;

            while (expectedIndicators.Any(x => !x.Value))
            {
                indicatorSupportedGetCmd.indicatorId = nextIndicatorId;
                var indicatorSupportedRequest = Service.RequestData(controller, zwaveEndDevice, indicatorSupportedGetCmd, TransmitOptions.Ack, indicatorSupportedReportCmd, 10000);
                if (indicatorSupportedRequest)
                {
                    var requestDataResponse = (COMMAND_CLASS_INDICATOR_V4.INDICATOR_SUPPORTED_REPORT)indicatorSupportedRequest.Command;

                    byte currentIndicatorId = requestDataResponse.indicatorId;
                    if (expectedIndicators.ContainsKey(currentIndicatorId))
                    {
                        AssertIsTrue(!expectedIndicators[currentIndicatorId], $"IndicatorID ={currentIndicatorId} received.",
                            $"IndicatorID ={currentIndicatorId} received second time.");
                        expectedIndicators[currentIndicatorId] = true;
                    }
                    else
                    {
                        break;
                    }
                    nextIndicatorId = requestDataResponse.nextIndicatorId;
                    Delay(500, "Reply takes time");
                }
                else
                {
                    break;
                }
            }
            return expectedIndicators.All(x => x.Value);
        }
    }
}
