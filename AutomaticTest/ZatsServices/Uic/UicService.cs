using System;
using System.Linq;
using Utils.UI.Enums;
using UicApplication.Devices;
using UicApplication.Operations;
using ZWave.BasicApplication.Devices;
using ZWave;
using System.Threading;
using ZWave.UicApplication;
using System.Collections.Generic;
using Newtonsoft.Json;
using ZWave.UicApplication.PayloadWrappers;
using ZWave.UicApplication.PayloadWrappers.Enums;
using UicApplication.Clusters;
using System.IO;
using System.Reflection;

namespace Zats.Services
{
    public class UicService : ServiceBase, IUicSerice
    {
        private UicManager _uicManager;
        private DeviceManager _deviceManager;

        private const int STANDARD_OPERATION_TIMEOUT_MS = 60000;

        public UicService(UicManager uicManager, DeviceManager deviceManager)
        {
            _uicManager = uicManager;
            _deviceManager = deviceManager;
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            _deviceManager.Log(text, logLevel);
        }

        public void RegisterUicDevice(Alias virtualDeviceNumber, string unID)
        {
            _uicManager.NewVirtualDevice(virtualDeviceNumber, unID);
        }

        public void UnRegisterUicDevice(Alias virtualDeviceNumber)
        {
            _uicManager.FinalizeVirtualDevice(virtualDeviceNumber);
        }

        public void SetDefault(Alias uicDeviceNumber, int timeoutMs)
        {
            if (_uicManager.UseMqttConnection)
            {
                string unid = _uicManager.mainUiClient.UnId;
                var retData = _uicManager.mainUiClient.SetDefault(unid, timeoutMs);
                _uicManager.mainUiClient.UnId = retData.UnId;
                _uicManager.NewVirtualDevice(uicDeviceNumber, retData.UnId);
                List<string> dsk = GetSmartStartListEntries(10000);
                if (dsk.Count > 0)
                {
                    DeleteSmartStartListEntries(dsk, 10000);
                }
                Thread.Sleep(1000);
                //UicApplication.Clusters.SmartStart.List topic = new UicApplication.Clusters.SmartStart.List();
                //_uicManager.mainUiClient.Publish(topic, "[]", null);
            }
            else
            {
                ActionToken setDefaultToken = _uicManager.mainUiClient.RequestPlainData(null, "UIC_SetDefault", null, null, timeoutMs, null);
                var res = (RequestDataResult)setDefaultToken.WaitCompletedSignal();
                setDefaultToken.Close();

                var getUnidToken = _uicManager.mainUiClient.RequestPlainData(null, "UIC_Get_Unid", null, "UIC_Get_Unid_Status", 15000, null);
                res = (RequestDataResult)getUnidToken.WaitCompletedSignal();
                getUnidToken.Close();
                if (res.mqttReceivedPayload != null)
                {
                    var getUnidPayloadWrapper = JsonConvert.DeserializeObject<GetUnidPayloadWrapper>(res.mqttReceivedPayload);
                    _uicManager.mainUiClient.UnId = getUnidPayloadWrapper.Unid;
                }

            }
        }

        public void SetIdle(Alias uicDeviceNumber, int timeoutMs)
        {
            string unid = _uicManager.mainUiClient.UnId;
            var retData = _uicManager.mainUiClient.SetIdle(unid, timeoutMs);
            Thread.Sleep(1000);
        }

        public void DeleteRetainedTopics(Alias uicDeviceNumber)
        {
            //Slightly arbitrary to expect a maximum count of 10000 retained messages, but I don't have any other quick workarounds...
            var topicsExpRes = new UicToken.ListenData(_uicManager.mainUiClient.ListenData(null, "ucl", 5000));
            _uicManager.mainUiClient.Subscribe("ucl/#", null);
            var topicsRes = topicsExpRes.WaitCompletedSignal();
            foreach (var topic in topicsRes.mqttMssg)
            {
                _uicManager.mainUiClient.Publish(topic.topic, null, true, null);
                Thread.Sleep(100);
            }
        }

        #region Publish

        public void Publish(string topic, string payload)
        {
            _uicManager.mainUiClient.Publish(topic, payload, null);
        }

        public void Publish(string topic, string payload, bool isRetain)
        {
            _uicManager.mainUiClient.Publish(topic, payload, isRetain, null);
        }

        public void Publish(Alias uicDeviceNumber, string topic, string payload)
        {
            _uicManager.mainUiClient.Publish(topic, payload, null);
        }

        public void PublishByUNID(Alias uicDeviceNumber, string topic, string payload)
        {
            string unid = "";
            if (uicDeviceNumber.Value == 1)
            {
                unid = _uicManager.mainUiClient.UnId;
            }
            else
            {
                unid = _uicManager.GetClient(uicDeviceNumber);
            }
            _uicManager.mainUiClient.PublishByUNID(unid, topic, payload, null);
        }

        public void PublishByUNIDandEp(Alias uicDeviceNumber, string topic, string payload, string ep)
        {
            string unid = _uicManager.GetClient(uicDeviceNumber);
            _uicManager.mainUiClient.PublishByUNIDandEndPoint(unid, topic, payload, ep, null);
        }

