// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    public interface ITestInterfaceService
    {
        int ProgramBootLoader(Alias deviceNumber, BootloaderPath regexFilter);
        void SetLearnMode(Alias deviceNumber);
        void ButtonSinglePress_UserInput(Alias deviceNumber, byte ButtonNumber);
        ServiceSettings Settings { get; set; }
        ButtonActions ButtonActions { get; }
        LibTypes GetLibrary(Alias endDevice);
        byte[] GetDSK(Alias deviceNumber, int numberOfBytes);
        BasicResult.AddNode AddNode(Alias primary, Alias doorLock, Alias deviceDebugNumber = new Alias());
        BasicResult.IncludeNode IncludeNode(Alias controllerNumber, Alias testInterfaceNumber, Alias deviceDebugNumber = new Alias());
        BasicResult.RemoveNode ExcludeNode(Alias controllerNumber, Alias testInterfaceNumber);
        BasicResult.RemoveNode ExcludeNode(Alias controllerNumber, Alias testInterfaceNumber, Modes mode);
        BasicResult.IncludeNodeWithLearnMode ReplaceFailedNode(Alias controllerNumber, Node node, Alias testInterfaceNumber);
        /// <summary>
        /// Send prog enable and prog disable
        /// </summary>
        /// <param name="deviceNumber">Device Alias</param>
        void SoftReset(Alias deviceNumber);
        /// <summary>
        /// Parralel reset several devices
        /// </summary>
        /// <param name="deviceNumbers"></param>
        void SoftReset(Alias[] deviceNumbers);
        //BasicResult.ReturnValue GetLedState(Alias deviceNumber, byte? ledMask = null);

#if NETCOREAPP
        void SendNodeInfo(Alias deviceNumber, byte destNodeId, TransmitOptions txOptions);
#endif
    }
}
