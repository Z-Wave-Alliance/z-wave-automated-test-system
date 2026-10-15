// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using System;
using System.Collections.Generic;
using System.Text;

namespace Zats.Services
{
    // Called when any data update is received via Z-Wave messages
    public delegate void DeviceDataUpdateCallback(CommandClassSupport commandClassSupport);

    // Called to restart the device so non-listening and FLiRS node are awake before any unsolicited reports
    public delegate void DeviceRestartCallback(CommandClassSupport commandClassSupport);

    // Called to reset the device to remove it from a Z-Wave network
    public delegate void DeviceResetCallback(CommandClassSupport commandClassSupport);

    // Called to send device back to sleep (either immediately or after normal wake up interval time)
    public delegate void DeviceSleepCallback(CommandClassSupport commandClassSupport, int timeout);

    // Called to send log message to backend or frontend log
    public delegate void LogMessageCallback(LogLocation location, LogLevel level, string message);

    public class CommandClassSupportDelegates
    {
        public DeviceDataUpdateCallback DeviceDataUpdate { get; set; }

        public DeviceRestartCallback DeviceRestart { get; set; }

        public DeviceResetCallback DeviceReset { get; set; }

        public DeviceSleepCallback DeviceSleep { get; set; }

        public LogMessageCallback LogMessage { get; set; }
    }

    public enum LogLocation
    {
        Backend,
        Frontend
    }

    public enum LogLevel
    {
        Verbose,
        Debug,
        Info,
        Error,
        Output
    }
}
