// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zats.Services.CommandClassesImpl.Definitions.Interfaces
{
    public interface ICommandClass
    {
        byte CommandClassId { get; set; }

        byte Version { get; set; }

        bool Secure { get; set; }
    }
}
