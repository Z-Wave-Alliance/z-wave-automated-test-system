// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;
using Utils.Threading;
using Utils.UI.Enums;
using ZWave;
using ZWave.Security;
using ZWave.Xml.Application;
using ZWave.ZnifferApplication;
using ZWave.ZnifferApplication.Devices;
using ZWave.ZnifferApplication.Operations;

namespace Zats.Services
{
    public class ZnifferService : ServiceBase, IZnifferService
    {
        internal ZnifferManager ZnifferManager { get; private set; }
        internal DeviceManager DeviceManager { get; private set; }
        internal DataItemParser DataItemParser { get; }
        internal SecurityManager SecurityManager { get { return DataItemParser.SecurityManager; } }
        private DataItem mWatchPrev = null;

        public ZnifferService(ZnifferManager znifferManager, DeviceManager deviceManager)
        {
            ZnifferManager = znifferManager;
            DeviceManager = deviceManager;
            DataItemParser = new DataItemParser();
        }

        public ServiceSettings Settings
        {
            get
            {
                return ZnifferManager.Settings;
            }
            set
            {
                ZnifferManager.Settings = value;
            }
        }

        /// <summary>
        /// Extract PTI device-client from <see>DeviceManager</see> by Alias
        /// Gets ZClinet if was enabled in <see cref="IService.EnableRadioPTI(Alias, bool)"/>
        /// </summary>
        /// <param name="znifferDevice">Device Alias.</param>
        /// <returns>Port Client for PTI supported device.</returns>
        private SnifferPti GetPTIClient(Alias znifferDevice)
        {
            var deviceClientHost = DeviceManager.GetDeviceHostClient(znifferDevice);
            var ptiClient = (SnifferPti)(deviceClientHost.ZClient ?? deviceClientHost.SClient);
            return ptiClient;
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            ZnifferManager.Log(text, logLevel);
        }

        public bool IsConnected()
        {
            return ZnifferManager.ZnifferAppClient?.TransportClient?.IsOpen ?? false;
        }

        public ZnifferToken.ExpectData ExpectData(int timeoutMs)
        {
            ActionToken token = ZnifferManager.Zniffer.ExpectData(null, null, OnDataItemReceived, timeoutMs, null);
            ZnifferToken.ExpectData ret = new ZnifferToken.ExpectData(token, SecurityManager);
            return ret;
        }

        public ZnifferToken.ExpectData ExpectData(Alias znifferDevice, int timeoutMs)
        {
            var ptiClient = GetPTIClient(znifferDevice);
            ActionToken token = ptiClient.ExpectData(null, null, OnDataItemReceived, timeoutMs, null);
            ZnifferToken.ExpectData ret = new ZnifferToken.ExpectData(token, SecurityManager);
            return ret;
        }

        public ZnifferToken.CollectData CollectData()
        {
            return CollectData(0);
        }

        public ZnifferToken.CollectData CollectData(Alias znifferDevice)
        {
            return CollectData(znifferDevice, 0);
        }

        public ZnifferToken.CollectData CollectData(int timeoutMs)
        {
            ActionToken token = ZnifferManager.Zniffer.CollectData(null, null, OnDataItemReceived, timeoutMs, null);
            ZnifferToken.CollectData ret = new ZnifferToken.CollectData(token, SecurityManager);
            return ret;
        }

        public ZnifferToken.CollectData CollectData(Alias znifferDevice, int timeoutMs)
        {
            var ptiClient = GetPTIClient(znifferDevice);
            ActionToken token = ptiClient.CollectData(null, null, OnDataItemReceived, timeoutMs, null);
            ZnifferToken.CollectData ret = new ZnifferToken.CollectData(token, SecurityManager);
            return ret;
        }

        /// <summary>
        /// Cancells async operation
        /// </summary>
        /// <param name="token">token of the started async operation</param>
        public void CancelToken(_ActionToken token)
        {
            ZnifferManager.ZnifferAppClient.Cancel(token.Token);
        }

        private bool OnDataItemReceived(ActionToken token, DataItem dataItem)
        {
            bool ret = false;
            bool isCollecting = false;
            ConcurrentList<FilterObject> FilterObjects;
            if (token.CustomObject as ConcurrentList<FilterObject> != null)
            {
                isCollecting = token.ParentType == typeof(CollectDataOperation);
                FilterObjects = token.CustomObject as ConcurrentList<FilterObject>;

                // first check only not completed filters (where filteredCount is less then expected)
                for (int i = 0; i < FilterObjects.Count; i++)
                {
                    var currentFilterObject = FilterObjects[i];
                    if (currentFilterObject.FilteredCount < currentFilterObject.ExpectedFramesCount)
                    {
                        if (FilterDataItem(dataItem, currentFilterObject))
                        {
                            ret = true;
                            break;
                        }
                    }
                }

                // if not found then check completed filters again
                if (!ret)
                {
                    for (int i = 0; i < FilterObjects.Count; i++)
                    {
                        var currentFilterObject = FilterObjects[i];
                        if (FilterDataItem(dataItem, currentFilterObject))
                        {
                            ret = true;
                            break;
                        }
                    }
                }
                ReportToConsole(dataItem, ret);
            }
            return ret;
        }

