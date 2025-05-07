using Newtonsoft.Json;
using System.Collections.Generic;

namespace UicApplication.Clusters
{
    public class ConfigurationParametersCluster
    {
        public const string ClusterName = "ConfigurationParameters";
        public const string ClusterDesired = "Desired";
        public const string ClusterReported = "Reported";
        public const string ClusterSupportedCommands = "SupportedCommands";
        public const string ClusterAttributes = "Attributes";

        #region Cluster Attributes

        public class ATT_ConfigurationParameters
        {
            public const string ConfigurationParameters_name = "ConfigurationParameters";
            public class ConfigurationParameters_Attributes
            {
                public static implicit operator string(ConfigurationParameters_Attributes command)
                {
                    return SumAllAtt(ConfigurationParametersCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_ConfigurationParameters command)
            {
                return SumATTStr(ConfigurationParametersCluster.ClusterName, ConfigurationParametersCluster.ATT_ConfigurationParameters.ConfigurationParameters_name);
            }
            public class ConfigurationParameters_Desired
            {
                public static implicit operator string(ConfigurationParameters_Desired command)
                {
                    return SumATTStr_WithSubValue(ConfigurationParametersCluster.ClusterName, ConfigurationParametersCluster.ATT_ConfigurationParameters.ConfigurationParameters_name, ClusterDesired);
                }
            }
            public class ConfigurationParameters_Report
            {
                public static implicit operator string(ConfigurationParameters_Report command)
                {
                    return SumATTStr_WithSubValue(ConfigurationParametersCluster.ClusterName, ConfigurationParametersCluster.ATT_ConfigurationParameters.ConfigurationParameters_name, ClusterReported);
                }
            }
            public class ConfigurationParameters_SupportCommands
            {
                public static implicit operator string(ConfigurationParameters_SupportCommands command)
                {
                    return SumSupCMD(ConfigurationParametersCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        #endregion

        #region Cluster Commands

        public class CMD_DiscoverParameter
        {
            public const string DiscoverParameter_name = "DiscoverParameter";
            public static implicit operator string(CMD_DiscoverParameter command)
            {
                return SumCMDStr(ConfigurationParametersCluster.ClusterName, ConfigurationParametersCluster.CMD_DiscoverParameter.DiscoverParameter_name);
            }
            public class CMD_DiscoverParameter_Payload
            {
                public int ParameterId { get; set; }
                public CMD_DiscoverParameter_Payload(int my_ParameterId)
                {
                    ParameterId = my_ParameterId;
                }
               
                /*End of public Payload*/
                public static implicit operator CMD_DiscoverParameter_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_DiscoverParameter_Payload>(command);
                }
                public static implicit operator string(CMD_DiscoverParameter_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        public class CMD_DefaultResetAllParameters
        {
            public const string DefaultResetAllParameters_name = "DefaultResetAllParameters";
            public static implicit operator string(CMD_DefaultResetAllParameters command)
            {
                return SumCMDStr(ConfigurationParametersCluster.ClusterName, ConfigurationParametersCluster.CMD_DefaultResetAllParameters.DefaultResetAllParameters_name);
            }
        }/*End of CMD class with Childnodes*/
        public class CMD_SetParameter
        {
            public const string SetParameter_name = "SetParameter";
            public static implicit operator string(CMD_SetParameter command)
            {
                return SumCMDStr(ConfigurationParametersCluster.ClusterName, ConfigurationParametersCluster.CMD_SetParameter.SetParameter_name);
            }
            public class CMD_SetParameter_Payload
            {
                public int ParameterId { get; set; }
                public int Value { get; set; }
                public CMD_SetParameter_Payload(int my_ParameterId, int my_value)
                {
                    ParameterId = my_ParameterId;
                    Value = my_value;
                }

                /*End of public Payload*/
                public static implicit operator CMD_SetParameter_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_SetParameter_Payload>(command);
                }
                public static implicit operator string(CMD_SetParameter_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        #endregion

        #region Cluster Operations
        public struct Payload_Att_SupportedCommands
        {
            public List<string> value { get; set; }

            public static implicit operator Payload_Att_SupportedCommands(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_SupportedCommands>(options);
            }
        }
        public struct ConfigurationsParameter_Payload
        {
            public bool Advanced;
            public bool AlteringCapabilities;
            public int DefaultValue;
            public int DisplayFormat;
            public string Info;
            public int MaximumValue;
            public int MinimumValue;
            public string Name;
            public int ParameterId;
            public bool ReadOnly;
            public int Value;

            public static implicit operator ConfigurationsParameter_Payload(string options)
            {
                return JsonConvert.DeserializeObject<ConfigurationsParameter_Payload>(options);
            }
        }
        public struct Payload_Att_Value
        {
            public ConfigurationsParameter_Payload[] value;

            public static implicit operator Payload_Att_Value(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_Value>(options);
            }
        }
        public static implicit operator ConfigurationParametersCluster(string options)
        {
            return JsonConvert.DeserializeObject<ConfigurationParametersCluster>(options);
        }
        public static implicit operator string(ConfigurationParametersCluster command)
        {
            return ConfigurationParametersCluster.ClusterName;
        }
        private static string SumAllAtt(string AttClusterName, string Att)
        {
            string SumStr = "/" + AttClusterName + "/" + Att + "/" + "#";
            return SumStr;
        }
        private static string SumCMDStr(string ClusterName, string CmdName)
        {
            string SumStr = "/" + ClusterName + "/" + "Commands" + "/" + CmdName;
            return SumStr;
        }

        private static string SumATTStr(string ClusterName, string AttName)
        {
            string SumStr = "/" + ClusterName + "/" + "Attributes" + "/" + AttName;
            return SumStr;
        }
        private static string SumATTStr_WithSubValue(string ClusterName, string AttName, string SubValue)
        {
            string SumStr = SumATTStr(ClusterName, AttName) + "/" + SubValue;
            return SumStr;
        }
        private static string SumSupCMD(string ClusterName, string SupCMD)
        {
            string SumStr = "/" + ClusterName + "/" + SupCMD;
            return SumStr;
        }
        #endregion
    }
}
