// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>

namespace Zats
{
    /// <summary>
    /// Product, company, copyright and version of all ZATS assemblies. Projects that reference ZatsServices use this class
    /// from there; the other projects link this file into their sources. The AssemblyInfo.cs files reference these constants.
    /// </summary>
    public static class ZatsVersionInfo
    {
        // The following constants should be updated for each release.
        // The copyright starts in 2025, the year Silicon Labs donated ZATS to the Z-Wave Alliance; keep that year and
        // set the end year to the year of the last change.
        public const string COPYRIGHT = "© 2025–2026 Z-Wave Alliance, Inc. All rights reserved.";
        public const string VERSION = "0.1.0.0"; // keep all four digits

        // The following constants should not be changed.
        public const string COMPANY_NAME = "Z-Wave Alliance";
        public const string PRODUCT = "Z-Wave Automated Test System";
    }
}
