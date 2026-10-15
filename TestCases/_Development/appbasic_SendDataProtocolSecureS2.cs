// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetDefault
    -Add controller. 
    -SendData
    -Multi Security Keys")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    public class appbasic_SendDataProtocolSecureS2 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            #region Aliases
            Alias Primary = new Alias(1, "Primary");
            Alias Slave = new Alias(2, "Slave");
            Alias[] ALL = new Alias[] { Primary, Slave };
            #endregion

            #region Reset
            ParallelService.Reset(ALL);
            #endregion

            #region Add Security support
            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);
            Service.ApplicationNodeInformation(Slave, true, (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED, new byte[]
            {
                COMMAND_CLASS_SECURITY.ID,
                COMMAND_CLASS_SECURITY_2.ID,
                COMMAND_CLASS_BASIC.ID
            });
            #endregion

            ReportService.LogText("Add Slave");
            var addRes = Service.IncludeNode(Primary, Slave);
            Assert.IsTrue(addRes);

            BasicResult.SendData sendRes;
            BasicResult.RequestData reqRes;
            BasicToken.ExpectData eToken = null;
            BasicResult.ExpectData eRes = null;
            var CMD = new COMMAND_CLASS_BASIC.BASIC_SET();
            var CMD_GET = new COMMAND_CLASS_BASIC.BASIC_GET();
            var CMD_RPT = new COMMAND_CLASS_BASIC.BASIC_REPORT();

            ReportService.LogTitle("Scheme DEFAULT: " + SecuritySchemes.S2_ACCESS);
            ReportService.LogText("SendData");
            eToken = AsyncService.ExpectData(Slave, CMD);
            sendRes = Service.SendData(Primary, Slave, CMD);
            eRes = eToken.WaitCompletedSignal();
            Inconclusive(eRes, "Data received");
            Inconclusive(SecuritySchemes.S2_ACCESS == eRes.SecurityScheme, "Received using Scheme: " + eRes.SecurityScheme);

            ReportService.LogText("RequestData");
            ApplicationService.AddResponseEx(Slave, CMD_RPT, CMD_GET);
            eToken = AsyncService.ExpectData(Slave, CMD_GET);
            reqRes = Service.RequestData(Primary, Slave, CMD_GET, CMD_RPT);
            eRes = eToken.WaitCompletedSignal();
            Inconclusive(eRes, "Data received");
            Inconclusive(SecuritySchemes.S2_ACCESS == eRes.SecurityScheme, "Received using Scheme: " + eRes.SecurityScheme);

            foreach (var schema in new[]
            {
                SecuritySchemes.NONE,
                SecuritySchemes.S2_ACCESS,
                SecuritySchemes.S2_AUTHENTICATED,
                SecuritySchemes.S2_UNAUTHENTICATED,
                SecuritySchemes.S0
            })
            {

                ReportService.LogTitle("Scheme: " + SecuritySchemes.NONE);
                ReportService.LogText("SendData");
                eToken = AsyncService.ExpectData(Slave, CMD);

                if (schema == SecuritySchemes.NONE)
                {
                    ApplicationService.SuspendSecurity(Primary);
                }
                else
                {
                    ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Primary, Slave, schema);
                }

                sendRes = Service.SendData(Primary, Slave, CMD);

                if (schema == SecuritySchemes.NONE)
                {
                    ApplicationService.ResumeSecurity(Primary);
                }

                eRes = eToken.WaitCompletedSignal();
                Inconclusive(eRes, "Data received");
                Inconclusive(schema == eRes.SecurityScheme, "Received using Scheme: " + eRes.SecurityScheme);
                ReportService.LogText("SendDataEx (Controller)");
                eToken = AsyncService.ExpectData(Slave, CMD);
                sendRes = Service.SendDataEx(Primary, Slave, CMD, schema);
                eRes = eToken.WaitCompletedSignal();
                Inconclusive(eRes, "Data received");
                Inconclusive(schema == eRes.SecurityScheme, "Received using Scheme: " + eRes.SecurityScheme);
                ReportService.LogText("SendDataEx (Slave)");
                eToken = AsyncService.ExpectData(Primary, CMD);
                sendRes = Service.SendDataEx(Slave, Primary, CMD, schema);
                eRes = eToken.WaitCompletedSignal();
                Inconclusive(eRes, "Data received");
                Inconclusive(schema == eRes.SecurityScheme, "Received using Scheme: " + eRes.SecurityScheme);
            }
        }
    }
}