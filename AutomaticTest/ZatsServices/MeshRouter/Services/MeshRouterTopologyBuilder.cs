// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Utils;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.Layers;

namespace Zats.Services.MeshNetworkRacks
{
    public class MeshRouterTopologyBuilder
    {
        private static char[] HOPS = new char[] { 'A', 'B', 'C', 'D', 'E', 'F' };
        private static Tuple<int, int, string>[] PREDEFINED_HOP_PATHS = new Tuple<int, int, string>[15]
        {
            // att, channel, hops pair
            new Tuple<int, int, string>(1,1, "AB"),
            new Tuple<int, int, string>(1,2, "AD"),
            new Tuple<int, int, string>(1,3, "BC"),
            new Tuple<int, int, string>(1,4, "AC"),

            new Tuple<int, int, string>(2,1, "BD"),
            new Tuple<int, int, string>(2,2, "AF"),
            new Tuple<int, int, string>(2,3, "BE"),
            new Tuple<int, int, string>(2,4, "AE"),

            new Tuple<int, int, string>(3,1, "CD"),
            new Tuple<int, int, string>(3,2, "DE"),
            new Tuple<int, int, string>(3,3, "EF"),
            //new Tuple<int, int, string>(3,4, "0"), //INACTIVE_ADDRESS 

            new Tuple<int, int, string>(4,1, "BF"),
            new Tuple<int, int, string>(4,2, "CF"),
            new Tuple<int, int, string>(4,3, "DF"),
            new Tuple<int, int, string>(4,4, "CE"),
        };

        public const string TURN_ON_ALL_CHANNELS = ":SL:CHAN:1:2:3:4:SETATT:0";
        public const string TURN_OFF_ALL_CHANNELS = ":SL:CHAN:1:2:3:4:SETATT:95";

        private MeshRouterDeviceInfo _routerInfo;

        public MeshRouterTopologyBuilder(MeshRouterDeviceInfo routerInfo)
        {
            _routerInfo = routerInfo;
        }

        public string GetRoute(string hopsPair)
        {
            if (!string.IsNullOrEmpty(hopsPair) && hopsPair.Length > 1)
                return GetRoute(hopsPair[0], hopsPair[1]);
            else
                return null;
        }

        public string GetRoute(char hop1, char hop2)
        {
            var ret = string.Empty;
            var path = PREDEFINED_HOP_PATHS.FirstOrDefault(x => x.Item3.Contains(hop1) && x.Item3.Contains(hop2));
            if (path != null)
            {
                //ret = $":0{atNo}:CHAN:{chNo}";
                ret = $":0{path.Item1}:CHAN:{path.Item2}";
            }
            return ret;
        }

