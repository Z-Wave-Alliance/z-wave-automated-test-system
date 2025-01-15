// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using System.Text;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Enums;
using ZWave.Devices;
using Utils;
using System.Linq;
using System;
using ZWave;
using ZWave.BasicApplication;

namespace Zats.Services
{
    public class ConfigurationSupport : CommandClassSupport
    {
        private ConfigurationSupportData ConfigurationSupportData { get; set; }

        private readonly string CommandClassName = "Configuration CC";
        private string EndPointName = "";
        private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly string UnassignedParamText = "Unassigned Parameter";

        public ConfigurationSupport()
            : this(new ConfigurationSupportData() { })
        { }

        public ConfigurationSupport(ConfigurationSupportData configurationSupportData)
                : base(COMMAND_CLASS_CONFIGURATION.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 4;
            ConfigurationSupportData = configurationSupportData;
            LogLevel = ConfigurationSupportData.LogLevel;
        }

        protected override void OnHandledDelayed(DataReceivedUnit ou)
        {
            ou.SetNextActionItems();
            if (DstNode.EndPointId == ReceivedAchData.DstNode.EndPointId)
            {
                var sendDstNode = ReceivedAchData.DstNode;
                var sendSrcNode = ReceivedAchData.SrcNode;
                byte[] command = ReceivedAchData.Command;
                var scheme = (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme;
                bool isResponseAllowed = IsResponseAllowed(Network, command, scheme, Secure);

                EndPointName = ReceivedAchData.DstNode.EndPointId == 0 ? " @ Root" : string.Format(" @ EP {0}", ReceivedAchData.DstNode.EndPointId);
                ResultForSv = SvResultNames.Undefined;
                if (isResponseAllowed && command != null && command.Length > 1 && ConfigurationSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_CONFIGURATION.CONFIGURATION_SET.ID)
                    {
                        ResultForSv = HandleConfigurationSet(command);
                        DumpConfiguration("Configuration Set", 0);
                    }
                    else if (command[1] == COMMAND_CLASS_CONFIGURATION.CONFIGURATION_GET.ID)
                    {
                        rpt = PrepareConfigurationReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_SET.ID)
                    {
                        ResultForSv = SvResultNames.Undefined;  // leads to NO_SUPPORT
                        if (Version >= 2)
                        {
                            if (Version >= 4 && ConfigurationSupportData.IgnoreBulkCommandsV4)
                            {
                                // ApplicationRejectedRequest MUST NOT be send if CONFIGURATION_BULK_SET was Supervision encapsulated
                                if (!ReceivedAchData.SubstituteIncomingFlags.HasFlag(ZWave.Enums.SubstituteIncomingFlags.Supervision))
                                {
                                    COMMAND_CLASS_APPLICATION_STATUS.APPLICATION_REJECTED_REQUEST data = new COMMAND_CLASS_APPLICATION_STATUS.APPLICATION_REJECTED_REQUEST() { };
                                    data.status = 0; // Rejected
                                    rpt = data;
                                }
                            }
                            else
                            {
                                COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_SET configurationBulkSetCmd = command;
                                ResultForSv = HandleConfigurationBulkSet(command);
                                if (configurationBulkSetCmd.properties1.handshake == 0x01)
                                {
                                    COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT[] bulkReports = CreateBulkReports(command);
                                    IActionItem[] actionItems = new IActionItem[bulkReports.Length];
                                    for (int r = 0; r < bulkReports.Length; r++)
                                    {
                                        bulkReports[r].properties1.handshake = 0x01;
                                        actionItems[r] = new SendDataExOperation(_network, sendDstNode, sendSrcNode, bulkReports[r], TxOptions, TxSecOptions, scheme, TxOptions2);
                                    }
                                    ou.SetNextActionItems(actionItems);
                                }
                            }
                            DumpConfiguration("Configuration Bulk Set", 0);
                        }
                    }
                    else if (command[1] == COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_GET.ID)
                    {
                        IActionItem[] actionItems = PrepareBulkReportActions(command);
                        if (actionItems.Length != 0)
                        {
                            ou.SetNextActionItems(actionItems);
                        }
                    }
                    else if (command[1] == COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_NAME_GET.ID)
                    {
                        IActionItem[] actionItems = PrepareNameReportActions(command);
                        if (actionItems.Length != 0)
                        {
                            ou.SetNextActionItems(actionItems);
                        }
                    }
                    else if (command[1] == COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_INFO_GET.ID)
                    {
                        IActionItem[] actionItems = PrepareInfoReportActions(command);
                        if (actionItems.Length != 0)
                        {
                            ou.SetNextActionItems(actionItems);
                        }
                    }
                    else if (command[1] == COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_PROPERTIES_GET.ID)
                    {
                        rpt = PreparePropertiesReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_CONFIGURATION_V4.CONFIGURATION_DEFAULT_RESET.ID)
                    {
                        ResultForSv = HandleDefaultReset(command);
                        DumpConfiguration("Default Reset", 0);
                    }

                    if (rpt != null)
                    {
                        ApiOperation sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command);
                }
            }
        }

