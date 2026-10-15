// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Utils;
using System;
using System.Windows;
using System.Windows.Automation;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zats.Services.UIAutomation
{
    public class ZatsAutomation : IZatsAutomation
    {
        private AutomationElement _navigationElement;
        public ZatsAutomation(ProcessProxy processProxy)
        {
            _navigationElement = AutomationElement.FromHandle(processProxy.MainWindowHandle);
        }

        public ZatsAutomation(AutomationElement navigationElement)
        {
            _navigationElement = navigationElement;
        }

        public AutomationElement AutomationElement
        {
            get { return _navigationElement; }
        }

        public string CurrentName
        {
            get { return _navigationElement.Current.Name; }
        }


        #region IAutomation Members

        public IZatsAutomation NavigateTo(string elementId)
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - NavigateTo: {elementId}"._DLOG();
            return new ZatsAutomation(GetElement(elementId));
        }

        public IZatsAutomation NavigateTo(string elementId, string elementName)
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - NavigateTo: {elementId}, {elementName}"._DLOG();
            return new ZatsAutomation(GetElement(elementId, elementName));
        }

        public IZatsAutomation NavigateToNameProperty(string name)
        {
            var condition = new PropertyCondition(AutomationElement.NameProperty, name);
            return NavigateTo(condition);
        }

        public IZatsAutomation NavigateToClassNameProperty(string name)
        {
            var condition = new PropertyCondition(AutomationElement.ClassNameProperty, name);
            return NavigateTo(condition);
        }

        public IZatsAutomation NavigateToLocalizedControlTypeAndNameProperties(string type, string name)
        {
            var condition = new AndCondition(new PropertyCondition(AutomationElement.LocalizedControlTypeProperty, type, PropertyConditionFlags.IgnoreCase),
                        new PropertyCondition(AutomationElement.NameProperty, name));
            return NavigateTo(condition);
        }

        private IZatsAutomation NavigateTo(PropertyCondition condition)
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - NavigateTo: {condition.Property.ProgrammaticName} = {condition.Value}"._DLOG();
            return new ZatsAutomation(GetElement(condition));
        }

        public IZatsAutomation NavigateTo(Condition condition)
        {
            //$"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - NavigateTo: {condition.ProgrammaticName} = {condition.Value}"._DLOG();
            return new ZatsAutomation(GetElement(condition));
        }

        public void MoveElement(int x, int y)
        {
            var transformPattern = _navigationElement.GetCurrentPattern(TransformPattern.Pattern) as TransformPattern;
            transformPattern.Move(x, y);
        }

        public void CloseWindow()
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - CloseWindow"._DLOG();
            var windowPattern = _navigationElement.GetCurrentPattern(WindowPattern.Pattern) as WindowPattern;
            windowPattern.Close();
        }

        public void Click()
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - Click"._DLOG();
            ClickWithEnableCheck(false);
        }

        public void ClickIfEnabled()
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - ClickIfEnabled"._DLOG();
            ClickWithEnableCheck(true);
        }

        public void TrySetCheckBoxStateAt(bool state)
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - TrySetCheckBoxStateAt {state}"._DLOG();
            try
            {
                SetCheckBoxStateAt(state);
            }
            catch (InvalidOperationException ex)
            {
                "{0},IsEnabled={1}, {2}"._EXLOG(_navigationElement.Current.AutomationId, _navigationElement.Current.IsEnabled, ex.Message);
            }
        }

        public void SelectListItemAt(string itemName)
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - SelectListItemAt {itemName}"._DLOG();
            var listItemElement = GetListItemElement(_navigationElement, itemName);
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - GetListItemElement ={listItemElement?.Current.Name}"._DLOG();
            var selectionItemPattern = GetSelectionItemPattern(listItemElement);
            selectionItemPattern.Select();
        }

        public bool GetCheckBoxStateAt()
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - GetCheckBoxStateAt"._DLOG();
            var togglePattern = _navigationElement.GetCurrentPattern(TogglePattern.Pattern) as TogglePattern;
            return togglePattern.Current.ToggleState == ToggleState.On;
        }

        public void SetCheckBoxStateAt(bool state)
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - SetCheckBoxStateAt {state}"._DLOG();
            var togglePattern = _navigationElement.GetCurrentPattern(TogglePattern.Pattern) as TogglePattern;
            if (_navigationElement.Current.IsEnabled)
            {
                togglePattern.Toggle(); //refresh binding if not changed
                if (state && togglePattern.Current.ToggleState == ToggleState.Off)
                {
                    togglePattern.Toggle();
                }
                else if (!state && togglePattern.Current.ToggleState == ToggleState.On)
                {
                    togglePattern.Toggle();
                }
            }
        }

        public string GetElementValue()
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - GetElementValue"._DLOG();
            var valuePattern = _navigationElement.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
            if (valuePattern == null)
            {
                throw new InvalidOperationException("Target control doesn't support ValuePattern");
            }
            return valuePattern.Current.Value;
        }

        public void SetElementValue(string text)
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - SetElementValue {text}"._DLOG();
            var valuePattern = _navigationElement.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
            if (valuePattern == null)
            {
                throw new InvalidOperationException("Target control doesn't support ValuePattern");
            }
            if (valuePattern.Current.IsReadOnly)
            {
                throw new InvalidOperationException("Target control's text can't be modified");
            }
            valuePattern.SetValue(text);
        }

        public string GetElementText(AutomationElement element)
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - GetElementText"._DLOG();
            var cacheRequest = new CacheRequest
            {
                AutomationElementMode = AutomationElementMode.None,
                TreeFilter = Automation.RawViewCondition
            };
            cacheRequest.Add(AutomationElement.NameProperty);
            using (cacheRequest.Activate())
            {
                var targetText = element.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ClassNameProperty, "TextBlock"));
                return targetText?.Cached.Name;
            }
        }

        public bool SelectComboboxItem(string itemName)
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - SelectComboboxItem = {itemName}"._DLOG();
            var expandItem = _navigationElement.GetCurrentPattern(ExpandCollapsePattern.Pattern) as ExpandCollapsePattern;
            expandItem.Expand();
            Condition findCondition = new PropertyCondition(AutomationElement.NameProperty, itemName, PropertyConditionFlags.IgnoreCase);
            var comboBoxItems = _navigationElement.FindFirst(TreeScope.Subtree, findCondition);
            if (comboBoxItems != null)
            {
                var selectionItemPattern = comboBoxItems.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
                selectionItemPattern.Select();
                return true;
            }
            return false;
        }

        public void SelectTabPage()
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - SelectTabPage"._DLOG();
            var selectionItemPattern = _navigationElement.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
            selectionItemPattern.Select();
        }

        public int GetGridRowCount()
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - GetGridRowCount"._DLOG();
            var gridPattern = _navigationElement.GetCurrentPattern(GridPattern.Pattern) as GridPattern;
            var ret = gridPattern.Current.RowCount;
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - GetGridRowCount returns = {ret}"._DLOG();
            return ret;
        }

        public int GetGridColumnsCount()
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - GetGridColumnsCount"._DLOG();
            var gridPattern = _navigationElement.GetCurrentPattern(GridPattern.Pattern) as GridPattern;
            var ret = gridPattern.Current.ColumnCount;
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - GetGridColumnsCount returns = {ret}"._DLOG();
            return ret;
        }

        public string GetValueFromGrid(int row, int column)
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - GetValueFromGrid r={row}, c={column}"._DLOG();
            Condition isGridCondition = new PropertyCondition(AutomationElement.IsGridPatternAvailableProperty, true);
            var gridElement = _navigationElement.FindFirst(TreeScope.Subtree, isGridCondition);
            if (gridElement != null)
            {
                var grid = gridElement.GetCurrentPattern(GridPattern.Pattern) as GridPattern;
                WaitForElementIsReady(_navigationElement.Current.AutomationId, null, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
                var gridCellElement = grid.GetItem(row, column);
                var isGridItem = gridCellElement != null && gridCellElement.TryGetCurrentPattern(GridItemPattern.Pattern, out object pattern);
                if (!isGridItem)
                {
                    var rowItem = grid.GetItem(0, row);
                    var treeWalkerGridItem = new TreeWalker(new PropertyCondition(AutomationElement.IsGridItemPatternAvailableProperty, true));
                    var gridItemElement = treeWalkerGridItem.GetFirstChild(rowItem);
                    while (gridItemElement != null)
                    {
                        var gridtem = gridItemElement.GetCurrentPattern(GridItemPattern.Pattern) as GridItemPattern;
                        if (gridtem.Current.Column == column)
                        {
                            gridCellElement = gridItemElement;
                            break;
                        }
                        gridItemElement = treeWalkerGridItem.GetNextSibling(gridItemElement);
                    }
                }
                if (gridCellElement != null)
                {
                    return GetElementText(gridCellElement);
                }
            }
            throw new InvalidOperationException("No element found within grid at row: " + row + " column: " + column);
        }

        public IZatsAutomation TreeViewGetFirstChild()
        {
            var treeWalker = TreeWalker.ContentViewWalker;
            var child = treeWalker.GetFirstChild(_navigationElement);
            return new ZatsAutomation(child);
        }

        public IZatsAutomation TreeViewGetNextSibling()
        {
            var treeWalker = TreeWalker.ContentViewWalker;
            var sibling = treeWalker.GetNextSibling(_navigationElement);
            if (sibling == null)
            {
                return null;
            }
            return new ZatsAutomation(sibling);
        }

        private void EnumerateGridRowsByColumn(int columnNo, Func<AutomationElement, bool> ContinueProdicate)
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - EnumerateGridRowsByColumn c={columnNo}"._DLOG();
            Condition isGridCondition = new PropertyCondition(AutomationElement.IsGridPatternAvailableProperty, true); //AutomationElement.IsGridItemPatternAvailableProperty - and find parrent 

            var red = WaitForElementIsReady(_navigationElement.Current.AutomationId, null, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);

            var gridElement = _navigationElement.FindFirst(TreeScope.Subtree, isGridCondition);
            if (gridElement != null)
            {
                var grid = gridElement.GetCurrentPattern(GridPattern.Pattern) as GridPattern;
                for (int rowNo = 0; rowNo < grid.Current.RowCount; rowNo++)
                {
                    var gridCellElement = grid.GetItem(rowNo, columnNo);
                    var isGridItem = gridCellElement != null && gridCellElement.TryGetCurrentPattern(GridItemPattern.Pattern, out object pattern);
                    if (!isGridItem)
                    {
                        var rowElement = grid.GetItem(0, rowNo);
                        var pc = new PropertyCondition(AutomationElement.IsGridItemPatternAvailableProperty, true);
                        var treeWalkerGridItem = new TreeWalker(pc);
                        if (rowElement != null)
                        {
                            var walkerCellElement = treeWalkerGridItem.GetFirstChild(rowElement);
                            while (walkerCellElement != null)
                            {
                                var gridItem = walkerCellElement.GetCurrentPattern(GridItemPattern.Pattern) as GridItemPattern;
                                if (gridItem.Current.Column == columnNo)
                                {
                                    gridCellElement = walkerCellElement;
                                    break;
                                }
                                walkerCellElement = treeWalkerGridItem.GetNextSibling(walkerCellElement);
                            }
                        }
                    }
                    if (gridCellElement != null)
                    {
                        if (!ContinueProdicate(gridCellElement))
                        {
                            break;
                        }
                    }
                }
            }
        }

        private AutomationElement FindRowWithinGrid(string value, int columnNo)
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - FindRowWithinGrid c={columnNo}, v={value}"._DLOG();
            AutomationElement gridRowElement = null;
            EnumerateGridRowsByColumn(columnNo, gridCellElement =>
            {
                var itemText = GetElementText(gridCellElement);
                if (!string.IsNullOrEmpty(itemText))
                {
                    if (itemText.Contains(' '))
                    {
                        itemText = string.Concat(string.Join(" ", itemText.Split(' ').OrderBy(x => x)));
                    }
                    if (string.Compare(value, itemText, StringComparison.InvariantCulture) == 0)
                    {
                        var treeWalkerSelectionItem = new TreeWalker(new PropertyCondition(AutomationElement.IsSelectionItemPatternAvailableProperty, true));
                        gridRowElement = treeWalkerSelectionItem.GetParent(gridCellElement);
                        return false;
                    }
                }
                return true;
            });
            if (gridRowElement == null)
            {
                throw new InvalidOperationException("No element found within grid within column: " + columnNo);
            }
            return gridRowElement;
        }

        public void SelectRowWithinGrid(string value, int column)
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - SelectRowWithinGrid c={column}, v={value}"._DLOG();
            var item = FindRowWithinGrid(value, column);
            var gridRow = item.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
            gridRow.Select();
        }

        public IZatsAutomation GetRowWithinGrid(string value, int column)
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - GetRowWithinGrid c={column}, v={value}"._DLOG();
            var item = FindRowWithinGrid(value, column);
            var gridRow = item.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
            gridRow.Select();
            return new ZatsAutomation(item);
        }

        public void SelectValuesWithinGrid(string[] values, int columnNo)
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - SelectValuesWithinGrid c={columnNo}, values"._DLOG();
            EnumerateGridRowsByColumn(columnNo, gridCellElement =>
            {
                var itemText = GetElementText(gridCellElement);
                var treeWalkerSelectionItem = new TreeWalker(new PropertyCondition(AutomationElement.IsSelectionItemPatternAvailableProperty, true));
                var gridRowElement = treeWalkerSelectionItem.GetParent(gridCellElement);
                var gridRow = gridRowElement.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
                if (gridRow != null && itemText != null)
                {
                    if (values.Contains(itemText))
                    {
                        gridRow.AddToSelection();
                    }
                    else
                    {
                        gridRow.RemoveFromSelection();
                    }
                }
                return true;
            });
        }

        public string[] GetSelectedValuesWithinGrid(int columnNo)
        {
            List<string> ret = new List<string>(); //values
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - GetValuesWithinGrid c={columnNo}"._DLOG();
            EnumerateGridRowsByColumn(columnNo, gridCellElement =>
            {
                var itemText = GetElementText(gridCellElement);
                var treeWalkerSelectionItem = new TreeWalker(new PropertyCondition(AutomationElement.IsSelectionItemPatternAvailableProperty, true));
                var gridRowElement = treeWalkerSelectionItem.GetParent(gridCellElement);
                var gridRow = gridRowElement.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
                if (gridRow != null && itemText != null)
                {
                    if (gridRow.Current.IsSelected)
                    {
                        ret.Add(itemText);
                    }
                }
                return true;
            });
            return ret.ToArray();
        }

        public void SelectValuesWithinGrid(Pair<int, string>[] columnsAndValues)
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - SelectValuesWithinGrid columnsAndValues"._DLOG();
            var columns = columnsAndValues.Select(pair => pair.First).Distinct();
            var valuesForEachColumn = new Dictionary<int, string[]>();
            foreach (var columnNo in columns)
            {
                valuesForEachColumn.Add(columnNo, columnsAndValues.Where(pair => pair.First == columnNo).Select(pair => pair.Second).ToArray());
            }

            foreach (var columnNo in columns)
            {
                EnumerateGridRowsByColumn(columnNo, gridCellElement =>
                {
                    var itemText = GetElementText(gridCellElement);
                    var treeWalkerSelectionItem = new TreeWalker(new PropertyCondition(AutomationElement.IsSelectionItemPatternAvailableProperty, true));
                    var gridRowElement = treeWalkerSelectionItem.GetParent(gridCellElement);
                    var gridRowItem = gridRowElement.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
                    if (gridRowItem != null && itemText != null)
                    {
                        if (valuesForEachColumn[columnNo].Contains(itemText))
                        {
                            gridRowItem.AddToSelection();
                        }
                        else
                        {
                            gridRowItem.RemoveFromSelection();
                        }
                    }
                    return true;
                });
            }
        }

        public bool IsValuesWithinGrid(Pair<int, string>[] columnsAndValues)
        {
            var columns = columnsAndValues.Select(pair => pair.First).Distinct();
            var valuesForEachColumn = new Dictionary<int, List<string>>();
            foreach (var columnNo in columns)
            {
                valuesForEachColumn.Add(columnNo, columnsAndValues.Where(pair => pair.First == columnNo).Select(pair => pair.Second).ToList());
            }
            foreach (var columnNo in columns)
            {
                EnumerateGridRowsByColumn(columnNo, gridCellElement =>
                {
                    var itemText = GetElementText(gridCellElement);
                    if (itemText != null && valuesForEachColumn[columnNo].Contains(itemText))
                    {
                        valuesForEachColumn[columnNo].Remove(itemText);
                        if (valuesForEachColumn[columnNo].Count == 0)
                        {
                            valuesForEachColumn.Remove(columnNo);
                        }
                    }
                    return true;
                });
            }
            return valuesForEachColumn.Count == 0;
        }

        private bool WaitPropertyChanged<T>(AutomationElement element, AutomationProperty property, T value)
        {
            bool ret = false;
            using (var waitPropertyChanged = new WaitPropertyChanged<T>(element))
            {
                ret = waitPropertyChanged.Wait(property, value, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
            }
            return ret;
        }

        public bool WaitForElementIsReady(string elementId, Action createElementAction, int timeout = Timeout.Infinite)
        {
            return WaitElementInternal(elementId, null, ae => { return new WaitElementReady(ae); }, createElementAction, timeout);
        }

        public bool WaitForElementNotVisible(string elementId, Action createElementAction, int timeout = Timeout.Infinite)
        {
            return WaitElementInternal(elementId, null, ae => { return new WaitForElementNotVisible(ae); }, createElementAction, timeout);
        }

        public bool WaitForElementIsVisible(string elementId, Action createElementAction)
        {
            return WaitElementInternal(elementId, null, ae => { return new WaitForElementIsVisible(ae); }, createElementAction, Timeout.Infinite);
        }

        public bool WaitForElementIsVisible(string elementId, int timeout)
        {
            return WaitElementInternal(elementId, null, ae => { return new WaitForElementIsVisible(ae); }, null, timeout);
        }

        public bool WaitForDialogIsVisible(string dlgId, string name, int timeout)
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - WaitForDialogIsVisible {dlgId}, {name}, {timeout}ms"._DLOG();
            using (var waitWindow = new WaitWindow(_navigationElement, dlgId, name, timeout))
            {
                return waitWindow.Run(null);
            }
        }

        public bool CheckVisible(string elementId)
        {
            return CheckVisible(elementId, null);
        }

        public bool CheckVisible(string elementId, string name)
        {
            $"{_navigationElement.Current.Name} - CheckVisible {elementId}, {name}"._DLOG();
            bool res = false;
            try
            {
                if (_navigationElement.Current.IsKeyboardFocusable)
                {
                    $"{_navigationElement.Current.Name} - SetFocus"._DLOG();
                    _navigationElement.SetFocus();
                }
                else
                {
                    $"{_navigationElement.Current.Name} - SetFocus Impossible"._DLOG();
                }
                var element = WaitForElementInternal(elementId, ae => { return new WaitForElementIsVisible(ae); }, null, 500);
                if (element != null)
                {
                    var r = element.TryGetClickablePoint(out Point clickablePoint);
                    if (r && string.IsNullOrEmpty(name) ? true : element.Current.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) &&
                        clickablePoint.X != 0 &&
                        clickablePoint.Y != 0)
                    {
                        res = true;
                    }
                }
            }
            catch (InvalidOperationException)
            {
                res = false;
            }
            catch (ElementNotAvailableException)
            {
                $"<!> Catched ElementNotAvailableException elementId: {elementId}, name: {name}"._DLOG();
                res = false;
            }
            return res;
        }

        #endregion

        private bool WaitElementInternal<WaitType>(string elementId, string name, Func<AutomationElement, WaitType> creator, Action createElementAction, int timeout)
            where WaitType : WaitElementBase
        {
            bool ret = false;
            var waitElementNotVisibleTask = Task.Run(() =>
            {
                using (WaitType waitElementNotVisible = creator(_navigationElement))
                {
                    ret = waitElementNotVisible.Wait(elementId, name, timeout);
                }
            });
            if (createElementAction != null)
            {
                createElementAction.Invoke();
            }
            waitElementNotVisibleTask.Wait();
            return ret;
        }

        private AutomationElement WaitForElementInternal<WaitType>(string elementId, Func<AutomationElement, WaitType> creator, Action createElementAction, int timeout)
            where WaitType : WaitElementBase
        {
            AutomationElement ret = null;
            var waitElementNotVisibleThread = new Thread(() =>
            {
                using (WaitType waitElementVisibility = creator(_navigationElement))
                {
                    ret = waitElementVisibility.WaitFor(elementId, timeout);
                }
            });
            waitElementNotVisibleThread.Start();
            if (createElementAction != null)
            {
                createElementAction.Invoke();
            }
            waitElementNotVisibleThread.Join();
            return ret;
        }

        private AutomationElement GetElement(PropertyCondition propertyCondition)
        {
            if (propertyCondition.Value.Equals(_navigationElement.GetCurrentPropertyValue(propertyCondition.Property)))
            {
                return _navigationElement;
            }
            var treeWalker = new TreeWalker(propertyCondition);
            var element = treeWalker.GetFirstChild(_navigationElement);
            if (element == null)
            {
                throw new InvalidOperationException("No element found for specified property condition");
            }
            return element;
        }

        private AutomationElement GetElement(Condition condition)
        {
            var treeWalker = new TreeWalker(condition);
            var element = treeWalker.GetFirstChild(_navigationElement);
            if (element == null)
            {
                throw new InvalidOperationException("No element found for specified property condition");
            }
            return element;
        }

        private AutomationElement GetElement(string id)
        {
            if (id.Equals(_navigationElement.GetCurrentPropertyValue(AutomationElement.AutomationIdProperty).ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                return _navigationElement;
            }
            var idCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, id);
            var treeWalker = new TreeWalker(idCondition);
            var element = treeWalker.GetFirstChild(_navigationElement);
            if (element == null)
            {
                if (CheckVisible(id, null))
                {
                    element = treeWalker.GetFirstChild(_navigationElement);
                }
                if (element == null)
                {
                    throw new InvalidOperationException("No element found with automation id: " + id);
                }
            }
            return element;
        }

        private AutomationElement GetElement(string id, string name)
        {
            var idCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, id);
            var nameCondition = new PropertyCondition(AutomationElement.NameProperty, name);
            var treeWalker = new TreeWalker(new AndCondition(idCondition, nameCondition));
            var element = treeWalker.GetFirstChild(_navigationElement);
            if (element == null)
            {
                if (CheckVisible(id, name))
                {
                    element = treeWalker.GetFirstChild(_navigationElement);
                }
                if (element == null)
                {
                    throw new InvalidOperationException("No element found with automation id: " + id + " and name: " + name);
                }
            }
            return element;
        }

        private AutomationElement GetListItemElement(AutomationElement list, string elementName)
        {
            try
            {
                var sp = list.GetCurrentPattern(ScrollPattern.Pattern) as ScrollPattern;
                sp.ScrollVertical(ScrollAmount.LargeIncrement);
            }
            catch
            {
                //throw new InvalidOperationException("No Vertical Scroll at selected List Item Element");
            }
            Condition isSelectionItemCondition = new PropertyCondition(AutomationElement.IsSelectionItemPatternAvailableProperty, true);
            Condition isContentCondition = new PropertyCondition(AutomationElement.IsContentElementProperty, true);
            var treeWalker = new TreeWalker(new AndCondition(isSelectionItemCondition, isContentCondition));
            var child = treeWalker.GetFirstChild(list);
            if (child == null)
            {
                throw new InvalidOperationException("List is empty");
            }
            do
            {
                var name = child.GetCurrentPropertyValue(AutomationElement.NameProperty, true).ToString();
                if (name.Contains(elementName))
                {
                    return child;
                }
                child = treeWalker.GetNextSibling(child);
            }
            while (child != null);
            throw new InvalidOperationException("No item found in list with name: " + elementName);
        }

        private InvokePattern GetInvokePattern(AutomationElement element)
        {
            return element.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
        }

        private SelectionItemPattern GetSelectionItemPattern(AutomationElement element)
        {
            return element.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
        }

        private void ClickWithEnableCheck(bool checkEnabled)
        {
            WaitForElementIsReady(_navigationElement.Current.AutomationId, null, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
            if (checkEnabled && !_navigationElement.Current.IsEnabled)
            {
                return;
            }
            var invokePattern = GetInvokePattern(_navigationElement);
            if (invokePattern == null)
            {
                throw new InvalidOperationException("Element with automation id: " + _navigationElement.Current.AutomationId + " can't be invoked");
            }
            else
            {
                try
                {
                    invokePattern.Invoke();
                }
                catch (InvalidOperationException ex)
                {
                    "{0}{1}"._EXLOG(_navigationElement.Current.AutomationId, ex.Message);
                    throw new InvalidOperationException(_navigationElement.Current.AutomationId + ": " + ex.Message, ex);
                }
            }
        }

        public IZatsAutomationDialog OpenDialog(string dialogId, string dialogName, Action createDialogAction, Action closeDialogAction, int timeout = 36000)/*Timeout.Infinite*/
        {
            $"{_navigationElement.Current.Name} - OpenDialog {dialogId}, {dialogName}"._DLOG();
            var dlg = new ZatsAutomationDialog();
            dlg.RunDialog(this, _navigationElement, dialogId, dialogName, createDialogAction, closeDialogAction, timeout);
            return dlg;
        }

        private class ZatsAutomationDialog : IZatsAutomationDialog
        {
            private WaitWindow _waitWindow;
            private Action _closeWindowAction;
            private IZatsAutomation _parent;
            private string _dialogId;
            private string _dialogName;

            IZatsAutomation _dialogView;
            public IZatsAutomation DialogView { get { return _dialogView; } }

            public bool RunDialog(IZatsAutomation automation, AutomationElement root, string dialogId, string name, Action openWindowAction, Action closeWindowAction, int timeout)
            {
                _parent = automation;
                _dialogId = dialogId;
                _dialogName = name;
                if (_parent.CheckVisible(_dialogId, _dialogName))
                {
                    return true;
                }
                _waitWindow = new WaitWindow(root, _dialogId, _dialogName, timeout, true);
                _closeWindowAction = closeWindowAction;
                if (_waitWindow.Run(openWindowAction))
                {
                    _dialogView = _dialogName != null ?
                        _parent.NavigateTo(_dialogId, _dialogName) :
                        _parent.NavigateTo(_dialogId);
                    return true;
                }
                return false;
            }

            public void Dispose()
            {
                if (_closeWindowAction != null)
                {
                    _closeWindowAction.Invoke();
                }
                else
                {
                    if (_parent.CheckVisible(_dialogId, _dialogName) && _dialogView != null)
                    {
                        _dialogView.CloseWindow();
                    }
                }
                if (_waitWindow != null)
                {
                    _waitWindow.Dispose();
                    _waitWindow = null;
                }
            }
        }

        /// <summary>
        /// Radio button click
        /// </summary>
        public void SelectRadioButtonItem()
        {
            var selectionPattern = _navigationElement.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
            if (selectionPattern == null)
            {
                throw new InvalidOperationException("Element with automation id: " + _navigationElement.Current.AutomationId + " can't be invoked");
            }
            else
            {
                try
                {
                    selectionPattern.Select();
                }
                catch (InvalidOperationException ex)
                {
                    "{0}{1}"._EXLOG(_navigationElement.Current.AutomationId, ex.Message);
                    throw new InvalidOperationException(_navigationElement.Current.AutomationId + ": " + ex.Message, ex);
                }
            }
        }

        public List<ushort> GetNodesFromAssociationsTree(Node deviceId)
        {
            List<ushort> nodeIds = new List<ushort>();

            PropertyCondition treeItemCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.AssociationsView.ItemAssociation);

            PropertyCondition rootDeviceIdCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.AssociationsView.TbRootDevice);

            var AssociationsTree = _navigationElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.AssociationsView.AssociationsInfoTvw));

            var PCControllerNodes = AssociationsTree.FindAll(TreeScope.Descendants, treeItemCondition);

            foreach (AutomationElement pccontrollerNode in PCControllerNodes)
            {

                TryExpandTreeNode(pccontrollerNode);

                AutomationElement RootDevice = null;

                var rootID = pccontrollerNode.FindFirst(TreeScope.Descendants, rootDeviceIdCondition);

                if (rootID != null && Convert.ToByte(rootID.Current.Name) == deviceId)
                {

                    WaitForElementNotVisible(PCControllerIdMap.AssociationsView.TbEndPoint, () =>
                    {
                        RootDevice = pccontrollerNode.FindFirst(TreeScope.Children, treeItemCondition);
                        SelectElement(RootDevice);
                        WaitForElementIsReady(PCControllerIdMap.AssociationsView.BtnGetGroups, null, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
                        NavigateTo(PCControllerIdMap.AssociationsView.BtnGetGroups).Click();
                    }, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);

                    TryExpandTreeNode(RootDevice);

                    var GroupNode = RootDevice.FindFirst(TreeScope.Children, treeItemCondition);

                    SelectElement(GroupNode);

                    var getNodesBtn = _navigationElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.AssociationsView.GetNodes));

                    InvokeElement(getNodesBtn);

                    WaitForElementIsReady(PCControllerIdMap.AssociationsView.ItemAssociation, null);

                    SelectElement(GroupNode);

                    TryExpandTreeNode(GroupNode);

                    var NodesProfileCommandClasses = GroupNode.FindAll(TreeScope.Children, treeItemCondition);

                    foreach (AutomationElement node in NodesProfileCommandClasses)
                    {

                        TryExpandTreeNode(node);

                        if (node.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ClassNameProperty, "TextBlock")).Current.Name.Contains("Nodes"))
                        {
                            var nodes = node.FindAll(TreeScope.Descendants, treeItemCondition);
                            nodeIds = new List<ushort>();
                            foreach (AutomationElement currentNode in nodes)
                            {
                                if (!string.IsNullOrEmpty(currentNode.Current.Name))
                                {
                                    nodeIds.Add(Convert.ToUInt16(currentNode.Current.Name));
                                }
                            }
                        }
                    }
                }
            }
            return nodeIds;
        }

        public void CreateNodeId(Node deviceId, byte groupId)
        {
            PropertyCondition treeItemCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.AssociationsView.ItemAssociation);

            PropertyCondition rootDeviceIdCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.AssociationsView.TbRootDevice);

            var treeWalkerTreeItem = new TreeWalker(treeItemCondition);

            var AssociationsTree = _navigationElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.AssociationsView.AssociationsInfoTvw));

            var PCControllerNodes = AssociationsTree.FindAll(TreeScope.Descendants, treeItemCondition);


            foreach (AutomationElement pccontrollerNode in PCControllerNodes)
            {

                TryExpandTreeNode(pccontrollerNode);

                var rootID = pccontrollerNode.FindFirst(TreeScope.Descendants, rootDeviceIdCondition);

                if (rootID != null && rootID.Current.Name.StartsWith(deviceId.Id.ToString()))
                {
                    var endPoint = treeWalkerTreeItem.GetFirstChild(pccontrollerNode);

                    while (endPoint != null)
                    {
                        string endPointName = endPoint.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ClassNameProperty, "TextBlock")).Current.Name;

                        if (endPointName.Contains(deviceId.EndPointId.ToString()))
                        {
                            SelectElement(endPoint);

                            var GroupBtn = _navigationElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.AssociationsView.BtnGetGroups));

                            WaitForElementNotVisible(PCControllerIdMap.AssociationsView.TbEndPoint, () => InvokeElement(GroupBtn), AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);

                            TryExpandTreeNode(endPoint);

                            var group = treeWalkerTreeItem.GetFirstChild(endPoint);

                            while (group != null)
                            {
                                var groupName = group.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ClassNameProperty, "TextBlock")).Current.Name;
                                if (groupName.Contains(groupId.ToString()))
                                {
                                    SelectElement(group);

                                    var createBtn = _navigationElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.AssociationsView.BtnCreate));

                                    InvokeElement(createBtn);

                                    return;
                                }
                                group = treeWalkerTreeItem.GetNextSibling(group);
                            }
                        }
                        endPoint = treeWalkerTreeItem.GetNextSibling(endPoint);
                    }
                }
            }
        }

        public void RemoveNodeId(Node deviceId, Node destNodeId, byte groupId)
        {
            PropertyCondition treeItemCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.AssociationsView.ItemAssociation);

            PropertyCondition rootDeviceIdCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.AssociationsView.TbRootDevice);

            var treeWalkerTreeItem = new TreeWalker(treeItemCondition);

            var AssociationsTree = _navigationElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.AssociationsView.AssociationsInfoTvw));

            var PCControllerNodes = AssociationsTree.FindAll(TreeScope.Descendants, treeItemCondition);

            foreach (AutomationElement pccontrollerNode in PCControllerNodes)
            {
                TryExpandTreeNode(pccontrollerNode);

                var rootID = pccontrollerNode.FindFirst(TreeScope.Descendants, rootDeviceIdCondition);

                if (rootID != null && rootID.Current.Name.StartsWith(deviceId.Id.ToString()))
                {
                    var endPoint = treeWalkerTreeItem.GetFirstChild(pccontrollerNode);

                    while (endPoint != null)
                    {
                        string endPointName = endPoint.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ClassNameProperty, "TextBlock")).Current.Name;

                        if (endPointName.Contains(deviceId.EndPointId.ToString()))
                        {
                            SelectElement(endPoint);

                            var GroupBtn = _navigationElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.AssociationsView.BtnGetGroups));

                            WaitForElementNotVisible(PCControllerIdMap.AssociationsView.TbEndPoint, () => InvokeElement(GroupBtn), AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);

                            TryExpandTreeNode(endPoint);

                            var group = treeWalkerTreeItem.GetFirstChild(endPoint);

                            while (group != null)
                            {
                                TryExpandTreeNode(group);

                                string groupName = group.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ClassNameProperty, "TextBlock")).Current.Name;

                                if (groupName.Contains(groupId.ToString()))
                                {
                                    var NodesProfileCommandClasses = group.FindFirst(TreeScope.Children, treeItemCondition);

                                    string nodeName = NodesProfileCommandClasses.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ClassNameProperty, "TextBlock")).Current.Name;

                                    if (nodeName.Contains("Nodes"))
                                    {
                                        TryExpandTreeNode(NodesProfileCommandClasses);

                                        var nodesProfilesCommandClasses = NodesProfileCommandClasses.FindAll(TreeScope.Descendants, treeItemCondition);

                                        foreach (AutomationElement nodes in nodesProfilesCommandClasses)
                                        {
                                            $"RemoveNodeId nodes.Current.Name = {nodes.Current.Name}"._DLOG();
                                            if (Convert.ToByte(nodes.Current.Name) == destNodeId)
                                            {
                                                SelectElement(nodes);
                                                var removeBtn = _navigationElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.AssociationsView.BtnRemove));
                                                InvokeElement(removeBtn);

                                                WaitForElementIsReady(PCControllerIdMap.AssociationsView.ItemAssociation, null);

                                                return;
                                            }
                                        }
                                    }
                                }
                                group = treeWalkerTreeItem.GetNextSibling(group);
                            }
                        }
                        endPoint = treeWalkerTreeItem.GetNextSibling(endPoint);
                    }
                }
            }
        }

        public BasicActionResult.AssociationGroupsResult GetAssociationModel(Node deviceId, byte endPointId)
        {
            BasicActionResult.AssociationGroupsResult res = new BasicActionResult.AssociationGroupsResult();

            WaitForElementIsVisible(PCControllerIdMap.AssociationsView.ID, Timeout.Infinite);
            var AssociationsTree = _navigationElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.AssociationsView.AssociationsInfoTvw));
            PropertyCondition treeItemCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.AssociationsView.ItemAssociation);
            var associativeNodes = AssociationsTree.FindAll(TreeScope.Descendants, treeItemCondition);
            PropertyCondition rootDeviceIdCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, PCControllerIdMap.AssociationsView.TbRootDevice);
            foreach (AutomationElement associativeNode in associativeNodes)
            {
                TryExpandTreeNode(associativeNode);
                WaitForElementIsReady(PCControllerIdMap.AssociationsView.TbEndPoint, null, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
                AutomationElementCollection EndPoints = null;

                var rootID = associativeNode.FindFirst(TreeScope.Descendants, rootDeviceIdCondition);
                if (rootID != null && rootID.Current.Name == deviceId.ToString())
                {
                    EndPoints = associativeNode.FindAll(TreeScope.Children, treeItemCondition);
                    foreach (AutomationElement endPoint in EndPoints)
                    {
                        string endPointName = endPoint.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ClassNameProperty, "TextBlock")).Current.Name;
                        if (endPointName.Contains(endPointId.ToString()))
                        {
                            SelectElement(endPoint);
                            NavigateTo(PCControllerIdMap.AssociationsView.BtnGetGroups).Click();
                            WaitForElementIsVisible(PCControllerIdMap.AssociationsView.TbEndPoint, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
                            WaitPropertyChanged(endPoint, AutomationElement.IsEnabledProperty, true);
                            TryExpandTreeNode(endPoint);

                            var associativeGroups = endPoint.FindAll(TreeScope.Children, treeItemCondition);
                            foreach (AutomationElement associativeGroupElement in associativeGroups)
                            {
                                WaitPropertyChanged(associativeGroupElement, AutomationElement.IsEnabledProperty, true);
                                TryExpandTreeNode(associativeGroupElement);
                                var groupItems = associativeGroupElement.FindAll(TreeScope.Children, treeItemCondition);
                                AssociationGroup associativeGroup = new AssociationGroup();
                                associativeGroup.Nodes = new List<ushort>();

                                string groupId = associativeGroupElement.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ClassNameProperty, "TextBlock")).Current.Name.Replace("Group ", "");
                                associativeGroup.GroupId = Convert.ToByte(groupId);
                                foreach (AutomationElement groupItem in groupItems)
                                {
                                    TryExpandTreeNode(groupItem);
                                    WaitForElementIsReady(PCControllerIdMap.AssociationsView.TbEndPoint, null, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);

                                    var cond = new PropertyCondition(AutomationElement.ClassNameProperty, "TextBlock");
                                    var condRes = groupItem.FindFirst(TreeScope.Descendants, cond);
                                    if (condRes != null)
                                    {
                                        if (condRes.Current.Name.Contains("Nodes"))
                                        {
                                            var nodes = groupItem.FindAll(TreeScope.Descendants, treeItemCondition);
                                            foreach (AutomationElement currentNode in nodes)
                                            {
                                                associativeGroup.Nodes.Add(Convert.ToUInt16(currentNode.Current.Name));
                                            }
                                        }
                                        else if (condRes.Current.Name.Contains("Profile"))
                                        {
                                            var Profiles = groupItem.FindAll(TreeScope.Descendants, treeItemCondition);
                                            associativeGroup.Profile = new List<string>();
                                            foreach (AutomationElement currentProfile in Profiles)
                                            {
                                                associativeGroup.Profile.Add(currentProfile.Current.Name);
                                            }
                                        }
                                        else if (condRes.Current.Name.Contains("Command"))
                                        {
                                            var commandClasses = groupItem.FindAll(TreeScope.Descendants, treeItemCondition);
                                            //associativeGroup.GroupCommandClasses = new List<string>();
                                            //foreach (AutomationElement currentCommandClasses in commandClasses)
                                            //{
                                            //    associativeGroup.GroupCommandClasses.Add(currentCommandClasses.Current.Name);
                                            //}
                                        }
                                    }

                                }

                                res.Groups.Add(associativeGroup);
                            }
                        }
                    }
                }
            }
            return res;
        }
        public void SelectElement(AutomationElement itemToSelect)
        {
            SelectionItemPattern SelectItemPattern = itemToSelect.GetCurrentPattern(SelectionItemPatternIdentifiers.Pattern) as SelectionItemPattern;
            SelectItemPattern.Select();
        }
        public void InvokeElement(AutomationElement itemToInvoke)
        {
            InvokePattern InvokeItemPattern = itemToInvoke.GetCurrentPattern(InvokePatternIdentifiers.Pattern) as InvokePattern;
            InvokeItemPattern.Invoke();
        }
        public void TryExpandTreeNode(AutomationElement itemToExpand)
        {
            try
            {
                ExpandCollapsePattern expandItemPattern = itemToExpand.GetCurrentPattern(ExpandCollapsePatternIdentifiers.Pattern) as ExpandCollapsePattern;
                expandItemPattern.Expand();
            }
            catch (Exception ex)
            {
                $"{ex.Message}"._DLOG();
            }
        }

        public void Select()
        {
            $"{_navigationElement.Current.Name} - Select"._DLOG();
            SelectionItemPattern SelectItemPattern = _navigationElement.GetCurrentPattern(SelectionItemPatternIdentifiers.Pattern) as SelectionItemPattern;
            SelectItemPattern.Select();
        }

        public void AddToSelection()
        {
            $"{_navigationElement.Current.Name} - AddToSelection"._DLOG();
            SelectionItemPattern SelectItemPattern = _navigationElement.GetCurrentPattern(SelectionItemPatternIdentifiers.Pattern) as SelectionItemPattern;
            SelectItemPattern.AddToSelection();
        }

        public void Expand()
        {
            try
            {
                ExpandCollapsePattern expandItemPattern = _navigationElement.GetCurrentPattern(ExpandCollapsePatternIdentifiers.Pattern) as ExpandCollapsePattern;
                expandItemPattern.Expand();
            }
            catch (Exception)
            {

            }
        }

        public IZatsAutomation TryNavigateTo(string elementId)
        {
            try
            {
                return new ZatsAutomation(GetElement(elementId));
            }
            catch (Exception)
            {
                // ignored
                return null;
            }
        }

        public IZatsAutomation TryNavigateToNameProperty(string name)
        {
            var condition = new PropertyCondition(AutomationElement.NameProperty, name, PropertyConditionFlags.IgnoreCase);
            return TryNavigateTo(condition);
        }

        public IZatsAutomation TryNavigateToClassNameProperty(string name)
        {
            var condition = new PropertyCondition(AutomationElement.ClassNameProperty, name, PropertyConditionFlags.IgnoreCase);
            return TryNavigateTo(condition);
        }

        public IZatsAutomation TryNavigateToIdProperty(string id)
        {
            var condition = new PropertyCondition(AutomationElement.AutomationIdProperty, id, PropertyConditionFlags.IgnoreCase);
            return TryNavigateTo(condition);
        }

        private IZatsAutomation TryNavigateTo(PropertyCondition condition)
        {
            try
            {
                return new ZatsAutomation(GetElement(condition));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private AutomationElement FindFirstRowWithinGrid(int columnNo)
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - FindFirstRowWithinGrid c={columnNo}"._DLOG();
            AutomationElement gridRowElement = null;
            EnumerateGridRowsByColumn(columnNo, gridCellElement =>
            {
                var itemText = GetElementText(gridCellElement);
                if (!string.IsNullOrEmpty(itemText))
                {
                    var treeWalkerSelectionItem = new TreeWalker(new PropertyCondition(AutomationElement.IsSelectionItemPatternAvailableProperty, true));
                    gridRowElement = treeWalkerSelectionItem.GetParent(gridCellElement);
                    return false;
                }
                return true;
            });
            if (gridRowElement == null)
            {
                throw new InvalidOperationException("No element found within grid within column: " + columnNo);
            }
            return gridRowElement;
        }

        public void SelectFirstRowWithinGrid(int column)
        {
            $"{_navigationElement.Current.AutomationId} : {_navigationElement.Current.Name} - SelectFirstRowWithinGrid c={column}"._DLOG();
            var item = FindFirstRowWithinGrid(column);
            var gridRow = item.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
            gridRow.Select();
        }
    }
}