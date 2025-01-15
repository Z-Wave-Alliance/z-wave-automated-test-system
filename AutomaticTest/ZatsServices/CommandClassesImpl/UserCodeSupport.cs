// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Enums;
using ZWave.Devices;
using Utils;
using System.Linq;
using System;
using ZWave;
using ZWave.BasicApplication;

namespace Zats.Services
{
    // ToDo (nice to have): Support for variable Keymaps (UserCodeSupportData.SupportedKeysBitMask)
    // ToDo (clarify): Interaction with DoorLockCC to simulate Keypad entries (Interactive_CheckUnlockByUserCode) and check User Codes
    // ToDo: Logging ?

    public class UserCodeSupport : CommandClassSupport
    {
        //private readonly string CommandClassName = "User Code CC";
        //private string EndPointName = "";
        //private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private UserCodeSupportData UserCodeSupportData { get; set; }

        public UserCodeSupport()
            : this(new UserCodeSupportData() { })
        { }

        public UserCodeSupport(UserCodeSupportData userCodeSupportData)
            : base(COMMAND_CLASS_USER_CODE.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 2;
            UserCodeSupportData = userCodeSupportData;
            InitializeUsers();
        }

        protected override void OnHandledDelayed(DataReceivedUnit ou)
        {
            ou.SetNextActionItems();
            if (DstNode.EndPointId == ReceivedAchData.DstNode.EndPointId)
            {
                var sendDstNode = ReceivedAchData.DstNode;
                var sendSrcNode = ReceivedAchData.SrcNode;
                byte[] command = ReceivedAchData.Command;
                var scheme = (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme;
                bool isResponseAllowed = IsResponseAllowed(Network, command, scheme, Secure);

                ResultForSv = SvResultNames.Undefined;
                if (isResponseAllowed && command != null && command.Length > 1 && UserCodeSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_USER_CODE_V2.USER_CODE_SET.ID)
                    {
                        ResultForSv = HandleUserCodeSet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.USER_CODE_GET.ID)
                    {
                        rpt = PrepareUserCodeReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.USERS_NUMBER_GET.ID)
                    {
                        rpt = PrepareUserNumberReport();
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_SET.ID && Version >= 2)
                    {
                        ResultForSv = HandleExtendedUserCodeSet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_GET.ID && Version >= 2)
                    {
                        rpt = PrepareExtUserCodeReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.USER_CODE_CAPABILITIES_GET.ID && Version >= 2)
                    {
                        rpt = PrepareCapabilitiesReport();
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.ADMIN_CODE_SET.ID && Version >= 2 && UserCodeSupportData.AdminCodeSup) // MC
                    {
                        ResultForSv = HandleAdminCodeSet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.ADMIN_CODE_GET.ID && Version >= 2)
                    {
                        rpt = PrepareAdminCodeReport();
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.USER_CODE_KEYPAD_MODE_SET.ID && Version >= 2)
                    {
                        ResultForSv = HandleKeypadModeSet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.USER_CODE_KEYPAD_MODE_GET.ID && Version >= 2)
                    {
                        COMMAND_CLASS_USER_CODE_V2.USER_CODE_KEYPAD_MODE_REPORT data = new COMMAND_CLASS_USER_CODE_V2.USER_CODE_KEYPAD_MODE_REPORT();
                        data.keypadMode = UserCodeSupportData.KeypadMode;
                        rpt = data;
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CODE_V2.USER_CODE_CHECKSUM_GET.ID && Version >= 2 && UserCodeSupportData.UserCodeChecksumSup) // UCC
                    {
                        rpt = PrepareChecksumReport();
                    }

                    if (rpt != null)
                    {
                        ApiOperation sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command);
                }
            }
        }

