// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Linq;
using System.Collections.Generic;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Security S0 Network Key Set override")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    public class appbasic_AddNodeS0 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);
            Alias[] ALL = new Alias[] { Primary, Slave, };

            ParallelService.Reset(ALL);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S0);
            ApplicationService.AddSecurity(Slave, SecurityVersions.S0);

            byte[] originalNetworkKey = new byte[16];                               //Define new Network Key
            originalNetworkKey[0] = 0xF1;
            ApplicationService.SecurityTestSettings.SetSecurityTestNetworkKeySetS0(Primary, originalNetworkKey);
            ApplicationService.SecurityTestSettings.SetSecurityTestNetworkKeyVerifyS0(Primary, originalNetworkKey);

            ZnifferService.SetSecurityNetworkKey(originalNetworkKey, false);

            var addToken = AsyncService.AddNode(Primary, Modes.NodeAnyS2);
            var learnReadySignal = AsyncService.WaitNodeStatus(Primary, 1000, NodeStatuses.LearnReady);
            AssertIsTrue(learnReadySignal, "LearnReady");
            var watchToken = ZnifferService.ExpectData(6000);
            watchToken.SetUpWatch(new COMMAND_CLASS_SECURITY.NETWORK_KEY_VERIFY());
            var learnToken = AsyncService.SetLearnMode(Slave);

            var addRes = addToken.WaitCompletedSignal();
            var learnRes = learnToken.WaitCompletedSignal();
            AssertIsTrue(addRes, "AddNode");
            AssertIsTrue(learnRes, "LearnMode");
            AssertIsTrue(addRes.SubstituteStatus == SubstituteStatuses.Done, "Node added successfully NodeId=" + addRes.NodeId, "Failed to add node to network");
            var watchRes = watchToken.WaitCompletedSignal();
            AssertIsTrue(watchRes, "Frame");


            // Remove node using Serial API (WriteZW, ExpectZW)

            byte funcId = 0xAA;                                                       // define funcId 
            var learnReadyToken = AsyncService.ExpectZW(Primary, "00 4B ? 01", 1000); // register callback listener with 1 sec timout 
            var nodeDoneToken = AsyncService.ExpectZW(Primary, "00 4B ? 06", 5000);   // register callback listener with 5 sec timout 

            Service.WriteZW(Primary, new byte[] { 0x00, 0x4B, 0x01, funcId });        // send data to ZW (using byte array as input)
            learnReadyToken.WaitCompletedSignal();                                    // stop TC thread and wait callback 

            Service.SetLearnMode(Slave, LearnModes.LearnModeClassic);                 // run learn mode on secondary

            nodeDoneToken.WaitCompletedSignal();                                      // stop TC thread and wait callback 
            Service.WriteZW(Primary, "00 4B 05");                                     // send data to ZW (using string as input)


            // GetVersion node using Serial API (RequestZW)

            var res = Service.RequestZW(Primary, "00 2E 00 00 00 00", "01 2E", 1000);                // send data to ZW (using string as input) and expect return value (using string as input)
            ReportService.LogText("version report: " + res.Data.ToHexString());
        }
    }
}