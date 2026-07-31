using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Xv2CoreLib.BCM;

namespace XenoKit.Views.BCM
{
    public partial class BcmTreeList : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly List<BcmTreeRow> allRows = new List<BcmTreeRow>();
        private readonly Dictionary<string, BcmTreeRow> rowsByIndex = new Dictionary<string, BcmTreeRow>();
        private bool isSelecting;
        private BcmTreeRow selectionAnchor;

        public static readonly DependencyProperty EntriesProperty = DependencyProperty.Register(
            nameof(Entries), typeof(IList), typeof(BcmTreeList), new PropertyMetadata(null, EntriesPropertyChanged));

        public static readonly DependencyProperty SelectedEntryProperty = DependencyProperty.Register(
            nameof(SelectedEntry), typeof(BCM_Entry), typeof(BcmTreeList), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectedEntryPropertyChanged));

        public IList Entries
        {
            get => (IList)GetValue(EntriesProperty);
            set => SetValue(EntriesProperty, value);
        }

        public BCM_Entry SelectedEntry
        {
            get => (BCM_Entry)GetValue(SelectedEntryProperty);
            set => SetValue(SelectedEntryProperty, value);
        }

        public ObservableCollection<BcmTreeRow> RootRows { get; } = new ObservableCollection<BcmTreeRow>();
        public ObservableCollection<BCM_Entry> SelectedEntries { get; } = new ObservableCollection<BCM_Entry>();
        public event EventHandler SelectionChanged;

        public BcmTreeList()
        {
            InitializeComponent();
        }

        public void Refresh()
        {
            BuildRows();
        }

        public void RefreshDisplay()
        {
            foreach (BcmTreeRow row in allRows)
                row.RefreshDisplay();
        }

        public void SelectEntry(BCM_Entry entry)
        {
            SelectRow(allRows.FirstOrDefault(row => ReferenceEquals(row.Entry, entry)), true, true);
        }

        private static void EntriesPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((BcmTreeList)dependencyObject).BuildRows();
        }

        private static void SelectedEntryPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            BcmTreeList treeList = (BcmTreeList)dependencyObject;
            if (treeList.isSelecting) return;

            treeList.SelectRow(treeList.allRows.FirstOrDefault(row => ReferenceEquals(row.Entry, e.NewValue as BCM_Entry)), false, true);
        }

        private void BuildRows()
        {
            List<BCM_Entry> oldSelections = SelectedEntries.ToList();
            BCM_Entry oldSelection = SelectedEntry;
            allRows.Clear();
            rowsByIndex.Clear();
            RootRows.Clear();
            SelectedEntries.Clear();

            foreach (BCM_Entry entry in Entries?.OfType<BCM_Entry>() ?? Enumerable.Empty<BCM_Entry>())
                RootRows.Add(AddRows(entry, null, 0));

            foreach (BcmTreeRow row in allRows.Where(row => oldSelections.Contains(row.Entry)))
                row.IsSelected = true;

            SyncSelectedEntries();
            SelectRow(allRows.FirstOrDefault(row => ReferenceEquals(row.Entry, oldSelection)) ?? allRows.FirstOrDefault(row => row.IsSelected), true, false);
        }

        private BcmTreeRow AddRows(BCM_Entry entry, BcmTreeRow parent, int depth)
        {
            BcmTreeRow row = new BcmTreeRow(entry, parent, depth, GetLinkText);
            allRows.Add(row);
            if (!string.IsNullOrWhiteSpace(entry?.Index))
                rowsByIndex[entry.Index] = row;

            foreach (BCM_Entry child in entry.BCMEntries ?? Enumerable.Empty<BCM_Entry>())
                row.Children.Add(AddRows(child, row, depth + 1));

            return row;
        }

        private string GetLinkText(string linkType, string index)
        {
            if (string.IsNullOrWhiteSpace(index)) return null;

            rowsByIndex.TryGetValue(index, out BcmTreeRow row);
            if (row == null) return $"{linkType} {index} | Missing";

            return $"{linkType} {index} | BAC {row.Entry.BacEntryPrimary}";
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (isSelecting) return;

            SelectRow(e.NewValue as BcmTreeRow, true, true);
        }

        private void RowHeader_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!((sender as FrameworkElement)?.DataContext is BcmTreeRow row)) return;

            ModifierKeys modifiers = Keyboard.Modifiers;
            if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                SelectRange(row);
            }
            else if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                ToggleRow(row);
            }
            else
            {
                SelectRow(row, true, true);
            }

            FocusTreeItem(row);
            e.Handled = true;
        }

        private void RowHeader_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!((sender as FrameworkElement)?.DataContext is BcmTreeRow row)) return;

            if (!row.IsSelected)
                SelectRow(row, true, true);

            FocusTreeItem(row);

            e.Handled = true;
        }

        private void ChildLink_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is BcmTreeRow row)
                SelectLinkedRow(row.Entry?.LoopAsChild);

            e.Handled = true;
        }

        private void SiblingLink_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is BcmTreeRow row)
                SelectLinkedRow(row.Entry?.LoopAsSibling);

            e.Handled = true;
        }

        private void SelectLinkedRow(string index)
        {
            if (string.IsNullOrWhiteSpace(index)) return;

            rowsByIndex.TryGetValue(index, out BcmTreeRow row);
            if (row == null) return;

            BcmTreeRow parent = row.Parent;
            while (parent != null)
            {
                parent.IsExpanded = true;
                parent = parent.Parent;
            }

            SelectRow(row, true, true);
        }

        private void SelectRow(BcmTreeRow row, bool updateEntry, bool replaceSelection)
        {
            isSelecting = true;
            if (replaceSelection)
            {
                foreach (BcmTreeRow existingRow in allRows)
                    existingRow.IsSelected = ReferenceEquals(existingRow, row);
            }

            if (updateEntry)
                SelectedEntry = row?.Entry;
            isSelecting = false;
            selectionAnchor = row ?? selectionAnchor;
            SyncSelectedEntries();

            if (row != null)
                treeView.Dispatcher.BeginInvoke(new Action(() => SelectTreeItem(treeView, row)));
        }

        private void ToggleRow(BcmTreeRow row)
        {
            row.IsSelected = !row.IsSelected;
            if (row.IsSelected)
            {
                selectionAnchor = row;
                SelectRow(row, true, false);
                return;
            }

            SyncSelectedEntries();
            if (ReferenceEquals(SelectedEntry, row.Entry))
                SelectRow(allRows.FirstOrDefault(existingRow => existingRow.IsSelected), true, false);
        }

        private void SelectRange(BcmTreeRow row)
        {
            List<BcmTreeRow> visibleRows = GetVisibleRows().ToList();
            BcmTreeRow anchor = selectionAnchor ?? row;
            int anchorIndex = visibleRows.IndexOf(anchor);
            int rowIndex = visibleRows.IndexOf(row);

            if (anchorIndex < 0 || rowIndex < 0)
            {
                SelectRow(row, true, true);
                return;
            }

            int start = Math.Min(anchorIndex, rowIndex);
            int end = Math.Max(anchorIndex, rowIndex);
            foreach (BcmTreeRow existingRow in allRows)
                existingRow.IsSelected = false;

            for (int index = start; index <= end; index++)
                visibleRows[index].IsSelected = true;

            SelectRow(row, true, false);
        }

        private IEnumerable<BcmTreeRow> GetVisibleRows()
        {
            foreach (BcmTreeRow row in RootRows)
            {
                yield return row;

                if (!row.IsExpanded) continue;
                foreach (BcmTreeRow child in GetVisibleRows(row))
                    yield return child;
            }
        }

        private IEnumerable<BcmTreeRow> GetVisibleRows(BcmTreeRow row)
        {
            foreach (BcmTreeRow child in row.Children)
            {
                yield return child;

                if (!child.IsExpanded) continue;
                foreach (BcmTreeRow grandChild in GetVisibleRows(child))
                    yield return grandChild;
            }
        }

        private void SyncSelectedEntries()
        {
            SelectedEntries.Clear();
            foreach (BcmTreeRow row in allRows.Where(row => row.IsSelected))
                SelectedEntries.Add(row.Entry);

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void FocusTreeItem(BcmTreeRow row)
        {
            TreeViewItem treeViewItem = FindTreeViewItem(treeView, row);
            if (treeViewItem == null) return;

            treeViewItem.IsSelected = true;
            treeViewItem.Focus();
        }

        private bool SelectTreeItem(ItemsControl parent, BcmTreeRow target)
        {
            parent.UpdateLayout();

            foreach (object item in parent.Items)
            {
                TreeViewItem treeViewItem = parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (treeViewItem == null) continue;

                if (ReferenceEquals(item, target))
                {
                    treeViewItem.IsSelected = true;
                    treeViewItem.BringIntoView();
                    treeViewItem.Focus();
                    return true;
                }

                BcmTreeRow row = item as BcmTreeRow;
                if (row != null && ContainsRow(row, target))
                {
                    treeViewItem.IsExpanded = true;
                    if (SelectTreeItem(treeViewItem, target)) return true;
                }
            }

            return false;
        }

        private TreeViewItem FindTreeViewItem(ItemsControl parent, BcmTreeRow target)
        {
            parent.UpdateLayout();

            foreach (object item in parent.Items)
            {
                TreeViewItem treeViewItem = parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (treeViewItem == null) continue;

                if (ReferenceEquals(item, target))
                    return treeViewItem;

                BcmTreeRow row = item as BcmTreeRow;
                if (row != null && ContainsRow(row, target))
                {
                    treeViewItem.IsExpanded = true;
                    TreeViewItem result = FindTreeViewItem(treeViewItem, target);
                    if (result != null) return result;
                }
            }

            return null;
        }

        private static bool ContainsRow(BcmTreeRow row, BcmTreeRow target)
        {
            foreach (BcmTreeRow child in row.Children)
            {
                if (ReferenceEquals(child, target)) return true;
                if (ContainsRow(child, target)) return true;
            }

            return false;
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class BcmTreeRow : INotifyPropertyChanged
    {
        private readonly Func<string, string, string> getLinkText;
        private bool isExpanded;
        private bool isSelected;

        public event PropertyChangedEventHandler PropertyChanged;

        public BCM_Entry Entry { get; }
        public BcmTreeRow Parent { get; }
        public ObservableCollection<BcmTreeRow> Children { get; } = new ObservableCollection<BcmTreeRow>();
        public int Depth { get; }

        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (isExpanded == value) return;
                isExpanded = value;
                NotifyPropertyChanged(nameof(IsExpanded));
            }
        }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected == value) return;
                isSelected = value;
                NotifyPropertyChanged(nameof(IsSelected));
            }
        }

        public string IndexText => Entry?.Index ?? string.Empty;
        public string BacText => $"BAC {Entry?.BacEntryPrimary ?? 0}";
        public string ChildLinkText => getLinkText("Child", Entry?.LoopAsChild);
        public string SiblingLinkText => getLinkText("Sibling", Entry?.LoopAsSibling);
        public Visibility ChildLinkVisibility => string.IsNullOrWhiteSpace(ChildLinkText) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility SiblingLinkVisibility => string.IsNullOrWhiteSpace(SiblingLinkText) ? Visibility.Collapsed : Visibility.Visible;

        public BcmTreeRow(BCM_Entry entry, BcmTreeRow parent, int depth, Func<string, string, string> getLinkText)
        {
            Entry = entry;
            Parent = parent;
            Depth = depth;
            this.getLinkText = getLinkText;
            isExpanded = depth == 0;
        }

        public void RefreshDisplay()
        {
            NotifyPropertyChanged(nameof(IndexText));
            NotifyPropertyChanged(nameof(BacText));
            NotifyPropertyChanged(nameof(ChildLinkText));
            NotifyPropertyChanged(nameof(SiblingLinkText));
            NotifyPropertyChanged(nameof(ChildLinkVisibility));
            NotifyPropertyChanged(nameof(SiblingLinkVisibility));
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
