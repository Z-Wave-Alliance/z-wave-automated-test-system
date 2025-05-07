using System;
using System.Linq;
using System.Text;
using Zats.Services;

namespace ZatsTestConsole
{

    /// <summary>
    /// List of existing command line arguments. 
    /// </summary>
    public class CmdLineArgs
    {
        /// <summary>
        /// Path to the assembly file with TestCases 
        /// </summary>
        public static CmdLineArgument INPUT = new CmdLineArgument("-i", "--input",
            "Path to the INPUT assembly file with test cases.", true, "filename | folderpath");
        /// <summary>
        /// RF region for attached zniffer and for auto programming mode
        /// </summary>
        public static CmdLineArgument FREQUENCY = new CmdLineArgument("-f", "--region",
            "REGION for an attached zniffer and for auto programming mode(FLASH MODE). Options: ANZ, CN, EU, EU_LR, HK, IL, IN, JP, KR, RU, US, US_LR, ALL", true, "[options]");
        /// <summary>
        /// Preferred LR channel for attached zniffer
        /// </summary>
        public static CmdLineArgument LR_CHANNEL = new CmdLineArgument("-lrch", "--long-range-channel",
            "Preferred LR CHANNEL for an attached zniffer. Options: A, B", false, "[options]");
        /// <summary>
        /// Auto Programming Mode. Possible values: 'r', 'o', 'a'
        /// </summary>
        public static CmdLineArgument RUN_MODE = new CmdLineArgument("-m", "-flash-mode",
            "FLASH MODE enables auto programming for ZATS run with required devices. Options: r | o | a, where r - readonly, o - overwrite all on ZATS start, a - append when required. Additionally: f (skip fails) - do not stop ZATS run on failed test case result. By default mode = read.", false, "[options]");
        /// <summary>
        /// Number of runs of the Test Casses
        /// </summary>
        public static CmdLineArgument NUMBER_OF_RUNS = new CmdLineArgument("-r", "--run-count",
            "Number of RUNS of test cases. By default = 1", false, "value");
        /// <summary>
        /// Number of runs of the Test Cases with Failed result until they Pass.
        /// </summary>
        public static CmdLineArgument NUMBER_OF_EXTRA_RUNS = new CmdLineArgument("-er", "--extra-runs",
            "Number of EXTRA RUNS of test cases with Failed result until they Pass, overrides parameter RUNS if specified.", false, "value");
        /// <summary>
        /// Enable WireShark logging if specified. Affects only Test Cases with Z/IP or Unify devices.
        /// </summary>
        public static CmdLineArgument DUMP = new CmdLineArgument("-dump", "--dump-log",
            "Enable WireShark logging if specified. Affects only Test Cases with Z/IP devices.");
        /// <summary>
        /// Enables specific SERVICE TYPE. Options:zwave | pcc(PC Controller UI test) | link(Link framework), by default zwave.
        /// </summary>
        public static CmdLineArgument RUN_SERVICES_TYPE = new CmdLineArgument("-st", "--service-type",
            "Enables specific ZATS SERVICE TYPE. Options: zwave | pcc, where zwave is basic service type, pcc - PC Controller UI tests, by default zwave.", false, "option");
        /// <summary>
        /// Timout for each Test Case execution before it expire with Failed result.
        /// </summary>
        public static CmdLineArgument TIMEOUT = new CmdLineArgument("-t", "--timeout",
            "TIMEOUT value in seconds for each Test Case execution before it expire with Failed result.", false, "seconds");
        /// <summary>
        /// Test Case filter. Could be a string or a file path.
        /// </summary>
        public static CmdLineArgument INPUT_FILTER = new CmdLineArgument("-if", "--input-filter",
            "Test Case INPUT FILTER. Could be a string or a file path.", false, "[testcase] | filename");
        /// <summary>
        /// Path for looking for the firmware file.
        /// </summary>
        public static CmdLineArgument BASE_PATH = new CmdLineArgument("-bp", "--base-path",
            "BASE PATH for looking for the firmware files in case enabled auto programming mode and use in ZATS services.", false, "folderpath");
        /// <summary>
        /// Path to the logging output files
        /// </summary>
        public static CmdLineArgument REPORTS_PATH = new CmdLineArgument("-rp", "--report-path",
            "REPORT PATH to the logging output files. By default: ZATS assembly location.", false, "folderpath");
        /// <summary>
        /// Path to the environment definition and configuration files
        /// </summary>
        public static CmdLineArgument CONFIG_PATH = new CmdLineArgument("-cp", "--config-path",
            "CONFIGURATION PATH to the environment definition and configuration files. By default: ZATS assembly location.", true, "folderpath");
        /// <summary>
        /// Limit the available device hosts using chip series filter. Possible values: '5', '7'
        /// </summary>
        public static CmdLineArgument CHIP_SERIES = new CmdLineArgument("-cs", "--chip-series",
            "Limit the available device hosts using CHIP SERIES filter.Options: 5 | 7 | 8, by default latest", false, "value");
        /// <summary>
        /// Environment profile. Value from the Profile.xml file will be used if not specified. Default value: Hostname  
        /// </summary>
        public static CmdLineArgument PROFILE = new CmdLineArgument("-ep", "--profile",
            "ENVIRONMENT PROFILE Value from the Profile.xml file will be used, by default Hostname", false, "name");
        /// <summary>
        /// Limit the amount of logging output if specified.
        /// </summary>
        public static CmdLineArgument LESS = new CmdLineArgument("-less", "--less-output",
            "Limit the amount of logging output if specified.");
        /// <summary>
        /// Skip detection dedicated zniffer device
        /// </summary>
        public static CmdLineArgument NOZNF = new CmdLineArgument("-noznf", "--no-zniffer",
            "Skip detection dedicated zniffer device");
        /// <summary>
        /// Filter for WSTK boards IP addresses. Could be a string or a file path.
        /// </summary>
        public static CmdLineArgument JLINK_DEVICES_FILTER = new CmdLineArgument("-jlf", "--jlink-filter",
            "Filter for WSTK boards IP addresses. Could be a string or a file path.", false, "[device numbers] | filename");
        /// <summary>
        /// Filter for mesh router IP address
        /// </summary>
        public static CmdLineArgument MESHROUTERS_DEVICES_FILTER = new CmdLineArgument("-nrf", "--mesh-router-filter",
            "MESHROUTERS DEVICES FILTER  for mesh router IP address", false, "device number");
        // internal value
        public static CmdLineArgument OUTPUT_COLOR_FORMATED = new CmdLineArgument("-coloutf", "--output-color-format",
            "OUTPUT COLOR FORMATED to enable special color tags");
        // internal value
        public static CmdLineArgument OUTPUT_USE_START_STOP_TAGS = new CmdLineArgument("-tagout", "--output-use-start-stop-tags",
            $"OUTPUT START STOP TAGS to enable '{ConsoleCustomOutput.ZATS_START_TAG}' and '{ConsoleCustomOutput.ZATS_END_TAG}' tags");
        /// <summary>
        /// Commander.ext application path
        /// </summary>
        public static CmdLineArgument COMMANDER_APP_PATH = new CmdLineArgument("-cap", "--commander-path",
            "COMMANDER APPLICATION PATH to a folder that contains commander.exe. By default: CONFIGURATION file parameter \'CommanderAppPath\'.", true, "folderpath");
        /// <summary>
        /// Filter for test beds
        /// </summary>
        public static CmdLineArgument TEST_BEDS_FILTER = new CmdLineArgument("-tb", "--test-bed-filter",
            "Filter for TEST BEDS", false, "all | all-new | test bed name");
        /// <summary>
        /// Filter for test beds
        /// </summary>
        public static CmdLineArgument IP_FILTER = new CmdLineArgument("-ipf", "--ip-filter",
            "Filter for IP addresses", false, "[masks] | filename with masks");
        
