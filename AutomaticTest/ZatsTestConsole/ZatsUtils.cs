// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: 2025 Z-Wave Alliance https://z-wavealliance.org
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Utils;
using Zats.Services;
using ZWave.Enums;
using ZWave.Xml.Application;
using ZWave.Xml.FrameHeader;

namespace ZatsTestConsole
{
    public static class ZatsUtils
    {
        private const string SETTINGS_FILE = @"ZatsDefinition.xml";
        private const string PROFILE_FILE = @"ZatsProfile.xml";
        private const string CACHE_FILE = @"ZatsCache.xml";
        private const string ZWAVE_DEFINITION_FILE = @"ZWave_cmd_classes.xml";
        private const string FRAME_DEFINITION_FILE = @"FrameDefinition.xml";

        public static CmdLineSettings ParseArguments(string[] args)
        {
            var cmdLineSettings = new CmdLineSettings();
            var errorMsgs = new StringBuilder();
            if (args != null)
            {
                LazyDictionary<CmdLineArgument, string> commands = new LazyDictionary<CmdLineArgument, string>();
                CmdLineArgument key = CmdLineArgument.Empty;
                foreach (string cmd in args)
                {
                    var arg = CmdLineArgs.Find(cmd);
                    if (arg != CmdLineArgument.Empty)
                    {
                        key = arg;
                        commands.Add(key, null);
                    }
                    else
                    {
                        if (key != CmdLineArgument.Empty)
                        {
                            commands[key] += " " + cmd;
                            commands[key] = commands[key].Trim();
                        }
                    }
                }
                if (commands.ContainsKey(CmdLineArgs.HELP) || commands.Count == 0)
                {
                    cmdLineSettings.HasHelp = true;
                    cmdLineSettings.Help = CmdLineArgs.GetHelp(CmdLineArgs.HELP);
                }

                // Mandatory Parameters

                cmdLineSettings.AssemblyFile = NormalizePath(commands[CmdLineArgs.INPUT], true);
                string rfRegionsStr = commands[CmdLineArgs.FREQUENCY] ?? string.Empty;
                if (rfRegionsStr == "ALL")
                {
                    rfRegionsStr = "ANZ,CN,EU,EU_LR,HK,IL,IN,JP,KR,RU,US,US_LR";
                }
                cmdLineSettings.Frequencies = rfRegionsStr?.Split(',', ';');
                foreach (string item in cmdLineSettings.Frequencies)
                {
                    if (!Enum.TryParse(item, true, out RfRegions rfRegion))
                    {
                        cmdLineSettings.HasParseError = true;
                        var erTxt = string.IsNullOrEmpty(item) ?
                            $"For REGION argument was not specified parameter" :
                            $"For REGION argument a wrong parameter <{item}> was specified: <{commands[CmdLineArgs.FREQUENCY]}>";
                        errorMsgs.AppendLine(erTxt);
                        errorMsgs.AppendLine(CmdLineArgs.GetHelp(CmdLineArgs.FREQUENCY));
                        break;
                    }
                }

                // Optional Parameters

                string lrChannelsStr = commands[CmdLineArgs.LR_CHANNEL] ?? string.Empty;
                string[] lrChannels = lrChannelsStr?.Split(',', ';');
                cmdLineSettings.LongRangeChannels = new LongRangeChannels[0];
                foreach (string ch in lrChannels)
                {
                    switch (ch)
                    {
                        case "a":
                        case "A":
                            if (!cmdLineSettings.LongRangeChannels.Contains(LongRangeChannels.ChannelA))
                            {
                                cmdLineSettings.LongRangeChannels = cmdLineSettings.LongRangeChannels.Append(LongRangeChannels.ChannelA).ToArray();
                            }
                            continue;
                        case "b":
                        case "B":
                            if (!cmdLineSettings.LongRangeChannels.Contains(LongRangeChannels.ChannelB))
                            {
                                cmdLineSettings.LongRangeChannels = cmdLineSettings.LongRangeChannels.Append(LongRangeChannels.ChannelB).ToArray();
                            }
                            continue;
                        default:
                            if (!string.IsNullOrEmpty(ch))
                            {
                                cmdLineSettings.HasParseError = true;
                                var erTxt = $"For LR_CHANNEL argument a wrong parameter <{ch}> was specified: <{commands[CmdLineArgs.LR_CHANNEL]}>";
                                errorMsgs.AppendLine(erTxt);
                                errorMsgs.AppendLine(CmdLineArgs.GetHelp(CmdLineArgs.LR_CHANNEL));
                            }
                            continue;
                    }
                }

                var strMode = commands[CmdLineArgs.RUN_MODE] ?? RunModeName.READONLY;
                cmdLineSettings.RunMode =
                    strMode.Contains(RunModeName.CLEAN) ? RunModes.Clean :
                    strMode.Contains(RunModeName.OVERWRITE) ? RunModes.Overwrite :
                    strMode.Contains(RunModeName.APPEND) ? RunModes.Append :
                    RunModes.Readonly;
                cmdLineSettings.IgnoreMode =
                    strMode.Contains(RunModeName.CONTINUE_ON_FAIL) ? IgnoreModes.Fail :
                    IgnoreModes.None;

                cmdLineSettings.CreateDumpFile = commands.ContainsKey(CmdLineArgs.DUMP);
                cmdLineSettings.IsOutputColorFormated = commands.ContainsKey(CmdLineArgs.OUTPUT_COLOR_FORMATED);
                cmdLineSettings.UseStartStopTags = commands.ContainsKey(CmdLineArgs.OUTPUT_USE_START_STOP_TAGS);
                cmdLineSettings.IsLessOutput = commands.ContainsKey(CmdLineArgs.LESS);
                cmdLineSettings.IsNoZnfDevice = commands.ContainsKey(CmdLineArgs.NOZNF);
                int.TryParse(commands[CmdLineArgs.NUMBER_OF_RUNS] ?? "1", out cmdLineSettings.NumberOfRuns);
                int.TryParse(commands[CmdLineArgs.NUMBER_OF_EXTRA_RUNS] ?? "1", out cmdLineSettings.NumberOfExtraRuns);
                cmdLineSettings.BasePath = NormalizePath(commands[CmdLineArgs.BASE_PATH], true);
                cmdLineSettings.InputFilter = NormalizePath(commands[CmdLineArgs.INPUT_FILTER], true);
                cmdLineSettings.IPFilter = NormalizePath(commands[CmdLineArgs.IP_FILTER], true);
                cmdLineSettings.IsZnifferVirtual = commands.ContainsKey(CmdLineArgs.ZNIFFER_IS_VIRTUAL);

                cmdLineSettings.ReportsPath = commands[CmdLineArgs.REPORTS_PATH]?.TrimEnd('/', '\\', '"', '\'');
                cmdLineSettings.ReportsPath = NormalizePath(cmdLineSettings.ReportsPath, false);
                cmdLineSettings.ConfigPath = commands[CmdLineArgs.CONFIG_PATH]?.TrimEnd('/', '\\', '"', '\'');
                cmdLineSettings.ConfigPath = NormalizePath(cmdLineSettings.ConfigPath, false);
                int.TryParse(commands[CmdLineArgs.TIMEOUT], out cmdLineSettings.TimeoutSeconds);
                cmdLineSettings.ChipSeries = commands[CmdLineArgs.CHIP_SERIES];
                cmdLineSettings.ProfileName = commands[CmdLineArgs.PROFILE];
                MessageBoxImpl.TimeOut = cmdLineSettings.TimeoutSeconds;
                var st = commands[CmdLineArgs.RUN_SERVICES_TYPE];
                if (st == RunServiceName.PC_CONTROLLER_UI)
                {
                    cmdLineSettings.RunServiceType = RunServiceType.PCControllerUI;
                }
                else if (st == RunServiceName.LINK_FRAMEWORK)
                {
                    cmdLineSettings.RunServiceType = RunServiceType.LinkFramework;
                }
                else
                {
                    cmdLineSettings.RunServiceType = RunServiceType.Basic;
                }
                cmdLineSettings.JLinkDevicesFilter = NormalizePath(commands[CmdLineArgs.JLINK_DEVICES_FILTER], true);
                cmdLineSettings.MeshRoutersDevicesFilter = NormalizePath(commands[CmdLineArgs.MESHROUTERS_DEVICES_FILTER], true);
                cmdLineSettings.TestBedsFilter = NormalizePath(commands[CmdLineArgs.TEST_BEDS_FILTER], true);
                if (commands.ContainsKey(CmdLineArgs.COMMANDER_APP_PATH))
                {
                    var pth = NormalizePath(commands[CmdLineArgs.COMMANDER_APP_PATH], true);
                    if (File.Exists(pth))
                    {
                        cmdLineSettings.CommanderAppPath = pth;
                    }
                    else if (File.Exists(Path.Combine(pth, "commander.exe")))
                    {
                        cmdLineSettings.CommanderAppPath = Path.Combine(pth, "commander.exe");
                    }
                    else if (File.Exists(Path.Combine(pth, "commander")))
                    {
                        cmdLineSettings.CommanderAppPath = Path.Combine(pth, "commander");
                    }
                    else if (Directory.Exists(pth))
                    {
                        cmdLineSettings.CommanderAppPath =
                            GetApplicationPath(pth, "commander.exe") ??
                            GetApplicationPath(pth, "commander");
                    }
                    if (string.IsNullOrEmpty(cmdLineSettings.CommanderAppPath))
                    {
                        cmdLineSettings.HasParseError = true;
                        var erTxt = string.IsNullOrEmpty(commands[CmdLineArgs.COMMANDER_APP_PATH]) ?
                            $"For COMMANDER APPLICATION PATH argument was not specified parameter" :
                            $"For COMMANDER APPLICATION PATH argument was specified an invalid path: <{commands[CmdLineArgs.COMMANDER_APP_PATH]}>";
                        errorMsgs.AppendLine(erTxt);
                        errorMsgs.AppendLine(CmdLineArgs.GetHelp(CmdLineArgs.COMMANDER_APP_PATH));
                    }
                }
            }
            if (cmdLineSettings.HasParseError)
            {
                cmdLineSettings.ParseErrorMessage = errorMsgs.ToString();
            }
            return cmdLineSettings;
        }

