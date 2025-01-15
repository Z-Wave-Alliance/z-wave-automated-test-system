// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using ZWave.Programmer.Classes;
using ZWave.Enums;
using System.IO;
using ZWave.Framework;
using ZWave.Devices;

namespace ZWave.Programmer.Models
{
    /// <summary>
    /// DocumentModel class. Implements application model class.
    /// </summary>
    public class DocumentModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentModel"/> class.
        /// </summary>
        public DocumentModel()
        {
            IsGenerateSecurity2Keys = true;
        }

#if NETCOREAPP
        public string PortInfo { get; set; }
#else
        /// <summary>
        /// Gets or sets the Port Info.
        /// </summary>
        /// <value>The port info.</value>
        public Win32PnPEntityClass PortInfo { get; set; }
#endif

        /// <summary>
        /// Gets or sets a value indicating whether application is in ASIC mode.
        /// </summary>
        /// <value><c>true</c> if application is in ASIC mode; otherwise, <c>false</c>.</value>
        public bool IsAsic { get; set; } = true;

        private ProgrammerSettings mSettings;
        /// <summary>
        /// Gets or sets the Settings.
        /// </summary>
        /// <value>The Settings.</value>
        public ProgrammerSettings Settings
        {
            get
            {
                if (mSettings == null)
                {
                    mSettings = new ProgrammerSettings();
                    mSettings.Load();

                }
                return mSettings;
            }
            set
            {
                mSettings = value;
            }
        }
        /// <summary>
        /// Gets or sets the type of the Selected chip.
        /// </summary>
        /// <value>The type of the Selected chip.</value>
        public byte SelectedChipType { get; set; }

        private byte[] mPrivateS2Key = null;
        public byte[] PrivateS2Key
        {
            get { return mPrivateS2Key; }
            set
            {
                if (value != null && value.Length == 32)
                {
                    mPrivateS2Key = value;
                }
                else
                {
                    mPrivateS2Key = null;
                }
            }
        }

        private byte[] mPublicS2Key = null;
        public byte[] PublicS2Key
        {
            get { return mPublicS2Key; }
            set
            {
                if (value != null && value.Length == 32)
                {
                    mPublicS2Key = value;
                }
                else
                {
                    mPublicS2Key = null;
                }
            }
        }

        public bool HasValidS2Keypair2Keys
        {
            get
            {
                return
                    mPrivateS2Key != null &&
                    mPrivateS2Key.Length == 32 &&
                    mPublicS2Key != null &&
                    mPublicS2Key.Length == 32;
            }
        }

        public bool IsAddSecurity2Keys { get; set; }
        public bool IsGenerateSecurity2Keys { get; set; }
        public IDevice Device { get; set; }

        /// <summary>
        /// Is set to true by ControllerManager.DeviceOpen to indicate, that device is busy by some operation.
        /// </summary>
        public bool DeviceBusy = false;
    }
}
