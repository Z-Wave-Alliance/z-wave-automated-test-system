// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZWave.Devices;
using ZWave.Enums;
using ZWave.Framework;
using ZWave.Programmer.Properties;

namespace ZWave.Programmer.Classes
{
    public static class CommandLineValidationHelper
    {
        public static List<CommandLineArgumentWrapper> CommandsList = new List<CommandLineArgumentWrapper>() 
        {
            new CommandLineArgumentWrapper("-c", SerialPortValidPredicate), 
            new CommandLineArgumentWrapper("-s"), 
            new CommandLineArgumentWrapper("-u"), 
            new CommandLineArgumentWrapper("-t", ChipTypeValidPredicate), 
            new CommandLineArgumentWrapper("-p", HexFilePathExistsPredicate), 
            new CommandLineArgumentWrapper("-r", CanWriteToDirectoryPredicate), 
            new CommandLineArgumentWrapper("-v", HexFilePathExistsPredicate), 
            new CommandLineArgumentWrapper("-e"), 
            new CommandLineArgumentWrapper("-pf", FrequencyValidPredicate), 
            new CommandLineArgumentWrapper("-ro"), 
            new CommandLineArgumentWrapper("-so", FlashOptionsLenghtValidPredicate), 
            new CommandLineArgumentWrapper("-f", HexFilePathExistsPredicate), 
            new CommandLineArgumentWrapper("-ls", LockBitsLenghtValidPredicate), 
            new CommandLineArgumentWrapper("-lg"), 
            new CommandLineArgumentWrapper("-sr", CanWriteToDirectoryPredicate), 
            new CommandLineArgumentWrapper("-sw", HexFilePathExistsPredicate), 
            new CommandLineArgumentWrapper("-sc", HexFilePathExistsPredicate),
            new CommandLineArgumentWrapper("-swrd", HexFilePathExistsPredicate), 
            new CommandLineArgumentWrapper("-sweo", HexFilePathExistsPredicate), 
            new CommandLineArgumentWrapper("-sro"), 
            new CommandLineArgumentWrapper("-sso", SramSetOptionsLenghtValidPredicate),
            new CommandLineArgumentWrapper("-mp", ZeroOrHexFilePredicate), 
            new CommandLineArgumentWrapper("-mr", CanWriteToDirectoryPredicate), 
            new CommandLineArgumentWrapper("-pe", ZeroOrHexFilePredicate), 
            new CommandLineArgumentWrapper("-pr", CanWriteToDirectoryPredicate), 
            new CommandLineArgumentWrapper("-ph", HomeIdLenghtValidPredicate),
            new CommandLineArgumentWrapper("-phg"),
            new CommandLineArgumentWrapper("-pks", (x)=> x == null || ValidDataArrayPredicate(x, 32,2)){HasDefault = true}, 
            new CommandLineArgumentWrapper("-pkf", FilePathExistsPredicate), 
            new CommandLineArgumentWrapper("-pkg"), 
            new CommandLineArgumentWrapper("-nv", HexFilePathExistsPredicate), 
            new CommandLineArgumentWrapper("-nV", CanWriteToDirectoryPredicate), 
            new CommandLineArgumentWrapper("-nCCAL", ValueIsInHexFormatPredicate),
            new CommandLineArgumentWrapper("-hvg"),
            new CommandLineArgumentWrapper("-less")

        };
        /// <summary>
        /// Usage method. Show help in console window.
        /// </summary>
        public static void ShowUsage()
        {
            #region Usage Output
            Console.WriteLine(@"
USAGE: ZWaveProgrammer -c COMx [-s | -u] [-t chipType] [-pf frequency] 
[-p filename] [-f filename] [-r filename] [-v filename] [-e] [-ls lockbitsbyte]
[-lg] [-sr filename] [-sw filename] [-sc filename] [-swrd filename] 
[-sweo filename][-mp filename] [-mp 0] [-mr filename] [-pe filename] [-pe 0] 
[-pr filename] [-ph HomeID] [-phg] [-hvg] [-less]

Programming interface options:

-c COMx Comport for programming via ZDP0xx programmer.
Use format 'COMx' where 'x' is a decimal number.

[-s] Enable direct UART programming mode.
Use the COM port specified with the '-c' command switch.

[-u] Enable direct USB programming mode.
Use the COM port specified with the '-c' command switch.

[-t chipType] Specify chip type (ZW010x, ZW020x, ZW030x, ZW040x, ZW050x).

[-less] View less output information to console (false by dafault)

[-hvg] Get Hosts Atmel Firmare Version.

Internal NVM Memory Programming options: (Flash/OTP)

[-p filename] Write hex file to internal NVM memory and and verify.

[-f filename] Write hex file to internal NVM memory.

[-r filename] Read internal NVM memory contents and write data to
the specified hex file.

[-v filename] Compare internal NVM memory contents to
the specified hex file.

[-e] Erase internal NVM memory.

[-ls lockbits ...] Set lockbits.
The number of 'lockbits' bytes depends on chip type.

[-lg] Get lockbits.

[-pf frequency] Set the frequency (EU, US, ANZ, HK, MY, 866, 870, 906, 910).

[-ro] Read options.

[-so (normalpowerbyte lowpowerbyte) ...] Set options.
The number of bytes depends on chip type.


External Non-Volatile Memory (NVM) Programming options: (EEPROM)

[-pe filename] Write hex file to external NVM memory.
Zero fill empty areas.

[-pr filename] Read external NVM memory contents and write data to
the specified hex file.

[-pe 0] Erase external NVM.

[-ph HomeID] Set HomeID in external NVM to Hex value WITHOUT '0x' e.g. DEADBEEF.

[-phg] Read HomeID from external NVM.

[-pks keyPr keyPu] Use only with -p option. Generate and write private and public keys for Security S2 
support if no <keyPr> and <keyPu> specified. Write private <keyPr> and public 
<keyPu> keys if they are specified.

[-pkf filename] Use only with -p option. Read private and public keys from file and write keys for 
Security S2 support. Keys are 32 hex format with no spaces. First line - private 
key, next line - public key.

[-pkg] Read Security S2 private and public keys.


ZW040x specific programming options:

[-sr filename] Read the SRAM contents and and write data to
the specified hex file.

[-sw filename] Write hex file to the SRAM memory.

[-sc filename] Compare SRAM memory contents to the specified hex file.

[-swrd filename] Write hex file to the SRAM memory and enter Development mode.

[-sweo filename] Write hex file to the SRAM memory and enter
Execute out of SRAM mode.

[-sro] SRAM Read options.

[-sso (normalpowerbyte lowpowerbyte) ...] - SRAM Set options.
The number of bytes depends on chip type.

[-mp filename] Write hex file to the MTP memory. Zero fill empty areas.

[-mp 0] Erase the MTP memory. Zero fill all bytes.

[-mr filename] Read the MTP contents and and write data to
the specified hex file.

Note that MTP memory is erased during programming OTP or SRAM.
Any operation with MTP memory will leave ZW040x chip in the reset state.
You need to power cycle the the chip to switch its working mode to Normal
and start execution of the embedded application from OTP.


ZW050x specific programming options:

[-nv filename] Download hexfile contents to NVR memory.

[-nV filename] Read NVR and display contents on the terminalor store 
content to the file specified.
 
[-nCCAL value] Write crystal calibration (CCAL) value to NVR memory.
Value should be used in HEX format eg. FA .
");
            #endregion
        }

        public static Dictionary<string, string> ParseAndValidateCommandLineArgs(string[] args)
        {
            Dictionary<string, string> commands = new Dictionary<string, string>();

            string key = null;

            foreach (string cmd in args)
            {
                if (cmd.StartsWith("/?"))
                {
                    ShowUsage();
                    commands.Clear();
                    break;
                }
                if (cmd.StartsWith("-"))
                {
                    key = cmd;
                    if (IsCommandKeyValid(key))
                    {
                        commands.Add(cmd, null);
                    }
                    else
                    {
                        ShowUsage();
                        commands.Clear();
                        break;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        commands[key] += " " + cmd;
                        commands[key] = commands[key].Trim();
                    }
                }
            }

            foreach (var command in commands)
            {
                var wrapper = CommandsList.FirstOrDefault(x => x.CmdKey == command.Key);
                //Unique case to support old format
                if (wrapper.CmdKey == "-u" || wrapper.CmdKey == "-s")
                {
                    if (!commands.ContainsKey("-c"))
                    {
                        wrapper.ArgumentPredicates = new Predicate<string>[] { SerialPortValidPredicate };
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(command.Value))
                        {
                            ShowUsage();
                            commands.Clear();
                            break;
                        }
                    }
                }
                else if (wrapper.CmdKey == "-pks")
                {
                    if (!commands.ContainsKey("-p"))
                    {
                        Console.WriteLine("[-pks keyPr keyPu] Use only with -p option.");
                        commands.Clear();
                        break;
                    }
                }
                else if (wrapper.CmdKey == "-pkf")
                {
                    if (!commands.ContainsKey("-p"))
                    {
                        Console.WriteLine("[-pkf filename] Use only with -p option.");
                        commands.Clear();
                        break;
                    }
                }

                if (wrapper.ArgumentPredicates != null && wrapper.ArgumentPredicates.Length > 0)
                {
                    if (!wrapper.HasDefault && string.IsNullOrEmpty(command.Value))
                    {
                        ShowParameterNotSpecifiedMessage(command.Key);
                        ShowUsage();
                        commands.Clear();
                        break;
                    }
                    if (!wrapper.IsValid(command.Value))
                    {
                        commands.Clear();
                        break;
                    }
                }
            }
            return commands;
        }

