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
        private FilePatternDefinition _filePatternDefinition = null;

        /// <summary>Test Case Assembly resources path with predefined files.</summary>
        /// <remarks>3rd priority for finding files.</remarks>
        private readonly string _defaultResourcesPath;
        /// <summary>Root folder for DevKits</summary>
        /// <remarks>May contain multiple paths separated by ';'.Used for the main file storage provided by the user in the CLI. 2nd priority for finding files</remarks>
        private readonly string _baseDevKitPath;
        /// <summary>Root folder for reports.</summary>
        private readonly string _reportsRootPath;
        /// <summary>Specific test case run folder.</summary>
        /// <remarks>Used for temporary files storage. 1st priority for finding files.</remarks>
        private string _testCaseFolder;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemManager"/> class.
        /// </summary>
        /// <param name="logAction">Log action to display error messages</param>
        /// <param name="resourcesPath">Resources path</param>
        /// <param name="baseDevKitPath">Base path with user-defined DevKits</param>
        /// <param name="reportsPath">ZATS Reports path</param>
        public FileSystemManager(Action<bool, string> logAction, string resourcesPath, string baseDevKitPath, string reportsPath)
        {
            _logAction = logAction;
            _defaultResourcesPath = string.IsNullOrEmpty(resourcesPath) ?
                 Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), RESOURSES_FOLDER) :
                 Path.Combine(resourcesPath, RESOURSES_FOLDER);
            if (_filePatternDefinition == null)
            {
                var location = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "XmlFiles");
                _filePatternDefinition = GetFilePatternDefinition(location, FilePatternDefinitionName);
            }

            _baseDevKitPath = baseDevKitPath;
            _reportsRootPath = reportsPath;

            ValidateInitialization();
        }

        /// <summary>
        /// Warns if initialization parameters are missing.
        /// </summary>
        private void ValidateInitialization()
        {
            if (_filePatternDefinition == null)
            {
                _logAction(false, $"File pattern definition '{FilePatternDefinitionName}' is missing or invalid");
            }
            if (string.IsNullOrEmpty(_baseDevKitPath))
            {
                _logAction(false, "'Base path' parameter (-bp <path>) is missing");
            }
            if (string.IsNullOrEmpty(_reportsRootPath))
            {
                _logAction(false, "'Reports path' parameter (-rp <path>) is missing");
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

        /// <summary>
        /// Apply filter to file paths.
        /// </summary>
        /// <param name="filePaths">File paths to filter</param>
        /// <param name="regexFilters">Regex filter to apply</param>
        /// <returns>Filtered items</returns>
        private static string[] FilterFilePaths(string[] filePaths, params string[] regexFilters)
        {
            if (filePaths == null || filePaths.Length == 0) return filePaths;
            if (regexFilters == null || regexFilters.Length == 0) return filePaths;

            string[] ret = filePaths;
            foreach (var filter in regexFilters.Where(x => !string.IsNullOrEmpty(x)))
            {
                var rgx = new Regex(filter, RegexOptions.IgnoreCase);
                //ret = ret.Where(x => rgx.Match(x).Success).ToArray();
                var tmpFs = ret.Where(x => rgx.Match(x).Success);
                if (tmpFs.Any())
                {
                    ret = tmpFs.ToArray();
                }
            }
            return ret;
        }

        public string GetLatestFirmwareFilePath(Libraries applicationLibrary, string chipSeries, string frequency, string ending, params string[] filters)
        {
            string resultFilePath = null;
            var genericChipSeries = new[]
            {
                ChipSeries.ZW080x, ChipSeries.ZW070x, ChipSeries.ZW0713, ChipSeries.ZW0714, ChipSeries.Default
            }.Select(x => x.ToString());

            if (genericChipSeries.Contains(chipSeries))
            {
                throw new ArgumentException($"Invalid chip series {chipSeries} specified");
            }
            else
            {
                var binaryPattern = GetBinaryPattern(chipSeries);

                // File search priority: 1) devkits folder 2) resources folder
                string devkitsRootFolders = _baseDevKitPath + ";" + _defaultResourcesPath;
                var folders = devkitsRootFolders.Split(';');
                foreach (var devkitsRootFolder in folders)
                {
                    if (string.IsNullOrEmpty(devkitsRootFolder) || !Directory.Exists(devkitsRootFolder))
                    {
                        continue;
                    }
                    //Try find specific library token for the chip series:
                    var libToken = binaryPattern.LibTokens?.FirstOrDefault(x => x.Library == applicationLibrary.ToString());
                    if (string.IsNullOrWhiteSpace(libToken?.Token))
                    {
                        //Try find general library token from file pattern definition:
                        libToken = _filePatternDefinition.LibTokens.FirstOrDefault(x => x.Library == applicationLibrary.ToString());
                        if (string.IsNullOrWhiteSpace(libToken?.Token))
                        {
                            //Use library name as token:
                            libToken = new LibToken() { Library = applicationLibrary.ToString(), Token = applicationLibrary.ToString() };
                        }
                    }
                    var freq = libToken.SkipFrequency ? string.Empty : frequency;
                    var end = ending ?? string.Empty;
                    resultFilePath = GetLatestFilePathInternal(filters, devkitsRootFolder, binaryPattern, libToken, freq, end, string.Empty);

                    if (string.IsNullOrEmpty(resultFilePath))
                    {
                        //Extra try to look for DEBUG versions:
                        resultFilePath = GetLatestFilePathInternal(filters, devkitsRootFolder, binaryPattern, libToken, freq, "_DEBUG" + end, string.Empty);
                    }

                    if (!string.IsNullOrEmpty(resultFilePath)) { break; }
                }

                if (string.IsNullOrEmpty(resultFilePath)) //use hardcoded files from resources:
                {
                    var location = _defaultResourcesPath;
                    var predefinedResourceName = string.Empty;
                    switch (applicationLibrary)
                    {
                        case Libraries.RailTest:
                            predefinedResourceName = GetRailTestResourceName(chipSeries);
                            break;
                        default:
                            break;
                    }
                    var resourcePath = Path.Combine(location, predefinedResourceName);
                    if (File.Exists(resourcePath))
                    {
                        resultFilePath = resourcePath;
                    }
                }

                if (string.IsNullOrWhiteSpace(resultFilePath))
                {
                    _logAction(false, $"Not Found {applicationLibrary}, {binaryPattern.ChipSeries}, {frequency}{ending} in {devkitsRootFolders}");
                }
            }
            return resultFilePath;
        }

        private BinaryPattern GetBinaryPattern(string chipSeries)
        {
            var binaryPattern = _filePatternDefinition?.BinaryPatterns?.FirstOrDefault(x => x.ChipSeries == chipSeries.ToString());
            if (binaryPattern == null)
            {
                var brdTokens = chipSeries.ToString().Split('_');
                if (brdTokens != null && brdTokens.Length == 2)
                {
                    binaryPattern = new BinaryPattern();
                    binaryPattern.ChipSeries = chipSeries.ToString();
                    binaryPattern.PathFilters = _filePatternDefinition.PathFilters.Select(x => new PathFilter()
                    {
                        Pattern = x.Pattern.Replace("{Brd0}", brdTokens[0]).Replace("{Brd1}", brdTokens[1])
                    }).ToArray();
                }
            }

            return binaryPattern;
        }

        /// <summary>
        /// Get predefined RailTest resource name from resources based on chip series.
        /// </summary>
        /// <param name="chipSeries">Chip series identifier.</param>
        /// <returns>Predefined RailTest resource name.</returns>
        private static string GetRailTestResourceName(string chipSeries)
        {
            string rtResourceName = string.Empty;
            if ((new[] { ChipSeries.ZG23_BRD4204A, ChipSeries.ZG23_BRD4204C, ChipSeries.ZG23_BRD4204D, ChipSeries.ZG23_BRD4210A })
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

            return rtResourceName;
        }

        private string GetLatestFilePathInternal(string[] filters, string devkitsRootFolder, BinaryPattern binPat, LibToken lm, string freq, string end, string subtype)
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

                    string[] regexFilters = new string[0];
                    if (binPat.RegexFilters != null && binPat.RegexFilters.Count > 0)
                    {
                        regexFilters = binPat.RegexFilters.ToArray();
                    }
                    regexFilters = regexFilters.Concat(filters).ToArray();

                    string[] priorityFilters = new string[0];
                    if (binPat.PriorityRegexFilters != null && binPat.PriorityRegexFilters.Count > 0)
                    {
                        priorityFilters = binPat.PriorityRegexFilters.ToArray();
                    }
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

        /// <summary>
        /// Search file in root directory and subdirectories applying filters in order by creation time.
        /// </summary>
        /// <param name="rootDirectory">Start root directory to search in.</param>
        /// <param name="searchPattern">Search pattern to match files against.</param>
        /// <param name="regexFilters">Regular expression filters to apply.</param>
        /// <param name="priorityRegexFilters">Priority regular expression filters to apply.</param>
        /// <returns>First matching file path or null if not found.</returns>
        private string GetFile(DirectoryInfo rootDirectory, string searchPattern, string[] regexFilters, string[] priorityRegexFilters)
        {
            // Select files in the current root directory matching the search pattern:
            var filteredFilePaths = GetMatchingFiles(rootDirectory, searchPattern);
            // Low priority filtering:
            filteredFilePaths = FilterFilePaths(filteredFilePaths, regexFilters);
            // High priority filtering:
            filteredFilePaths = FilterFilePaths(filteredFilePaths, priorityRegexFilters);

            var resultFilePath = filteredFilePaths?.FirstOrDefault();
            if (string.IsNullOrEmpty(resultFilePath))
            {
                var subFolders = rootDirectory.GetDirectories().OrderByDescending(x => x.CreationTime);
                foreach (var dir in subFolders)
                {
                    resultFilePath = GetFile(dir, searchPattern, regexFilters, priorityRegexFilters);
                    if (resultFilePath != null)
                    {
                        break;
                    }
                }
            }
            else
            {
                _logAction(true, $"{resultFilePath}");
            }
            return resultFilePath;
        }

        /// <summary>
        /// Find files matching the search pattern in the specified root directory.
        /// </summary>
        /// <param name="rootDirectory">Root directory to search in.</param>
        /// <param name="searchPattern">Search pattern to match files against.</param>
        /// <param name="topFolder"></param>
        /// <returns>Array of matching file paths.</returns>
        private static string[] GetMatchingFiles(DirectoryInfo rootDirectory, string searchPattern, bool topFolder = true)
        {
            string[] filteredFilePaths = new string[0];
            try
            {
#if NETCOREAPP
                filteredFilePaths = Directory.GetFiles(rootDirectory.FullName, searchPattern,
                    new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = !topFolder });
#else
                filteredFilePaths = rootDirectory.
                    GetFiles(searchPattern,  topFolder? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories).
                    Select(x => x.FullName).ToArray();
#endif
            }
            catch (Exception ex)
            {
                "{0}"._DLOG(ex);
            }

            return filteredFilePaths;
        }

        public string GetLatestFilePath(string searchPattern, params string[] filters)
        {
            var resultFilePath = string.Empty;
            // File search priority: 1) specific file (downloaded, modified by TC) in test case folder 2) devkits folder 3) resources folder
            string devkitsRootFolders = _testCaseFolder + ";" + _baseDevKitPath + ";" + _defaultResourcesPath;

            var folders = devkitsRootFolders.Split(';');
            foreach (var devkitsRootFolder in folders)
            {
                if (string.IsNullOrEmpty(devkitsRootFolder) || !Directory.Exists(devkitsRootFolder))
                {
                    continue;
                }
                var di = new DirectoryInfo(devkitsRootFolder);

                resultFilePath = GetFile(di, searchPattern, filters, new string[0]);

                if (!string.IsNullOrEmpty(resultFilePath))
                {
                    break;
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
            if (_filePatternDefinition != null && _filePatternDefinition.DefaultBootloaderInfos != null && _filePatternDefinition.DefaultBootloaderInfos.Length > 1)
            {
                //Set predefined keys from RESOURSES_FOLDER:
                var idx = isSAPI ? 1 : 0;
                var keyTokensFileName = _filePatternDefinition.DefaultBootloaderInfos?[idx].KeyTokensFileName;
                var encryptKeyFileName = _filePatternDefinition.DefaultBootloaderInfos?[idx].EncryptKeyFileName;
                var signKeyFileName = _filePatternDefinition.DefaultBootloaderInfos?[idx].SignKeyFileName;

                var idxBrd = deviceHostClient.Host.ChipSeries.ToLower().IndexOf("brd");
                var brdNumber = string.Concat(deviceHostClient.Host.ChipSeries.Skip(idxBrd)); // specify filter for board from chip version.
                defaultSearchPattern = _filePatternDefinition.DefaultBootloaderInfos?[idx].Pattern.Replace("{brdNumber}", brdNumber);

                var location = _defaultResourcesPath;
                ret.KeysToken = Path.Combine(location, keyTokensFileName);
                ret.EncryptKey = Path.Combine(location, encryptKeyFileName);
                ret.SignKey = Path.Combine(location, signKeyFileName);
            }

            var expectedFileNames = specificFileNamePattern ?? new[] { defaultSearchPattern };
            foreach (var item in expectedFileNames)
            {
                var bootloaderFilePath = GetLatestFilePath(item, regexFilters);
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

        /// <inheritdoc/>
        public string GetPredefinedControllerPathFromResources(ChipSeries chipSeries)
        {
            var expectedFileName = string.Empty;
            switch (chipSeries)
            {
                case ChipSeries.ZG23_BRD4204A:
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

        /// <inheritdoc/>
        public string GetPath(PathTypes pathType)
        {
            var ret = _baseDevKitPath;
            switch (pathType)
            {
                case PathTypes.TestCaseTemp:
                    ret = _testCaseFolder;
                    break;
                case PathTypes.ReportsRoot:
                    ret = _reportsRootPath;
                    break;
                case PathTypes.DefaultResources:
                    ret = _defaultResourcesPath;
                    break;
                case PathTypes.DevKitsRoot:
                default:
                    break;
            }
            return ret;
        }

        /// <inheritdoc/>
        public void SetTestCaseFolderPath(string testCaseFolderName)
        {
            var fullOrTemp = Path.Combine(_reportsRootPath ?? Path.GetTempPath(), testCaseFolderName, "temps");
            _testCaseFolder = fullOrTemp;
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
