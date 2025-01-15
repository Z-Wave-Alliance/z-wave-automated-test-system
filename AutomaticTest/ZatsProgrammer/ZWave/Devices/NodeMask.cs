// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Devices
{
    /// <summary>
    /// NodeMask handling class.
    /// </summary>
    public class NodeMask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NodeMask"/> class.
        /// </summary>
        public NodeMask()
        {
            nodeMask = new byte[29];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeMask"/> class.
        /// </summary>
        /// <param name="_nodeMask">The node mask.</param>
        public NodeMask(byte[] _nodeMask)
        {
            if (_nodeMask == null)
            {
                throw new ArgumentNullException("_nodeMask");
            }
            if (nodeMask == null)
            {
                nodeMask = new byte[29];
            }
            if (_nodeMask.Length == 29)
            {
                for (int i = 0; i < 29; i++)
                {
                    nodeMask[i] = _nodeMask[i];
                }
            }
        }

        /// <summary>
        /// Clears the data.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < 29; i++)
            {
                nodeMask[i] = 0;
            }
        }


        /// <summary>
        /// Gets the mask by specified Node Id.
        /// </summary>
        /// <param name="nodeId">The Node Id.</param>
        /// <returns></returns>
        public bool Get(int nodeId)
        {
            if ((nodeId > 0) && (nodeId <= 232))
            {
                return ((nodeMask[(nodeId - 1) >> 3] & (1 << ((nodeId - 1) & 0x07))) != 0);
            }
            return false;
        }

        /// <summary>
        /// Sets the mask by specified node id.
        /// </summary>
        /// <param name="nodeId">The node id.</param>
        /// <param name="value">if set to <c>true</c> mask value.</param>
        /// <returns></returns>
        public bool Set(int nodeId, bool value)
        {
            if ((nodeId > 0) && (nodeId <= 232))
            {
                if (value)
                {
                    /* Set nodeMask bit */
                    nodeMask[((byte)nodeId - 1) >> 3] |= (byte)(1 << (((byte)nodeId - 1) & 0x07));
                }
                else
                {
                    /* Clear nodeMask bit */
                    nodeMask[((byte)nodeId - 1) >> 3] &= (byte)~(1 << (((byte)nodeId - 1) & 0x07));
                }
                return true;
            }
            return false;
        }

        private readonly byte[] nodeMask;
    }
}