        private byte[] PrepareCapabilitiesReport()
        {
            COMMAND_CLASS_USER_CODE_V2.USER_CODE_CAPABILITIES_REPORT rpt = new COMMAND_CLASS_USER_CODE_V2.USER_CODE_CAPABILITIES_REPORT();

            rpt.properties1.acSupport = (byte) (UserCodeSupportData.AdminCodeSup ? 0x01 : 0x00);
            rpt.properties1.acdSupport = (byte) (UserCodeSupportData.AdminCodeDeactivationSup ? 0x01 : 0x00);
            rpt.supportedUserIdStatusBitMask = CommandClassSupportUtils.CreateBitmaskFromList(UserCodeSupportData.SupportedUserIdStatusList, 0);   // Bit 0 represents type 0
            rpt.properties1.supportedUserIdStatusBitMaskLength = (byte) rpt.supportedUserIdStatusBitMask.Count;
            rpt.properties2.uccSupport = (byte) (UserCodeSupportData.UserCodeChecksumSup ? 0x01 : 0x00);
            rpt.properties2.mucrSupport = (byte) (UserCodeSupportData.MultipleUserCodeReportSup ? 0x01 : 0x00);
            rpt.properties2.mucsSupport = (byte) (UserCodeSupportData.MultipleUserCodeSetSup ? 0x01 : 0x00);
            rpt.supportedKeypadModesBitMask = CommandClassSupportUtils.CreateBitmaskFromList(UserCodeSupportData.SupportedKeypadModesList, 0);   // Bit 0 represents type 0
            rpt.properties2.supportedKeypadModesBitMaskLength = (byte) rpt.supportedKeypadModesBitMask.Count;
            rpt.supportedKeysBitMask = CommandClassSupportUtils.CreateBitmaskFromList(UserCodeSupportData.SupportedKeysList, 0); ;
            rpt.properties3.supportedKeysBitMaskLength = (byte) rpt.supportedKeysBitMask.Count;
            return rpt;
        }

        private byte[] PrepareUserNumberReport()
        {
            if (Version == 1)
            {
                COMMAND_CLASS_USER_CODE.USERS_NUMBER_REPORT rpt = new COMMAND_CLASS_USER_CODE.USERS_NUMBER_REPORT() { };
                if (UserCodeSupportData.SupportedUsers <= 255)
                    rpt.supportedUsers = Convert.ToByte(UserCodeSupportData.SupportedUsers);
                else
                    rpt.supportedUsers = 255;
                return rpt;
            }
            if (Version >= 2)
            {
                COMMAND_CLASS_USER_CODE_V2.USERS_NUMBER_REPORT rpt = new COMMAND_CLASS_USER_CODE_V2.USERS_NUMBER_REPORT() { };
                if (UserCodeSupportData.SupportedUsers <= 255)
                    rpt.supportedUsers = Convert.ToByte(UserCodeSupportData.SupportedUsers);
                else
                    rpt.supportedUsers = 255;
                rpt.extendedSupportedUsers = new byte[] { Convert.ToByte(UserCodeSupportData.SupportedUsers / 256), Convert.ToByte(UserCodeSupportData.SupportedUsers % 256) };
                return rpt;
            }
            return null;
        }

        private byte[] PrepareUserCodeReport(byte[] command)
        {
            COMMAND_CLASS_USER_CODE_V2.USER_CODE_GET userCodeGetCmd = command;
            UserCodeUser currentUser = UserCodeSupportData.Users.Find(user => user.userId[1] == userCodeGetCmd.userIdentifier && user.userId[0] == 0x00);

            if (currentUser != null)
            {
                COMMAND_CLASS_USER_CODE_V2.USER_CODE_REPORT rpt = new COMMAND_CLASS_USER_CODE_V2.USER_CODE_REPORT()
                {
                    userIdentifier = currentUser.userId[1],
                    userIdStatus = (byte) currentUser.userIdStatus,
                    userCode = currentUser.userCode
                };
                return rpt;
            }
            else if (userCodeGetCmd.userIdentifier > UserCodeSupportData.SupportedUsers)
            {
                COMMAND_CLASS_USER_CODE_V2.USER_CODE_REPORT rpt = new COMMAND_CLASS_USER_CODE_V2.USER_CODE_REPORT()
                {
                    userIdentifier = userCodeGetCmd.userIdentifier,
                    userIdStatus = 0xFE,
                    userCode = new byte[] { 0x00, 0x00, 0x00, 0x00 }
                };
                return rpt;
            }
            return null;
        }

