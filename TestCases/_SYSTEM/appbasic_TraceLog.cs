// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Check Trace Log of the SerialAPI call")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    public class appbasic_TraceLog : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);
            ReportService.LogText("Start");
            var res = Service.SendBroadCastNodeInfo(Primary, TX_OPTIONS);
            AssertIsTrue(res.TraceLog.Count == 3);
            AssertIsTrue(res.TraceLog[0].IsOutcome);
            AssertIsTrue(!res.TraceLog[1].IsOutcome);
            AssertIsTrue(!res.TraceLog[2].IsOutcome);
            Service.SetDefault(Primary);
            Service.GetPriorityRoute(Primary, Primary);
            
            var token = ZnifferService.ExpectData(2000);
            token.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_GET());
            ComplianceService.SendData(Primary, 0xFF, new COMMAND_CLASS_BASIC.BASIC_GET());
            AssertIsTrue(token.WaitCompletedSignal());

            AssertIsTrue(Service.AddNode(Primary, Slave, Modes.NodeAny, LearnModes.LearnModeClassic, 500),"ff","Fall");
        }
    }
}