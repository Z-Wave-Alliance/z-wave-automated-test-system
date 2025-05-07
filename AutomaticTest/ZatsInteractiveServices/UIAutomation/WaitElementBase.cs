using System;
using System.Threading;
using System.Windows.Automation;
using Utils;

namespace Zats.Services.UIAutomation
{
    public abstract class WaitElementBase : IDisposable
    {
        private const int CHECK_TIMING = 100;
        private AutoResetEvent _timeoutEvent;
        private AutoResetEvent _timingEvent;
        private AutomationElement _parent;
        private Thread _timeEventThread;


        protected WaitElementBase(AutomationElement parent)
        {
            _parent = parent;
            _timeoutEvent = new AutoResetEvent(false);
            _timingEvent = new AutoResetEvent(false);
        }

        protected abstract bool CheckCondition(AutomationElement element);

        public bool Wait(string elementId, string name, int timeout)
        {
            var parrName = _parent.Current.Name;
            $"{parrName} - Wait: {elementId}, {timeout}ms"._DLOG();
            if (timeout != Timeout.Infinite)
            {
                _timeEventThread = new Thread(() =>
                {
                    if (_timeoutEvent != null && !_timeoutEvent.WaitOne(timeout))
                    {
                        _timingEvent.Set();
                    }
                });
                _timeEventThread.Start();
            }
            bool conditionSatisfied = true;
            var idCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, elementId);
            Condition treeCondition = idCondition;
            if (!string.IsNullOrEmpty(name))
            {
                var nameCondition = new PropertyCondition(AutomationElement.NameProperty, name);
                treeCondition = new AndCondition(idCondition, nameCondition);
            }
            var treeWalker = new TreeWalker(treeCondition);
            do
            {
                try
                {
                    var element = treeWalker.GetFirstChild(_parent);
                    if (element != null)
                    {
                        conditionSatisfied = CheckCondition(element);
                    }
                }
                catch (Exception e)
                {
                    $"<!> {parrName} Wait: catch {e.Message}"._DLOG();
                }
            }
            while (!conditionSatisfied && !_timingEvent.WaitOne(CHECK_TIMING));
            _timeoutEvent.Set();
            if (conditionSatisfied)
            {
                $"{parrName} - Element ready: {elementId}, {name}"._DLOG();
            }
            return conditionSatisfied;
        }

        public AutomationElement WaitFor(string elementId, int timeout)
        {
            $"{_parent.Current.Name} - WaitFor: {elementId}, {timeout}ms"._DLOG();
            AutomationElement ret = null;
            if (timeout != Timeout.Infinite)
            {
                _timeEventThread = new Thread(() =>
                {
                    if (_timeoutEvent != null && !_timeoutEvent.WaitOne(timeout))
                    {
                        _timingEvent.Set();
                    }
                });
                _timeEventThread.Start();
            }
            bool conditionFailed = true;
            do
            {
                ret = _parent.FindFirst(TreeScope.Subtree, new PropertyCondition(AutomationElement.AutomationIdProperty, elementId));
                conditionFailed = !CheckCondition(ret);
            }
            while (conditionFailed && !_timingEvent.WaitOne(CHECK_TIMING));
            _timeoutEvent.Set();
            if (!conditionFailed)
            {
                $"{_parent.Current.Name} - Element is ready: {elementId}, {timeout}ms"._DLOG();
            }
            return ret;
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_timeEventThread != null)
            {
                _timeEventThread.Abort();
                _timeEventThread = null;
            }
            if (_timeoutEvent != null)
            {
                _timeoutEvent.Close();
                _timeoutEvent = null;
            }
            if (_timingEvent != null)
            {
                _timingEvent.Close();
                _timingEvent = null;
            }
        }

        #endregion
    }
}
