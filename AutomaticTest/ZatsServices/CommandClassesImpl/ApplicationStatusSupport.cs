using System;
using System.Collections.Generic;
using System.Text;

namespace Zats.Services
{
    [CommandClassName("Application Status Command Class")]
    [CommandClassId(COMMAND_CLASS_APPLICATION_STATUS.ID)]
    [MinVersion(1)]
    [MaxVersion(1)]
    public class ApplicationStatusSupport : CommandClassSupport
    {
        private ApplicationStatusSupportData ApplicationStatusSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return ApplicationStatusSupportData; }
            set { ApplicationStatusSupportData = (ApplicationStatusSupportData)value; }
        }

        public ApplicationStatusSupport()
            : this(new ApplicationStatusSupportData() { })
        { }

        public ApplicationStatusSupport(ApplicationStatusSupportData applicationStatusSupportData)
            : base(COMMAND_CLASS_APPLICATION_STATUS.ID)
        {
            ApplicationStatusSupportData = applicationStatusSupportData;
        }
        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet,
                                                    COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        { }

        public override void UpdateSupportData(CommandClassSupportData data)
        { }

    }

    public class ApplicationStatusSupportData : CommandClassSupportData
    {
    }
}
