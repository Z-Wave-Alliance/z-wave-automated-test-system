using ZWave;
using ZWave.Devices;


namespace Zats.Services
{
    [CommandClassName("Generic Command Class")]
    [MinVersion(1)]
    [MaxVersion(255)]
    /// <summary>
    /// Generic Command Class support to support unknown CC's or known CC's freely
    /// Supervision is handled with SUCCESS if a Command Callback has been registered using
    /// ApplicationService.AddCommandCallbacks() method
    /// </summary>

    public class GenericCcSupport : CommandClassSupport
    {
        public override CommandClassSupportData CommandClassSupportData { get; set; } // dummy

        public GenericCcSupport(byte commandClassId)
            : base(commandClassId)
        {
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (IsCommandCallbackRegistered(supervisionGet.encapsulatedCommand[1])) {
                supervisionReport.status = (byte)SvStatusNames.Success;
            
            } else {
                supervisionReport.status = (byte)SvStatusNames.NoSupport;
            }
            
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


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            // @todo: placeholder - still needs to be implemented
        }
    }

}
