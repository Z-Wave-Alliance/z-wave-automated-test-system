// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: 2025 Z-Wave Alliance https://z-wavealliance.org
using System;
using System.IO;
using System.Linq;
using System.Text;
using Utils;
using Utils.UI.Enums;
using Zats.Services;
using ZWave.ZipApplication.Devices;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using ZWave.ZnifferApplication.Enums;

namespace ZatsTestConsole
{
    public class ConsoleCustomOutput
    {
        public static readonly string ZATS_START_TAG = "::StartTag::";
        public static readonly string ZATS_END_TAG = "::EndTag::";

        private readonly object _consoleLocker = new object();
        private ConsoleOutputDecorator _consoleOutputDecorator;

        internal StreamWriter Out { get; set; }
        internal StreamWriter TotalOut { get; set; }
        internal StreamWriter ArchiveTotalOut { get; set; }
        internal StreamWriter ArchiveOut { get; set; }

        internal bool IsLessOutput { get; set; }

        internal bool UseStartStopTags { get; set; }

        public ConsoleCustomOutput(bool isLessOutput, bool isOutputColorFormated, bool useStartStopTags)
        {
            IsLessOutput = isLessOutput;
            _consoleOutputDecorator = new ConsoleOutputDecorator
            {
                IsLess = IsLessOutput,
                IsOutputColorFormated = isOutputColorFormated
            };
            UseStartStopTags = useStartStopTags;

        }

        internal void SetDumpFile(string folder, string testCaseName)
        {
            if (Out != null)
            {
                Out.Close();
            }

            Out = new StreamWriter(Path.Combine(folder, $"{testCaseName}.txt"));
            Out.AutoFlush = true;
        }

        internal void SetArchiveDumpFile(string folder, string testCaseName)
        {
            ArchiveOut = new StreamWriter(Path.Combine(folder, $"{testCaseName}.txt"));
            ArchiveOut.AutoFlush = true;
        }

        internal void ConsoleWriteLine()
        {
            if (!IsLessOutput)
            {
                Console.Out.WriteLine();
            }
            Out?.WriteLine();
            ArchiveOut?.WriteLine();
        }

        internal void ConsoleWriteLineTotal()
        {
            if (IsLessOutput)
            {
                Console.Out.WriteLine();
            }
            TotalOut?.WriteLine();
            ArchiveTotalOut?.WriteLine();
        }

        internal void ConsoleWrite(string text)
        {
            if (!IsLessOutput)
            {
                Console.Out.Write(text);
            }
            Out?.Write(text);
            ArchiveOut?.Write(text);
        }

        internal void ConsoleWriteFull(char symbol)
        {
            string text = "";
            ConsoleWrite(text);
        }

        internal void ConsoleWriteTotal(string text)
        {
            if (IsLessOutput)
            {
                Console.Out.Write(text);
            }
            TotalOut?.Write(text);
            ArchiveTotalOut?.Write(text);
        }

        internal void ConsoleWriteLine(string text)
        {
            ConsoleWrite(text);
            ConsoleWriteLine();
        }

        internal void ConsoleWriteLineTotal(string text)
        {
            ConsoleWriteTotal(text);
            ConsoleWriteLineTotal();
        }

        internal void ConsoleWriteLine(string text, LogLevels logLevel)
        {
            ConsoleColor color = GetLogLevelColor(logLevel);
            ConsoleWriteLine(text, color);
        }

        private void ConsoleWrite(string text, ConsoleColor color)
        {
            _consoleOutputDecorator.WithColor(color, text, t => ConsoleWrite(t));
        }

        private void ConsoleWriteLine(string text, ConsoleColor color)
        {
            _consoleOutputDecorator.WithColor(color, text, t => ConsoleWriteLine(t));
        }

        private void ConsoleWriteTotal(string text, ConsoleColor color)
        {
            _consoleOutputDecorator.WithColor(color, text, t => ConsoleWriteTotal(t));
        }

        private void ConsoleWriteLineTotal(string text, ConsoleColor color)
        {
            _consoleOutputDecorator.WithColor(color, text, t => ConsoleWriteLineTotal(t));
        }

        private void ConsoleWriteFullLine(char symbol, ConsoleColor color)
        {
            string text = GetFullLine(symbol);
            _consoleOutputDecorator.WithColor(color, text, t => ConsoleWriteLine(t));
        }

