// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Newtonsoft.Json;

namespace UicApplication.Clusters
{
    public class OTAUpgradeCluster
    {
        public const string ClusterName = "OTAUpgrade";
        public const string ClusterDesired = "Desired";
        public const string ClusterReported = "Reported";
        public const string ClusterSupportedCommands = "SupportedCommands";
        public const string ClusterAttributes = "CurrentVersion";

        #region Cluster Attributes
        public class ATT_OTAUpgrade_Switch
        {
            public const string OTAUpgrade_name = "OTA";
            public const string UIID_Swtich_name = "ZWave-0000-0002-0004-00-01";
            public class OTAUpgrade_Attributes
            {
                public static implicit operator string(OTAUpgrade_Attributes command)
                {
                    return SumATTStr_OTA(ClusterName, UIID_Swtich_name, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_OTAUpgrade_Switch command)
            {
                return SumATTStr_OTA(OTAUpgrade_name, UIID_Swtich_name, ClusterAttributes);
            }
            public class OTAUpgrade_Desired
            {
                public static implicit operator string(OTAUpgrade_Desired command)
                {
                    return SumATTStr_WithSub_OTA_Value(OTAUpgrade_name, UIID_Swtich_name, ClusterAttributes, ClusterDesired);
                }
            }
            public class OTAUpgrade_Report
            {
                public static implicit operator string(OTAUpgrade_Report command)
                {
                    return SumATTStr_WithSub_OTA_Value(OTAUpgrade_name, UIID_Swtich_name, ClusterAttributes, ClusterReported);
                }
            }           
        }/*End of ATT class*/
        #endregion

        #region Cluster Commands

        public class CMD_QueryNextImageRequest
        {
            public const string QueryNextImageRequest_name = "QueryNextImageRequest";
            public static implicit operator string(CMD_QueryNextImageRequest command)
            {
                return SumCMDStr(OTAUpgradeCluster.ClusterName, OTAUpgradeCluster.CMD_QueryNextImageRequest.QueryNextImageRequest_name);
            }
            public class CMD_QueryNextImageRequest_Payload
            {
                public string FieldControl { get; set; }
                public string ManufacturerCode { get; set; }
                public string ImageType { get; set; }
                public string CurrentFileVersion { get; set; }
                public struct HardwareVersion_HardwareVersionPresent
                {
                    public string HardwareVersionPresent { get; set; }
                }

                public class HardwareVersion
                {
                    public HardwareVersion_HardwareVersionPresent my_HardwareVersionPresent { get; set; }

                }

