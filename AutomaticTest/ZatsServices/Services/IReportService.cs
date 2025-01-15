// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils.UI.Enums;

namespace Zats.Services
{
    public interface IReportService
    {
        void LogInfo(string text);
        void LogError(string text);
        void LogOk(string text);
        void LogText(string text);
        void LogTitle(string text);
        void LogWarning(string text);
        event Action<string, string, LogLevels> OnLog;
        HtmlFile CreateHtmlFile(string repFile);
        string GetTestRunFolder();
        DateTime GetTestRunTimeStamp();
    }
}
