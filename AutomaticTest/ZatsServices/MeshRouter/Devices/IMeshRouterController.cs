// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services.MeshNetworkRacks
{
    public interface IMeshRouterController
    {
        string CommandRequest(string command);
    }
}
