// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Devices;
using ZWave.Enums;

namespace ZWave.SerialPortApplication.Devices
{
    public class FlashSettings : IFlashSettings
    {
        #region IFlashSettings Members

        private byte mFrequency = MemoryLayout.APP_DEFAULT_FREQ;
        public byte Frequency
        {
            get { return mFrequency; }
            set { mFrequency = value; IsFrequencySet = true; }
        }

        private bool mIsFrequencySet = false;
        public bool IsFrequencySet
        {
            get { return mIsFrequencySet; }
            set { mIsFrequencySet = value; }
        }

        private byte mNormalPower = MemoryLayout.APP_DEFAULT_NORM_POWER;
        public byte NormalPower
        {
            get { return mNormalPower; }
            set { mNormalPower = value; IsNormalPowerSet = true; }
        }

        private byte mNormalPowerCh0 = MemoryLayout.APP_DEFAULT_NORM_POWER;
        public byte NormalPowerCh0
        {
            get { return mNormalPowerCh0; }
            set { mNormalPowerCh0 = value; IsNormalPowerSetCh0 = true; }
        }

        private byte mNormalPowerCh1 = MemoryLayout.APP_DEFAULT_NORM_POWER;
        public byte NormalPowerCh1
        {
            get { return mNormalPowerCh1; }
            set { mNormalPowerCh1 = value; IsNormalPowerSetCh1 = true; }
        }

        private byte mNormalPowerCh2 = MemoryLayout.APP_DEFAULT_NORM_POWER;
        public byte NormalPowerCh2
        {
            get { return mNormalPowerCh2; }
            set { mNormalPowerCh2 = value; IsNormalPowerSetCh2 = true; }
        }

        private bool mIsNormalPowerSet = false;
        public bool IsNormalPowerSet
        {
            get { return mIsNormalPowerSet; }
            set { mIsNormalPowerSet = value; }
        }

        private bool mIsNormalPowerSetCh0 = false;
        public bool IsNormalPowerSetCh0
        {
            get { return mIsNormalPowerSetCh0; }
            set { mIsNormalPowerSetCh0 = value; }
        }

        private bool mIsNormalPowerSetCh1 = false;
        public bool IsNormalPowerSetCh1
        {
            get { return mIsNormalPowerSetCh1; }
            set { mIsNormalPowerSetCh1 = value; }
        }

        private bool mIsNormalPowerSetCh2 = false;
        public bool IsNormalPowerSetCh2
        {
            get { return mIsNormalPowerSetCh2; }
            set { mIsNormalPowerSetCh2 = value; }
        }

        private byte mLowPower = MemoryLayout.APP_DEFAULT_LOW_POWER;
        public byte LowPower
        {
            get { return mLowPower; }
            set { mLowPower = value; IsLowPowerSet = true; }
        }

        private byte mLowPowerCh0 = MemoryLayout.APP_DEFAULT_LOW_POWER;
        public byte LowPowerCh0
        {
            get { return mLowPowerCh0; }
            set { mLowPowerCh0 = value; IsLowPowerSetCh0 = true; }
        }

        private byte mLowPowerCh1 = MemoryLayout.APP_DEFAULT_LOW_POWER;
        public byte LowPowerCh1
        {
            get { return mLowPowerCh1; }
            set { mLowPowerCh1 = value; IsLowPowerSetCh1 = true; }
        }

        private byte mLowPowerCh2 = MemoryLayout.APP_DEFAULT_LOW_POWER;
        public byte LowPowerCh2
        {
            get { return mLowPowerCh2; }
            set { mLowPowerCh2 = value; IsLowPowerSetCh2 = true; }
        }

        private bool mIsLowPowerSet = false;
        public bool IsLowPowerSet
        {
            get { return mIsLowPowerSet; }
            set { mIsLowPowerSet = value; }
        }

