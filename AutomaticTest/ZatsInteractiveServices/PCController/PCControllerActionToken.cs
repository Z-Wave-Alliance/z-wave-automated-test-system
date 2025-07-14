// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ZWave.Xml.Application;

namespace Zats.Services
{
    public class PCControllerActionToken
    {
        public class ExpectData : BasicToken.ExpectData
        {
            private readonly UIInstance _uiInstance;
            private readonly ZWaveDefinition _zWaveDefinition;
            private readonly byte[] _expectedCommand;
            private readonly int _numberOfBytesToCompare;
            private readonly int _timeout;
            private DateTime expectetionStartedAt;

            public ExpectData(UIInstance uiInstance, ZWaveDefinition zWaveDefinition, byte[] expectedCommand, int numberOfBytesToCompare, int timeout)
            {
                _uiInstance = uiInstance;
                _zWaveDefinition = zWaveDefinition;
                _expectedCommand = expectedCommand;
                _numberOfBytesToCompare = numberOfBytesToCompare;
                _timeout = timeout;
                expectetionStartedAt = DateTime.Now;
            }

            public override BasicResult.ExpectData WaitCompletedSignal()
            {
                var timeSpent = (int)(DateTime.Now - expectetionStartedAt).TotalMilliseconds;
                if (_timeout > timeSpent)
                {
                    var timeLeft = _timeout - timeSpent;
                    var MAX_WAIT_TIME_MS = 10 * 60 * 1000; // 10min
                    timeLeft = timeLeft > MAX_WAIT_TIME_MS ? MAX_WAIT_TIME_MS : timeLeft;
                    System.Threading.Thread.Sleep(timeLeft);
                }
                return new PCControllerActionResult.ExpectData(_uiInstance, _zWaveDefinition, _expectedCommand, _numberOfBytesToCompare);
            }
        }

        public class ExpectDataWithS2Extension : BasicToken.ExpectData
        {
            private readonly UIInstance _uiInstance;
            private readonly ZWaveDefinition _zWaveDefinition;
            private readonly int _timeout;
            private readonly SecurityS2ExtensionTypes[] _securityS2ExtensionType;
            private DateTime expectetionStartedAt;

            public ExpectDataWithS2Extension(UIInstance uiInstance, ZWaveDefinition zWaveDefinition, SecurityS2ExtensionTypes[] securityS2ExtensionType, int timeout)
            {
                _uiInstance = uiInstance;
                _zWaveDefinition = zWaveDefinition;
                _securityS2ExtensionType = securityS2ExtensionType;
                _timeout = timeout;
                expectetionStartedAt = DateTime.Now;
            }

            public override BasicResult.ExpectData WaitCompletedSignal()
            {
                var timeSpent = (int)(DateTime.Now - expectetionStartedAt).TotalMilliseconds;
                if (_timeout > timeSpent)
                {
                    var timeLeft = _timeout - timeSpent;
                    var MAX_WAIT_TIME_MS = 10 * 60 * 1000; // 10min
                    timeLeft = timeLeft > MAX_WAIT_TIME_MS ? MAX_WAIT_TIME_MS : timeLeft;
                    System.Threading.Thread.Sleep(timeLeft);
                }
                return new PCControllerActionResult.ExpectDataWithS2Extension(_uiInstance, _zWaveDefinition, _securityS2ExtensionType);
            }
        }

        public class AddNode : BasicToken.AddNode
        {
            private UIInstance _uiInstance;
            public SecuritySchemes[] _grantSchemes;
            private readonly int _timeout;
            private INodesListObserver _nodesListObserver;
            private PCControllerDeviceManager _pcControllerDeviceManager;
            private Stopwatch _stopwatch;

            public AddNode(UIInstance uiInstance, INodesListObserver nodesListObserver, PCControllerDeviceManager pcControllerDeviceManager, SecuritySchemes[] grantedSchemes, int timeout)
            {
                _uiInstance = uiInstance;
                _timeout = timeout;
                _pcControllerDeviceManager = pcControllerDeviceManager;
                _grantSchemes = grantedSchemes;
                _nodesListObserver = nodesListObserver;
                _stopwatch = new Stopwatch();
                _stopwatch.Start();
            }