        public void PublishByGroup(Alias uicDeviceNumber, string topic, string payload, int group)
        {
            string unid = _uicManager.GetClient(uicDeviceNumber);
            _uicManager.mainUiClient.PublishByGroup(unid, topic, payload, group, null);
        }

        public void PublishByMachineId(string machineId, string topic, string payload)
        {
            _uicManager.mainUiClient.PublishByMachineId(machineId, topic, payload, null);
        }
        #endregion

        #region Subscribe
        public void Subscribe(Alias uicDeviceNumber, string topic)
        {
            _uicManager.mainUiClient.Subscribe(topic, null);
        }

        public void SubscribeToAllUNID(Alias uicDeviceNumber, string topic)
        {
            _uicManager.mainUiClient.SubscribeByUNID(topic, "+", null);
        }

        public void SubscribeToAllUNIDandEp(string topic)
        {
            _uicManager.mainUiClient.SubscribeByUNIDandEndPoint(topic, "+", "+", null);
        }

        public void SubscribeByUNID(Alias uicDeviceNumber, string topic)
        {
            string unid = _uicManager.GetClient(uicDeviceNumber);
            _uicManager.mainUiClient.SubscribeByUNID(topic, unid, null);
        }

        public void SubscribeByUNIDandEndPoint(Alias uicDeviceNumber, string topic, string endPoint)
        {
            string unid = _uicManager.GetClient(uicDeviceNumber);
            _uicManager.mainUiClient.SubscribeByUNIDandEndPoint(topic, unid, endPoint, null);
        }
        #endregion

        #region Inclusion

        public void AddToSmartStartList(Alias zwaveEndDevice)
        {
            Device node = _deviceManager.GetDevice(zwaveEndDevice);
            byte[] dsk = _deviceManager.GetDSK(zwaveEndDevice, 16);
            string deviceDsk_str = "";
            for (int indi = 0; indi < dsk.Length; indi = indi + 2)
            {
                deviceDsk_str += BitConverter.ToUInt16(new byte[] { dsk[indi],
                dsk[indi+1]}.Reverse().ToArray(), 0).ToString() + "-";
            }
            deviceDsk_str = deviceDsk_str.TrimEnd('-');

            SmartStart.List.Payload payload = new SmartStart.List.Payload()
            {
                DSK = deviceDsk_str,
                Include = true,
                ProtocolControllerUnid = "",
                Unid = "",
                PreferredProtocols = new string[] { "Z-Wave Long Range", "Z-Wave" }
            };

            _uicManager.mainUiClient.Publish(new SmartStart.List.Update(), payload, null);
        }

        public UicResult.Inclusion IncludeNode(Alias uicDeviceNumber, Alias zwaveEndDevice, int timeoutMs, bool isWaitForInterview = false)
        {
            if (_uicManager.UseMqttConnection)
            {
                Device node = _deviceManager.GetDevice(zwaveEndDevice);
                byte[] dsk = _deviceManager.GetDSK(zwaveEndDevice, 16);
                string deviceDsk_str = "";
                for (int indi = 0; indi < dsk.Length; indi = indi + 2)
                {
                    deviceDsk_str += BitConverter.ToUInt16(new byte[] { dsk[indi],
                dsk[indi+1]}.Reverse().ToArray(), 0).ToString() + "-";
                }
                deviceDsk_str = deviceDsk_str.TrimEnd('-');

                SmartStart.List.Payload payload = new SmartStart.List.Payload()
                {
                    DSK = deviceDsk_str,
                    Include = true,
                    ProtocolControllerUnid = "",
                    Unid = "",
                    PreferredProtocols = new string[] { "Z-Wave Long Range", "Z-Wave" }
                };

                var groupsToken = new UicToken.ExpectData(
                    _uicManager.mainUiClient.ExpectData(new UicApplication.Clusters.GroupsCluster.ATT_NameSupport.NameSupport_SupportCommands(),
                        timeoutMs, null));
                var onOffToken = new UicToken.ExpectData(
                    _uicManager.mainUiClient.ExpectData(new UicApplication.Clusters.OnOffCluster.ATT_OnOff.OnOff_SupportCommands(), timeoutMs, null));

                ActionToken addToken = _uicManager.mainUiClient.IncludeNode(payload, timeoutMs, null);
                Thread.Sleep(500);
                //Check that the entry was added to the provisioning list        
                bool isFound = false;
                for (int indi = 0; indi < 8; indi++)
                {
                    var entries = GetSmartStartListEntries(timeoutMs);
                    if (entries.Count > 0)
                    {
                        foreach (UicApplication.Clusters.SmartStart.List.Payload entry in entries)
                        {
                            if (entry.DSK == payload.DSK)
                            {
                                isFound = true;
                            }
                        }
                    }
                    if (!isFound)
                    {
                        Thread.Sleep(1000);
                        Publish(new SmartStart.List.Update(), payload);
                    }
                }
                ZWave.BasicApplication.Operations.SetLearnModeResult learnRes = node.SetLearnMode(
                    ZWave.Enums.LearnModes.LearnModeSmartStart | ZWave.Enums.LearnModes.NetworkMask,
                    timeoutMs);

                InclusionResult res = (InclusionResult)addToken.WaitCompletedSignal();
                UicResult.Inclusion ret = new UicResult.Inclusion(res);
                addToken.Close();

                // Interview process is finished when inclusion signal is received or expired.
                // Expire interview tokens to prevent additional delays.

                //In Case the test needs the interview result, we should not cancel the tokens ahead of time
                if(isWaitForInterview == false)
                {
                    _uicManager.mainUiClient.Cancel(groupsToken.Token);
                    _uicManager.mainUiClient.Cancel(onOffToken.Token);
                }

                // groupsToken.Token.ExpireDateTime = DateTime.Now;
                // onOffToken.Token.ExpireDateTime = DateTime.Now;                

                if (res)
                {
                    ret.NodeInterviewResult = new UicResult.NodeInterviewResult();
                    var groupsRes = groupsToken.WaitCompletedSignal();
                    if (groupsRes)
                    {
                        GroupsCluster.Payload_Att_SupportedCommands newGroupsSupportCMD = groupsRes.ReceivedPayload;
                        ret.NodeInterviewResult.GroupsInformation = new UicResult.GroupsInformation()
                        {
                            SupportedCommands = newGroupsSupportCMD._payload.value
                        };
                    }
                    var onOffRes = onOffToken.WaitCompletedSignal();
                    if (onOffRes)
                    {
                        OnOffCluster.Payload_Att_SupportedCommands newSupportCMD = onOffRes.ReceivedPayload;

                        ret.NodeInterviewResult.OnOffInformation = new UicResult.OnOffInformation()
                        {
                            SupportedCommands = newSupportCMD._payload.value
                        };
                    }
                }

                return ret;
            }
            else
            {
                UicResult.Inclusion ret = UICPair(zwaveEndDevice, timeoutMs);
                return ret;
            }
        }

