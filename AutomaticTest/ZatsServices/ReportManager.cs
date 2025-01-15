// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.IO;
using ZWave.Layers;
using ZWave.Layers.Session;
using ZWave.Layers.Transport;
using ZWave.TextApplication;

namespace Zats.Services
{
    public class ReportManager
    {
        public ISessionLayer SessionLayer { get; set; }
        public ReportManager()
        {
            SessionLayer = new SessionLayer() { SuppressDebugOutput = true };
            ApplicationLayer = new TextApplicationLayer(SessionLayer, new TextFrameLayer(), new TextTransportLayer() { SuppressDebugOutput = true });
            TestRunTimeStamp = DateTime.Now;
        }

        public TextDevice TextDevice { get; private set; }
        public TextApplicationLayer ApplicationLayer { get; set; }
        public string TestRunFolder { get; set; }
        public string TestCaseFolder { get; set; }
        public string TestCaseLogName { get; set; }
        public string CurrentFolder { get; set; }
        public DateTime TestRunTimeStamp { get; set; }

        public void InitDevice()
        {
            TextDevice = ApplicationLayer.CreateTextDevice();
            TextDevice.Connect(new TextDataSource(null, null));
        }

        public void FinalizeDevice()
        {
            TextDevice?.Dispose();
        }

        public static string GetFileName(string folder, string testCaseName, string frequency, bool isOwerwrite)
        {
            var tcName = $"{testCaseName}-{frequency}";

            if (!isOwerwrite)
            {
                int existingCount = 1;
                while (File.Exists(Path.Combine(folder, $"{tcName}.txt")))
                {
                    existingCount++;
                    tcName = $"{testCaseName}-{frequency}-{existingCount}";
                }
            }
            return tcName;
        }
    }
}
