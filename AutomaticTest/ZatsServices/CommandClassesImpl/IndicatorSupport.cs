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
using ZWave.Xml.Application;
using MQTTnet.Protocol;
using static Zats.Services.AssociationGroupEx;
using static UicApplication.Clusters.SmartStart;
using System.Reflection;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;
using Newtonsoft.Json;

// ToDo: Supervision Report Status 'Working' ?
// ToDo: Validation of Indicator IDs and Property IDs according to CC version in AddIndicator()
// ToDo: Validation of Property ID groups in Indicator Set
// ToDo: Realtime support for Toggling and Timeout Property IDs
// ToDo: Logging ?
// ToDo (copy from below):
// If several Property IDs not belonging to the same Property group defined in SDS14220 are specified in the
// command, the Property IDs from one Property group MUST be applied and all other Property IDs MUST
// be ignored. For example, if Multilevel and Binary property IDs are defined, only one Property ID value
// MUST be applied and the other MUST be ignored.

namespace Zats.Services
{
    [CommandClassName("Indicator Command Class")]
    [CommandClassId(COMMAND_CLASS_INDICATOR.ID)]
    [MinVersion(1)]
    [MaxVersion(4)]
    public class IndicatorSupport : CommandClassSupport
    {
        private IndicatorSupportData IndicatorSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return IndicatorSupportData; }
            set { IndicatorSupportData = (IndicatorSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public IndicatorSupport()
            : this(new IndicatorSupportData() { })
        { }

        public IndicatorSupport(IndicatorSupportData indicatorSupportData)
            : base(COMMAND_CLASS_INDICATOR.ID)
        {
            IndicatorSupportData = indicatorSupportData;
        }

        public override void ResetEmulation()
        {
            foreach (Indicator indicator in IndicatorSupportData.Indicators)
            {
                foreach(IndicatorProperty indicatorProperty in indicator.Properties)
                {
                    indicatorProperty.PropertyValue = 0;
                }
            }
            IndicatorSupportData.Indicator0Value = 0;
            SupportDelegates?.DeviceDataUpdate?.Invoke(this);
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
                        rpt = PrepareIndicatorReportCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_INDICATOR_V2.INDICATOR_SUPPORTED_GET.ID)
                    {
                        rpt = PrepareSupportedReportCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_INDICATOR_V4.INDICATOR_DESCRIPTION_GET.ID)
                    {
                        rpt = PrepareDescriptionReportCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_INDICATOR.INDICATOR_SET.ID)
                    {
                        ResultForSv = HandleIndicatorSetCmd(command, sendSrcNode);
                    }

                    if (rpt != null && IsAnsweringAllowed())
                    {
                        ApiOperation sendData = new SendDataExOperation(Network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command, sendDstNode.EndPointId);
                }
            }
        }

        private byte[] PrepareSupportedReport(byte requestedIndicatorId)
        {
            if (Version >= 2)
            {
                COMMAND_CLASS_INDICATOR_V2.INDICATOR_SUPPORTED_REPORT rpt = new COMMAND_CLASS_INDICATOR_V2.INDICATOR_SUPPORTED_REPORT();

                var orderedIndicators = IndicatorSupportData.Indicators.OrderBy(x => x.Id).ToList();

                // A supporting node receiving this field set to 0x00 MUST advertise the first supported Indicator ID in response.
                if (requestedIndicatorId == 0 && IndicatorSupportData.Indicators.Count > 0)
                {
                    requestedIndicatorId = orderedIndicators.First().Id;

                }

                Indicator indObj = IndicatorSupportData.Indicators.Find(indicator => indicator.Id == requestedIndicatorId);
                if (indObj != null)
                {
                    List<byte> propList = new List<byte>();
                    foreach (IndicatorProperty prop in indObj.Properties)
                    {
                        propList.Add(prop.PropertyId);
                    }
                    rpt.indicatorId = requestedIndicatorId;
                    rpt.nextIndicatorId = orderedIndicators.FirstOrDefault(x => x.Id > requestedIndicatorId)?.Id ?? 0;
                    rpt.propertySupportedBitMask = CommandClassSupportUtils.CreateBitmaskFromList(propList, 0);
                    rpt.properties1.propertySupportedBitMaskLength = (byte) rpt.propertySupportedBitMask.Count;
                }
                else
                {
                    rpt.indicatorId = 0;
                    rpt.nextIndicatorId = 0;
                    rpt.propertySupportedBitMask = new List<byte>();
                    rpt.properties1.propertySupportedBitMaskLength = (byte) rpt.propertySupportedBitMask.Count;
                }
                return rpt;
            }
            return null;
        }
        private byte[] PrepareSupportedReportCmd(byte[] command)
        {
            if (Version >= 2)
            {
                byte requestedIndicatorId = ((COMMAND_CLASS_INDICATOR_V2.INDICATOR_SUPPORTED_GET)command).indicatorId;
                return PrepareSupportedReport(requestedIndicatorId);
            }
            return null;
        }

