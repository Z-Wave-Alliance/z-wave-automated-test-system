// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Utils;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;
using Utils.UI.Enums;
using System.Security.Authentication.ExtendedProtection;
using ZWave.Xml.FrameHeader;

// ToDo: Supervision Report Status 'Working' ?
// ToDo: Validation of Indicator IDs and Property IDs according to CC version in AddIndicator()
// ToDo: Validation of Property ID groups in Indicator Set
// ToDo: Logging ?

namespace Zats.Services
{
    public class IndicatorSupportModify : CommandClassSupport
    {
        private IndicatorSupportData IndicatorSupportData { get; set; }

        //private readonly string CommandClassName = "Indicator CC";
        //private string EndPointName = "";
        //private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public IndicatorSupportModify()
            : this(new IndicatorSupportData() { })
        { }

        public IndicatorSupportModify(IndicatorSupportData indicatorSupportData)
            : base(COMMAND_CLASS_INDICATOR.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 4;
            IndicatorSupportData = indicatorSupportData;
            //LogLevel = IndicatorSupportData.LogLevel;

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
                if (isHandlingAllowed && command != null && command.Length > 1 && IndicatorSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_INDICATOR_V2.INDICATOR_GET.ID)
                    {
                        rpt = PrepareIndicatorReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_INDICATOR_V2.INDICATOR_SUPPORTED_GET.ID)
                    {
                        rpt = PrepareSupportedReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_INDICATOR_V4.INDICATOR_DESCRIPTION_GET.ID)
                    {
                        rpt = PrepareDescriptionReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_INDICATOR.INDICATOR_SET.ID)
                    {
                        ResultForSv = HandleIndicatorSet(command);
                    }

                    if (rpt != null)
                    {
                        ApiOperation sendData = new SendDataExOperation(Network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command, sendDstNode.EndPointId);
                }
            }
        }

        private byte[] PrepareSupportedReport(byte[] command)
        {
            if (Version >= 2)
            {
                COMMAND_CLASS_INDICATOR_V2.INDICATOR_SUPPORTED_REPORT rpt = new COMMAND_CLASS_INDICATOR_V2.INDICATOR_SUPPORTED_REPORT() { };
                byte requestedIndicatorId = ((COMMAND_CLASS_INDICATOR_V2.INDICATOR_SUPPORTED_GET) command).indicatorId;

                // A supporting node receiving this field set to 0x00 MUST advertise the first supported Indicator ID in response.
                if (requestedIndicatorId == 0 && IndicatorSupportData.Indicators.Count != 0)
                {
                    requestedIndicatorId = IndicatorSupportData.Indicators[0].Id;
                }

                Indicator indObj = IndicatorSupportData.Indicators.Find(indicator => indicator.Id == requestedIndicatorId);
                if (indObj != null)
                {
                    List<byte> propList = new List<byte> { };
                    foreach (IndicatorProperty prop in indObj.Properties)
                    {
                        propList.Add(prop.PropertyId);
                    }
                    rpt.indicatorId = requestedIndicatorId;
                    rpt.nextIndicatorId = GetNextIndicatorId(requestedIndicatorId);
                    rpt.propertySupportedBitMask = CommandClassSupportUtils.CreateBitmaskFromList(propList, 0);
                    rpt.properties1.propertySupportedBitMaskLength = (byte) rpt.propertySupportedBitMask.Count;
                }
                else
                {
                    rpt.indicatorId = 0;
                    rpt.nextIndicatorId = 0;
                    rpt.propertySupportedBitMask = new List<byte> { };
                    rpt.properties1.propertySupportedBitMaskLength = (byte) rpt.propertySupportedBitMask.Count;
                }
                return rpt;
            }
            return null;
        }

        private byte[] PrepareDescriptionReport(byte[] command)
        {
            if (Version == 4)
            {
                COMMAND_CLASS_INDICATOR_V4.INDICATOR_DESCRIPTION_REPORT rpt = new COMMAND_CLASS_INDICATOR_V4.INDICATOR_DESCRIPTION_REPORT() { };
                byte requestedIndicatorId = ((COMMAND_CLASS_INDICATOR_V4.INDICATOR_SUPPORTED_GET) command).indicatorId;

                rpt.description = new List<byte> { };
                if (requestedIndicatorId >= (byte) IndicatorIdNames.ManufacturerDefinedIndicator1
                 && requestedIndicatorId <= (byte) IndicatorIdNames.ManufacturerDefinedIndicator32)
                {
                    Indicator indObj = IndicatorSupportData.Indicators.Find(indicator => indicator.Id == requestedIndicatorId);
                    if (indObj != null)
                    {
                        rpt.description = indObj.Description;
                    }
                }
                rpt.indicatorId = requestedIndicatorId;
                rpt.descriptionLength = (byte) rpt.description.Count;
                return rpt;
            }
            return null;
        }

