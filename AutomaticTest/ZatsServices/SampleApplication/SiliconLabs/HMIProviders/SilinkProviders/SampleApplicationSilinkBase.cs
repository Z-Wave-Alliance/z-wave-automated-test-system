// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using ZWave.TextApplication;

namespace Zats.Services.SampleApplication.SiliconLabs.SilinkProviders
{
    class SampleApplicationSilinkBase : ISampleApplicationProviderBase
    {
        protected readonly IIOHandler _silinkAdminIOHandler;

        public SampleApplicationSilinkBase(TextDevice devBoardDevice)
        {
            _silinkAdminIOHandler = new SilinkAdminIOHandler(devBoardDevice);
        }

        /// <summary>
        /// Click PB0 on a WSKT board to activate learn mode
        /// </summary>
        public void LearnMode()
        {
            _silinkAdminIOHandler.ToggleButton((byte)SilinkButtons.BTN_1);
        }

        /// <summary>
        /// Press and hold PB1 to activate self reset on a device
        /// </summary>
        public void Reset()
        {
            _silinkAdminIOHandler.HoldButton((byte)SilinkButtons.BTN_1, 5);
        }

    }
}