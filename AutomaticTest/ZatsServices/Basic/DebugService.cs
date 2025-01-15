// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZWave.TextApplication;
using Zats.Services;
using System.Threading;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using Utils.UI.Enums;

namespace Zats.Services
{
    public class DebugService : ServiceBase, IDebugService
    {
        private DebugDeviceManager DebugDeviceManager { get; set; }
        private DeviceManager DeviceManager { get; set; }

        public DebugService(DebugDeviceManager debugDeviceManager, DeviceManager deviceManager)
        {
            DebugDeviceManager = debugDeviceManager;
            DeviceManager = deviceManager;
        }

        public ServiceSettings Settings
        {
            get
            {
                return DebugDeviceManager.Settings;
            }
            set
            {
                DebugDeviceManager.Settings = value;
            }
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            DebugDeviceManager.Log(text, logLevel);
        }

        public void Send(Alias deviceNumber, dChannel channel, dCommand data)
        {
            Send(deviceNumber, string.Format("{0} {1}", channel.Channel, data.Command));
        }

        public void Send(Alias deviceNumber, string data)
        {
            TextDevice device = DebugDeviceManager.GetDevice(deviceNumber);
            try
            {
                device.Send(data);
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("Debug interface doesn't work :(");
            }
        }

        public DebugResult.Request Request(Alias deviceNumber, dChannel channel, dCommand data, string parameter, int timeoutMs)
        {
            return Request(deviceNumber, string.Format("{0} {1} {2}", channel.Channel, data.Command, parameter), timeoutMs);
        }

        public DebugToken.Receive Receive(Alias deviceNumber, int timeOut)
        {
            var device = DebugDeviceManager.GetDevice(deviceNumber);
            var result = device.ReceiveData(timeOut, null);
            return new DebugToken.Receive( result );
        }

        public DebugToken.Void Listen(Alias deviceNumber, Action<string> listenCallback)
        {
            var device = DebugDeviceManager.GetDevice(deviceNumber);
            var result = device.Listen(listenCallback, null);
            return new DebugToken.Void(result);
        }

        public void CancelToken(Alias deviceNumber, _ActionToken token)
        {
            var device = DebugDeviceManager.GetDevice(deviceNumber);
            device.Cancel(token.Token);
        }

        public DebugResult.Request Request(Alias deviceNumber, dChannel channel, dCommand data, int timeoutMs)
        {
            return Request(deviceNumber, string.Format("{0} {1}", channel.Channel, data.Command), timeoutMs);
        }

        public DebugResult.Request Request(Alias deviceNumber, dCommand data, int timeoutMs)
        {
            return Request(deviceNumber, string.Format("{0}", data.Command), timeoutMs);
        }

        public DebugResult.Request Request(Alias deviceNumber, string data, int timeoutMs)
        {
            TextDevice device = DebugDeviceManager.GetDevice(deviceNumber);
            try
            {
                return new DebugActionResult.Request(device.Request(data, timeoutMs));
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("Debug interface doesn't work :(");
                return null;
            }
        }



        /* Return type is STUDENT: */

        public ProductPlusAppIds InitDUT(Alias debugDeviceNumber)
        {
            ProductPlusAppIds applId = 0;
            // Enable TestInterface on DUT. Send "\r\" until DUT return "1".
            for (int i = 0; i < 100; i++)
            {
                var reqTestResult = Request(debugDeviceNumber, D_CH.a, D_CMD.ZW, 500);
                if (reqTestResult.ReceivedText.Contains("ZW"))
                {
                    int firmId = 0;
                    var FirmareId = "Firmware ID: ";
                    int indexStart = reqTestResult.ReceivedText.IndexOf(FirmareId); // index of begining of value
                    indexStart += FirmareId.Length;
                    int indexEnd = reqTestResult.ReceivedText.IndexOf("\r", indexStart); //index of end of value

                    string hexValue = reqTestResult.ReceivedText.Substring(indexStart, indexEnd - indexStart); // hex value
                    firmId = Convert.ToInt32(hexValue, 16); //parse to int
                    ProductTypeIds t1 = (ProductTypeIds)(byte)(firmId >> 8);
                    applId = (ProductPlusAppIds)(byte)firmId;
                    break;
                }
            }
            if (0 != applId)
            {
                // Send Reset cmd
                Send(debugDeviceNumber, D_CH.a, D_CMD.Reset);
                Thread.Sleep(2000);

                // Enable TestInterface on DUT. Send "\r\" until DUT return "1".
                for (int i = 0; i < 100; i++)
                {
                    var reqTestResult = Request(debugDeviceNumber, D_CH.a, D_CMD.ZW, 500);
                    if (reqTestResult.ReceivedText.Contains("ZW"))
                    {
                        break;
                    }
                }
            }
            return applId;
        }


        public BasicResult.AddNode AddDUT(Alias Primary, Alias debugDeviceNumber, Modes mode)
        {
            Modes modeFx = mode == Modes.NodeAnyS2 ? Modes.NodeAny : mode;
            // Start AddNode
            Controller controller = (Controller)DeviceManager.GetDevice(Primary);
            DeviceManager.DisableSmartStart(Primary);
            var addToken = controller.AddNodeToNetwork((ZWave.Enums.Modes)modeFx, 65000, (x) => DeviceManager.EnableSmartStart(Primary));
            // Wait LearnReady callback
            controller.WaitNodeStatusSignal(ZWave.BasicApplication.Enums.NodeStatuses.LearnReady, 10000);
            // Send LearnModeStart cmd
            Send(debugDeviceNumber, D_CH.a, D_CMD.LearnModeStart);
            // Wait AddNode completed
            var addRes = (AddRemoveNodeResult)addToken.WaitCompletedSignal();
            return new BasicActionResult.AddNode(addRes);
        }

        public BasicResult.RemoveNode RemoveDUT(Alias Primary, Alias debugDeviceNumber, Modes mode)
        {
            Modes modeFx = mode == Modes.NodeAnyS2 ? Modes.NodeAny : mode;
            // Start AddNode
            Controller controller = (Controller)DeviceManager.GetDevice(Primary);
            DeviceManager.DisableSmartStart(Primary);
            var addToken = controller.RemoveNodeFromNetwork((ZWave.Enums.Modes)modeFx, 65000, (x) => DeviceManager.EnableSmartStart(Primary));
            // Wait LearnReady callback
            controller.WaitNodeStatusSignal(ZWave.BasicApplication.Enums.NodeStatuses.LearnReady, 10000);
            // Send LearnModeStart cmd
            Send(debugDeviceNumber, D_CH.a, D_CMD.LearnModeStart);
            // Wait AddNode completed
            var addRes = (AddRemoveNodeResult)addToken.WaitCompletedSignal();
            return new BasicActionResult.RemoveNode(addRes);
        }
    }








}