        private bool FilterDataItem(DataItem dataItem, FilterObject currentFilterObject)
        {
            bool ret = false;
            if (currentFilterObject.FilterIt(dataItem, SecurityManager))
            {
                currentFilterObject.FramesCount++;
                if (mWatchPrev != null && dataItem.CreatedAt > mWatchPrev.CreatedAt)
                {
                    dataItem.Delta = (int)(dataItem.CreatedAt - mWatchPrev.CreatedAt).TotalMilliseconds;
                }
                ret = true;
            }
            return ret;
        }

        private void ReportToConsole(DataItem dataItem, bool isExpected)
        {
            if (isExpected)
            {
                var cipherBlock = dataItem.Decrypt(SecurityManager);
                if (!dataItem.IsWakeupBeam)
                {
                    if (mWatchPrev != null && mWatchPrev.IsWakeupBeam)
                    {
                        ReportToConsole(mWatchPrev, cipherBlock, isExpected);
                    }
                    ReportToConsole(dataItem, cipherBlock, isExpected);
                }
                else if (mWatchPrev == null)
                {
                    ReportToConsole(dataItem, cipherBlock, isExpected);
                }
                mWatchPrev = dataItem;
            }
            else
            {
                if (Settings.IsDiagnosticsEnabled)
                {
                    var cipherBlock = dataItem.Decrypt(SecurityManager);
                    ReportToConsole(dataItem, cipherBlock, isExpected);
                }
            }
        }

        private void ReportToConsole(DataItem dataFrame, CipherDataItem cipherDataItem, bool isExpected)
        {
            LogLevels ll = isExpected ? LogLevels.ZnifferTrace : LogLevels.Diagnostic;
            var mWatchSource = dataFrame.Source;
            byte[] mWatchHomeId = dataFrame.HomeId;
            byte[] mWatchMessageBuffer = dataFrame.DataBuffer;
            byte[] mWatchPayload = cipherDataItem.Payload ?? dataFrame.LastPayload;
            byte[] mWatchRepeaters = dataFrame.Repeaters ?? new byte[0];
            var mWatchDestination = dataFrame.Destination;
            byte mWatchSpeed = dataFrame.Speed;
            bool mWatchIsLowPower = dataFrame.IsLow;
            byte mWatchHeaderType = dataFrame.HeaderType;
            string mHeaderString = GetTextFromHeaderType(mWatchHeaderType, mWatchIsLowPower, mWatchRepeaters);
            string mCommandString = string.Empty;
            var mWatchDelta = dataFrame.Delta;
            if (dataFrame.LastPayload != null && dataFrame.LastPayload.Length > 0)
            {
                if (dataFrame.LastPayload.Length == 1 && dataFrame.LastPayload[0] == 0)
                {
                    mCommandString = "NOP";
                }
                else
                {
                    mCommandString = GetTextFromCommandData(mWatchPayload);
                }
                if (cipherDataItem.IsDecrypted)
                {
                    mCommandString += ".S" + dataFrame.GetCipherVersion();
                    if (cipherDataItem.S2Extensions != null)
                    {
                        var ext = cipherDataItem.S2Extensions.ExtensionsList.Union(cipherDataItem.S2Extensions.EncryptedExtensionsList).
                            Select(x => ((ExtensionTypes)x.properties1.type).ToString());
                        if (ext.Any())
                        {
                            mCommandString += "+" + ext.Aggregate((x, y) => x + "," + y);
                        }
                    }
                }
                Log(string.Format("{4,7} {0} {1,3} {2,3} {0} {3,11} {0} {5}",
                    "|",
                    mWatchSource,
                    mWatchDestination,
                    mHeaderString,
                    mWatchDelta,
                    mCommandString), ll);
            }
            else if (dataFrame.IsWakeupBeam)
            {
                Log(string.Format("{4,7} {0} {1,3} {2,3} {0} {3,11} {0} ({5})",
                    "|",
                    mWatchSource,
                    mWatchDestination,
                    mHeaderString,
                    mWatchDelta,
                    dataFrame.WakeupCounter), ll);
            }
            else
            {
                Log(string.Format("{4,7} {0} {1,3} {2,3} {0} {3,11} {0}",
                    "|",
                    mWatchSource,
                    mWatchDestination,
                    mHeaderString,
                    mWatchDelta), ll);
            }
        }

        public void SetSecurityNetworkKey(byte[] networkKey)
        {
            SetSecurityNetworkKey(networkKey, false);
        }

