// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using ZWave.Enums;
using ZWave.Framework;

namespace ZWave.Logging
{
    /// <summary>
    /// Represents a log packet item in the <see cref="LogSession.Packets"/>.
    /// </summary>
    public class LogPacket
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogPacket"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <param name="isPut">if set to <c>true</c> [is put].</param>
        /// <param name="isRequest">if set to <c>true</c> [is request].</param>
        /// <param name="time">The time.</param>
        /// <param name="applicationByte">The application byte.</param>
        public LogPacket(byte[] payload, bool isPut, bool isRequest, DateTime time, byte applicationByte, Type enumType)
        {
            mPayload = payload;
            mIsPut = isPut;
            mIsRequest = isRequest;
            mTime = time;
            mApplicationByte = applicationByte;
            mApplicationString = SerialApiParser.GetApplicationString(applicationByte, payload, enumType);
        }
        private readonly byte mApplicationByte;

        private byte[] mPayload;
        /// <summary>
        /// Gets the payload.
        /// </summary>
        /// <value>The payload.</value>
        public byte[] Payload
        {
            get
            {
                if (mPayload != null && mPayload.Length == 1 && mPayload[0] == mApplicationByte)
                {
                    return null;
                }
                else
                {
                    return mPayload;
                }
            }
        }

        private readonly bool mIsPut;
        /// <summary>
        /// Gets a value indicating whether this instance is for put.
        /// </summary>
        /// <value><c>true</c> if this instance is for put; otherwise, <c>false</c>.</value>
        public bool IsPut
        {
            get { return mIsPut; }
        }

        private bool mIsRequest;
        /// <summary>
        /// Gets or sets a value indicating whether this instance is request.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is request; otherwise, <c>false</c>.
        /// </value>
        public bool IsRequest
        {
            get { return mIsRequest; }
            set { mIsRequest = value; }
        }
	

        private readonly DateTime mTime;
        /// <summary>
        /// Gets the date time string.
        /// </summary>
        /// <value>The date time string.</value>
        public DateTime Time
        {
            get { return mTime; }
        }

        private string mApplicationString;
        /// <summary>
        /// Gets or sets the application string.
        /// </summary>
        /// <value>The application string.</value>
        public string ApplicationString
        {
            get { return mApplicationString; }
            set { mApplicationString = value; }
        }

        private string mParameterString;
        /// <summary>
        /// Gets or sets the parameters string.
        /// </summary>
        /// <value>The parameters string.</value>
        public string ParameterString
        {
            get { return mParameterString; }
            set { mParameterString = value; }
        }
	
    }
}
