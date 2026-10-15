// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Text.RegularExpressions;
using System.Threading;
using ZWave.TextApplication;

namespace Zats.Services.SampleApplication.SiliconLabs.CLIProviders
{
    class DoorLockKeyPad : SampleApplicationCLIBase, IDoorLookKeypadProvider
    {
        private const string DEFAULT_PIN_CODE = "1234";
        public DoorLockKeyPad(TextDevice device) : base(device)
        { }


        public void ToggleDoorHandle()
        {
            SendCommand("set_doorhandle_state activate", TIMEOUT_MS);
            Thread.Sleep(20);
            SendCommand("set_doorhandle_state deactivate", TIMEOUT_MS);
        }

        public void ToggleDoorHandle(byte duration)
        {
            SendCommand("set_doorhandle_state activate", TIMEOUT_MS);
            Thread.Sleep(TimeSpan.FromSeconds(duration));
            SendCommand("set_doorhandle_state deactivate", TIMEOUT_MS);
        }

        public void ToggleKeyPad()
        {
            // Under the hood, the keypad is a button, and when the button is pressed, it simulate the code entered event
            SendCommand($"enter_user_code {DEFAULT_PIN_CODE}", TIMEOUT_MS);
        }

        public void BatteryReport()
        {
            SendCommand("battery_report", TIMEOUT_MS);
        }

        public bool? IsLatchOpen()
        {
            // Latch state can be open or closed
            Regex ledStateRegex = new Regex(@"(?<latch_state>open|closed)");
            Match match = SendToTestInterfaceAndMatchRegex("get_doorlatch_state", ledStateRegex);
            if (match == null)
                return null;
            return match.Groups["latch_state"].Value == "open";
        }

        public bool? IsBoltLocked()
        {
            // Bolt state can be locked or unlocked
            Regex boltStateRegex = new Regex(@"(?<bolt_state>unlocked|locked)");
            Match match = SendToTestInterfaceAndMatchRegex("get_doorbolt_state", boltStateRegex);
            if (match == null)
                return null;
            return match.Groups["bolt_state"].Value == "locked";
        }
    }
}