        private bool mIsLowPowerSetCh0 = false;
        public bool IsLowPowerSetCh0
        {
            get { return mIsLowPowerSetCh0; }
            set { mIsLowPowerSetCh0 = value; }
        }

        private bool mIsLowPowerSetCh1 = false;
        public bool IsLowPowerSetCh1
        {
            get { return mIsLowPowerSetCh1; }
            set { mIsLowPowerSetCh1 = value; }
        }

        private bool mIsLowPowerSetCh2 = false;
        public bool IsLowPowerSetCh2
        {
            get { return mIsLowPowerSetCh2; }
            set { mIsLowPowerSetCh2 = value; }
        }

        private byte mRxMatch = MemoryLayout.APP_DEFAULT_RX_MATCH;
        public byte RxMatch
        {
            get { return mRxMatch; }
            set { mRxMatch = value; IsRxMatchSet = true; }
        }

        private bool mIsRxMatchSet = false;
        public bool IsRxMatchSet
        {
            get { return mIsRxMatchSet; }
            set { mIsRxMatchSet = value; }
        }


        private byte mTxMatch = MemoryLayout.APP_DEFAULT_TX_MACTH;
        public byte TxMatch
        {
            get { return mTxMatch; }
            set { mTxMatch = value; IsTxMatchSet = true; }
        }

        private sbyte mCalibrationValue = 0;
        public sbyte CalibrationValue
        {
            get { return mCalibrationValue; }
            set { mCalibrationValue = value; }
        }


        private bool mIsTxMatchSet = false;
        public bool IsTxMatchSet
        {
            get { return mIsTxMatchSet; }
            set { mIsTxMatchSet = value; }
        }

        public void ParseBuffer(byte chipType, int bufferAddress, byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("Wrong page for FlashSettings.ParsePageBuffer");
            }
            if (buffer.Length == 0)
            {
                throw new ArgumentNullException("Wrong page for FlashSettings.ParsePageBuffer");
            }
            int offset;
            if (bufferAddress > (uint)MemoryLayout.FlashLayout.FLASH_APPL_TABLE_OFFSET)
            {
                throw new ArgumentNullException("Wrong page for FlashSettings.ParsePageBuffer");
            }
            offset = (int)(MemoryLayout.FlashLayout.FLASH_APPL_TABLE_OFFSET - bufferAddress);
            if (buffer[offset + (int)MemoryLayout.FlashLayout.FLASH_APPL_MAGIC_VALUE_OFFS] != MemoryLayout.RF_MAGIC_VALUE)
            {
                //throw new ArgumentNullException("MemoryLayout.RF_MAGIC_VALUE is not found in RF Options");
            }
            this.Frequency = buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_FREQ_OFFS];

