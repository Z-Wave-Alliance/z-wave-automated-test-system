// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Framework;

namespace ZWave.Programmer.Classes
{
    public class NVRData
    {
        /*
         REV � NVR layout revision (8bit)
         CCAL � Crystal Calibration (8bit)
         PINS � Pin Swap (8bit)  
         NVMCS � NVM Chip Select (8bit)
         SAWC � SAW Center Frequency (24bit)
         SAWB � SAW Bandwidth (8bit)
         NVMT Non-Volatile-Memory Type (8bit)
         NVMS Non-Volatile-Memory Size (16bit)
         NVMP Non-Volatile-Memory Page Size (16bit)
         UUID � Universally Unique Identifier (128bit)
         VID � USB Vendor ID (16bit)
         PID � USB Product ID (16bit)
         TXCAL1 � Frequency Calibration 868.4MHz (8bit)
         TXCAL2 � Frequency Calibration 924.4MHz (8bit)
         PUK � Public Key for Security2 (256bit)
         PRK � Private Key for Security2 (256bit)
         CRC16 � CRC (16bit)
         */
        //public const byte FIELDS_OFFSET = 0x10;
        public const byte REV_ADDRESS = 0x10;
        private byte mREV = 0xFF;
        public byte REV
        {
            get { return mREV; }
            set
            {
                mREV = value;
                //if (mREV == 0xFF) mREV = 0x01;
            }
        }
        public const byte CCAL_ADDRESS = 0x11;
        private byte mCCAL = 0xFF;
        public byte CCAL
        {
            get { return mCCAL; }
            set
            {
                mCCAL = value;
            }
        }
        public const byte PINS_ADDRESS = 0x12;
        private byte mPINS = 0xFF;
        public byte PINS
        {
            get { return mPINS; }
            set
            {
                mPINS = value;
            }
        }
        public const byte NVMCS_ADDRESS = 0x13;
        private byte mNVMCS = 0xFF;
        public byte NVMCS
        {
            get { return mNVMCS; }
            set
            {
                mNVMCS = value;
            }
        }
        public const byte SAWC_ADDRESS = 0x14;
        private byte[] mSAWC = new byte[] { 0xFF, 0xFF, 0xFF };
        public byte[] SAWC
        {
            get { return mSAWC; }
            set
            {
                mSAWC = value;
            }
        }
        public const byte SAWB_ADDRESS = 0x17;
        private byte mSAWB = 0xFF;
        public byte SAWB
        {
            get { return mSAWB; }
            set
            {
                mSAWB = value;
            }
        }
        public const byte NVMT_ADDRESS = 0x18;
        private byte mNVMT = 0xFF;
        public byte NVMT
        {
            get { return mNVMT; }
            set
            {
                mNVMT = value;
            }
        }
        public const byte NVMS_ADDRESS = 0x19;
        private byte[] mNVMS = new byte[] { 0xFF, 0xFF };
        public byte[] NVMS
        {
            get { return mNVMS; }
            set
            {
                mNVMS = value;
            }
        }
        public const byte NVMP_ADDRESS = 0x1B;
        private byte[] mNVMP = new byte[] { 0xFF, 0xFF };
        public byte[] NVMP
        {
            get { return mNVMP; }
            set
            {
                mNVMP = value;
            }
        }
        public const byte UUID_ADDRESS = 0x1D;
        private byte[] mUUID = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        public byte[] UUID
        {
            get { return mUUID; }
            set
            {
                mUUID = value;
            }
        }

        public const byte VID_ADDRESS = 0x2D;
        private byte[] mVID = new byte[] { 0xFF, 0xFF };
        public byte[] VID
        {
            get { return mVID; }
            set
            {
                mVID = value;
            }
        }
        public const byte PID_ADDRESS = 0x2F;
        private byte[] mPID = new byte[] { 0xFF, 0xFF };
        public byte[] PID
        {
            get { return mPID; }
            set
            {
                mPID = value;
            }
        }
        public const byte TXCAL1_ADDRESS = 0x31;
        private byte mTXCAL1 = 0xFF;
        public byte TXCAL1
        {
            get { return mTXCAL1; }
            set
            {
                mTXCAL1 = value;
            }
        }

