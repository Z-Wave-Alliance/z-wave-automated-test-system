// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Program from dev kit pack")]
    [TestInterfaceDeviceMapping(1, ChipSeries.ZW050x, DevKits.v6_51, LibTypes.SwitchOnOff, FileNameFilter = @"(?!.*SCHEME_0).*")]
   // [TestInterfaceDeviceMapping(1, LibTypes.SwitchOnOff)]
    public class appbasic_ProgramFromDevKit : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1, "Primary");



        }
    }
}