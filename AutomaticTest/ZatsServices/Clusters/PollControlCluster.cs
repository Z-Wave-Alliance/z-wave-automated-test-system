// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Newtonsoft.Json;

namespace UicApplication.Clusters
{
    public class PollControlCluster
    {
        public const string ClusterName = "PollControl";
        public const string ClusterDesired = "Desired";
        public const string ClusterReported = "Reported";
        public const string ClusterSupportedCommands = "SupportedCommands";
        public const string ClusterAttributes = "Attributes";

        #region Cluster Attributes

        public class ATT_CheckInInterval
        {
            public const string CheckInInterval_name = "CheckInInterval";
            public class CheckInInterval_Attributes
            {
                public static implicit operator string(CheckInInterval_Attributes command)
                {
                    return SumAllAtt(PollControlCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_CheckInInterval command)
            {
                return SumATTStr(PollControlCluster.ClusterName, PollControlCluster.ATT_CheckInInterval.CheckInInterval_name);
            }
            public class CheckInInterval_Desired
            {
                public static implicit operator string(CheckInInterval_Desired command)
                {
                    return SumATTStr_WithSubValue(PollControlCluster.ClusterName, PollControlCluster.ATT_CheckInInterval.CheckInInterval_name, ClusterDesired);
                }
            }
            public class CheckInInterval_Report
            {
                public static implicit operator string(CheckInInterval_Report command)
                {
                    return SumATTStr_WithSubValue(PollControlCluster.ClusterName, PollControlCluster.ATT_CheckInInterval.CheckInInterval_name, ClusterReported);
                }
            }
            public class CheckInInterval_SupportCommands
            {
                public static implicit operator string(CheckInInterval_SupportCommands command)
                {
                    return SumSupCMD(PollControlCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_LongPollInterval
        {
            public const string LongPollInterval_name = "LongPollInterval";
            public class LongPollInterval_Attributes
            {
                public static implicit operator string(LongPollInterval_Attributes command)
                {
                    return SumAllAtt(PollControlCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_LongPollInterval command)
            {
                return SumATTStr(PollControlCluster.ClusterName, PollControlCluster.ATT_LongPollInterval.LongPollInterval_name);
            }
            public class LongPollInterval_Desired
            {
                public static implicit operator string(LongPollInterval_Desired command)
                {
                    return SumATTStr_WithSubValue(PollControlCluster.ClusterName, PollControlCluster.ATT_LongPollInterval.LongPollInterval_name, ClusterDesired);
                }
            }
            public class LongPollInterval_Report
            {
                public static implicit operator string(LongPollInterval_Report command)
                {
                    return SumATTStr_WithSubValue(PollControlCluster.ClusterName, PollControlCluster.ATT_LongPollInterval.LongPollInterval_name, ClusterReported);
                }
            }
            public class LongPollInterval_SupportCommands
            {
                public static implicit operator string(LongPollInterval_SupportCommands command)
                {
                    return SumSupCMD(PollControlCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_ShortPollInterval
        {
            public const string ShortPollInterval_name = "ShortPollInterval";
            public class ShortPollInterval_Attributes
            {
                public static implicit operator string(ShortPollInterval_Attributes command)
                {
                    return SumAllAtt(PollControlCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_ShortPollInterval command)
            {
                return SumATTStr(PollControlCluster.ClusterName, PollControlCluster.ATT_ShortPollInterval.ShortPollInterval_name);
            }
            public class ShortPollInterval_Desired
            {
                public static implicit operator string(ShortPollInterval_Desired command)
                {
                    return SumATTStr_WithSubValue(PollControlCluster.ClusterName, PollControlCluster.ATT_ShortPollInterval.ShortPollInterval_name, ClusterDesired);
                }
            }
            public class ShortPollInterval_Report
            {
                public static implicit operator string(ShortPollInterval_Report command)
                {
                    return SumATTStr_WithSubValue(PollControlCluster.ClusterName, PollControlCluster.ATT_ShortPollInterval.ShortPollInterval_name, ClusterReported);
                }
            }
            public class ShortPollInterval_SupportCommands
            {
                public static implicit operator string(ShortPollInterval_SupportCommands command)
                {
                    return SumSupCMD(PollControlCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_FastPollTimeout
        {
            public const string FastPollTimeout_name = "FastPollTimeout";
            public class FastPollTimeout_Attributes
            {
                public static implicit operator string(FastPollTimeout_Attributes command)
                {
                    return SumAllAtt(PollControlCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_FastPollTimeout command)
            {
                return SumATTStr(PollControlCluster.ClusterName, PollControlCluster.ATT_FastPollTimeout.FastPollTimeout_name);
            }
            public class FastPollTimeout_Desired
            {
                public static implicit operator string(FastPollTimeout_Desired command)
                {
                    return SumATTStr_WithSubValue(PollControlCluster.ClusterName, PollControlCluster.ATT_FastPollTimeout.FastPollTimeout_name, ClusterDesired);
                }
            }
            public class FastPollTimeout_Report
            {
                public static implicit operator string(FastPollTimeout_Report command)
                {
                    return SumATTStr_WithSubValue(PollControlCluster.ClusterName, PollControlCluster.ATT_FastPollTimeout.FastPollTimeout_name, ClusterReported);
                }
            }
            public class FastPollTimeout_SupportCommands
            {
                public static implicit operator string(FastPollTimeout_SupportCommands command)
                {
                    return SumSupCMD(PollControlCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_CheckInIntervalMin
        {
            public const string CheckInIntervalMin_name = "CheckInIntervalMin";
            public class CheckInIntervalMin_Attributes
            {
                public static implicit operator string(CheckInIntervalMin_Attributes command)
                {
                    return SumAllAtt(PollControlCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_CheckInIntervalMin command)
            {
                return SumATTStr(PollControlCluster.ClusterName, PollControlCluster.ATT_CheckInIntervalMin.CheckInIntervalMin_name);
            }
            public class CheckInIntervalMin_Desired
            {
                public static implicit operator string(CheckInIntervalMin_Desired command)
                {
                    return SumATTStr_WithSubValue(PollControlCluster.ClusterName, PollControlCluster.ATT_CheckInIntervalMin.CheckInIntervalMin_name, ClusterDesired);
                }
            }
            public class CheckInIntervalMin_Report
            {
                public static implicit operator string(CheckInIntervalMin_Report command)
                {
                    return SumATTStr_WithSubValue(PollControlCluster.ClusterName, PollControlCluster.ATT_CheckInIntervalMin.CheckInIntervalMin_name, ClusterReported);
                }
            }
            public class CheckInIntervalMin_SupportCommands
            {
                public static implicit operator string(CheckInIntervalMin_SupportCommands command)
                {
                    return SumSupCMD(PollControlCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_LongPollIntervalMin
        {
            public const string LongPollIntervalMin_name = "LongPollIntervalMin";
            public class LongPollIntervalMin_Attributes
            {
                public static implicit operator string(LongPollIntervalMin_Attributes command)
                {
                    return SumAllAtt(PollControlCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_LongPollIntervalMin command)
            {
                return SumATTStr(PollControlCluster.ClusterName, PollControlCluster.ATT_LongPollIntervalMin.LongPollIntervalMin_name);
            }
            public class LongPollIntervalMin_Desired
            {
                public static implicit operator string(LongPollIntervalMin_Desired command)
                {
                    return SumATTStr_WithSubValue(PollControlCluster.ClusterName, PollControlCluster.ATT_LongPollIntervalMin.LongPollIntervalMin_name, ClusterDesired);
                }
            }
            public class LongPollIntervalMin_Report
            {
                public static implicit operator string(LongPollIntervalMin_Report command)
                {
                    return SumATTStr_WithSubValue(PollControlCluster.ClusterName, PollControlCluster.ATT_LongPollIntervalMin.LongPollIntervalMin_name, ClusterReported);
                }
            }
            public class LongPollIntervalMin_SupportCommands
            {
                public static implicit operator string(LongPollIntervalMin_SupportCommands command)
                {
                    return SumSupCMD(PollControlCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_FastPollTimeoutMax
        {
            public const string FastPollTimeoutMax_name = "FastPollTimeoutMax";
            public class FastPollTimeoutMax_Attributes
            {
                public static implicit operator string(FastPollTimeoutMax_Attributes command)
                {
                    return SumAllAtt(PollControlCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_FastPollTimeoutMax command)
            {
                return SumATTStr(PollControlCluster.ClusterName, PollControlCluster.ATT_FastPollTimeoutMax.FastPollTimeoutMax_name);
            }
            public class FastPollTimeoutMax_Desired
            {
                public static implicit operator string(FastPollTimeoutMax_Desired command)
                {
                    return SumATTStr_WithSubValue(PollControlCluster.ClusterName, PollControlCluster.ATT_FastPollTimeoutMax.FastPollTimeoutMax_name, ClusterDesired);
                }
            }
            public class FastPollTimeoutMax_Report
            {
                public static implicit operator string(FastPollTimeoutMax_Report command)
                {
                    return SumATTStr_WithSubValue(PollControlCluster.ClusterName, PollControlCluster.ATT_FastPollTimeoutMax.FastPollTimeoutMax_name, ClusterReported);
                }
            }
            public class FastPollTimeoutMax_SupportCommands
            {
                public static implicit operator string(FastPollTimeoutMax_SupportCommands command)
                {
                    return SumSupCMD(PollControlCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        #endregion

        #region Cluster Commands

        public class CMD_CheckIn
        {
            public const string CheckIn_name = "CheckIn";
            public static implicit operator string(CMD_CheckIn command)
            {
                return SumCMDStr(PollControlCluster.ClusterName, PollControlCluster.CMD_CheckIn.CheckIn_name);
            }
        }/*End of CMD class*/
        #endregion

        #region Cluster Operations

        public struct Payload_Att_SupportedCommands
        {
            public string[] SupportedCommands;
            public Payload_Att_SupportedCommands(string[] newStrArray)
            {
                SupportedCommands = newStrArray;
            }
            public static implicit operator Payload_Att_SupportedCommands(string options)
            {
                return new Payload_Att_SupportedCommands(JsonConvert.DeserializeObject<string[]>(options));
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

        public static implicit operator PollControlCluster(string options)
        {
            return JsonConvert.DeserializeObject<PollControlCluster>(options);
        }
        public static implicit operator string(PollControlCluster command)
        {
            return PollControlCluster.ClusterName;
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