        private string GetFullLine(char symbol)
        {
            int textWidth = 80;

            if (Console.IsOutputRedirected)
            {
                // MKL: Accessing Console.BufferWidth throws an exception if ZATS is executed from CTT,
                // the default defined above is too small however and causes a lot of line breaks in the CTT output log
                textWidth = 120;
            }
            try
            {
                textWidth = (Console.BufferWidth - 1) > 20 ? (Console.BufferWidth - 1) : textWidth;
            }
            catch { }
            return new string(symbol, textWidth);
        }

        internal void ReportDevicesToConsole(TestCaseDefinition tcDefinition, IManagersContainer managers, string rfRegion, 
            byte? lrChannelConfigCode = null, string lrChannelConfigDescription = null)
        {
            lock (_consoleLocker)
            {
                string snifferName = string.Empty;
                bool isSnifferPresent = managers.ZnifferManager.Zniffer != null;
                if (isSnifferPresent)
                {
                    snifferName = managers.ZnifferManager.Zniffer is ZWave.ZnifferApplication.Devices.Zniffer ? "Zniffer" : "Sniffer PTI";
                }

                ConsoleWriteLine();
                if (UseStartStopTags)
                {
                    ConsoleWriteLine(ZATS_START_TAG);
                }
                ConsoleWrite("    ", ConsoleColor.White);
                ConsoleWrite(snifferName, ConsoleColor.Yellow);
                if (isSnifferPresent)
                {
                    var znifferAppClient = managers.ZnifferManager.ZnifferAppClient;
                    if (znifferAppClient.DataSource != null)
                    {
                        var revision = (managers.ZnifferManager.Zniffer as ZWave.ZnifferApplication.Devices.Zniffer)?.Revision;
                        ConsoleWrite($" ");
                        ConsoleWrite($"{znifferAppClient.Version}");
                        if (revision.HasValue)
                            ConsoleWrite($".{revision}");
                        ConsoleWrite($" ");
                        //ConsoleWrite($" ");
                        string rfRegionName;
                        if (managers.ZnifferManager.Zniffer is ZWave.ZnifferApplication.Devices.Zniffer znifferApp)
                        {
                            rfRegionName = znifferApp.RfRegionName ?? // Value retrieved during device init or try get by code for old devices.
                                managers.ZnifferManager.FrameDefinition.RadioFrequency?.FirstOrDefault(x => x.Code == znifferApp.Frequency)?.Text;
                        }
                        else
                        {
                            rfRegionName = rfRegion; // Use value from CLI arguments in case PTI Zniffer (doesn't support SerialAPI to return RfRegions).
                        }

                        ConsoleWrite($"{rfRegionName}", ConsoleColor.Yellow);
                        if (lrChannelConfigCode != null && RfRegionsExtensions.IsLongRangeRfRegion(rfRegionName))
                        {
                            ConsoleWrite($":", ConsoleColor.Yellow);
                            ConsoleWrite($" CH_CFG{lrChannelConfigCode.ToString()}", ConsoleColor.Yellow);
                            if (!string.IsNullOrEmpty(lrChannelConfigDescription))
                            {
                                ConsoleWrite($" [{lrChannelConfigDescription}]", ConsoleColor.Gray);
                            }
                        }
                        ConsoleWrite($" ");
                        ConsoleWrite($"{znifferAppClient.DataSource.SourceName}", ConsoleColor.DarkYellow);
                        ConsoleWrite($" ^{znifferAppClient.SessionId:00} ");
                        if (managers.ZnifferManager.Zniffer.Frequencies == null)
                        {
                            ConsoleWrite("Check Zniffer port!", ConsoleColor.Red);
                        }
                    }
                }
                ConsoleWriteLine();

                foreach (var item in tcDefinition.Devices)
                {
                    ConsoleWrite($" {item.Key}.", ConsoleColor.White);
                    var sdev = managers.DeviceManager.GetDeviceHostClient(item.Key);
                    if (sdev != null)
                    {
                        ConsoleWrite($" ");
                        ConsoleWrite($"{sdev.Library}", ConsoleColor.Yellow);
                        ConsoleWrite($" ");
                        ConsoleWrite($"{sdev.Host.ChipSeries}");
                        if (!string.IsNullOrWhiteSpace(sdev.Frequency))
                        {
                            ConsoleWrite($" {sdev.Frequency}", ConsoleColor.Cyan);
                        }
                        if (sdev.DevKit != DevKits.Default)
                        {
                            ConsoleWrite($"{sdev.DevKit}");
                        }

                        ConsoleWrite(!string.IsNullOrWhiteSpace(sdev.Version) ? " " : "");
                        ConsoleWrite($"{sdev.Version}");
                        ConsoleWrite($" ");
                        ConsoleWriteDeviceHost(sdev);
                        ConsoleWrite($" ");
                        ConsoleWriteDeviceClientHost(sdev);
                        ConsoleWriteLine();
                    }
                    else if (managers.PCControllerDeviceManager.GetUIInstance(item.Key) != null)
                    {
                        var pcdev = managers.PCControllerDeviceManager.GetDeviceHostClient(item.Key);
                        if (pcdev != null)
                        {
                            ConsoleWrite($" ");
                            ConsoleWrite("PCController", ConsoleColor.DarkCyan);
                            ConsoleWrite($" ");
                            ConsoleWrite($"{pcdev.Library}", ConsoleColor.Yellow);
                            ConsoleWrite($" ");
                            ConsoleWrite($" {pcdev.Host.ChipSeries}");
                            ConsoleWrite(!string.IsNullOrWhiteSpace(pcdev.Version) ? " " : "");
                            ConsoleWrite($"{pcdev.Version}");
                            ConsoleWrite($" ");
                            ConsoleWriteDeviceHost(pcdev);
                            ConsoleWrite($" ");
                            ConsoleWriteDeviceClientHost(pcdev);
                            ConsoleWriteLine();
                        }
                    }
                    else
                    {
                        ConsoleWrite($"{GetText(item.Value.Libraries)}", ConsoleColor.Red);
                        if (!string.IsNullOrEmpty(item.Value.Alias))
                        {
                            ConsoleWrite($" \"{item.Value.Alias}\"", ConsoleColor.Gray);
                        }
                        ConsoleWrite($" {item.Value.ChipSeries}", ConsoleColor.Red);
                        ConsoleWriteLine();
                    }
                }

                foreach (var item in tcDefinition.DeviceGroups)
                {
                    var sdevs = managers.DeviceManager.GetDeviceHostClients(item.Key);
                    if (sdevs != null)
                    {
                        foreach (var sdev in sdevs)
                        {
                            ConsoleWrite($" {sdev.DeviceAlias}.", ConsoleColor.White);
                            ConsoleWrite($" ");
                            ConsoleWrite($"{sdev.Library}", ConsoleColor.Yellow);
                            ConsoleWrite($" ");
                            ConsoleWrite($"{sdev.Host.ChipSeries}");
                            if (!string.IsNullOrWhiteSpace(sdev.Frequency))
                            {
                                ConsoleWrite($" {sdev.Frequency}", ConsoleColor.Cyan);
                            }
                            if (sdev.DevKit != DevKits.Default)
                            {
                                ConsoleWrite($"{sdev.DevKit}");
                            }

                            ConsoleWrite(!string.IsNullOrWhiteSpace(sdev.Version) ? " " : "");
                            ConsoleWrite($"{sdev.Version}");
                            ConsoleWrite($" ");
                            ConsoleWriteDeviceHost(sdev);
                            ConsoleWrite($" ");
                            ConsoleWriteDeviceClientHost(sdev);
                            ConsoleWriteLine();
                        }
                    }
                    else
                    {
                        ConsoleWrite($"{GetText(item.Value.Libraries)}", ConsoleColor.Red);
                        ConsoleWrite($" {item.Value.ChipSeries}", ConsoleColor.Red);
                        ConsoleWriteLine();
                    }
                }

                foreach (var item in tcDefinition.DebugDevices)
                {
                    ConsoleWrite($" {item.Key}.", ConsoleColor.White);
                    var sdev = managers.DebugDeviceManager.GetDevice(item.Key);
                    if (sdev != null)
                    {
                        if (!string.IsNullOrEmpty(item.Value.Alias))
                        {
                            ConsoleWrite($" \"{item.Value.Alias}\"", ConsoleColor.Gray);
                        }
                        ConsoleWrite($" {sdev.ChipType} {sdev.Version}");
                        ConsoleWrite($" {sdev.DataSource?.SourceName}", ConsoleColor.DarkYellow);
                        ConsoleWrite($" ^");
                        ConsoleWrite($"{sdev.SessionId:00}");
                    }
                    ConsoleWriteLine();
                }

                foreach (var item in tcDefinition.ZipDevices)
                {
                    ConsoleWrite($" {item.Key}.", ConsoleColor.Cyan);
                    if (!string.IsNullOrEmpty(item.Value.Alias))
                    {
                        ConsoleWrite($" \"{item.Value.Alias}\"", ConsoleColor.Gray);
                    }
                    var device = managers.ZipManager.GetDevice(item.Key);
                    ConsoleWrite($" {device?.DataSource?.SourceName}", ConsoleColor.DarkYellow);
                    ConsoleWrite($" ^");
                    ConsoleWrite($"{device?.SessionId:00}");
                    ConsoleWriteLine();
                }

                foreach (var item in tcDefinition.Instruments)
                {
                    ConsoleWrite($" {item.Key}.", ConsoleColor.Cyan);
                    if (!string.IsNullOrEmpty(item.Value.Alias))
                    {
                        ConsoleWrite($" \"{item.Value.Alias}\"", ConsoleColor.Gray);
                    }
                    var instrument = managers.InstrumentsManager.GetInstrument(item.Key);
                    ConsoleWrite($" ");
                    ConsoleWrite($"{instrument?.Version}", ConsoleColor.Yellow);
                    ConsoleWrite($" ");
                    ConsoleWrite($" {instrument?.DataSource?.SourceName}", ConsoleColor.DarkYellow);
                    ConsoleWrite($" ^");
                    ConsoleWrite($"{instrument?.SessionId:00}");
                    ConsoleWriteLine();
                }
                ConsoleWriteLine();

                if (UseStartStopTags)
                {
                    ConsoleWriteLine(ZATS_END_TAG);
                }
            }
        }

