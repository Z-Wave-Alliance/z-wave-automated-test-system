// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;
using System.Linq;
using System;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -FUNC_ID_GET_LR_CHANNEL and FUNC_ID_SET_LR_CHANNEL SerialAPI Controller version > 9",
                      Tags.SYS,
                      Tags.Brief,
                      Tags.PCController)]
    [DeviceMapping(1, LibTypes.ControllerStaticLib, RfRegion = RfRegions.US_LR)]
    [DeviceMapping(2, LibTypes.EndDeviceAny, RfRegion = RfRegions.US_LR)]
    public class appbasic_Get_Set_LRChannel : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Dut = new Alias(1);
            Alias slave1 = new Alias(2);

            ReportService.LogTitle("Start");

            var capRes = Service.SerialApiGetCapabilities(Dut);
            AssertIsTrue(capRes && capRes.SupportedSerialApiCommands != null &&
                capRes.SupportedSerialApiCommands.Any(x => x == SerialApiCommands.GetLRChannel) &&
                capRes.SupportedSerialApiCommands.Any(x => x == SerialApiCommands.SetLRChannel),
                "Controller supports CmdGetLRChannel(0xDB) & CmdSetLRChannel(0xDC)",
                "Controller doesn't support CmdGetLRChannel(0xDB) & CmdSetLRChannel(0xDC)!");

            ParallelService.Reset(Dut, slave1);
            ApplicationService.AddSecurity(Dut, SecurityVersions.S0 | SecurityVersions.S2);
            var cmdClasses = new[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID };
            Service.ApplicationNodeInformationCmdClasses(slave1, cmdClasses, cmdClasses, cmdClasses);
            ApplicationService.AddResponseEx(slave1, new COMMAND_CLASS_VERSION.VERSION_REPORT(), new COMMAND_CLASS_VERSION.VERSION_GET());

            var icnlRes1 = Service.IncludeNode(Dut, slave1);
            AssertIsTrue(icnlRes1 && icnlRes1.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done);
            var nodeId = icnlRes1.AddNodeResult.Node;

            var txt = "Display LR.NODE_INFO";
            var ztLR = ZnifferService.ExpectData(2000).SetUpWatch(new COMMAND_CLASS_ZWAVE_LONG_RANGE.ZWAVE_LR_CMD_NODE_INFO_FRAME());
            ComplianceService.SendNodeInfo(Dut, 0xFFF, TX_OPTIONS);
            if (ztLR.WaitCompletedSignal()) ReportService.LogOk(txt);
            else ReportService.LogWarning(txt);

            ReportService.LogText("1. Get the active Long Range rf-channel: FUNC_ID_GET_LR_CHANNEL = 0xDB.");
            var getRes = Service.GetLRChannel(Dut);
            AssertIsTrue(getRes,
               "GetLRChannel = " + getRes.Channel,
               "Failed GetLRChannel");

            var r1 = ComplianceService.SendData(Dut, nodeId, new COMMAND_CLASS_BASIC.BASIC_SET());
            var expAckChannelNo = getRes.Channel == LongRangeChannels.ChannelA ? 0x03 : 0x04;
            AssertIsTrue(r1 && r1.AckChannelNo == expAckChannelNo);

            var testValue = getRes.Channel != LongRangeChannels.ChannelA ? LongRangeChannels.ChannelA : LongRangeChannels.ChannelB;
            ReportService.LogText("2. Set the active Long Range rf-channel: FUNC_ID_SET_LR_CHANNEL = 0xDC. Test Value = " + testValue);
            var setRes = Service.SetLRChannel(Dut, testValue);
            AssertIsTrue(setRes,
                "Set Channel to " + testValue,
                "Failed Set Channel");

            ztLR = ZnifferService.ExpectData(2000).SetUpWatch(new COMMAND_CLASS_ZWAVE_LONG_RANGE.ZWAVE_LR_CMD_NODE_INFO_FRAME());
            ComplianceService.SendNodeInfo(Dut, 0xFFF, TX_OPTIONS);
            if (ztLR.WaitCompletedSignal()) ReportService.LogOk(txt);
            else ReportService.LogWarning(txt);

            ReportService.LogText("3. Get the active Long Range rf-channel: FUNC_ID_GET_LR_CHANNEL = 0xDB.");
            getRes = Service.GetLRChannel(Dut);
            AssertIsTrue(getRes,
              "GetLRChannel = " + getRes.Channel,
              "Failed GetLRChannel");
            AssertIsTrue(testValue == getRes.Channel,
                "Set == Get",
                "Set != Get");

            var r2 = ComplianceService.SendData(Dut, nodeId, new COMMAND_CLASS_BASIC.BASIC_SET());
            expAckChannelNo = getRes.Channel == LongRangeChannels.ChannelA ? 0x03 : 0x04;
            AssertIsTrue(r2 && r2.AckChannelNo == expAckChannelNo);

            ReportService.LogText("4. Set the active Long Range rf-channel to Auto");
            setRes = Service.SetLRChannel(Dut, LongRangeChannels.ChannelAuto);
            AssertIsTrue(setRes,
                "Set Channel to " + testValue,
                "Failed Set Channel");

            ReportService.LogText("5. Get the active Long Range rf-channel");
            getRes = Service.GetLRChannel(Dut);
            AssertIsTrue(getRes,
               "GetLRChannel = " + getRes.Channel,
               "Failed GetLRChannel");
        }
    }
}