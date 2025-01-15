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
    public class NodeBitmask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NodeBitmask"/> class.
        /// </summary>
        public NodeBitmask()
        {
            mask = null;
        }
        /// <summary>
        /// Creates a NodeBitmask object.
        /// </summary>
        /// <param name="length">length of nodemask (number of ids/8)</param>
        public NodeBitmask(byte length)
        {
            if (length > 0)
            {
                mask = new byte[length];
                //   maxNodeID = length*8;
            }
        }
        /// <summary>
        /// Gets the length.
        /// </summary>
        /// <value>The length.</value>
        public int Length
        {
            get
            {
                if (mask != null)
                {
                    return mask.Length;
                }
                else
                {
                    return 0;
                }
            }
        }
        /// <summary>
        /// Gets the nodemask as an bit array.
        /// </summary>
        /// <returns></returns>
        public byte[] Get()
        {
            return mask;
        }
        /// <summary>
        /// Returns the bitMask for the given Node Id.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public byte Get(byte nodeId)
        {
            nodeId--;
            return mask[nodeId >> 3];
        }
        /// <summary>
        /// Stores an entire array.
        /// </summary>
        /// <param name="array">The array.</param>
        public void Store(byte[] array)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (mask == null)
            {
                mask = new byte[array.Length];
            }
            for (int i = 0; i < mask.Length; i++)
            {
                mask[i] = array[i];
            }

        }


        /// <summary>
        /// Set the bit corresponding to the Node Id supplied.
        /// </summary>
        /// <param name="nodeId"></param>
        public void ZWaveNodeMaskSetBit(byte nodeId)
        {
            if (nodeId < 1)
            {
                return;
            }
            nodeId--;
            mask[nodeId >> 3] |= (byte)(0x1 << (nodeId & 7));
        }

        /// <summary>
        /// Clears the bit corresponding to then Node Id supplied.
        /// </summary>
        /// <param name="nodeId"></param>
        public void ZWaveNodeMaskClearBit(byte nodeId)
        {
            if (nodeId < 1)
            {
                return;
            }
            nodeId--;
            mask[nodeId >> 3] &= (byte)~(0x1 << (nodeId & 7));
        }
        /// <summary>
        /// Checks if the bit corresponding to the Node Id is set.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns>true if bit is set</returns>
        public bool ZWaveNodeMaskNodeIn(byte nodeId)
        {
            if (nodeId < 1)
            {
                return false;
            }
            nodeId--;
            if (mask == null)
            {
                return false;
            }
            else
            {
                if (mask.Length > (nodeId >> 3) && (((mask[(nodeId >> 3)]) >> (byte)(nodeId & 7)) & (byte)0x01) != 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        /// <summary>
        /// Returns true if any bits in mask.
        /// </summary>
        /// <returns>true if bits set, false if not</returns>
        public bool ZWaveNodeMaskBitsIn()
        {
            if (mask != null)
            {
                for (int i = 0; i < mask.Length; i++)
                {
                    if (mask[i] != 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Clears the nodemask.
        /// </summary>
        public void ZWaveNodeMaskClear()
        {
            if (mask != null)
            {
                for (int i = 0; i < mask.Length; i++)
                {
                    mask[i] = 0;
                }
            }
        }

        private byte[] mask;

    }
}
