// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;
using Zats.Services.CommandClassesImpl.Definitions.Interfaces;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using ZWave.Devices;
using static Zats.Services.AssociationGroupEx;

namespace Zats.Services
{
    public delegate void CommandCallback(byte[] command, NodeTag dstNode, NodeTag srcNode);

    public abstract class CommandClassSupport : DelayedResponseOperation, ICommandClass
    {
        #region Enums
        public ZWave.Enums.TransmitOptions TxOptions { get; set; }
        public ZWave.Enums.TransmitOptions2 TxOptions2 { get; set; }
        public ZWave.Enums.TransmitSecurityOptions TxSecOptions { get; set; }
        #endregion

        protected int MinCCVersion { get; set; }
        protected int MaxCCVersion { get; set; }
        public abstract CommandClassSupportData CommandClassSupportData { get; set; }

        public string CommandClassName;

        public NetworkViewPoint Network
        {
            get { return _network; }
            set { _network = value; }
        }
        // Command Class Id Byte
        public byte CommandClassId { get; set; }
        // Command Class Version
        public byte Version { get { return version; } set { version = value; ValidateCCVersion(); } }
        private byte version;
        // End Point Id
        private byte endPoint;
        public byte EndPoint { get { return endPoint; } }
        // Command Class is only supported on highest Security Class
        public bool Secure { get; set; }
        // Command Class is only support if the Device is included securely
        public bool SecureExclusive { get; set; }
        // Command Class specific data object
        public Dictionary<byte, Dictionary<byte, CommandCallback>> CommandCallbacks { get; set; }

        public CommandClassSupportDelegates SupportDelegates { get; set; }

        public AssociationSupportData RootAssociationData { get; set; } = null;

        // For Logging: End Point Name
        public string EndPointName { get { return EndPoint == 0 ? "Root" : string.Format("EP {0}", EndPoint); } }
        // For Logging: Shorter Command Class Name (truncate 14 chars " Command Class")
        public string CommandClassShortName { get { return CommandClassName.Substring(0, CommandClassName.Length - 14); } }

        public NodeTag InstanceNodeTag { 
            get
            {
                return new NodeTag(_network.NodeTag.Id, endPoint);
            } 
        }

        public TimeSupportData TimeSupportData { get; set; } = null;

        public DateTime GetDeviceDateTime()
        {
            if (TimeSupportData != null)
            {
                return TimeSupportData.DeviceDateTime;
            }
            return DateTime.Now;
        }


        #region Reporting
        /// <summary>
        /// Z-Wave device which shall send the report
        /// </summary>
        public Device ReportDevice { get; set; }

        /// <summary>
        /// define on which changes an unsolicited report shall be sent automatically
        /// </summary>
        public ReportTypes ReportTypes { get; set; } = ReportTypes.None;

        /// <summary>
        /// define a Destination Node for automated unsolicited reports
        /// </summary>
        public NodeIdEx ReportDestination { get; set; }

        /// <summary>
        /// define whether unsolicited reports should be sent to the configured Node, the root Lifeline or not at all
        /// </summary>
        public ReportDestinationTypes ReportDestinationType { get; set; } = ReportDestinationTypes.Disabled;

        protected int ReportWakeupTimeout = 10000;
        #endregion

        private bool ValidateCCVersion()
        {
            //            if (MaxCCVersion == 0) return false;  // skip check if support implementation of CC is not completed

            if (version < MinCCVersion || version > MaxCCVersion)
            {
                string msg = string.Format("Version '{0}' of Command Class 0x{1:X2} is not supported.", version, CommandClassId);
                throw new NotImplementedException(msg);
            }
            return true;
        }