        private static bool IsCommandKeyValid(string key)
        {
            return CommandsList.Exists(x => x.CmdKey == key);
        }

        private static void ShowParameterNotSpecifiedMessage(string command)
        {
            Console.WriteLine(string.Format("{0} Command: {1}.",
                Resources.MsgParameterNotSpecified, command));
            Console.WriteLine("");
        }

        private static bool ValidDataArrayPredicate(string dataStr, int bytesInItem, int numberOfItems)
        {
            bool ret = false;
            if (!string.IsNullOrEmpty(dataStr))
            {
                string[] content = dataStr.Split(' ', '\t');
                if (content.Length == numberOfItems)
                {
                    foreach (var item in content)
                    {
                        try
                        {
                            ret = Tools.GetBytes(item).Length == bytesInItem;
                        }
                        catch
                        {
                        }
                        if (!ret)
                        {
                            break;
                        }
                    }
                }
            }
            if (!ret)
            {
                Console.WriteLine("Not valid parameters");
            }
            return ret;
        }

        private static bool HexFilePathExistsPredicate(string filePath)
        {
            bool ret = IsHexExtensionPredicate(filePath) && File.Exists(filePath);

            if (!ret)
            {
                Console.WriteLine(string.Format("Can't open file '{0}'.", filePath));
            }
            return ret;
        }