        private byte[] PrepareExtUserCodeReport(byte[] command)
        {
            COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_GET userCodeGetCmd = command;
            COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_REPORT rpt = new COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_REPORT();
            UserCodeUser currentUser = UserCodeSupportData.Users.Find(user => user.userId.SequenceEqual(userCodeGetCmd.userIdentifier));

            if (currentUser == null)
            {
                rpt.numberOfUserCodes = 0x01;

                rpt.vg1.Add(new COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_REPORT.TVG1()
                {
                    userIdentifier = userCodeGetCmd.userIdentifier,
                    userIdStatus = 0xFE,
                    properties1 = new COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_REPORT.TVG1.Tproperties1() { userCodeLength = 0x00 }
                });
            }
            else if (userCodeGetCmd.properties1.reportMore == 1 && UserCodeSupportData.MultipleUserCodeReportSup) // MUCR
            {
                int currentUserPos = UserCodeSupportData.Users.FindIndex(user => user.userId.SequenceEqual(currentUser.userId));

                rpt.vg1.Add(GetExtendedUserData(UserCodeSupportData.Users[currentUserPos]));

                for (int i = 0; i < UserCodeSupportData.SupportedUsers; i++)
                {
                    if (i > currentUserPos && UserCodeSupportData.Users[i].userIdStatus != 0)
                    {
                        rpt.vg1.Add(GetExtendedUserData(UserCodeSupportData.Users[i]));
                    }
                    if (rpt.vg1.Count == 4) // vg1 must fit in a single Z-Wave frame
                        break;
                }

                rpt.numberOfUserCodes = Convert.ToByte(rpt.vg1.Count());
            }
            else
            {
                rpt.numberOfUserCodes = 0x01;

                rpt.vg1.Add(GetExtendedUserData(currentUser));
            }
            return rpt;
        }

        private byte[] PrepareAdminCodeReport()
        {
            COMMAND_CLASS_USER_CODE_V2.ADMIN_CODE_REPORT rpt = new COMMAND_CLASS_USER_CODE_V2.ADMIN_CODE_REPORT();

            if (UserCodeSupportData.AdminCode.SequenceEqual(new List<byte> { 0x00, 0x00, 0x00, 0x00 })
             || ! UserCodeSupportData.AdminCodeSup) // MC
            {
                rpt.properties1.adminCodeLength = 0;
            }
            else
            {
                rpt.properties1.adminCodeLength = Convert.ToByte(UserCodeSupportData.AdminCode.Count);
                rpt.adminCode = UserCodeSupportData.AdminCode;
            }
            return rpt;
        }

        private byte[] PrepareChecksumReport()
        {
            COMMAND_CLASS_USER_CODE_V2.USER_CODE_CHECKSUM_REPORT rpt = new COMMAND_CLASS_USER_CODE_V2.USER_CODE_CHECKSUM_REPORT();

            List<byte> checkSumInput = new List<byte>();

            foreach (UserCodeUser user in UserCodeSupportData.Users)
            {
                if (user.userIdStatus != 0x00)
                {
                    checkSumInput.AddRange(user.userId);
                    checkSumInput.Add((byte) user.userIdStatus);
                    checkSumInput.AddRange(user.userCode);
                }
            }

            if (checkSumInput.Count != 0)
            {
                ushort crc = Tools.ZW_CreateCrc16(null, 0, checkSumInput.ToArray(), (byte)(checkSumInput.Count()));
                rpt.userCodeChecksum = new[] { (byte) (crc >> 8), (byte) (crc & 0xFF) };
            }
            else
            {
                rpt.userCodeChecksum = new byte[] { 0x00, 0x00 };
            }
            return rpt;
        }

