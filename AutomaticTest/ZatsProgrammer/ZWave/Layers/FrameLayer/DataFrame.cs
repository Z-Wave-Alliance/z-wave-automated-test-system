// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Enums;

namespace ZWave.ProgrammerFrame
{
    class DataFrame : IDataFrame
    {
        private const int BUFFER_SIZE = 512 * 8;

        public DataFrame()
        {
            this.timestamp = DateTime.Now;
            this.payloadIdx = 0;
            this.payloadBuffer = new byte[BUFFER_SIZE];
        }

        public DataFrame(ushort payloadSize)
        {
            this.timestamp = DateTime.Now;
            this.payloadIdx = 0;
            this.payloadBuffer = new byte[payloadSize];
        }

        public bool AddPayload(byte data)
        {
            if (payloadIdx > payloadBuffer.Length - 1) return false;
            payloadBuffer[payloadIdx++] = data;
            return true;
        }

        public bool AddPayload(byte[] data)
        {
            if ((payloadIdx + data.Length) > payloadBuffer.Length - 1) return false;
            data.CopyTo(payloadBuffer, payloadIdx);
            payloadIdx += (ushort)data.Length;
            return true;
        }

        public byte[] GetPayloadBuffer()
        {
            return payloadBuffer;
        }

        public byte Command
        {
            set
            {
                this.cmd = value;
            }
            get
            {
                return cmd;
            }
        }

        public FrameTypes Type
        {
            set
            {
                this.type = value;
            }
            get
            {
                return type;
            }
        }

        public bool IsPayloadFull()
        {
            return payloadIdx > payloadBuffer.Length - 1;
        }

        public bool IsChecksumValid(byte checksum)
        {
            byte calcChecksum = calculateChecksum();
            return calcChecksum == checksum;
        }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder(BUFFER_SIZE);

            // Header...
            sb.Append((byte)HeaderType.HEADER_SOF);
            sb.Append(' ');

            // Length MSB...
            sb.Append((byte)((payloadIdx + 4) >> 8));
            sb.Append(' ');

            // Length LSB...
            sb.Append((byte)((payloadIdx + 4) & 8));
            sb.Append(' ');

            // Type...
            sb.Append((byte)type);
            sb.Append(' ');

            // Command...
            sb.Append((byte)cmd);
            sb.Append(' ');

            // Data payload...
            for (int i = 0; i < payloadIdx; i++)
            {
                sb.Append(payloadBuffer[i]);
                sb.Append(' ');
            }

            // Checksum...
            sb.Append(calculateChecksum());

            return sb.ToString();
        }
        public byte[] GetFrameBuffer()
        {
            byte[] buffer = new byte[payloadIdx + 6];
            buffer[IDX_HEADER] = (byte)HeaderType.HEADER_SOF;
            buffer[IDX_LENGTH] = (byte)((payloadIdx + 4) >> 8); // length MSB
            buffer[IDX_LENGTH + 1] = (byte)((payloadIdx + 4) & 0xFF); // length LSB
            buffer[IDX_TYPE] = (byte)type;
            buffer[IDX_CMD] = (byte)cmd;
            Array.Copy(payloadBuffer, 0, buffer, IDX_DATA, payloadIdx);
            buffer[buffer.Length - 1] = calculateChecksum();
            return buffer;
        }

        private byte calculateChecksum()
        {
            byte calcChksum = 0xFF;
            calcChksum ^= (byte)((payloadIdx + 4) >> 8); // Length MSB
            calcChksum ^= (byte)((payloadIdx + 4) & 0xFF); // Length LSB
            calcChksum ^= (byte)type;     // Type
            calcChksum ^= (byte)cmd;      // Command
            for (int i = 0; i < payloadIdx; i++)
                calcChksum ^= payloadBuffer[i];      // Data
            return calcChksum;
        }

        public DateTime TimeStamp
        {
            get { return timestamp; }
        }

        public static bool operator ==(DataFrame df1, DataFrame df2)
        {
            if (!(df1 is DataFrame) || !(df2 is DataFrame)) return false;
            if (df1.Type != df2.Type) return false;
            if (df1.Command != df2.Command) return false;
            if (df1.payloadIdx != df2.payloadIdx) return false;
            for (int i = 0; i < df1.payloadIdx; i++)
                if (df1.payloadBuffer[i] != df2.payloadBuffer[i]) return false;
            return true;
        }

        public static bool operator !=(DataFrame df1, DataFrame df2)
        {
            return !(df1 == df2);
        }

        public override bool Equals(object o)
        {
            if (!(o is DataFrame)) return false;
            return this == (DataFrame)o;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private byte[] payloadBuffer;
        private ushort payloadIdx;
        private readonly DateTime timestamp;
        private FrameTypes type;
        private byte cmd;

        public enum HeaderType : byte
        {
            HEADER_SOF = 0x01,
            HEADER_ACK = 0x06,
            HEADER_NAK = 0x15
        }

        private const int IDX_HEADER = 0;
        private const int IDX_LENGTH = 1;
        private const int IDX_TYPE = 3;
        private const int IDX_CMD = 4;
        private const int IDX_DATA = 5;





        #region IDataFrame Members


        public byte[] GetPayload()
        {
            if (payloadIdx > 0)
            {
                byte[] buffer = new byte[payloadIdx];
                Array.Copy(payloadBuffer, 0, buffer, 0, payloadIdx);
                return buffer;
            }
            else
                return null;
            // return payloadBuffer;
        }

        #endregion
    }
}
