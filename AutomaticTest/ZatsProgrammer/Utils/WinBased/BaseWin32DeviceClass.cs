// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Framework
{
    public class BaseWin32DeviceClass
    {
        #region Private Fields
        private ushort? mAvailability = null;
        private string mCaption = null;
        private uint? mConfigManagerErrorCode = null;
        private bool? mConfigManagerUserConfig = null;
        private string mCreationClassName = null;
        private string mDescription = null;
        private string mDeviceID = null;
        private string mSystemName = null;
        private string mSystemCreationClassName = null;
        private string mName = null;
        private string mPNPDeviceID = null;
        private UInt16[] mPowerManagementCapabilities = null;
        private bool? mPowerManagementSupported = null;
        private string mStatus = null;
        private ushort? mStatusInfo = null;
        #endregion
        #region Public Properties

        private string mHardwareID;
        public string HardwareID
        {
            get { return mHardwareID; }
            set
            {
                mHardwareID = value;
            }
        }

        /// <summary>
        /// Gets the availability.
        /// </summary>
        /// <value>The availability.</value>
        public ushort? Availability
        {
            get { return mAvailability; }
            set { mAvailability = value; }
        }
        /// <summary>
        /// Gets the caption.
        /// </summary>
        /// <value>The caption.</value>
        public string Caption
        {
            get { return mCaption; }
            set { mCaption = value; }
        }
        /// <summary>
        /// Gets the config manager error code.
        /// </summary>
        /// <value>The config manager error code.</value>
        public uint? ConfigManagerErrorCode
        {
            get { return mConfigManagerErrorCode; }
            set { mConfigManagerErrorCode = value; }
        }
        /// <summary>
        /// Gets the config manager user config.
        /// </summary>
        /// <value>The config manager user config.</value>
        public bool? ConfigManagerUserConfig
        {
            get { return mConfigManagerUserConfig; }
            set { mConfigManagerUserConfig = value; }
        }
        /// <summary>
        /// Gets the name of the creation class.
        /// </summary>
        /// <value>The name of the creation class.</value>
        public string CreationClassName
        {
            get { return mCreationClassName; }
            set { mCreationClassName = value; }
        }
        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return mDescription; }
            set { mDescription = value; }
        }
        /// <summary>
        /// Gets the device ID.
        /// </summary>
        /// <value>The device ID.</value>
        public string DeviceID
        {
            get { return mDeviceID; }
            set { mDeviceID = value; }
        }
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return mName; }
            set { mName = value; }
        }
        /// <summary>
        /// Gets the PNP device ID.
        /// </summary>
        /// <value>The PNP device ID.</value>
        public string PNPDeviceID
        {
            get { return mPNPDeviceID; }
            set { mPNPDeviceID = value; }
        }
        /// <summary>
        /// Gets the power management capabilities.
        /// </summary>
        /// <value>The power management capabilities.</value>
        public UInt16[] PowerManagementCapabilities
        {
            get { return mPowerManagementCapabilities; }
            set { mPowerManagementCapabilities = value; }
        }
        /// <summary>
        /// Gets the power management supported.
        /// </summary>
        /// <value>The power management supported.</value>
        public bool? PowerManagementSupported
        {
            get { return mPowerManagementSupported; }
            set { mPowerManagementSupported = value; }
        }
        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <value>The status.</value>
        public string Status
        {
            get { return mStatus; }
            set { mStatus = value; }
        }
        /// <summary>
        /// Gets the status info.
        /// </summary>
        /// <value>The status info.</value>
        public ushort? StatusInfo
        {
            get { return mStatusInfo; }
            set { mStatusInfo = value; }
        }
        /// <summary>
        /// Gets the name of the system creation class.
        /// </summary>
        /// <value>The name of the system creation class.</value>
        public string SystemCreationClassName
        {
            get { return mSystemCreationClassName; }
            set { mSystemCreationClassName = value; }
        }
        /// <summary>
        /// Gets the name of the system.
        /// </summary>
        /// <value>The name of the system.</value>
        public string SystemName
        {
            get { return mSystemName; }
            set { mSystemName = value; }
        }
        #endregion
        public override string ToString()
        {
            if (this.Description != null)
            {
                return string.Format("{0} - {1}", this.Caption, this.Description);
            }
            else
            {
                return string.Format("{0}", this.Caption);
            }
        }
    }
}