        private UicResult.Inclusion UICPair(Alias zwaveEndDevice, int timeoutMs)
        {
            Device node = _deviceManager.GetDevice(zwaveEndDevice);
            byte[] dsk = _deviceManager.GetDSK(zwaveEndDevice, 16);
            string deviceDsk_str = "";
            for (int indi = 0; indi < dsk.Length; indi = indi + 2)
            {
                deviceDsk_str += BitConverter.ToUInt16(new byte[] { dsk[indi],
                dsk[indi+1]}.Reverse().ToArray(), 0).ToString() + "-";
            }
            deviceDsk_str = deviceDsk_str.TrimEnd('-');

            var payloadJson = JsonConvert.SerializeObject(new PairPayloadWrapper
            {
                DeviceDsk = deviceDsk_str
            });


            ActionToken addToken = _uicManager.mainUiClient.RequestPlainData(_uicManager.mainUiClient.UnId, "UIC_Pair", payloadJson, "UIC_Pair_Status", timeoutMs, null);
            Thread.Sleep(500);
            ZWave.BasicApplication.Operations.SetLearnModeResult learnRes = node.SetLearnMode(
                ZWave.Enums.LearnModes.LearnModeSmartStart | ZWave.Enums.LearnModes.NetworkMask,
                timeoutMs);
            var res = (RequestDataResult)addToken.WaitCompletedSignal();
            addToken.Close();
            UicResult.Inclusion ret = null;
            if (res.mqttReceivedPayload != null && res.mqttReceivedPayload != "null")
            {
                var pairStatusPayloadWrapper = JsonConvert.DeserializeObject<PairStatusPayloadWrapper>(res.mqttReceivedPayload);
                bool isStateCompleted = pairStatusPayloadWrapper.Status == ZWave.UicApplication.PayloadWrappers.Enums.PairStatus.Successful;
                ret = new UicResult.Inclusion(isStateCompleted);
                ret.UnId = pairStatusPayloadWrapper.Unid;
            }
            else
            {
                ret = new UicResult.Inclusion(false);
            }
            return ret;
        }

        public UicResult.Inclusion ClassicIncludeNode(Alias uicDeviceNumber, Alias zwaveEndDevice, int timeoutMs)
        {
            Device node = _deviceManager.GetDevice(zwaveEndDevice);
            byte[] dsk = _deviceManager.GetDSK(zwaveEndDevice, 16);
            string deviceDsk_str = "";

            deviceDsk_str += BitConverter.ToUInt16(new byte[] { dsk[0],
            dsk[1]}.Reverse().ToArray(), 0).ToString();

            Console.WriteLine("Using DSK: " + deviceDsk_str);

            ActionToken addToken = _uicManager.mainUiClient.IncludeNodeClassic(timeoutMs, null);
            Thread.Sleep(1000);
            ActionToken token = node.SetLearnMode(ZWave.Enums.LearnModes.LearnModeClassic, false, timeoutMs, null);
            InclusionResult res = (InclusionResult)addToken.WaitCompletedSignal();
            UicResult.Inclusion ret = null;
            if (res.IsRequestingPing)
            {
                addToken.Close();
                Thread.Sleep(100);
                addToken = _uicManager.mainUiClient.InputPin(deviceDsk_str, timeoutMs, null);
                InclusionResult res_pin = (InclusionResult)addToken.WaitCompletedSignal();
                ret = new UicResult.Inclusion(res_pin);
                token.WaitCompletedSignal();
            }
            else
            {
                addToken.Close();
                ret = new UicResult.Inclusion(res);
                token.WaitCompletedSignal();
            }

            return ret;
        }

