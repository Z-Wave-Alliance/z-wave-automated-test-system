using System;
using System.Collections.Generic;
using System.IO;
using Utils;
#if !NETCOREAPP
using Zats.Services.UIAutomation;
#endif
using System.Linq;

namespace Zats.Services
{
    public class UIInstance : IDisposable
    {
        private const int LOG_TIMESTAMP_COLUMN = 0;
        private const int LOG_RECORD_COLUMN = 1;
        private const int SCR_OFFSET = 10;
        private const int SECOND_ROW_VERTICAL_MARGIN = 150;
        private const int WAIT_UI_AVAILABILITY_TIMEOUT = 3000;
        private ProcessProxy _processProxy;
        private bool _isConnected;
        public bool IsConnected { get { return _isConnected; } }
        public LibTypes Library { get; set; }
        public Profile CurrentProfile { get; set; }
        public string DataSourceName { get; private set; }
        public bool IsZipDataSource { get; set; }

#if !NETCOREAPP
        private ZatsAutomation _automation;
#else
        private IZatsAutomation _automation;
#endif

        public IZatsAutomation Automation
        {
            get
            {
                return _automation;
            }
        }

        public UIInstance(string dataSourceName)
        {
            DataSourceName = dataSourceName;
        }

        public bool Reconnect(int instanceCount)
        {
            $"- Reconnect - {instanceCount} : {DataSourceName}"._DLOG();
            try
            {
                var mainWindow = _automation.NavigateTo(PCControllerIdMap.MainWindow.ID);

                _automation.WaitForElementIsVisible(PCControllerIdMap.MainWindow.SettingsBtn, 1000);
                using (var settingsDlg = _automation.OpenDialog(PCControllerIdMap.SettingsDialog.ID, "Settings",
                        () => _automation.NavigateTo(PCControllerIdMap.MainWindow.SettingsBtn).Click(),
                        () => _automation.NavigateTo(PCControllerIdMap.SettingsDialog.ID).NavigateTo(PCControllerIdMap.SettingsDialog.BtnOk).Click())
                        )
                {
                    settingsDlg.DialogView.WaitForElementIsReady(PCControllerIdMap.SettingsDialog.SerialDataSourcesLb, null, WAIT_UI_AVAILABILITY_TIMEOUT);
                    if (IsZipDataSource)
                    {
                        settingsDlg.DialogView.NavigateTo(PCControllerIdMap.SettingsDialog.DiscoverBtn).Click();
                        settingsDlg.DialogView.WaitForElementIsReady(PCControllerIdMap.SettingsDialog.BtnOk, null, 10000);
                        settingsDlg.DialogView.NavigateTo(PCControllerIdMap.SettingsDialog.SocketDataSourcesLb).SelectListItemAt(DataSourceName);
                    }
                    else
                    {
                        settingsDlg.DialogView.NavigateTo(PCControllerIdMap.SettingsDialog.SerialDataSourcesLb).SelectListItemAt(DataSourceName);
                    }
                    if (!settingsDlg.DialogView.WaitForElementIsReady(PCControllerIdMap.SettingsDialog.BtnOk, null, WAIT_UI_AVAILABILITY_TIMEOUT))
                    {
                        $"- Reconnect - Failed DataSource selection: {DataSourceName}"._DLOG();
                        settingsDlg.DialogView.CloseWindow();
                    }
                }
                HandleFailedConnectionDialog(mainWindow);
                _isConnected = mainWindow.WaitForElementIsReady(PCControllerIdMap.MainMenuView.NetworkManagementBtn, null, WAIT_UI_AVAILABILITY_TIMEOUT);
            }
            catch (InvalidOperationException)
            {
                _isConnected = false;
            }
            return _isConnected;
        }

        private static void HandleFailedConnectionDialog(IZatsAutomation mainWindow)
        {
            $" - HandleFailedConnectionDialog"._DLOG();
            mainWindow.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null, WAIT_UI_AVAILABILITY_TIMEOUT);
            if (mainWindow.WaitForDialogIsVisible(PCControllerIdMap.MessageDialog.ID, PCControllerIdMap.MessageDialog.Title, WAIT_UI_AVAILABILITY_TIMEOUT))
            {
                mainWindow.NavigateTo(PCControllerIdMap.MessageDialog.ID, PCControllerIdMap.MessageDialog.Title).CloseWindow();
            }
            else
            if (mainWindow.WaitForElementIsReady(PCControllerIdMap.MessageDialog.CloseBtn, null, WAIT_UI_AVAILABILITY_TIMEOUT) &&
                mainWindow.CheckVisible(PCControllerIdMap.MessageDialog.CloseBtn, null))
            {
                System.Threading.Thread.Sleep(WAIT_UI_AVAILABILITY_TIMEOUT);
                mainWindow.NavigateTo(PCControllerIdMap.MessageDialog.CloseBtn).ClickIfEnabled();
            }
        }

