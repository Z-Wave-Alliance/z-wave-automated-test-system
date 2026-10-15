// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
namespace Zats.Services
{
    public enum RoutingSchemes
    {
        Idle = ZWave.BasicApplication.Enums.RoutingSchemes.Idle,
        Direct = ZWave.BasicApplication.Enums.RoutingSchemes.Direct,
        CachedRouteSr = ZWave.BasicApplication.Enums.RoutingSchemes.CachedRouteSr,
        CachedRoute = ZWave.BasicApplication.Enums.RoutingSchemes.CachedRoute,
        CachedRouteNLwr = ZWave.BasicApplication.Enums.RoutingSchemes.CachedRouteNLwr,
        Route = ZWave.BasicApplication.Enums.RoutingSchemes.Route,
        ResortDirect = ZWave.BasicApplication.Enums.RoutingSchemes.ResortDirect,
        ResortExplore = ZWave.BasicApplication.Enums.RoutingSchemes.ResortExplore,
    }
}
