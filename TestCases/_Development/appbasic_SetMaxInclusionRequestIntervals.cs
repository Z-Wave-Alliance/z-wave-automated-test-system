// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
﻿using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Test Set Max Inclusion Request interval", Tags.Long)]

    [DeviceMapping(1, LibTypes.EndDeviceAny)]
    public class appbasic_SetMaxInclusionRequestIntervals : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            var slave = new Alias(1);

            Service.Reset(slave);

            var homeId = Service.GetInclusionRequestHomeId(slave);
            ReportService.LogText("1. Get DSK from the device, filter for HomeId = " + homeId.ToHexString());

            ReportService.LogText("2. SetMaxInclusionRequestIntervals, interval = 5");
            Service.SetMaxInclusionRequestIntervals(slave, 5);

            var ztc = ZnifferService.CollectData();
            ztc.SetUpWatch(0, 255, homeId, null, HeaderTypes.EXPLORER_AUTOINCLUSION, null, false);

            var defaultTimeout = 30 * 60 * 1000;
            ReportService.LogTitle("3. Start LearnMode for " + defaultTimeout);
            var lmToken = AsyncService.SetLearnMode(slave, LearnModes.LearnModeSmartStart | LearnModes.LearnModeNetworkMask, defaultTimeout);

            using (Service.Settings.IsExpectingFailure.Set())
            {
                ReportService.LogTitle("4. Stop LearnMode");
                lmToken.WaitCompletedSignal();
            }
            var ztRes = ztc.WaitCompletedSignal();
            foreach (var item in ztRes.Frames)
            {
                ReportService.LogText($"Delta = {item.Delta}, Time = {item.CreatedAt.ToString("mm:ss.fff") }");
            }
        }
    }
}