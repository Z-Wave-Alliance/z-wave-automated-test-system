// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    public enum ProtocolStatuses
    {
        /// <summary>
        /// Protocol is idle
        /// </summary>
        Idle = ZWave.BasicApplication.Enums.ProtocolStatuses.Idle,
        /// <summary>
        /// Protocol is analyzing the routing table
        /// </summary>
        Routing = ZWave.BasicApplication.Enums.ProtocolStatuses.Routing,
        /// <summary>
        /// SUC sends pending updates
        /// </summary>
        SUC = ZWave.BasicApplication.Enums.ProtocolStatuses.SUC
    }

    public enum RequestNeighborUpdateStatuses
    {
        Started = ZWave.Enums.RequestNeighborUpdateStatuses.Started,
        Done = ZWave.Enums.RequestNeighborUpdateStatuses.Done,
        Failed = ZWave.Enums.RequestNeighborUpdateStatuses.Failed
    }
}
