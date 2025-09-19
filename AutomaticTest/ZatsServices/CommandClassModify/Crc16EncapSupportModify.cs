// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
using ZWave;
using ZWave.Devices;


namespace Zats.Services
{
    /// <summary>
    /// Generic Command Class support to support unknown CC's or known CC's freely
    /// Supervision is handled with SUCCESS if a Command Callback has been registered using
    /// ApplicationService.AddCommandCallbacks() method
    /// </summary>

    public class Crc16EncapSupportModify : CommandClassSupport
    {
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public Crc16EncapSupportModify(byte commandClassId)
            : base(commandClassId)
        {
            MinCCVersion = 1;
            MaxCCVersion = 3;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (IsCommandCallbackRegistered(supervisionGet.encapsulatedCommand[1]))
            {
                supervisionReport.status = (byte)SvStatusNames.Success;
                return;
            }

            supervisionReport.status = (byte)SvStatusNames.NoSupport;
            switch (supervisionGet.encapsulatedCommand[1])
            {
                case COMMAND_CLASS_CRC_16_ENCAP.CRC_16_ENCAP.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte)SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }

        protected override void OnHandledDelayed(DataReceivedUnit ou)
        {
            ou.SetNextActionItems();

            if (MatchesDestinationEndPoint())
            {
                var sendDstNode = new NodeTag(ReceivedAchData.DstNode.Id, DstNode.EndPointId);
                byte[] command = ReceivedAchData.Command;
                var scheme = (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme;
                bool isHandlingAllowed = IsHandlingAllowed(Network, command, scheme, Secure);

                if (isHandlingAllowed && command != null && command.Length > 1)
                {
                    CommandCallback(command, sendDstNode.EndPointId);
                }
            }
        }

        // CC doesn't use internal data, just satisfy abstract members
        public override CommandClassSupportData CommandClassSupportData {
            get { return null; }
            set {; }
        }

        public override void UpdateSupportData(CommandClassSupportData data) {

        }
    }

}