            this.LowPower = buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_LOW_OFFS];
            this.NormalPower = buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_NORM_OFFS];

            this.LowPowerCh0 = buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_LOW_POWER_OFFS_0];
            this.NormalPowerCh0 = buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_NORM_POWER_OFFS_0];
            this.LowPowerCh1 = buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_LOW_POWER_OFFS_1];
            this.NormalPowerCh1 = buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_NORM_POWER_OFFS_1];
            this.LowPowerCh2 = buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_LOW_POWER_OFFS_2];
            this.NormalPowerCh2 = buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_NORM_POWER_OFFS_2];

            if (chipType == (byte)ChipTypes.ZW010x)
            {
                this.RxMatch = buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_RX_MATCH_OFFS];
                this.TxMatch = buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_TX_MACTH_OFFS];
            }
        }

        public bool StoreToBuffer(byte chipType, int bufferAddress, byte[] buffer)
        {
            bool isBufferChanged = false;
            if (buffer == null)
            {
                throw new ArgumentNullException("Wrong page for FlashSettings.StoreToPageBuffer");
            }
            if (buffer.Length == 0)
            {
                throw new ArgumentNullException("Wrong page for FlashSettings.StoreToPageBuffer");
            }
            int offset, offsetRF;
            if (bufferAddress > (uint)MemoryLayout.FlashLayout.FLASH_APPL_TABLE_OFFSET ||
                bufferAddress > (uint)MemoryLayout.FlashLayout.FLASH_RF_TABLE_OFFSET)
            {
                throw new ArgumentNullException("Wrong page for FlashSettings.StoreToPageBuffer");
            }
            offset = (int)MemoryLayout.FlashLayout.FLASH_APPL_TABLE_OFFSET - bufferAddress;
            offsetRF = (int)MemoryLayout.FlashLayout.FLASH_RF_TABLE_OFFSET - bufferAddress;

            if (this.IsFrequencySet)
            {
                buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_FREQ_OFFS] = this.Frequency;
                isBufferChanged = true;
                if (chipType == (byte)ChipTypes.ZW020x || chipType == (byte)ChipTypes.ZW030x)
                {
                    if (this.Frequency != MemoryLayout.RF_DEFAULT)
                    {
                        buffer[offsetRF + (uint)MemoryLayout.FlashLayout.FLASH_RF_TABLE_NORM_PWR_OFFS] = MemoryLayout.PowerTable[this.Frequency, 0];
                        buffer[offsetRF + (uint)MemoryLayout.FlashLayout.FLASH_RF_TABLE_LOW_PWR_OFFS] = MemoryLayout.PowerTable[this.Frequency, 1];
                        for (int i = 0; i < MemoryLayout.FrequencyTable.GetLength(1); i++)
                        {
                            buffer[offsetRF + (uint)MemoryLayout.FlashLayout.FLASH_RF_TABLE_FREQ_OFFS + i] = MemoryLayout.FrequencyTable[this.Frequency, i];
                        }
                    }
                }
            }
            if (this.IsLowPowerSet)
            {
                buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_LOW_OFFS] = this.LowPower;
                isBufferChanged = true;
            }
            if (this.IsNormalPowerSet)
            {
                buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_NORM_OFFS] = this.NormalPower;
                isBufferChanged = true;
            }

            if (this.IsLowPowerSetCh0)
            {
                buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_LOW_POWER_OFFS_0] = this.LowPowerCh0;
                isBufferChanged = true;
            }
            if (this.IsNormalPowerSetCh0)
            {
                buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_NORM_POWER_OFFS_0] = this.NormalPowerCh0;
                isBufferChanged = true;
            }

            if (this.IsLowPowerSetCh1)
            {
                buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_LOW_POWER_OFFS_1] = this.LowPowerCh1;
                isBufferChanged = true;
            }
            if (this.IsNormalPowerSetCh1)
            {
                buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_NORM_POWER_OFFS_1] = this.NormalPowerCh1;
                isBufferChanged = true;
            }

            if (this.IsLowPowerSetCh2)
            {
                buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_LOW_POWER_OFFS_2] = this.LowPowerCh2;
                isBufferChanged = true;
            }
            if (this.IsNormalPowerSetCh2)
            {
                buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_NORM_POWER_OFFS_2] = this.NormalPowerCh2;
                isBufferChanged = true;
            }

            if (chipType == (byte)ChipTypes.ZW010x)
            {
                if (this.IsRxMatchSet)
                {
                    buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_RX_MATCH_OFFS] = this.RxMatch;
                    isBufferChanged = true;
                }
                if (this.IsTxMatchSet)
                {
                    buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_TX_MACTH_OFFS] = this.TxMatch;
                    isBufferChanged = true;
                }
            }
            if (isBufferChanged)
            {
                if (chipType < (byte)ChipTypes.ZW050x)
                    buffer[offset + (uint)MemoryLayout.FlashLayout.FLASH_APPL_MAGIC_VALUE_OFFS] = MemoryLayout.RF_MAGIC_VALUE;

            }
            return isBufferChanged;
        }
        #endregion
    }
}