        public UicResult.Inclusion ClassicIncludeForFw(Alias uicDeviceNumber, byte[] deviceDSK, int timeoutMs)
        {            
            byte[] dsk = deviceDSK;
            string deviceDsk_str = "";

            deviceDsk_str += BitConverter.ToUInt16(new byte[] { dsk[0],
            dsk[1]}.Reverse().ToArray(), 0).ToString();

            Console.WriteLine("Using DSK: " + deviceDsk_str);

            ActionToken addToken = _uicManager.mainUiClient.IncludeNodeClassic(timeoutMs, null);
            Thread.Sleep(1000);

            InclusionResult res = (InclusionResult)addToken.WaitCompletedSignal();
            UicResult.Inclusion ret = null;
            if (res.IsRequestingPing)
            {
                addToken.Close();
                Thread.Sleep(100);
                addToken = _uicManager.mainUiClient.InputPin(deviceDsk_str, timeoutMs, null);
                InclusionResult res_pin = (InclusionResult)addToken.WaitCompletedSignal();
                ret = new UicResult.Inclusion(res_pin);

            }
            else
            {
                addToken.Close();
                ret = new UicResult.Inclusion(res);

            }

            return ret;
        }
        #endregion

        #region Exclusion
        public UicResult.Exclusion ExcludeNode(Alias uicDeviceNumber, Alias zwaveEndDevice, int timeoutMs)
        {

            Device node = _deviceManager.GetDevice(zwaveEndDevice);
            //string networkHomeId = _uicManager.mainUiClient.UnId.Substring(0, _uicManager.mainUiClient.UnId.IndexOf("-") + 1);
            string networkHomeId = _uicManager.mainUiClient.UnId.Substring(0, _uicManager.mainUiClient.UnId.IndexOf("-") + 10);
            string removeNodeId = "9999";

            if ((int)node.Id <= 9999)
            {
                removeNodeId = ((int)node.Id).ToString().PadLeft(4, '0');
            }
            string removeUnId = networkHomeId + removeNodeId;

            if (_uicManager.UseMqttConnection)
            {
                List<string> payloadList = new List<string>();
                payloadList.Add(removeUnId);

                ActionToken addToken = _uicManager.mainUiClient.ExcludNode(_uicManager.mainUiClient.UnId,
                    payloadList, timeoutMs, null);

                Thread.Sleep(3000);
                ZWave.BasicApplication.Operations.SetLearnModeResult learnRes = node.SetLearnMode(
                    ZWave.Enums.LearnModes.LearnModeClassic,
                    timeoutMs);
                ExclusionResult res = (ExclusionResult)addToken.WaitCompletedSignal();

                UicResult.Exclusion ret = new UicResult.Exclusion(res);
                return ret;
            }
            else
            {
                return UICRemove(removeUnId, node, timeoutMs);
            }
        }
        public UicResult.Exclusion ExcludeNodeForFW(Alias uicDeviceNumber, string endDeviceNodeId, int timeoutMs)
        {
            string removeUnId = endDeviceNodeId;
                        
            List<string> payloadList = new List<string>();
            payloadList.Add(removeUnId);

            ActionToken addToken = _uicManager.mainUiClient.ExcludNode(_uicManager.mainUiClient.UnId,
                payloadList, timeoutMs, null);

            Thread.Sleep(3000);
               
            ExclusionResult res = (ExclusionResult)addToken.WaitCompletedSignal();

            UicResult.Exclusion ret = new UicResult.Exclusion(res);
            return ret;
            
        }

        private UicResult.Exclusion UICRemove(string removeUnId, Device zwaveEndNode, int timeoutMs)
        {
            var payloadJson = JsonConvert.SerializeObject(new RemovePayloadWrapper
            {
                Unid = removeUnId
            });


            ActionToken removeToken = _uicManager.mainUiClient.RequestPlainData("", "UIC_Remove", payloadJson, "UIC_Remove_Status", timeoutMs, null);
            Thread.Sleep(3000);
            ZWave.BasicApplication.Operations.SetLearnModeResult learnRes = zwaveEndNode.SetLearnMode(
                ZWave.Enums.LearnModes.LearnModeClassic,
                timeoutMs);
            var res = (RequestDataResult)removeToken.WaitCompletedSignal();
            removeToken.Close();
            UicResult.Exclusion ret = null;
            if (res.mqttReceivedPayload != null && res.mqttReceivedPayload != "null")
            {
                var removeStatusPayloadWrapper = JsonConvert.DeserializeObject<RemoveStatusPayloadWrapper>(res.mqttReceivedPayload);
                bool isStateCompleted = removeStatusPayloadWrapper.Status == ZWave.UicApplication.PayloadWrappers.Enums.RemoveStatus.Successful;
                ret = new UicResult.Exclusion(isStateCompleted);
            }
            else
            {
                ret = new UicResult.Exclusion(false);
            }
            return ret;
        }
        #endregion

        #region Failed Remove

        public UicResult.Exclusion FailedRemove(Alias zwaveEndDevice, int timeoutMs)
        {
            Device node = _deviceManager.GetDevice(zwaveEndDevice);
            string removeUnId = _uicManager.GetClient(zwaveEndDevice);

            ActionToken rmToken = _uicManager.mainUiClient.FailedRemove(_uicManager.mainUiClient.UnId,
                removeUnId, timeoutMs, null);

            Thread.Sleep(3000);
            ExclusionResult res = (ExclusionResult)rmToken.WaitCompletedSignal();

            UicResult.Exclusion ret = new UicResult.Exclusion(res);
            return ret;
        }

