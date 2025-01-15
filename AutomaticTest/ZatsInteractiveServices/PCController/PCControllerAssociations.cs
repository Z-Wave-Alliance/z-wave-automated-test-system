// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using Utils.UI.Enums;

namespace Zats.Services
{
    public class PCControllerAssociations : AutomationServiceBase, IAssociations
    {
        public PCControllerAssociations(PCControllerDeviceManager pccDeviceManager) : base(pccDeviceManager)
        {
        }

        public BasicResult.AssociationGetGroups GetGroups(Alias controllerNumber, Node deviceId)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var associationsView = FromMainMenuPage(uiController, PCControllerIdMap.AssociationsView.ID);
            var res = associationsView.GetAssociationModel(deviceId.Id, deviceId.EndPointId);

            return new BasicActionResult.AssociationGroupCollection(res);
        }

        public BasicResult.Void AssociationCreate(Alias controllerNumber, Node deviceId, byte groupId, Node destNode, bool IsAssignReturnRoutes = false)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var associationsView = FromMainMenuPage(uiController, PCControllerIdMap.AssociationsView.ID);
            var primaryMainWindow = uiController.Automation.NavigateTo(PCControllerIdMap.MainWindow.ID);
            primaryMainWindow.NavigateTo(PCControllerIdMap.NodesListView.ID)
                .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                .NavigateToNameProperty(destNode.ToString())
                .NavigateToNameProperty(destNode.ToString())
                .Select();
            associationsView.CreateNodeId(deviceId, groupId);
            return new BasicResult.Void();
        }


        public BasicResult.Void RemoveNodeId(Alias controllerNumber, Node deviceId, byte groupId, Node destNodeId, bool IsAssignReturnRoutes = false)
        {
            if (destNodeId.EndPointId > 0)
            {
                throw new NotImplementedException("Destination End Point for UI Auto");
            }
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var associationsView = FromMainMenuPage(uiController, PCControllerIdMap.AssociationsView.ID);
            associationsView.RemoveNodeId(deviceId, destNodeId, groupId);
            return new BasicResult.Void();
        }

        public BasicResult.AssociationGetNodes GetNodes(Alias controllerNumber, Node deviceId, byte groupId)
        {
            BasicActionResult.AssociationNodesResult res = new BasicActionResult.AssociationNodesResult();

            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var associationsView = FromMainMenuPage(uiController, PCControllerIdMap.AssociationsView.ID);
            var nodes = associationsView.GetNodesFromAssociationsTree(deviceId);
            res.Nodes = nodes;

            return new BasicActionResult.AssociationNodeCollection(res);
        }

        public BasicResult.Void EnableSupport(Alias[] controlles)
        {
            return new BasicResult.Void();
        }

        protected override void Log(string text, LogLevels logLevel)
        {
        }
    }
}
