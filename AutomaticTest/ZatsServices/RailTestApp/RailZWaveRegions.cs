// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    /// <summary>
    /// Z-Wave Region Numbers.
    /// Uses as a parameter for 'setzwaveregion' command.
    /// Desc: INS14283 Table 1.
    /// </summary>
    public enum RailZWaveRegions
    {
        /// <summary> EU-European Union, E </summary>
        EU = 0,
        /// <summary> US-United States, U </summary>
        US = 1,
        /// <summary> ANZ-Australia/New Zealand, H </summary>
        ANZ = 2,
        /// <summary> HK-Hong Kong, H </summary>
        HK = 3,
        /// <summary> Non-Valid Z-Wave region* </summary>
        INVALID = 4,
        /// <summary> IN-India, E </summary>
        IN = 5,
        /// <summary> JP-Japan, H </summary>
        JP = 6,
        /// <summary> RU-Russia, E</summary>
        RU = 7,
        /// <summary> IL-Israel, U </summary>
        IL = 8,
        /// <summary> KR-Korea, H </summary>
        KR = 9,
        /// <summary> CN-China, E </summary>
        CN = 10,
        /// <summary> Z-Wave Long Range region 2, U </summary>
        LR_1 = 11,
        /// <summary> Z-Wave Long Range region 2, U </summary>
        LR_2 = 12,
        /// <summary> Z-Wave Long Range End Device, U </summary>
        LR_END_DEVICE = 13,
    }
}
