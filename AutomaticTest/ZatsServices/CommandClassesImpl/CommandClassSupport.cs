// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using ZWave.Devices;

namespace Zats.Services
{
    public delegate void CommandCallback(byte[] command, NodeTag dstNode, NodeTag srcNode);

    public abstract class CommandClassSupport : DelayedResponseOperation
    {
        #region Enums
        public ZWave.Enums.TransmitOptions TxOptions { get; set; }
        public ZWave.Enums.TransmitOptions2 TxOptions2 { get; set; }
        public ZWave.Enums.TransmitSecurityOptions TxSecOptions { get; set; }
        #endregion

        protected int MinCCVersion { get; set; }
        protected int MaxCCVersion { get; set; }

        public NetworkViewPoint Network
        {
            get { return _network; }
            set { _network = value; }
        }
        // Command Class Id Byte
        public new byte Id { get; set; }
        // Command Class Version
        public byte Version { get { return version; } set { version = value; ValidateCCVersion(); } }
        private byte version;
        // End Point Id
        public byte EndPoint { get; set; }
        // Command Class is only supported on highest Security Class
        public bool Secure { get; set; }
        // Command Class is only support if the Device is included securely
        public bool SecureExclusive { get; set; }
        // Command Class specific data object
        public Dictionary<byte, Dictionary<byte, CommandCallback>> CommandCallbacks { get; set; }

        private bool ValidateCCVersion()
        {
            //            if (MaxCCVersion == 0) return false;  // skip check if support implementation of CC is not completed

            if (version < MinCCVersion || version > MaxCCVersion)
            {
                string msg = string.Format("Version '{0}' of Command Class 0x{1:X2} is not supported.", version, Id);
                throw new NotImplementedException(msg);
            }
            return true;
        }

        public CommandClassSupport(byte commandClassId)
            : base(null, NodeTag.Empty, NodeTag.Empty, new ByteIndex(commandClassId))
        {
            TxOptions2 = ZWave.Enums.TransmitOptions2.TRANSMIT_OPTION_2_TRANSPORT_SERVICE;
            TxSecOptions = ZWave.Enums.TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY;

            Id = commandClassId;
        }

        public CommandClassSupport(NodeTag dstNode, NodeTag srcNode, byte commandClassId)
            : base(null, dstNode, srcNode, new ByteIndex(commandClassId))
        {
            TxOptions2 = ZWave.Enums.TransmitOptions2.TRANSMIT_OPTION_2_TRANSPORT_SERVICE;
            TxSecOptions = ZWave.Enums.TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY;

            Id = commandClassId;
        }

        public void SetNetwork(NetworkViewPoint network)
        {
            _network = network;
        }

        protected void CommandCallback(byte[] command)
        {
            if (CommandCallbacks != null && CommandCallbacks.ContainsKey(Id))
            {
                if (CommandCallbacks[Id].ContainsKey(command[1]))
                {
                    NodeTag dNode = DstNode;
                    NodeTag dstNode = new NodeTag(Network.NodeTag.Id, ReceivedAchData.DstNode.EndPointId);
                    CommandCallbacks[Id][command[1]](command, dstNode, ReceivedAchData.SrcNode);
                }
            }
        }

        protected bool IsCommandCallbackRegistered(byte commandId) {
            if(CommandCallbacks != null && CommandCallbacks.ContainsKey(Id))
            {
                if (CommandCallbacks[Id].ContainsKey(commandId)) {
                    return true;
                }
            }
            return false;
        }


        public abstract void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport);
    }

    public interface IBasicMapping
    {
        bool BasicSet(byte value);
        COMMAND_CLASS_BASIC_V2.BASIC_REPORT BasicV2Get();
    }
}