            public override BasicResult.AddNode WaitCompletedSignal()
            {
                string dskString = null;
                var _dsk = _pcControllerDeviceManager.IncludeSettings.JoinDSK;
                if (_dsk != null && _dsk.Length > 1)
                {
                    dskString = ((_dsk[0] << 8) | _dsk[1]).ToString();
                }
                var sisUI = _pcControllerDeviceManager.SisController.Value == 0 ? _uiInstance : _pcControllerDeviceManager.GetUIInstance(_pcControllerDeviceManager.SisController);
                SetupLearnModeWithDSKExchange(sisUI, dskString, _grantSchemes);

                sisUI.Automation.WaitForElementIsVisible(PCControllerIdMap.MainWindow.BusyView, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
                var timeoutLeft = _stopwatch.ElapsedMilliseconds > _timeout ? 100 : (int)(_timeout - _stopwatch.ElapsedMilliseconds);
                if (sisUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null, timeoutLeft))
                {
                    sisUI.Automation.TryNavigateTo(PCControllerIdMap.MainWindow.BusyView)?.TryNavigateTo(PCControllerIdMap.MainWindow.AbortBtn)?.ClickIfEnabled();
                    sisUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
                }
                if (_pcControllerDeviceManager.SisController.Value != 0 &&
                    _uiInstance.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null, timeoutLeft))
                {
                    _uiInstance.Automation.TryNavigateTo(PCControllerIdMap.MainWindow.BusyView)?.TryNavigateTo(PCControllerIdMap.MainWindow.AbortBtn)?.ClickIfEnabled();
                    _uiInstance.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
                }

                return new PCControllerActionResult.AddNode(_uiInstance, _nodesListObserver.GetIncludedNodeInfo()?.First());
            }

            public override bool IsStateActive
            {
                get { return _uiInstance.Automation.CheckVisible(PCControllerIdMap.MainWindow.BusyView); }
            }

