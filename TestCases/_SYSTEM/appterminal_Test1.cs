// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Test1")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib, Alias = "D1")]
    [DeviceMapping(2, LibTypes.EndDeviceLib, Alias = "D2")]
    [TerminalDeviceMapping(3, LibTypes.RailTest, Alias = "N1")]
    [TerminalDeviceMapping(4, LibTypes.RailTest, Alias = "N2")]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class appterminal_Test1 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);
            Alias Noise1 = new Alias(3);
            Alias Noise2 = new Alias(4);
            bool is700 = Context.IsZW070x(Slave);
            const int NoisePower = 30;
            int Count = 200;
            const int FailSequenceThreshold = 5;
            const int LongTransmitThreshold = 5;
            byte[] Frame = "820133".ToByteArray();

            TerminalService.CommSettings.AppendNewLine = true;

            DisableNoise(Noise1, Noise2);

            ReportService.LogText("Start");


            for (int JJ = 0; JJ < 2; JJ++)
            {
                if (JJ == 1)
                {
                    ReportService.LogInfo($"=|={Context.Frequency} frequency (FLiRS)");
                    Service.ApplicationNodeInformation(Slave, DeviceOptions.FreqListeningMode250ms,
                        (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                        new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });
                    Service.SoftReset(Slave);
                    Count = Count / 2;
                }

                BasicResult.IncludeNodeWithLearnMode addRes = null;
                for (int i = 0; i < 10; i++)
                {
                    TestInterfaceService.SoftReset(Primary);
                    TestInterfaceService.SoftReset(Slave);
                    ParallelService.SetDefault(Primary, Slave);
                    addRes = Service.IncludeNode(Primary, Slave, 65000);
                    if (addRes && addRes.AddNodeResult && addRes.SetLearnMode)
                    {
                        break;
                    }
                }
                AssertIsTrue(addRes, "Inclusion");
                Delay(1000);
                if (JJ == 1)
                {
                    Service.PowerMgmtCancel(Slave, 0);
                }

                Ch ch = new Ch(NoisePower);
                Mc[] mcArray = ch.GetForFrequency(Context.Frequency, -40);

                int txMaxFailSequence = 0;
                int txMaxLongTransmitSequence = 0;
                int prevLbt = 0;
                ReportService.LogInfo($"=.={Context.Frequency} frequency");
                foreach (var mc in mcArray)
                {
                    if (prevLbt != mc.lbt)
                    {
                        prevLbt = mc.lbt;
                    }
                    Service.SetListenBeforeTalkThreshold(Primary, 0, mc.lbt);
                    Service.SetListenBeforeTalkThreshold(Primary, 1, mc.lbt);
                    Service.SetListenBeforeTalkThreshold(Primary, 2, mc.lbt);

                    EnableNoise(Noise1, Noise2, mc, NoisePower);

                    var block1 = mc.mc1 >= 0 ? ch.menu_list[mc.mc1] : "none";
                    var block2 = mc.mc2 >= 0 ? ch.menu_list[mc.mc2] : "none";
                    ReportService.LogInfo($"=== frequency {Context.Frequency}; lbt:{mc.lbt}; block1:{block1}; block2:{block2};");

                    var iter = new Iter(is700);
                    for (int i = 0; i < Count; i++)
                    {
                        if (JJ == 1)
                        {
                            Delay(2000);
                        }
                        var sendRes = Service.SendData(Primary, Slave, Frame);
                        ReportService.LogText($"TransmitStatus:{sendRes.TransmitStatus}, RouteSpeed{sendRes.RouteSpeed}, TransmitTicks:{sendRes.TransmitTicks}, LastTxChannelNo:{sendRes.LastTxChannelNo}, AckChannelNo:{sendRes.AckChannelNo}");
                        iter.Parse(sendRes);
                    }
                    iter.Report();
                    DisableNoise(Noise1, Noise2, mc);
                }
                Inconclusive(txMaxFailSequence <= FailSequenceThreshold && txMaxLongTransmitSequence <= LongTransmitThreshold, "thresholds not exceeded", "thresholds exceeded");
            }
        }


        private void DisableNoise(Alias noiseTerm1, Alias noiseTerm2)
        {
            DisableNoise(noiseTerm1, noiseTerm2, new Mc(1, 1, 0));
        }

        private void DisableNoise(Alias noiseTerm1, Alias noiseTerm2, Mc mc)
        {
            if (mc.mc1 >= 0)
            {
                ReportService.LogText("Reset chip to start from a known state");
                ResetNoiseTerm(noiseTerm1);
                ReportService.LogText("Make sure to be in Idle mode and enable Zwave");
                SetIdleNoiseTerm(noiseTerm1);
            }
            if (mc.mc2 >= 0)
            {
                ReportService.LogText("Reset chip to start from a known state");
                ResetNoiseTerm(noiseTerm2);
                ReportService.LogText("Make sure to be in Idle mode and enable Zwave");
                SetIdleNoiseTerm(noiseTerm2);
            }
        }

        private void EnableNoise(Alias noiseTerm1, Alias noiseTerm2, Mc mc, int noisePower)
        {
            if (mc.mc1 >= 0)
            {
                ReportService.LogText("N1. Setup Channel");
                SetNoiseTerm(noiseTerm1, mc.mc1, noisePower);
                ReportService.LogText("N1. Start Tx Carrier mode: ");
                StartTxCarrierNoiseTerm(noiseTerm1);
            }

            if (mc.mc2 >= 0)
            {
                ReportService.LogText("N2. Setup Channel");
                SetNoiseTerm(noiseTerm2, mc.mc2, noisePower);
                ReportService.LogText("N2. Start Tx Carrier mode: ");
                StartTxCarrierNoiseTerm(noiseTerm2);
            }
        }

        private void SetNoiseTerm(Alias noiseTerm, int mc, int noisePower)
        {
            Ch ch = new Ch(noisePower);
            var reg_cmd = $"setzwaveregion {ch.reg_list[mc]}";
            var pow_cmd = $"SetPower {ch.pow_list[mc]} raw";
            var ch_cmd = $"setchannel {ch.ch_list[mc]}";

            TerminalService.RequestWithDelay(noiseTerm, reg_cmd, 300);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
            //TerminalService.RequestWithDelay(noiseTerm, ch.PB14[ch.saw_list[mc]], 300);
            //TerminalService.RequestWithDelay(noiseTerm, "", 300);
            //TerminalService.RequestWithDelay(noiseTerm, ch.PB15[ch.saw_list[mc]], 300);
            //TerminalService.RequestWithDelay(noiseTerm, "", 300);
            TerminalService.RequestWithDelay(noiseTerm, pow_cmd, 300);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
            TerminalService.RequestWithDelay(noiseTerm, ch_cmd, 300);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
        }

        private void StartTxCarrierNoiseTerm(Alias noiseTerm)
        {
            TerminalService.RequestWithDelay(noiseTerm, "settxtone 1", 300);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
        }

        private void SetIdleNoiseTerm(Alias noiseTerm)
        {
            TerminalService.RequestWithDelay(noiseTerm, "rx 0", 300);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
            TerminalService.RequestWithDelay(noiseTerm, "SetZwaveMode 1 3", 300);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
            TerminalService.RequestWithDelay(noiseTerm, "SetTxLength 20", 300);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
            TerminalService.RequestWithDelay(noiseTerm, "SetTxPayload 7 20", 300);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
        }

        private void ResetNoiseTerm(Alias noiseTerm)
        {
            TestInterfaceService.SoftReset(noiseTerm);
            Delay(1000);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
            TerminalService.RequestWithDelay(noiseTerm, "reset", 700);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
            TerminalService.RequestWithDelay(noiseTerm, "reset", 700);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
            TerminalService.RequestWithDelay(noiseTerm, "", 300);
        }
    }

    public struct Mc
    {
        public Mc(sbyte m1, sbyte m2, sbyte lt)
        {
            mc1 = m1;
            mc2 = m2;
            lbt = (byte)lt;
        }
        public sbyte mc1;
        public sbyte mc2;
        public byte lbt;
    }

    public class Ch
    {
        public string[] menu_list { get; set; }
        public int[] reg_list { get; set; }
        public int[] ch_list { get; set; }
        public int[] pow_list { get; set; }
        public int[] saw_list { get; set; }
        public string[] PB14 { get; set; }
        public string[] PB15 { get; set; }
        int powerLevel;
        public Ch(int noisePower)
        {
            powerLevel = noisePower;
            PB14 = new string[] { "setGPIOoutPin B 14 1", "setGPIOoutPin B 14 0", "setGPIOoutPin B 14 0" };
            PB15 = new string[] { "setGPIOoutPin B 15 0", "setGPIOoutPin B 15 1", "setGPIOoutPin B 15 0" };

            menu_list = new string[33];
            reg_list = new int[33];
            ch_list = new int[33];
            pow_list = new int[33];
            saw_list = new int[33];

            menu_list[0] = "EU, CH0, 100 kbit/s";
            reg_list[0] = 0;
            ch_list[0] = 0;
            pow_list[0] = powerLevel;
            saw_list[0] = 0;
            menu_list[1] = "EU, CH1, 40 kbit/s"; ;
            reg_list[1] = 0;
            ch_list[1] = 1;
            pow_list[1] = powerLevel;
            saw_list[1] = 0;
            menu_list[2] = "EU, CH2, 9.6 kbit/s"; ;
            reg_list[2] = 0;
            ch_list[2] = 2;
            pow_list[2] = powerLevel;
            saw_list[2] = 0;
            menu_list[3] = "US, CH0, 100 kbit/s"; ;
            reg_list[3] = 1;
            ch_list[3] = 0;
            pow_list[3] = powerLevel;
            saw_list[3] = 1;
            menu_list[4] = "US, CH1, 40 kbit/s"; ;
            reg_list[4] = 1;
            ch_list[4] = 1;
            pow_list[4] = powerLevel;
            saw_list[4] = 1;
            menu_list[5] = "US, CH2, 9.6 kbit/s";
            reg_list[5] = 1;
            ch_list[5] = 2;
            pow_list[5] = powerLevel;
            saw_list[5] = 1;
            menu_list[6] = "ANZ, CH0, 100 kbit/s";
            reg_list[6] = 2;
            ch_list[6] = 0;
            pow_list[6] = powerLevel;
            saw_list[6] = 2;
            menu_list[7] = "ANZ, CH1, 40 kbit/s";
            reg_list[7] = 2;
            ch_list[7] = 1;
            pow_list[7] = powerLevel;
            saw_list[7] = 2;
            menu_list[8] = "ANZ, CH2, 9.6 kbit/s";
            reg_list[8] = 2;
            ch_list[8] = 2;
            pow_list[8] = powerLevel;
            saw_list[8] = 2;
            menu_list[9] = "HK, CH0, 100 kbit/s";
            reg_list[9] = 3;
            ch_list[9] = 0;
            pow_list[9] = powerLevel;
            saw_list[9] = 2;
            menu_list[10] = "HK, CH1, 40 kbit/s";
            reg_list[10] = 3;
            ch_list[10] = 1;
            pow_list[10] = powerLevel;
            saw_list[10] = 2;
            menu_list[11] = "HK, CH2, 9.6 kbit/s";
            reg_list[11] = 3;
            ch_list[11] = 2;
            pow_list[11] = powerLevel;
            saw_list[11] = 2;
            menu_list[12] = "MY, CH0, 100 kbit/s";
            reg_list[12] = 4;
            ch_list[12] = 0;
            pow_list[12] = powerLevel;
            saw_list[12] = 0;
            menu_list[13] = "MY, CH1, 40 kbit/s";
            reg_list[13] = 4;
            ch_list[13] = 1;
            pow_list[13] = powerLevel;
            saw_list[13] = 0;
            menu_list[14] = "MY, CH2, 9.6 kbit/s";
            reg_list[14] = 4;
            ch_list[14] = 2;
            pow_list[14] = powerLevel;
            saw_list[14] = 0;
            menu_list[15] = "IN, CH0, 100 kbit/s";
            reg_list[15] = 5;
            ch_list[15] = 0;
            pow_list[15] = powerLevel;
            saw_list[15] = 0;
            menu_list[16] = "IN, CH1, 40 kbit/s";
            reg_list[16] = 5;
            ch_list[16] = 1;
            pow_list[16] = powerLevel;
            saw_list[16] = 0;
            menu_list[17] = "IN, CH2, 9.6 kbit/s";
            reg_list[17] = 5;
            ch_list[17] = 2;
            pow_list[17] = powerLevel;
            saw_list[17] = 0;
            menu_list[18] = "JP, CH0, 100 kbit/s";
            reg_list[18] = 6;
            ch_list[18] = 0;
            pow_list[18] = powerLevel;
            saw_list[18] = 2;
            menu_list[19] = "JP, CH1, 100 kbit/s";
            reg_list[19] = 6;
            ch_list[19] = 1;
            pow_list[19] = powerLevel;
            saw_list[19] = 2;
            menu_list[20] = "JP, CH2, 100 kbit/s";
            reg_list[20] = 6;
            ch_list[20] = 2;
            pow_list[20] = powerLevel;
            saw_list[20] = 2;
            menu_list[21] = "RU, CH0, 100 kbit/s";
            reg_list[21] = 7;
            ch_list[21] = 0;
            pow_list[21] = powerLevel;
            saw_list[21] = 0;
            menu_list[22] = "RU, CH1, 40 kbit/s";
            reg_list[22] = 7;
            ch_list[22] = 1;
            pow_list[22] = powerLevel;
            saw_list[22] = 0;
            menu_list[23] = "RU, CH2, 9.6 kbit/s";
            reg_list[23] = 7;
            ch_list[23] = 2;
            pow_list[23] = powerLevel;
            saw_list[23] = 0;
            menu_list[24] = "IL, CH0, 100 kbit/s";
            reg_list[24] = 8;
            ch_list[24] = 0;
            pow_list[24] = powerLevel;
            saw_list[24] = 1;
            menu_list[25] = "IL, CH1, 40 kbit/s";
            reg_list[25] = 8;
            ch_list[25] = 1;
            pow_list[25] = powerLevel;
            saw_list[25] = 1;
            menu_list[26] = "IL, CH2, 9.6 kbit/s";
            reg_list[26] = 8;
            ch_list[26] = 2;
            pow_list[26] = powerLevel;
            saw_list[26] = 1;
            menu_list[27] = "KR, CH0, 100 kbit/s";
            reg_list[27] = 9;
            ch_list[27] = 0;
            pow_list[27] = powerLevel;
            saw_list[27] = 2;
            menu_list[28] = "KR, CH1, 100 kbit/s";
            reg_list[28] = 9;
            ch_list[28] = 1;
            pow_list[28] = powerLevel;
            saw_list[28] = 2;
            menu_list[29] = "KR, CH2, 100 kbit/s";
            reg_list[29] = 9;
            ch_list[29] = 2;
            pow_list[29] = powerLevel;
            saw_list[29] = 2;
            menu_list[30] = "CN, CH0, 100 kbit/s";
            reg_list[30] = 10;
            ch_list[30] = 0;
            pow_list[30] = powerLevel;
            saw_list[30] = 0;
            menu_list[31] = "CN, CH1, 40 kbit/s";
            reg_list[31] = 10;
            ch_list[31] = 1;
            pow_list[31] = powerLevel;
            saw_list[31] = 0;
            menu_list[32] = "CN, CH2, 9.6 kbit/s";
            reg_list[32] = 10;
            ch_list[32] = 2;
            pow_list[32] = powerLevel;
            saw_list[32] = 0;
        }

        internal Mc[] GetForFrequency(string frequency, sbyte p1)
        {
            Mc[] mcArray = null;
            if (frequency == "EU")
            {
                mcArray = new[]
                {
                    new Mc(0, -1, p1),
                    new Mc(1, -1, p1),
                    new Mc(0, -1, p1),
                };
            }
            else if (frequency == "US")
            {
                mcArray = new[]
                {
                    new Mc(3, -1, p1),
                    new Mc(4, -1, p1),
                    new Mc(3, -1, p1),
                };
            }
            else if (frequency == "ANZ")
            {
                mcArray = new[]
                {
                    new Mc(6, -1, p1),
                    new Mc(7, -1, p1),
                    new Mc(6, -1, p1),
                };
            }
            else if (frequency == "JP")
            {
                mcArray = new[]
                {
                    new Mc(18, 19, p1),
                    new Mc(18, 20, p1),
                    new Mc(19, 20, p1),
                    new Mc(18, 19, p1),
                };
            }
            else if (frequency == "KR")
            {
                mcArray = new[]
                {
                    new Mc(27, 28, p1),
                    new Mc(27, 29, p1),
                    new Mc(28, 29, p1),
                    new Mc(27, 28, p1),
                };
            }
            else
            {
                mcArray = new[]
                {
                    new Mc(-1, -1, p1),
                };
            }
            return mcArray;
        }
    }
}