// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zats.Services
{
    public interface ITransportServiceTestSettings
    {
        void SetFirstFragmentCRC16(Alias deviceNumber, byte[] crc16, int timesUsed);
        void SetIgnoreFirstSegment(Alias deviceNumber, int timesUsed);
        void SetIgnoreSubsequentSegment(Alias deviceNumber, int offset, int timesUsed);
        void SetSegmentCompleteCmdSessionId(Alias deviceNumber, byte sessionId, int timesUsed);
        void SetSubsequentFragmentCRC16(Alias deviceNumber, byte[] crc16, int offset, int timesUsed);
    }
}
