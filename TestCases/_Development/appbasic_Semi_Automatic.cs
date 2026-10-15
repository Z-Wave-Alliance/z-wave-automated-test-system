// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Show usage of Message Box")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    public class appbasic_Semi_Automatic : ZatsCodeInteractiveTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1, "Primary");
            Alias Slave = new Alias(2, "Slave");

            ReportService.LogText("Start");
            ParallelService.Reset(Primary, Slave);

            Service.AddNode(Primary, Slave);

            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_SET());

            DialogResult boxEarlyResponse = MessageBox.Show("Power reset Slave Device # " + Slave.ToString(),
                            "My Application", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk);
            if (boxEarlyResponse == DialogResult.Cancel)
            {
                Fail("TC Cancelled");
            }

            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_SET());
        }
    }
}