        private uint GetNextParameterNumber (uint currentParameterNumber)
        {
            uint NextParameterNumber = 99999;
            ConfigurationParam reqParam = ConfigurationSupportData.Params.Find(param => param.Number > currentParameterNumber);
            foreach (ConfigurationParam c in ConfigurationSupportData.Params)
            {
                if (c.Number <= currentParameterNumber)
                    continue;
                if (c.Number < NextParameterNumber)
                    NextParameterNumber = c.Number;
            }
            if (NextParameterNumber == 99999)
                NextParameterNumber = 0;
            return NextParameterNumber;
        }

        /*private COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT CreateBulkReport(byte[] command, bool isHandshake)
        {
            COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_GET configurationBulkGetCmd = command; // works for BULK_SET (isHandskake) too
            uint paramOffset = BitConverter.ToUInt16(configurationBulkGetCmd.parameterOffset.Reverse().ToArray(), 0);
            uint paramCount = configurationBulkGetCmd.numberOfParameters;
            byte size = 0;
            byte isAllDefault = 0x01;

            COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT configurationBulkReportCmd = new COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT();
            List<COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT.TVG> valueList = configurationBulkReportCmd.vg;

            for (int paramIndex = 0; paramIndex < paramCount; paramIndex++)
            {
                COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT.TVG tmp = new COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT.TVG();
                ConfigurationParam reqParam = ConfigurationSupportData.Params.Find(param => param.Number == paramIndex + paramOffset);
                if (reqParam != null)
                {
                    if (size == 0)
                        size = reqParam.Size;
                    tmp.parameter = CommandClassSupportUtils.Int64ToByteField(reqParam.Value, size).ToList();
                    if (reqParam.Value != reqParam.DefaultValue)
                        isAllDefault = 0x00;
                }
                else
                {
                    if (size == 0)
                        size = 4;
                    tmp.parameter = CommandClassSupportUtils.Int64ToByteField(0, size).ToList();
                }
                valueList.Add(tmp);
            }

            configurationBulkReportCmd.parameterOffset = configurationBulkGetCmd.parameterOffset;
            configurationBulkReportCmd.numberOfParameters = configurationBulkGetCmd.numberOfParameters;
            configurationBulkReportCmd.properties1.size = size;
            configurationBulkReportCmd.properties1.mdefault = isAllDefault;
            if (isHandshake)
            {
                configurationBulkReportCmd.properties1.handshake = 0x01;
            }
            else
            {
                configurationBulkReportCmd.properties1.handshake = 0x00;
            }
            configurationBulkReportCmd.reportsToFollow = 0;
            return configurationBulkReportCmd;
        }*/

