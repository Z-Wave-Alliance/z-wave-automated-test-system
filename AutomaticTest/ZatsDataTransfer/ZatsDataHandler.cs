// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static Zats.Services.DeviceAttributes;

namespace Zats.Services
{
    public class ZatsDataHandler
    {
        public static ZcpZatsDataList GroupZatsData(ZcpZatsDataList ungroupedList)
        {
            ZcpZatsDataList groupedList = new ZcpZatsDataList() { ZcpZatsData = new List<ZcpZatsData>() };

            if (ungroupedList?.ZcpZatsData != null)
            {
                foreach (ZcpZatsData ungroupedItem in ungroupedList.ZcpZatsData)
                {
                    // check if current ZcpZatsData is already in the grouped list.
                    // if it is, simply add new values, if not, add the whole ZcpZatsData
                    ZcpZatsData groupedItem = groupedList.ZcpZatsData.Find(item => item.Key == ungroupedItem.Key);
                    if (groupedItem != null)
                    {
                        // add the itemIds of the ungroupedItem to the groupedItems itemId list.
                        groupedItem.FormItemNumbers.AddRange(ungroupedItem.FormItemNumbers);
                        groupedItem.FormItemNumbers = groupedItem.FormItemNumbers.Distinct().ToList();
                        groupedItem.ItemIds.AddRange(ungroupedItem.ItemIds);
                        groupedItem.ItemIds = groupedItem.ItemIds.Distinct().ToList();

                        foreach (ZatsValue value in ungroupedItem.ZatsValues)
                        {
                            ZatsValue existingValue = groupedItem.ZatsValues.Find(x => x.Value == value.Value);
                            // add value only if its not already there
                            if (existingValue == null)
                            {
                                groupedItem.ZatsValues.Add(value);
                            }
                            else
                            {
                                existingValue.FormItemNumbers.AddRange(value.FormItemNumbers);
                                existingValue.FormItemNumbers = existingValue.FormItemNumbers.Distinct().ToList();
                                existingValue.ItemIds.AddRange(value.ItemIds);
                                existingValue.ItemIds = existingValue.ItemIds.Distinct().ToList();
                            }
                        }
                    }
                    else
                    {
                        if (groupedList.ZcpZatsData == null)
                        {
                            groupedList.ZcpZatsData = new List<ZcpZatsData>();
                        }
                        groupedList.ZcpZatsData.Add(ungroupedItem);
                    }
                }

                // order keys and values alphabetically
                groupedList.ZcpZatsData = groupedList.ZcpZatsData.OrderBy(x => x.Key).ToList();
                foreach (ZcpZatsData groupedItem in groupedList.ZcpZatsData)
                {
                    groupedItem.ZatsValues = groupedItem.ZatsValues.OrderBy(x => x.Value).ToList();
                }
            }

            return groupedList;
        }

        public static List<DeviceEndpoint> GetEndpointsFromZatsData(List<ZcpZatsData> dataList)
        {
            List<DeviceEndpoint> returnList = new List<DeviceEndpoint>();

            string pattern = @"^ENDPOINT_[0-9]+$";
            List<ZcpZatsData> endpoints = dataList.Where(x => Regex.Match(x.Key, pattern).Success).ToList();

            foreach (ZcpZatsData data in endpoints)
            {
                string endpointKey = data.Key;
                ZcpZatsData optionalCommandClasses = dataList.Where(z => z.Key == (endpointKey + "_OPTIONAL_COMMAND_CLASSES")).FirstOrDefault();

                DeviceEndpoint currentEndpoint = new DeviceEndpoint()
                {
                    Sequence = Convert.ToUInt32(endpointKey.Replace("ENDPOINT_", "")),
                    Id = GetUIntFromZatsData(dataList, endpointKey + "_ID"),
                    Type = GetStringFromZatsData(dataList, endpointKey + "_TYPE"),
                    DeviceType = GetStringFromZatsData(dataList, endpointKey + "_DEVICE_TYPE"),
                    OptionalCommandClasses = new Dictionary<StringValue, ZcpLink>()
                };

                if (optionalCommandClasses != null)
                {
                    foreach (ZatsValue commandClass in optionalCommandClasses.ZatsValues)
                    {
                        currentEndpoint.OptionalCommandClasses.Add(new StringValue()
                        {
                            Key = endpointKey + "_OPTIONAL_COMMAND_CLASSES",
                            Value = commandClass.Value,
                            ItemNumbers = commandClass.FormItemNumbers
                        }, null);
                    }
                }

                returnList.Add(currentEndpoint);
            }
            return returnList;
        }