        public static CmdLineArgument ZNIFFER_IS_VIRTUAL = new CmdLineArgument("-ziv", "--zniffer-is-virtual",
            "Zniffer is a virtual device - do not use e.g. JLink");
        /// <summary>
        /// Print HELP text
        /// </summary>
        public static CmdLineArgument HELP = new CmdLineArgument("-h", "--help",
            "Provides help information for ZATS commands.  View all in case not found or empty parameter. NOT case sensitive.", false, "command");

        private static CmdLineArgument[] args = null;
        public static CmdLineArgument[] GetAllArguments()
        {
            if (args == null)
            {
                args = new[]
                {
                    INPUT,
                    FREQUENCY,
                    LR_CHANNEL,
                    RUN_MODE,
                    NUMBER_OF_RUNS,
                    NUMBER_OF_EXTRA_RUNS,
                    DUMP,
                    RUN_SERVICES_TYPE,
                    TIMEOUT,
                    INPUT_FILTER,
                    BASE_PATH,
                    REPORTS_PATH,
                    CONFIG_PATH,
                    CHIP_SERIES,
                    PROFILE,
                    LESS,
                    NOZNF,
                    JLINK_DEVICES_FILTER,
                    MESHROUTERS_DEVICES_FILTER,
                    OUTPUT_COLOR_FORMATED,
                    OUTPUT_USE_START_STOP_TAGS,
                    COMMANDER_APP_PATH,
                    TEST_BEDS_FILTER,
                    IP_FILTER,
                    ZNIFFER_IS_VIRTUAL,
                    HELP,
                };
            }
            return args;
        }

