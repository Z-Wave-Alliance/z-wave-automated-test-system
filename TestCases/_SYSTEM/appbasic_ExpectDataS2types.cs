// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetDefault
    -Add slave
    -SendData")]
    [DeviceMapping(1, LibTypes.Controller)]
    [TestInterfaceDeviceMapping(2, LibTypes.SwitchOnOff)]
    public class appbasic_ExpectDataS2types : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);


            ReportService.LogText("Start");
            ParallelService.Reset(Primary);
            ApplicationService.AddSupervision(Primary);
            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);


            TestInterfaceService.ExcludeNode(Primary, Slave);
            ReportService.LogText("Include DUT");
            var addRes = TestInterfaceService.IncludeNode(Primary, Slave);
            var DutId = addRes.AddNodeResult.Node;

            ReportService.LogText("optinal Clear MPAN");
            ApplicationService.SecurityTestSettings.ClearMPANTable(Primary);

            ReportService.LogText("Setup expecations and send data");
            var lt = AsyncService.ListenData(Primary);
            var ExpRes1 = AsyncService.ExpectData(Primary, DutId, null, new[] { SecurityS2ExtensionTypes.Mos }, 5000);
            var ExpRes2 = AsyncService.ExpectData(Primary, DutId, new COMMAND_CLASS_SUPERVISION_V2.SUPERVISION_REPORT(), new[] { SecurityS2ExtensionTypes.Mos }, 5000);
            var ExpRes3 = AsyncService.ExpectData(Primary, DutId, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), new[] { SecurityS2ExtensionTypes.Mos }, 5000);
            var ExpRes4 = AsyncService.ExpectData(Primary, DutId, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), 15000);
            var ExpRes5 = AsyncService.ExpectData(Primary, DutId, new COMMAND_CLASS_SUPERVISION_V2.SUPERVISION_REPORT(), 5000);
            var ExpRes6 = AsyncService.ExpectData(Primary, DutId, null, 5000);
            var ExpRes7 = AsyncService.ExpectData(Primary, DutId, null, new SecurityS2ExtensionTypes[0], 5000);
            var ExpRes8 = AsyncService.ExpectData(Primary, DutId, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), new SecurityS2ExtensionTypes[0], 5000);

            var MSendRes = ComplianceService.SendDataMulti(Primary, new Node[] { DutId }, new COMMAND_CLASS_BASIC.BASIC_SET(), TransmitOptions.Ack);
            Inconclusive(MSendRes.TransmitStatus == TransmitStatuses.CompleteOk, "SendDataMulti completed");
            var SDres = ComplianceService.SendData(Primary, DutId, new COMMAND_CLASS_VERSION.VERSION_GET(), TransmitOptions.Ack);
            Inconclusive(SDres.TransmitStatus == TransmitStatuses.CompleteOk, "SendData completed");

            var er1 = ExpRes1.WaitCompletedSignal();
            var er2 = ExpRes2.WaitCompletedSignal();
            var er3 = ExpRes3.WaitCompletedSignal();
            var er4 = ExpRes4.WaitCompletedSignal();
            var er5 = ExpRes5.WaitCompletedSignal();
            var er6 = ExpRes6.WaitCompletedSignal();
            var er7 = ExpRes7.WaitCompletedSignal();
            var er8 = ExpRes8.WaitCompletedSignal();
            AsyncService.CancelToken(Primary, lt);
            var lr = lt.WaitCompletedSignal();
            Inconclusive(er1);
            Inconclusive(er2);
            Inconclusive(er3);
            Inconclusive(er4);
            Inconclusive(er5);
            Inconclusive(er6);
            Inconclusive(er7);
            Inconclusive(er8);
            Inconclusive(lr.ReceivedData.Count == 4, "x2 for each recived frame: Supervision report and Basic Report");
        }
    }
}