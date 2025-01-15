// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;
using System.Linq;

namespace ZatsTests
{
    [ZatsTestCase(@"FrameExamples - SendDataForced")]
    [DeviceMapping(1, LibTypes.Controller)]
    [DeviceMapping(2, LibTypes.Controller)]
    [DeviceMapping(3, ChipSeries.ZW070x, LibTypes.EndDeviceSysTestLib)] // controller?
    public class apptestiface_PTI : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias pmry = new Alias(1);
            Alias sdry = new Alias(2);
            Alias gntr = new Alias(3);

            var capRes = Service.SerialApiGetCapabilities(gntr);
            var txt = capRes && capRes.SupportedSerialApiCommands != null && capRes.SupportedSerialApiCommands.Any(x => x == SerialApiCommands.TestSetup) ?
                "listed" : "not listed";
            ReportService.LogInfo($"SerialApiGetCapabilities.SupportedSerialApiCommands {txt} SerialApiCommands.TestSetup(0x0F <0x01>)");

            Service.Reset(pmry);
            Service.AddNode(pmry, sdry);
            Service.SendData(pmry, sdry, "2001AA".ToByteArray(), TransmitOptions.None);
            Delay(300);
            Service.SendDataForced(gntr, Context.ChannelConfig, TransmitRfOptions.RfOptionSpeed9600, "BE0096EF".ToByteArray(), 0x0A, 0x01, false, false, 0xE1, "0B".ToByteArray(), "200304".ToByteArray());
            Delay(300);
            Service.SendData(pmry, sdry, "2001AA".ToByteArray(), TransmitOptions.None);
            Delay(300);
            Service.SendDataForced(gntr, Context.ChannelConfig, TransmitRfOptions.RfOptionSpeed40k, "BE0400EF".ToByteArray(), 0x0A, 0x01, false, false, 0xE2, "0B".ToByteArray(), "200304".ToByteArray());
            Delay(300);
            Service.SendData(pmry, sdry, "2001AA".ToByteArray(), TransmitOptions.None);
            Delay(300);
            Service.SendDataForced(gntr, Context.ChannelConfig, TransmitRfOptions.RfOptionSpeed100k, "BE0400EF".ToByteArray(), 0x0A, 0x01, false, false, 0xE2, "0B".ToByteArray(), "200304".ToByteArray());
            Delay(300);
            Service.SendData(pmry, sdry, "2001AA".ToByteArray(), TransmitOptions.None);
            Delay(300);
            Service.SendDataForced(gntr, Context.ChannelConfig, TransmitRfOptions.RfOptionSpeedLR100k, "BE0400EF".ToByteArray(), 0x0A, 0x01, false, false, 0xE2, "03FF".ToByteArray(), "200304".ToByteArray());
            Delay(300);
            Service.SendData(pmry, sdry, "2001AA".ToByteArray(), TransmitOptions.None);
            Delay(300);
            Service.SendDataForced(gntr, Context.ChannelConfig, TransmitRfOptions.RfOptionSpeedLR100k, "BE0400EF".ToByteArray(), 0x0A, 0x01, false, false, 0xE2, "0400".ToByteArray(), "200304".ToByteArray());
        }
    }
}