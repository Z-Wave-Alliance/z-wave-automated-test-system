// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.IO;
using ZWave.Devices;
using ZWave.Enums;
using ZWave.Framework;
using ZWave.Programmer.Controllers;
using ZWave.Programmer.Properties;
using ZWave.SerialPortApplication.Devices;

namespace ZWave.Programmer.Classes
{
    /// <summary>
    /// ProgrammerConsole class. Contains methods for parsing command line arguments.
    /// </summary>
    public class ProgrammerConsole
    {
        private ControllerManager mControllerManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgrammerConsole"/> class.
        /// </summary>
        /// <param name="controller">The controller.</param>
        public ProgrammerConsole(ControllerManager controller)
        {
            mControllerManager = controller;
        }

        private void ShowCaption()
        {
            Console.WriteLine(string.Format("{0} v.{1}.{2}", "", "", ""));
        }

        internal void ProcessCommandLine(string[] args)
        {
            Dictionary<string, string> commands = CommandLineValidationHelper.ParseAndValidateCommandLineArgs(args);
            var isOnlySPI = commands.ContainsKey("-phg") || commands.ContainsKey("-hvg"); //Avvilable only for SPI ('-c')
            ControllerManager.Instance.IsLessOutput = commands.ContainsKey("-less");

            if (!ControllerManager.Instance.IsLessOutput)
            {
                ShowCaption();
            }
            try
            {
                if (commands.ContainsKey("-s") && !isOnlySPI)
                {
                    if (commands.Count > 1)
                    {
                        string comPort = "";
                        if (commands.ContainsKey("-c"))
                        {
                            comPort = commands["-c"];
                        }
                        else
                        {
                            comPort = commands["-s"];
                        }
                        if (string.IsNullOrEmpty(comPort))
                        {
                            CommandLineValidationHelper.ShowUsage();
                            return;
                        }
                        ProcessSerialPort(comPort);
                        if (commands.Count == 2 && commands.ContainsKey("-t"))
                        {
                            ProcessChipType(commands["-t"]);
                        }
                        else
                        {
                            //ShowMessage("UART interface enabled.\nPlease keep reset button on Programmer board pressed during programming.");
                            mControllerManager.ProgInterface = 1;
                            if (commands.ContainsKey("-t"))
                            {
                                ProcessChipType(commands["-t"]);
                                ProcessCommandLine(commands);
                            }
                            else
                            {
                                if (mControllerManager.Actions.ProgrammerActions.DetectDeviceA())
                                {
                                    ProcessCommandLine(commands);
                                }
                            }

                        }
                    }
                }
                else if (commands.ContainsKey("-u") && !isOnlySPI)
                {
                    if (commands.Count > 1)
                    {
                        string comPort = "";
                        if (commands.ContainsKey("-c"))
                        {
                            comPort = commands["-c"];
                        }
                        else
                        {
                            comPort = commands["-u"];
                        }
                        if (string.IsNullOrEmpty(comPort))
                        {
                            CommandLineValidationHelper.ShowUsage();
                            return;
                        }
                        ProcessSerialPort(comPort);
                        if (commands.Count == 2 && commands.ContainsKey("-t"))
                        {
                            ProcessChipType(commands["-t"]);
                        }
                        else
                        {
                            ShowMessage("USB interface enabled.");
                            mControllerManager.ProgInterface = 1;
                            mControllerManager.DocumentModel.SelectedChipType = (byte)ChipTypes.ZW050x;
                            if (commands.ContainsKey("-t"))
                            {
                                ProcessChipType(commands["-t"]);
                                ProcessCommandLine(commands);
                            }
                            else
                            {
                                ProcessCommandLine(commands);
                            }
                        }
                    }
                }
                else if (commands.ContainsKey("-c"))
                {
                    if (commands.Count == 1)
                    {
                        ProcessSerialPort(commands["-c"]);
                    }
                    else if (commands.Count > 1)
                    {
                        ProcessSerialPort(commands["-c"]);
                        if (commands.Count == 2 && commands.ContainsKey("-t"))
                        {
                            ProcessChipType(commands["-t"]);
                        }
                        else
                        {
                            if (commands.ContainsKey("-t"))
                            {
                                ProcessChipType(commands["-t"]);
                                ProcessCommandLine(commands);
                            }
                            else
                            {
                                if (mControllerManager.Actions.ProgrammerActions.DetectDeviceA())
                                {
                                    ProcessCommandLine(commands);
                                }
                            }
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                ShowMessage(Resources.ErrorCantDetectDevice);
            }
        }

        private void ShowMessage(string msg)
        {
            Console.WriteLine(msg);
        }

        private void ProcessCommandLine(Dictionary<string, string> commands)
        {
            var priorityList = new string[]
            {
                "-pks","-pkf"
            };

            foreach (var item in priorityList)
            {
                if (commands.ContainsKey(item))
                {
                    ProcessCommandArgs(item, commands[item]);
                    commands.Remove(item);
                }
            }

            foreach (KeyValuePair<string, string> cmd in commands)
            {
                if (cmd.Key != "-c" && cmd.Key != "-s" && cmd.Key != "-u" && cmd.Key != "-t")
                {
                    ProcessCommandArgs(cmd.Key, cmd.Value);
                }
            }
        }

        private void ProcessCommandArgs(string commandKey, string commandValue)
        {
            if (commandKey == "-pf")
            {
                if (mControllerManager.DocumentModel.SelectedChipType < (byte)ChipTypes.ZW050x)
                {
                    IFlashSettings flashSettings = new FlashSettings();
                    flashSettings.Frequency = (byte)CommandLineValidationHelper.GetFrequencyByName(commandValue);
                    if (mControllerManager.ProgInterface == 0)
                    {
                        mControllerManager.Actions.ProgrammerActions.FlashWriteOptions(flashSettings);
                    }
                }
                else
                {
                    ShowMessage("Command is not supported using ZW050x chip type.");
                }
            }
            else if (commandKey == "-p")
            {
                if (mControllerManager.ProgInterface == 0)
                {
                    mControllerManager.Actions.ProgrammerActions.FlashProgram(commandValue, false);
                }
            }
            else if (commandKey == "-f")
            {
                if (mControllerManager.DocumentModel.SelectedChipType < (byte)ChipTypes.ZW050x)
                {
                    if (mControllerManager.ProgInterface == 0)
                    {
                        mControllerManager.Actions.ProgrammerActions.FlashWrite(commandValue, false);
                    }
                }
                else
                {
                    ShowMessage("Command is not supported using ZW050x chip type.");
                }
            }
            else if (commandKey == "-r")
            {
                if (mControllerManager.ProgInterface == 0)
                {
                    mControllerManager.Actions.ProgrammerActions.FlashRead(commandValue);
                }
            }
            else if (commandKey == "-v")
            {
                if (mControllerManager.ProgInterface == 0)
                {
                    mControllerManager.Actions.ProgrammerActions.FlashCompare(commandValue);
                }
            }
            else if (commandKey == "-e")
            {
                if (mControllerManager.ProgInterface == 0)
                {
                    mControllerManager.Actions.ProgrammerActions.FlashErase();
                }
            }
            else if (commandKey == "-pe")
            {
                if (int.TryParse(commandValue, out int commandIntValue))
                {
                    if (mControllerManager.ProgInterface == 0)
                    {
                        mControllerManager.Actions.ProgrammerActions.EEPROMErase();
                    }
                    else if (mControllerManager.ProgInterface == 1 || mControllerManager.ProgInterface == 2)
                    {

                        ShowMessage("EEPROM Erase is not implemented for UART/USB programming interfaces");
                    }
                }
                else
                {
                    if (mControllerManager.ProgInterface == 0)
                    {
                        mControllerManager.Actions.ProgrammerActions.EEPROMWrite(commandValue);
                    }
                    else if (mControllerManager.ProgInterface == 1 || mControllerManager.ProgInterface == 2)
                    {

                        ShowMessage("EEPROM Write is not implemented for UART/USB programming interfaces");
                    }
                }
            }
            else if (commandKey == "-pr")
            {
                if (mControllerManager.ProgInterface == 0)
                {
                    mControllerManager.Actions.ProgrammerActions.EEPROMRead(commandValue);
                }
                else if (mControllerManager.ProgInterface == 1 || mControllerManager.ProgInterface == 2)
                {

                    ShowMessage("EEPROM Read is not implemented for UART/USB programming interfaces");
                }
            }
            else if (commandKey == "-ph")
            {
                if (mControllerManager.ProgInterface == 0)
                {
                    var homeId = Tools.GetBytes(commandValue);
                    mControllerManager.Actions.ProgrammerActions.WriteHomeId(Tools.GetHexShort(homeId));
                }
                else if (mControllerManager.ProgInterface == 1 || mControllerManager.ProgInterface == 2)
                {
                    ShowMessage("Write Home ID is not implemented for UART/USB programming interfaces");
                }
            }
            else if (commandKey == "-phg")
            {
                if (mControllerManager.ProgInterface == 0)
                {
                    mControllerManager.Actions.ProgrammerActions.ReadHomeId();
                }
                else if (mControllerManager.ProgInterface == 1 || mControllerManager.ProgInterface == 2)
                {
                    ShowMessage("Write Home ID is not implemented for UART/USB programming interfaces");
                }
            }
            else if (commandKey == "-pks")
            {
                if (string.IsNullOrEmpty(commandValue))
                {
                    mControllerManager.DocumentModel.IsAddSecurity2Keys = true;
                }
                else
                {
                    string[] content = commandValue.Split(' ', '\t');
                    if (content.Length > 1)
                    {
                        mControllerManager.DocumentModel.PrivateS2Key = Tools.GetBytes(content[0]);
                        mControllerManager.DocumentModel.PublicS2Key = Tools.GetBytes(content[1]);
                    }
                    if (mControllerManager.DocumentModel.HasValidS2Keypair2Keys)
                    {
                        mControllerManager.DocumentModel.IsAddSecurity2Keys = true;
                    }
                    else
                        Console.WriteLine(string.Format("Can't parse keys from input '{0}'.", commandValue));
                }
            }
            else if (commandKey == "-pkf")
            {
                try
                {
                    string[] content = File.ReadAllLines(commandValue);
                    if (content.Length > 1)
                    {
                        mControllerManager.DocumentModel.PrivateS2Key = Tools.GetBytes(content[0]);
                        mControllerManager.DocumentModel.PublicS2Key = Tools.GetBytes(content[1]);
                    }
                }
                catch
                {
                }
                if (mControllerManager.DocumentModel.HasValidS2Keypair2Keys)
                {
                    mControllerManager.DocumentModel.IsAddSecurity2Keys = true;
                }
                else
                    Console.WriteLine(string.Format("Can't read keys from file '{0}'.", commandValue));
            }
            else if (commandKey == "-pkg")
            {
                mControllerManager.Actions.ProgrammerActions.GetS2Keys();
            }
            else if (commandKey == "-hvg")
            {
                if (mControllerManager.ProgInterface == 0)
                {
                    mControllerManager.Actions.ProgrammerActions.HostVersionGet();
                }
                else if (mControllerManager.ProgInterface == 1 || mControllerManager.ProgInterface == 2)
                {
                    ShowMessage("Write Home ID is not implemented for UART/USB programming interfaces");
                }
            }
            else if (commandKey == "-sr")
            {
                if (mControllerManager.DocumentModel.SelectedChipType == (byte)ChipTypes.ZW050x)
                {

                    if (mControllerManager.ProgInterface == 0)
                    {
                        mControllerManager.Actions.ZW050xFormActions.SramRead(commandValue, WorkingModes.ExecuteOutOfSram);
                    }
                }
            }
            else if (commandKey == "-sw")
            {
                if (mControllerManager.DocumentModel.SelectedChipType == (byte)ChipTypes.ZW050x)
                {
                    if (mControllerManager.ProgInterface == 0)
                    {
                        mControllerManager.Actions.ZW050xFormActions.SramWrite(commandValue, WorkingModes.ExecuteOutOfSram);
                    }
                }
            }
            else if (commandKey == "-sc")
            {
                if (mControllerManager.DocumentModel.SelectedChipType == (byte)ChipTypes.ZW050x)
                {

                    ShowMessage("SRAM Compare is not implemented for UART/USB programming interfaces");
                }
            }
            else if (commandKey == "-swrd")
            {
                if (mControllerManager.DocumentModel.SelectedChipType == (byte)ChipTypes.ZW050x)
                {

                    ShowMessage("Development mode is not implemented for UART/USB programming interfaces");
                }
            }
            else if (commandKey == "-sweo")
            {
                if (mControllerManager.DocumentModel.SelectedChipType == (byte)ChipTypes.ZW050x)
                {
                    if (mControllerManager.ProgInterface == 0)
                    {
                        mControllerManager.Actions.ZW050xFormActions.SramWriteAndRunMode(commandValue, WorkingModes.ExecuteOutOfSram);
                    }
                }
            }
            else if (commandKey == "-ls")
            {
                if (mControllerManager.DocumentModel.SelectedChipType == (byte)ChipTypes.ZW050x)
                {
                    var lockBits = Tools.GetBytes(commandValue);
                    if (lockBits.Length == 9)
                    {
                        if (mControllerManager.ProgInterface == 0)
                        {
                            mControllerManager.Actions.ProgrammerActions.LockBitsSetZW050x(new List<byte>(lockBits));
                        }
                    }
                }
            }
            else if (commandKey == "-lg")
            {
                if (mControllerManager.ProgInterface == 0)
                {
                    if (mControllerManager.DocumentModel.SelectedChipType == (byte)ChipTypes.ZW050x)
                    {
                        List<byte> lockBits = new List<byte>();
                        mControllerManager.Actions.ProgrammerActions.LockBitsReadZW050x(lockBits);
                        if (lockBits != null)
                        {
                            for (int i = 0; i < lockBits.Count; i++)
                            {
                                Console.WriteLine(string.Format("LockBit: {0} {1}", i, lockBits[i].ToString("X2")));

                            }
                        }
                    }
                }
            }
            else if (commandKey == "-mp")
            {
                if (commandValue == "0")
                {
                    if (mControllerManager.DocumentModel.SelectedChipType == (byte)ChipTypes.ZW050x)
                    {

                        ShowMessage("MTP Erase is not implemented for UART/USB programming interfaces");
                    }
                }
                else
                {
                    if (mControllerManager.DocumentModel.SelectedChipType == (byte)ChipTypes.ZW050x)
                    {

                        ShowMessage("MTP Program is not implemented for UART/USB programming interfaces");
                    }
                }
            }
            else if (commandKey == "-mr")
            {
                if (mControllerManager.DocumentModel.SelectedChipType == (byte)ChipTypes.ZW050x)
                {

                    ShowMessage("MTP Read is not implemented for UART/USB programming interfaces");
                }
            }
            else if (commandKey == "-ro")
            {
                ChipTypes deviceChipType = ChipTypes.UNKNOWN;
                IFlashSettings flashSettings = null;
                if (mControllerManager.ProgInterface == 0)
                {
                    flashSettings = mControllerManager.Actions.ProgrammerActions.FlashReadOptions(out deviceChipType);
                }
                if (flashSettings != null)
                {
                    switch (deviceChipType)
                    {
                        case (ChipTypes.ZW020x):
                        case (ChipTypes.ZW030x):
                            {
                                Console.WriteLine("");
                                Console.WriteLine(string.Format("Chip: {0}", deviceChipType.ToString()));
                                Console.WriteLine("------------------");
                                Console.WriteLine(string.Format("Normal Power:{0}", flashSettings.NormalPower.ToString("X2")));
                                Console.WriteLine(string.Format("Low Power:{0}", flashSettings.LowPower.ToString("X2")));
                            }
                            break;
                        case (ChipTypes.ZW050x):
                            {
                                Console.WriteLine("");
                                Console.WriteLine(string.Format("Chip: {0}", deviceChipType.ToString()));
                                Console.WriteLine("------------------");
                                Console.WriteLine(string.Format("Normal Power (ch0):{0}", flashSettings.NormalPowerCh0.ToString("X2")));
                                Console.WriteLine(string.Format("Low Power (ch0):{0}", flashSettings.LowPowerCh0.ToString("X2")));
                                Console.WriteLine(string.Format("Normal Power (ch1):{0}", flashSettings.NormalPowerCh1.ToString("X2")));
                                Console.WriteLine(string.Format("Low Power (ch1):{0}", flashSettings.LowPowerCh1.ToString("X2")));
                                Console.WriteLine(string.Format("Normal Power (ch2):{0}", flashSettings.NormalPowerCh2.ToString("X2")));
                                Console.WriteLine(string.Format("Low Power (ch2):{0}", flashSettings.LowPowerCh2.ToString("X2")));
                            }
                            break;

                    }
                }
            }
            else if (commandKey == "-sro")
            {
                IFlashSettings sramSettings = null;
                ChipTypes deviceChipType = ChipTypes.UNKNOWN;
                if (mControllerManager.DocumentModel.SelectedChipType == (byte)ChipTypes.ZW050x)
                {
                    Console.WriteLine(string.Format("Chip: {0}. Read SRAM options not supported.", ((ChipTypes)mControllerManager.DocumentModel.SelectedChipType).ToString()));
                }
            }
            else if (commandKey == "-so")
            {
                if (mControllerManager.DocumentModel.SelectedChipType == (byte)ChipTypes.ZW010x ||
                    mControllerManager.DocumentModel.SelectedChipType == (byte)ChipTypes.ZW020x ||
                    mControllerManager.DocumentModel.SelectedChipType == (byte)ChipTypes.ZW030x)
                {
                    var arr = Tools.GetBytes(commandValue);
                    if (arr.Length == 2)
                    {
                        byte npValue = arr[0];
                        byte lpValue = arr[1];

                        IFlashSettings flashSettings = new FlashSettings();
                        flashSettings.NormalPower = npValue;
                        flashSettings.LowPower = lpValue;
                        mControllerManager.Actions.ProgrammerActions.FlashWriteOptions(flashSettings);
                    }
                }
                else if (mControllerManager.DocumentModel.SelectedChipType == (byte)ChipTypes.ZW050x)
                {
                    var arr = Tools.GetBytes(commandValue);
                    if (arr.Length == 6)
                    {
                        byte np0Value = arr[0];
                        byte lp0Value = arr[1];
                        byte np1Value = arr[2];
                        byte lp1Value = arr[3];
                        byte np2Value = arr[4];
                        byte lp2Value = arr[5];

                        IFlashSettings flashSettings = new FlashSettings();
                        flashSettings.NormalPowerCh0 = np0Value;
                        flashSettings.NormalPowerCh1 = np1Value;
                        flashSettings.NormalPowerCh2 = np2Value;
                        flashSettings.LowPowerCh0 = lp0Value;
                        flashSettings.LowPowerCh1 = lp1Value;
                        flashSettings.LowPowerCh2 = lp2Value;

                        if (mControllerManager.ProgInterface == 0)
                        {
                            mControllerManager.Actions.ProgrammerActions.FlashWriteOptions(flashSettings);
                        }
                    }

                }

            }
            else if (commandKey == "-sso")
            {

            }
            else if (commandKey == "-nV")
            {
                byte[] nvrData = NvrReadPart0();
                if (nvrData != null)
                {
                    string hexData = HexFileHelper.WriteIntelHexFileStartingFromAddress(nvrData, 9, nvrData.Length, 0x10, 0xFF, false);
                    Console.WriteLine(hexData);
                    if (!string.IsNullOrEmpty(commandValue))
                    {
                        File.WriteAllText(commandValue, hexData);
                    }
                    ShowMessage("Read NVR data completed..");
                }
            }
            else if (commandKey == "-nv")
            {
                if (!string.IsNullOrEmpty(commandValue))
                {
                    SortedList<short, List<byte>> _hexData = HexFileHelper.ReadIntelHexData(File.ReadAllText(commandValue), Constants.BLANK_VALUE);
                    byte[] hexData = HexFileHelper.GetBytes(_hexData, Constants.NVR_END_ADDRESS, Constants.BLANK_VALUE);
                    if (hexData != null)
                    {
                        NVRData _nvr = (NVRData)hexData;
                        _nvr.CalculateCrc16();
                        if ((_nvr.CRC16[0] != hexData[NVRData.CRC16_ADDRESS]) &&
                            (_nvr.CRC16[1] != hexData[NVRData.CRC16_ADDRESS + 1]))
                        {
                            ShowMessage("NVR data contains bad CRC16 value.");
                        }
                        else
                        {
                            NvrWrite(_nvr);
                        }
                    }
                }
            }
            else if (commandKey == "-nCCAL")
            {
                byte CCALValue = Tools.GetByte(commandValue);
                byte[] nvrData = NvrReadPart0();
                NVRData nvrFormatted = (NVRData)nvrData;
                nvrFormatted.CCAL = CCALValue;
                nvrFormatted.CalculateCrc16();
                NvrWrite(nvrFormatted);
            }
        }

        private void NvrWrite(NVRData _nvr)
        {
            bool ret = false;
            if (mControllerManager.ProgInterface == 0)
            {
                ret = mControllerManager.Actions.ZW050xFormActions.NvrDataWrite(_nvr);
            }
            if (ret)
            {
                ShowMessage("Write NVR data completed..");
            }
            else
            {
                ShowMessage("Write NVR data failed..");
            }
        }

        private byte[] NvrReadPart0()
        {
            byte[] nvrData = null;
            if (mControllerManager.ProgInterface == 0)
            {
                var result = mControllerManager.Actions.ZW050xFormActions.NvrDataRead(false);
                if (result != null && result.Count > 0)
                    nvrData = result[0];
            }
            return nvrData;
        }

        private void ProcessSerialPort(string serialPort)
        {
#if NETCOREAPP
            mControllerManager.DocumentModel.PortInfo = serialPort;
#else
            Win32PnPEntityClass serialPortInfo = ComputerSystemHardwareHelper.GetWin32PnPEntityClassSerialPortDevice(serialPort);
            mControllerManager.DocumentModel.PortInfo = serialPortInfo;
#endif
            //Settings.Default.LastUsedDevice = serialPortInfo.DeviceID;
            //Settings.Default.Save();
        }

        private void ProcessChipType(string chipType)
        {
            foreach (byte i in Enum.GetValues(typeof(ChipTypes)))
            {
                if (chipType == Enum.GetName(typeof(ChipTypes), i))
                {
                    mControllerManager.DocumentModel.SelectedChipType = i;
                    //Settings.Default.SelectedChipType = i;
                    //Settings.Default.Save();
                    break;
                }
            }
        }
    }
}
