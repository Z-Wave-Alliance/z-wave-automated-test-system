// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Zats.Services;

namespace TestCmdPrompt
{
    [ZatsTestCase("MultiChannel and Basic Support tasks using S2 security")]
    [DeviceMapping(1, LibTypes.EndDeviceLib)]
    [DeviceMapping(2, LibTypes.Controller)]
    class appbasic_MultichannelAndBasicSupportTasks : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias zwaveEndDevice = new Alias(1);
            Alias controller = new Alias(2);
            Alias[] ALL = new Alias[] { zwaveEndDevice, controller };

            ParallelService.Reset(ALL);

            #region 1.- Configure the Slave as a multichannel switch.

            ApplicationService.AddSecurity(controller, SecurityVersions.S2);
            ApplicationService.AddSecurity(zwaveEndDevice, SecurityVersions.S2);

            var responseTokens = ApplicationService.SetApplicationCmdWithExtraClasses(zwaveEndDevice, true, true, LibTypes.SwitchOnOff,
                new byte[] { COMMAND_CLASS_MULTI_CHANNEL_V4.ID, COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V4.ID });
            #endregion

            Service.IncludeNode(controller, zwaveEndDevice);

            var multCmdEncap = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP()
            {
                commandClass = COMMAND_CLASS_BASIC_V2.ID,
                command = COMMAND_CLASS_BASIC_V2.BASIC_GET.ID,
                properties1 = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP.Tproperties1
                {
                    sourceEndPoint = 0x01
                },
                properties2 = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP.Tproperties2
                {
                    destinationEndPoint = 0x02
                }
            };

            var multRequest = Service.RequestData(controller, zwaveEndDevice, multCmdEncap, TransmitOptions.Ack, multCmdEncap, 10000);

            if (multRequest)
            {
                var requestDataResponse = (COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP)multRequest.Command;
                AssertIsTrue(requestDataResponse.commandClass == COMMAND_CLASS_BASIC_V2.ID
                    && requestDataResponse.command == COMMAND_CLASS_BASIC_V2.BASIC_REPORT.ID,
                "BASIC_REPORT is recieved inside Multichannel CMD encap frame", "Could not receive BASIC_REPORT inside Multichannel CMD encap frame");
            }
        }
    }
}
