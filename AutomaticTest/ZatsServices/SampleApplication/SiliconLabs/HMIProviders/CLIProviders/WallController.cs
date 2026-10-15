// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Linq;
using System.Threading;
using ZWave.TextApplication;

namespace Zats.Services.SampleApplication.SiliconLabs.CLIProviders
{
    class WallController : SampleApplicationCLIBase, IWallControllerProvider
    {
        private readonly int[] AVAILABE_KEYS = { 1, 2, 3 };
        private readonly string[] VALID_ACTIONS = { "hold", "release", "press" };
        public WallController(TextDevice device) : base(device)
        { }

        public void Key01()
        {
            string cli_cmd = WallControllerCLIBuilder(1, "press");
            SendCommand(cli_cmd, TIMEOUT_MS);
        }

        public void HoldKey01(byte duration)
        {
            string cli_cmd = WallControllerCLIBuilder(1, "hold");
            SendCommand(cli_cmd, TIMEOUT_MS);
            Thread.Sleep(TimeSpan.FromSeconds(duration));
            cli_cmd = WallControllerCLIBuilder(1, "release");
            SendCommand(cli_cmd, TIMEOUT_MS);
        }

        public void Key02()
        {
            string cli_cmd = WallControllerCLIBuilder(2, "press");
            SendCommand(cli_cmd, TIMEOUT_MS);
        }

        public void HoldKey02(byte duration)
        {
            string cli_cmd = WallControllerCLIBuilder(2, "hold");
            SendCommand(cli_cmd, TIMEOUT_MS);
            Thread.Sleep(TimeSpan.FromSeconds(duration));
            cli_cmd = WallControllerCLIBuilder(2, "release");
            SendCommand(cli_cmd, TIMEOUT_MS);
        }

        public void Key03()
        {
            string cli_cmd = WallControllerCLIBuilder(3, "press");
            SendCommand(cli_cmd, TIMEOUT_MS);
        }

        //HoldButton
        public void HoldKey03(byte duration)
        {
            string cli_cmd = WallControllerCLIBuilder(3, "hold");
            SendCommand(cli_cmd, TIMEOUT_MS);
            Thread.Sleep(TimeSpan.FromSeconds(duration));
            cli_cmd = WallControllerCLIBuilder(3, "release");
            SendCommand(cli_cmd, TIMEOUT_MS);
        }

        private void ValidateKey(int key)
        {
            if (AVAILABE_KEYS.Contains(key) == false)
            {
                throw new ArgumentException("Wrong key");
            }
        }

        private void ValidateAction(string action)
        {
            if (VALID_ACTIONS.Contains(action) == false)
            {
                throw new ArgumentException("Wrong action");
            }
        }
        private string WallControllerCLIBuilder(int key, string action)
        {
            ValidateKey(key);
            ValidateAction(action);
            return $"send_central_scene_key {key} {action}";
        }
    }
}