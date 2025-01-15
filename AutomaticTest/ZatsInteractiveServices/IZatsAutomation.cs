// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using Utils;

namespace Zats.Services
{
    public interface IZatsAutomation
    {
        string CurrentName { get; }
        IZatsAutomation NavigateTo(string elementId);
        IZatsAutomation NavigateTo(string elementId, string elementName);
        IZatsAutomation NavigateToNameProperty(string name);
        IZatsAutomation NavigateToClassNameProperty(string name);
        IZatsAutomation NavigateToLocalizedControlTypeAndNameProperties(string type, string name);
        IZatsAutomation TryNavigateTo(string elementId);
        IZatsAutomation TryNavigateToNameProperty(string name);
        IZatsAutomation TryNavigateToClassNameProperty(string name);
        IZatsAutomation TryNavigateToIdProperty(string id);
        void Click();
        void ClickIfEnabled();
        void TrySetCheckBoxStateAt(bool checkEnabled);
        void SelectListItemAt(string itemName);
        bool GetCheckBoxStateAt();
        void SetCheckBoxStateAt(bool state);
        string GetElementValue();
        void SetElementValue(string text);
        bool SelectComboboxItem(string itemName);
        void SelectTabPage();
        int GetGridRowCount();
        int GetGridColumnsCount();
        string GetValueFromGrid(int row, int column);
        IZatsAutomation TreeViewGetFirstChild();
        IZatsAutomation TreeViewGetNextSibling();
        void SelectRowWithinGrid(string value, int column);
        IZatsAutomation GetRowWithinGrid(string value, int column);
        void SelectValuesWithinGrid(string[] values, int column);
        void SelectValuesWithinGrid(Pair<int, string>[] columnsAndValues);
        string[] GetSelectedValuesWithinGrid(int columnNo);
        bool IsValuesWithinGrid(Pair<int, string>[] columnAndValue);
        void SelectFirstRowWithinGrid(int column);
        IZatsAutomationDialog OpenDialog(string dialogId, string dialogName, Action createDialogAction, Action closeDialogAction, int timeout = System.Threading.Timeout.Infinite);
        bool WaitForElementIsReady(string elementId, Action createElementAction, int timeout = System.Threading.Timeout.Infinite);
        bool WaitForElementNotVisible(string elementId, Action createElementAction, int timeout = System.Threading.Timeout.Infinite);
        bool WaitForElementIsVisible(string elementId, int timeout);
        bool WaitForElementIsVisible(string elementId, Action createElementAction);
        bool WaitForDialogIsVisible(string dlgId, string name, int timeout);
        bool CheckVisible(string elementId);
        bool CheckVisible(string elementId, string name);
        void MoveElement(int x, int y);
        void CloseWindow();
        void SelectRadioButtonItem();
        BasicActionResult.AssociationGroupsResult GetAssociationModel(Node nodeId, byte endPointId);
        void RemoveNodeId(Node deviceId, Node nodeId, byte groupId);
        void CreateNodeId(Node deviceId, byte groupId);
        List<ushort> GetNodesFromAssociationsTree(Node deviceId);
        void Select();
        void AddToSelection();
        void Expand();
    }
}
