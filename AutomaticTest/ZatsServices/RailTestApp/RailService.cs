// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Utils.UI.Enums;
using ZWave.TextApplication;

namespace Zats.Services
{
    public class RailService : ServiceBase
    {
        private const int DEFAULT_TM_MS = 500;
        private DeviceManager DeviceManager { get; set; }

        private Dictionary<Alias, DebugToken.Void> _listenTokens = new Dictionary<Alias, DebugToken.Void>();
        private StringBuilder _listenData = new StringBuilder();

        public RailService(DeviceManager deviceManager)
        {
            DeviceManager = deviceManager;
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            DeviceManager.Log(text, logLevel);
        }

        private TextResult.Request RequestLine(Alias deviceNumber, string data, string expectData, int timeoutMs)
        {
            var device = DeviceManager.GetDeviceHostClient(deviceNumber).SClient as TextDevice;
            var res = device.RequestLine(data, expectData, timeoutMs);
            return new TextActionResult.Request(res);
        }

        private DebugToken.Void Listen(Alias deviceNumber, Action<string> listenCallback)
        {
            var device = DeviceManager.GetDeviceHostClient(deviceNumber).SClient as TextDevice;
            var result = device.Listen(listenCallback, null);
            return new DebugToken.Void(result);
        }

        public void StartListen(Alias deviceNumber)
        {
            if (!_listenTokens.ContainsKey(deviceNumber))
            {
                var lTkn = Listen(deviceNumber,
                    (x) =>
                    {
                        _listenData.Append(x);
                    });
                _listenTokens.Add(deviceNumber, lTkn);
            };
        }

        public string[] StopListen(Alias deviceNumber)
        {
            var ret = new string[0];
            if (_listenTokens.ContainsKey(deviceNumber))
            {
                //listenTokens[deviceNumber].SetCompleted();
                //listenTokens[deviceNumber].SetCompletedSignal();
                //var listenRes = listenTokens[deviceNumber].WaitCompletedSignal();
                //or:
                var device = DeviceManager.GetDeviceHostClient(deviceNumber).SClient as TextDevice;
                device.Cancel(_listenTokens[deviceNumber].Token);

                _listenTokens.Remove(deviceNumber);
            }
            if (_listenData.Length > 0)
            {
                var railOut = _listenData.ToString().Split(Environment.NewLine.ToArray(), StringSplitOptions.RemoveEmptyEntries);
                if (railOut.Length > 0)
                {
                    ret = railOut;
                }
            }
            _listenData.Clear();
            return ret;
        }

        public string[] ParsetOutput(string data)
        {
            var ret = new string[0];
            if (!string.IsNullOrEmpty(data))
            {
                var output = data
                    .Replace("\r", "").Replace("\n", "")
                    .Split('{', '}')
                    .Where(x => !string.IsNullOrEmpty(x)) //, StringSplitOptions.RemoveEmptyEntries
                    .ToArray();
                ret = output; //.TakeWhile(x => !x.Contains(RailTestCommands.DEVICE_READY)).ToArray(); //optional
                //var rest = output.SkipWhile(x => x.Contains(RailTestCommands.DEVICE_READY)).ToArray(); //optional
            }
            return ret;
        }

        /// <summary>
        /// Perform a reboot of the chip.
        /// </summary>
        public TextResult.Request Reset(Alias deviceNumber)
        {
            var res = RequestLine(deviceNumber, RailCommands.RESET, $"{{({RailCommands.RESET})}}", DEFAULT_TM_MS);
            return res;
        }

        /// <summary>
        /// Setup control receive mode.
        /// </summary>
        /// <param name="IsEnable">Enable/Disable receiver mode </param>
        public TextResult.Request SetRx(Alias deviceNumber, bool IsEnable)
        {
            var cmd = $"{RailCommands.RX} {(IsEnable ? "1" : "0")}";
            var res = RequestLine(deviceNumber, cmd, $"({RailCommands.RX})", DEFAULT_TM_MS);
            return res;
        }

