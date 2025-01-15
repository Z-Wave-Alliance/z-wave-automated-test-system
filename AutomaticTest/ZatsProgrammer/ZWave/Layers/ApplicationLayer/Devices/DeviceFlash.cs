// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Devices;
using ZWave.Enums;
using System.Threading;
using ZWave.Logging;
using ZWave.Framework;
using ZWave.Events;
using ZWave.Exceptions;

namespace ZWave.SerialPortApplication.Devices
{
    public class DeviceFlash : IDeviceFlash
    {

        #region IDeviceFlash Members


        public event ProgressChangedEventHandler ProgressChanged;

        private IDevice mDevice;
        private ISessionLayer mSessionLayer;
        public List<IFlashPage> mPages;

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

        public IList<IFlashPage> Pages
        {
            get
            {
                return mPages;
            }
            set
            {
                mPages = (List<IFlashPage>)value;
            }
        }

        public DeviceFlash(ISessionLayer sessionLayer)
        {
            mSessionLayer = sessionLayer;
        }

        public bool Erase(bool isAsic)
        {
            bool result = false;
            //mSessionLayer.LogDataSource.AddTopSession(new LogSession("Erase Flash"));
            if (isAsic)
            {
                mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_ZW0x0x_CHIP_ERASE,
                    new byte[] { this.Device.ChipType, this.Device.CalculateWriteCycle(), mSessionLayer.SequenceNumber });
                result = true;
            }
            else
            {
                byte seqNum = mSessionLayer.SequenceNumber;
                byte[] report = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_M128_CHIP_ERASE,
                    new byte[] { seqNum });
                if (report.Length == 7)
                {
                    if (report[6] == seqNum)
                    {
                        if (report[0] == (byte)FlashProgrammingStatuses.DONE)
                        {
                            //boot loader signature are correct (returned in report[1,2,3,4,5]) and erasing are done.
                            result = true;
                        }
                        else if (report[0] == 0xF0)
                        {
                            //boot loader signature are not correct and returned in report[1,2,3,4,5]
                            result = false;
                        }
                    }
                }
            }
            return result;
        }
        public void Read(bool isAsic)
        {
            for (int i = 0; i < this.Pages.Count; i++)
            {
                //mSessionLayer.LogDataSource.AddTopSession(new LogSession(string.Format("{0} ({1})", "Flash Read Page", i.ToString())));
                this.Pages[i] = ReadPage(isAsic, i);
                if (ProgressChanged != null)
                {
                    if (this.Pages[i] != null)
                    {
                        if (this.Pages[i].Buffer != null)
                        {
                            Tools._writeDebugDiagnosticMessage("Page:" + i.ToString() + " " + this.Pages[i].Buffer.Length.ToString(), true, false);
                            ProgressChanged(new ProgressChangedEventArgs(ProgressStatuses.Read, this.Pages[i].Buffer.Length * (i + 1), (int)this.Device.FlashSize));
                        }
                    }
                    else
                    {
                        throw new ApplicationLayerException("Device was removed from board.");
                    }
                }
            }
        }
        public bool Write(bool isAsic, byte[] flashDataRaw, IFlashSettings flashSettings, bool verify)
        {
            bool result = false;
            int flashSize;
            if (isAsic)
            {
                flashSize = this.Device.FlashSize;
            }
            else
            {
                flashSize = Constants.FIRMWARE_SIZE;
            }

            if (isAsic && (flashSettings != null))
            {
                flashSettings.StoreToBuffer(this.Device.ChipType, 0, flashDataRaw);
            }
            try
            {
                result = this.Write(isAsic, flashDataRaw, verify);
            }
            catch (Exception ex)
            {
                Tools._writeDebugDiagnosticMessage(ex.Message, true, true);
            }
            return result;
        }

        public List<IFlashPage> GenerateOutput(bool isAsic, byte[] flashDataRaw, int pagesCount, int pageSize)
        {
            if (isAsic)
                return GenerateOutput(flashDataRaw, pagesCount, (int)this.Device.FlashSize, pageSize);
            else
                return GenerateOutput(flashDataRaw, pagesCount, Constants.FIRMWARE_SIZE, Constants.FIRMWARE_PAGE_SIZE);
        }

        public bool Compare(bool isAsic, IDeviceFlash flash, IFlashSettings flashSettings, byte[] flashDataRaw, out SortedList<int, List<byte[]>> diffs)
        {
            bool result = true;
            diffs = new SortedList<int, List<byte[]>>();
            int flashSize;
            int pageSize;
            int pageCount;
            if (isAsic)
            {
                flashSize = this.Device.FlashSize;
                pageSize = Constants.BYTES_IN_PAGE;
                pageCount = Constants.PAGES_IN_FLASH;
                if (flashSettings != null)
                {
                    flashSettings.StoreToBuffer(this.Device.ChipType, 0, flashDataRaw);
                }
            }
            else
            {
                flashSize = Constants.FIRMWARE_SIZE;
                pageSize = Constants.FIRMWARE_PAGE_SIZE;
                pageCount = Constants.PAGES_IN_FIRMWARE;
            }
            List<IFlashPage> flashData = this.GenerateOutput(isAsic, flashDataRaw, (int)pageCount, (int)pageSize);
            if (flashData.Count == flash.Pages.Count)
            {
                for (int pageIndex = 0; pageIndex < flash.Pages.Count; pageIndex++)
                {
                    if (pageIndex == 0)
                    {
                        if (!flashData[pageIndex].Compare(flash.Pages[pageIndex], true))
                        {
                            List<byte[]> diffData = new List<byte[]>();
                            diffData.Add(flash.Pages[pageIndex].Buffer); //Flash content
                            diffData.Add(flashData[pageIndex].Buffer); //HEX File content
                            diffs.Add(pageIndex, diffData);
                            result = false;
                            //break;
                        }
                    }
                    else
                    {
                        //if (pageIndex != 124 && pageIndex != 126)
                        if (!flashData[pageIndex].Compare(flash.Pages[pageIndex], false))
                        {
                            List<byte[]> diffData = new List<byte[]>();
                            diffData.Add(flash.Pages[pageIndex].Buffer); //Flash content
                            diffData.Add(flashData[pageIndex].Buffer); //HEX File content
                            diffs.Add(pageIndex, diffData);
                            result = false;
                            //break;
                        }
                    }
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
        public byte[] GetBytes()
        {
            byte[] result = new byte[Constants.FLASH_SIZE];
            for (int i = 0; i < this.Pages.Count; i++)
            {
                FlashPage page = (FlashPage)this.Pages[i];
                if (page != null && page.Buffer != null)
                {
                    page.Buffer.CopyTo(result, i * Constants.BYTES_IN_PAGE);
                }
            }
            return result;
        }
        public IFlashPage ReadPage(bool isAsic, int pageIndex)
        {
            byte command;
            byte paramChipType;
            byte paramPageIndex;
            if (isAsic)
            {
                command = (byte)ProgrammerCommandTypes.FUNC_ID_ZW0x0x_READ_PAGE;
                paramChipType = this.Device.ChipType;
                paramPageIndex = (byte)pageIndex;
            }
            else
            {
                command = (byte)ProgrammerCommandTypes.FUNC_ID_M128_BLOCK_READ;
                paramChipType = (byte)(pageIndex << 8);
                paramPageIndex = (byte)(pageIndex & 0xFF);
            }
            FlashPage result = null;

            if (paramChipType == (byte)ChipTypes.ZW050x)
            {
                List<byte> tempBuffer = new List<byte>();
                paramPageIndex = (byte)(paramPageIndex * 2);
                byte seqNum = mSessionLayer.SequenceNumber;
                byte[] response = mSessionLayer.ExecuteRequest(command, new byte[] { paramChipType, paramPageIndex, seqNum });
                if (response.Length >= 1)
                {
                    if (response[response.Length - 1] == seqNum)
                    {
                        tempBuffer.AddRange(response);
                        tempBuffer.RemoveAt(tempBuffer.Count - 1);
                    }
                }
                paramPageIndex = (byte)(paramPageIndex + 1);
                seqNum = mSessionLayer.SequenceNumber;
                response = mSessionLayer.ExecuteRequest(command, new byte[] { paramChipType, paramPageIndex, seqNum });
                if (response.Length >= 1)
                {
                    if (response[response.Length - 1] == seqNum)
                    {
                        tempBuffer.AddRange(response);
                        tempBuffer.RemoveAt(tempBuffer.Count - 1);
                    }
                }
                if (tempBuffer.Count > 0)
                {
                    result = new FlashPage(mSessionLayer, this);
                    result.Buffer = tempBuffer.ToArray();
                }
            }
            else
            {
                byte seqNum = mSessionLayer.SequenceNumber;
                byte[] response = mSessionLayer.ExecuteRequest(command, new byte[] { paramChipType, paramPageIndex, seqNum });
                if (response.Length >= 1)
                {
                    if (response[response.Length - 1] == seqNum)
                    {
                        List<byte> tempBuffer = new List<byte>(response);
                        tempBuffer.RemoveAt(tempBuffer.Count - 1);
                        result = new FlashPage(mSessionLayer, this);
                        result.Buffer = tempBuffer.ToArray();
                    }
                }
            }
            return result;
        }
        public bool ErasePage(int pageIndex)
        {
            this.Device.SetWriteCycleTime();
            byte[] response = mSessionLayer.ExecuteRequest(
                (byte)ProgrammerCommandTypes.FUNC_ID_ZW0x0x_ERASE_PAGE,
                new byte[] { this.Device.ChipType, (byte)pageIndex, mSessionLayer.SequenceNumber });
            if (response.Length >= 1)
            {
                if (response[0] == (byte)FlashProgrammingStatuses.SUCCESS)
                {
                    return true;
                }
            }
            return false;
        }
        public bool WritePage(IFlashPage page, bool isAsic, bool verify)
        {
            return page.Write(isAsic, verify);
        }

        #endregion
        #region Private Methods
        private bool Write(bool isAsic, byte[] flashDataRaw, bool verify)
        {
            int flashSize;
            int pageSize;
            int pageCount;
            if (isAsic)
            {
                flashSize = this.Device.FlashSize;
                pageSize = Constants.BYTES_IN_PAGE;
                pageCount = Constants.PAGES_IN_FLASH;
            }
            else
            {
                flashSize = Constants.FIRMWARE_SIZE;
                pageSize = Constants.FIRMWARE_PAGE_SIZE;
                pageCount = Constants.PAGES_IN_FIRMWARE;
            }
            List<IFlashPage> flashData = this.GenerateOutput(isAsic, flashDataRaw, pageCount, pageSize);
            if (isAsic)
            {
                return Write(isAsic, flashData, pageCount, verify);
            }
            else
            {
                return Write(isAsic, flashData, pageCount, verify);
            }
        }
        private bool Write(bool isAsic, List<IFlashPage> flashPages, int pageCount, bool verify)
        {
            bool result = false;
            if (isAsic)
            {
                this.Device.SetWriteCycleTime();
            }
            int pageNo;
            int pageSize;
            if (isAsic)
            {
                pageSize = Constants.BYTES_IN_PAGE;
            }
            else
            {
                pageSize = Constants.FIRMWARE_PAGE_SIZE;
            }
            //mSessionLayer.LogDataSource.AddTopSession(new LogSession(string.Format("{0} ({1})", "Flash Write Page", "0")));
            result = true;
            if (!WritePage(flashPages[0], isAsic, false))
            {
                result = false;
            }
            for (pageNo = 0; pageNo < pageCount; pageNo++)
            {
                //mSessionLayer.LogDataSource.AddTopSession(new LogSession(string.Format("{0} ({1})", "Flash Write Page", pageNo.ToString())));
                if (!WritePage(flashPages[pageNo], isAsic, verify))
                {
                    result = false;
                }
                ProgressChanged?.Invoke(new ProgressChangedEventArgs(ProgressStatuses.Write, flashPages[pageNo].Buffer.Length * (pageNo + 1), this.Device.FlashSize));
            }
            return result;
        }
        private List<IFlashPage> GenerateOutput(byte[] flashDataRaw, int pagesCount, int size, int pageSize)
        {
            List<IFlashPage> result = new List<IFlashPage>();

            for (int pageIndex = 0; pageIndex < pagesCount; pageIndex++)
            {

                FlashPage page = new FlashPage(mSessionLayer, this);
                page.Buffer = new byte[pageSize];
                page.Address = pageIndex;
                page.Flash = this;
                for (int j = 0; j < pageSize; j++)
                {
                    page.Buffer[j] = flashDataRaw[pageIndex * pageSize + j];
                }
                result.Add(page);
            }
            return result;
        }

        public bool RunCRCCheck()
        {
            byte[] response = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_ZW0x0x_CRC_CHECK_RUN, new byte[] { this.Device.ChipType, mSessionLayer.SequenceNumber });
            if (response.Length >= 1 && response[0] == 0x31)
            {
                return true;
            }
            return false;//TODO
        }

        public byte CheckState()
        {
            byte[] response = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_ZW0x0x_CHECK_STATE, new byte[] { this.Device.ChipType, mSessionLayer.SequenceNumber });
            if (response.Length >= 1)
            {
                return response[0];
            }
            return 0;
        }
        public bool IsInAutoProgMode()
        {
            byte[] response = mSessionLayer.ExecuteRequest((byte)ProgrammerCommandTypes.FUNC_ID_ZW050x_GET_PROG_MODE, new byte[] { mSessionLayer.SequenceNumber });
            if (response.Length >= 1)
            {
                return (response[0] == 0) ? false : true;
            }
            return false;
        }
        #endregion
    }
}
