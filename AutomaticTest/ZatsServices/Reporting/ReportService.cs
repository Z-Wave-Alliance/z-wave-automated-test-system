// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.IO;
using Utils.UI.Enums;

namespace Zats.Services
{
    public class ReportService : ServiceBase, IReportService
    {
        private ReportManager ReportManager { get; set; }
        public event Action<string, string, LogLevels> OnLog;

        public static bool mIsInitialized;
        public static ReportService mInstance;
        public ReportService(ReportManager reportManager)
        {
            ReportManager = reportManager;
            mInstance = this;
            mIsInitialized = true;
        }

        public static void AddLog(string text, LogLevels logLevel)
        {
            if (mIsInitialized)
                if (mInstance != null)
                    mInstance.Log(text, logLevel);
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            Log(text, logLevel, true);
        }

        private void Log(string text, LogLevels logLevel, bool isSendToTextDevice)
        {
            if (isSendToTextDevice)
                if (!string.IsNullOrEmpty(text))
                    ReportManager.TextDevice?.Send(text);
            OnLog?.Invoke(text, null, logLevel);
        }

        public void LogTitle(string text)
        {
            Log(text, LogLevels.Title);
        }

        public void LogText(string text)
        {
            Log(text, LogLevels.Text);
        }

        public void LogOk(string text)
        {
            Log(text, LogLevels.Ok);
        }

        public void LogWarning(string text)
        {
            Log(text, LogLevels.Warning);
        }

        public void LogError(string text)
        {
            Log(text, LogLevels.Error);
        }

        readonly string fn = @"qqq.txt";
        StreamWriter tw = null;
        public void LogInfo(string text)
        {
            if (text != null && text.StartsWith("="))
            {
                if (tw == null)
                {
                    tw = new StreamWriter(fn, true);
                }
                tw.WriteLine(text);
                tw.Flush();
            }
            Log(text, LogLevels.Information);
        }

        public HtmlFile CreateHtmlFile(string nameWithoutExtension)
        {
            var fname = ReportManager.GetFileName(ReportManager.TestCaseFolder, nameWithoutExtension, null, false);
            return new HtmlFile(Path.Combine(ReportManager.TestCaseFolder, Path.ChangeExtension(fname, "html")));
        }

        public string CreateCsvFile(string nameWithoutExtension)
        {
            var fname = ReportManager.GetFileName(ReportManager.TestCaseFolder, nameWithoutExtension, null, false);
            return Path.Combine(ReportManager.TestCaseFolder, Path.ChangeExtension(fname, "csv"));
        }

        public string GetTestRunFolder()
        {
            return ReportManager.TestRunFolder;
        }

        public DateTime GetTestRunTimeStamp()
        {
            return ReportManager.TestRunTimeStamp;
        }
    }
}
