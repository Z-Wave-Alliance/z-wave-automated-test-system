// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using Utils.UI.Enums;

namespace Zats.Services
{
    public class ServiceSettings
    {
        public string Name { get; set; }
        public bool IsDiagnosticsEnabled { get; set; }
        public AutoSwitch IsShowSuccessfulOperationsShortInfo { get; set; }
        public AutoSwitch IsShowSuccessfulOperationsFullInfo { get; set; }
        public AutoSwitch IsShowFailedOperations { get; set; }

        // Global IsExpectingFailure.
        private static AutoSwitch _isExpectingFailure = new AutoSwitch();
        public AutoSwitch IsExpectingFailure
        {
            get
            {
                _isExpectingFailure.SwitchName = Name + ".IsExpectingFailure";
                _isExpectingFailure.SwitchLog = Log;
                return _isExpectingFailure;
            }
        }

        public int FailCount { get; set; }
        public Action<string, LogLevels> Log { get; set; }
        public ServiceSettings(string name, Action<string, LogLevels> logAction)
        {
            Name = name;
            Log = logAction;

            IsShowSuccessfulOperationsShortInfo = new AutoSwitch(Name + ".IsShowSuccessfulOperationsShortInfo", Log);
            IsShowSuccessfulOperationsFullInfo = new AutoSwitch(Name + ".IsShowSuccessfulOperationsFullInfo", Log);
            IsShowFailedOperations = new AutoSwitch(Name + ".IsShowFailedOperations", Log);
        }

        public void Default()
        {
            IsShowSuccessfulOperationsFullInfo.Reset();
            IsShowSuccessfulOperationsShortInfo.Set();
            IsShowFailedOperations.Set();
        }
    }
}