        private static bool FilePathExistsPredicate(string filePath)
        {
            bool ret = File.Exists(filePath);

            if (!ret)
            {
                Console.WriteLine(string.Format("Can't open file '{0}'.", filePath));
            }
            return ret;
        }

        private static bool ZeroOrHexFilePredicate(string input)
        {
            return ParameterIsZeroPredicate(input) || HexFilePathExistsPredicate(input);
        }

        private static bool CanWriteToDirectoryPredicate(string filePath)
        {
            var fileDir = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(fileDir))
            {
                fileDir = Directory.GetCurrentDirectory();
            }
            bool ret = IsHexExtensionPredicate(filePath) && Directory.Exists(fileDir);
            if (!ret)
            {
                Console.WriteLine(string.Format("Directory doesn't exist '{0}'.", fileDir));
            }
            return ret;
        }

        private static bool IsHexExtensionPredicate(string filePath)
        {
            bool ret = Path.GetExtension(filePath).ToLower() == ".hex";
            if (!ret)
            {
                Console.WriteLine(string.Format("Invalid file extension in file '{0}'.", filePath));
            }
            return ret;
        }

        private static bool ParameterIsZeroPredicate(string x)
        {
            return int.TryParse(x, out int res);
        }

        private static bool SerialPortValidPredicate(string serialPort)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(serialPort))
            {
#if NETCOREAPP
                return true;
#else
                Win32PnPEntityClass serialPortInfo = ComputerSystemHardwareHelper.GetWin32PnPEntityClassSerialPortDevice(serialPort);
                if (serialPortInfo != null)
                {
                    result = true;
                }
                else
                {
                    Console.WriteLine(Resources.MsgIncorrectSerialPort);
                }
#endif
            }
            else
            {
                ShowUsage();
            }
            return result;
        }

        private static bool ChipTypeValidPredicate(string chipType)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(chipType))
            {
                foreach (byte i in Enum.GetValues(typeof(ChipTypes)))
                {
                    if (chipType == Enum.GetName(typeof(ChipTypes), i))
                    {
                        result = true;
                        break;
                    }
                }
            }
            return result;
        }

        private static bool FrequencyValidPredicate(string frequency)
        {
            return GetFrequencyByName(frequency) != -1;
        }

        public static int GetFrequencyByName(string frequency)
        {
            int frequencyCode = -1;
            if (frequency == "US") frequencyCode = MemoryLayout.RF_US;
            else if (frequency == "EU") frequencyCode = MemoryLayout.RF_EU;
            else if (frequency == "ANZ") frequencyCode = MemoryLayout.RF_ANZ;
            else if (frequency == "HK") frequencyCode = MemoryLayout.RF_HK;
            else if (frequency == "MY") frequencyCode = MemoryLayout.RF_MY;
            else if (frequency == "IN") frequencyCode = MemoryLayout.RF_IN;
            else if (frequency == "RU") frequencyCode = MemoryLayout.RF_RU;
            else if (frequency == "IL") frequencyCode = MemoryLayout.RF_IL;
            else if (frequency == "866") frequencyCode = MemoryLayout.RF_866;
            else if (frequency == "870") frequencyCode = MemoryLayout.RF_870;
            else if (frequency == "906") frequencyCode = MemoryLayout.RF_906;
            else if (frequency == "910") frequencyCode = MemoryLayout.RF_910;
            return frequencyCode;
        }

        private static bool FlashOptionsLenghtValidPredicate(string options)
        {
            bool ret = false;
            byte[] arr = null;
            try
            {
                arr = Tools.GetBytes(options);
                ret = arr.Length == 2 || arr.Length == 6;
            }
            catch
            {
            }
            if (!ret)
            {
                Console.WriteLine(string.Format("Provided flash options: '{0}' is not in valid format.", options));
            }
            return ret;
        }

        private static bool LockBitsLenghtValidPredicate(string lockBits)
        {
            bool ret = false;
            byte[] arr = null;
            try
            {
                arr = Tools.GetBytes(lockBits);
                ret = arr.Length == 9;
            }
            catch
            {
            }
            if (!ret)
            {
                Console.WriteLine(string.Format("Provided lock bits: '{0}' is not in valid format.", lockBits));
            }
            return ret;
        }

        private static bool SramSetOptionsLenghtValidPredicate(string sramOptions)
        {
            bool ret = false;
            byte[] arr = null;
            try
            {
                arr = Tools.GetBytes(sramOptions);
                ret = arr.Length == 6;
            }
            catch
            {
            }
            if (!ret)
            {
                Console.WriteLine(string.Format("Provided sram options: '{0}' is not in valid format.", sramOptions));
            }
            return ret;
        }

        private static bool HomeIdLenghtValidPredicate(string homeId)
        {
            bool ret = false;
            byte[] arr = null;
            try
            {
                arr = Tools.GetBytes(homeId);
                ret = arr.Length == 4;
            }
            catch
            {
            }
            if (!ret)
            {
                Console.WriteLine(string.Format("Provided homeId: '{0}' is not in valid format.", homeId));
            }
            return ret;
        }

        private static bool ValueIsInHexFormatPredicate(string value)
        {
            bool ret = false;
            try
            {
                byte byteVal = Tools.GetByte(value);
                ret = true;
            }
            catch
            {
                Console.WriteLine(string.Format("Provided value: '{0}' is not in HEX format.", value));
                ret = false;
            }
            return ret;
        }

        public class CommandLineArgumentWrapper
        {
            public string CmdKey { get; set; }
            public Predicate<string>[] ArgumentPredicates { get; set; }
            public bool HasDefault { get; set; }
            public CommandLineArgumentWrapper(string cmdKey, params Predicate<string>[] argumentPredicates)
            {
                CmdKey = cmdKey;
                ArgumentPredicates = argumentPredicates;
            }

            public bool IsValid(string arg)
            {
                bool ret = true;
                if (ArgumentPredicates != null)
                {
                    foreach (var item in ArgumentPredicates)
                    {
                        ret &= item(arg);
                    }
                }
                return ret;
            }
        }
    }
}