        public static List<DeviceAssociationGroup> GetAssociationGroupsFromZatsData(List<ZcpZatsData> dataList)
        {
            List<DeviceAssociationGroup> returnList = new List<DeviceAssociationGroup>();

            string pattern = @"^ASSOCIATION_GROUP_[0-9]+$";
            List<ZcpZatsData> associationGroups = dataList.Where(x => Regex.Match(x.Key, pattern).Success).ToList();

            foreach (ZcpZatsData data in associationGroups)
            {
                string associationGroupKey = data.Key;

                DeviceAssociationGroup currentAssociationGroup = new DeviceAssociationGroup()
                {
                    Sequence = Convert.ToUInt32(associationGroupKey.Replace("ASSOCIATION_GROUP_", "")),
                    Name = GetStringFromZatsData(dataList, associationGroupKey + "_NAME"),
                    Profile = GetStringFromZatsData(dataList, associationGroupKey + "_PROFILE"),
                    GroupingId = GetUIntFromZatsData(dataList, associationGroupKey + "_GROUPING_ID"),
                    MaxNodes = GetUIntFromZatsData(dataList, associationGroupKey + "_MAX_NODES"),
                    CommandClassesAndCommand = GetStringFromZatsData(dataList, associationGroupKey + "_COMMAND"),
                };

                ZcpZatsData multiChannelEndpoint = dataList.Where(y => y.Key == associationGroupKey + "_END_POINT_ID").FirstOrDefault();
                if (multiChannelEndpoint != null)
                {
                    currentAssociationGroup.HasMultiChannelEndpoint = true;
                    currentAssociationGroup.MultiChannelEndpointId = GetUIntFromZatsData(dataList, associationGroupKey + "_END_POINT_ID");
                } else
                {
                    currentAssociationGroup.HasMultiChannelEndpoint = false;
                }
                returnList.Add(currentAssociationGroup);
            }

            return returnList;
        }

        public static List<DeviceConfigurationParameter> GetConfigParamsFromZatsData(List<ZcpZatsData> dataList)
        {
            List<DeviceConfigurationParameter> returnList = new List<DeviceConfigurationParameter>();

            string pattern = @"^CONFIG_PARAM_[0-9]+$";
            List<ZcpZatsData> configParams = dataList.Where(x => Regex.Match(x.Key, pattern).Success).ToList();

            foreach (ZcpZatsData data in configParams)
            {
                string configParamKey = data.Key;

                DeviceConfigurationParameter currentConfigParam = new DeviceConfigurationParameter()
                {
                    Sequence = Convert.ToUInt32(configParamKey.Replace("CONFIG_PARAM_", "")),
                    Number = GetUIntFromZatsData(dataList, configParamKey + "_NUMBER"),
                    Name = GetStringFromZatsData(dataList, configParamKey + "_NAME"),
                    Info = GetStringFromZatsData(dataList, configParamKey + "_INFO"),
                    Format = GetStringFromZatsData(dataList, configParamKey + "_FORMAT"),
                    MinValue = GetStringFromZatsData(dataList, configParamKey + "_MIN_VALUE"),
                    MaxValue = GetStringFromZatsData(dataList, configParamKey + "_MAX_VALUE"),
                    DefaultValue = GetStringFromZatsData(dataList, configParamKey + "_DEFAULT_VALUE"),
                    Size = GetStringFromZatsData(dataList, configParamKey + "_SIZE"),
                    Readonly = GetBoolFromZatsData(dataList, configParamKey + "_READ_ONLY"),
                    Advanced = GetBoolFromZatsData(dataList, configParamKey + "_ADVANCED"),
                    AlteringCapabilities = GetBoolFromZatsData(dataList, configParamKey + "_ALTERING_CAPABILITIES"),
                };

                returnList.Add(currentConfigParam);
            }
            return returnList;
        }

