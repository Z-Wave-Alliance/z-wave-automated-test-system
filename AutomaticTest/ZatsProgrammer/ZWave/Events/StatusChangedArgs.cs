// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Events
{
    /// <summary>
    /// Provides data for <see cref="ZWave.Devices.IDevice.ConnectionStatusChanged"/> event.
    /// </summary>
    public class StatusChangedArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatusChangedArgs"/> class.
        /// </summary>
        /// <param name="prevValue">The previous value.</param>
        /// <param name="newValue">The new value.</param>
        public StatusChangedArgs(int prevValue, int newValue)
        {
            mPrevStatusValue = prevValue;
            mNewStatusValue = newValue;
        }

        private int mPrevStatusValue;
        /// <summary>
        /// Gets or sets the previous status value.
        /// </summary>
        /// <value>The previous status value.</value>
        public int PrevStatusValue
        {
            get
            {
                return mPrevStatusValue;
            }
            set
            {
                mPrevStatusValue = value;
            }
        }

        private int mNewStatusValue;
        /// <summary>
        /// Gets or sets the new status value.
        /// </summary>
        /// <value>The new status value.</value>
        public int NewStatusValue
        {
            get
            {
                return mNewStatusValue;
            }
            set
            {
                mNewStatusValue = value;
            }
        }
    }
}