        private byte[] PrepareDescriptionReport(byte requestedIndicatorId)
        {
            if (Version == 4)
            {
                COMMAND_CLASS_INDICATOR_V4.INDICATOR_DESCRIPTION_REPORT rpt = new COMMAND_CLASS_INDICATOR_V4.INDICATOR_DESCRIPTION_REPORT();

                rpt.description = new List<byte>();
                if (requestedIndicatorId >= (byte)IndicatorIdNames.ManufacturerDefinedIndicator1
                 && requestedIndicatorId <= (byte)IndicatorIdNames.ManufacturerDefinedIndicator32)
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
        private byte[] PrepareDescriptionReportCmd(byte[] command)
        {
            if (Version == 4)
            {
                byte requestedIndicatorId = ((COMMAND_CLASS_INDICATOR_V4.INDICATOR_SUPPORTED_GET)command).indicatorId;
                return PrepareDescriptionReport(requestedIndicatorId);
            }
            return null;
        }

        private byte[] PrepareIndicatorReport(byte requestedIndicatorId)
        {
            if (Version == 1)
            {
                COMMAND_CLASS_INDICATOR.INDICATOR_REPORT rpt = new COMMAND_CLASS_INDICATOR.INDICATOR_REPORT();
                rpt.value = IndicatorSupportData.Indicator0Value;
                /*if (IndicatorSupportData.Indicators.Count > 0)
                {
                    if (IndicatorSupportData.Indicators[0].Properties.Count > 0)
                    {
                        rpt.value = IndicatorSupportData.Indicators[0].Properties[0].PropertyValue;
                        return rpt;
                    }
                }*/
            }
            if (Version >= 2)
            {
                COMMAND_CLASS_INDICATOR_V2.INDICATOR_REPORT rpt = new COMMAND_CLASS_INDICATOR_V2.INDICATOR_REPORT();

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
                rpt.indicator0Value = IndicatorSupportData.Indicator0Value; // default/fallback
                if (IndicatorSupportData.Indicators.Count > 0)              // map Indicator0 to first Indicator object
                {
                    if (IndicatorSupportData.Indicators[0].Properties.Count > 0)
                    {
                        rpt.indicator0Value = IndicatorSupportData.Indicators[0].Properties[0].PropertyValue;
                    }
                }
                rpt.properties1.indicatorObjectCount = (byte) rpt.vg1.Count;
                return rpt;
            }
            return null;
        }
        private byte[] PrepareIndicatorReportCmd(byte[] command)
        {
            if (Version == 1)
            {
                return PrepareIndicatorReport(0);
            }
            if (Version >= 2)
            {
                byte requestedIndicatorId = ((COMMAND_CLASS_INDICATOR_V2.INDICATOR_GET)command).indicatorId;
                return PrepareIndicatorReport(requestedIndicatorId);
            }
            return null;
        }

        private SvResultNames HandleIndicatorSet(byte indicator0Value, List<Indicator> indicatorsToSet, bool updateUI, NodeTag srcNode)
        {
            if (Version == 1)
            {
                if (IndicatorSupportData.Indicators.Count > 0)
                {
                    if (IndicatorSupportData.Indicators[0].Properties.Count > 0)
                    {
                        if (IndicatorSupportData.ValidatePropertyValue(IndicatorSupportData.Indicators[0].Properties[0].PropertyId, indicator0Value))
                        {
                            if (IndicatorSupportData.Indicators[0].Properties[0].PropertyValue != indicator0Value)
                            {
                                IndicatorSupportData.Indicators[0].Properties[0].PropertyValue = indicator0Value;
                                if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                                {
                                    SendUnsolicitedReport(PrepareIndicatorReport(IndicatorSupportData.Indicators[0].Id), srcNode);
                                }
                                if (updateUI)
                                {
                                    SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about the current state
                                }
                            }
                            return SvResultNames.Successful;
                        }
                    }
                }
                return SvResultNames.Failed;
            }
            if (Version >= 2)
            {
                if (indicatorsToSet.Count == 0)
                {
                    if (IndicatorSupportData.Indicators.Count > 0)
                    {
                        if (IndicatorSupportData.Indicators[0].Properties.Count > 0)
                        {
                            IndicatorSupportData.Indicators[0].Properties[0].PropertyValue = indicator0Value;
                            return SvResultNames.Successful;
                        }
                    }
                    return SvResultNames.Failed;
                }

                SvResultNames success = SvResultNames.Successful;
                List<byte> handledIndicatorIds = new List<byte>();
                foreach (Indicator indToSet in indicatorsToSet)
                {
                    //bool isAnyPropertyValueChanged = false;
                    foreach (IndicatorProperty propToSet in indToSet.Properties)
                    {
                        // Properties marked as "ADVERTISE ONLY:" MUST be ignored if received in a controlling command.
                        if (propToSet.PropertyId == (byte) IndicatorPropertyNames.AdvertiseSleep)
                        {
                            success = SvResultNames.Failed;
                            continue;
                        }
                        if (!IndicatorSupportData.ValidatePropertyValue(propToSet.PropertyId, propToSet.PropertyValue))
                        {
                            success = SvResultNames.Failed;
                            continue;
                        }

                        Indicator indicatorObject = IndicatorSupportData.Indicators.Find(indObj => indObj.Id == indToSet.Id);
                        if (indicatorObject != null)
                        {
                            // Requirement: If an indicator ID is specified in an object but not all Property IDs are included
                            // in the command, a supporting node MUST assume non-specified Property IDs values to be set to 0x00.
                            // Currently ALL Property ID values if the Indicator are reset to 0x00 initially to fulfill this requirement.
                            // CAUTION: This does not affect isAnyPropertyValueChanged and the decision to send an Indicator Report!
                            if (!handledIndicatorIds.Contains(indicatorObject.Id))
                            {
                                indicatorObject.Properties.ForEach(prop => prop.PropertyValue = 0x00);
                                handledIndicatorIds.Add(indicatorObject.Id);
                            }

                            // ToDo:
                            // If several Property IDs not belonging to the same Property group defined in SDS14220 are specified in the
                            // command, the Property IDs from one Property group MUST be applied and all other Property IDs MUST
                            // be ignored. For example, if Multilevel and Binary property IDs are defined, only one Property ID value
                            // MUST be applied and the other MUST be ignored.
                            IndicatorProperty propObj = indicatorObject.Properties.Find(property => property.PropertyId == propToSet.PropertyId);
                            if (propObj != null)
                            {
                                if (propObj.PropertyValue != propToSet.PropertyValue)
                                {
                                    propObj.PropertyValue = propToSet.PropertyValue;
                                    //isAnyPropertyValueChanged = true;
                                }
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
                    //if (isAnyPropertyValueChanged)    // The Sample App (Jan 2023) sends the Lifeline Indicator Report after each Indicator Set Command
                    {
                        if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                        {
                            SendUnsolicitedReport(PrepareIndicatorReport(indToSet.Id), srcNode);
                        }
                        if (updateUI)
                        {
                            SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about the current state
                        }
                    }
                }
                return success;
            }
            return SvResultNames.Failed;
        }
        private SvResultNames HandleIndicatorSetCmd(byte[] command, NodeTag srcNode)
        {
            if (Version == 1)
            {
                byte propertyValue = ((COMMAND_CLASS_INDICATOR.INDICATOR_SET)command).value;
                return HandleIndicatorSet(propertyValue, null, true, srcNode);
            }
            if (Version >= 2)
            {
                List<Indicator> indicatorsToSet = new List<Indicator>();
                COMMAND_CLASS_INDICATOR_V2.INDICATOR_SET setCmd = command;
                if (setCmd.properties1.indicatorObjectCount == 0 /*|| setCmd.properties1.indicatorObjectCount omitted*/)
                {
                    return HandleIndicatorSet(setCmd.indicator0Value, indicatorsToSet, true, srcNode);
                }

                List<COMMAND_CLASS_INDICATOR_V2.INDICATOR_SET.TVG1> indicatorList = setCmd.vg1;
                foreach (COMMAND_CLASS_INDICATOR_V2.INDICATOR_SET.TVG1 indicator in indicatorList)
                {
                    List<IndicatorProperty> propertyValueList = new List<IndicatorProperty>
                    {
                        new IndicatorProperty(indicator.propertyId, indicator.value)
                    };
                    indicatorsToSet.Add(new Indicator(indicator.indicatorId, propertyValueList, string.Empty));
                }

                return HandleIndicatorSet(0, indicatorsToSet, true, srcNode);
            }
            return SvResultNames.Failed;
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
                case COMMAND_CLASS_INDICATOR.INDICATOR_SET.ID:
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
            if (data is IndicatorSupportData newData)
            {
                if (IndicatorSupportData.Indicator0Value != newData.Indicator0Value)
                {   // something more to do?
                    IndicatorSupportData.Indicator0Value = newData.Indicator0Value;
                }

                List<Indicator> indRemoved = IndicatorSupportData.Indicators.FindAll(x => newData.Indicators.Find(y => y.Id == x.Id) == null);
              //List<Indicator> indAdded   = newData.Indicators.FindAll(x => IndicatorSupportData.Indicators.Find(y => y.Id == x.Id) == null);
                if (indRemoved.Count > 0 /*|| indAdded.Count > 0*/)
                {
                    indRemoved.ForEach(x => IndicatorSupportData.Indicators.Remove(x));
                }

                List<byte[]> indicatorReports = new List<byte[]>();
                foreach (Indicator newInd in newData.Indicators)
                {
                    bool isValueChanged = false;        // Indicator Report V1+ (per Indicator)
                    bool isPropertyChanged = false;     // Supported Report V2+ (per Indicator)
                    bool isDescriptionChanged = false;  // Description Report V3+ (per Indicator)
                    if (IndicatorSupportData.Indicators.Find(x => x.Id == newInd.Id) is Indicator currentInd)
                    {
                        if (!currentInd.Description.SequenceEqual(newInd.Description))
                        {
                            isDescriptionChanged = true;
                        }
                        // It is an existing Indicator, check all components for changes
                        List<IndicatorProperty> propRemoved = currentInd.Properties.FindAll(x => newInd.Properties.Find(y => y.PropertyId == x.PropertyId) == null);
                      //List<IndicatorProperty> propAdded   = newInd.Properties.FindAll(x => currentInd.Properties.Find(y => y.PropertyId == x.PropertyId) == null);
                        if (propRemoved.Count > 0 /*|| propAdded.Count > 0*/)
                        {
                            isPropertyChanged = true;
                        }
                        else
                        {
                            foreach (IndicatorProperty newProp in newInd.Properties)
                            {
                                if (currentInd.Properties.Find(x => x.PropertyId == newProp.PropertyId) is IndicatorProperty currentProp)
                                {
                                    if (currentProp.PropertyValue != newProp.PropertyValue)
                                    {
                                        isValueChanged = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    isPropertyChanged = true;
                                    break;
                                }
                            }
                        }
                        if (isValueChanged || isPropertyChanged || isDescriptionChanged )
                        {
                            IndicatorSupportData.Indicators.Remove(currentInd);
                            IndicatorSupportData.Indicators.Add(newInd);
                        }
                    }
                    else // It is a new Indicator, add it
                    {
                        isValueChanged = true;
                        isPropertyChanged = true;
                        isDescriptionChanged = true;
                        IndicatorSupportData.Indicators.Add(newInd);
                    }
                    // Parsing done. Now prepare several Indicator Reports...
                    if (isValueChanged && ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        indicatorReports.Add(PrepareIndicatorReport(newInd.Id));
                    }
                    if (isPropertyChanged && ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        indicatorReports.Add(PrepareSupportedReport(newInd.Id));
                    }
                    if (isDescriptionChanged && ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        indicatorReports.Add(PrepareDescriptionReport(newInd.Id));
                    }
                }

                // Send all prepared reports
                foreach (byte[] rpt in indicatorReports)
                {
                    SendUnsolicitedReport(rpt, NodeTag.Empty);
                }

                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class IndicatorProperty : INestedItem
    {
        public byte PropertyId;
        [CustomDataParameter(2, "Value", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Hex8, EditType.Config | EditType.Edit)]
        public byte PropertyValue { get; set; }

        public IndicatorProperty(byte propertyId, byte propertyValue)
        {
            PropertyId = propertyId;
            PropertyValue = propertyValue;
        }

        // only used for EDE UI. see interface for useage notes.
        [JsonIgnore]
        public int NestedId
        {
            get => PropertyId;
            set { }
        }

        [JsonIgnore]
        public IndicatorPropertyNames IdName { get { return (IndicatorPropertyNames)PropertyId; } set { PropertyId = (byte)value; } }
    }

    public class Indicator : INestedItem
    {
        public byte Id;
        public List<IndicatorProperty> Properties = new List<IndicatorProperty>();
        public List<byte> Description = new List<byte>();
        [JsonIgnore]
        public IndicatorIdNames IdName
        {
            get => (IndicatorIdNames)Id;
            set { Id = (byte)value; }
        }
        [JsonIgnore]
        [CustomDataParameter(4, "Description", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Text, EditType.Config)]
        public string DescriptionString
        {
            get { return Encoding.UTF8.GetString(Description.ToArray()); }
            set { Description = Encoding.UTF8.GetBytes(value).ToList(); }
        }
        public Indicator(byte id, List<IndicatorProperty> indicatorProperties, string description)
        {
            Id = id;
            Properties.AddRange(indicatorProperties);
            DescriptionString = description;
        }

        public Indicator() { }

        // only used for EDE UI. see interface for useage notes.
        [JsonIgnore]
        public int NestedId
        {
            get => Id;
            set { }
        }
    }

    public class IndicatorSupportData : CommandClassSupportData, INodeItem
    {
        [CustomDataParameter(2, "Indicator0Value", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Hex8, EditType.Config | EditType.Edit)]
        public byte Indicator0Value { get; set; } = 0x00;

        // Internals accessible from TestCases or EDE
        [NestedControl(2, "Indicators")]
        public List<Indicator> Indicators { get; set; } = new List<Indicator>();

        [JsonIgnore]
        public IEnumerable<INestedItem> ChildItems => Indicators;

        // Helper methods for TestCases
        #region Helper methods
        public Indicator AddIndicator(IndicatorIdNames indicatorId, IndicatorPropertyNames propertyId, byte propertyValue, string description)
        {
            List<byte> lStr = Encoding.UTF8.GetBytes(description).ToArray().ToList();
            return AddIndicator((byte) indicatorId, (byte) propertyId, propertyValue, lStr);
        }
        public Indicator AddIndicator(IndicatorIdNames indicatorId, IndicatorPropertyNames propertyId, byte propertyValue)
        {
            return AddIndicator((byte) indicatorId, (byte) propertyId, propertyValue, new List<byte>());
        }
        public Indicator AddIndicator(byte indicatorId, byte propertyId, byte propertyValue)
        {
            return AddIndicator(indicatorId, propertyId, propertyValue, new List<byte>());
        }
        public Indicator AddIndicator(byte indicatorId, byte propertyId, byte propertyValue, List<byte> description)
        {
            if (!ValidateIndicatorId(indicatorId))
            {
                return null;
            }
            if (!ValidatePropertyId(propertyId))
            {
                return null;
            }
            if (!ValidatePropertyValue(propertyId, propertyValue))
            {
                return null;
            }

            Indicator indObj = Indicators.Find(indicator => indicator.Id == indicatorId);
            if (indObj != null)
            {
                // Add new Indicator property, if not already added. No more than 31 properties (5 bits) are allowed.
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
                indObj = new Indicator()
                {
                    Id = indicatorId,
                    Properties = new List<IndicatorProperty>() { new IndicatorProperty(propertyId, propertyValue) },
                    Description = description
                };
                Indicators.Add(indObj);
            }
            return indObj;
        }
        private bool ValidateIndicatorId(byte indicatorId)
        {
            if (Enum.IsDefined(typeof(IndicatorIdNames), indicatorId))
            {
                return true;
            }
            if (indicatorId >= (byte)IndicatorIdNames.GenericEventSoundNotification1
             && indicatorId <= (byte)IndicatorIdNames.GenericEventSoundNotification32)
            {
                return true;
            }
            if (indicatorId >= (byte)IndicatorIdNames.ManufacturerDefinedIndicator1
             && indicatorId <= (byte)IndicatorIdNames.ManufacturerDefinedIndicator32)
            {
                return true;
            }
            return false;
        }
        private bool ValidatePropertyId(byte propertyId)
        {
            if (propertyId == (byte)IndicatorPropertyNames.AdvertiseSleep)
            {
                return false;
            }
            if (Enum.IsDefined(typeof(IndicatorPropertyNames), propertyId))
            {
                return true;
            }
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
        #endregion // Helper methods
    }

    #region Indicator enums
    public enum IndicatorIdNames : byte
    {
        [Description("0x01 Armed")]
        Armed = 0x01,                       // V2+
        [Description("0x02 Not Armed")]
        NotArmed = 0x02,                    // V2+
        [Description("0x03 Ready")]
        Ready = 0x03,                       // V2+
        [Description("0x04 Fault")]
        Fault = 0x04,                       // V2+
        [Description("0x05 Busy")]
        Busy = 0x05,                        // V2+
        [Description("0x06 Enter ID")]
        EnterId = 0x06,                     // V2+
        [Description("0x07 Enter PIN")]
        EnterPin = 0x07,                    // V2+
        [Description("0x08 Code Accepted")]
        CodeAccepted = 0x08,                // V2+
        [Description("0x09 Code not Accepted")]
        CodeNotAccepted = 0x09,             // V2+
        [Description("0x0A Armed Stay")]
        ArmedStay = 0x0A,                   // V3+
        [Description("0x0 Armed Away")]
        ArmedAway = 0x0B,                   // V3+
        [Description("0x0C Alarming")]
        Alarming = 0x0C,                    // V3+
        [Description("0x0D Alarming Burglar")]
        AlarmingBurglar = 0x0D,             // V3+
        [Description("0x0E Alarming Smoke Fire")]
        AlarmingSmokeFire = 0x0E,           // V3+
        [Description("0x0F Alarming Carbon Monoxide")]
        AlarmingCarbonMonoxide = 0x0F,      // V3+
        [Description("0x10 Bypass Challenge")]
        BypassChallenge = 0x10,             // V3+
        [Description("0x11 Entry Delay")]
        EntryDelay = 0x11,                  // V3+
        [Description("0x12 Exit Delay")]
        ExitDelay = 0x12,                   // V3+
        [Description("0x13 Alarming Medical")]
        AlarmingMedical = 0x13,             // V3+
        [Description("0x14 Alarming Freeze Warning")]
        AlarmingFreezeWarning = 0x14,       // V3+
        [Description("0x15 Alarming Water Leak")]
        AlarmingWaterLeak = 0x15,           // V3+
        [Description("0x16 Alarming Panic")]
        AlarmingPanic = 0x16,               // V3+

        [Description("0x20 Zone 1 Armed")]
        Zone1Armed = 0x20,                  // V2+
        [Description("0x21 Zone 2 Armed")]
        Zone2Armed = 0x21,                  // V2+
        [Description("0x22 Zone 3 Armed")]
        Zone3Armed = 0x22,                  // V2+
        [Description("0x23 Zone 4 Armed")]
        Zone4Armed = 0x23,                  // V2+
        [Description("0x24 Zone 5 Armed")]
        Zone5Armed = 0x24,                  // V2+
        [Description("0x25 Zone 6 Armed")]
        Zone6Armed = 0x25,                  // V2+
        [Description("0x26 Zone 7 Armed")]
        Zone7Armed = 0x26,                  // V3+
        [Description("0x27 Zone 8 Armed")]
        Zone8Armed = 0x27,                  // V3+

        [Description("0x30 LCD Backlight")]
        LcdBacklight = 0x30,                // V2+

        [Description("0x40 Button Backlight Letters")]
        ButtonBacklightLetters = 0x40,      // V2+
        [Description("0x41 Button Backlight Digits")]
        ButtonBacklightDigits  = 0x41,      // V2+
        [Description("0x42 Button Backlight Command")]
        ButtonBacklightCommand = 0x42,      // V2+
        [Description("0x43 Button 1 Indication")]
        Button1Indication  = 0x43,          // V2+
        [Description("0x44 Button 2 Indication")]
        Button2Indication  = 0x44,          // V2+
        [Description("0x45 Button 3 Indication")]
        Button3Indication  = 0x45,          // V2+
        [Description("0x46 Button 4 Indication")]
        Button4Indication  = 0x46,          // V2+
        [Description("0x47 Button 5 Indication")]
        Button5Indication  = 0x47,          // V2+
        [Description("0x48 Button 6 Indication")]
        Button6Indication  = 0x48,          // V2+
        [Description("0x49 Button 7 Indication")]
        Button7Indication  = 0x49,          // V2+
        [Description("0x4A Button 8 Indication")]
        Button8Indication  = 0x4A,          // V2+
        [Description("0x4B Button 9 Indication")]
        Button9Indication  = 0x4B,          // V2+
        [Description("0x4C Button 10 Indication")]
        Button10Indication = 0x4C,          // V2+
        [Description("0x4D Button 11 Indication")]
        Button11Indication = 0x4D,          // V2+
        [Description("0x4E Button 12 Indication")]
        Button12Indication = 0x4E,          // V2+

        [Description("0x50 Node Identify")]
        NodeIdentify = 0x50,                // V3+

        [Description("0x60 Generic Event Sound Notification 1")]
        GenericEventSoundNotification1  = 0x60,
        [Description("0x61 Generic Event Sound Notification 2")]
        GenericEventSoundNotification2  = 0x61,
        [Description("0x62 Generic Event Sound Notification 3")]
        GenericEventSoundNotification3  = 0x62,
        [Description("0x63 Generic Event Sound Notification 4")]
        GenericEventSoundNotification4  = 0x63,
        [Description("0x7F Generic Event Sound Notification 32")]
        GenericEventSoundNotification32 = 0x7F,
        
        [Description("0x80 Manufacturer Defined Indicator 1")]
        ManufacturerDefinedIndicator1   = 0x80,
        [Description("0x81 Manufacturer Defined Indicator 2")]
        ManufacturerDefinedIndicator2   = 0x81,
        [Description("0x82 Manufacturer Defined Indicator 3")]
        ManufacturerDefinedIndicator3   = 0x82,
        [Description("0x83 Manufacturer Defined Indicator 4")]
        ManufacturerDefinedIndicator4   = 0x83,
        [Description("0x9F Manufacturer Defined Indicator 32")]
        ManufacturerDefinedIndicator32  = 0x9F,

        [Description("0xF0 Buzzer")]
        Buzzer = 0xF0,
    }

    public enum IndicatorPropertyNames : byte
    {
        [Description("0x01 Specific Level")]
        SpecificLevel = 0x01,               // V2+
        [Description("0x02 On Off")]
        OnOff = 0x02,                       // V2+
        [Description("0x03 On Off Periods")]
        OnOffPeriods = 0x03,                // V2+
        [Description("0x04 On Off Cycles")]
        OnOffCycles = 0x04,                 // V2+
        [Description("0x05 On Time")]
        OnTime = 0x05,                      // V3+
        [Description("0x06 Timeout Min")]
        TimeoutMin = 0x06,                  // V3+
        [Description("0x07 Timeout Sec")]
        TimeoutSec = 0x07,                  // V3+
        [Description("0x08 Timeout 1/100 Sec")]
        TimeoutHSec = 0x08,                 // V3+
        [Description("0x09 Volume")]
        Volume = 0x09,                      // V3+

        [Description("0x10 Advertise Sleep")]
        AdvertiseSleep = 0x10,              // V2+
    }
    #endregion // Indicator enums
}
