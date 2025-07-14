// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Windows.Automation;
using Utils;

namespace Zats.Services.UIAutomation
{
    public class WaitForElementIsVisible : WaitElementBase
    {
        public WaitForElementIsVisible(AutomationElement parent)
            : base(parent)
        {
            $"{parent.Current.Name} WaitForElementIsVisible"._DLOG();
        }

        protected override bool CheckCondition(AutomationElement element)
        {
            try
            {
                if (element == null)
                {
                    return false;
                }
                else
                {
                    var res = element.TryGetClickablePoint(out System.Windows.Point point);
                    return res;
                }
            }
            catch (ArgumentNullException)
            {
                "<!> Catched ArgumentNullException in WaitForElementIsVisible.CheckCondition"._DLOG();
                return false;
            }
            catch(ElementNotAvailableException)
            {
                "<!> Catched ElementNotAvailableException in WaitForElementIsVisible.CheckCondition"._DLOG();
                return false;
            }
        }
    }
}
