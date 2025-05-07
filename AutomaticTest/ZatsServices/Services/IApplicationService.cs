using System;
using System.Collections.Generic;
using Zats.Services.PreconfiguredDevices;
using Zats.Services.QuirkDatabase;


namespace Zats.Services
{
    public interface IApplicationService
    {
        Profile CurrentProfile { get; set; }
        BasicToken.Void AddBasic(Alias deviceNumber);
        BasicToken.Void AddBasicEx(Alias deviceNumber);
        BasicToken.Void AddBasicEx(Alias deviceNumber, SecuritySchemes scheme);
        void AddCrc16(Alias deviceNumber);
        void AddMultiChannel(Alias deviceNumber);
        void AddManufacturerSpecific(Alias deviceNumber, ushort manufacturerId, ushort productId, ushort productTypeId);
        void AddManufacturerSpecific_V2(Alias deviceNumber, ushort manufacturerId, ushort productId, ushort productTypeId, byte deviceIdType, byte deviceIdDataFormat, byte[] deviceId);
        BasicToken.Void AddMultiChannelSupport(Alias deviceNumber);
        List<BasicToken.ResponseData> AddSupportCc(Alias deviceNumber, byte CcId);
        BasicToken.Void AddResponse(Alias deviceNumber, byte[] cmd, byte[] expectedCmd);
        BasicToken.Void AddResponse(Alias deviceNumber, byte[] cmd, TransmitOptions txOptions, byte[] expectedCmd);
        List<BasicToken.ResponseData> SetupRootDevice(Alias deviceNumber, DeviceOptions deviceOptions, byte genericDeviceClass, byte specificDeviceClass,
            bool multiChannel, bool supervision, bool crc16, SecurityVersions securityVersions, bool transportService, IEnumerable<CommandClassSupport> commandClassSupportList, Configuration.ConfigurationItem configuration = null);
        List<BasicToken.ResponseData> SetupEndPoint(Alias deviceNumber, byte endPointId, byte genericDeviceClass, byte specificDeviceClass,
            bool supervision, SecurityVersions securityVersions, IEnumerable<CommandClassSupport> commandClassSupportList, AssociationSupportData rootAssociationSupportData = null);
        TasksAccessPoint SetupControllerDeviceConfigured(Alias deviceNumber, bool multiChannel, bool supervision, bool crc16, SecurityVersions securityVersions, bool transportService, bool overwriteConfig = false);
        TasksAccessPoint SetupControllerDeviceConfigured(Alias deviceNumber);
        TasksAccessPoint SetupControllerDeviceConfiguredAndWaitForDut(Alias deviceNumber, Node dutNodeId, RoleTypes roleType);
        TasksAccessPoint SetupControllerDeviceConfiguredAndWaitForDut(Alias deviceNumber, Node dutNodeId, RoleTypes roleType, byte? initS2SequenceNumber);
        void EnableCommandClassSupportForCallbacks(Alias deviceNumber, byte endPointId, CommandClassSupport commandClassSupport);
        void AddCommandCallbacks(byte commandClass, Dictionary<byte, CommandCallback> callbacks);
        void RemoveCommandCallbacks(byte commandClass);
        BasicToken.Void AddResponseEx(Alias deviceNumber, byte[] cmd, byte[] expectedCmd);
        void AddResponseEx(Alias deviceNumber, byte[] cmd, TransmitOptions txOptions, byte[] expectedCmd);
        void AddResponseEx(Alias deviceNumber, byte[] cmd, byte[] expectedCmd, int NumBytesToCompare);
        void AddSecurity(Alias deviceNumber, SecurityVersions securityVersions, params byte[] supportedCC);
        void AddSecurity(Alias deviceNumber);
        void AddSecurity(Alias deviceNumber, SecurityVersions securityVersions);
        void AddSecurity(Alias deviceNumber, SecurityVersions securityVersions, LibTypes CommandClasses);
        void AddSecurity(Alias deviceNumber, params byte[] supportedCC);
        void AddSecurity(Alias deviceNumber, bool checkIfSupportSecurityClass, params byte[] supportedCC);
        void AddOrModifySecurity(Alias deviceNumber, SecurityVersions securityVersions, bool checkIfSupportSecurityClass, params byte[] supportedCC);
        ApplicationToken.Supervision AddSupervision(Alias deviceNumber);
        void AddTransportService(Alias deviceNumber, int maxFrameSize);
        int AddTransportService(Alias deviceNumber);
        void ClearSecurityDelays(Alias deviceNumber);
        void ClearSecurityTestOverrides(Alias deviceNumber);
        //Zats.Configuration.Xml.Profile CurrentProfile { get; set; }
        byte[] DecryptS0(byte[] receiverNonce, byte[] senderNonce, byte[] securityKey, byte[] message);
        byte[] EncryptS0(byte[] receiverNonce, byte[] senderNonce, byte[] securityKey, byte[] message);
        byte[] DecryptS2(Alias deviceNumber, byte senderId, byte receiverId, byte[] homeId, byte sequenceNumber, byte[] receiverNonce, byte[] senderNonce, byte[] securityKey, int generationCount, bool isRealKey, byte[] fullMessageS2, out int currentGenerationCount);
        byte[] EncryptS2(Alias deviceNumber, byte senderId, byte receiverId, byte[] homeId, byte sequenceNumber, byte[] receiverNonce, byte[] senderNonce, byte[] securityKey, byte[] textToEncrypt, int generationCount, bool isRealKey);
        byte[] DecryptS0MessageEncapsulation(COMMAND_CLASS_SECURITY.SECURITY_MESSAGE_ENCAPSULATION frame, byte[] nonceBytes, byte[] networkKey);
        COMMAND_CLASS_SECURITY.SECURITY_MESSAGE_ENCAPSULATION EncryptS0MessageEncapsulation(byte property, byte[] command, Node senderNodeId, Node receiverNodeId, byte[] senderNonce, byte[] recevierNonce, byte[] networkKey);
        COMMAND_CLASS_SECURITY.SECURITY_MESSAGE_ENCAPSULATION_NONCE_GET EncryptS0MessageEncapsulationNonceGet(byte property, byte[] command, Node senderNodeId, Node receiverNodeId, byte[] senderNonce, byte[] recevierNonce, byte[] networkKey);
        [Obsolete("Marked to be removed. Use method with explicit 'freqency' argument (example value: Context.Frequency)")]
        string GetFile(LibTypes library, ChipSeries chipSeries, DevKits devKit, params string[] regexFilters);
        string GetFile(LibTypes library, ChipSeries chipSeries, string frequency, DevKits devKit, params string[] regexFilters);
        string GetFile(Alias deviceNumber, string ending, DevKits devKit, params string[] regexFilters);
        string GetFile(Alias deviceNumber, DevKits devKit, params string[] regexFilters);
        void EnableNetworkKey(Alias secondary, IEnumerable<SecuritySchemes> requestSchemes);
        byte[] GetSecurityNetworkKey(Alias deviceNumber);
        BasicResult.Test GetTrafficTestResult(Alias deviceNumber, Node node);
        byte[] GetSupportedCommandClasses(Alias deviceNumber);
        void RemoveSecurity(Alias deviceNumber);
        void ResetSecurity(Alias deviceNumber);
        void ResumeSecurity(Alias deviceNumber);
        void ResumeTransportService(Alias deviceNumber, int maxFragmentSize);
        ISecurityTestSettings SecurityTestSettings { get; }
        IProvisioningService Provisioning { get; }
        IAssociations Associations { get; }
        ISecurityS2 SecurityS2 { get; }
        IFirmwareUpdateMD FirmwareUpdateMD { get; }
        Parameters Parameters { get; }
        void SetSecurityDelay(Alias deviceNumber, SecurityS0Delays delay, int timeoutMs);
        void SetSecurityForNode(Alias deviceNumber, Alias nodeNumber, bool isEnable);
        void SetSecurityTestFragmentedPartDisabled(Alias deviceNumber, bool isFirstPartDisabled, bool isSecondPartDisabled);
        void SetSecurityTestMAC(Alias deviceNumber, byte[] code);
        void SetSecurityTestNetworkKey(Alias deviceNumber, byte[] networkKey);
        void SetSecurityTestNetworkKeyTemp(Alias deviceNumber);
        void SetSecurityTestPropertyByte(Alias deviceNumber, byte value);
        void SetSecurityTestPropertyByteSecondFrame(Alias deviceNumber, byte value);
        void SetSecurityTestReceiverNonce(Alias deviceNumber, byte[] nonceData);
        void SetSecurityTestReceiverNonceId(Alias deviceNumber, byte? nonceId);
        void SetSecurityTestScheme(Alias deviceNumber, byte scheme);
        void SetSecurityTestSchemeInGet(Alias deviceNumber, byte scheme);
        void SetSecurityTestSchemeInInherit(Alias deviceNumber, byte scheme);
        void SetSecurityTestSchemeInReport(Alias deviceNumber, byte scheme);
        void SetSecurityTestSchemeInReportEncrypted(Alias deviceNumber, byte scheme);
        void SetSecurityTestSenderNonce(Alias deviceNumber, byte[] nonceData);
        void SetSecurityVersionForNode(Alias deviceNumber, Alias nodeNumber, SecurityVersions securityVersion);
        void StartTrafficFromEndDeviceToNodeIdTest(Alias deviceNumber, Node node, byte[] cmd, byte[] expectedCmd, int intervalMs, int timeoutMs, SecuritySchemes securityScheme);
        void StartTrafficToNodeIdTest(Alias deviceNumber, Node node, byte[] cmd, byte[] expectedCmd, int intervalMs, int timeoutMs, bool isSetSpanFree);
        void StartTrafficToNodeIdTest(Alias deviceNumber, Node node, byte[] cmd, byte[] expectedCmd, int intervalMs, int timeoutMs);
        void StartTrafficToNodeIdTest(Alias deviceNumber, Node node, byte[] cmd, int intervalMs);
        BasicResult.Test StopTrafficTest(Alias deviceNumber, Node node);
        void SuspendSecurity(Alias deviceNumber);
        void SuspendTransportService(Alias deviceNumber);
        ITransportServiceTestSettings TransportServiceTestSettings { get; set; }
        void EnableNetworkKey(Alias deviceNumber, SecuritySchemes securityScheme, bool isEnable);
        void EnableCSA(Alias deviceNumber, bool isEnable);
        void SaveNetworkKeysToFile(Alias deviceNumber, string networkKeysFilePath);
        BasicToken.Void[] SetApplicationCmd(Alias deviceNumber, bool IsS0, bool IsS2s, LibTypes Library);
        BasicToken.Void[] SetApplicationCmd(Alias deviceNumber, bool IsS0, bool IsS2, LibTypes Library, byte[] wakeUpInterval);
        BasicToken.Void[] SetApplicationCmd(Alias deviceNumber, bool IsS0, bool IsS2, LibTypes Library, byte zwavePlusVersion);
        BasicToken.Void[] SetApplicationCmd(Alias deviceNumber, bool IsS0, bool IsS2, LibTypes Library, byte[] wakeUpInterval, byte zwavePlusVersion);
        BasicToken.Void[] SetApplicationResponse(Alias deviceNumber, LibTypes Library, byte[] FullCmd);        
        BasicToken.Void[] SetApplicationResponse(Alias deviceNumber, LibTypes Library, byte[] FullCmd, byte[] wakeUpInterval, byte zWavePlusersion);
        BasicToken.Void[] SetApplicationCmdWithExtraClasses(Alias deviceNumber, bool IsS0, bool IsS2, LibTypes Library, byte[] extraClasses);
        BasicToken.Void[] SetApplicationResponsePreconfigured(Alias deviceNumber, IPreconfiguredDevice preconfiguredDevice);
        void SetApplicationCmdNoResponse(Alias deviceNumber, bool IsS0, bool IsS2s, LibTypes Library);
        void SetProfileSecurityKeys(string S2Access, string S2Auth, string S2UnAuth, string S0Key, string tempNetKey);
        byte[] GetProfileDsk();
        byte[] GetProfileNetworkKey(SecuritySchemes securityScheme);
        Dictionary<SecuritySchemes, byte[]> GetProfileNetworkKeys();
        Profile GetProfile();
        void SetRoleType(Alias deviceNumber, RoleTypes roleType);
        BasicToken.Void AddMultiChannelSupport(Alias deviceNumber, byte[] EndPointCcs);