        private SvResultNames HandleUserCodeSet (byte[] command)
        {
            COMMAND_CLASS_USER_CODE_V2.USER_CODE_SET userCodeSetCmd = command;
            UserCodeUserIdStatusV2 newUserIdStatus = (UserCodeUserIdStatusV2) (byte) userCodeSetCmd.userIdStatus;

            if (userCodeSetCmd.userIdentifier == 0x00)
            {
                if (newUserIdStatus == UserCodeUserIdStatusV2.Available)
                {
                    foreach (UserCodeUser user in UserCodeSupportData.Users)
                    {
                        if (user.userId[0] == 0) // apply to User IDs 1..255 only (legacy users)
                        {
                            user.userIdStatus = newUserIdStatus;
                            user.userCode = new List<byte> { 0x00, 0x00, 0x00, 0x00 };
                        }
                    }
                    return SvResultNames.Successful;
                }
                return SvResultNames.Failed;
            }
            UserCodeUser currentUser = UserCodeSupportData.Users.Find(user => user.userId[1] == userCodeSetCmd.userIdentifier && user.userId[0] == 0x00);
            if (currentUser != null)
            {
                if (ValidateUserIdStatus(newUserIdStatus))
                {
                    if (newUserIdStatus == UserCodeUserIdStatusV2.Available)
                    {
                        currentUser.userIdStatus = newUserIdStatus;
                        currentUser.userCode = new List<byte> { 0x00, 0x00, 0x00, 0x00 };
                        return SvResultNames.Successful;
                    }
                    if (ValidateUserCode(newUserIdStatus, userCodeSetCmd.userCode.ToList(), false))
                    {
                        currentUser.userIdStatus = newUserIdStatus;
                        currentUser.userCode = userCodeSetCmd.userCode.ToList();
                        return SvResultNames.Successful;
                    }
                }
            }
            return SvResultNames.Failed;
        }

        private SvResultNames HandleExtendedUserCodeSet (byte[] command)
        {
            COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_SET extendedUserCodeSetCmd = command;

            if (extendedUserCodeSetCmd.numberOfUserCodes != 0)
            {
                List<COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_SET.TVG1> userCodesList = extendedUserCodeSetCmd.vg1;

                if (userCodesList.Count == 1 && userCodesList[0].userIdentifier.SequenceEqual(new byte[] { 0x00, 0x00 }))
                {   // Delete all
                    foreach (UserCodeUser user in UserCodeSupportData.Users)
                    {
                        user.userIdStatus = 0x00;
                        user.userCode = new List<byte> { 0x00, 0x00, 0x00, 0x00 };
                    }
                    return SvResultNames.Successful;
                }
                foreach (COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_SET.TVG1 userCode in userCodesList)
                {
                    UserCodeUserIdStatusV2 newUserIdStatus = (UserCodeUserIdStatusV2) (byte) userCode.userIdStatus;
                    UserCodeUser currentUser = UserCodeSupportData.Users.Find(user => user.userId.SequenceEqual(userCode.userIdentifier));

                    if (currentUser == null)
                    {
                        continue; // ignore User Code Block
                    }
                    if (ValidateUserIdStatus(newUserIdStatus) && ValidateUserCode(newUserIdStatus, userCode.userCode.ToList(), true))
                    {
                        currentUser.userIdStatus = newUserIdStatus;
                        currentUser.userCode = userCode.userCode.ToList();
                    }
                    else
                    {
                        if (userCode.userCode.Count >= 4 && userCode.userCode.Count <= 10)
                        {
                            continue; // ignore User Code Block, if Count is valid (-> invalid char or duplicate detected)
                        }
                        return SvResultNames.Failed; // report error if Count is invalid
                    }
                    if (!UserCodeSupportData.MultipleUserCodeSetSup)
                    {
                        break; // No MUCS: handle first entry only
                    }
                }
                return SvResultNames.Successful;
            }
            return SvResultNames.Failed;
        }

        private SvResultNames HandleAdminCodeSet(byte[] command)
        {
            COMMAND_CLASS_USER_CODE_V2.ADMIN_CODE_SET adminCodeSetCmd = command;

            if (adminCodeSetCmd.properties1.adminCodeLength != 0
                && ValidateUserCode(UserCodeUserIdStatusV2.Enabled, adminCodeSetCmd.adminCode.ToList(), false)
                && (UserCodeSupportData.KeypadMode == 0x00 || UserCodeSupportData.KeypadMode == 0x01))
            {
                UserCodeSupportData.AdminCode = adminCodeSetCmd.adminCode.ToList();
                return SvResultNames.Successful;
            }
            if (adminCodeSetCmd.properties1.adminCodeLength == 0 && UserCodeSupportData.AdminCodeDeactivationSup) // MCD
            {
                UserCodeSupportData.AdminCode = new List<byte> { 0x00, 0x00, 0x00, 0x00 };
                return SvResultNames.Successful;
            }
            return SvResultNames.Failed;
        }

