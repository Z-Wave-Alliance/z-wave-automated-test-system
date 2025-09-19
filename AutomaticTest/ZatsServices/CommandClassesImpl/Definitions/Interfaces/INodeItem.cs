// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
using System.Collections.Generic;

namespace ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces
{
    public interface INodeItem
    {
        IEnumerable<INestedItem> ChildItems { get; } // MUST be [JsonIgnore] in implementation!
    }
}
