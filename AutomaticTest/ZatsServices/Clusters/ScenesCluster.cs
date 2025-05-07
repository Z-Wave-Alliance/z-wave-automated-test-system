using Newtonsoft.Json;
using System.Collections.Generic;

namespace UicApplication.Clusters
{
    public class ScenesCluster
    {
        public const string ClusterName = "Scenes";
        public const string ClusterDesired = "Desired";
        public const string ClusterReported = "Reported";
        public const string ClusterSupportedCommands = "SupportedCommands";
        public const string ClusterAttributes = "Attributes";

        #region Cluster Attributes

        public class ATT_SceneCount
        {
            public const string SceneCount_name = "SceneCount";
            public class SceneCount_Attributes
            {
                public static implicit operator string(SceneCount_Attributes command)
                {
                    return SumAllAtt(ScenesCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_SceneCount command)
            {
                return SumATTStr(ScenesCluster.ClusterName, ScenesCluster.ATT_SceneCount.SceneCount_name);
            }
            public class SceneCount_Desired
            {
                public static implicit operator string(SceneCount_Desired command)
                {
                    return SumATTStr_WithSubValue(ScenesCluster.ClusterName, ScenesCluster.ATT_SceneCount.SceneCount_name, ClusterDesired);
                }
            }
            public class SceneCount_Report
            {
                public static implicit operator string(SceneCount_Report command)
                {
                    return SumATTStr_WithSubValue(ScenesCluster.ClusterName, ScenesCluster.ATT_SceneCount.SceneCount_name, ClusterReported);
                }
            }
            public class SceneCount_SupportCommands
            {
                public static implicit operator string(SceneCount_SupportCommands command)
                {
                    return SumSupCMD(ScenesCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_CurrentScene
        {
            public const string CurrentScene_name = "CurrentScene";
            public class CurrentScene_Attributes
            {
                public static implicit operator string(CurrentScene_Attributes command)
                {
                    return SumAllAtt(ScenesCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_CurrentScene command)
            {
                return SumATTStr(ScenesCluster.ClusterName, ScenesCluster.ATT_CurrentScene.CurrentScene_name);
            }
            public class CurrentScene_Desired
            {
                public static implicit operator string(CurrentScene_Desired command)
                {
                    return SumATTStr_WithSubValue(ScenesCluster.ClusterName, ScenesCluster.ATT_CurrentScene.CurrentScene_name, ClusterDesired);
                }
            }
            public class CurrentScene_Report
            {
                public static implicit operator string(CurrentScene_Report command)
                {
                    return SumATTStr_WithSubValue(ScenesCluster.ClusterName, ScenesCluster.ATT_CurrentScene.CurrentScene_name, ClusterReported);
                }
            }
            public class CurrentScene_SupportCommands
            {
                public static implicit operator string(CurrentScene_SupportCommands command)
                {
                    return SumSupCMD(ScenesCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_CurrentGroup
        {
            public const string CurrentGroup_name = "CurrentGroup";
            public class CurrentGroup_Attributes
            {
                public static implicit operator string(CurrentGroup_Attributes command)
                {
                    return SumAllAtt(ScenesCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_CurrentGroup command)
            {
                return SumATTStr(ScenesCluster.ClusterName, ScenesCluster.ATT_CurrentGroup.CurrentGroup_name);
            }
            public class CurrentGroup_Desired
            {
                public static implicit operator string(CurrentGroup_Desired command)
                {
                    return SumATTStr_WithSubValue(ScenesCluster.ClusterName, ScenesCluster.ATT_CurrentGroup.CurrentGroup_name, ClusterDesired);
                }
            }
            public class CurrentGroup_Report
            {
                public static implicit operator string(CurrentGroup_Report command)
                {
                    return SumATTStr_WithSubValue(ScenesCluster.ClusterName, ScenesCluster.ATT_CurrentGroup.CurrentGroup_name, ClusterReported);
                }
            }
            public class CurrentGroup_SupportCommands
            {
                public static implicit operator string(CurrentGroup_SupportCommands command)
                {
                    return SumSupCMD(ScenesCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_SceneValid
        {
            public const string SceneValid_name = "SceneValid";
            public class SceneValid_Attributes
            {
                public static implicit operator string(SceneValid_Attributes command)
                {
                    return SumAllAtt(ScenesCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_SceneValid command)
            {
                return SumATTStr(ScenesCluster.ClusterName, ScenesCluster.ATT_SceneValid.SceneValid_name);
            }
            public class SceneValid_Desired
            {
                public static implicit operator string(SceneValid_Desired command)
                {
                    return SumATTStr_WithSubValue(ScenesCluster.ClusterName, ScenesCluster.ATT_SceneValid.SceneValid_name, ClusterDesired);
                }
            }
            public class SceneValid_Report
            {
                public static implicit operator string(SceneValid_Report command)
                {
                    return SumATTStr_WithSubValue(ScenesCluster.ClusterName, ScenesCluster.ATT_SceneValid.SceneValid_name, ClusterReported);
                }
            }
            public class SceneValid_SupportCommands
            {
                public static implicit operator string(SceneValid_SupportCommands command)
                {
                    return SumSupCMD(ScenesCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_NameSupport
        {
            public const string NameSupport_name = "NameSupport";
            public class NameSupport_Attributes
            {
                public static implicit operator string(NameSupport_Attributes command)
                {
                    return SumAllAtt(ScenesCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_NameSupport command)
            {
                return SumATTStr(ScenesCluster.ClusterName, ScenesCluster.ATT_NameSupport.NameSupport_name);
            }
            public class NameSupport_Desired
            {
                public static implicit operator string(NameSupport_Desired command)
                {
                    return SumATTStr_WithSubValue(ScenesCluster.ClusterName, ScenesCluster.ATT_NameSupport.NameSupport_name, ClusterDesired);
                }
            }
            public class NameSupport_Report
            {
                public static implicit operator string(NameSupport_Report command)
                {
                    return SumATTStr_WithSubValue(ScenesCluster.ClusterName, ScenesCluster.ATT_NameSupport.NameSupport_name, ClusterReported);
                }
            }
            public class NameSupport_SupportCommands
            {
                public static implicit operator string(NameSupport_SupportCommands command)
                {
                    return SumSupCMD(ScenesCluster.ClusterName, ClusterSupportedCommands);
                }
            }
            public class Bitmap
            {
                public class SceneNamesSupported
                {
                    public const string SceneNamesSupported_name = "SceneNamesSupported";
                }
            }
        }/*End of ATT class*/
        public class ATT_LastConfiguredBy
        {
            public const string LastConfiguredBy_name = "LastConfiguredBy";
            public class LastConfiguredBy_Attributes
            {
                public static implicit operator string(LastConfiguredBy_Attributes command)
                {
                    return SumAllAtt(ScenesCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_LastConfiguredBy command)
            {
                return SumATTStr(ScenesCluster.ClusterName, ScenesCluster.ATT_LastConfiguredBy.LastConfiguredBy_name);
            }
            public class LastConfiguredBy_Desired
            {
                public static implicit operator string(LastConfiguredBy_Desired command)
                {
                    return SumATTStr_WithSubValue(ScenesCluster.ClusterName, ScenesCluster.ATT_LastConfiguredBy.LastConfiguredBy_name, ClusterDesired);
                }
            }
            public class LastConfiguredBy_Report
            {
                public static implicit operator string(LastConfiguredBy_Report command)
                {
                    return SumATTStr_WithSubValue(ScenesCluster.ClusterName, ScenesCluster.ATT_LastConfiguredBy.LastConfiguredBy_name, ClusterReported);
                }
            }
            public class LastConfiguredBy_SupportCommands
            {
                public static implicit operator string(LastConfiguredBy_SupportCommands command)
                {
                    return SumSupCMD(ScenesCluster.ClusterName, ClusterSupportedCommands);
                }
            }
            public class Restriction
            {
                public class UnknownOrNotConfigured
                {
                    public const string UnknownOrNotConfigured_value = "ffffffffffffffff";
                    public const string UnknownOrNotConfigured_name = "UnknownOrNotConfigured";
                }
            }/*End of Restriction*/
        }/*End of ATT class*/
        public class ATT_SupportedGeneratedCommands
        {
            public const string SupportedGeneratedCommands_name = "SupportedGeneratedCommands";
            public class SupportedGeneratedCommands_Attributes
            {
                public static implicit operator string(SupportedGeneratedCommands_Attributes command)
                {
                    return SumAllAtt(ScenesCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_SupportedGeneratedCommands command)
            {
                return SumATTStr(ScenesCluster.ClusterName, ScenesCluster.ATT_SupportedGeneratedCommands.SupportedGeneratedCommands_name);
            }            
            public class SupportedGeneratedCommands
            {
                public static implicit operator string(SupportedGeneratedCommands command)
                {
                    return SumSupCMD(ScenesCluster.ClusterName, SupportedGeneratedCommands_name);
                }
            }
        }/*End of ATT class*/
        public class ATT_SupportedCommands
        {
            public const string SupportedCommands_name = "SupportedCommands";
            public class SupportedCommands_Attributes
            {
                public static implicit operator string(SupportedCommands_Attributes command)
                {
                    return SumAllAtt(ScenesCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_SupportedCommands command)
            {
                return SumATTStr(ScenesCluster.ClusterName, ScenesCluster.ATT_SupportedCommands.SupportedCommands_name);
            }
            public class SupportedCommands
            {
                public static implicit operator string(SupportedCommands command)
                {
                    return SumSupCMD(ScenesCluster.ClusterName, SupportedCommands_name);
                }
            }
        }/*End of ATT class*/
        public class ATT_SceneTable
        {
            public const string SceneTable_name = "SceneTable";
            public class SceneTable_Attributes
            {
                public static implicit operator string(SceneTable_Attributes command)
                {
                    return SumAllAtt(ScenesCluster.ClusterName, ClusterAttributes);
                }                
            }
            public static implicit operator string(ATT_SceneTable command)
            {
                return SumATTStr(ScenesCluster.ClusterName, ScenesCluster.ATT_SceneTable.SceneTable_name);
            }
            public class SceneTable_Desired
            {
                public static implicit operator string(SceneTable_Desired command)
                {
                    return SumATTStr_WithSubValue(ScenesCluster.ClusterName, ScenesCluster.ATT_SceneTable.SceneTable_name, ClusterDesired);
                }
            }
            public class SceneTable_Report
            {
                public static implicit operator string(SceneTable_Report command)
                {
                    return SumATTStr_WithSubValue(ScenesCluster.ClusterName, ScenesCluster.ATT_SceneTable.SceneTable_name, ClusterReported);
                }
            }
            public class SceneTable_SupportCommands
            {
                public static implicit operator string(SceneTable_SupportCommands command)
                {
                    return SumSupCMD(ScenesCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }
        #endregion

        #region Cluster Commands

        public class CMD_AddScene
        {
            public const string AddScene_name = "AddScene";
            public static implicit operator string(CMD_AddScene command)
            {
                return SumCMDStr(ScenesCluster.ClusterName, ScenesCluster.CMD_AddScene.AddScene_name);
            }
            public class CMD_AddScene_Payload
            {
                public int GroupID { get; set; }
                public int SceneID { get; set; }
                public int TransitionTime { get; set; }
                public string SceneName { get; set; }
                public SExtensionFieldSetList ExtensionFieldSets { get; set; }
                public int TransitionTime100ms { get; set; }
                public class SExtensionFieldSetList
                {                    
                    public LevelAtt Level { get; set; }
                    public class LevelAtt
                    {
                        public int CurrentLevel { get; set; }
                    }
                    public OnOffAtt OnOff { get; set; }
                    public class OnOffAtt
                    {
                        public bool OnOff { get; set; }
                    }
                }
                /*Start of public Payload*/
                public CMD_AddScene_Payload(int my_GroupID, int my_SceneID, string my_SceneName, SExtensionFieldSetList my_ExtensionFieldSets, int my_TransitionTime, int my_TransitionTime100ms)
                {
                    GroupID = my_GroupID;
                    SceneID = my_SceneID;                    
                    SceneName = my_SceneName;
                    ExtensionFieldSets = my_ExtensionFieldSets;
                    TransitionTime = my_TransitionTime;
                    TransitionTime100ms = my_TransitionTime100ms;
                }
                /*End of public Payload*/
                public static implicit operator CMD_AddScene_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_AddScene_Payload>(command);
                }
                public static implicit operator string(CMD_AddScene_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        public class CMD_ViewScene
        {
            public const string ViewScene_name = "ViewScene";
            public static implicit operator string(CMD_ViewScene command)
            {
                return SumCMDStr(ScenesCluster.ClusterName, ScenesCluster.CMD_ViewScene.ViewScene_name);
            }
            public class CMD_ViewScene_Payload
            {
                public int GroupID { get; set; }
                public int SceneID { get; set; }
                /*Start of public Payload*/
                public CMD_ViewScene_Payload(int my_GroupID, int my_SceneID)
                {
                    GroupID = my_GroupID;
                    SceneID = my_SceneID;

                }
                /*End of public Payload*/
                public static implicit operator CMD_ViewScene_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_ViewScene_Payload>(command);
                }
                public static implicit operator string(CMD_ViewScene_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        public class CMD_RemoveScene
        {
            public const string RemoveScene_name = "RemoveScene";
            public static implicit operator string(CMD_RemoveScene command)
            {
                return SumCMDStr(ScenesCluster.ClusterName, ScenesCluster.CMD_RemoveScene.RemoveScene_name);
            }
            public class CMD_RemoveScene_Payload
            {
                public string GroupID { get; set; }
                public string SceneID { get; set; }
                /*Start of public Payload*/
                public CMD_RemoveScene_Payload(string my_GroupID, string my_SceneID)
                {
                    GroupID = my_GroupID;
                    SceneID = my_SceneID;

                }
                /*End of public Payload*/
                public static implicit operator CMD_RemoveScene_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_RemoveScene_Payload>(command);
                }
                public static implicit operator string(CMD_RemoveScene_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        public class CMD_RemoveAllScenes
        {
            public const string RemoveAllScenes_name = "RemoveAllScenes";
            public static implicit operator string(CMD_RemoveAllScenes command)
            {
                return SumCMDStr(ScenesCluster.ClusterName, ScenesCluster.CMD_RemoveAllScenes.RemoveAllScenes_name);
            }
            public class CMD_RemoveAllScenes_Payload
            {
                public string GroupID { get; set; }
                /*Start of public Payload*/
                public CMD_RemoveAllScenes_Payload(string my_GroupID)
                {
                    GroupID = my_GroupID;

                }
                /*End of public Payload*/
                public static implicit operator CMD_RemoveAllScenes_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_RemoveAllScenes_Payload>(command);
                }
                public static implicit operator string(CMD_RemoveAllScenes_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        public class CMD_StoreScene
        {
            public const string StoreScene_name = "StoreScene";
            public static implicit operator string(CMD_StoreScene command)
            {
                return SumCMDStr(ScenesCluster.ClusterName, ScenesCluster.CMD_StoreScene.StoreScene_name);
            }
            public class CMD_StoreScene_Payload
            {
                public int GroupID { get; set; }
                public int SceneID { get; set; }
                /*Start of public Payload*/
                public CMD_StoreScene_Payload(int my_GroupID, int my_SceneID)
                {
                    GroupID = my_GroupID;
                    SceneID = my_SceneID;

                }
                /*End of public Payload*/
                public static implicit operator CMD_StoreScene_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_StoreScene_Payload>(command);
                }
                public static implicit operator string(CMD_StoreScene_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        public class CMD_RecallScene
        {
            public const string RecallScene_name = "RecallScene";
            public static implicit operator string(CMD_RecallScene command)
            {
                return SumCMDStr(ScenesCluster.ClusterName, ScenesCluster.CMD_RecallScene.RecallScene_name);
            }
            public class CMD_RecallScene_Payload
            {
                public int GroupID { get; set; }
                public int SceneID { get; set; }
                public int TransitionTime { get; set; }
                /*Start of public Payload*/
                public CMD_RecallScene_Payload(int my_GroupID, int my_SceneID, int my_TransitionTime)
                {
                    GroupID = my_GroupID;
                    SceneID = my_SceneID;
                    TransitionTime = my_TransitionTime;

                }
                /*End of public Payload*/
                public static implicit operator CMD_RecallScene_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_RecallScene_Payload>(command);
                }
                public static implicit operator string(CMD_RecallScene_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        public class CMD_GetSceneMembership
        {
            public const string GetSceneMembership_name = "GetSceneMembership";
            public static implicit operator string(CMD_GetSceneMembership command)
            {
                return SumCMDStr(ScenesCluster.ClusterName, ScenesCluster.CMD_GetSceneMembership.GetSceneMembership_name);
            }
            public class CMD_GetSceneMembership_Payload
            {
                public string GroupID { get; set; }
                /*Start of public Payload*/
                public CMD_GetSceneMembership_Payload(string my_GroupID)
                {
                    GroupID = my_GroupID;

                }
                /*End of public Payload*/
                public static implicit operator CMD_GetSceneMembership_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_GetSceneMembership_Payload>(command);
                }
                public static implicit operator string(CMD_GetSceneMembership_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        public class CMD_EnhancedAddScene
        {
            public const string EnhancedAddScene_name = "EnhancedAddScene";
            public static implicit operator string(CMD_EnhancedAddScene command)
            {
                return SumCMDStr(ScenesCluster.ClusterName, ScenesCluster.CMD_EnhancedAddScene.EnhancedAddScene_name);
            }
            public class CMD_EnhancedAddScene_Payload
            {
                public string GroupID { get; set; }
                public string SceneID { get; set; }
                public string TransitionTime { get; set; }
                public string SceneName { get; set; }
                public string ExtensionFieldSets { get; set; }
                /*Start of public Payload*/
                public CMD_EnhancedAddScene_Payload(string my_GroupID, string my_SceneID, string my_TransitionTime, string my_SceneName, string my_ExtensionFieldSets)
                {
                    GroupID = my_GroupID;
                    SceneID = my_SceneID;
                    TransitionTime = my_TransitionTime;
                    SceneName = my_SceneName;
                    ExtensionFieldSets = my_ExtensionFieldSets;

                }
                /*End of public Payload*/
                public static implicit operator CMD_EnhancedAddScene_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_EnhancedAddScene_Payload>(command);
                }
                public static implicit operator string(CMD_EnhancedAddScene_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        public class CMD_EnhancedViewScene
        {
            public const string EnhancedViewScene_name = "EnhancedViewScene";
            public static implicit operator string(CMD_EnhancedViewScene command)
            {
                return SumCMDStr(ScenesCluster.ClusterName, ScenesCluster.CMD_EnhancedViewScene.EnhancedViewScene_name);
            }
            public class CMD_EnhancedViewScene_Payload
            {
                public string GroupID { get; set; }
                public string SceneID { get; set; }
                /*Start of public Payload*/
                public CMD_EnhancedViewScene_Payload(string my_GroupID, string my_SceneID)
                {
                    GroupID = my_GroupID;
                    SceneID = my_SceneID;

                }
                /*End of public Payload*/
                public static implicit operator CMD_EnhancedViewScene_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_EnhancedViewScene_Payload>(command);
                }
                public static implicit operator string(CMD_EnhancedViewScene_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        public class CMD_CopyScene
        {
            public const string CopyScene_name = "CopyScene";
            public static implicit operator string(CMD_CopyScene command)
            {
                return SumCMDStr(ScenesCluster.ClusterName, ScenesCluster.CMD_CopyScene.CopyScene_name);
            }
            public class CMD_CopyScene_Payload
            {
                public string Mode { get; set; }
                public string GroupIdentifierFrom { get; set; }
                public string SceneIdentifierFrom { get; set; }
                public string GroupIdentifierTo { get; set; }
                public struct SceneIdentifierTo_CopyAllScenes
                {
                    public string CopyAllScenes { get; set; }
                }

                public class SceneIdentifierTo
                {
                    public SceneIdentifierTo_CopyAllScenes my_CopyAllScenes { get; set; }

                }

                /*Start of public Payload*/
                public CMD_CopyScene_Payload(string my_Mode, string my_GroupIdentifierFrom, string my_SceneIdentifierFrom, string my_GroupIdentifierTo, SceneIdentifierTo my_SceneIdentifierTo)
                {
                    Mode = my_Mode;
                    GroupIdentifierFrom = my_GroupIdentifierFrom;
                    SceneIdentifierFrom = my_SceneIdentifierFrom;
                    GroupIdentifierTo = my_GroupIdentifierTo;
                    SceneIdentifierTo SceneIdentifierTo_payload = my_SceneIdentifierTo;
                }/*End of public Payload*/
                public static implicit operator CMD_CopyScene_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_CopyScene_Payload>(command);
                }
                public static implicit operator string(CMD_CopyScene_Payload command)
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

        public struct Payload_Att_Value
        {
            public string value;

            public static implicit operator Payload_Att_Value(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_Value>(options);
            }
        }
        public struct Payload_Att_Int_Value
        {
            public int value;

            public static implicit operator Payload_Att_Int_Value(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_Int_Value>(options);
            }
        }

        #region Scene specific payload
        //integer type
        public struct Payload_Att_ClusterRevision_Value
        {
            public int value;

            public static implicit operator Payload_Att_ClusterRevision_Value(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_ClusterRevision_Value>(options);
            }
        }
        public struct Payload_Att_SceneCount_Value
        {
            public int value;

            public static implicit operator Payload_Att_SceneCount_Value(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_SceneCount_Value>(options);
            }
        }
        public struct Payload_Att_CurrentScene_Value
        {
            public int value;

            public static implicit operator Payload_Att_CurrentScene_Value(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_CurrentScene_Value>(options);
            }
        }
        public struct Payload_Att_CurrentGroup_Value
        {
            public int value;

            public static implicit operator Payload_Att_CurrentGroup_Value(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_CurrentGroup_Value>(options);
            }
        }
        //bool type
        public struct Payload_Att_SceneValid_Value
        {
            public bool value;

            public static implicit operator Payload_Att_SceneValid_Value(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_SceneValid_Value>(options);
            }
        }
        public struct Payload_Att_NameSupport_Value
        {
            public bool value;

            public static implicit operator Payload_Att_NameSupport_Value(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_NameSupport_Value>(options);
            }
        }
        public struct Payload_Att_SceneTable_Value
        {

            public class SceneTableExtensionsObj
            {
                public class LevelAtt
                {
                    public int CurrentLevel { get; set; }
                }
                public class OnOffAtt
                {
                    public bool OnOff { get; set; }
                }
                public LevelAtt Level { get; set; }
                public OnOffAtt OnOff { get; set; }
            }
            public class TableObjects
            {
                public int GroupID { get; set; }
                public int SceneID { get; set; }
                public string SceneName { get; set; }
                public SceneTableExtensionsObj SceneTableExtensions { get; set; }
                public int TransitionTime { get; set; }
                public int TransitionTime100ms { get; set; }

            }
            public List<TableObjects> value { get; set; }                      

            public static implicit operator Payload_Att_SceneTable_Value(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_SceneTable_Value>(options);
            }
        }

        #endregion

        public static implicit operator ScenesCluster(string options)
        {
            return JsonConvert.DeserializeObject<ScenesCluster>(options);
        }
        public static implicit operator string(ScenesCluster command)
        {
            return ScenesCluster.ClusterName;
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