        private static string NormalizePath(string argument, bool checkIsExists)
        {
            var str = argument?.TrimEnd('/', '\\', '"', '\'');
            var tkens = str?.Split(';', ',');
            for (int i = 0; tkens != null && i < tkens.Length; i++)
            {
                if (!checkIsExists || Directory.Exists(tkens[i]) || File.Exists(tkens[i]))
                {
                    tkens[i] = Path.GetFullPath(tkens[i]);
                }
            }
            str = tkens?.Aggregate((x, y) => $"{x};{y}");
            return str;
        }

        internal static string GetApplicationPath(string rootPath, string applicationFile)
        {
            string ret = null;
            List<DirectoryInfo> dirList = new List<DirectoryInfo>();
            string[] patterns = new string[] { "*silabs*", "*silicon*", "*simplicity*", "*commander*" };

            if (rootPath == null)
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType == DriveType.Fixed)
                    {
                        try
                        {
                            DirectoryInfo di = drive.RootDirectory;
                            foreach (var item in patterns)
                            {
                                var dirs = di.GetDirectories(item, SearchOption.TopDirectoryOnly);
                                if (dirs != null)
                                {
                                    dirList.AddRange(dirs);
                                }
                            }
                        }
                        catch (IOException ex)
                        {
                            $"{ex.Message}"._DLOG();
                        }
                    }
                }
            }
            else
            {
                DirectoryInfo di = new DirectoryInfo(rootPath);
                foreach (var item in patterns)
                {
                    var dirs = di.GetDirectories(item, SearchOption.TopDirectoryOnly);
                    if (dirs != null)
                    {
                        dirList.AddRange(dirs);
                    }
                }
            }

            foreach (var dir in dirList)
            {
                try
                {
                    var files = dir.GetFiles(applicationFile, SearchOption.AllDirectories);
                    if (files != null && files.Length > 0)
                    {
                        ret = files[0].FullName;
                        break;
                    }
                }
                catch (UnauthorizedAccessException)
                { }
            }
            return ret;
        }

        private static byte GetFrequencyCode(FrameDefinition frameDefinition, string frequency)
        {
            RadioFrequency rf = frameDefinition.RadioFrequency.FirstOrDefault(x => x.Name == frequency);
            if (rf != null)
                return rf.Code;
            else
                return 0;
        }

        public static string GetFrequencyName(FrameDefinition frameDefinition, byte frequency)
        {
            RadioFrequency rf = frameDefinition.RadioFrequency.FirstOrDefault(x => x.Code == frequency);
            if (rf != null)
                return rf.Name;
            else
                return null;
        }

        public static ZWaveDefinition LoadZWaveDefinition()
        {
            ZWaveDefinition ret = null;
            string zWaveXmlRoot = GetZWaveDllXmlFilesDirectory();
            if (zWaveXmlRoot != null)
            {
                var zwaveSystemDefFilePath = Path.Combine(zWaveXmlRoot, ZWAVE_DEFINITION_FILE);
                FileAccessMonitor.Read(zwaveSystemDefFilePath, () => ret = ZWaveDefinition.Load(zwaveSystemDefFilePath));
            }
            return ret;
        }

        public static FrameDefinition LoadFrameDefinition()
        {
            FrameDefinition ret = null;
            string zWaveXmlRoot = GetZWaveDllXmlFilesDirectory();
            if (zWaveXmlRoot != null)
            {
                var frameDefFilePath = Path.Combine(zWaveXmlRoot, FRAME_DEFINITION_FILE);
                FileAccessMonitor.Read(frameDefFilePath, () => ret = FrameDefinition.Load(frameDefFilePath));
            }
            return ret;
        }

        public static string GetZatsXmlFilesDirectory(string zatsConfigPath)
        {
            if (string.IsNullOrWhiteSpace(zatsConfigPath))
            {
                var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return FindZatsXmlFilesDirectory(location);
            }
            else
            {
                return zatsConfigPath;
            }
        }

        private static string FindZatsXmlFilesDirectory(string childLeaf)
        {
            string zatsXmlRoot = childLeaf;
            var dirPath = Path.Combine(zatsXmlRoot, "XmlFiles");
            dirPath += Path.DirectorySeparatorChar;
            if (!Directory.Exists(dirPath))
            {
                try
                {
                    zatsXmlRoot = Directory.GetParent(childLeaf).FullName;
                    return FindZatsXmlFilesDirectory(zatsXmlRoot);
                }
                catch (Exception)
                {
                    return null;
                }
            }
            else
            {
                return dirPath;
            }
        }

        private static string GetZWaveDllXmlFilesDirectory()
        {
            var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return FindZWaveDllXmlFilesDirectory(location);
        }

        private static string FindZWaveDllXmlFilesDirectory(string childLeaf)
        {
            string zatsXmlRoot = childLeaf;
            var dirPath = Path.Combine(zatsXmlRoot, "Libraries");
            dirPath = Path.Combine(dirPath, "ZWaveDll");
            dirPath = Path.Combine(dirPath, "XmlFiles");
            dirPath += Path.DirectorySeparatorChar;
            if (!Directory.Exists(dirPath))
            {
                try
                {
                    zatsXmlRoot = Directory.GetParent(childLeaf).FullName;
                    return FindZWaveDllXmlFilesDirectory(zatsXmlRoot);
                }
                catch (Exception)
                {
                    return null;
                }
            }
            else
            {
                return dirPath;
            }
        }

        public static ZatsDefinition LoadZatsDefinition(string zatsXmlRoot)
        {
            ZatsDefinition ret = null;
            if (zatsXmlRoot != null)
            {
                string xmlZatsDefinitionFileName = Path.Combine(zatsXmlRoot, SETTINGS_FILE);
                FileAccessMonitor.Read(xmlZatsDefinitionFileName, () =>
                {
                    XmlSerializer zwClassXmlSerializer = new XmlSerializer(typeof(ZatsDefinition));
                    XmlReader reader = XmlReader.Create(xmlZatsDefinitionFileName);
                    try
                    {
                        ret = (ZatsDefinition)zwClassXmlSerializer.Deserialize(reader);
                    }
                    catch (InvalidOperationException e)
                    {
                        Console.WriteLine("Exception catched:");
                        Console.WriteLine(e.ToString());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception catched:");
                        Console.WriteLine(e.ToString());
                    }
                    finally
                    {
                        reader.Close();
                    }
                });
            }
            return ret;
        }

        internal static bool SaveZatsDefinition(ZatsDefinition zatsDefinition, string zatsXmlRoot)
        {
            bool ret = false;
            if (zatsXmlRoot != null)
            {
                XmlSerializer zwClassXmlSerializer = new XmlSerializer(typeof(ZatsDefinition));
                XmlWriterSettings sett = new XmlWriterSettings();
                sett.Indent = true;
                sett.NewLineChars = Environment.NewLine;
                sett.OmitXmlDeclaration = true;
                XmlSerializerNamespaces names = new XmlSerializerNamespaces();
                names.Add(string.Empty, string.Empty);
                string xmlZatsProfileFileName = Path.Combine(zatsXmlRoot, SETTINGS_FILE);
                XmlWriter writer = XmlWriter.Create(xmlZatsProfileFileName, sett);
                try
                {
                    zwClassXmlSerializer.Serialize(writer, zatsDefinition, names);
                    ret = true;
                }
                catch (InvalidOperationException e)
                {
                    Console.WriteLine("Exception catched:");
                    Console.WriteLine(e.ToString());
                }
                finally
                {
                    Console.WriteLine($"ZatsDefinition saved in {xmlZatsProfileFileName}");
                    writer.Flush();
                    writer.Close();
                }
            }
            return ret;
        }

        public static ZatsProfile LoadZatsProfile(string zatsXmlRoot)
        {
            ZatsProfile ret = null;
            if (zatsXmlRoot != null)
            {
                string xmlZatsProfileFileName = Path.Combine(zatsXmlRoot, PROFILE_FILE);
                FileAccessMonitor.Read(xmlZatsProfileFileName, () =>
                {
                    XmlSerializer zwClassXmlSerializer = new XmlSerializer(typeof(ZatsProfile));
                    XmlReader reader = XmlReader.Create(xmlZatsProfileFileName);
                    try
                    {
                        ret = (ZatsProfile)zwClassXmlSerializer.Deserialize(reader);
                    }
                    catch (InvalidOperationException ex)
                    {
                        $"{ex.Message}"._DLOG();
                    }
                    finally
                    {
                        reader.Close();
                    }
                });
            }
            return ret;
        }

        public static bool SaveZatsProfile(ZatsProfile ZatsProfile, string zatsXmlRoot)
        {
            bool ret = false;
            XmlSerializer zwClassXmlSerializer = new XmlSerializer(typeof(ZatsProfile));

            XmlWriterSettings sett = new XmlWriterSettings();
            sett.Indent = true;
            sett.NewLineChars = Environment.NewLine;
            sett.OmitXmlDeclaration = true;
            XmlSerializerNamespaces names = new XmlSerializerNamespaces();
            names.Add(string.Empty, string.Empty);
            string xmlZatsProfileFileName = Path.Combine(zatsXmlRoot, PROFILE_FILE);
            XmlWriter writer = XmlWriter.Create(xmlZatsProfileFileName, sett);
            try
            {
                zwClassXmlSerializer.Serialize(writer, ZatsProfile, names);
                ret = true;
            }
            catch (InvalidOperationException ex)
            {
                $"{ex.Message}"._DLOG();
            }
            finally
            {
                writer.Close();
            }
            return ret;
        }

        internal static void DeleteZatsCache(string zatsLogPath)
        {
            if (zatsLogPath != null && File.Exists(Path.Combine(zatsLogPath, CACHE_FILE)))
            {
                File.Delete(Path.Combine(zatsLogPath, CACHE_FILE));
            }
        }

        public static ZatsCache LoadZatsCache(string zatsLogPath)
        {
            ZatsCache ret = null;
            if (zatsLogPath != null)
            {
                string xmlZatsCacheFileName = Path.Combine(zatsLogPath, CACHE_FILE);
                FileAccessMonitor.Read(xmlZatsCacheFileName, () =>
                {
                    XmlSerializer zwClassXmlSerializer = new XmlSerializer(typeof(ZatsCache));
                    XmlReader reader = XmlReader.Create(xmlZatsCacheFileName);
                    try
                    {
                        ret = (ZatsCache)zwClassXmlSerializer.Deserialize(reader);
                    }
                    catch (InvalidOperationException e)
                    {
                        Console.WriteLine("Exception catched:");
                        Console.WriteLine(e.ToString());
                    }
                    finally
                    {
                        reader.Close();
                    }
                });
            }
            return ret;
        }

        public static bool SaveZatsTestSuites(testsuites testsuites, string path)
        {
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            bool ret = false;
            XmlSerializer zwClassXmlSerializer = new XmlSerializer(typeof(testsuites));

            XmlWriterSettings sett = new XmlWriterSettings();
            sett.Indent = true;
            sett.NewLineChars = Environment.NewLine;
            //sett.OmitXmlDeclaration = true;
            XmlSerializerNamespaces names = new XmlSerializerNamespaces();
            names.Add(String.Empty, string.Empty);
            string xmlZatsCacheFileName = Path.Combine(path, "TestResult.xml");
            XmlWriter writer = XmlWriter.Create(xmlZatsCacheFileName, sett);
            try
            {
                zwClassXmlSerializer.Serialize(writer, testsuites, names);
                ret = true;
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("Exception catched:");
                Console.WriteLine(e.ToString());
            }
            finally
            {
                writer.Close();
            }
            return ret;
        }

        public static bool SaveZatsCache(ZatsCache ZatsCache, string zatsLogPath)
        {
            bool ret = false;
            XmlSerializer zwClassXmlSerializer = new XmlSerializer(typeof(ZatsCache));

            XmlWriterSettings sett = new XmlWriterSettings();
            sett.Indent = true;
            sett.NewLineChars = Environment.NewLine;
            sett.OmitXmlDeclaration = true;
            XmlSerializerNamespaces names = new XmlSerializerNamespaces();
            names.Add(string.Empty, string.Empty);
            string xmlZatsCacheFileName = Path.Combine(zatsLogPath, CACHE_FILE);
            XmlWriter writer = XmlWriter.Create(xmlZatsCacheFileName, sett);
            try
            {
                zwClassXmlSerializer.Serialize(writer, ZatsCache, names);
                ret = true;
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("Exception catched:");
                Console.WriteLine(e.ToString());
            }
            finally
            {
                writer.Close();
            }
            return ret;
        }

        private static bool FileReadyToRead(string filePath, int maxDuration)
        {
            int readAttempt = 0;
            while (readAttempt < maxDuration)
            {
                readAttempt++;
                try
                {
                    using (StreamReader stream = new StreamReader(filePath))
                    {
                        return true;
                    }
                }
                catch
                {
                    System.Threading.Thread.Sleep(1000);
                }
            }
            return false;
        }

        public static LibTypes GetLibType(Libraries library)
        {
            LibTypes lType = LibTypes.ControllerStaticLib;
            switch (library)
            {
                case Libraries.NoLib:
                    break;
                case Libraries.ControllerStaticLib:
                    lType = LibTypes.ControllerStaticLib;
                    break;
                case Libraries.ControllerPortableLib:
                    lType = LibTypes.ControllerPortableLib;
                    break;
                case Libraries.EndDeviceLib:
                    lType = LibTypes.EndDeviceLib;
                    break;
                case Libraries.EndDeviceSysTestLib:
                    lType = LibTypes.EndDeviceSysTestLib;
                    break;
                case Libraries.EndDeviceLegacyLib:
                    lType = LibTypes.EndDeviceLegacyLib;
                    break;
                case Libraries.InstallerLib:
                    lType = LibTypes.InstallerLib;
                    break;
                case Libraries.EndDeviceLegacyRoutingLib:
                    lType = LibTypes.EndDeviceLegacyRoutingLib;
                    break;
                case Libraries.ControllerBridgeLib:
                    lType = LibTypes.ControllerBridgeLib;
                    break;
                case Libraries.TestController:
                    lType = LibTypes.TestController;
                    break;
                default:
                    break;
            }
            return lType;
        }

        public static Libraries GetLibrary(LibTypes libType)
        {
            Libraries ret = Libraries.ControllerStaticLib;
            switch (libType)
            {
                case LibTypes.ControllerStaticLib:
                    ret = Libraries.ControllerStaticLib;
                    break;
                case LibTypes.ControllerPortableLib:
                    ret = Libraries.ControllerPortableLib;
                    break;
                case LibTypes.EndDeviceSysTestLib:
                    ret = Libraries.EndDeviceSysTestLib;
                    break;
                case LibTypes.EndDeviceLib:
                    ret = Libraries.EndDeviceLib;
                    break;
                case LibTypes.EndDeviceLegacyLib:
                    ret = Libraries.EndDeviceLegacyLib;
                    break;
                case LibTypes.InstallerLib:
                    ret = Libraries.InstallerLib;
                    break;
                case LibTypes.EndDeviceLegacyRoutingLib:
                    ret = Libraries.EndDeviceLegacyRoutingLib;
                    break;
                case LibTypes.ControllerBridgeLib:
                    ret = Libraries.ControllerBridgeLib;
                    break;
                case LibTypes.TestController:
                    ret = Libraries.TestController;
                    break;
                case LibTypes.DoorLockKeyPad:
                    ret = Libraries.DoorLockKeyPad;
                    break;
                case LibTypes.LEDBulb:
                    ret = Libraries.LEDBulb;
                    break;
                case LibTypes.ZnifferPTI:
                    ret = Libraries.ZnifferPTI;
                    break;
                case LibTypes.PowerStrip:
                    ret = Libraries.PowerStrip;
                    break;
                case LibTypes.SensorPIR:
                    ret = Libraries.SensorPIR;
                    break;
                case LibTypes.SwitchOnOff:
                    ret = Libraries.SwitchOnOff;
                    break;
                case LibTypes.WallController:
                    ret = Libraries.WallController;
                    break;
                case LibTypes.MultilevelSensor:
                    ret = Libraries.MultilevelSensor;
                    break;
                case LibTypes.RailTest:
                    ret = Libraries.RailTest;
                    break;
                case LibTypes.KeyFob:
                    ret = Libraries.KeyFob;
                    break;
                case LibTypes.NoLib:
                    ret = Libraries.NoLib;
                    break;
                default:
                    break;
            }
            return ret;
        }

        internal static Libraries[] GetLibraries(LibTypes[] libTypes)
        {
            Libraries[] ret = new Libraries[libTypes.Length];
            for (int i = 0; i < libTypes.Length; i++)
            {
                ret[i] = GetLibrary(libTypes[i]);
            }
            return ret;
        }
    }
}
