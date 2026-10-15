// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Devices;
using ZWave.Enums;
using ZWave.Framework;

namespace ZWave.SerialPortApplication.Devices
{
    public class FlashPage : IFlashPage
    {
        private ISessionLayer mSessionLayer;

        public FlashPage(ISessionLayer sessionLayer, IDeviceFlash flash)
        {
            mSessionLayer = sessionLayer;
            Flash = flash;
        }
        private IDeviceFlash mFlash;
        public IDeviceFlash Flash
        {
            get { return mFlash; }
            set { mFlash = value; }
        }
        private byte[] mBuffer;
        public byte[] Buffer
        {
            get { return mBuffer; }
            set { mBuffer = value; }
        }
        private int mAddress;

        public int Address
        {
            get { return mAddress; }
            set { mAddress = value; }
        }

        #region IFlashPage Members
        public bool Write(bool isAsic, bool verify)
        {
            bool result = false;
            int byteNo;
            bool program = false;
            int pageSize;
            if (isAsic)
            {
                pageSize = Constants.BYTES_IN_PAGE;
            }
            else
            {
                pageSize = Constants.FIRMWARE_PAGE_SIZE;
            }
            //if (Flash.Device.ChipType != (byte)ChipTypes.ZW050x)
            //{
            for (byteNo = 0; byteNo < pageSize; byteNo++)
            {
                if (this.Buffer[byteNo] != Constants.BLANK_VALUE)
                {
                    program = true;
                    break;
                }
            }
            if (program)
            {
                //find first address
                short firstAddress = 0;
                short lastAddress = 0;

                for (int i = 0; i < this.Buffer.Length; i++)
                {
                    if (this.Buffer[i] != Constants.BLANK_VALUE)
                    {
                        firstAddress = (short)i;
                        break;
                    }
                }
                //find last address
                for (int i = this.Buffer.Length - 1; i >= 0; i--)
                {
                    if (this.Buffer[i] != Constants.BLANK_VALUE)
                    {
                        lastAddress = (short)i;
                        break;
                    }
                }
                if (Flash.Device.ChipType == (byte)ChipTypes.ZW050x)
                {
                    byte[] buffer1 = new byte[this.Buffer.Length / 2];
                    Array.Copy(this.Buffer, 0, buffer1, 0, buffer1.Length);
                    byte[] buffer2 = new byte[this.Buffer.Length / 2];
                    Array.Copy(this.Buffer, buffer1.Length, buffer2, 0, buffer2.Length);
                    List<byte> _buffer1 = new List<byte>(buffer1);
                    List<byte> _buffer2 = new List<byte>(buffer2);

                    for (int i = _buffer2.Count - 1; i >= 0; i--)
                    {
                        if (_buffer2[i] == Constants.BLANK_VALUE)
                        {
                            _buffer2.RemoveAt(i);
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (_buffer2.Count == 0)
                    {
                        for (int i = _buffer1.Count - 1; i >= 0; i--)
                        {
                            if (_buffer1[i] == Constants.BLANK_VALUE)
                            {
                                _buffer1.RemoveAt(i);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    if (_buffer1.Count < this.Buffer.Length / 2)
                    {
                        if (_buffer1.Count % 3 == 0)
                        {
                            _buffer1.Add(Constants.BLANK_VALUE);
                        }
                        else if (_buffer1.Count % 3 == 1)
                        {
                            _buffer1.Add(Constants.BLANK_VALUE);
                            _buffer1.Add(Constants.BLANK_VALUE);
                            _buffer1.Add(Constants.BLANK_VALUE);
                        }
                        else if (_buffer1.Count % 3 == 2)
                        {
                            _buffer1.Add(Constants.BLANK_VALUE);
                            _buffer1.Add(Constants.BLANK_VALUE);
                        }
                    }
                    if (_buffer2.Count > 0 && _buffer2.Count < (this.Buffer.Length / 2) + 1)
                    {
                        if (_buffer2.Count % 3 == 1)
                        {
                            _buffer2.Add(Constants.BLANK_VALUE);
                            _buffer2.Add(Constants.BLANK_VALUE);
                        }
                        else if (_buffer2.Count % 3 == 2)
                        {
                            _buffer2.Add(Constants.BLANK_VALUE);
                        }
                    }
                    buffer1 = _buffer1.ToArray();
                    buffer2 = _buffer2.ToArray();

                    result = WriteInner500(isAsic, verify, (byte)this.Flash.Device.ChipType, buffer1, this.Address * 2, firstAddress, lastAddress);
                    result &= WriteInner500(isAsic, verify, (byte)this.Flash.Device.ChipType, buffer2, this.Address * 2 + 1, firstAddress, lastAddress);

                }
                else
                {
                    result = WriteInner(isAsic, verify, (byte)this.Flash.Device.ChipType, this.Buffer, this.Address);
                }
            }
            if (program & verify)
            {
                return result;
            }
            return true;
        }

        private bool WriteInner500(bool isAsic, bool verify, byte chipType, byte[] buffer, int address, short firstAddress, short lastAddress)
        {
            ProgrammerCommandTypes cmd;
            byte[] request = new byte[8 + buffer.Length];
            if (isAsic)
            {
                cmd = ProgrammerCommandTypes.FUNC_ID_ZW0x0x_WRITE_PAGE;
                request[0] = chipType;
                request[1] = (byte)address;
            }
            else
            {
                cmd = ProgrammerCommandTypes.FUNC_ID_M128_BLOCK_WRITE;
                request[0] = (byte)(address >> 8);
                request[1] = (byte)(address & 0xFF);
            }
            if (verify)
            {
                request[2] = 1;
            }
            else
            {
                request[2] = 0;
            }
            request[3] = (byte)(firstAddress >> 8);
            request[4] = (byte)firstAddress;

            request[5] = (byte)(lastAddress >> 8);
            request[6] = (byte)lastAddress;

            for (int i = 0; i < buffer.Length; i++)
            {
                request[i + 7] = buffer[i];
            }
            request[buffer.Length + 7] = mSessionLayer.SequenceNumber;
            byte[] response = mSessionLayer.ExecuteRequest((byte)cmd, request);
            if (response.Length > 0)
                return (response[0] == (byte)WritePageStatuses.Succeeded);
            else
                return false;
        }

        private bool WriteInner(bool isAsic, bool verify, byte chipType, byte[] buffer, int address)
        {
            ProgrammerCommandTypes cmd;
            byte[] request = new byte[4 + buffer.Length];
            if (isAsic)
            {
                cmd = ProgrammerCommandTypes.FUNC_ID_ZW0x0x_WRITE_PAGE;
                request[0] = chipType;
                request[1] = (byte)address;
            }
            else
            {
                cmd = ProgrammerCommandTypes.FUNC_ID_M128_BLOCK_WRITE;
                request[0] = (byte)(address >> 8);
                request[1] = (byte)(address & 0xFF);
            }
            if (verify)
            {
                request[2] = 1;
            }
            else
            {
                request[2] = 0;
            }
            for (int i = 0; i < buffer.Length; i++)
            {
                request[i + 3] = buffer[i];
            }
            request[buffer.Length + 3] = mSessionLayer.SequenceNumber;
            byte[] response = mSessionLayer.ExecuteRequest((byte)cmd, request);
            if (response.Length > 0)
                return (response[0] == (byte)WritePageStatuses.Succeeded);
            else
                return false;
        }

        public bool Compare(IFlashPage page, bool ignoreCalibrationValue)
        {
            bool result = true;
            if (page.Buffer.Length == this.Buffer.Length)
            {
                for (int i = 0; i < page.Buffer.Length; i++)
                {

                    if (page.Buffer[i] != this.Buffer[i])
                    {
                        if (ignoreCalibrationValue == true)
                        {
                            if (i != Constants.CALIBRATION_VALUE_OTP_POS)
                            {
                                result = false;
                                break;
                            }
                        }
                        else
                        {
                            result = false;
                            break;
                        }
                    }
                }
            }
            else
            {
                result = false;
            }
            return result;
        }

        #endregion
    }
}