            /// <summary>
            /// Sete up KEX Set with granted Schemes
            /// </summary>
            /// <param name="grantSchemes">Null or CancelGrantedSchemes - Cancel KEX Set, 0 by dafault = enable all available, else rest listed</param>
            /// <param name="dlgPrepSolution">Dialog window of inclusion the controller</param>
            private void SetGrantedSchemes(SecuritySchemes[] grantSchemes, IZatsAutomation dlgPrepSolution)
            {
                if (grantSchemes == null)
                {
                    if (dlgPrepSolution.CheckVisible(PCControllerIdMap.DialogWindow.ID))
                    {
                        dlgPrepSolution.CloseWindow();
                    }
                }
                else if (grantSchemes.Length > 0) //= 0 by default
                {
                    dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.S2AccessChk).SetCheckBoxStateAt(false);
                    dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.S2AuthenticatedChk).SetCheckBoxStateAt(false);
                    dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.S2UnauthenticatedChk).SetCheckBoxStateAt(false);
                    dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.S0Chk).SetCheckBoxStateAt(false);

                    if (grantSchemes.Contains(SecuritySchemes.S0))
                    {
                        dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.S0Chk).SetCheckBoxStateAt(true);
                    }
                    if (grantSchemes.Contains(SecuritySchemes.S2_ACCESS))
                    {
                        dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.S2AccessChk).SetCheckBoxStateAt(true);
                    }
                    if (grantSchemes.Contains(SecuritySchemes.S2_AUTHENTICATED))
                    {
                        dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.S2AuthenticatedChk).SetCheckBoxStateAt(true);
                    }
                    if (grantSchemes.Contains(SecuritySchemes.S2_UNAUTHENTICATED))
                    {
                        dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.S2UnauthenticatedChk).SetCheckBoxStateAt(true);
                    }
                }
                dlgPrepSolution.WaitForElementNotVisible(PCControllerIdMap.DialogWindow.ID,
                    () => dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.OkBtn).ClickIfEnabled());

            }

            private void SetupLearnModeWithDSKExchange(UIInstance includingUI, string dsk, SecuritySchemes[] grantSchemes)
            {
                var KexReportTs = 10000;
                bool dialogReady = includingUI.Automation.WaitForDialogIsVisible(PCControllerIdMap.DialogWindow.ID, "Preparing inclusion", AutomationServiceBase.WAIT_DLG_TIMEOUT_MS);
                if (dialogReady)
                {
                    var prepDlg = includingUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Preparing inclusion");
                    SetGrantedSchemes(grantSchemes, prepDlg);

                    var isInProc = includingUI.Automation.CheckVisible(PCControllerIdMap.MainWindow.BusyView);
                    var isIncludingEnterDSKShown = includingUI.Automation.CheckVisible(PCControllerIdMap.DialogWindow.ID) ||
                                                   includingUI.Automation.WaitForDialogIsVisible(PCControllerIdMap.DialogWindow.ID, "Enter DSK", isInProc ? KexReportTs : 100);
                    if (isIncludingEnterDSKShown)
                    {
                        try
                        {
                            var enterDksDlg = includingUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Enter DSK");
                            if (!string.IsNullOrEmpty(dsk))
                            {
                                enterDksDlg.NavigateTo(PCControllerIdMap.UserInputDialog.InTb).SetElementValue(dsk);
                                enterDksDlg.NavigateTo(PCControllerIdMap.UserInputDialog.OkBtn).Click();
                            }
                            else
                            {
                                enterDksDlg.CloseWindow();
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            // In case we do not need to specify DSK key. 
                            // Or interupted by inclusion test frame
                        }
                    }
                }
            }
        }

        public class IncludeNode : BasicToken.Includenode
        {
            private AddNode _addNode;

            public IncludeNode(UIInstance uiInstance, INodesListObserver nodesListObserver, PCControllerDeviceManager pcControllerDeviceManager, SecuritySchemes[] grantedSchemes, int timeout)
            {
                _addNode = new AddNode(uiInstance, nodesListObserver, pcControllerDeviceManager, grantedSchemes, timeout);
            }

            public override BasicResult.IncludeNode WaitCompletedSignal()
            {
                var addNodeRes = (PCControllerActionResult.AddNode)_addNode.WaitCompletedSignal();
                var sd = new BasicResult.SendData();
                return new PCControllerActionResult.IncludeNode(addNodeRes, sd);
            }

            public override bool IsStateActive
            {
                get { return _addNode.IsStateActive; }
            }

        }

        public class SetLearnMode : BasicToken.SetLearnMode
        {
            private UIInstance _uiInstance;
            private INodesListObserver _nodesListObserver;

            public SetLearnMode(UIInstance uiInstance, INodesListObserver nodesListObserver)
            {
                _uiInstance = uiInstance;
                _nodesListObserver = nodesListObserver;
            }

            public override BasicResult.SetLearnMode WaitCompletedSignal()
            {
                _uiInstance.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                _uiInstance.Automation.NavigateTo(PCControllerIdMap.MainWindow.AbortBtn).ClickIfEnabled();
                return new PCControllerActionResult.LearnModeResult(_uiInstance, _nodesListObserver.GetIncludedNodeInfo()?.First());
            }
        }

        public class RemoveNode : BasicToken.RemoveNode
        {
            private UIInstance _uiInstance;
            private readonly int _timeout;

            public RemoveNode(UIInstance uiInstance, int timeout)
            {
                _uiInstance = uiInstance;
                _timeout = timeout;
            }

            public override BasicResult.RemoveNode WaitCompletedSignal()
            {
                //var secondaryMainWindow = _uiInstance.Automation.NavigateTo(PCControllerIdMap.MainWindow.ID);
                //secondaryMainWindow.WaitElementVisible(PCControllerIdMap.MainWindow.BusyView, null, -1);
                _uiInstance.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                return new PCControllerActionResult.RemoveNode(_uiInstance);
            }

            public override bool IsStateActive
            {
                get { return _uiInstance.Automation.CheckVisible(PCControllerIdMap.MainWindow.BusyView); }
            }
        }

        public class ListenData : BasicToken.ListenData
        {
            private UIInstance _uiInstance;
            private readonly ZWaveDefinition _zWaveDefinition;
            private byte[] _compareData;

            public ListenData(UIInstance uiInstance, ZWaveDefinition zWaveDefinition, byte[] compareData)
            {
                _uiInstance = uiInstance;
                _zWaveDefinition = zWaveDefinition;
                _compareData = compareData;
            }

            public override BasicResult.ListenData WaitCompletedSignal()
            {
                var receivedData = new List<AchData>();
                var records = _uiInstance.GetLogRecords();
                foreach (var record in records)
                {
                    var logRecordParser = new LogRecordParser(_zWaveDefinition);
                    var res = logRecordParser.Parse(record.Value);
                    var data = res.Payload == null ? res.Command : res.Command.Concat(res.Payload).ToArray();
                    if (_compareData == null || 
                        (_compareData.Length > 1 && _compareData.Take(2).SequenceEqual(data.Take(2).ToArray())))
                    {
                        receivedData.Add(new AchData(new ZWave.BasicApplication.Operations.AchData() { Command = data }));
                    }
                }
                return new BasicResult.ListenData(receivedData);
            }
        }

        public class ExpectSmartStartAdd : BasicToken.ExpectSmartStartAdd
        {
            private UIInstance _uiInstance;
            private readonly int _timeout;
            private INodesListObserver _nodesListObserver;

            public ExpectSmartStartAdd(UIInstance uiInstance, INodesListObserver nodesListObserver, int timeout)
            {
                _uiInstance = uiInstance;
                _timeout = timeout;
                _nodesListObserver = nodesListObserver;
            }

            public override BasicResult.ExpectSmartStartAdd WaitCompletedSignal()
            {
                System.Threading.Thread.Sleep(_timeout);
                var addNodeRes = new PCControllerActionResult.AddNode(_uiInstance, _nodesListObserver.GetIncludedNodeInfo()?.First());
                return new PCControllerActionResult.ExpectSmartStartAdd(addNodeRes);
            }

            public bool IsStateActive
            {
                get { return _uiInstance.Automation.CheckVisible(PCControllerIdMap.MainWindow.BusyView); }
            }
        }

    }
}
