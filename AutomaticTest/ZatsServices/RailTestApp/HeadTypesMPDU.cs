// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    /// <summary>
    /// Table 8-14 – Header type, Rec. ITU-T G.9959 (01/2015)
    /// </summary>
    public enum HeadTypesMPDU
    {
        /// <summary>
        /// Singlecast MPDU
        /// </summary>
        Singlecast = 0x1,
        /// <summary>
        /// Multicast MPDU
        /// </summary>
        Multicast = 0x2,
        /// <summary>
        /// Acknowledgement MPDU
        /// </summary>
        Ack = 0x3,
        Explorer = 0x5,

        //0x4..0x7 Not to be used (Note)

        /// <summary>
        /// Routed MPDU
        /// </summary>
        routed = 0x8,
        //(Channel configuration 3 only)
        //0x9..0xB Not to be used(Note)
        //0xC..0xF Reserved
        //NOTE – These values shall not be transmitted due to backward compatibility.Received frames shall be
        //forwarded to higher layers.
    }
}
