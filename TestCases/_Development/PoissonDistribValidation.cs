// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
﻿using System;
using System.Collections.Generic;
using Zats.Services;
using System.Net;
using System.Linq;

namespace ZatsTests
{
    [ZatsTestCase(@"GW, Z/IP Router application verification")]
    //Devices Required
    [ZipDeviceMapping(1, "z2")] // Zip Device   
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]

    public class PoissonDsitribValidation : ZatsCodeTestCase
    {
        Alias ZipController = new Alias(1);

        protected override void ExecuteTest()
        {
            Random rnd = new Random();

            int WakeUpInterval = 5000;

            double Lambda = 1/(double)WakeUpInterval;
            double Average = 0.0;

            List<double> AverageContainer = new List<double>();

            for (int indi = 0; indi < 1000000; indi++)
            {
                AverageContainer.Add( CalculateTime(Lambda, rnd.NextDouble()));
            }
            double AverageResult = AverageContainer.Sum() / (double)AverageContainer.Count;
            ReportService.LogOk(AverageResult.ToString());
        }

        double CalculateTime(double Lambda, double RandVar)
        {            
            return -Math.Log( 1.0 - RandVar) / Lambda;
        }
    }
}
