// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using ZWave.Programmer.Properties;

namespace ZWave.Programmer.Classes
{
    /// <summary>
    /// ProgrammerSettings class. Contains application settings.
    /// </summary>
    public class ProgrammerSettings
    {
        private bool mDetectTargetOnStartup = false;
        /// <summary>
        /// Gets or sets a value indicating whether Detect Target on Startup is set.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if Detect Target on Startup; otherwise, <c>false</c>.
        /// </value>
        [Category("Common")]
        [DisplayName("Detect Target on startup")]
        [Description("Detect Target on startup.")]
        public bool DetectTargetOnStartup
        {
            get { return mDetectTargetOnStartup; }
            set { mDetectTargetOnStartup = value; }
        }
        private bool mShowHexFilesPath = false;
        /// <summary>
        /// Gets or sets a value indicating whether Show HEX files path.
        /// </summary>
        /// <value><c>true</c> if Show hex files path; otherwise, <c>false</c>.</value>
        [Category("Common")]
        [DisplayName("Show HEX files path")]
        [Description("Show HEX files path.")]
        public bool ShowHexFilesPath
        {
            get { return mShowHexFilesPath; }
            set { mShowHexFilesPath = value; }
        }

        private bool mResetChipAfterProgramming = true;
        /// <summary>
        /// Gets or sets a value indicating whether Reset Chip After Programming.
        /// </summary>
        /// <value><c>true</c> if Show hex files path; otherwise, <c>false</c>.</value>
        [Category("Common")]
        [DisplayName("Reset Chip After Programming")]
        [Description("Reset Chip After Programming.")]
        public bool ResetChipAfterProgramming
        {
            get { return mResetChipAfterProgramming; }
            set { mResetChipAfterProgramming = value; }
        }

        public enum EepromSizes
        {
            EepromSize_16K = 16384,
            EepromSize_64K = 65536,
            EepromSize_128K = 131072
        }
        private EepromSizes mEepromSize = EepromSizes.EepromSize_16K;
        [Category("Common")]
        [DisplayName("EEPROM size")]
        [Description("EEPROM size.")]
        public EepromSizes EepromSize
        {
            get { return mEepromSize; }
            set { mEepromSize = value; }
        }
        //private bool mWriteFlashOnFileChange = false;
        //[Category("Common")]
        //[DisplayName("Write Flash on file change")]
        //[Description("Write Flash on file change.")]
        //public bool WriteFlashOnFileChange
        //{
        //    get { return mWriteFlashOnFileChange; }
        //    set { mWriteFlashOnFileChange = value; }
        //}

        private string mInterfaceFilter;
        /// <summary>
        /// Gets or sets the Interface filter (e.g. "Silicon Labs").
        /// </summary>
        /// <value>The Interface filter.</value>
        [Category("Common")]
        [DisplayName("Interface Filter")]
        [Description("Interface Filter string (e.g \"Silicon Labs\").")]
        public string InterfaceFilter
        {
            get { return mInterfaceFilter; }
            set { mInterfaceFilter = value; }
        }

        private bool mHoldDeviceInReset;

        /// <summary>
        /// Gets or sets a value indicating whether hold device in reset.
        /// </summary>
        /// <value><c>true</c> if hold device in reset; otherwise, <c>false</c>.</value>
        [Category("Common")]
        [DisplayName("Hold device in reset")]
        [Description("Hold device in reset.")]
        public bool HoldDeviceInReset
        {
            get { return mHoldDeviceInReset; }
            set { mHoldDeviceInReset = value; }
        }

        private bool mUseTestFrequencies;

        /// <summary>
        /// Gets or sets a value indicating whether use test frequencies.
        /// </summary>
        /// <value><c>true</c> if use test frequencies; otherwise, <c>false</c>.</value>
        /// [Category("Common")]
        [DisplayName("Use test frequencies")]
        [Description("Use test frequencies.")]
        public bool UseTestFrequencies
        {
            get { return mUseTestFrequencies; }
            set { mUseTestFrequencies = value; }
        }

        private string mCalibrationHexPath;

        /// <summary>
        /// Gets or sets the path to calibration application HEX file.
        /// </summary>
        /// <value>The path to calibration application HEX file.</value>
        [Category("Common")]
        [DisplayName("Calibration HEX file")]
        [Description("Path to calibration application HEX file.\nZW050x_calibration_en_JTCK_ref_auto.hex file is used by default \nfor 500 series.")]
        public string CalibrationHexPath
        {
            get { return mCalibrationHexPath; }
            set { mCalibrationHexPath = value; }
        }

        /// <summary>
        /// Loads the application settings.
        /// </summary>
        public void Load()
        {
            this.DetectTargetOnStartup = Settings.Default.DetectTargetOnStartup;
            this.ShowHexFilesPath = Settings.Default.ShowHexFilesPath;
            this.ResetChipAfterProgramming = Settings.Default.ResetChipAfterProgramming;
            //this.WriteFlashOnFileChange = Settings.Default.WriteFlashOnFileChange;
            this.InterfaceFilter = Settings.Default.InterfaceFilter;
            this.HoldDeviceInReset = Settings.Default.HoldDeviceInReset;
            this.UseTestFrequencies = Settings.Default.UseTestFrequencies;
            this.CalibrationHexPath = Settings.Default.CalibrationHexPath;
            this.EepromSize = (EepromSizes)Settings.Default.EEPROMSize;
            //this.RfConstantTable = (RfConstantTable)Settings.Default.RfConstantsTable;
        }

        /// <summary>
        /// Save the application settings
        /// </summary>
        public void Save()
        {
            Settings.Default.DetectTargetOnStartup = this.DetectTargetOnStartup;
            Settings.Default.ShowHexFilesPath = this.ShowHexFilesPath;
            Settings.Default.ResetChipAfterProgramming = this.ResetChipAfterProgramming;
            //Settings.Default.WriteFlashOnFileChange = this.WriteFlashOnFileChange;
            Settings.Default.InterfaceFilter = this.InterfaceFilter;
            Settings.Default.HoldDeviceInReset = this.HoldDeviceInReset;
            Settings.Default.UseTestFrequencies = this.UseTestFrequencies;
            Settings.Default.CalibrationHexPath = this.CalibrationHexPath;
            Settings.Default.EEPROMSize = (int)this.EepromSize;
            //Settings.Default.RfConstantsTable = this.RfConstantTable;
            Settings.Default.Save();
        }

    }
}
