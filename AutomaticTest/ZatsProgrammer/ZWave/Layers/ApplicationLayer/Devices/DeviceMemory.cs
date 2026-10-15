// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Devices;
using ZWave.Enums;
using ZWave.Framework;
using ZWave.Logging;
using ZWave.Events;
using ZWave.Exceptions;
using System.Linq;

namespace ZWave.SerialPortApplication.Devices
{
    public class DeviceMemory : IDeviceMemory
    {
        public event ProgressChangedEventHandler ProgressChanged;

        private ISessionLayer mSessionLayer;

        public DeviceMemory(ISessionLayer sessionLayer)
        {
            mSessionLayer = sessionLayer;
        }

        #region IDeviceMemory Members

        private IDevice mDevice;
        public IDevice Device
        {
            get
            {
                return mDevice;
            }
            set
            {
                mDevice = value;
            }
        }
        private byte[] mBuffer;
        public byte[] Buffer
        {
            get { return mBuffer; }
            set { mBuffer = value; }
        }

        public NVMTypes SwitchEEPROM(bool state, int defaultMemorySize)
        {
            NVMTypes nvm_type = NVMTypes.NVMInvalid;
            //mSessionLayer.LogDataSource.AddTopSession(new LogSession(string.Format("{0} ({1})", "Switch EEPROM", state.ToString())));
            byte seqNum = mSessionLayer.SequenceNumber;
            byte[] response = null;
            if (state)
            {
                try
                {
                    response = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_TOGGLE_EEPROM_IF,
                        new byte[] { 0x01, this.Device.ChipType, seqNum });
                }
                catch (RequestTimeoutException)
                {
                    //Old programmer firmwares are supported only Serial EEPROM and does not supprt returnting the responces.
                    nvm_type = NVMTypes.NVMSerialEEPROM;
                }
                catch
                {
                    nvm_type = NVMTypes.NVMInvalid;
                }
            }
            else
            {
                try
                {
                    response = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_TOGGLE_EEPROM_IF,
                        new byte[] { 0x00, this.Device.ChipType, seqNum });
                }
                catch (RequestTimeoutException)
                {
                    nvm_type = NVMTypes.NVMUnknown;
                }
                catch
                {
                    nvm_type = NVMTypes.NVMInvalid;
                }
            }
            if (response != null)
            {
                if (response.Length == 5)
                {
                    if (response[4] == seqNum)
                    {
                        nvm_type = (NVMTypes)
                            ((((uint)response[0]) << (int)NVMTypes.NVMTypePos)
                            | (((uint)response[1]) << (int)NVMTypes.NVMManufacturerPos)
                            | (((uint)response[2]) << (int)NVMTypes.NVMDevicePos)
                            | (((uint)response[3]) << (int)NVMTypes.NVMSizePos));
                        mEepromSize = 0;
                        switch (nvm_type)
                        {
                            case NVMTypes.NVMAdestotechAT25XE021A:
                                mEepromSize = 256 * 1024; //KB
                                break;
                            case NVMTypes.NVMAdestotechAT25XE041B:
                                mEepromSize = 512 * 1024; //KB
                                break;
                            case NVMTypes.NVMAdestotechAT45DB021E:
                                mEepromSize = 256 * 1024; //KB
                                break;
                            case NVMTypes.NVMAdestotechAT45DB041E:
                                mEepromSize = 512 * 1024; //KB
                                break;
                            case NVMTypes.NVMAdestotechAT45DB081E:
                                mEepromSize = 1024 * 1024; //KB
                                break;
                            case NVMTypes.NVMAdestotechAT45DB161E:
                                mEepromSize = 2 * 1024 * 1024; //KB
                                break;
                            case NVMTypes.NVMAdestotechAT45DB321E:
                                mEepromSize = 4 * 1024 * 1024; //KB
                                break;
                            case NVMTypes.NVMAdestotechAT45DB641E:
                                mEepromSize = 8 * 1024 * 1024; //KB
                                break;
                            case NVMTypes.NVMGigaDevicesGD25Q21:
                                mEepromSize = 256 * 1024; //KB
                                break;
                            case NVMTypes.NVMGigaDevicesGD25Q41:
                                mEepromSize = 512 * 1024; //KB
                                break;
                            case NVMTypes.NVMGigaDevicesGD25Q80:
                                mEepromSize = 1 * 1024 * 1024; //KB
                                break;
                            case NVMTypes.NVMGigaDevicesGD25Q16:
                                mEepromSize = 2 * 1024 * 1024; //KB
                                break;
                            case NVMTypes.NVMGigaDevicesGD25Q32:
                                mEepromSize = 4 * 1024 * 1024; //KB
                                break;
                            case NVMTypes.NVMGigaDevicesGD25Q64:
                                mEepromSize = 8 * 1024 * 1024; //KB
                                break;
                            case NVMTypes.NVMGigaDevicesGD25Q128:
                                mEepromSize = 16 * 1024 * 1024; //KB
                                break;
                            case NVMTypes.NVMMicrochipSST25PF020B:
                                mEepromSize = 256 * 1024; //KB
                                break;
                            case NVMTypes.NVMMicronM25PE10VP:
                                mEepromSize = 128 * 1024; //KB
                                break;
                            case NVMTypes.NVMMicronM25PE20VP:
                                mEepromSize = 256 * 1024; //KB
                                break;
                            case NVMTypes.NVMNumonyxM25PE40VP:
                                mEepromSize = 512 * 1024; //KB
                                break;
                            case NVMTypes.NVMSTMicro25PE16VP:
                                mEepromSize = 2 * 1024 * 1024; //KB
                                break;
                            case NVMTypes.NVMSTMicro25PE80VP:
                                mEepromSize = 1024 * 1024; //KB
                                break;
                            default:
                                mEepromSize = defaultMemorySize;
                                break;
                        }
                    }
                }
            }
            return nvm_type;
        }

        public void ClearEEPROM(byte[] eepromDataRaw)
        {
            //mSessionLayer.LogDataSource.AddTopSession(new LogSession(string.Format("{0}", "Clear EEPROM")));
            WriteEEPROM(0, eepromDataRaw, Constants.EEPROM_PAGE_SIZE);
        }

        public byte[] ReadEEPROM(int index, int length)
        {
            List<byte> tempBuffer = new List<byte>();
            byte iterCount = 0x20;
            int i = index;
            while (i < length && i < this.EepromSize)
            {
                byte[] request = new byte[5];
                request[0] = (byte)(i >> 16);
                request[1] = (byte)(i >> 8);
                request[2] = (byte)(i & 0xFF);
                //request[3] = iterCount;
                request[3] = (length - i) > iterCount ? iterCount : (byte)(length - i);
                request[4] = mSessionLayer.SequenceNumber;
                System.Diagnostics.Debug.WriteLine("!!!!!!!!!!!!!!!!!!!" + request[0].ToString("X2") +
                    request[1].ToString("X2") +
                    request[2].ToString("X2") +
                    request[3].ToString("X2") +
                    request[4].ToString("X2"));
                byte[] buffer = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_MEMORY_GET_BUFFER, request);//(ushort)i, (byte)Constants.EEP_MAX_CLASSES);
                if (buffer != null)
                {
                    tempBuffer.AddRange(buffer);
                    tempBuffer.RemoveAt(tempBuffer.Count - 1); //Remove checksum
                    if (tempBuffer[i] == (byte)(i >> 16)
                        && tempBuffer[i + 1] == (byte)(i >> 8)
                        && tempBuffer[i + 2] == (byte)(i & 0xFF))
                    {
                        tempBuffer.RemoveAt(i); //Remove address
                        tempBuffer.RemoveAt(i);
                        tempBuffer.RemoveAt(i);
                    }
                }
                i += iterCount;
            }
            if (tempBuffer.Count > length)
                tempBuffer.RemoveRange(length, tempBuffer.Count - length);
            return tempBuffer.ToArray();
        }

        public void ReadEEPROM()
        {
            //mSessionLayer.LogDataSource.AddTopSession(new LogSession(string.Format("{0}", "Read EEPROM")));
            List<byte> tempBuffer = new List<byte>();
            for (int i = 0; i < (int)this.EepromSize; i += (int)Constants.EEP_MAX_CLASSES)
            {
                byte[] request = new byte[5];
                request[0] = (byte)(i >> 16);
                request[1] = (byte)(i >> 8);
                request[2] = (byte)(i & 0xFF);
                request[3] = (byte)Constants.EEP_MAX_CLASSES;
                request[4] = mSessionLayer.SequenceNumber;
                System.Diagnostics.Debug.WriteLine("!!!!!!!!!!!!!!!!!!!" + request[0].ToString("X2") +
                    request[1].ToString("X2") +
                    request[2].ToString("X2") +
                    request[3].ToString("X2") +
                    request[4].ToString("X2"));
                byte[] buffer = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_MEMORY_GET_BUFFER, request);//(ushort)i, (byte)Constants.EEP_MAX_CLASSES);
                if (buffer != null)
                {
                    tempBuffer.AddRange(buffer);
                    tempBuffer.RemoveAt(tempBuffer.Count - 1); //Remove checksum
                    if (tempBuffer[i] == (byte)(i >> 16)
                        && tempBuffer[i + 1] == (byte)(i >> 8)
                        && tempBuffer[i + 2] == (byte)(i & 0xFF))
                    {
                        tempBuffer.RemoveAt(i); //Remove address
                        tempBuffer.RemoveAt(i);
                        tempBuffer.RemoveAt(i);
                    }
                }
            }
            if (tempBuffer.Count > 0)
            {
                this.Buffer = tempBuffer.ToArray();
            }
            else
            {
                this.Buffer = null;
            }
        }

        public bool Compare(byte[] eepromDataRaw)
        {
            bool result = true;
            if (this.Buffer.Length == eepromDataRaw.Length)
            {
                for (int i = 0; i < this.Buffer.Length; i++)
                {
                    if (i < 8 || i > 11)
                    {
                        if (this.Buffer[i] != eepromDataRaw[i])
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

        public bool Compare(byte[] eepromDataRaw, out SortedList<int, List<byte[]>> diffs)
        {
            bool result = true;
            diffs = new SortedList<int, List<byte[]>>();

            if (eepromDataRaw != null && this.Buffer != null)
            {
                if (eepromDataRaw.SequenceEqual(this.Buffer))
                {
                    result = true;
                }
                else
                {
                    List<byte[]> diffData = new List<byte[]>();
                    diffData.Add(this.Buffer); //EEPROM content
                    diffData.Add(eepromDataRaw); //HEX File content
                    diffs.Add(0, diffData);
                    result = false;
                }
            }
            else
            {
                result = false;
            }
            if (result)
            {
                diffs = null;
            }
            return result;
        }

        public bool WriteSRAM(byte[] sramDataRaw, bool verify)
        {
            //mSessionLayer.LogDataSource.AddTopSession(new LogSession(string.Format("{0}", "Write SRAM")));
            return WriteSRAMByPages(sramDataRaw,
                (int)(this.Device.SramSize / Constants.SRAM_PAGE_SIZE), (int)Constants.SRAM_PAGE_SIZE, verify);
        }

        private bool WriteSRAMByPages(byte[] sramDataRaw, int pagesCount, int pageSize, bool verify)
        {
            verify = false;
            bool result = true;
            for (int pageIndex = 0; pageIndex < pagesCount; pageIndex++)
            {
                //mSessionLayer.LogDataSource.AddTopSession(new LogSession(string.Format("{0} ({1})", "SRAM Write Page", pageIndex.ToString())));

                byte[] pageBuffer = new byte[pageSize];
                for (int j = 0; j < pageSize; j++)
                {
                    pageBuffer[j] = sramDataRaw[pageIndex * pageSize + j];
                }

                if (this.Device.ChipType == (byte)ChipTypes.ZW050x)
                {
                    //verify = false;
                    byte[] buffer1 = new byte[pageBuffer.Length / 2];
                    Array.Copy(pageBuffer, 0, buffer1, 0, buffer1.Length);
                    byte[] buffer2 = new byte[pageBuffer.Length / 2];
                    Array.Copy(pageBuffer, buffer1.Length, buffer2, 0, buffer2.Length);

                    result = WriteSRAMPage(pageIndex * 2, buffer1, verify);
                    result &= WriteSRAMPage(pageIndex * 2 + 1, buffer2, verify);
                }
                else
                {
                    result = WriteSRAMPage(pageIndex, pageBuffer, verify);
                }

            }
            return result;
        }

        private bool WriteSRAMPage(int pageIndex, byte[] pageBuffer, bool verify)
        {
            bool result = false;
            ProgrammerCommandTypes cmd;
            byte[] request = new byte[4 + pageBuffer.Length];
            int i = 0;
            cmd = ProgrammerCommandTypes.FUNC_ID_ZW0x0x_SRAM_WRITE_PAGE;
            request[i++] = (byte)this.Device.ChipType;
            request[i++] = (byte)pageIndex;
            request[i++] = (byte)(verify ? 1 : 0);
            for (int j = 0; j < pageBuffer.Length; j++)
                request[i++] = pageBuffer[j];
            byte seqNum;
            request[i++] = seqNum = mSessionLayer.SequenceNumber;
            byte[] response = mSessionLayer.ExecuteRequest((byte)cmd, request);
            if (response != null)
            {
                if (response.Length == 2)
                {
                    result = (response[0] == (byte)FlashProgrammingStatuses.SUCCESS && response[1] == seqNum);
                }
            }
            return result;
        }

        public bool ReadSRAM()
        {
            bool res = true;
            //mSessionLayer.LogDataSource.AddTopSession(new LogSession(string.Format("{0}", "Read SRAM")));
            List<byte> tempBuffer = new List<byte>();

            for (int paramPageIndex = 0; paramPageIndex < this.Device.SramSize / Constants.SRAM_PAGE_SIZE; paramPageIndex++)
            {
                if (this.Device.ChipType == (byte)ChipTypes.ZW050x)
                {
                    byte[] page = ReadSRAMPage(paramPageIndex * 2);
                    if (page != null)
                    {
                        tempBuffer.AddRange(page);
                    }
                    else
                    {
                        res = false;
                        break;
                    }
                    page = ReadSRAMPage(paramPageIndex * 2 + 1);
                    if (page != null)
                    {
                        tempBuffer.AddRange(page);
                    }
                    else
                    {
                        res = false;
                        break;
                    }
                }
                else
                {
                    byte[] page = ReadSRAMPage(paramPageIndex);
                    if (page != null)
                    {
                        tempBuffer.AddRange(page);
                    }
                    else
                    {
                        res = false;
                        break;
                    }
                }
            }

            if (tempBuffer.Count > 0 && res)
            {
                this.Buffer = tempBuffer.ToArray();
            }
            else
            {
                this.Buffer = null;
            }
            return res;
        }

        public byte[] ReadSRAMPage(int pageIndex)
        {
            byte[] result = null;
            ProgrammerCommandTypes cmd;
            byte[] request = new byte[3];
            int i = 0;
            cmd = ProgrammerCommandTypes.FUNC_ID_ZW0x0x_SRAM_READ_PAGE;
            request[i++] = (byte)this.Device.ChipType;
            request[i++] = (byte)pageIndex;
            byte seqNum;
            request[i++] = seqNum = mSessionLayer.SequenceNumber;
            byte[] response = mSessionLayer.ExecuteRequest((byte)cmd, request);
            if (response != null)
            {
                //if (response.Length == Constants.SRAM_PAGE_SIZE + 1)
                //{
                //	if (response[Constants.SRAM_PAGE_SIZE] == seqNum)
                //	{
                List<byte> tempBuffer = new List<byte>();
                tempBuffer.AddRange(response);
                tempBuffer.RemoveAt(tempBuffer.Count - 1);
                result = tempBuffer.ToArray();
                //	}
                //}
            }
            return result;
        }


        public IFlashSettings ReadSRAMRfOptions()
        {
            //mSessionLayer.LogDataSource.AddTopSession(new LogSession("Read SRAM Options"));
            IFlashSettings result = null;
            int pageIndex = (int)(this.Device.SramSize / (uint)Constants.SRAM_PAGE_SIZE - 1);	//RF Settings are in last page.
            int offset = this.Device.FlashSize - Constants.SRAM_PAGE_SIZE;	//we need Absolute OTP
            // address here, because SRAM emulates the part of OTP in Development mode.
            byte[] page = this.ReadSRAMPage(pageIndex);
            if (page != null)
            {
                result = new FlashSettings();
                result.ParseBuffer(this.Device.ChipType, offset, page);
            }
            return result;
        }

        public WriteRfOptionsStatuses WriteSRAMRfOptions(IFlashSettings rfSettings)
        {
            //mSessionLayer.LogDataSource.AddTopSession(new LogSession("Write SRAM Options"));
            WriteRfOptionsStatuses result = WriteRfOptionsStatuses.NoErrors;
            int pageIndex = (this.Device.SramSize / Constants.SRAM_PAGE_SIZE - 1);	//RF Settings are in last page.
            int offset = this.Device.FlashSize - Constants.SRAM_PAGE_SIZE;	//we need Absolute OTP
            // address here, because SRAM emulates the part of OTP in Development mode.
            if (rfSettings != null)
            {
                byte[] page = this.ReadSRAMPage(pageIndex);
                if (page != null)
                {
                    rfSettings.StoreToBuffer(this.Device.ChipType, offset, page);
                    if (!WriteSRAMPage(pageIndex, page, true))
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

        public bool WriteEEPROM(byte[] eepromDataRaw)
        {
            //mSessionLayer.LogDataSource.AddTopSession(new LogSession(string.Format("{0}", "Write EEPROM")));
            return WriteEEPROM(0, eepromDataRaw, Constants.EEPROM_PAGE_SIZE);
        }

        public bool WriteEEPROM(int startAddress, byte[] eepromDataRaw, ushort pageSize)
        {
            ushort index;
            byte[] data = new byte[pageSize];
            if (pageSize == 1)
            {
                while (startAddress < eepromDataRaw.Length)
                {
                    if (!WriteEEPByte(startAddress, eepromDataRaw[startAddress++]))
                        return false;
                }
            }
            while (startAddress < eepromDataRaw.Length)
            {
                for (index = 0; index < pageSize; index++)
                {
                    if (startAddress + index >= eepromDataRaw.Length)
                        break;
                    data[index] = eepromDataRaw[startAddress + index];
                }
                if (index < pageSize)
                {
                    byte[] wData = new byte[index];
                    for (int i = 0; i < wData.Length; i++)
                        wData[i] = data[i];
                    if (!WriteEEP(startAddress, wData))
                    {
                        return false;
                    }
                    else
                    {
                        ProgressChanged?.Invoke(new ProgressChangedEventArgs(ProgressStatuses.Write, wData.Length * (startAddress / pageSize), eepromDataRaw.Length));
                    }
                }
                else
                {
                    if (!WriteEEP(startAddress, data))
                    {
                        return false;
                    }
                    else
                    {
                        ProgressChanged?.Invoke(new ProgressChangedEventArgs(ProgressStatuses.Write, data.Length * (startAddress / pageSize), eepromDataRaw.Length));
                    }
                }

                startAddress += pageSize;
            }
            return true;
        }

        private bool WriteEEP(int startAddress, byte[] wData)
        {
            bool result = false;
            try
            {
                byte[] request = new byte[wData.Length + 5];
                int i = 0;
                byte seqNum = mSessionLayer.SequenceNumber;
                request[i++] = (byte)((startAddress >> 16) & 0xFF);
                request[i++] = (byte)((startAddress >> 8) & 0xFF);
                request[i++] = (byte)(startAddress & 0xFF);
                request[i++] = (byte)(wData.Length & 0xFF);

                for (int j = 0; j < wData.Length; j++)
                    request[i++] = wData[j];
                request[i++] = seqNum;

                byte[] resp = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_MEMORY_PUT_BUFFER, request);
                if (resp.Length < 2)
                    return false;
                return (resp[0] == (byte)FlashProgrammingStatuses.DONE
                    && resp[1] == seqNum);
            }
            catch
            {
                result = false;
            }
            return result;
        }

        private bool WriteEEPByte(int startAddress, byte byteData)
        {
            bool result = true;
            try
            {
                mSessionLayer.ExecuteNonRequest((byte)ProgrammerCommandTypes.FUNC_ID_MEMORY_PUT_BYTE, new byte[] { 0x00, (byte)(startAddress >> 8), (byte)(startAddress & 0xFF), byteData, mSessionLayer.SequenceNumber });
            }
            catch
            {
                result = false;
            }
            return result;
        }

        private int mEepromSize = (int)ZWave.Enums.EepromSize.EEPROM_CS_SIZE_ZERO;
        public int EepromSize
        {
            get
            {
                return mEepromSize;
            }
            set
            {
                mEepromSize = value;
            }
        }

        public byte[] ReadHomeId()
        {
            //mSessionLayer.LogDataSource.AddTopSession(new LogSession(string.Format("{0}", "Read Home Id")));
            byte[] result = null;
            byte[] response = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_MEMORY_GET_ID, new byte[] { 0, mSessionLayer.SequenceNumber });
            if (response.Length == 5)
            {
                List<byte> tmp = new List<byte>(response);
                tmp.RemoveAt(tmp.Count - 1);
                result = tmp.ToArray();
            }
            return result;
        }

        public bool WriteHomeId(byte[] homeId)
        {
            //mSessionLayer.LogDataSource.AddTopSession(new LogSession(string.Format("{0}", "Write Home Id")));
            bool result = true;
            List<byte> request = new List<byte>();
            request.Add(0);
            for (int i = (int)homeId.Length - (int)4; i < (int)homeId.Length; i++)
            {
                if (i < 0)
                {
                    request.Add(0);
                }
                else
                {
                    request.Add(homeId[i]);
                }
            }
            request.Add(mSessionLayer.SequenceNumber);
            byte[] response = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_STORE_HOMEID, request.ToArray());
            return result;
        }

        public int MtpSize
        {
            get
            {
                return 0;
            }
        }

        public bool SwitchMTP(bool state)
        {
            bool result = false;
            try
            {
                //mSessionLayer.LogDataSource.AddTopSession(new LogSession(string.Format("{0} ({1})", "Switch MTP", state.ToString())));
                byte[] response;
                byte seqNum = mSessionLayer.SequenceNumber;
                if (state)
                {
                    response = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_TOGGLE_MTP_IF,
                        new byte[] { 0x01, this.Device.ChipType, seqNum });
                }
                else
                {
                    response = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_TOGGLE_MTP_IF,
                        new byte[] { 0x00, this.Device.ChipType, seqNum });
                }

                if (response.Length == 2)
                {
                    result = (response[0] == (byte)FlashProgrammingStatuses.SUCCESS && response[1] == seqNum);
                }
            }
            catch
            {
                result = false;
            }
            return result;
        }

        public bool FillMTP(byte fillValue)
        {
            bool result = false;
            try
            {
                //mSessionLayer.LogDataSource.AddTopSession(new LogSession(string.Format("{0} ({1})", "Fill MTP with value", fillValue.ToString())));
                byte seqNum = mSessionLayer.SequenceNumber;
                byte[] response = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_MTP_FILL,
                    new byte[] { this.Device.ChipType, fillValue, seqNum });
                if (response.Length == 2)
                {
                    result = (response[0] == (byte)FlashProgrammingStatuses.SUCCESS && response[1] == seqNum);
                }
            }
            catch
            {
                result = false;
            }
            return result;
        }

        public bool ReadMTP()
        {
            bool res = true;
            //mSessionLayer.LogDataSource.AddTopSession(new LogSession(string.Format("{0}", "Read MTP")));
            List<byte> tempBuffer = new List<byte>();

            for (int paramPageIndex = 0; paramPageIndex < this.MtpSize / Constants.MTP_PAGE_SIZE; paramPageIndex++)
            {
                byte seqNum = mSessionLayer.SequenceNumber;
                byte[] response = mSessionLayer.ExecuteRequest(
                    (byte)ProgrammerCommandTypes.FUNC_ID_MTP_READ_PAGE,
                    new byte[] { (byte)this.Device.ChipType, (byte)paramPageIndex, seqNum });
                if (response != null)
                {
                    if (response.Length == Constants.MTP_PAGE_SIZE + 1)
                    {
                        if (response[Constants.MTP_PAGE_SIZE] == seqNum)
                        {
                            tempBuffer.AddRange(response);
                            tempBuffer.RemoveAt(tempBuffer.Count - 1);
                        }
                        else
                        {
                            res = false;
                            break;
                        }
                    }
                    else
                    {
                        res = false;
                        break;
                    }
                }
                else
                {
                    res = false;
                    break;
                }
            }

            if (tempBuffer.Count > 0 && res)
            {
                this.Buffer = tempBuffer.ToArray();
            }
            else
            {
                this.Buffer = null;
            }
            return res;
        }

        public bool WriteMTP(byte[] mtpDataRaw, bool verify)
        {
            //mSessionLayer.LogDataSource.AddTopSession(new LogSession(string.Format("{0}", "Write MTP")));
            return WriteMTPByPages(mtpDataRaw,
                (int)(this.MtpSize / Constants.MTP_PAGE_SIZE), (int)Constants.MTP_PAGE_SIZE, verify);
        }

        private bool WriteMTPByPages(byte[] mtpDataRaw, int pagesCount, int pageSize, bool verify)
        {
            bool result = true;
            for (int pageIndex = 0; pageIndex < pagesCount; pageIndex++)
            {
                //mSessionLayer.LogDataSource.AddTopSession(new LogSession(string.Format("{0} ({1})", "MTP Write Page", pageIndex.ToString())));
                byte[] pageBuffer = new byte[pageSize];
                for (int j = 0; j < pageSize; j++)
                {
                    pageBuffer[j] = mtpDataRaw[pageIndex * pageSize + j];
                }
                if (!WriteMTPPage(pageIndex, pageBuffer, verify))
                {
                    result = false;
                }
            }
            return result;
        }

        private bool WriteMTPPage(int pageIndex, byte[] pageBuffer, bool verify)
        {
            bool result = false;
            ProgrammerCommandTypes cmd;
            byte[] request = new byte[4 + pageBuffer.Length];
            int i = 0;
            cmd = ProgrammerCommandTypes.FUNC_ID_MTP_WRITE_PAGE;
            request[i++] = (byte)this.Device.ChipType;
            request[i++] = (byte)pageIndex;
            request[i++] = (byte)(verify ? 1 : 0);
            for (int j = 0; j < pageBuffer.Length; j++)
                request[i++] = pageBuffer[j];
            byte seqNum;
            request[i++] = seqNum = mSessionLayer.SequenceNumber;
            byte[] response = mSessionLayer.ExecuteRequest((byte)cmd, request);
            if (response.Length == 2)
                result = (response[0] == (byte)FlashProgrammingStatuses.SUCCESS && response[1] == seqNum);
            return result;
        }

        #endregion
    }
}
