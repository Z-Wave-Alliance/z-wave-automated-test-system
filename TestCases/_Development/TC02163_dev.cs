// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Security v2 PC Controller - S2 Network Key Get - Included Node")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    class TC02163_dev : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias primary = new Alias(1);
            Alias secondary = new Alias(2);
            Alias[] ALL = new Alias[] { primary, secondary };
            ParallelService.Reset(ALL);

            BasicResult.AddNodeWithLearnMode addRes;
            ApplicationService.AddSecurity(primary, SecurityVersions.S2 | SecurityVersions.S0);
            ApplicationService.AddSecurity(secondary, SecurityVersions.S2 | SecurityVersions.S0);

            #region 1
            ReportService.LogText("1. Include secondary to primary controller and save the public key.");

            Delay(2000, "! - Add Node");
            addRes = Service.AddNode(primary, secondary);
            Delay(2000, "! - Wait Learn Mode Completed");

            var addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            AssertIsTrue(SubstituteStatuses.Done == addRes.SubstituteStatus, "Inclusion is successful and security v2 is enabled.");

            // Cut key length to 16 if needed.
            var rawPublicKey = ApplicationService.SecurityTestSettings.GetControllerPublicKey(primary);
            byte[] publicKey = null;
            if (rawPublicKey.Length > 16)
            {
                publicKey = rawPublicKey.Take(16).ToArray();
            }
            else
            {
                publicKey = rawPublicKey;
            }
            #endregion

            #region 2
            ReportService.LogText("2. Send non-secured S2 Network Key Get to secondary controller.");
            ApplicationService.SuspendSecurity(primary);
            var expData = AsyncService.ExpectData(secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT(), 2000);
            var sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET() { requestedKey = 0x02 }); // ACCESS KEY
            Delay(2000);
            using (Service.Settings.IsExpectingFailure.Set())
            {
                AssertIsTrue(!expData.WaitCompletedSignal());
            }
            AssertIsTrue(sendRes, "2. Verify receiver does not send a S2 Network Key Report.");
            ApplicationService.ResumeSecurity(primary);
            #endregion

            #region 3
            ReportService.LogText("3. Send S2 Message Encapsulated S2 Network Key Get.");
            expData = AsyncService.ExpectData(secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT(), 2000);
            sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET() { requestedKey = 0x02 }); // ACCESS KEY
            Delay(2000);
            using (Service.Settings.IsExpectingFailure.Set())
            {
                AssertIsTrue(!expData.WaitCompletedSignal());
            }
            AssertIsTrue(sendRes, "3. Verify receiver does not send a S2 Network Key Report.");
            #endregion

            #region 4
            ReportService.LogText("4. Send S2 Message Encapsulated, encrypted with the public network key, S2 Network Key Get.");
            var oldKey = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(primary, SecuritySchemes.S2_ACCESS);

            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(primary, publicKey, SecuritySchemes.S2_ACCESS);
            expData = AsyncService.ExpectData(secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT(), 2000);
            sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET() { requestedKey = 0x02 }); // ACCESS KEY
            Delay(2000);
            using (Service.Settings.IsExpectingFailure.Set())
            {
                AssertIsTrue(!expData.WaitCompletedSignal());
            }
            AssertIsTrue(sendRes, "4. Verify receiver does not send a S2 Network Key Report.");
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(primary, oldKey, SecuritySchemes.S2_ACCESS);
            #endregion

            #region 5
            ReportService.LogText("5. Reset secondary controller application.");
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(primary, addedNodeId);
            #endregion

            #region 6
            ReportService.LogText("6. Send non-secured S2 Network Key Get to secondary controller.");
            ApplicationService.SuspendSecurity(primary);
            expData = AsyncService.ExpectData(secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT(), 2000);
            sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET() { requestedKey = 0x02 }); // ACCESS KEY
            Delay(2000);
            using (Service.Settings.IsExpectingFailure.Set())
            {
                AssertIsTrue(!expData.WaitCompletedSignal());
            }
            AssertIsTrue(sendRes, "6. Verify receiver does not send a S2 Network Key Report.");
            ApplicationService.ResumeSecurity(primary);
            #endregion

            #region 7
            ReportService.LogText("7. Send S2 Message Encapsulated S2 Network Key Get.");
            expData = AsyncService.ExpectData(secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT(), 2000);
            sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET() { requestedKey = 0x02 }); // ACCESS KEY
            Delay(2000);
            using (Service.Settings.IsExpectingFailure.Set())
            {
                AssertIsTrue(!expData.WaitCompletedSignal());
            }
            AssertIsTrue(sendRes, "7. Verify receiver does not send a S2 Network Key Report.");
            #endregion

            #region 8
            ReportService.LogText("8. Send S2 Message Encapsulated, encrypted with the public network key, S2 Network Key Get.");
            oldKey = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(primary, SecuritySchemes.S2_ACCESS);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(primary, publicKey, SecuritySchemes.S2_ACCESS);
            expData = AsyncService.ExpectData(secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT(), 2000);
            sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET() { requestedKey = 0x02 }); // ACCESS KEY
            Delay(2000);
            using (Service.Settings.IsExpectingFailure.Set())
            {
                AssertIsTrue(!expData.WaitCompletedSignal());
            }
            AssertIsTrue(sendRes, "8. Verify receiver does not send a S2 Network Key Report.");
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(primary, oldKey, SecuritySchemes.S2_ACCESS);
            #endregion

            #region 9
            ReportService.LogText("9. Reset primary controller application.");
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(secondary, 0x01);
            #endregion

            #region 10
            ReportService.LogText("10. Send non-secured S2 Network Key Get to secondary controller.");
            ApplicationService.SuspendSecurity(primary);
            expData = AsyncService.ExpectData(secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT(), 2000);
            sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET() { requestedKey = 0x02 }); // ACCESS KEY
            Delay(2000);
            using (Service.Settings.IsExpectingFailure.Set())
            {
                AssertIsTrue(!expData.WaitCompletedSignal());
            }
            AssertIsTrue(sendRes, "10. Verify receiver does not send a S2 Network Key Report.");
            ApplicationService.ResumeSecurity(primary);
            #endregion

            #region 11
            ReportService.LogText("11. Send S2 Message Encapsulated S2 Network Key Get.");
            expData = AsyncService.ExpectData(secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT(), 2000);
            sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET() { requestedKey = 0x02 }); // ACCESS KEY
            Delay(2000);
            using (Service.Settings.IsExpectingFailure.Set())
            {
                AssertIsTrue(!expData.WaitCompletedSignal());
            }
            AssertIsTrue(sendRes, "11. Verify receiver does not send a S2 Network Key Report.");
            #endregion

            #region 12
            ReportService.LogText("12. Send S2 Message Encapsulated, encrypted with the public network key, S2 Network Key Get.");
            oldKey = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(primary, SecuritySchemes.S2_ACCESS);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(primary, publicKey, SecuritySchemes.S2_ACCESS);
            expData = AsyncService.ExpectData(secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT(), 2000);
            sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET() { requestedKey = 0x02 }); // ACCESS KEY
            Delay(2000);
            using (Service.Settings.IsExpectingFailure.Set())
            {
                AssertIsTrue(!expData.WaitCompletedSignal());
            }
            AssertIsTrue(sendRes, "12. Verify receiver does not send a S2 Network Key Report.");
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(primary, oldKey, SecuritySchemes.S2_ACCESS);
            #endregion

            #region 13
            ReportService.LogText("13. Reset primary and secondary controller application.");
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(secondary, 0x01);
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(primary, addedNodeId);
            #endregion

            #region 14
            ReportService.LogText("14. Send non-secured S2 Network Key Get to secondary controller.");
            ApplicationService.SuspendSecurity(primary);
            expData = AsyncService.ExpectData(secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT(), 2000);
            sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET() { requestedKey = 0x02 }); // ACCESS KEY
            Delay(2000);
            using (Service.Settings.IsExpectingFailure.Set())
            {
                AssertIsTrue(!expData.WaitCompletedSignal());
            }
            AssertIsTrue(sendRes, "14. Verify receiver does not send a S2 Network Key Report.");
            ApplicationService.ResumeSecurity(primary);
            #endregion

            #region 15
            ReportService.LogText("15. Send S2 Message Encapsulated S2 Network Key Get.");
            expData = AsyncService.ExpectData(secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT(), 2000);
            sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET() { requestedKey = 0x02 }); // ACCESS KEY
            Delay(2000);
            using (Service.Settings.IsExpectingFailure.Set())
            {
                AssertIsTrue(!expData.WaitCompletedSignal());
            }
            AssertIsTrue(sendRes, "15. Verify receiver does not send a S2 Network Key Report.");
            #endregion

            #region 16
            ReportService.LogText("16. Send S2 Message Encapsulated, encrypted with the public network key, S2 Network Key Get.");
            oldKey = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(primary, SecuritySchemes.S2_ACCESS);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(primary, publicKey, SecuritySchemes.S2_ACCESS);
            expData = AsyncService.ExpectData(secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT(), 2000);
            sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET() { requestedKey = 0x02 }); // ACCESS KEY
            Delay(2000);
            using (Service.Settings.IsExpectingFailure.Set())
            {
                AssertIsTrue(!expData.WaitCompletedSignal());
            }
            AssertIsTrue(sendRes, "16.  Verify receiver does not send a S2 Network Key Report.");
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(primary, oldKey, SecuritySchemes.S2_ACCESS);
            #endregion

            var randData = new byte[] { 0x3F, 0x14, 0x4E, 0xBD, 0x0A, 0xE1, 0x8A, 0x5C, 0x2C, 0xA7, 0x5A, 0xFB, 0x4C, 0x58, 0xF8, 0xEF };

            #region 17
            ReportService.LogText("17. Send a S2 Message Encapsulated BASIC GET to secondary controller using a wrong SPAN.");
            ApplicationService.SecurityTestSettings.SetS2ParametersOverride(primary, SecurityS2ParameterTypes.Span, randData);
            var listenToken = AsyncService.ListenData(primary);
            sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
            Delay(2000);
            AsyncService.CancelToken(primary, listenToken);
            var listenRes = listenToken.WaitCompletedSignal();
            var dataCollection = ReceivedDataCollection.FromListenResult(listenRes);
            var command = dataCollection.FindCommand(new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT());
            AssertIsTrue(command != null);
            AssertIsTrue(0x01 == ((COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT)command).properties1.sos);
            AssertIsTrue(sendRes, "17. Verify that the receiver respond with S2 Nonce Report with the flag SOS set to true.");
            ApplicationService.SecurityTestSettings.ClearS2ParametersOverride(primary);
            #endregion

            #region 18
            ReportService.LogText("18. Send non-secured S2 Network Key Get to secondary controller.");
            ApplicationService.SuspendSecurity(primary);
            expData = AsyncService.ExpectData(secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT(), 2000);
            sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET() { requestedKey = 0x02 }); // ACCESS KEY
            Delay(2000);
            using (Service.Settings.IsExpectingFailure.Set())
            {
                AssertIsTrue(!expData.WaitCompletedSignal());
            }
            AssertIsTrue(sendRes, "18. Verify receiver does not send a S2 Network Key Report.");
            ApplicationService.ResumeSecurity(primary);
            #endregion

            #region 19
            ReportService.LogText("19. Send S2 Message Encapsulated S2 Network Key Get.");
            expData = AsyncService.ExpectData(secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT(), 2000);
            sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET() { requestedKey = 0x02 }); // ACCESS KEY
            Delay(2000);
            using (Service.Settings.IsExpectingFailure.Set())
            {
                AssertIsTrue(!expData.WaitCompletedSignal());
            }
            AssertIsTrue(sendRes, "19. Verify receiver does not send a S2 Network Key Report.");
            #endregion

            #region 20
            ReportService.LogText("20. Send S2 Message Encapsulated, encrypted with the public network key, S2 Network Key Get.");
            oldKey = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(primary, SecuritySchemes.S2_ACCESS);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(primary, publicKey, SecuritySchemes.S2_ACCESS);
            expData = AsyncService.ExpectData(secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT(), 2000);
            sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET() { requestedKey = 0x02 }); // ACCESS KEY
            Delay(2000);
            using (Service.Settings.IsExpectingFailure.Set())
            {
                AssertIsTrue(!expData.WaitCompletedSignal());
            }
            AssertIsTrue(sendRes, "20.  Verify receiver does not send a S2 Network Key Report.");
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(primary, oldKey, SecuritySchemes.S2_ACCESS);
            #endregion

            #region 21
            ReportService.LogText("21. Send a S2 Message Encapsulated BASIC GET to secondary controller using a wrong Network Key.");
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(primary, randData, SecuritySchemes.S2_ACCESS);
            sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
            Delay(2000);
            AsyncService.CancelToken(primary, listenToken);
            listenRes = listenToken.WaitCompletedSignal();
            dataCollection = ReceivedDataCollection.FromListenResult(listenRes);
            command = dataCollection.FindCommand(new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT());
            AssertIsTrue(command != null);
            AssertIsTrue(0x01 == ((COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT)command).properties1.sos);
            AssertIsTrue(sendRes, "21. Verify that the receiver respond with S2 Nonce Report with the flag SOS set to true.");
            #endregion

            #region 22
            ReportService.LogText("22. Send non-secured S2 Network Key Get to secondary controller.");
            ApplicationService.SuspendSecurity(primary);
            expData = AsyncService.ExpectData(secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT(), 2000);
            sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET() { requestedKey = 0x02 }); // ACCESS KEY
            Delay(2000);
            using (Service.Settings.IsExpectingFailure.Set())
            {
                AssertIsTrue(!expData.WaitCompletedSignal());
            }
            AssertIsTrue(sendRes, "22. Verify receiver does not send a S2 Network Key Report.");
            ApplicationService.ResumeSecurity(primary);
            #endregion

            #region 23
            ReportService.LogText("23. Send S2 Message Encapsulated S2 Network Key Get.");
            expData = AsyncService.ExpectData(secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT(), 2000);
            sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET() { requestedKey = 0x02 }); // ACCESS KEY
            Delay(2000);
            using (Service.Settings.IsExpectingFailure.Set())
            {
                AssertIsTrue(!expData.WaitCompletedSignal());
            }
            AssertIsTrue(sendRes, "23. Verify receiver does not send a S2 Network Key Report.");
            #endregion

            #region 24
            ReportService.LogText("24. Send S2 Message Encapsulated, encrypted with the public network key, S2 Network Key Get.");
            oldKey = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(primary, SecuritySchemes.S2_ACCESS);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(primary, publicKey, SecuritySchemes.S2_ACCESS);
            expData = AsyncService.ExpectData(secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT(), 2000);
            sendRes = Service.SendData(primary, secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET() { requestedKey = 0x02 }); // ACCESS KEY
            Delay(2000);
            using (Service.Settings.IsExpectingFailure.Set())
            {
                AssertIsTrue(!expData.WaitCompletedSignal());
            }
            AssertIsTrue(sendRes, "24.  Verify receiver does not send a S2 Network Key Report.");
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(primary, oldKey, SecuritySchemes.S2_ACCESS);
            #endregion
        }
    }
}
