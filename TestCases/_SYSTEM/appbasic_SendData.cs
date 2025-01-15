// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetDefault
    -Add controller. 
    -Add slave
    -SendData")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class appbasic_SendData : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);

            ReportService.LogText("Start");
            ParallelService.SetDefault(Primary, Slave);

            var cmdClasses = new byte[] { COMMAND_CLASS_ALARM.ID, COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID };
            Service.ApplicationNodeInformationCmdClasses(Slave, cmdClasses, cmdClasses, cmdClasses);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);

            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, "01010101010101010101010101010101".ToByteArray(), SecuritySchemes.S0);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, "C0000000000000000000000000000000".ToByteArray(), SecuritySchemes.S2_UNAUTHENTICATED);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, "C1000000000000000000000000000000".ToByteArray(), SecuritySchemes.S2_AUTHENTICATED);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, "C2000000000000000000000000000000".ToByteArray(), SecuritySchemes.S2_ACCESS);

            var schemes = new[]
            {
                new SecuritySchemes[0],
                new[] { SecuritySchemes.S0},
                new[] { SecuritySchemes.S0, SecuritySchemes.S2_ACCESS},
                new[] { SecuritySchemes.S0, SecuritySchemes.S2_ACCESS, SecuritySchemes.S2_AUTHENTICATED},
                new[] { SecuritySchemes.S0, SecuritySchemes.S2_ACCESS, SecuritySchemes.S2_AUTHENTICATED, SecuritySchemes.S2_UNAUTHENTICATED},
                new[] { SecuritySchemes.S0, SecuritySchemes.S2_ACCESS, SecuritySchemes.S2_UNAUTHENTICATED},
                new[] { SecuritySchemes.S0, SecuritySchemes.S2_AUTHENTICATED},
                new[] { SecuritySchemes.S0, SecuritySchemes.S2_AUTHENTICATED, SecuritySchemes.S2_UNAUTHENTICATED},
                new[] { SecuritySchemes.S0, SecuritySchemes.S2_UNAUTHENTICATED},
                new[] { SecuritySchemes.S2_ACCESS},
                new[] { SecuritySchemes.S2_ACCESS, SecuritySchemes.S2_AUTHENTICATED},
                new[] { SecuritySchemes.S2_ACCESS, SecuritySchemes.S2_AUTHENTICATED, SecuritySchemes.S2_UNAUTHENTICATED},
                new[] { SecuritySchemes.S2_ACCESS, SecuritySchemes.S2_UNAUTHENTICATED},
                new[] { SecuritySchemes.S2_AUTHENTICATED},
                new[] { SecuritySchemes.S2_AUTHENTICATED, SecuritySchemes.S2_UNAUTHENTICATED},
                new[] { SecuritySchemes.S2_UNAUTHENTICATED},
            };

            foreach (var item in schemes)
            {
                Service.SetSecurityInclusionRequestedKeys(Slave, item);
                var addRes = Service.IncludeNode(Primary, Slave);
                var actualSchemes = addRes.AddNodeResult.SecuritySchemes ?? new SecuritySchemes[0];
                Inconclusive(actualSchemes.Intersect(item).Count() == actualSchemes.Length,
                    $"Setup to ask schemes: {item.DefaultIfEmpty().Select(x => x.ToString()).Aggregate((a, b) => a + ',' + b)}, granted: {actualSchemes.DefaultIfEmpty().Select(x => x.ToString()).Aggregate((a, b) => a + ',' + b)}");
                var remRes = Service.RemoveNode(Primary, Slave);
                if (!remRes)
                {
                    Service.SoftReset(Slave);
                    remRes = Service.RemoveNode(Primary, Slave);
                }
            }
        }
    }
}