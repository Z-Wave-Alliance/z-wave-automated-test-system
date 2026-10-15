// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Enums;

namespace ZWave.Events
{
    /// <summary>
    /// Provides data for <see cref="ZWave.Devices.IDeviceFlash.ProgressChanged"/> or <see cref="ZWave.Devices.IDeviceMemory.ProgressChanged"/> event.
    /// </summary>
    public class ProgressChangedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressChangedEventArgs"/> class.
        /// </summary>
        /// <param name="prevValue">The current value.</param>
        /// <param name="newValue">The total value.</param>
        public ProgressChangedEventArgs(ProgressStatuses status, int current, int total)
        {
            this.ProgressStatus = status;
            this.Current = current;
            this.Total = total;
        }
        /// <summary>
        /// Gets or sets the current progress status.
        /// </summary>
        /// <value>The current progress status.</value>
        private ProgressStatuses mProgressStatus;
        public ProgressStatuses ProgressStatus
        {
            get { return mProgressStatus; }
            set { mProgressStatus = value; }
        }
	
        private int mCurrent;
        /// <summary>
        /// Gets or sets the current progress value.
        /// </summary>
        /// <value>The current progress value.</value>
        public int Current
        {
            get { return mCurrent; }
            set { mCurrent = value; }
        }

        private int mTotal;
        /// <summary>
        /// Gets or sets the total progress value.
        /// </summary>
        /// <value>The current total value.</value>
        public int Total
        {
            get { return mTotal; }
            set { mTotal = value; }
        }	
    }
}
