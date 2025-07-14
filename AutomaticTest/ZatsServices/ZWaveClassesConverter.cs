// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZWave.Xml.Application;

namespace Zats.Services
{
    public class ZWaveClassesConverter : IZWaveClassesConverter
    {
        private ZWaveDefinition _zWaveDefinition;
        public ZWaveClassesConverter(ZWaveDefinition zWaveDefinition)
        {
            _zWaveDefinition = zWaveDefinition;
        }

        public string GetFullDescription(byte[] command)
        {
            StringBuilder ret = new StringBuilder();
            ;
            _zWaveDefinition.ParseApplicationObject(command, out CommandClassValue[] commandClassValues);
            if (commandClassValues != null && commandClassValues.Length > 0)
            {
                if (commandClassValues[0].CommandValue != null)
                {
                    var cmd = commandClassValues[0].CommandValue;
                    ret.AppendLine($"= {cmd.CommandDefinition.Text} =");
                    AddParameters(ret, cmd.ParamValues, 1);
                }
            }
            return ret.ToString();
        }

        public string GetCommandClassName(byte commandClassId, byte version = 1)
        {
            CommandClass commandClass = _zWaveDefinition.FindCommandClass(commandClassId, version);

            if (commandClass != null)
            {
                return commandClass.Name;
            }
            else
            {
                return String.Format("0x{0:X} - Command Class not found", commandClassId);
            }
        }

        public CommandClass GetCommandClass(byte commandClassId, byte version = 1)
        {
            return _zWaveDefinition.FindCommandClass(commandClassId, version);
        }

        public CommandClass GetCommandClass(string commandClassNameWithVersion)
        {
            string ccName;
            byte ccVersion;
            string versionString = commandClassNameWithVersion.Substring(commandClassNameWithVersion.LastIndexOf("_"));

            if (versionString.Substring(0, 2) == "_V")
            {
                ccName = commandClassNameWithVersion.Substring(0, commandClassNameWithVersion.LastIndexOf("_"));
                try
                {
                    string versionString2 = versionString.Substring(2);
                    ccVersion = Convert.ToByte(versionString.Substring(2));
                }
                catch
                {
                    ccVersion = 0x01;
                }
            }
            else
            {
                ccName = commandClassNameWithVersion;
                ccVersion = 0x01;
            }

            return _zWaveDefinition.CommandClasses.Where(x => x.Name == ccName && x.Version == ccVersion).FirstOrDefault();
        }

        public string GetCommandName(byte commandClassId, byte commandId, byte version = 1)
        {
            CommandClass commandClass = _zWaveDefinition.FindCommandClass(commandClassId, version);

            if (commandClass != null)
            {
                Command command = _zWaveDefinition.FindCommand(commandClass, commandId);

                if (command != null)
                {
                    return command.Name;
                }
                else
                {
                    return String.Format("0x{0:X}-0x{1:X} - Command not found", commandClassId, commandId);
                }
            }
            else
            {
                return String.Format("0x{0:X} - Command Class not found", commandClassId);
            }
        }

        public Command GetCommand(CommandClass commandClass, byte commandId)
        {
           return _zWaveDefinition.FindCommand(commandClass, commandId);
        }

        private static void AddParameters(StringBuilder ret, IList<ParamValue> pars, int level)
        {
            if (pars != null)
            {
                foreach (var item in pars)
                {
                    if (item.ByteValueList != null && item.ByteValueList.Any())
                    {
                        ret.AppendLine(new string(' ', level * 3) + $"{item.ParamDefinitionText}: {item.TextValue}");
                    }
                    AddParameters(ret, item.InnerValues, level + 1);
                }
            }
        }
    }
}
