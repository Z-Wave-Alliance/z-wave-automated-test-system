// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Utils.UI.Enums;
using UicApplication.Devices;
using System.Linq;
using System;
using ZWave;
using ZWave.BasicApplication.Devices;
using System.Collections.Generic;
using ZWave.UicApplication;
using System.Threading;

namespace Zats.Services
{
    public class UicAsyncService : ServiceBase, IUicAsyncService
    {
        private UicManager _uicManager { get; set; }
        private DeviceManager _deviceManager { get; set; }
        protected override void Log(string text, LogLevels logLevel)
        {
            _deviceManager.Log(text, logLevel);
        }
        public UicAsyncService(UicManager uicManager, DeviceManager deviceManager)
        {
            _uicManager = uicManager;
            _deviceManager = deviceManager;
        }
        public UicToken.ExpectData ExpectData(Alias uicDeviceNumber, string topic, int timeoutMs)
        {
            return new UicToken.ExpectData(_uicManager.mainUiClient.ExpectData(topic, timeoutMs, null));
        }
        public UicToken.ExpectData ExpectData(Alias uicDeviceNumber, string topic, int timeoutMs, int count)
        {
            return new UicToken.ExpectData(_uicManager.mainUiClient.ExpectData(topic, timeoutMs, count, null));
        }
        public UicToken.ExpectData ExpectDataByUnId(Alias uicDeviceNumber, string topic, int timeoutMs)
        {
            string unid = _uicManager.GetClient(uicDeviceNumber);
            return new UicToken.ExpectData(_uicManager.mainUiClient.ExpectDataByUNID(unid, topic, timeoutMs, null));
        }
        public UicToken.ExpectData ExpectDataByUnIdAndEp(Alias uicDeviceNumber, string topic, string ep, int timeoutMs)
        {
            string unid = _uicManager.GetClient(uicDeviceNumber);
            return new UicToken.ExpectData(_uicManager.mainUiClient.ExpectDataByUNIDAndEp(unid,
                topic, ep, timeoutMs, null));
        }
        public UicToken.ExpectData ExpectDataByMqttClient(string mqttclient, string topic)
        {            
            return new UicToken.ExpectData(_uicManager.mainUiClient.ExpectDataByMqttClient(mqttclient,
                topic, null));
        }
        public UicToken.ExpectData ExpectDataByEp(string topic, string ep, int timeoutMs)
        {
            return new UicToken.ExpectData(_uicManager.mainUiClient.ExpectDataByEp(topic, ep, timeoutMs, null));
        }
        public UicToken.ExpectData ExpectDataByUnIdAndEp(Alias uicDeviceNumber, string topic, string ep, int timeoutMs,
            int count)
        {
            string unid = _uicManager.GetClient(uicDeviceNumber);
            return new UicToken.ExpectData(_uicManager.mainUiClient.ExpectDataByUNIDAndEp(unid,
                topic, ep, timeoutMs, count, null));
        }
        public UicToken.ListenData ListenData(Alias uicDeviceNumber, string topic, int timeoutMs)
        {
            string unid = _uicManager.GetClient(uicDeviceNumber);
            return new UicToken.ListenData(_uicManager.mainUiClient.ListenData(null, topic, timeoutMs));
        }
        public UicToken.ExpectData ExpectDataByMachineId(string machineId, string topic)
        {
            return new UicToken.ExpectData(_uicManager.mainUiClient.ExpectDataByMachineId(machineId,
                topic,null));
        }
        public UicToken.Inclusion IncludeNode(Alias uicDeviceNumber, byte[] dsk, int timeoutMs)
        {
            if (!_uicManager.mainUiClient.IsSubscribedToTopic(new UicApplication.Clusters.SmartStart.List.Update()))
            {
                ActionToken subscribeToken =
                    _uicManager.mainUiClient.Subscribe(new UicApplication.Clusters.SmartStart.List.Update(), null);
                subscribeToken.WaitCompletedSignal();
            }

            string deviceDsk_str = "";
            for (int indi = 0; indi < dsk.Length; indi = indi + 2)
            {
                deviceDsk_str += BitConverter.ToUInt16(new byte[] { dsk[indi],
                dsk[indi+1]}.Reverse().ToArray(), 0).ToString() + "-";
            }
            deviceDsk_str = deviceDsk_str.TrimEnd('-');

            UicApplication.Clusters.SmartStart.List.Payload payload = new UicApplication.Clusters.SmartStart.List.Payload()
            {
                DSK = deviceDsk_str,
                Include = true,
                ProtocolControllerUnid = "",
                Unid = "",
                PreferredProtocols = new string[] { "Z-Wave Long Range", "Z-Wave" }
                //PreferredProtocols = new string[] { "Z-Wave" }
            };

            return new UicToken.Inclusion(_uicManager.mainUiClient.IncludeNode(payload, timeoutMs, null));
        }
        public UicToken.Inclusion IncludeGenericNode(Alias uicDeviceNumber, string dsk, int timeoutMs)
        {
            if (!_uicManager.mainUiClient.IsSubscribedToTopic(new UicApplication.Clusters.SmartStart.List.Update()))
            {
                ActionToken subscribeToken =
                    _uicManager.mainUiClient.Subscribe(new UicApplication.Clusters.SmartStart.List.Update(), null);
                subscribeToken.WaitCompletedSignal();
            }

            UicApplication.Clusters.SmartStart.List.Payload payload = new UicApplication.Clusters.SmartStart.List.Payload()
            {
                DSK = dsk,
                Include = true,
                ProtocolControllerUnid = "",
                Unid = "",
            };

            return new UicToken.Inclusion(_uicManager.mainUiClient.IncludeNode(payload, timeoutMs, null));
        }
        public UicToken.Exclusion ExcludeNode(Alias uicDeviceNumber, Alias zwaveEndDevice, int timeoutMs)
        {
            if (!_uicManager.mainUiClient.IsSubscribedToTopic(new UicApplication.Clusters.ProtocolController.NetworkManagement()))
            {
                ActionToken subscribeToken =
                    _uicManager.mainUiClient.Subscribe(new UicApplication.Clusters.ProtocolController.NetworkManagement(), null);
                subscribeToken.WaitCompletedSignal();
            }
            string unid = _uicManager.mainUiClient.UnId;

            List<string> payloadList = new List<string>();

            return new UicToken.Exclusion(_uicManager.mainUiClient.ExcludNode(unid, payloadList, timeoutMs, null));
        }
        public UicResult.Inclusion HandlePinRequest(Alias zwaveEndDevice, int timeoutMs)
        {
            Device node = _deviceManager.GetDevice(zwaveEndDevice);
            byte[] dsk = _deviceManager.GetDSK(zwaveEndDevice, 16);
            string deviceDsk_str = "";

            deviceDsk_str += BitConverter.ToUInt16(new byte[] { dsk[0],
            dsk[1]}.Reverse().ToArray(), 0).ToString();

            Console.WriteLine("Using DSK: " + deviceDsk_str);

            UicResult.Inclusion ret = null;

            var pinReq = new UicToken.ExpectData(
                    _uicManager.mainUiClient.ExpectData(new UicApplication.Clusters.ProtocolController.NetworkManagement(),
                        timeoutMs, null));
            if (pinReq.WaitCompletedSignal())
            {
                Thread.Sleep(100);
                var addToken = _uicManager.mainUiClient.InputPin(deviceDsk_str, timeoutMs, null);
                InclusionResult res_pin = (InclusionResult)addToken.WaitCompletedSignal();
                ret = new UicResult.Inclusion(res_pin);
            }
            else
            {
                ret = new UicResult.Inclusion(new InclusionResult());
            }
            return ret;
        }
    }
}