        public bool RunDetect(DeviceHostClient[] deviceHostClients, BasicApplicationLayer basicApp, BasicApplicationLayer tcpApp)
        {
            using (var meshRouterController = new MeshRouterApplicationLayer(new ZWave.Layers.Session.SessionLayer()).CreateMeshRouterController())
            {
                if (meshRouterController.Connect(new SocketDataSource(_routerInfo.IPAddress.ToString(), _routerInfo.Port)) != ZWave.Enums.CommunicationStatuses.Done)
                    return false;

                var routerReq = meshRouterController.Request(TURN_ON_ALL_CHANNELS, 1000);

                var controllers = new List<Controller>();
                var failedControllers = new List<Controller>();
                foreach (var devHostclient in deviceHostClients)
                {
                    //devHostclient.Host.Hop = string.Empty;
                    var controller = devHostclient.Host.SType == InterfaceTypes.COM ?
                        basicApp.CreateController(devHostclient.SClient) :
                        tcpApp.CreateController(devHostclient.SClient);
                    if (controller.Connect() == ZWave.Enums.CommunicationStatuses.Done)
                    {
                        controller.SetDefault();
                        controller.GetVersion();
                        devHostclient.SClient = controller;
                        controllers.Add(controller);
                    }
                    else
                    {
                        failedControllers.Add(controller);
                    }
                }

                var mainController = controllers[0];
                mainController.MemoryGetId();
                mainController.SetSucNodeID(new ZWave.Devices.NodeTag(mainController.Id), true, true, 0x01);
                foreach (var controller in controllers.Skip(1))
                {
                    var addRemoveToken = mainController.AddNodeToNetwork((ZWave.Enums.Modes)Modes.NodeAny, 10000, null);
                    if (mainController.WaitNodeStatusSignal(ZWave.BasicApplication.Enums.NodeStatuses.LearnReady, 1000))
                    {
                        var learnRes = controller.SetLearnMode(ZWave.Enums.LearnModes.LearnModeClassic, 10000);
                        var addRemoveResult = addRemoveToken.WaitCompletedSignal();
                        if (!learnRes || !addRemoveResult)
                        {
                            failedControllers.Add(controller);
                        }
                    }
                    else
                    {
                        failedControllers.Add(mainController);
                        mainController = controller;
                        mainController.SetSucNodeID(new ZWave.Devices.NodeTag(mainController.Id), true, true, 0x01);
                    }
                }
                foreach (var controller in controllers.Skip(1))
                {
                    controller.RequestNetworkUpdate();
                }

                for (int i = 0; i < deviceHostClients.Length; i++)
                {
                    var sendingController = deviceHostClients[i].SClient as Controller;
                    if (failedControllers.Contains(sendingController) ||
                        !string.IsNullOrEmpty(deviceHostClients[i].Host.Hop))
                    {
                        continue;
                    }
                    meshRouterController.Request(TURN_OFF_ALL_CHANNELS, 1000);

                    var listenHosts = deviceHostClients.Where(x => x != deviceHostClients[i]).ToArray();
                    var oneHopHosts = GetReceiverHosts(sendingController, listenHosts);
                    listenHosts = listenHosts.Where(x => !oneHopHosts.Contains(x)).ToArray();

                    var detectedPaths = new List<Tuple<string, int>>();
                    foreach (var pathToDetect in PREDEFINED_HOP_PATHS)
                    {
                        // Turn off all.
                        meshRouterController.Request(TURN_OFF_ALL_CHANNELS, 1000);
                        // Turn on specific channel
                        string routeName = GetRoute(pathToDetect.Item3);
                        string oneChanelOn = $"{routeName}:SETATT:0";
                        var responseChOn = meshRouterController.Request(oneChanelOn, 1000);

                        var receivedList = GetReceiverHosts(sendingController, listenHosts);

                        if (receivedList.Count > 0)
                        {
                            detectedPaths.Add(new Tuple<string, int>(pathToDetect.Item3, receivedList.Count));
                        }
                    }

                    if (detectedPaths.Count == PREDEFINED_HOP_PATHS.Count())
                    {
                        //expected detected Paths count = num of connected hops with device(s) - else device is not in a hop
                        var min = detectedPaths.Min(x => x.Item2);
                        detectedPaths = detectedPaths.Where(x => x.Item2 != min).ToList(); // remove neigbors;
                    }
                    if (detectedPaths.Count > 0)
                    {
                        var hop = HOPS.OrderByDescending(ch => detectedPaths.Count(x => x.Item1.Contains(ch))).First();
                        var hopName = hop.ToString();
                        deviceHostClients[i].Host.Hop = hopName;
                        foreach (var item in oneHopHosts)
                        {
                            item.Host.Hop = hopName;
                        }
                    }
                }
                routerReq = meshRouterController.Request(TURN_ON_ALL_CHANNELS, 1000);
                return true;
            }
        }

        /// <summary>
        /// Get receivers using send data nulti probing to all possible listeners.
        /// </summary>
        /// <param name="sendingController">Sender</param>
        /// <param name="listenHosts">Listeners</param>
        /// <returns>Receivers</returns>
        private static List<DeviceHostClient> GetReceiverHosts(Controller sendingController, DeviceHostClient[] listenHosts)
        {
            var receivedList = new List<DeviceHostClient>();
            var lockObj = new object();
            var listeningTokens = new List<Tuple<IApplicationClient, ZWave.ActionToken>>();
            foreach (var recNode in listenHosts)
            {
                var listeningData = new Tuple<IApplicationClient, ZWave.ActionToken>(recNode.SClient,
                    recNode.SClient.Listen(new ByteIndex[] { ByteIndex.AnyValue }
                        /*new ByteIndex[] { 0x00, 0xA8, 0x08, 0x00, sendingController.Id, 0x03, COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_BASIC.BASIC_SET.ID }*/,
                        data =>
                        {
                            lock (lockObj)
                            {
                                if (!receivedList.Contains(recNode))
                                {
                                    receivedList.Add(recNode);
                                }
                            }
                        }));
                listeningTokens.Add(listeningData);
            }

            // Send multicast.
            var nodesIds = listenHosts.Select(devHClient => ((Controller)devHClient.SClient).Network.NodeTag).ToArray();
            for (int mult = 0; mult < 3; mult++)
            {
                sendingController.SendDataMulti(nodesIds, new COMMAND_CLASS_BASIC.BASIC_SET(), ZWave.Enums.TransmitOptions.TransmitOptionNone);
                Thread.Sleep(100);
            }
            foreach (var lToken in listeningTokens)
            {
                lToken.Item1.Cancel(lToken.Item2);
            }
            listeningTokens.Clear();
            return receivedList;
        }

        public void LoadFromProfile(Profile activeProfile)
        {
            var meshRouterHost = activeProfile.DeviceHosts.FirstOrDefault(x => x.JName == _routerInfo.SerialNo);
            if (meshRouterHost == null)
                return;
        }

    }
}