        #endregion

        #region SmartStart

        public List<string> GetSmartStartListEntries(int timeoutMs)
        {
            List<string> entries = new List<string>();
            UicApplication.Clusters.SmartStart.List topic = new UicApplication.Clusters.SmartStart.List();
            var uicExpRes = new UicToken.ExpectData(_uicManager.mainUiClient.ExpectData(topic, timeoutMs, null));
            _uicManager.mainUiClient.Subscribe(topic, null);
            if (uicExpRes.WaitCompletedSignal())
            {
                UicApplication.Clusters.SmartStart.List.SmartStartListPayload payload = new SmartStart.List.SmartStartListPayload();
                string payloadContainer = uicExpRes.WaitCompletedSignal().ReceivedPayload;
                payload = payloadContainer;
                if (payload.value.SmartStartList != null)
                {
                    foreach (SmartStart.List.SmartStartList data in payload.value.SmartStartList)
                    {
                        entries.Add(data.DSK);
                    }
                }
            }
            return entries;
        }

        public void DeleteSmartStartListEntries(List<string> entries, int timeoutMs)
        {
            UicApplication.Clusters.SmartStart.List.Remove topic = new UicApplication.Clusters.SmartStart.List.Remove();
            UicApplication.Clusters.SmartStart.List.Remove.Payload payload = new UicApplication.Clusters.SmartStart.List.Remove.Payload();
            foreach (string data in entries)
            {
                payload.DSK = data;
                _uicManager.mainUiClient.Publish(topic, payload, null);
                Thread.Sleep(500);
            }

        }

        #endregion

        #region FirmwareUpdate
        public void CreateLocalJson(Root JsonData)
        {
            string output = Newtonsoft.Json.JsonConvert.SerializeObject(JsonData, Newtonsoft.Json.Formatting.Indented);

            File.WriteAllText("images.json", output);
        }

        public void UpdateHomeIdToJson(string NewHomeId)
        {
            Console.WriteLine("End device Home ID: " + NewHomeId);

            //Read file to string
            string jsonfile = File.ReadAllText("images.json");

            //Deserialize from file to object:
            var root = new Root();
            JsonConvert.PopulateObject(jsonfile, root);

            //Change Value
            root.Images[0].Unid = new List<string> { NewHomeId };
            string output = Newtonsoft.Json.JsonConvert.SerializeObject(root, Newtonsoft.Json.Formatting.Indented);

            //Serialize JSON directly to a file again
            File.WriteAllText("images.json", output);

            ////Serialize JSON directly to a file again
            //using (StreamWriter file = File.CreateText(@"images.json"))
            //{
            //    JsonSerializer serializer = new JsonSerializer();
            //    serializer.Serialize(file, root);
            //}
        }

        public Root setRootFile(string DeviceHomeId)
        {
            Root newJsonFile = new Root();
            List<Image> newJsonImage = new List<Image>();

            newJsonImage = SetImagesData(DeviceHomeId);
            newJsonFile.Images = newJsonImage;
            newJsonFile.Version = @"1";
            return newJsonFile;
        }
        private List<Image> SetImagesData(string jsonUnid)
        {
            List<Image> imageData = new List<Image>();
            Image myImageData = new Image();
            myImageData.FileName = @"ZW_SwitchOnOff_7.16.3_297_ZGM130S_REGION_IN_v255.gbl";
            myImageData.Uiid = @"ZWave-0000-0002-0004-00-01"; //Switch OnOff
            myImageData.Unid = new List<string> { jsonUnid };
            myImageData.Version = @"255.0.0";
            myImageData.ApplyAfter = System.DateTime.Today;
            myImageData.Md5 = @"3WSlmY280kBqEjH7gXkxLg==";
            imageData.Add(myImageData);

            return imageData;
        }
        #region image Json class
        public class Image
        {
            public string FileName { get; set; }
            public string Uiid { get; set; }
            public List<string> Unid { get; set; }
            public string Version { get; set; }
            public DateTime ApplyAfter { get; set; }
            public string Md5 { get; set; }
        }

        public class Root
        {
            public string Version { get; set; }
            public List<Image> Images { get; set; }
        }
        #endregion
        #endregion

        public UicResult.GetNodes GetNodes(int timeout)
        {
            return new UicResult.GetNodes(_uicManager.mainUiClient.GetNodes(timeout));
        }
        public void SetOnOff(Alias uicDeviceNumber, OnOffStatuses onOffStatus, string ep)
        {
            string unid = _uicManager.GetClient(uicDeviceNumber);
            var payloadJson = JsonConvert.SerializeObject(new SetOnOffPayloadWrapper
            {
                Unid = unid,
                Status = onOffStatus,
                Ep = ep
            });

            var setOnOffToken = _uicManager.mainUiClient.RequestPlainData("", "UIC_SetOnOff", payloadJson, "", 1000, null);
            setOnOffToken.WaitCompletedSignal();
            setOnOffToken.Close();
        }
        public string GetUnId(Alias uicDeviceNuner)
        {
            return _uicManager.GetClient(uicDeviceNuner);
        }

