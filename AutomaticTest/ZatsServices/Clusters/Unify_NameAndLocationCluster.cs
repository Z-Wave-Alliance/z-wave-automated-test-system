using Newtonsoft.Json;
using System.Collections.Generic;

namespace UicApplication.Clusters
{
    public class NameAndLocationCluster
    {
        public const string ClusterName = "NameAndLocation";
        public const string ClusterDesired = "Desired";
        public const string ClusterReported = "Reported";
        public const string ClusterSupportedCommands = "SupportedCommands";
        public const string ClusterAttributes = "Attributes";

        #region Cluster Attributes

        public class ATT_Name
        {
            public const string Name_name = "Name";
            public class Name_Attributes
            {
                public static implicit operator string(Name_Attributes command)
                {
                    return SumAllAtt(NameAndLocationCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_Name command)
            {
                return SumATTStr(NameAndLocationCluster.ClusterName, NameAndLocationCluster.ATT_Name.Name_name);
            }
            public class Name_Desired
            {
                public static implicit operator string(Name_Desired command)
                {
                    return SumATTStr_WithSubValue(NameAndLocationCluster.ClusterName, NameAndLocationCluster.ATT_Name.Name_name, ClusterDesired);
                }
            }
            public class Name_Report
            {
                public static implicit operator string(Name_Report command)
                {
                    return SumATTStr_WithSubValue(NameAndLocationCluster.ClusterName, NameAndLocationCluster.ATT_Name.Name_name, ClusterReported);
                }
            }
            public class Name_SupportCommands
            {
                public static implicit operator string(Name_SupportCommands command)
                {
                    return SumSupCMD(NameAndLocationCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_Location
        {
            public const string Location_name = "Location";
            public class Location_Attributes
            {
                public static implicit operator string(Location_Attributes command)
                {
                    return SumAllAtt(NameAndLocationCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_Location command)
            {
                return SumATTStr(NameAndLocationCluster.ClusterName, NameAndLocationCluster.ATT_Location.Location_name);
            }
            public class Location_Desired
            {
                public static implicit operator string(Location_Desired command)
                {
                    return SumATTStr_WithSubValue(NameAndLocationCluster.ClusterName, NameAndLocationCluster.ATT_Location.Location_name, ClusterDesired);
                }
            }
            public class Location_Report
            {
                public static implicit operator string(Location_Report command)
                {
                    return SumATTStr_WithSubValue(NameAndLocationCluster.ClusterName, NameAndLocationCluster.ATT_Location.Location_name, ClusterReported);
                }
            }
            public class Location_SupportCommands
            {
                public static implicit operator string(Location_SupportCommands command)
                {
                    return SumSupCMD(NameAndLocationCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        #endregion

        #region Cluster Commands
        public class CMD_WriteAttributes
        {
            public const string WriteAttributes_name = "WriteAttributes";
            public static implicit operator string(CMD_WriteAttributes command)
            {
                return SumCMDStr(NameAndLocationCluster.ClusterName, NameAndLocationCluster.CMD_WriteAttributes.WriteAttributes_name);
            }
            public class CMD_WriteAttributes_Payload
            {
                public string Name { get; set; }
                public string Location { get; set; }
                /*Start of public Payload*/
                public CMD_WriteAttributes_Payload(string my_Name, string my_Location)
                {
                    Name = my_Name;
                    Location = my_Location;
                }
               
                /*End of public Payload*/
                public static implicit operator CMD_WriteAttributes_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_WriteAttributes_Payload>(command);
                }
                public static implicit operator string(CMD_WriteAttributes_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/

        public class CMD_ForceReadAttributes
        {
            public const string ForceReadAttributes_name = "ForceReadAttributes";
            public static implicit operator string(CMD_ForceReadAttributes command)
            {
                return SumCMDStr(NameAndLocationCluster.ClusterName, NameAndLocationCluster.CMD_ForceReadAttributes.ForceReadAttributes_name);
            }
            public class CMD_ForceReadAttributes_Payload
            {
                public List<string> value { get; set; }
                /*Start of public Payload*/
                public CMD_ForceReadAttributes_Payload(List<string> my_value)
                {
                    value = my_value;
                }

                /*End of public Payload*/
                public static implicit operator CMD_ForceReadAttributes_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_ForceReadAttributes_Payload>(command);
                }
                public static implicit operator string(CMD_ForceReadAttributes_Payload command)
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

        public static implicit operator NameAndLocationCluster(string options)
        {
            return JsonConvert.DeserializeObject<NameAndLocationCluster>(options);
        }
        public static implicit operator string(NameAndLocationCluster command)
        {
            return NameAndLocationCluster.ClusterName;
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
