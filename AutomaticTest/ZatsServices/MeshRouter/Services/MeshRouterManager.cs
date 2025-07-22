// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using Utils;
using Utils.Extensions;
using ZWave.Layers;

namespace Zats.Services.MeshNetworkRacks
{
    public class MeshRouterManager : DeviceManagerBase
    {
        public MeshRouterTopologyBuilder MeshRouterTopologyBuilder { get; private set; }

        private Dictionary<Alias, IMeshRouterController> _devices = new Dictionary<Alias, IMeshRouterController>();
        public MeshRouterApplicationLayer ApplicationLayer { get; private set; }

        public MeshRouterManager(MeshRouterTopologyBuilder meshRouterTopologyBuilder)
            : base()
        {
            ApplicationLayer = new MeshRouterApplicationLayer(SessionLayer);
            MeshRouterTopologyBuilder = meshRouterTopologyBuilder;
        }

        public bool InitDevice(List<DeviceHost> available, KeyValuePair<Alias, MeshNetworkRouterMappingAttribute> requiredDevice)
        {
            var device = available.FirstOrDefault(dh =>
            dh.SType == InterfaceTypes.HTTP
            && dh.RApiType == RemoteApiTypes.MeshRouter);

            if (device == null)
                return false;

            available.Remove(device);
            AddNewDevice(requiredDevice.Key, device);
            var d = GetDevice(requiredDevice.Key);
            d?.CommandRequest($":00:MN?");
            d?.CommandRequest($":00:SN?");
            d?.CommandRequest($":00:FIRMWARE?");
            d?.CommandRequest($"HOP:ATT?");
            d?.CommandRequest($":SL:CHAN:1:2:3:4:SETATT:0.0"); //Enable all channels by default.

            return true;
        }

        internal void SetChannel(Alias routerAlias, string hop1, string hop2, int dbLevel)
        {
            var channel = MeshRouterTopologyBuilder.GetRoute(string.Concat(hop1, hop2));
            var ret = _devices[routerAlias].CommandRequest($"{channel}:SETATT:{dbLevel}.0");
            var retGet = _devices[routerAlias].CommandRequest($"{channel}:ATT?");
        }

        public void AddNewDevice(Alias id, DeviceHost deviceHost)
        {
            if (_devices.ContainsKey(id))
                return;

            int portNo = 80;
            var addresData = deviceHost.SName.Split(new[] { ':' });
            if (addresData.Length > 0)
            {
                if (addresData.Length > 1)
                {
                    int.TryParse(addresData[1], out portNo);
                }
                var networkRackController = ApplicationLayer.CreateMeshRouterController();
                if (networkRackController.Connect(new SocketDataSource(addresData[0], portNo, null, deviceHost.Alias)) == ZWave.Enums.CommunicationStatuses.Done)
                    _devices.Add(id, networkRackController);
            }
        }

        public IMeshRouterController GetDevice(Alias alias)
        {
            lock (this)
            {
                if (_devices.Keys.Contains(alias))
                    return _devices[alias];
                return null;
            }
        }

        public void FinalizeDevices()
        {
            if (_devices == null || _devices.Count == 0) return;

            var numbers = _devices.Keys.ToArray();
            try
            {
                numbers.DoForEach(number =>
                {
                    var d = GetDevice(number);
                    d?.CommandRequest($":SL:CHAN:1:2:3:4:SETATT:0.0"); //Enable all channels by default.
                });
            }
            catch (AggregateException agex)
            {
                agex.Message._DLOG();
            }
        }
    }
}
