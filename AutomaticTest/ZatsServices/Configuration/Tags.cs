// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zats.Services
{
    /// <summary>
    /// Feature tag cloud.
    /// Please avoid dublicating feature tags i.e SecurityS2, SecS2, S2security
    /// </summary>
    public enum Tags
    {
        //ZIP, // DO NOT USE such tag, it can be found based on DeviceMappings
        //Debug, // DO NOT USE such tag, it can be found based on DeviceMappings
        //TestInterface // DO NOT USE such tag, it can be found based on DeviceMappings

        SYS, // core feature tests for ZATS diagnostic purpose
        DEV, // developer/user feature tests
        CTT, // certification test case

        Brief, // for quick tests. Up to 5 minutes approx.
        Long, // for long tests, stress tests. More than 15 minutes approx. 

        DK660,
        DK670,
        //DKxx // feel free to expand devkit tags
        
        Flirs,
        Mixed,
        Generic,
        S0,
        S2,
        PCController,
        // expand here

        LongRange,
        Association
    }
}
