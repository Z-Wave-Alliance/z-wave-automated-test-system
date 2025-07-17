// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: 2025 Z-Wave Alliance https://z-wavealliance.org
using System.Collections.Generic;
using ZWave.BasicApplication.Operations;
using ZWave.Devices;
using Utils;
using System.Linq;
using System;
using ZWave;
using ZWave.BasicApplication;
using Newtonsoft.Json;
using System.ComponentModel;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;

namespace Zats.Services
{
    [CommandClassName("User Code Command Class")]
    [CommandClassId(COMMAND_CLASS_USER_CODE.ID)]
    [MinVersion(1)]
    [MaxVersion(2)]

    // ToDo: Keypad Mode 'Locked Out'
    // ToDo: User ID Status 'Messaging'
    // ToDo (nice to have): Support for variable Keymaps (UserCodeSupportData.SupportedKeysBitMask)
    // ToDo (clarify): Interaction with DoorLockCC to simulate Keypad entries (Interactive_CheckUnlockByUserCode) and check User Codes
    // ToDo: Logging ?

    public class UserCodeSupport : CommandClassSupport
    {
        private UserCodeSupportData UserCodeSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return UserCodeSupportData; }
            set { UserCodeSupportData = (UserCodeSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;
        
        public UserCodeSupport()
            : this(new UserCodeSupportData() { })
        { }

        public UserCodeSupport(UserCodeSupportData userCodeSupportData)
            : base(COMMAND_CLASS_USER_CODE.ID)
        {
            UserCodeSupportData = userCodeSupportData;
        }

        public UserCredentialSupport UserCredentialSupport = null;

        public static readonly byte[] EmptyCode = new byte[] { 0x00, 0x00, 0x00, 0x00 };

        protected override void OnHandledDelayed(DataReceivedUnit ou)
        {
            ou.SetNextActionItems();

            if (MatchesDestinationEndPoint())
            {
                /*if (UserCodeSupportData.Users.Count == 0)
                {
                    InitializeUsersForCTT();
                    InitializeCapabilitiesForCTT();
                }*/
                var sendDstNode = new NodeTag(ReceivedAchData.DstNode.Id, DstNode.EndPointId);
                var sendSrcNode = ReceivedAchData.SrcNode;
                byte[] command = ReceivedAchData.Command;
                var scheme = (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme;
                bool isHandlingAllowed = IsHandlingAllowed(Network, command, scheme, Secure);

                ResultForSv = SvResultNames.Undefined;
                if (isHandlingAllowed && command != null && command.Length > 1 && UserCodeSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_USER_CODE_V2.USER_CODE_SET.ID)
                    {
                        ResultForSv = HandleUserCodeSetCmd(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.USER_CODE_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareUserCodeReportCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.USERS_NUMBER_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareUserNumberReport();
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_SET.ID)
                    {
                        ResultForSv = HandleExtendedUserCodeSetCmd(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareExtUserCodeReportCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.USER_CODE_CAPABILITIES_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareCapabilitiesReport();
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.ADMIN_CODE_SET.ID)
                    {
                        ResultForSv = HandleMasterCodeSetCmd(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.ADMIN_CODE_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareMasterCodeReport();
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.USER_CODE_KEYPAD_MODE_SET.ID)
                    {
                        ResultForSv = HandleKeypadModeSetCmd(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.USER_CODE_KEYPAD_MODE_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareKeypadModeReport();
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.USER_CODE_CHECKSUM_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareChecksumReport();
                    }

                    if (rpt != null && IsAnsweringAllowed())
                    {
                        ApiOperation sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command, sendDstNode.EndPointId);
                }
            }
        }

        private byte[] PrepareCapabilitiesReport()
        {
            if (Version >= 2)
            {
                COMMAND_CLASS_USER_CODE_V2.USER_CODE_CAPABILITIES_REPORT rpt = new COMMAND_CLASS_USER_CODE_V2.USER_CODE_CAPABILITIES_REPORT();

                rpt.properties1.acSupport = (byte)(UserCodeSupportData.AdminCodeSup ? 0x01 : 0x00);
                rpt.properties1.acdSupport = (byte)(UserCodeSupportData.AdminCodeDeactivationSup ? 0x01 : 0x00);
                rpt.supportedUserIdStatusBitMask = CommandClassSupportUtils.CreateBitmaskFromList(GetSupportedUserStatusIds(), 0);   // Bit 0 represents type 0
                rpt.properties1.supportedUserIdStatusBitMaskLength = (byte)rpt.supportedUserIdStatusBitMask.Count;
                rpt.properties2.uccSupport = (byte)(UserCodeSupportData.UserCodeChecksumSup ? 0x01 : 0x00);
                rpt.properties2.mucrSupport = (byte)(UserCodeSupportData.MultipleUserCodeReportSup ? 0x01 : 0x00);
                rpt.properties2.mucsSupport = (byte)(UserCodeSupportData.MultipleUserCodeSetSup ? 0x01 : 0x00);
                rpt.supportedKeypadModesBitMask = CommandClassSupportUtils.CreateBitmaskFromList(UserCodeSupportData.SupportedKeypadModesList, 0);   // Bit 0 represents type 0
                rpt.properties2.supportedKeypadModesBitMaskLength = (byte)rpt.supportedKeypadModesBitMask.Count;
                rpt.supportedKeysBitMask = CommandClassSupportUtils.CreateBitmaskFromList(UserCodeSupportData.SupportedKeysList, 0); ;
                rpt.properties3.supportedKeysBitMaskLength = (byte)rpt.supportedKeysBitMask.Count;
                return rpt;
            }
            return null;
        }

        private byte[] PrepareUserNumberReport()
        {
            if (Version == 1)
            {
                COMMAND_CLASS_USER_CODE.USERS_NUMBER_REPORT rpt = new COMMAND_CLASS_USER_CODE.USERS_NUMBER_REPORT
                {
                    supportedUsers = 255
                };
                if (UserCodeSupportData.SupportedUsers <= 255)
                {
                    rpt.supportedUsers = Convert.ToByte(UserCodeSupportData.SupportedUsers);
                }
                return rpt;
            }
            if (Version >= 2)
            {
                COMMAND_CLASS_USER_CODE_V2.USERS_NUMBER_REPORT rpt = new COMMAND_CLASS_USER_CODE_V2.USERS_NUMBER_REPORT
                {
                    supportedUsers = 255
                };
                if (UserCodeSupportData.SupportedUsers <= 255)
                {
                    rpt.supportedUsers = Convert.ToByte(UserCodeSupportData.SupportedUsers);
                }
                rpt.extendedSupportedUsers = CommandClassSupportUtils.UInt64ToByteField(UserCodeSupportData.SupportedUsers, 2);
                return rpt;
            }
            return null;
        }

        private byte[] PrepareUserCodeReport(byte requiredUserId)
        {
            COMMAND_CLASS_USER_CODE_V2.USER_CODE_REPORT rpt = new COMMAND_CLASS_USER_CODE_V2.USER_CODE_REPORT();
            UserCodeUser currentUser = UserCodeSupportData.Users.Find(user => user.UserId == requiredUserId);

            rpt.userIdentifier = requiredUserId;
            if (currentUser != null)
            {
                rpt.userIdStatus = GetUserStatus(currentUser);
                rpt.userCode = currentUser.UserCode;
                return rpt;
            }
            else if (requiredUserId > UserCodeSupportData.SupportedUsers)
            {
                rpt.userIdStatus = 0xFE;
                rpt.userCode = EmptyCode;
                return rpt;
            }
            return null;
        }
        private byte[] PrepareUserCodeReportCmd(byte[] command)
        {
            COMMAND_CLASS_USER_CODE_V2.USER_CODE_GET userCodeGetCmd = command;
            return PrepareUserCodeReport(userCodeGetCmd.userIdentifier);
        }

        private byte[] PrepareExtUserCodeReport(ushort requiredUserId, bool reportMore)
        {
            if (Version >= 2)
            {
                COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_REPORT rpt = new COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_REPORT();
                UserCodeUser currentUser = UserCodeSupportData.Users.Find(user => user.UserId == requiredUserId);

                if (currentUser == null)
                {
                    rpt.numberOfUserCodes = 0x01;

                    rpt.vg1.Add(new COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_REPORT.TVG1()
                    {
                        userIdentifier = CommandClassSupportUtils.UInt64ToByteField(requiredUserId, 2),
                        userIdStatus = 0xFE,
                        properties1 = new COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_REPORT.TVG1.Tproperties1() { userCodeLength = 0x00 }
                    });
                    rpt.nextUserIdentifier = new byte[] { 0x00, 0x00 };
                }
                else if (reportMore && UserCodeSupportData.MultipleUserCodeReportSup) // MUCR
                {
                    int currentUserPos = UserCodeSupportData.Users.FindIndex(user => user.UserId == requiredUserId);

                    rpt.vg1.Add(GetExtendedUserData(UserCodeSupportData.Users[currentUserPos]));

                    for (int i = 0; i < UserCodeSupportData.SupportedUsers; i++)
                    {
                        if (i > currentUserPos && UserCodeSupportData.Users[i].UserIdStatus != 0)
                        {
                            rpt.vg1.Add(GetExtendedUserData(UserCodeSupportData.Users[i]));
                            rpt.nextUserIdentifier = GetNextUserId(UserCodeSupportData.Users[i].UserId);
                        }
                        if (rpt.vg1.Count == 4) // vg1 must fit in a single Z-Wave frame
                        {
                            break;
                        }
                    }

                    rpt.numberOfUserCodes = Convert.ToByte(rpt.vg1.Count());
                }
                else
                {
                    rpt.numberOfUserCodes = 0x01;

                    rpt.vg1.Add(GetExtendedUserData(currentUser));
                    rpt.nextUserIdentifier = GetNextUserId(currentUser.UserId);
                }
                return rpt;
            }
            return null;
        }

        private byte[] GetNextUserId(ushort currentUserId)
        {
            byte[] nextUserId = new byte[] { 0x00, 0x00 };

            int currentUserPos = UserCodeSupportData.Users.FindIndex(user => user.UserId == currentUserId);
            for (int i = currentUserPos + 1; i < UserCodeSupportData.Users.Count; i++)
            {
                if (UserCodeSupportData.Users[i].UserIdStatus != 0)
                {
                    nextUserId = CommandClassSupportUtils.UInt64ToByteField(UserCodeSupportData.Users[i].UserId, 2);
                    return nextUserId;
                }
            }
            return nextUserId;
        }

        private byte[] PrepareExtUserCodeReportCmd(byte[] command)
        {
            COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_GET userCodeGetCmd = command;
            ushort requiredUserId = (ushort) CommandClassSupportUtils.ByteFieldToUInt64(userCodeGetCmd.userIdentifier, 2);
            return PrepareExtUserCodeReport(requiredUserId, userCodeGetCmd.properties1.reportMore == 0 ? false : true);
        }

        private byte[] PrepareMasterCodeReport()
        {
            if (Version >= 2)
            {
                COMMAND_CLASS_USER_CODE_V2.ADMIN_CODE_REPORT rpt = new COMMAND_CLASS_USER_CODE_V2.ADMIN_CODE_REPORT();

                if (UserCodeSupportData.AdminCode.SequenceEqual(EmptyCode)
                 || UserCodeSupportData.AdminCode.Length > 10   // avoid overflow
                 || !UserCodeSupportData.AdminCodeSup) // MC
                {
                    rpt.properties1.adminCodeLength = 0;
                }
                else
                {
                    rpt.properties1.adminCodeLength = Convert.ToByte(UserCodeSupportData.AdminCode.Length);
                    rpt.adminCode = UserCodeSupportData.AdminCode;
                }
                return rpt;
            }
            return null;
        }

        private byte[] PrepareKeypadModeReport()
        {
            if (Version >= 2)
            {
                COMMAND_CLASS_USER_CODE_V2.USER_CODE_KEYPAD_MODE_REPORT rpt = new COMMAND_CLASS_USER_CODE_V2.USER_CODE_KEYPAD_MODE_REPORT();
                rpt.keypadMode = (byte) UserCodeSupportData.KeypadMode;
                return rpt;
            }
            return null;
        }

        private byte[] PrepareChecksumReport()
        {
            if (Version >= 2 && UserCodeSupportData.UserCodeChecksumSup) // UCC
            {
                COMMAND_CLASS_USER_CODE_V2.USER_CODE_CHECKSUM_REPORT rpt = new COMMAND_CLASS_USER_CODE_V2.USER_CODE_CHECKSUM_REPORT();

                List<byte> checksumInput = new List<byte>();

                foreach (UserCodeUser user in UserCodeSupportData.Users)
                {
                    if (user.UserIdStatus != 0x00)
                    {
                        checksumInput.AddRange(CommandClassSupportUtils.UInt64ToByteField(user.UserId, 2));
                        checksumInput.Add(GetUserStatus(user));
                        checksumInput.AddRange(user.UserCode);
                    }
                }

                if (checksumInput.Count != 0)
                {
                    ushort crc = CommandClassSupportUtils.CalculateCRC16(checksumInput.ToArray());
                    rpt.userCodeChecksum = new[] { (byte)(crc >> 8), (byte)(crc & 0xFF) };
                }
                else
                {
                    rpt.userCodeChecksum = new byte[] { 0x00, 0x00 };
                }
                return rpt;
            }
            return null;
        }

        private SvResultNames HandleUserCodeSetCmd(byte[] command, NodeTag srcNode)
        {
            COMMAND_CLASS_USER_CODE_V2.USER_CODE_SET userCodeSetCmd = command;
            byte newUserIdStatus = (byte)userCodeSetCmd.userIdStatus;
            if (userCodeSetCmd.userIdentifier == 0x00)
            {
                if (newUserIdStatus == (byte)UserCodeUserIdStatusV2.Available)
                {   // Delete all
                    foreach (UserCodeUser user in UserCodeSupportData.Users)
                    {
                        if (user.UserId <= 255) // apply to User IDs 1..255 only (legacy users)
                        {
                            user.UserIdStatus = newUserIdStatus;
                            user.UserCode = EmptyCode;
                        }
                        if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                        {
                            SendUnsolicitedReport(PrepareUserCodeReport((byte)user.UserId), srcNode);
                        }
                    }
                    SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about the current state
                    UserCredentialSupport?.SyncPinCodeCredentials();
                    return SvResultNames.Successful;
                }
                return SvResultNames.Failed;
            }

            UserCodeUser currentUser = UserCodeSupportData.Users.Find(user => user.UserId == userCodeSetCmd.userIdentifier);
            if (currentUser != null)
            {
                if (ValidateUserIdStatus((UserCodeUserIdStatusV2)newUserIdStatus))
                {
                    if (newUserIdStatus == (byte)UserCodeUserIdStatusV2.Available)
                    {
                        currentUser.UserIdStatus = newUserIdStatus;
                        currentUser.UserCode = EmptyCode;
                        if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                        {
                            SendUnsolicitedReport(PrepareUserCodeReport((byte)currentUser.UserId), srcNode);
                        }
                        SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about the current state
                        UserCredentialSupport?.SyncPinCodeCredentials();
                        return SvResultNames.Successful;
                    }
                    if (ValidateUserCode(userCodeSetCmd.userIdentifier, (UserCodeUserIdStatusV2)newUserIdStatus, userCodeSetCmd.userCode.ToList(), false))
                    {
                        currentUser.UserIdStatus = newUserIdStatus;
                        currentUser.UserCode = userCodeSetCmd.userCode.ToArray();
                        if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                        {
                            SendUnsolicitedReport(PrepareUserCodeReport((byte)currentUser.UserId), srcNode);
                        }
                        SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about the current state
                        UserCredentialSupport?.SyncPinCodeCredentials();
                        return SvResultNames.Successful;
                    }
                }
            }
            return SvResultNames.Failed;
        }

        private SvResultNames HandleExtendedUserCodeSetCmd(byte[] command, NodeTag srcNode)
        {
            if (Version >= 2)
            {
                COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_SET extendedUserCodeSetCmd = command;

                if (extendedUserCodeSetCmd.numberOfUserCodes != 0)
                {
                    List<COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_SET.TVG1> userCodesList = extendedUserCodeSetCmd.vg1;

                    if (userCodesList.Count == 1 && userCodesList[0].userIdentifier.SequenceEqual(new byte[] { 0x00, 0x00 }))
                    {   // Delete all
                        foreach (UserCodeUser user in UserCodeSupportData.Users)
                        {
                            user.UserIdStatus = 0x00;
                            user.UserCode = EmptyCode;
                            if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                            {
                                SendUnsolicitedReport(PrepareExtUserCodeReport(user.UserId, false), srcNode);
                            }
                        }
                        SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about the current state
                        UserCredentialSupport?.SyncPinCodeCredentials();
                        return SvResultNames.Successful;
                    }

                    var svResult = SvResultNames.Successful;

                    foreach (COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_SET.TVG1 userCode in userCodesList)
                    {
                        byte newUserIdStatus = (byte)userCode.userIdStatus;
                        ushort requiredUserId = (ushort)CommandClassSupportUtils.ByteFieldToUInt64(userCode.userIdentifier, 2);
                        UserCodeUser currentUser = UserCodeSupportData.Users.Find(user => user.UserId == requiredUserId);

                        if (currentUser == null)
                        {
                            svResult = SvResultNames.Failed;
                            continue; // ignore User Code Block
                        }
                        if (ValidateUserIdStatus((UserCodeUserIdStatusV2)newUserIdStatus)
                         && ValidateUserCode(requiredUserId, (UserCodeUserIdStatusV2)newUserIdStatus, userCode.userCode.ToList(), true))
                        {
                            currentUser.UserIdStatus = newUserIdStatus;
                            currentUser.UserCode = userCode.userCode.ToArray();
                            if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                            {
                                SendUnsolicitedReport(PrepareExtUserCodeReport(currentUser.UserId, false), srcNode);
                            }
                        }
                        else
                        {
                            if (userCode.userCode.Count >= 4 && userCode.userCode.Count <= 10)
                            {
                                svResult = SvResultNames.Failed;
                                continue; // ignore User Code Block, if Count is valid (-> invalid char or duplicate detected)
                            }
                            return SvResultNames.Failed; // report error if Count is invalid
                        }
                        if (!UserCodeSupportData.MultipleUserCodeSetSup)
                        {
                            break; // No MUCS: handle first entry only
                        }
                    }
                    UserCredentialSupport?.SyncPinCodeCredentials();
                    SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about the current state
                    return svResult;
                }
            }
            return SvResultNames.Failed;
        }

        private SvResultNames HandleMasterCodeSetCmd(byte[] command, NodeTag srcNode)
        {
            if (Version >= 2 && UserCodeSupportData.AdminCodeSup) // MC
            {
                COMMAND_CLASS_USER_CODE_V2.ADMIN_CODE_SET masterCodeSetCmd = command;

                if (masterCodeSetCmd.properties1.adminCodeLength != 0
                 && ValidateUserCode(null, UserCodeUserIdStatusV2.Enabled, masterCodeSetCmd.adminCode.ToList(), false)
                 && (UserCodeSupportData.KeypadMode == UserCodeKeypadMode.Normal || UserCodeSupportData.KeypadMode == UserCodeKeypadMode.Vacation))
                {
                    UserCodeSupportData.AdminCode = masterCodeSetCmd.adminCode.ToArray();
                    UserCredentialSupport?.SyncPinCodeCredentials();
                    if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        SendUnsolicitedReport(PrepareMasterCodeReport(), srcNode);
                    }
                    return SvResultNames.Successful;
                }
                if (masterCodeSetCmd.properties1.adminCodeLength == 0 && UserCodeSupportData.AdminCodeDeactivationSup) // MCD
                {
                    UserCodeSupportData.AdminCode = EmptyCode;
                    UserCredentialSupport?.SyncPinCodeCredentials();
                    if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        SendUnsolicitedReport(PrepareMasterCodeReport(), srcNode);
                    }
                    return SvResultNames.Successful;
                }
            }
            return SvResultNames.Failed;
        }

        private SvResultNames HandleKeypadModeSetCmd(byte[] command, NodeTag srcNode)
        {
            if (Version >= 2)
            {
                COMMAND_CLASS_USER_CODE_V2.USER_CODE_KEYPAD_MODE_SET keypadModeSetCmd = command;
                if ((keypadModeSetCmd.keypadMode == 0x00 && UserCodeSupportData.SupportedKeypadModesList.Contains(UserCodeKeypadMode.Normal))
                 || (keypadModeSetCmd.keypadMode == 0x01 && UserCodeSupportData.SupportedKeypadModesList.Contains(UserCodeKeypadMode.Vacation))
                 || (keypadModeSetCmd.keypadMode == 0x02 && UserCodeSupportData.SupportedKeypadModesList.Contains(UserCodeKeypadMode.Privacy))
//              // Do not activate the following line, until the Keypad Mode 0x03 (Locked Out) is implemented!
//               || (keypadModeSetCmd.keypadMode == 0x03 && UserCodeSupportData.SupportedKeypadModesList.Contains(UserCodeKeypadMode.LockedOut))
                 )
                {
                    UserCodeSupportData.KeypadMode = (UserCodeKeypadMode) (byte) keypadModeSetCmd.keypadMode;
                    if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        SendUnsolicitedReport(PrepareKeypadModeReport(), srcNode);
                    }
                    return SvResultNames.Successful;
                }
            }
            return SvResultNames.Failed;
        }

        private void InitializeUsersForCTT()
        {
            for (ushort i = 1; i <= UserCodeSupportData.SupportedUsers; i++)
            {
                UserCodeSupportData.Users.Add(new UserCodeUser(i, UserCodeUserIdStatusV2.Available, EmptyCode));
            }
        }
        private void InitializeCapabilitiesForCTT()
        {
            // Bitmask in Supported Report: Bit 0 = Available; Bit 1 = Enabled/Grant Access; Bit 2 = Disabled; Bit 3 = Messaging (not implemented); Bit 4 = Passage Mode
            UserCodeSupportData.SupportedUserIdStatusListV1 = new List<UserCodeSupportedUserIdStatusV1>()
            {
                UserCodeSupportedUserIdStatusV1.Available,
                UserCodeSupportedUserIdStatusV1.Occupied
            };

            // Bitmask in Supported Report: Bit 0 = Available; Bit 1 = Enabled/Grant Access; Bit 2 = Disabled; Bit 3 = Messaging (not implemented); Bit 4 = Passage Mode
            UserCodeSupportData.SupportedUserIdStatusListV2 = new List<UserCodeSupportedUserIdStatusV2>()
            {
                UserCodeSupportedUserIdStatusV2.Available,
                UserCodeSupportedUserIdStatusV2.Enabled,
                UserCodeSupportedUserIdStatusV2.Disabled,
                UserCodeSupportedUserIdStatusV2.PassageMode,
            };
            // Bitmask in Supported Report: Bit 0 = Normal mode; Bit 1 = Vacation mode; Bit 2 = Privacy Mode; Bit 3 = Locked Out Mode (not implemented)
            UserCodeSupportData.SupportedKeypadModesList = new List<UserCodeKeypadMode>()
            {
                UserCodeKeypadMode.Normal,
                UserCodeKeypadMode.Vacation,
                UserCodeKeypadMode.Privacy,
            };
            // SupportedKeysList contains all supported ASCII codes. Set: 0...9
            UserCodeSupportData.SupportedKeysList = new byte[] { 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 };
        }

        private COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_REPORT.TVG1 GetExtendedUserData(UserCodeUser user)
        {
            if (user.UserIdStatus != 0x00)
            {
                return new COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_REPORT.TVG1()
                {
                    userIdentifier = CommandClassSupportUtils.UInt64ToByteField(user.UserId, 2),
                    userIdStatus = GetUserStatus(user),
                    properties1 = new COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_REPORT.TVG1.Tproperties1()
                    { 
                        userCodeLength = Convert.ToByte(user.UserCode.Count())
                    },
                    userCode = user.UserCode
                };
            }
            else
            {
                return new COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_REPORT.TVG1()
                {
                    userIdentifier = CommandClassSupportUtils.UInt64ToByteField(user.UserId, 2),
                    userIdStatus = 0x00,
                    properties1 = new COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_REPORT.TVG1.Tproperties1() { userCodeLength = 0x00 }
                };
            }
        }

        private bool ValidateUserCode(ushort? userId, UserCodeUserIdStatusV2 userIdStatus, List<byte> userCode, bool isExtended)
        {
            if (userIdStatus == UserCodeUserIdStatusV2.Available)
            {
                if (!isExtended)
                {
                    return userCode.SequenceEqual(EmptyCode);
                }
                else
                {
                    return userCode.Count == 0;
                }
            }
            else if ((byte) userIdStatus > (byte) UserCodeUserIdStatusV2.PassageMode)
            {
                return false;
            }
            // Messaging is not supported, but there is no real interaction with it (neither enter a code, nor interaction with a lock)
            // So we deceided to allow it for compatibility reasons regarding to User Credentials Pin Code Mapping
            //else if (userIdStatus == UserCodeUserIdStatusV2.Messaging) // SupportedUserIdStatusList: Messaging is not supported
            //{
            //    return false;
            //}
            else
            {
                if (Version == 1 && userIdStatus > UserCodeUserIdStatusV2.Disabled)
                {
                    return false;
                }

                if (userCode.Count < 4 || userCode.Count > 10)
                {
                    return false;
                }

                foreach (byte codeByte in userCode)
                {
                    if (! UserCodeSupportData.SupportedKeysList.Contains(codeByte))
                    {
                        return false;
                    }
                }

                foreach (UserCodeUser user in UserCodeSupportData.Users)
                {
                    if (user.UserId != userId && user.UserCode != null && user.UserCode.SequenceEqual(userCode))
                    {
                        return false;
                    }
                }

                if (userId != null && UserCodeSupportData.AdminCode.SequenceEqual(userCode))
                {
                    return false;
                }

            }
            return true;
        }

        private bool ValidateUserIdStatus(UserCodeUserIdStatusV2 userIdStatus)
        {
            var supportedUserIds = GetSupportedUserStatusIds();

            if ((userIdStatus == UserCodeUserIdStatusV2.Available && supportedUserIds.Contains((byte)UserCodeUserIdStatusV2.Available))
             || (userIdStatus == UserCodeUserIdStatusV2.Enabled && supportedUserIds.Contains((byte)UserCodeUserIdStatusV2.Enabled))
             || (userIdStatus == UserCodeUserIdStatusV2.Disabled && supportedUserIds.Contains((byte)UserCodeUserIdStatusV2.Disabled))
             || (userIdStatus == UserCodeUserIdStatusV2.Messaging && supportedUserIds.Contains((byte)UserCodeUserIdStatusV2.Messaging) && Version >= 2)
             || (userIdStatus == UserCodeUserIdStatusV2.PassageMode && supportedUserIds.Contains((byte)UserCodeUserIdStatusV2.PassageMode) && Version >= 2))
            {
                return true;
            }
            return false;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != CommandClassId)  
            {
                supervisionReport.status = (byte) SvStatusNames.NoSupport;
                return;
            }

            supervisionReport.status = (byte) SvStatusNames.Fail;  // FAIL as default
            switch (supervisionGet.encapsulatedCommand[1])
            {
                case COMMAND_CLASS_USER_CODE_V2.USER_CODE_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    }
                    break;
                case COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    }
                    break;
                case COMMAND_CLASS_USER_CODE_V2.ADMIN_CODE_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    }
                    break;
                case COMMAND_CLASS_USER_CODE_V2.USER_CODE_KEYPAD_MODE_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    }
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }

        private byte GetUserStatus(UserCodeUser user)
        {
            byte userStatus = (byte)UserCodeUserIdStatusV1.StatusNotAvailable;
            
            if (Version== 1) 
            {
                if (Enum.IsDefined(typeof(UserCodeUserIdStatusV1), user.UserIdStatus))
                {
                    userStatus = user.UserIdStatus;
                }
            }
            else
            {
                if (Enum.IsDefined(typeof(UserCodeUserIdStatusV2), user.UserIdStatus))
                {
                    userStatus = user.UserIdStatus;
                }
            }
            return userStatus;
        }

        private List<byte> GetSupportedUserStatusIds() 
        {
            return GetSupportedUserStatusIds(UserCodeSupportData);
        }

        private List<byte> GetSupportedUserStatusIds(UserCodeSupportData supportData)
        {
            List<byte> supportedIds;
            if (Version == 1)
            {
                supportedIds = supportData?.SupportedUserIdStatusListV1?.Cast<byte>().ToList();
            }
            else
            {
                supportedIds = supportData?.SupportedUserIdStatusListV2?.Cast<byte>().ToList();
            }

            return supportedIds == null ? new List<byte>() : supportedIds;
        }

        /// <summary>
        /// Synchronize User Codes with User Credentials where User Credential CC is leading
        /// </summary>
        /// <param name="sendUnsolicitedReports"></param>
        public void SyncUserCodes(bool sendUnsolicitedReports = true)
        {
            if (UserCredentialSupport == null)
            {
                return;
            }

            if (UserCredentialSupport.CommandClassSupportData is UserCredentialSupportData credentialSupportData)
            {

                var pinCodeCredentials = credentialSupportData.UserCredentials.FindAll(x => x.Type == CredentialType.PinCode);

                foreach (var pinCodeCredential in pinCodeCredentials)
                {
                    var correspondingUserCredentialUser = credentialSupportData.UserSlots.Find(x => x.UserUniqeId == pinCodeCredential.UserUniqeId);

                    if (correspondingUserCredentialUser == null)
                    {
                        continue;
                    }

                    var userCodeSlot = UserCodeSupportData.Users.Find(x => x.UserId == pinCodeCredential.SlotId);

                    if (userCodeSlot != null)
                    {
                        bool hasChanges = false;

                        if (Version == 1)
                        {
                            var mappedStatus = correspondingUserCredentialUser.MapToUserCodeStatusV1();
                            if (userCodeSlot.UserIdStatusV1 != mappedStatus)
                            {
                                userCodeSlot.UserIdStatusV1 = mappedStatus;
                                hasChanges = true;
                            }
                        }
                        else
                        {
                            var mappedStatus = correspondingUserCredentialUser.MapToUserCodeStatus();
                            if (userCodeSlot.UserIdStatusV2 != mappedStatus)
                            {
                                userCodeSlot.UserIdStatusV2 = mappedStatus;
                                hasChanges = true;
                            }
                        }

                        if ((userCodeSlot.UserCode != null && pinCodeCredential.Data != null)
                            && userCodeSlot.UserCode.SequenceEqual(pinCodeCredential.Data) == false)
                        {
                            userCodeSlot.UserCode = pinCodeCredential.Data;
                            hasChanges = true;
                        }

                        if (hasChanges && sendUnsolicitedReports)
                        {
                            SendUnsolicitedReport(PrepareExtUserCodeReport(userCodeSlot.UserId, false), NodeTag.Empty);
                        }
                    }
                }

                foreach (var userCode in UserCodeSupportData.Users)
                {
                    if (userCode.UserIdStatusV2 != UserCodeUserIdStatusV2.Available)
                    {
                        var correspondingUserCredential = credentialSupportData.UserCredentials.Find(x => x.Type == CredentialType.PinCode && x.SlotId == userCode.UserId);

                        if (correspondingUserCredential == null)
                        {
                            userCode.UserIdStatusV2 = UserCodeUserIdStatusV2.Available;
                            userCode.UserCode = EmptyCode;

                            if (sendUnsolicitedReports)
                            {
                                SendUnsolicitedReport(PrepareExtUserCodeReport(userCode.UserId, false), NodeTag.Empty);
                            }
                        }
                    }
                }


                if (UserCodeSupportData.AdminCodeSup && credentialSupportData.AdminCodeSupport)
                {
                    if (string.IsNullOrEmpty(credentialSupportData.AdminCode))
                    {
                        UserCodeSupportData.AdminCode = EmptyCode;
                    }
                    else
                    {
                        byte[] adminPinCode = UserCredentialSupport.DecodeString(credentialSupportData.AdminCode, UserNameEncoding.ASCII);
                        if (UserCodeSupportData.AdminCode != adminPinCode)
                        {
                            UserCodeSupportData.AdminCode = adminPinCode;

                            SendUnsolicitedReport(PrepareMasterCodeReport(), NodeTag.Empty);
                        }
                    }

                }
            }
        }

        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is UserCodeSupportData newData)
            {
                if (UserCodeSupportData.SupportedUsers != newData.SupportedUsers)
                {
                    //UserCodeSupportData.SupportedUsers = newData.SupportedUsers;
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareUserNumberReport(), NodeTag.Empty);
                    }
                }

                bool isSupportedUserIdStatusListChanged = !CommandClassSupportUtils.ListElementsEqual(GetSupportedUserStatusIds(), GetSupportedUserStatusIds(newData));

                bool isSupportedKeypadModesListChanged = !CommandClassSupportUtils.ListElementsEqual(UserCodeSupportData.SupportedKeypadModesList, newData.SupportedKeypadModesList);

                bool isSupportedKeysListChanged = !CommandClassSupportUtils.ListElementsEqual(UserCodeSupportData.SupportedKeysList, newData.SupportedKeysList);

                if (UserCodeSupportData.AdminCodeSup != newData.AdminCodeSup
                 || UserCodeSupportData.AdminCodeDeactivationSup != newData.AdminCodeDeactivationSup
                 || UserCodeSupportData.UserCodeChecksumSup != newData.UserCodeChecksumSup
                 || UserCodeSupportData.MultipleUserCodeReportSup != newData.MultipleUserCodeReportSup
                 || UserCodeSupportData.MultipleUserCodeSetSup != newData.MultipleUserCodeSetSup
                 || isSupportedUserIdStatusListChanged
                 || isSupportedKeypadModesListChanged
                 || isSupportedKeysListChanged)
                {
                    UserCodeSupportData.AdminCodeSup = newData.AdminCodeSup;
                    UserCodeSupportData.AdminCodeDeactivationSup = newData.AdminCodeDeactivationSup;
                    UserCodeSupportData.UserCodeChecksumSup = newData.UserCodeChecksumSup;
                    UserCodeSupportData.MultipleUserCodeReportSup = newData.MultipleUserCodeReportSup;
                    UserCodeSupportData.MultipleUserCodeSetSup = newData.MultipleUserCodeSetSup;
                    UserCodeSupportData.SupportedUserIdStatusListV1 = newData.SupportedUserIdStatusListV1;
                    UserCodeSupportData.SupportedUserIdStatusListV2 = newData.SupportedUserIdStatusListV2;
                    UserCodeSupportData.SupportedKeypadModesList = newData.SupportedKeypadModesList;
                    UserCodeSupportData.SupportedKeysList = newData.SupportedKeysList;
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    { 
                        SendUnsolicitedReport(PrepareCapabilitiesReport(), NodeTag.Empty);
                    }
                }

                if (UserCodeSupportData.KeypadMode != newData.KeypadMode)
                {
                    UserCodeSupportData.KeypadMode = newData.KeypadMode;
                    if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        SendUnsolicitedReport(PrepareKeypadModeReport(), NodeTag.Empty);
                    }
                }

                if (!UserCodeSupportData.AdminCode.SequenceEqual(newData.AdminCode))
                {
                    UserCodeSupportData.AdminCode = newData.AdminCode;
                    if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        SendUnsolicitedReport(PrepareMasterCodeReport(), NodeTag.Empty);
                    }
                }

                bool isAnyUserChanged = false;
                List<UserCodeUser> usersRemoved = UserCodeSupportData.Users.FindAll(x => newData.Users.Find(y => y.UserId == x.UserId) == null);
              //List<UserCodeUser> usersAdded   = newData.Users.FindAll(x => UserCodeSupportData.Users.Find(y => y.UserId == x.UserId) == null);
                if (usersRemoved.Count > 0 /*|| userAdded.Count > 0*/)
                {
                    usersRemoved.ForEach(x => UserCodeSupportData.Users.Remove(x));
                    isAnyUserChanged = true;
                }
                foreach (UserCodeUser newUser in newData.Users)
                {
                    bool isUserChanged = false;
                    if (UserCodeSupportData.Users.Find(x => x.UserId == newUser.UserId) is UserCodeUser currentUser)
                    {   // It is an existing User, check all components for changes
                        if (currentUser.UserIdStatus != newUser.UserIdStatus
                         || !currentUser.UserCode.SequenceEqual(newUser.UserCode))
                        {
                            isUserChanged = true;
                            isAnyUserChanged = true;
                            UserCodeSupportData.Users.Remove(currentUser);
                            UserCodeSupportData.Users.Add(newUser);
                        }
                    }
                    else // It is a new User, add it
                    {
                        isUserChanged = true;
                        isAnyUserChanged = true;
                        UserCodeSupportData.Users.Add(newUser);
                    }
                    // Parsing of this user done. Now send [Ext] User Code Report.
                    if (isUserChanged && ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        if (newUser.UserId <= 255)
                        {
                            SendUnsolicitedReport(PrepareUserCodeReport((byte)newUser.UserId), NodeTag.Empty);
                        }
                        else
                        {
                            SendUnsolicitedReport(PrepareExtUserCodeReport(newUser.UserId, false), NodeTag.Empty);
                        }
                    }
                }
                // Parsing of all users done. Finally send Checksum Report.
                if (isAnyUserChanged && ReportTypes.HasFlag(ReportTypes.OnStateChange))
                {
                    SendUnsolicitedReport(PrepareChecksumReport(), NodeTag.Empty);
                }

                // just sort the users by their id for the EDE UI
                UserCodeSupportData.Users.Sort();

                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class UserCodeUser : INestedItem, IComparable<UserCodeUser> 
    {
        public ushort UserId;

        public byte UserIdStatus;

        // only used for EDE UI. see interface for useage notes.
        [CustomDataParameter(1, "User Id", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Integer, EditType.Readonly)]
        [JsonIgnore]
        public int NestedId
        {
            get => UserId;
            set
            {
                UserId = (ushort)value;
            }
        }

        [JsonIgnore]
        [CustomDataParameter(1, 1, "Status", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config | EditType.Edit, ItemsSource = typeof(UserCodeUserIdStatusV1))]
        public UserCodeUserIdStatusV1 UserIdStatusV1
        {
            get 
            {
                if (Enum.IsDefined(typeof(UserCodeUserIdStatusV1), UserIdStatus))
                {
                    return (UserCodeUserIdStatusV1)UserIdStatus;
                }
                return UserCodeUserIdStatusV1.StatusNotAvailable;
            }
            set 
            {
                if (Enum.IsDefined(typeof(UserCodeUserIdStatusV1), value))
                {
                    UserIdStatus = (byte)value;
                }
                else
                {
                    UserIdStatus = (byte)UserCodeUserIdStatusV1.StatusNotAvailable;
                }
            }
        }

        [JsonIgnore]
        [CustomDataParameter(2, "Status", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config | EditType.Edit, ItemsSource = typeof(UserCodeUserIdStatusV2))]
        public UserCodeUserIdStatusV2 UserIdStatusV2
        {
            get
            {
                if (Enum.IsDefined(typeof(UserCodeUserIdStatusV2), UserIdStatus))
                {
                    return (UserCodeUserIdStatusV2)UserIdStatus;
                }
                return UserCodeUserIdStatusV2.StatusNotAvailable;
            }
            set
            {
                if (Enum.IsDefined(typeof(UserCodeUserIdStatusV2), value))
                {
                    UserIdStatus = (byte)value;
                }
                else
                {
                    UserIdStatus = (byte)UserCodeUserIdStatusV2.StatusNotAvailable;
                }
            }
        }

        [CustomDataParameter(1, "User Code", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.HexByteArray, EditType.Config | EditType.Edit)]
        public byte[] UserCode { get; set; }

        //[JsonIgnore]
        //[CustomDataParameter(1, "User Code", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Text_127, EditType.Config | EditType.Edit)]
        //public string UserCodeString
        //{
        //    get { return Encoding.ASCII.GetString(UserCode.ToArray()); }
        //    set { UserCode = Encoding.ASCII.GetBytes(value).ToList(); }
        //}

        public UserCodeUser()
        {
        }
        public UserCodeUser(ushort userId, UserCodeUserIdStatusV1 userIdStatus, byte[] userCode)
        {
            UserId = userId;
            UserIdStatus = (byte)userIdStatus;
            UserCode = userCode;
        }
        public UserCodeUser(ushort userId, UserCodeUserIdStatusV2 userIdStatus, byte[] userCode)
        {
            UserId = userId;
            UserIdStatus = (byte)userIdStatus;
            UserCode = userCode;
        }

        public int CompareTo(UserCodeUser other)
        {
            return UserId.CompareTo(other?.UserId);
        }
    }

    public class UserCodeSupportData : CommandClassSupportData, INodeItem
    {
        // Internals accessible from TestCases or EDE
        [NestedControl(1, "Users")]
        public List<UserCodeUser> Users { get; set; } = new List<UserCodeUser>();

        // V1+
        [JsonIgnore]
        public ushort SupportedUsers { get { return (ushort)(Users != null ? Users.Count : 0); } }//{ get; set; } = 10;            // Supported Users: V1: 1...255, V2: 1...65535

        // V2+
        [CustomDataParameter(2, "Keypad Mode", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config | EditType.Edit, ItemsSource = typeof(UserCodeKeypadMode))]
        public UserCodeKeypadMode KeypadMode { get; set; } = UserCodeKeypadMode.Normal; // 0x00 = Normal; 0x01 = Vacation; 0x02 = Privacy; 0x03 = Locked out

        [CustomDataParameter(1, "Admin Code", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.HexByteArray, EditType.Config | EditType.Edit)]
        public byte[] AdminCode { get; set; } = new byte[] { };
       
        // V2+ Capabilities
        [ParameterSeparator(2, "User Code Capabilities Report")]
        [CustomDataParameter(2, "Admin Code Supported", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config)]
        public bool AdminCodeSup { get; set; } = true;             // AC
        
        [CustomDataParameter(2, "Admin Code Deactivation Supported", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config)]
        public bool AdminCodeDeactivationSup { get; set; } = true; // ACD

        [JsonIgnore]
        public List<UserCodeSupportedUserIdStatusV1> SupportedUserIdStatusListV1 { get; set; } = new List<UserCodeSupportedUserIdStatusV1>()
        {
            UserCodeSupportedUserIdStatusV1.Available, UserCodeSupportedUserIdStatusV1.Occupied, UserCodeSupportedUserIdStatusV1.Reserved
        };

        [CustomDataParameter(2, "Supported User Statuses", CommandClassesImpl.UiControl.ListCheckBox, CommandClassesImpl.InputType.Enum, EditType.Config)]
        public List<UserCodeSupportedUserIdStatusV2> SupportedUserIdStatusListV2 { get; set; } = new List<UserCodeSupportedUserIdStatusV2>();

        [CustomDataParameter(2, "User Code Checksum Supported", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config)]
        public bool UserCodeChecksumSup { get; set; } = true;       // UCC

        [CustomDataParameter(2, "Multiple User Code Report Supported", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config)]
        public bool MultipleUserCodeReportSup { get; set; } = true; // MUCR

        [CustomDataParameter(2, "Multiple User Code Set Supported", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config)]
        public bool MultipleUserCodeSetSup { get; set; } = true;    // MUCS

        // Bitmask in Supported Report: Bit 0 = Normal mode; Bit 1 = Vacation mode; Bit 2 = Privacy Mode; Bit 3 = Locked Out Mode (not implemented)
        [CustomDataParameter(2, "Supported Keypad Modes", CommandClassesImpl.UiControl.ListCheckBox, CommandClassesImpl.InputType.Enum, EditType.Config)]
        public List<UserCodeKeypadMode> SupportedKeypadModesList { get; set; } = new List<UserCodeKeypadMode>();

        // SupportedKeysList contains all supported ASCII codes.
        [CustomDataParameter(1, "Supported Keys", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.HexByteArray, EditType.Config)]
        public byte[] SupportedKeysList { get; set; } = new byte[] { };

        [JsonIgnore]
        public IEnumerable<INestedItem> ChildItems => Users;
    }

    #region Enums
    public enum UserCodeKeypadMode : byte
    {
        [Description("0x00 Normal")]
        Normal = 0x00,
        [Description("0x01 Vacation")]
        Vacation = 0x01,
        [Description("0x02 Privacy")]
        Privacy = 0x02,
        [Description("0x03 Locked Out")]
        LockedOut = 0x03
    }

    public enum UserCodeUserIdStatusV1 : byte
    {
        [Description("0x00 Available")]
        Available = 0x00,
        [Description("0x01 Occupied")]
        Occupied = 0x01,
        [Description("0x02 Reserved")]
        Reserved = 0x02,
        [Description("0xFE Status not available")]
        StatusNotAvailable = 0xFE
    };

    public enum UserCodeUserIdStatusV2 : byte
    {
        [Description("0x00 Available")]
        Available = 0x00,
        [Description("0x01 Enabled")]
        Enabled = 0x01,
        [Description("0x02 Disabled")]
        Disabled = 0x02,
        [Description("0x03 Messaging")]
        Messaging = 0x03,
        [Description("0x04 Passage mode")]
        PassageMode = 0x04,
        [Description("0xFE Status not available")]
        StatusNotAvailable = 0xFE
    };

    public enum UserCodeSupportedUserIdStatusV1 : byte
    {
        [Description("0x00 Available")]
        Available = 0x00,
        [Description("0x01 Occupied")]
        Occupied = 0x01,
        [Description("0x02 Reserved")]
        Reserved = 0x02,
    };

    public enum UserCodeSupportedUserIdStatusV2 : byte
    {
        [Description("0x00 Available")]
        Available = 0x00,
        [Description("0x01 Enabled")]
        Enabled = 0x01,
        [Description("0x02 Disabled")]
        Disabled = 0x02,
        [Description("0x03 Messaging")]
        Messaging = 0x03,
        [Description("0x04 Passage mode")]
        PassageMode = 0x04,
    };

    #endregion // Enums

    public static class UserCodeExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userCodeUser"></param>
        /// <param name="userCodeVersion"></param>
        /// <param name="userType"></param>
        /// <returns></returns>
        public static bool MapToUserCredentialUserType(this UserCodeUser userCodeUser, byte userCodeVersion, UserSlot userCredentialUser, bool newlyCreatedUser)
        {
            bool hasChanges = false;

            if (userCodeUser == null || userCredentialUser == null)
            {
                return false;
            }

            if (userCodeVersion == 1)
            {
                switch (userCodeUser.UserIdStatusV1)
                {
                    // this is just a fallback, the correct handling is done by the Sync Method itself
                    case UserCodeUserIdStatusV1.Available:
                        break;

                    case UserCodeUserIdStatusV1.Occupied:
                        if (newlyCreatedUser)
                        {
                            userCredentialUser.Type = UserCredentialUserType.GeneralUser;
                        }
                        if (userCredentialUser.ActiveState != UserCredentialUserActiveState.OccupiedEnabled)
                        {
                            userCredentialUser.ActiveState = UserCredentialUserActiveState.OccupiedEnabled;
                            hasChanges = true;
                        }
                        break;

                    case UserCodeUserIdStatusV1.Reserved:
                    case UserCodeUserIdStatusV1.StatusNotAvailable:
                        if (newlyCreatedUser)
                        {
                            userCredentialUser.Type = UserCredentialUserType.GeneralUser;
                        }
                        if (userCredentialUser.ActiveState != UserCredentialUserActiveState.OccupiedDisabled)
                        {
                            userCredentialUser.ActiveState = UserCredentialUserActiveState.OccupiedDisabled;
                            hasChanges = true;
                        }
                        break;
                }
            } 
            else
            {
                switch (userCodeUser.UserIdStatusV2)
                {
                    case UserCodeUserIdStatusV2.Available:
                        break;

                    case UserCodeUserIdStatusV2.Enabled:
                    case UserCodeUserIdStatusV2.PassageMode:
                        if (newlyCreatedUser)
                        {
                            userCredentialUser.Type = UserCredentialUserType.GeneralUser;
                        }
                        if (userCredentialUser.ActiveState != UserCredentialUserActiveState.OccupiedEnabled)
                        {
                            userCredentialUser.ActiveState = UserCredentialUserActiveState.OccupiedEnabled;
                            hasChanges = true;
                        }
                        break;

                    case UserCodeUserIdStatusV2.Disabled:
                        if (newlyCreatedUser)
                        {
                            userCredentialUser.Type = UserCredentialUserType.GeneralUser;
                        }
                        if (userCredentialUser.ActiveState != UserCredentialUserActiveState.OccupiedDisabled)
                        {
                            userCredentialUser.ActiveState = UserCredentialUserActiveState.OccupiedDisabled;
                            hasChanges = true;
                        }
                        break;

                    case UserCodeUserIdStatusV2.Messaging:
                        if (userCredentialUser.Type != UserCredentialUserType.NonAccessUser)
                        {
                            userCredentialUser.Type = UserCredentialUserType.NonAccessUser;
                            hasChanges = true;
                        }
                        if (userCredentialUser.ActiveState != UserCredentialUserActiveState.OccupiedEnabled)
                        {
                            userCredentialUser.ActiveState = UserCredentialUserActiveState.OccupiedEnabled;
                            hasChanges = true;
                        }
                        break;
                }
            }
            return hasChanges;
        }
    }

}