        #region Metrics
        public DateTime Get_PublishMetrics_ByUNIDandEp(Alias uicDeviceNumber, string topic, string payload, string ep)
        {
            string unid = _uicManager.GetClient(uicDeviceNumber);
            ActionToken tok = _uicManager.mainUiClient.PublishByUNIDandEndPoint(unid, topic, payload, ep, null);
            ActionResult res = tok.WaitCompletedSignal();
            return res.StartTimestamp;
        }

        #endregion

        public bool ConfigurePollingIntervals(Alias sshClientAlias)
        {
            string zatsConfigPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Uic\Unify configs\zwave_poll_config.yaml");
            string pollingConfigPath = "/usr/share/uic/zwave_poll_config.yaml";

            return CopyFileOverSsh(sshClientAlias, zatsConfigPath, pollingConfigPath);
        }

        public bool CopyFileOverSsh(Alias sshClientAlias, string sourcePath, string targetPath)
        {
            bool ret = false;
            if (!File.Exists(sourcePath))
                return ret;

            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            sshClient.CommandTimeoutMs = 15000;
            var connectResult = sshClient.Connect();

            if (connectResult != ZWave.Enums.CommunicationStatuses.Done)
                return ret;

            using (var fileStream = new FileStream(sourcePath, FileMode.Open))
            {
                ret = sshClient.WriteFileStream(fileStream, targetPath, true);
            }

            return ret;
        }

        public bool RestartRpi(Alias sshClientAlias)
        {
            bool ret = false;
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            sshClient.CommandTimeoutMs = 15000;
            var connectResult = sshClient.Connect();

            if (connectResult != ZWave.Enums.CommunicationStatuses.Done)
                return ret;

            var result = sshClient.Send("sudo reboot");
            if (!sshClient.IsSshClientConnected)
            {
                Thread.Sleep(60000);
                connectResult = sshClient.Connect();
                if (connectResult == ZWave.Enums.CommunicationStatuses.Done)
                {
                    ret = true;
                }
            }
            return ret;
        }
        public string SendCustomSshCommand(Alias sshClientAlias, string commandToSend)
        {
            return _uicManager.GetSshClient(sshClientAlias).Send(commandToSend);
        }

        #region Common Bridge Functions

        public void StopAndCleanMatterBridgeLogs(Alias sshClientAlias)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            sshClient.Send("sudo systemctl stop uic-zpc.service");
            sshClient.Send("sudo systemctl stop uic-matter-bridge.service");
            sshClient.Send("sudo rm /tmp/chip_*");
            sshClient.Send("sudo rm /var/chip_*");
        }

        public string StartMatterBridgeLogs(Alias sshClientAlias)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            sshClient.Send("sudo journalctl --flush --rotate");
            sshClient.Send("sudo journalctl --vacuum-time=1s");
            Thread.Sleep(2000);//We need to give time for the log to clear
            sshClient.Send("sudo systemctl start uic-zpc.service");            
            sshClient.Send("sudo systemctl start uic-matter-bridge.service");
            Thread.Sleep(2000);//We need to give time for the log to populate
            var res = sshClient.Send("sudo journalctl -u uic-matter-bridge.service");
            string[] lines = res.Split('\n');
            List<string> qrCode = new List<string>();       

