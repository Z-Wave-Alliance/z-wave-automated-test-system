// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Newtonsoft.Json;

namespace UicApplication.Clusters
{
	public class IASZone
    {		
	    public const string ClusterName = "IASZone";
        public const string ClusterDesired = "Desired";
        public const string ClusterReported = "Reported";
        public const string ClusterAttributes = "Attributes";
        public class Value
        {
            public bool Alarm1 { get; set; }
            public bool Alarm2 { get; set; }
            public bool Tamper { get; set; }
            public bool BatteryLow { get; set; }
            public bool SupervisionNotify { get; set; }
            public bool RestoreNotify { get; set; }
            public bool trouble { get; set; }
            public bool ACMainsFault { get; set; }
            public bool Test { get; set; }
            public bool BatteryDefect { get; set; }

        }

        #region Cluster Attributes

        public class ATT_ZoneStatus
        {
            public const string ZoneStatus_name = "ZoneStatus";
            
            public class ZoneStatus_Desired
            {

                public Value value;

                public static implicit operator ZoneStatus_Desired(string options)
                {
                    return JsonConvert.DeserializeObject<ZoneStatus_Desired>(options);
                }

                public static implicit operator string(ZoneStatus_Desired command)
                {
                    return SumATTStr_WithSubValue(IASZone.ClusterName, IASZone.ATT_ZoneStatus.ZoneStatus_name, ClusterDesired);
                }
            }

        }/*End of ATT class*/          

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
        public class Payload_ZoneSatus_Value
        {
            public Value _value { get; set; }

            public Payload_ZoneSatus_Value(Value value)
            {
                _value = value;
            }

            public static implicit operator Payload_ZoneSatus_Value(string options)
            {
                Payload_ZoneSatus_Value deserialized = JsonConvert.DeserializeObject<Payload_ZoneSatus_Value>(options);
                return deserialized;
            }
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
            string SumStr = SumATTStr(ClusterName,AttName) + "/" + SubValue;
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
