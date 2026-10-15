// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using Utils.UI.Enums;

namespace Zats.Services.PCController
{
    public class PCControllerManualService : ServiceBase, IManualService
    {
        protected override void Log(string text, LogLevels logLevel)
        {
            throw new NotImplementedException();
        }

        public BasicResult.AddNode AddNode(Alias controllerNumber, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RemoveNode RemoveNode(Alias controllerNumber, int timeoutMs)
        {
            throw new NotImplementedException();
        }
    }
}
