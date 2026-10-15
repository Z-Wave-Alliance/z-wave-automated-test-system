// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Security 2 Controller Side Inclusion Workflow Test case", Tags.Long)]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    [DeviceMapping(3, LibTypes.ControllerStaticLib)]
    public class appbasic_AddRemoveSlaveLoop : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias PrimaryController = new Alias(1);
            Alias SlaveNode = new Alias(2);
            Alias ExclusionController = new Alias(3);
            Alias[] ALL = new Alias[] { PrimaryController, SlaveNode, ExclusionController };

            ParallelService.Reset(ALL);

            BasicResult.AddNode addRes;
            ReportService.LogText("4. Add and Remove SlaveNode to/from network 62 times (remove with another controller)");
            for (int i = 0; i < 62; i++)
            {
                addRes = Service.AddNode(PrimaryController, SlaveNode);
                AssertIsTrue(addRes, "Successfully added SlaveNode, Id: " + addRes.NodeId, "Unable to add SlaveNode");
                Delay(500); // wait SUC notify
                Service.RemoveNode(ExclusionController, SlaveNode);
                Delay(500); // wait SUC notify
            }

        }
    }
}