using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Policy;
using System.Threading;
using Utils;
using Utils.UI.Enums;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;
using ZWave.Xml.Application;

// ToDo: Supervision Report Status 'Working' for CentralScene Configuration Set
// ToDo: Create unsolicited Central Scene Notification Commands (optional)

namespace Zats.Services
{
    [CommandClassName("Central Scene Command Class")]
    [CommandClassId(COMMAND_CLASS_CENTRAL_SCENE.ID)]
    [MinVersion(1)]
    [MaxVersion(3)]
    public class CentralSceneSupport : CommandClassSupport
    {
        private CentralSceneSupportData CentralSceneSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return CentralSceneSupportData; }
            set { CentralSceneSupportData = (CentralSceneSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public const int MaximumNumberOfScenes = 100;  // Scene numbers 1 to 100 are supported

        public CentralSceneSupport()
            : this(new CentralSceneSupportData() { })
        { }

        public CentralSceneSupport(CentralSceneSupportData centralSceneSupportData)
            : base(COMMAND_CLASS_CENTRAL_SCENE.ID)
        {
            CentralSceneSupportData = centralSceneSupportData;

            // CC:005B.03.00.21.001: Slow Refresh MUST be enabled by default after inclusion, if supported.
            if (CentralSceneSupportData.SlowRefreshIsSupported)
            {
                CentralSceneSupportData.SlowRefreshIsActive = true;
            }
        }

        public override void StartEmulation()
        {
            // CC:005B.03.00.21.001: Slow Refresh MUST be enabled by default after inclusion, if supported.
            if (CentralSceneSupportData.SlowRefreshIsSupported)
            {
                CentralSceneSupportData.SlowRefreshIsActive = true;
            }
        }

        public override void ResetEmulation()
        {
            // CC:005B.03.00.21.001: Slow Refresh MUST be enabled by default after inclusion, if supported.
            if (CentralSceneSupportData.SlowRefreshIsSupported)
            {
                CentralSceneSupportData.SlowRefreshIsActive = true;
            }
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
                if (isHandlingAllowed && command != null && command.Length > 1 && CentralSceneSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_CENTRAL_SCENE.CENTRAL_SCENE_SUPPORTED_GET.ID)
                    {
                        rpt = PrepareSupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_CONFIGURATION_GET.ID)
                    {
                        rpt = PrepareConfigurationReport();
                    }
                    else if (command[1] == COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_CONFIGURATION_SET.ID)
                    {
                        ResultForSv = HandleConfigurationSetCmd(command, sendSrcNode);
                    }

                    if (rpt != null && IsResponseAllowed())
                    {
                        ApiOperation sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command, sendDstNode.EndPointId);
                }
            }
        }