        /// <summary>
        /// UI Automation only
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="settingsFilePath"></param>
        void SaveSecuritySettingsToFile(Alias deviceNumber, string settingsFilePath);

        /// <summary>
        /// UI Automation only
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="settingsFilePath"></param>
        void OpenSecuritySettingsFromFile(Alias deviceNumber, string securitySettingsFilePath);

        IZWaveClassesConverter ZWaveClasses { get; }
        void ModifyHomeId(Alias deviceNumber, byte[] homeId);

        void AddAssociationSupport(Alias deviceNumber);
        BasicToken.Void AddBasicSupport(Alias deviceNumber);
        void AddCommandClassSupport(Alias deviceNumber, byte endPointId, CommandClassSupport ccSupport, bool updateVersionCC = true);
        void AddCommandClassSupport(Alias deviceNumber, byte[] ccIds);
        void AddOrUpdateVersionCommandClassSupport(Alias deviceNumber, byte endPointId);
        BasicToken.Void[] SetApplicationQuirk(Alias deviceNumber, bool isS0, bool isS2, QuirckLibrary library);
        BasicToken.Void[] SetApplicationQuirk(Alias deviceNumber, bool isS0, bool isS2, QuirckLibrary library, byte[] wakeUpInterval, byte zwavePlusVersion, byte[] extraClasses);
        BasicToken.Void[] SetApplicationQuirkResponse(Alias deviceNumber, QuirckLibrary library, byte[] fullCmd);
        BasicToken.Void[] SetApplicationQuirkResponse(Alias deviceNumber, QuirckLibrary library, byte[] fullCmd, byte[] wakeUpInterval, byte zWavePlusersion, int groupings = 1);
        void CancelAllRunningSupportCommands(Alias deviceNumber);
    }

    public class Parameters
    {
        /// <summary>
        /// Set DSK value for inclusion flow. If value null it means DSK callback cancelled. 
        /// </summary>
        public byte[] InclusionDSKValue { get; set; }
        /// <summary>
        /// Set inclusion granted schemes
        /// </summary>
        public SecuritySchemes InclusionGrantedSchemes { get; set; }
        /// <summary>
        /// Set inclusion granted schemes
        /// </summary>
        public bool InclusionGrantAllRequestedSchemes { get; set; }
        public int InclusionDSKRequestsCount { get; set; }
        public int InclusionSchemeRequestsCount { get; set; }
        public Modes InclusionMode { get; set; }
        public LearnModes InclusionLearnMode { get; set; }
    }
}
