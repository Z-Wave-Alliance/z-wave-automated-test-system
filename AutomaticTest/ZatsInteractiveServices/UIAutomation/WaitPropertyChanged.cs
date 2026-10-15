// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Windows.Automation;
using System.Threading;

namespace Zats.Services.UIAutomation
{
    public class WaitPropertyChanged<T>: IDisposable
    {
        private readonly AutomationElement _element;
        private AutoResetEvent _eventWaitPropertyChanged;
        private AutomationPropertyChangedEventHandler _propChangeHandler;
        private AutomationProperty _property;
        private T _propertyValue;

        public AutomationProperty Property { get; set; }
        public object Value { get; set; }

        public WaitPropertyChanged(AutomationElement element)
        {
            _element = element;
            _eventWaitPropertyChanged = new AutoResetEvent(false);
            Automation.AddAutomationPropertyChangedEventHandler(_element,
                TreeScope.Element,
                _propChangeHandler = new AutomationPropertyChangedEventHandler(OnPropertyChange),
                AutomationElement.IsEnabledProperty);
        }

        public bool Wait(AutomationProperty property, T value, int timeout)
        {
            _property = property;
            _propertyValue = value;
            return _eventWaitPropertyChanged.WaitOne(timeout);
        }

        private void OnPropertyChange(object src, AutomationPropertyChangedEventArgs e)
        {
            AutomationElement sourceElement = src as AutomationElement;
            if (e.Property == _property &&
                e.NewValue is T &&
                _propertyValue.Equals((T) e.NewValue)
                )
            {
                _eventWaitPropertyChanged.Set();
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            _eventWaitPropertyChanged.Close();
            _eventWaitPropertyChanged = null;
            Automation.RemoveAutomationPropertyChangedEventHandler(_element, _propChangeHandler);
            _propChangeHandler = null;
        }

        #endregion
    }
}