        public bool RunInstanceAndConnect(int instanceCount)
        {
            if (_isConnected)
            {
                return false;
            }
            bool ret = false;
            var pathPcc = string.Empty;
            var cmd = string.Empty;
            if (CurrentProfile.PCControllerAppPath != null && File.Exists(CurrentProfile.PCControllerAppPath))
            {
                pathPcc = Path.GetDirectoryName(CurrentProfile.PCControllerAppPath);
                cmd = Path.GetFileName(CurrentProfile.PCControllerAppPath);
            }
            else
            {
                pathPcc = @"D:\work\anicca\trunk\SampleApplications\ZWavePCController\ZWaveControllerUI\bin\Debug";
                cmd = "ZWaveControllerUI.exe";
            }
            try
            {
                _processProxy = new ProcessProxy(pathPcc, cmd, string.Empty);
                _processProxy.Start(string.Empty);
#if !NETCOREAPP
                _automation = new ZatsAutomation(_processProxy);
#endif
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }
            var mainWindow = _automation.NavigateTo(PCControllerIdMap.MainWindow.ID);
            HandleFailedConnectionDialog(mainWindow);
            ret = Reconnect(instanceCount);
            if (ret)
            {
                var isReady = mainWindow.WaitForElementIsReady(PCControllerIdMap.MainMenuView.ID, null, WAIT_UI_AVAILABILITY_TIMEOUT);
                if (isReady)
                {
                    // Wait all buttons from main menu is created.
                    mainWindow.WaitForElementIsReady(PCControllerIdMap.MainMenuView.NetworkManagementBtn, null, WAIT_UI_AVAILABILITY_TIMEOUT);
                    mainWindow.WaitForElementIsReady(PCControllerIdMap.MainMenuView.CommandClassesBtn, null, WAIT_UI_AVAILABILITY_TIMEOUT);
                    //var size = mainWindow.GetElementSize();
                    //int x = 0;
                    //int y = 0;

                    //if (!IsOdd(instanceCount))
                    //{
                    //    x = SCR_OFFSET + (instanceCount - 1) * ((int)size.Width + SCR_OFFSET);
                    //    y = SCR_OFFSET;
                    //}
                    //else
                    //{
                    //    y = SCR_OFFSET + (instanceCount - 1) * (SECOND_ROW_VERTICAL_MARGIN + SCR_OFFSET);
                    //    x = SCR_OFFSET;
                    //}
                    //mainWindow.MoveElement(x, y);
                }
                else
                {
                    mainWindow.CloseWindow();
                    "Instance is not connected"._EXLOG();
                    ret = false;
                }
            }
            else
            {
                mainWindow.CloseWindow();
                "Instance failed to connect"._EXLOG();
            }
            return ret;
        }

        public INodesListObserver ActivateNodesListObserver()
        {
#if !NETCOREAPP
            return new NodesListObserver(_automation);
#else
            return null;
#endif
        }

        public INodesListObserver ActivateNodesListObserver(byte joiningNodeId)
        {
#if !NETCOREAPP
            return new NodesListObserver(_automation, joiningNodeId);
#else
            return null;
#endif
        }

        private bool IsOdd(int value)
        {
            return value % 2 != 0;
        }

        public string FindLastOperation(string operationName)
        {
            var operationNameLower = operationName.ToLower();
            return FindRecordFromLogView(false, (record) =>
            {
                bool ret = false;
                if (record.ToLower().Contains(operationNameLower))
                {
                    ret = true;
                }
                return ret;
            });
        }

