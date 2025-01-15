// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Windows.Automation;
using Utils;

namespace Zats.Services.UIAutomation
{
    public class WaitForElementNotVisible : WaitElementBase
    {
        public WaitForElementNotVisible(AutomationElement parent)
            : base(parent)
        {
            $"{parent.Current.Name} WaitForElementNotVisible"._DLOG();
        }

        protected override bool CheckCondition(AutomationElement element)
        {
            bool res = true;
            if (element != null)
            {
                res = !element.TryGetClickablePoint(out System.Windows.Point point);
            }
            return res;
        }
    }
}
