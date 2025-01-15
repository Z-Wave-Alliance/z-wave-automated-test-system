// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Devices;

namespace ZWave
{
    /// <summary>
    /// Provides the features required to support scenarios when creating Z-Wave Device classes.
    /// </summary>
    public interface IApplicationLayer
    {
        /// <summary>
        /// Inits instance with the specified session layer.
        /// </summary>
        /// <param name="sessionLayer">The session layer.</param>
        /// <param name="useExternalCommandClassesStorage">if set to <c>true</c> [use external command classes storage].</param>
        void Init(ISessionLayer sessionLayer);
        /// <summary>
        /// Creates the Generic Device class.
        /// </summary>
        /// <returns></returns>
        IDevice CreateDevice();
        /// <summary>
        /// Creates the Flash Settings class.
        /// </summary>
        /// <returns></returns>
        IFlashSettings CreateFlashSettings();
    }
}
