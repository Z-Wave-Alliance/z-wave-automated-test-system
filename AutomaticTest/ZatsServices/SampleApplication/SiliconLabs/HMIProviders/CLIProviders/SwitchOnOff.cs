// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Text.RegularExpressions;
using ZWave.TextApplication;

namespace Zats.Services.SampleApplication.SiliconLabs.CLIProviders
{
    class SwitchOnOff : SampleApplicationCLIBase, ISwitchOnOffProvider
    {
        public SwitchOnOff(TextDevice device) : base(device)
        { }

        public void OnOff()
        {
            SendCommand("toggle_led", TIMEOUT_MS);
        }

        public void HoldKey01(byte duration)
        {
            if (duration < 5)
            {
                SendCommand("send_nif", TIMEOUT_MS);
            }
            else
            {
                // Holding key 1 same as reset the device
                Reset();
            }
        }

        public bool? IsStatusOn()
        {
            string pattern = @"LED\d+ state: (?<led_state>on|off)";
            Regex ledStateRegex = new Regex(pattern);
            Match match = SendToTestInterfaceAndMatchRegex("get_led_state", ledStateRegex);
            if (match == null)
                return null;
            var result = match.Groups["led_state"].Value == "on";
            return result;
        }
    }
}