        private byte[] PrepareIndicatorReport(byte[] command)
        {
            if (Version == 1)
            {
                COMMAND_CLASS_INDICATOR.INDICATOR_REPORT rpt = new COMMAND_CLASS_INDICATOR.INDICATOR_REPORT() { };
                if (IndicatorSupportData.Indicators.Count != 0)
                {
                    rpt.value = IndicatorSupportData.Indicators[0].Properties[0].PropertyValue;
                    return rpt;
                }
            }
            if (Version >= 2)
            {
                byte requestedIndicatorId = ((COMMAND_CLASS_INDICATOR_V2.INDICATOR_GET) command).indicatorId;
                COMMAND_CLASS_INDICATOR_V2.INDICATOR_REPORT rpt = new COMMAND_CLASS_INDICATOR_V2.INDICATOR_REPORT() { };

                Indicator indObj = IndicatorSupportData.Indicators.Find(indicator => indicator.Id == requestedIndicatorId);
                if (indObj != null)
                {
                    foreach (IndicatorProperty prop in indObj.Properties)
                    {
                        rpt.vg1.Add(new COMMAND_CLASS_INDICATOR_V2.INDICATOR_REPORT.TVG1()
                        {
                            indicatorId = requestedIndicatorId,
                            propertyId = prop.PropertyId,
                            value = prop.PropertyValue,
                        });
                    }
                }
                else
                {
                    rpt.vg1.Add(new COMMAND_CLASS_INDICATOR_V2.INDICATOR_REPORT.TVG1()
                    {
                        indicatorId = 0x00,
                        propertyId = 0x00,
                        value = 0x00,
                    });
                }
                if (IndicatorSupportData.Indicators.Count != 0)
                    rpt.indicator0Value = IndicatorSupportData.Indicators[0].Properties[0].PropertyValue;
                else
                    rpt.indicator0Value = 0x00;
                rpt.properties1.indicatorObjectCount = (byte) rpt.vg1.Count;
                return rpt;
            }
            return null;
        }

        private SvResultNames HandleIndicatorSet(byte[] command)
        {
            if (Version == 1)
            {
                byte propertyValue = ((COMMAND_CLASS_INDICATOR.INDICATOR_SET) command).value;
                if (IndicatorSupportData.Indicators.Count != 0)
                {
                    if (IndicatorSupportData.ValidatePropertyValue(IndicatorSupportData.Indicators[0].Properties[0].PropertyId, propertyValue))
                    {
                        IndicatorSupportData.Indicators[0].Properties[0].PropertyValue = propertyValue;
                        return SvResultNames.Successful;
                    }
                }
                return SvResultNames.Failed;
            }
            if (Version >= 2)
            {
                COMMAND_CLASS_INDICATOR_V2.INDICATOR_SET setCmd = command;
                if (setCmd.properties1.indicatorObjectCount == 0 /*|| setCmd.properties1.indicatorObjectCount omitted*/)
                {
                    IndicatorSupportData.Indicators[0].Properties[0].PropertyValue = setCmd.indicator0Value;
                    return SvResultNames.Successful;
                }
                SvResultNames success = SvResultNames.Successful;
                List<byte> handledIndicatorIds = new List<byte> { };
                foreach (COMMAND_CLASS_INDICATOR_V2.INDICATOR_SET.TVG1 vg1 in setCmd.vg1)
                {
                    // Properties marked as "ADVERTISE ONLY:" MUST be ignored if received in a controlling command.
                    if (vg1.propertyId == (byte) IndicatorPropertyNames.AdvertiseSleep)
                    {
                        success = SvResultNames.Failed;
                        continue;
                    }
                    if (!IndicatorSupportData.ValidatePropertyValue(vg1.propertyId, vg1.value))
                    {
                        success = SvResultNames.Failed;
                        continue;
                    }
                    Indicator indObj = IndicatorSupportData.Indicators.Find(indicator => indicator.Id == vg1.indicatorId);
                    if (indObj != null)
                    {
                        // If an indicator ID is specified in an object but not all Property IDs are included in the command,
                        // a supporting node MUST assume non-specified Property IDs values to be set to 0x00.
                        if (!handledIndicatorIds.Contains(indObj.Id))
                        {
                            foreach (IndicatorProperty prop in indObj.Properties)
                            {
                                prop.PropertyValue = 0x00;
                            }
                            handledIndicatorIds.Add(indObj.Id);
                        }

                        // ToDo:
                        // If several Property IDs not belonging to the same Property group defined in SDS14220 are specified in the
                        // command, the Property IDs from one Property group MUST be applied and all other Property IDs MUST
                        // be ignored. For example, if Multilevel and Binary property IDs are defined, only one Property ID value
                        // MUST be applied and the other MUST be ignored.

                        IndicatorProperty propObj = indObj.Properties.Find(property => property.PropertyId == vg1.propertyId);
                        if (propObj != null)
                        {
                            propObj.PropertyValue = vg1.value;
                        }
                        else
                        {
                            // A receiving node MUST ignore the entire indicator object if an unsupported Property ID for the corresponding 
                            // Indicator ID is specified. The receiving node MUST NOT ignore other indicator objects included in the command.
                            success = SvResultNames.Failed;
                        }
                    }
                    else
                    {
                        success = SvResultNames.Failed;
                    }
                }
                return success;
            }
            return SvResultNames.Failed;
        }

        private byte GetNextIndicatorId(byte currentIndicatorId)
        {
            // Requires sorted List<Indicator> IndicatorSupportData.Indicators (is done in AddIndicator methods)
            byte nextIndicatorId = 0;
            foreach (Indicator ind in IndicatorSupportData.Indicators)
            {
                if (ind.Id <= currentIndicatorId)
                    continue;
                nextIndicatorId = ind.Id;
                break;
            }
            return nextIndicatorId;
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
                case COMMAND_CLASS_INDICATOR.INDICATOR_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }

        // Satisfy abstract members
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return null; }
            set {; }
        }

        public override void UpdateSupportData(CommandClassSupportData data)
        {

        }
    }
}