        private IActionItem[] PrepareBulkReportActions(byte[] command)
        {
            List<IActionItem> actionItems = new List<IActionItem> { };
            if (Version >= 2)
            {
                if (Version >= 4 && ConfigurationSupportData.IgnoreBulkCommandsV4)
                {
                    COMMAND_CLASS_APPLICATION_STATUS.APPLICATION_REJECTED_REQUEST data = new COMMAND_CLASS_APPLICATION_STATUS.APPLICATION_REJECTED_REQUEST() { };
                    data.status = 0; // Rejected
                    ApiOperation sendData = new SendDataExOperation(_network, ReceivedAchData.DstNode, ReceivedAchData.SrcNode, data, TxOptions, TxSecOptions, (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme, TxOptions2);
                    actionItems.Add(sendData);
                }
                else
                {
                    COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT[] bulkReports = CreateBulkReports(command);
                    //IActionItem[] actionItems = new IActionItem[bulkReports.Length];
                    for (int r = 0; r < bulkReports.Length; r++)
                    {
                        actionItems.Add(new SendDataExOperation(_network, ReceivedAchData.DstNode, ReceivedAchData.SrcNode, bulkReports[r], TxOptions, TxSecOptions, (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme, TxOptions2));
                    }
                }
            }
            return actionItems.ToArray();
        }

        private IActionItem[] PrepareNameReportActions(byte[] command)
        {
            IActionItem[] actionItems = new IActionItem[] { };
            if (Version >= 3)
            {
                COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_NAME_GET configurationNameGetCmd = command;
                COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_NAME_REPORT rpt = new COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_NAME_REPORT() { };
                uint paramNumber = BitConverter.ToUInt16(configurationNameGetCmd.parameterNumber.Reverse().ToArray(), 0);
                string s = UnassignedParamText;

                ConfigurationParam reqParam = ConfigurationSupportData.Params.Find(param => param.Number == paramNumber);
                rpt.parameterNumber = configurationNameGetCmd.parameterNumber;
                if (reqParam != null)
                {
                    s = reqParam.Name;
                }
                byte[] bStr = Encoding.UTF8.GetBytes(s).ToArray();

                byte reports = (byte)((bStr.Length - 1) / 29 + 1);  // 29 Bytes per frame are available for the name field
                actionItems = new IActionItem[reports];
                for (int r = 0; r < reports; r++)
                {
                    int idx = r * 29;
                    int len = Math.Min(bStr.Length - idx, 29);
                    byte[] bSubStr = new byte[len];
                    Array.Copy(bStr, idx, bSubStr, 0, len);
                    rpt.name = bSubStr;
                    rpt.reportsToFollow = (byte)(reports - r - 1);
                    actionItems[r] = new SendDataExOperation(_network, ReceivedAchData.DstNode, ReceivedAchData.SrcNode, rpt, TxOptions, TxSecOptions, (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme, TxOptions2);
                }
            }
            return actionItems;
        }
        private IActionItem[] PrepareInfoReportActions(byte[] command)
        {
            IActionItem[] actionItems = new IActionItem[] { };
            if (Version >= 3)
            {
                COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_INFO_GET configurationInfoGetCmd = command;
                COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_INFO_REPORT rpt = new COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_INFO_REPORT() { };
                uint paramNumber = BitConverter.ToUInt16(configurationInfoGetCmd.parameterNumber.Reverse().ToArray(), 0);
                string s = UnassignedParamText;

                ConfigurationParam reqParam = ConfigurationSupportData.Params.Find(param => param.Number == paramNumber);
                rpt.parameterNumber = configurationInfoGetCmd.parameterNumber;
                if (reqParam != null)
                {
                    s = reqParam.Info;
                }
                byte[] bStr = Encoding.UTF8.GetBytes(s).ToArray();

                byte reports = (byte)((bStr.Length - 1) / 29 + 1);  // 29 Bytes per frame are available for the info field
                actionItems = new IActionItem[reports];
                for (int r = 0; r < reports; r++)
                {
                    int idx = r * 29;
                    int len = Math.Min(bStr.Length - idx, 29);
                    byte[] bSubStr = new byte[len];
                    Array.Copy(bStr, idx, bSubStr, 0, len);
                    rpt.info = bSubStr;
                    rpt.reportsToFollow = (byte) (reports - r - 1);
                    actionItems[r] = new SendDataExOperation(_network, ReceivedAchData.DstNode, ReceivedAchData.SrcNode, rpt, TxOptions, TxSecOptions, (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme, TxOptions2);
                }
            }
            return actionItems;
        }

        private byte[] PreparePropertiesReport(byte[] command)
        {
            if (Version >= 3)
            {
                COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_PROPERTIES_GET configurationPropertiesGetCmd = command;
                uint paramNumber = BitConverter.ToUInt16(configurationPropertiesGetCmd.parameterNumber.Reverse().ToArray(), 0);
                ConfigurationParam reqParam = ConfigurationSupportData.Params.Find(param => param.Number == paramNumber);

                DumpConfiguration("Properties Get", paramNumber);
                if (Version == 3)
                {
                    COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_PROPERTIES_REPORT rpt = new COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_PROPERTIES_REPORT() { };
                    rpt.parameterNumber = configurationPropertiesGetCmd.parameterNumber;
                    rpt.nextParameterNumber = CommandClassSupportUtils.UInt64ToByteField(GetNextParameterNumber(paramNumber), 2).ToArray();
                    rpt.properties1.format = 0x00;
                    rpt.properties1.size = 0x00;
                    if (reqParam != null)
                    {
                        rpt.properties1.format = (byte) reqParam.Format;
                        rpt.properties1.size = reqParam.Size;
                        rpt.minValue = CommandClassSupportUtils.UInt64ToByteField((uint)reqParam.MinValue, reqParam.Size).ToList();
                        rpt.maxValue = CommandClassSupportUtils.UInt64ToByteField((uint)reqParam.MaxValue, reqParam.Size).ToList();
                        rpt.defaultValue = CommandClassSupportUtils.UInt64ToByteField((uint)reqParam.DefaultValue, reqParam.Size).ToList();
                    }
                    return rpt;
                }
                if (Version == 4)
                {
                    COMMAND_CLASS_CONFIGURATION_V4.CONFIGURATION_PROPERTIES_REPORT rpt = new COMMAND_CLASS_CONFIGURATION_V4.CONFIGURATION_PROPERTIES_REPORT() { };
                    rpt.parameterNumber = configurationPropertiesGetCmd.parameterNumber;
                    rpt.nextParameterNumber = CommandClassSupportUtils.UInt64ToByteField(GetNextParameterNumber(paramNumber), 2).ToArray();
                    rpt.properties1.format = 0x00;
                    rpt.properties1.size = 0x00;
                    if (reqParam != null)
                    {
                        rpt.properties1.format = (byte) reqParam.Format;
                        rpt.properties1.size = reqParam.Size;
                        rpt.properties1.alteringCapabilities = Convert.ToByte(reqParam.IsAltering);
                        rpt.properties1.mreadonly = Convert.ToByte(reqParam.IsReadOnly);
                        rpt.minValue = CommandClassSupportUtils.UInt64ToByteField((uint)reqParam.MinValue, reqParam.Size).ToList();
                        rpt.maxValue = CommandClassSupportUtils.UInt64ToByteField((uint)reqParam.MaxValue, reqParam.Size).ToList();
                        rpt.defaultValue = CommandClassSupportUtils.UInt64ToByteField((uint)reqParam.DefaultValue, reqParam.Size).ToList();
                        rpt.properties2.advanced = Convert.ToByte(reqParam.IsAdvanced);
                    }
                    rpt.properties2.noBulkSupport = Convert.ToByte(ConfigurationSupportData.IgnoreBulkCommandsV4);
                    return rpt;
                }
            }
            return null;
        }

        private byte[] PrepareConfigurationReport(byte[] command)
        {
            COMMAND_CLASS_CONFIGURATION.CONFIGURATION_GET configurationGetCmd = command;
            ConfigurationParam reqParam = ConfigurationSupportData.Params.Find(param => param.Number == configurationGetCmd.parameterNumber);

            DumpConfiguration("Configuration Get", 0);
            if (reqParam == null)
            {
                reqParam = ConfigurationSupportData.Params[0];
            }
            COMMAND_CLASS_CONFIGURATION.CONFIGURATION_REPORT rpt = new COMMAND_CLASS_CONFIGURATION.CONFIGURATION_REPORT()
            {
                parameterNumber = (byte) reqParam.Number,
                properties1 = new COMMAND_CLASS_CONFIGURATION.CONFIGURATION_REPORT.Tproperties1() { size = reqParam.Size },
                configurationValue = CommandClassSupportUtils.UInt64ToByteField((uint) reqParam.Value, reqParam.Size).ToList(),
            };
            return rpt;

        }

        private COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT[] CreateBulkReports(byte[] command)
        {
            COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_GET configurationBulkGetCmd = command; // works for BULK_SET (isHandskake) too
            uint paramOffset = BitConverter.ToUInt16(configurationBulkGetCmd.parameterOffset.Reverse().ToArray(), 0);
            uint paramCount = configurationBulkGetCmd.numberOfParameters;
            byte size = 0;
            byte isAllDefault = 0x01;

            List<COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT.TVG> valueList = new List<COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT.TVG>();

            for (int paramIndex = 0; paramIndex < paramCount; paramIndex++)
            {
                COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT.TVG tmp = new COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT.TVG();
                ConfigurationParam reqParam = ConfigurationSupportData.Params.Find(param => param.Number == paramIndex + paramOffset);
                if (reqParam != null)
                {
                    if (size == 0)
                        size = reqParam.Size;
                    tmp.parameter = CommandClassSupportUtils.Int64ToByteField(reqParam.Value, size).ToList();
                    if (reqParam.Value != reqParam.DefaultValue)
                        isAllDefault = 0x00;
                }
                else
                {
                    if (size == 0)
                        size = 4;
                    tmp.parameter = CommandClassSupportUtils.Int64ToByteField(0, size).ToList();
                }
                valueList.Add(tmp);
            }

            // vg is now ready for all requested parameters. For each parameter block fitting one frame the frame data are 
            // created and stored into a separate list for use as actionItem data.
            byte valuesPerFrame = (byte)(27 / size);    // 27 Bytes per frame are available for vg
            byte reports = (byte)(((valueList.Count - 1) / valuesPerFrame) + 1);

            COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT[] bulkReports = new COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT[reports];

            for (int r = 0; r < reports; r++)
            {
                COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT rpt = new COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT();
                rpt.parameterOffset = CommandClassSupportUtils.Int64ToByteField(paramOffset + (r * valuesPerFrame), 2);
                rpt.numberOfParameters = (byte)Math.Min(valuesPerFrame, valueList.Count - r * valuesPerFrame);
                rpt.reportsToFollow = (byte)(reports - r - 1);
                rpt.properties1.size = size;
                rpt.properties1.mdefault = isAllDefault;
                rpt.properties1.handshake = 0x00; // will be overwritten if method was called from a BulkSet command
                for (int v = 0; v < valuesPerFrame; v++)
                {
                    if (r * valuesPerFrame + v < valueList.Count)
                    {
                        rpt.vg.Add(valueList[r * valuesPerFrame + v]);
                    }
                }
                bulkReports[r] = rpt;
            }
            return bulkReports;
        }

        private SvResultNames HandleConfigurationBulkSet(byte[] command)
        {
            COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_SET configurationBulkSetCmd = command;
            SvResultNames success = SvResultNames.Successful;

            uint paramOffset = BitConverter.ToUInt16(configurationBulkSetCmd.parameterOffset.Reverse().ToArray(), 0);
            uint paramCount = configurationBulkSetCmd.numberOfParameters;
            byte size = configurationBulkSetCmd.properties1.size;

            for (int paramIndex = 0; paramIndex < paramCount; paramIndex++)
            {
                ConfigurationParam reqParam = ConfigurationSupportData.Params.Find(param => param.Number == paramIndex + paramOffset);
                List<COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_SET.TVG> valueList = configurationBulkSetCmd.vg;
                if (SetParam(reqParam, configurationBulkSetCmd.properties1.mdefault, valueList.ElementAt(paramIndex).parameter, size) == SvResultNames.Failed)
                {
                    success = SvResultNames.Failed;
                }
            }
            return success;
        }

        private SvResultNames SetParam(ConfigurationParam param, byte mdefault, IList<byte> valueBytesList, byte size)
        {
            SvResultNames success = SvResultNames.Failed;

            if (param != null)
            {
                if (!param.IsReadOnly || Version < 4)
                {
                    int newValue = (int)CommandClassSupportUtils.ByteFieldToInt64(valueBytesList.ToArray(), size);
                    if (mdefault == 1)
                    {
                        param.Value = param.DefaultValue;
                        success = SvResultNames.Successful;
                    }
                    else if ((size == 1 || size == 2 || size == 4) && param.Size == size && newValue >= param.MinValue && newValue <= param.MaxValue)
                    {
                        param.Value = newValue;
                        success = SvResultNames.Successful;
                    }
                }
            }
            return success;
        }
        private SvResultNames HandleConfigurationSet(byte[] command)
        {
            SvResultNames success = SvResultNames.Failed;

            COMMAND_CLASS_CONFIGURATION.CONFIGURATION_SET configurationSetCmd = command;
            byte size = configurationSetCmd.properties1.size;
            int newValue = (int) CommandClassSupportUtils.ByteFieldToInt64(configurationSetCmd.configurationValue.ToArray(), size);

            ConfigurationParam reqParam = ConfigurationSupportData.Params.Find(param => param.Number == configurationSetCmd.parameterNumber);
            success = SetParam(reqParam, configurationSetCmd.properties1.mdefault, configurationSetCmd.configurationValue, size);

            return success;
        }
        private SvResultNames HandleDefaultReset(byte[] command)
        {
            if (Version == 4)
            { 
                foreach (ConfigurationParam c in ConfigurationSupportData.Params)
                {
                    c.Value = c.DefaultValue;
                }
                return SvResultNames.Successful;
            }
            return SvResultNames.Failed;
        }

        private void DumpConfiguration(string eventText, uint paramNumber)
        {
            if (eventText.Length != 0)
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: {2}", CommandClassName, EndPointName, eventText);
            foreach (ConfigurationParam c in ConfigurationSupportData.Params)
            {
                if (paramNumber == 0 || paramNumber == c.Number)
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "No: {0,5}, Size: {1}, Value: {2,6}, Format = {3}, Min = {4,6}, Max = {5,6}, Def = {6,6},{7}           IsAltering = {8}, IsAdvanced = {9}, Name = {10}, Info = {11}",
                        c.Number, c.Size, c.Value, (byte) c.Format, c.MinValue, c.MaxValue, c.DefaultValue, Environment.NewLine, 
                        c.IsAltering, c.IsAdvanced, c.Name, c.Info);
            }
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet,
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != Id)
            {
                supervisionReport.status = (byte) SvStatusNames.NoSupport;
                return;
            }

            supervisionReport.status = (byte) SvStatusNames.Fail;  // FAIL as default
            switch (supervisionGet.encapsulatedCommand[1])
            {
                case COMMAND_CLASS_CONFIGURATION.CONFIGURATION_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
                case COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
                case COMMAND_CLASS_CONFIGURATION_V4.CONFIGURATION_DEFAULT_RESET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }

    }

    public class ConfigurationParam
    {
        // V1+
        public uint Number;
        public byte Size;
        public int Value = 0;
        // V2+
        // V3+
        public string Name = "Unknown";
        public string Info = "Unknown";
        // Format: 0x00 = Signed Integer (V1+); 0x01 = Unsigned Integer (V3+); 0x02 = Enumerated (Radio buttons) (V3+); 0x03 = Bit field (Checkboxes) (V4+)
        public ConfigurationParameterFormatNames Format = ConfigurationParameterFormatNames.SignedInt;
        public int MinValue = 0;
        public int MaxValue = 0;
        public int DefaultValue = 0;
        // V4+
        public bool IsReadOnly = false;
        public bool IsAltering = false;
        public bool IsAdvanced = false;

        public ConfigurationParam(uint number, byte size, int value, int minValue, int maxValue, int defaultValue)
        {
            Number = number;
            Size = size;
            Value = value;
            MinValue = minValue;
            MaxValue = maxValue;
            DefaultValue = defaultValue;
        }
        public ConfigurationParam(uint number, byte size, int value, int minValue, int maxValue, int defaultValue, 
                                  ConfigurationParameterFormatNames format, string name, string info)
        {
            Number = number;
            Size = size;
            Value = value;
            Name = name;
            Info = info;
            Format = format;
            MinValue = minValue;
            MaxValue = maxValue;
            DefaultValue = defaultValue;
        }
        public ConfigurationParam(uint number, byte size, int value, int minValue, int maxValue, int defaultValue, 
                                  ConfigurationParameterFormatNames format, string name, string info, bool isReadOnly, bool isAltering, bool isAdvanced)
        {
            Number = number;
            Size = size;
            Value = value;
            Name = name;
            Info = info;
            Format = format;
            MinValue = minValue;
            MaxValue = maxValue;
            DefaultValue = defaultValue;
            IsReadOnly = isReadOnly;
            IsAltering = isAltering;
            IsAdvanced = isAdvanced;
        }
    }

    public class ConfigurationSupportData
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        // Number, Size, Value [, Min, Max, Default, Format, Name, Info [, IsReadOnly, IsAltering, IsAdvanced ] ]
        // Example: configurationSupportData.Params.Add(new ConfigurationParam(4, 1, 55, -20, 80, -10, 0x00, "Name4", "Info4", false, false, false));
        public List<ConfigurationParam> Params { get; set; } = new List<ConfigurationParam>();
        // V4+
        public bool IgnoreBulkCommandsV4 = false;
    }

    public enum ConfigurationParameterFormatNames : byte
    {
        // Format: 0x00 = Signed Integer (V1+); 0x01 = Unsigned Integer (V3+); 0x02 = Enumerated (Radio buttons) (V3+); 0x03 = Bit field (Checkboxes) (V4+)
        SignedInt = 0x00,   // V1+
        UnsignedInt = 0x01, // V3+
        Enumerated = 0x02,  // V3+
        BitField = 0x03,    // V4+
    }
}
