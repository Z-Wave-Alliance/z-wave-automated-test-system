// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using System;
using System.Collections.Generic;
using ZWave;
using ZWave.BasicApplication.Operations;
using ZWave.BasicApplication.Tasks;
using System.Linq;
using System.Threading;
using Utils.Threading;
using Utils;

namespace Zats.Services
{
    public static class BasicActionToken
    {
        public class Void : BasicToken.Void
        {
            public Void(ActionToken token)
            {
                Token = token;
            }

            public override BasicResult.Void WaitCompletedSignal()
            {
                return new BasicActionResult.Void(Token.WaitCompletedSignal());
            }
        }

        public class ExpectZW : BasicToken.ExpectZW
        {
            public ExpectZW(ActionToken token)
            {
                Token = token;
            }

            public override BasicResult.ExpectZW WaitCompletedSignal()
            {
                var res = Token.WaitCompletedSignal();
                return new BasicActionResult.ExpectZW((ExpectZWResult)res);
            }

            public override bool IsStateActive
            {
                get { return Token.IsStateActive; }
            }
        }

        public class ExpectData : BasicToken.ExpectData
        {
            public ExpectData(ActionToken token)
            {
                Token = token;
            }

            public override BasicResult.ExpectData WaitCompletedSignal()
            {
                return new BasicActionResult.ExpectData((ExpectDataResult)Token.WaitCompletedSignal());
            }

        }

        public class AddNode : BasicToken.AddNode
        {
            public AddNode(ActionToken token)
            {
                Token = token;
            }

            public override BasicResult.AddNode WaitCompletedSignal()
            {
                return new BasicActionResult.AddNode((AddRemoveNodeResult)Token.WaitCompletedSignal());
            }

            public override bool IsStateActive
            {
                get { return Token.IsStateActive; }
            }
        }

        public class IncludeNode : BasicToken.Includenode
        {
            public IncludeNode(ActionToken token)
            {
                Token = token;
            }

            public override BasicResult.IncludeNode WaitCompletedSignal()
            {
                return new BasicActionResult.IncludeNode((InclusionResult)Token.WaitCompletedSignal());
            }

            public override bool IsStateActive
            {
                get { return Token.IsStateActive; }
            }
        }

        public class ListenControllerUpdate : BasicToken.ListenControllerUpdate
        {
            private List<ApplicationControllerUpdate> _receivedData;

            public ListenControllerUpdate()
            {
                _receivedData = new List<ApplicationControllerUpdate>();
                ListenControllerUpdateDelegate = (x) =>
                {
                    _receivedData.Add(new ApplicationControllerUpdate(x));
                };
            }

            public void InitToken(ActionToken token)
            {
                Token = token;
            }

            public Action<ApplicationControllerUpdateResult> ListenControllerUpdateDelegate { get; }

            public override BasicResult.ListenControllerUpdate WaitCompletedSignal()
            {
                Token.WaitCompletedSignal();
                return new BasicResult.ListenControllerUpdate(_receivedData);
            }
        }

        public class ListenData : BasicToken.ListenData
        {
            private List<AchData> _receivedData;

            public ListenData()
            {
                _receivedData = new List<AchData>();
                ListenDataDelegate = (achData) =>
                {
                    _receivedData.Add(new AchData(achData));
                };
            }

            public void InitToken(ActionToken token)
            {
                Token = token;
            }

            public ListenDataDelegate ListenDataDelegate { get; }

            public override BasicResult.ListenData WaitCompletedSignal()
            {
                Token.WaitCompletedSignal();
                return new BasicResult.ListenData(_receivedData);
            }
        }

        public class RemoveNode : BasicToken.RemoveNode
        {
            public RemoveNode(ActionToken token)
            {
                Token = token;
            }

            public override BasicResult.RemoveNode WaitCompletedSignal()
            {
                return new BasicActionResult.RemoveNode((AddRemoveNodeResult)Token.WaitCompletedSignal());
            }

            public override bool IsStateActive
            {
                get { return Token.IsStateActive; }
            }
        }

        public class SetLearnMode : BasicToken.SetLearnMode
        {
            private const int NODE_INFORMATION_TIMEOUT_MS = 10000;

            private readonly ManualResetEventSlim _nodeInformationWritten;
            private readonly Action _nodeInformationTimedOut;

            public SetLearnMode(ActionToken token)
            {
                Token = token;
            }

            /// <param name="nodeInformationWritten">
            /// Set once the NIF of the new inclusion state is written, which happens after the operation signals its completion.
            /// </param>
            /// <param name="nodeInformationTimedOut">Called when the NIF is not written in time.</param>
            public SetLearnMode(ActionToken token, ManualResetEventSlim nodeInformationWritten, Action nodeInformationTimedOut)
            {
                Token = token;
                _nodeInformationWritten = nodeInformationWritten;
                _nodeInformationTimedOut = nodeInformationTimedOut;
            }

            public override BasicResult.SetLearnMode WaitCompletedSignal()
            {
                ActionResult result = Token.WaitCompletedSignal();
                WaitNodeInformationWritten();
                return new BasicActionResult.SetLearnMode((SetLearnModeResult)result);
            }

            /// <summary>
            /// Waits until the NIF of the new inclusion state is written, for at most <see cref="NODE_INFORMATION_TIMEOUT_MS"/>.
            /// Returns at once for a Learn Mode that writes none. Reports a NIF that is not written in time.
            /// </summary>
            internal void WaitNodeInformationWritten()
            {
                if (_nodeInformationWritten != null && !_nodeInformationWritten.Wait(NODE_INFORMATION_TIMEOUT_MS))
                {
                    _nodeInformationTimedOut?.Invoke();
                }
            }
        }

        public class ExpectSmartStartAdd : BasicToken.ExpectSmartStartAdd
        {
            internal SimpleToken Token { get; set; }
            public ExpectSmartStartAdd(SimpleToken token)
            {
                Token = token;
            }

            public override BasicResult.ExpectSmartStartAdd WaitCompletedSignal()
            {
                return new BasicActionResult.ExpectSmartStartAdd((InclusionResult)Token.WaitCompletedSignal());
            }
        }
    }
}