        public CommandClassSupport(byte commandClassId)
            : base(null, NodeTag.Empty, NodeTag.Empty, new ByteIndex(commandClassId))
        {
            TxOptions2 = ZWave.Enums.TransmitOptions2.TRANSMIT_OPTION_2_TRANSPORT_SERVICE;
            TxSecOptions = ZWave.Enums.TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY;

            CommandClassId = commandClassId;

            CommandClassNameAttribute commandClassNameAttribute =
                (CommandClassNameAttribute)Attribute.GetCustomAttribute(this.GetType(), typeof(CommandClassNameAttribute));

            if (commandClassNameAttribute != null)
            {
                CommandClassName = commandClassNameAttribute.CommandClassName;
            }
            else
            {
                throw new MissingFieldException("CommandClassNameAttribute");
            }

            MinVersionAttribute minVersionAttribute =
                (MinVersionAttribute)Attribute.GetCustomAttribute(this.GetType(), typeof(MinVersionAttribute));

            if (minVersionAttribute != null)
            {
                MinCCVersion = minVersionAttribute.MinVersion;
            }
            else
            {
                throw new MissingFieldException("MinVersionAttribute");
            }

            MaxVersionAttribute maxVersionAttribute =
                (MaxVersionAttribute)Attribute.GetCustomAttribute(this.GetType(), typeof(MaxVersionAttribute));

            if (maxVersionAttribute != null)
            {
                MaxCCVersion = maxVersionAttribute.MaxVersion;
            }
            else
            {
                throw new MissingFieldException("MaxVersionAttribute");
            }
        }

        public CommandClassSupport(NodeTag dstNode, NodeTag srcNode, byte commandClassId)
            : base(null, dstNode, srcNode, new ByteIndex(commandClassId))
        {
            TxOptions2 = ZWave.Enums.TransmitOptions2.TRANSMIT_OPTION_2_TRANSPORT_SERVICE;
            TxSecOptions = ZWave.Enums.TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY;

            CommandClassId = commandClassId;
        }

        public void SetNetwork(NetworkViewPoint network)
        {
            _network = network;
        }

        protected void CommandCallback(byte[] command, byte dstEp)
        {
            if (CommandCallbacks != null && CommandCallbacks.ContainsKey(CommandClassId))
            {
                if (CommandCallbacks[CommandClassId].ContainsKey(command[1]))
                {
                    NodeTag dstNode = new NodeTag(Network.NodeTag.Id, dstEp);
                    CommandCallbacks[CommandClassId][command[1]](command, dstNode, ReceivedAchData.SrcNode);
                }
            }
        }

        protected bool IsCommandCallbackRegistered(byte commandId) {
            if (CommandCallbacks != null && CommandCallbacks.ContainsKey(CommandClassId))
            {
                if (CommandCallbacks[CommandClassId].ContainsKey(commandId)) {
                    return true;
                }
            }
            return false;
        }

        protected void SendUnsolicitedReport(byte[] command, NodeTag sourceNode)
        {
            if (ReportDestinationType == ReportDestinationTypes.Lifeline)
            {
                foreach (NodeIdEx destinationNode in GetLifelineNodes())
                {
                    SendUnsolicitedReport(command, destinationNode, sourceNode);
                }
            }
            else if (ReportDestinationType == ReportDestinationTypes.Fixed)
            {
                SendUnsolicitedReport(command, ReportDestination, sourceNode);
            }
        }
        /// <summary>
        /// Send Unsolicited Report to any destination (ID 1...255) regardless to the AGI.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="destinationNode">NOTE that only classic Nodes are supported!</param>
        /// <param name="sourceNode"></param>
        protected void SendUnsolicitedReportToFixedDestination(byte[] command, NodeTag destinationNode, NodeTag sourceNode)
        {
            if (destinationNode == null || destinationNode.Id > 255)
            {
                return;
            }

            SendUnsolicitedReport(command, new NodeIdEx((byte)destinationNode.Id, destinationNode.EndPointId, destinationNode.EndPointId != 0), sourceNode);
        }

