// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Enums;
using ZWave.Devices;
using ZWave.Events;
using System.Threading;
using ZWave.Framework;
using ZWave.Logging;
using ZWave.Exceptions;

namespace ZWave.SerialPortApplication.Devices
{
    public class Device : IDevice
    {
        //protected const int TIMEOUT = 180000;
        public event StatusChangedEventHandler ConnectionStatusChanged;
        public event DeviceAppCommandHandlerEventHandler ApplicationCommandHandlerEvent;

        private ISessionLayer mSessionLayer;
        public ISessionLayer SessionLayer
        {
            get
            {
                return mSessionLayer;
            }
        }

        protected Device()
        {
        }

        public Device(ISessionLayer sessionLayer)
        {
            mSessionLayer = sessionLayer;
            mFlash = new DeviceFlash(sessionLayer);
            mFlash.Device = this;
            mMemory = new DeviceMemory(sessionLayer);
            mMemory.Device = this;
        }

        protected void LogTopSession()
        {
            //SessionLayer.LogDataSource.AddTopSession(new LogSession(string.Format("{0} {1} {2}", Tools.CurrentDateTime.ToString("hh:mm:ss.ffff"), SessionLayer.SequenceNumber, Tools.GetMethodName(1))));
        }

        protected virtual void OnSessionLayerResponseReceivedCustomActions(ResponseReceivedEventArgs args)
        {
            //throw new NotImplementedException("The method or operation should be ovverided in inherited class.");
            if (args.CommandType == (byte)ProgrammerCommandTypes.FUNC_ID_BUTTON_PRESSED)
            {
                ApplicationCommandHandlerEvent?.Invoke(new DeviceAppCommandHandlerEventArgs(args.FrameBuffer, 0, 0, args.CommandType, false, args.IsSubstituted));
            }
        }

        #region IDevice Members

        #region Properties
        private byte mId;
        public byte Id
        {
            get
            {
                return mId;
            }
            set
            {
                mId = value;
            }
        }

        private byte mChipType = 0x00;
        public byte ChipType
        {
            get
            {
                return mChipType;
            }
            set
            {
                mChipType = value;
            }
        }

        public int SramSize
        {
            get
            {
                if (this.ChipType == (byte)ChipTypes.ZW050x)
                    return 4 * 1024;

                else
                    return 0;
            }
        }

        public int FlashSize
        {
            get
            {
                return Constants.FLASH_SIZE;
            }
        }

        private string mSerialPort;
        public string SerialPort
        {
            get
            {
                return mSerialPort;
            }
            set
            {
                mSerialPort = value;
            }
        }

        private IDeviceFlash mFlash;
        public IDeviceFlash Flash
        {
            get
            {
                return mFlash;
            }
            set
            {
                mFlash = value;
            }
        }

        private IDeviceMemory mMemory;
        public IDeviceMemory Memory
        {
            get
            {
                return mMemory;
            }
            set
            {
                mMemory = value;
            }
        }

        private ConnectionStatuses mConnectionStatus;
        public ConnectionStatuses ConnectionStatus
        {
            get
            {
                return mConnectionStatus;
            }
            set
            {
                StatusChangedArgs args = new StatusChangedArgs((int)mConnectionStatus, (int)value);
                mConnectionStatus = value;
                ConnectionStatusChanged?.Invoke(args);
            }
        }

       

        #endregion

        public void Open(string portName)
        {
            mSessionLayer.Open(portName);
            this.SerialPort = portName;
            ConnectionStatus = ConnectionStatuses.Opened;
        }

        public void Close()
        {
            mSessionLayer.Close();
            ConnectionStatus = ConnectionStatuses.Closed;
        }

        public void Reset()
        {
            byte[] response = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_ZW050x_RESET, new byte[] { this.ChipType, mSessionLayer.SequenceNumber });
            if (response.Length >= 1 && response[0] == 0x31)
            {
                //TODO check result
            }
        }