        public static List<DeviceMeterType> GetMeterTypesFromZatsData(List<ZcpZatsData> dataList)
        {
            List<DeviceMeterType> returnList = new List<DeviceMeterType>();

            ZcpZatsData meterTypes = dataList.Where(x => x.Key == ("METER_TYPE")).FirstOrDefault();

            if (meterTypes != null)
            {
                foreach (ZatsValue meterType in meterTypes.ZatsValues)
                {
                    string meterTypeKey = "METER_TYPE_" + meterType.Value.Substring(2, 2);
                    ZcpZatsData meterScales = dataList.Where(z => z.Key == (meterTypeKey + "_SCALE")).FirstOrDefault();
                    DeviceMeterType currentMeterType = new DeviceMeterType()
                    {
                        Sequence = Convert.ToUInt32(Convert.ToUInt32(meterType.Value.Substring(0, 4), 16)),
                        Type = new StringValue()
                        {
                            Key = "METER_TYPE",
                            Value = meterType.Value,
                            ItemNumbers = meterType.FormItemNumbers
                        },
                        RateType = GetStringFromZatsData(dataList, meterTypeKey + "_RATE_TYPE"),
                        Reset = GetBoolFromZatsData(dataList, meterTypeKey + "_RESET"),
                        Scales = new List<DeviceMeterType.DeviceMeterScale>()
                    };

                    if (meterScales != null)
                    {
                        foreach (ZatsValue meterScale in meterScales.ZatsValues)
                        {
                            string scaleSizeKey = meterTypeKey + "_SCALE_" + meterScale.Value.Substring(2, 2) + "_SIZE";
                            string scalePrecisionKey = meterTypeKey + "_SCALE_" + meterScale.Value.Substring(2, 2) + "_PRECISION";
                            currentMeterType.Scales.Add(
                                new DeviceMeterType.DeviceMeterScale(
                                    new StringValue()
                                    {
                                        Key = meterTypeKey + "_SCALE",
                                        Value = meterScale.Value,
                                        ItemNumbers = meterScale.FormItemNumbers
                                    },
                                    GetUIntFromZatsData(dataList, scaleSizeKey),
                                    GetUIntFromZatsData(dataList, scalePrecisionKey)
                                )
                            );
                        }
                    }
                    returnList.Add(currentMeterType);
                }
            }

            return returnList;
        }

        public static List<DeviceMultiLevelSensorType> GetMultiLevelSensorTypesFromZatsData(List<ZcpZatsData> dataList)
        {
            List<DeviceMultiLevelSensorType> returnList = new List<DeviceMultiLevelSensorType>();

            ZcpZatsData sensorTypes = dataList.Where(x => x.Key == ("SUPPORTED_MULTILEVEL_SENSOR_TYPE")).FirstOrDefault();

            if (sensorTypes != null)
            {
                foreach (ZatsValue sensorType in sensorTypes.ZatsValues)
                {
                    string sensorTypeKey = "MULTILEVEL_SENSOR_TYPE_" + sensorType.Value.Substring(2, 2);
                    ZcpZatsData sensorScales = dataList.Where(z => z.Key == (sensorTypeKey + "_SCALE")).FirstOrDefault();
                    DeviceMultiLevelSensorType currentSensor = new DeviceMultiLevelSensorType()
                    {
                        Sequence = Convert.ToUInt32(sensorType.Value.Substring(0, 4), 16),
                        Type = new StringValue()
                        {
                            Key = "SUPPORTED_MULTILEVEL_SENSOR_TYPE",
                            Value = sensorType.Value,
                            ItemNumbers = sensorType.FormItemNumbers
                        },
                        Scales = new List<DeviceMultiLevelSensorType.DeviceSensorScale>()
                    };

                    if (sensorScales != null)
                    {
                        foreach (ZatsValue sensorScale in sensorScales.ZatsValues)
                        {
                            string scaleSizeKey = sensorTypeKey + "_SCALE_" + sensorScale.Value.Substring(2, 2) + "_SIZE";
                            string scalePrecisionKey = sensorTypeKey + "_SCALE_" + sensorScale.Value.Substring(2, 2) + "_PRECISION";
                            currentSensor.Scales.Add(
                                new DeviceMultiLevelSensorType.DeviceSensorScale(
                                    new StringValue()
                                    {
                                        Key = sensorTypeKey + "_SCALE",
                                        Value = sensorScale.Value,
                                        ItemNumbers = sensorScale.FormItemNumbers
                                    },
                                    GetUIntFromZatsData(dataList, scaleSizeKey),
                                    GetUIntFromZatsData(dataList, scalePrecisionKey)
                                )
                            );
                        }
                    }
                    returnList.Add(currentSensor);
                }
            }

            return returnList;
        }

