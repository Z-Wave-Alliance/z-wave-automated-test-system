// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"appbasic_NetworkStatistic")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [TestInterfaceDeviceMapping(2, LibTypes.SwitchOnOff)]
    public class appbasic_NetworkStatistic : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);
            Service.Reset(Primary);
            var mpRes1 = Service.GetMaxPayloadSize(Primary);
            var mpRes2 = Service.GetMaxLRPayloadSize(Primary);


            ApplicationService.AddSecurity(Primary, SecurityVersions.S2 | SecurityVersions.S0);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);
            Service.ClearTxTimers(Primary);
            Service.ClearNetworkStats(Primary);
            ApplicationService.Provisioning.Add(Primary, Slave);

            TestInterfaceService.ExcludeNode(Primary, Slave);
            Delay(1000);

            var addToken = AsyncService.ExpectSmartStartAdd(Primary, 60000);
            TestInterfaceService.SoftReset(Slave);
            var addRes = addToken.WaitCompletedSignal();
            AssertIsTrue(addRes);
            var txTimerRes = Service.GetTxTimer(Primary);
            var nwStatRes = Service.GetNetworkStats(Primary);

            Service.ClearTxTimers(Primary);
            Service.ClearNetworkStats(Primary);
            ComplianceService.SendData(Primary, addRes.AddNodeResult.Node, new COMMAND_CLASS_BASIC.BASIC_SET());
            txTimerRes = Service.GetTxTimer(Primary);
            nwStatRes = Service.GetNetworkStats(Primary);

        }
    }
}