                /*Start of public Payload*/
                public CMD_QueryNextImageRequest_Payload(string my_FieldControl, string my_ManufacturerCode, string my_ImageType, string my_CurrentFileVersion, HardwareVersion my_HardwareVersion)
                {
                    FieldControl = my_FieldControl;
                    ManufacturerCode = my_ManufacturerCode;
                    ImageType = my_ImageType;
                    CurrentFileVersion = my_CurrentFileVersion;
                    HardwareVersion HardwareVersion_payload = my_HardwareVersion;
                }/*End of public Payload*/
                public static implicit operator CMD_QueryNextImageRequest_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_QueryNextImageRequest_Payload>(command);
                }
                public static implicit operator string(CMD_QueryNextImageRequest_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        public class CMD_ImageBlockRequest
        {
            public const string ImageBlockRequest_name = "ImageBlockRequest";
            public static implicit operator string(CMD_ImageBlockRequest command)
            {
                return SumCMDStr(OTAUpgradeCluster.ClusterName, OTAUpgradeCluster.CMD_ImageBlockRequest.ImageBlockRequest_name);
            }
            public class CMD_ImageBlockRequest_Payload
            {
                public string FieldControl { get; set; }
                public string ManufacturerCode { get; set; }
                public string ImageType { get; set; }
                public string FileVersion { get; set; }
                public string FileOffset { get; set; }
                public string MaximumDataSize { get; set; }
                public string RequestNodeAddress { get; set; }
                public struct MinimumBlockPeriod_RequestNodeIEEEAddressPresent
                {
                    public string RequestNodeIEEEAddressPresent { get; set; }
                }
                public struct MinimumBlockPeriod_MinimumBlockPeriodPresent
                {
                    public string MinimumBlockPeriodPresent { get; set; }
                }

                public class MinimumBlockPeriod
                {
                    public MinimumBlockPeriod_RequestNodeIEEEAddressPresent my_RequestNodeIEEEAddressPresent { get; set; }
                    public MinimumBlockPeriod_MinimumBlockPeriodPresent my_MinimumBlockPeriodPresent { get; set; }

                }

                /*Start of public Payload*/
                public CMD_ImageBlockRequest_Payload(string my_FieldControl, string my_ManufacturerCode, string my_ImageType, string my_FileVersion, string my_FileOffset, string my_MaximumDataSize, string my_RequestNodeAddress, MinimumBlockPeriod my_MinimumBlockPeriod)
                {
                    FieldControl = my_FieldControl;
                    ManufacturerCode = my_ManufacturerCode;
                    ImageType = my_ImageType;
                    FileVersion = my_FileVersion;
                    FileOffset = my_FileOffset;
                    MaximumDataSize = my_MaximumDataSize;
                    RequestNodeAddress = my_RequestNodeAddress;
                    MinimumBlockPeriod MinimumBlockPeriod_payload = my_MinimumBlockPeriod;
                }/*End of public Payload*/
                public static implicit operator CMD_ImageBlockRequest_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_ImageBlockRequest_Payload>(command);
                }
                public static implicit operator string(CMD_ImageBlockRequest_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        public class CMD_ImagePageRequest
        {
            public const string ImagePageRequest_name = "ImagePageRequest";
            public static implicit operator string(CMD_ImagePageRequest command)
            {
                return SumCMDStr(OTAUpgradeCluster.ClusterName, OTAUpgradeCluster.CMD_ImagePageRequest.ImagePageRequest_name);
            }
            public class CMD_ImagePageRequest_Payload
            {
                public string FieldControl { get; set; }
                public string ManufacturerCode { get; set; }
                public string ImageType { get; set; }
                public string FileVersion { get; set; }
                public string FileOffset { get; set; }
                public string MaximumDataSize { get; set; }
                public string PageSize { get; set; }
                public string ResponseSpacing { get; set; }
                public struct RequestNodeAddress_RequestNodeIEEEAddressPresent
                {
                    public string RequestNodeIEEEAddressPresent { get; set; }
                }

                public class RequestNodeAddress
                {
                    public RequestNodeAddress_RequestNodeIEEEAddressPresent my_RequestNodeIEEEAddressPresent { get; set; }

                }

                /*Start of public Payload*/
                public CMD_ImagePageRequest_Payload(string my_FieldControl, string my_ManufacturerCode, string my_ImageType, string my_FileVersion, string my_FileOffset, string my_MaximumDataSize, string my_PageSize, string my_ResponseSpacing, RequestNodeAddress my_RequestNodeAddress)
                {
                    FieldControl = my_FieldControl;
                    ManufacturerCode = my_ManufacturerCode;
                    ImageType = my_ImageType;
                    FileVersion = my_FileVersion;
                    FileOffset = my_FileOffset;
                    MaximumDataSize = my_MaximumDataSize;
                    PageSize = my_PageSize;
                    ResponseSpacing = my_ResponseSpacing;
                    RequestNodeAddress RequestNodeAddress_payload = my_RequestNodeAddress;
                }/*End of public Payload*/
                public static implicit operator CMD_ImagePageRequest_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_ImagePageRequest_Payload>(command);
                }
                public static implicit operator string(CMD_ImagePageRequest_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        public class CMD_UpgradeEndRequest
        {
            public const string UpgradeEndRequest_name = "UpgradeEndRequest";
            public static implicit operator string(CMD_UpgradeEndRequest command)
            {
                return SumCMDStr(OTAUpgradeCluster.ClusterName, OTAUpgradeCluster.CMD_UpgradeEndRequest.UpgradeEndRequest_name);
            }
            public class CMD_UpgradeEndRequest_Payload
            {
                public string Status { get; set; }
                public string ManufacturerCode { get; set; }
                public string ImageType { get; set; }
                public string FileVersion { get; set; }
                /*Start of public Payload*/
                public CMD_UpgradeEndRequest_Payload(string my_Status, string my_ManufacturerCode, string my_ImageType, string my_FileVersion)
                {
                    Status = my_Status;
                    ManufacturerCode = my_ManufacturerCode;
                    ImageType = my_ImageType;
                    FileVersion = my_FileVersion;

                }
                /*End of public Payload*/
                public static implicit operator CMD_UpgradeEndRequest_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_UpgradeEndRequest_Payload>(command);
                }
                public static implicit operator string(CMD_UpgradeEndRequest_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        public class CMD_QueryDeviceSpecificFileRequest
        {
            public const string QueryDeviceSpecificFileRequest_name = "QueryDeviceSpecificFileRequest";
            public static implicit operator string(CMD_QueryDeviceSpecificFileRequest command)
            {
                return SumCMDStr(OTAUpgradeCluster.ClusterName, OTAUpgradeCluster.CMD_QueryDeviceSpecificFileRequest.QueryDeviceSpecificFileRequest_name);
            }
            public class CMD_QueryDeviceSpecificFileRequest_Payload
            {
                public string RequestNodeAddress { get; set; }
                public string ManufacturerCode { get; set; }
                public string ImageType { get; set; }
                public string FileVersion { get; set; }
                public string CurrentZigbeeStackVersion { get; set; }
                /*Start of public Payload*/
                public CMD_QueryDeviceSpecificFileRequest_Payload(string my_RequestNodeAddress, string my_ManufacturerCode, string my_ImageType, string my_FileVersion, string my_CurrentZigbeeStackVersion)
                {
                    RequestNodeAddress = my_RequestNodeAddress;
                    ManufacturerCode = my_ManufacturerCode;
                    ImageType = my_ImageType;
                    FileVersion = my_FileVersion;
                    CurrentZigbeeStackVersion = my_CurrentZigbeeStackVersion;

                }
                /*End of public Payload*/
                public static implicit operator CMD_QueryDeviceSpecificFileRequest_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_QueryDeviceSpecificFileRequest_Payload>(command);
                }
                public static implicit operator string(CMD_QueryDeviceSpecificFileRequest_Payload command)
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
        public static implicit operator OTAUpgradeCluster(string options)
        {
            return JsonConvert.DeserializeObject<OTAUpgradeCluster>(options);
        }
        public static implicit operator string(OTAUpgradeCluster command)
        {
            return OTAUpgradeCluster.ClusterName;
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
        private static string SumATTStr_OTA(string ClusterName, string Uiid, string AttName)
        {
            string SumStr = "/" + ClusterName + "/" + "Attributes" + "/" + "UIID" + "/" + Uiid + "/" + AttName;
            return SumStr;
        }
        private static string SumATTStr_WithSub_OTA_Value(string ClusterName, string Uiid, string AttName, string SubValue)
        {
            string SumStr = SumATTStr_OTA(ClusterName, Uiid, AttName) + "/" + SubValue;
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
