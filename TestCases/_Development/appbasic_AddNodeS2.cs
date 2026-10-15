// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Linq;
using System.Collections.Generic;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Security S2 Inclusion")]
    [DeviceMapping(1, ChipSeries.ZW050x, LibTypes.Controller)]
    [DeviceMapping(2, ChipSeries.ZW050x, LibTypes.Controller)]
    [DeviceMapping(3, ChipSeries.ZW050x, LibTypes.EndDeviceAny)]
    public class appbasic_AddNodeS2 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias Slave = new Alias(3);
            Alias[] ALL = new Alias[] { Primary, Secondary, Slave, };

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2 | SecurityVersions.S0);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S2 | SecurityVersions.S0);

            List<SecuritySchemes> grantSchemes = new List<SecuritySchemes>(new[]
            {
                SecuritySchemes.S0, SecuritySchemes.S2_ACCESS, SecuritySchemes.S2_AUTHENTICATED, SecuritySchemes.S2_UNAUTHENTICATED
            });

            List<SecuritySchemes> requestSchemes = new List<SecuritySchemes>(new[]
            {
                SecuritySchemes.S0, SecuritySchemes.S2_ACCESS, SecuritySchemes.S2_AUTHENTICATED, SecuritySchemes.S2_UNAUTHENTICATED
            });
           
            int schemesCount = grantSchemes.Count;
            for (int i = 0; i < schemesCount + 1; i++)
            {
                ParallelService.Reset(ALL);
                ReportService.LogTitle($"Include controller with schemes [{grantSchemes.Select(x => x.ToString()).DefaultIfEmpty().Aggregate((z, y) => z + "," + y)}] granted");
                ApplicationService.SecurityTestSettings.IncludeSettings.GrantedSchemes = grantSchemes.ToArray();
                var includeRes = Service.IncludeNode(Primary, Secondary);
                AssertIsTrue(includeRes);
                if (grantSchemes.Count == 0)
                {
                    AssertIsTrue(includeRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.None);
                    AssertIsTrue(!includeRes.AddNodeResult.SecuritySchemes.Any());
                    AssertIsTrue(includeRes.SetLearnMode.SubstituteStatus == SubstituteStatuses.None);
                    AssertIsTrue(!includeRes.SetLearnMode.SecuritySchemes.Any());
                }
                else
                {
                    AssertIsTrue(includeRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done);
                    AssertIsTrue(!includeRes.AddNodeResult.SecuritySchemes.Except(grantSchemes).Any());
                    AssertIsTrue(includeRes.SetLearnMode.SubstituteStatus == SubstituteStatuses.Done);
                    AssertIsTrue(!includeRes.SetLearnMode.SecuritySchemes.Except(grantSchemes).Any());
                }

                ReportService.LogTitle($"Include slave with schemes [{grantSchemes.Select(x => x.ToString()).DefaultIfEmpty().Aggregate((z, y) => z + "," + y)}] granted");
                ApplicationService.SecurityTestSettings.IncludeSettings.GrantedSchemes = grantSchemes.ToArray();
                includeRes = Service.IncludeNode(Primary, Slave);
                var keysRes = Service.GetSecurityKeys(Slave);

                AssertIsTrue(includeRes);
                if (grantSchemes.Count == 0)
                {
                    AssertIsTrue(includeRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.None);
                    AssertIsTrue(!includeRes.AddNodeResult.SecuritySchemes.Any());
                    AssertIsTrue(!keysRes.SecuritySchemes.Any());
                }
                else
                {
                    AssertIsTrue(includeRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done);
                    AssertIsTrue(!includeRes.AddNodeResult.SecuritySchemes.Except(grantSchemes).Any());
                    AssertIsTrue(!keysRes.SecuritySchemes.Except(grantSchemes).Any());
                }

                if (grantSchemes.Count > 0)
                {
                    grantSchemes.RemoveAt(0);
                }
            }

            schemesCount = requestSchemes.Count;
            for (int i = 0; i < schemesCount + 1; i++)
            {
                ParallelService.Reset(ALL);
                ReportService.LogTitle($"Include controller with schemes [{requestSchemes.Select(x => x.ToString()).DefaultIfEmpty().Aggregate((z, y) => z + "," + y)}] requested");
                ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary, SecurityS2TestFrames.KEXReport, new COMMAND_CLASS_SECURITY_2.KEX_REPORT
                {
                    properties1 = 0x00,
                    supportedEcdhProfiles = 0x01,
                    supportedKexSchemes = 0x02,
                    requestedKeys = SecurityTestSettings.GetNetworkKeysMask(requestSchemes)
                });
                var includeRes = Service.IncludeNode(Primary, Secondary);
                AssertIsTrue(includeRes);
                if (requestSchemes.Count == 0)
                {
                    AssertIsTrue(includeRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.None);
                    AssertIsTrue(!includeRes.AddNodeResult.SecuritySchemes.Any());
                    AssertIsTrue(includeRes.SetLearnMode.SubstituteStatus == SubstituteStatuses.None);
                    AssertIsTrue(!includeRes.SetLearnMode.SecuritySchemes.Any());
                }
                else
                {
                    AssertIsTrue(includeRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done);
                    AssertIsTrue(!includeRes.AddNodeResult.SecuritySchemes.Except(requestSchemes).Any());
                    AssertIsTrue(includeRes.SetLearnMode.SubstituteStatus == SubstituteStatuses.Done);
                    AssertIsTrue(!includeRes.SetLearnMode.SecuritySchemes.Except(requestSchemes).Any());
                }

                ReportService.LogTitle($"Include slave with schemes [{requestSchemes.Select(x => x.ToString()).DefaultIfEmpty().Aggregate((z, y) => z + "," + y)}] requested");
                Service.SetSecurityInclusionRequestedKeys(Slave, requestSchemes.ToArray());
                includeRes = Service.IncludeNode(Primary, Slave);
                var keysRes = Service.GetSecurityKeys(Slave);

                AssertIsTrue(includeRes);

                if (requestSchemes.Count == 0)
                {
                    //Inconclusive(includeRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.None,
                    //    "Serial API call CMD_SET_SECURITY_INCLUSION_REQUESTED_KEYS doesn't set requested keys to empty array!");
                    //Inconclusive(!includeRes.AddNodeResult.SecuritySchemes.Any(),
                    //    "Serial API call CMD_SET_SECURITY_INCLUSION_REQUESTED_KEYS doesn't set requested keys to empty array!");
                    //Inconclusive(!keysRes.SecuritySchemes.Any(),
                    //    "Serial API call CMD_SET_SECURITY_INCLUSION_REQUESTED_KEYS doesn't set requested keys to empty array!");
                }
                else
                {
                    AssertIsTrue(includeRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done);
                    AssertIsTrue(!includeRes.AddNodeResult.SecuritySchemes.Except(requestSchemes).Any());
                    AssertIsTrue(!keysRes.SecuritySchemes.Except(requestSchemes).Any());
                }

                if (requestSchemes.Count > 0)
                {
                    requestSchemes.RemoveAt(0);
                }
            }
        }
    }
}