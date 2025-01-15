// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Newtonsoft.Json;
using System.Collections.Generic;

namespace UicApplication.Clusters
{
    public class BindingCluster
    {
        public const string ClusterName = "Binding";
        public const string ClusterDesired = "Desired";
        public const string ClusterReported = "Reported";
        public const string ClusterSupportedCommands = "SupportedCommands";
        public const string ClusterAttributes = "Attributes";

        #region Cluster Attributes

        public class ATT_BindingTableFull
        {
            public const string BindingTableFull_name = "BindingTableFull";
            public class BindingTableFull_Attributes
            {
                public static implicit operator string(BindingTableFull_Attributes command)
                {
                    return SumAllAtt(BindingCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_BindingTableFull command)
            {
                return SumATTStr(BindingCluster.ClusterName, BindingCluster.ATT_BindingTableFull.BindingTableFull_name);
            }
            public class BindingTableFull_Desired
            {
                public static implicit operator string(BindingTableFull_Desired command)
                {
                    return SumATTStr_WithSubValue(BindingCluster.ClusterName, BindingCluster.ATT_BindingTableFull.BindingTableFull_name, ClusterDesired);
                }
            }
            public class BindingTableFull_Report
            {
                public static implicit operator string(BindingTableFull_Report command)
                {
                    return SumATTStr_WithSubValue(BindingCluster.ClusterName, BindingCluster.ATT_BindingTableFull.BindingTableFull_name, ClusterReported);
                }
            }
            public class BindingTableFull_SupportCommands
            {
                public static implicit operator string(BindingTableFull_SupportCommands command)
                {
                    return SumSupCMD(BindingCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_BindableClusterList
        {
            public const string BindableClusterList_name = "BindableClusterList";
            public class BindableClusterList_Attributes
            {
                public static implicit operator string(BindableClusterList_Attributes command)
                {
                    return SumAllAtt(BindingCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_BindableClusterList command)
            {
                return SumATTStr(BindingCluster.ClusterName, BindingCluster.ATT_BindableClusterList.BindableClusterList_name);
            }
            public class BindableClusterList_Desired
            {
                public static implicit operator string(BindableClusterList_Desired command)
                {
                    return SumATTStr_WithSubValue(BindingCluster.ClusterName, BindingCluster.ATT_BindableClusterList.BindableClusterList_name, ClusterDesired);
                }
            }
            public class BindableClusterList_Report
            {
                public static implicit operator string(BindableClusterList_Report command)
                {
                    return SumATTStr_WithSubValue(BindingCluster.ClusterName, BindingCluster.ATT_BindableClusterList.BindableClusterList_name, ClusterReported);
                }
            }
            public class BindableClusterList_SupportCommands
            {
                public static implicit operator string(BindableClusterList_SupportCommands command)
                {
                    return SumSupCMD(BindingCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_BindingTable
        {
            public const string BindingTable_name = "BindingTable";
            public class BindingTable_Attributes
            {
                public static implicit operator string(BindingTable_Attributes command)
                {
                    return SumAllAtt(BindingCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_BindingTable command)
            {
                return SumATTStr(BindingCluster.ClusterName, BindingCluster.ATT_BindingTable.BindingTable_name);
            }
            public class BindingTable_Desired
            {
                public static implicit operator string(BindingTable_Desired command)
                {
                    return SumATTStr_WithSubValue(BindingCluster.ClusterName, BindingCluster.ATT_BindingTable.BindingTable_name, ClusterDesired);
                }
            }
            public class BindingTable_Report
            {
                public static implicit operator string(BindingTable_Report command)
                {
                    return SumATTStr_WithSubValue(BindingCluster.ClusterName, BindingCluster.ATT_BindingTable.BindingTable_name, ClusterReported);
                }
            }
            public class BindingTable_SupportCommands
            {
                public static implicit operator string(BindingTable_SupportCommands command)
                {
                    return SumSupCMD(BindingCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        #endregion

        #region Cluster Commands

        public class CMD_Bind
        {
            public const string Bind_name = "Bind";
            public static implicit operator string(CMD_Bind command)
            {
                return SumCMDStr(BindingCluster.ClusterName, BindingCluster.CMD_Bind.Bind_name);
            }
            public class CMD_Bind_Payload
            {
                public string ClusterName { get; set; }
                public string DestinationUnid { get; set; }
                public int DestinationEp { get; set; }
                /*Start of public Payload*/
                public CMD_Bind_Payload(string my_ClusterName, string my_DestinationUnid, int my_DestinationEp)
                {
                    ClusterName = my_ClusterName;
                    DestinationUnid = my_DestinationUnid;
                    DestinationEp = my_DestinationEp;

                }
                /*End of public Payload*/
                public static implicit operator CMD_Bind_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_Bind_Payload>(command);
                }
                public static implicit operator string(CMD_Bind_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        public class CMD_Unbind
        {
            public const string Unbind_name = "Unbind";
            public static implicit operator string(CMD_Unbind command)
            {
                return SumCMDStr(BindingCluster.ClusterName, BindingCluster.CMD_Unbind.Unbind_name);
            }
            public class CMD_Unbind_Payload
            {
                public string ClusterName { get; set; }
                public string DestinationUnid { get; set; }
                public int DestinationEp { get; set; }
                /*Start of public Payload*/
                public CMD_Unbind_Payload(string my_ClusterName, string my_DestinationUnid, int my_DestinationEp)
                {
                    ClusterName = my_ClusterName;
                    DestinationUnid = my_DestinationUnid;
                    DestinationEp = my_DestinationEp;

                }
                /*End of public Payload*/
                public static implicit operator CMD_Unbind_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_Unbind_Payload>(command);
                }
                public static implicit operator string(CMD_Unbind_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        public class CMD_BindToProtocolController
        {
            public const string BindToProtocolController_name = "BindToProtocolController";
            public static implicit operator string(CMD_BindToProtocolController command)
            {
                return SumCMDStr(BindingCluster.ClusterName, BindingCluster.CMD_BindToProtocolController.BindToProtocolController_name);
            }
            public class CMD_BindToProtocolController_Payload
            {
                public string ClusterName { get; set; }
                /*Start of public Payload*/
                public CMD_BindToProtocolController_Payload(string my_ClusterName)
                {
                    ClusterName = my_ClusterName;

                }
                /*End of public Payload*/
                public static implicit operator CMD_BindToProtocolController_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_BindToProtocolController_Payload>(command);
                }
                public static implicit operator string(CMD_BindToProtocolController_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        public class CMD_UnbindFromProtocolController
        {
            public const string UnbindFromProtocolController_name = "UnbindFromProtocolController";
            public static implicit operator string(CMD_UnbindFromProtocolController command)
            {
                return SumCMDStr(BindingCluster.ClusterName, BindingCluster.CMD_UnbindFromProtocolController.UnbindFromProtocolController_name);
            }
            public class CMD_UnbindFromProtocolController_Payload
            {
                public string ClusterName { get; set; }
                /*Start of public Payload*/
                public CMD_UnbindFromProtocolController_Payload(string my_ClusterName)
                {
                    ClusterName = my_ClusterName;

                }
                /*End of public Payload*/
                public static implicit operator CMD_UnbindFromProtocolController_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_UnbindFromProtocolController_Payload>(command);
                }
                public static implicit operator string(CMD_UnbindFromProtocolController_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
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
        public struct Payload_BindingTable_Value
        {
            public class BindingObjects
            {
                public string ClusterName { get; set; }
                public string DestinationUnid { get; set; }
                public int DestinationEp { get; set; }
            }            
            public List<BindingObjects> value { get; set; }  

            public static implicit operator Payload_BindingTable_Value(string options)
            {
                return JsonConvert.DeserializeObject<Payload_BindingTable_Value>(options);
            }
        }
        public struct Payload_BindableClusterList_Value
        {           
            public List<string> value { get; set; }
         
            public static implicit operator Payload_BindableClusterList_Value(string options)
            {
                return JsonConvert.DeserializeObject<Payload_BindableClusterList_Value>(options);
            }
        }
        public struct Payload_BindingTabelFull_Value
        {
            public bool value;

            public static implicit operator Payload_BindingTabelFull_Value(string options)
            {
                return JsonConvert.DeserializeObject<Payload_BindingTabelFull_Value>(options);
            }
        }

        public static implicit operator BindingCluster(string options)
        {
            return JsonConvert.DeserializeObject<BindingCluster>(options);
        }
        public static implicit operator string(BindingCluster command)
        {
            return BindingCluster.ClusterName;
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
