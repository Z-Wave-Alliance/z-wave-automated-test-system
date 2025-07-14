// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Newtonsoft.Json;
using System.Collections.Generic;

namespace UicApplication.Clusters
{
    public class BarrierOperatorCluster
    {
        //WARNING WARNING WARNING ---- This is a dummy cluster to be completed when it is fully implemented on the ZPC

        public const string ClusterName = "BarrierControl";
        public const string ClusterDesired = "Desired";
        public const string ClusterReported = "Reported";
        public const string ClusterSupportedCommands = "SupportedCommands";
        public const string ClusterGeneratedCommands = "GeneratedCommands";
        public const string ClusterAttributes = "Attributes";

        #region Cluster Attributes

        public class ATT_BarrierPosition
        {
            public const string BarrierPosition_name = "BarrierPosition";
            public class BarrierPosition_Attributes
            {
                public static implicit operator string(BarrierPosition_Attributes command)
                {
                    return SumAllAtt(BarrierOperatorCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_BarrierPosition command)
            {
                return SumATTStr(BarrierOperatorCluster.ClusterName, BarrierOperatorCluster.ATT_BarrierPosition.BarrierPosition_name);
            }
            public class BarrierPosition_Desired
            {
                public static implicit operator string(BarrierPosition_Desired command)
                {
                    return SumATTStr_WithSubValue(BarrierOperatorCluster.ClusterName, BarrierOperatorCluster.ATT_BarrierPosition.BarrierPosition_name, ClusterDesired);
                }
            }
            public class BarrierPosition_Report
            {
                public static implicit operator string(BarrierPosition_Report command)
                {
                    return SumATTStr_WithSubValue(BarrierOperatorCluster.ClusterName, BarrierOperatorCluster.ATT_BarrierPosition.BarrierPosition_name, ClusterReported);
                }
            }
            public class BarrierPosition_SupportCommands
            {
                public static implicit operator string(BarrierPosition_SupportCommands command)
                {
                    return SumSupCMD(BarrierOperatorCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/

        //We are missing the rest of the attributes!

        #endregion

        #region Cluster Commands

        public class CMD_GoToPercent
        {
            public const string GoToPercent_name = "GoToPercent";
            public static implicit operator string(CMD_GoToPercent command)
            {
                return SumCMDStr(BarrierOperatorCluster.ClusterName, BarrierOperatorCluster.CMD_GoToPercent.GoToPercent_name);
            }
            public class CMD_GoToPercent_Payload
            {
                public int PercentOpen { get; set; }
                /*Start of public Payload*/
                public CMD_GoToPercent_Payload(int my_value)
                {
                    PercentOpen = my_value;

                }
                /*End of public Payload*/
                public static implicit operator CMD_GoToPercent_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_GoToPercent_Payload>(command);
                }
                public static implicit operator string(CMD_GoToPercent_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }
        }/*End of CMD class*/
        #endregion

        #region Cluster Operations
        public struct Payload_Att_BarrierPosition_Value
        {
            public int value;

            public static implicit operator Payload_Att_BarrierPosition_Value(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_BarrierPosition_Value>(options);
            }
        }

        public static implicit operator BarrierOperatorCluster(string options)
        {
            return JsonConvert.DeserializeObject<BarrierOperatorCluster>(options);
        }
        public static implicit operator string(BarrierOperatorCluster command)
        {
            return BarrierOperatorCluster.ClusterName;
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