        public static List<DeviceNotificationType> GetNotificationTypesFromZatsData(List<ZcpZatsData> dataList)
        {
            List<DeviceNotificationType> returnList = new List<DeviceNotificationType>();

            ZcpZatsData notificationTypes = dataList.Where(x => x.Key == ("NOTIFICATION_TYPE")).FirstOrDefault();

            if (notificationTypes != null)
            {
                foreach (ZatsValue notificationType in notificationTypes.ZatsValues)
                {
                    string notificationTypeKey = "NOTIFICATION_TYPE_" + notificationType.Value.Substring(2, 2);
                    ZcpZatsData notificationEvents = dataList.Where(z => z.Key == (notificationTypeKey + "_EVENT")).FirstOrDefault();
                    DeviceNotificationType currentNotificationType = new DeviceNotificationType()
                    {
                        Sequence = Convert.ToUInt32(notificationType.Value.Substring(0, 4), 16),
                        Type = new StringValue()
                        {
                            Key = "NOTIFICATION_TYPE",
                            Value = notificationType.Value,
                            ItemNumbers = notificationType.FormItemNumbers
                        },
                        Events = new List<StringValue>()
                    };

                    if (notificationEvents != null)
                    {
                        foreach (ZatsValue notficationEvent in notificationEvents.ZatsValues)
                        {
                            currentNotificationType.Events.Add(
                                new StringValue()
                                {
                                    Key = notificationEvents.Key,
                                    Value = notficationEvent.Value,
                                    ItemNumbers = notficationEvent.FormItemNumbers
                                }
                            );
                        }
                    }
                    returnList.Add(currentNotificationType);
                }
            }

            return returnList;
        }

        public static bool HasKey(List<ZcpZatsData> dataList, string key)
        {
            return dataList.Any(y => y.Key == key);
        }

        public static StringValue GetStringFromZatsData(List<ZcpZatsData> dataList, string key)
        {
            string value;
            List<string> itemNumbers = new List<string>();
            List<int> itemIds = new List<int>();
            ZcpZatsData tmpData = dataList.Where(y => y.Key == key).FirstOrDefault();
            if (tmpData is null)
            {
                value = "";
            }
            else if (tmpData.ZatsValues.Count == 0)
            {
                value = "";
            }
            else
            {
                value = tmpData.ZatsValues.First().Value;
                itemNumbers = tmpData.ZatsValues.First().FormItemNumbers;
                itemIds = tmpData.ZatsValues.First().ItemIds;
            }
            return new StringValue()
            {
                Key = key,
                Value = value,
                ItemNumbers = itemNumbers,
                ItemIds = itemIds
            };
        }

        public static UIntValue GetUIntFromZatsData(List<ZcpZatsData> dataList, string key)
        {
            uint value;
            List<string> itemNumbers = new List<string>();
            List<int> itemIds = new List<int>();
            ZcpZatsData tmpData = dataList.Where(y => y.Key == key).FirstOrDefault();
            if (tmpData is null)
            {
                value = 0;
            }
            else if (tmpData.ZatsValues.Count == 0)
            {
                value = 0;
            }
            else
            {
                value = Convert.ToUInt32(tmpData.ZatsValues.First().Value);
                itemNumbers = tmpData.ZatsValues.First().FormItemNumbers;
                itemIds = tmpData.ZatsValues.First().ItemIds;
            }
            return new UIntValue()
            {
                Key = key,
                Value = value,
                ItemNumbers = itemNumbers,
                ItemIds = itemIds
            };
        }

