// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Enums;

namespace ZWave.Devices
{
    /// <summary>
    /// This interface provides methods that allow the client program using Z-Wave Memory Serial API.
    /// </summary>
    public interface IDeviceFlash
    {
        /// <summary>
        /// Occurs when progress changed.
        /// </summary>
        event Events.ProgressChangedEventHandler ProgressChanged;
        /// <summary>
        /// Gets or sets the pages collection.
        /// </summary>
        /// <value>The pages.</value>
        IList<IFlashPage> Pages
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the device.
        /// </summary>
        /// <value>The device.</value>
        IDevice Device
        {
            get;
            set;
        }
        /// <summary>
        /// Erases the flash.
        /// </summary>
        /// <param name="isAsic">if set to <c>true</c> is Asic.</param>
        /// <returns></returns>
        bool Erase(bool isAsic);
        /// <summary>
        /// Reads the flash.
        /// </summary>
        /// <param name="isAsic">if set to <c>true</c> is Asic.</param>
        void Read(bool isAsic);
        /// <summary>
        /// Reads the <see cref="IFlashPage"/>.
        /// </summary>
        /// <param name="isAsic">if set to <c>true</c> is Asic.</param>
        /// <param name="pageIndex">Index of the page.</param>
        /// <returns></returns>
        IFlashPage ReadPage(bool isAsic, int pageIndex);
        /// <summary>
        /// Erases the page. This function is not aviable for ZW010x and returns failure for this chip.
        /// </summary>
        /// <param name="pageIndex">Index of the page.</param>
        /// <returns></returns>
        bool ErasePage(int pageIndex);
        /// <summary>
        /// Writes the flash.
        /// </summary>
        /// <param name="isAsic">if set to <c>true</c> is Asic.</param>
        /// <param name="data">The data.</param>
        /// <param name="flashSettings">The flash settings.</param>
        /// <param name="verify">if set to <c>true</c> verify.</param>
        /// <returns></returns>
        bool Write(bool isAsic, byte[] data, IFlashSettings flashSettings, bool verify);
        /// <summary>
        /// Writes the <see cref="IFlashPage" />.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="isAsic">if set to <c>true</c> is Asic.</param>
        /// <param name="verify">if set to <c>true</c> verify.</param>
        /// <returns></returns>
        bool WritePage(IFlashPage page, bool isAsic, bool verify);
        /// <summary>
        /// Generates the output.
        /// </summary>
        /// <param name="isAsic">if set to <c>true</c> is Asic.</param>
        /// <param name="flashDataRaw">The flash data raw.</param>
        /// <param name="pagesCount">The pages count.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <returns></returns>
        List<IFlashPage> GenerateOutput(bool isAsic, byte[] flashDataRaw, int pagesCount, int pageSize);
        /// <summary>
        /// Gets the bytes buffer.
        /// </summary>
        /// <returns></returns>
        byte[] GetBytes();
        /// <summary>
        /// Compares the flash.
        /// </summary>
        /// <param name="isAsic">if set to <c>true</c> is Asic.</param>
        /// <param name="flash">The flash.</param>
        /// <param name="flashSettings">The flash settings.</param>
        /// <param name="flashDataRaw">The flash data raw.</param>
        /// <param name="diffs">The different data. (PageIndex, HEX File page, Flash Page)</param>
        /// <returns></returns>
        bool Compare(bool isAsic, IDeviceFlash flash, IFlashSettings flashSettings, byte[] flashDataRaw, out SortedList<int, List<byte[]>> diffs);
        /// <summary>
        /// Runs CRC check in 500 series.
        /// </summary>
        /// <returns></returns>
        bool RunCRCCheck();
        /// <summary>
        /// Runs CheckState in 500 series.
        /// </summary>
        /// <returns></returns>
        byte CheckState();
        /// <summary>
        /// Check APM mode on the ZW050x chip.
        /// </summary>
        /// <returns></returns>
        bool IsInAutoProgMode();
    }
}
