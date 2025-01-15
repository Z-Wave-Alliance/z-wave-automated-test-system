// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using ZWave;

namespace Zats.Services
{
    public abstract class _ActionToken : IDisposable
    {
        internal ActionToken Token { get; set; }
        public _ActionToken()
        {
        }
        public _ActionToken(ActionToken token)
        {
            Token = token;
        }

        public void Init(ActionToken token)
        {
            Token = token;
        }

        #region IDisposable Members

        public void Dispose()
        {
            Token.WaitCompletedSignal();
        }

        #endregion
    }
}
