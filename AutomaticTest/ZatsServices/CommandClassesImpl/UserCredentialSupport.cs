using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Utils;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.Devices;

namespace Zats.Services
{

    [CommandClassName("User Credential Command Class")]
    [CommandClassId(COMMAND_CLASS_USER_CREDENTIAL.ID)]
    [MinVersion(1)]
    [MaxVersion(1)]

    public class UserCredentialSupport : CommandClassSupport
    {
        private UserCredentialSupportData UserCredentialSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return UserCredentialSupportData; }
            set { UserCredentialSupportData = (UserCredentialSupportData)value; }
        }

        public ScheduleEntryLockSupportData ScheduleEntryLockSupportData = null;

        public DoorLockSupport DoorLockSupport = null;

        public UserCodeSupport UserCodeSupport = null;

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public UserCredentialSupport()
            : this(new UserCredentialSupportData() { }) { }

        public UserCredentialSupport(UserCredentialSupportData userCredentialSupportData)
            : base(COMMAND_CLASS_USER_CREDENTIAL.ID)
        {
            UserCredentialSupportData = userCredentialSupportData;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private Dictionary<ushort, Timer> expiringTimers = new Dictionary<ushort, Timer>();

        private byte notificationSequenceNumber = 0;

        public delegate void LearningStartedEvent(UserCredentialSupport userCredentialSupport);

        public LearningStartedEvent CredentialLearningStarted = null;

        public class CredentialLearningData
        {
            public bool LearningActive { get; set; }

            public CredentialType CredentialType { get; set; }

            public ushort UserUniqueId { get; set; }

            public ushort CredentialSlot { get; set; }

            public byte LearningSteps { get; set; }

            public byte RemainingSteps { get; set; }

            public Timer Timeout { get; set; }

            public NodeTag NodeStartedLearning { get; set; } = NodeTag.Empty;

            public CredentialLearnOperationType OperationType { get; set; }

            public static implicit operator COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_LEARN_REPORT(CredentialLearningData data)
            {
                return new COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_LEARN_REPORT()
                {
                    //credentialLearnStatus = (byte)CredentialLearnStatus.AlreadyInProgress,
                    userUniqueIdentifier = Tools.GetBytes(data.UserUniqueId),
                    credentialType = (byte)data.CredentialType,
                    credentialSlot = Tools.GetBytes(data.CredentialSlot),
                    credentialLearnStepsRemaining = data.RemainingSteps
                };
            }
        }

        public CredentialLearningData CredentialLearning = new CredentialLearningData();

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
                if (isHandlingAllowed && command != null && command.Length > 1 && UserCredentialSupportData != null)
                {
                    byte[] rpt = null;
                    byte[] autoReportDueToSet = null;
                    if (command[1] == COMMAND_CLASS_USER_CREDENTIAL.USER_CAPABILITIES_GET.ID && DstNode != NodeTag.FF)
                    {
                        rpt = PrepareUserCapabilitiesReport();
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_CAPABILITIES_GET.ID)
                    {
                        rpt = PrepareCredentialCapabilitiesReport();
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CREDENTIAL.USER_SET.ID)
                    {
                        ResultForSv = HandleUserSetCmd(command, sendSrcNode, out autoReportDueToSet);
                        //UserCredentialSupportData.PrintUsersInfo();
                        //LogUserCodes();
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CREDENTIAL.USER_GET.ID && DstNode != NodeTag.FF)
                    {
                        rpt = PrepareUserReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_SET.ID)
                    {
                        ResultForSv = HandleCredentialSetCmd(command, sendSrcNode, out autoReportDueToSet);
                        //UserCredentialSupportData.PrintUsersInfo();
                        //LogUserCodes();
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_GET.ID && DstNode != NodeTag.FF)
                    {
                        rpt = PrepareCredentialReportCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_LEARN_START.ID)
                    {
                        //UserCredentialSupportData.PrintUsersInfo();
                        ResultForSv = HandleCredentialLearnStart(command, sendSrcNode, out autoReportDueToSet);
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_LEARN_CANCEL.ID)
                    {
                        StopCredentialLearning(CredentialLearnStatus.EndedNotDueToTimeout);
                        ResultForSv = SvResultNames.Successful;
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CREDENTIAL.USER_CREDENTIAL_ASSOCIATION_SET.ID)
                    {
                        //UserCredentialSupportData.PrintUsersInfo();
                        ResultForSv = HandleCredentialAssociationSetCmd(command, sendSrcNode, out autoReportDueToSet);
                        //UserCredentialSupportData.PrintUsersInfo();
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CREDENTIAL.ALL_USERS_CHECKSUM_GET.ID)
                    {
                        rpt = PrepareAllUsersChecksumReport();
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CREDENTIAL.USER_CHECKSUM_GET.ID)
                    {
                        rpt = PrepareUserChecksumReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_CHECKSUM_GET.ID)
                    {
                        rpt = PrepareCredentialChecksumReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CREDENTIAL.ADMIN_PIN_CODE_SET.ID)
                    {
                        ResultForSv = HandleAdminCodeSetCmd(command, sendSrcNode, out autoReportDueToSet);
                    }
                    else if (command[1] == COMMAND_CLASS_USER_CREDENTIAL.ADMIN_PIN_CODE_GET.ID)
                    {
                        rpt = PrepareAdminPinCodeReport();
                    }


                    // Report due to Get command
                    if (rpt != null && IsResponseAllowed())
                    {
                        ApiOperation sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    // "Auto-Report" due to Set command
                    if (autoReportDueToSet != null) // Contrary to Get-type commands: Broadcast, Multicast and MC Bit Addressing are NO specified reasons to prevent
                                                // sending the "Auto-Reports" upon a Set-type command in User Credential CC.
                    {
                        ApiOperation sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, autoReportDueToSet, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }


                    CommandCallback(command, sendDstNode.EndPointId);
                }
            }
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet,
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != CommandClassId)
            {
                supervisionReport.status = (byte)SvStatusNames.NoSupport;
                return;
            }

            supervisionReport.status = (byte)SvStatusNames.Fail;  // FAIL as default
            switch (supervisionGet.encapsulatedCommand[1])
            {
                case COMMAND_CLASS_USER_CREDENTIAL.USER_SET.ID:
                case COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_SET.ID:
                case COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_LEARN_START.ID:
                case COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_LEARN_CANCEL.ID:
                case COMMAND_CLASS_USER_CREDENTIAL.USER_CREDENTIAL_ASSOCIATION_SET.ID:
                case COMMAND_CLASS_USER_CREDENTIAL.ADMIN_PIN_CODE_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte)SvStatusNames.Success;
                    }
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }

        private byte[] PrepareUserCapabilitiesReport()
        {
            List<byte> suppUserTypesBitMask = CommandClassSupportUtils.CreateBitmaskFromList(UserCredentialSupportData.SupportedUserTypes, 0);

            var usrCapaReport = new COMMAND_CLASS_USER_CREDENTIAL.USER_CAPABILITIES_REPORT()
            {
                numberOfSupportedUserUniqueIdentifiers = Tools.GetBytes(UserCredentialSupportData.NoOfSupportedUsers),
                supportedCredentialRulesBitMask = CommandClassSupportUtils.CreateBitmaskFromList(UserCredentialSupportData.SupportedCredentialRules, 0, true).First(),
                maxLengthOfUserName = UserCredentialSupportData.MaxUserNameLength,
                properties1 = new COMMAND_CLASS_USER_CREDENTIAL.USER_CAPABILITIES_REPORT.Tproperties1()
                {
                    /*Bit 0 and 1 reserved*/
                    /*Bit 2*/ asciiEncodingSupport = (byte)(UserCredentialSupportData.UserNameEncodingsAdvertisedAsSupported.Contains(UserNameEncoding.ASCII) ? 0x01 : 0x00),
                    /*Bit 3*/ extendedAsciiEncodingSupport = (byte)(UserCredentialSupportData.UserNameEncodingsAdvertisedAsSupported.Contains(UserNameEncoding.ExtendedASCII) ? 0x01 : 0x00),
                    /*Bit 4*/ utf16EncodingSupport = (byte)(UserCredentialSupportData.UserNameEncodingsAdvertisedAsSupported.Contains(UserNameEncoding.UTF16BE) ? 0x01 : 0x00),
                    /*Bit 5*/ userChecksumSupport = (byte)(UserCredentialSupportData.UserChecksumSupport ? 0x01 : 0x00),
                    /*Bit 6*/ allUsersChecksumSupport = (byte)(UserCredentialSupportData.AllUsersChecksumSupport ? 0x01 : 0x00),
                    /*Bit 7*/ userScheduleSupport = (byte)(UserCredentialSupportData.UserScheduleSupport ? 0x01 : 0x00),
                },
                supportedUserTypesBitMaskLength = (byte)suppUserTypesBitMask.Count,
                supportedUserTypesBitMask = suppUserTypesBitMask,
            };

            return usrCapaReport;
        }

        private byte[] PrepareCredentialCapabilitiesReport()
        {
            var credentialCapReport = new COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_CAPABILITIES_REPORT();

            credentialCapReport.properties1.credentialChecksumSupport = (byte)(UserCredentialSupportData.CredentialChecksumSupport ? 0x01 : 0x00);
            credentialCapReport.properties1.adminCodeSupport = (byte)(UserCredentialSupportData.AdminCodeSupport ? 0x01 : 0x00);
            credentialCapReport.properties1.adminCodeDeactivationSupport = (byte)(UserCredentialSupportData.AdminCodeDeactivationSupport ? 0x01 : 0x00);

            credentialCapReport.numberOfSupportedCredentialTypes = (byte)UserCredentialSupportData.SupportedCredentialTypes.Count;

            foreach (SupportedCredentialType suppCredType in UserCredentialSupportData.SupportedCredentialTypes)
            {
                var vg1 = new COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_CAPABILITIES_REPORT.TVG1CREDENTIALTYPE();
                vg1.credentialType = (byte)suppCredType.Type;
                credentialCapReport.vg1CredentialType.Add(vg1);

                var vg2 = new COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_CAPABILITIES_REPORT.TVG2PROPERTIES2PERCREDENTIALTYPE();
                vg2.properties1.clSupport = (byte)(suppCredType.CredentialLearnSupport ? 0x01 : 0x00);
                credentialCapReport.vg2Properties2PerCredentialType.Add(vg2);

                var vg3 = new COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_CAPABILITIES_REPORT.TVG3NUMBEROFSUPPORTEDCREDENTIALSLOTSPERCREDENTIALTYPE();
                vg3.numberOfSupportedCredentialSlots = Tools.GetBytes(suppCredType.NoOfSupportedSlots);
                credentialCapReport.vg3NumberOfSupportedCredentialSlotsPerCredentialType.Add(vg3);

                var vg4 = new COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_CAPABILITIES_REPORT.TVG4MINLENGTHOFCREDENTIALDATAPERCREDENTIALTYPE();
                vg4.minLengthOfCredentialData = suppCredType.MinCredentialDataLength;
                credentialCapReport.vg4MinLengthOfCredentialDataPerCredentialType.Add(vg4);

                var vg5 = new COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_CAPABILITIES_REPORT.TVG5MAXLENGTHOFCREDENTIALDATAPERCREDENTIALTYPE();
                vg5.maxLengthOfCredentialData = suppCredType.MaxCredentialDataLength;
                credentialCapReport.vg5MaxLengthOfCredentialDataPerCredentialType.Add(vg5);

                var vg6 = new COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_CAPABILITIES_REPORT.TVG6CLRECOMMENDEDTIMEOUTPERCREDENTIALTYPE();
                vg6.clRecommendedTimeout = suppCredType.CredentialLearnRecommendedTimeout;
                credentialCapReport.vg6ClRecommendedTimeoutPerCredentialType.Add(vg6);

                var vg7 = new COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_CAPABILITIES_REPORT.TVG7CLNUMBEROFSTEPSPERCREDENTIALTYPE();
                vg7.clNumberOfSteps = suppCredType.CredentialLearnNoOfSteps;
                credentialCapReport.vg7ClNumberOfStepsPerCredentialType.Add(vg7);

                var vg8 = new COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_CAPABILITIES_REPORT.TVG8MAXIMUMCREDENTIALHASHLENGTHPERCREDENTIALTYPE();
                vg8.maximumCredentialHashLength = suppCredType.MaxHCredentialHashLength;
                credentialCapReport.vg8MaximumCredentialHashLengthPerCredentialType.Add(vg8);
            }

            return credentialCapReport;
        }

        private SvResultNames HandleUserSetCmd(COMMAND_CLASS_USER_CREDENTIAL.USER_SET userSetCmd, NodeTag srcNode, out byte[] rpt)
        {
            byte[] lifeLineReport = null;
            rpt = null;

            var resultForSv = SvResultNames.Successful;

            var opType = (UserSetOperationType)userSetCmd.properties1.operationType;
            ushort userUniqueId = Tools.ByteArrayToUInt16(userSetCmd.userUniqueIdentifier);
            var userType = (UserCredentialUserType)userSetCmd.userType.Value;
            var userNameEncoding = (UserNameEncoding)userSetCmd.properties3.userNameEncoding;
            var userActiveState = (UserCredentialUserActiveState)userSetCmd.properties2.userActiveState;
            var credentialRule = (UserCredentialRule)userSetCmd.credentialRule.Value;
            ushort expiringTimeout = Tools.ByteArrayToUInt16(userSetCmd.expiringTimeoutMinutes);

            var existingUserSlot = UserCredentialSupportData.UserSlots.Find(x => x.UserUniqeId == userUniqueId);
            ushort nextUserUniqueId = GetNextUserSlot(userUniqueId);

            COMMAND_CLASS_USER_CREDENTIAL.USER_REPORT userReport = null;

            if (opType == UserSetOperationType.Delete)
            {
                if (userUniqueId == 0)
                {
                    // CC:0083.01.05.11.007, CC:0083.01.05.11.009 - delete all users, credentials and schedules
                    UserCredentialSupportData.UserCredentials.Clear();
                    UserCredentialSupportData.UserSlots.Clear();
                    ScheduleEntryLockSupportData?.ScheduleSlots.Clear();

                    // CC:0083.01.05.11.010 - sending User Report to sender and lifeline
                    userReport = new COMMAND_CLASS_USER_CREDENTIAL.USER_REPORT()
                    {
                        userUniqueIdentifier = new byte[] { 0x00, 0x00 },
                        userReportType = (byte)UserReportType.UsersDeleted,
                        userModifierType = (byte)ModifierType.DNE,
                        credentialRule = (byte)UserCredentialRule.Single,
                    };

                    rpt = userReport;
                    lifeLineReport = userReport;
                }
                else if (existingUserSlot != null)
                {
                    UserCredentialSupportData.DeleteUser(existingUserSlot);

                    DeleteUsersSchedules(userUniqueId);

                    // CC:0083.01.05.11.013 - sending User Report to sender and lifeline
                    userReport = existingUserSlot;
                    userReport.userReportType = (byte)UserReportType.UsersDeleted;
                    userReport.nextUserUniqueIdentifier = Tools.GetBytes(nextUserUniqueId);

                    rpt = userReport;
                    lifeLineReport = userReport;
                }
                else
                {
                    // there is no user to delete - but the outcome is as expected, so set SV successful

                    userReport = new COMMAND_CLASS_USER_CREDENTIAL.USER_REPORT()
                    {
                        userReportType = (byte)UserReportType.UserUnchanged,
                        userUniqueIdentifier = Tools.GetBytes(userUniqueId),
                        userModifierType = (byte)ModifierType.DNE,
                        credentialRule = (byte)UserCredentialRule.Single,
                        nextUserUniqueIdentifier = Tools.GetBytes(nextUserUniqueId)
                    };

                    rpt = userReport;

                    return SvResultNames.Successful;
                }

                HandleExpiringUsers();

            }
            else if (opType == UserSetOperationType.Add || opType == UserSetOperationType.Modify)
            {
                resultForSv = EncodeString(userSetCmd.userName.ToArray(), userNameEncoding, UserCredentialSupportData.MaxUserNameLength, out string userName) ? SvResultNames.Successful : SvResultNames.Failed;

                // CC:0083.01.05.11.014, CC:0083.01.05.11.015 - irgnore command when userUniqueId is out of range
                if (userUniqueId == 0 || userUniqueId > UserCredentialSupportData.NoOfSupportedUsers)
                {
                    return SvResultNames.Failed;
                }

                if (Enum.IsDefined(typeof(UserCredentialUserType), userType) == false)
                {
                    return SvResultNames.Failed;
                }

                if (Enum.IsDefined(typeof(UserCredentialRule), credentialRule) == false)
                {
                    return SvResultNames.Failed;
                }

                if (Enum.IsDefined(typeof(UserCredentialUserActiveState), userActiveState) == false)
                {
                    return SvResultNames.Failed;
                }

                if (Enum.IsDefined(typeof(UserNameEncoding), userNameEncoding) == false)
                {
                    return SvResultNames.Failed;
                }

                if (UserCredentialSupportData.UserNameEncodingsAdvertisedAsSupported.IsUserNameEncodingSupported(userNameEncoding) == false)
                {
                    return SvResultNames.Failed;
                }

                var userToAddOrModify = new UserSlot()
                {
                    UserUniqeId = userUniqueId,
                    Name = userName ?? UserSlot.GetDefaultUserName(userUniqueId),
                    Type = userType,
                    ActiveState = userActiveState,
                    CredentialRule = credentialRule,
                    ExpiringTimeout = userType == UserCredentialUserType.ExpiringUser ? expiringTimeout : (ushort)0,
                    NameEncoding = userNameEncoding,
                    ModifierType = srcNode != NodeTag.Empty ? ModifierType.ZWave : ModifierType.Locally,
                    ModifierId = srcNode.Id
                };

                // CC:0083.01.07.11.007 - userdata already there
                if (existingUserSlot != null && existingUserSlot.Equals(userToAddOrModify))
                {
                    userReport = existingUserSlot;
                    userReport.userReportType = (byte)UserReportType.UserUnchanged;
                    userReport.nextUserUniqueIdentifier = Tools.GetBytes(nextUserUniqueId);

                    rpt = userReport;
                    return SvResultNames.Successful;
                }

                // CC:0083.01.07.11.004 
                if (opType == UserSetOperationType.Add && existingUserSlot != null)
                {
                    userReport = existingUserSlot;
                    userReport.userReportType = (byte)UserReportType.UserAddRejectedOccupied;
                    userReport.nextUserUniqueIdentifier = Tools.GetBytes(nextUserUniqueId);

                    rpt = userReport;
                    return SvResultNames.Failed;
                }

                if (opType == UserSetOperationType.Add && userType == UserCredentialUserType.ExpiringUser && expiringTimeout == 0)
                {
                    userReport = userToAddOrModify;
                    userReport.userReportType = (byte)UserReportType.ZeroExpiringInvalid;
                    userReport.nextUserUniqueIdentifier = Tools.GetBytes(nextUserUniqueId);
                    userReport.userModifierType = (byte)ModifierType.DNE;
                    userReport.userModifierNodeId = new byte[] { 0x00, 0x00 };

                    rpt = userReport;
                    return SvResultNames.Failed;
                }

                if (opType == UserSetOperationType.Modify && userType == UserCredentialUserType.ExpiringUser && expiringTimeout == 0 && existingUserSlot.Type != UserCredentialUserType.ExpiringUser)
                {
                    userReport = existingUserSlot;
                    userReport.userReportType = (byte)UserReportType.ZeroExpiringInvalid;
                    userReport.nextUserUniqueIdentifier = Tools.GetBytes(nextUserUniqueId);

                    rpt = userReport;
                    return SvResultNames.Failed;
                }

                // CC:0083.01.07.11.004 
                if (opType == UserSetOperationType.Modify && existingUserSlot == null)
                {
                    userReport = userToAddOrModify;
                    userReport.userReportType = (byte)UserReportType.UserModifyRejectedEmpty;
                    userReport.nextUserUniqueIdentifier = Tools.GetBytes(nextUserUniqueId);
                    userReport.userModifierType = (byte)ModifierType.DNE;
                    userReport.userModifierNodeId = new byte[] { 0x00, 0x00 };

                    rpt = userReport;
                    return SvResultNames.Failed;
                }

                UserCredentialSupportData.AddOrModifyUser(userToAddOrModify);

                // CC:0083.01.05.11.011, CC:0083.01.05.11.012, CC:0083.01.07.11.004 
                userReport = userToAddOrModify;
                userReport.userReportType = (byte)(opType == UserSetOperationType.Add ? UserReportType.UserAdded : UserReportType.UserModified);
                userReport.nextUserUniqueIdentifier = Tools.GetBytes(nextUserUniqueId);

                rpt = userReport;
                lifeLineReport = userReport;

                HandleExpiringUsers();

            }
            else
            {
                resultForSv = SvResultNames.Failed;
            }


            if (lifeLineReport != null)
            {
                SendUnsolicitedReport(lifeLineReport, srcNode);
            }

            UserCodeSupport?.SyncUserCodes();

            return resultForSv;
        }

        private ushort GetNextUserSlot(ushort currentUserUniqueId)
        {
            var users = UserCredentialSupportData.UserSlots.OrderBy(x => x.UserUniqeId).ToList();
            UserSlot nextUser = null;

            for (int i = 0; i < users.Count; i++)
            {
                if (currentUserUniqueId == 0 || users[i].UserUniqeId == currentUserUniqueId)
                {
                    if (i + 1 < users.Count)
                    {
                        nextUser = users[i + 1];
                    }
                    break;
                }
            }
            return nextUser?.UserUniqeId ?? 0;
        }

        private byte[] PrepareUserReport(COMMAND_CLASS_USER_CREDENTIAL.USER_GET userGetCmd)
        {
            ushort requestedUserId = Tools.ByteArrayToUInt16(userGetCmd.userUniqueIdentifier);

            var users = UserCredentialSupportData.UserSlots.OrderBy(x => x.UserUniqeId).ToList();

            UserSlot requestedUser = requestedUserId == 0 ? users.FirstOrDefault() : users.Find(x => x.UserUniqeId == requestedUserId);
            ushort nextUserUnique = GetNextUserSlot(requestedUserId);

            COMMAND_CLASS_USER_CREDENTIAL.USER_REPORT rpt = requestedUser ?? new COMMAND_CLASS_USER_CREDENTIAL.USER_REPORT()
            {
                userReportType = (byte)UserReportType.ResponseToGet,
                userUniqueIdentifier = Tools.GetBytes(requestedUserId),
                userModifierType = (byte)ModifierType.DNE,
                credentialRule = (byte)UserCredentialRule.Single,

            };

            rpt.nextUserUniqueIdentifier = Tools.GetBytes(nextUserUnique);

            return rpt;
        }

        private SvResultNames HandleCredentialSetCmd(COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_SET credentialSetCmd, NodeTag srcNode, out byte[] rpt)
        {
            rpt = null;
            byte[] lifelineReport = null;

            var resultForSv = SvResultNames.Successful;


            ushort userUniqueId = Tools.ByteArrayToUInt16(credentialSetCmd.userUniqueIdentifier);
            var credentialType = (CredentialType)credentialSetCmd.credentialType.Value;
            ushort credentialSlot = Tools.ByteArrayToUInt16(credentialSetCmd.credentialSlot);
            var opType = (CredentialSetOperationType)credentialSetCmd.properties1.operationType;
            byte credentialLength = credentialSetCmd.credentialLength;
            byte[] credentialData = credentialSetCmd.credentialData.ToArray();


            if (opType == CredentialSetOperationType.Add || opType == CredentialSetOperationType.Modify)
            {
                var credential = UserCredential.CreateCredential(userUniqueId, credentialType, credentialSlot, credentialData, ModifierType.ZWave, srcNode.Id);

                var credentialTypeSettings = UserCredentialSupportData.SupportedCredentialTypes.FirstOrDefault(x => x.Type == credentialType);
                if (credentialTypeSettings != null)
                {
                    credential.InherritCredentialReadback(credentialTypeSettings.ReadBack);
                }

                resultForSv = HandleAddOrModifyCredential(opType, credential, srcNode, out rpt);
            }
            else if (opType == CredentialSetOperationType.Delete)
            {
                if (credentialType != 0 && credentialSlot != 0)
                {
                    // Delete SINGLE
                    var credential = UserCredentialSupportData.UserCredentials.Find(x => x.Type == credentialType && x.SlotId == credentialSlot);

                    if (credential != null)
                    {
                        // CC:0083.01.0A.11.008
                        UserCredentialSupportData.UserCredentials.Remove(credential);

                        // CC:0083.01.0A.11.013
                        rpt = credential.CreateUserCredentialReport(CredentialReportType.CredentialDeleted);
                        lifelineReport = rpt;
                    }
                    else
                    {
                        rpt = SetupDeleteCredentialReport(userUniqueId, credentialType, credentialSlot, CredentialReportType.CredentialUnchanged);
                    }

                    
                }
                else if (userUniqueId != 0 && credentialType != 0 && credentialSlot == 0)
                {
                    // Delete TYPEWISE for single user (CC:0083.01.0A.11.008)
                    var credentials = UserCredentialSupportData.UserCredentials.FindAll(x => x.UserUniqeId == userUniqueId && x.Type == credentialType);

                    credentials.ForEach(x => {
                        UserCredentialSupportData.UserCredentials.Remove(x);
                    });

                    // CC:0083.01.0A.11.013 
                    rpt = SetupDeleteCredentialReport(userUniqueId, credentialType, credentialSlot);
                    lifelineReport = rpt;

                }
                else if (userUniqueId != 0 && credentialType == 0 && credentialSlot == 0)
                {
                    // Delete USERWISE (CC:0083.01.0A.11.008)
                    var credentials = UserCredentialSupportData.UserCredentials.FindAll(x => x.UserUniqeId == userUniqueId);

                    credentials.ForEach(x => {
                        UserCredentialSupportData.UserCredentials.Remove(x);
                    });

                    // CC:0083.01.0A.11.013 
                    rpt = SetupDeleteCredentialReport(userUniqueId, credentialType, credentialSlot);
                    lifelineReport = rpt;
                }
                else if (userUniqueId == 0 && credentialType == 0 && credentialSlot == 0)
                {
                    // Delete ALL (CC:0083.01.0A.11.008)
                    UserCredentialSupportData.UserCredentials.Clear();

                    // CC:0083.01.0A.11.010 
                    rpt = SetupDeleteCredentialReport(userUniqueId, credentialType, credentialSlot);
                    lifelineReport = rpt;

                }
                else if (userUniqueId == 0 && credentialType != 0 && credentialSlot == 0)
                {
                    // Delete TYPEWISE for all users (CC:0083.01.0A.11.008)
                    var credentials = UserCredentialSupportData.UserCredentials.FindAll(x => x.Type == credentialType);

                    credentials.ForEach(x => {
                        UserCredentialSupportData.UserCredentials.Remove(x);
                    });

                    // CC:0083.01.0A.11.013 
                    rpt = SetupDeleteCredentialReport(credentialSlot, credentialType, credentialSlot);
                    lifelineReport = rpt;
                }
            }

            if (lifelineReport != null)
            {
                SendUnsolicitedReport(lifelineReport, srcNode);
            }

            return resultForSv;
        }

        private SvResultNames HandleAddOrModifyCredential(CredentialSetOperationType opType, UserCredential newCredentialData, NodeTag srcNode, out byte[] rpt)
        {
            rpt = null;
            byte[] lifelineReport = null;

            var user = UserCredentialSupportData.UserSlots.Find(x => x.UserUniqeId == newCredentialData.UserUniqeId);
            var existingCredential = UserCredentialSupportData.UserCredentials.Find(x => x.SlotId == newCredentialData.SlotId /* && x.UserUniqeId == newCredentialData.UserUniqeId */ && x.Type == newCredentialData.Type);
            var duplicateCredential = UserCredentialSupportData.UserCredentials.Find(x => x.Type == newCredentialData.Type && Enumerable.SequenceEqual(x.Data, newCredentialData.Data));

            var credentialTypeSettings = UserCredentialSupportData.SupportedCredentialTypes.FirstOrDefault(x => x.Type == newCredentialData.Type);


            // CC:0083.01.0A.11.000 - check if credential type is valid
            if (Enum.IsDefined(typeof(CredentialType), newCredentialData.Type) == false)
            {
                return SvResultNames.Failed;
            }

            // CC:0083.01.0A.11.002 - check is credential type is supported
            if (credentialTypeSettings == null)
            {
                return SvResultNames.Failed;
            }

            // CC:0083.01.0A.11.003, CC:0083.01.0A.11.004, CC:0083.01.0A.11.005 - reject invalid slots
            if (newCredentialData.SlotId == 0 || newCredentialData.SlotId > credentialTypeSettings.NoOfSupportedSlots)
            {
                return SvResultNames.Failed;
            }


            if (newCredentialData.UserUniqeId == 0 || newCredentialData.UserUniqeId > UserCredentialSupportData.NoOfSupportedUsers)
            {
                return SvResultNames.Failed;
            }

            if (opType == CredentialSetOperationType.Add)
            {
                if (existingCredential != null && existingCredential.Equals(newCredentialData) == false)
                {
                    var nextCredential = GetNextUsersCredential(existingCredential);

                    // CC:0083.01.0C.11.011
                    COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_REPORT credentialReport = 
                        existingCredential.CreateUserCredentialReport(CredentialReportType.CredentialAddRejectedLocationOccupied);

                    credentialReport.nextCredentialSlot = Tools.GetBytes(nextCredential?.SlotId ?? 0);
                    credentialReport.nextCredentialType = (byte)(nextCredential?.Type ?? 0);
                    rpt = credentialReport;

                    return SvResultNames.Failed;
                }

                if (user == null)
                {
                    // CC:0083.01.0C.11.011
                    COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_REPORT credentialReport = 
                        newCredentialData.CreateUserCredentialReport(CredentialReportType.WrongUserUniqueIdentifier, true);

                    credentialReport.credentialModifierType = (byte)ModifierType.DNE;
                    credentialReport.credentialModifierNodeId = new byte[] { 0x00, 0x00 };
                    rpt = credentialReport;
                    return SvResultNames.Failed;
                }


            } else if (opType == CredentialSetOperationType.Modify)
            {
                if (user == null)
                {
                    // CC:0083.01.0C.11.011
                    COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_REPORT credentialReport = 
                        newCredentialData.CreateUserCredentialReport(CredentialReportType.WrongUserUniqueIdentifier, true);

                    credentialReport.credentialModifierType = (byte)ModifierType.DNE;
                    credentialReport.credentialModifierNodeId = new byte[] { 0x00, 0x00 };
                    rpt = credentialReport;
                    return SvResultNames.Failed;
                }

                if (existingCredential == null)
                {
                    // CC:0083.01.0C.11.011
                    COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_REPORT credentialReport = 
                        newCredentialData.CreateUserCredentialReport(CredentialReportType.CredentialModifyRejectedLocationEmpty, true);

                    credentialReport.credentialModifierType = (byte)ModifierType.DNE;
                    credentialReport.credentialModifierNodeId = new byte[] { 0x00, 0x00 };
                    rpt = credentialReport;
                    return SvResultNames.Failed;
                }

                if (existingCredential != null && newCredentialData.UserUniqeId != existingCredential.UserUniqeId)
                {
                    // CC:0083.01.0C.11.011
                    COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_REPORT credentialReport = 
                        newCredentialData.CreateUserCredentialReport(CredentialReportType.WrongUserUniqueIdentifier, true);

                    credentialReport.credentialModifierType = (byte)ModifierType.DNE;
                    credentialReport.credentialModifierNodeId = new byte[] { 0x00, 0x00 };
                    rpt = credentialReport;
                    return SvResultNames.Failed;
                }
            }

            if (duplicateCredential != null && duplicateCredential.Equals(existingCredential) == false)
            {
                var nextCredential = GetNextUsersCredential(duplicateCredential);

                // CC:0083.01.0C.11.011
                COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_REPORT credentialReport = 
                    duplicateCredential.CreateUserCredentialReport(CredentialReportType.DuplicateCredential);

                credentialReport.nextCredentialSlot = Tools.GetBytes(nextCredential?.SlotId ?? 0);
                credentialReport.nextCredentialType = (byte)(nextCredential?.Type ?? 0);
                rpt = credentialReport;
                return SvResultNames.Failed;
            }

            if (UserCredentialSupportData.AdminCodeSupport && newCredentialData.Type == CredentialType.PinCode &&
                    Enumerable.SequenceEqual(DecodeString(UserCredentialSupportData.AdminCode, UserNameEncoding.ASCII), newCredentialData.Data))
            {
                var nextCredential = GetNextUsersCredential(newCredentialData);

                // CC:0083.01.0C.11.011
                COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_REPORT credentialReport = 
                    newCredentialData.CreateUserCredentialReport(CredentialReportType.DuplicateAdminCode, true);

                credentialReport.nextCredentialSlot = Tools.GetBytes(nextCredential?.SlotId ?? 0);
                credentialReport.nextCredentialType = (byte)(nextCredential?.Type ?? 0);
                rpt = credentialReport;
                return SvResultNames.Failed;
            }

            if (existingCredential != null && existingCredential.Equals(newCredentialData))
            {
                var nextCredential = GetNextUsersCredential(existingCredential);

                // CC:0083.01.0C.11.013
                COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_REPORT credentialReport = 
                    existingCredential.CreateUserCredentialReport(CredentialReportType.CredentialUnchanged);

                credentialReport.nextCredentialSlot = Tools.GetBytes(nextCredential?.SlotId ?? 0);
                credentialReport.nextCredentialType = (byte)(nextCredential?.Type ?? 0);
                rpt = credentialReport;
                return SvResultNames.Successful;
            }

            if (UserCredentialSupportData.AddOrModifyCredential(newCredentialData))
            {
                var nextCredential = GetNextUsersCredential(newCredentialData);

                // CC:0083.01.0A.11.011, CC:0083.01.0A.11.012
                COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_REPORT credentialReport = 
                    newCredentialData.CreateUserCredentialReport(opType == CredentialSetOperationType.Add ? CredentialReportType.CredentialAdded : CredentialReportType.CredentialModified);

                credentialReport.nextCredentialSlot = Tools.GetBytes(nextCredential?.SlotId ?? 0);
                credentialReport.nextCredentialType = (byte)(nextCredential?.Type ?? 0);
                rpt = credentialReport;
                lifelineReport = credentialReport;

                UserCodeSupport?.SyncUserCodes();
            }
            else
            {
                return SvResultNames.Failed;
            }


            if (lifelineReport != null)
            {
                SendUnsolicitedReport(lifelineReport, srcNode);
            }

            return SvResultNames.Successful;
        }

        private byte[] PrepareCredentialReportCmd(COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_GET credentialGetCmd)
        {
            COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_REPORT rpt = null;

            ushort userUniqueId = Tools.ByteArrayToUInt16(credentialGetCmd.userUniqueIdentifier);
            var credentialType = (CredentialType)credentialGetCmd.credentialType.Value;
            ushort credentialSlot = Tools.ByteArrayToUInt16(credentialGetCmd.credentialSlot);

            UserCredential credential = null, nextCredential = null;


            if (userUniqueId != 0 && credentialType != 0 && credentialSlot != 0) // CC:0083.01.0B.11.003 
            {
                var credentialsList = UserCredentialSupportData.UserCredentials.FindAll(x => x.UserUniqeId == userUniqueId).OrderBy(x => x.SlotId).OrderBy(x => x.Type).ToList();
                credential = credentialsList.Find(x => x.Type == credentialType && x.SlotId == credentialSlot);
                nextCredential = GetNextCredential(credentialsList, credential);
            }
            else if (userUniqueId != 0 && credentialType == 0 && credentialSlot == 0) // CC:0083.01.0B.11.005
            {
                var credentialsList = UserCredentialSupportData.UserCredentials.FindAll(x => x.UserUniqeId == userUniqueId).OrderBy(x => x.SlotId).OrderBy(x => x.Type).ToList();
                credential = credentialsList.FirstOrDefault();
                nextCredential = GetNextCredential(credentialsList, credential);
            }
            else if (userUniqueId == 0 && credentialType != 0 && credentialSlot != 0) // CC:0083.01.0B.11.008
            {
                var credentialsList = UserCredentialSupportData.UserCredentials.OrderBy(x => x.SlotId).OrderBy(x => x.Type).ToList();
                credential = credentialsList.Find(x => x.SlotId == credentialSlot && x.Type == credentialType);
                nextCredential = GetNextCredential(credentialsList, credential);
            }
            else if (userUniqueId == 0 && credentialType != 0 && credentialSlot == 0) // CC:0083.01.0B.11.007
            {
                var credentialsList = UserCredentialSupportData.UserCredentials.FindAll(x => x.Type == credentialType).OrderBy(x => x.SlotId).ToList();
                credential = credentialsList.FirstOrDefault();
                nextCredential = GetNextCredential(credentialsList, credential);
            }
            else if (userUniqueId != 0 && credentialType != 0 && credentialSlot == 0) // CC:0083.01.0B.11.010
            {
                var credentialsList = UserCredentialSupportData.UserCredentials.FindAll(x => x.UserUniqeId == userUniqueId).OrderBy(x => x.SlotId).OrderBy(x => x.Type).ToList();
                credential = credentialsList.FirstOrDefault(x => x.Type == credentialType);
                nextCredential = GetNextCredential(credentialsList, credential);
            }
            else if (userUniqueId == 0 && credentialType == 0 && credentialSlot == 0) // CC:0083.01.0B.11.011
            {
                var credentialsList = UserCredentialSupportData.UserCredentials.OrderBy(x => x.UserUniqeId).OrderBy(x => x.Type).OrderBy(x => x.SlotId).ToList();
                credential = credentialsList.FirstOrDefault();
                nextCredential = GetNextCredential(credentialsList, credential);
            }
            else
            {
                return null;
            }

            rpt = credential?.CreateUserCredentialReport(CredentialReportType.ResponseToGet) ?? new COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_REPORT();

            // CC:0083.01.0B.11.004, CC:0083.01.0B.11.012 
            if (credential == null)
            {              
                var credentialTypeSettings = UserCredentialSupportData.SupportedCredentialTypes.FirstOrDefault(x => x.Type == credentialType);

                rpt.credentialReportType = (byte)CredentialReportType.ResponseToGet;
                rpt.userUniqueIdentifier = Tools.GetBytes(userUniqueId);
                rpt.credentialType = (byte)credentialType;
                rpt.credentialSlot = Tools.GetBytes(credentialSlot);
                rpt.credentialModifierType = (byte)ModifierType.DNE;
                rpt.credentialModifierNodeId = new byte[] { 0x00, 0x00 };
                rpt.properties1.crb = (byte)(credentialTypeSettings?.ReadBack == true ? 0x01 : 0x00);
            }

            rpt.nextCredentialType = (byte)(nextCredential?.Type ?? 0);
            rpt.nextCredentialSlot = nextCredential != null ? Tools.GetBytes(nextCredential.SlotId) : new byte[] { 0x00, 0x00 };


            return rpt;
        }

        private UserCredential GetNextCredential(List<UserCredential> credentialsList, UserCredential currentCredential)
        {
            int credentialIndex;
            if (currentCredential != null && (credentialIndex = credentialsList.IndexOf(currentCredential)) < credentialsList.Count - 1)
            {
                return credentialsList[credentialIndex + 1];
            }

            return null;
        }

        private UserCredential GetNextUsersCredential(UserCredential currentCredential)
        {
            var credentialsList = UserCredentialSupportData.UserCredentials.FindAll(x => x.UserUniqeId == currentCredential.UserUniqeId).OrderBy(x => x.SlotId).OrderBy(x => x.Type).ToList();
            //credential = credentialsList.Find(x => x.Type == credentialType && x.SlotId == credentialSlot);
            return GetNextCredential(credentialsList, currentCredential);
        }

        private SvResultNames HandleCredentialLearnStart(COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_LEARN_START learnStartCmd, NodeTag srcNode, out byte[] rpt)
        {
            rpt = null;

            ushort userUniqueId = Tools.ByteArrayToUInt16(learnStartCmd.userUniqueIdentifier);
            var credentialType = (CredentialType)learnStartCmd.credentialType.Value;
            ushort credentialSlot = Tools.ByteArrayToUInt16(learnStartCmd.credentialSlot);
            var opType = (CredentialLearnOperationType)learnStartCmd.properties1.operationType;
            byte learnTimeout = learnStartCmd.credentialLearnTimeout.Value;

            UserSlot user = UserCredentialSupportData.UserSlots.Find(x => x.UserUniqeId == userUniqueId);
            UserCredential existingCredential = UserCredentialSupportData.UserCredentials.Find(x => x.UserUniqeId == userUniqueId && x.SlotId == credentialSlot && x.Type == credentialType);

            var credentialTypeSettings = UserCredentialSupportData.SupportedCredentialTypes.FirstOrDefault(x => x.Type == credentialType);

            if (credentialTypeSettings == null)
            {
                return SvResultNames.Failed;
            }

            if (credentialTypeSettings.CredentialLearnSupport == false)
            {
                return SvResultNames.Undefined;
            }

            if (user == null)
            {
                return SvResultNames.Failed;
            }

            if (opType == CredentialLearnOperationType.CredentialLearnAdd && UserCredentialSupportData.UserCredentials.FindAll(x => x.Type == credentialType).Count >= credentialTypeSettings.NoOfSupportedSlots)
            {
                return SvResultNames.Failed;
            }

            if (CredentialLearning.LearningActive)
            {
                var credentialLearningStatusReport = (COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_LEARN_REPORT)CredentialLearning;
                credentialLearningStatusReport.credentialLearnStatus = (byte)CredentialLearnStatus.AlreadyInProgress;
                rpt = credentialLearningStatusReport;
                return SvResultNames.Successful;
            }

            CredentialLearning.LearningActive = true;
            CredentialLearning.CredentialSlot = credentialSlot;
            CredentialLearning.LearningSteps = credentialTypeSettings.CredentialLearnNoOfSteps;
            CredentialLearning.RemainingSteps = credentialTypeSettings.CredentialLearnNoOfSteps;
            CredentialLearning.CredentialType = credentialType;
            CredentialLearning.UserUniqueId = userUniqueId;
            CredentialLearning.NodeStartedLearning = srcNode;
            CredentialLearning.OperationType = opType;


            if (learnTimeout == 0)
            {
                CredentialLearning.LearningActive = false;

                var credentialLearningStatusReport = (COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_LEARN_REPORT)CredentialLearning;
                credentialLearningStatusReport.credentialLearnStatus = (byte)CredentialLearnStatus.Timeout;
                credentialLearningStatusReport.credentialLearnStepsRemaining = 0;

                SendUnsolicitedReportToFixedDestination(credentialLearningStatusReport, CredentialLearning.NodeStartedLearning, InstanceNodeTag);

                return SvResultNames.Successful;

            }

            CredentialLearning.Timeout = new Timer()
            {
                AutoReset = false,
                Interval = learnTimeout * 1000,
                Enabled = true
            };
            CredentialLearning.Timeout.Elapsed += (object sender, ElapsedEventArgs e) => {
                CredentialLearning.LearningActive = false;

                var credentialLearningStatusReport = (COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_LEARN_REPORT)CredentialLearning;
                credentialLearningStatusReport.credentialLearnStatus = (byte)CredentialLearnStatus.Timeout;
                credentialLearningStatusReport.credentialLearnStepsRemaining = 0;

                SendUnsolicitedReportToFixedDestination(credentialLearningStatusReport, CredentialLearning.NodeStartedLearning, InstanceNodeTag);

                CredentialLearning.Timeout = null;
            };


            var startLearningReport = (COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_LEARN_REPORT)CredentialLearning;
            startLearningReport.credentialLearnStatus = (byte)CredentialLearnStatus.Started;
            rpt = startLearningReport;

            if (CredentialLearningStarted != null)
            {
                Task.Run(async () => {
                    CredentialLearningStarted(this);
                });

            }

            return SvResultNames.Successful;
        }

        private SvResultNames HandleCredentialAssociationSetCmd(COMMAND_CLASS_USER_CREDENTIAL.USER_CREDENTIAL_ASSOCIATION_SET associationSetCmd, NodeTag srcNode, out byte[] rpt)
        {
            rpt = null;

            var credentialType = (CredentialType)associationSetCmd.credentialType.Value;
            ushort srcCredentialSlot = Tools.ByteArrayToUInt16(associationSetCmd.sourceCredentialSlot);
            ushort dstUserUniqueId = Tools.ByteArrayToUInt16(associationSetCmd.destinationUserUniqueIdentifier);
            ushort dstCredentialSlot = Tools.ByteArrayToUInt16(associationSetCmd.destinationCredentialSlot);

            var dstUser = UserCredentialSupportData.UserSlots.Find(x => x.UserUniqeId == dstUserUniqueId);

            var srcCredential = UserCredentialSupportData.UserCredentials.Find(x => x.SlotId == srcCredentialSlot && x.Type == credentialType);
            var credentialTypeSettings = UserCredentialSupportData.SupportedCredentialTypes.FirstOrDefault(x => x.Type == credentialType);

            var srcUser = UserCredentialSupportData.UserSlots.Find(x => x.UserUniqeId == srcCredential?.UserUniqeId);

            if (credentialTypeSettings == null)
            {
                rpt = SetupCredentialAssociationReport(associationSetCmd, UserCredentialAssociationStatus.SourceCredentialTypeInvalid);
                return SvResultNames.Failed;
            }

            if (srcCredentialSlot == 0 || srcCredentialSlot > credentialTypeSettings.NoOfSupportedSlots)
            {
                rpt = SetupCredentialAssociationReport(associationSetCmd, UserCredentialAssociationStatus.SourceCredentialSlotInvalid);
                return SvResultNames.Failed;
            }

            if (dstUserUniqueId == 0 || dstUserUniqueId > UserCredentialSupportData.NoOfSupportedUsers)
            {
                rpt = SetupCredentialAssociationReport(associationSetCmd, UserCredentialAssociationStatus.DestinationUserUniqueIdentifierInvalid);
                return SvResultNames.Failed;
            }

            if (srcCredential == null)
            {
                rpt = SetupCredentialAssociationReport(associationSetCmd, UserCredentialAssociationStatus.SourceCredentialSlotEmpty);
                return SvResultNames.Failed;
            }

            if (dstUser == null)
            {
                rpt = SetupCredentialAssociationReport(associationSetCmd, UserCredentialAssociationStatus.DestinationUserUniqueIdentifierNonexistent);
                return SvResultNames.Failed;
            }

            if (dstCredentialSlot == 0 || dstCredentialSlot > credentialTypeSettings.NoOfSupportedSlots)
            {
                rpt = SetupCredentialAssociationReport(associationSetCmd, UserCredentialAssociationStatus.DestinationCredentialSlotInvalid);
                return SvResultNames.Failed;
            }

            if (srcCredentialSlot != dstCredentialSlot && UserCredentialSupportData.UserCredentials.FirstOrDefault(x => x.SlotId == dstCredentialSlot && x.Type == credentialType) != null)
            {
                rpt = SetupCredentialAssociationReport(associationSetCmd, UserCredentialAssociationStatus.DestinationCredentialSlotOccupied);
                return SvResultNames.Failed;
            }

            if (srcUser != null && srcUser.UserUniqeId == dstUserUniqueId && srcCredentialSlot == dstCredentialSlot)
            {
                // Nothing has changed
                rpt = SetupCredentialAssociationReport(associationSetCmd, UserCredentialAssociationStatus.Success);
                return SvResultNames.Successful;
            }

            srcCredential.UserUniqeId = dstUserUniqueId;
            srcCredential.SlotId = dstCredentialSlot;
            srcCredential.ModifierType = ModifierType.ZWave;
            srcCredential.ModifierNodeId = srcNode.Id;

            rpt = SetupCredentialAssociationReport(associationSetCmd, UserCredentialAssociationStatus.Success);

            // CC:0083.01.13.11.002: Send updates to Lifeline
            SendUnsolicitedReport(rpt, srcNode);

            UserCodeSupport?.SyncUserCodes();
            return SvResultNames.Successful;
        }

        private SvResultNames HandleAdminCodeSetCmd(COMMAND_CLASS_USER_CREDENTIAL.ADMIN_PIN_CODE_SET adminCodeSetCmd, NodeTag srcNode, out byte[] rpt)
        {
            rpt = null;
            byte[] lifelineReport = null;

            var pinCodeSettings = UserCredentialSupportData.SupportedCredentialTypes.FirstOrDefault(x => x.Type == CredentialType.PinCode);

            byte minCodeLength = pinCodeSettings?.MinCredentialDataLength ?? 4;
            byte maxCodeLength = pinCodeSettings?.MaxCredentialDataLength ?? 10;

            // CC:0083.01.1A.13.002
            if (UserCredentialSupportData.AdminCodeSupport == false)
            {
                rpt = PrepareAdminPinCodeReport(AdminPinCodeOperationResult.ErrorAdminCodeNotSupported);
                return SvResultNames.Failed;
            }


            if (adminCodeSetCmd.properties1.adminPinCodeLength == 0)
            {
                // CC:0083.01.1A.13.003
                if (UserCredentialSupportData.AdminCodeDeactivationSupport == false)
                {
                    rpt = PrepareAdminPinCodeReport(AdminPinCodeOperationResult.ErrorAdminCodeDeactivationNotSupported);
                    return SvResultNames.Failed;
                }
                else
                {
                    if (string.IsNullOrEmpty(UserCredentialSupportData.AdminCode) == false)
                    {
                        rpt = PrepareAdminPinCodeReport(AdminPinCodeOperationResult.Modified);
                        lifelineReport = rpt;
                    }
                    else
                    {
                        rpt = PrepareAdminPinCodeReport(AdminPinCodeOperationResult.Unmodified);
                    }

                    UserCredentialSupportData.AdminCode = string.Empty;
                    UserCodeSupport?.SyncUserCodes();
                }
            } 
            else if (adminCodeSetCmd.properties1.adminPinCodeLength >= minCodeLength && adminCodeSetCmd.properties1.adminPinCodeLength <= maxCodeLength)
            {
                if (pinCodeSettings == null)
                {
                    // CC0083.01.00.11.005 - Pin Code Credentials must be supported
                    return SvResultNames.Failed;
                }

                if (EncodeString(adminCodeSetCmd.adminCode.ToArray(), UserNameEncoding.ASCII, maxCodeLength, out string adminPinCode) == false)
                {
                    // CC:0083.01.1A.11.010 - contains non-ascii chars: ignore command
                    return SvResultNames.Failed;
                }

                if (UserCredentialSupportData.UserCredentials.Find(x => x.Type == CredentialType.PinCode && x.Data.SequenceEqual(adminCodeSetCmd.adminCode)) != null)
                {
                    // CC:0083.01.1A.13.005
                    rpt = PrepareAdminPinCodeReport(AdminPinCodeOperationResult.FailDuplicateCredential);
                    return SvResultNames.Failed;
                }

                if (UserCredentialSupportData.AdminCode == adminPinCode)
                {
                    // CC:0083.01.1A.13.004
                    rpt = PrepareAdminPinCodeReport(AdminPinCodeOperationResult.Unmodified, adminPinCode);
                    return SvResultNames.Successful;
                }

                UserCredentialSupportData.AdminCode = adminPinCode;
                UserCodeSupport?.SyncUserCodes();
                rpt = PrepareAdminPinCodeReport(AdminPinCodeOperationResult.Modified, adminPinCode);
                lifelineReport = rpt;
            }
            else
            {
                // CC:0083.01.1A.11.011
                rpt = PrepareAdminPinCodeReport(AdminPinCodeOperationResult.FailManufacturerSecurityRules);
                return SvResultNames.Failed;
            }

            if (lifelineReport != null)
            {
                SendUnsolicitedReport(lifelineReport, srcNode);
            }

            return SvResultNames.Successful;

        }


        private byte[] PrepareAdminPinCodeReport()
        {
            return PrepareAdminPinCodeReport(AdminPinCodeOperationResult.ResponseToGet, UserCredentialSupportData.AdminCode);
        }

        private byte[] PrepareAdminPinCodeReport(AdminPinCodeOperationResult operationResult, string adminPinCode = null)
        {
            COMMAND_CLASS_USER_CREDENTIAL.ADMIN_PIN_CODE_REPORT rpt = new COMMAND_CLASS_USER_CREDENTIAL.ADMIN_PIN_CODE_REPORT();

            rpt.properties1.adminPinCodeOperationResult = (byte) operationResult;
            rpt.properties1.adminPinCodeLength = (byte)(adminPinCode?.Length ?? 0);
            rpt.adminCode = adminPinCode != null ? DecodeString(adminPinCode, UserNameEncoding.ASCII) : new byte[0];

            return rpt;

        }

        private byte[] PrepareAllUsersChecksumReport()
        {

            if (UserCredentialSupportData.AllUsersChecksumSupport == false)
            {
                return null;
            }

            byte[] checksumBuffer = new byte[0];

            var users = UserCredentialSupportData.UserSlots.OrderBy(x => x.UserUniqeId).ToList();

            ushort crc = 0;

            if (users.Count > 0)
            {
                users.ForEach(user => {
                    checksumBuffer = AppendUserAndCredentialDataToBuffer(checksumBuffer, user, true);
                });

                crc = CommandClassSupportUtils.CalculateCRC16(checksumBuffer);
            }
            //Console.WriteLine("DATABUF: " + string.Join("", checksumBuffer.Select(x => x.ToString("X2"))));
            //Console.WriteLine("CHECKSUM: " + crc.ToString("X"));

            var report = new COMMAND_CLASS_USER_CREDENTIAL.ALL_USERS_CHECKSUM_REPORT()
            {
                allUsersChecksum = Tools.GetBytes(crc)
            };

            return report;
        }

        private byte[] PrepareUserChecksumReport(COMMAND_CLASS_USER_CREDENTIAL.USER_CHECKSUM_GET userChecksumGetCmd)
        {
            if (UserCredentialSupportData.UserChecksumSupport == false)
            {
                return null;
            }

            byte[] checksumBuffer = new byte[0];

            ushort uniqueUserId = Tools.ByteArrayToUInt16(userChecksumGetCmd.userUniqueIdentifier);
            UserSlot user = UserCredentialSupportData.UserSlots.Find(x => x.UserUniqeId == uniqueUserId);

            ushort crc = 0;

            if (user != null)
            {
                checksumBuffer = AppendUserAndCredentialDataToBuffer(checksumBuffer, user, false);

                crc = CommandClassSupportUtils.CalculateCRC16(checksumBuffer);
            }
            //Console.WriteLine("DATABUF: " + string.Join("", checksumBuffer.Select(x => x.ToString("X2"))));
            //Console.WriteLine("CHECKSUM: " + crc.ToString("X"));

            var report = new COMMAND_CLASS_USER_CREDENTIAL.USER_CHECKSUM_REPORT()
            {
                userUniqueIdentifier = userChecksumGetCmd.userUniqueIdentifier,
                userChecksum = Tools.GetBytes(crc)
            };

            return report;
        }

        private byte[] PrepareCredentialChecksumReport(COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_CHECKSUM_GET credentialChecksumGetCmd)
        {
            if (UserCredentialSupportData.CredentialChecksumSupport == false)
            {
                return null;
            }

            byte[] checksumBuffer = new byte[0];

            var credentialType = (CredentialType)credentialChecksumGetCmd.credentialType.Value;

            var credentials = UserCredentialSupportData.UserCredentials.FindAll(x => x.Type == credentialType).OrderBy(x => x.SlotId).ToList();

            ushort crc = 0;

            if (credentials.Count > 0)
            {
                credentials.ForEach(credential => {
                    checksumBuffer = AppendCredentialDataToBuffer(checksumBuffer, credential, false);
                });

                crc = CommandClassSupportUtils.CalculateCRC16(checksumBuffer);
            }

            //Console.WriteLine("DATABUF: " + string.Join("", checksumBuffer.Select(x => x.ToString("X2"))));
            //Console.WriteLine("CHECKSUM: " + crc.ToString("X"));

            var report = new COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_CHECKSUM_REPORT()
            {
                credentialType = credentialChecksumGetCmd.credentialType.Value,
                credentialChecksum = Tools.GetBytes(crc)
            };

            return report;
        }

        private byte[] AppendUserAndCredentialDataToBuffer(byte[] buffer, UserSlot user, bool includeUserUniqeId)
        {
            var userName = DecodeString(user.Name, user.NameEncoding);

            if (includeUserUniqeId)
            {
                buffer = buffer.Concat(Tools.GetBytes(user.UserUniqeId)).ToArray();
            }
            buffer = buffer.Append((byte)user.Type).ToArray();
            buffer = buffer.Append((byte)user.ActiveState).ToArray();
            buffer = buffer.Append((byte)user.CredentialRule).ToArray();
            buffer = buffer.Append((byte)user.NameEncoding).ToArray();
            buffer = buffer.Append((byte)userName.Length).ToArray();
            buffer = buffer.Concat(userName).ToArray();

            var credentials = UserCredentialSupportData.UserCredentials.FindAll(y => y.UserUniqeId == user.UserUniqeId).OrderBy(y => y.SlotId).OrderBy(y => y.Type).ToList();

            credentials.ForEach(credential => {
                buffer = AppendCredentialDataToBuffer(buffer, credential, true);
            });
            return buffer;
        }

        private byte[] AppendCredentialDataToBuffer(byte[] buffer, UserCredential credential, bool includeCredentialType)
        {
            var data = credential.GetDataOrHash();

            if (includeCredentialType)
            {
                buffer = buffer.Append((byte)credential.Type).ToArray();
            }
            buffer = buffer.Concat(Tools.GetBytes(credential.SlotId)).ToArray();
            buffer = buffer.Append((byte)data.Length).ToArray();
            buffer = buffer.Concat(data).ToArray();

            return buffer;
        }

        private COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_REPORT SetupDeleteCredentialReport(ushort userUniqueId, CredentialType credentialType, ushort credentialSlot, CredentialReportType reportType = CredentialReportType.CredentialDeleted)
        {
            var credentialTypeSettings = UserCredentialSupportData.SupportedCredentialTypes.FirstOrDefault(x => x.Type == credentialType);

            var rpt = new COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_REPORT()
            {
                credentialReportType = (byte)reportType,
                userUniqueIdentifier = Tools.GetBytes(userUniqueId),
                credentialType = (byte)credentialType,
                credentialSlot = Tools.GetBytes(credentialSlot),
                credentialModifierType = (byte)ModifierType.DNE,
                credentialModifierNodeId = new byte[] { 0x00, 0x00 },
            };

            rpt.properties1.crb = (byte)(credentialTypeSettings?.ReadBack == true ? 0x01 : 0x00);

            return rpt;
        }

        private byte[] SetupCredentialNotificationReport(NotificationAccessControlEvents notificationEvent, ushort userUniqeId, CredentialType credentialType, ushort credentialSlot)
        {
            // Setup Notifications Credential Usage Data (free datastream)
            var data = Tools.GetBytes(userUniqeId).                     // User Unique Identifier as MSB, LSB
                Append((byte) 1).                                       // Number of Credentials
                Append((byte)credentialType).                           // (first) Credential Type
                Concat(Tools.GetBytes(credentialSlot)).ToArray();       // (first) Credential Slot as MSB, LSB

            var notificationReport = new COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_REPORT()
            {
                notificationType = (byte)NotificationTypeNames.AccessControl,
                notificationStatus = 0xFF,
                mevent = (byte)notificationEvent,
                eventParameter = data,
                sequenceNumber = notificationSequenceNumber++
            };
            notificationReport.properties1.eventParametersLength = (byte)notificationReport.eventParameter.Count;

            return notificationReport;
        }

        private byte[] SetupCredentialAssociationReport(COMMAND_CLASS_USER_CREDENTIAL.USER_CREDENTIAL_ASSOCIATION_SET associationSetCmd, UserCredentialAssociationStatus status)
        {
            var credentialAssociationReport = new COMMAND_CLASS_USER_CREDENTIAL.USER_CREDENTIAL_ASSOCIATION_REPORT()
            {
                credentialType = associationSetCmd.credentialType,
                sourceCredentialSlot = associationSetCmd.sourceCredentialSlot,
                destinationUserUniqueIdentifier = associationSetCmd.destinationUserUniqueIdentifier,
                destinationCredentialSlot = associationSetCmd.destinationCredentialSlot,
                userCredentialAssociationStatus = (byte)status
            };
            return credentialAssociationReport;
        }

        private void DeleteUsersSchedules(ushort userUniqueId)
        {
            if (ScheduleEntryLockSupportData == null)
            {
                return;
            }

            var usersSchedules = ScheduleEntryLockSupportData.ScheduleSlots.FindAll(x => x.UserIdentifier == userUniqueId);

            foreach (var schedule in usersSchedules)
            {
                ScheduleEntryLockSupportData?.ScheduleSlots.Remove(schedule);
            }
        }

        public static bool EncodeString(byte[] data, UserNameEncoding userNameEncoding, int maxLenghtInBytes, out string userName)
        {
            userName = string.Empty;
            bool success = true;

            if (data == null || data.Length == 0)
            {
                return false;
            }


            if (data.Length > maxLenghtInBytes)
            {
                data = data.Take(maxLenghtInBytes).ToArray();
                success = false;
            }

            switch (userNameEncoding)
            {
                case UserNameEncoding.ASCII:
                    var hasInvalidChars = data.Any(x => x > 0x7F);
                    if (hasInvalidChars)
                    {
                        success = false;
                        userName = null;
                    }
                    else
                    {
                        userName = Tools.GetStringFromAsciiBytes(data);
                    }
                    break;

                case UserNameEncoding.ExtendedASCII:
                    userName = Tools.GetStringFromOemExtendedAsciiBytes(data);
                    break;
                case UserNameEncoding.UTF16BE:
                    userName = Tools.GetStringFromUtf16BigEndianBytes(data);
                    break;
            }
            return success;
        }

        public static byte[] DecodeString(string name, UserNameEncoding userNameEncoding)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new byte[0];
            }

            switch (userNameEncoding)
            {
                case UserNameEncoding.ASCII:
                    return Tools.GetAsciiBytes(name);
                case UserNameEncoding.ExtendedASCII:
                    return Tools.GetOemExtendedAsciiBytes(name);
                case UserNameEncoding.UTF16BE:
                    return Tools.GetUtf16BigEndianBytes(name);
            }
            return new byte[0];
        }

        private void HandleExpiringUsers(UserSlot user = null)
        {
            if (user != null && expiringTimers.ContainsKey(user.UserUniqeId) == false)
            {
                var expiringTimer = new Timer(user.ExpiringTimeout * 60000)
                {
                    AutoReset = false,
                };

                expiringTimer.Elapsed += (object source, ElapsedEventArgs e) => {
                    user.ActiveState = UserCredentialUserActiveState.OccupiedDisabled;
                    user.ModifierType = ModifierType.Locally;
                    user.ModifierId = 0;

                    expiringTimers.Remove(user.UserUniqeId);
                    expiringTimer.Stop();
                    expiringTimer.Dispose();


                    COMMAND_CLASS_USER_CREDENTIAL.USER_REPORT userReport = user;
                    userReport.userReportType = (byte)UserReportType.UserModified;
                    SendUnsolicitedReport(userReport, InstanceNodeTag);


                    UserCodeSupport?.SyncUserCodes();
                };

                expiringTimer.Start();

                expiringTimers.Add(user.UserUniqeId, expiringTimer);

            }


            foreach (var timer in expiringTimers)
            {
                var correspondingUser = UserCredentialSupportData.UserSlots.FirstOrDefault(x => x.UserUniqeId == timer.Key);

                if (correspondingUser == null || correspondingUser.Type != UserCredentialUserType.ExpiringUser)
                {
                    timer.Value.Stop();
                    timer.Value.Dispose();
                    expiringTimers.Remove(timer.Key);
                }
            }
        }

        public void LearnCredential(byte[] credentialData)
        {
            if (CredentialLearning.LearningActive)
            {
                if (--CredentialLearning.RemainingSteps == 0)
                {
                    var credential = UserCredential.CreateCredential(CredentialLearning.UserUniqueId, CredentialLearning.CredentialType, CredentialLearning.CredentialSlot, credentialData, ModifierType.Locally);

                    var result = HandleAddOrModifyCredential(
                        CredentialLearning.OperationType == CredentialLearnOperationType.CredentialLearnAdd ? CredentialSetOperationType.Add : CredentialSetOperationType.Modify,
                        credential,
                        CredentialLearning.NodeStartedLearning,
                        out byte[] credentialReport);

                    if (result == SvResultNames.Successful)
                    {
                        StopCredentialLearning(CredentialLearnStatus.Success);

                        //// CC:0083.01.0F.11.001
                        //SendUnsolicitedReportToFixedDestination(credentialReport, CredentialLearning.NodeStartedLearning, InstanceNodeTag);

                        UserCodeSupport?.SyncUserCodes();

                    }
                    else if (CredentialLearning.OperationType == CredentialLearnOperationType.CredentialLearnAdd)
                    {
                        StopCredentialLearning(CredentialLearnStatus.InvalidCredentialLearnAddOperationType);
                    }
                    else
                    {
                        StopCredentialLearning(CredentialLearnStatus.InvalidCredentialLearnModifyOperationType);
                    }

                    if (credentialReport != null)
                    {
                        SendUnsolicitedReportToFixedDestination(credentialReport, CredentialLearning.NodeStartedLearning, InstanceNodeTag);
                    }

                    //UserCredentialSupportData.PrintUsersInfo();
                }
                else
                {
                    CredentialLearning.Timeout.Stop();
                    CredentialLearning.Timeout.Start();

                    var credentialLearningStatusReport = (COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_LEARN_REPORT)CredentialLearning;
                    credentialLearningStatusReport.credentialLearnStatus = (byte)CredentialLearnStatus.Started;

                    SendUnsolicitedReportToFixedDestination(credentialLearningStatusReport, CredentialLearning.NodeStartedLearning, InstanceNodeTag);
                }
            }
        }

        public void StopCredentialLearning(CredentialLearnStatus status)
        {
            if (CredentialLearning.LearningActive)
            {
                CredentialLearning.LearningActive = false;
                CredentialLearning.Timeout.Stop();
                CredentialLearning.Timeout = null;

                var credentialLearningStatusReport = (COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_LEARN_REPORT)CredentialLearning;
                credentialLearningStatusReport.credentialLearnStatus = (byte)status;
                credentialLearningStatusReport.credentialLearnStepsRemaining = 0;

                SendUnsolicitedReportToFixedDestination(credentialLearningStatusReport, CredentialLearning.NodeStartedLearning, InstanceNodeTag);

            }
        }

        public EnterCredentialResult EnterCredential(DoorLockModes doorLockOperationType, CredentialType type, byte[] credentialData)
        {
            EnterCredentialResult result = EnterCredentialResult.Success;

            var notifications = new List<byte[]>();

            var credential = UserCredentialSupportData.UserCredentials.Find(x => x.Type == type && Enumerable.SequenceEqual(x.Data, credentialData));

            if (credential == null)
            {
                notifications.Add(new COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_REPORT()
                {
                    notificationType = (byte)NotificationTypeNames.AccessControl,
                    notificationStatus = 0xFF,
                    mevent = (byte)NotificationAccessControlEvents.InvalidCredentialUsedToAccessTheNode,
                    sequenceNumber = notificationSequenceNumber++
                });

                result = EnterCredentialResult.CredentialNotFound;
            }
            else
            {
                var correspondingUser = UserCredentialSupportData.UserSlots.Find(x => x.UserUniqeId == credential.UserUniqeId);

                if (correspondingUser == null)
                {
                    return EnterCredentialResult.NoUserAssigned;
                }

                if (correspondingUser.ActiveState == UserCredentialUserActiveState.OccupiedDisabled)
                {
                    notifications.Add(SetupCredentialNotificationReport(NotificationAccessControlEvents.ValidCredentialAccessDeniedDueToUserActiveStateBeingSetToOccupiedDisabled, credential.UserUniqeId, credential.Type, credential.SlotId));
                    result = EnterCredentialResult.UserOccupiedDisabled;
                }
                else
                {
                    switch (correspondingUser.Type)
                    {
                        case UserCredentialUserType.GeneralUser:
                            if (CheckForUsersSchedule(correspondingUser.UserUniqeId))
                            {
                                notifications.Add(HandleDoorLockMode(doorLockOperationType, credential));
                            }
                            else
                            {
                                notifications.Add(SetupCredentialNotificationReport(NotificationAccessControlEvents.ValidCredentialAccessDeniedDueToTheUserScheduleBeingInactive, credential.UserUniqeId, credential.Type, credential.SlotId));
                            }
                            break;

                        case UserCredentialUserType.ProgranmmingUser:
                            // CC:0083.01.05.11.023 Programming User (0x03) MUST NOT be scheduled.
                            notifications.Add(HandleDoorLockMode(doorLockOperationType, credential));
                            break;

                        case UserCredentialUserType.NonAccessUser:
                            notifications.Add(SetupCredentialNotificationReport(NotificationAccessControlEvents.NonAccessCredentialEnteredViaLocalInterface, credential.UserUniqeId, credential.Type, credential.SlotId));
                            break;

                        case UserCredentialUserType.DuressUser:
                            if (CheckForUsersSchedule(correspondingUser.UserUniqeId))
                            {
                                if (doorLockOperationType.IsUnlockingType())
                                {
                                    HandleDoorLockMode(doorLockOperationType, credential);
                                    notifications.Add(new COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_REPORT()
                                    {
                                        notificationType = (byte)NotificationTypeNames.EmergencyAlarm,
                                        notificationStatus = 0xFF,
                                        mevent = (byte)NotificationEmergencyAlarmEvents.PanicAlert,
                                        sequenceNumber = notificationSequenceNumber++
                                    });
                                }
                            }
                            else
                            {
                                notifications.Add(SetupCredentialNotificationReport(NotificationAccessControlEvents.ValidCredentialAccessDeniedDueToTheUserScheduleBeingInactive, credential.UserUniqeId, credential.Type, credential.SlotId));
                            }
                            break;

                        case UserCredentialUserType.DisposableUser:
                            if (CheckForUsersSchedule(correspondingUser.UserUniqeId))
                            {
                                if (doorLockOperationType.IsUnlockingType())
                                {
                                    notifications.Add(HandleDoorLockMode(doorLockOperationType, credential));

                                    correspondingUser.ActiveState = UserCredentialUserActiveState.OccupiedDisabled;

                                    COMMAND_CLASS_USER_CREDENTIAL.USER_REPORT userReport = correspondingUser;
                                    userReport.userReportType = (byte)UserReportType.UserModified;
                                    notifications.Add(userReport);

                                    UserCodeSupport?.SyncUserCodes();
                                }
                            }
                            else
                            {
                                notifications.Add(SetupCredentialNotificationReport(NotificationAccessControlEvents.ValidCredentialAccessDeniedDueToTheUserScheduleBeingInactive, credential.UserUniqeId, credential.Type, credential.SlotId));
                            }
                            break;

                        case UserCredentialUserType.ExpiringUser:
                            if (CheckForUsersSchedule(correspondingUser.UserUniqeId))
                            {
                                notifications.Add(HandleDoorLockMode(doorLockOperationType, credential));
                                HandleExpiringUsers(correspondingUser);
                            }
                            else
                            {
                                notifications.Add(SetupCredentialNotificationReport(NotificationAccessControlEvents.ValidCredentialAccessDeniedDueToTheUserScheduleBeingInactive, credential.UserUniqeId, credential.Type, credential.SlotId));
                            }
                            break;

                        case UserCredentialUserType.RemoteOnlyUser:
                            break;

                    }

                }
            }


            foreach (var notification in notifications)
            {
                SendUnsolicitedReport(notification, NodeTag.Empty);
            }


            return result;
        }

        private byte[] HandleDoorLockMode(DoorLockModes doorLockMode, UserCredential credential)
        {
            byte[] notification;


            // Door Lock Operation via Association
            if (RootAssociationData?.FindGroupByCommand(COMMAND_CLASS_DOOR_LOCK.ID, COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_SET.ID) is List<AssociationGroupEx> doorLockAssociationGroups &&
                doorLockAssociationGroups.Count > 0)
            {
                var doorLockOperationSetCmd = new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_SET()
                {
                    doorLockMode = (byte) doorLockMode
                };

                foreach (var group in doorLockAssociationGroups)
                {
                    foreach (AssociationGroupEx.NodeIdEx nodeId in group.AssociatedNodeIds)
                    {
                        SendUnsolicitedReport(doorLockOperationSetCmd, nodeId, NodeTag.Empty);
                    }
                }
            }

            // local Door Lock support
            DoorLockSupport?.HandleOperationSet(doorLockMode, false, NodeTag.Empty);


            if (doorLockMode.IsUnlockingType())
            {
                notification = SetupCredentialNotificationReport(NotificationAccessControlEvents.CredentialUnlockOpenOperation, credential.UserUniqeId, credential.Type, credential.SlotId);
            }
            else
            {
                notification = SetupCredentialNotificationReport(NotificationAccessControlEvents.CredentialLockCloseOperation, credential.UserUniqeId, credential.Type, credential.SlotId);
            }


            return notification;

        }

        private bool CheckForUsersSchedule(ushort userUniqueId)
        {
            return UserCredentialSupportData.UserScheduleSupport == false || (ScheduleEntryLockSupportData?.CheckUsersScheduleForAccess(userUniqueId) ?? true);
        }

        /// <summary>
        /// Synchronize User Credential Pin Codes with User Codes where User Code is leading
        /// </summary>
        public void SyncPinCodeCredentials()
        {
            if (UserCodeSupport == null)
            {
                return;
            }

            var pinCodeTypeSettings = UserCredentialSupportData.SupportedCredentialTypes.FirstOrDefault(x => x.Type == CredentialType.PinCode);

            if (pinCodeTypeSettings == null) 
            {
                return;
            }

            if (UserCodeSupport.CommandClassSupportData is UserCodeSupportData userCodeSupportData)
            {
                var pinCodeCredentials = UserCredentialSupportData.UserCredentials.FindAll(x => x.Type == CredentialType.PinCode);

                foreach (var userCode in userCodeSupportData.Users)
                {
                    var pinCodeCredential = pinCodeCredentials.Find(x => x.SlotId == userCode.UserId);

                    if (pinCodeCredential != null)
                    {
                        if (userCode.UserIdStatusV2 == UserCodeUserIdStatusV2.Available)
                        {
                            UserCredentialSupportData.UserCredentials.Remove(pinCodeCredential);
                            continue;
                        }

                        var correspondingUser = UserCredentialSupportData.UserSlots.Find(x => x.UserUniqeId == pinCodeCredential.UserUniqeId);

                        if (correspondingUser != null)
                        {
                            if (userCode.MapToUserCredentialUserType(UserCodeSupport.Version, correspondingUser, false))
                            {
                                COMMAND_CLASS_USER_CREDENTIAL.USER_REPORT userReport = correspondingUser;
                                userReport.userReportType = (byte)UserReportType.UserModified;

                                SendUnsolicitedReport(userReport, NodeTag.Empty);

                            }

                            if (pinCodeCredential.Data.SequenceEqual(userCode.UserCode) == false)
                            {
                                pinCodeCredential.Data = userCode.UserCode.ToArray();

                                SendUnsolicitedReport(pinCodeCredential.CreateUserCredentialReport(CredentialReportType.CredentialModified), NodeTag.Empty);
                            }
                        }

                    } 
                    else
                    {
                        if (userCode.UserIdStatusV2 == UserCodeUserIdStatusV2.Available)
                        {
                            continue;
                        }

                        var newUserUniqueId = UserCredentialSupportData.GetFreeUserUniqueId();

                        if (newUserUniqueId != null && 
                            UserCredentialSupportData.UserCredentials.FindAll(x => x.Type == CredentialType.PinCode).Count() < pinCodeTypeSettings.NoOfSupportedSlots)

                        {
                            var newUser = new UserSlot()
                            {
                                CredentialRule = UserCredentialRule.Single,
                                UserUniqeId = newUserUniqueId ?? 0
                            };

                            userCode.MapToUserCredentialUserType(UserCodeSupport.Version, newUser, true);

                            var newCredential = 
                                new UserCredential(
                                    newUser.UserUniqeId,
                                    CredentialType.PinCode,
                                    userCode.UserId)
                                {
                                    Data = userCode.UserCode
                                };

                            UserCredentialSupportData.AddOrModifyUser(newUser, new List<UserCredential>() { newCredential });

                            COMMAND_CLASS_USER_CREDENTIAL.USER_REPORT userReport = newUser;
                            userReport.userReportType = (byte)UserReportType.UserAdded;
                            SendUnsolicitedReport(userReport, NodeTag.Empty);

                            SendUnsolicitedReport(newCredential.CreateUserCredentialReport(CredentialReportType.CredentialAdded), NodeTag.Empty);
                        }
                    }
                }

                foreach (var pinCodeCredential in pinCodeCredentials)
                {
                    var userCode = userCodeSupportData.Users.Find(x => x.UserId == pinCodeCredential.SlotId);

                    if (userCode == null || userCode.UserIdStatusV2 == UserCodeUserIdStatusV2.Available) 
                    {
                        UserCredentialSupportData.UserCredentials.Remove(pinCodeCredential);

                        SendUnsolicitedReport(pinCodeCredential.CreateUserCredentialReport(CredentialReportType.CredentialDeleted), NodeTag.Empty);
                    }

                }

                if (userCodeSupportData.AdminCodeSup && UserCredentialSupportData.AdminCodeSupport)
                {
                    if (userCodeSupportData.AdminCode.SequenceEqual(UserCodeSupport.EmptyCode))
                    {
                        UserCredentialSupportData.AdminCode = string.Empty;
                    } 
                    else
                    {
                        if (EncodeString(userCodeSupportData.AdminCode, UserNameEncoding.ASCII, 10, out string adminPinCode) && 
                            UserCredentialSupportData.AdminCode != adminPinCode)
                        {
                            UserCredentialSupportData.AdminCode = adminPinCode;

                            SendUnsolicitedReport(PrepareAdminPinCodeReport(AdminPinCodeOperationResult.Modified), NodeTag.Empty);
                        }
                    }
                    
                }

            }

            //LogUserCodes();
            //UserCredentialSupportData.PrintUsersInfo();
        }

        public override void StartEmulation()
        {
            if (UserCodeSupport != null && UserCodeSupport.UserCredentialSupport == this)
            {
                UserCodeSupport.SyncUserCodes(false);
            }
        }

        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is UserCodeSupportData newData)
            {
            }
        }


        public enum EnterCredentialResult
        {
            CredentialNotFound,
            NoUserAssigned,
            UserOccupiedDisabled,
            Success
        }

        private void LogUserCodes()
        {
            if (UserCodeSupport != null)
            {
                Console.WriteLine("----- USER CODE ---");

                var usrCodeSupportData = UserCodeSupport.CommandClassSupportData as UserCodeSupportData;

                foreach (var usrCode in usrCodeSupportData.Users)
                {
                    Console.WriteLine($"{usrCode.UserId} - {usrCode.UserIdStatusV2}: {Encoding.ASCII.GetString(usrCode.UserCode)}");
                }

            }
        }
    }

    public class UserCredentialSupportData : CommandClassSupportData, INodeItem
    {
        private const int UserIndex = 2;

        [ParameterSeparator(1, "User Capabilities", UserIndex + 1)]

        [CustomDataParameter(1, "Number of Supported Users", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.UShort, EditType.Config, UserIndex + 2)]
        public ushort NoOfSupportedUsers { get; set; } = 0;

        [CustomDataParameter(1, "Supported Credential Rules", CommandClassesImpl.UiControl.ListCheckBox, CommandClassesImpl.InputType.Enum, EditType.Config, UserIndex + 3)]
        public List<UserCredentialRule> SupportedCredentialRules { get; set; } = new List<UserCredentialRule>();

        [CustomDataParameter(1, "Max Length Of User Name", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Int_1_255, EditType.Config, UserIndex + 4)]
        public byte MaxUserNameLength { get; set; } = 255;

        /// <summary>
        /// List of 'Supported User Name Encodings' as advertised within the User Capabilities Report.
        /// These may not be complete because of the legacy fallback:
        /// If empty, it means that ASCII, OEM Extended ASCII and UTF-16 (Big Endian) are all supported.
        /// </summary>
        [CustomDataParameter(1, "User Name Encoding Support", CommandClassesImpl.UiControl.ListCheckBox, CommandClassesImpl.InputType.Enum, EditType.Config, UserIndex + 5)]
        public List<UserNameEncoding> UserNameEncodingsAdvertisedAsSupported { get; set; } = new List<UserNameEncoding>();

        [CustomDataParameter(1, "User Schedule Support", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config, UserIndex + 6)]
        public bool UserScheduleSupport { get; set; } = false;

        [CustomDataParameter(1, "All Users Checksum Support", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config, UserIndex + 7)]
        public bool AllUsersChecksumSupport { get; set; } = true;

        [CustomDataParameter(1, "Users Checksum Support", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config, UserIndex + 8)]
        public bool UserChecksumSupport { get; set; } = true;

        [CustomDataParameter(1, "Supported User Types", CommandClassesImpl.UiControl.ListCheckBox, CommandClassesImpl.InputType.Enum, EditType.Config, UserIndex + 9)]
        public List<UserCredentialUserType> SupportedUserTypes { get; set; } = new List<UserCredentialUserType>();

        public List<SupportedCredentialType> SupportedCredentialTypes { get; set; } = new List<SupportedCredentialType>();

        [ParameterSeparator(1, "Credential Capabilities", UserIndex + 21)]

        [CustomDataParameter(1, "Credential Checksum Support", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config, UserIndex + 22)]
        public bool CredentialChecksumSupport { get; set; } = true;

        [CustomDataParameter(1, "Admin Code Support", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config, UserIndex + 23)]
        public bool AdminCodeSupport { get; set; } = true;

        [CustomDataParameter(1, "Admin Code Deactivation Support", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config, UserIndex + 24)]
        public bool AdminCodeDeactivationSupport { get; set; } = true;

        [ParameterSeparator(1, "Admin Code", UserIndex + 41)]

        [CustomDataParameter(1, "Admin Code", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Text_127, EditType.Config | EditType.Edit, UserIndex + 42, withoutValidation:true)]
        public string AdminCode { get; set; } = "";

        [ParameterSeparator(1, "Users", UserIndex)]
        [JsonIgnore]
        public List<UserSlotViewModel> UserSlotViewModels
        {
            get
            {
                return GetUserSlotViewModels();
            }
        }

        public List<UserSlot> UserSlots { get; set; } = new List<UserSlot>();

        public List<UserCredential> UserCredentials { get; set; } = new List<UserCredential>();

        public UserCredentialSupportData()
        {
        }

        public UserCredentialSupportData(ushort noOfSupportedUsers, List<UserCredentialRule> supportedCredentialRules, List<UserCredentialUserType> supportedUserTypes)
        {
            NoOfSupportedUsers = noOfSupportedUsers;
            SupportedCredentialRules = supportedCredentialRules;
            SupportedUserTypes = supportedUserTypes;
        }

        public SupportedCredentialType EnableCredentialType(CredentialType credentialType, ushort noOfSupportedSlots, byte minDataLength, byte maxDataLength)
        {
            SupportedCredentialType supportedCredentialType = SupportedCredentialTypes.FirstOrDefault(x => x.Type == credentialType);

            if (supportedCredentialType == null)
            {
                supportedCredentialType = new SupportedCredentialType();
                SupportedCredentialTypes.Add(supportedCredentialType);
            }

            supportedCredentialType.Type = credentialType;
            supportedCredentialType.NoOfSupportedSlots = noOfSupportedSlots;
            supportedCredentialType.MinCredentialDataLength = minDataLength;
            supportedCredentialType.MaxCredentialDataLength = maxDataLength;
            supportedCredentialType.CredentialLearnRecommendedTimeout = 0;
            supportedCredentialType.CredentialLearnNoOfSteps = 0;
            supportedCredentialType.CredentialLearnSupport = false;

            return supportedCredentialType;
        }

        public SupportedCredentialType EnableCredentialType(CredentialType credentialType, bool clSupport, ushort noOfSupportedSlots,
            byte minDataLength, byte maxDataLength, byte clRecommendedTimeout, byte clNoOfSteps, bool readback)
        {
            SupportedCredentialType supportedCredentialType = EnableCredentialType(credentialType, noOfSupportedSlots, minDataLength, maxDataLength);

            supportedCredentialType.CredentialLearnRecommendedTimeout = clRecommendedTimeout;
            supportedCredentialType.CredentialLearnNoOfSteps = clNoOfSteps;
            supportedCredentialType.CredentialLearnSupport = clSupport;

            supportedCredentialType.ReadBack = readback;

            return supportedCredentialType;
        }

        [NestedControl(1, "User Slots & Credential", 1)]
        public List<UserCredentialCCViewModel> UserCredentialCCViewModels
        {
            get
            {
                return GetUserCredentialCCViewModels();
            }
        }

        [JsonIgnore]
        public IEnumerable<INestedItem> ChildItems => UserCredentialCCViewModels;

        public bool AddOrModifyUser(UserSlot user, List<UserCredential> credentials = null, bool allowIncorrectParameter = false)
        {
            if (user.UserUniqeId == 0)
            {
                return false;
            }

            var userSlot = UserSlots.FirstOrDefault(x => x.UserUniqeId == user.UserUniqeId);

            if (userSlot == null)
            {
                if (UserSlots.Count >= NoOfSupportedUsers
                    && allowIncorrectParameter == false)
                {
                    return false;
                }
            }
            else
            {
                UserSlots.Remove(userSlot);
            }

            if (string.IsNullOrEmpty(user.Name)
                && allowIncorrectParameter == false)
            {
                user.Name = UserSlot.GetDefaultUserName(user.UserUniqeId);
            }

            UserSlots.Add(user);

            if (credentials != null && credentials.Count > 0)
            {
                foreach (var credential in credentials)
                {
                    var credentialTypeSettings = SupportedCredentialTypes.FirstOrDefault(x => x.Type == credential.Type);

                    if (credential.SlotId == 0
                        && allowIncorrectParameter == false)
                    {
                        var slotId = GetNextAvailableSlotId(credential.Type);

                        if (slotId == null)
                        {
                            // no free slot left
                            continue;
                        }

                        credential.SlotId = slotId ?? 0;
                    }

                    var existingCredential = UserCredentials.FirstOrDefault(x => x.Type == credential.Type && x.SlotId == credential.SlotId);

                    if (existingCredential != null)
                    {
                        UserCredentials.Remove(existingCredential);
                    }

                    credential.UserUniqeId = user.UserUniqeId;
                    credential.InherritCredentialReadback(credentialTypeSettings?.ReadBack ?? true);

                    UserCredentials.Add(credential);
                }
            }
            return true;
        }

        public bool AddOrModifyUser(UserSlotViewModel user)
        {
            if (user == null)
            {
                return false;
            }

            if (!AddOrModifyUser(user.UserSlot, user.UserCredentials, true))
            {
                return false;
            }

            return true;
        }

        public bool DeleteUser(ushort uniqueUserId)
        {
            var user = UserSlots.Find(x => x.UserUniqeId == uniqueUserId);

            return DeleteUser(user);
        }

        public bool DeleteUser(UserSlot user)
        {
            if (user == null) return false;

            var usersCredentials = UserCredentials.FindAll(x => x.UserUniqeId == user.UserUniqeId);
            usersCredentials.ForEach(x => UserCredentials.Remove(x));

            return UserSlots.Remove(user);
        }

        public bool DeleteUser(UserSlotViewModel userSlotViewModel)
        {
            if (userSlotViewModel == null) return false;

            return DeleteUser(userSlotViewModel.UserSlot);
        }

        public bool AddOrModifyCredential(UserCredential credential, bool allowIncorrectParameter = false)
        {
            var credentialTypeSettings = SupportedCredentialTypes.FirstOrDefault(x => x.Type == credential.Type);

            if (credentialTypeSettings == null
                && allowIncorrectParameter == false)
            {
                return false;
            }

            if (credential.UserUniqeId == 0)
            {
                return false;
            }

            if (allowIncorrectParameter == false
                && (credential.Data.Length < credentialTypeSettings.MinCredentialDataLength
                || credential.Data.Length > credentialTypeSettings.MaxCredentialDataLength))
            {
                return false;
            }

            var userSlot = UserSlots.FirstOrDefault(x => x.UserUniqeId == credential.UserUniqeId);

            if (userSlot == null)
            {
                return false;
            }

            var existingCredential = UserCredentials.FirstOrDefault(x => x.SlotId == credential.SlotId && x.Type == credential.Type);

            if (existingCredential == null)
            {
                if (UserCredentials.FindAll(x => x.Type == credential.Type).Count >= credentialTypeSettings.NoOfSupportedSlots
                    && allowIncorrectParameter == false)
                {
                    return false;
                }
            }
            else
            {
                UserCredentials.Remove(existingCredential);
            }

            credential.InherritCredentialReadback(credentialTypeSettings.ReadBack);

            UserCredentials.Add(credential);

            return true;
        }

        public void DeleteCredential(UserCredential credential)
        {
            UserCredentials.Remove(credential);
        }

        public ushort? GetFreeUserUniqueId()
        {
            if (UserSlots.Count < NoOfSupportedUsers)
            {
                for (int id = 1; id <= 0xFFFF; id++)
                {
                    if (UserSlots.Find(x => x.UserUniqeId == id) == null)
                    {
                        return (ushort)id;
                    }
                }
            }
            return null;
        }

        public ushort? GetNextAvailableSlotId(CredentialType credentialType, bool allowIncorrectParameter = false)
        {
            var credentialTypeConfig = SupportedCredentialTypes.FirstOrDefault(x => x.Type == credentialType);

            if (credentialTypeConfig != null)
            {
                var existingCredentials = UserCredentials.FindAll(x => x.Type == credentialType);

                if (existingCredentials.Count >= credentialTypeConfig.NoOfSupportedSlots)
                {
                    return null;
                }

                for (ushort id = 1; id < credentialTypeConfig.NoOfSupportedSlots; id++)
                {
                    if (existingCredentials.FirstOrDefault(x => x.SlotId == id) == null)
                    {
                        return id;
                    }
                }

            }
            else if (allowIncorrectParameter)
            {
                var existingCredentials = UserCredentials.FindAll(x => x.Type == credentialType);

                for (ushort id = 1; id <= credentialTypeConfig.NoOfSupportedSlots; id++)
                {
                    if (existingCredentials.FirstOrDefault(x => x.SlotId == id) == null)
                    {
                        return id;
                    }
                }
            }

            return null;
        }

        public void PrintUsersInfo()
        {
            Console.WriteLine();
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff"));

            if (UserSlots.Count == 0)
            {
                Console.WriteLine($"\tNo users available.");
            }

            UserSlots.ForEach(x => {
                Console.WriteLine($"{x.UserUniqeId.ToString().PadLeft(5, ' ')} {x.Name}:");
                var credentials = UserCredentials.FindAll(y => y.UserUniqeId == x.UserUniqeId);
                credentials.ForEach(y => {
                    Console.WriteLine($"\t* {y.SlotId.ToString().PadLeft(5, ' ')} {y.Type.ToString().PadRight(30, ' ')} {y.ToString()}");
                });

                if (credentials.Count == 0)
                {
                    Console.WriteLine($"\tNo credentials available.");
                }
            });
        }

        public List<UserCredentialCCViewModel> GetUserCredentialCCViewModels()
        {
            List<UserCredentialCCViewModel> userCredentialCCViewModels = new List<UserCredentialCCViewModel>();
            List<UserSlotViewModel> userSlotViewModels = GetUserSlotViewModels();

            userCredentialCCViewModels.Add(new UserCredentialCCViewModel(1, "All", SupportedCredentialTypes, GetUserSlotViewModels()));

            foreach (SupportedCredentialType supportedCredentialType in SupportedCredentialTypes)
            {
                int newId = userCredentialCCViewModels.Any() ? (userCredentialCCViewModels.Max(x => x.NestedId) + 1) : 1;

                List<UserSlotViewModel> realtedUserSlotViewModels = userSlotViewModels.FindAll(x => x.UserCredentials.Find(y => y.Type == supportedCredentialType.Type) != null);

                userCredentialCCViewModels.Add(new UserCredentialCCViewModel(newId, supportedCredentialType.Type.ToString(), new List<SupportedCredentialType>() { supportedCredentialType }, realtedUserSlotViewModels));
            }

            return userCredentialCCViewModels;
        }

        public List<UserSlotViewModel> GetUserSlotViewModels()
        {
            var userSlotViewModels = new List<UserSlotViewModel>();

            foreach (var userSlot in UserSlots)
            {
                List<UserCredential> userCredentials = UserCredentials.FindAll(x => x.UserUniqeId == userSlot.UserUniqeId);

                userSlotViewModels.Add(new UserSlotViewModel(userSlot, userCredentials));
            }

            return userSlotViewModels;
        }
    }

    public class UserCredentialCCViewModel : INestedItem
    {
        //// only used for EDE UI. see interface for useage notes.
        [JsonIgnore]
        public int NestedId { get; set; }
        public string Name { get; set; }
        public List<SupportedCredentialType> SupportedCredentialTypes { get; set; } = new List<SupportedCredentialType>();
        public List<UserSlotViewModel> UserSlotViewModels { get; set; } = new List<UserSlotViewModel>();

        public UserCredentialCCViewModel()
        {

        }

        public UserCredentialCCViewModel(
            int nestedId,
            string name,
            List<SupportedCredentialType> supportedCredentialTypeViewModels,
            List<UserSlotViewModel> userSlotViewModels)
        {
            NestedId = nestedId;
            Name = name;
            SupportedCredentialTypes = supportedCredentialTypeViewModels;
            UserSlotViewModels = userSlotViewModels;
        }

    }

    public class UserSlot
    {
        public ushort UserUniqeId { get; set; }

        public UserCredentialUserType Type { get; set; }

        public UserCredentialUserActiveState ActiveState { get; set; }

        public UserCredentialRule CredentialRule { get; set; }

        public ushort ExpiringTimeout { get; set; }

        public UserNameEncoding NameEncoding { get; set; }

        public string Name { get; set; } = "";

        public ModifierType ModifierType { get; set; }

        public ushort ModifierId { get; set; }

        public static string GetDefaultUserName(ushort uniqueUserId)
        {
            return $"User-{uniqueUserId}";
        }

        public override bool Equals(object other)
        {
            string jsonThis = JsonConvert.SerializeObject(this);
            string jsonOther = JsonConvert.SerializeObject(other);
            return jsonThis == jsonOther;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static implicit operator COMMAND_CLASS_USER_CREDENTIAL.USER_REPORT(UserSlot userSlot)
        {
            var rpt = new COMMAND_CLASS_USER_CREDENTIAL.USER_REPORT();
            byte[] userName = UserCredentialSupport.DecodeString(userSlot.Name, userSlot.NameEncoding);

            rpt.userReportType = (byte)UserReportType.ResponseToGet;
            rpt.userModifierType = (byte)userSlot.ModifierType;
            rpt.userModifierNodeId = Tools.GetBytes(userSlot.ModifierId);
            rpt.userUniqueIdentifier = Tools.GetBytes(userSlot.UserUniqeId);
            rpt.userType = (byte)userSlot.Type;
            rpt.properties1.userActiveState = (byte)userSlot.ActiveState;
            rpt.credentialRule = (byte)userSlot.CredentialRule;
            rpt.expiringTimeoutMinutes = Tools.GetBytes(userSlot.ExpiringTimeout);
            rpt.properties2.userNameEncoding = (byte)userSlot.NameEncoding;
            rpt.userNameLength = userName.Length < 256 ? (byte)userName.Length : (byte)255;
            rpt.userName = userName.Length < 256 ? userName : userName.Take(255).ToArray();

            return rpt;
        }

    }

    public class UserSlotViewModel : INestedItem
    {
        public ushort UserId
        {
            get
            {
                return UserSlot.UserUniqeId;
            }

            set
            {
                UserSlot.UserUniqeId = value;
            }
        }

        public int NestedId { get; set; }

        [CustomDataParameter(1, "User Type", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config | EditType.Edit, ItemsSource = typeof(UserCredentialUserType))]
        public UserCredentialUserType Type
        {
            get
            {
                return UserSlot.Type;
            }
            set
            {
                UserSlot.Type = value;
            }
        }

        [CustomDataParameter(1, "Active State", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config | EditType.Edit, ItemsSource = typeof(UserCredentialUserActiveState))]
        public UserCredentialUserActiveState ActiveState
        {
            get
            {
                return UserSlot.ActiveState;
            }
            set
            {
                UserSlot.ActiveState = value;
            }
        }

        [CustomDataParameter(1, "Credential Rule", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config | EditType.Edit, ItemsSource = typeof(UserCredentialRule))]
        public UserCredentialRule CredentialRule
        {
            get
            {
                return UserSlot.CredentialRule;
            }
            set
            {
                UserSlot.CredentialRule = value;
            }
        }

        [CustomDataParameter(1, "Expiring Timeout", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.UShort, EditType.Config | EditType.Edit)]
        public ushort ExpiringTimeout
        {
            get
            {
                return UserSlot.ExpiringTimeout;
            }
            set
            {
                UserSlot.ExpiringTimeout = value;
            }
        }

        //[CustomDataParameter(1, "Name Encoding", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config | EditType.Edit, ItemsSource = typeof(UserNameEncoding))]
        public UserNameEncoding NameEncoding
        {
            get
            {
                return UserSlot.NameEncoding;
            }
            set
            {
                UserSlot.NameEncoding = value;
            }
        }

        [CustomDataParameter(1, "Name", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Text, EditType.Config | EditType.Edit)]
        public string Name
        {
            get
            {
                if (UserSlot.Name == null)
                {
                    UserSlot.Name = "";
                }

                return UserSlot.Name;
            }
            set
            {
                UserSlot.Name = value;
            }
        }

        [CustomDataParameter(1, "Modifier Type", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Text, EditType.Readonly)]
        public string ModifierTypeString
        {
            get
            {
                return UserSlot.ModifierType.ToString();
            }
            set
            {
                if (Enum.TryParse(value, true, out ModifierType result))
                {
                    UserSlot.ModifierType = result;
                }
                else
                {
                    // Handle the case where the string does not match any enum value
                    UserSlot.ModifierType = ModifierType.Unknown;
                }
            }
        }

        [CustomDataParameter(1, "Modifier Id", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.UShort, EditType.Readonly)]
        public ushort ModifierId
        {
            get
            {
                return UserSlot.ModifierId;
            }
            set
            {
                UserSlot.ModifierId = value;
            }
        }

        [JsonIgnore]
        public UserSlot UserSlot { get; set; } = new UserSlot();

        [JsonIgnore]
        public List<UserCredential> UserCredentials { get; set; } = new List<UserCredential>();

        public UserSlotViewModel()
        {

        }

        public UserSlotViewModel(UserSlot userSlot, List<UserCredential> userCredentials)
        {
            NestedId = userSlot.UserUniqeId;
            UserSlot = userSlot;
            UserCredentials = userCredentials;
        }
    }

    public enum UserCredentialUserType
    {
        [Description("0x00 General User")]
        GeneralUser = 0x00,
        [Description("0x03 Programming User")]
        ProgranmmingUser = 0x03,
        [Description("0x04 Non-Access User")]
        NonAccessUser = 0x04,
        [Description("0x05 Duress User")]
        DuressUser = 0x05,
        [Description("0x06 Disposable User")]
        DisposableUser = 0x06,
        [Description("0x07 Expiring User")]
        ExpiringUser = 0x07,
        [Description("0x09 Remote Only User")]
        RemoteOnlyUser = 0x09,
    }

    public enum UserCredentialUserActiveState
    {
        [Description("0x00 Occupied Disabled")]
        OccupiedDisabled = 0x00,
        [Description("0x01 Occupied Enabled")]
        OccupiedEnabled = 0x01,
    }

    public enum UserCredentialRule
    {
        [Description("0x01 Single")]
        Single = 0x01,
        [Description("0x02 Dual")]
        Dual = 0x02,
        [Description("0x03 Triple")]
        Triple = 0x03,

    }

    public enum UserNameEncoding
    {
        [Description("0x00 Standard ASCII")]
        ASCII = 0x00,
        [Description("0x01 Extended ASCII")]
        ExtendedASCII = 0x01,
        [Description("0x02 UTF-16 (big endian)")]
        UTF16BE = 0x02,
    }

    public enum UserSetOperationType
    {
        Add = 0x00,
        Modify = 0x01,
        Delete = 0x02
    }

    public enum ModifierType
    {
        DNE = 0x00,
        Unknown = 0x01,
        ZWave = 0x02,
        Locally = 0x03,
        IoT = 0x04
    }

    public enum UserReportType
    {
        UserAdded = 0x00,
        UserModified = 0x01,
        UsersDeleted = 0x02,
        UserUnchanged = 0x03,
        ResponseToGet = 0x04,
        UserAddRejectedOccupied = 0x05,
        UserModifyRejectedEmpty = 0x06,
        ZeroExpiringInvalid = 0x07
    }

    public enum AdminPinCodeOperationResult
    {
        Modified = 0x01,
        Unmodified = 0x03,
        ResponseToGet = 0x04,
        FailDuplicateCredential = 0x07,
        FailManufacturerSecurityRules = 0x08,
        ErrorAdminCodeNotSupported = 0x0D,
        ErrorAdminCodeDeactivationNotSupported = 0x0E,
        UnspecifiedNodeError = 0x0F
    }

    public class UserCredential : INestedItem
    {
        // only used for EDE UI. see interface for usage notes.
        public int NestedId { get; set; }
        public ushort UserUniqeId { get; set; }

        public CredentialType Type { get; set; }

        public ushort SlotId { get; set; }

        // use standard string / hex field? 
        [CustomDataParameter(1, "Data", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.HexByteArray, EditType.Config | EditType.Edit)]
        public byte[] Data { get; set; } = new byte[] { };

        [CustomDataParameter(1, "Modifier Type", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config | EditType.Edit, ItemsSource = typeof(ModifierType))]
        public ModifierType ModifierType { get; set; } = ModifierType.Locally;

        [CustomDataParameter(1, "Modifier Id", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.UShort, EditType.Config)]
        public ushort ModifierNodeId { get; set; }

        private bool readBack = false;
        public bool ReadBack { get { return readBack; } }

        public UserCredential()
        {

        }

        public UserCredential(ushort userUniqeId, CredentialType type, ushort slotId)
        {
            UserUniqeId = userUniqeId;
            Type = type;
            SlotId = slotId;
        }

        public static UserCredential CreateCredential(ushort uuid, CredentialType type, ushort slotId, byte[] data, ModifierType modifierType, ushort nodeId = 0)
        {
            return new UserCredential(uuid, type, slotId)
            {
                Data = data,
                ModifierType = modifierType,
                ModifierNodeId = nodeId
            };
        }

        public static UserCredential CreatePinCodeCredential(ushort uuid, ushort slotId, string pin)
        {
            foreach (char c in pin)
            {
                if (c < '0' || c > '9')
                {
                    throw new ArgumentException("PIN Code MUST contain numbers only");
                }
            }

            return new UserCredential(uuid, CredentialType.PinCode, slotId)
            {
                Data = Encoding.ASCII.GetBytes(pin)
            };
        }

        public static UserCredential CreatePasswordCredential(ushort uuid, ushort slotId, string password)
        {
            return new UserCredential(uuid, CredentialType.Password, slotId)
            {
                Data = Encoding.BigEndianUnicode.GetBytes(password)
            };
        }


        public void IgnoreCredentialReadback()
        {
            readBack = true;
        }

        public byte[] GetDataOrHash(bool getDataAnyway = false)
        {
            byte[] data = new byte[0];

            if (ReadBack || getDataAnyway)
            {
                data = Data;
            }
            else
            {
                data = GetHash(Data);
            }
            return data;
        }

        public static byte[] GetHash(byte[] data)
        {
            const int p = 16777619;
            uint hash = 2166136261;

            for (uint i = 0; i < data.Length; i++)
            {
                hash = (hash ^ data[i]) * p;
            }

            return BitConverter.GetBytes(hash);
        }

        public COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_REPORT CreateUserCredentialReport(CredentialReportType reportType, bool getDataAnyway = false)
        {
            var data = GetDataOrHash(getDataAnyway);

            var rpt = new COMMAND_CLASS_USER_CREDENTIAL.CREDENTIAL_REPORT()
            {
                userUniqueIdentifier = Tools.GetBytes(UserUniqeId),
                credentialReportType = (byte) reportType,
                credentialType = (byte) Type,
                credentialSlot = Tools.GetBytes(SlotId),
                credentialLength = (byte)data.Length,
                credentialData = data,
                credentialModifierNodeId = Tools.GetBytes(ModifierNodeId),
                credentialModifierType = (byte)ModifierType
            };
            rpt.properties1.crb = (byte)(ReadBack ? 0x01 : 0x00);
            return rpt;
        }

        public override bool Equals(object other)
        {
            string jsonThis = JsonConvert.SerializeObject(this);
            string jsonOther = JsonConvert.SerializeObject(other);
            return jsonThis == jsonOther;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public void InherritCredentialReadback(bool credentialReadBack)
        {
            readBack = credentialReadBack;
        }

        public override string ToString()
        {
            switch(Type)
            {
                case CredentialType.PinCode:
                    return Encoding.ASCII.GetString(Data);

                case CredentialType.Password:
                    return Encoding.BigEndianUnicode.GetString(Data);

                default:
                    return CommandClassSupportUtils.ByteArrayToString(Data.ToList());
            }
        }

    }

    public class SupportedCredentialType : INestedItem
    {
        // only used for EDE UI. see interface for useage notes.
        public int NestedId { get; set; }

        [CustomDataParameter(1, "Credential Type", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config | EditType.Edit, ItemsSource = typeof(CredentialType))]
        public CredentialType Type { get; set; }

        [CustomDataParameter(1, "No Of Slots", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.UShort, EditType.Config | EditType.Edit)]
        public ushort NoOfSupportedSlots { get; set; } = 0;

        [CustomDataParameter(1, "Learn Support", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config | EditType.Edit)]
        public bool CredentialLearnSupport { get; set; }

        [CustomDataParameter(1, "Min Data Length", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Hex8, EditType.Config | EditType.Edit)]
        public byte MinCredentialDataLength { get; set; } = 0;

        [CustomDataParameter(1, "Max Data Length", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Hex8, EditType.Config | EditType.Edit)]
        public byte MaxCredentialDataLength { get; set; } = 0;

        [CustomDataParameter(1, "Learn Timeout", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Hex8, EditType.Config | EditType.Edit)]
        public byte CredentialLearnRecommendedTimeout { get; set; } = 0;

        [CustomDataParameter(1, "No Of Steps", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Hex8, EditType.Config | EditType.Edit)]
        public byte CredentialLearnNoOfSteps { get; set; } = 0;

        [CustomDataParameter(1, "Read Back", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config | EditType.Edit)]
        public bool ReadBack { get; set; } = true;

        [JsonIgnore]
        public string Name
        {
            get
            {
                return NestedId + " - " + Type.ToString();
            }
        }

        // the default hash method in C# returns an int32, which is 4 bytes long
        public byte MaxHCredentialHashLength { get { return (byte)(ReadBack ? 0 : 4); } }

        public SupportedCredentialType()
        {

        }

        public SupportedCredentialType(int nestedId, CredentialType type)
        {
            NestedId = nestedId;
            Type = type;
        }
    }

    public enum CredentialType
    {
        [Description("0x01 PIN Code")]
        PinCode = 0x01,
        [Description("0x02 Password")]
        Password = 0x02,
        [Description("0x03 RFID Code")]
        RfidCode = 0x03,
        [Description("0x04 BLE")]
        BLE = 0x04,
        [Description("0x05 NFC")]
        NFC = 0x05,
        [Description("0x06 UWB")]
        UWB = 0x06,
        [Description("0x07 Eye Biometric")]
        EyeBiometric = 0x07,
        [Description("0x08 Face Biometric")]
        FaceBiometric = 0x08,
        [Description("0x09 Finger Biometric")]
        FingerBiometric = 0x09,
        [Description("0x0A Hand Biometric")]
        HandBiometric = 0x0A,
        [Description("0x0B Unspecified Biometric")]
        UnspecBiometric = 0x0B,
    }

    public enum CredentialSetOperationType
    {
        Add = 0x00,
        Modify = 0x01,
        Delete = 0x02
    }

    public enum CredentialLearnOperationType
    {
        CredentialLearnAdd = 0x00,
        CredentialLearnModify = 0x01
    }

    public enum CredentialLearnStatus
    {
        Started = 0x00,
        Success = 0x01,
        AlreadyInProgress = 0x02,
        EndedNotDueToTimeout = 0x03,
        Timeout = 0x04,
        CredentialLearnStepRetry = 0x05,
        InvalidCredentialLearnAddOperationType = 0xFE,
        InvalidCredentialLearnModifyOperationType = 0xFF
    }

    public enum CredentialReportType
    {
        CredentialAdded = 0x00,
        CredentialModified = 0x01,
        CredentialDeleted = 0x02,
        CredentialUnchanged = 0x03,
        ResponseToGet = 0x04,
        CredentialAddRejectedLocationOccupied = 0x05,
        CredentialModifyRejectedLocationEmpty = 0x06,
        DuplicateCredential = 0x07,
        ManufacturerSecurityRules = 0x08,
        WrongUserUniqueIdentifier = 0x09,
        DuplicateAdminCode = 0x0A,
    }

    public enum UserCredentialAssociationStatus
    {
        Success = 0x00,
        SourceCredentialTypeInvalid = 0x01,
        SourceCredentialSlotInvalid = 0x02,
        SourceCredentialSlotEmpty = 0x03,
        DestinationUserUniqueIdentifierInvalid = 0x04,
        DestinationUserUniqueIdentifierNonexistent = 0x05,
        DestinationCredentialSlotInvalid = 0x06,
        DestinationCredentialSlotOccupied = 0x07
    }

    public static class UserCredentialExtensions
    {
        /// <summary>
        /// Determines whether the specified user name encoding is actually supported.
        /// It considers the legacy fallback where if the list is empty,
        /// ASCII, OEM Extended ASCII and UTF-16 (Big Endian) are all supported.
        /// </summary>
        /// <param name="advertisedUserNameEncodings">User name encodings as advertised within the User Capabilities Report</param>
        /// <param name="encoding">Encoding in question</param>
        /// <returns>True if actually supported.</returns>
        public static bool IsUserNameEncodingSupported(this List<UserNameEncoding> advertisedUserNameEncodings, UserNameEncoding encoding)
        {
            return advertisedUserNameEncodings.GetSupportedUserNameEncodings().Contains(encoding);
        }

        /// <summary>
        /// Gets all actually supported User Name Encodings.
        /// It considers the legacy fallback where if the list is empty,
        /// ASCII, OEM Extended ASCII and UTF-16 (Big Endian) are all supported.
        /// </summary>
        /// <param name="advertisedUserNameEncodings">User name encodings as advertised within the User Capabilities Report</param>
        /// <returns>All actually supported User Name Encodings.</returns>
        public static List<UserNameEncoding> GetSupportedUserNameEncodings(this List<UserNameEncoding> advertisedUserNameEncodings)
        {
            if (advertisedUserNameEncodings is null)
            {
                return new List<UserNameEncoding>();
            }

            if (advertisedUserNameEncodings.Count == 0)
            {
                // Fallback for first implementations
                return new List<UserNameEncoding>
                {
                    UserNameEncoding.ASCII,
                    UserNameEncoding.ExtendedASCII,
                    UserNameEncoding.UTF16BE
                };
            }

            return advertisedUserNameEncodings;
        }

        public static UserCodeUserIdStatusV2 MapToUserCodeStatus(this UserSlot user)
        {
            if (user == null)
            {
                return UserCodeUserIdStatusV2.StatusNotAvailable;
            }

            if (user.ActiveState == UserCredentialUserActiveState.OccupiedDisabled)
            {
                return UserCodeUserIdStatusV2.Disabled;
            }

            switch (user.Type)
            {
                case UserCredentialUserType.GeneralUser:
                case UserCredentialUserType.ProgranmmingUser:
                case UserCredentialUserType.DuressUser:
                case UserCredentialUserType.DisposableUser:
                case UserCredentialUserType.ExpiringUser:
                case UserCredentialUserType.RemoteOnlyUser:
                    return UserCodeUserIdStatusV2.Enabled;

                case UserCredentialUserType.NonAccessUser:
                    return UserCodeUserIdStatusV2.Messaging;
            }

            return UserCodeUserIdStatusV2.StatusNotAvailable;
        }

        public static UserCodeUserIdStatusV1 MapToUserCodeStatusV1(this UserSlot user)
        {
            if (user == null)
            {
                return UserCodeUserIdStatusV1.StatusNotAvailable;
            }

            if (user.ActiveState == UserCredentialUserActiveState.OccupiedDisabled)
            {
                return UserCodeUserIdStatusV1.Reserved;
            }

            switch (user.Type)
            {
                case UserCredentialUserType.GeneralUser:
                case UserCredentialUserType.ProgranmmingUser:
                case UserCredentialUserType.DuressUser:
                case UserCredentialUserType.DisposableUser:
                case UserCredentialUserType.ExpiringUser:
                case UserCredentialUserType.RemoteOnlyUser:
                    return UserCodeUserIdStatusV1.Occupied;

                case UserCredentialUserType.NonAccessUser:
                    return UserCodeUserIdStatusV1.Reserved;
            }

            return UserCodeUserIdStatusV1.StatusNotAvailable;
        }
    }
}
