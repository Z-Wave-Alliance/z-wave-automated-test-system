// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
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
    public class IndicatorSupport : CommandClassSupport
    {
        private IndicatorSupportData IndicatorSupportData { get; set; }

        //private readonly string CommandClassName = "Indicator CC";
        //private string EndPointName = "";
        //private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public IndicatorSupport()
            : this(new IndicatorSupportData() { })
        { }

        public IndicatorSupport(IndicatorSupportData indicatorSupportData)
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
            if (DstNode.EndPointId == ReceivedAchData.DstNode.EndPointId)
            {
                var sendDstNode = ReceivedAchData.DstNode;
                var sendSrcNode = ReceivedAchData.SrcNode;
                byte[] command = ReceivedAchData.Command;
                var scheme = (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme;
                bool isResponseAllowed = IsResponseAllowed(Network, command, scheme, Secure);

                ResultForSv = SvResultNames.Undefined;
                if (isResponseAllowed && command != null && command.Length > 1 && IndicatorSupportData != null)
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

                    CommandCallback(command);
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
    }

    public class IndicatorProperty
    {
        public byte PropertyId;
        public byte PropertyValue;

        public IndicatorProperty(byte propertyId, byte propertyValue)
        {
            PropertyId = propertyId;
            PropertyValue = propertyValue;
        }
    }

    public class Indicator
    {
        public byte Id;
        public List<IndicatorProperty> Properties = new List<IndicatorProperty> { };
        public List<byte> Description = new List<byte> { };

        public Indicator(byte id, byte property, byte value, List<byte> description)
        {
            Id = id;
            Properties.Add(new IndicatorProperty(property, value));
            if (description.Count != 0      // avoid resetting an existing description if adding multiple properties
             && id >= (byte) IndicatorIdNames.ManufacturerDefinedIndicator1
             && id <= (byte) IndicatorIdNames.ManufacturerDefinedIndicator32)
            {
                Description = description;
            }
        }
    }

    public class IndicatorSupportData
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        public List<Indicator> Indicators { get; set; } = new List<Indicator>();

        public Indicator AddIndicator(IndicatorIdNames indicatorId, IndicatorPropertyNames propertyId, byte propertyValue, string description)
        {
            List<byte> lStr = Encoding.UTF8.GetBytes(description).ToArray().ToList();
            return AddIndicator((byte) indicatorId, (byte) propertyId, propertyValue, lStr);
        }
        public Indicator AddIndicator(IndicatorIdNames indicatorId, IndicatorPropertyNames propertyId, byte propertyValue)
        {
            return AddIndicator((byte) indicatorId, (byte) propertyId, propertyValue, new List<byte> { });
        }
        public Indicator AddIndicator(byte indicatorId, byte propertyId, byte propertyValue)
        {
            return AddIndicator(indicatorId, propertyId, propertyValue, new List<byte> { });
        }

        public Indicator AddIndicator(byte indicatorId, byte propertyId, byte propertyValue, List<byte> description)
        {
            if (!ValidateIndicatorId(indicatorId))
                return null;
            if (!ValidatePropertyId(propertyId))
                return null;
            if (!ValidatePropertyValue(propertyId, propertyValue))
                return null;

            Indicator indObj = Indicators.Find(indicator => indicator.Id == indicatorId);
            if (indObj != null)
            {
                // Add new Indicator property, if not already added. no more than 31 properties (5 bits) are allowed.
                if (indObj.Properties.Count >= 31
                 || indObj.Properties.Find(prop => prop.PropertyId == propertyId) != null)
                {
                    return null;
                }
                indObj.Properties.Add(new IndicatorProperty(propertyId, propertyValue));
            }
            // Create new Indicator object, if not already added
            else
            {
                indObj = new Indicator(indicatorId, propertyId, propertyValue, description);
                Indicators.Add(indObj);
            }
            // Sort Indicator array for Supported Report
            IEnumerable<Indicator> orderQuery1 = Indicators.OrderBy(indId => indId.Id);
            Indicators = orderQuery1.ToList();
            foreach (Indicator ind in Indicators)
            {
                IEnumerable<IndicatorProperty> orderQuery2 = ind.Properties.OrderBy(property => property.PropertyId);
                ind.Properties = orderQuery2.ToList();
            }
            return indObj;
        }
        private bool ValidateIndicatorId(byte indicatorId)
        {
            if (Enum.IsDefined(typeof(IndicatorIdNames), indicatorId))
                return true;
            if (indicatorId >= (byte)IndicatorIdNames.GenericEventSoundNotification1
             && indicatorId <= (byte)IndicatorIdNames.GenericEventSoundNotification32)
                return true;
            if (indicatorId >= (byte)IndicatorIdNames.ManufacturerDefinedIndicator1
             && indicatorId <= (byte)IndicatorIdNames.ManufacturerDefinedIndicator32)
                return true;
            return false;
        }
        private bool ValidatePropertyId(byte propertyId)
        {
            if (propertyId == (byte) IndicatorPropertyNames.AdvertiseSleep)
                return false;
            if (Enum.IsDefined(typeof(IndicatorPropertyNames), propertyId))
                return true;
            return false;
        }
        /// <summary>
        /// Validation of Property Values in Indicator Set against SDS14220
        /// </summary>
        /// <param name="propertyId"></param>
        /// <param name="propertyValue"></param>
        /// <returns>true: valid; false: invalid</returns>
        public bool ValidatePropertyValue(byte propertyId, byte propertyValue)
        {
            if (propertyId == 0x01
             || propertyId == 0x02)
            {
                return (propertyValue <= 0x63 || propertyValue == 0xFF);
            }
            if (propertyId == 0x03
             || propertyId == 0x04
             || propertyId == 0x05
             || propertyId == 0x06)
            {
                return true;
            }
            if (propertyId == 0x07)
            {
                return (propertyValue <= 0x3B);
            }
            if (propertyId == 0x08)
            {
                return (propertyValue <= 0x63);
            }
            if (propertyId == 0x09)
            {
                return (propertyValue <= 0x64 || propertyValue == 0xFF);
            }
            return false;
        }


    }

        public enum IndicatorIdNames : byte
    {
        Armed = 0x01,                       // V2+
        NotArmed = 0x02,                    // V2+
        Ready = 0x03,                       // V2+
        Fault = 0x04,                       // V2+
        Busy = 0x05,                        // V2+
        EnterId = 0x06,                     // V2+
        EnterPin = 0x07,                    // V2+
        CodeAccepted = 0x08,                // V2+
        CodeNotAccepted = 0x09,             // V2+
        ArmedStay = 0x0A,                   // V3+
        ArmedAway = 0x0B,                   // V3+
        Alarming = 0x0C,                    // V3+
        AlarmingBurglar = 0x0D,             // V3+
        AlarmingSmokeFire = 0x0E,           // V3+
        AlarmingCarbonMonoxide = 0x0F,      // V3+
        BypassChallenge = 0x10,             // V3+
        EntryDelay = 0x11,                  // V3+
        ExitDelay = 0x12,                   // V3+
        AlarmingMedical = 0x13,             // V3+
        AlarmingFreezeWarning = 0x14,       // V3+
        AlarmingWaterLeak = 0x15,           // V3+
        AlarmingPanic = 0x16,               // V3+

        Zone1Armed = 0x20,                  // V2+
        Zone2Armed = 0x21,                  // V2+
        Zone3Armed = 0x22,                  // V2+
        Zone4Armed = 0x23,                  // V2+
        Zone5Armed = 0x24,                  // V2+
        Zone6Armed = 0x25,                  // V2+
        Zone7Armed = 0x26,                  // V3+
        Zone8Armed = 0x27,                  // V3+

        LcdBacklight = 0x30,                // V2+

        ButtonBacklightLetters = 0x40,      // V2+
        ButtonBacklightDigits  = 0x41,      // V2+
        ButtonBacklightCommand = 0x42,      // V2+
        Button1Indication  = 0x43,          // V2+
        Button2Indication  = 0x44,          // V2+
        Button3Indication  = 0x45,          // V2+
        Button4Indication  = 0x46,          // V2+
        Button5Indication  = 0x47,          // V2+
        Button6Indication  = 0x48,          // V2+
        Button7Indication  = 0x49,          // V2+
        Button8Indication  = 0x4A,          // V2+
        Button9Indication  = 0x4B,          // V2+
        Button10Indication = 0x4C,          // V2+
        Button11Indication = 0x4D,          // V2+
        Button12Indication = 0x4E,          // V2+

        NodeIdentify = 0x50,                // V3+

        GenericEventSoundNotification1  = 0x60,
        GenericEventSoundNotification2  = 0x61,
        GenericEventSoundNotification3  = 0x62,
        GenericEventSoundNotification4  = 0x63,
        GenericEventSoundNotification32 = 0x7F,
        ManufacturerDefinedIndicator1   = 0x80,
        ManufacturerDefinedIndicator2   = 0x81,
        ManufacturerDefinedIndicator3   = 0x82,
        ManufacturerDefinedIndicator4   = 0x83,
        ManufacturerDefinedIndicator32  = 0x9F,

        Buzzer = 0xF0,
    }

    public enum IndicatorPropertyNames : byte
    {
        SpecificLevel = 0x01,               // V2+
        OnOff = 0x02,                       // V2+
        OnOffPeriods = 0x03,                // V2+
        OnOffCycles = 0x04,                 // V2+
        OnTime = 0x05,                      // V3+
        TimeoutMin = 0x06,                  // V3+
        TimeoutSec = 0x07,                  // V3+
        TimeoutHSec = 0x08,                 // V3+
        Volume = 0x09,                      // V3+

        AdvertiseSleep = 0x10,              // V2+
    }
}
