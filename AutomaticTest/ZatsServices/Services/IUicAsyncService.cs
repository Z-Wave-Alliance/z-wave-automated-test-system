// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zats.Services
{
    public interface IUicAsyncService
    {
        UicToken.ExpectData ExpectData(Alias uicDeviceNumber, string topic, int timeoutMs);
        UicToken.ExpectData ExpectData(Alias uicDeviceNumber, string topic, int timeoutMs, int count);
        UicToken.ExpectData ExpectDataByUnId(Alias uicDeviceNumber, string topic, int timeoutMs);
        UicToken.ExpectData ExpectDataByUnIdAndEp(Alias uicDeviceNumber, string topic, string ep, int timeoutMs);
        UicToken.ExpectData ExpectDataByUnIdAndEp(Alias uicDeviceNumber, string topic, string ep, int timeoutMs,
            int count);
        UicToken.ListenData ListenData(Alias uicDeviceNumber, string topic, int timeoutMs);
        UicToken.Inclusion IncludeNode(Alias uicDeviceNumber, byte[] dsk, int timeoutMs);
        UicToken.Inclusion IncludeGenericNode(Alias uicDeviceNumber, string dsk, int timeoutMs);
        UicToken.Exclusion ExcludeNode(Alias uicDeviceNumber, Alias zwaveEndDevice, int timeoutMs);
        UicToken.ExpectData ExpectDataByEp(string topic, string ep, int timeoutMs);
        UicToken.ExpectData ExpectDataByMqttClient(string mqttclient, string topic);
        UicResult.Inclusion HandlePinRequest(Alias zwaveEndDevice, int timeoutMs);
        UicToken.ExpectData ExpectDataByMachineId(string machineId, string topic);
    }
}