        private SvResultNames HandleKeypadModeSet(byte[] command)
        {
            COMMAND_CLASS_USER_CODE_V2.USER_CODE_KEYPAD_MODE_SET keypadModeSetCmd = command;
            if ((keypadModeSetCmd.keypadMode == 0x00 && UserCodeSupportData.SupportedKeypadModesList.Contains(UserCodeKeypadMode.Normal))
             || (keypadModeSetCmd.keypadMode == 0x01 && UserCodeSupportData.SupportedKeypadModesList.Contains(UserCodeKeypadMode.Vacation))
             || (keypadModeSetCmd.keypadMode == 0x02 && UserCodeSupportData.SupportedKeypadModesList.Contains(UserCodeKeypadMode.Privacy))
//           // Do not activate the following line, until the Keypad Mode 0x03 (Locked Out) is implemented!
//           || (keypadModeSetCmd.keypadMode == 0x03 && UserCodeSupportData.SupportedKeypadModesList.Contains(UserCodeKeypadMode.LockedOut))
             )
            {
                UserCodeSupportData.KeypadMode = keypadModeSetCmd.keypadMode;
                return SvResultNames.Successful;
            }
            return SvResultNames.Failed;
        }

        private void InitializeUsers()
        {
            for (int i = 1; i <= UserCodeSupportData.SupportedUsers; i++)
            {
                UserCodeSupportData.Users.Add(new UserCodeUser(new byte[] { (byte) (i / 256), (byte) (i % 256) }));
            }
        }

        private COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_REPORT.TVG1 GetExtendedUserData(UserCodeUser user)
        {
            if (user.userIdStatus != 0x00)
            {
                return new COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_REPORT.TVG1()
                {
                    userIdentifier = user.userId,
                    userIdStatus = (byte) user.userIdStatus,
                    properties1 = new COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_REPORT.TVG1.Tproperties1() { userCodeLength = Convert.ToByte(user.userCode.Count()) },
                    userCode = user.userCode
                };
            }
            else
            {
                return new COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_REPORT.TVG1()
                {
                    userIdentifier = user.userId,
                    userIdStatus = 0x00,
                    properties1 = new COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_REPORT.TVG1.Tproperties1() { userCodeLength = 0x00 }
                };
            }
        }

