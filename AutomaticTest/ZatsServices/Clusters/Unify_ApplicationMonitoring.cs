// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Newtonsoft.Json;
using System.Collections.Generic;

namespace UicApplication.Clusters
{
    public class ApplicationMonitoringCluster
    {
        public const string ClusterName = "ApplicationMonitoring";
        public const string ClusterDesired = "Desired";
        public const string ClusterReported = "Reported";
        public const string ClusterSupportedCommands = "SupportedCommands";
        public const string ClusterAttributes = "Attributes";

        #region Cluster Attributes

        public class ATT_ApplicationName
        {
            public const string ApplicationName_name = "ApplicationName";
            public class ApplicationName_Attributes
            {
                public static implicit operator string(ApplicationName_Attributes command)
                {
                    return SumAllAtt(ApplicationMonitoringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_ApplicationName command)
            {
                return SumATTStr(ApplicationMonitoringCluster.ClusterName, ApplicationMonitoringCluster.ATT_ApplicationName.ApplicationName_name);
            }
            public class ApplicationName_Desired
            {
                public static implicit operator string(ApplicationName_Desired command)
                {
                    return SumATTStr_WithSubValue(ApplicationMonitoringCluster.ClusterName, ApplicationMonitoringCluster.ATT_ApplicationName.ApplicationName_name, ClusterDesired);
                }
            }
            public class ApplicationName_Report
            {
                public static implicit operator string(ApplicationName_Report command)
                {
                    return SumATTStr_WithSubValue(ApplicationMonitoringCluster.ClusterName, ApplicationMonitoringCluster.ATT_ApplicationName.ApplicationName_name, ClusterReported);
                }
            }
            public class ApplicationName_SupportCommands
            {
                public static implicit operator string(ApplicationName_SupportCommands command)
                {
                    return SumSupCMD(ApplicationMonitoringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_ApplicationVersion
        {
            public const string ApplicationVersion_name = "ApplicationVersion";
            public class ApplicationVersion_Attributes
            {
                public static implicit operator string(ApplicationVersion_Attributes command)
                {
                    return SumAllAtt(ApplicationMonitoringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_ApplicationVersion command)
            {
                return SumATTStr(ApplicationMonitoringCluster.ClusterName, ApplicationMonitoringCluster.ATT_ApplicationVersion.ApplicationVersion_name);
            }
            public class ApplicationVersion_Desired
            {
                public static implicit operator string(ApplicationVersion_Desired command)
                {
                    return SumATTStr_WithSubValue(ApplicationMonitoringCluster.ClusterName, ApplicationMonitoringCluster.ATT_ApplicationVersion.ApplicationVersion_name, ClusterDesired);
                }
            }
            public class ApplicationVersion_Report
            {
                public static implicit operator string(ApplicationVersion_Report command)
                {
                    return SumATTStr_WithSubValue(ApplicationMonitoringCluster.ClusterName, ApplicationMonitoringCluster.ATT_ApplicationVersion.ApplicationVersion_name, ClusterReported);
                }
            }
            public class ApplicationVersion_SupportCommands
            {
                public static implicit operator string(ApplicationVersion_SupportCommands command)
                {
                    return SumSupCMD(ApplicationMonitoringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_ApplicationConnected
        {
            public const string ApplicationConnected_name = "ApplicationConnected";
            public class ApplicationConnected_Attributes
            {
                public static implicit operator string(ApplicationConnected_Attributes command)
                {
                    return SumAllAtt(ApplicationMonitoringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_ApplicationConnected command)
            {
                return SumATTStr(ApplicationMonitoringCluster.ClusterName, ApplicationMonitoringCluster.ATT_ApplicationConnected.ApplicationConnected_name);
            }
            public class ApplicationConnected_Desired
            {
                public static implicit operator string(ApplicationConnected_Desired command)
                {
                    return SumATTStr_WithSubValue(ApplicationMonitoringCluster.ClusterName, ApplicationMonitoringCluster.ATT_ApplicationConnected.ApplicationConnected_name, ClusterDesired);
                }
            }
            public class ApplicationConnected_Report
            {
                public static implicit operator string(ApplicationConnected_Report command)
                {
                    return SumATTStr_WithSubValue(ApplicationMonitoringCluster.ClusterName, ApplicationMonitoringCluster.ATT_ApplicationConnected.ApplicationConnected_name, ClusterReported);
                }
            }
            public class ApplicationConnected_SupportCommands
            {
                public static implicit operator string(ApplicationConnected_SupportCommands command)
                {
                    return SumSupCMD(ApplicationMonitoringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_ApplicationMQTTTopics
        {
            public const string ApplicationMQTTTopics_name = "ApplicationMQTTTopics";
            public class ApplicationMQTTTopics_Attributes
            {
                public static implicit operator string(ApplicationMQTTTopics_Attributes command)
                {
                    return SumAllAtt(ApplicationMonitoringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_ApplicationMQTTTopics command)
            {
                return SumATTStr(ApplicationMonitoringCluster.ClusterName, ApplicationMonitoringCluster.ATT_ApplicationMQTTTopics.ApplicationMQTTTopics_name);
            }
            public class ApplicationMQTTTopics_Desired
            {
                public static implicit operator string(ApplicationMQTTTopics_Desired command)
                {
                    return SumATTStr_WithSubValue(ApplicationMonitoringCluster.ClusterName, ApplicationMonitoringCluster.ATT_ApplicationMQTTTopics.ApplicationMQTTTopics_name, ClusterDesired);
                }
            }
            public class ApplicationMQTTTopics_Report
            {
                public static implicit operator string(ApplicationMQTTTopics_Report command)
                {
                    return SumATTStr_WithSubValue(ApplicationMonitoringCluster.ClusterName, ApplicationMonitoringCluster.ATT_ApplicationMQTTTopics.ApplicationMQTTTopics_name, ClusterReported);
                }
            }
            public class ApplicationMQTTTopics_SupportCommands
            {
                public static implicit operator string(ApplicationMQTTTopics_SupportCommands command)
                {
                    return SumSupCMD(ApplicationMonitoringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
       
        #endregion

        #region Cluster Commands

        public class CMD_LogEntry
        {
            public const string LogEntry_name = "LogEntry";
            public static implicit operator string(CMD_LogEntry command)
            {
                return SumCMDStr(ApplicationMonitoringCluster.ClusterName, ApplicationMonitoringCluster.CMD_LogEntry.LogEntry_name);
            }
            public class CMD_LogEntry_Payload
            {
                public string Timestamp { get; set; }
                public string LogLevel { get; set; }
                public string LogTag { get; set; }
                public string LogMessage { get; set; }
                /*Start of public Payload*/
                public CMD_LogEntry_Payload(string my_Timestamp, string my_LogLevel, string my_LogTag, string my_LogMessage)
                {
                    Timestamp = my_Timestamp;
                    LogLevel = my_LogLevel;
                    LogTag = my_LogTag;
                    LogMessage = my_LogMessage;

                }
                /*End of public Payload*/
                public static implicit operator CMD_LogEntry_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_LogEntry_Payload>(command);
                }
                public static implicit operator string(CMD_LogEntry_Payload command)
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

        public struct Payload_Att_Value
        {
            public string value;

            public static implicit operator Payload_Att_Value(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_Value>(options);
            }
        }

        public static implicit operator ApplicationMonitoringCluster(string options)
        {
            return JsonConvert.DeserializeObject<ApplicationMonitoringCluster>(options);
        }
        public static implicit operator string(ApplicationMonitoringCluster command)
        {
            return ApplicationMonitoringCluster.ClusterName;
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

        #region ApplicationMonitoringRelated

        public struct Payload_Att_Value_ApplicationConnected
        {
            public bool value;

            public static implicit operator Payload_Att_Value_ApplicationConnected(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_Value_ApplicationConnected>(options);
            }
        }

        public struct Payload_Att_Value_ApplicationName
        {
            public string value;

            public static implicit operator Payload_Att_Value_ApplicationName(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_Value_ApplicationName>(options);
            }
        }

        public struct Payload_Att_Value_ApplicationVersion
        {
            public string value;

            public static implicit operator Payload_Att_Value_ApplicationVersion(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_Value_ApplicationVersion>(options);
            }
        }

        public struct Payload_Att_Value_ApplicationMQTTTopics
        {
            public List<string> value { get; set; }

            public static implicit operator Payload_Att_Value_ApplicationMQTTTopics(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_Value_ApplicationMQTTTopics>(options);
            }
        }
        #endregion
        #endregion
    }
}
