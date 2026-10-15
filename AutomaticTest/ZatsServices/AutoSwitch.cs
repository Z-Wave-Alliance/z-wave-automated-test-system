// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using Utils.UI.Enums;

namespace Zats.Services
{
    public class AutoSwitch : IDisposable
    {
        private bool _value;
        public string Name { get; set; }
        public Action<string, LogLevels> Log { get; set; }

        private string _switchName;
        public string SwitchName
        {
            get { return _switchName; }
            set
            {
                if (_switchName != null && value != null)
                    return;
                _switchName = value;
            }
        }

        private Action<string, LogLevels> _switchLog;
        public Action<string, LogLevels> SwitchLog
        {
            get { return _switchLog; }
            set
            {
                if (_switchLog != null && value != null)
                    return;
                _switchLog = value;
            }
        }

        public AutoSwitch() : this(string.Empty, null)
        {
        }

        public AutoSwitch(string name, Action<string, LogLevels> logAction)
        {
            Name = name;
            Log = logAction;
        }

        public AutoSwitch Set()
        {
            _value = true;
            var logger = SwitchLog ?? Log;
            logger?.Invoke((SwitchName ?? Name) + " TRUE", LogLevels.Information);
            return this;
        }

        public void Reset()
        {
            _value = false;
            var logger = SwitchLog ?? Log;
            logger?.Invoke((SwitchName ?? Name) + " FALSE", LogLevels.Information);
            SwitchLog = null;
            SwitchName = null;
        }


        public static implicit operator bool(AutoSwitch autoSwitch)
        {
            return autoSwitch._value;
        }

        #region IDisposable Members

        public void Dispose()
        {
            Reset();
        }

        #endregion
    }
}
