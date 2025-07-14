// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    public abstract class TestSettingsBase
    {
        public DeviceManager DeviceManager { get; set; }

        public TestSettingsBase(DeviceManager deviceManager)
        {
            DeviceManager = deviceManager;
        }
    }
}
