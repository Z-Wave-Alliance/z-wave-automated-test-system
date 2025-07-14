// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.IO;
using System.Linq;
using Utils;
using Utils.Events;
using Utils.UI.Enums;
using Zats.Services.Telemetry;
using ZWave;
using ZWave.Layers.Session;
using ZWave.ZnifferApplication.Operations;

namespace Zats.Services
{
    public class DeviceManagerBase
    {
        public event Action<string, string, LogLevels> OnLog;
        public event Action<string, byte[], byte> OnLogKey;
        public ServiceSettings Settings { get; set; }
        public bool IsSuppressLogDebug { get; set; }
        public Profile CurrentProfile { get; set; }
        public ZatsCache ZatsCache { get; set; }
        public SessionLayer SessionLayer { get; set; }
        public ITelemetryClient TelemetryClient { get; set; }
        public DeviceManagerBase()
        {
            Settings = new ServiceSettings(GetType().Name, Log);
            Settings.Default();
            SessionLayer = CreateSessionLayer();
        }

        private SessionLayer CreateSessionLayer()
        {
            var ret = new SessionLayer();
            ret.LogEntryPointClass = "ZatsCodeTestCase";
            ret.ActionChanged += new EventHandler<EventArgs<ActionBase>>(OnActionChangedLogInfo);
            ret.ActionChanged += new EventHandler<EventArgs<ActionBase>>(OnActionChangedUpdateConfigItem);
            return ret;
        }

#if !NETCOREAPP
        public static string GetDefaultProgrammerPath()
        {
            string basePath = string.Empty;
            string programmerPath = string.Empty;
            if (8 == IntPtr.Size ||
                (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                basePath = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }
            else
            {
                basePath = Environment.GetEnvironmentVariable("ProgramFiles");
            }

            if (Directory.Exists(basePath))
            {
                basePath = Path.Combine(basePath, "Sigma Designs");
                if (Directory.Exists(basePath))
                {

                    var dirs = Directory.GetDirectories(basePath, "Z-Wave Programmer*");
                    if (dirs.Length == 1)
                    {
                        basePath = Path.Combine(basePath, "Z-Wave Programmer");
                        if (Directory.Exists(basePath))
                        {
                            basePath = Path.Combine(basePath, "ZWaveProgrammer.exe");
                            if (File.Exists(basePath))
                            {
                                programmerPath = basePath;
                            }
                        }
                    }
                }
            }
            return programmerPath;
        }
#endif

        void OnActionChangedLogInfo(object sender, EventArgs<ActionBase> e)
        {
            var actionToken = e.Value.Token;
            if (actionToken.Result.State == ActionStates.Running)
            {
                if (TelemetryClient != null)
                {
                    TelemetryClient.StartActivity(actionToken.ActionId, actionToken.Name);
                }
            }
            else if (!actionToken.IsStateActive && !actionToken.IsChildAction)
            {
                switch (actionToken.Result.State)
                {
                    case ActionStates.Completed:
                    case ActionStates.Cancelled:
                        if (TelemetryClient != null)
                        {
                            TelemetryClient.StopActivity(actionToken.ActionId);
                        }
                        if (Settings.IsShowSuccessfulOperationsShortInfo || Settings.IsShowSuccessfulOperationsFullInfo)
                        {
                            Log(actionToken.Name, actionToken.Description, LogLevels.Done);
                            if (Settings.IsShowSuccessfulOperationsFullInfo)
                            {
                                LogDebug(actionToken.Result);
                            }
                        }
                        break;
                    default:
                        if (TelemetryClient != null)
                        {
                            TelemetryClient.StopErrorActivity(actionToken.ActionId, "Error");
                        }
                        Settings.FailCount++;
                        if (actionToken.ParentType == typeof(CollectDataOperation))
                        {
                            if (Settings.IsShowSuccessfulOperationsShortInfo || Settings.IsShowSuccessfulOperationsFullInfo)
                            {
                                Log(actionToken.Name, actionToken.Description, LogLevels.Done);
                                if (Settings.IsShowSuccessfulOperationsFullInfo)
                                {
                                    LogDebug(actionToken.Result);
                                }
                            }
                        }
                        else if (Settings.IsExpectingFailure)
                        {
                            if (Settings.IsShowSuccessfulOperationsShortInfo || Settings.IsShowSuccessfulOperationsFullInfo)
                            {
                                Log(actionToken.State + " " + actionToken.Name, actionToken.Description, LogLevels.Done);
                                if (Settings.IsShowSuccessfulOperationsFullInfo)
                                {
                                    LogDebug(actionToken.Result);
                                }
                            }
                        }
                        else
                        {
                            if (actionToken.LogEntryPointException == null)
                            {
                                Log(actionToken.Name, actionToken.Description, LogLevels.Error);
                            }
                            else
                            {
                                Log(actionToken.Name, $"{actionToken.LogEntryPointException} {actionToken.Description}", LogLevels.Error);
                            }
                            LogDebug(actionToken.Result);
                        }
                        break;
                }
            }
            else 
            {
                switch (actionToken.Result.State)
                {
                    case ActionStates.Completed:
                    case ActionStates.Cancelled:
                        if (TelemetryClient != null)
                        {
                            TelemetryClient.StopActivity(actionToken.ActionId);
                        }
                        break;
                    default:
                        if (TelemetryClient != null)
                        {
                            TelemetryClient.StopErrorActivity(actionToken.ActionId, "Error");
                        }
                        break;
                }
            }
        }

        protected virtual void OnActionChangedUpdateConfigItem(object sender, EventArgs<ActionBase> e)
        {
        }

        private void LogDebug(ActionResult res)
        {
            if (!IsSuppressLogDebug)
            {
                Log(res.State.ToString(), LogLevels.Information);
                LogTrace(res);

            }
        }

        private void LogTrace(ActionResult res)
        {
            Log(res.TraceLogString, LogLevels.Trace);
            if (res.InnerResults != null)
            {
                foreach (var item in res.InnerResults)
                {
                    LogTrace(item);
                }
            }
        }

        public void Log(string text, string suffix, LogLevels logLevel)
        {
            if (_filterLogLevels == null || _filterLogLevels.Contains(logLevel))
            {
                OnLog?.Invoke(text, suffix, logLevel);
            }
        }

        public void Log(string text, LogLevels logLevel)
        {
            Log(text, null, logLevel);
        }

        public void WriteKeyToStorage(string hid, byte[] key, byte scheme = 0x80)
        {
            OnLogKey?.Invoke(hid, key, scheme);
        }

        private LogLevels[] _filterLogLevels;
        public void ResetFilterLog(LogLevels[] filterLogLevels)
        {
            _filterLogLevels = filterLogLevels;
        }
    }
}