        private static BoolValue GetBoolFromZatsData(List<ZcpZatsData> dataList, string key)
        {
            bool value;
            List<string> itemNumbers = new List<string>();
            List<int> itemIds = new List<int>();
            ZcpZatsData tmpData = dataList.Where(y => y.Key == key).FirstOrDefault();
            if (tmpData is null)
            {
                value = false;
            }
            else if (tmpData.ZatsValues.Count == 0)
            {
                value = false;
            }
            else
            {
                value = Convert.ToBoolean(tmpData.ZatsValues.First().Value);
                itemNumbers = tmpData.ZatsValues.First().FormItemNumbers;
                itemIds = tmpData.ZatsValues.First().ItemIds;
            }
            return new BoolValue()
            {
                Key = key,
                Value = value,
                ItemNumbers = itemNumbers,
                ItemIds = itemIds
            };
        }

        public static List<ZatsValue> GetValueFromZatsData(List<ZcpZatsData> dataList, string key)
        {
            ZcpZatsData tmpData = dataList.Where(y => y.Key == key).FirstOrDefault();
            return tmpData?.ZatsValues;
        }

        public static bool HasListValue(List<ZcpZatsData> dataList, string key, string value)
        {
            List<ZatsValue> valueList = GetValueFromZatsData(dataList, key);

            if (valueList == null)
            {
                return false;
            }

            ZatsValue tmpData = valueList.Where(y => y.Value == value).FirstOrDefault();
            return tmpData is null ? false : true;
        }

        public static byte GetHighestControlledCommandClassVersion(ZcpZatsDataList zcpZatsDataList, string commandClass)
        {
            if (string.IsNullOrEmpty(commandClass) || zcpZatsDataList == null || zcpZatsDataList.ZcpZatsData == null || zcpZatsDataList.ZcpZatsData.Count == 0)
            {
                return 0;
            }

            string[] ccNameSubStringsWithoutVersion = Regex.Split(commandClass, @"_V\d+$");
            if (ccNameSubStringsWithoutVersion.Length > 0)
            {
                commandClass = ccNameSubStringsWithoutVersion[0];
            }

            string highestVersionString = GetValueFromZatsData(zcpZatsDataList.ZcpZatsData, "COMMAND_CLASS_CONTROL")?.FindAll(x => Regex.IsMatch(x.Value, commandClass + @"_V\d+$"))?
                .Select(x => Regex.Match(x.Value, @"\d+$"))?.OrderByDescending(x => x.Value)?.FirstOrDefault()?.Value;
            
            // if there is no CC with a version suffix '_V?', see if the CC is listed without any version suffix
            if (string.IsNullOrEmpty(highestVersionString) && (GetValueFromZatsData(zcpZatsDataList.ZcpZatsData, "COMMAND_CLASS_CONTROL")?.Find(x => x.Value == commandClass)) != null)
            {
                highestVersionString = "1";
            }
            
            Byte.TryParse(highestVersionString, out byte version);

            return version;
        }

        public static byte GetHighestSupportedCommandClassVersion(ZcpZatsDataList zcpZatsDataList, string commandClass)
        {
            if (string.IsNullOrEmpty(commandClass) || zcpZatsDataList == null || zcpZatsDataList.ZcpZatsData == null || zcpZatsDataList.ZcpZatsData.Count == 0)
            {
                return 0;
            }

            string[] ccNameSubStringsWithoutVersion = Regex.Split(commandClass, @"_V\d+$");
            if (ccNameSubStringsWithoutVersion.Length > 0)
            {
                commandClass = ccNameSubStringsWithoutVersion[0];
            }

            string highestVersionString = GetValueFromZatsData(zcpZatsDataList.ZcpZatsData, "COMMAND_CLASS_SUPPORT")?.FindAll(x => Regex.IsMatch(x.Value, commandClass + @"_V\d+$"))?
                .Select(x => Regex.Match(x.Value, @"\d+$"))?.OrderByDescending(x => x.Value)?.FirstOrDefault()?.Value;

            // if there is no CC with a version suffix '_V?', see if the CC is listed without any version suffix
            if (string.IsNullOrEmpty(highestVersionString) && (GetValueFromZatsData(zcpZatsDataList.ZcpZatsData, "COMMAND_CLASS_CONTROL")?.Find(x => x.Value == commandClass)) != null)
            {
                highestVersionString = "1";
            }

            Byte.TryParse(highestVersionString, out byte version);

            return version;
        }
    }
}
