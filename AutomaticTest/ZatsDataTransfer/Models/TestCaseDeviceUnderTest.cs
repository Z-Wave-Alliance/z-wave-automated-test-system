// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zats.Services
{
    public partial class TestCaseDeviceUnderTest
    {
        /// <summary>
        /// Constructor for a new DUT based on an old DUT (to keep some properties)
        /// </summary>
        /// <param name="dutToInherritFrom"></param>
        public TestCaseDeviceUnderTest(TestCaseDeviceUnderTest dutToInherritFrom)
        {
            if (dutToInherritFrom != null)
            {
                DSK = dutToInherritFrom.DSK;
                QRData = dutToInherritFrom.QRData;
            }
        }

        public TestCaseDeviceUnderTest()
        {

        }
    }
}