        public const byte TXCAL2_ADDRESS = 0x32;
        private byte mTXCAL2 = 0xFF;
        public byte TXCAL2
        {
            get { return mTXCAL2; }
            set
            {
                mTXCAL2 = value;
            }
        }

        public const byte PUK_ADDRESS = 0x33;
        private byte[] mPUK = new byte[] { 
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        public byte[] PUK
        {
            get { return mPUK; }
            set
            {
                mPUK = value;
            }
        }

        public const byte PRK_ADDRESS = 0x53;
        private byte[] mPRK = new byte[] { 
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        public byte[] PRK
        {
            get { return mPRK; }
            set
            {
                mPRK = value;
            }
        }

        public const byte HW_ADDRESS = 0x80;
        private byte mHW = 0xFF;
        public byte HW
        {
            get { return mHW; }
            set
            {
                mHW = value;
            }
        }

        public const byte PROTOCOL_DATA_ADDRESS = 0x73;
        private byte[] mProtocolData = new byte[] 
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

        public byte[] ProtocolData
        {
            get { return mProtocolData; }
            set
            {
                mProtocolData = value;
            }
        }

        public const byte CRC16_ADDRESS = 0x7E;
        private byte[] mCRC16 = new byte[] { 0xFF, 0xFF };
        public byte[] CRC16
        {
            get { return mCRC16; }
            set { mCRC16 = value; }
        }

        public const byte APP_DATA_ADDRESS = 0x80;
        private byte[] mAPP_DATA = null;
        public byte[] APP_DATA
        {
            get
            {
                if (mAPP_DATA == null)
                {
                    mAPP_DATA = NewByteArray(0xFF - 0x80, 0xFF);
                }
                return mAPP_DATA;
            }
            set { mAPP_DATA = value; }
        }

        private byte[] NewByteArray(int size, byte initialValue)
        {
            byte[] result = new byte[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = initialValue;
            }
            return result;
        }
        private const ushort POLY = 0x1021;

        public bool IsValidCRC16
        {
            get
            {
                ushort crc = (ushort)((CRC16[0] << 8) + CRC16[1]);
                ushort res = GetCRC16();
                return crc == res;
            }
        }

        public void CalculateCrc16()
        {
            ushort crc = GetCRC16();
            this.CRC16 = new byte[] { (byte)(crc >> 8), (byte)crc };
        }

        private ushort GetCRC16()
        {
            List<byte> data = new List<byte>();
            data.Add(this.REV);
            data.Add(this.CCAL);
            data.Add(this.PINS);
            data.Add(this.NVMCS);
            data.AddRange(this.SAWC);
            data.Add(this.SAWB);
            data.Add(this.NVMT);
            data.AddRange(this.NVMS);
            data.AddRange(this.NVMP);
            data.AddRange(this.UUID);
            data.AddRange(this.VID);
            data.AddRange(this.PID);
            data.Add(this.TXCAL1);
            data.Add(this.TXCAL2);
            data.AddRange(this.PUK);
            data.AddRange(this.PRK);
            data.AddRange(this.ProtocolData);
            ushort crc = 0x1D0F;
            foreach (byte b in data)
            {
                for (byte bitMask = 0x80; bitMask != 0; bitMask >>= 1)
                {
                    byte NewBit = (byte)(Convert.ToByte(((b & bitMask) != 0)) ^ Convert.ToByte(((crc & 0x8000) != 0)));
                    crc <<= 1;
                    if (NewBit != 0)
                    {
                        crc ^= POLY;
                    }
                }
            }
            return crc;
        }
        public static implicit operator NVRData(byte[] data)
        {
            if (data != null && data.Length > CRC16_ADDRESS + 2)
            {
                NVRData result = new NVRData();
                result.REV = data[REV_ADDRESS];
                result.CCAL = data[CCAL_ADDRESS];
                result.PINS = data[PINS_ADDRESS];
                result.NVMCS = data[NVMCS_ADDRESS];
                for (byte i = SAWC_ADDRESS; i < result.SAWC.Length + SAWC_ADDRESS; i++)
                {
                    result.SAWC[i - SAWC_ADDRESS] = data[i];
                }
                result.SAWB = data[SAWB_ADDRESS];
                result.NVMT = data[NVMT_ADDRESS];
                for (byte i = NVMS_ADDRESS; i < result.NVMS.Length + NVMS_ADDRESS; i++)
                {
                    result.NVMS[i - NVMS_ADDRESS] = data[i];
                }
                for (byte i = NVMP_ADDRESS; i < result.NVMP.Length + NVMP_ADDRESS; i++)
                {
                    result.NVMP[i - NVMP_ADDRESS] = data[i];
                }
                for (byte i = UUID_ADDRESS; i < result.UUID.Length + UUID_ADDRESS; i++)
                {
                    result.UUID[i - UUID_ADDRESS] = data[i];
                }
                for (byte i = VID_ADDRESS; i < result.VID.Length + VID_ADDRESS; i++)
                {
                    result.VID[i - VID_ADDRESS] = data[i];
                }
                for (byte i = PID_ADDRESS; i < result.PID.Length + PID_ADDRESS; i++)
                {
                    result.PID[i - PID_ADDRESS] = data[i];
                }
                result.TXCAL1 = data[TXCAL1_ADDRESS];
                result.TXCAL2 = data[TXCAL2_ADDRESS];
                for (byte i = PUK_ADDRESS; i < result.PUK.Length + PUK_ADDRESS; i++)
                {
                    result.PUK[i - PUK_ADDRESS] = data[i];
                }
                for (byte i = PRK_ADDRESS; i < result.PRK.Length + PRK_ADDRESS; i++)
                {
                    result.PRK[i - PRK_ADDRESS] = data[i];
                }
                for (byte i = PROTOCOL_DATA_ADDRESS; i < result.ProtocolData.Length + PROTOCOL_DATA_ADDRESS; i++)
                {
                    result.ProtocolData[i - PROTOCOL_DATA_ADDRESS] = data[i];
                }
                for (byte i = CRC16_ADDRESS; i < result.CRC16.Length + CRC16_ADDRESS; i++)
                {
                    result.CRC16[i - CRC16_ADDRESS] = data[i];
                }
                for (byte i = APP_DATA_ADDRESS; i < result.APP_DATA.Length + APP_DATA_ADDRESS; i++)
                {
                    result.APP_DATA[i - APP_DATA_ADDRESS] = data[i];
                }
                result.HW = data[HW_ADDRESS];

                //System.Diagnostics.Debug.WriteLine(result.ToString());
                return result;
            }
            else
            {
                throw new ApplicationException("Invalid NVR data.");
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("REV: " + Tools.ToHexString(this.REV));
            sb.AppendLine("CCAL: " + Tools.ToHexString(this.CCAL));
            sb.AppendLine("MTYP: " + Tools.ToHexString(this.PINS));
            sb.AppendLine("MREV: " + Tools.ToHexString(this.NVMCS));
            sb.AppendLine("SAWC: " + Tools.ToHexString(this.SAWC, ""));
            sb.AppendLine("SAWB: " + Tools.ToHexString(this.SAWB));
            sb.AppendLine("NVMT: " + Tools.ToHexString(this.NVMT));
            sb.AppendLine("NVMS: " + Tools.ToHexString(this.NVMS, ""));
            sb.AppendLine("NVMP: " + Tools.ToHexString(this.NVMP, ""));
            sb.AppendLine("UUID: " + Tools.ToHexString(this.UUID, ""));
            sb.AppendLine("IDVEN: " + Tools.ToHexString(this.VID, ""));
            sb.AppendLine("IDPROD: " + Tools.ToHexString(this.PID, ""));
            sb.AppendLine("TXCAL1: " + Tools.ToHexString(this.TXCAL1));
            sb.AppendLine("TXCAL2: " + Tools.ToHexString(this.TXCAL2));
            sb.AppendLine("PUK: " + Tools.ToHexString(this.PUK, ""));
            sb.AppendLine("PRK: " + Tools.ToHexString(this.PRK, ""));
            sb.AppendLine("CRC16: " + Tools.ToHexString(this.CRC16, ""));
            sb.AppendLine("HW: " + Tools.ToHexString(this.HW));
            return sb.ToString();
        }


        public static void ToBuffer(NVRData nvrData, byte[] buffer)
        {
            buffer[NVRData.REV_ADDRESS] = nvrData.REV;
            buffer[NVRData.CCAL_ADDRESS] = nvrData.CCAL;
            buffer[NVRData.PINS_ADDRESS] = nvrData.PINS;
            buffer[NVRData.NVMCS_ADDRESS] = nvrData.NVMCS;
            for (byte i = NVRData.SAWC_ADDRESS; i < nvrData.SAWC.Length + NVRData.SAWC_ADDRESS; i++)
            {
                buffer[i] = nvrData.SAWC[i - NVRData.SAWC_ADDRESS];
            }
            buffer[NVRData.SAWB_ADDRESS] = nvrData.SAWB;
            buffer[NVRData.NVMT_ADDRESS] = nvrData.NVMT;
            for (byte i = NVRData.NVMS_ADDRESS; i < nvrData.NVMS.Length + NVRData.NVMS_ADDRESS; i++)
            {
                buffer[i] = nvrData.NVMS[i - NVRData.NVMS_ADDRESS];
            }
            for (byte i = NVRData.NVMP_ADDRESS; i < nvrData.NVMP.Length + NVRData.NVMP_ADDRESS; i++)
            {
                buffer[i] = nvrData.NVMP[i - NVRData.NVMP_ADDRESS];
            }
            for (byte i = NVRData.UUID_ADDRESS; i < nvrData.UUID.Length + NVRData.UUID_ADDRESS; i++)
            {
                buffer[i] = nvrData.UUID[i - NVRData.UUID_ADDRESS];
            }
            for (byte i = NVRData.VID_ADDRESS; i < nvrData.VID.Length + NVRData.VID_ADDRESS; i++)
            {
                buffer[i] = nvrData.VID[i - NVRData.VID_ADDRESS];
            }
            for (byte i = NVRData.PID_ADDRESS; i < nvrData.PID.Length + NVRData.PID_ADDRESS; i++)
            {
                buffer[i] = nvrData.PID[i - NVRData.PID_ADDRESS];
            }
            buffer[NVRData.TXCAL1_ADDRESS] = nvrData.TXCAL1;
            buffer[NVRData.TXCAL2_ADDRESS] = nvrData.TXCAL2;
            for (byte i = NVRData.PUK_ADDRESS; i < nvrData.PUK.Length + NVRData.PUK_ADDRESS; i++)
            {
                buffer[i] = nvrData.PUK[i - NVRData.PUK_ADDRESS];
            }
            for (byte i = NVRData.PRK_ADDRESS; i < nvrData.PRK.Length + NVRData.PRK_ADDRESS; i++)
            {
                buffer[i] = nvrData.PRK[i - NVRData.PRK_ADDRESS];
            }
            for (byte i = NVRData.PROTOCOL_DATA_ADDRESS; i < nvrData.ProtocolData.Length + NVRData.PROTOCOL_DATA_ADDRESS; i++)
            {
                buffer[i] = nvrData.ProtocolData[i - NVRData.PROTOCOL_DATA_ADDRESS];
            }
            for (byte i = NVRData.CRC16_ADDRESS; i < nvrData.CRC16.Length + NVRData.CRC16_ADDRESS; i++)
            {
                buffer[i] = nvrData.CRC16[i - NVRData.CRC16_ADDRESS];
            }
            for (byte i = NVRData.APP_DATA_ADDRESS; i < nvrData.APP_DATA.Length + NVRData.APP_DATA_ADDRESS; i++)
            {
                buffer[i] = nvrData.APP_DATA[i - NVRData.APP_DATA_ADDRESS];
            }
            buffer[NVRData.HW_ADDRESS] = nvrData.HW;
        }
    }
}