        public static CmdLineArgument Find(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return CmdLineArgument.Empty;
            }
            return GetAllArguments().FirstOrDefault(x =>
                x.ShortName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                x.LongName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static string GetHelp()
        {
            return GetHelp(HELP);
        }
        
        public static string GetHelp(CmdLineArgument cmd)
        {
            string ret = cmd.About();
            if (cmd == HELP)
            {
                ret = string.Join(Environment.NewLine, GetAllArguments().Select(x => x.About()).ToArray());
                ret = $"{ret}{Environment.NewLine}{GetEndLink()}";
            }
            return ret;
        }

        private static string GetEndLink()
        {
            return $"{Environment.NewLine}For more details how to use ZATS and commands examples, please visit:{Environment.NewLine}" +
           $"https://confluence.silabs.com/pages/viewpage.action?spaceKey=ZWAVE&title=How+To+Run";
        }
    }

    internal class RunModeName
    {
        public const string APPEND = "a";
        public const string OVERWRITE = "o";
        public const string READONLY = "r";
        public const string CLEAN = "c";
        public const string CONTINUE_ON_FAIL = "f";
    }

    public enum IgnoreModes
    {
        None,
        Fail,
    }

    internal class RunServiceName
    {
        public const string ZWAVE = "zwave";
        public const string PC_CONTROLLER_UI = "pcc";
        public const string LINK_FRAMEWORK = "link";
    }

    public enum RunServiceType
    {
        Basic,
        PCControllerUI,
        LinkFramework
    }

    public struct CmdLineSettings
    {
        public bool IsAppendMode
        {
            get
            {
                return RunMode == RunModes.Overwrite
                    || RunMode == RunModes.Clean
                    || RunMode == RunModes.Append;
            }
        }
        public RunModes RunMode;
        public IgnoreModes IgnoreMode;
        public string[] Frequencies;
        public LongRangeChannels[] LongRangeChannels;
        public string[] DutNames;
        public string AssemblyFile;
        public int NumberOfRuns;
        public int NumberOfExtraRuns;
        public bool CreateDumpFile;
        public RunServiceType RunServiceType;
        public int TimeoutSeconds;
        public string InputFilter;
        public string BasePath;
        public string ReportsPath;
        public string ConfigPath;
        public string ChipSeries;
        public string ProfileName;
        public bool IsLessOutput;
        public bool UseStartStopTags;
        public bool IsNoZnfDevice;
        public bool IsOutputColorFormated;
        public string JLinkDevicesFilter;
        public string MeshRoutersDevicesFilter;
        public string TestBedsFilter;
        public string CommanderAppPath;
        public string IPFilter;
        public bool HasHelp;
        public string Help;