        private void ConsoleWriteDeviceClientHost(DeviceHostClient dhClient)
        {
            ConsoleWrite($"^", ConsoleColor.DarkGray);
            ConsoleWrite($"{dhClient.SClient?.SessionId:00}", ConsoleColor.DarkGray);
            ConsoleWrite(dhClient.SClient?.DataSource != null && dhClient.PClient?.DataSource != null ? "." : "", ConsoleColor.DarkGray);
            ConsoleWrite($"{dhClient.PClient?.SessionId:00}", ConsoleColor.DarkGray);
        }

        private void ConsoleWriteDeviceHost(DeviceHostClient sdev)
        {
            ConsoleWrite($"{sdev.Host.SName}", ConsoleColor.DarkYellow);
            ConsoleWrite(!string.IsNullOrWhiteSpace(sdev.Host.PName) && !(string.IsNullOrWhiteSpace(sdev.Host.SName)) ? " TI:" : "", ConsoleColor.DarkGray);
            ConsoleWrite($"{sdev.Host.PName}", ConsoleColor.DarkCyan);
            ConsoleWrite(!string.IsNullOrWhiteSpace(sdev.Host.JName) && !(string.IsNullOrWhiteSpace(sdev.Host.SName) && string.IsNullOrWhiteSpace(sdev.Host.PName)) ? " " : "", ConsoleColor.DarkGray);
            ConsoleWrite($"{sdev.Host.JName}", ConsoleColor.Cyan);
            ConsoleWrite(!string.IsNullOrWhiteSpace(sdev.Host.Alias) && !(string.IsNullOrWhiteSpace(sdev.Host.SName) && string.IsNullOrWhiteSpace(sdev.Host.PName) && string.IsNullOrWhiteSpace(sdev.Host.JName)) ? " " : "", ConsoleColor.DarkGray);
            if (!string.IsNullOrWhiteSpace(sdev.Host.Alias))
            {
                ConsoleWrite($"\"{sdev.Host.Alias}\"", ConsoleColor.Gray);
            }
            ConsoleWrite(!string.IsNullOrWhiteSpace(sdev.Host.Hop) && !(string.IsNullOrWhiteSpace(sdev.Host.Alias) && string.IsNullOrWhiteSpace(sdev.Host.SName) && string.IsNullOrWhiteSpace(sdev.Host.PName) && string.IsNullOrWhiteSpace(sdev.Host.JName)) ? " " : "", ConsoleColor.DarkGray);
            if (!string.IsNullOrWhiteSpace(sdev.Host.Hop))
            {
                ConsoleWrite($"Hop{sdev.Host.Hop}", ConsoleColor.Gray);
            }
        }

