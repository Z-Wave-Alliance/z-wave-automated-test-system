// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetDefault
    -Add/Remove controller. 
    -Add/Remove slave")]
    [ZipDeviceMapping(1, "z2")]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    public class appzip_AddRemoveNode_DTLS1 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
        }
    }
}