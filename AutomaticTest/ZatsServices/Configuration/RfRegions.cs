// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zats.Services
{
    /// <summary>
    /// ZWave supported RF regions.
    /// Important: US_LR_BACKUP and US_LR_END_DEVICE is no longer supported from the SL-ZWave 7.22.0 SDK
    /// </summary>
    public enum RfRegions
    {
        /// <summary>Undefined/unknown region. This value SHOULD be used if there was an error retrieving the configured region.</summary>
        UNKNOWN = ZWave.BasicApplication.Enums.RfRegions.Undefined,
        /// <summary>Europe</summary>
        EU = ZWave.BasicApplication.Enums.RfRegions.EU,
        /// <summary>United States</summary>
        US = ZWave.BasicApplication.Enums.RfRegions.US,
        /// <summary>Australia/New Zealand</summary>
        ANZ = ZWave.BasicApplication.Enums.RfRegions.ANZ,
        /// <summary>Hong Kong</summary>
        HK = ZWave.BasicApplication.Enums.RfRegions.HK,
        /// <summary>Malaysia</summary>
        /// <remarks>Deprecated: should no longer be used.</remarks>
        //MY = ZWave.BasicApplication.Enums.RfRegions.MY,
        /// <summary>India</summary>
        IN = ZWave.BasicApplication.Enums.RfRegions.IN,
        /// <summary>Japan</summary>
        JP = ZWave.BasicApplication.Enums.RfRegions.JP,
        /// <summary>ru</summary>
        RU = ZWave.BasicApplication.Enums.RfRegions.RU,
        /// <summary>Israel</summary>
        IL = ZWave.BasicApplication.Enums.RfRegions.IL,
        /// <summary>Korea</summary>
        KR = ZWave.BasicApplication.Enums.RfRegions.KR,
        /// <summary>China</summary>
        CN = ZWave.BasicApplication.Enums.RfRegions.CN,
        /// <summary>Europe, Long Range</summary>
        EU_LR = ZWave.BasicApplication.Enums.RfRegions.EU_LR,
        /// <summary>USA</summary>
        US_LR = ZWave.BasicApplication.Enums.RfRegions.US_LR,
    }

    public enum RfLongRangeRegions
    {
        /// <summary>USA</summary>
        US_LR = ZWave.BasicApplication.Enums.RfRegions.US_LR,
        /// <summary>Europe, Long Range</summary>
        EU_LR = ZWave.BasicApplication.Enums.RfRegions.EU_LR,
    }


    public static class RfRegionsExtensions
    {
        /// <summary>
        /// Z-Wave Long Range RF Regions with their corresponding classic Z-Wave RF Region
        /// </summary>
        private static readonly Dictionary<RfLongRangeRegions, RfRegions> LongRangeRegions = new Dictionary<RfLongRangeRegions, RfRegions>()
        {
            { RfLongRangeRegions.EU_LR, RfRegions.EU },
            { RfLongRangeRegions.US_LR, RfRegions.US },
        };

        public static RfRegions GetCorrespondingClassicRfRegion(this RfLongRangeRegions lrRfRegion)
        {
            return LongRangeRegions.ContainsKey(lrRfRegion) ? LongRangeRegions[lrRfRegion] : RfRegions.UNKNOWN;
        }

        public static RfRegions GetCorrespondingClassicRfRegion(string longRangeRfRegionStr)
        {
            if (Enum.TryParse(longRangeRfRegionStr, out RfLongRangeRegions lrRfRegion))
            {
                return GetCorrespondingClassicRfRegion(lrRfRegion);
            }
            return RfRegions.UNKNOWN;
        }

        public static bool IsLongRangeRfRegion(this RfRegions rfRegion)
        {
            if (Enum.TryParse(rfRegion.ToString(), out RfLongRangeRegions lrRfRegion))
            {
                return LongRangeRegions.Keys.Contains(lrRfRegion);
            }
            return false;
        }

        public static bool IsLongRangeRfRegion(ZWave.BasicApplication.Enums.RfRegions rfRegion)
        {
            return IsLongRangeRfRegion(rfRegion.ToRfRegion());
        }

        public static bool IsLongRangeRfRegion(string rfRegionStr)
        {
            if (Enum.TryParse(rfRegionStr, out RfRegions rfRegion))
            {
                return rfRegion.IsLongRangeRfRegion();
            }
            return false;
        }

        public static RfRegions ToRfRegion(this ZWave.BasicApplication.Enums.RfRegions rfRegion)
        {
            RfRegions ret = RfRegions.UNKNOWN;
            switch (rfRegion)
            {
                case ZWave.BasicApplication.Enums.RfRegions.Undefined:
                    ret = RfRegions.UNKNOWN;
                    break;
                case ZWave.BasicApplication.Enums.RfRegions.EU:
                    ret = RfRegions.EU;
                    break;
                case ZWave.BasicApplication.Enums.RfRegions.US:
                    ret = RfRegions.US;
                    break;
                case ZWave.BasicApplication.Enums.RfRegions.ANZ:
                    ret = RfRegions.ANZ;
                    break;
                case ZWave.BasicApplication.Enums.RfRegions.HK:
                    ret = RfRegions.HK;
                    break;
                case ZWave.BasicApplication.Enums.RfRegions.IN:
                    ret = RfRegions.IN;
                    break;
                case ZWave.BasicApplication.Enums.RfRegions.IL:
                    ret = RfRegions.IL;
                    break;
                case ZWave.BasicApplication.Enums.RfRegions.RU:
                    ret = RfRegions.RU;
                    break;
                case ZWave.BasicApplication.Enums.RfRegions.CN:
                    ret = RfRegions.CN;
                    break;
                case ZWave.BasicApplication.Enums.RfRegions.US_LR:
                    ret = RfRegions.US_LR;
                    break;
                case ZWave.BasicApplication.Enums.RfRegions.JP:
                    ret = RfRegions.JP;
                    break;
                case ZWave.BasicApplication.Enums.RfRegions.KR:
                    ret = RfRegions.KR;
                    break;
                case ZWave.BasicApplication.Enums.RfRegions.EU_LR:
                    ret = RfRegions.EU_LR;
                    break;
                default:
                    break;
            }
            return ret;
        }

        public static ZWave.BasicApplication.Enums.RfRegions ToRfRegion(this RfRegions rfRegion)
        {
            ZWave.BasicApplication.Enums.RfRegions ret = ZWave.BasicApplication.Enums.RfRegions.Undefined;
            switch (rfRegion)
            {
                case RfRegions.UNKNOWN:
                    ret = ZWave.BasicApplication.Enums.RfRegions.Undefined;
                    break;
                case RfRegions.EU:
                    ret = ZWave.BasicApplication.Enums.RfRegions.EU;
                    break;
                case RfRegions.US:
                    ret = ZWave.BasicApplication.Enums.RfRegions.US;
                    break;
                case RfRegions.ANZ:
                    ret = ZWave.BasicApplication.Enums.RfRegions.ANZ;
                    break;
                case RfRegions.HK:
                    ret = ZWave.BasicApplication.Enums.RfRegions.HK;
                    break;
                case RfRegions.IN:
                    ret = ZWave.BasicApplication.Enums.RfRegions.IN;
                    break;
                case RfRegions.JP:
                    ret = ZWave.BasicApplication.Enums.RfRegions.JP;
                    break;
                case RfRegions.RU:
                    ret = ZWave.BasicApplication.Enums.RfRegions.RU;
                    break;
                case RfRegions.IL:
                    ret = ZWave.BasicApplication.Enums.RfRegions.IL;
                    break;
                case RfRegions.KR:
                    ret = ZWave.BasicApplication.Enums.RfRegions.KR;
                    break;
                case RfRegions.CN:
                    ret = ZWave.BasicApplication.Enums.RfRegions.CN;
                    break;
                case RfRegions.US_LR:
                    ret = ZWave.BasicApplication.Enums.RfRegions.US_LR;
                    break;
                case RfRegions.EU_LR:
                    ret = ZWave.BasicApplication.Enums.RfRegions.EU_LR;
                    break;
                default:
                    break;
            }
            return ret;
        }

        public static string ToFrequency(this RfRegions rfRegion)
        {
            return rfRegion.ToString();
        }
    }
}
