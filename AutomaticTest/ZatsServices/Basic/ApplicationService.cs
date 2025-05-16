using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Utils.UI.Enums;
using Zats.Services.PreconfiguredDevices;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using ZWave.BasicApplication.Security;
using ZWave.BasicApplication.Tasks;
using ZWave.BasicApplication.TransportService;
using ZWave.Devices;
using ZWave.Layers;
using ZWave.Security.S2;
using Zats.Services.QuirkDatabase;


namespace Zats.Services
{
    public class ApplicationService : ServiceBase, IApplicationService
    {
        Dictionary<int, ActionToken> testResults = new Dictionary<int, ActionToken>();
        readonly ZWave.Enums.TransmitOptions TXO = ZWave.Enums.TransmitOptions.TransmitOptionAcknowledge |
            ZWave.Enums.TransmitOptions.TransmitOptionAutoRoute |
            ZWave.Enums.TransmitOptions.TransmitOptionExplore;
        readonly ZWave.Enums.TransmitOptions2 TXO2 = ZWave.Enums.TransmitOptions2.NONE;
        readonly ZWave.Enums.TransmitSecurityOptions TXSO = ZWave.Enums.TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY;
        private DeviceManager DeviceManager { get; set; }
        public Profile CurrentProfile { get; set; }
        public IZWaveClassesConverter ZWaveClasses { get; private set; }
        public IProvisioningService Provisioning { get; private set; }
        public CommandClassesSupportHandler CommandClassesSupportHandler { get; private set; }

        public Parameters Parameters
        {
            get { return DeviceManager.Parameters; }
        }
        public ITransportServiceTestSettings TransportServiceTestSettings { get; set; }

        public ISecurityTestSettings SecurityTestSettings
        {
            get
            {
                return DeviceManager.SecurityTestSettings;
            }
        }

        public IAssociations Associations
        {
            get
            {
                return DeviceManager.Associations;
            }
        }

        public ISecurityS2 SecurityS2
        {
            get
            {
                return DeviceManager.SecurityS2;
            }
        }

        public IFirmwareUpdateMD FirmwareUpdateMD
        {
            get
            {
                return DeviceManager.FirmwareUpdateMD;
            }
        }


        public IIncludeSettings IncludeSettings
        {
            get
            {
                return DeviceManager.IncludeSettings;
            }

            set
            {
                DeviceManager.IncludeSettings = value;
            }
        }


        public ApplicationService(DeviceManager deviceManager)
        {
            DeviceManager = deviceManager;
            TransportServiceTestSettings = new TransportServiceTestSettings(deviceManager);
            ZWaveClasses = new ZWaveClassesConverter(deviceManager.ZWaveDefinition);
            Provisioning = new ProvisioningService(deviceManager);
            CommandClassesSupportHandler = new CommandClassesSupportHandler();
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            DeviceManager.Log(text, logLevel);
        }

        public BasicToken.Void AddResponse(Alias deviceNumber, byte[] cmd, byte[] expectedCmd)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            var responseToken = device.ResponseData(new Node(0, deviceNumber.EndPointId), cmd, TXO, expectedCmd);
            return new BasicActionToken.Void(responseToken);
        }

