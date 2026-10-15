// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;

namespace Zats.Services
{
    /// <summary>
    /// Represents a log packet item
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
        /// <param name="applicationString">The application string.</param>
        /// <param name="deviceNumber">The device number.</param>
        /// <param name="serialPort">The serial port.</param>
        public LogPacket(byte[] payload, bool isPut, bool isRequest, DateTime time, string applicationString, int deviceNumber, string serialPort)
        {
            Payload = payload;
            IsPut = isPut;
            IsRequest = isRequest;
            Time = time;
            ApplicationString = applicationString;
            DeviceNumber = deviceNumber;
            SerialPort = serialPort;
        }

        /// <summary>
        /// Gets or sets the device number.
        /// </summary>
        /// <value>The device number.</value>
        public int DeviceNumber { get; set; }

        /// <summary>
        /// Gets or sets the serial port.
        /// </summary>
        /// <value>The serial port.</value>
        public string SerialPort { get; set; }

        /// <summary>
        /// Gets the payload.
        /// </summary>
        /// <value>The payload.</value>
        public byte[] Payload { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is for put.
        /// </summary>
        /// <value><c>true</c> if this instance is for put; otherwise, <c>false</c>.</value>
        public bool IsPut { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is request.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is request; otherwise, <c>false</c>.
        /// </value>
        public bool IsRequest { get; set; }

        /// <summary>
        /// Gets the date time string.
        /// </summary>
        /// <value>The date time string.</value>
        public DateTime Time { get; set; }

        /// <summary>
        /// Gets or sets the application string.
        /// </summary>
        /// <value>The application string.</value>
        public string ApplicationString { get; set; }

        public override string ToString()
        {
            return Tools.FormatStr("{0}: {1} {2}{3}{4} {5}", Time, DeviceNumber, SerialPort, IsPut?">>":"<<", IsRequest?"RQ":"RS", Tools.GetHex(Payload));
        }
    }
}