        /// <summary>
        /// Show/Control Z-Wave mode.
        /// </summary>
        /// <param name="IsEnable">Enable/Disable Z-Wave Mode</param>
        /// <param name="optionsBitmask">optionsBitmask (by default = 3)</param>
        public TextResult.Request ControlZWaveMode(Alias deviceNumber, bool IsEnable, byte optionsBitmask = 3)
        {
            var cmd = $"{RailCommands.SET_ZWAVE_MODE} {(IsEnable ? "1" : "0")} {optionsBitmask}";
            var res = RequestLine(deviceNumber, cmd, $"({RailCommands.SET_ZWAVE_MODE})", DEFAULT_TM_MS);
            return res;
        }

        /// <summary>
        /// Set how much data to load into the TX FIFO for transmitting.
        /// Actual packet length may vary based on radio configuration.
        /// </summary>
        /// <param name="lenght">Bytes lenght</param>
        public TextResult.Request SetTxLength(Alias deviceNumber, int length)
        {
            var cmd = $"{RailCommands.SET_TX_LENGTH} {length}";
            var res = RequestLine(deviceNumber, cmd, $"({RailCommands.SET_TX_LENGTH})", DEFAULT_TM_MS);
            return res;
        }

        /// <summary>
        /// Set TX packet payload bytes for future transmits.
        /// </summary>
        /// <param name="offset">offset</param>
        /// <param name="values">set of values to put after offset</param>
        public TextResult.Request SetTxPayload(Alias deviceNumber, ushort offset, byte[] values)
        {
            var values_str = string.Concat(values.Select(x => x.ToString() + " "));
            var cmd = $"{RailCommands.SET_TX_PAYLOAD} {offset} {values_str}";
            var res = RequestLine(deviceNumber, cmd, $"({RailCommands.SET_TX_PAYLOAD})", DEFAULT_TM_MS);
            return res;
        }

        public TextResult.Request SetTxPayload(Alias deviceNumber, byte[] payload)
        {
            TextResult.Request res = null;
            int len = 18;
            for (int i = 0; i < ((payload.Length / len) + 1); i++)
            {
                if (i * len < payload.Length)
                {
                    var rest = ((i + 1) * len) < payload.Length ? len : payload.Length - (i * len);
                    res = SetTxPayload(deviceNumber, (ushort)(i * len), payload.Skip(i * len).Take(rest).ToArray());
                }
            }
            return res;
        }

        /// <summary>
        /// Set the Z-Wave region.
        /// </summary>
        /// <param name="zWaveRegion">Z-Wave region</param>
        public TextResult.Request SetZWaveRegion(Alias deviceNumber, RailZWaveRegions zWaveRegion)
        {
            var cmd = $"{RailCommands.SET_ZWAVE_REGION} {(byte)zWaveRegion}";
            var res = RequestLine(deviceNumber, cmd, $"({RailCommands.SET_ZWAVE_REGION})", DEFAULT_TM_MS);
            return res;
        }
        public TextResult.Request SetZWaveRegion(Alias deviceNumber, string frequency)
        {
            return SetZWaveRegion(deviceNumber, ConvertToRailZWaveRegion(frequency));
        }

        /// <summary>
        /// Set the transmit power. The radio must be IDLE.
        /// </summary>
        /// <param name="power">deci-dBm unless 'raw' is added, is a number ranging from 0 to 155</param>
        public TextResult.Request SetPower(Alias deviceNumber, int power)
        {
            //[int32] power: deci-dBm unless 'raw' is added
            //[stringopt] 'raw'=units are raw power level
            var cmd = $"{RailCommands.SET_POWER} {power} raw";
            var res = RequestLine(deviceNumber, cmd, $"({RailCommands.SET_POWER})", DEFAULT_TM_MS);
            return res;
        }

        /// <summary>
        /// Set the radio channel.
        /// </summary>
        /// <param name="channel">Radio Channel</param>
        public TextResult.Request SetChannel(Alias deviceNumber, RailChannels channel)
        {
            //[uint16] channel
            var cmd = $"{RailCommands.SET_CHANNEL} {(short)channel}";
            var res = RequestLine(deviceNumber, cmd, $"({RailCommands.SET_CHANNEL})", DEFAULT_TM_MS);
            return res;
        }

