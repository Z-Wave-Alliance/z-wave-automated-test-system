// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetDefault
    -Add controller. 
    -Add slave
    -SendData
    -Stress test with reports")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, "CSL2", LibTypes.EndDeviceLib)]
    public class stressbasic_Diagnose_Module : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);

            Alias[] ALL = new Alias[] { Primary, Secondary };

            ReportService.LogText("Start");

            ParallelService.Reset(ALL);

            int addNodeFailures = 0;
            int removeNodeFailures = 0;
            int setDefaultFailures = 0;

            var libres = Service.TypeLibrary(Secondary);
            ReportService.LogTitle("DUT library: " + libres.Library);

            ReportService.LogTitle("1. Added/Reset");
            for (int i = 0; i < 5; i++)
            {
                var addRes = Service.AddNode(Primary, Secondary);
                if (!addRes)
                    addNodeFailures++;
                Service.SetDefault(Primary);
                Service.Reset(Secondary);
            }

            ReportService.LogTitle("2. Added/Removed/SetDefault");
            for (int i = 0; i < 5; i++)
            {
                var addRes = Service.AddNode(Primary, Secondary);
                if (!addRes)
                    addNodeFailures++;
                var removeRes = Service.RemoveNode(Primary, Secondary);
                if (!removeRes)
                    removeNodeFailures++;
                Service.SetDefault(Primary);
                var setDefaultRes = Service.SetDefault(Secondary);
                if (!setDefaultRes)
                    setDefaultFailures++;
            }

            if (libres.Library == LibTypes.ControllerPortableLib || libres.Library == LibTypes.ControllerBridgeLib || libres.Library == LibTypes.ControllerStaticLib)
            {
                ReportService.LogTitle("3. Adding/Reset (Controllers only)");
                for (int i = 0; i < 5; i++)
                {
                    var addRes = Service.AddNode(Secondary, Primary);
                    if (!addRes)
                        addNodeFailures++;
                    Service.SetDefault(Primary);
                    Service.Reset(Secondary);
                }

                ReportService.LogTitle("4. Adding/Removing/SetDefault (Controllers only)");
                for (int i = 0; i < 5; i++)
                {
                    var addRes = Service.AddNode(Secondary, Primary);
                    if (!addRes)
                        addNodeFailures++;
                    var removeRes = Service.RemoveNode(Secondary, Primary);
                    if (!removeRes)
                        removeNodeFailures++;
                    Service.SetDefault(Primary);
                    var setDefaultRes = Service.SetDefault(Secondary);
                    if (!setDefaultRes)
                        setDefaultFailures++;
                }
            }

            Service.AddNode(Primary, Secondary);

            if (libres.Library == LibTypes.ControllerPortableLib || libres.Library == LibTypes.EndDeviceLib || libres.Library == LibTypes.EndDeviceLegacyRoutingLib)
            {
                ReportService.LogTitle("5. Sleep Timer");
                for (int i = 0; i < 5; i++)
                {
                    Service.SetWutTimeout(Secondary, 1);
                    Service.SetSleepMode(Secondary, SleepModes.WUT);
                    Delay(1100);
                    Service.Reconnect(Secondary);
                    Service.SendBroadCastNodeInfo(Secondary, TX_OPTIONS);
                }
            }

            ReportService.LogTitle("6. Stress Test");


            byte[] requestCmd = new byte[30];
            new Random().NextBytes(requestCmd);
            requestCmd[0] = COMMAND_CLASS_BASIC.ID;
            requestCmd[1] = COMMAND_CLASS_BASIC.BASIC_GET.ID;

            byte[] responseCmd = new byte[30];
            new Random().NextBytes(responseCmd);
            responseCmd[0] = COMMAND_CLASS_BASIC.ID;
            responseCmd[1] = COMMAND_CLASS_BASIC.BASIC_REPORT.ID;

            ApplicationService.AddResponse(Secondary, responseCmd, requestCmd);

            var idPrimary = Service.MemoryGetId(Primary);
            var idSecondary = Service.MemoryGetId(Secondary);

            var zTokenGet = ZnifferService.CollectData().SetUpWatch(idPrimary.NodeId, idSecondary.NodeId, idPrimary.HomeId, new COMMAND_CLASS_BASIC.BASIC_GET(), HeaderTypes.SINGLECAST, null, false);
            var zTokenGetAck = ZnifferService.CollectData().SetUpAckWatch(idSecondary.NodeId, idPrimary.NodeId, idPrimary.HomeId, null);
            var zTokenReport = ZnifferService.CollectData().SetUpWatch(idSecondary.NodeId, idPrimary.NodeId, idPrimary.HomeId, new COMMAND_CLASS_BASIC.BASIC_REPORT(), HeaderTypes.SINGLECAST, null, false);
            var zTokenReportAck = ZnifferService.CollectData().SetUpAckWatch(idPrimary.NodeId, idSecondary.NodeId, idPrimary.HomeId, null);

            ApplicationService.StartTrafficToNodeIdTest(Primary, 0x02, requestCmd, responseCmd, 100, 10000);
            Delay(20000);
            BasicResult.Test resTotal = ApplicationService.StopTrafficTest(Primary, 0x02);

            var zResultGet = zTokenGet.WaitCompletedSignal();
            var zResultGetAck = zTokenGetAck.WaitCompletedSignal();
            var zResultReport = zTokenReport.WaitCompletedSignal();
            var zResultReportAck = zTokenReportAck.WaitCompletedSignal();

            ReportService.LogText(string.Format("Total: {0}({1}), {2}%{3}Zniffer GET:{4}, GET.ACK: {5}, REPORT: {6}, REPORT.ACK: {7}",
                resTotal.Total, resTotal.Fails, resTotal.SuccessPercentRate, Environment.NewLine,
                zResultGet.Frames.Count, zResultGetAck.Frames.Count, zResultReport.Frames.Count, zResultReportAck.Frames.Count));

            Inconclusive(
               zResultGet.Frames.Count == zResultGetAck.Frames.Count &&
               zResultGet.Frames.Count == zResultReport.Frames.Count &&
               zResultGet.Frames.Count == zResultReportAck.Frames.Count, "number of requests equals number of reports and it's acknowledges", "number of requests NOT equals number of reports and it's acknowledges");

            Inconclusive(addNodeFailures == 0, "addNodeFailures: " + addNodeFailures);
            Inconclusive(removeNodeFailures == 0, "removeNodeFailures: " + removeNodeFailures);
            Inconclusive(setDefaultFailures == 0, "setDefaultFailures: " + setDefaultFailures);
        }
    }
}