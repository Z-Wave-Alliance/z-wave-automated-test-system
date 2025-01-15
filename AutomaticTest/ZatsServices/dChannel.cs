// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com

namespace Zats.Services
{
    public struct dChannel
    {
        public string Channel;
        public dChannel(string channel)
        {
            Channel = channel;
        }
    }

    public static class D_CH
    {
        public static dChannel a = new dChannel("a");
    }
}
