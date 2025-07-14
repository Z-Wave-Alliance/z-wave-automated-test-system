// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using Utils.UI.Enums;

namespace Zats.Services.PCController
{
    public class PCControllerInstantAsyncService : ServiceBase, IInstantAsyncService
    {
        protected override void Log(string text, LogLevels logLevel)
        {
            throw new NotImplementedException();
        }

        public BasicToken.AddNode AddNode(Alias controllerNumber, Modes mode, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.AddNode AddNode(Alias controllerNumber, Modes mode)
        {
            throw new NotImplementedException();
        }

        public BasicToken.AddNode AddNode(Alias controllerNumber)
        {
            throw new NotImplementedException();
        }

        public BasicToken.AddNode ControllerChange(Alias controllerNumber, ControllerChangeModes mode, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.RemoveNode RemoveNode(Alias controllerNumber, Modes mode, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.RemoveNode RemoveNode(Alias controllerNumber)
        {
            throw new NotImplementedException();
        }

        public BasicToken.RemoveNode RemoveNode(Alias controllerNumber, Modes mode)
        {
            throw new NotImplementedException();
        }

        public BasicToken.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, byte[] expectedData, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, byte[] expectedData)
        {
            throw new NotImplementedException();
        }

        public BasicToken.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, byte[] expectedData, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.RequestNetworkUpdate RequestNetworkUpdate(Alias senderNumber)
        {
            throw new NotImplementedException();
        }

        public BasicToken.RequestNodeNeighborUpdate RequestNodeNeighborUpdate(Alias controllerNumber, Alias nodeNumber)
        {
            throw new NotImplementedException();
        }

        public BasicToken.SendData SendData(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions)
        {
            throw new NotImplementedException();
        }

        public BasicToken.SendData SendData(Alias senderNumber, Alias receiverNumber, byte[] data)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Void SendDataAbort(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Void SetDefault(Alias senderNumber)
        {
            throw new NotImplementedException();
        }

        public BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode)
        {
            throw new NotImplementedException();
        }

        public BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode, bool isNonSecure, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Void SetRFReceiveMode(Alias deviceNumber, bool mode)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Void SetSleepMode(Alias deviceNumber, SleepModes mode, byte intEnable)
        {
            throw new NotImplementedException();
        }
    }
}
