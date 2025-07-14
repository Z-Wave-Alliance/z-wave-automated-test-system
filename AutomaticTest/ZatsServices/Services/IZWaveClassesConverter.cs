// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using ZWave.Xml.Application;

namespace Zats.Services
{
    public interface IZWaveClassesConverter
    {
        string GetFullDescription(byte[] command);
        string GetCommandClassName(byte commandClassId, byte version = 1);
        CommandClass GetCommandClass(byte commandClassId, byte version = 1);
        CommandClass GetCommandClass(string commandClassNameWithVersion);
        string GetCommandName(byte commandClassId, byte commandId, byte version = 1);
        Command GetCommand(CommandClass commandClass, byte commandId);
    }
}