            foreach (string line in lines)
            {
                if (line.Contains("SetupQRCode"))
                {
                    //Split the line by [
                    string[] half = line.Split('[');
                    qrCode.Add( half[4].TrimEnd(']') );
                    break;
                }
            }
            return qrCode.Last();
        }

        public void PairBridge(Alias sshClientAlias, string qrcode)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            sshClient.Send("sudo /usr/bin/chip-tool pairing code 2222 " + qrcode);
        }

        public string ReadPartsList(Alias sshClientAlias)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            var res = sshClient.Send("sudo /usr/bin/chip-tool descriptor read parts-list 2222 0");
            var lines = res.Split('\n');
            var partNo = "";
            foreach (string line in lines)
            {
                if (line.Contains("PartsList: "))
                {
                    partNo = line[line.Length - 9].ToString();
                    break;
                }
            }
            int partNo_int = Convert.ToInt32(partNo);
            partNo_int = partNo_int + 0;//Why this you may be wondering, well the part assignment has been changing withing matter periodically
            partNo = partNo_int.ToString();
            return partNo;
        }

        #endregion

        #region Matter Bridge OnOff Functions

        public void ToogleMatterBridge(Alias sshClientAlias, string partNo)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            sshClient.Send("sudo /usr/bin/chip-tool onoff toggle 2222 " + partNo);            
        }

        public void OnMatterBridge(Alias sshClientAlias, string partNo)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            sshClient.Send("sudo /usr/bin/chip-tool onoff on 2222 " + partNo);
        }

        public void OffMatterBridge(Alias sshClientAlias, string partNo)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            sshClient.Send("sudo /usr/bin/chip-tool onoff off 2222 " + partNo);
        }

        public string ReadOnOffMatterBridge(Alias sshClientAlias, string partNo)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            var res = sshClient.Send("sudo /usr/bin/chip-tool onoff read on-off 2222 " + partNo);
            var lines = res.Split('\n');
            string statusOnOff = "FALSE";//OnOff is reported in MATTER as TRUE or FALSE, not to be mixed with the unify values "true","fase"
            foreach (string line in lines)
            {
                if (line.Contains("OnOff"))
                {
                    if (line.Contains("TRUE"))
                    {
                        statusOnOff = "TRUE";
                        break;
                    }
                }
            }
            return statusOnOff;
        }

        #endregion

        #region Matter Bridge Level Functions

        public void MoveToLevelMatterBridge(Alias sshClientAlias, string partNo, string level)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            sshClient.Send("sudo chip-tool levelcontrol move-to-level " + level + " 1 1 0 2222 " + partNo);
        }

        public string ReadLevelMatterBridge(Alias sshClientAlias, string partNo)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            var res = sshClient.Send("sudo chip-tool levelcontrol read current-level 2222 " + partNo);
            var lines = res.Split('\n');
            string currentLevel_str = "";
            foreach (string line in lines)
            {
                if (line.Contains("CurrentLevel: "))
                {
                    string[] line_split = line.Split(':');
                    currentLevel_str = line_split.LastOrDefault();
                    break;
                }
            }
            return currentLevel_str;
        }

        #endregion

        #region Matter Bridge Occupancy Functions

        public string ReadOccupancyMatterBridge(Alias sshClientAlias, string partNo )
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            var res = sshClient.Send("sudo chip-tool occupancysensing read occupancy 2222 " + partNo);
            var lines = res.Split('\n');
            string occupancy_status = "";
            foreach (string line in lines)
            {
                if (line.Contains("Occupancy: "))
                {
                    string[] line_split = line.Split(':');
                    occupancy_status = line_split.LastOrDefault();
                    break;
                }
            }
            return occupancy_status;
        }

        #endregion

        #region Matter Bridge Temperature Sensing Functions

        public string ReadTemperatureSensingMatterBridge(Alias sshClientAlias, string partNo)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            var res = sshClient.Send("sudo /usr/bin/chip-tool temperaturemeasurement read measured-value 2222 " + partNo);
            var lines = res.Split('\n');
            string occupancy_status = "";
            foreach (string line in lines)
            {
                if (line.Contains("MeasuredValue: "))
                {
                    occupancy_status = line.Substring(line.Length-4);
                    break;
                }
            }
            return occupancy_status;
        }

        #endregion

        #region Matter Bridge Color Control Functions

        public string ReadColorControlHUEMatterBridge(Alias sshClientAlias, string partNo)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            var res = sshClient.Send("sudo /usr/bin/chip-tool colorcontrol read current-hue  2222 " + partNo);
            var lines = res.Split('\n');
            string hueValue= "";
            foreach (string line in lines)
            {
                if (line.Contains("CurrentHue:"))
                {
                    var start = line.IndexOf("CurrentHue:");
                    hueValue = line.Substring(start+11);
                    break;
                }
            }
            return hueValue;
        }

        public string ReadColorControlSaturationMatterBridge(Alias sshClientAlias, string partNo)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            var res = sshClient.Send("sudo /usr/bin/chip-tool colorcontrol read current-saturation 2222 " + partNo);
            var lines = res.Split('\n');
            string hueValue = "";
            foreach (string line in lines)
            {
                if (line.Contains("CurrentSaturation:"))
                {
                    var start = line.IndexOf("CurrentSaturation:");
                    hueValue = line.Substring(start+18);
                    break;
                }
            }
            return hueValue;
        }

        public void MoveToHUEMatterBridge(Alias sshClientAlias, string partNo, string value)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            sshClient.Send("sudo /usr/bin/chip-tool colorcontrol move-to-hue " + value + " 1 1 1 0 2222 " + partNo);
        }

        public void MoveHUEMatterBridge(Alias sshClientAlias, string mode, string partNo)
        {
            string direction = "0";
            if (mode == "Up" || mode == "up")
            {
                direction = "1";
            }
            else if (mode == "down" || mode == "DOWN" || mode == "Down")
            {
                direction = "3";
            }
            else if (mode == "stop" || mode == "Stop" || mode == "STOP")
            {
                direction = "0";
            }

            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            sshClient.Send("sudo /usr/bin/chip-tool colorcontrol move-hue " + direction + " 1 1 0 2222 " + partNo);
        }

        public void StepHUEMatterBridge(Alias sshClientAlias, string mode, string size, string partNo)
        {
            string direction = "1";
            if (mode == "Up" || mode == "up")
            {
                direction = "1";
            }
            else if (mode == "down" || mode == "DOWN" || mode == "Down")
            {
                direction = "3";
            }

            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            sshClient.Send("sudo /usr/bin/chip-tool colorcontrol step-hue " + direction + " " + size + " 1 1 0 2222 " + partNo);
        }

        public void MoveToSaturationMatterBridge(Alias sshClientAlias, string partNo, string value)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            sshClient.Send("sudo /usr/bin/chip-tool colorcontrol move-to-saturation " + value + " 1 1 0 2222 " + partNo);
        }

        public void MoveSaturationMatterBridge(Alias sshClientAlias, string mode, string partNo)
        {
            string direction = "0";
            if(mode == "Up" || mode == "up")
            {
                direction = "1";
            }
            else if(mode == "down" || mode == "DOWN" || mode == "Down")
            {
                direction = "3";
            }
            else if(mode == "stop" || mode == "Stop" || mode == "STOP")
            {
                direction = "0";
            }

            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            sshClient.Send("sudo /usr/bin/chip-tool colorcontrol move-saturation " + direction + " 1 1 0 2222 " + partNo);
        }

        public void StepSaturationMatterBridge(Alias sshClientAlias, string mode, string size, string partNo)
        {
            string direction = "1";
            if (mode == "Up" || mode == "up")
            {
                direction = "1";
            }
            else if (mode == "down" || mode == "DOWN" || mode == "Down")
            {
                direction = "3";
            }

            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            sshClient.Send("sudo /usr/bin/chip-tool colorcontrol step-saturation " + direction + " " + size + " 1 1 0 2222 " + partNo);
        }

        public void MoveToHueandSaturationMatterBridge(Alias sshClientAlias, string partNo, string hueValue, string saturationValue)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            sshClient.Send("sudo /usr/bin/chip-tool colorcontrol move-to-hue-and-saturation " + hueValue + " " + saturationValue + " 1 1 0 2222 " + partNo);
        }

        #endregion

        #region Basic Info Bridge Cluster Functions

        public bool BasicInfoMatterBridge(Alias sshClientAlias, string partNo)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            var res = sshClient.Send("sudo chip-tool bridgeddevicebasicinformation read reachable 2222 " + partNo);

            var lines = res.Split('\n');
            bool isFound = false;
            foreach (string line in lines)
            {
                if (line.Contains("Reachable: TRUE"))
                {
                    isFound = true;
                    break;
                }
            }
            return isFound;
        }

        public string ReadNodeLableMatterBridge(Alias sshClientAlias, string partNo)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            var res = sshClient.Send("sudo chip-tool bridgeddevicebasicinformation read node-label 2222 " + partNo);
            var lines = res.Split('\n');
            string name = "";
            foreach (string line in lines)
            {
                if (line.Contains("NodeLabel: "))
                {
                    string[] splittedLine = line.Split(' ');
                    name = splittedLine[splittedLine.Length-1];
                    break;
                }
            }
            return name;
        }

        #endregion

        #region Group Functions

        public void SetGroupMatterBridge(Alias sshClientAlias)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            //To send a group command, first set up the group keys in the bridge. 
            //This example assumes the bridge to be Node ID 1, and GroupKeySetID 42 is added to Group ID 1:
            var res = sshClient.Send("sudo /usr/bin/chip-tool accesscontrol write acl " +
                "'[{\"fabricIndex\": 1, \"privilege\": 5, \"authMode\": 2, \"subjects\": [112233], \"targets\": null }," +
                "{\"fabricIndex\": 1, \"privilege\": 4, \"authMode\": 3, \"subjects\": [1], \"targets\": null }]' 2222 0");
            Thread.Sleep(1000);
            res = sshClient.Send("sudo /usr/bin/chip-tool groupkeymanagement key-set-write '{\"groupKeySetID\": 42, \"groupKeySecurityPolicy\": 0," +
                " \"epochKey0\": \"d0d1d2d3d4d5d6d7d8d9dadbdcdddedf\", \"epochStartTime0\": 2220000,\"epochKey1\": \"d1d1d2d3d4d5d6d7d8d9dadbdcdddedf\"," +
                " \"epochStartTime1\": 2220001,\"epochKey2\": \"d2d1d2d3d4d5d6d7d8d9dadbdcdddedf\", \"epochStartTime2\": 2220002 }' 2222 0");
            Thread.Sleep(1000);
            res = sshClient.Send("sudo /usr/bin/chip-tool groupkeymanagement write group-key-map " +
                "'[{\"groupId\": 1, \"groupKeySetID\": 42, \"fabricIndex\": 1}]' 2222 0");
            Thread.Sleep(1000);
            //Next, add bridge endpoint 2 to Group ID 0x0001
            res = sshClient.Send("sudo /usr/bin/chip-tool groups add-group 0x0001 grp1 2222 2");
            res = sshClient.Send("sudo /usr/bin/chip-tool groups add-group 0x0001 grp1 2222 3");
            Thread.Sleep(1000);
            //Next, program the chip-tool:
            res = sshClient.Send("sudo /usr/bin/chip-tool groupsettings add-group grp1 0x0002");
            res = sshClient.Send("sudo /usr/bin/chip-tool groupsettings add-keysets 0x0042 0 0x000000000021dfe0 hex:d0d1d2d3d4d5d6d7d8d9dadbdcdddedf");
            res = sshClient.Send("sudo /usr/bin/chip-tool groupsettings bind-keyset 0x0001 0x0042");
            //As Matter is based on IPv6 and the group message will be sent as a multicast message,
            //a multicast route must be set up to tell the Linux kernel what interface to send the group message to.
            res = sshClient.Send("sudo route add -6 ff35:40:fd00::/24 dev eth0");
        }

        public void sendMulticastMatterBridge(Alias sshClientAlias)
        {
            var sshClient = _uicManager.GetSshClient(sshClientAlias);
            var res = sshClient.Send("sudo /usr/bin/chip-tool onoff off 0xffffffffffff0001 1");
        }
        #endregion
    }
}
