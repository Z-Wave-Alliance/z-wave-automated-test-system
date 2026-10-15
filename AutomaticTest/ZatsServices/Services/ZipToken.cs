// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using ZWave;
using ZWave.ZipApplication.Operations;

namespace Zats.Services
{
    public class ZipToken
    {
        public class AddNode : _ActionToken
        {
            public AddNode(ActionToken token)
                : base(token)
            { }

            public ZipResult.AddNode WaitCompletedSignal()
            {
                return new ZipResult.AddNode((AddNodeResult)Token.WaitCompletedSignal());
            }
        }

        public class RemoveNode : _ActionToken
        {
            public RemoveNode(ActionToken token)
                : base(token)
            { }

            public ZipResult.RemoveNode WaitCompletedSignal()
            {
                return new ZipResult.RemoveNode((RemoveNodeResult)Token.WaitCompletedSignal());
            }
        }

        public class LearnMode : _ActionToken
        {
            public LearnMode(ActionToken token)
                : base(token)
            { }

            public ZipResult.LearnMode WaitCompletedSignal()
            {
                return new ZipResult.LearnMode((SetLearnModeResult)Token.WaitCompletedSignal());
            }
        }

        public class ExpectData : _ActionToken
        {
            public ExpectData(ActionToken token)
                : base(token)
            { }

            public ZipResult.ExpectData WaitCompletedSignal()
            {
                ZipResult.ExpectData WaitCompletedReturnVale = new ZipResult.ExpectData(
                    (ExpectDataResult)Token.WaitCompletedSignal());
                Token.Complete();
                return WaitCompletedReturnVale;
            }
        }

        public class SendData : _ActionToken
        {
            public SendData(ActionToken token)
                : base(token)
            { }

            public ZipResult.SendData WaitCompletedSignal()
            {
                return new ZipResult.SendData((SendDataResult)Token.WaitCompletedSignal());
            }
        }

        public class Void : _ActionToken
        {
            public Void(ActionToken token)
                : base(token)
            { }            
        }
    }
}
