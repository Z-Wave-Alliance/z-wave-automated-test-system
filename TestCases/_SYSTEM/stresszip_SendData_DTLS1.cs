// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetDefault
    -Add controller. 
    -Add slave
    -SendData
    -Stress test")]
    [ZipDeviceMapping(1, "z2")]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    public class stresszip_SendData_DTLS1 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        { 
        }
    }
}