        public void SetSecurityNetworkKey(byte[] networkKey, bool isTemp)
        {
            if (networkKey != null && networkKey.Length == 16)
            {
                if (isTemp)
                {
                    if (SecurityManager.AddNewTempSecurityKey(networkKey))
                    {
                        Log($"Zniffer TEMP: {networkKey.GetHex()}", LogLevels.Information);
                    }
                }
                else
                {
                    if (SecurityManager.AddNewSecurityKey(networkKey))
                    {
                        Log($"Zniffer NETW: {networkKey.GetHex()}", LogLevels.Information);
                    }
                }
            }
        }

        public void SetSecurityNetworkKeyS2(byte[] networkKey)
        {
            SetSecurityNetworkKey(networkKey, false);
        }

        public void SetSecuritySupport(bool isSecure)
        {
        }

        private byte GetCommandClassKey(string cmdClass)
        {
            CommandClass commandClass = ZnifferManager.ZWaveDefinition.CommandClasses.
                   FirstOrDefault(x => x.Name == cmdClass);
            return commandClass.KeyId;
        }

        private byte GetCommandKey(string cmdClass, string cmd)
        {
            foreach (var commandClass in ZnifferManager.ZWaveDefinition.CommandClasses.Where(x => x.Name == cmdClass))
            {
                foreach (var command in commandClass.Command.
                     Where(x => x.Name == cmd))
                {
                    return command.KeyId;
                }
            }
            throw new ApplicationException();
        }

        private string GetTextFromHeaderType(byte headerType, bool isLowPower, byte[] repeaters)
        {
            StringBuilder sb = new StringBuilder();
            if (isLowPower)
            {
                sb.Append("L.");
            }
            sb.Append(((HeaderTypesShortEx)headerType).ToString());
            if (repeaters.Any())
            {
                sb.Append(":");
                sb.Append(repeaters.DefaultIfEmpty().Select(x => x.ToString()).Aggregate((a, b) => a + "," + b));
            }
            return sb.ToString();
        }

        private string GetTextFromCommandData(byte[] data)
        {
            StringBuilder sb = new StringBuilder();
            if (data.Length > 0)
            {
                CommandClass cmdClass = ZnifferManager.ZWaveDefinition.CommandClasses.FirstOrDefault(x => x.KeyId == data[0]);
                if (cmdClass != null)
                {
                    if (data.Length > 1 && cmdClass.Command != null)
                    {
                        byte mask = 0xFF;
                        if (data[0] == COMMAND_CLASS_TRANSPORT_SERVICE_V2.ID)
                        {
                            mask = 0xF8;
                        }
                        Command cmd = cmdClass.Command.FirstOrDefault(x => x.KeyId == (data[1] & mask));
                        if (cmd != null)
                        {
                            sb.AppendFormat("{0}", cmd.Name);
                        }
                        else
                        {
                            sb.AppendFormat("{0}.Cmd=0x{1:X2}", cmdClass.Name, data[1]);
                        }
                    }
                    else
                    {
                        sb.AppendFormat(cmdClass.Name);
                    }
                }
                else
                {
                    sb.AppendFormat("CmdClass=0x{0:X2}", data[0]);
                    if (data.Length > 1)
                    {
                        sb.AppendFormat(".Cmd=0x{0:X2}", data[1]);
                    }
                }
            }
            return sb.ToString();
        }

        private Dictionary<DataItemParser, SnifferPti> _ptiSniffers = new Dictionary<DataItemParser, SnifferPti>();
        public void RestartListeners()
        {
            SecurityManager.Reset();
            if (ZnifferManager != null && ZnifferManager.Zniffer != null)
            {
                var listenToken = ZnifferManager.Zniffer.RestartListening(
                    (token, dataItem) =>
                    {
                        DataItemParser.Parse(dataItem);
                        return false;
                    });
                ZnifferToken.ExpectData ret = new ZnifferToken.ExpectData(listenToken, SecurityManager);
                ret.SetUpWatch(240, 240, null, null, false);
            }
            else
            {
                "Check Zniffer Settings - RestartListener"._DLOG();
            }

            // Start all available PTI Sniffers.
            if (_ptiSniffers.Count == 0)
            {
                _ptiSniffers = DeviceManager.Devices.Values.Where(client => client.SClient is SnifferPti).
                    Select(client => client.SClient).
                    Cast<SnifferPti>().
                    ToDictionary(spti => new DataItemParser());
            }

            foreach (var pair in _ptiSniffers)
            {
                var listenToken = pair.Value.RestartListening(
                    (token, dataItem) =>
                    {
                        pair.Key.Parse(dataItem);
                        return false;
                    });
                ZnifferToken.ExpectData ret = new ZnifferToken.ExpectData(listenToken, SecurityManager);
                ret.SetUpWatch(240, 240, null, null, false);
            }
        }
    }
}
