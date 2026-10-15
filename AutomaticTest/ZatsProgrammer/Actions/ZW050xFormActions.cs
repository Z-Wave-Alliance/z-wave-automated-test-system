// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using ZWave.Programmer.Classes;
using ZWave.Programmer.Controllers;
using ZWave.Enums;
using ZWave.Devices;
using ZWave.Programmer.Properties;
using System.IO;
using System.Globalization;
using System.Threading;
using ZWave.Framework;

namespace ZWave.Programmer.Actions
{
    /// <summary>
    /// ZW050xFormActions class. Contains ZW050x Form functions.
    /// </summary>
    public class ZW050xFormActions : BaseAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ZW050xFormActions"/> class.
        /// </summary>
        /// <param name="controller">The controller.</param>

        public ZW050xFormActions(ControllerManager controller)
            : base(controller)
        {

        }

        /// <summary>
        /// Return SRAM Working Mode, selected by user
        /// </summary>
        public WorkingModes GetSelectedWorkingMode()
        {
            return WorkingModes.ExecuteOutOfSram;
        }

        #region SRAM

        /// <summary>
        /// Reads the SRAM.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="workingMode">Working mode.</param>
        public void SramRead(string fileName, WorkingModes workingMode)
        {
            ControllerManager.ShowMessageInConsole(Resources.MsgReadSramProgress);
            int fileOffset = 0;
            int sramOffset = 0;
            byte[] sramBuffer = null;
            var device = ControllerManager.DeviceOpen();
            try
            {
                if (device.SetProgrammingMode(true))
                {
                    device.ReadSignatureBits();
                    if (CheckDeviceChipTypeCompatibility(device))
                    {
                        device.SetBusyLedState(true);
                        switch (workingMode)
                        {
                            case WorkingModes.Development:
                                {
                                    fileOffset = (int)(device.FlashSize - Constants.SRAM_DEVMODE_SIZE);
                                    sramOffset = (int)Constants.SRAM_DEVMODE_OFFSET;
                                }
                                break;
                        }
                        if (!device.Memory.ReadSRAM())
                        {
                            ControllerManager.ShowMessage(Resources.ErrorCantReadSram, true);
                        }

                        if (device.Memory.Buffer != null)
                        {
                            sramBuffer = HexFileHelper.BlankArray(device.Memory.Buffer.Length - sramOffset, Constants.SRAM_BLANK_VALUE);
                            for (int i = 0; i < device.Memory.Buffer.Length - sramOffset; i++)
                            {
                                sramBuffer[i] = device.Memory.Buffer[sramOffset + i];
                            }
                        }

                        device.SetBusyLedState(false);
                    }
                    else
                    {
                        ControllerManager.ShowMessage(Resources.MsgIncorrectChipType, true);
                    }
                    if (!ControllerManager.DocumentModel.Settings.HoldDeviceInReset)
                        device.SetProgrammingMode(false);
                }
                else
                {
                    ControllerManager.ShowMessage(Resources.ErrorCantSetProgrammingMode, true);
                }
            }
            catch
            {
                ControllerManager.ShowMessage(Resources.ErrorCantReadSram, true);
            }
            finally
            {
                ControllerManager.DeviceClose();
            }
            if (sramBuffer != null)
            {
                try
                {
                    if (sramBuffer.Length > 0)
                    {
                        TextWriter writer = new StreamWriter(fileName);
                        writer.Write(HexFileHelper.WriteIntelHexFile(sramBuffer, fileOffset, sramBuffer.Length, 0x10, Constants.SRAM_BLANK_VALUE, false));
                        writer.Close();
                    }
                    ControllerManager.ShowMessage("Read SRAM completed.", false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public bool WirteHEXToSRAM(IDevice device, string fileName, IFlashSettings rfSettings)
        {
            return WirteHEXToSRAM(device, fileName, 0, 0, rfSettings);
        }

        private bool WirteHEXToSRAM(IDevice device, string fileName, int fileUseFromOffset, int sramWriteOffset, IFlashSettings rfSettings)
        {
            bool result = true;
            if (CheckDeviceChipTypeCompatibility(device))
            {
                device.SetBusyLedState(true);
                string hexFileName = fileName;
                if (!string.IsNullOrEmpty(hexFileName) && File.Exists(hexFileName))
                {
                    byte[] flashDataRaw = HexFileHelper.GetBytes(HexFileHelper.ReadIntelHexFile(hexFileName, Constants.SRAM_BLANK_VALUE), device.SramSize, Constants.SRAM_BLANK_VALUE);

                    if (flashDataRaw != null)
                    {
                        //Apply the SRAM RF Settings:
                        if (rfSettings != null)
                        {
                            rfSettings.StoreToBuffer(device.ChipType, fileUseFromOffset, flashDataRaw);
                        }

                        //Apply the SRAM Write Offset:
                        if (sramWriteOffset != 0)
                        {
                            int i;
                            for (i = device.SramSize - 1; i >= sramWriteOffset; i--)
                            {
                                flashDataRaw[i] = flashDataRaw[i - sramWriteOffset];
                            }
                            for (; i >= 0; i--)
                            {
                                flashDataRaw[i] = Constants.SRAM_BLANK_VALUE;
                            }
                        }
                        //TODO: if hex file have more than X kB of data - show error (or warning??)

                        //Erace the MTP Memory if needed, before programming SRAM.
                        // We must did this before SRAM programming, because operations with MTP can corrupt the SRAM.
                        WorkingModes wMode = GetSelectedWorkingMode();
                        if (wMode == WorkingModes.ExecuteOutOfSram)
                        {
                            if (!device.Memory.WriteSRAM(flashDataRaw, true))
                            {
                                ControllerManager.ShowMessage(Resources.ErrorCantWriteSram, true);
                                result = false;
                            }
                        }
                        else
                        {
                            if (!device.Memory.WriteSRAM(flashDataRaw, true))
                            {
                                ControllerManager.ShowMessage(Resources.ErrorCantWriteSram, true);
                                result = false;
                            }
                        }
                    }
                    else
                    {
                        ControllerManager.ShowMessage(Resources.ErrorHexFileNotValid, true);
                        result = false;
                    }
                }
                else if (string.IsNullOrEmpty(hexFileName))
                {
                    ControllerManager.ShowMessage("Hex file is not selected.", true);
                    result = false;
                }
                else
                {
                    ControllerManager.ShowMessage(string.Format("{0} does not exists.", hexFileName), true);
                    result = false;
                }
                device.SetBusyLedState(false);
            }
            else
            {
                ControllerManager.ShowMessage(Resources.MsgIncorrectChipType, true);
                result = false;
            }
            return result;
        }
        /// <summary>
        /// Write the SRAM.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="workingMode">Working mode.</param>
        public void SramWrite(string fileName, WorkingModes workingMode)
        {
            ControllerManager.ShowMessageInConsole(Resources.MsgWriteSramProgress);
            var device = ControllerManager.DeviceOpen();
            try
            {
                if (device.SetProgrammingMode(true))
                {
                    device.ReadSignatureBits();

                    if (CheckDeviceChipTypeCompatibility(device))
                    {
                        string hexFileName;
                        hexFileName = fileName;
                        int fileOffset = 0;
                        int sramOffset = 0;
                        IFlashSettings rfSettings = null;
                        switch (workingMode)
                        {
                            case WorkingModes.Development:
                                {
                                    fileOffset = (int)device.FlashSize - (int)Constants.SRAM_DEVMODE_SIZE;
                                    sramOffset = (int)Constants.SRAM_DEVMODE_OFFSET;
                                    rfSettings = null;			//the flashSettings will be used from the .hex file
                                }
                                break;
                        }
                        if (WirteHEXToSRAM(device, hexFileName, fileOffset, sramOffset, rfSettings))
                        {
                            ControllerManager.ShowMessageInStatus("Write SRAM completed.", false);
                        }
                    }
                    else
                    {
                        ControllerManager.ShowMessage(Resources.MsgIncorrectChipType, true);
                    }
                    if (!ControllerManager.DocumentModel.Settings.HoldDeviceInReset)
                        device.SetProgrammingMode(false);
                }
                else
                {
                    ControllerManager.ShowMessage(Resources.ErrorCantSetProgrammingMode, true);
                }
            }
            catch (Exception)
            {
                ControllerManager.ShowMessage(Resources.ErrorCantWriteSram, true);
            }
            finally
            {
                ControllerManager.DeviceClose();
            }
        }

        private bool CompareDataWithDiffs(byte[] sramDataRaw, byte[] buffer, out SortedList<int, List<byte[]>> diffs)
        {
            bool result = true;
            diffs = new SortedList<int, List<byte[]>>();

            if (sramDataRaw != null && buffer != null)
            {
                if (sramDataRaw.SequenceEqual(buffer))
                {
                    result = true;
                }
                else
                {
                    List<byte[]> diffData = new List<byte[]>();
                    diffData.Add(buffer); //SRAM content
                    diffData.Add(sramDataRaw); //HEX File content
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

        /// <summary>
        /// Write the SRAM and run specified mode.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="workingMode">Working mode.</param>
        public void SramWriteAndRunMode(string fileName, WorkingModes workingMode)
        {
            switch (workingMode)
            {
                case WorkingModes.Development:
                    {
                        SramWriteAndRunDevMode(fileName);
                    }
                    break;

                case WorkingModes.ExecuteOutOfSram:
                    {
                        SramWriteAndExecOutOfSram(fileName);
                    }
                    break;
            }
        }
        /// <summary>
        /// Write the SRAM and run development mode.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        private void SramWriteAndRunDevMode(string fileName)
        {
            ControllerManager.ShowMessageInConsole(Resources.MsgWriteSramProgress);
            var device = ControllerManager.DeviceOpen();
            try
            {
                if (device.SetProgrammingMode(true))
                {
                    device.ReadSignatureBits();
                    if (CheckDeviceChipTypeCompatibility(device))
                    {
                        string hexFileName;
                        IFlashSettings rfSettings = null;
                        hexFileName = fileName;
                        rfSettings = null;			//the flashSettings will be used from the .hex file
                        if (WirteHEXToSRAM(device, hexFileName, (int)device.FlashSize - (int)Constants.SRAM_DEVMODE_SIZE, (int)Constants.SRAM_DEVMODE_OFFSET, rfSettings))
                        {
                            if (device.SetMode(WorkingModes.Development))
                            {
                                ControllerManager.ShowMessageInStatus("Write SRAM completed. Development mode enabled.", false);
                            }
                            else
                            {
                                ControllerManager.ShowMessage(Resources.ErrorCantSetChipWorkingMode, true);
                            }
                        }
                    }
                    else
                    {
                        ControllerManager.ShowMessage(Resources.MsgIncorrectChipType, true);
                    }
                    if (!ControllerManager.DocumentModel.Settings.HoldDeviceInReset)
                        device.SetProgrammingMode(false);
                }
                else
                {
                    ControllerManager.ShowMessage(Resources.ErrorCantSetProgrammingMode, true);
                }
            }
            catch
            {
                ControllerManager.ShowMessage(Resources.ErrorCantWriteSram, true);
            }
            finally
            {
                ControllerManager.DeviceClose();
            }
        }

        /// <summary>
        /// Srams the write and exec out of sram.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        private void SramWriteAndExecOutOfSram(string fileName)
        {
            ControllerManager.ShowMessageInConsole(Resources.MsgWriteSramProgress);
            var device = ControllerManager.DeviceOpen();
            try
            {
                if (device.SetProgrammingMode(true))
                {
                    device.ReadSignatureBits();
                    if (CheckDeviceChipTypeCompatibility(device))
                    {
                        string hexFileName;
                        hexFileName = fileName;
                        if (WirteHEXToSRAM(device, hexFileName, null))
                        {
                            bool result = false;
                            if (device.SetMode(WorkingModes.ExecuteOutOfSram))
                            {
                                if (device.ChipType == (byte)ChipTypes.ZW050x)
                                {
                                    Thread.Sleep(300);
                                    device.Flash.CheckState(); //TODO
                                    result = true;
                                }
                                if (result)
                                    ControllerManager.ShowMessageInStatus("Write SRAM completed. Execute out of SRAM enabled.", false);
                                else
                                    ControllerManager.ShowMessage(Resources.ErrorCantSetChipWorkingMode, true);
                            }
                            else
                            {
                                ControllerManager.ShowMessage(Resources.ErrorCantSetChipWorkingMode, true);
                            }
                        }
                    }
                    else
                    {
                        ControllerManager.ShowMessage(Resources.MsgIncorrectChipType, true);
                    }
                    if (!ControllerManager.DocumentModel.Settings.HoldDeviceInReset)
                        device.SetProgrammingMode(false);
                }
                else
                {
                    ControllerManager.ShowMessage(Resources.ErrorCantSetProgrammingMode, true);
                }
            }
            catch
            {
                ControllerManager.ShowMessage(Resources.ErrorCantWriteSram, true);
            }
            finally
            {
                ControllerManager.DeviceClose();
            }
        }

        #endregion

        const string NO_SECTORS_PROTECTED = "<no sectors protected>";

        public void NvrWrite(byte[] nvrData, IProgrammableDevice device)
        {
            try
            {
                if (nvrData != null)
                {
                    for (int i = Constants.NVR_START_ADDRESS; i < Constants.NVR_END_ADDRESS; i++)
                    {
                        device.WriteNvrByte((byte)i, nvrData[i]);
                    }
                }
                else
                {
                    ControllerManager.ShowMessage(Resources.ErrorCantWriteNvr, true);
                }
            }
            catch
            {

            }
            finally
            {

            }
        }

        private void OnDeviceMemoryProgressChanged(ZWave.Events.ProgressChangedEventArgs args)
        {
            ControllerManager.ChangeProgressInfo(args.ProgressStatus, args.Current, args.Total);
        }

        public bool NvrDataWrite(NVRData nvrData)
        {
            bool ret = true; //Assume everything OK to add minimum changes
            var device = ControllerManager.DeviceOpen();
            try
            {
                if (device.SetProgrammingMode(true))
                {
                    device.ReadSignatureBits();
                    if (CheckDeviceChipTypeCompatibility(device))
                    {
                        device.SetBusyLedState(true);
                        if (nvrData != null)
                        {
                            byte[] buffer = device.ReadNvr(0);
                            if (IsEmpty(buffer, 0xFF))
                            {
                                device.WriteNvrByte(NVRData.REV_ADDRESS, nvrData.REV);
                                device.WriteNvrByte(NVRData.CCAL_ADDRESS, nvrData.CCAL);
                                device.WriteNvrByte(NVRData.PINS_ADDRESS, nvrData.PINS);
                                device.WriteNvrByte(NVRData.NVMCS_ADDRESS, nvrData.NVMCS);
                                for (byte i = NVRData.SAWC_ADDRESS; i < nvrData.SAWC.Length + NVRData.SAWC_ADDRESS; i++)
                                {
                                    device.WriteNvrByte(i, nvrData.SAWC[i - NVRData.SAWC_ADDRESS]);
                                }
                                device.WriteNvrByte(NVRData.SAWB_ADDRESS, nvrData.SAWB);
                                device.WriteNvrByte(NVRData.NVMT_ADDRESS, nvrData.NVMT);

                                for (byte i = NVRData.NVMS_ADDRESS; i < nvrData.NVMS.Length + NVRData.NVMS_ADDRESS; i++)
                                {
                                    device.WriteNvrByte(i, nvrData.NVMS[i - NVRData.NVMS_ADDRESS]);
                                }
                                for (byte i = NVRData.NVMP_ADDRESS; i < nvrData.NVMP.Length + NVRData.NVMP_ADDRESS; i++)
                                {
                                    device.WriteNvrByte(i, nvrData.NVMP[i - NVRData.NVMP_ADDRESS]);
                                }
                                for (byte i = NVRData.UUID_ADDRESS; i < nvrData.UUID.Length + NVRData.UUID_ADDRESS; i++)
                                {
                                    device.WriteNvrByte(i, nvrData.UUID[i - NVRData.UUID_ADDRESS]);
                                }
                                for (byte i = NVRData.VID_ADDRESS; i < nvrData.VID.Length + NVRData.VID_ADDRESS; i++)
                                {
                                    device.WriteNvrByte(i, nvrData.VID[i - NVRData.VID_ADDRESS]);
                                }
                                for (byte i = NVRData.PID_ADDRESS; i < nvrData.PID.Length + NVRData.PID_ADDRESS; i++)
                                {
                                    device.WriteNvrByte(i, nvrData.PID[i - NVRData.PID_ADDRESS]);
                                }
                                device.WriteNvrByte(NVRData.TXCAL1_ADDRESS, nvrData.TXCAL1);
                                device.WriteNvrByte(NVRData.TXCAL2_ADDRESS, nvrData.TXCAL2);
                                for (byte i = NVRData.PUK_ADDRESS; i < nvrData.PUK.Length + NVRData.PUK_ADDRESS; i++)
                                {
                                    device.WriteNvrByte(i, nvrData.PUK[i - NVRData.PUK_ADDRESS]);
                                }
                                for (byte i = NVRData.PRK_ADDRESS; i < nvrData.PRK.Length + NVRData.PRK_ADDRESS; i++)
                                {
                                    device.WriteNvrByte(i, nvrData.PRK[i - NVRData.PRK_ADDRESS]);
                                }
                                for (byte i = NVRData.PROTOCOL_DATA_ADDRESS; i < nvrData.ProtocolData.Length + NVRData.PROTOCOL_DATA_ADDRESS; i++)
                                {
                                    device.WriteNvrByte(i, nvrData.ProtocolData[i - NVRData.PROTOCOL_DATA_ADDRESS]);
                                }

                                for (byte i = NVRData.CRC16_ADDRESS; i < nvrData.CRC16.Length + NVRData.CRC16_ADDRESS; i++)
                                {
                                    device.WriteNvrByte(i, nvrData.CRC16[i - NVRData.CRC16_ADDRESS]);
                                }
                                for (byte i = NVRData.APP_DATA_ADDRESS; i < nvrData.APP_DATA.Length + NVRData.APP_DATA_ADDRESS; i++)
                                {
                                    device.WriteNvrByte(i, nvrData.APP_DATA[i - NVRData.APP_DATA_ADDRESS]);
                                }
                                device.WriteNvrByte(NVRData.HW_ADDRESS, nvrData.HW);
                            }
                            else
                            {
                                if (ControllerManager.ShowMessage("NVR area is not empty. \nModification of NVR will erase the whole Flash Code Memory. \nDo you want to continue?", "MessageBoxButtonsYesNo", "MessageBoxIconQuestion") == true)
                                {
                                    device.Flash.Erase(ControllerManager.DocumentModel.IsAsic);
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

                                    for (int i = Constants.NVR_START_ADDRESS; i < Constants.NVR_END_ADDRESS; i++)
                                    {
                                        device.WriteNvrByte((byte)i, buffer[i]);
                                    }
                                }
                                else
                                {
                                    ret = false;
                                }
                            }

                        }
                        else
                        {
                            ControllerManager.ShowMessage(Resources.ErrorCantWriteNvr, true);
                        }
                        device.SetBusyLedState(false);
                    }
                    else
                    {
                        ControllerManager.ShowMessage(Resources.MsgIncorrectChipType, true);
                    }
                }
                else
                {
                    ControllerManager.ShowMessage(Resources.ErrorCantSetProgrammingMode, true);
                }
            }
            catch
            {

            }
            finally
            {

                if (!ControllerManager.DocumentModel.Settings.HoldDeviceInReset)
                    device.SetProgrammingMode(false);
                ControllerManager.DeviceClose();
            }
            return ret;
        }

        private bool IsEmpty(byte[] buffer, byte emptyValue)
        {
            bool result = true;
            for (int i = Constants.NVR_START_ADDRESS; i < Constants.NVR_END_ADDRESS; i++)
            {
                if (buffer[i] != emptyValue)
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        public List<byte[]> NvrRead()
        {
            List<byte[]> ret = null;
            int progInterface = ControllerManager.ProgInterface;
            if (progInterface == 0)
            {
                ret = NvrDataRead(true);
            }
            return ret;
        }

        public List<byte[]> NvrDataRead(bool readWholeNvr)
        {
            List<byte[]> result = null;
            var device = ControllerManager.DeviceOpen();
            try
            {
                if (device.SetProgrammingMode(true))
                {
                    device.ReadSignatureBits();
                    if (CheckDeviceChipTypeCompatibility(device))
                    {
                        device.SetBusyLedState(true);
                        byte[] nvrData = device.ReadNvr(0);
                        if (nvrData != null)
                        {
                            result = new List<byte[]>();
                            result.Add(nvrData);
                            if (readWholeNvr)
                            {
                                byte[] nvrData2 = device.ReadNvr(1);
                                if (nvrData2 != null)
                                    result.Add(nvrData2);
                            }
                        }
                        else
                        {
                            ControllerManager.ShowMessage(Resources.ErrorCantReadNvr, true);
                        }
                        device.SetBusyLedState(false);
                    }
                    else
                    {
                        ControllerManager.ShowMessage(Resources.MsgIncorrectChipType, true);
                    }
                }
                else
                {
                    ControllerManager.ShowMessage(Resources.ErrorCantSetProgrammingMode, true);
                }
            }
            catch
            {

            }
            finally
            {

                if (!ControllerManager.DocumentModel.Settings.HoldDeviceInReset)
                    device.SetProgrammingMode(false);
                ControllerManager.DeviceClose();
            }
            return result;
        }
    }
}
