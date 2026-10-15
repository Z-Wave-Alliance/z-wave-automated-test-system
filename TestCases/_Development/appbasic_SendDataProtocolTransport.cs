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
    public class appbasic_SendDataProtocolTransport : ZatsCodeTestCase
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
            #endregion

            ReportService.LogText("Add Slave");
            var addRes = Service.AddNodeWithDSK(Primary, Slave, new byte[] { 0xCF, 0x07 });
            Assert.IsTrue(addRes);

            ApplicationService.AddTransportService(Primary, 30);
            BasicResult.SendData sendRes;
            BasicToken.ExpectData eToken = null;
            BasicResult.ExpectData eRes = null;
            var CMD = new byte[50];
            for (int i = 0; i < CMD.Length; i++)
            {
                CMD[i] = (byte)i;
            }
            CMD[0] = COMMAND_CLASS_BASIC.ID;
            CMD[1] = COMMAND_CLASS_BASIC.BASIC_REPORT.ID;

            ReportService.LogTitle("Scheme: " + SecuritySchemes.NONE);
        
            eToken = AsyncService.ExpectData(Primary, CMD);
            sendRes = Service.SendDataEx(Slave, Primary, CMD, TX_OPTIONS, TX_SEC_OPTIONS, SecuritySchemes.NONE, TransmitOptions2.TRANSMIT_OPTION_2_TRANSPORT_SERVICE);
            eRes = eToken.WaitCompletedSignal();
            Inconclusive(eRes, "Data received");
            Inconclusive(SecuritySchemes.NONE == eRes.SecurityScheme, "Received using Scheme: " + eRes.SecurityScheme);

            ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Primary, Slave, SecuritySchemes.S2_UNAUTHENTICATED);
            eToken = AsyncService.ExpectData(Slave, CMD);
            sendRes = Service.SendData(Primary, Slave, CMD);
            eRes = eToken.WaitCompletedSignal();
            Inconclusive(eRes, "Data received");
            Inconclusive(SecuritySchemes.S2_UNAUTHENTICATED == eRes.SecurityScheme, "Received using Scheme: " + eRes.SecurityScheme);

       }
    }
}