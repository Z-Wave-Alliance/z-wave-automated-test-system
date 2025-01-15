// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using System.Linq;

namespace Zats.Services
{
    /// <summary>
    /// Grouped list for all protocol versions.
    /// </summary>
    public class HeaderTypes
    {
        public static byte[] BROADCAST = new byte[] { (byte)HeaderTypesEx.BROADCAST, (byte)HeaderTypesEx.BROADCAST24, (byte)HeaderTypesEx.BROADCASTLR };
        public static byte[] SINGLECAST = new[] { (byte)HeaderTypesEx.SINGLECAST, (byte)HeaderTypesEx.SINGLECAST24, (byte)HeaderTypesEx.SINGLECASTLR };
        public static byte[] MULTICAST = new byte[] { (byte)HeaderTypesEx.MULTICAST, (byte)HeaderTypesEx.MULTICAST24 };
        public static byte[] TRANSFER_ACKNOWLEDGE = new byte[] { (byte)HeaderTypesEx.ACK, (byte)HeaderTypesEx.ACK24, (byte)HeaderTypesEx.ACKLR };
        public static byte[] ROUTED_SINGLECAST = new byte[] { (byte)HeaderTypesEx.ROUTED_SINGLECAST, (byte)HeaderTypesEx.ROUTED_SINGLECAST24 };
        public static byte[] ROUTED_ACKNOWLEDGE = new byte[] { (byte)HeaderTypesEx.ROUTED_ACKNOWLEDGE, (byte)HeaderTypesEx.ROUTED_ACKNOWLEDGE24 };
        public static byte[] ROUTED_ERROR = new byte[] { (byte)HeaderTypesEx.ROUTED_ERROR, (byte)HeaderTypesEx.ROUTED_ERROR24 };
        public static byte[] FLOODED = new byte[] { (byte)HeaderTypesEx.FLOODED, (byte)HeaderTypesEx.FLOODED24 };
        public static byte[] EXPLORER_NORMAL = new byte[] { (byte)HeaderTypesEx.EXPLORER_NORMAL, (byte)HeaderTypesEx.EXPLORER_NORMAL24 };
        public static byte[] EXPLORER_SEARCH_RESULT = new byte[] { (byte)HeaderTypesEx.EXPLORER_SEARCH_RESULT, (byte)HeaderTypesEx.EXPLORER_SEARCH_RESULT24 };
        public static byte[] EXPLORER_AUTOINCLUSION = new byte[] { (byte)HeaderTypesEx.EXPLORER_AUTOINCLUSION, (byte)HeaderTypesEx.EXPLORER_AUTOINCLUSION24 };
        public static byte[] WAKEUP_BEAM = new byte[] { (byte)HeaderTypesEx.WAKEUP_BEAM, (byte)HeaderTypesEx.WAKEUP_BEAM_START, (byte)HeaderTypesEx.WAKEUP_BEAM_STOP, (byte)HeaderTypesEx.WAKEUP_BEAM_LR };

        //Array containing both the Singlecast and Routed Singlecast header types
        public static byte[] ALL_SINGLECAST = new byte[] {(byte)HeaderTypesEx.SINGLECAST, (byte)HeaderTypesEx.SINGLECAST24,
            (byte)HeaderTypesEx.ROUTED_SINGLECAST, (byte)HeaderTypesEx.ROUTED_SINGLECAST24, (byte)HeaderTypesEx.SINGLECASTLR };
        //Array containing all types of ACK header types
        public static byte[] ALL_ACKNOWLEDGE = new byte[] { (byte)HeaderTypesEx.ACK, (byte)HeaderTypesEx.ACK24,
            (byte)HeaderTypesEx.ROUTED_ACKNOWLEDGE, (byte)HeaderTypesEx.ROUTED_ACKNOWLEDGE24, (byte)HeaderTypesEx.ACKLR };
    }

    public enum ChannelConfigs
    {
        Ch12,
        Ch3,
        ChLR
    }

    /// <summary>
    /// Detailed list for protocol versions:
    ///  - normal, 
    ///  - 2.4Gz, 
    ///  - 2.4Gz Extended
    /// </summary>
    public enum HeaderTypesEx
    {
        BROADCAST = 10,
        BROADCAST24 = 11,
        BROADCASTLR = 72,
        SINGLECAST = 13,
        SINGLECAST24 = 14,
        SINGLECASTLR = 70,
        MULTICAST = 16,
        MULTICAST24 = 17,
        ACK = 19,
        ACK24 = 20,
        ACKLR = 71,
        ROUTED_SINGLECAST = 22,
        ROUTED_SINGLECAST24 = 23,
        ROUTED_ACKNOWLEDGE = 25,
        ROUTED_ACKNOWLEDGE24 = 26,
        ROUTED_ERROR = 28,
        ROUTED_ERROR24 = 29,
        FLOODED = 31,
        FLOODED24 = 32,
        EXPLORER_NORMAL = 33,
        EXPLORER_NORMAL24 = 34,
        EXPLORER_SEARCH_RESULT = 35,
        EXPLORER_SEARCH_RESULT24 = 36,
        EXPLORER_AUTOINCLUSION = 37,
        EXPLORER_AUTOINCLUSION24 = 38,
        WAKEUP_BEAM = 60,
        WAKEUP_BEAM_START = 61,
        WAKEUP_BEAM_STOP = 62,
        WAKEUP_BEAM_LR = 63
    }

    public static class StaticHeaderEnumExtensions
    {
        public static bool BelongsTo(this HeaderTypesEx headerType, IEnumerable<byte> headerTypeCodes)
        {
            bool ret = headerTypeCodes != null && headerTypeCodes.Contains((byte)headerType);
            return ret;
        }
    }

    internal enum HeaderTypesShortEx
    {
        BC = 10,
        BC24 = 12,
        BCLR = 72,
        SC = 13,
        SC24 = 15,
        SCLR = 70,
        MC = 16,
        MC24 = 18,
        Ack = 19,
        Ack24 = 20,
        AckLR = 71,
        rSC = 22,
        rSC24 = 24,
        rAck = 25,
        rAck24 = 27,
        rErr = 28,
        rErr24 = 30,
        Flood = 31,
        Flood24 = 32,
        eNorm = 33,
        eNorm24 = 34,
        eSrch = 35,
        eSrch24 = 36,
        eAInc = 37,
        eAInc24 = 38,
        wbeam = 60,
        wbStart = 61,
        wbStop = 62,
        wbeamLR = 63
    }
}