        private bool ValidateUserCode(UserCodeUserIdStatusV2 userIdStatus, List<byte> userCode, bool isExtended)
        {
            if (userIdStatus == UserCodeUserIdStatusV2.Available)
            {
                if (!isExtended)
                {
                    return userCode.SequenceEqual(new List<byte> { 0x00, 0x00, 0x00, 0x00 });
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
            else if (userIdStatus == UserCodeUserIdStatusV2.Messaging) // SupportedUserIdStatusList: Messaging is not supported
            {
                return false;
            }
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
                    if (user.userCode.SequenceEqual(userCode))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool ValidateUserIdStatus(UserCodeUserIdStatusV2 userIdStatus)
        {
            if ((userIdStatus == UserCodeUserIdStatusV2.Available   && UserCodeSupportData.SupportedUserIdStatusList.Contains(UserCodeUserIdStatusV2.Available))
             || (userIdStatus == UserCodeUserIdStatusV2.Enabled     && UserCodeSupportData.SupportedUserIdStatusList.Contains(UserCodeUserIdStatusV2.Enabled))
             || (userIdStatus == UserCodeUserIdStatusV2.Disabled    && UserCodeSupportData.SupportedUserIdStatusList.Contains(UserCodeUserIdStatusV2.Disabled))
             || (userIdStatus == UserCodeUserIdStatusV2.Messaging   && UserCodeSupportData.SupportedUserIdStatusList.Contains(UserCodeUserIdStatusV2.Messaging) && Version >= 2)
             || (userIdStatus == UserCodeUserIdStatusV2.PassageMode && UserCodeSupportData.SupportedUserIdStatusList.Contains(UserCodeUserIdStatusV2.PassageMode) && Version >= 2))
                return true;
            return false;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != Id)
            {
                supervisionReport.status = (byte) SvStatusNames.NoSupport;
                return;
            }

            supervisionReport.status = (byte) SvStatusNames.Fail;  // FAIL as default
            switch (supervisionGet.encapsulatedCommand[1])
            {
                case COMMAND_CLASS_USER_CODE_V2.USER_CODE_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
                case COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
                case COMMAND_CLASS_USER_CODE_V2.ADMIN_CODE_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
                case COMMAND_CLASS_USER_CODE_V2.USER_CODE_KEYPAD_MODE_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }


    }

    public enum UserCodeUserIdStatusV1 : byte
    {
        Available = 0x00,
        Occupied = 0x01,
        Reserved = 0x02,
        StatusNotAvailable = 0xFE
    };

    public enum UserCodeUserIdStatusV2 : byte
    {
        Available = 0x00,
        Enabled = 0x01,
        Disabled = 0x02,
        Messaging = 0x03,
        PassageMode = 0x04,
        StatusNotAvailable = 0xFE
    };

    public enum UserCodeKeypadMode : byte
    {
        Normal = 0x00,
        Vacation = 0x01,
        Privacy = 0x02,
        LockedOut = 0x03
    }

    public class UserCodeUser
    {
        public byte[] userId = new byte[2];
        public UserCodeUserIdStatusV2 userIdStatus;
        public List<byte> userCode;

        public UserCodeUser(byte[] UserId)
        {
            userId = UserId;
            userIdStatus = UserCodeUserIdStatusV2.Available;
            userCode = new List<byte> { 0x00, 0x00, 0x00, 0x00 };
        }
    }

    public class UserCodeSupportData
    {
        // Internals accessible from TestCases
        public List<UserCodeUser> Users { get; set; } = new List<UserCodeUser>();
        public List<byte> AdminCode { get; set; } = new List<byte> { 0x00, 0x00, 0x00, 0x00 };
        public byte KeypadMode { get; set; } = 0x00; // 0x00 = Normal; 0x01 = Vacation; 0x02 = Privacy; 0x03 = Locked out

        // V1+
        // Supported Users: V1: 1...255, V2: 1...65535
        public uint SupportedUsers { get; set; } = 10;
        // V2+ Capabilities
        public bool AdminCodeSup { get; set; } = true;             // MC
        public bool AdminCodeDeactivationSup { get; set; } = true; // MCD
        public bool UserCodeChecksumSup { get; set; } = true;       // UCC
        public bool MultipleUserCodeReportSup { get; set; } = true; // MUCR
        public bool MultipleUserCodeSetSup { get; set; } = true;    // MUCS

        // Bit 0 = Available; Bit 1 = Enabled/Grant Access; Bit 2 = Disabled; Bit 3 = Messaging (not implemented); Bit 4 = Passage Mode
        public List<UserCodeUserIdStatusV2> SupportedUserIdStatusList { get; set; } = new List<UserCodeUserIdStatusV2> 
        {
            UserCodeUserIdStatusV2.Available,
            UserCodeUserIdStatusV2.Enabled,
            UserCodeUserIdStatusV2.Disabled,
            UserCodeUserIdStatusV2.PassageMode,
        };

        // Bit 0 = Normal mode; Bit 1 = Vacation mode; Bit 2 = Privacy Mode; Bit 3 = Locked Out Mode (not implemented)
        public List<UserCodeKeypadMode> SupportedKeypadModesList { get; set; } = new List<UserCodeKeypadMode> 
        {
            UserCodeKeypadMode.Normal,
            UserCodeKeypadMode.Vacation,
            UserCodeKeypadMode.Privacy,
        };

        // SupportedKeysList contains all supported ASCII codes. Default: 0...9
        public List<byte> SupportedKeysList { get; set; } = new List<byte> { 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 };
        // Keys: FF 30 = keys 0...9
        //public List<byte> SupportedKeysBitMask { get; set; } = new List<byte> { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x03 };
    }
}