        public string FindRecordFromLogView(bool isFindFirst, Predicate<string> findPredicate)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Instance is not connected");
            }

            string ret = string.Empty;
            _automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
            using (var logDlg = _automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Log",
                () =>
                {
                    _automation.WaitForElementIsReady(PCControllerIdMap.MainWindow.ShowLogBtn, null);
                    _automation.NavigateTo(PCControllerIdMap.MainWindow.ShowLogBtn).Click();
                },
                null, 30000))
            {
                var logListView = logDlg.DialogView.NavigateTo(PCControllerIdMap.LogView.LogLv);
                int logRecordsCount = logListView.GetGridRowCount();

                if (isFindFirst)
                {
                    for (int i = 0; i < logRecordsCount; i++)
                    {
                        var record = logListView.GetValueFromGrid(i, LOG_RECORD_COLUMN);

                        if (findPredicate != null && findPredicate(record))
                        {
                            ret = record;
                            $"{_automation.CurrentName} - FindRecordFromLogView with Predicate = {record}; isFindFirst={isFindFirst}"
                                ._DLOG();
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = logRecordsCount - 1; i >= 0; i--)
                    {
                        var record = logListView.GetValueFromGrid(i, LOG_RECORD_COLUMN);

                        if (findPredicate != null && findPredicate(record))
                        {
                            ret = record;
                            $"{_automation.CurrentName} - FindRecordFromLogView with Predicate = {record}; isFindFirst={isFindFirst}"
                                ._DLOG();
                            break;
                        }
                    }
                }
            }
            _automation.WaitForElementIsReady(PCControllerIdMap.MainWindow.MainMenuBtn, null, WAIT_UI_AVAILABILITY_TIMEOUT);
            return ret;
        }

        public string FindLastOperationCommandQueue(string operationName)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Instance is not connected");
            }

            string ret = string.Empty;
            _automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
            using (var logDlg = _automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Commands Queue",
                    () => _automation.NavigateTo(PCControllerIdMap.MainWindow.CommandQueueBtn).Click(),
                    null)
                    )
            {
                var сommandQueueListView = logDlg.DialogView.NavigateTo(PCControllerIdMap.CommandQueueView.ItemsLv);
                int logRecordsCount = сommandQueueListView.GetGridRowCount();
                var operationNameLower = operationName.ToLower();
                for (int i = logRecordsCount - 1; i >= 0; i--)
                {
                    var record = сommandQueueListView.GetValueFromGrid(i, LOG_RECORD_COLUMN);
                    if (record.ToLower().Contains(operationNameLower))
                    {
                        ret = record;
                        $"{_automation.CurrentName} - FindLastOperationCommandQueue = {record}"._DLOG();
                        break;
                    }
                }
            }
            _automation.WaitForElementIsReady(PCControllerIdMap.MainWindow.MainMenuBtn, null, WAIT_UI_AVAILABILITY_TIMEOUT);
            return ret;
        }

        public void ClearLog()
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Instance is not connected");
            }
            _automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
            using (var logDlg = _automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Log",
                    () => _automation.NavigateTo(PCControllerIdMap.MainWindow.ShowLogBtn).Click(),
                    null)
                    )
            {
                logDlg.DialogView.NavigateTo(PCControllerIdMap.LogView.ClearBtn).Click();
            }
            _automation.WaitForElementIsReady(PCControllerIdMap.MainWindow.MainMenuBtn, null, WAIT_UI_AVAILABILITY_TIMEOUT);
        }

        public string GetLastLogRecord()
        {
            string ret = string.Empty;
            if (!_isConnected)
            {
                throw new InvalidOperationException("Instance is not connected");
            }

            _automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
            using (var logDlg = _automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Log",
                () => _automation.NavigateTo(PCControllerIdMap.MainWindow.ShowLogBtn).Click(),
                null)
                )
            {
                var logListView = logDlg.DialogView.NavigateTo(PCControllerIdMap.LogView.LogLv);
                int logRecordsCount = logListView.GetGridRowCount();
                ret = logListView.GetValueFromGrid(logRecordsCount - 1, LOG_RECORD_COLUMN);
            }
            _automation.WaitForElementIsReady(PCControllerIdMap.MainWindow.MainMenuBtn, null, WAIT_UI_AVAILABILITY_TIMEOUT);
            return ret;
        }

        public IDictionary<DateTime, string> GetLogRecords()
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Instance is not connected");
            }

            _automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
            var ret = new Dictionary<DateTime, string>();
            using (var logDlg = _automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Log",
                    () => _automation.NavigateTo(PCControllerIdMap.MainWindow.ShowLogBtn).Click(),
                    null)
                    )
            {
                var logListView = logDlg.DialogView.NavigateTo(PCControllerIdMap.LogView.LogLv);
                int logRecordsCount = logListView.GetGridRowCount();
                for (int i = 0; i < logRecordsCount; i++)
                {
                    var record = logListView.GetValueFromGrid(i, LOG_RECORD_COLUMN);
                    if (record.Contains("Rx"))
                    {
                        DateTime timestamp = DateTime.Today;
                        var timeString = logListView.GetValueFromGrid(i, LOG_TIMESTAMP_COLUMN);
                        DateTime.TryParse(timeString, out timestamp);
                        bool sameTime = ret.Keys.Where(key => key.Millisecond == timestamp.Millisecond).Count() > 0;

                        if (!sameTime)
                        {
                            ret.Add(timestamp, record);
                        }
                        else
                        {
                            timestamp = timestamp.AddMilliseconds(1);
                            ret.Add(timestamp, record);
                        }
                    }
                }
            }
            _automation.WaitForElementIsReady(PCControllerIdMap.MainWindow.MainMenuBtn, null, WAIT_UI_AVAILABILITY_TIMEOUT);
            return ret;
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_isConnected)
            {
                _processProxy.Close();
            }
        }

        #endregion
    }
}
