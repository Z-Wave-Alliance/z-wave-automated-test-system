using System;
using System.Text.RegularExpressions;
using ZWave.TextApplication;

namespace Zats.Services.SampleApplication.SiliconLabs.CLIProviders
{
    class PowerStrip : SampleApplicationCLIBase, IPowerStripProvider
    {
        public PowerStrip(TextDevice device) : base(device)
        { }

        public void DimHold(int duration)
        {
            // In the software the dimmer can be hold for a specific duration
            // In this case the firmware will start a level change with 10 sec duration
            // This functionality is not implemented with the CLI so we just send a command to set the level to 49%
            SendCommand("dim_endpoint 2 49", TIMEOUT_MS);
        }

        public void DimOnOff()
        {
            // Toggle endpoint 2, which is the dimmer (Multilevel Switch Command Class)
            SendCommand("toggle_endpoint 2", TIMEOUT_MS);
        }

        public void ToggleNotify()
        {
            SendCommand("toggle_notification_sending", TIMEOUT_MS);
        }

        public void OnOff()
        {
            // Toggle endpoint 1, which is the main switch (Binary Switch Command Class)
            SendCommand("toggle_endpoint 1", TIMEOUT_MS);
        }

        public bool? IsStatusOn()
        {
            Regex ledStateRegex = new Regex(@"(?<led_state>on|off)");
            Match match = SendToTestInterfaceAndMatchRegex("get_led_state", ledStateRegex);
            if (match == null)
                return null;
            return match.Groups["led_state"].Value == "on";
        }

        public bool? IsDimStatusOn()
        {
            Regex ledStateRegex = new Regex(@"Red:\s(?<red_value>\d+),\sGreen:\s(?<green_value>\d+),\sBlue:\s(?<blue_value>\d+)");
            Match match = SendToTestInterfaceAndMatchRegex("get_led_state", ledStateRegex);
            if (match == null)
                return null;

            uint redValue = Convert.ToUInt32(match.Groups["red_value"].Value);
            uint greenValue = Convert.ToUInt32(match.Groups["green_value"].Value);
            uint blueValue = Convert.ToUInt32(match.Groups["blue_value"].Value);

            // If any of the RGB values is greater than 0, the LED is on
            return redValue > 0 || greenValue > 0 || blueValue > 0;
        }
    }
}