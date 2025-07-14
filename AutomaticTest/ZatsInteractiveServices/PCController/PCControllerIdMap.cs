// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    public static class PCControllerIdMap
    {
        public static class MainWindow
        {
            public const string ID = "ID_MAIN_WINDOW";
            public const string SettingsBtn = "ID_BTN_SETTINGS";
            public const string SecuritySettingsBtn = "ID_BTN_SECURITY_SETTINGS";
            public const string BusyView = "ID_BUSY_VIEW";
            public const string AbortBtn = "ID_BTN_ABORT";
            public const string MainMenuBtn = "ID_BTN_MAIN_MENU";
            public const string ShowLogBtn = "ID_BTN_SHOWLOG";
            public const string CommandQueueBtn = "ID_BTN_COMMAND_QUEUE";
            public const string SendDataSettingsBtn = "ID_BTN_SEND_DATA_SETTINGS";
        }

        public static class MainMenuView
        {
            public const string ID = "ID_MAIN_MENU_VIEW";
            public const string NetworkManagementBtn = "ID_BTN_NETWORK_MANAGEMENT";
            public const string CommandClassesBtn = "ID_BTN_COMMAND_CLASSES";
            public const string SetupRouteBtn = "ID_BTN_SETUP_ROUTE";
            public const string EncryptDecryptBtn = "ID_BTN_ENCRYPT_DECRYPT";
            public const string AssociationsBtn = "ID_BTN_ASSOCIATIONS";
            public const string SamrtStartBtn = "ID_BTN_SMART_START";
        }

        public static class LogView
        {
            public const string LogLv = "ID_LV_LOG";
            public const string ClearBtn = "ID_BTN_CLEAR";
        }

        public static class CommandQueueView
        {
            public const string ItemsLv = "ID_LV_ITEMS";
            public const string DeleteBtn = "ID_BTN_DELETE";
            public const string ClearBtn = "ID_BTN_CLEAR";
        }

        public static class NetworkManagementView
        {
            public const string ID = "ID_NETWORK_MANAGEMENT_VIEW";
            public const string ResetBtn = "ID_BTN_RESET";
            public const string AddBtn = "ID_BTN_ADD";
            public const string RemoveBtn = "ID_BTN_REMOVE";
            public const string ClassicLearnModeBtn = "ID_BTN_LEARN_MODE";
            public const string SelectLearnModeBtn = "ID_BTN_SET_LEARN_MODE";
            public const string ResetSpanBtn = "ID_BTN_RESET_SPAN";
            public const string NextSpanBtn = "ID_BTN_NEXT_SPAN";
            public const string SecuritySchemeBtn = "ID_BTN_SECURITY_SCHEME";
            public const string ControllerInfoTvw = "ID_TVW_CONTROLLER_INFO";
            public const string NodeInfoBtn = "ID_BTN_NODE_INFO";
            public const string MpanTableBtn = "ID_BTN_MPAN_TABLE";
            public const string SetSisBtn = "ID_BTN_SET_SIS";
            public const string UpdateBtn = "ID_BTN_UPDATE";
            public const string ShiftBtn = "ID_BTN_SHIFT";
            public const string NopBtn = "ID_BTN_NOP";
            public const string IsFailedBtn = "ID_BTN_IS_FAILED";
            public const string ReplaceFailedBtn = "ID_BTN_REPLACE_FAILED";
            public const string RemoveFailedBtn = "ID_BTN_REMOVE_FAILED";
            public const string NwiBtn = "ID_BTN_NWI";
            public const string NweBtn = "ID_BTN_NWE";
            public const string BasicTestBtn = "ID_BTN_TOGGLE_BASIC";
            public const string NeighborsUpdateBtn = "ID_BTN_NEIGHBORS_UPDATE";
            public const string AddCustomBtn = "ID_BTN_ADD_NODE_CUSTOM";
            public const string SendNodeInfoBtn = "ID_BTN_SEND_NODE_INFO";
        }

        public static class SelectLearnModeView
        {
            public const string ID = "ID_DIALOG_WINDOW";
            public const string CAPTION = "Select Learn Mode";
            public const string ClassicLMBtn = "ID_BTN_CLASSIC_LEARN_MODE";
            public const string SmartStartLMBtn = "ID_BTN_SMART_START_LEARN_MODE";
            public const string NWILMBtn = "ID_BTN_NWI_LEARN_MODE";
            public const string NWELMBtn = "ID_BTN_NWE_LEARN_MODE";
            public const string VirtualDeviceLMBtn = "ID_BTN_VIRTUAL_DEVICE_LEARN_MODE";
        }

        public static class AddNodeWithCustomSettingsView
        {
            public const string ID = "ID_DIALOG_WINDOW";
            public const string CAPTION = "Add node with custom settings";
            public const string OkBtn = "ID_BTN_ADD_CUSTOM_CONFIRM_OK";
            public const string CancelBtn = "ID_BTN_ADD_CUSTOM_CONFIRM_CANCEL";
            public const string IsSetAsSisAutomaticallyChk = "ID_CHK_AUTO_SIS";
            public const string IsBasedOnZwpRoleTypeChk = "ID_CHK_BASED_ONBASIC_TYPE";
            public const string IsDeleteReturnRouteChk = "ID_CHK_DeleteReturnRoute";
            public const string IsAssignReturnRouteChk = "ID_CHK_AssignReturnRoute";
            public const string IsAssociationCreateChk = "ID_CHK_AssociationCreate";
            public const string IsMultichannelAssociationCreateChk = "ID_CHK_MultichannelAssociationCreate";
            public const string IsWakeUpCapabilitiesChk = "ID_CHK_WakeUpCapabilities";
            public const string IsWakeUpIntervalChk = "ID_CHK_WakeUpInterval";
            
        }

        public static class MPANTableConfigurationView
        {
            public const string ID = "ID_MPAN_TABLE_CONFIGURATION_VIEW";
            public const string LoadMPANsBtn = "ID_BTN_LOAD_MPANS";
            public const string ClearBtn = "ID_BTN_CLEAR";
            public const string MPANsLv = "ID_LV_MPANS";
            public const string GroupIdTb = "ID_TB_GROUP_ID";
            public const string OwnerIdTb = "ID_TB_OWNER_ID";
            public const string MosChk = "ID_CHK_MOS";
            public const string SequenceNumberTb = "ID_TB_SEQUENCE_NUMBER";
            public const string MPANStateTb = "ID_TB_MPAN_STATE";
            public const string NodeIdsTb = "ID_TB_NODE_IDS";
            public const string AddUpdateBtn = "ID_BTN_ADD_UPDATE";
            public const string RemoveBtn = "ID_BTN_REMOVE";
            public const string NextMPANBtn = "ID_BTN_NEXT_MPAN";
        }

        public static class CommandClassesView
        {
            public const string ID = "ID_COMMAND_CLASSES_VIEW";
            public const string BtnSend = "ID_BTN_SEND";
            public const string TbCommandToSend = "ID_TB_COMMAND_TO_SEND";
            public const string RbDefault = "ID_RB_DEFAULT";
            public const string RbSecure = "ID_RB_SECURE";
            public const string RbNonSercure = "ID_RB_NONSECURE";
            public const string CRC16Chk = "ID_CHK_CRC16";
            public const string ForceMulticastChk = "ID_CHK_FORCE_MULTICAST";
            public const string SuppressMulticastFollowUpChk = "ID_CHK_SUPPRESS_MULTICAST_FOLLOW_UP";
            public const string SupervisionGetChk = "ID_CHK_SUPERVISION_GET";
            public const string StatusUpdatesChk = "ID_CHK_STATUS_UPDATES";
            public const string TbSessionID = "ID_TB_SESSION_ID";
            public const string AutoIncrementChk = "ID_CHK_AUTO_INCREMENT";
            public const string MultiChannelChk = "ID_CHK_MULTI_CHANNEL";
            public const string TbSrcEndPoint = "ID_TB_SRC_END_POINT";
            public const string TbDestEndPoint = "ID_TB_DEST_END_POINT";
            public const string BitAddressChk = "ID_CHK_BIT_ADDRESS";
            public const string CommandClassesInfoTvw = "ID_TVW_COMMAND_CLASSES_INFO";
            public const string ShowSelectCommandBtn = "ID_TBN_SHOW_SELECT_COMMAND";
            public const string RbSerialApi = "ID_RB_SERIAL_API";
            public const string RbBroadcast = "ID_RB_BROADCAST";
        }

        public static class EncryptDecryptView
        {
            public const string ID = "ID_COMMAND_ENCRYPT_DECRYPT_VIEW";
            public const string HomeIdTb = "ID_TB_HOME_ID";
            public const string SenderIdTb = "ID_TB_SENDER_ID";
            public const string ReceiverIdTb = "ID_TB_RECEIVER_ID";
            public const string SequenceNumberTb = "ID_TB_SEQUENCE_NUMBER_ID";
            public const string ReceiverNonceTb = "ID_TB_RECEIVER_NONCE";
            public const string SenderNonceTb = "ID_TB_SENDER_NONCE";
            public const string SecurityKeyTb = "ID_TB_SECURITY_KEY";
            public const string DecryptTb = "ID_TB_DECRYPT";
            public const string EncryptTb = "ID_TB_ENCRYPT";
            public const string DecryptBtn = "ID_BTN_DECRYPT";
            public const string EncryptBtn = "ID_BTN_ENCRYPT";
            public const string NormalChk = "ID_CHK_NORMAL";
            public const string TempChk = "ID_CHK_TEMP";
        }

        public static class NodesListView
        {
            public const string ID = "ID_NODES_LIST_VIEW";
            public const string NodesLv = "ID_LV_NODES";
            public const string NodeIdTb = "ID_TB_NODEID";
            public const string NodeTypeTb = "ID_TB_NODETYPE";
            public const string EnableQOChk = "ID_CHK_QO";
            public const string IsListeningChk = "ID_CHK_IL";
            public const string IsVirtualChk = "ID_CHK_IV";
        }

        public static class SettingsDialog
        {
            public const string ID = "ID_DLG_SETTINGS";
            public const string SerialDataSourcesLb = "ID_LB_SERIAL_DATA_SOURCES";
            public const string SocketDataSourcesLb = "ID_LB_SOCKET_DATA_SOURCES";
            public const string DiscoverBtn = "ID_DISCOVER_BTN";
            public const string BtnOk = "ID_BTN_OK";
        }

        public static class SecuritySettingsDialog
        {
            public const string ID = "ID_SECURITY_SETTINGS_VIEW";
            public const string EnableSecurityTgl = "ID_BTN_ENABLE_SECURITY";
            public const string EnableS0Chk = "ID_CHK_ENABLE_S0";
            public const string EnableS2UnauthChk = "ID_CHK_ENABLE_UNAUTH";
            public const string EnableS2AuthChk = "ID_CHK_ENABLE_AUTH";
            public const string EnableS2AccessChk = "ID_CHK_ENABLE_ACCESS";
            public const string JoinCSAChk = "ID_CHK_JOIN_CSA";
            public const string KeyS0Tb = "ID_TB_KEY_S0";
            public const string KeyUnauthTb = "ID_TB_UNAUTH";
            public const string KeyAuthTb = "ID_TB_AUTH";
            public const string KeyAccessTb = "ID_TB_ACCESS";
            public const string KeyLastUsedTempTb = "ID_TB_KEYTEMP";
            public const string OverrideS0Chk = "ID_CHK_OVRRD_S0";
            public const string OverrideS2UnauthChk = "ID_CHK_OVRRD_UNAUTH";
            public const string OverrideS2AuthChk = "ID_CHK_OVRRD_AUTH";
            public const string OverrideS2AccessChk = "ID_CHK_OVRRD_ACCESS";
            public const string OverrideS0Tb = "ID_TB_OVRRD_S0";
            public const string OverrideS2UnauthTb = "ID_TB_OVRRD_UNAUTH";
            public const string OverrideS2AuthTb = "ID_TB_OVRRD_AUTH";
            public const string OverrideS2AccessTb = "ID_TB_OVRRD_ACCESS";
            public const string ContainerTestsSchemeTab = "ID_TAB_CONTAINER_TEST_SCHEME";
            public const string PageS0TestsSchemeTab = "ID_TAB_PAGE_S0_TEST_SCHEME";
            public const string PageS2TestsSchemeTab = "ID_TAB_PAGE_S2_TEST_SCHEME";
            public const string EnableS0TestsSchemeChk = "ID_CHK_ENABLE_S0_TEST_SCHEME";
            public const string EnableS2TestsSchemeChk = "ID_CHK_ENABLE_S2_TEST_SCHEME";
            public const string S2ParameterTypeCmb = "ID_CMB_S2_PARAMETER_TYPE";
            public const string S2ParameterValueTb = "ID_TB_S2_PARAMETER_VALUE";
            public const string S2ParameterSetAddBtn = "ID_BТN_S2_PARAMETER_SET_ADD";
            public const string S2ParameterClearBtn = "ID_BТN_S2_PARAMETER_CLEAR";
            //Inclusion Test Frames
            public const string S2InclusionFramesSetAddBtn = "ID_BТN_S2_FRAMES_SET_ADD";
            public const string S2InclusionFramesClearBtn = "ID_BТN_S2_FRAMES_CLEAR";
            public const string S2InclusionFrameTypeCmb = "ID_CMB_S2_TESTFRAME_TYPE";
            public const string S2InclusionFrameCommandChk = "ID_CHK_FRAME_COMMAND";
            public const string S2InclusionFrameCommandValueTb = "ID_TB_FRAME_COMMAND_VALUE";
            public const string S2InclusionFrameEncryptedChk = "ID_CHK_FRAME_ENCRYPTED";
            public const string S2InclusionFrameEncryptedValueChk = "ID_CHK_FRAME_ENCRYPTED_VALUE";
            public const string S2InclusionFrameNetworkKeyChk = "ID_CHK_FRAME_NETWORK_KEY";
            public const string S2InclusionFrameNetworkKeyValueTb = "ID_TB_FRAME_NETWORK_KEY_VALUE";
            public const string S2InclusionFrameTempKeyChk = "ID_CHK_FRAME_TEMP_KEY";
            public const string S2InclusionFrameTempKeyValueChk = "ID_CHK_FRAME_TEMP_KEY_VALUE";
            public const string S2InclusionFrameDelayChk = "ID_CHK_FRAME_DELAY";
            public const string S2InclusionFrameDelayValueTb = "ID_TB_FRAME_DELAY_VALUE";
            public const string S2InclusionFrameMulticastChk = "ID_CHK_FRAME_MULT";
            public const string S2InclusionFrameMulticastValueChk = "ID_CHK_FRAME_MULT_VALUE";
            public const string S2InclusionFrameBroadcastChk = "ID_CHK_FRAME_BROAD";
            public const string S2InclusionFrameBroadcastValueChk = "ID_CHK_FRAME_BROAD_VALUE";
            //Message Extensions Overrides opt:
            public const string S2MessageExtensionsClearBtn = "ID_BTN_S2_EXTENSION_CLEAR";
            public const string S2MessageExtensionsAddBtn = "ID_BTN_S2_EXTENSION_ADD";
            public const string ExtensionMessageTypeCmb = "ID_CMB_EXTENSION_MESSAGE_TYPE";
            public const string ExtensionTypeCmb = "ID_CMB_EXTENSION_TYPE";
            public const string ExtensionAppliedActionCmb = "ID_CMB_APPLIED_ACTION";
            public const string ExtensionHasValueChk = "ID_CHK_HAS_VALUE";
            public const string ExtensionValueTb = "ID_TB_EXTENSION_VALUE";
            public const string ExtensionEncryptedKeyChk = "ID_CHK_EXTENSION_ENCRYPTED_KEY";
            public const string ExtensionEncryptedValueChk = "ID_CHK_EXTENSION_ENCRYPTED_VALUE";
            public const string ExtensionLenghtKeyChk = "ID_CHK_EXTENSION_LENGHT_KEY";
            public const string ExtensionLenghtValueTb = "ID_TB_EXTENSION_LENGHT_VALUE";
            public const string ExtensionMoreToFollowKeyChk = "ID_CHK_EXTENSION_MORE_TO_FOLLOW_KEY";
            public const string ExtensionMoreToFollowValueChk = "ID_CHK_EXTENSION_MORE_TO_FOLLOW_VALUE";
            public const string ExtensionCriticalKeyChk = "ID_CHK_EXTENSION_CRITICAL_KEY";
            public const string ExtensionCriticalValueChk = "ID_CHK_EXTENSION_CRITICAL_VALUE";
            public const string ExtensionNumberOfUsageKeyChk = "ID_CHK_EXTENSION_NUMBER_OF_USAGE_KEY";
            public const string ExtensionNumberOfUsageValueTb = "ID_TB_EXTENSION_NUMBER_OF_USAGE_VALUE";
            //S0 Test Scheme properties:
            public const string S0IncludingControllerRb = "ID_RB_INCLUDING_CONTROLLER";
            public const string S0IncludingNodeRb = "ID_RB_INCLUDING_NODE";

            public const string S0InvalidNonceSetKeyChk = "ID_CHK_INVALID_NONCE_IN_NETWORK_SET_KEY";
            public const string S0InvalidNonceSetValueTb = "ID_TB_INVALID_NONCE_IN_NETWORK_SET_VALUE";
            public const string S0InvalidNonceVerifyKeyChk = "ID_CHK_INVALID_NONCE_IN_NETWORK_VERIFY_KEY";
            public const string S0InvalidNonceVerifyValueTb = "ID_CHK_INVALID_NONCE_IN_NETWORK_VERIFY_VALUE";

            public const string S0InvalidMACSetKeyChk = "ID_CHK_INVALID_MAC_IN_NETWORK_SET_KEY";
            public const string S0InvalidMACSetValueTb = "ID_TB_INVALID_MAC_IN_NETWORK_SET_VALUE";
            public const string S0InvalidMACVerifyKeyChk = "ID_CHK_INVALID_MAC_IN_NETWORK_VERIFY_KEY";
            public const string S0InvalidMACVerifyValueTb = "ID_TB_INVALID_MAC_IN_NETWORK_VERIFY_VALUE";

            public const string S0InvalidNetworkKeySetKeyChk = "ID_CHK_INVALID_NETWORK_KEY_SET_KEY";
            public const string S0InvalidNetworkKeySetValueTb = "ID_TB_INVALID_NETWORK_KEY_SET_VALUE";
            public const string S0InvalidNetworkKeyVerifyKeyChk = "ID_CHK_INVALID_NETWORK_KEY_VERIFY_KEY";
            public const string S0InvalidNetworkKeyVerifyValueTb = "ID_TB_INVALID_NETWORK_KEY_VERIFY_VALUE";

            public const string S0SendInvalidSchemeGetKeyChk = "ID_CHK_SEND_INVALID_SCHEME_GET_KEY";
            public const string S0SendInvalidSchemeGetValueTb = "ID_TB_SEND_INVALID_SCHEME_GET_VALUE";
            public const string S0SendInvalidSchemeReportKeyChk = "ID_CHK_SEND_INVALID_SCHEME_REPORT_KEY";
            public const string S0SendInvalidSchemeReportValueTb = "ID_CHK_SEND_INVALID_SCHEME_REPORT_VALUE";

            public const string S0SendInvalidSchemeInheritKeyChk = "ID_CHK_SEND_INVALID_SCHEME_INHERIT_KEY";
            public const string S0SendInvalidSchemeInheritValueTb = "ID_TB_SEND_INVALID_SCHEME_INHERIT_VALUE";

            public const string S0DelaySchemeGetKeyChk = "ID_CHK_DELAY_SCHEME_GET_KEY";
            public const string S0DelaySchemeGetValueTb = "ID_TB_DELAY_SCHEME_GET_VALUE";
            public const string S0DelayNonceGetKeyChk = "ID_CHK_DELAY_NONCE_GET_KEY";
            public const string S0DelayNonceGetValueTb = "ID_TB_DELAY_NONCE_GET_VALUE";
            public const string S0DelayNetworkKeySetKeyChk = "ID_CHK_DELAY_NETWORK_KEY_SET_KEY";
            public const string S0DelayNetworkKeySetValueTb = "ID_TB_DELAY_NETWORK_KEY_SET_VALUE";
            public const string S0DelayNonceReportKeyChk = "ID_CHK_DELAY_NONCE_REPORT_KEY";
            public const string S0DelayNonceReportValueTb = "ID_TB_DELAY_NONCE_REPORT_VALUE";
            public const string S0DelaySchemeInheritKeyChk = "ID_CHK_DELAY_SCHEME_INHERIT_KEY";
            public const string S0DelaySchemeInheritValueTb = "ID_TB_DELAY_SCHEME_INHERIT_VALUE";
            public const string S0DelaySchemeReportKeyChk = "ID_TB_DELAY_SCHEME_REPORT_KEY";
            public const string S0DelaySchemeReportValueTb = "ID_TB_DELAY_SCHEME_REPORT_VALUE";
            public const string S0DelayNonceReportInKeyChk = "ID_TB_DELAY_NONCE_REPORT_IN_KEY";
            public const string S0DelayNonceReportInValueTb = "ID_TB_DELAY_NONCE_REPORT_IN_VALUE";
            public const string S0DelayNonceGetInKeyChk = "ID_TB_DELAY_NONCE_GET_IN_KEY";
            public const string S0DelayNonceGetInValueTb = "ID_TB_DELAY_NONCE_GET_IN_VALUE";
            public const string S0DelayNetworkKeyVerifyKeyChk = "ID_TB_DELAY_NETWORK_KEY_VERIFY_KEY";
            public const string S0DelayNetworkKeyVerifyValueTb = "ID_TB_DELAY_NETWORK_KEY_VERIFY_VALUE";
            public const string S0DelayReportAfterSchemeInheritChk = "ID_TB_DELAY_SCHEME_REPORT_AFTER_SCHEME_IHERIT_KEY";
            public const string S0DelayReportAfterSchemeInheritTb = "ID_TB_DELAY_SCHEME_REPORT_AFTER_SCHEME_IHERIT_VALUE";

            public const string BtnSaveSettings = "BTN_SAVE_SETTINGS";
            public const string BtnLoadSettings = "BTN_LOAD_SETTINGS";
            public const string ChkSaveKeys = "CHK_SAVE_KEYS";
            public const string TbSavedKeysStoragePath = "TB_SAVED_KEYS_STORAGE_PATH";
            public const string OkBtn = "ID_BTN_OK";
            public const string CancelBtn = "ID_BTN_CANCEL";

            public const string CopyS0KeyBtn = "ID_BTN_COPY_S0_KEY";
            public const string CopyUnauthKeyBtn = "ID_BTN_COPY_UNAUTH_KEY";
            public const string CopyAuthKeyBtn = "ID_BTN_COPY_AUTH_KEY";
            public const string CopyAcessKeyBtn = "ID_BTN_COPY_ACESS_KEY";
            public const string CopyTempKeyBtn = "ID_BTN_COPY_TEMP_KEY";
        }

        public static class UserInputDialog
        {
            public const string OkBtn = "ID_BTN_USER_INPUT_OK";
            public const string CancelBtn = "ID_BTN_USER_INPUT_CANCEL";
            public const string InTb = "ID_TB_USER_INPUT_IN";
            public const string OutTb = "ID_TB_USER_INPUT_OUT";
            public const string InCmb = "ID_CMB_USER_INPUT";
        }

        public static class KexSetConfirmDialog
        {
            public const string OkBtn = "ID_BTN_KEX_SET_CONFIRM_OK";
            public const string CancleBtn = "ID_BTN_KEX_SET_CONFIRM_CANCEL";
            public const string S2AccessChk = "ID_CHK_S2_ACCESS";
            public const string S2AuthenticatedChk = "ID_CHK_S2_AUTHENTICATED";
            public const string S2UnauthenticatedChk = "ID_CHK_S2_UNAUTHENTICATED";
            public const string S0Chk = "ID_CHK_S0";
        }

        public static class DialogWindow
        {
            public const string ID = "ID_DIALOG_WINDOW";
        }

        public static class SetupRouteView
        {
            public const string ID = "ID_SETUP_ROUTE_VIEW";
            public const string LvSourcesNodeList = "ID ID_NODES_LIST_VIEW_SOURCES";
            public const string LvDestinationsNodeList = "ID_NODES_LIST_VIEW_DESTINATIONS";
            public const string RbGetSetPriorityRoute = "ID_RB_GET_SET_PRIORITY_ROUTE";
            public const string TbPriorityRoute = "ID_TB_PRIORITY_ROUTE";
            public const string CmbRouteSpeed = "ID_CMB_ROUTE_SPEED";
            public const string BtnSetPriorityRoute = "ID_BTN_SET_PRIORITY_ROUTE";
        }

        public static class ErrorDialog
        {
            public const string ID = "ID_DIALOG_ERROR_MESSAGE";
        }

        public static class AssociationsView
        {
            public const string ID = "ID_ASSOCIATIONS_VIEW";
            public const string BtnCreate = "ID_BTN_CREATE";
            public const string BtnRemove = "ID_BTN_REMOVE";
            public const string BtnGetGroups = "ID_BTN_GET_GROUPS";
            public const string GetNodes = "ID_BTN_GET_NODES";
            public const string AssignReturnRoutesChk = "ID_CHK_ASSIGN_RETURN_ROUTES";
            public const string AssociationsInfoTvw = "ID_TVW_ASSOCIATIONS_INFO";
            public const string TbRootDevice = "ID_ROOT_DEVICE";
            public const string TbNodeType = "ID_NODE_TYPE";
            public const string TbEndPoint = "ID_END_POINT";
            public const string ItemAssociation = "ID_TVM_ITEM_ASSOCIATIONS";
            public const string DebugItem = "DEBUG";
        }

        public static class SmartStartView //ID_BTN_SMART_START
        {
            public const string ID = "ID_SMART_START_VIEW";
            public const string LvProvisioningListTvw = "ID_TVW_PROVISIONING_LIST_VIEW";
            public const string AddDskBtn= "ID_BTN_ADD_DSK";
            public const string DskValueTb = "ID_TB_DSK_VALUE";
        }

        public static class SelectCommandView
        {
            public const string ID = "ID_SELECT_COMMAND_VIEW";
            public const string SelectCommandClassesTvw = "ID_TVW_ALL_COMMAND_CLASSES_INFO";
            public const string CancelBtn = "ID_BTN_CANCEL";
            public const string OkBtn = "ID_BTN_OK";
            public const string AllCmdClassesChk = "ID_CHK_ALL_COMMAND_CLASSES";
        }

        public static class SendDataSettingsDialog
        {
            public const string ID = "ID_DIALOG_WINDOW";
            public const string Title = "Send Data Settings";
            public const string OkBtn = "ID_BTN_OK";
            public const string CancelBtn = "ID_BTN_CANCEL";
            public const string TransportServiceMaxSegmentSizeTb = "ID_TB_TRANSPORT_SERVICE_SEGMENT_MAX_SIZE";
            public const string S0MaxBytesPerFrameSizeTb = "ID_TB_MAX_BYTES_PER_FRAME_SIZE";
            public const string DelayWakeUpNoMoreInformationMsTb = "ID_TB_DELAY_WAKEUP_NOMORE_INFORMATION_MS";
            public const string RequestsTimeoutMsTb = "ID_TB_REQUESTS_TIMEOUT";
            public const string DelayResponseMsTb = "ID_TB_DELAY_RESPONSE_SEC";
        }

        public static class MessageDialog
        {
            public const string ID = "ID_MESSAGE_DIALOG";
            public const string Title = "Z-Wave PC Controller";
            public const string CloseBtn = "ID_BTN_MESSAGE_DIALOG_CLOSE";
        }

        public static class FileDialog
        {
            public const string SaveTitle = "Save As";
            public const string OpenTitle = "Open";
            public const string FileNameTb = "File name:";
            public const string SaveBtn = "Save";
            public const string OpenBtn = "Open";
        }
    }
}
