// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using Utils.UI.Enums;
using Zats.Services.MeshNetworkRacks;

namespace Zats.Services
{
    public class MeshRouterService : ServiceBase
    {
        private MeshRouterManager _meshRouterManager;
        private DeviceManager _deviceManager;

        public MeshRouterService(MeshRouterManager meshRouterManager, DeviceManager deviceManager)
        {
            _meshRouterManager = meshRouterManager;
            _deviceManager = deviceManager;
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            _meshRouterManager.Log(text, logLevel);
        }

        public void SetChannel(Alias router, Alias nodeNumber1, Alias nodeNumber2, DbLevels dbLevel)
        {
            var device1 = _deviceManager.GetDeviceHostClient(nodeNumber1);
            var device2 = _deviceManager.GetDeviceHostClient(nodeNumber2);

            _meshRouterManager.SetChannel(router, device1.Host.Hop, device2.Host.Hop, (int)dbLevel);
        }

        public void SetChannels(Alias routerNum, DbLevels dbLevel)
        {
            var router = _meshRouterManager.GetDevice(routerNum);
            router.CommandRequest($":SL:CHAN:1:2:3:4:SETATT:{(int)dbLevel}.0");
            //Check each channel for debug
            //router.CommandRequest($":01:CHAN:1:ATT?");
            //router.CommandRequest($":01:CHAN:2:ATT?");
            //router.CommandRequest($":01:CHAN:3:ATT?");
            //router.CommandRequest($":01:CHAN:4:ATT?");
            //router.CommandRequest($":02:CHAN:1:ATT?");
            //router.CommandRequest($":02:CHAN:2:ATT?");
            //router.CommandRequest($":02:CHAN:3:ATT?");
            //router.CommandRequest($":02:CHAN:4:ATT?");
            //router.CommandRequest($":03:CHAN:1:ATT?");
            //router.CommandRequest($":03:CHAN:2:ATT?");
            //router.CommandRequest($":03:CHAN:3:ATT?");
            ////router.CommandRequest($":03:CHAN:4:ATT?");//unused
            //router.CommandRequest($":04:CHAN:1:ATT?");
            //router.CommandRequest($":04:CHAN:2:ATT?");
            //router.CommandRequest($":04:CHAN:3:ATT?");
            //router.CommandRequest($":04:CHAN:4:ATT?");
        }

        public void SetChannel(Alias routerNum, int atn, int ch, DbLevels dbLevel)
        {
            var router = _meshRouterManager.GetDevice(routerNum);
            router.CommandRequest($":{atn.ToString("00")}:CHAN:{ch}:SETATT:{(int)dbLevel}.0");
        }
    }

    public enum DbLevels
    {
        Unknown = -1,
        db0 = 0,
        db5 = 5,
        db10 = 10,
        db20 = 20,
        db25 = 25,
        db30 = 30,
        db35 = 35,
        db40 = 40,
        db41 = 41,
        db42 = 42,
        db43 = 43,
        db44 = 44,
        db45 = 45,
        db46 = 46,
        db47 = 47,
        db48 = 48,
        db49 = 49,
        db50 = 50,
        db51 = 51,
        db52 = 52,
        db53 = 53,
        db54 = 54,
        db55 = 55,
        db56 = 56,
        db57 = 57,
        db58 = 58,
        db59 = 59,
        db60 = 60,
        db61 = 61,
        db62 = 62,
        db63 = 63,
        db64 = 64,
        db65 = 65,
        db66 = 66,
        db67 = 67,
        db68 = 68,
        db69 = 69,
        db70 = 70,
        db71 = 71,
        db72 = 72,
        db73 = 73,
        db74 = 74,
        db75 = 75,
        db76 = 76,
        db77 = 77,
        db78 = 78,
        db79 = 79,
        db80 = 80,
        db85 = 85,
        db90 = 90,
        db95 = 95
    }
}
