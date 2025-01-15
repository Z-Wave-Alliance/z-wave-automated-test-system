// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Utils;
using Utils.UI.Enums;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;

// ToDo: Supervision Report Status 'Working' for CentralScene Configuration Set
// ToDo: Create unsolicited Central Scene Notification Commands (optional)

namespace Zats.Services
{
    public class CentralSceneSupport : CommandClassSupport
    {
        private CentralSceneSupportData CentralSceneSupportData { get; set; }

        //private readonly static string CommandClassName = "Central Scene CC";
        //private string EndPointName = "";
        //private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public const int MaximumNumberOfScenes = 100;  // Scene numbers 1 to 100 are supported

        public CentralSceneSupport()
            : this(new CentralSceneSupportData() { })
        { }

        public CentralSceneSupport(CentralSceneSupportData centralSceneSupportData)
            : base(COMMAND_CLASS_CENTRAL_SCENE.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 3;
            CentralSceneSupportData = centralSceneSupportData;
            //LogLevel = CentralSceneSupportData.LogLevel;

            // CC:005B.03.00.21.001: Slow Refresh MUST be enabled by default after inclusion, if supported.
            if (CentralSceneSupportData.SlowRefreshIsSupported)
            {
                CentralSceneSupportData.SlowRefreshIsActive = true;
            }
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
                if (isResponseAllowed && command != null && command.Length > 1 && CentralSceneSupportData != null)
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
                        ResultForSv = HandleConfigurationSet(command);
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
                rpt.supportedScenes = (byte)CentralSceneSupportData.Scenes.Count;
                rpt.properties1.numberOfBitMaskBytes = 0;
                if (CentralSceneSupportData.Scenes.Count > 0)
                {
                    foreach (SceneData s in CentralSceneSupportData.Scenes)
                    {
                        rpt.vg1.Add(new COMMAND_CLASS_CENTRAL_SCENE_V2.CENTRAL_SCENE_SUPPORTED_REPORT.TVG1()
                        {
                            supportedKeyAttributesForScene = s.SupportedKeyAttributesBitmask
                        });
                        if (CentralSceneSupportData.Identical)
                        {
                            break;
                        }
                    }
                    return rpt;
                }
            }
            if (Version == 3)
            {
                COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_SUPPORTED_REPORT rpt = new COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_SUPPORTED_REPORT() { };
                rpt.supportedScenes = (byte)CentralSceneSupportData.Scenes.Count;
                rpt.properties1.numberOfBitMaskBytes = CentralSceneSupportData.NumberOfBitmaskBytes;
                rpt.properties1.slowRefreshSupport = (byte)(CentralSceneSupportData.SlowRefreshIsSupported ? 0x01 : 0x00);
                if (CentralSceneSupportData.Scenes.Count > 0)
                {
                    foreach (SceneData s in CentralSceneSupportData.Scenes)
                    {
                        rpt.vg1.Add(new COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_SUPPORTED_REPORT.TVG1()
                        {
                            supportedKeyAttributesForScene = s.SupportedKeyAttributesBitmask
                        });
                        if (CentralSceneSupportData.Identical)
                        {
                            break;
                        }
                    }
                    return rpt;
                }
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

        private SvResultNames HandleConfigurationSet(byte[] command)
        {
            if (Version == 3)
            {
                if (CentralSceneSupportData.SlowRefreshIsSupported)
                {
                    COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_CONFIGURATION_SET setCmd = command;
                    CentralSceneSupportData.SlowRefreshIsActive = setCmd.properties1.slowRefresh == 0 ? false : true;
                    return SvResultNames.Successful;
                }
            }
            return SvResultNames.Failed;
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
                case COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_CONFIGURATION_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }

    }

    public class SceneData
    {
        // Internals
        public byte SceneNumber { get; set; } = 0;
        // V2+ Supported Report
        public List<byte> SupportedKeyAttributesBitmask { get; set; }

        public SceneData(byte sceneNumber, List<byte> supportedKeyAttributesBitmask)
        {
            SceneNumber = sceneNumber;
            SupportedKeyAttributesBitmask = supportedKeyAttributesBitmask;
        }
    };

    public class CentralSceneSupportData
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        public List<SceneData> Scenes { get; set; } = new List<SceneData>();
        public bool SlowRefreshIsActive { get; set; } = false;

        // V2+ Supported Report
        public bool Identical { get; set; } = false;
        public byte NumberOfBitmaskBytes { get; set; } = 1;
        // V3+ Supported Report
        public bool SlowRefreshIsSupported { get; set; } = false;
        // V3+ Configuration Report

        public SceneData AddScene(byte sceneNumber, List<byte> supportedKeyAttributesBitmask)
        {
            if (sceneNumber >= 0x01 && sceneNumber < CentralSceneSupport.MaximumNumberOfScenes)     // validate scene number range
            {
                if (Scenes.Find(scene => scene.SceneNumber == sceneNumber) == null)                 // validate scene number existence
                {
                    if (supportedKeyAttributesBitmask.Count == NumberOfBitmaskBytes)                // validate bitmask length (avoid different length)
                    {
                        if ((Identical && Scenes.Count > 0 && supportedKeyAttributesBitmask == Scenes[0].SupportedKeyAttributesBitmask)
                         || (Identical && Scenes.Count == 0)
                         || (!Identical))                                                           // validate bitmask vs. identical
                        {
                            SceneData obj = new SceneData(sceneNumber, supportedKeyAttributesBitmask);
                            Scenes.Add(obj);
                            return obj;
                        }
                    }
                }
            }
            return null;
        }
    }

    [Flags]
    public enum SceneKeyAttributeNames : byte
    {
        KeyPressed1 = 1,
        KeyReleased = 2,
        KeyHeldDown = 4,
        KeyPressed2 = 8,
        KeyPressed3 = 16,
        KeyPressed4 = 32,
        KeyPressed5 = 64,
    }
}