        /// <summary>
        /// Control tone transmission.
        /// Send a tone at the carrier frequency.
        /// </summary>
        /// <param name="IsEnable">Enable/Disable CW (Carrier Wave: RF single tone)</param>
        public TextResult.Request SetTxTone(Alias deviceNumber, bool IsEnable/*, int antenna*/)
        {
            //[uint32] 0=Disable 1=Enable
            //[uint32opt] antenna: [0]/1
            var cmd = $"{RailCommands.SET_TX_TONE} {(IsEnable ? "1" : "0")}"; // {antenna}
            var res = RequestLine(deviceNumber, cmd, $"({RailCommands.SET_TX_TONE})", DEFAULT_TM_MS);
            return res;
        }

        /// <summary>
        /// Set interval
        /// </summary>
        /// <param name="intervalMs"></param>
        public TextResult.Request SetTxDelay(Alias deviceNumber, int intervalMs)
        {
            //[uint32] number of packets, 0=continuous until next 'tx 0'
            var cmd = $"{RailCommands.SET_TX_DELAY} {intervalMs}";
            var res = RequestLine(deviceNumber, cmd, $"({RailCommands.SET_TX_DELAY})", DEFAULT_TM_MS);
            return res;
        }

        /// <summary>
        /// Transmit packets with current TX options.
        /// </summary>
        /// <param name="packetsNumber">number of packets, 0=continuous until next 'tx 0' (Start endless transmission of frames)</param>
        public TextResult.Request SetTx(Alias deviceNumber, int packetsNumber)
        {
            //[uint32] number of packets, 0=continuous until next 'tx 0'
            var cmd = $"{RailCommands.TX} {packetsNumber}";
            var res = RequestLine(deviceNumber, cmd, $"({RailCommands.TX})", DEFAULT_TM_MS);
            return res;
        }

        /// <summary>
        /// Configure Multi-Channel Settings
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="isJpOrKr"></param>
        /// <returns></returns>
        public TextResult.Request ConfigRXChannelHopping(Alias deviceNumber, RailZWaveRegions zWaveRegion)
        {
            //'configRXChannelHopping x1 x2 x3 x4 x5 x6 x7 x8 x9 x10 x11 x12� ;; Ch setup
            //Table 4. Multi-Channel Settings:
            var multiChannelSettings = "0 3 270 0 1 3 450 0 2 3 560 0";
            if (zWaveRegion == RailZWaveRegions.KR || zWaveRegion == RailZWaveRegions.JP) // region == 6|9 '3ch'
            {
                multiChannelSettings = "0 3 270 0 1 3 270 0 2 3 270 0";
            }
            var cmd = $"{RailCommands.CONFIG_RX_CHANNEL_HOPPING} {multiChannelSettings}";
            var res = RequestLine(deviceNumber, cmd, $"({RailCommands.CONFIG_RX_CHANNEL_HOPPING})", DEFAULT_TM_MS);
            return res;
        }

        public TextResult.Request ConfigRXChannelHopping(Alias deviceNumber, string frequency)
        {
            return ConfigRXChannelHopping(deviceNumber, ConvertToRailZWaveRegion(frequency));
        }

        /// <summary>
        ///  Enable multi scanning mode.
        /// </summary>
        /// <param name="IsEnable">Multi scanning mode state</param>
        public TextResult.Request EnableRxChannelHopping(Alias deviceNumber, bool IsEnable)
        {
            //[uint8] 0=Disable [1=Enable]
            var cmd = $"{RailCommands.ENABLE_RX_CHANNEL_HOPPING} {(IsEnable ? "1" : "0")}";
            var res = RequestLine(deviceNumber, cmd, $"({RailCommands.ENABLE_RX_CHANNEL_HOPPING})", DEFAULT_TM_MS);
            return res;
        }


        #region Device Usage Methods: sets of commands