        public void SetBusyLedState(bool state)
        {
            //mSessionLayer.LogDataSource.AddTopSession(new LogSession(string.Format("{0} {1} ({2})", Type.ToString(), "Led SetState", state.ToString())));
            if (state)
            {
                mSessionLayer.ExecuteNonRequest((byte)ProgrammerCommandTypes.FUNC_ID_PROGRAMMER_SET_LED, new byte[] { (byte)LedTypes.Busy, (byte)LedStates.ON, mSessionLayer.SequenceNumber });
            }
            else
            {
                //mSessionLayer.ExecuteNonRequest(new byte[] { 0x01, 0x00, 0x07, 0x00, 0x53, 0x02, 0x00, 0x05, 0xac });
                mSessionLayer.ExecuteNonRequest((byte)ProgrammerCommandTypes.FUNC_ID_PROGRAMMER_SET_LED, new byte[] { (byte)LedTypes.Busy, (byte)LedStates.OFF, mSessionLayer.SequenceNumber });
            }
        }
      
        public bool SetProgrammingMode(bool enable)
        {
            bool ret = true;
            LogTopSession();
            try
            {
                if (enable)
                {
                    byte[] request = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_ZW0x0x_PROG_ENABLE, new byte[] { mSessionLayer.SequenceNumber });
                    if (request.Length > 0)
                    {
                        //mSyncCount = request[0];
                    }
                }
                else
                {

                    byte[] request = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_ZW0x0x_PROG_RELEASE, new byte[] { this.ChipType, mSessionLayer.SequenceNumber });
                    //if (request.Length > 0)
                    //{
                    //    mSyncCount = request[0];
                    //}
                }
            }
            catch (RequestTimeoutException rEx)
            {
                ret = false;
            }
            return ret;
        }

        public void EraseChip(bool isAsic)
        {
            this.Flash.Erase(isAsic);
        }

        public void ReadSignatureBits()
        {
            LogTopSession();
            byte[] signature = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_ZW0x0x_READ_SIG_BYTE, new byte[] { 0x00, mSessionLayer.SequenceNumber });
            if (signature.Length == 8)
            {
                if (signature[0] == 0x7f && signature[1] == 0x7f && signature[2] == 0x7f && signature[3] == 0x7f
                && signature[4] == 0x1f && signature[5] == 0x04)
                {
                    Constants.BLANK_VALUE = 0xFF;
                    Constants.FLASH_SIZE = 131072;
                    Constants.PAGES_IN_FLASH = 64;
                    this.ChipType = (byte)ChipTypes.ZW050x;
                    Constants.SRAM_PAGE_SIZE = 2048;
                    this.Flash.Pages = new List<IFlashPage>(new IFlashPage[Constants.PAGES_IN_FLASH]);
                }
                else if (signature[3] == 0x7F && signature[4] == 0x1F)
                {
                    Constants.BLANK_VALUE = 0xFF;
                    Constants.FLASH_SIZE = 32768;
                    Constants.PAGES_IN_FLASH = 128;
                    this.Flash.Pages = new List<IFlashPage>(new IFlashPage[Constants.PAGES_IN_FLASH]);

                    if (signature[6] >= 0x06)
                    {
                        this.ChipType = (byte)ChipTypes.ZW030x;
                    }
                    else
                    {
                        this.ChipType = (byte)ChipTypes.ZW020x;
                    }
                }
                else if (signature[3] == 0x9E && signature[4] == 0x95)
                {
                    Constants.BLANK_VALUE = 0xFF;
                    Constants.FLASH_SIZE = 32768;
                    Constants.PAGES_IN_FLASH = 256;
                    this.Flash.Pages = new List<IFlashPage>(new IFlashPage[Constants.PAGES_IN_FLASH]);
                    this.ChipType = (byte)ChipTypes.ZW010x;
                }
                else
                {
                    this.ChipType = (byte)ChipTypes.UNKNOWN;
                }
                Constants.BYTES_IN_PAGE = (Constants.FLASH_SIZE / Constants.PAGES_IN_FLASH);
            }
        }

        public IFlashSettings FlashReadRfOptions()
        {
            IFlashSettings result = null;
            int pageIndex = Constants.PAGES_IN_FLASH - 1;
            int offset = Constants.FLASH_SIZE - Constants.FLASH_SIZE / Constants.PAGES_IN_FLASH;
            if (this.ChipType == (byte)ChipTypes.ZW050x)
            {
                pageIndex = 15;//last sector in 64K
                offset = 0x7800;
            }
            IFlashPage page = this.Flash.ReadPage(true, pageIndex);
            if (page != null)
            {
                result = new FlashSettings();
                result.ParseBuffer(this.ChipType, offset, page.Buffer);
            }
            return result;
        }

        public WriteRfOptionsStatuses FlashWriteRfOptions(IFlashSettings flashSettings)
        {
            WriteRfOptionsStatuses result = WriteRfOptionsStatuses.NoErrors;
            int pageIndex = Constants.PAGES_IN_FLASH - 1;
            int offset = Constants.FLASH_SIZE - Constants.FLASH_SIZE / Constants.PAGES_IN_FLASH;
            if (this.ChipType == (byte)ChipTypes.ZW050x)
            {
                pageIndex = 15;//last sector in 64K
                offset = 0x7800;
            }
            if (flashSettings != null)
            {
                IFlashPage page = this.Flash.ReadPage(true, pageIndex);
                if (page != null)
                {
                    flashSettings.StoreToBuffer(this.ChipType, offset, page.Buffer);
                    this.SetWriteCycleTime();
                    //TODO: !!! Erase page function is not aviable for ZW010x and returns failure for this chip. So, for ZW010x, we need other solution!!!
                    this.Flash.ErasePage(pageIndex);
                    page.Address = pageIndex;
                    if (!page.Write(true, false))
                    {
                        result = WriteRfOptionsStatuses.CantWriteAppRfSettings;
                    }
                }
                else
                {
                    result = WriteRfOptionsStatuses.CantReadAppRfSettings;
                }
            }
            else
            {
                result = WriteRfOptionsStatuses.UndefinedRfSettings;
            }
            return result;
        }

        public bool WriteNvrByte(byte index, byte data)
        {
            if (data != 0xFF)
            {
                byte sNum = mSessionLayer.SequenceNumber;
                byte[] response = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_ZW050x_SET_NVR, new byte[] { index, data, sNum });
            }
            return true;
        }

        public byte[] ReadNvr(byte partNo)
        {
            List<byte> result = new List<byte>();

            for (int i = Constants.NVR_START_ADDRESS; i < Constants.NVR_END_ADDRESS; i++)
            {
                if (ReadNvrByte((byte)i, out byte data, partNo))
                {
                    result.Add(data);
                }
            }
            if (result.Count > 0)
            {
                result.InsertRange(0, new byte[9]);
                return result.ToArray();
            }
            else
            {
                return null;
            }
        }

        public bool ReadNvrByte(byte index, out byte data, byte partNo)
        {
            data = 0x00;
            byte sNum = mSessionLayer.SequenceNumber;
            byte[] response = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_ZW050x_READ_NVR, new byte[] { partNo, index, sNum });
            if (response != null && response.Length == 2 && response[1] == sNum)
            {
                data = response[0];
                return true;
            }
            return false;
        }

        public byte ReadLockBits()
        {
            LogTopSession();
            byte lockBits = 0xff;
            byte seqNum = mSessionLayer.SequenceNumber;
            byte[] request = mSessionLayer.ExecuteRequest(
                (byte)ProgrammerCommandTypes.FUNC_ID_ZW0x0x_READ_LOCKBITS, new byte[] { this.ChipType, seqNum });
            if (request.Length == 2)
            {
                if (request[1] == seqNum)
                    lockBits = request[0];
            }
            return lockBits;
        }

        public bool WriteLockBits(byte lockBits)
        {
            LogTopSession();
            bool res = false;
            byte seqNum = mSessionLayer.SequenceNumber;
            byte[] request = mSessionLayer.ExecuteRequest(
                (byte)ProgrammerCommandTypes.FUNC_ID_ZW0x0x_WRITE_LOCKBITS, new byte[] { this.ChipType, lockBits, seqNum });
            if (request.Length == 2)
            {
                res = ((request[0] == (byte)FlashProgrammingStatuses.DONE ||
                        request[0] == (byte)FlashProgrammingStatuses.SUCCESS) && request[1] == seqNum);
            }
            return res;
        }

        public bool SetWriteCycleTime()
        {
            return SetWriteCycleTime(-1.0F);
        }

        public bool SetWriteCycleTime(float xtalFrequencyMHz)
        {
            LogTopSession();
            bool res = false;
            byte seqNum = mSessionLayer.SequenceNumber;
            byte[] request = mSessionLayer.ExecuteRequest(
                (byte)ProgrammerCommandTypes.FUNC_ID_ZW0x0x_SET_WRITE_CYCLE,
                new byte[] { CalculateWriteCycle(xtalFrequencyMHz), seqNum });
            if (request.Length == 2)
            {
                res = ((request[0] == (byte)FlashProgrammingStatuses.DONE ||
                        request[0] == (byte)FlashProgrammingStatuses.SUCCESS) && request[1] == seqNum);
            }
            return res;
        }

        public bool SetMode(WorkingModes mode)
        {
            LogTopSession();
            bool res = false;
            if (mode == WorkingModes.Development)
            {
                //set development mode
                byte seqNum = mSessionLayer.SequenceNumber;
                byte[] request = mSessionLayer.ExecuteRequest(
                    (byte)ProgrammerCommandTypes.FUNC_ID_ZW0x0x_DEV_MODE_ENABLE, new byte[] { this.ChipType, seqNum });
                if (request.Length == 2)
                {
                    res = ((request[0] == (byte)FlashProgrammingStatuses.DONE ||
                            request[0] == (byte)FlashProgrammingStatuses.SUCCESS) && request[1] == seqNum);
                }
            }
            else if (mode == WorkingModes.ExecuteOutOfSram)
            {
                //set execute out of sram mode
                byte seqNum = mSessionLayer.SequenceNumber;
                byte[] request = mSessionLayer.ExecuteRequest(
                    (byte)ProgrammerCommandTypes.FUNC_ID_ZW0x0x_SRAM_EXECUTE, new byte[] { this.ChipType, seqNum });
                if (request.Length == 2)
                {
                    res = ((request[0] == (byte)FlashProgrammingStatuses.DONE ||
                            request[0] == (byte)FlashProgrammingStatuses.SUCCESS) && request[1] == seqNum);
                }
            }
            return res;
        }

        public bool SetModemBits()
        {
            LogTopSession();
            bool res = false;
            //Set the bit that identifies the Chip as a �Modem� device
            byte seqNum = mSessionLayer.SequenceNumber;
            byte[] request = mSessionLayer.ExecuteRequest(
                (byte)ProgrammerCommandTypes.FUNC_ID_ZW0x0x_MODEM_BIT_WRITE, new byte[] { this.ChipType, seqNum });
            if (request.Length == 2)
            {
                res = ((request[0] == (byte)FlashProgrammingStatuses.DONE ||
                        request[0] == (byte)FlashProgrammingStatuses.SUCCESS) && request[1] == seqNum);
            }
            return res;
        }

        public bool SetBootLoaderMode(bool enable)
        {
            LogTopSession();
            bool result = false;
            if (enable)
            {
                byte seqNum = mSessionLayer.SequenceNumber;
                byte[] request = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_M128_ENTER_PROG_MODE, new byte[] { seqNum });
                if (request.Length == 2)
                {
                    if (request[0] == (byte)FlashProgrammingStatuses.DONE &&
                        request[1] == seqNum)
                    {
                        Thread.Sleep(250);
                        byte checksum = 0xff;
                        byte[] sigBytes = { 0xa5, 0xfe, 0x1a, 0x56 };// random values
                        for (int i = 0; i < sigBytes.Length; i++)
                        {
                            checksum ^= sigBytes[i];
                        }
                        seqNum = mSessionLayer.SequenceNumber;
                        //send enter bootloader command again, but with bootloader signature and its sckecksum.
                        request = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_M128_ENTER_PROG_MODE,
                            new byte[] { sigBytes[0], sigBytes[1], sigBytes[2], sigBytes[3], checksum, seqNum });
                        if (request.Length == 2)
                        {
                            if (request[0] == (byte)FlashProgrammingStatuses.DONE &&
                                request[1] == seqNum)
                            {
                                result = true;
                            }
                        }
                    }
                }
            }
            else
            {
                byte seqNum = mSessionLayer.SequenceNumber;
                byte[] request = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_EXIT_PROG_MODE, new byte[] { seqNum });
                if (request.Length == 2)
                {
                    if (request[0] == (byte)FlashProgrammingStatuses.DONE &&
                        request[1] == seqNum)
                    {
                        result = true;
                    }
                }
                Thread.Sleep(200);//Delay for re-start Atmel Firmware
            }
            return result;
        }

        public bool GetCurrentFirmwareVersion(out int version, out int revision)
        {
            LogTopSession();
            bool result = false;
            version = 0;
            revision = 0;
            byte seqNum = mSessionLayer.SequenceNumber;
            byte[] request = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_M128_GET_SW_VER, new byte[] { seqNum });
            if (request.Length == 3)
            {
                if (request[2] == seqNum)
                {
                    version = request[0];
                    revision = request[1];
                    result = true;
                }
            }
            return result;
        }

        public byte CalculateWriteCycle()
        {
            return CalculateWriteCycle(-1.0F);
        }

        public byte CalculateWriteCycle(float xtalFrequencyMHz)
        {
            byte c = 0;
            if (this.ChipType == (byte)ChipTypes.ZW010x)
            {
                if (xtalFrequencyMHz == -1.0F)
                    xtalFrequencyMHz = 7.3F;	//3.6F;
                c = (byte)(30.0 * xtalFrequencyMHz / (16.0 * 1.0)); // 30 uS / (16*clock_periode)

            }
            else if (this.ChipType == (byte)ChipTypes.ZW020x ||
                      this.ChipType == (byte)ChipTypes.ZW030x)
            {
                if (xtalFrequencyMHz == -1.0F)
                    xtalFrequencyMHz = 32.0F;
                c = (byte)(30.0 * xtalFrequencyMHz / (16.0 * 4.0)); // 30 uS / (16*4*Tclock_periode)
            }
            return c;
        }

        public bool Calibration(bool enable)
        {
            LogTopSession();
            if (enable)
            {
                byte[] request = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_CALIBRATION_START, new byte[] { this.ChipType, mSessionLayer.SequenceNumber });

            }
            else
            {

                mSessionLayer.ExecuteNonRequest((byte)ProgrammerCommandTypes.FUNC_ID_CALIBRATION_STOP, new byte[] { this.ChipType, mSessionLayer.SequenceNumber });

            }
            return true;
        }

        public bool WriteLockBit(int num, byte lockData)
        {
            byte[] response = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_ZW5XX_WRITE_LOCKBIT,
                new byte[] { this.ChipType, (byte)num, lockData, mSessionLayer.SequenceNumber });
            if (response.Length >= 1 && response[0] == 0x31)
            {
                return true;
            }
            return false;//TODO
        }
        public byte ReadLockBit(int num)
        {
            byte[] response = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_ZW5XX_READ_LOCKBIT,
                new byte[] { this.ChipType, (byte)num, mSessionLayer.SequenceNumber });
            if (response.Length >= 1)
            {
                return response[0];
            }
            return 0;//TODO
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);

        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                SessionLayer.ResponseReceived = null;
            }
        }

        #endregion
    }
}
