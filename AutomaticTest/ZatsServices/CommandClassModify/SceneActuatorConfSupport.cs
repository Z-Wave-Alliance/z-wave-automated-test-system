// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;

namespace Zats.Services
{
    /// <summary>
    /// Generic Command Class support to support unknown CC's or known CC's freely
    /// Supervision is handled with SUCCESS if a Command Callback has been registered using
    /// ApplicationService.AddCommandCallbacks() method
    /// </summary>

    public class SceneActuatorConfSupport : CommandClassSupport
    {

        private SvResultNames ResultForSv = SvResultNames.Undefined;
        private byte _sceneId = 0;
        private byte _dimmingDuration  = 0;
        private byte _level = 0;

        public SceneActuatorConfSupport(byte commandClassId)
            : base(COMMAND_CLASS_SCENE_ACTUATOR_CONF.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 3;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (IsCommandCallbackRegistered(supervisionGet.encapsulatedCommand[1]))
            {
                supervisionReport.status = (byte)SvStatusNames.Success;
                return;
            }
            
            supervisionReport.status = (byte)SvStatusNames.NoSupport;
            switch(supervisionGet.encapsulatedCommand[1])
            {
                case COMMAND_CLASS_SCENE_ACTUATOR_CONF.SCENE_ACTUATOR_CONF_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        _sceneId = supervisionGet.encapsulatedCommand[2];
                        _dimmingDuration = supervisionGet.encapsulatedCommand[3];
                        _level = supervisionGet.encapsulatedCommand[5];
                        supervisionReport.status = (byte)SvStatusNames.Success;
                    }
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
            
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

                if (isResponseAllowed && command != null && command.Length > 1)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_SCENE_ACTUATOR_CONF.SCENE_ACTUATOR_CONF_GET.ID)
                    {
                        COMMAND_CLASS_SCENE_ACTUATOR_CONF.SCENE_ACTUATOR_CONF_GET ActuConfGet = command;
                        rpt = new COMMAND_CLASS_SCENE_ACTUATOR_CONF.SCENE_ACTUATOR_CONF_REPORT()
                        {
                            sceneId = ActuConfGet.sceneId,
                            dimmingDuration = _dimmingDuration,
                            level = _level
                        };
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
    }
    public class SceneSupportData
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        public List<SceneActuatorConfSupport> SceneGroups { get; set; } = new List<SceneActuatorConfSupport>();

        // Access to Scene Groups for other supporting Command Classes
        public static SceneSupportData SupportDataObject;
        public SceneSupportData()
        {
            SupportDataObject = this;
        }
    }
}
