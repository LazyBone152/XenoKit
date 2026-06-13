using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using XenoKit.Editor;
using XenoKit.Editor.Undo;
using Xv2CoreLib;
using Xv2CoreLib.BCM;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Views
{
    public partial class BcmTab : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public Files files => Files.Instance;

        private BCM_Entry selectedEntry;

        public IList<Xv2File<BCM_File>> BcmFiles
        {
            get
            {
                List<Xv2File<BCM_File>> bcmFiles = new List<Xv2File<BCM_File>>();
                if (files.SelectedMove?.Files?.BcmFile != null) bcmFiles.Add(files.SelectedMove.Files.BcmFile);
                if (files.SelectedMove?.Files?.AfterBcmFile != null) bcmFiles.Add(files.SelectedMove.Files.AfterBcmFile);
                return bcmFiles;
            }
        }

        public IList Entries => files.SelectedItem?.SelectedBcmFile?.File?.BCMEntries;
        public Visibility BcmFileSelectorVisibility => BcmFiles.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility BcmFileTextVisibility => BcmFiles.Count <= 1 ? Visibility.Visible : Visibility.Collapsed;
        public string SelectedBcmFileName => files.SelectedItem?.SelectedBcmFile?.DisplayName ?? string.Empty;
        // Root entries anchor the state tree, so they can be selected for navigation but not edited as states.
        public BCM_Entry EditableSelectedEntry => CanEditSelectedEntry() ? SelectedEntry : null;
        public bool HasMultipleSelectedEntries => GetSelectedEntries().Count > 1;
        public bool IsSelectedEntryRoot => IsRootEntry(SelectedEntry);
        public string SelectedEntrySiblingIndex => GetNextSiblingIndex(SelectedEntry);
        public string SelectedEntryChildIndex => GetFirstChildIndex(SelectedEntry);

        public BCM_Entry SelectedEntry
        {
            get => selectedEntry;
            set
            {
                selectedEntry = value;
                NotifyPropertyChanged(nameof(SelectedEntry));
                NotifyPropertyChanged(nameof(EditableSelectedEntry));
                NotifyPropertyChanged(nameof(HasMultipleSelectedEntries));
                NotifyPropertyChanged(nameof(IsSelectedEntryRoot));
                NotifyPropertyChanged(nameof(SelectedEntrySiblingIndex));
                NotifyPropertyChanged(nameof(SelectedEntryChildIndex));
            }
        }

        public BcmTab()
        {
            InitializeComponent();
            DataContext = this;
            entryTree.ContextMenu.DataContext = this;
            entryTree.SelectionChanged += EntryTree_SelectionChanged;
            InputBindings.Add(new KeyBinding(CopyCommand, new KeyGesture(Key.C, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(PasteCommand, new KeyGesture(Key.V, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(CutCommand, new KeyGesture(Key.X, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(DeleteCommand, new KeyGesture(Key.Delete)));
            files.PropertyChanged += Files_PropertyChanged;
            UndoManager.Instance.UndoOrRedoCalled += UndoManager_UndoOrRedoCalled;
            Unloaded += BcmTab_Unloaded;
        }

        private void BcmTab_Unloaded(object sender, RoutedEventArgs e)
        {
            UndoManager.Instance.UndoOrRedoCalled -= UndoManager_UndoOrRedoCalled;
        }

        private void UndoManager_UndoOrRedoCalled(object sender, UndoEventRaisedEventArgs e)
        {
            RefreshAfterUndoRedo();
        }

        private void RefreshAfterUndoRedo()
        {
            BCM_File file = files.SelectedItem?.SelectedBcmFile?.File;
            BCM_Entry previousEntry = SelectedEntry;

            if (file == null)
            {
                SelectEntry(null);
                NotifyAll();
                return;
            }

            RefreshTree();

            if (ContainsEntry(file, previousEntry))
                SelectEntry(previousEntry);
            else
                SelectEntry(GetFirstEntry(file));

            entryTree?.RefreshDisplay();
            NotifyPropertyChanged(nameof(EditableSelectedEntry));
            NotifyPropertyChanged(nameof(HasMultipleSelectedEntries));
            NotifyPropertyChanged(nameof(IsSelectedEntryRoot));
            NotifyPropertyChanged(nameof(SelectedEntrySiblingIndex));
            NotifyPropertyChanged(nameof(SelectedEntryChildIndex));
        }

        private void EntryTree_SelectionChanged(object sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(EditableSelectedEntry));
            NotifyPropertyChanged(nameof(HasMultipleSelectedEntries));
            NotifyPropertyChanged(nameof(IsSelectedEntryRoot));
            NotifyPropertyChanged(nameof(SelectedEntrySiblingIndex));
            NotifyPropertyChanged(nameof(SelectedEntryChildIndex));
        }

        private void Files_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Files.SelectedItem) || e.PropertyName == nameof(Files.SelectedMove))
            {
                SelectedEntry = null;
                NotifyAll();
                RefreshTree();
            }
        }

        private void BcmFileSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            SelectedEntry = null;
            NotifyPropertyChanged(nameof(SelectedBcmFileName));
            RefreshTree();
        }

        private void EntryEditor_EntryEdited(object sender, EventArgs e)
        {
            entryTree?.RefreshDisplay();
        }

        private void ToolsButton_Click(object sender, RoutedEventArgs e)
        {
            if (ToolsButton.ContextMenu == null) return;

            ToolsButton.ContextMenu.PlacementTarget = ToolsButton;
            ToolsButton.ContextMenu.IsOpen = true;
        }

        public RelayCommand AddRootCommand => new RelayCommand(AddRoot);
        private void AddRoot()
        {
            BCM_File file = files.SelectedItem?.SelectedBcmFile?.File;
            if (file == null) return;

            BCM_Entry entry = CreateEntry(file);
            file.BCMEntries.Add(entry);
            UndoManager.Instance.AddUndo(new UndoableListAdd<BCM_Entry>(file.BCMEntries, entry, "BCM Entry Add"));
            SelectedEntry = entry;
            RefreshTree();
        }

        public RelayCommand AddChildCommand => new RelayCommand(AddChild, () => SelectedEntry != null);
        private void AddChild()
        {
            BCM_File file = files.SelectedItem?.SelectedBcmFile?.File;
            if (file == null || SelectedEntry == null) return;

            if (SelectedEntry.BCMEntries == null) SelectedEntry.BCMEntries = new List<BCM_Entry>();
            BCM_Entry entry = CreateEntry(file);
            SelectedEntry.BCMEntries.Add(entry);
            UndoManager.Instance.AddUndo(new UndoableListAdd<BCM_Entry>(SelectedEntry.BCMEntries, entry, "BCM Child Add"));
            SelectedEntry = entry;
            RefreshTree();
        }

        public RelayCommand AddSiblingCommand => new RelayCommand(AddSibling, CanEditSelectedEntry);
        private void AddSibling()
        {
            BCM_File file = files.SelectedItem?.SelectedBcmFile?.File;
            IList<BCM_Entry> list = FindOwnerList(file?.BCMEntries, SelectedEntry);
            if (file == null || list == null) return;

            BCM_Entry entry = CreateEntry(file);
            int index = list.IndexOf(SelectedEntry) + 1;
            list.Insert(index, entry);
            UndoManager.Instance.AddUndo(new UndoableListAdd<BCM_Entry>(list, entry, "BCM Sibling Add"));
            SelectedEntry = entry;
            RefreshTree();
        }

        public RelayCommand DuplicateCommand => new RelayCommand(DuplicateEntry, CanEditSelectedEntry);
        private void DuplicateEntry()
        {
            BCM_File file = files.SelectedItem?.SelectedBcmFile?.File;
            IList<BCM_Entry> list = FindOwnerList(file?.BCMEntries, SelectedEntry);
            if (file == null || list == null) return;

            BCM_Entry clone = SelectedEntry.Clone();
            AssignNewIndexes(file, clone);
            int index = list.IndexOf(SelectedEntry) + 1;
            list.Insert(index, clone);
            UndoManager.Instance.AddUndo(new UndoableListAdd<BCM_Entry>(list, clone, "BCM Duplicate"));
            SelectedEntry = clone;
            RefreshTree();
        }

        public RelayCommand CopyCommand => new RelayCommand(CopyEntry, HasSelectedEntries);
        private void CopyEntry()
        {
            List<BCM_Entry> entries = GetNormalizedSelectedEntries();
            if (entries.Count == 0) return;

            DataObject data = new DataObject();
            data.SetData(ClipboardConstants.BcmSubtrees_CopyItems, entries.Select(entry => entry.Clone()).ToList());
            Clipboard.SetDataObject(data);
        }

        public RelayCommand CutCommand => new RelayCommand(CutEntry, HasSelectedEntries);
        private void CutEntry()
        {
            CopyEntry();
            DeleteEntry();
        }

        public RelayCommand PasteCommand => PasteAsSiblingCommand;
        public RelayCommand PasteAsSiblingCommand => new RelayCommand(PasteAsSibling, CanPasteAsSibling);
        public RelayCommand PasteAsChildCommand => new RelayCommand(PasteAsChild, CanPasteAsChild);

        private void PasteAsSibling()
        {
            BCM_File file = files.SelectedItem?.SelectedBcmFile?.File;
            List<BCM_Entry> clones = GetClipboardEntries(file);
            if (file == null || clones.Count == 0) return;

            IList<BCM_Entry> list = FindOwnerList(file.BCMEntries, SelectedEntry);
            if (list == null) list = file.BCMEntries;

            int index = SelectedEntry != null ? list.IndexOf(SelectedEntry) + 1 : list.Count;
            List<IUndoRedo> undos = new List<IUndoRedo>();
            foreach (BCM_Entry clone in clones)
            {
                list.Insert(index++, clone);
                undos.Add(new UndoableListAdd<BCM_Entry>(list, clone, "BCM Paste"));
            }

            UndoManager.Instance.AddCompositeUndo(undos, "BCM Paste");
            SelectedEntry = clones.LastOrDefault();
            RefreshTree();
        }

        private void PasteAsChild()
        {
            BCM_File file = files.SelectedItem?.SelectedBcmFile?.File;
            List<BCM_Entry> clones = GetClipboardEntries(file);
            if (file == null || clones.Count == 0) return;

            IList<BCM_Entry> list = SelectedEntry != null ? SelectedEntry.BCMEntries : file.BCMEntries;
            if (SelectedEntry != null && SelectedEntry.BCMEntries == null)
            {
                SelectedEntry.BCMEntries = new List<BCM_Entry>();
                list = SelectedEntry.BCMEntries;
            }

            List<IUndoRedo> undos = new List<IUndoRedo>();
            foreach (BCM_Entry clone in clones)
            {
                list.Add(clone);
                undos.Add(new UndoableListAdd<BCM_Entry>(list, clone, "BCM Paste"));
            }

            UndoManager.Instance.AddCompositeUndo(undos, "BCM Paste");
            SelectedEntry = clones.LastOrDefault();
            RefreshTree();
        }

        private bool CanPasteAsSibling()
        {
            return files.SelectedItem?.SelectedBcmFile?.File != null && Clipboard.ContainsData(ClipboardConstants.BcmSubtrees_CopyItems);
        }

        private bool CanPasteAsChild()
        {
            return files.SelectedItem?.SelectedBcmFile?.File != null && Clipboard.ContainsData(ClipboardConstants.BcmSubtrees_CopyItems);
        }

        private List<BCM_Entry> GetClipboardEntries(BCM_File file)
        {
            if (file == null || !Clipboard.ContainsData(ClipboardConstants.BcmSubtrees_CopyItems)) return new List<BCM_Entry>();

            List<BCM_Entry> clones = ((List<BCM_Entry>)Clipboard.GetData(ClipboardConstants.BcmSubtrees_CopyItems))?
                .Select(entry => entry.Clone())
                .ToList() ?? new List<BCM_Entry>();

            foreach (BCM_Entry clone in clones)
            {
                if (HasIndexCollision(file, clone) || clones.Where(entry => !ReferenceEquals(entry, clone)).SelectMany(entry => Flatten(new[] { entry })).Any(entry => entry.Index == clone.Index))
                    AssignNewIndexes(file, clone);
            }

            return clones;
        }

        public RelayCommand DeleteCommand => new RelayCommand(DeleteEntry, HasSelectedEntries);
        private async void DeleteEntry()
        {
            BCM_File file = files.SelectedItem?.SelectedBcmFile?.File;
            if (file?.BCMEntries == null) return;

            List<BCM_Entry> entries = GetNormalizedSelectedEntries();
            if (entries.Count == 0) return;

            if (entries.Any(entry => entry.BCMEntries?.Count > 0))
            {
                string message = entries.Count == 1
                    ? "Are you sure you want to delete this entry? This will delete ALL children of this entry"
                    : "Are you sure you want to delete these entries? This will delete ALL children of these entries";
                MessageDialogResult result = await DialogCoordinator.Instance.ShowMessageAsync(this, "Delete BCM Entry", message, MessageDialogStyle.AffirmativeAndNegative, DialogSettings.DefaultYesNo);
                if (result != MessageDialogResult.Affirmative)
                    return;
            }

            List<DeleteEntryInfo> deleteInfos = entries
                .Select(entry => new DeleteEntryInfo(entry, FindOwnerList(file.BCMEntries, entry)))
                .Where(info => info.OwnerList != null)
                .OrderByDescending(info => info.OwnerList.IndexOf(info.Entry))
                .ToList();

            List<IUndoRedo> undos = new List<IUndoRedo>();
            foreach (DeleteEntryInfo info in deleteInfos)
            {
                int index = info.OwnerList.IndexOf(info.Entry);
                undos.Add(new UndoableListRemove<BCM_Entry>(info.OwnerList, info.Entry, index, "BCM Delete"));
                info.OwnerList.Remove(info.Entry);
            }

            if (undos.Count > 0)
                UndoManager.Instance.AddCompositeUndo(undos, entries.Count > 1 ? "BCM Entries Delete" : "BCM Entry Delete");

            SelectedEntry = null;
            RefreshTree();
        }

        public RelayCommand MoveUpCommand => new RelayCommand(() => Move(-1), () => CanMove(-1));
        public RelayCommand MoveDownCommand => new RelayCommand(() => Move(1), () => CanMove(1));

        private bool CanMove(int direction)
        {
            IList<BCM_Entry> list = FindOwnerList(files.SelectedItem?.SelectedBcmFile?.File?.BCMEntries, SelectedEntry);
            if (list == null || IsRootEntry(SelectedEntry)) return false;

            int index = list.IndexOf(SelectedEntry);
            int newIndex = index + direction;
            return index >= 0 && newIndex >= 0 && newIndex < list.Count;
        }

        private void Move(int direction)
        {
            IList<BCM_Entry> list = FindOwnerList(files.SelectedItem?.SelectedBcmFile?.File?.BCMEntries, SelectedEntry);
            if (list == null) return;

            int oldIndex = list.IndexOf(SelectedEntry);
            int newIndex = oldIndex + direction;
            UndoManager.Instance.AddUndo(new ListMoveUndo<BCM_Entry>(list, oldIndex, newIndex, "BCM Move"));
            RefreshTree();
        }

        public void SelectEntry(BCM_Entry entry)
        {
            SelectedEntry = entry;
            entryTree?.SelectEntry(entry);
        }

        public void RefreshAfterFindReplace(string propertyName)
        {
            if (propertyName == nameof(BCM_Entry.Index))
            {
                RefreshTree();
                return;
            }

            NotifyPropertyChanged(nameof(SelectedEntrySiblingIndex));
            NotifyPropertyChanged(nameof(SelectedEntryChildIndex));
            entryTree?.RefreshDisplay();
        }

        public RelayCommand ReindexCommand => new RelayCommand(Reindex);
        private async void Reindex()
        {
            BCM_File file = files.SelectedItem?.SelectedBcmFile?.File;
            if (file == null) return;

            MessageDialogResult result = await DialogCoordinator.Instance.ShowMessageAsync(this, "Reindex BCM", "Reindex all BCM states? This changes state IDs and updates loop links.", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.DefaultYesNo);
            if (result != MessageDialogResult.Affirmative)
                return;

            List<ReindexSnapshot> oldValues = Flatten(file.BCMEntries)
                .Select(entry => new ReindexSnapshot(entry, entry.Index, entry.LoopAsChild, entry.LoopAsSibling))
                .ToList();

            ReindexFile(file);

            List<IUndoRedo> undos = new List<IUndoRedo>();
            foreach (ReindexSnapshot oldValue in oldValues)
            {
                AddChangedPropertyUndo(undos, oldValue.Entry, nameof(BCM_Entry.Index), oldValue.Index, oldValue.Entry.Index);
                AddChangedPropertyUndo(undos, oldValue.Entry, nameof(BCM_Entry.LoopAsChild), oldValue.LoopAsChild, oldValue.Entry.LoopAsChild);
                AddChangedPropertyUndo(undos, oldValue.Entry, nameof(BCM_Entry.LoopAsSibling), oldValue.LoopAsSibling, oldValue.Entry.LoopAsSibling);
            }

            if (undos.Count > 0)
                UndoManager.Instance.AddCompositeUndo(undos, "BCM Reindex");

            RefreshTree();
        }

        public RelayCommand CompressCommand => new RelayCommand(Compress, () => false);
        private async void Compress()
        {
            await DialogCoordinator.Instance.ShowMessageAsync(this, "BCM Compression", "BCM compression is not available in the current Xv2CoreLib API.", MessageDialogStyle.Affirmative, DialogSettings.Default);
        }

        private void ReindexFile(BCM_File file)
        {
            List<BCM_Entry> entries = Flatten(file.BCMEntries).ToList();
            Dictionary<string, string> idMap = entries
                .Select((entry, index) => new { OldId = entry.Index, NewId = index.ToString() })
                .Where(item => !string.IsNullOrWhiteSpace(item.OldId))
                .GroupBy(item => item.OldId)
                .ToDictionary(group => group.Key, group => group.First().NewId);

            for (int index = 0; index < entries.Count; index++)
            {
                BCM_Entry entry = entries[index];
                entry.Index = index.ToString();

                if (!string.IsNullOrWhiteSpace(entry.LoopAsChild) && idMap.TryGetValue(entry.LoopAsChild, out string childId))
                    entry.LoopAsChild = childId;

                if (!string.IsNullOrWhiteSpace(entry.LoopAsSibling) && idMap.TryGetValue(entry.LoopAsSibling, out string siblingId))
                    entry.LoopAsSibling = siblingId;
            }
        }

        private BCM_Entry CreateEntry(BCM_File file)
        {
            return new BCM_Entry
            {
                Index = GetNextIndex(file).ToString()
            };
        }

        private int GetNextIndex(BCM_File file)
        {
            HashSet<int> used = new HashSet<int>();
            foreach (BCM_Entry entry in Flatten(file.BCMEntries))
            {
                if (int.TryParse(entry.Index, out int index))
                    used.Add(index);
            }

            int next = 0;
            while (used.Contains(next)) next++;
            return next;
        }

        private void AssignNewIndexes(BCM_File file, BCM_Entry entry)
        {
            entry.Index = GetNextIndex(file).ToString();
            if (entry.BCMEntries == null) return;

            foreach (BCM_Entry child in entry.BCMEntries)
                AssignNewIndexes(file, child);
        }

        private bool HasIndexCollision(BCM_File file, BCM_Entry entry)
        {
            HashSet<string> usedIndexes = new HashSet<string>(Flatten(file.BCMEntries).Select(x => x.Index));
            return Flatten(new[] { entry }).Any(x => usedIndexes.Contains(x.Index));
        }

        private IEnumerable<BCM_Entry> Flatten(IEnumerable<BCM_Entry> entries)
        {
            if (entries == null) yield break;

            foreach (BCM_Entry entry in entries)
            {
                yield return entry;
                foreach (BCM_Entry child in Flatten(entry.BCMEntries))
                    yield return child;
            }
        }

        private IList<BCM_Entry> FindOwnerList(IList<BCM_Entry> entries, BCM_Entry entry)
        {
            if (entries == null || entry == null) return null;
            if (entries.Contains(entry)) return entries;

            foreach (BCM_Entry child in entries)
            {
                IList<BCM_Entry> result = FindOwnerList(child.BCMEntries, entry);
                if (result != null) return result;
            }

            return null;
        }

        private void RefreshTree()
        {
            NotifyPropertyChanged(nameof(Entries));
            NotifyPropertyChanged(nameof(IsSelectedEntryRoot));
            NotifyPropertyChanged(nameof(SelectedEntrySiblingIndex));
            NotifyPropertyChanged(nameof(SelectedEntryChildIndex));
            entryTree?.Refresh();
        }

        private bool ContainsEntry(BCM_File file, BCM_Entry entry)
        {
            return file != null && entry != null && Flatten(file.BCMEntries).Contains(entry);
        }

        private BCM_Entry GetFirstEntry(BCM_File file)
        {
            return file?.BCMEntries?.FirstOrDefault();
        }

        private void NotifyAll()
        {
            NotifyPropertyChanged(nameof(BcmFiles));
            NotifyPropertyChanged(nameof(BcmFileSelectorVisibility));
            NotifyPropertyChanged(nameof(BcmFileTextVisibility));
            NotifyPropertyChanged(nameof(SelectedBcmFileName));
            NotifyPropertyChanged(nameof(Entries));
            NotifyPropertyChanged(nameof(EditableSelectedEntry));
            NotifyPropertyChanged(nameof(HasMultipleSelectedEntries));
            NotifyPropertyChanged(nameof(IsSelectedEntryRoot));
            NotifyPropertyChanged(nameof(SelectedEntrySiblingIndex));
            NotifyPropertyChanged(nameof(SelectedEntryChildIndex));
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static void AddChangedPropertyUndo<T>(ICollection<IUndoRedo> undos, BCM_Entry entry, string propertyName, T oldValue, T newValue)
        {
            if (Equals(oldValue, newValue))
                return;

            undos.Add(new UndoablePropertyGeneric(propertyName, entry, oldValue, newValue, propertyName));
        }

        private bool HasSelectedEntries()
        {
            return GetSelectedEntries().Count > 0;
        }

        private List<BCM_Entry> GetSelectedEntries()
        {
            List<BCM_Entry> selectedEntries = entryTree?.SelectedEntries?.Where(entry => entry != null).ToList() ?? new List<BCM_Entry>();
            if (selectedEntries.Count == 0 && SelectedEntry != null)
                selectedEntries.Add(SelectedEntry);

            return selectedEntries;
        }

        private List<BCM_Entry> GetNormalizedSelectedEntries()
        {
            List<BCM_Entry> selectedEntries = GetSelectedEntries();
            return selectedEntries
                .Where(entry => !selectedEntries.Any(parent => !ReferenceEquals(parent, entry) && IsDescendantOf(entry, parent)))
                .ToList();
        }

        private bool IsDescendantOf(BCM_Entry entry, BCM_Entry possibleParent)
        {
            if (possibleParent?.BCMEntries == null) return false;

            foreach (BCM_Entry child in possibleParent.BCMEntries)
            {
                if (ReferenceEquals(child, entry)) return true;
                if (IsDescendantOf(entry, child)) return true;
            }

            return false;
        }

        private bool CanEditSelectedEntry()
        {
            return GetSelectedEntries().Count <= 1 && SelectedEntry != null && !IsRootEntry(SelectedEntry);
        }

        private bool IsRootEntry(BCM_Entry entry)
        {
            BCM_File file = files.SelectedItem?.SelectedBcmFile?.File;
            return file?.BCMEntries != null && entry != null && file.BCMEntries.Contains(entry);
        }

        private string GetFirstChildIndex(BCM_Entry entry)
        {
            return entry?.BCMEntries?.FirstOrDefault()?.Index ?? "0";
        }

        private string GetNextSiblingIndex(BCM_Entry entry)
        {
            BCM_File file = files.SelectedItem?.SelectedBcmFile?.File;
            IList<BCM_Entry> list = FindOwnerList(file?.BCMEntries, entry);
            if (list == null) return "0";

            int index = list.IndexOf(entry);
            if (index < 0 || index + 1 >= list.Count) return "0";

            return list[index + 1]?.Index ?? "0";
        }

        private BCM_Entry GetParentEntry(BCM_Entry entry)
        {
            BCM_File file = files.SelectedItem?.SelectedBcmFile?.File;
            if (file?.BCMEntries == null || entry == null) return null;

            foreach (BCM_Entry rootEntry in file.BCMEntries)
            {
                BCM_Entry parent = FindParent(rootEntry, entry);
                if (parent != null) return parent;
            }

            return null;
        }

        private BCM_Entry FindParent(BCM_Entry parent, BCM_Entry entry)
        {
            if (parent?.BCMEntries == null) return null;

            foreach (BCM_Entry child in parent.BCMEntries)
            {
                if (ReferenceEquals(child, entry)) return parent;

                BCM_Entry result = FindParent(child, entry);
                if (result != null) return result;
            }

            return null;
        }

        private class ReindexSnapshot
        {
            public BCM_Entry Entry { get; }
            public string Index { get; }
            public string LoopAsChild { get; }
            public string LoopAsSibling { get; }

            public ReindexSnapshot(BCM_Entry entry, string index, string loopAsChild, string loopAsSibling)
            {
                Entry = entry;
                Index = index;
                LoopAsChild = loopAsChild;
                LoopAsSibling = loopAsSibling;
            }
        }

        private class DeleteEntryInfo
        {
            public BCM_Entry Entry { get; }
            public IList<BCM_Entry> OwnerList { get; }

            public DeleteEntryInfo(BCM_Entry entry, IList<BCM_Entry> ownerList)
            {
                Entry = entry;
                OwnerList = ownerList;
            }
        }

    }
}
