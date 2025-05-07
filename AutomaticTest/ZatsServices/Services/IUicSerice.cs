using System;
using System.Collections.Generic;
using ZWave.UicApplication.PayloadWrappers.Enums;
using static Zats.Services.UicService;

namespace Zats.Services
{
    public interface IUicSerice
    {
        void SetDefault(Alias uicDeviceNumber, int timeoutMs);
        void SetIdle(Alias uicDeviceNumber, int timeoutMs);
        void DeleteRetainedTopics(Alias uicDeviceNumber);
        void Publish(Alias uicDeviceNumber, string topic, string payload);
        void Publish(string topic, string payload, bool isRetain);
        void PublishByUNID(Alias uicDeviceNumber, string topic, string payload);
        void PublishByGroup(Alias uicDeviceNumber, string topic, string payload, int group);
        void Subscribe(Alias uicDeviceNumber, string topic);
        void SubscribeByUNID(Alias uicDeviceNumber, string topic);
        void SubscribeByUNIDandEndPoint(Alias uicDeviceNumber, string topic, string endPoing);
        UicResult.Inclusion IncludeNode(Alias uicDeviceNumber, Alias zwaveEndDevice, int timeoutMs, bool isWaitForInterview = false);
        UicResult.Inclusion ClassicIncludeNode(Alias uicDeviceNumber, Alias zwaveEndDevice, int timeoutMs);
        UicResult.Inclusion ClassicIncludeForFw(Alias uicDeviceNumber, byte[] deviceDsk, int timeoutMs);
        UicResult.Exclusion ExcludeNode(Alias uicDeviceNumber, Alias zwaveEndDevice, int timeoutMs);
        UicResult.Exclusion ExcludeNodeForFW(Alias uicDeviceNumber, string endDeviceNodeId, int timeoutMs);
        UicResult.Exclusion FailedRemove(Alias zwaveEndDevice, int timeoutMs);
        void RegisterUicDevice(Alias virtualDeviceNumber, string unID);
        void UnRegisterUicDevice(Alias virtualDeviceNumber);
        void SubscribeToAllUNID(Alias uicDeviceNumber, string topic);
        void PublishByUNIDandEp(Alias uicDeviceNumber, string topic, string payload, string ep);
        void PublishByMachineId(string machineId, string topic, string payload);
        UicResult.GetNodes GetNodes(int timeout);
        void SetOnOff(Alias uicDeviceNumber, OnOffStatuses onOffStatus, string ep);
        void SubscribeToAllUNIDandEp(string topic);
        List<string> GetSmartStartListEntries(int timeoutMs);
        void DeleteSmartStartListEntries(List<string> entries, int timeoutMs);
        DateTime Get_PublishMetrics_ByUNIDandEp(Alias uicDeviceNumber, string topic, string payload, string ep);
        string GetUnId(Alias uicDeviceNuner);
        void CreateLocalJson(Root JsonData);
        void UpdateHomeIdToJson(string NewHomeId);
        Root setRootFile(string DeviceHomeId);
        bool ConfigurePollingIntervals(Alias sshClientAlias);
        bool RestartRpi(Alias sshClientAlias);
        string SendCustomSshCommand(Alias sshClientAlias, string commandToSend);
        bool CopyFileOverSsh(Alias sshClientAlias, string sourcePath, string targetPath);
        void StopAndCleanMatterBridgeLogs(Alias sshClientAlias);
        string StartMatterBridgeLogs(Alias sshClientAlias);
        void PairBridge(Alias sshClientAlias, string qrcode);
        string ReadPartsList(Alias sshClientAlias);
        void ToogleMatterBridge(Alias sshClientAlias, string partNo);
        void OnMatterBridge(Alias sshClientAlias, string partNo);
        void OffMatterBridge(Alias sshClientAlias, string partNo);
        string ReadOnOffMatterBridge(Alias sshClientAlias, string partNo);
        bool BasicInfoMatterBridge(Alias sshClientAlias, string partNo);
        string ReadNodeLableMatterBridge(Alias sshClientAlias, string partNo);
        void MoveToLevelMatterBridge(Alias sshClientAlias, string partNo, string level);
        string ReadLevelMatterBridge(Alias sshClientAlias, string partNo);
        string ReadOccupancyMatterBridge(Alias sshClientAlias, string partNo);
        string ReadTemperatureSensingMatterBridge(Alias sshClientAlias, string partNo);
        string ReadColorControlHUEMatterBridge(Alias sshClientAlias, string partNo);
        void MoveToHUEMatterBridge(Alias sshClientAlias, string partNo, string value);
        string ReadColorControlSaturationMatterBridge(Alias sshClientAlias, string partNo);
        void MoveToSaturationMatterBridge(Alias sshClientAlias, string partNo, string value);
        void MoveSaturationMatterBridge(Alias sshClientAlias, string mode, string partNo);
        void StepSaturationMatterBridge(Alias sshClientAlias, string mode, string size, string partNo);
        void MoveHUEMatterBridge(Alias sshClientAlias, string mode, string partNo);
        void StepHUEMatterBridge(Alias sshClientAlias, string mode, string size, string partNo);
        void MoveToHueandSaturationMatterBridge(Alias sshClientAlias, string partNo, string hueValue, string saturationValue);
        void SetGroupMatterBridge(Alias sshClientAlias);
        void sendMulticastMatterBridge(Alias sshClientAlias);
        void AddToSmartStartList(Alias zwaveEndDevice);
    }
}
