// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    public enum GenericTypes
    {
        GENERIC_CONTROLLER = 0x01,
        STATIC_CONTROLLER = 0x02,
        AV_CONTROL_POINT = 0x03,
        DISPLAY = 0x04,
        NETWORK_EXTENDER = 0x05,
        APPLIANCE = 0x06,
        SENSOR_NOTIFICATION = 0x07,
        THERMOSTAT = 0x08,
        WINDOW_COVERING = 0x09,
        REPEATER_END_NODE = 0x0F,
        SWITCH_BINARY = 0x10,
        SWITCH_MULTILEVEL = 0x11,
        SWITCH_REMOTE = 0x12,
        SWITCH_TOGGLE = 0x13,
        ZIP_NODE = 0x15,
        VENTILATION = 0x16,
        SECURITY_PANEL = 0x17,
        WALL_CONTROLLER = 0x18,
        SENSOR_BINARY = 0x20,
        SENSOR_MULTILEVEL = 0x21,
        METER_PULSE = 0x30,
        METER = 0x31,
        ENTRY_CONTROL = 0x40,
        SEMI_INTEROPERABLE = 0x50,
        SENSOR_ALARM = 0xA1,
        NON_INTEROPERABLE = 0xFF,
    }
}