        public void DeviceSetup(Alias railTestDevice, string frequency, byte payloadLenght, int power, RailChannels channel)
        {
            SetRx(railTestDevice, false);
            ControlZWaveMode(railTestDevice, true);
            SetTxLength(railTestDevice, payloadLenght);
            SetZWaveRegion(railTestDevice, frequency);
            SetPower(railTestDevice, power);
            SetChannel(railTestDevice, channel);
        }

        #endregion

        #region Tool methods:

        private RailZWaveRegions ConvertToRailZWaveRegion(string frequency)
        {
            var ret = RailZWaveRegions.EU; //Default
            if (frequency == RfRegions.US_LR.ToFrequency())
            {
                ret = RailZWaveRegions.LR_1;
            }
            else if (frequency == RfRegions.EU_LR.ToFrequency())
            {
                ret = RailZWaveRegions.LR_1;
            }
            {
                foreach (var item in typeof(RailZWaveRegions).GetEnumValues())
                {
                    if (item.ToString().Equals(frequency))
                    {
                        ret = (RailZWaveRegions)item;
                        break;
                    }
                }
            }
            return ret;
        }

        public byte GetFrameProperties1(HeadTypesMPDU ht = HeadTypesMPDU.Singlecast, bool speedModified = false, bool lowPower = false, bool ack = true, bool routed = false)
        {
            var ret = (byte)
                (
                    (((byte)ht) & 0x0F)
                    + (speedModified ? (1 << 4) : 0)
                    + (lowPower ? (1 << 5) : 0)
                    + (ack ? (1 << 6) : 0)
                    + (routed ? (1 << 7) : 0)
                );
            return ret;
        }

        public byte[] PrepareFrame(byte[] homeId, Node src, Node dst, byte[] payload, RailChannels railChannel = RailChannels.Ch0, byte prop1 = 0, byte prop2 = 0)
        {
            var frame = new List<byte>();
            frame.AddRange(homeId);
            var lenIx = 0;
            if (railChannel == RailChannels.Ch3)
            {
                var s1 = new byte[] { (byte)(src.Id >> 8), (byte)src.Id };
                var d1 = new byte[] { (byte)(dst.Id >> 8), (byte)dst.Id };

                frame.Add(s1[0]);
                frame.Add((byte)((s1[1] << 4) | (d1[0] & 0x0F)));
                frame.Add(d1[1]);

                lenIx = frame.Count();
                frame.Add(0);

                frame.Add(prop1);
                frame.Add(prop2); //SeqNo

                frame.Add(0x00); //noise floor
                frame.Add(0x00); //Tx power
                frame.Add(0x00); //prop3
                //no ext: frame.Add(0x00); 
            }
            else
            {
                if ((prop1 & 0x0F) == (byte)HeadTypesMPDU.Explorer)
                {
                    frame.Add((byte)src.Id);
                    frame.Add(prop1);
                    frame.Add(prop2); //SeqNo
                    lenIx = frame.Count();
                    frame.Add(0); // length updated later
                    frame.Add((byte)dst.Id);
                    frame.Add(0x21); // prop3
                    frame.Add(0x00); // prop4
                    frame.Add(0xFA); // Session Tx Random Interval
                    frame.Add(0x40); // prop5
                    frame.Add(0x00); // repeater 0
                    frame.Add(0x00); // repeater 1
                    frame.Add(0x00); // repeater 2
                    frame.Add(0x00); // repeater 3
                    frame.AddRange(new byte[4]); // Network Home Id
                }
                else
                {
                    frame.Add((byte)src.Id);
                    frame.Add(prop1);
                    frame.Add(prop2); //SeqNo
                    lenIx = frame.Count();
                    frame.Add(0);
                    frame.Add((byte)dst.Id);
                }
            }
            if (payload != null && payload.Length > 0)
            {
                frame.AddRange(payload);
            }
            if (railChannel == RailChannels.Ch0)
            {
                frame.AddRange(new byte[] { 0x00, 0x00 }); //extra CRC bytes 
            }
            else
            {
                frame.AddRange(new byte[] { 0x00 }); //extra CRC bytes
            }
            frame[lenIx] = (byte)frame.Count();
            return frame.ToArray();
        }

        #endregion
    }
}
