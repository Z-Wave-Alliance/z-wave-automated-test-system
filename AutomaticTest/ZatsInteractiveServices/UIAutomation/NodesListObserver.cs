// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Automation;
using Utils;

namespace Zats.Services.UIAutomation
{
    public class NodesListObserver : INodesListObserver
    {
        readonly ZatsAutomation _mainWindowAutomation;
        private Dictionary<string, AutomationElement> _initialNodesListState = new Dictionary<string, AutomationElement>();
        private List<string> _otherNodes;

        public NodesListObserver(ZatsAutomation automation)//, AutomationElement mainWindowElement)
        {
            _mainWindowAutomation = automation;
            _initialNodesListState = GetNodesState(_mainWindowAutomation.AutomationElement);
        }

        public NodesListObserver(ZatsAutomation automation, byte joiningNodeId) : this(automation)
        {
            _otherNodes = _initialNodesListState.
                Where(node => joiningNodeId.ToString() != node.Key).
                Select(node => node.Key).
                ToList();
            if (_otherNodes.Count == 0)
            {
                _otherNodes = null;
            }
        }

        private Dictionary<string, AutomationElement> GetNodesState(AutomationElement mainWindowElement)
        {
            mainWindowElement.SetFocus();
            System.Threading.Thread.Sleep(100);
            var idCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.NetworkManagementView.ID, PropertyConditionFlags.IgnoreCase);
            var treeWalker = new TreeWalker(idCondition);
            var networkManagementView = treeWalker.GetFirstChild(mainWindowElement);

            var idConditionNodes = new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.NodesListView.NodesLv, PropertyConditionFlags.IgnoreCase);
            var treeWalkerNodesLv = new TreeWalker(idConditionNodes);
            var nodesLv = treeWalkerNodesLv.GetFirstChild(networkManagementView);

            var itemContainer = nodesLv.GetCurrentPattern(ItemContainerPattern.Pattern) as ItemContainerPattern;
            var nodesState = new Dictionary<string, AutomationElement>();
            AutomationElement item = null;
            do
            {
                item = itemContainer.FindItemByProperty(item, null, string.Empty);
                if (item != null)
                {
                    if (string.IsNullOrEmpty(item.Current.Name))
                    {
                        $"Catched as empty node item in GetNodesState{item.Current.Name}"._DLOG();
                    }
                    nodesState.Add(item.Current.Name, item);
                }
            }
            while (item != null);
            return nodesState;
        }

        public NodeInfo[] GetIncludedNodeInfo()
        {
            var nodesToExcept = _otherNodes ?? _initialNodesListState.Select(node => node.Key);
            var currentState = GetNodesState(_mainWindowAutomation.AutomationElement);
            var newNodesKeys = currentState.Keys.Except(nodesToExcept).ToArray();
            if (newNodesKeys == null || newNodesKeys.Length == 0)
            {
                return null;
            }
            List<NodeInfo> ret = new List<NodeInfo>();
            foreach (var newNodeKey in newNodesKeys)
            {
                var addedElement = currentState[newNodeKey];
                try
                {
                    var nodeIdCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.NodesListView.NodeIdTb, PropertyConditionFlags.IgnoreCase);
                    var nodeIdTreeWalker = new TreeWalker(nodeIdCondition);
                    var textBlockElement = nodeIdTreeWalker.GetFirstChild(addedElement);
                    var peerName = textBlockElement.GetCurrentPropertyValue(AutomationElement.NameProperty).ToString();
                    var nodeIdname = peerName.Split('.');
                    var id = byte.Parse(nodeIdname[0]);
                    byte endPointId = 0;
                    if (nodeIdname.Length > 1)
                    {
                        endPointId = byte.Parse(nodeIdname[1]);
                    }
                    var nodeTypeCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.NodesListView.NodeTypeTb, PropertyConditionFlags.IgnoreCase);
                    var nodeTypeTreeWalker = new TreeWalker(nodeTypeCondition);
                    textBlockElement = nodeTypeTreeWalker.GetFirstChild(addedElement);
                    if (textBlockElement == null)
                    {
                        $"{_mainWindowAutomation?.CurrentName} - GetIncludedNodeInfo, textBlockElement == null, GetPreviousSibling"._DLOG();
                        textBlockElement = nodeTypeTreeWalker.GetPreviousSibling(addedElement);
                        if (textBlockElement == null)
                        {
                            $"{_mainWindowAutomation?.CurrentName} - GetIncludedNodeInfo, textBlockElement == null, GetNextSibling"._DLOG();
                            textBlockElement = nodeTypeTreeWalker.GetNextSibling(addedElement);
                        }
                    }
                    var name = textBlockElement.GetCurrentPropertyValue(AutomationElement.NameProperty).ToString();
                    var addRemoveNodeStatus = _initialNodesListState.ContainsKey(newNodeKey) ?
                        AddRemoveNodeStatuses.Replicated :
                        AddRemoveNodeStatuses.Added;

                    ret.Add(new NodeInfo(id,
                        name,
                        GetLvItemCheckBoxState(addedElement, PCControllerIdMap.NodesListView.IsListeningChk),
                        GetLvItemCheckBoxState(addedElement, PCControllerIdMap.NodesListView.IsVirtualChk),
                        GetLvItemCheckBoxState(addedElement, PCControllerIdMap.NodesListView.EnableQOChk),
                        addRemoveNodeStatus,
                        endPointId));
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("Automation: unsupported node information view, " + e.Message);
                }
            }
            return ret.ToArray();
        }

        private bool? GetLvItemCheckBoxState(AutomationElement lvItem, string automationId)
        {
            var idCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);
            var checkBoxTreeWalker = new TreeWalker(idCondition);
            var checkBoxElement = checkBoxTreeWalker.GetFirstChild(lvItem);
            if (checkBoxElement != null)
            {
                var isEnabled = (bool)checkBoxElement.GetCurrentPropertyValue(AutomationElement.IsEnabledProperty);
                if (isEnabled)
                {
                    var toggleElement = checkBoxElement.GetCurrentPattern(TogglePattern.Pattern) as TogglePattern;
                    if (toggleElement.Current.ToggleState != ToggleState.Indeterminate)
                    {
                        return toggleElement.Current.ToggleState == ToggleState.On;
                    }
                }
            }
            return false;
        }
    }
}
