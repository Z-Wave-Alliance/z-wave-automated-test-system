/// SPDX-License-Identifier: BSD-3-Clause
/// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
/// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using Utils;
using ZWave.Enums;

namespace Zats.Services
{
    public class FileSystemManager : IFileSystemManager
    {
        private const string RESOURSES_FOLDER = "Resources";
        private const string FilePatternDefinitionName = "FilePatternDefinition.xml";

        private Action<bool, string> _logAction;
        private string _defaultResourcesPath;
        private FilePatternDefinition _definition = null;

        public string BasePath { get; set; }

        public FileSystemManager(Action<bool, string> logAction, string resourcesPath)
        {
            _logAction = logAction;
            _defaultResourcesPath = string.IsNullOrEmpty(resourcesPath) ?
                 Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), RESOURSES_FOLDER) :
                 Path.Combine(resourcesPath, RESOURSES_FOLDER);
            if (_definition == null)
            {
                var location = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "XmlFiles");
                _definition = GetFilePatternDefinition(location, FilePatternDefinitionName);
            }
        }

        private FilePatternDefinition GetFilePatternDefinition(string path, string fileName)
        {
            FilePatternDefinition ret = null;
            XmlSerializer serializer = new XmlSerializer(typeof(FilePatternDefinition));
            var files = new DirectoryInfo(path).GetFiles(fileName, SearchOption.TopDirectoryOnly);
            if (files != null && files.Length > 0)
            {
                var file = files.OrderByDescending(x => x.CreationTime).First();
                using (FileStream fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
                using (XmlReader reader = XmlReader.Create(fs))
                {
                    ret = (FilePatternDefinition)serializer.Deserialize(reader);
                }
            }
            return ret;
        }

        private static FileSystemInfo[] GetFullNames(FileSystemInfo[] fs, params string[] cascadeFilters)
        {
            FileSystemInfo[] ret = fs;
            if (fs != null && fs.Length > 0)
            {
                foreach (var filter in cascadeFilters)
                {
                    if (!string.IsNullOrEmpty(filter))
                    {
                        var rgx = new Regex(filter, RegexOptions.IgnoreCase);
                        ret = ret.Where(x => rgx.Match(x.FullName).Success).ToArray();
                    }
                }
            }
            return ret;
        }

        private static string[] GetFullNames(string[] fs, params string[] cascadeFilters)
        {
            string[] ret = fs;
            if (fs != null && fs.Length > 0)
            {
                foreach (var filter in cascadeFilters)
                {
                    if (!string.IsNullOrEmpty(filter))
                    {
                        var rgx = new Regex(filter, RegexOptions.IgnoreCase);
                        ret = ret.Where(x => rgx.Match(x).Success).ToArray();
                    }
                }
            }
            return ret;
        }

        private static string[] GetExistingFileNames(string[] fs, params string[] cascadeFilters)
        {
            string[] ret = fs;
            foreach (var filter in cascadeFilters)
            {
                var rgx = new Regex(filter, RegexOptions.IgnoreCase);
                var tmpFs = ret.Where(x => rgx.Match(x).Success);
                if (tmpFs.Any())
                {
                    ret = tmpFs.ToArray();
                }
            }
            return ret;
        }

        public string GetLatestFilePath(Libraries lib, string chipSeries, string frequency, DevKits devKit, params string[] filters)
        {
            return GetLatestFilePath(lib, chipSeries, frequency, null, devKit, filters);
        }

        public string GetLatestFilePath(Libraries lib, string chipSeries, string frequency, string ending, DevKits devKit, params string[] filters)
        {
            string resultFilePath = null;
            var requiresSpecific = new[]
            {
                ChipSeries.ZW080x, ChipSeries.ZW070x, ChipSeries.ZW0713, ChipSeries.ZW0714, ChipSeries.Default
            }.Select(x => x.ToString());

            if (!requiresSpecific.Contains(chipSeries))
            {
                var binPat = _definition?.BinaryPatterns?.FirstOrDefault(x => x.ChipSeries == chipSeries.ToString());
                if (binPat == null)
                {
                    var brdTokens = chipSeries.ToString().Split('_');
                    if (brdTokens != null && brdTokens.Length == 2)
                    {
                        binPat = new BinaryPattern();
                        binPat.ChipSeries = chipSeries.ToString();
                        binPat.PathFilters = _definition.PathFilters.Select(x => new PathFilter()
                        {
                            Pattern = x.Pattern.Replace("{Brd0}", brdTokens[0]).Replace("{Brd1}", brdTokens[1])
                        }).ToArray();
                    }
                }

                string devkitsRootFolders = BasePath + ";" + _defaultResourcesPath;
                if (!string.IsNullOrEmpty(devkitsRootFolders))
                {
                    var folders = devkitsRootFolders.Split(';');
                    foreach (var devkitsRootFolder in folders)
                    {
                        if (string.IsNullOrEmpty(devkitsRootFolder) || !Directory.Exists(devkitsRootFolder))
                        {
                            continue;
                        }
                        var lm = binPat.LibTokens?.FirstOrDefault(x => x.Library == lib.ToString());
                        if (string.IsNullOrWhiteSpace(lm?.Token))
                        {
                            lm = _definition.LibTokens.FirstOrDefault(x => x.Library == lib.ToString());
                            if (string.IsNullOrWhiteSpace(lm?.Token))
                            {
                                lm = new LibToken() { Library = lib.ToString(), Token = lib.ToString() };
                            }
                        }
                        var freq = lm.SkipFrequency ? string.Empty : frequency;
                        var end = ending ?? string.Empty;
                        resultFilePath = GetLatestFilePathInternal(devKit, filters, devkitsRootFolder, binPat, lm, freq, end, string.Empty);

                        if (!string.IsNullOrEmpty(resultFilePath))
                        {
                            break;
                        }
                        else //look for DEBUG versions:
                        {
                            resultFilePath = GetLatestFilePathInternal(devKit, filters, devkitsRootFolder, binPat, lm, freq, "_DEBUG" + end, string.Empty);
                        }

                        if (!string.IsNullOrEmpty(resultFilePath))
                        {
                            break;
                        }
                        else //use hardcoded files from resources:
                        {
                            var location = _defaultResourcesPath;
                            var rtResourceName = string.Empty;
                            switch (lib)
                            {
                                case Libraries.RailTest: //from ZATS\Resources\railtest
                                    if ((new[] { ChipSeries.ZG23_BRD4204C, ChipSeries.ZG23_BRD4204D, ChipSeries.ZG23_BRD4210A })
                                        .Select(x => x.ToString()).Contains(chipSeries))
                                    {
                                        rtResourceName = "rt823B.hex";
                                    }
                                    else if ((new[] { ChipSeries.ZGM230S_BRD4205A, ChipSeries.ZGM230S_BRD4205B, ChipSeries.ZGM230_BRD2603A })
                                        .Select(x => x.ToString()).Contains(chipSeries))
                                    {
                                        rtResourceName = "rt8230.hex";
                                    }
                                    else if ((new[] { ChipSeries.xG28_BRD4400C, ChipSeries.xG28_BRD4401C })
                                        .Select(x => x.ToString()).Contains(chipSeries))
                                    {
                                        rtResourceName = "rtxG28.hex";
                                    }
                                    else if ((new[] { ChipSeries.ZG14_BRD4201A, ChipSeries.ZG14_BRD4206A, ChipSeries.ZG14_BRD4208A })
                                     .Select(x => x.ToString()).Contains(chipSeries))
                                    {
                                        rtResourceName = "rt714.hex";
                                    }
                                    else if ((new[] { ChipSeries.ZGM130S_BRD4200A, ChipSeries.ZGM130S_BRD4202A, ChipSeries.ZGM130S_BRD4207A, ChipSeries.RZ13_BRD4209A })
                                  .Select(x => x.ToString()).Contains(chipSeries))
                                    {
                                        rtResourceName = "rt713.hex";
                                    }
                                    break;
                                default:
                                    break;
                            }
                            var resourcePath = Path.Combine(location, rtResourceName);
                            if (File.Exists(resourcePath))
                            {
                                resultFilePath = resourcePath;
                            }
                        }
                    }
                }
                else
                {
                    _logAction(false, "'Base path' parameter (-bp <path>) is missing");
                }
                if (string.IsNullOrWhiteSpace(resultFilePath))
                {
                    _logAction(false, $"Not Found {lib}, {binPat.ChipSeries}, {frequency}{ending} in {devkitsRootFolders}");
                }
            }
            else
            {
                throw new ArgumentException($"Invalid chip series {chipSeries} specified");
            }
            return resultFilePath;
        }

        private string GetLatestFilePathInternal(DevKits devKit, string[] filters, string devkitsRootFolder, BinaryPattern binPat, LibToken lm, string freq, string end, string subtype)
        {
            string resultFilePath = null;
            foreach (var pathFilterPattern in binPat.PathFilters)
            {
                foreach (var libToken in lm.Token.Split(','))
                {
                    var pathFilter = pathFilterPattern.Pattern.
                                                Replace("{Lib}", libToken).
                                                Replace("{SubType}", subtype).
                                                Replace("{Freq}", freq).
                                                Replace("{End}", end);

                    string[] regexFilters = new string[] { "" };
                    string[] priorityFilters = new string[] { "" };
                    if (binPat.RegexFilters != null && binPat.RegexFilters.Count > 0)
                    {
                        regexFilters = binPat.RegexFilters.ToArray();
                    }
                    if (binPat.PriorityRegexFilters != null && binPat.PriorityRegexFilters.Count > 0)
                    {
                        priorityFilters = binPat.PriorityRegexFilters.ToArray();
                    }
                    regexFilters = regexFilters.Concat(filters).ToArray();
                    priorityFilters = priorityFilters.Concat(new[] { ".hex" }).ToArray();
                    var di = new DirectoryInfo(devkitsRootFolder);
                    resultFilePath = GetFile(di, pathFilter, regexFilters, priorityFilters);
                    if (!string.IsNullOrWhiteSpace(resultFilePath))
                    {
                        break;
                    }
                }
                if (!string.IsNullOrWhiteSpace(resultFilePath))
                {
                    break;
                }
            }
            return resultFilePath;
        }

        private string GetFile(DirectoryInfo di, string pathFilter, string[] regexFilters, string[] priorityFilters)
        {
            string ret = null;
            string[] fs = null;
            try
            {
#if !NETCOREAPP
                fs = di.GetFiles(pathFilter, SearchOption.TopDirectoryOnly).Select(x => x.FullName).ToArray();
#else
                fs = Directory.GetFiles(di.FullName, pathFilter, new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });
#endif
                fs = GetFullNames(fs, regexFilters);
                fs = GetExistingFileNames(fs, priorityFilters);
            }
            catch (Exception ex)
            {
                "{0}"._DLOG(ex);
            }

            string file = fs?.FirstOrDefault();
            if (file != null)
            {
                ret = file;
                _logAction(true, $"{file}");
            }
            else
            {
                var dirs = di.GetDirectories();
                if (dirs != null && dirs.Length > 0)
                {
                    foreach (var dir in dirs.OrderByDescending(x => x.CreationTime))
                    {
                        ret = GetFile(dir, pathFilter, regexFilters, priorityFilters);
                        if (ret != null)
                        {
                            break;
                        }
                    }
                }
            }
            return ret;
        }

        public string GetLatestFilePath(string filter, DevKits devKit, params string[] filters)
        {
            var resultFilePath = string.Empty;
            string devkitsRootFolders = BasePath;
            if (!string.IsNullOrEmpty(devkitsRootFolders))
            {
                var folders = devkitsRootFolders.Split(';');
                foreach (var devkitsRootFolder in folders)
                {
                    if (string.IsNullOrEmpty(devkitsRootFolder) || !Directory.Exists(devkitsRootFolder))
                    {
                        continue;
                    }

                    string[] regexFilters = new string[] { "" };
                    regexFilters = regexFilters.Concat(filters).ToArray();

                    var di = new DirectoryInfo(devkitsRootFolder);
                    FileSystemInfo[] dirs = di.GetDirectories();
                    if (dirs == null || dirs.Length == 0)
                    {
                        dirs = new[] { di };
                    }
                    if (devKit != DevKits.Default)
                    {
                        dirs = GetFullNames(dirs, devKit.ToString().Substring(1));
                    }
                    foreach (DirectoryInfo dir in dirs.OrderByDescending(x => x.CreationTime))
                    {
                        try
                        {
#if !NETCOREAPP
                            var fs = dir.GetFiles(filter, SearchOption.AllDirectories).Select(x => x.FullName).ToArray();
#else
                            var fs = Directory.GetFiles(dir.FullName, filter, new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = true });
#endif
                            fs = GetFullNames(fs, regexFilters);
                            if (fs != null)
                            {
                                var file = fs.FirstOrDefault();
                                if (file != null)
                                {
                                    resultFilePath = file;
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            "{0}"._DLOG(ex);
                        }
                    }
                    if (!string.IsNullOrEmpty(resultFilePath))
                    {
                        break;
                    }
                }
            }
            return resultFilePath;
        }

        public BootloaderPath GetBootloaderPath(DeviceHostClient deviceHostClient, string[] specificFileNamePattern, params string[] regexFilters)
        {
            BootloaderPath ret = new BootloaderPath();
            if (deviceHostClient.Library.IsTerminalApplication()) return ret;

            bool isSpecialCase = DeviceManager.SpecificChips.Any(x => deviceHostClient.Host.ChipSeries.Contains(x));
            var isSerialApiControlled = !deviceHostClient.Library.IsSampleApplication();
            var isSAPI = isSerialApiControlled && !isSpecialCase;
            var defaultSearchPattern = string.Empty;
            if (_definition != null && _definition.DefaultBootloaderInfos != null && _definition.DefaultBootloaderInfos.Length > 1)
            {
                //Set predefined keys from RESOURSES_FOLDER:
                var idx = isSAPI ? 1 : 0;
                var keyTokensFileName = _definition.DefaultBootloaderInfos?[idx].KeyTokensFileName;
                var encryptKeyFileName = _definition.DefaultBootloaderInfos?[idx].EncryptKeyFileName;
                var signKeyFileName = _definition.DefaultBootloaderInfos?[idx].SignKeyFileName;

                var idxBrd = deviceHostClient.Host.ChipSeries.ToLower().IndexOf("brd");
                var brdNumber = string.Concat(deviceHostClient.Host.ChipSeries.Skip(idxBrd)); // specify filter for board from chip version.
                defaultSearchPattern = _definition.DefaultBootloaderInfos?[idx].Pattern.Replace("{brdNumber}", brdNumber);

                var location = _defaultResourcesPath;
                ret.KeysToken = Path.Combine(location, keyTokensFileName);
                ret.EncryptKey = Path.Combine(location, encryptKeyFileName);
                ret.SignKey = Path.Combine(location, signKeyFileName);
            }

            var expectedFileNames = specificFileNamePattern ?? new[] { defaultSearchPattern };
            foreach (var item in expectedFileNames)
            {
                var bootloaderFilePath = GetLatestFilePath(item, DevKits.Default, regexFilters);
                ret.Bootloader = bootloaderFilePath;

                //try to update keys if exists from detected bootloader path:
                if (!string.IsNullOrEmpty(bootloaderFilePath) && File.Exists(bootloaderFilePath))
                {
                    var newKeysToken = Path.Combine(Path.GetDirectoryName(bootloaderFilePath), Path.GetFileName(ret.KeysToken));
                    if (File.Exists(newKeysToken))
                        ret.KeysToken = newKeysToken;
                    var newEncKey = Path.Combine(Path.GetDirectoryName(bootloaderFilePath), Path.GetFileName(ret.EncryptKey));
                    if (File.Exists(newEncKey))
                        ret.EncryptKey = newEncKey;
                    var newsignKey = Path.Combine(Path.GetDirectoryName(bootloaderFilePath), Path.GetFileName(ret.SignKey));
                    if (File.Exists(newsignKey))
                        ret.SignKey = newsignKey;

                    break;
                }
            }

            return ret;
        }

        public string GetPredefinedControllerPathFromResources(ChipSeries chipSeries)
        {
            var expectedFileName = string.Empty;
            switch (chipSeries)
            {
                case ChipSeries.ZG23_BRD4204C:
                case ChipSeries.ZG23_BRD4204D:
                case ChipSeries.ZG23_BRD4210A:
                    expectedFileName = "sac823us.hex";
                    break;
                case ChipSeries.ZGM230S_BRD4205B:
                    expectedFileName = "sac8230sbus.hex";
                    break;
                case ChipSeries.ZGM230_BRD2603A:
                    expectedFileName = "zwave_ncp_serial_api_controller-brd2603a-us.hex";
                    break;
                case ChipSeries.ZGM230S_BRD4205A:
                    expectedFileName = "sac8230saus.hex";
                    break;
                case ChipSeries.ZGM130S_BRD4200A:
                case ChipSeries.ZGM130S_BRD4202A:
                case ChipSeries.ZGM130S_BRD4207A:
                case ChipSeries.RZ13_BRD4209A:
                    expectedFileName = "sact713us.hex";
                    break;
                case ChipSeries.RZ13_BRD4201B:
                    expectedFileName = "sac713_32us.hex";
                    break;
                case ChipSeries.RZ13_BRD4203A:
                case ChipSeries.ZG13S_BRD4201D:
                case ChipSeries.ZG13L_BRD4201C:
                    expectedFileName = "sac713us.hex";
                    break;
                case ChipSeries.ZG14_BRD4201A:
                case ChipSeries.ZG14_BRD4206A:
                case ChipSeries.ZG14_BRD4208A:
                    expectedFileName = "sac714us.hex";
                    break;
                default:
                    expectedFileName = chipSeries.ToString().ToLower() + "_controller.hex"; //wrong name for missing file to log
                    break;
            }
            var ret = Path.Combine(_defaultResourcesPath, expectedFileName);
            if (!File.Exists(ret))
            {
                _logAction(false, $"Not Found predefined controller file {expectedFileName} in {_defaultResourcesPath}");
                ret = string.Empty;
            }
            return ret;
        }

    }

    public class BootloaderPath
    {
        public string EncryptKey { get; set; }
        public string SignKey { get; set; }
        public string KeysToken { get; set; }
        public string Bootloader { get; set; }
    }
}