        public bool HasParseError;
        public string ParseErrorMessage;

        public bool IsZnifferVirtual;
    }

    public struct CmdLineArgument : IEquatable<CmdLineArgument>, IComparable<CmdLineArgument>
    {
        private const int outLengh = 85;

        private string _shortName;
        private string _longName;
        private string _description;
        private bool _isMandatory;
        private string _params;

        public string ShortName => _shortName;
        public string LongName => _longName;
        public string Description => _description;
        public bool IsMandatory => _isMandatory;
        public string Params => _params;
        public CmdLineArgument(string shortName, string longName, string description, bool isMandatory = false, string parameters = "")
        {
            _shortName = shortName;
            _longName = longName;
            _description = description;
            _isMandatory = isMandatory;
            _params = parameters;
        }

        public static CmdLineArgument Empty = new CmdLineArgument(null, null, null);

        public override string ToString()
        {
            return ShortName;
        }

        public string About()
        {
            StringBuilder ret = new StringBuilder();
            ret.Append(" " + ShortName);
            if (!string.IsNullOrEmpty(LongName))
            {
                ret.Append($" | {LongName}");
            }
            if (!string.IsNullOrEmpty(Params))
            {
                ret.Append($" <{Params}>");
            }
            if (!string.IsNullOrEmpty(Description))
            {
                ret.AppendLine("");
                ret.Append("\t");

                if (Description.Length > outLengh)
                {
                    var words = Description.Split(' ');
                    var lastSentenceLen = 0;
                    foreach (var word in words)
                    {
                        if (lastSentenceLen - word.Length > outLengh)
                        {
                            ret.AppendLine("");
                            ret.Append("\t");
                            lastSentenceLen = 0;
                        }
                        ret.Append(word + " ");
                        lastSentenceLen += word.Length + 1;
                    }
                }
                else
                {
                    ret.Append(Description);
                }
            }
            if (IsMandatory)
            {
                ret.AppendLine();
                ret.Append("\t--- Mandatory Argument! ---");
            }
            ret.AppendLine();
            return ret.ToString();
        }

        public override int GetHashCode()
        {
            if (ShortName != null)
            {
                return ShortName.GetHashCode();
            }
            else
            {
                return base.GetHashCode();
            }
        }

        public bool Equals(CmdLineArgument other)
        {
            return GetHashCode() == other.GetHashCode() && ShortName == other.ShortName && LongName == other.LongName;
        }

        public static bool operator !=(CmdLineArgument arg1, CmdLineArgument arg2)
        {
            return !arg1.Equals(arg2);
        }

        public static bool operator ==(CmdLineArgument arg1, CmdLineArgument arg2)
        {
            return arg1.Equals(arg2);
        }

        public override bool Equals(object obj)
        {
            bool ret = false;
            if (obj is CmdLineArgument)
            {
                ret = Equals((CmdLineArgument)obj);
            }
            return ret;
        }

        public int CompareTo(CmdLineArgument other)
        {
            int ret = -1;
            if (ShortName != null)
            {
                ret = ShortName.CompareTo(other.ShortName);
            }
            else if (LongName != null)
            {
                ret = LongName.CompareTo(other.LongName);
            }
            return ret;
        }

        public int CompareTo(object obj)
        {
            if (!(obj is CmdLineArgument))
                return 0;

            CmdLineArgument nodeId = (CmdLineArgument)obj;
            return CompareTo(nodeId);
        }
    }
}
