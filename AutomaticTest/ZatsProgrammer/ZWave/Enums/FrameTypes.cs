// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Enums
{
    /// <summary>
    /// Frame Types enumeration.
    /// </summary>
    public enum FrameTypes
    {
        /// <summary>
        /// Request.
        /// </summary>
        Request = 0x00,
        /// <summary>
        /// Response.
        /// </summary>
        Response = 0x01
    }

	public enum ProgrammerFrameInfo : int
	{
		/// <summary>
		/// Miminum size of Z-Wave Programmer frame.
		/// </summary>
		ProgrammerFrameSizeMin = 7,

		ProgrammerFrameButtonPressedSize = 8,
		ProgrammerFrameButtonStateOffset = 5,
		ProgrammerFrameButtonStateS1Pressed = 0x01,
	}
}
