// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>

using ZWave;

namespace Zats.Services
{
    public interface IAssociations
    {
        BasicResult.Void EnableSupport(Alias[] controlles);
        BasicResult.AssociationGetGroups GetGroups(Alias controllerNumber, Node deviceId);
        BasicResult.Void RemoveNodeId(Alias controllerNumber, Node deviceId, byte groupId, Node destNodeId, bool IsAssignReturnRoutes = false);
        BasicResult.Void AssociationCreate(Alias controllerNumber, Node deviceId, byte groupId, Node destNode, bool IsAssignReturnRoutes = false);
        BasicResult.AssociationGetNodes GetNodes(Alias controllerNumber, Node associativeDevice, byte groupId);
    }
}
