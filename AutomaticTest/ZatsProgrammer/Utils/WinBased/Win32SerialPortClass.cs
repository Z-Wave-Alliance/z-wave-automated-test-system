// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Framework
{
    public class Win32SerialPortClass : BaseWin32DeviceClass
    {
        #region Private Fields
        private bool? mBinary = null;
        private uint? mMaxBaudRate = null;
        private uint? mMaximumInputBufferSize = null;
        private uint? mMaximumOutputBufferSize = null;
        private bool? mOSAutoDiscovered = null;
        private string mProviderType = null;
        private bool? mSettableBaudRate = null;
        private bool? mSettableDataBits = null;
        private bool? mSettableFlowControl = null;
        private bool? mSettableParity = null;
        private bool? mSettableParityCheck = null;
        private bool? mSettableRLSD = null;
        private bool? mSettableStopBits = null;
        private bool? mSupports16BitMode = null;
        private bool? mSupportsDTRDSR = null;
        private bool? mSupportsElapsedTimeouts = null;
        private bool? mSupportsIntTimeouts = null;
        private bool? mSupportsParityCheck = null;
        private bool? mSupportsRLSD = null;
        private bool? mSupportsRTSCTS = null;
        private bool? mSupportsSpecialCharacters = null;
        private bool? mSupportsXOnXOff = null;
        private bool? mSupportsXOnXOffSet = null;
        #endregion
        #region Public Properties
        /// <summary>
        /// Gets the binary.
        /// </summary>
        /// <value>The binary.</value>
        public bool? Binary
        {
            get { return mBinary; }
            set { mBinary = value; }
        }
        /// <summary>
        /// Gets the max baud rate.
        /// </summary>
        /// <value>The max baud rate.</value>
        public uint? MaxBaudRate
        {
            get { return mMaxBaudRate; }
            set { mMaxBaudRate = value; }
        }
        /// <summary>
        /// Gets the maximum size of the input buffer.
        /// </summary>
        /// <value>The maximum size of the input buffer.</value>
        public uint? MaximumInputBufferSize
        {
            get { return mMaximumInputBufferSize; }
            set { mMaximumInputBufferSize = value; }
        }
        /// <summary>
        /// Gets the maximum size of the output buffer.
        /// </summary>
        /// <value>The maximum size of the output buffer.</value>
        public uint? MaximumOutputBufferSize
        {
            get { return mMaximumOutputBufferSize; }
            set { mMaximumOutputBufferSize = value; }
        }
        /// <summary>
        /// Gets the OS auto discovered.
        /// </summary>
        /// <value>The OS auto discovered.</value>
        public bool? OSAutoDiscovered
        {
            get { return mOSAutoDiscovered; }
            set { mOSAutoDiscovered = value; }
        }
        /// <summary>
        /// Gets the type of the provider.
        /// </summary>
        /// <value>The type of the provider.</value>
        public string ProviderType
        {
            get { return mProviderType; }
            set { mProviderType = value; }
        }
        /// <summary>
        /// Gets the settable baud rate.
        /// </summary>
        /// <value>The settable baud rate.</value>
        public bool? SettableBaudRate
        {
            get { return mSettableBaudRate; }
            set { mSettableBaudRate = value; }
        }
        /// <summary>
        /// Gets the settable data bits.
        /// </summary>
        /// <value>The settable data bits.</value>
        public bool? SettableDataBits
        {
            get { return mSettableDataBits; }
            set { mSettableDataBits = value; }
        }
        /// <summary>
        /// Gets the settable flow control.
        /// </summary>
        /// <value>The settable flow control.</value>
        public bool? SettableFlowControl
        {
            get { return mSettableFlowControl; }
            set { mSettableFlowControl = value; }
        }
        /// <summary>
        /// Gets the settable parity.
        /// </summary>
        /// <value>The settable parity.</value>
        public bool? SettableParity
        {
            get { return mSettableParity; }
            set { mSettableParity = value; }
        }
        /// <summary>
        /// Gets the settable parity check.
        /// </summary>
        /// <value>The settable parity check.</value>
        public bool? SettableParityCheck
        {
            get { return mSettableParityCheck; }
            set { mSettableParityCheck = value; }
        }
        /// <summary>
        /// Gets the settable RLSD.
        /// </summary>
        /// <value>The settable RLSD.</value>
        public bool? SettableRLSD
        {
            get { return mSettableRLSD; }
            set { mSettableRLSD = value; }
        }
        /// <summary>
        /// Gets the settable stop bits.
        /// </summary>
        /// <value>The settable stop bits.</value>
        public bool? SettableStopBits
        {
            get { return mSettableStopBits; }
            set { mSettableStopBits = value; }
        }
        /// <summary>
        /// Gets the supports 16 bit mode.
        /// </summary>
        /// <value>The supports 16 bit mode.</value>
        public bool? Supports16BitMode
        {
            get { return mSupports16BitMode; }
            set { mSupports16BitMode = value; }
        }
        /// <summary>
        /// Gets the supports DTRDSR.
        /// </summary>
        /// <value>The supports DTRDSR.</value>
        public bool? SupportsDTRDSR
        {
            get { return mSupportsDTRDSR; }
            set { mSupportsDTRDSR = value; }
        }
        /// <summary>
        /// Gets the supports elapsed timeouts.
        /// </summary>
        /// <value>The supports elapsed timeouts.</value>
        public bool? SupportsElapsedTimeouts
        {
            get { return mSupportsElapsedTimeouts; }
            set { mSupportsElapsedTimeouts = value; }
        }
        /// <summary>
        /// Gets the supports int timeouts.
        /// </summary>
        /// <value>The supports int timeouts.</value>
        public bool? SupportsIntTimeouts
        {
            get { return mSupportsIntTimeouts; }
            set { mSupportsIntTimeouts = value; }
        }
        /// <summary>
        /// Gets the supports parity check.
        /// </summary>
        /// <value>The supports parity check.</value>
        public bool? SupportsParityCheck
        {
            get { return mSupportsParityCheck; }
            set { mSupportsParityCheck = value; }
        }
        /// <summary>
        /// Gets the supports RLSD.
        /// </summary>
        /// <value>The supports RLSD.</value>
        public bool? SupportsRLSD
        {
            get { return mSupportsRLSD; }
            set { mSupportsRLSD = value; }
        }
        /// <summary>
        /// Gets the supports RTSCTS.
        /// </summary>
        /// <value>The supports RTSCTS.</value>
        public bool? SupportsRTSCTS
        {
            get { return mSupportsRTSCTS; }
            set { mSupportsRTSCTS = value; }
        }
        /// <summary>
        /// Gets the supports special characters.
        /// </summary>
        /// <value>The supports special characters.</value>
        public bool? SupportsSpecialCharacters
        {
            get { return mSupportsSpecialCharacters; }
            set { mSupportsSpecialCharacters = value; }
        }
        /// <summary>
        /// Gets the supports XOnXOff.
        /// </summary>
        /// <value>The supports XOnXOff.</value>
        public bool? SupportsXOnXOff
        {
            get { return mSupportsXOnXOff; }
            set { mSupportsXOnXOff = value; }
        }
        /// <summary>
        /// Gets the supports XOnXOffSet.
        /// </summary>
        /// <value>The supports XOnXOffSet.</value>
        public bool? SupportsXOnXOffSet
        {
            get { return mSupportsXOnXOffSet; }
            set { mSupportsXOnXOffSet = value; }
        }
        #endregion
    }
}
