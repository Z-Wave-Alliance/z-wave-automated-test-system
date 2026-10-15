// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Collections.Generic;
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("")]
    [ZipDeviceMapping(1, "z2")]
    [DeviceMapping(2, ChipSeries.ZW050x, LibTypes.EndDeviceLib, Alias = "G1", FileNameFilter = "generator")]
    [DeviceMapping(3, LibTypes.ControllerBridgeLib)]
    [DeviceMapping(4, LibTypes.EndDeviceLib)]
    [DeviceMapping(5, LibTypes.EndDeviceLib)]
    [DeviceMapping(6, LibTypes.EndDeviceLib)]
    [DeviceMapping(7, LibTypes.EndDeviceLib)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class appzip_SecurityNonceGetFromSelf : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            bool skipStep1 = false;
            bool skipStep2 = false;
            bool skipStep3 = false;
            bool skipStep4 = false;

            Alias ZipPrimary = new Alias(1);
            Alias RogueNode = new Alias(2);

            Alias Secondary = new Alias(3);

            Alias Slave1 = new Alias(4);
            Alias Slave2 = new Alias(5);
            Alias Slave3 = new Alias(6);
            Alias Slave4 = new Alias(7);


            Alias[] ALL = new Alias[] { RogueNode, Secondary, Slave1, Slave2, Slave3, Slave4 };

            Service.ApplicationNodeInformation(Secondary, true,
                   (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                   new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID });
            Service.ApplicationNodeInformation(Slave1, true,
                   (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                   new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID });
            Service.ApplicationNodeInformation(Slave2, true,
                   (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                   new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID });
            Service.ApplicationNodeInformation(Slave3, true,
                (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID });
            Service.ApplicationNodeInformation(Slave4, true,
               (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
               new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID });

            ParallelService.SetDefault(ALL);
            ZipService.SetDefault(ZipPrimary);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S0 | SecurityVersions.S2);

            var addRes = ZipService.AddNodeWithDSK(ZipPrimary, Secondary, Service.GetDsk(Secondary, 2));
            ZipService.RegisterZipDevice(ZipPrimary, Secondary, addRes.NodeId);

            addRes = ZipService.AddNodeWithDSK(ZipPrimary, Slave1, Service.GetDsk(Slave1, 2));
            ZipService.RegisterZipDevice(ZipPrimary, Slave1, addRes.NodeId);

            addRes = ZipService.AddNodeWithDSK(ZipPrimary, Slave2, Service.GetDsk(Slave2, 2));
            ZipService.RegisterZipDevice(ZipPrimary, Slave2, addRes.NodeId);

            addRes = ZipService.AddNodeWithDSK(ZipPrimary, Slave3, Service.GetDsk(Slave3, 2));
            ZipService.RegisterZipDevice(ZipPrimary, Slave3, addRes.NodeId);

            addRes = ZipService.AddNodeWithDSK(ZipPrimary, Slave4, Service.GetDsk(Slave4, 2));
            ZipService.RegisterZipDevice(ZipPrimary, Slave4, addRes.NodeId);

            var memSecondaryRes = Service.MemoryGetId(Secondary);

            if (!skipStep1)
            {
                ReportService.LogTitle("1. RX single S0 frame");

                var zniffWatch = ZnifferService.CollectData();
                zniffWatch.SetUpWatch(0x01, 0xFF, new COMMAND_CLASS_SECURITY.SECURITY_NONCE_GET());
                zniffWatch.SetUpWatch(memSecondaryRes.NodeId, null, new COMMAND_CLASS_SECURITY.SECURITY_NONCE_GET());
                zniffWatch.SetUpWatch(null, memSecondaryRes.NodeId, new COMMAND_CLASS_SECURITY.SECURITY_NONCE_REPORT());

                BasicResult.Void sendRes = Service.SendDataForced(
                    RogueNode,
                    Context.ChannelConfig,
                    TransmitRfOptions.RfOptionSpeed40k,
                    memSecondaryRes.HomeId,
                    0x01,
                    0x01,
                    true,
                    true,
                    0x0F,
                    new byte[] { 0xFF },
                    new COMMAND_CLASS_SECURITY.SECURITY_NONCE_GET());

                Delay(100);
                ApplicationService.SuspendSecurity(Secondary);
                var resRequestNonce = ComplianceService.RequestData(Secondary, (byte)0x01, new COMMAND_CLASS_SECURITY.SECURITY_NONCE_GET(), new COMMAND_CLASS_SECURITY.SECURITY_NONCE_REPORT(), 5 * 60000);
                var zniffRes = zniffWatch.WaitCompletedSignal();
                ApplicationService.ResumeSecurity(Secondary);

                Inconclusive(resRequestNonce, "1.1. Nonce Report received", "1.1. Nonce Report not received");
                Inconclusive(resRequestNonce.ElapsedMs <= 10000,
                    $"1.2. Nonce Report delivery time {resRequestNonce.ElapsedMs} less than 10s",
                    $"1.2. Nonce Report delivery time {resRequestNonce.ElapsedMs} greater than 10s");
            }

            if (!skipStep2)
            {
                ReportService.LogTitle("2. TX single S0 frame");

                var zniffWatch = ZnifferService.ExpectData(10000);
                zniffWatch.SetUpWatch(0x01, 0xFF, new COMMAND_CLASS_SECURITY.SECURITY_NONCE_GET());
                zniffWatch.SetUpWatch(null, memSecondaryRes.NodeId, new COMMAND_CLASS_SECURITY.SECURITY_NONCE_GET());
                zniffWatch.SetUpWatch(memSecondaryRes.NodeId, null, new COMMAND_CLASS_SECURITY.SECURITY_NONCE_REPORT());

                BasicResult.Void sendRes = Service.SendDataForced(
                    RogueNode,
                    Context.ChannelConfig,
                    TransmitRfOptions.RfOptionSpeed40k,
                    memSecondaryRes.HomeId,
                    0x01,
                    0x01,
                    false,
                    true,
                    0x0F,
                    new byte[] { 0xFF },
                    new COMMAND_CLASS_SECURITY.SECURITY_NONCE_GET());

                Delay(100);
                var pOptions = new byte[] { (1 << 7) + 4, 2, 0, 0 };
                ZipService.SendData(Secondary, pOptions, new COMMAND_CLASS_SECURITY.SECURITY_NONCE_GET(), true);
                var zniffRes = zniffWatch.WaitCompletedSignal();
                Inconclusive(zniffRes, "2.3. Nonce Get/Report received within 10 sec", "2.3. Nonce Get/Report not received within 10 sec");
                Delay(30000);
            }

            if (!skipStep3)
            {
                ReportService.LogTitle("3. RX single S2 frame");

                var zniffWatch = ZnifferService.CollectData();
                zniffWatch.SetUpWatch(memSecondaryRes.NodeId, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_GET());
                zniffWatch.SetUpWatch(null, memSecondaryRes.NodeId, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT());

                BasicResult.Void sendRes = Service.SendDataForced(
                    RogueNode,
                    Context.ChannelConfig,
                    TransmitRfOptions.RfOptionSpeed40k,
                    memSecondaryRes.HomeId,
                    0x01,
                    0x01,
                    false,
                    true,
                    0x0A,
                    new byte[] { 0x01 },
                    new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_GET());

                Delay(100);
                ApplicationService.SuspendSecurity(Secondary);
                var resRequestNonce = ComplianceService.RequestData(Secondary, (byte)0x01, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_GET(), new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT(), 5 * 60000);
                var zniffRes = zniffWatch.WaitCompletedSignal();
                ApplicationService.ResumeSecurity(Secondary);

                Inconclusive(resRequestNonce, "3.1. S2 Nonce Report received", "3.1. S2 Nonce Report not received");
                Inconclusive(resRequestNonce.ElapsedMs <= 10000,
                    $"3.2. Nonce Report delivery time {resRequestNonce.ElapsedMs} less than 10s",
                    $"3.2. Nonce Report delivery time {resRequestNonce.ElapsedMs} greater than 10s");
            }

            if (!skipStep4)
            {
                ReportService.LogTitle("4. TX single S2 frame");

                var zniffWatch = ZnifferService.ExpectData(10000);
                zniffWatch.SetUpWatch(0x01, 0x01, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_GET());
                zniffWatch.SetUpWatch(null, memSecondaryRes.NodeId, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_GET());
                zniffWatch.SetUpWatch(memSecondaryRes.NodeId, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT());

                BasicResult.Void sendRes = Service.SendDataForced(
                    RogueNode,
                    Context.ChannelConfig,
                    TransmitRfOptions.RfOptionSpeed40k,
                    memSecondaryRes.HomeId,
                    0x01,
                    0x01,
                    false,
                    true,
                    0x0A,
                    new byte[] { 0x01 },
                    new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_GET());

                Delay(100);
                var pOptions = new byte[] { (1 << 7) + 4, 2, 0, 0 };
                ZipService.SendData(Secondary, pOptions, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_GET(), true);
                var zniffRes = zniffWatch.WaitCompletedSignal();
                Inconclusive(zniffRes, "4.3. S2 Nonce Get/Report received within 10 sec", "2.3. S2 Nonce Get/Report not received within 10 sec");
                Delay(30000);
            }
        }
    }
}