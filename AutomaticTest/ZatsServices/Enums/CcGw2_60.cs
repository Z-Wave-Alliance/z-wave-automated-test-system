// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZWave.Enums
{
    public struct CmdClass
    {
        public byte CcId;
        public byte Version;
        public bool SecureOnly;
        public bool LanNif;
        public bool PanGwNif;
        public bool AppendedPanNodeNif;
    }
}
