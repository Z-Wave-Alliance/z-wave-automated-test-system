// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using Zats.Services.Enums;

namespace Zats.Services
{
    public interface IAsyncService
    {
        BasicToken.AddNode AddNode(Alias controllerNumber, Modes mode, bool isModeStopEnabled, int timeoutMs);
        BasicToken.AddNode AddNode(Alias controllerNumber, Modes mode, int timeoutMs);
        BasicToken.AddNode AddNode(Alias controllerNumber, Modes mode);
        BasicToken.AddNode AddNode(Alias controllerNumber);
        BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, Modes mode, int dsk);
        BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, int dsk);
        BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, Modes mode, int dsk, int timeoutMs);
        BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, byte[] dsk);
        BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, Modes mode, byte[] dsk, int timeoutMs);
        BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, Modes mode, int dsk, SecuritySchemes[] grantSchemes, int timeoutMs);
        BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, Modes mode, byte[] dsk, SecuritySchemes[] grantSchemes, int timeoutMs);
        void CancelToken(Alias deviceNumber, _ActionToken token);
        BasicToken.AddNode ControllerChange(Alias controllerNumber, ControllerChangeModes mode, int timeoutMs);
        BasicToken.AddNode ControllerChange(Alias controllerNumber, ControllerChangeModes mode, bool isModeStopEnabled, int timeoutMs);
        BasicToken.ExpectControllerUpdate ExpectControllerUpdate(Alias controllerNumber, ControllerUpdateStatuses updateStatus, int timeoutMs);
        BasicToken.ExpectData ExpectData(Alias deviceNumber, byte[] expectedData);
        BasicToken.ExpectData ExpectData(Alias deviceNumber, byte[] expectedData, int timeoutMs);
        BasicToken.ExpectData ExpectData(Alias deviceNumber, byte[] expectedData, int numberOfBytesToCompare, int timeoutMs);
        BasicToken.ExpectData ExpectData(Alias deviceNumber, Node srcNode, byte[] expectedData, int timeoutMs);
        BasicToken.ExpectData ExpectData(Alias deviceNumber, Node srcNode, byte[] expectedData, SecurityS2ExtensionTypes[] securityS2ExtensionType, int timeoutMs);
        BasicToken.ListenControllerUpdate ListenControllerUpdate(Alias deviceNumber);
        BasicToken.ListenData ListenData(Alias deviceNumber);
        BasicToken.ListenData ListenData(Alias deviceNumber, byte[] compareData);
        BasicToken.RemoveNode RemoveNode(Alias controllerNumber);
        BasicToken.RemoveNode RemoveNode(Alias controllerNumber, Modes mode);
        BasicToken.RemoveNode RemoveNode(Alias controllerNumber, Modes mode, int timeoutMs);
        BasicToken.RemoveNode RemoveNodeIdFromNetwork(Alias controllerNumber, Node node, Modes mode, int timeoutMs);
        BasicToken.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, byte[] expectedData, int timeoutMs);
        BasicToken.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, byte[] expectedData);
        BasicToken.ExpectZW ExpectZW(Alias deviceNumber, string filter, int timeoutMs);
        BasicToken.ExpectZW ExpectZW(Alias deviceNumber, IEnumerable<string> filter, int timeoutMs);
        BasicToken.ExpectZW ExpectZW(Alias deviceNumber, ByteData[] filter, int timeoutMs);
        BasicToken.ExpectZW ExpectZW(Alias deviceNumber, IEnumerable<ByteData[]> filter, int timeoutMs);
        BasicToken.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, byte[] expectedData, int timeoutMs);
        BasicToken.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, SecuritySchemes scheme, byte[] expectedData);
        BasicToken.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2, byte[] expectedData, int timeoutMs);
        BasicToken.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, SecuritySchemes scheme, byte[] expectedData, int timeoutMs);
        BasicToken.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, byte[] expectedData, int timeoutMs);
        BasicToken.RequestNetworkUpdate RequestNetworkUpdate(Alias senderNumber);
        BasicToken.RequestNodeInfo RequestNodeInfo(Alias deviceNumber, Node nodeNumberID);
        BasicToken.RequestNodeNeighborUpdate RequestNodeNeighborUpdate(Alias controllerNumber, Alias nodeNumber);
        BasicToken.SendData SendData(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions);
        BasicToken.Void NoiseData(Alias alias, Node node, byte[] data, TransmitOptions txOptions, Action<BasicResult.SendData> sendCallback, int interval, int timeout);
        BasicToken.Void NoiseData(Alias alias, Node[] nodes, byte[] data, TransmitOptions txOptions, Action<BasicResult.SendData> sendCallback, int interval, int timeout);
        BasicToken.Void NoiseData(Alias alias, Node node, byte[] data, TransmitOptions txOptions, byte[] expectData, Action<BasicResult.RequestData> requestCallback, int interval, int timeout);
        BasicToken.Void NoiseData(Alias alias, Node[] nodes, byte[] data, TransmitOptions txOptions, byte[] expectData, Action<BasicResult.RequestData> requestCallback, int interval, int timeout);
        BasicToken.Void NoiseDataEx(Alias deviceNumber, Node[] nodes, byte[] data, TransmitOptions txOptions, byte[] expectData, Action<BasicResult.RequestData> requestCallback, int interval, int timeout, SecuritySchemes secSch, bool isPoisson);
        BasicToken.SendData SendData(Alias senderNumber, Alias receiverNumber, byte[] data);
        BasicToken.SendData SendData(Alias senderNumber, Node receiver, byte[] data, TransmitOptions txOptions);
        BasicToken.SendData SendDataMulti(Alias senderNumber, Node[] nodes, byte[] data, SubstituteFlags Flags);
        BasicToken.SendData SendDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2);
        BasicToken.SendData SendDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, SecuritySchemes scheme);
        BasicToken.SendData SendDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme);
        BasicToken.SendData SendDataEx(Alias senderNumber, Node receiver, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2);
        BasicToken.SendData SendDataEx(Alias senderNumber, Node receiver, byte[] data, SecuritySchemes scheme);
        BasicToken.SendData SendDataMultiEx(Alias senderNumber, byte[] data, SecuritySchemes scheme, byte groupId);
        BasicToken.SendData SendDataMultiEx(Alias senderNumber, byte[] data, TransmitOptions txOptions, SecuritySchemes scheme, byte groupId);
        BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode);
        BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber);
        BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode, int timeoutMs);
        BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode, bool isNonSecure, int timeoutMs);        
        BasicToken.ExpectSmartStartAdd ExpectSmartStartAdd(Alias controllerNumber, int timeoutMs);
        bool WaitAssignStatus(Alias deviceNumber, int timeoutMs, AssignStatuses status);
        bool WaitCompletedSignal(_ActionToken token);
        bool WaitNodeStatus(Alias deviceNumber, int timeoutMs, NodeStatuses status);
        BasicToken.Void SetRFReceiveMode(Alias DeviceNumber, bool mode);
        BasicToken.Includenode IncludeNode(Alias controllerNumber, byte[] dks, Modes addMode, int timeoutMs, SetupNodeLifelineSettings setupNodeLifelineSettings = SetupNodeLifelineSettings.Default);
        BasicToken.AddNode ReplaceFailedNode(Alias controllerNumber, Node NodeId, byte[] dsk, Modes addMode, int timeoutMs);
        BasicToken.SetLearnMode WaitForLearnMode(Alias deviceNumber, int timeoutMs);
    }
}
