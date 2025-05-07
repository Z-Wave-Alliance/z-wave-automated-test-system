using System.Windows.Automation;

namespace Zats.Services.UIAutomation
{
    public class WaitElementReady : WaitElementBase
    {
        public WaitElementReady(AutomationElement parent)
            : base(parent)
        {
        }

        protected override bool CheckCondition(AutomationElement element)
        {
            return element != null && element.Current.IsEnabled;
        }
    }
}