        internal void ReportProfileInformationToConsole(Profile activeProfile)
        {
            lock (_consoleLocker)
            {
                var wasLess = IsLessOutput;
                IsLessOutput = true;

                if (UseStartStopTags)
                {
                    ConsoleWriteLine();
                    ConsoleWriteLine(ZATS_START_TAG);
                }

                ConsoleWriteTotal(Tools.FormatStr("Profile:", activeProfile.Name));
                ConsoleWriteTotal(Tools.FormatStr("{0}", activeProfile.Name), ConsoleColor.Yellow);
                ConsoleWriteLineTotal();
                foreach (var item in activeProfile.DeviceHosts
                    .OrderBy(x => x.SName != null)
                    .ThenBy(x => x.JName != null)
                    .ThenBy(x => x.PName != null))
                {
                    ConsoleWriteTotal($"  ");

                    if (!string.IsNullOrWhiteSpace(item.SName))
                    {
                        ConsoleWriteTotal($"SName", ConsoleColor.DarkGray);
                        ConsoleWriteTotal($"=", ConsoleColor.DarkGray);
                        ConsoleWriteTotal($"{item.SName}", ConsoleColor.DarkYellow);
                        ConsoleWriteTotal($" ");
                        if (item.IsUZB)
                        {
                            ConsoleWriteTotal($"IsUZB", ConsoleColor.DarkGray);
                            ConsoleWriteTotal($"=", ConsoleColor.DarkGray);
                            ConsoleWriteTotal($"true", ConsoleColor.Yellow);
                            ConsoleWriteTotal($" ");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(item.PName))
                    {
                        ConsoleWriteTotal($"PName", ConsoleColor.DarkGray);
                        ConsoleWriteTotal($"=", ConsoleColor.DarkGray);
                        ConsoleWriteTotal($"{item.PName}", ConsoleColor.DarkCyan);
                        ConsoleWriteTotal($" ");
                    }

                    if (!string.IsNullOrWhiteSpace(item.JName))
                    {
                        ConsoleWriteTotal($"JName", ConsoleColor.DarkGray);
                        ConsoleWriteTotal($"=", ConsoleColor.DarkGray);
                        ConsoleWriteTotal($"{item.JName}", ConsoleColor.Cyan);
                        ConsoleWriteTotal($" ");
                    }

                    if (!string.IsNullOrWhiteSpace(item.ChipSeries))
                    {
                        ConsoleWriteTotal($"ChipSeries", ConsoleColor.DarkGray);
                        ConsoleWriteTotal($"=", ConsoleColor.DarkGray);
                        ConsoleWriteTotal($"{item.ChipSeries}", string.IsNullOrWhiteSpace(item.ChipSeries) || item.ChipSeries == "Default" ? ConsoleColor.Red : ConsoleColor.Gray);
                        ConsoleWriteTotal($" ");
                    }

                    if (!string.IsNullOrWhiteSpace(item.Alias))
                    {
                        ConsoleWriteTotal($"Alias", ConsoleColor.DarkGray);
                        ConsoleWriteTotal($"=", ConsoleColor.DarkGray);
                        ConsoleWriteTotal($"{item.Alias}", ConsoleColor.Gray);
                        ConsoleWriteTotal($" ");
                    }

                    ConsoleWriteLineTotal();
                }
                ConsoleWriteLineTotal();

                if (UseStartStopTags)
                {
                    ConsoleWriteLine(ZATS_END_TAG);
                }

                IsLessOutput = wasLess;
            }
        }

        internal void ReportTestCaseInformationToConsole(TestCaseDefinition tcDefinition)
        {
            lock (_consoleLocker)
            {
                if (UseStartStopTags)
                {
                    ConsoleWriteLine();
                    ConsoleWriteLine(ZATS_START_TAG);
                }
                ConsoleWriteFullLine('-', ConsoleColor.White);
                ConsoleWrite($"{tcDefinition.Name}", ConsoleColor.White);
                ConsoleWrite(" ");
                ConsoleWrite($"{tcDefinition.Description}", ConsoleColor.Yellow);
                ConsoleWriteLine();
                ConsoleWriteFullLine('-', ConsoleColor.White);

                ConsoleWriteLine();
                if (UseStartStopTags)
                {
                    ConsoleWriteLine(ZATS_END_TAG);
                }
            }
        }

        private string GetText(LibTypes[] libTypes)
        {
            var sb = new StringBuilder();
            sb.Append(" ");
            if (libTypes != null)
            {
                if (libTypes.SequenceEqual(new[] { LibTypes.ControllerStaticLib, LibTypes.ControllerBridgeLib, LibTypes.ControllerPortableLib, LibTypes.InstallerLib }))
                {
                    sb.Append(LibTypes.Controller);
                }
                else if (libTypes.SequenceEqual(new[] { LibTypes.EndDeviceLib, LibTypes.EndDeviceLegacyRoutingLib, LibTypes.EndDeviceLegacyLib }))
                {
                    sb.Append(LibTypes.EndDeviceAny);
                }
                else
                {
                    for (int i = 0; i < libTypes.Length; i++)
                    {
                        if (i != 0)
                            sb.Append(" | ");
                        sb.Append(libTypes[i]);
                    }
                }
            }
            return sb.ToString();
        }

        internal void ReportConnectionStatusToConsole(ManagersContainer managers, TestCaseDefinition tcDefinition)
        {
            foreach (var item in tcDefinition.ZipDevices)
            {
                bool failed = true;
                string message = null;
                var device = managers.ZipManager.GetDevice(item.Key);
                if (device != null)
                {
                    var zipController = device as ZipController;
                    if (zipController != null)
                    {
                        if (!zipController.Discovered)
                            message = "failed to discover zip gateway";
                        else if (!zipController.TransportClient.IsOpen)
                            message = "failed to connect zip gateway";
                        else
                            failed = false;
                    }
                    else if (!device.TransportClient.IsOpen)
                        message = "failed to connect to virtual device";
                    else
                        failed = false;
                }
                else
                {
                    ConsoleWrite(string.Format(" device with key {0} not found", item.Key));
                }
                if (failed)
                {
                    ConsoleWrite(Tools.FormatStr("ZipDevice"));
                    ConsoleWrite(Tools.FormatStr("{0} ", item.Key), ConsoleColor.Yellow);
                    ConsoleWrite(Tools.FormatStr("{0} ", item.Value.Alias), ConsoleColor.Yellow);
                    ConsoleWriteLine(message, ConsoleColor.Red);
                }
            }
        }

        internal void SetTotalDumpFile(string archiveFolder)
        {
            TotalOut = new StreamWriter("dump.txt");
            TotalOut.AutoFlush = true;

            if (!Directory.Exists(archiveFolder))
            {
                Directory.CreateDirectory(archiveFolder);
            }
            ArchiveTotalOut = new StreamWriter(Path.Combine(archiveFolder, "dump.txt"));
            ArchiveTotalOut.AutoFlush = true;
        }

        private void ReportToConsole(int deviceNumber, LibTypes library, string port, string programmingPort, int y, string memory)
        {
            lock (_consoleLocker)
            {
                Console.SetCursorPosition(0, 2 * y);
                ConsoleWrite(Tools.FormatStr("prgm:>"));
                ConsoleWrite(Tools.FormatStr("{0} ", library), ConsoleColor.Yellow);
                ConsoleWrite(Tools.FormatStr("to Device"));
                ConsoleWrite(Tools.FormatStr("{0} ", deviceNumber), ConsoleColor.Yellow);
                ConsoleWrite(Tools.FormatStr("{0} @ {1}->{2}\r", memory, programmingPort, port));
            }
        }

        internal void ReportResultToConsole(TestCaseDefinition tcDefinition, TestCaseResult result, Stopwatch timeInfo, int runCounter, int tcCounter, String tcMessage = "")
        {
            lock (_consoleLocker)
            {
                if (UseStartStopTags)
                {
                    ConsoleWriteLine();
                    ConsoleWriteLine(ZATS_START_TAG);
                }

                var etm = timeInfo.Elapsed;
                var hetm = string.Empty;
                if (((int)etm.TotalHours) > 0)
                {
                    hetm = $"{((int)etm.TotalHours).ToString().PadLeft(2)} hr ";
                }
                var metm = string.Empty;
                if (etm.Minutes > 0)
                {
                    metm = $"{etm.Minutes.ToString().PadLeft(2)} min ";
                }
                var setm = $"{etm.Seconds.ToString().PadLeft(2)} sec";

                var color = ConsoleColor.White;
                switch (result)
                {
                    case TestCaseResult.Pass:
                        color = ConsoleColor.Green;
                        break;
                    case TestCaseResult.Fail:
                        color = ConsoleColor.Red;
                        break;
                    case TestCaseResult.Skip:
                        color = ConsoleColor.DarkYellow;
                        break;
                    default:
                        break;
                }
                var asterix = result != TestCaseResult.Pass ? "*" : " ";
                var desc = tcDefinition.Description ?? "";
                desc = desc.Replace("\r\n", " ");
                desc = desc.Replace('\r', ' ');
                if (desc.Length > 144)
                {
                    desc = desc.Substring(0, 144);
                }
                ConsoleWriteTotal($"{asterix}", color);
                ConsoleWriteTotal(" | ");
                ConsoleWriteTotal($"{tcCounter.ToString().PadLeft(4)}");
                ConsoleWriteTotal(" | ");
                ConsoleWriteTotal($"{DateTime.Now:HH:mm:ss}");
                ConsoleWriteTotal(" | ");
                ConsoleWriteTotal($"{result}", color);
                ConsoleWriteTotal(" | ");
                ConsoleWriteTotal($"{hetm}{metm}{setm}".PadLeft(20));
                ConsoleWriteTotal(" | ");
                ConsoleWriteTotal($"{tcDefinition.Name}", ConsoleColor.White);
                ConsoleWriteTotal(" ");
                ConsoleWriteTotal($"{desc}", ConsoleColor.Yellow);
                ConsoleWriteTotal(" | ");
                ConsoleWriteTotal($"{tcMessage}", ConsoleColor.White);
                ConsoleWriteLineTotal();

                ConsoleWriteLine(result.ToString(), color);
                ConsoleWriteLine($"Elapsed: {hetm}{metm}{setm}");

                // 2 lines between TCs
                ConsoleWriteLine();
                ConsoleWriteLine();

                if (UseStartStopTags)
                {
                    ConsoleWriteLine(ZATS_END_TAG);
                }
            }
        }

        internal void ReportToConsole(string text, string suffix, LogLevels logLevel)
        {
            var fcl = GetLogLevelColor(logLevel);
            string msg = GetLogLevelMessage(text, logLevel);
            lock (_consoleLocker)
            {
                ReportToConsole(new[] { new Tuple<string, ConsoleColor>(msg, fcl) }, suffix);
            }
        }

        internal void ReportDiagnosticToConsole(bool isOk, string text)
        {
            var fcl1 = GetLogLevelColor(isOk ? LogLevels.Ok : LogLevels.Fail);
            var fcl2 = GetLogLevelColor(LogLevels.Diagnostic);
            lock (_consoleLocker)
            {
                ReportToConsole(new[] { new Tuple<string, ConsoleColor>(isOk ? "[ OK ] " : "[Fail] ", fcl1), new Tuple<string, ConsoleColor>(text, fcl2) }, null);
            }
        }

        private void ReportToConsole(IEnumerable<Tuple<string, ConsoleColor>> msgs, string suffix)
        {
            int textWidth = 80 - 13;

            if (Console.IsOutputRedirected)
            {
                // MKL: Accessing Console.BufferWidth throws an exception if ZATS is executed from CTT,
                // the default defined above is too small however and causes a lot of line breaks in the CTT output log
                textWidth = 120;
            }
            else
            {
                try
                {
                    textWidth = (Console.BufferWidth - 14) > 20 ? (Console.BufferWidth - 14) : textWidth;
                }
                catch { }
            }

            if (UseStartStopTags)
            {
                ConsoleWriteLine(ZATS_START_TAG);
            }

            bool needSuffix = !string.IsNullOrEmpty(suffix);
            var timestampMask = "HH:mm:ss.fff ";
            var indent = timestampMask.Length;
            ConsoleWrite(Tools.CurrentDateTime.ToString(timestampMask), ConsoleColor.DarkGray);
            int index = 0;
            int position = 0;
            foreach (var tuple in msgs)
            {
                var msg = tuple.Item1;
                var color = tuple.Item2;
                string[] lines = msg.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
                for (int j = 0; j < lines.Length; j++)
                {
                    index = 0;
                    string line = lines[j];
                    if (line == string.Empty)
                    {
                        continue;
                    }
                    if (j > 0)
                    {
                        position = 0;
                        ConsoleWriteLine();
                        ConsoleWrite(new string(' ', indent));
                    }

                    while (index < line.Length)
                    {
                        // KNC: replaced following line to break the line automatically on a white space character

                        //var item = new string(line.Skip(index).Take(textWidth - position).ToArray());

                        string item;
                        bool autoLineBreak = false;

                        // do not break lines that start with message box control sequences
                        if (line.StartsWith("»"))
                        {
                            item = line;
                        }
                        else
                        {
                            #region AutoLineBreak

                            if (index + textWidth - position < line.Length)
                            {
                                item = new string(line.Skip(index).Take(textWidth - position).ToArray());

                                int charIndexToBreak = item.LastIndexOf(@" ");

                                if (charIndexToBreak > 0)
                                {
                                    item = new string(item.Take(charIndexToBreak + 1).ToArray());
                                }
                                autoLineBreak = true;
                            }
                            else
                            {
                                item = new string(line.Skip(index).Take(textWidth - position).ToArray());
                            }
                            #endregion // AutoLinebreak
                        }

                        index += item.Length;
                        position += item.Length;

                        item = item.TrimEnd();

                        if (!string.IsNullOrEmpty(item))
                        {
                            ConsoleWrite(item, color);
                            if (index >= line.Length && needSuffix)
                            {
                                needSuffix = false;
                                if (item.Length + suffix.Length >= textWidth)
                                {
                                    ConsoleWriteLine();
                                    ConsoleWrite(new string(' ', indent));
                                    ConsoleWrite(new string(' ', textWidth - (suffix.Length % textWidth)));
                                    ConsoleWrite(suffix.Substring(0, suffix.Length % textWidth), ConsoleColor.DarkGray);
                                    suffix = suffix.Substring(suffix.Length % textWidth, suffix.Length - suffix.Length % textWidth);
                                    while (suffix.Length >= textWidth)
                                    {
                                        ConsoleWriteLine();
                                        ConsoleWrite(new string(' ', indent));
                                        ConsoleWrite(suffix.Substring(0, textWidth), ConsoleColor.DarkGray);
                                        suffix = suffix.Substring(textWidth, suffix.Length - textWidth);
                                    }
                                }
                                else
                                {
                                    ConsoleWrite(new string(' ', textWidth - (item.Length + suffix.Length)));
                                    ConsoleWrite(suffix, ConsoleColor.DarkGray);
                                }

                            }
                        }
                        if (position >= textWidth || autoLineBreak)
                        {
                            position = 0;
                            ConsoleWriteLine();
                            ConsoleWrite(new string(' ', indent));
                        }
                    }
                }
            }
            ConsoleWriteLine();
            if (UseStartStopTags)
            {
                ConsoleWriteLine(ZATS_END_TAG);
            }
        }

        private static string GetLogLevelMessage(string text, LogLevels logLevel)
        {
            string ret = text ?? "<null>";
            switch (logLevel)
            {
                case LogLevels.Done:
                    ret = "Done " + ret;
                    break;
                case LogLevels.Ok:
                    ret = "OK " + ret;
                    break;
                case LogLevels.Warning:
                    ret = string.Format("*{0}Warning {1}{0}*", Environment.NewLine, ret);
                    break;
                case LogLevels.Error:
                    ret = string.Format("*{0}Error {1}{0}*", Environment.NewLine, ret);
                    break;
                case LogLevels.Fail:
                    ret = string.Format("*{0}Fail {1}{0}*", Environment.NewLine, ret);
                    break;
                default:
                    break;
            }
            return ret;
        }

        private static ConsoleColor GetLogLevelColor(LogLevels logLevel)
        {
            var ret = Console.ForegroundColor;
            switch (logLevel)
            {
                case LogLevels.Title:
                    ret = ConsoleColor.White;
                    break;
                case LogLevels.Text:
                    ret = ConsoleColor.Gray;
                    break;
                case LogLevels.Trace:
                    ret = ConsoleColor.Cyan;
                    break;
                case LogLevels.ZnifferTrace:
                    ret = ConsoleColor.DarkCyan;
                    break;
                case LogLevels.Information:
                    ret = ConsoleColor.DarkYellow;
                    break;
                case LogLevels.Done:
                    ret = ConsoleColor.DarkGreen;
                    break;
                case LogLevels.Ok:
                    ret = ConsoleColor.Green;
                    break;
                case LogLevels.Warning:
                    ret = ConsoleColor.Yellow;
                    break;
                case LogLevels.Error:
                case LogLevels.Fail:
                    ret = ConsoleColor.Red;
                    break;
                case LogLevels.Diagnostic:
                    ret = ConsoleColor.DarkGray;
                    break;
                default:
                    break;
            }
            return ret;
        }

        internal void ReportDevicesToConsole(List<DeviceHostClient> hostsWithKnownVersion)
        {
            if (UseStartStopTags)
            {
                ConsoleWriteLine(ZATS_START_TAG);
            }

            foreach (var sdev in hostsWithKnownVersion)
            {
                ConsoleWrite(Tools.FormatStr("    {0}", sdev.Library), ConsoleColor.Yellow);
                ConsoleWrite(Tools.FormatStr(" {0} {1}", sdev.Host.ChipSeries, sdev.Version));
                ConsoleWrite($" {sdev.SClient?.DataSource?.SourceName}", ConsoleColor.Yellow);
                if (sdev.SClient?.DataSource != null && sdev.PClient?.DataSource != null)
                {
                    ConsoleWrite(".");
                }
                ConsoleWrite($"{sdev.PClient?.DataSource?.SourceName}", ConsoleColor.Yellow);
                ConsoleWriteLine();
            }
            ConsoleWriteLine();
            ConsoleWriteLine();

            if (UseStartStopTags)
            {
                ConsoleWriteLine(ZATS_END_TAG);
            }
        }
    }
}
