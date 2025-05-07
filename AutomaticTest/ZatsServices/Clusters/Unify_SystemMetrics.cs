using Newtonsoft.Json;
using System.Collections.Generic;

namespace UicApplication.Clusters
{
    public class SystemMetricsCluster
    {
        public const string ClusterName = "SystemMetrics";
        public const string ClusterDesired = "Desired";
        public const string ClusterReported = "Reported";
        public const string ClusterSupportedCommands = "SupportedCommands";
        public const string ClusterAttributes = "Attributes";

        #region Cluster Attributes

        #region Minimum required attribute
        public class ATT_ReportingIntervalSeconds
        {
            public const string ReportingIntervalSeconds_name = "ReportingIntervalSeconds";
            public class ReportingIntervalSeconds_Attributes
            {
                public static implicit operator string(ReportingIntervalSeconds_Attributes command)
                {
                    return SumAllAtt(SystemMetricsCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_ReportingIntervalSeconds command)
            {
                return SumATTStr(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_ReportingIntervalSeconds.ReportingIntervalSeconds_name);
            }

            public class ReportingIntervalSeconds_Report
            {
                public static implicit operator string(ReportingIntervalSeconds_Report command)
                {
                    return SumATTStr_WithSubValue(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_ReportingIntervalSeconds.ReportingIntervalSeconds_name, ClusterReported);
                }
            }
            public class ReportingIntervalSeconds_SupportCommands
            {
                public static implicit operator string(ReportingIntervalSeconds_SupportCommands command)
                {
                    return SumSupCMD(SystemMetricsCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        #endregion

        #region CPU usage
        public class ATT_CPUUsagePercent
        {
            public const string CPUUsagePercent_name = "CPUUsagePercent";
            public class CPUUsagePercent_Attributes
            {
                public static implicit operator string(CPUUsagePercent_Attributes command)
                {
                    return SumAllAtt(SystemMetricsCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_CPUUsagePercent command)
            {
                return SumATTStr(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_CPUUsagePercent.CPUUsagePercent_name);
            }

            public class CPUUsagePercent_Report
            {
                public static implicit operator string(CPUUsagePercent_Report command)
                {
                    return SumATTStr_WithSubValue(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_CPUUsagePercent.CPUUsagePercent_name, ClusterReported);
                }
            }
            public class CPUUsagePercent_SupportCommands
            {
                public static implicit operator string(CPUUsagePercent_SupportCommands command)
                {
                    return SumSupCMD(SystemMetricsCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_CPUFrequencyMHz
        {
            public const string CPUFrequencyMHz_name = "CPUFrequencyMHz";
            public class CPUFrequencyMHz_Attributes
            {
                public static implicit operator string(CPUFrequencyMHz_Attributes command)
                {
                    return SumAllAtt(SystemMetricsCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_CPUFrequencyMHz command)
            {
                return SumATTStr(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_CPUFrequencyMHz.CPUFrequencyMHz_name);
            }

            public class CPUFrequencyMHz_Report
            {
                public static implicit operator string(CPUFrequencyMHz_Report command)
                {
                    return SumATTStr_WithSubValue(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_CPUFrequencyMHz.CPUFrequencyMHz_name, ClusterReported);
                }
            }
            public class CPUFrequencyMHz_SupportCommands
            {
                public static implicit operator string(CPUFrequencyMHz_SupportCommands command)
                {
                    return SumSupCMD(SystemMetricsCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_CPUAverageUsagePercent
        {
            public const string CPUAverageUsagePercent_name = "CPUAverageUsagePercent";
            public class CPUAverageUsagePercent_Attributes
            {
                public static implicit operator string(CPUAverageUsagePercent_Attributes command)
                {
                    return SumAllAtt(SystemMetricsCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_CPUAverageUsagePercent command)
            {
                return SumATTStr(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_CPUAverageUsagePercent.CPUAverageUsagePercent_name);
            }

            public class CPUAverageUsagePercent_Report
            {
                public static implicit operator string(CPUAverageUsagePercent_Report command)
                {
                    return SumATTStr_WithSubValue(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_CPUAverageUsagePercent.CPUAverageUsagePercent_name, ClusterReported);
                }
            }
            public class CPUAverageUsagePercent_SupportCommands
            {
                public static implicit operator string(CPUAverageUsagePercent_SupportCommands command)
                {
                    return SumSupCMD(SystemMetricsCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_CPUMinUsagePercent
        {
            public const string CPUMinUsagePercent_name = "CPUMinUsagePercent";
            public class CPUMinUsagePercent_Attributes
            {
                public static implicit operator string(CPUMinUsagePercent_Attributes command)
                {
                    return SumAllAtt(SystemMetricsCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_CPUMinUsagePercent command)
            {
                return SumATTStr(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_CPUMinUsagePercent.CPUMinUsagePercent_name);
            }

            public class CPUMinUsagePercent_Report
            {
                public static implicit operator string(CPUMinUsagePercent_Report command)
                {
                    return SumATTStr_WithSubValue(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_CPUMinUsagePercent.CPUMinUsagePercent_name, ClusterReported);
                }
            }
            public class CPUMinUsagePercent_SupportCommands
            {
                public static implicit operator string(CPUMinUsagePercent_SupportCommands command)
                {
                    return SumSupCMD(SystemMetricsCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_CPUMaxUsagePercent
        {
            public const string CPUMaxUsagePercent_name = "CPUMaxUsagePercent";
            public class CPUMaxUsagePercent_Attributes
            {
                public static implicit operator string(CPUMaxUsagePercent_Attributes command)
                {
                    return SumAllAtt(SystemMetricsCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_CPUMaxUsagePercent command)
            {
                return SumATTStr(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_CPUMaxUsagePercent.CPUMaxUsagePercent_name);
            }

            public class CPUMaxUsagePercent_Report
            {
                public static implicit operator string(CPUMaxUsagePercent_Report command)
                {
                    return SumATTStr_WithSubValue(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_CPUMaxUsagePercent.CPUMaxUsagePercent_name, ClusterReported);
                }
            }
            public class CPUMaxUsagePercent_SupportCommands
            {
                public static implicit operator string(CPUMaxUsagePercent_SupportCommands command)
                {
                    return SumSupCMD(SystemMetricsCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        #endregion

        #region Memory data
        public class ATT_RAMTotalMB
        {
            public const string RAMTotalMB_name = "RAMTotalMB";
            public class RAMTotalMB_Attributes
            {
                public static implicit operator string(RAMTotalMB_Attributes command)
                {
                    return SumAllAtt(SystemMetricsCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_RAMTotalMB command)
            {
                return SumATTStr(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_RAMTotalMB.RAMTotalMB_name);
            }

            public class RAMTotalMB_Report
            {
                public static implicit operator string(RAMTotalMB_Report command)
                {
                    return SumATTStr_WithSubValue(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_RAMTotalMB.RAMTotalMB_name, ClusterReported);
                }
            }
            public class RAMTotalMB_SupportCommands
            {
                public static implicit operator string(RAMTotalMB_SupportCommands command)
                {
                    return SumSupCMD(SystemMetricsCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_RAMFreeMB
        {
            public const string RAMFreeMB_name = "RAMFreeMB";
            public class RAMFreeMB_Attributes
            {
                public static implicit operator string(RAMFreeMB_Attributes command)
                {
                    return SumAllAtt(SystemMetricsCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_RAMFreeMB command)
            {
                return SumATTStr(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_RAMFreeMB.RAMFreeMB_name);
            }

            public class RAMFreeMB_Report
            {
                public static implicit operator string(RAMFreeMB_Report command)
                {
                    return SumATTStr_WithSubValue(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_RAMFreeMB.RAMFreeMB_name, ClusterReported);
                }
            }
            public class RAMFreeMB_SupportCommands
            {
                public static implicit operator string(RAMFreeMB_SupportCommands command)
                {
                    return SumSupCMD(SystemMetricsCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_RAMAvailableMB
        {
            public const string RAMAvailableMB_name = "RAMAvailableMB";
            public class RAMAvailableMB_Attributes
            {
                public static implicit operator string(RAMAvailableMB_Attributes command)
                {
                    return SumAllAtt(SystemMetricsCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_RAMAvailableMB command)
            {
                return SumATTStr(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_RAMAvailableMB.RAMAvailableMB_name);
            }

            public class RAMAvailableMB_Report
            {
                public static implicit operator string(RAMAvailableMB_Report command)
                {
                    return SumATTStr_WithSubValue(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_RAMAvailableMB.RAMAvailableMB_name, ClusterReported);
                }
            }
            public class RAMAvailableMB_SupportCommands
            {
                public static implicit operator string(RAMAvailableMB_SupportCommands command)
                {
                    return SumSupCMD(SystemMetricsCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_SWAPMemoryTotalMB
        {
            public const string SWAPMemoryTotalMB_name = "SWAPMemoryTotalMB";
            public class SWAPMemoryTotalMB_Attributes
            {
                public static implicit operator string(SWAPMemoryTotalMB_Attributes command)
                {
                    return SumAllAtt(SystemMetricsCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_SWAPMemoryTotalMB command)
            {
                return SumATTStr(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_SWAPMemoryTotalMB.SWAPMemoryTotalMB_name);
            }

            public class SWAPMemoryTotalMB_Report
            {
                public static implicit operator string(SWAPMemoryTotalMB_Report command)
                {
                    return SumATTStr_WithSubValue(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_SWAPMemoryTotalMB.SWAPMemoryTotalMB_name, ClusterReported);
                }
            }
            public class SWAPMemoryTotalMB_SupportCommands
            {
                public static implicit operator string(SWAPMemoryTotalMB_SupportCommands command)
                {
                    return SumSupCMD(SystemMetricsCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_SWAPMemoryUsedMB
        {
            public const string SWAPMemoryUsedMB_name = "SWAPMemoryUsedMB";
            public class SWAPMemoryUsedMB_Attributes
            {
                public static implicit operator string(SWAPMemoryUsedMB_Attributes command)
                {
                    return SumAllAtt(SystemMetricsCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_SWAPMemoryUsedMB command)
            {
                return SumATTStr(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_SWAPMemoryUsedMB.SWAPMemoryUsedMB_name);
            }

            public class SWAPMemoryUsedMB_Report
            {
                public static implicit operator string(SWAPMemoryUsedMB_Report command)
                {
                    return SumATTStr_WithSubValue(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_SWAPMemoryUsedMB.SWAPMemoryUsedMB_name, ClusterReported);
                }
            }
            public class SWAPMemoryUsedMB_SupportCommands
            {
                public static implicit operator string(SWAPMemoryUsedMB_SupportCommands command)
                {
                    return SumSupCMD(SystemMetricsCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_VirtualMemoryTotalMB
        {
            public const string VirtualMemoryTotalMB_name = "VirtualMemoryTotalMB";
            public class VirtualMemoryTotalMB_Attributes
            {
                public static implicit operator string(VirtualMemoryTotalMB_Attributes command)
                {
                    return SumAllAtt(SystemMetricsCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_VirtualMemoryTotalMB command)
            {
                return SumATTStr(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_VirtualMemoryTotalMB.VirtualMemoryTotalMB_name);
            }

            public class VirtualMemoryTotalMB_Report
            {
                public static implicit operator string(VirtualMemoryTotalMB_Report command)
                {
                    return SumATTStr_WithSubValue(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_VirtualMemoryTotalMB.VirtualMemoryTotalMB_name, ClusterReported);
                }
            }
            public class VirtualMemoryTotalMB_SupportCommands
            {
                public static implicit operator string(VirtualMemoryTotalMB_SupportCommands command)
                {
                    return SumSupCMD(SystemMetricsCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_VirtualMemoryUsedMB
        {
            public const string VirtualMemoryUsedMB_name = "VirtualMemoryUsedMB";
            public class VirtualMemoryUsedMB_Attributes
            {
                public static implicit operator string(VirtualMemoryUsedMB_Attributes command)
                {
                    return SumAllAtt(SystemMetricsCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_VirtualMemoryUsedMB command)
            {
                return SumATTStr(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_VirtualMemoryUsedMB.VirtualMemoryUsedMB_name);
            }

            public class VirtualMemoryUsedMB_Report
            {
                public static implicit operator string(VirtualMemoryUsedMB_Report command)
                {
                    return SumATTStr_WithSubValue(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_VirtualMemoryUsedMB.VirtualMemoryUsedMB_name, ClusterReported);
                }
            }
            public class VirtualMemoryUsedMB_SupportCommands
            {
                public static implicit operator string(VirtualMemoryUsedMB_SupportCommands command)
                {
                    return SumSupCMD(SystemMetricsCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_DisksUsage
        {
            public const string DisksUsage_name = "DisksUsage";
            public class DisksUsage_Attributes
            {
                public static implicit operator string(DisksUsage_Attributes command)
                {
                    return SumAllAtt(SystemMetricsCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_DisksUsage command)
            {
                return SumATTStr(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_DisksUsage.DisksUsage_name);
            }

            public class DisksUsage_Report
            {
                public static implicit operator string(DisksUsage_Report command)
                {
                    return SumATTStr_WithSubValue(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_DisksUsage.DisksUsage_name, ClusterReported);
                }
            }
            public class DisksUsage_SupportCommands
            {
                public static implicit operator string(DisksUsage_SupportCommands command)
                {
                    return SumSupCMD(SystemMetricsCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_DisksCounters
        {
            public const string DisksCounters_name = "DisksCounters";
            public class DisksCounters_Attributes
            {
                public static implicit operator string(DisksCounters_Attributes command)
                {
                    return SumAllAtt(SystemMetricsCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_DisksCounters command)
            {
                return SumATTStr(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_DisksCounters.DisksCounters_name);
            }

            public class DisksCounters_Report
            {
                public static implicit operator string(DisksCounters_Report command)
                {
                    return SumATTStr_WithSubValue(SystemMetricsCluster.ClusterName, SystemMetricsCluster.ATT_DisksCounters.DisksCounters_name, ClusterReported);
                }
            }
            public class DisksCounters_SupportCommands
            {
                public static implicit operator string(DisksCounters_SupportCommands command)
                {
                    return SumSupCMD(SystemMetricsCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        #endregion

        #endregion

        #region Cluster Commands
        public class CMD_WRiteAttributes
        {
            public const string WriteAttributes_name = "WriteAttributes";
            public static implicit operator string(CMD_WRiteAttributes command)
            {
                return SumCMDStr(SystemMetricsCluster.ClusterName, SystemMetricsCluster.CMD_WRiteAttributes.WriteAttributes_name);
            }            
        }/*End of CMD class with Childnodes*/
        public class CMD_WriteAttributes_WithPayload
        {
            public const string WriteAttributes_name = "WriteAttributes";
            public static implicit operator string(CMD_WriteAttributes_WithPayload command)
            {
                return SumCMDStr(SystemMetricsCluster.ClusterName, SystemMetricsCluster.CMD_WriteAttributes_WithPayload.WriteAttributes_name);
            }
            public class CMD_WriteAttributes_Payload
            {
                public int ReportingIntervalSeconds { get; set; }                
                /*Start of public Payload*/
                public CMD_WriteAttributes_Payload(int my_ReportingIntervalSeconds)
                {
                    ReportingIntervalSeconds = my_ReportingIntervalSeconds;                   
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
        public struct Payload_WriteAttributes 
        {
            public string[] value;
            public Payload_WriteAttributes(string[] newStrArray)
            {
                value = newStrArray;
            }
            public static implicit operator Payload_WriteAttributes(string options)
            {
                return new Payload_WriteAttributes(JsonConvert.DeserializeObject<string[]>(options));
            }
        }
        public struct Payload_Att_ReportingIntervalSeconds_Rep
        {
            public int value;

            public static implicit operator Payload_Att_ReportingIntervalSeconds_Rep(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_ReportingIntervalSeconds_Rep>(options);
            }
        }
        public struct Payload_ATT_CPU_Rep
        {
            public double value;

            public static implicit operator Payload_ATT_CPU_Rep(string options)
            {
                return JsonConvert.DeserializeObject<Payload_ATT_CPU_Rep>(options);
            }
        }
        public struct Payload_Att_MemoryData_Rep
        {
            public double value;

            public static implicit operator Payload_Att_MemoryData_Rep(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_MemoryData_Rep>(options);
            }
        }
        public struct Payload_Att_DisksUsage_Rep
        {
            public class DiskUsageData
            {                
                public string UUID { get; set; }
                public double UsagePercent { get; set; }
                public double FreeSpaceMB { get; set; }
            }

            public DiskUsageData _value { get; set; }

            public Payload_Att_DisksUsage_Rep(DiskUsageData value)
            {
                _value = value;
            }

            public static implicit operator Payload_Att_DisksUsage_Rep(string options)
            {
                DiskUsageData deserialized = JsonConvert.DeserializeObject<DiskUsageData>(options);
                return new Payload_Att_DisksUsage_Rep(deserialized);
            }
        }
        public struct Payload_Att_DisksCounters_Rep
        {
            public class DiskIOCounters
            {
                public string UUID { get; set; }
                public ulong ReadCount { get; set; }
                public ulong WriteCount { get; set; }
                public ulong ReadBytes { get; set; }
                public ulong WriteBytes { get; set; }

            }
            public DiskIOCounters _value { get; set; }

            public Payload_Att_DisksCounters_Rep(DiskIOCounters value)
            {
                _value = value;
            }

            public static implicit operator Payload_Att_DisksCounters_Rep(string options)
            {
                DiskIOCounters deserialized = JsonConvert.DeserializeObject<DiskIOCounters>(options);
                return new Payload_Att_DisksCounters_Rep(deserialized);
            }
        }

        public static implicit operator SystemMetricsCluster(string options)
        {
            return JsonConvert.DeserializeObject<SystemMetricsCluster>(options);
        }
        public static implicit operator string(SystemMetricsCluster command)
        {
            return SystemMetricsCluster.ClusterName;
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
