// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.ProgrammerFrame
{
    public enum FrameReceiveState 
    {
        FRS_SOF_HUNT = 0,
        FRS_LENGTH_H = 1,
        FRS_LENGTH_L = 2,
        FRS_TYPE = 3,
        FRS_COMMAND = 4,
        FRS_DATA = 5,
        FRS_CHECKSUM = 6,
        FRS_RX_TIMEOUT = 7
    }
}