        protected void SendUnsolicitedReport(byte[] command, NodeIdEx destinationNode, NodeTag sourceNode)
        {
            // Do not send unsolicited reports if device is not included
            if (_network.NodeTag == NodeTag.Empty)
            {
                return;
            }

            if (destinationNode != null && sourceNode != default(NodeTag) &&
                sourceNode != NodeTag.Empty && destinationNode.NodeId == sourceNode.Id)
            {
                // Sending unsolicited reports is skipped if sourceNode is not NodeTag.Empty (was triggered by Z-Wave Set) and has the same Node Id as the destinationNode.
                // Unsolicited Reports are always sent if sourceNode is NodeTag.Empty which means the report was triggered by an EDE UI update.
                return;
            }

            if (destinationNode != null && !destinationNode.IsMultiChannel && EndPoint != 0)
            {
                // Sending unsolicited reports is skipped from End Points if no End Point association has been set
                SupportDelegates?.LogMessage?.Invoke(LogLocation.Frontend, LogLevel.Info, "Unsolicited Report not sent from End Point " + EndPoint +
                    " to Node Id " + destinationNode.NodeId + "! No End Point report destination configured!");
                return;
            }

            if (ReportDevice != null &&
                destinationNode != null &&
                command != null && command.Length > 0 &&
                !ReceivedAchData.Options.HasFlag(ZWave.Enums.ReceiveStatuses.TypeBroad) &&
                !ReceivedAchData.Options.HasFlag(ZWave.Enums.ReceiveStatuses.TypeMulti))
            {
                NodeTag destinationNodeTag;
                if (destinationNode.IsMultiChannel)
                {
                    destinationNodeTag = new NodeTag(destinationNode.NodeId, destinationNode.EndPoint);
                }
                else
                {
                    destinationNodeTag = new NodeTag(destinationNode.NodeId);
                }

                if (EndPoint == 0 && (!destinationNode.IsMultiChannel || destinationNode.EndPoint == 0))
                {
                    // Send asynchronously without waiting for completion.  This should send the reports in the order that they are called in.
                    ReportDevice.SendDataEx(destinationNodeTag, command, TxOptions, ReportDevice.Network.GetHighestSecurityScheme(), null);
                }
                else
                {
                    var multichannelEncap = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP()
                    {
                        commandClass = command[0],
                        command = command[1],
                        parameter = command.Skip(2).ToArray(),
                    };
                    multichannelEncap.properties1.sourceEndPoint = EndPoint;
                    multichannelEncap.properties2.destinationEndPoint = destinationNodeTag.EndPointId;

                    // Send asynchronously without waiting for completion. This should send the reports in the order that they are called in.
                    ReportDevice.SendDataEx(destinationNodeTag, multichannelEncap, TxOptions, ReportDevice.Network.GetHighestSecurityScheme(), null);
                }
            }
        }

        protected void DeviceRestart(NodeTag sourceNode)
        {
            bool restartNeeded = false;

            if (_network.NodeTag == NodeTag.Empty)
            {
                // Do not restart if device is not included
                return;
            }

            if (sourceNode == NodeTag.Empty)
            {
                // Always restart on sourceNode == NodeTag.Empty
                restartNeeded = true;
            }
            else
            {
                if (ReportDestinationType == ReportDestinationTypes.Lifeline)
                {
                    foreach (NodeIdEx destinationNode in GetLifelineNodes())
                    {
                        if (destinationNode.NodeId == sourceNode.Id)
                        {
                            // Report skipped because destinationNode == sourceNode, restart not needed
                            continue;
                        }

                        if (!destinationNode.IsMultiChannel && EndPoint != 0)
                        {
                            // Report skipped because it is coming from End Point but assocation is not multi channel, restart not needed
                            continue;
                        }
                        restartNeeded = true;
                    }
                }
                else if (ReportDestinationType == ReportDestinationTypes.Fixed)
                {
                    if (ReportDestination.NodeId == sourceNode.Id)
                    {
                        /// Report skipped because destinationNode == sourceNode, restart not needed
                        return;
                    }

                    if (!ReportDestination.IsMultiChannel && EndPoint != 0)
                    {
                        // Report skipped because it is coming from End Point but assocation is not multi channel, restart not needed
                        return;
                    }
                    restartNeeded = true;
                }
            }

            if (restartNeeded)
            {
                SupportDelegates?.DeviceRestart(this);
            }
        }

        public List<NodeIdEx> GetLifelineNodes()
        {
            List<NodeIdEx> lifelineNodes = new List<NodeIdEx>();

            if (RootAssociationData != null)
            {
                AssociationGroupEx lifelineGroup = RootAssociationData.AssociationGroups.FirstOrDefault(x => x.GroupId == 1);

                if (lifelineGroup != null)
                {
                    lifelineNodes = lifelineGroup.AssociatedNodeIds;
                }
            }
            return lifelineNodes;
        }

        /// <summary>
        /// Checks if the given destination matches the known destination
        /// considering NodeIDs and EndPointIDs (including potential received MC bit addressing).
        /// </summary>
        /// <param name="receivedDst">May be single EP or bit address.</param>
        /// <param name="knownDst">Must NOT be bit address!</param>
        public bool MatchesDestination(NodeTag receivedDst, NodeTag knownDst)
        {
            if (knownDst.IsBitAddress)
            {
                return false;
            }

            if (receivedDst.Id != knownDst.Id)
            {
                return false;
            }

            return MatchesDestinationEndPoint(receivedDst, knownDst.EndPointId);
        }

