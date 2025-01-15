// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetDefault
    -Add controller. 
    -Add slave
    -SendData")]
    [DeviceMapping(1, LibTypes.Controller)]
    [DeviceMapping(2, LibTypes.Controller)]
    [DeviceMapping(3, LibTypes.EndDeviceAny)]
    public class appbasic_ExpectData : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias Slave = new Alias(3);
            Alias[] ALL = new Alias[] { Primary, Secondary, Slave };


            ReportService.LogText("Start");

            Service.ApplicationNodeInformation(Primary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_BASIC.ID });
            Service.ApplicationNodeInformation(Secondary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_BASIC.ID });
            Service.ApplicationNodeInformation(Slave, false, (byte)GenericTypes.SENSOR_BINARY, (byte)SpecificTypes.NOT_USED, new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY_2.ID });

            ParallelService.SetDefault(ALL);
            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);

            Service.IncludeNode(Primary, Secondary);
            Service.IncludeNode(Primary, Slave);
            //setup Sensor PIR emulation
            List<CommandClassSupport> supportedCCs = new List<CommandClassSupport>();
            supportedCCs.Add(new ZWavePlusInfoSupport() { Version = 2, Secure = false });
            supportedCCs.Add(new VersionSupport(supportedCCs) { Version = 3, Secure = true, SecureExclusive = true });
            supportedCCs.Add(new WakeUpSupport() { Version = 2, Secure = true });

            ApplicationService.SetupRootDevice(
                Slave,                                         // deviceNumber                                            
                DeviceOptions.NoneListening,                        // deviceOptions
                (byte)GenericTypes.SENSOR_NOTIFICATION,             // genericDeviceClass
                (byte)SpecificTypes.NOT_USED,                       // specificDeviceClass
                false,                                              // multiChannel
                true,                                               // supervision
                false,                                              // crc16
                SecurityVersions.S0 | SecurityVersions.S2,          // securityVersions
                true,                                               // transportService
                supportedCCs);

            BasicToken.ExpectData token = AsyncService.ExpectData(Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT() { value = 0x55 }, 2000);
            Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            AssertIsTrue(token.WaitCompletedSignal(), "expected message");

            token = AsyncService.ExpectData(Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT() { value = 0x55 }, 3, 2000);
            Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT() { value = 0x55 });
            AssertIsTrue(token.WaitCompletedSignal(), "expected message");


            token = AsyncService.ExpectData(Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT() { value = 0x55 }, 3, 2000);
            Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT() { value = 0x77 });
            using (Service.Settings.IsExpectingFailure.Set())
            {
                AssertIsTrue(!token.WaitCompletedSignal(), "expected message");
            }


            token = AsyncService.ExpectData(Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT() { value = 0x55 }, 3, 2000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT() { value = 0x55 });
            AssertIsTrue(token.WaitCompletedSignal(), "expected message");

            Delay(40000);
        }
    }
}