        public BasicToken.Void AddResponse(Alias deviceNumber, byte[] cmd, TransmitOptions txOptions, byte[] expectedCmd)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            var responseToken = device.ResponseData(new Node(0, deviceNumber.EndPointId), cmd, (ZWave.Enums.TransmitOptions)txOptions, expectedCmd);
            return new BasicActionToken.Void(responseToken);
        }

        public BasicToken.Void AddResponseEx(Alias deviceNumber, byte[] cmd, byte[] expectedCmd)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            var responseToken = device.ResponseDataEx(new NodeTag(0, deviceNumber.EndPointId), cmd, ZWave.Enums.TransmitOptions.TransmitOptionAcknowledge | ZWave.Enums.TransmitOptions.TransmitOptionAutoRoute |
                ZWave.Enums.TransmitOptions.TransmitOptionExplore, expectedCmd);
            return new BasicActionToken.Void(responseToken);
        }

        public void AddResponseEx(Alias deviceNumber, byte[] cmd, TransmitOptions txOptions, byte[] expectedCmd)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            device.ResponseDataEx(new Node(0, deviceNumber.EndPointId), cmd, (ZWave.Enums.TransmitOptions)txOptions, expectedCmd);
        }

        public void AddResponseEx(Alias deviceNumber, byte[] cmd, byte[] expectedCmd, int NumBytesToCompare)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            device.ResponseDataEx(new Node(0, deviceNumber.EndPointId), cmd, (ZWave.Enums.TransmitOptions)TXO, expectedCmd, NumBytesToCompare);
        }

        public List<BasicToken.ResponseData> AddSupportCc(Alias deviceNumber, byte CcId)
        {
            List<BasicToken.ResponseData> returnTokens = new List<BasicToken.ResponseData>();
            Device device = DeviceManager.GetDevice(deviceNumber);
            byte[] cmd = new byte[] { };
            byte[] expectedCmd = new byte[] { };

            switch (CcId)
            {
                case COMMAND_CLASS_VERSION.ID:

                    cmd = new COMMAND_CLASS_VERSION_V3.VERSION_COMMAND_CLASS_REPORT()
                    {
                        commandClassVersion = 3,
                        requestedCommandClass = CcId
                    };
                    expectedCmd = new COMMAND_CLASS_VERSION_V3.VERSION_COMMAND_CLASS_GET()
                    {
                        requestedCommandClass = CcId
                    };
                    returnTokens.Add(new BasicToken.ResponseData(device.ResponseDataEx(new Node(0, deviceNumber.EndPointId), cmd,
                        TXO, expectedCmd, 3)));

                    cmd = new COMMAND_CLASS_VERSION_V3.VERSION_CAPABILITIES_REPORT()
                    {
                        properties1 = { zWaveSoftware = 0x01, version = 0x01, commandClass = 0x01 }
                    };
                    expectedCmd = new COMMAND_CLASS_VERSION_V3.VERSION_CAPABILITIES_GET();
                    returnTokens.Add(new BasicToken.ResponseData(device.ResponseDataEx(new Node(0, deviceNumber.EndPointId), cmd,
                            TXO, expectedCmd)));

                    cmd = new COMMAND_CLASS_VERSION_V3.VERSION_ZWAVE_SOFTWARE_REPORT()
                    {
                    };
                    expectedCmd = new COMMAND_CLASS_VERSION_V3.VERSION_ZWAVE_SOFTWARE_GET();
                    returnTokens.Add(new BasicToken.ResponseData(device.ResponseDataEx(new Node(0, deviceNumber.EndPointId), cmd,
                            TXO, expectedCmd)));

                    break;

                case COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID:

                    cmd = new COMMAND_CLASS_VERSION_V3.VERSION_COMMAND_CLASS_REPORT()
                    {
                        commandClassVersion = 2,
                        requestedCommandClass = CcId
                    };
                    expectedCmd = new COMMAND_CLASS_VERSION_V3.VERSION_COMMAND_CLASS_GET()
                    {
                        requestedCommandClass = CcId
                    };
                    returnTokens.Add(new BasicToken.ResponseData(device.ResponseDataEx(new Node(0, deviceNumber.EndPointId), cmd,
                        TXO, expectedCmd, 3)));

                    cmd = new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_REPORT()
                    {
                        zWaveVersion = 0x02,
                        roleType = 0x05,
                        nodeType = 0x00,
                        installerIconType = new byte[] { 0x07, 0x00 },
                        userIconType = new byte[] { 0x07, 0x00 }
                    };
                    expectedCmd = new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_GET();
                    returnTokens.Add(new BasicToken.ResponseData(device.ResponseDataEx(new Node(0, deviceNumber.EndPointId), cmd, TXO, expectedCmd)));

                    break;

                case COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID:
                    ushort manufacturerId = 0;
                    ushort productId = 2;
                    ushort productTypeId = 3;

                    device.SessionClient.ExecuteAsync(
                        new ZWave.BasicApplication.CommandClasses.ManufacturerSpecificSupport(
                            device.Network, TXO, manufacturerId, productId, productTypeId));

                    break;

                case COMMAND_CLASS_WAKE_UP.ID:
                    device.SessionClient.ExecuteAsync(
                        new ZWave.BasicApplication.CommandClasses.WakeupSupport(device.Network, TXO));
                    break;

                case COMMAND_CLASS_MULTI_CHANNEL_V4.ID:
                    byte[] ENDPOINTS_COMMAND_CLASSES = {
                        COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID, COMMAND_CLASS_ASSOCIATION.ID,
                        COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_BASIC_V2.ID, COMMAND_CLASS_SWITCH_BINARY_V2.ID,
                        COMMAND_CLASS_SWITCH_ALL.ID, COMMAND_CLASS_POWERLEVEL.ID, COMMAND_CLASS_SECURITY_2.ID, COMMAND_CLASS_SUPERVISION.ID,
                        COMMAND_CLASS_TRANSPORT_SERVICE_V2.ID };
                    device.SessionClient.ExecuteAsync(
                        new ZWave.BasicApplication.CommandClasses.MultiChannelSupport(device.Network, TXO, ENDPOINTS_COMMAND_CLASSES));

                    break;

                case COMMAND_CLASS_ASSOCIATION.ID:

                    device.SessionClient.ExecuteAsync(
                         new ZWave.BasicApplication.CommandClasses.AssociationSupport(device.Network, TXO));
                    break;

                case COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID:

                    cmd = new COMMAND_CLASS_VERSION_V3.VERSION_COMMAND_CLASS_REPORT()
                    {
                        commandClassVersion = 3,
                        requestedCommandClass = CcId
                    };
                    expectedCmd = new COMMAND_CLASS_VERSION_V3.VERSION_COMMAND_CLASS_GET()
                    {
                        requestedCommandClass = CcId
                    };
                    returnTokens.Add(new BasicToken.ResponseData(device.ResponseDataEx(new Node(0, deviceNumber.EndPointId), cmd, TXO, expectedCmd, 3)));

                    break;

                default:
                    return null;
            }

            return returnTokens;
        }

        public List<BasicToken.ResponseData> SetupRootDevice(Alias deviceNumber, DeviceOptions deviceOptions, byte genericDeviceClass, byte specificDeviceClass,
            bool multiChannel, bool supervision, bool crc16, SecurityVersions securityVersions, bool transportService, IEnumerable<CommandClassSupport> commandClassSupportList, 
            Configuration.ConfigurationItem configuration = null)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            List<BasicToken.ResponseData> returnTokens = new List<BasicToken.ResponseData>();

            List<byte> beforeInclusionCCs = commandClassSupportList.Where(x => x.SecureExclusive == false && x.CommandClassId != COMMAND_CLASS_BASIC.ID).Select(x => x.CommandClassId).ToList();
            List<byte> unsecureCCs = commandClassSupportList.Where(x => x.Secure == false && x.CommandClassId != COMMAND_CLASS_BASIC.ID).Select(x => x.CommandClassId).ToList();
            List<byte> secureCCs = commandClassSupportList.Where(x => x.Secure == true && x.CommandClassId != COMMAND_CLASS_BASIC.ID).Select(x => x.CommandClassId).ToList();

            byte supervisionVersion = 0;
            if (supervision)
            {
                var superVisionToken = AddSupervision(deviceNumber);
                var supervisionCallbacks = GetSupervisionCallbacks(commandClassSupportList);
                superVisionToken.SetSupervisionReportCallbacks(supervisionCallbacks);

                beforeInclusionCCs.Add(COMMAND_CLASS_SUPERVISION.ID);
                unsecureCCs.Add(COMMAND_CLASS_SUPERVISION.ID);
                supervisionVersion = 1;
            }

            if (multiChannel)
            {
                AddMultiChannel(deviceNumber);
            }

            AddResponseEx(deviceNumber,
                        new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT()
                        {
                            requestedCommandClass = COMMAND_CLASS_SUPERVISION.ID,
                            commandClassVersion = supervisionVersion
                        },
                        new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET() { requestedCommandClass = COMMAND_CLASS_SUPERVISION.ID }, 3);

            byte crc16Version = 0;
            if (crc16)
            {
                AddCrc16(deviceNumber);
                beforeInclusionCCs.Add(COMMAND_CLASS_CRC_16_ENCAP.ID);
                unsecureCCs.Add(COMMAND_CLASS_CRC_16_ENCAP.ID);
                crc16Version = 1;
            }
            AddResponseEx(deviceNumber,
                        new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT()
                        {
                            requestedCommandClass = COMMAND_CLASS_CRC_16_ENCAP.ID,
                            commandClassVersion = crc16Version
                        },
                        new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET() { requestedCommandClass = COMMAND_CLASS_CRC_16_ENCAP.ID }, 3);

            AddOrModifySecurityConfigured( // includes CreateInclusionControllerSupportTask
                deviceNumber, securityVersions, configuration?.SecuritySettings?.NetworkKeys, false, secureCCs.ToArray()); 

            byte s0Version = 0;
            if (securityVersions.HasFlag(SecurityVersions.S0))
            {
                beforeInclusionCCs.Add(COMMAND_CLASS_SECURITY.ID);
                unsecureCCs.Add(COMMAND_CLASS_SECURITY.ID);
                s0Version = 1;
            }
            AddResponseEx(deviceNumber,
                        new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT()
                        {
                            requestedCommandClass = COMMAND_CLASS_SECURITY.ID,
                            commandClassVersion = s0Version
                        },
                        new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET() { requestedCommandClass = COMMAND_CLASS_SECURITY.ID }, 3);

            byte s2Version = 0;
            if (securityVersions.HasFlag(SecurityVersions.S2))
            {
                beforeInclusionCCs.Add(COMMAND_CLASS_SECURITY_2.ID);
                unsecureCCs.Add(COMMAND_CLASS_SECURITY_2.ID);
                s2Version = 1;
            }
            AddResponseEx(deviceNumber,
                        new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT()
                        {
                            requestedCommandClass = COMMAND_CLASS_SECURITY_2.ID,
                            commandClassVersion = s2Version
                        },
                        new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET() { requestedCommandClass = COMMAND_CLASS_SECURITY_2.ID }, 3);

            byte transportServiceVersion = 0;
            if (transportService)
            {
                AddTransportService(deviceNumber);
                beforeInclusionCCs.Add(COMMAND_CLASS_TRANSPORT_SERVICE.ID);
                unsecureCCs.Add(COMMAND_CLASS_TRANSPORT_SERVICE.ID);
                transportServiceVersion = 2;
            }
            AddResponseEx(deviceNumber,
                        new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT()
                        {
                            requestedCommandClass = COMMAND_CLASS_TRANSPORT_SERVICE.ID,
                            commandClassVersion = transportServiceVersion
                        },
                        new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET() { requestedCommandClass = COMMAND_CLASS_TRANSPORT_SERVICE.ID }, 3);

            beforeInclusionCCs = GetDistinctOrderedCCIds(beforeInclusionCCs);
            unsecureCCs = GetDistinctOrderedCCIds(unsecureCCs);
            secureCCs = GetDistinctOrderedCCIds(secureCCs);

            //byte[] afterInclusionCCs = beforeInclusionCCs.Where(x => !secureCCs.Contains(x)).ToArray(); // general alternative for unsecureCCs, would work as well
            ActionResult res = device.ApplicationNodeInformation((ZWave.Enums.DeviceOptions)deviceOptions, genericDeviceClass, specificDeviceClass, unsecureCCs.ToArray());
            device.Network.SetApplicationNodeInformation((ZWave.Enums.DeviceOptions)deviceOptions, genericDeviceClass, specificDeviceClass, beforeInclusionCCs.ToArray());
            res = device.ApplicationNodeInformationCmdClasses(beforeInclusionCCs.ToArray(), unsecureCCs.ToArray(), secureCCs.ToArray());

            AssociationSupportData associationSupportData = (commandClassSupportList.ToList().Find(x => x.CommandClassId == COMMAND_CLASS_ASSOCIATION.ID) as AssociationSupport)?.AssociationSupportData;

            foreach (CommandClassSupport ccSupport in commandClassSupportList)
            {
                DeviceManager.EnableCommandClassSupport(deviceNumber, 0, ccSupport);
                ccSupport.ReportDevice = DeviceManager.GetDevice(deviceNumber);
                ccSupport.RootAssociationData = associationSupportData;
                ccSupport.StartEmulation();
            }
            return returnTokens;
        }

        private List<byte> GetDistinctOrderedCCIds(List<byte> ccList)
        {
            return ccList.Distinct()
                .OrderBy(x => x != COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID)
                .ThenBy(x => x != COMMAND_CLASS_SECURITY_2.ID)
                .ThenBy(x => x != COMMAND_CLASS_SECURITY.ID)
                .ThenBy(x => x != COMMAND_CLASS_INCLUSION_CONTROLLER.ID && x != COMMAND_CLASS_CRC_16_ENCAP.ID && x != COMMAND_CLASS_MULTI_CMD.ID
                    && x != COMMAND_CLASS_MULTI_CHANNEL_V4.ID && x != COMMAND_CLASS_SUPERVISION.ID && x != COMMAND_CLASS_TRANSPORT_SERVICE_V2.ID)
                .ThenBy(x => x != COMMAND_CLASS_ASSOCIATION_V2.ID && x != COMMAND_CLASS_ASSOCIATION_GRP_INFO.ID && x != COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID
                    && x != COMMAND_CLASS_VERSION.ID && x != COMMAND_CLASS_WAKE_UP_V2.ID)
                .ToList();
        }

        public void CancelAllRunningSupportCommands(Alias deviceNumber)
        {
            DeviceManager.CancelAllRunningSupportCommands(deviceNumber);
        }

        public List<BasicToken.ResponseData> SetupEndPoint(Alias deviceNumber, byte endPointId, byte genericDeviceClass, byte specificDeviceClass, bool supervision, SecurityVersions securityVersions, IEnumerable<CommandClassSupport> commandClassSupportList, AssociationSupportData rootAssociationSupportData = null)
        {
            List<BasicToken.ResponseData> returnTokens = new List<BasicToken.ResponseData>();

            List<byte> secureCCs = commandClassSupportList.Where(x => x.Secure == true && x.CommandClassId != COMMAND_CLASS_BASIC.ID).Select(x => x.CommandClassId).ToList();

            if (supervision)
            {
                var superVisionToken = AddSupervision(deviceNumber.EndPoint(endPointId));
                var supervisionCallbacks = GetSupervisionCallbacks(commandClassSupportList);
                superVisionToken.SetSupervisionReportCallbacks(supervisionCallbacks);
            }

            if (securityVersions.HasFlag(SecurityVersions.S2))
            {
                AddResponseEx(deviceNumber.EndPoint(endPointId),
                    new COMMAND_CLASS_SECURITY_2.SECURITY_2_COMMANDS_SUPPORTED_REPORT()
                    {
                        commandClass = secureCCs
                    },
                    new COMMAND_CLASS_SECURITY_2.SECURITY_2_COMMANDS_SUPPORTED_GET());

                if (securityVersions.HasFlag(SecurityVersions.S0))
                {
                    // if S2 and S0 is supported the S0 Security Commands Supported Report must be emtpy
                    AddResponseEx(deviceNumber.EndPoint(endPointId),
                        new COMMAND_CLASS_SECURITY.SECURITY_COMMANDS_SUPPORTED_REPORT()
                        {
                            commandClassSupport = new List<byte>(),
                            commandClassControl = new List<byte>(),
                            reportsToFollow = 0
                        },
                        new COMMAND_CLASS_SECURITY.SECURITY_COMMANDS_SUPPORTED_GET());
                }
            }
            else if (securityVersions.HasFlag(SecurityVersions.S0))
            {
                AddResponseEx(deviceNumber.EndPoint(endPointId),
                        new COMMAND_CLASS_SECURITY.SECURITY_COMMANDS_SUPPORTED_REPORT()
                        {
                            commandClassSupport = secureCCs,
                            commandClassControl = new List<byte>(),
                            reportsToFollow = 0
                        },
                        new COMMAND_CLASS_SECURITY.SECURITY_COMMANDS_SUPPORTED_GET());
            }

            foreach (CommandClassSupport ccSupport in commandClassSupportList)
            {
                DeviceManager.EnableCommandClassSupport(deviceNumber, endPointId, ccSupport);
                ccSupport.ReportDevice = DeviceManager.GetDevice(deviceNumber);
                if (ccSupport.RootAssociationData == null)
                {
                    ccSupport.RootAssociationData = rootAssociationSupportData;
                }
                ccSupport.StartEmulation();
            }

            return returnTokens;
        }

        private Dictionary<byte, Action<COMMAND_CLASS_SUPERVISION.SUPERVISION_GET, COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT>> GetSupervisionCallbacks(IEnumerable<CommandClassSupport> commandClassSupportList)
        {
            var supervisionCallbacks = new Dictionary<byte, Action<COMMAND_CLASS_SUPERVISION.SUPERVISION_GET, COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT>>();

            foreach (CommandClassSupport commandClass in commandClassSupportList)
            {
                supervisionCallbacks.Add((byte)commandClass.CommandClassId, commandClass.SupervisionCallback);
            }

            return supervisionCallbacks;
        }

        /// <summary>
        /// Enable CommandClassSupport for callbacks
        /// </summary>
        public void EnableCommandClassSupportForCallbacks(Alias deviceNumber, byte endPointId, CommandClassSupport commandClassSupport)
        {
            DeviceManager.EnableCommandClassSupport(deviceNumber, endPointId, commandClassSupport);
        }

        public void AddCommandCallbacks(byte commandClass, Dictionary<byte, CommandCallback> callbacks)
        {
            DeviceManager.AddCommandCallbacks(commandClass, callbacks);
        }

        public void RemoveCommandCallbacks(byte commandClass)
        {
            DeviceManager.RemoveCommandCallbacks(commandClass);
        }

        public void SetSecurityTestScheme(Alias deviceNumber, byte scheme)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestSecuritySchemeS0(scheme);
            }
        }

        public void SetSecurityTestSchemeInGet(Alias deviceNumber, byte scheme)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestSecuritySchemeInGetS0(scheme);
            }
        }

        public void SetSecurityTestSchemeInReport(Alias deviceNumber, byte scheme)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestSecuritySchemeInReportS0(scheme);
            }
        }

        public void SetSecurityTestSchemeInReportEncrypted(Alias deviceNumber, byte scheme)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestSecuritySchemeInReportEncryptedS0(scheme);
            }
        }

        public void SetSecurityTestSchemeInInherit(Alias deviceNumber, byte scheme)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestSecuritySchemeInInheritS0(scheme);
            }
        }

        public void SetSecurityTestReceiverNonce(Alias deviceNumber, byte[] nonceData)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestReceiverNonceS0(nonceData);
            }
        }

        public void SetSecurityTestSenderNonce(Alias deviceNumber, byte[] nonceData)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestSenderNonceS0(nonceData);
            }
        }

        public void SetSecurityTestFragmentedPartDisabled(Alias deviceNumber, bool isFirstPartDisabled, bool isSecondPartDisabled)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestFragmentedPartDisabled(isFirstPartDisabled, isSecondPartDisabled);
            }
        }

        public void SetSecurityTestReceiverNonceId(Alias deviceNumber, byte? nonceId)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestReceiverNonceIdS0(nonceId);
            }
        }

        public byte[] DecryptS0MessageEncapsulation(COMMAND_CLASS_SECURITY.SECURITY_MESSAGE_ENCAPSULATION frame, byte[] nonceBytes, byte[] networkKey)
        {
            // thats correct encryptS0(encryptS0(data)) == data
            return EncryptS0MessageEncapsulation(frame.properties1, frame.commandByte.ToArray(),
                2, 1, frame.initializationVectorByte.ToArray(), nonceBytes, networkKey).commandByte.ToArray();
        }

        public COMMAND_CLASS_SECURITY.SECURITY_MESSAGE_ENCAPSULATION EncryptS0MessageEncapsulation(byte property, byte[] command, Node senderNodeId, Node receiverNodeId, byte[] senderNonce, byte[] recevierNonce, byte[] networkKey)
        {
            return SecurityS0CryptoProvider.EncryptCommand(property, command, senderNodeId, receiverNodeId,
                senderNonce, recevierNonce, networkKey, false);
        }

        public COMMAND_CLASS_SECURITY.SECURITY_MESSAGE_ENCAPSULATION_NONCE_GET EncryptS0MessageEncapsulationNonceGet(byte property, byte[] command, Node senderNodeId, Node receiverNodeId, byte[] senderNonce, byte[] recevierNonce, byte[] networkKey)
        {
            return SecurityS0CryptoProvider.EncryptCommand(property, command, senderNodeId, receiverNodeId,
                senderNonce, recevierNonce, networkKey, true);
        }

        public byte[] EncryptS0(byte[] receiverNonce, byte[] senderNonce, byte[] securityKey, byte[] message)
        {
            return SecurityS0CryptoProvider.EncryptPayload(receiverNonce, senderNonce, securityKey, message);
        }

        public byte[] DecryptS0(byte[] receiverNonce, byte[] senderNonce, byte[] securityKey, byte[] message)
        {
            return SecurityS0CryptoProvider.DecryptPayload(receiverNonce, senderNonce, securityKey, message);
        }

        public void SetSecurityTestNetworkKey(Alias deviceNumber, byte[] networkKey)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestNetworkKey(networkKey, ZWave.Enums.SecuritySchemes.S0, false);
                securityManager.SecurityManagerInfo.ActivateNetworkKeyS0();
            }
        }

        public void SetSecurityTestNetworkKeyTemp(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.ActivateNetworkKeyS0Temp();
            }
        }

        public void SetSecurityTestMAC(Alias deviceNumber, byte[] code)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestMacS0(code);
            }
        }

        public void SetSecurityTestPropertyByte(Alias deviceNumber, byte value)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestPropertyByteS0(value);
            }
        }

        public void SetSecurityTestPropertyByteSecondFrame(Alias deviceNumber, byte value)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestPropertyByteSecondFrameS0(value);
            }
        }

        public void ClearSecurityTestOverrides(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.ClearTestOverridesS0();
            }
        }

        public void ClearSecurityDelays(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.ClearDelaysS0();
            }
        }

        public void SetSecurityDelay(Alias deviceNumber, SecurityS0Delays delay, int timeoutMs)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetDelayS0((ZWave.BasicApplication.Security.SecurityS0Delays)((byte)delay), timeoutMs);
            }
        }

        public byte[] GetSecurityNetworkKey(Alias deviceNumber)
        {
            byte[] ret = new byte[0];
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                ret = securityManager.SecurityManagerInfo.GetActualNetworkKey(ZWave.Enums.SecuritySchemes.S0, false);
            }
            return ret;
        }

        public void SuspendTransportService(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(TransportServiceManager));
            sm.Suspend();
        }

        public void ResumeTransportService(Alias deviceNumber, int maxFragmentSize)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            device.MemoryGetId();
            TransportServiceManager sm = (TransportServiceManager)device.SessionClient.GetSubstituteManager(typeof(TransportServiceManager));
            sm.Resume();
        }

        public void SuspendSecurity(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            SecurityManager sm = (SecurityManager)device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            sm.Suspend();
        }

        public void ResumeSecurity(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            device.MemoryGetId();
            SecurityManager sm = (SecurityManager)device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            sm.Resume();
        }

        /// <summary>
        /// Add Security Support (version S0)
        /// </summary>
        /// <param name="deviceNumber">device alias</param>
        public void AddSecurity(Alias deviceNumber)
        {
            AddSecurity(deviceNumber, SecurityVersions.S0);
        }

        /// <summary>
        /// Add Security Support
        /// </summary>
        /// <param name="deviceNumber">device alias</param>
        /// <param name="securityVersions">security version (S0, S2)</param>
        public void AddSecurity(Alias deviceNumber, SecurityVersions securityVersions)
        {
            var supportedCC = new byte[]
            {
                COMMAND_CLASS_BASIC.ID,
                COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID,
                COMMAND_CLASS_VERSION.ID,
                COMMAND_CLASS_POWERLEVEL.ID,
                COMMAND_CLASS_INCLUSION_CONTROLLER.ID
            };
            if (securityVersions.HasFlag(SecurityVersions.S0))
            {
                supportedCC = supportedCC.Union(new[] { COMMAND_CLASS_SECURITY.ID }).ToArray();
            }
            if (securityVersions.HasFlag(SecurityVersions.S2))
            {
                supportedCC = supportedCC.Union(new[] { COMMAND_CLASS_SECURITY_2.ID }).ToArray();
            }
            SetApplicationCmd(deviceNumber, securityVersions.HasFlag(SecurityVersions.S0),
                securityVersions.HasFlag(SecurityVersions.S2), LibTypes.Controller);

            AddOrModifySecurityConfigured(deviceNumber, securityVersions, null, false, supportedCC);
        }

        public TasksAccessPoint SetupControllerDeviceConfigured(Alias deviceNumber, bool multiChannel, bool supervision, bool crc16, SecurityVersions securityVersions, 
            bool transportService, bool overwriteConfig = false)
        {
            Device controllerDevice = DeviceManager.GetDevice(deviceNumber);
            if (overwriteConfig)
            {
                DeviceManager.ConfigurationClear(deviceNumber);
            }

            // Prepare NIF
            var deviceOptions = DeviceOptions.Listening; // for CSC Role Type [RT:01.11.0004.1]
            // Generic Controller DT (ZWPv2) [DT:32.11.0001.1]
            byte genericType = (byte)GenericTypes.GENERIC_CONTROLLER;
            byte specificType = SpecificTypes.NOT_USED;

            // Prepare CC Support Data
            var assocSuppData = new AssociationSupportData()
            {
                AssociationGroups = new List<AssociationGroupEx>
                    {
                        new AssociationGroupEx(1, 5, "Lifeline", 0x0001,
                        new byte[] { COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID, COMMAND_CLASS_DEVICE_RESET_LOCALLY.DEVICE_RESET_LOCALLY_NOTIFICATION.ID }),
                    },
            };
            var zwpInfoSuppData = new ZWavePlusInfoSupportData()
            {
                RoleType = ZPIRoleTypes.ControllerCentralStatic,
                NodeType = ZPINodeTypes.ZWavePlusNode,
                InstallerIconType = new byte[] { 0x01, 0x00 },
                UserIconType = new byte[] { 0x01, 0x00 }
            };

            // Prepare Supported CCs
            var zwpInfoSupp = new ZWavePlusInfoSupport(zwpInfoSuppData)     { Version = 2, Secure = false };
            var agiSupp = new AssociationGroupInfoSupport(assocSuppData)    { Version = 3, Secure = true };
            var assocSupp = new AssociationSupport(assocSuppData)           { Version = 2, Secure = true };
            var mcaSupp = new MultiChannelAssociationSupport(assocSuppData) { Version = 3, Secure = true };
            var basicSupp = new BasicSupport()                              { Version = 2, Secure = true };
            var deviceResetLocallySupp = new DeviceResetLocallySupport()    { Version = 1, Secure = true };
            var manSpecSupp = new ManufacturerSpecificSupport()             { Version = 2, Secure = true };
            var fakeInclCtrlSupp = new GenericCcSupport(COMMAND_CLASS_INCLUSION_CONTROLLER.ID) { Version = 1, Secure = false };
            byte versionSuppVersion = 3;
            bool isVersionSuppSecure = true;

            // Setup Controller Device
            if (overwriteConfig || DeviceManager.ConfigurationIsEmpty(deviceNumber))
            {
                var supportedCCs = new List<CommandClassSupport>();
                supportedCCs.AddRange(new CommandClassSupport[]
                {
                    zwpInfoSupp,
                    agiSupp,
                    assocSupp,
                    //basicSupp, // Do not add Basic CC. [DT:32.11.0004.1]
                    deviceResetLocallySupp,
                    manSpecSupp,
                    fakeInclCtrlSupp,
                    new VersionSupport(supportedCCs)                        { Version = versionSuppVersion, Secure = isVersionSuppSecure },
                });
                if (multiChannel)
                {
                    supportedCCs.Add(mcaSupp);
                }

                SetupRootDevice(deviceNumber,
                    deviceOptions,
                    genericType,
                    specificType,
                    multiChannel,
                    supervision,
                    crc16,
                    securityVersions,
                    transportService,
                    supportedCCs);

                controllerDevice.Network.SetRoleType(ZWave.Enums.RoleTypes.CONTROLLER_CENTRAL_STATIC);
                controllerDevice.Network.SetNodeType(ZWave.Enums.NodeTypes.ZWAVEPLUS_NODE);
            }
            else
            {
                var configuration = DeviceManager.GetConfiguration(deviceNumber);

                var supportedCCs = new List<CommandClassSupport>();
                if (configuration.Network.HasCommandClass(COMMAND_CLASS_ZWAVEPLUS_INFO.ID))
                { 
                    supportedCCs.Add(zwpInfoSupp); 
                }
                if (configuration.Network.HasCommandClass(COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ID))
                { 
                    supportedCCs.Add(agiSupp); 
                }
                if (configuration.Network.HasCommandClass(COMMAND_CLASS_ASSOCIATION_V2.ID))
                { 
                    supportedCCs.Add(assocSupp); 
                }
                if (configuration.Network.HasCommandClass(COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID)
                    && multiChannel)
                {
                    supportedCCs.Add(mcaSupp);
                }
                if (configuration.Network.HasCommandClass(COMMAND_CLASS_BASIC.ID))
                {
                    supportedCCs.Add(basicSupp);
                }
                if (configuration.Network.HasCommandClass(COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID))
                { 
                    supportedCCs.Add(deviceResetLocallySupp); 
                }
                if (configuration.Network.HasCommandClass(COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID))
                { 
                    supportedCCs.Add(manSpecSupp); 
                }
                if (configuration.Network.HasCommandClass(COMMAND_CLASS_INCLUSION_CONTROLLER.ID))
                {
                    supportedCCs.Add(fakeInclCtrlSupp);
                }
                if (configuration.Network.HasCommandClass(COMMAND_CLASS_VERSION.ID))
                {
                    supportedCCs.Add(new VersionSupport(supportedCCs)           { Version = versionSuppVersion, Secure = isVersionSuppSecure }); 
                }

                if (securityVersions != SecurityVersions.None)
                {
                    SetupRootDevice(deviceNumber, 
                        deviceOptions,
                        genericType,
                        specificType,
                        multiChannel, 
                        supervision, 
                        configuration.Network.HasCommandClass(COMMAND_CLASS_CRC_16_ENCAP.ID), 
                        securityVersions, 
                        transportService, 
                        supportedCCs, 
                        configuration);
                }
                else
                {
                    SetupRootDevice(deviceNumber,
                        deviceOptions,
                        genericType,
                        specificType,
                        configuration.Network.HasCommandClass(COMMAND_CLASS_MULTI_CHANNEL_V2.ID),
                        configuration.Network.HasCommandClass(COMMAND_CLASS_SUPERVISION.ID),
                        configuration.Network.HasCommandClass(COMMAND_CLASS_CRC_16_ENCAP.ID),
                        SecurityVersions.None,
                        configuration.Network.HasCommandClass(COMMAND_CLASS_TRANSPORT_SERVICE.ID),
                        supportedCCs,
                        configuration);
                }
            }
            DeviceManager.ConfigurationUpdate(deviceNumber);
            return new TasksAccessPoint(controllerDevice);
        }

        public TasksAccessPoint SetupControllerDeviceConfigured(Alias deviceNumber)
        {
            return SetupControllerDeviceConfigured(deviceNumber, true, true, true, SecurityVersions.S0 | SecurityVersions.S2, true);
        }


        private readonly TransmitOptions TX_OPTIONS = TransmitOptions.Ack | TransmitOptions.AutoRoute | TransmitOptions.Explore;

        public TasksAccessPoint SetupControllerDeviceConfiguredAndWaitForDut(Alias deviceNumber, Node dutNode, RoleTypes roleType)
        {
            return SetupControllerDeviceConfiguredAndWaitForDut(deviceNumber, dutNode, roleType, null);
        }

        public TasksAccessPoint SetupControllerDeviceConfiguredAndWaitForDut(Alias deviceNumber, Node dutNode, RoleTypes roleType, byte? initS2SequenceNumber)
        {
            SendDataResult resNop = null;
            Device sender = DeviceManager.GetDevice(deviceNumber);
            bool sleepingDevice = roleType == RoleTypes.CONTROLLER_PORTABLE ||
                                    roleType == RoleTypes.CONTROLLER_PORTABLE_REPORTING ||
                                    roleType == RoleTypes.END_NODE_PORTABLE ||
                                    roleType == RoleTypes.END_NODE_SLEEPING_REPORTING;

            if (sleepingDevice)
            {
                // Sending the NOP before SetupControllerDeviceConfigured() keeps sleeping devices awake most of the times
                resNop = sender.SendDataEx(dutNode, new byte[] { COMMAND_CLASS_NO_OPERATION.ID }, (ZWave.Enums.TransmitOptions)TX_OPTIONS,
                    ZWave.Enums.SecuritySchemes.NONE);
            }

            var ret = SetupControllerDeviceConfigured(deviceNumber, true, true, true, SecurityVersions.S0 | SecurityVersions.S2, true);
            SecurityTestSettings.SetInitialS2SequenceNumber(deviceNumber, initS2SequenceNumber);

            if (sleepingDevice)
            {
                if (resNop.TransmitStatus == (ZWave.Enums.TransmitStatuses)TransmitStatuses.CompleteOk)
                {
                    // Final check if the DUT still awake before asking for a Wake Up Notification
                    resNop = sender.SendDataEx(dutNode, new byte[] { COMMAND_CLASS_NO_OPERATION.ID }, (ZWave.Enums.TransmitOptions)TX_OPTIONS, 
                        ZWave.Enums.SecuritySchemes.NONE);
                }

                if (resNop.TransmitStatus == (ZWave.Enums.TransmitStatuses)TransmitStatuses.CompleteNoAcknowledge)
                {
                    // if NOP was not acked 
                    Log(Environment.NewLine, LogLevels.Information);
                    Log("The DUT is not responding. Wake up the DUT!", LogLevels.Title);

                    ExpectDataResult result = sender.ExpectData(new COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_NOTIFICATION(), 600000); // 10 minutes
                    BasicResult.ExpectData wakeupNotification = new BasicActionResult.ExpectData(result);

                    Log(Environment.NewLine, LogLevels.Information);

                    if (result.IsStateCompleted)
                    {
                        Log("DUT is awake, continuing Test Case...", LogLevels.Information);
                    }
                    else
                    {
                        Log("No Wake Up Notification has been received!", LogLevels.Error);
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// Add Security Support
        /// </summary>
        /// <param name="deviceNumber">device alias</param>
        /// <param name="securityVersions">security version (S0, S2)</param>
        /// <param name="CommandClasseClasses">set the libtype to emulate</param>
        public void AddSecurity(Alias deviceNumber, SecurityVersions securityVersions, LibTypes CommandClasses)
        {
            var supportedCC = new byte[]
            {
                COMMAND_CLASS_BASIC.ID,
                COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID,
                COMMAND_CLASS_VERSION.ID,
                COMMAND_CLASS_POWERLEVEL.ID,
                COMMAND_CLASS_INCLUSION_CONTROLLER.ID
            };
            if (securityVersions.HasFlag(SecurityVersions.S0))
            {
                supportedCC = supportedCC.Union(new[] { COMMAND_CLASS_SECURITY.ID }).ToArray();
            }
            if (securityVersions.HasFlag(SecurityVersions.S2))
            {
                supportedCC = supportedCC.Union(new[] { COMMAND_CLASS_SECURITY_2.ID }).ToArray();
            }

            SetApplicationCmd(deviceNumber, securityVersions.HasFlag(SecurityVersions.S0),
                securityVersions.HasFlag(SecurityVersions.S2), CommandClasses);

            AddSecurity(deviceNumber, securityVersions, supportedCC);
        }

        /// <summary>
        /// Add Security Support (version S0)
        /// </summary>
        /// <param name="deviceNumber">device alias</param>
        /// <param name="supportedCC">list of securely supported command classes</param>
        public void AddSecurity(Alias deviceNumber, params byte[] supportedCC)
        {
            AddSecurity(deviceNumber, SecurityVersions.S0, supportedCC);
        }

        /// <summary>
        /// Add Security Support
        /// </summary>
        /// <param name="deviceNumber">device alias</param>
        /// <param name="securityVersions">security version (S0, S2)</param>
        /// <param name="supportedCC">list of securely supported command classes</param>
        public void AddSecurity(Alias deviceNumber, SecurityVersions securityVersions, params byte[] supportedCC)
        {
            AddOrModifySecurity(deviceNumber, securityVersions, false, supportedCC);
        }

        public void AddCrc16(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            var crc16 = new Crc16EncapManager(device.Network);
            device.SessionClient.AddSubstituteManager(crc16);
        }

        public ApplicationToken.Supervision AddSupervision(Alias deviceNumber)
        {
            ApplicationToken.Supervision ret = null;
            Device device = DeviceManager.GetDevice(deviceNumber);
            if (deviceNumber.EndPointId == 0)
            {
                var task = new SupervisionReportTask(device.Network, TXO);
                ret = new ApplicationToken.Supervision(task);
                var supervision = new SupervisionManager(device.Network, SupervisionManagerSendDataSubstitutionCallback);
                device.SessionClient.AddSubstituteManager(supervision, task);
            }
            else
            {
                var task = new SupervisionReportTask(device.Network, new NodeTag(0, deviceNumber.EndPointId), TXO);
                ret = new ApplicationToken.Supervision(task);
                device.SessionClient.ExecuteAsync(task);
            }
            return ret;
        }

        private bool SupervisionManagerSendDataSubstitutionCallback(NodeTag nodeId, byte[] data)
        {
            return true;
        }

        public void AddMultiChannel(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            var multiChannel = new MultiChannelManager(device.Network);
            device.SessionClient.AddSubstituteManager(multiChannel);
        }

        /// <summary>
        /// Adds Transport Service Support
        /// </summary>
        /// <param name="deviceNumber">Device alias</param>
        /// <param name="maxFrameSize">Maximum size of frame to be sent</param>
        public void AddTransportService(Alias deviceNumber, int maxFrameSize)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            device.MemoryGetId();
            device.Network.TransportServiceMaxSegmentSize = maxFrameSize;
            var transportService = new TransportServiceManager(device.Network,
                new TransportServiceManagerInfo(TXO,
                    TransportServiceManagerSendDataSubstitutionCallback)
                );
            device.SessionClient.AddSubstituteManager(transportService);
        }

        /// <summary>
        /// Adds Transport Service Support
        /// </summary>
        /// <param name="deviceNumber">Device alias</param>
        public const int MAX_FRAME_SIZE = 42;
        public int AddTransportService(Alias deviceNumber)
        {
            var maxPayloadSize = MAX_FRAME_SIZE;
            Device device = DeviceManager.GetDevice(deviceNumber);
            var res = device.GetMaxPayloadSize();
            if (res && res.MaxPayloadSize > 0)
            {
                maxPayloadSize = res.MaxPayloadSize;
            }
            AddTransportService(deviceNumber, maxPayloadSize);
            return maxPayloadSize;
        }

        private bool TransportServiceManagerSendDataSubstitutionCallback(NodeTag node)
        {
            return true;
        }

        /// <summary>
        /// Add Security Support (version S0)
        /// </summary>
        /// <param name="deviceNumber">device alias</param>
        /// <param name="checkIfSupportSecurityClass">check if joining node supports securuty (S0 or S2) command class</param>
        /// <param name="supportedCC">list of securely supported command classes</param>
        public void AddSecurity(Alias deviceNumber, bool checkIfSupportSecurityClass, params byte[] supportedCC)
        {
            AddOrModifySecurity(deviceNumber, SecurityVersions.S0, checkIfSupportSecurityClass, supportedCC);
        }

        /// <summary>
        /// Add Security Support
        /// </summary>
        /// <param name="deviceNumber">device alias</param>
        /// <param name="securityVersions">security version (S0, S2)</param>
        /// <param name="checkIfSupportSecurityClass">check if joining node supports securuty (S0 or S2) command class</param>
        /// <param name="supportedCC">list of securely supported command classes</param>
        public void AddOrModifySecurity(Alias deviceNumber, SecurityVersions securityVersions, bool checkIfSupportSecurityClass, params byte[] supportedCC)
        {
            AddOrModifySecurityConfigured(deviceNumber, securityVersions, null, checkIfSupportSecurityClass, supportedCC);
        }

        private void AddOrModifySecurityConfigured(Alias deviceNumber, SecurityVersions securityVersions, ZWave.Configuration.NetworkKey[] networkKeys, bool checkIfSupportSecurityClass, params byte[] supportedCC)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            SecurityManager securityManager = null;
            if (DeviceManager.IsSerialApiController(device.Library) || device.Library == ZWave.Enums.Libraries.EndDeviceSysTestLib)
            {
                if (device.ChipType != ZWave.Enums.ChipTypes.ZW070x)
                {
                    device.GetPRK();
                }
                securityManager = (SecurityManager)device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
                if (securityManager == null)
                {
                    securityManager = new SecurityManager(device.Network, networkKeys, device.PRK);
                    device.SessionClient.AddSubstituteManager(securityManager,
                        securityManager.CreateSecurityReportTask(),
                        securityManager.CreateSecurityS2ReportTask(),
                        securityManager.CreateInclusionControllerSupportTask(InclusionControllerUpdateCallback, null));
                }
            }

            device.MemoryGetId();
            var cmdClasses = device.Network.GetCommandClasses(device.Network.NodeTag);
            var newCmdClasses = (cmdClasses ?? new byte[0]).Union(supportedCC ?? new byte[0]);

            device.Network.SetCommandClasses(newCmdClasses.ToArray());
            device.Network.SetCommandClassesSecureVirtual(newCmdClasses.ToArray());

            device.Network.IsEnabledS0 = false;
            device.Network.IsEnabledS2_UNAUTHENTICATED = false;
            device.Network.IsEnabledS2_AUTHENTICATED = false;
            device.Network.IsEnabledS2_ACCESS = false;
            if (securityVersions == SecurityVersions.None)
            {
                device.Network.SetEnabledSecuritySchemes();
                DeviceManager.ConfigurationUpdate(deviceNumber);
                return;
            }
            else
            {
                if (securityVersions.HasFlag(SecurityVersions.S0))
                {
                    device.Network.IsEnabledS0 = true;
                }
                if (securityVersions.HasFlag(SecurityVersions.S2))
                {
                    device.Network.IsEnabledS2_UNAUTHENTICATED = true;
                    device.Network.IsEnabledS2_AUTHENTICATED = true;
                    device.Network.IsEnabledS2_ACCESS = true;
                }
                device.Network.SetEnabledSecuritySchemes();
                DeviceManager.ConfigurationUpdate(deviceNumber);
            }

            if (securityManager != null)
            {
                securityManager.SecurityManagerInfo.ActivateNetworkKeyS0();
                // ATTENTION! While use setting ='true' it is necessary needed to set application node information for each Test Case.
                securityManager.SecurityManagerInfo.CheckIfSupportSecurityCC = checkIfSupportSecurityClass;

                securityManager.SecurityManagerInfo.DSKNeededCallback = (nodeId, DSKFull) =>
                {
                    byte[] ret = null;
                    if (!IncludeSettings.CancelDSKInputDialog)
                    {
                        ret = IncludeSettings.JoinDSK;
                    }
                    Log("Join DSK: " + (ret != null ? $"{Utils.Tools.GetDskStringFromBytes(ret)}" : "cancelled"), LogLevels.Diagnostic);
                    return ret;
                };

                securityManager.SecurityManagerInfo.KEXSetConfirmCallback = (requestedSchemes, isClientSideAuthRequested) =>
                {
                    KEXSetConfirmResult ret = new KEXSetConfirmResult() { IsConfirmed = !IncludeSettings.CancelGrantedSchemes };
                    ret.IsAllowedCSA = isClientSideAuthRequested;
                    if (IncludeSettings.GrantedSchemes != null)
                    {
                        ret.GrantedSchemes = IncludeSettings.GrantedSchemes.Select(x => (ZWave.Enums.SecuritySchemes)(x)).ToList();
                    }
                    else
                    {
                        var supportedSchemes = requestedSchemes.
                            Where(x => x == ZWave.Enums.SecuritySchemes.S0 ? device.Network.HasSecurityScheme(ZWave.Enums.SecuritySchemes.S0) : device.Network.HasSecurityScheme(x)).
                            ToArray();
                        ret.GrantedSchemes = supportedSchemes.ToList();
                    }
                    Log("Grant schemes: [" + (ret.IsConfirmed
                        ? ret
                            .GrantedSchemes.Select(x => x.ToString())
                            .DefaultIfEmpty()
                            .Aggregate((x, y) => x + "," + y)
                        : "cancelled") + "]", LogLevels.Information);
                    return ret;
                };

                securityManager.SecurityManagerInfo.DSKVerificationOnReceiverCallback = () =>
                {
                    byte[] ret = null;
                    if (IncludeSettings.IsInteractiveMode)
                    {
                        DeviceManager.MessageBox.Show("Enter the displayed 10 digit PIN portion of DUT'S DSK value in the PC Controller dialog.",
                            "USER INSTRUCTION");
                        string dskStr = DeviceManager.MessageBox.GetUserInput();
                        int i = 0;
                        IncludeSettings.JoinCSA = new byte[4];
                        foreach (var item in dskStr.Split('-', ' '))
                        {
                            if (UInt16.TryParse(item, out UInt16 pinPart))
                            {
                                IncludeSettings.JoinCSA[i++] = (byte)(pinPart >> 8);
                                IncludeSettings.JoinCSA[i++] = (byte)pinPart;
                            }
                        }
                    }

                    if (!IncludeSettings.CancelDSKInputDialog)
                    {
                        ret = IncludeSettings.JoinCSA;
                    }
                    var part1 = IncludeSettings.JoinCSA?.Take(2).ToArray().ToNumber().ToString("D5");
                    var part2 = IncludeSettings.JoinCSA?.Skip(2).ToArray().ToNumber().ToString("D5");
                    Log("Join CSA: " + (ret != null
                        ? part1 + " " + part2
                        : "cancelled"), LogLevels.Information);
                    return ret;
                };

                // Store to dump file.
                var hid = securityManager.SecurityManagerInfo.Network.HomeId.ToHexString();
                securityManager.SecurityManagerInfo.NetworkKeyS0Changed +=
                    (key, isTest) => DeviceManager.WriteKeyToStorage(hid, key);
                securityManager.SecurityManagerInfo.NetworkKeyS2Changed +=
                    (peer, key, scheme, istemp, isTest) => DeviceManager.WriteKeyToStorage(hid, key, (byte)scheme);
            }
        }

        public void InclusionControllerUpdateCallback(ActionResult result)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                RequestNodeInfoResult requestResult = null;
                if (result is RequestNodeInfoResult)
                {
                    requestResult = (RequestNodeInfoResult)result;
                }
                else if (result is SetupNodeLifelineResult)
                {
                    requestResult = ((SetupNodeLifelineResult)result).NodeInfo.RequestNodeInfo;
                }
            });
        }

        public void SetSecurityForNode(Alias deviceNumber, Alias nodeNumber, bool isEnable)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            Device nodeDevice = DeviceManager.GetDevice(nodeNumber);
            nodeDevice.MemoryGetId();
            if (isEnable)
            {
                device.Network.SetSecuritySchemes(nodeDevice.Network.NodeTag, new[] { ZWave.Enums.SecuritySchemes.S0 });
            }
            else
            {
                device.Network.ResetSecuritySchemes(nodeDevice.Network.NodeTag);
            }
        }

        public void SetSecurityVersionForNode(Alias deviceNumber, Alias nodeNumber, SecurityVersions securityVersion)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            Device node = DeviceManager.GetDevice(nodeNumber);
            node.MemoryGetId();
            device.MemoryGetId();
            device.Network.ResetSecuritySchemes(device.Network.NodeTag);
            if (securityVersion.HasFlag(SecurityVersions.S0))
            {
                device.Network.SetSecuritySchemes(device.Id, new[] { ZWave.Enums.SecuritySchemes.S0 });
            }
            if (securityVersion.HasFlag(SecurityVersions.S2))
            {
                device.Network.SetSecuritySchemes(device.Id, ZWave.Enums.SecuritySchemeSet.ALLS2);
            }
        }

        public void ResetSecurity(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            device.MemoryGetId();
        }

        public void RemoveSecurity(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            //device.SessionClient.AddSubstituteManager(null);
            device.SessionClient.ClearSubstituteManagers();
        }

        public BasicToken.Void AddBasic(Alias deviceNumber)
        {
            BasicToken.Void ret = null;
            Device device = DeviceManager.GetDevice(deviceNumber);
            if (device is BridgeController)
            {
                COMMAND_CLASS_BASIC.BASIC_REPORT report = new COMMAND_CLASS_BASIC.BASIC_REPORT();
                ret = new BasicActionToken.Void(device.ResponseData(new Node(0, deviceNumber.EndPointId), report, TXO, new COMMAND_CLASS_BASIC.BASIC_GET()));
            }
            else
            {
                COMMAND_CLASS_BASIC.BASIC_REPORT report = new COMMAND_CLASS_BASIC.BASIC_REPORT();
                ret = new BasicActionToken.Void(device.ResponseData(new Node(0, deviceNumber.EndPointId), report, TXO, new COMMAND_CLASS_BASIC.BASIC_GET()));
            }
            return ret;
        }

        public BasicToken.Void AddBasicEx(Alias deviceNumber)
        {
            BasicToken.Void ret = null;
            Device device = DeviceManager.GetDevice(deviceNumber);
            if (device is BridgeController)
            {
                //device.ExecuteAsync(new VirtualBasicTask(TXO), null);
            }
            else
            {
                COMMAND_CLASS_BASIC.BASIC_REPORT report = new COMMAND_CLASS_BASIC.BASIC_REPORT();
                ret = new BasicActionToken.Void(device.ResponseDataEx(new Node(0, deviceNumber.EndPointId), report, TXO, TXSO, TXO2, new COMMAND_CLASS_BASIC.BASIC_GET()));
            }
            return ret;
        }

        public BasicToken.Void AddBasicEx(Alias deviceNumber, SecuritySchemes scheme)
        {
            BasicToken.Void ret = null;
            Device device = DeviceManager.GetDevice(deviceNumber);
            if (device is BridgeController)
            {
                //device.ExecuteAsync(new VirtualBasicTask(TXO), null);
            }
            else
            {
                COMMAND_CLASS_BASIC.BASIC_REPORT report = new COMMAND_CLASS_BASIC.BASIC_REPORT();
                ret = new BasicActionToken.Void(device.ResponseDataEx(new Node(0, deviceNumber.EndPointId), report, TXO, TXSO, (ZWave.Enums.SecuritySchemes)scheme, TXO2, new COMMAND_CLASS_BASIC.BASIC_GET()));
            }
            return ret;
        }

        public BasicToken.Void AddBasicSupport(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            device.MemoryGetId();
            return new BasicActionToken.Void(device.SessionClient.ExecuteAsync(new ZWave.BasicApplication.CommandClasses.BasicSupport(device.Network, TXO)));
        }

        public void AddCommandClassSupport(Alias deviceNumber, byte[] ccIds)
        {
            foreach (var ccSupport in CommandClassesSupportHandler.GetDefaultCommandClassSupportsForCCIds(ccIds.ToList()))
            {
                AddCommandClassSupport(deviceNumber, 0, ccSupport, false);
            }
            // Can't use Version from Cert because we have multiple places of register Versions responses.
            //AddOrUpdateVersionCommandClassSupport(deviceNumber, 0);
        }

        public void AddCommandClassSupport(Alias deviceNumber, byte endPointId, CommandClassSupport ccSupport, bool updateVersionCC = true)
        {
            DeviceManager.AddCommandClassSupport(deviceNumber, endPointId, ccSupport, updateVersionCC);
        }

        public void AddOrUpdateVersionCommandClassSupport(Alias deviceNumber, byte endPointId)
        {
            DeviceManager.AddOrUpdateVersionCommandClassSupportForDevice(deviceNumber, endPointId);
        }

        //public void AddMultiChannelSupport(Alias deviceNumber)
        //{
        //    Device device = DeviceManager.GetDevice(deviceNumber);
        //    device.MemoryGetId();
        //    device.SessionClient.ExecuteAsync(new MultiChannelSupport(device.Network, TXO));
        //}

        public BasicToken.Void AddMultiChannelSupport(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            device.MemoryGetId();
            return new BasicActionToken.Void(device.SessionClient.ExecuteAsync(
                new ZWave.BasicApplication.CommandClasses.MultiChannelSupport(device.Network, TXO)));
        }

        public BasicToken.Void AddMultiChannelSupport(Alias deviceNumber, byte[] EndPointCcs)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            device.MemoryGetId();
            return new BasicActionToken.Void(device.SessionClient.ExecuteAsync(
                new ZWave.BasicApplication.CommandClasses.MultiChannelSupport (device.Network, TXO, EndPointCcs)));
        }

        public void AddAssociationSupport(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            device.MemoryGetId();
            device.SessionClient.ExecuteAsync(new ZWave.BasicApplication.CommandClasses.AssociationSupport(device.Network, TXO));
            device.SessionClient.ExecuteAsync(new ZWave.BasicApplication.CommandClasses.AssociationGroupInfoSupport(device.Network, TXO));
        }

        public void AddManufacturerSpecific(Alias deviceNumber, ushort manufacturerId, ushort productId, ushort productTypeId)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            COMMAND_CLASS_MANUFACTURER_SPECIFIC.MANUFACTURER_SPECIFIC_REPORT report = new COMMAND_CLASS_MANUFACTURER_SPECIFIC.MANUFACTURER_SPECIFIC_REPORT();
            report.manufacturerId = new byte[] { (byte)(manufacturerId >> 8), (byte)manufacturerId };
            report.productId = new byte[] { (byte)(productId >> 8), (byte)productId };
            report.productTypeId = new byte[] { (byte)(productTypeId >> 8), (byte)productTypeId };
            device.ResponseDataEx(new Node(0, deviceNumber.EndPointId), report, TXO, new COMMAND_CLASS_MANUFACTURER_SPECIFIC.MANUFACTURER_SPECIFIC_GET());
        }

        public void AddManufacturerSpecific_V2(Alias deviceNumber, ushort manufacturerId, ushort productId, ushort productTypeId, byte deviceIdType, byte deviceIdDataFormat, byte[] deviceId)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.MANUFACTURER_SPECIFIC_REPORT report = new COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.MANUFACTURER_SPECIFIC_REPORT();
            report.manufacturerId = new byte[] { (byte)(manufacturerId >> 8), (byte)manufacturerId };
            report.productId = new byte[] { (byte)(productId >> 8), (byte)productId };
            report.productTypeId = new byte[] { (byte)(productTypeId >> 8), (byte)productTypeId };
            device.ResponseDataEx(new Node(0, deviceNumber.EndPointId), report, TXO, new COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.MANUFACTURER_SPECIFIC_GET());
        }

        public void StartTrafficToNodeIdTest(Alias deviceNumber, Node nodeId, byte[] cmd, byte[] expectedCmd, int intervalMs, int timeoutMs)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionToken token;
            if (expectedCmd == null)
            {
                token = device.NoiseData(new NodeTag[] { nodeId }, cmd, TXO, null, intervalMs, timeoutMs);
            }
            else
            {
                token = device.NoiseData(new NodeTag[] { nodeId }, cmd, TXO, expectedCmd, null, intervalMs, timeoutMs);
            }
            int key = (deviceNumber.Value << 24) + nodeId.GetHashCode();
            if (testResults.ContainsKey(key))
            {
                testResults[key].SetCompleted();
                testResults.Remove(key);
            }
            testResults.Add(key, token);
        }

        public void StartTrafficFromEndDeviceToNodeIdTest(Alias deviceNumber, Node node, byte[] cmd, byte[] expectedCmd, int intervalMs, int timeoutMs, SecuritySchemes securityScheme)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionToken token = device.NoiseDataEx(new NodeTag[] { node }, cmd, TXO, expectedCmd, intervalMs, timeoutMs, (ZWave.Enums.SecuritySchemes)securityScheme, (ZWave.Enums.TransmitSecurityOptions)TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY, TXO2);
            int key = (deviceNumber.Value << 24) + node.GetHashCode();
            if (testResults.ContainsKey(key))
            {
                testResults[key].SetCompleted();
                testResults.Remove(key);
            }
            testResults.Add(key, token);
        }

        public void StartTrafficToNodeIdTest(Alias deviceNumber, Node node, byte[] cmd, int intervalMs)
        {
            StartTrafficToNodeIdTest(deviceNumber, node, cmd, null, intervalMs, 0);
        }

        public void StartTrafficToNodeIdTest(Alias deviceNumber, Node node, byte[] cmd, byte[] expectedCmd, int intervalMs, int timeoutMs, bool isSetSpanFree)
        {
            if (isSetSpanFree)
                SecurityTestSettings.FreeSpanS2ForNode(deviceNumber, node);

            StartTrafficToNodeIdTest(deviceNumber, node, cmd, expectedCmd, intervalMs, timeoutMs);
        }

        public BasicResult.Test GetTrafficTestResult(Alias deviceNumber, Node node)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            int key = (deviceNumber.Value << 24) + node.GetHashCode();
            BasicResult.Test ret = new BasicResult.Test();
            if (testResults.ContainsKey(key))
            {
                NoiseDataResult res = (NoiseDataResult)testResults[key].Result;
                ret.Total = res.TotalCount;
                ret.Fails = res.FailCount;
            }
            return ret;
        }

        public BasicResult.Test StopTrafficTest(Alias deviceNumber, Node node)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            int key = (deviceNumber.Value << 24) + node.GetHashCode();
            BasicResult.Test ret = new BasicResult.Test();
            if (testResults.ContainsKey(key))
            {
                testResults[key].SetCompleted();
                testResults[key].SetCompletedSignal();
                NoiseDataResult res = (NoiseDataResult)testResults[key].WaitCompletedSignal();
                ret.Total = res.TotalCount;
                ret.Fails = res.FailCount;
            }
            return ret;
        }

        public string GetFile(Alias deviceNumber, DevKits devKit, params string[] regexFilters)
        {
            return GetFile(deviceNumber, null, devKit, regexFilters);
        }

        public string GetFile(Alias deviceNumber, string ending, DevKits devKit, params string[] regexFilters)
        {
            var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
            var frequency = deviceHostClient.Frequency;
            var lib = deviceHostClient.Library;
            var chipSeries = deviceHostClient.Host.ChipSeries;
            if (string.IsNullOrWhiteSpace(chipSeries)
                || chipSeries == ChipSeries.ZW070x.ToString()
                || chipSeries == ChipSeries.ZW0713.ToString()
                || chipSeries == ChipSeries.ZW0714.ToString()
                || chipSeries == ChipSeries.ZW080x.ToString()
                || chipSeries == ChipSeries.Default.ToString()
                || chipSeries == ChipSeries.Any.ToString())
            {
                throw new ArgumentException("invalid chip series");
            }
            else
            {
                var chs = (ChipSeries)Enum.Parse(typeof(ChipSeries), chipSeries);
                return DeviceManager.GetLatestFilePath(new[] { lib }, chs, frequency, ending, devKit, regexFilters);
            }
        }

        public string GetFile(LibTypes library, ChipSeries chipSeries, DevKits devKit, params string[] regexFilters)
        {
            string frequency = null;
            if (DeviceManager.Devices != null && DeviceManager.Devices.Count > 0)
            {
                frequency = DeviceManager.Devices.First().Value.Frequency;
            }
            return GetFile(library, chipSeries, frequency, devKit, regexFilters);
        }

        public string GetFile(LibTypes library, ChipSeries chipSeries, string frequency, DevKits devKit, params string[] regexFilters)
        {
            ZWave.Enums.Libraries lib = GetZWaveLibrary(library);
            return DeviceManager.FileSystemManager.GetLatestFilePath(lib, chipSeries.ToString(), frequency, devKit, regexFilters);
        }

        private ZWave.Enums.Libraries GetZWaveLibrary(LibTypes library)
        {
            var ret = ZWave.Enums.Libraries.NoLib;
            switch (library)
            {
                case LibTypes.EndDeviceAny:
                    ret = ZWave.Enums.Libraries.EndDeviceLib;
                    break;
                case LibTypes.Controller:
                    ret = ZWave.Enums.Libraries.ControllerStaticLib;
                    break;
                case LibTypes.Sample:
                    ret = ZWave.Enums.Libraries.SwitchOnOff;
                    break;
                default:
                    ret = (ZWave.Enums.Libraries)library;
                    break;
            }
            return ret;
        }

        public byte[] GetSupportedCommandClasses(Alias deviceNumber)
        {
            var device = DeviceManager.GetDevice(deviceNumber);
            return device.Network.GetCommandClasses();
        }

        public void EnableNetworkKey(Alias deviceNumber, IEnumerable<SecuritySchemes> securitySchemes)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            device.Network.IsEnabledS0 = securitySchemes != null && securitySchemes.Contains(SecuritySchemes.S0);
            device.Network.IsEnabledS2_ACCESS = securitySchemes != null && securitySchemes.Contains(SecuritySchemes.S2_ACCESS);
            device.Network.IsEnabledS2_AUTHENTICATED = securitySchemes != null && securitySchemes.Contains(SecuritySchemes.S2_AUTHENTICATED);
            device.Network.IsEnabledS2_UNAUTHENTICATED = securitySchemes != null && securitySchemes.Contains(SecuritySchemes.S2_UNAUTHENTICATED);
            device.Network.ResetAndEnableAndSelfRestore();
        }

        public void EnableNetworkKey(Alias deviceNumber, SecuritySchemes securityScheme, bool isEnable)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);

            switch (securityScheme)
            {
                case SecuritySchemes.S0:
                    device.Network.IsEnabledS0 = isEnable;
                    break;
                case SecuritySchemes.S2_ACCESS:
                    device.Network.IsEnabledS2_ACCESS = isEnable;
                    break;
                case SecuritySchemes.S2_AUTHENTICATED:
                    device.Network.IsEnabledS2_AUTHENTICATED = isEnable;
                    break;
                case SecuritySchemes.S2_UNAUTHENTICATED:
                    device.Network.IsEnabledS2_UNAUTHENTICATED = isEnable;
                    break;
                case SecuritySchemes.NONE:
                case SecuritySchemes.S2_TEMP:
                default:
                    break;
            }
            device.Network.ResetAndEnableAndSelfRestore();
        }

        public void EnableCSA(Alias deviceNumber, bool isEnable)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            SecurityManager sm = (SecurityManager)device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                sm.SecurityManagerInfo.TestEnableClientSideAuthS2 = isEnable;
            }
        }

        public void SaveNetworkKeysToFile(Alias deviceNumber, string networkKeysFilePath)
        {
            string extension = ".txt";
            char separator = ';';
            var lines = new List<string>();
            Device device = DeviceManager.GetDevice(deviceNumber);
            device.GetPRK();
            SecurityManager sm = new SecurityManager(device.Network, null, device.PRK);
            ZWave.Enums.SecuritySchemes[] usedFlags = new ZWave.Enums.SecuritySchemes[]
            {
                ZWave.Enums.SecuritySchemes.S0,
                ZWave.Enums.SecuritySchemes.S2_UNAUTHENTICATED,
                ZWave.Enums.SecuritySchemes.S2_AUTHENTICATED,
                ZWave.Enums.SecuritySchemes.S2_ACCESS
            };
            for (int i = 0; i < usedFlags.Length; i++)
            {
                var netKey = string.Concat(sm.SecurityManagerInfo.GetActualNetworkKey(usedFlags[i], false).
                                            Select(x => x.ToString("x2").ToUpper()).ToArray());
                string securityId = COMMAND_CLASS_SECURITY_2.ID.ToString("X2");
                if (usedFlags[i] == ZWave.Enums.SecuritySchemes.S0)
                {
                    securityId = COMMAND_CLASS_SECURITY.ID.ToString("X2");
                }
                lines.Add(string.Concat(securityId, separator, netKey, separator, 1));

                if (usedFlags[i] == ZWave.Enums.SecuritySchemes.S2_ACCESS || usedFlags[i] == ZWave.Enums.SecuritySchemes.S2_AUTHENTICATED)
                {
                    var netKeyLR = string.Concat(sm.SecurityManagerInfo.GetActualNetworkKey(usedFlags[i], true).
                                            Select(x => x.ToString("x2").ToUpper()).ToArray());
                    lines.Add(string.Concat(securityId, separator, netKeyLR, separator, 1));
                }
            }
            var fileName = Path.Combine(networkKeysFilePath, device.HomeId.ToHexString().Replace(" ", "") + extension);
            File.WriteAllLines(fileName, lines.ToArray());
        }

        public byte[] DecryptS2(Alias deviceNumber, byte senderId, byte receiverId, byte[] homeId, byte sequenceNumber, byte[] receiverNonce, byte[] senderNonce, byte[] securityKey, int generationCount, bool isRealKey, byte[] fullMessageS2, out int currentGenerationCount)
        {
            return SecurityS2CryptoProvider.DecryptPayload(new NodeTag(senderId), new NodeTag(receiverId), homeId, sequenceNumber, receiverNonce,
                senderNonce, securityKey, generationCount, isRealKey, fullMessageS2, out currentGenerationCount);
        }

        public byte[] EncryptS2(Alias deviceNumber, byte senderId, byte receiverId, byte[] homeId, byte sequenceNumber, byte[] receiverNonce, byte[] senderNonce, byte[] securityKey, byte[] textToEncrypt, int generationCount, bool isRealKey)
        {
            return SecurityS2CryptoProvider.EncryptPayload(new NodeTag(senderId), new NodeTag(receiverId), homeId, sequenceNumber, receiverNonce,
                senderNonce, securityKey, textToEncrypt, generationCount, isRealKey);
        }

        public BasicToken.Void[] SetApplicationCmdWithExtraClasses(Alias deviceNumber, bool IsS0, bool IsS2, LibTypes Library, byte[] extraClasses)
        {
            return SetApplicationCmd(deviceNumber, IsS0, IsS2, Library, null, 0x02, extraClasses);
        }

        public BasicToken.Void[] SetApplicationCmd(Alias deviceNumber, bool IsS0, bool IsS2, LibTypes Library)
        {
            return SetApplicationCmd(deviceNumber, IsS0, IsS2, Library, null, 0x02, null);
        }

        public BasicToken.Void[] SetApplicationCmd(Alias deviceNumber, bool IsS0, bool IsS2, LibTypes Library, byte[] wakeUpInterval)
        {
            return SetApplicationCmd(deviceNumber, IsS0, IsS2, Library, wakeUpInterval, 0x02, null);
        }

        public BasicToken.Void[] SetApplicationCmd(Alias deviceNumber, bool IsS0, bool IsS2, LibTypes Library, byte zwavePlusVersion)
        {
            return SetApplicationCmd(deviceNumber, IsS0, IsS2, Library, null, zwavePlusVersion, null);
        }

        /// <summary>
        /// Set Application supported Command Classes and responses
        /// </summary>
        /// <param name="IsS0">Is S0 Supported?</param>
        /// <param name="IsS2s">Is S2 Supported?</param>
        /// <param name="Library">Use the LibTypes Enumerate to choose which application is needed. Select Controller lib to simulate a controller</param>
        /// <param name="WakeUpInterval">Wake up interval used in case the device were a sleeping node</param>
        public BasicToken.Void[] SetApplicationCmd(Alias deviceNumber, bool IsS0, bool IsS2, LibTypes Library, byte[] wakeUpInterval, byte zwavePlusVersion, byte[] extraClasses)
        {
            SecurityTestSettings.SetInclusionFrameDelayS2(deviceNumber, SecurityS2TestFrames.KEXSet, 10000);

            List<byte> StandardClasses = new List<byte>() { COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID,
                        COMMAND_CLASS_SUPERVISION.ID, COMMAND_CLASS_TRANSPORT_SERVICE_V2.ID, COMMAND_CLASS_ZWAVEPLUS_INFO.ID };
            List<byte> SecuredClasses = new List<byte>() { COMMAND_CLASS_ASSOCIATION.ID, COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ID, COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID,
                COMMAND_CLASS_POWERLEVEL.ID, COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID, COMMAND_CLASS_CONFIGURATION_V4.ID,
                COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_INDICATOR_V4.ID};
            List<byte> SpecificCc = new List<byte>();
            List<byte> FullCc = new List<byte>();

            if (extraClasses != null)
            {
                SecuredClasses.AddRange(extraClasses);
            }

            if (IsS0)
            {
                StandardClasses.Add(COMMAND_CLASS_SECURITY.ID);
            }
            if (IsS2)
            {
                StandardClasses.Add(COMMAND_CLASS_SECURITY_2.ID);
            }

            (var generic, var specific, var devOpt) = GetDeviceDataByLibType(Library, SpecificCc, StandardClasses);

            SecuredClasses.AddRange(SpecificCc);

            FullCc.AddRange(StandardClasses);
            FullCc.AddRange(SecuredClasses);
            FullCc = FullCc.Distinct().ToList();

            Device device = DeviceManager.GetDevice(deviceNumber);

            ActionResult AppNodeInfo = device.ApplicationNodeInformation(devOpt, (byte)generic, specific, FullCc.ToArray());
            ActionResult AppNodeInfoCmd = device.ApplicationNodeInformationCmdClasses(FullCc.ToArray(),
                StandardClasses.ToArray(), SecuredClasses.ToArray());

            device.Network.SetApplicationNodeInformation(devOpt, (byte)generic, specific, FullCc.ToArray());
            device.Network.SetSecureCommandClasses(new NodeTag(device.Id), SecuredClasses.ToArray());

            return SetApplicationResponse(deviceNumber, Library, FullCc.ToArray(), wakeUpInterval, zwavePlusVersion);
        }

        private (GenericTypes generic, byte specific, ZWave.Enums.DeviceOptions devOpt) GetDeviceDataByLibType(LibTypes library, List<byte> specificCC, List<byte> standardCC)
        {
            GenericTypes generic = GenericTypes.GENERIC_CONTROLLER;
            byte specific = 0x00;
            ZWave.Enums.DeviceOptions devOpt = ZWave.Enums.DeviceOptions.Listening;

            switch (library)
            {
                case LibTypes.SwitchOnOff:
                    specificCC.AddRange(new byte[] { COMMAND_CLASS_SWITCH_ALL.ID, COMMAND_CLASS_SWITCH_BINARY_V2.ID });
                    generic = GenericTypes.SWITCH_BINARY;
                    specific = 0x01; //SPECIFIC_TYPE_POWER_SWITCH_BINARY
                    break;
                case LibTypes.MultilevelSwitch:
                    specificCC.AddRange(new byte[] { COMMAND_CLASS_SWITCH_ALL.ID, COMMAND_CLASS_SWITCH_BINARY_V2.ID,
                        COMMAND_CLASS_SWITCH_MULTILEVEL_V4.ID });
                    generic = GenericTypes.SWITCH_MULTILEVEL;
                    specific = 0x01; //SPECIFIC_TYPE_POWER_SWITCH_BINARY
                    break;
                case LibTypes.SensorPIR:
                    standardCC.Add(COMMAND_CLASS_MULTI_CMD.ID);
                    specificCC.AddRange(new byte[] { COMMAND_CLASS_WAKE_UP_V2.ID, COMMAND_CLASS_BATTERY.ID, COMMAND_CLASS_NOTIFICATION_V9.ID });
                    generic = GenericTypes.SENSOR_NOTIFICATION;
                    specific = 0x01; //SPECIFIC_TYPE_NOTIFICATION_SENSOR
                    devOpt = ZWave.Enums.DeviceOptions.NoneListening | ZWave.Enums.DeviceOptions.OptionalFunctionality;
                    break;
                case LibTypes.MultilevelSensor:
                    standardCC.Add(COMMAND_CLASS_MULTI_CMD.ID);
                    specificCC.AddRange(new byte[] { COMMAND_CLASS_WAKE_UP_V2.ID, COMMAND_CLASS_BATTERY.ID,
                        COMMAND_CLASS_SENSOR_BINARY_V2.ID, COMMAND_CLASS_ALARM.ID, COMMAND_CLASS_SENSOR_MULTILEVEL_V10.ID});
                    generic = GenericTypes.SENSOR_NOTIFICATION;
                    specific = 0x01;
                    devOpt = ZWave.Enums.DeviceOptions.NoneListening | ZWave.Enums.DeviceOptions.OptionalFunctionality;
                    break;
                case LibTypes.DoorLockKeyPad:
                    specificCC.AddRange(new byte[] { COMMAND_CLASS_DOOR_LOCK_V3.ID, COMMAND_CLASS_BATTERY.ID, COMMAND_CLASS_USER_CODE.ID, COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID });
                    generic = GenericTypes.ENTRY_CONTROL;
                    specific = 0x03; //SPECIFIC_TYPE_SECURE_KEYPAD_DOOR_LOCK
                    devOpt = ZWave.Enums.DeviceOptions.FreqListeningMode1000ms;
                    break;
                case LibTypes.LEDBulb: case LibTypes.LEDBulb_v4:
                    specificCC.AddRange(new byte[] { COMMAND_CLASS_SWITCH_COLOR_V3.ID, COMMAND_CLASS_SWITCH_MULTILEVEL_V4.ID, COMMAND_CLASS_SWITCH_BINARY_V2.ID });
                    generic = GenericTypes.SWITCH_MULTILEVEL;
                    specific = 0x02;
                    break;
                case LibTypes.LEDBulb_v1:
                    specificCC.AddRange(new byte[] { COMMAND_CLASS_SWITCH_COLOR_V3.ID, COMMAND_CLASS_SWITCH_MULTILEVEL_V4.ID });
                    generic = GenericTypes.SWITCH_MULTILEVEL;
                    specific = 0x02;
                    break;
                case LibTypes.LEDBulb_v2:
                    specificCC.AddRange(new byte[] { COMMAND_CLASS_SWITCH_COLOR_V3.ID, COMMAND_CLASS_SWITCH_BINARY_V2.ID });
                    generic = GenericTypes.SWITCH_BINARY;
                    specific = 0x02;
                    break;
                case LibTypes.LEDBulb_v3:
                    specificCC.AddRange(new byte[] { COMMAND_CLASS_SWITCH_COLOR_V3.ID });
                    generic = GenericTypes.SWITCH_BINARY;
                    specific = 0x02;
                    break;
                case LibTypes.WallController:
                    specificCC.AddRange(new byte[] { COMMAND_CLASS_MULTI_CHANNEL_V4.ID, COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID, COMMAND_CLASS_SWITCH_MULTILEVEL_V4.ID });
                    generic = GenericTypes.WALL_CONTROLLER;
                    specific = 0x01; //SPECIFIC_TYPE_BASIC_WALL_CONTROLLER
                    break;
                case LibTypes.PowerStrip:
                    specificCC.AddRange(new byte[] { COMMAND_CLASS_MULTI_CHANNEL_V4.ID, COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID,
                    COMMAND_CLASS_SWITCH_MULTILEVEL_V4.ID});
                    generic = GenericTypes.SWITCH_BINARY;
                    specific = 0x04; //SPECIFIC_TYPE_POWER_STRIP
                    break;
                case LibTypes.Controller:
                case LibTypes.ControllerBridgeLib:
                    specificCC.AddRange(new byte[] { COMMAND_CLASS_INCLUSION_CONTROLLER.ID, COMMAND_CLASS_CRC_16_ENCAP.ID });
                    generic = GenericTypes.STATIC_CONTROLLER;
                    specific = 0x01; //SPECIFIC_TYPE_PC_CONTROLLER
                    break;
                case LibTypes.ControllerPortableLib:
                    specificCC.AddRange(new byte[] { COMMAND_CLASS_WAKE_UP_V2.ID, COMMAND_CLASS_BATTERY.ID });
                    generic = GenericTypes.GENERIC_CONTROLLER;
                    specific = 0x01; //SPECIFIC_TYPE_PORTABLE_REMOTE_CONTROLLER
                    devOpt = ZWave.Enums.DeviceOptions.NoneListening;
                    break;
                case LibTypes.Thermostat:
                    specificCC.AddRange(new byte[] { COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID,
                    COMMAND_CLASS_SWITCH_MULTILEVEL_V4.ID, COMMAND_CLASS_THERMOSTAT_MODE_V3.ID, COMMAND_CLASS_THERMOSTAT_SETPOINT_V3.ID});
                    generic = GenericTypes.SWITCH_BINARY;
                    specific = 0x01; //SPECIFIC_TYPE_POWER_SWITCH_BINARY
                    break;
                case LibTypes.KeyFob: // imptlement if needed or remove it.
                    specificCC.AddRange(new byte[] { COMMAND_CLASS_BASIC.ID });
                    generic = GenericTypes.GENERIC_CONTROLLER; //GENERIC_TYPE_GENERIC_CONTROLLER
                    specific = 0x01; //SPECIFIC_TYPE_PORTABLE_REMOTE_CONTROLLER
                    break;
                case LibTypes.Meter:
                    specificCC.AddRange(new byte[] { COMMAND_CLASS_METER_V6.ID });
                    generic = GenericTypes.METER;
                    specific = 0x01; //SPECIFIC_TYPE_PORTABLE_REMOTE_CONTROLLER
                    break;
            }
            return (generic, specific, devOpt);
        }

        public BasicToken.Void[] SetApplicationQuirk(Alias deviceNumber, bool isS0, bool isS2, QuirckLibrary library)
        {
            return SetApplicationQuirk(deviceNumber, isS0, isS2, library, null, 0x02, null);
        }
        public BasicToken.Void[] SetApplicationQuirk(Alias deviceNumber, bool isS0, bool isS2, QuirckLibrary library, byte[] wakeUpInterval, byte zwavePlusVersion, byte[] extraClasses)
        {

            SecurityTestSettings.SetInclusionFrameDelayS2(deviceNumber, SecurityS2TestFrames.KEXSet, 10000);

            Dictionary<String, List<byte>> deviceConfiguration = new QuirkLibraryCommands().GetSupportedCommands(library);

            byte generic = deviceConfiguration["Generic Device Class"][0];
            byte specific = deviceConfiguration["Specific Device Class"][0];
            List<byte> standardClasses = deviceConfiguration["Standard Command Classes"];
            List<byte> securedClasses = deviceConfiguration["Secured Command Classes"];

            if (isS0)
            {
                standardClasses.Add(COMMAND_CLASS_SECURITY.ID);
            }
            if (isS2)
            {
                standardClasses.Add(COMMAND_CLASS_SECURITY_2.ID);
            }

            List<byte> FullCc = new List<byte>();
            FullCc.AddRange(standardClasses);
            FullCc.AddRange(securedClasses);
            FullCc = FullCc.Distinct().ToList();

            ZWave.Enums.DeviceOptions devOpt = ZWave.Enums.DeviceOptions.NoneListening;

            Device device = DeviceManager.GetDevice(deviceNumber);

            ActionResult appNodeInfo = device.ApplicationNodeInformation(devOpt, generic, specific, FullCc.ToArray());
            ActionResult AppNodeInfoCmd = device.ApplicationNodeInformationCmdClasses(FullCc.ToArray(),
                standardClasses.ToArray(), securedClasses.ToArray());

            device.Network.SetApplicationNodeInformation(devOpt, generic, specific, FullCc.ToArray());
            device.Network.SetSecureCommandClasses(new NodeTag(device.Id), securedClasses.ToArray());


            return SetApplicationQuirkResponse(deviceNumber, library, FullCc.ToArray(), wakeUpInterval, zwavePlusVersion);

        }

        public void SetApplicationCmdNoResponse(Alias deviceNumber, bool IsS0, bool IsS2, LibTypes Library)
        {
            SecurityTestSettings.SetInclusionFrameDelayS2(deviceNumber, SecurityS2TestFrames.KEXSet, 10000);

            List<byte> StandardClasses = new List<byte>() { COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID,
                        COMMAND_CLASS_SUPERVISION.ID, COMMAND_CLASS_TRANSPORT_SERVICE_V2.ID, COMMAND_CLASS_ZWAVEPLUS_INFO.ID };
            List<byte> SecuredClasses = new List<byte>() { COMMAND_CLASS_ASSOCIATION.ID, COMMAND_CLASS_POWERLEVEL.ID, COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID,
            COMMAND_CLASS_POWERLEVEL.ID, COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_BASIC.ID};
            List<byte> SpecificCc = new List<byte>();

            if (IsS0)
            {
                StandardClasses.Add(COMMAND_CLASS_SECURITY.ID);
            }
            if (IsS2)
            {
                StandardClasses.Add(COMMAND_CLASS_SECURITY_2.ID);
            }

            byte generic = 0;
            byte specific = 0;
            ZWave.Enums.DeviceOptions devOpt = ZWave.Enums.DeviceOptions.Listening;

            switch (Library)
            {
                case LibTypes.SwitchOnOff:
                    SpecificCc.AddRange(new byte[] { COMMAND_CLASS_SWITCH_ALL.ID, COMMAND_CLASS_SWITCH_BINARY_V2.ID, COMMAND_CLASS_ASSOCIATION_GRP_INFO.ID, });
                    generic = 0x10;
                    specific = 0x01;
                    break;
                case LibTypes.SensorPIR:
                    StandardClasses.Add(COMMAND_CLASS_MULTI_CMD.ID);
                    SpecificCc.AddRange(new byte[] { COMMAND_CLASS_WAKE_UP_V2.ID, COMMAND_CLASS_BATTERY.ID, COMMAND_CLASS_SENSOR_BINARY_V2.ID,
                    COMMAND_CLASS_ALARM.ID});
                    generic = 0x07;
                    specific = 0x01;
                    devOpt = ZWave.Enums.DeviceOptions.NoneListening | ZWave.Enums.DeviceOptions.OptionalFunctionality;
                    break;
                case LibTypes.DoorLockKeyPad:
                    SpecificCc.AddRange(new byte[] { COMMAND_CLASS_DOOR_LOCK_V3.ID, COMMAND_CLASS_BATTERY.ID });
                    generic = 0x40;
                    specific = 0x01;
                    devOpt = ZWave.Enums.DeviceOptions.FreqListeningMode250ms;
                    break;
                case LibTypes.LEDBulb:
                    SpecificCc.AddRange(new byte[] { COMMAND_CLASS_SWITCH_COLOR_V3.ID, COMMAND_CLASS_SWITCH_MULTILEVEL_V4.ID });
                    generic = (byte)GenericTypes.SWITCH_MULTILEVEL;
                    specific = 0x02;
                    break;
                case LibTypes.WallController:
                    SpecificCc.AddRange(new byte[] { COMMAND_CLASS_MULTI_CHANNEL_V4.ID, COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID,
                    COMMAND_CLASS_SWITCH_MULTILEVEL_V4.ID});
                    generic = 0x10;
                    specific = 0x01;
                    break;
                case LibTypes.PowerStrip:
                    SpecificCc.AddRange(new byte[] { COMMAND_CLASS_MULTI_CHANNEL_V4.ID, COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID,
                    COMMAND_CLASS_SWITCH_MULTILEVEL_V4.ID});
                    generic = 0x10;
                    specific = 0x01;
                    break;
                case LibTypes.Controller:
                case LibTypes.ControllerBridgeLib:
                    generic = 0x01;
                    specific = 0x00;
                    //No extra CC here....
                    break;
                case LibTypes.ControllerPortableLib:
                    SpecificCc.AddRange(new byte[] { COMMAND_CLASS_WAKE_UP_V2.ID, COMMAND_CLASS_BATTERY.ID });
                    generic = 0x10;
                    specific = 0x04;
                    devOpt = ZWave.Enums.DeviceOptions.NoneListening;
                    break;
            }

            List<byte> FullCc = new List<byte>();

            SecuredClasses.AddRange(SpecificCc);

            FullCc.AddRange(StandardClasses);
            FullCc.AddRange(SecuredClasses);
            FullCc = FullCc.Distinct().ToList();

            Device device = DeviceManager.GetDevice(deviceNumber);

            ActionResult AppNodeInfo = device.ApplicationNodeInformation(devOpt, generic, specific, FullCc.ToArray());
            ActionResult AppNodeInfoCmd = device.ApplicationNodeInformationCmdClasses(FullCc.ToArray(),
                StandardClasses.ToArray(), SecuredClasses.ToArray());
        }

        public BasicToken.Void[] SetApplicationResponse(Alias deviceNumber, LibTypes Library, byte[] FullCmd)
        {
            return SetApplicationResponse(deviceNumber, Library, FullCmd, null, 0x02);
        }

        /// <summary>
        /// Registers response data by Id + adds Version Support for each specified CC
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="Library"></param>
        /// <param name="FullCmd"></param>
        /// <param name="wakeUpInterval"></param>
        /// <param name="zWavePlusersion"></param>
        /// <returns></returns>
        public BasicToken.Void[] SetApplicationResponse(Alias deviceNumber, LibTypes Library, byte[] FullCmd, byte[] wakeUpInterval, byte zWavePlusersion)
        {
            List<ActionToken> ret = new List<ActionToken>();

            // In case of duplicates.
            FullCmd = FullCmd.Distinct().ToArray();

            Device device = DeviceManager.GetDevice(deviceNumber);

            CancelRunningResponseOperations(device);

            #region Common Application responses
            var commonCCIds = new List<byte>
            {
                COMMAND_CLASS_NOTIFICATION_V9.ID,
                COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ID,
                COMMAND_CLASS_ASSOCIATION.ID
            };
            #endregion      

            var allSupportCommandClassesForDevice = new List<CommandClassSupport>();
            allSupportCommandClassesForDevice.AddRange(CommandClassesSupportHandler.GetSupportCommandClassesByLibrary(Library));
            allSupportCommandClassesForDevice.AddRange(CommandClassesSupportHandler.GetDefaultCommandClassSupportsForCCIds(commonCCIds));
            allSupportCommandClassesForDevice.AddRange(CommandClassesSupportHandler.GetDefaultCommandClassSupportsForCCIds(FullCmd.ToList()));

            // Distinct!
            allSupportCommandClassesForDevice = allSupportCommandClassesForDevice.GroupBy(x => x.CommandClassId).Select(y => y.First()).ToList();
            foreach (var ccSupport in allSupportCommandClassesForDevice)
            {
                if (ccSupport is IndicatorSupport)
                {
                    var indicatorSupportData = new IndicatorSupportData();
                    indicatorSupportData.AddIndicator((byte)IndicatorIdNames.NodeIdentify, 0x38, 0x00);
                    indicatorSupportData.AddIndicator((byte)IndicatorIdNames.Busy, 0x38, 0x00);
                    AddCommandClassSupport(deviceNumber, 0, new IndicatorSupport(indicatorSupportData) { Version = 4 }, false);//The cert bitmask is wrong, this one shows the right supported id
                }
                else if (ccSupport is ColorSwitchSupport && Library == LibTypes.LEDBulb_v4)
                {
                    var colorSwitchSupport = new ColorSwitchSupport();
                    //Configure the support class to report ColorTemperature on the supported repport
                    AddCommandClassSupport(deviceNumber, 0, ccSupport, false);
                }
                else if (ccSupport is MultiChannelSupport)
                {
                    //Do nothing
                }
                else
                {
                    AddCommandClassSupport(deviceNumber, 0, ccSupport, false);
                }                
            }

            // PowerLevel from Zwave not Cert.
            ret.Add(device.SessionClient.ExecuteAsync(
                new ZWave.BasicApplication.CommandClasses.PowerLevelSupport(device.Network)));

            // Version from Zwave not Cert.
            ret.Add(device.ResponseDataEx(new Node(0, deviceNumber.EndPointId),
                new COMMAND_CLASS_VERSION_V3.VERSION_REPORT(),
                TXO,
                new COMMAND_CLASS_VERSION_V3.VERSION_GET()));

            // MultiChannel from Zwave not Cert. We do have two multichannel support handlers one in ZWave.BasicApplication.CommandClasses and another in ZWave.Services.
            // The problem is that the one in ZWave.BasicApplication.CommandClasses does allow for duplicate command threads which results in duplicated calls.
            if (FullCmd.Contains(COMMAND_CLASS_MULTI_CHANNEL_V4.ID))
            {
                //If Multichannel support is required, use ApplicationService.AddMultiChannelSupport()
                //ret.Add(device.SessionClient.ExecuteAsync(
                //    new ZWave.BasicApplication.CommandClasses.MultiChannelSupport(device.Network, TXO)));
            }

            #region Version CC Application Responses
            // register Version CC responses for CMDs not handled by Support tasks.
            foreach (var cmd in FullCmd)
            {
                byte ccVersionToReport = 1;
                if (cmd == COMMAND_CLASS_TRANSPORT_SERVICE_V2.ID)
                {
                    ccVersionToReport = 2;
                }
                else if (cmd == COMMAND_CLASS_VERSION_V3.ID)
                {
                    ccVersionToReport = 3;
                }
                
                if (allSupportCommandClassesForDevice.Any(y => y.CommandClassId == cmd))
                {
                    var supportCC = allSupportCommandClassesForDevice.FirstOrDefault(y => y.CommandClassId == cmd);
                    ccVersionToReport = supportCC.Version;
                }
                ret.Add(device.ResponseDataEx(new Node(0, deviceNumber.EndPointId),
                    new COMMAND_CLASS_VERSION_V3.VERSION_COMMAND_CLASS_REPORT()
                    {
                        commandClassVersion = ccVersionToReport,
                        requestedCommandClass = cmd
                    },
                    TXO,
                    new COMMAND_CLASS_VERSION_V3.VERSION_COMMAND_CLASS_GET()
                    {
                        requestedCommandClass = cmd
                    },
                    3));
            }
            ret.Add(device.ResponseDataEx(new Node(0, deviceNumber.EndPointId),
                new COMMAND_CLASS_VERSION_V3.VERSION_CAPABILITIES_REPORT()
                {
                    properties1 = { commandClass = 1, version = 1, zWaveSoftware = 0 }
                },
                TXO,
                new COMMAND_CLASS_VERSION_V3.VERSION_CAPABILITIES_GET()));
            #endregion

            return ret.Select(x => new BasicActionToken.Void(x)).ToArray();
        }

        /// <summary>
        /// Registers response data by Id + adds Version Support for each specified CC
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="preconfiguredDevice">Custom device with Modify support command classes where needed</param>
        /// <returns></returns>
        public BasicToken.Void[] SetApplicationResponsePreconfigured(Alias deviceNumber, IPreconfiguredDevice preconfiguredDevice)
        {
            List<ActionToken> ret = new List<ActionToken>();

            // In case of duplicates.
            var fullCmd = preconfiguredDevice.FullCc.Distinct().ToArray();

            Device device = DeviceManager.GetDevice(deviceNumber);

            CancelRunningResponseOperations(device);

            #region Common Application responses
            var commonCCIds = new List<byte>
            {
                COMMAND_CLASS_NOTIFICATION_V9.ID,
                COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ID,
                COMMAND_CLASS_ASSOCIATION.ID
            };
            #endregion

            SecurityTestSettings.SetInclusionFrameDelayS2(deviceNumber, SecurityS2TestFrames.KEXSet, 10000);
            _ = device.ApplicationNodeInformation(preconfiguredDevice.DeviceOptions, (byte)preconfiguredDevice.GenericType, preconfiguredDevice.SpecificType, fullCmd);
            _ = device.ApplicationNodeInformationCmdClasses(fullCmd, preconfiguredDevice.StandardCc.ToArray(), preconfiguredDevice.SecuredCc.ToArray());

            device.Network.SetApplicationNodeInformation(preconfiguredDevice.DeviceOptions, (byte)preconfiguredDevice.GenericType, preconfiguredDevice.SpecificType, fullCmd);
            device.Network.SetSecureCommandClasses(new NodeTag(device.Id), preconfiguredDevice.SecuredCc.ToArray());

            var allSupportCommandClassesForDevice = new List<CommandClassSupport>();
            allSupportCommandClassesForDevice.AddRange(CommandClassesSupportHandler.GetSupportCommandClassesByLibrary(preconfiguredDevice.Library));
            allSupportCommandClassesForDevice.AddRange(CommandClassesSupportHandler.GetDefaultCommandClassSupportsForCCIds(commonCCIds));
            allSupportCommandClassesForDevice.AddRange(CommandClassesSupportHandler.GetDefaultCommandClassSupportsForCCIds(fullCmd.ToList()));

            // Distinct!
            allSupportCommandClassesForDevice = allSupportCommandClassesForDevice.GroupBy(x => x.CommandClassId).Select(y => y.First()).ToList();
            foreach (var ccSupport in allSupportCommandClassesForDevice)
            {
                var modifySupportCommandClass = preconfiguredDevice.SupportedModifyCommandClasses.FirstOrDefault(x => x.GetType().IsSubclassOf(ccSupport.GetType()));
                if (modifySupportCommandClass != null)
                {
                    AddCommandClassSupport(deviceNumber, 0, modifySupportCommandClass, false);
                }
                else
                {
                    AddCommandClassSupport(deviceNumber, 0, ccSupport, false);
                }
            }

            // PowerLevel from Zwave not Cert.
            ret.Add(device.SessionClient.ExecuteAsync(
                new ZWave.BasicApplication.CommandClasses.PowerLevelSupport(device.Network)));

            // Version from Zwave not Cert.
            ret.Add(device.ResponseDataEx(new Node(0, deviceNumber.EndPointId),
                new COMMAND_CLASS_VERSION_V3.VERSION_REPORT(),
                TXO,
                new COMMAND_CLASS_VERSION_V3.VERSION_GET()));

            // MultiChannel from Zwave not Cert.
            if (fullCmd.Contains(COMMAND_CLASS_MULTI_CHANNEL_V4.ID))
            {
                ret.Add(device.SessionClient.ExecuteAsync(
                    new ZWave.BasicApplication.CommandClasses.MultiChannelSupport(device.Network, TXO)));
            }          

            #region Version CC Application Responses
            // register Version CC responses for CMDs not handled by Support tasks.
            foreach (var cmd in fullCmd)
            {
                byte ccVersionToReport = 1;
                if (cmd == COMMAND_CLASS_TRANSPORT_SERVICE_V2.ID)
                {
                    ccVersionToReport = 2;
                }
                else if (cmd == COMMAND_CLASS_VERSION_V3.ID)
                {
                    ccVersionToReport = 3;
                }

                if (allSupportCommandClassesForDevice.Any(y => y.CommandClassId == cmd))
                {
                    var supportCC = allSupportCommandClassesForDevice.FirstOrDefault(y => y.CommandClassId == cmd);
                    ccVersionToReport = supportCC.Version;
                }
                ret.Add(device.ResponseDataEx(new Node(0, deviceNumber.EndPointId),
                    new COMMAND_CLASS_VERSION_V3.VERSION_COMMAND_CLASS_REPORT()
                    {
                        commandClassVersion = ccVersionToReport,
                        requestedCommandClass = cmd
                    },
                    TXO,
                    new COMMAND_CLASS_VERSION_V3.VERSION_COMMAND_CLASS_GET()
                    {
                        requestedCommandClass = cmd
                    },
                    3));
            }
            ret.Add(device.ResponseDataEx(new Node(0, deviceNumber.EndPointId),
                new COMMAND_CLASS_VERSION_V3.VERSION_CAPABILITIES_REPORT()
                {
                    properties1 = { commandClass = 1, version = 1, zWaveSoftware = 0 }
                },
                TXO,
                new COMMAND_CLASS_VERSION_V3.VERSION_CAPABILITIES_GET()));
            #endregion

            return ret.Select(x => new BasicActionToken.Void(x)).ToArray();
        }

        public BasicToken.Void[] SetApplicationQuirkResponse(Alias deviceNumber, QuirckLibrary library, byte[] FullCmd)
        {
            return SetApplicationQuirkResponse(deviceNumber, library, FullCmd, null, 0x02);
        }
        public BasicToken.Void[] SetApplicationQuirkResponse(Alias deviceNumber, QuirckLibrary library, byte[] fullCmd, byte[] wakeUpInterval, byte zWavePlusersion, int groupings = 1)
        {
            // In case of duplicates.
            fullCmd = fullCmd.Distinct().ToArray();

            List<ActionToken> ret = new List<ActionToken>();
            Device device = DeviceManager.GetDevice(deviceNumber);
            //device.Network = new NetworkViewPoint();
            device.CancelRunningActionsByType(typeof(ResponseDataExOperation));  

            CommandClassesSupportHandlerModify ccSuportHandlerModify = new CommandClassesSupportHandlerModify();

            var allSupportCommandClassesForDevice = new List<CommandClassSupport>();

            switch (library)
            {
                case QuirckLibrary.FibaroFloodSensor:
                    //adding version for command classes manually (function GetDefaultCommandClassSupportsForCCIdsQuirk gets different versions)
                    //allSupportCommandClassesForDevice.Add( new NotificationSupport( ccSuportHandlerModify.GetDummyNotificationSupportData() ) { Version = 5 });
                    AutoResetEvent _fwuMDCompleted = new AutoResetEvent(false);
                    allSupportCommandClassesForDevice.Add(new FirmwareUpdateMdSupportBasicModify(() => { _fwuMDCompleted.Set(); }) { Version = 3 });

                    //byte byteToRemove = COMMAND_CLASS_ALARM.ID;
                    //fullCmd = fullCmd.Where(val => val != byteToRemove).ToArray();
                    break;
            }         

            allSupportCommandClassesForDevice.AddRange(ccSuportHandlerModify.GetDefaultCommandClassSupportsForCCIdsQuirk(fullCmd.ToList()));

            // Distinct!
            allSupportCommandClassesForDevice = allSupportCommandClassesForDevice.GroupBy(x => x.CommandClassId).Select(y => y.First()).ToList();
            foreach (var ccSupport in allSupportCommandClassesForDevice)
            {

                if (groupings != 1)
                {
                    switch (library)
                    {
                        case QuirckLibrary.DevoloSwitchBinary:
                            break;
                    }
                }
                else if (ccSupport is FirmwareUpdateMdSupportPCC)
                {
                    FirmwareUpdateMdSupportBasicModify firmwareUpdateMdSupportModify =
                        new FirmwareUpdateMdSupportBasicModify(ccSuportHandlerModify.GetDummySetNewImageCompletedCallback()) { Version = 2 };
                    AddCommandClassSupport(deviceNumber, 0, firmwareUpdateMdSupportModify, false);
                    firmwareUpdateMdSupportModify.SetNetwork(device.Network);
                }
                else
                {
                    AddCommandClassSupport(deviceNumber, 0, ccSupport, false);
                }
            }
            
            // PowerLevel from Zwave not Cert.
            ret.Add(device.SessionClient.ExecuteAsync(
                new ZWave.BasicApplication.CommandClasses.PowerLevelSupport(device.Network)));

            // Version from Zwave not Cert.
            ret.Add(device.ResponseDataEx(new Node(0, deviceNumber.EndPointId),
                new COMMAND_CLASS_VERSION_V3.VERSION_REPORT(),
                TXO,
                new COMMAND_CLASS_VERSION_V3.VERSION_GET()));

            // MultiChannel from Zwave not Cert.
            if (fullCmd.Contains(COMMAND_CLASS_MULTI_CHANNEL_V4.ID))
            {
                ret.Add(device.SessionClient.ExecuteAsync(
                    new ZWave.BasicApplication.CommandClasses.MultiChannelSupport(device.Network, TXO)));
            }

            #region Version CC Application Responses
            // register Version CC responses for CMDs not handled by Support tasks.
            foreach (var cmd in fullCmd)
            {
                byte ccVersionToReport = 1;
                if (cmd == COMMAND_CLASS_TRANSPORT_SERVICE_V2.ID)
                {
                    ccVersionToReport = 2;
                }
                else if (cmd == COMMAND_CLASS_VERSION_V3.ID)
                {
                    ccVersionToReport = 3;
                }

                if (allSupportCommandClassesForDevice.Any(y => y.CommandClassId == cmd))
                {
                    var supportCC = allSupportCommandClassesForDevice.FirstOrDefault(y => y.CommandClassId == cmd);
                    ccVersionToReport = supportCC.Version;
                }
                ret.Add(device.ResponseDataEx(new Node(0, deviceNumber.EndPointId),
                    new COMMAND_CLASS_VERSION_V3.VERSION_COMMAND_CLASS_REPORT()
                    {
                        commandClassVersion = ccVersionToReport,
                        requestedCommandClass = cmd
                    },
                    TXO,
                    new COMMAND_CLASS_VERSION_V3.VERSION_COMMAND_CLASS_GET()
                    {
                        requestedCommandClass = cmd
                    },
                    3));

            }
            ret.Add(device.ResponseDataEx(new Node(0, deviceNumber.EndPointId),
                    new COMMAND_CLASS_VERSION_V3.VERSION_CAPABILITIES_REPORT()
                    {
                        properties1 = { commandClass = 1, version = 1, zWaveSoftware = 0 }
                    },
                    TXO,
                    new COMMAND_CLASS_VERSION_V3.VERSION_CAPABILITIES_GET()));
            #endregion

            return ret.Select(x => new BasicActionToken.Void(x)).ToArray();
        }

        private void CancelRunningResponseOperations(Device device)
        {
            device.CancelRunningActionsByType(typeof(ResponseDataExOperation));
        }

        public void SetProfileSecurityKeys(string S2Access, string S2Auth, string S2UnAuth, string S0key,
            string tempNetKey)
        {

            CurrentProfile.CertS2AccessKey = S2Access;
            CurrentProfile.CertS2AuthKey = S2Auth;
            CurrentProfile.CertS2UnAuthKey = S2UnAuth;
            CurrentProfile.CertS2S0key = S0key;
            CurrentProfile.TempNetKey = tempNetKey;
        }

        public void SetRoleType(Alias deviceNumber, RoleTypes roleType)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            device.Network.SetRoleType((ZWave.Enums.RoleTypes)roleType);
        }

        public Profile GetProfile()
        {
            return CurrentProfile;
        }

        public byte[] GetProfileNetworkKey(SecuritySchemes securityScheme)
        {
            switch (securityScheme)
            {
                case SecuritySchemes.S2_ACCESS:
                    return ConvertHexStringToHexArray(CurrentProfile.CertS2AccessKey);
                case SecuritySchemes.S2_AUTHENTICATED:
                    return ConvertHexStringToHexArray(CurrentProfile.CertS2AuthKey);
                case SecuritySchemes.S2_UNAUTHENTICATED:
                    return ConvertHexStringToHexArray(CurrentProfile.CertS2UnAuthKey);
                case SecuritySchemes.S0:
                    return ConvertHexStringToHexArray(CurrentProfile.CertS2S0key);
            }
            return null;
        }

        public Dictionary<SecuritySchemes, byte[]> GetProfileNetworkKeys()
        {
            Dictionary<SecuritySchemes, byte[]> netKeys = new Dictionary<SecuritySchemes, byte[]>();
            netKeys.Add(SecuritySchemes.S2_ACCESS, ConvertHexStringToHexArray(CurrentProfile.CertS2AccessKey));
            netKeys.Add(SecuritySchemes.S2_AUTHENTICATED, ConvertHexStringToHexArray(CurrentProfile.CertS2AuthKey));
            netKeys.Add(SecuritySchemes.S2_UNAUTHENTICATED, ConvertHexStringToHexArray(CurrentProfile.CertS2UnAuthKey));
            netKeys.Add(SecuritySchemes.S0, ConvertHexStringToHexArray(CurrentProfile.CertS2S0key));
            netKeys.Add(SecuritySchemes.S2_TEMP, ConvertHexStringToHexArray(CurrentProfile.CertS2UnAuthKey));
            return netKeys;
        }

        public byte[] GetProfileDsk()
        {
            return ConvertHexStringToHexArray(CurrentProfile.CertS2Dsk);
        }
        private byte[] ConvertHexStringToHexArray(string hexString)
        {
            string hexString_fix = hexString.Replace(" ", String.Empty);
            return Enumerable.Range(0, hexString_fix.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(hexString_fix.Substring(x, 2), 16))
                     .ToArray(); ;
        }
        public void ModifyHomeId(Alias deviceNumber, byte[] homeId)
        {
            var dhc = DeviceManager.GetDeviceHostClient(deviceNumber);
            string args = "-c " + dhc.PClient.DataSource.SourceId + " -ph " + homeId.ToHexString().Replace(" ", "");
            DeviceManager.RunProgrammerWithArgs(dhc, args);
        }

        public void SaveSecuritySettingsToFile(Alias deviceNumber, string settingsFilePath)
        {
            // UI Instance only
        }

        public void OpenSecuritySettingsFromFile(Alias deviceNumber, string securitySettingsFilePath)
        {
            // UI Instance only
        }

        public BasicToken.Void[] SetApplicationCmd(Alias deviceNumber, bool IsS0, bool IsS2, LibTypes Library, byte[] wakeUpInterval, byte zwavePlusVersion)
        {
            throw new NotImplementedException();
        }
    }
}