        /// <summary>
        /// Checks if the given destination matches the known destination EP
        /// NOT considering NodeID but its EndPointIDs (including potential received MC bit addressing).
        /// </summary>
        protected bool MatchesDestinationEndPoint()
        {
            return MatchesDestinationEndPoint(ReceivedAchData.DstNode, DstNode.EndPointId);
        }

        /// <summary>
        /// Checks if the given destination EP matches the known destination EP
        /// considering their EndPointIDs (including potential received MC bit addressing).
        /// </summary>
        /// <param name="receivedDst">May be single EP or bit address.</param>
        /// <param name="knownDstEP">Must NOT be bit address!</param>
        public bool MatchesDestinationEndPoint(NodeTag receivedDst, byte knownDstEP)
        {
            if ((knownDstEP & 0x80) == 1) // Bit 7 is for MC bit addressing!
            {
                return false;
            }

            if (receivedDst.IsBitAddress)
            {
                return IsEPAmongMCBitAddressingDestinations(receivedDst.EndPointId, knownDstEP);
            }
            else
            {
                return receivedDst.EndPointId == knownDstEP;
            }
        }

        private bool IsEPAmongMCBitAddressingDestinations(byte destinationEPBitAddress, byte epIdInQuestion)
        {
            if (epIdInQuestion < 0 || epIdInQuestion > 7)
            {
                return false;
            }

            if (epIdInQuestion == 0)
            {
                return destinationEPBitAddress == 0x00;
            }

            return (destinationEPBitAddress & (1 << (epIdInQuestion - 1))) != 0;
        }

        protected override bool IsHandlingAllowed(NetworkViewPoint network, byte[] command, ZWave.Enums.SecuritySchemes scheme, bool secureCommandClass)
        {
            if (scheme == ZWave.Enums.SecuritySchemes.NONE && SecureExclusive == true)
            {
                return false;
            }
            else
            {
                return base.IsHandlingAllowed(network, command, scheme, secureCommandClass);
            }
        }

        protected bool IsAnsweringAllowed()
        {
            return !IsBroadcast() && !IsMulticast() && !IsMultiChannelBitAddress();
        }

        protected bool IsSupervised()
        {
            return ReceivedAchData.SubstituteIncomingFlags.HasFlag(ZWave.Enums.SubstituteIncomingFlags.Supervision);
        }

        private bool IsBroadcast()
        {
            return ReceivedAchData.Options.HasFlag(ZWave.Enums.ReceiveStatuses.TypeBroad);
        }

        private bool IsMulticast()
        {
            return ReceivedAchData.Options.HasFlag(ZWave.Enums.ReceiveStatuses.TypeMulti);
        }

        private bool IsMultiChannelBitAddress()
        {
            return ReceivedAchData.DstNode.IsBitAddress;
        }

        public void SetupReporting(Device device, NodeIdEx dstNode, ReportTypes reportTypes)
        {
            ReportDevice = device;
            ReportDestination = dstNode;
            ReportTypes = reportTypes;
        }

        public void UpdateSupportData(CommandClassSupportData data, Device device)
        {
            ReportDevice = device;
            CommandClassSupportData.LogLevel = data.LogLevel;

            UpdateSupportData(data);
        }

        public void SetEndPoint(byte endPoint)
        {
            this.endPoint = endPoint;
        }

        public abstract void UpdateSupportData(CommandClassSupportData data);

        public abstract void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport);

        // Override this if any thread or worker should not start before device initialization/restart is completed
        public virtual void StartEmulation() { }

        // Override this if any Command Class specific data needs to be reset when device is excluded or reset (e.g. Association group contents)
        public virtual void ResetEmulation() { }
    }

    public interface IBasicMapping
    {
        bool BasicSet(byte value, NodeTag srcNode);
        COMMAND_CLASS_BASIC_V2.BASIC_REPORT BasicV2Get();
    }

    [Flags]
    public enum ReportTypes
    {
        None = 0,
        OnStateChange = 1,
        OnCapabilityChange = 2,
        Always = 3
    }

    public enum ReportDestinationTypes
    {
        Lifeline = 0,
        Fixed,
        Disabled
    }
}
