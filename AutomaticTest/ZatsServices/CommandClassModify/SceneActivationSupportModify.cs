using ZWave;
using ZWave.Devices;


namespace Zats.Services
{
    /// <summary>
    /// Generic Command Class support to support unknown CC's or known CC's freely
    /// Supervision is handled with SUCCESS if a Command Callback has been registered using
    /// ApplicationService.AddCommandCallbacks() method
    /// </summary>

    public class SceneActivationSupportModify : CommandClassSupport
    {
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public SceneActivationSupportModify(byte commandClassId)
            : base(commandClassId)
        {
            MinCCVersion = 1;
            MaxCCVersion = 3;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (IsCommandCallbackRegistered(supervisionGet.encapsulatedCommand[1])) {
                supervisionReport.status = (byte)SvStatusNames.Success;
                return;
            }

            supervisionReport.status = (byte)SvStatusNames.Fail;  // FAIL as default
            switch (supervisionGet.encapsulatedCommand[1])
            {
                case COMMAND_CLASS_SCENE_ACTIVATION.SCENE_ACTIVATION_SET.ID:
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
                byte[] rpt = null;
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
