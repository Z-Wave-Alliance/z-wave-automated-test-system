// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using ZWave.Enums;
using ZWave.Layers;
using ZWave.TextApplication;

namespace Zats.Services.MeshNetworkRacks
{
    public class MeshRouterController : TextDevice, IMeshRouterController
    {
        public MeshRouterController(ushort sessionId, ISessionClient sc, IFrameClient fc, ITransportClient tc)
            : base(ApiTypes.Text, sessionId, sc, fc, tc)
        {
        }

        public string CommandRequest(string command)
        {
            var res = Request(command, 2000);
            if (res)
                return res.ReceivedText;

            return string.Empty;
        }
    }
}
