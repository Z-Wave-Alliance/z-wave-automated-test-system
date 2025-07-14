// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zats.Services
{
    public interface IInstantAsyncService
    {
        BasicToken.AddNode AddNode(Alias controllerNumber, Modes mode, int timeoutMs);
        BasicToken.AddNode AddNode(Alias controllerNumber, Modes mode);
        BasicToken.AddNode AddNode(Alias controllerNumber);
        BasicToken.AddNode ControllerChange(Alias controllerNumber, ControllerChangeModes mode, int timeoutMs);
        BasicToken.RemoveNode RemoveNode(Alias controllerNumber, Modes mode, int timeoutMs);
        BasicToken.RemoveNode RemoveNode(Alias controllerNumber);
        BasicToken.RemoveNode RemoveNode(Alias controllerNumber, Modes mode);
        BasicToken.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, byte[] expectedData, int timeoutMs);
        BasicToken.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, byte[] expectedData);
        BasicToken.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, byte[] expectedData, int timeoutMs);
        BasicToken.RequestNetworkUpdate RequestNetworkUpdate(Alias senderNumber);
        BasicToken.RequestNodeNeighborUpdate RequestNodeNeighborUpdate(Alias controllerNumber, Alias nodeNumber);
        BasicToken.SendData SendData(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions);
        BasicToken.SendData SendData(Alias senderNumber, Alias receiverNumber, byte[] data);
        BasicToken.Void SendDataAbort(Alias deviceNumber);
        BasicToken.Void SetDefault(Alias senderNumber);
        BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber);
        BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode);
        BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode, int timeoutMs);
        BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode, bool isNonSecure, int timeoutMs);
        BasicToken.Void SetRFReceiveMode(Alias deviceNumber, bool mode);
        BasicToken.Void SetSleepMode(Alias deviceNumber, SleepModes mode, byte intEnable);
    }
}
