// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using ZWave;
using ZWave.BasicApplication.Operations;

namespace Zats.Services
{
    public class BasicToken
    {
        public abstract class Void : _ActionToken
        {
            public abstract BasicResult.Void WaitCompletedSignal();
        }

        public abstract class ExpectZW : _ActionToken
        {
            public abstract BasicResult.ExpectZW WaitCompletedSignal();
            public abstract bool IsStateActive { get; }
        }

        public class RequestData : _ActionToken
        {
            public RequestData(ActionToken token)
                : base(token)
            { }

            public BasicResult.RequestData WaitCompletedSignal()
            {
                return new BasicActionResult.RequestData((RequestDataResult)Token.WaitCompletedSignal());
            }
        }

        public class ResponseData : _ActionToken
        {
            public ResponseData(ActionToken token)
                : base(token)
            { }

            public BasicResult.Void WaitCompletedSignal()
            {
                return new BasicActionResult.Void(Token.WaitCompletedSignal());
            }
        }

        public abstract class ExpectData : _ActionToken
        {
            public abstract BasicResult.ExpectData WaitCompletedSignal();
        }

        public abstract class ListenData : _ActionToken
        {
            //public abstract void InitToken(ActionToken token);
            //public abstract ListenDataDelegate ListenDataDelegate { get; }
            public abstract BasicResult.ListenData WaitCompletedSignal();
        }

        public abstract class ListenControllerUpdate : _ActionToken
        {
            //public abstract void InitToken(ActionToken token);
            //public abstract ListenDataDelegate ListenDataDelegate { get; }
            public abstract BasicResult.ListenControllerUpdate WaitCompletedSignal();
        }

        public class ExpectControllerUpdate : _ActionToken
        {
            public ExpectControllerUpdate(ActionToken token)
                : base(token)
            { }

            public BasicResult.ExpectControllerUpdate WaitCompletedSignal()
            {
                return new BasicResult.ExpectControllerUpdate((ApplicationControllerUpdateResult)Token.WaitCompletedSignal());
            }
        }

        public abstract class AddNode : _ActionToken
        {
            public abstract BasicResult.AddNode WaitCompletedSignal();
            public abstract bool IsStateActive { get; }
        }

        public abstract class Includenode : _ActionToken
        {
            public abstract BasicResult.IncludeNode WaitCompletedSignal();
            public abstract bool IsStateActive { get; }
        }

        public abstract class RemoveNode : _ActionToken
        {
            public abstract BasicResult.RemoveNode WaitCompletedSignal();
            public abstract bool IsStateActive { get; }
        }

        public abstract class SetLearnMode : _ActionToken
        {
            public abstract BasicResult.SetLearnMode WaitCompletedSignal();
        }

        public class SendData : _ActionToken
        {
            public SendData(ActionToken token)
                : base(token)
            { }

            public BasicResult.SendData WaitCompletedSignal()
            {
                return new BasicActionResult.SendData((SendDataResult)Token.WaitCompletedSignal());
            }
        }

        public class RequestNodeNeighborUpdate : _ActionToken
        {
            public RequestNodeNeighborUpdate(ActionToken token)
                : base(token)
            { }

            public BasicResult.RequestNodeNeighborUpdate WaitCompletedSignal()
            {
                return new BasicActionResult.RequestNodeNeighborUpdate((RequestNodeNeighborUpdateResult)Token.WaitCompletedSignal());
            }

        }

        public class RequestNodeInfo : _ActionToken
        {
            public RequestNodeInfo(ActionToken token)
                : base(token)
            { }

            public BasicResult.RequestNodeInfo WaitCompletedSignal()
            {
                return new BasicActionResult.RequestNodeInfo((RequestNodeInfoResult)Token.WaitCompletedSignal());
            }
        }

        public class RequestNetworkUpdate : _ActionToken
        {
            public RequestNetworkUpdate(ActionToken token)
                : base(token)
            { }

            public BasicResult.RequestNetworkUpdate WaitCompletedSignal()
            {
                return new BasicActionResult.RequestNetworkUpdate((RequestNetworkUpdateResult)Token.WaitCompletedSignal());
            }
        }

        public abstract class ExpectSmartStartAdd : _ActionToken
        {
            public abstract BasicResult.ExpectSmartStartAdd WaitCompletedSignal();
        }
    }
}
