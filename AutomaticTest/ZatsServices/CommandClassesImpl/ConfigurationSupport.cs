// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: 2025 Z-Wave Alliance https://z-wavealliance.org
using System.Collections.Generic;
using System.Text;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Enums;
using ZWave.Devices;
using Utils;
using System.Linq;
using System.Threading;
using System;
using ZWave;
using ZWave.BasicApplication;
using Newtonsoft.Json;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;
using Zats.Services.CommandClassesImpl;
using System.ComponentModel;

namespace Zats.Services
{
    [CommandClassName("Configuration Command Class")]
    [CommandClassId(COMMAND_CLASS_CONFIGURATION.ID)]
    [MinVersion(1)]
    [MaxVersion(4)]
    public class ConfigurationSupport : CommandClassSupport
    {
        private ConfigurationSupportData ConfigurationSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return ConfigurationSupportData; }
            set { ConfigurationSupportData = (ConfigurationSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly string UnassignedParamText = "Unassigned Parameter";

        public ConfigurationSupport()
            : this(new ConfigurationSupportData() { })
        { }

        public ConfigurationSupport(ConfigurationSupportData configurationSupportData)
                : base(COMMAND_CLASS_CONFIGURATION.ID)
        {
            ConfigurationSupportData = configurationSupportData;
        }

        protected override void OnHandledDelayed(DataReceivedUnit ou)
        {
            ou.SetNextActionItems();

            if (MatchesDestinationEndPoint())
            {
                var sendDstNode = new NodeTag(ReceivedAchData.DstNode.Id, DstNode.EndPointId);
                var sendSrcNode = ReceivedAchData.SrcNode;
                byte[] command = ReceivedAchData.Command;
                var scheme = (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme;
                bool isHandlingAllowed = IsHandlingAllowed(Network, command, scheme, Secure);

                ResultForSv = SvResultNames.Undefined;
                if (isHandlingAllowed && command != null && command.Length > 1 && ConfigurationSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_CONFIGURATION.CONFIGURATION_SET.ID)
                    {
                        ResultForSv = HandleConfigurationSetCmd(command);
                        DumpConfiguration("Configuration Set", 0);
                    }
                    else if (command[1] == COMMAND_CLASS_CONFIGURATION.CONFIGURATION_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareConfigurationReportCmd(command);
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
                                    ResultForSv = HandleConfigurationBulkSetCmd(command);
                                    if (configurationBulkSetCmd.properties1.handshake == 0x01 && IsAnsweringAllowed())
                                    {
                                        List<byte[]> bulkReports = CreateBulkReportsCmd(command);
                                        IActionItem[] actionItems = new IActionItem[bulkReports.Count];
                                        for (int r = 0; r < bulkReports.Count; r++)
                                        {
                                            COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT bulkRpt = (COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT)bulkReports[r];
                                            bulkRpt.properties1.handshake = 0x01;
                                            actionItems[r] = new SendDataExOperation(_network, sendDstNode, sendSrcNode, bulkRpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                                        }
                                        ou.SetNextActionItems(actionItems);
                                    }
                            }
                            DumpConfiguration("Configuration Bulk Set", 0);
                        }
                    }
                    else if (command[1] == COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_GET.ID && !IsSupervised())
                    {
                        IActionItem[] actionItems = PrepareBulkReportActionsCmd(command);
                        if (actionItems.Length != 0 && IsAnsweringAllowed())
                        {
                            ou.SetNextActionItems(actionItems);
                        }
                    }
                    else if (command[1] == COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_NAME_GET.ID && !IsSupervised())
                    {
                        IActionItem[] actionItems = PrepareNameReportActionsCmd(command);
                        if (actionItems.Length != 0 && IsAnsweringAllowed())
                        {
                            ou.SetNextActionItems(actionItems);
                        }
                    }
                    else if (command[1] == COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_INFO_GET.ID && !IsSupervised())
                    {
                        IActionItem[] actionItems = PrepareInfoReportActionsCmd(command);
                        if (actionItems.Length != 0 && IsAnsweringAllowed())
                        {
                            ou.SetNextActionItems(actionItems);
                        }
                    }
                    else if (command[1] == COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_PROPERTIES_GET.ID && !IsSupervised())
                    {
                        rpt = PreparePropertiesReportCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_CONFIGURATION_V4.CONFIGURATION_DEFAULT_RESET.ID)
                    {
                        ResultForSv = HandleDefaultReset();
                        DumpConfiguration("Default Reset", 0);
                    }

                    if (rpt != null && IsAnsweringAllowed())
                    {
                        ApiOperation sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command, sendDstNode.EndPointId);
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
                {
                    continue;
                }
                if (c.Number < NextParameterNumber)
                {
                    NextParameterNumber = c.Number;
                }
            }
            if (NextParameterNumber == 99999)
            {
                NextParameterNumber = 0;
            }
            return NextParameterNumber;
        }

        /*private COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_REPORT CreateBulkReportCmd(byte[] command, bool isHandshake)
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
                    {
                        size = reqParam.Size;
                    }
                    tmp.parameter = CommandClassSupportUtils.Int64ToByteField(reqParam.Value, size).ToList();
                    if (reqParam.Value != reqParam.DefaultValue)
                    {
                        isAllDefault = 0x00;
                    }
                }
                else
                {
                    if (size == 0)
                    {
                        size = 4;
                    }
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

        private IActionItem[] PrepareBulkReportActionsCmd(byte[] command)
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
                    List<byte[]> bulkReports = CreateBulkReportsCmd(command);
                    //IActionItem[] actionItems = new IActionItem[bulkReports.Length];
                    for (int r = 0; r < bulkReports.Count; r++)
                    {
                        actionItems.Add(new SendDataExOperation(_network, ReceivedAchData.DstNode, ReceivedAchData.SrcNode, bulkReports[r], TxOptions, TxSecOptions, (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme, TxOptions2));
                    }
                }
            }
            return actionItems.ToArray();
        }

        private List<byte[]> PrepareNameReports(uint paramNumber)
        {
            List<byte[]> reportsList = new List<byte[]>();
            if (Version >= 3)
            {
                COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_NAME_REPORT rpt = new COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_NAME_REPORT() { };
                string s = UnassignedParamText;

                ConfigurationParam reqParam = ConfigurationSupportData.Params.Find(param => param.Number == paramNumber);
                rpt.parameterNumber = CommandClassSupportUtils.UInt64ToByteField(paramNumber, 2);
                if (reqParam != null)
                {
                    s = reqParam.Name;
                }
                byte[] bStr = Encoding.UTF8.GetBytes(s).ToArray();

                byte reports = (byte)((bStr.Length - 1) / 29 + 1);  // 29 Bytes per frame are available for the name field
                for (int r = 0; r < reports; r++)
                {
                    int idx = r * 29;
                    int len = Math.Min(bStr.Length - idx, 29);
                    byte[] bSubStr = new byte[len];
                    Array.Copy(bStr, idx, bSubStr, 0, len);
                    rpt.name = bSubStr;
                    rpt.reportsToFollow = (byte)(reports - r - 1);
                    reportsList.Add(rpt);
                }
            }
            return reportsList;
        }
        private IActionItem[] PrepareNameReportActionsCmd(byte[] command)
        {
            IActionItem[] actionItems = new IActionItem[] { };
            if (Version >= 3)
            {
                COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_NAME_GET configurationNameGetCmd = command;
                uint paramNumber = BitConverter.ToUInt16(configurationNameGetCmd.parameterNumber.Reverse().ToArray(), 0);
                List<byte[]> reportsList = PrepareNameReports(paramNumber);
                actionItems = new IActionItem[reportsList.Count];
                for (int r = 0; r < reportsList.Count; r++)
                {
                    actionItems[r] = new SendDataExOperation(_network, ReceivedAchData.DstNode, ReceivedAchData.SrcNode, reportsList[r], TxOptions, TxSecOptions, (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme, TxOptions2);
                }
            }
            return actionItems;
        }
        private List<byte[]> PrepareInfoReports(uint paramNumber)
        {
            List<byte[]> reportsList = new List<byte[]>();
            if (Version >= 3)
            {
                COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_INFO_REPORT rpt = new COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_INFO_REPORT() { };
                string s = UnassignedParamText;

                ConfigurationParam reqParam = ConfigurationSupportData.Params.Find(param => param.Number == paramNumber);
                rpt.parameterNumber = CommandClassSupportUtils.UInt64ToByteField(paramNumber, 2);
                if (reqParam != null)
                {
                    s = reqParam.Info;
                }
                byte[] bStr = Encoding.UTF8.GetBytes(s).ToArray();

                byte reports = (byte)((bStr.Length - 1) / 29 + 1);  // 29 Bytes per frame are available for the info field
                for (int r = 0; r < reports; r++)
                {
                    int idx = r * 29;
                    int len = Math.Min(bStr.Length - idx, 29);
                    byte[] bSubStr = new byte[len];
                    Array.Copy(bStr, idx, bSubStr, 0, len);
                    rpt.info = bSubStr;
                    rpt.reportsToFollow = (byte) (reports - r - 1);
                    reportsList.Add(rpt);
                }
            }
            return reportsList;
        }
        private IActionItem[] PrepareInfoReportActionsCmd(byte[] command)
        {
            IActionItem[] actionItems = new IActionItem[] { };
            if (Version >= 3)
            {
                COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_INFO_GET configurationInfoGetCmd = command;
                uint paramNumber = BitConverter.ToUInt16(configurationInfoGetCmd.parameterNumber.Reverse().ToArray(), 0);
                List<byte[]> reportsList = PrepareInfoReports(paramNumber);
                actionItems = new IActionItem[reportsList.Count];
                for (int r = 0; r < reportsList.Count; r++)
                {
                    actionItems[r] = new SendDataExOperation(_network, ReceivedAchData.DstNode, ReceivedAchData.SrcNode, reportsList[r], TxOptions, TxSecOptions, (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme, TxOptions2);
                }
            }
            return actionItems;
        }

        private byte[] PreparePropertiesReport(uint paramNumber)
        {
            if (Version >= 3)
            {
                ConfigurationParam reqParam = ConfigurationSupportData.Params.Find(param => param.Number == paramNumber);

                DumpConfiguration("Properties Get", paramNumber);
                if (Version == 3)
                {
                    COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_PROPERTIES_REPORT rpt = new COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_PROPERTIES_REPORT() { };
                    rpt.parameterNumber = CommandClassSupportUtils.UInt64ToByteField(paramNumber, 2);
                    rpt.nextParameterNumber = CommandClassSupportUtils.UInt64ToByteField(GetNextParameterNumber(paramNumber), 2).ToArray();
                    rpt.properties1.format = 0x00;
                    rpt.properties1.size = 0x00;
                    if (reqParam != null)
                    {
                        rpt.properties1.format = (byte)reqParam.Format;
                        rpt.properties1.size = reqParam.Size;
                        rpt.minValue = CommandClassSupportUtils.Int64ToByteField(reqParam.MinValue, reqParam.Size).ToList();
                        rpt.maxValue = CommandClassSupportUtils.Int64ToByteField(reqParam.MaxValue, reqParam.Size).ToList();
                        rpt.defaultValue = CommandClassSupportUtils.Int64ToByteField(reqParam.DefaultValue, reqParam.Size).ToList();
                    }
                    return rpt;
                }
                if (Version == 4)
                {
                    COMMAND_CLASS_CONFIGURATION_V4.CONFIGURATION_PROPERTIES_REPORT rpt = new COMMAND_CLASS_CONFIGURATION_V4.CONFIGURATION_PROPERTIES_REPORT() { };
                    rpt.parameterNumber = CommandClassSupportUtils.UInt64ToByteField(paramNumber, 2);
                    rpt.nextParameterNumber = CommandClassSupportUtils.UInt64ToByteField(GetNextParameterNumber(paramNumber), 2).ToArray();
                    rpt.properties1.format = 0x00;
                    rpt.properties1.size = 0x00;
                    if (reqParam != null)
                    {
                        rpt.properties1.format = (byte)reqParam.Format;
                        rpt.properties1.size = reqParam.Size;
                        rpt.properties1.alteringCapabilities = Convert.ToByte(reqParam.IsAltering);
                        rpt.properties1.mreadonly = Convert.ToByte(reqParam.IsReadOnly);
                        rpt.minValue = CommandClassSupportUtils.Int64ToByteField(reqParam.MinValue, reqParam.Size).ToList();
                        rpt.maxValue = CommandClassSupportUtils.Int64ToByteField(reqParam.MaxValue, reqParam.Size).ToList();
                        rpt.defaultValue = CommandClassSupportUtils.Int64ToByteField(reqParam.DefaultValue, reqParam.Size).ToList();
                        rpt.properties2.advanced = Convert.ToByte(reqParam.IsAdvanced);
                    }
                    rpt.properties2.noBulkSupport = Convert.ToByte(ConfigurationSupportData.IgnoreBulkCommandsV4);
                    return rpt;
                }
            }
            return null;
        }
        private byte[] PreparePropertiesReportCmd(byte[] command)
        {
            if (Version >= 3)
            {
                COMMAND_CLASS_CONFIGURATION_V3.CONFIGURATION_PROPERTIES_GET configurationPropertiesGetCmd = command;
                uint paramNumber = BitConverter.ToUInt16(configurationPropertiesGetCmd.parameterNumber.Reverse().ToArray(), 0);
                return PreparePropertiesReport(paramNumber);
            }
            return null;
        }

        private byte[] PrepareConfigurationReport(uint reqParameterNumber)
        {
            ConfigurationParam reqParam = ConfigurationSupportData.Params.Find(param => param.Number == reqParameterNumber);

            DumpConfiguration("Configuration Get", 0);
            if (reqParam == null && ConfigurationSupportData.Params.Count > 0)
            {
                reqParam = ConfigurationSupportData.Params[0];
            }
            if (reqParam != null)
            {
                COMMAND_CLASS_CONFIGURATION.CONFIGURATION_REPORT rpt = new COMMAND_CLASS_CONFIGURATION.CONFIGURATION_REPORT()
                {
                    parameterNumber = (byte)reqParam.Number,
                    properties1 = new COMMAND_CLASS_CONFIGURATION.CONFIGURATION_REPORT.Tproperties1() { size = reqParam.Size },
                    configurationValue = CommandClassSupportUtils.UInt64ToByteField((uint)reqParam.Value, reqParam.Size).ToList(),
                };
                return rpt;
            }
            return null;
        }
        private byte[] PrepareConfigurationReportCmd(byte[] command)
        {
            COMMAND_CLASS_CONFIGURATION.CONFIGURATION_GET configurationGetCmd = command;
            return PrepareConfigurationReport(configurationGetCmd.parameterNumber);
        }

        private List<byte[]> CreateBulkReports(uint paramOffset, uint paramCount)
        {
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
                    {
                        size = reqParam.Size;
                    }
                    tmp.parameter = CommandClassSupportUtils.Int64ToByteField(reqParam.Value, size).ToList();
                    if (reqParam.Value != reqParam.DefaultValue)
                    {
                        isAllDefault = 0x00;
                    }
                }
                else
                {
                    if (size == 0)
                    {
                        size = 4;
                    }
                    tmp.parameter = CommandClassSupportUtils.Int64ToByteField(0, size).ToList();
                }
                valueList.Add(tmp);
            }

            // vg is now ready for all requested parameters. For each parameter block fitting one frame the frame data are 
            // created and stored into a separate list for use as actionItem data.
            byte valuesPerFrame = (byte)(27 / size);    // 27 Bytes per frame are available for vg
            byte reports = (byte)(((valueList.Count - 1) / valuesPerFrame) + 1);

            List<byte[]> bulkReports = new List<byte[]>();

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
                bulkReports.Add(rpt);
            }
            return bulkReports;
        }
        private List<byte[]> CreateBulkReportsCmd(byte[] command)
        {
            COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_GET configurationBulkGetCmd = command; // works for BULK_SET (isHandskake) too
            uint paramOffset = BitConverter.ToUInt16(configurationBulkGetCmd.parameterOffset.Reverse().ToArray(), 0);
            uint paramCount = configurationBulkGetCmd.numberOfParameters;
            return CreateBulkReports(paramOffset, paramCount);
        }

        private SvResultNames HandleConfigurationBulkSetCmd(byte[] command)
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
                    if (mdefault == 1)
                    {
                        param.Value = param.DefaultValue;
                        success = SvResultNames.Successful;
                    }
                    else
                    {
                        int newValue = (int)CommandClassSupportUtils.ByteFieldToInt64(valueBytesList.ToArray(), size);
                        if ((size == 1 || size == 2 || size == 4) && param.Size == size)
                        {
                            if ((param.Format == ConfigurationParameterFormatNames.SignedInt && newValue >= param.MinValue && newValue <= param.MaxValue)
                             || (param.Format != ConfigurationParameterFormatNames.SignedInt && (uint)newValue >= (uint)param.MinValue && newValue <= (uint)param.MaxValue))
                            {
                                param.Value = newValue;
                                success = SvResultNames.Successful;
                            }
                        }
                    }
                }
            }
            return success;
        }
        private SvResultNames HandleConfigurationSet(uint parameterNumber, byte mdefault, IList<byte> newValue, byte size)
        {
            SvResultNames success = SvResultNames.Failed;

            ConfigurationParam reqParam = ConfigurationSupportData.Params.Find(param => param.Number == parameterNumber);
            success = SetParam(reqParam, mdefault, newValue, size);

            return success;
        }
        private SvResultNames HandleConfigurationSetCmd(byte[] command)
        {
            COMMAND_CLASS_CONFIGURATION.CONFIGURATION_SET configurationSetCmd = command;
            return HandleConfigurationSet(configurationSetCmd.parameterNumber, configurationSetCmd.properties1.mdefault, 
                                          configurationSetCmd.configurationValue, configurationSetCmd.properties1.size);
        }

        private SvResultNames HandleDefaultReset()
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
            {
                CommandClassSupportUtils.ReportAddLog(ConfigurationSupportData.LogLevel, "{0} CC @ {1}: {2}", CommandClassShortName, EndPointName, eventText);
            }
            foreach (ConfigurationParam c in ConfigurationSupportData.Params)
            {
                if (paramNumber == 0 || paramNumber == c.Number)
                {
                    CommandClassSupportUtils.ReportAddLog(ConfigurationSupportData.LogLevel, 
                                "No: {0,5}, Size: {1}, Value: {2,6}, Format = {3}, Min = {4,6}, Max = {5,6}, Def = {6,6},{7}           IsAltering = {8}, IsAdvanced = {9}, Name = {10}, Info = {11}",
                                c.Number, c.Size, c.Value, (byte)c.Format, c.MinValue, c.MaxValue, c.DefaultValue, Environment.NewLine,
                                c.IsAltering, c.IsAdvanced, c.Name, c.Info);
                }
            }
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet,
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != CommandClassId)
            {
                supervisionReport.status = (byte) SvStatusNames.NoSupport;
                return;
            }

            supervisionReport.status = (byte) SvStatusNames.Fail;  // FAIL as default
            switch (supervisionGet.encapsulatedCommand[1])
            {
                case COMMAND_CLASS_CONFIGURATION.CONFIGURATION_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    }
                    break;
                case COMMAND_CLASS_CONFIGURATION_V2.CONFIGURATION_BULK_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    }
                    break;
                case COMMAND_CLASS_CONFIGURATION_V4.CONFIGURATION_DEFAULT_RESET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    }
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }

        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is ConfigurationSupportData newData)
            {
                bool isBulkSupportChanged = false;
                bool isPropertyReportSent = false;
                List<byte[]> configurationReports = new List<byte[]>();
                List<uint> changedParamNumbers = new List<uint>();
                List<ConfigurationParam> parRemoved = ConfigurationSupportData.Params.FindAll(x => newData.Params.Find(y => y.Number == x.Number) == null);
              //List<ConfigurationParam> parAdded   = newData.Params.FindAll(x => ConfigurationSupportData.Params.Find(y => y.Number == x.Number) == null);
                if (parRemoved.Count > 0 /*|| parAdded.Count > 0*/)
                {
                    parRemoved.ForEach(x => ConfigurationSupportData.Params.Remove(x));
                }

                if (ConfigurationSupportData.IgnoreBulkCommandsV4 != newData.IgnoreBulkCommandsV4)
                {
                    isBulkSupportChanged = true;
                    ConfigurationSupportData.IgnoreBulkCommandsV4 = newData.IgnoreBulkCommandsV4;
                }

                foreach (ConfigurationParam newPar in newData.Params)
                {
                    bool isValueChanged = false;
                    bool isNameChanged = false;
                    bool isInfoChanged = false;
                    bool isPropChanged = false;
                    if (ConfigurationSupportData.Params.Find(x => x.Number == newPar.Number) is ConfigurationParam currentPar)
                    {
                        if (currentPar.Size != newPar.Size
                         || currentPar.Value != newPar.Value)
                        {
                            isValueChanged = true;
                        }
                        if (currentPar.Name != newPar.Name)
                        {
                            isNameChanged = true;
                        }
                        if (currentPar.Info != newPar.Info)
                        {
                            isInfoChanged = true;
                        }
                        if (currentPar.MinValue != newPar.MinValue
                         || currentPar.MaxValue != newPar.MaxValue
                         || currentPar.DefaultValue != newPar.DefaultValue)
                        {
                            isPropChanged = true;   // V3+
                        }
                        if (currentPar.IsReadOnly != newPar.IsReadOnly
                         || currentPar.IsAltering != newPar.IsAltering
                         || currentPar.IsAdvanced != newPar.IsAdvanced)
                        {
                            isPropChanged = true;   // V4+
                        }
                        if (isPropChanged)
                        {
                            changedParamNumbers.Add(newPar.Number);
                        }
                        if (isValueChanged || isNameChanged || isInfoChanged || isPropChanged)
                        {
                            ConfigurationSupportData.Params.Remove(currentPar);
                            ConfigurationSupportData.Params.Add(newPar);
                        }
                    }
                    else // It is a new parameter, add it
                    {
                        isValueChanged = true;
                        isNameChanged = true;
                        isInfoChanged = true;
                        isPropChanged = true;
                        changedParamNumbers.Add(newPar.Number);
                        ConfigurationSupportData.Params.Add(newPar);
                    }

                    if (isValueChanged && ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        if (newPar.Number <= 255)
                        {   // Send Configuration Report if Number <= 255
                            configurationReports.Add(PrepareConfigurationReport(newPar.Number));
                        }
                        else
                        {   // Send one Configuration Bulk Report if Number > 255 and Bulk Commands are allowed
                            if (Version == 2 || Version == 3 || (Version >= 4 && !ConfigurationSupportData.IgnoreBulkCommandsV4))
                            {
                                configurationReports.AddRange(CreateBulkReports(newPar.Number, 1));
                            }
                        }

                    }
                    if (isNameChanged && ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        configurationReports.AddRange(PrepareNameReports(newPar.Number));
                    }
                    if (isInfoChanged && ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        configurationReports.AddRange(PrepareInfoReports(newPar.Number));
                    }
                    // We have to delay the Properties Report creation until all newData.Params are parsed, to have the correct nextParameter number
                    //if (isPropChanged && ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    //{
                        //configurationReports.Add(PreparePropertiesReport(newPar.Number));
                        //isPropertyReportSent = true;
                    //}
                }

                // Parsing done. Now prepare the delayed Properties Reports.
                if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                {
                    foreach (uint number in changedParamNumbers)
                    {
                        configurationReports.Add(PreparePropertiesReport(number));
                        isPropertyReportSent = true;
                    }
                }
                // Send a single Properties Report to announce changed BulkSupport, if no other Properties Reports sent.
                // This will send empty values, if #1 not exists, but the changed BulkSupport flag is reported.
                if (isBulkSupportChanged && !isPropertyReportSent && ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                {
                    configurationReports.Add(PreparePropertiesReport(1));
                }

                // Send all prepared reports
                foreach (byte[] rpt in configurationReports)
                {
                    SendUnsolicitedReport(rpt, NodeTag.Empty);
                }

                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class ConfigurationParam : INestedItem
    {
        // V1+
        public uint Number { get; set; }
        public byte Size { get; set; }
        public int Value { get; set; } = 0;
        // V2+
        // V3+
        [CustomDataParameter(2, "Name", UiControl.TextBox, InputType.TextUTF8, EditType.Config)]
        public string Name { get; set; } = "Unknown";
        [CustomDataParameter(2, "Info", UiControl.TextBox, InputType.TextUTF8, EditType.Config)]
        public string Info { get; set; } = "Unknown";
        // Format: 0x00 = Signed Integer (V1+); 0x01 = Unsigned Integer (V3+); 0x02 = Enumerated (Radio buttons) (V3+); 0x03 = Bit field (Checkboxes) (V4+)
        [CustomDataParameter(2, "Format", UiControl.ListComboBox, InputType.Enum, EditType.Config, typeof(ConfigurationParameterFormatNames))]
        public ConfigurationParameterFormatNames Format { get; set; } = ConfigurationParameterFormatNames.SignedInt;
        public int MinValue { get; set; } = 0;
        public int MaxValue { get; set; } = 0;
        public int DefaultValue { get; set; } = 0;
        // V4+
        [CustomDataParameter(4, "Is ReadOnly", UiControl.CheckBox, InputType.Boolean, EditType.Config)]
        public bool IsReadOnly { get; set; } = false;
        [CustomDataParameter(4, "Is Altering", UiControl.CheckBox, InputType.Boolean, EditType.Config)]
        public bool IsAltering { get; set; } = false;
        [CustomDataParameter(4, "Is Advanced", UiControl.CheckBox, InputType.Boolean, EditType.Config)]
        public bool IsAdvanced { get; set; } = false;

        // only used for EDE UI. see interface for useage notes.
        [JsonIgnore]
        public int NestedId
        {
            get => (int)Number;
            set { } 
        }

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
        [JsonConstructor]
        public ConfigurationParam(uint number, byte size, int value, int minValue, int maxValue, int defaultValue, 
                                  ConfigurationParameterFormatNames format, string name, string info, 
                                  bool isReadOnly, bool isAltering, bool isAdvanced)
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

    public class ConfigurationSupportData : CommandClassSupportData, INodeItem
    {
        // V4+
        [CustomDataParameter(4, "No Bulk Support", UiControl.CheckBox, InputType.Boolean, EditType.Config)]
        public bool IgnoreBulkCommandsV4 { get; set; } = false;

        // Internals accessible from TestCases or EDE
        // Number, Size, Value [, Min, Max, Default, Format, Name, Info [, IsReadOnly, IsAltering, IsAdvanced ] ]
        // Example: configurationSupportData.Params.Add(new ConfigurationParam(4, 1, 55, -20, 80, -10, 0x00, "Name4", "Info4", false, false, false));
        [NestedControl(1, "Parameters")]
        public List<ConfigurationParam> Params { get; set; } = new List<ConfigurationParam>();

        [JsonIgnore]
        public IEnumerable<INestedItem> ChildItems => Params;
    }

    public enum ConfigurationParameterFormatNames : byte
    {
        // Format: 0x00 = Signed Integer (V1+); 0x01 = Unsigned Integer (V3+); 0x02 = Enumerated (Radio buttons) (V3+); 0x03 = Bit field (Checkboxes) (V4+)
        [Description("0x00 Signed Integer")]
        SignedInt = 0x00,   // V1+
        [Description("0x01 Unsigned Integer")]
        UnsignedInt = 0x01, // V3+
        [Description("0x02 Enumerated")]
        Enumerated = 0x02,  // V3+
        [Description("0x03 Bit Field")]
        BitField = 0x03,    // V4+
    }
}