        private byte[] PrepareSupportedReport()
        {
            if (Version == 1)
            {
                COMMAND_CLASS_CENTRAL_SCENE.CENTRAL_SCENE_SUPPORTED_REPORT rpt = new COMMAND_CLASS_CENTRAL_SCENE.CENTRAL_SCENE_SUPPORTED_REPORT() { };
                rpt.supportedScenes = (byte)CentralSceneSupportData.Scenes.Count;
                return rpt;
            }
            if (Version == 2)
            {
                COMMAND_CLASS_CENTRAL_SCENE_V2.CENTRAL_SCENE_SUPPORTED_REPORT rpt = new COMMAND_CLASS_CENTRAL_SCENE_V2.CENTRAL_SCENE_SUPPORTED_REPORT() { };
                rpt.supportedScenes = (byte) CentralSceneSupportData.Scenes.Count;
                rpt.properties1.identical = (byte) (CentralSceneSupportData.Identical ? 0x01 : 0x00);
                if (CentralSceneSupportData.Scenes.Count > 0)
                {
                    rpt.properties1.numberOfBitMaskBytes = (byte) CentralSceneSupportData.Scenes.Select(x => x.GetSupportedAttributeBitmaskArray().Length).Max();
                    foreach (SceneData s in CentralSceneSupportData.Scenes)
                    {
                        rpt.vg1.Add(new COMMAND_CLASS_CENTRAL_SCENE_V2.CENTRAL_SCENE_SUPPORTED_REPORT.TVG1()
                        {
                            supportedKeyAttributesForScene = s.GetSupportedAttributeBitmaskArray()
                        });
                        if (rpt.properties1.identical == 0x01)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    rpt.properties1.numberOfBitMaskBytes = 1;
                    rpt.vg1.Add(new COMMAND_CLASS_CENTRAL_SCENE_V2.CENTRAL_SCENE_SUPPORTED_REPORT.TVG1()
                    {
                        supportedKeyAttributesForScene = new List<byte> { 0x00 }
                    });
                }
                return rpt;
            }
            if (Version == 3)
            {
                COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_SUPPORTED_REPORT rpt = new COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_SUPPORTED_REPORT() { };
                rpt.supportedScenes = (byte)CentralSceneSupportData.Scenes.Count;
                rpt.properties1.identical = (byte) (CentralSceneSupportData.Identical ? 0x01 : 0x00);
                rpt.properties1.slowRefreshSupport = (byte) (CentralSceneSupportData.SlowRefreshIsSupported ? 0x01 : 0x00);
                if (CentralSceneSupportData.Scenes.Count > 0)
                {
                    rpt.properties1.numberOfBitMaskBytes = (byte) CentralSceneSupportData.Scenes.Select(x => x.GetSupportedAttributeBitmaskArray().Length).Max();
                    foreach (SceneData s in CentralSceneSupportData.Scenes)
                    {
                        rpt.vg1.Add(new COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_SUPPORTED_REPORT.TVG1()
                        {
                            supportedKeyAttributesForScene = s.GetSupportedAttributeBitmaskArray()
                        });
                        if (rpt.properties1.identical == 0x01)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    rpt.properties1.numberOfBitMaskBytes = 1;
                    rpt.vg1.Add(new COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_SUPPORTED_REPORT.TVG1()
                    {
                        supportedKeyAttributesForScene = new List<byte> { 0x00 }
                    });
                }
                return rpt;
            }
            return null;
        }

        private byte[] PrepareConfigurationReport()
        {
            if (Version == 3)
            {
                COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_CONFIGURATION_REPORT rpt = new COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_CONFIGURATION_REPORT() { };
                rpt.properties1.slowRefresh = (byte)(CentralSceneSupportData.SlowRefreshIsActive ? 0x01 : 0x00);
                return rpt;
            }
            return null;
        }

        private byte[] PrepareCentralSceneNotification(byte sceneNumber, SceneKeyAttribute keyAttribute)
        {
            CentralSceneSupportData.SequenceNumber++;

            switch (Version)
            {
                case 3:
                    {
                        var notification = new COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_NOTIFICATION()
                        {
                            sceneNumber = sceneNumber,
                            sequenceNumber = CentralSceneSupportData.SequenceNumber,
                        };
                        notification.properties1.keyAttributes = (byte)keyAttribute;
                        notification.properties1.slowRefresh =(byte) (CentralSceneSupportData.SlowRefreshIsActive ? 0x01 : 0x00);
                        
                        return notification;
                    }
                    
                default:
                    {
                        var notification = new COMMAND_CLASS_CENTRAL_SCENE.CENTRAL_SCENE_NOTIFICATION()
                        {
                            sceneNumber = sceneNumber,
                            sequenceNumber = CentralSceneSupportData.SequenceNumber,
                        };
                        notification.properties1.keyAttributes = (byte)keyAttribute;

                        return notification;
                    }
            }
        }

        private SvResultNames HandleConfigurationSet(bool slowRefreshIsActive, bool updateUI, NodeTag srcNode)
        {
            if (Version == 3)
            {
                if (CentralSceneSupportData.SlowRefreshIsSupported)
                {
                    CentralSceneSupportData.SlowRefreshIsActive = slowRefreshIsActive;
                    if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        SendUnsolicitedReport(PrepareConfigurationReport(), srcNode);
                    }
                    if (updateUI)
                    {
                        SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                    }
                    return SvResultNames.Successful;
                }
            }
            return SvResultNames.Failed;
        }
        private SvResultNames HandleConfigurationSetCmd(byte[] command, NodeTag srcNode)
        {
            if (Version == 3)
            {
                COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_CONFIGURATION_SET setCmd = (COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_CONFIGURATION_SET) command;
                return HandleConfigurationSet(setCmd.properties1.slowRefresh == 0 ? false : true, true, srcNode);
            }
            return SvResultNames.Failed;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet,
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != CommandClassId)
            {
                supervisionReport.status = (byte)SvStatusNames.NoSupport;
                return;
            }

            supervisionReport.status = (byte)SvStatusNames.Fail;  // FAIL as default
            switch (supervisionGet.encapsulatedCommand[1])
            {
                case COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_CONFIGURATION_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte)SvStatusNames.Success;
                    }
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is CentralSceneSupportData newData)
            {
                bool supportedReportRequired = false;

                if (newData.Scenes?.FindAll(x => x.TriggerCentralSceneNotification) is var sceneNotificationTriggers &&
                    sceneNotificationTriggers.Count > 0)
                {
                    foreach (var scene in sceneNotificationTriggers)
                    {
                        SendUnsolicitedReport(PrepareCentralSceneNotification(scene.SceneNumber, scene.NotificationKeyAttribute), NodeTag.Empty);
                    
                        // remember last event
                        if (CentralSceneSupportData.Scenes.Find(x => x.SceneNumber == scene.SceneNumber) is SceneData sceneToUpdate)
                        {
                            sceneToUpdate.NotificationKeyAttribute = scene.NotificationKeyAttribute;
                        }
                    }
                }
                else
                {
                    bool isScenesChanged = CentralSceneSupportData.Scenes.Count != newData.Scenes.Count;
                    if (!isScenesChanged)
                    {
                        foreach (SceneData newScene in newData.Scenes)
                        {
                            if (CentralSceneSupportData.Scenes.Find(x => x.SceneNumber == newScene.SceneNumber) is SceneData currentScene)
                            {
                                if (currentScene.SupportedKeyAttributes.FindAll(x => !newScene.SupportedKeyAttributes.Contains(x)).Count > 0
                                 || newScene.SupportedKeyAttributes.FindAll(x => !currentScene.SupportedKeyAttributes.Contains(x)).Count > 0)
                                {
                                    isScenesChanged = true;
                                }
                            }
                            else
                            {
                                isScenesChanged = true;
                            }
                            if (isScenesChanged)
                            {
                                break;
                            }
                        }
                    }
                    if (isScenesChanged)
                    {
                        CentralSceneSupportData.Scenes = newData.Scenes;
                        supportedReportRequired = true;
                    }
                    if (CentralSceneSupportData.SlowRefreshIsSupported != newData.SlowRefreshIsSupported)
                    {
                        CentralSceneSupportData.SlowRefreshIsSupported = newData.SlowRefreshIsSupported;
                        supportedReportRequired = true;
                    }
                    if (supportedReportRequired && ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareSupportedReport(), NodeTag.Empty);
                    }

                    if (CentralSceneSupportData.SlowRefreshIsActive != newData.SlowRefreshIsActive)
                    {
                        HandleConfigurationSet(newData.SlowRefreshIsActive, false, NodeTag.Empty);
                    }
                }

                // safety mechanism: make sure that trigger flag is still false
                CentralSceneSupportData.Scenes.ForEach(x => x.TriggerCentralSceneNotification = false);

                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class SceneData : INestedItem
    {
        // Internals
        public byte SceneNumber { get; set; } = 0;
        // V2+ Supported Report

        [CustomDataParameter(1, "Supported Key Attributes", CommandClassesImpl.UiControl.ListCheckBox, CommandClassesImpl.InputType.Enum, EditType.Config)]
        public List<SceneKeyAttribute> SupportedKeyAttributes { get; set; } = new List<SceneKeyAttribute>();

        #region Trigger Central Scene Notification (EDE)
        [CustomDataParameterEventTrigger]
        [ParameterSeparator(1, "Runtime Notifications")]
        [CustomDataParameter(1, "Trigger Notification", 
            CommandClassesImpl.UiControl.Button, CommandClassesImpl.InputType.Boolean, EditType.View | EditType.Edit)]
        public bool TriggerCentralSceneNotification { get; set; }

        [CustomDataParameter(1, "Selected Key Attribute", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.View | EditType.Edit, nameof(SupportedKeyAttributes))]
        public SceneKeyAttribute NotificationKeyAttribute { get; set; }

        #endregion

        #region N+1 widget interface (EDE)
        // only used for EDE UI. see interface for useage notes.
        [JsonIgnore]
        public int NestedId 
        { 
            get { return SceneNumber; } 
            set { SceneNumber = (byte) value; } 
        }

        #endregion

        public SceneData()
        {

        }

        public SceneData(byte sceneNumber, List<SceneKeyAttribute> supportedKeyAttributes)
        {
            SceneNumber = sceneNumber;
            SupportedKeyAttributes = supportedKeyAttributes;
        }

        public byte[] GetSupportedAttributeBitmaskArray()
        {
             return CommandClassSupportUtils.CreateBitmaskFromList(SupportedKeyAttributes, 0).ToArray();
        }

        public UInt32 GetSupportedAttributesBitmask()
        {
            UInt32 result = 0;
            SupportedKeyAttributes.ForEach(x => { result |= (UInt32)(1 << (byte)x); });
            return result;
        }

    };

    public class CentralSceneSupportData : CommandClassSupportData, INodeItem
    {
        // Internals accessible from TestCases or EDE
        [NestedControl(1, "Scenes")]
        public List<SceneData> Scenes { get; set; } = new List<SceneData>();
        public bool SlowRefreshIsActive { get; set; } = false;
        
        // Internal since V1 Notification Report
        public byte SequenceNumber { get; set; }

        // V2+ Supported Report
        [JsonIgnore]
        public bool Identical 
        { 
            get 
            {
                if (Scenes != null && Scenes.Count > 0)
                {
                    UInt32 bitmaskScene1 = Scenes.First().GetSupportedAttributesBitmask();
                    foreach (SceneData sceneData in Scenes)
                    {
                        if (sceneData.GetSupportedAttributesBitmask() != bitmaskScene1)
                        {
                            return false;
                        }
                    }
                    return true;
                    // Does not work as expected:
                    // return Scenes.Any(scene => scene.GetSupportedAttributesBitmask() != Scenes.First().GetSupportedAttributesBitmask());
                }
                return false;
            } 
        }

        // V3+ Supported Report
        [ParameterSeparator(3, "Central Scene Supported Report")]
        [CustomDataParameter(3, "Slow Report Supported", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config)]
        public bool SlowRefreshIsSupported { get; set; } = false;
        // V3+ Configuration Report

        [JsonIgnore]
        public IEnumerable<INestedItem> ChildItems => Scenes;

        public SceneData AddScene(byte sceneNumber, List<SceneKeyAttribute> supportedKeyAttributesBitmask)
        {
            if (sceneNumber >= 0x01 && sceneNumber < CentralSceneSupport.MaximumNumberOfScenes)     // validate scene number range
            {
                if (Scenes.Find(scene => scene.SceneNumber == sceneNumber) == null)                 // validate scene number existence
                {
                    SceneData obj = new SceneData(sceneNumber, supportedKeyAttributesBitmask);
                    Scenes.Add(obj);
                    return obj;
                }
            }
            return null;
        }
    }

    public enum SceneKeyAttribute
    {
        [Description("0x00 Key pressed 1 time")]
        KeyPressed1Time = 0x00,
        [Description("0x01 Key released")]
        KeyReleased = 0x01,
        [Description("0x02 Key held down")]
        KeyHeldDown = 0x02,
        [Description("0x03 Key pressed 2 times")]
        KeyPressed2Time = 0x03,
        [Description("0x04 Key pressed 3 times")]
        KeyPressed3Time = 0x04,
        [Description("0x05 Key pressed 4 times")]
        KeyPressed4Time = 0x05,
        [Description("0x06 Key pressed 5 times")]
        KeyPressed5Time = 0x06,
    }
}
