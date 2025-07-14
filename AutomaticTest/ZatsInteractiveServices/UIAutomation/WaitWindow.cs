// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace Zats.Services.UIAutomation
{
    public class WaitWindow : IDisposable
    {
        private readonly string _viewId;
        private readonly string _name;
        private readonly int _timeout;
        private readonly AutomationElement _root;
        private readonly AutomationEventHandler _windowOpenOrClosedEventHandler;
        private ManualResetEventSlim _waitWindowLoadedEvent;
        private ManualResetEventSlim _waitWindowClosedEvent;
        private bool _isWaitForClose;

        private readonly object _sync = new object();

        /// <summary>
        /// Wait handler for open/close dialog window
        /// </summary>
        /// <param name="root">Root AutomationElement</param>
        /// <param name="viewId">Window Automatic Id</param>
        /// <param name="name">Window name</param>
        /// <param name="timeout">Wait time for open/close windon</param>
        /// <param name="isWaitForClose">Optional if required wait until window is not closed</param>
        public WaitWindow(AutomationElement root, string viewId, string name, int timeout, bool isWaitForClose = false)
        {
            _root = root;
            _viewId = viewId;
            _name = name;
            _timeout = timeout;
            _waitWindowLoadedEvent = new ManualResetEventSlim(false, 500);
            _windowOpenOrClosedEventHandler = new AutomationEventHandler(OnWindowOpenOrClose);
            Automation.AddAutomationEventHandler(WindowPattern.WindowOpenedEvent,
                root,
                TreeScope.Subtree,
                _windowOpenOrClosedEventHandler);
            _isWaitForClose = isWaitForClose;
        }

        public bool Run(Action openWindowAction)
        {
            bool ret = false;
            var waitWindowOpenedTask = Task.Run(() =>
            {
                var automation = new ZatsAutomation(_root);
                ret = automation.CheckVisible(_viewId, _name) || _waitWindowLoadedEvent.Wait(_timeout);
            });
            if (openWindowAction != null)
            {
                openWindowAction.Invoke();
            }
            waitWindowOpenedTask.Wait();
            return ret;
        }

        private void OnWindowOpenOrClose(object src, AutomationEventArgs e)
        {
            AutomationElement sourceElement = src as AutomationElement;
            if (src != null && e.EventId == WindowPattern.WindowOpenedEvent && sourceElement.Current.AutomationId.Equals(_viewId))
            {
                if (_name != null)
                {
                    if (sourceElement.Current.Name.Equals(_name))
                    {
                        _waitWindowLoadedEvent?.Set();
                    }
                }
                else
                {
                    _waitWindowLoadedEvent?.Set();
                }

                var idCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, _viewId);
                var isWindowCondition = new PropertyCondition(AutomationElement.IsWindowPatternAvailableProperty, true);
                var nameCondition = new PropertyCondition(AutomationElement.NameProperty, _name);
                var windowWalker = new TreeWalker(new AndCondition(idCondition, isWindowCondition, nameCondition));

                var resElement = windowWalker.GetFirstChild(_root);
                if (resElement != null)
                {
                    _waitWindowClosedEvent = new ManualResetEventSlim(false, 1000);
                    Automation.AddAutomationEventHandler(WindowPattern.WindowClosedEvent,
                        resElement,
                        TreeScope.Element,
                        _windowOpenOrClosedEventHandler);
                }
            }
            else if (e.EventId == WindowPattern.WindowClosedEvent)
            {
                var args = (WindowClosedEventArgs)e;
                if (_waitWindowClosedEvent != null)
                {
                    _waitWindowClosedEvent.Set();
                }

                if (sourceElement != null)
                {
                    Automation.RemoveAutomationEventHandler(WindowPattern.WindowClosedEvent,
                        sourceElement,
                        _windowOpenOrClosedEventHandler);
                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Automation.RemoveAutomationEventHandler(WindowPattern.WindowOpenedEvent,
                _root,
                _windowOpenOrClosedEventHandler);
            if (_waitWindowClosedEvent != null)
            {
                if (_isWaitForClose)
                {
                    _waitWindowClosedEvent.Wait(_timeout);
                }
                _waitWindowClosedEvent.Dispose();
                _waitWindowClosedEvent = null;
            }
            _waitWindowLoadedEvent.Dispose();
            _waitWindowLoadedEvent = null;
        }

        #endregion
    }
}
