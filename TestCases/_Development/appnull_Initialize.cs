// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
 using System.Linq;
using System.Collections.Generic;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Init ZATS")]
    public class appnull_Initialize : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            ReportService.LogTitle("Ta da!!!");
        }
    }
}