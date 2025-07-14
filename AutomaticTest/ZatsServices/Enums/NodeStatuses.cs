// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    public enum NodeStatuses
    {
        Unknown = ZWave.BasicApplication.Enums.NodeStatuses.Unknown,
        LearnReady = ZWave.BasicApplication.Enums.NodeStatuses.LearnReady,
        NodeFound = ZWave.BasicApplication.Enums.NodeStatuses.NodeFound,
        AddingRemovingEndDevice = ZWave.BasicApplication.Enums.NodeStatuses.AddingRemovingEndDevice,
        AddingRemovingController = ZWave.BasicApplication.Enums.NodeStatuses.AddingRemovingController,
        ProtocolDone = ZWave.BasicApplication.Enums.NodeStatuses.ProtocolDone,
        Done = ZWave.BasicApplication.Enums.NodeStatuses.Done,
        Failed = ZWave.BasicApplication.Enums.NodeStatuses.Failed,
    }

    public enum ControllerUpdateStatuses
    {
        NodeInfoIncludedReceived = ZWave.BasicApplication.Enums.ControllerUpdateStatuses.NodeInfoIncludedReceived,
        NodeInfoSmartStartReceived = ZWave.BasicApplication.Enums.ControllerUpdateStatuses.NodeInfoSmartStartReceived,
        NodeInfoSmartStartReceivedLR = ZWave.BasicApplication.Enums.ControllerUpdateStatuses.NodeInfoSmartStartReceivedLR,
        NodeInfoReceived = ZWave.BasicApplication.Enums.ControllerUpdateStatuses.NodeInfoReceived,
        NopPowerReceived = ZWave.BasicApplication.Enums.ControllerUpdateStatuses.NopPowerReceived,
        NodeInfoReqDone = ZWave.BasicApplication.Enums.ControllerUpdateStatuses.NodeInfoReqDone,
        NodeInfoReqFailed = ZWave.BasicApplication.Enums.ControllerUpdateStatuses.NodeInfoReqFailed,
        RoutingPending = ZWave.BasicApplication.Enums.ControllerUpdateStatuses.RoutingPending,
        NewIdAssigned = ZWave.BasicApplication.Enums.ControllerUpdateStatuses.NewIdAssigned,
        DeleteDone = ZWave.BasicApplication.Enums.ControllerUpdateStatuses.DeleteDone,
        SucId = ZWave.BasicApplication.Enums.ControllerUpdateStatuses.SucId
    }
}
