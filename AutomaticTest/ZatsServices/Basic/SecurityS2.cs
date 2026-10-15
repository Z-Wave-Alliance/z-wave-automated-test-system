// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Linq;
using Utils;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using ZWave.Devices;
using ZWave.Layers;

namespace Zats.Services
{
    public class SecurityS2 : TestSettingsBase, ISecurityS2
    {
        private readonly int REQUEST_TIMEOUT_MS = 10000;
        private readonly TransmitOptions TX_OPTIONS = TransmitOptions.Ack | TransmitOptions.AutoRoute | TransmitOptions.Explore;

        public SecurityS2(DeviceManager deviceManager)
            : base(deviceManager)
        {
        }

        private RequestDataResult RequestData(Device device, Node receiverNodeId, byte[] data, byte[] expectedData)
        {
            return device.RequestData(receiverNodeId, data, (ZWave.Enums.TransmitOptions)TX_OPTIONS, expectedData, REQUEST_TIMEOUT_MS);
        }

        public BasicResult.BootstrapS2 Bootstrap(Alias controllerNumber, Node nodeId, byte[] dsk)
        {
            RequestDataResult res = null;
            Device device = DeviceManager.GetDevice(controllerNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.IsInclusion = true;
            }

            device.Network.SetSecuritySchemes(nodeId, ZWave.Enums.SecuritySchemeSet.ALL);
            var isLr = device.Network.IsLongRangeEnabled(nodeId);

            #region local variables declaration
            var kexGetCmd = new COMMAND_CLASS_SECURITY_2.KEX_GET();
            var kexReportCmd = new COMMAND_CLASS_SECURITY_2.KEX_REPORT();
            var kexSetCmd = new COMMAND_CLASS_SECURITY_2.KEX_SET();
            var publicKeyPrimaryRptCmd = new COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT();
            var publicKeySecondaryRptCmd = new COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT();
            var kexReportEchoCmd = new COMMAND_CLASS_SECURITY_2.KEX_REPORT();
            var kexSetEchoCmd = new COMMAND_CLASS_SECURITY_2.KEX_SET();
            var networkKeyGetCmd = new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET();
            var networkKeyReportCmd = new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT();
            var networkKeyVerifyCmd = new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_VERIFY();
            var transferEndCmd = new COMMAND_CLASS_SECURITY_2.SECURITY_2_TRANSFER_END();
            byte grantedKeys = 0;
            byte highestGrantedScheme = 1;
            #endregion

            //ReportService.LogText("Send KEX Get, wait KEX Report");
            DeviceManager.SecurityTestSettings.SetActiveSecurityScheme(controllerNumber, nodeId, SecuritySchemes.NONE);
            res = RequestData(device, nodeId, kexGetCmd, kexReportCmd);
            kexReportCmd = res.Command;

            //ReportService.LogText("Send KEX Set, wait Public Key Report");
            if (res)
            {
                DeviceManager.SecurityTestSettings.SetActiveSecurityScheme(controllerNumber, nodeId, SecuritySchemes.NONE);
                kexSetCmd.selectedEcdhProfile = 1;
                kexSetCmd.selectedKexScheme = 2;
                grantedKeys = ((COMMAND_CLASS_SECURITY_2.KEX_REPORT)res.Command).requestedKeys;
                var highestGrantedKey = grantedKeys & (~0x80);
                if (highestGrantedKey > 0)
                {
                    while ((highestGrantedKey >> highestGrantedScheme) > 0)
                    {
                        highestGrantedScheme++;
                    }
                }
                else
                {
                    highestGrantedScheme = 4; //S0
                }
                kexSetCmd.grantedKeys = grantedKeys;
                res = RequestData(device, nodeId, kexSetCmd, publicKeySecondaryRptCmd);
                if (res.Command != null) kexSetEchoCmd = kexSetCmd;
                kexSetEchoCmd.properties1.echo = 0x01;
            }

            //ReportService.LogText("Create shared network key");
            if (res)
            {
                var secondarySentPublic = ((COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT)res.Command).ecdhPublicKey.ToArray();
                secondarySentPublic = dsk.Concat(secondarySentPublic.Skip(dsk.Length)).ToArray();
                byte[] primaryShared = DeviceManager.SecurityTestSettings.CalculateTempNetworkKeyS2(controllerNumber, secondarySentPublic, true);
                $"primaryShared:{primaryShared.ToHexString()}"._DLOG();
                //ReportService.LogInfo($"S2_TEMP key: {primaryShared.ToHexString()}");
                DeviceManager.SecurityTestSettings.SetNetworkKeyS2Temp(controllerNumber, primaryShared);

                //ReportService.LogText("Send Public Key Report, wait KEX Set (echo)");
                DeviceManager.SecurityTestSettings.SetActiveSecurityScheme(controllerNumber, nodeId, SecuritySchemes.NONE);
                publicKeyPrimaryRptCmd.ecdhPublicKey = DeviceManager.SecurityTestSettings.GetControllerPublicKey(controllerNumber);
                publicKeyPrimaryRptCmd.properties1.includingNode = 1;
                res = RequestData(device, nodeId, publicKeyPrimaryRptCmd, kexSetEchoCmd);
            }

            //ReportService.LogText("Send KEX Report (echo), wait Network Key Get");
            if (res)
            {
                DeviceManager.SecurityTestSettings.SetActiveSecurityScheme(controllerNumber, nodeId, SecuritySchemes.S2_TEMP);
                kexReportEchoCmd = kexReportCmd;
                kexReportEchoCmd.properties1.echo = 0x01;
                res = RequestData(device, nodeId, kexReportEchoCmd, networkKeyGetCmd);
                var reuestedKey = ((COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET)res.Command).requestedKey;
                while (grantedKeys > 0 && res)
                {
                    //ReportService.LogText("Send  Network Key Report, wait Network Key Verify");
                    //DeviceManager.SecurityTestSettings.SetActiveSecurityScheme(controllerNumber, nodeId, SecuritySchemes.S2_TEMP);

                    networkKeyReportCmd.grantedKey = reuestedKey;
                    switch (reuestedKey)
                    {
                        case 0x01:
                            networkKeyReportCmd.networkKey = DeviceManager.SecurityTestSettings.GetNetworkKeyS2(controllerNumber, SecuritySchemes.S2_UNAUTHENTICATED, isLr);
                            res = RequestData(device, nodeId, networkKeyReportCmd, networkKeyVerifyCmd);
                            break;
                        case 0x02:
                            networkKeyReportCmd.networkKey = DeviceManager.SecurityTestSettings.GetNetworkKeyS2(controllerNumber, SecuritySchemes.S2_AUTHENTICATED, isLr);
                            res = RequestData(device, nodeId, networkKeyReportCmd, networkKeyVerifyCmd);
                            break;
                        case 0x04:
                            networkKeyReportCmd.networkKey = DeviceManager.SecurityTestSettings.GetNetworkKeyS2(controllerNumber, SecuritySchemes.S2_ACCESS, isLr);
                            res = RequestData(device, nodeId, networkKeyReportCmd, networkKeyVerifyCmd);
                            break;
                        case 0x80:
                            networkKeyReportCmd.networkKey = DeviceManager.SecurityTestSettings.GetNetworkKeyS2(controllerNumber, SecuritySchemes.S0);
                            res = RequestData(device, nodeId, networkKeyReportCmd, networkKeyVerifyCmd);
                            break;
                        default:
                            break;
                    }
                    DeviceManager.SecurityTestSettings.SetActiveSecurityScheme(controllerNumber, nodeId, SecuritySchemes.S2_TEMP);
                    transferEndCmd.properties1.keyVerified = 0x01;
                    grantedKeys = (byte)(grantedKeys - reuestedKey);
                    if (grantedKeys > 0)
                    {
                        //ReportService.LogText("Send Transfer End, wait Network Key Get");
                        res = RequestData(device, nodeId, transferEndCmd, networkKeyGetCmd);
                        reuestedKey = ((COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET)res.Command).requestedKey;
                    }
                    else
                    {
                        //ReportService.LogText("Send Transfer End, wait Transfer End");
                        res = RequestData(device, nodeId, transferEndCmd, transferEndCmd);
                    }
                }
            }
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.IsInclusion = false;
            }

            DeviceManager.LogNetworkKeys(controllerNumber, nodeId);
            if (res == null)
            {
                return new BasicActionResult.BootstrapS2(true, (SecuritySchemes)highestGrantedScheme);
            }
            else
            {
                return new BasicActionResult.BootstrapS2(false, (SecuritySchemes)highestGrantedScheme);
            }
        }
    }
}




