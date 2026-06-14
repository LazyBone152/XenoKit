using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls.Dialogs;
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
using XenoKit.Editor;
using XenoKit.Editor.Undo;
using XenoKit.Engine;
using XenoKit.Engine.Scripting.BSA;
using XenoKit.ViewModel.BSA;
using XenoKit.Windows;
using Xv2CoreLib;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Views
{
    public partial class BsaTab : UserControl, INotifyPropertyChanged
    {
        public RelayCommand AddEntryCommand => new RelayCommand(AddEntry, IsBsaFileLoaded);

        public RelayCommand AddEntryAtSpecificIdCommand => new RelayCommand(AddEntryAtSpecificId, IsBsaFileLoaded);

        public RelayCommand DuplicateEntryCommand => new RelayCommand(DuplicateEntry, () => SelectedEntry != null);

        public RelayCommand CopyEntryCommand => new RelayCommand(CopyEntry, () => SelectedEntry != null);

        public RelayCommand PasteEntryCommand => new RelayCommand(PasteEntry, CanPasteEntry);

        public RelayCommand DeleteEntryCommand => new RelayCommand(DeleteEntry, () => SelectedEntry != null);

        public RelayCommand ReindexCommand => new RelayCommand(ReindexEntries, IsBsaFileLoaded);

        private void AddEntry()
        {
            BSA_File file = GetSelectedFile();
            if (file == null) return;

            BSA_Entry entry = new BSA_Entry();
            entry.InitializeIBsaTypes();
            file.AddEntry(entry);
            UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_Entry>(file.BSA_Entries, entry, "BSA Entry Add"));
            RefreshEntryList();
            SelectEntry(entry);
        }

        private async void AddEntryAtSpecificId()
        {
            BSA_File file = GetSelectedFile();
            if (file == null) return;

            ValueSelector selector = new ValueSelector("Add Projectile Entry", "ID", null, GetFreeEntryId(file), 0, 10000);
            selector.ShowDialog();
            if (!selector.IsFinished) return;

            if (file.BSA_Entries.Any(entry => entry.SortID == selector.Parameter))
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "ID Already Used", "The entered ID is already used by another BSA entry.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                return;
            }

            BSA_Entry newEntry = new BSA_Entry();
            newEntry.InitializeIBsaTypes();
            file.AddEntry(selector.Parameter, newEntry);
            UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_Entry>(file.BSA_Entries, newEntry, "BSA Entry Add"));
            RefreshEntryList();
            SelectEntry(newEntry);
        }

        private void DuplicateEntry()
        {
            BSA_File file = GetSelectedFile();
            if (file == null || SelectedEntry == null) return;

            BSA_Entry entry = SelectedEntry.Copy();
            file.AddEntry(entry);
            UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_Entry>(file.BSA_Entries, entry, "BSA Entry Duplicate"));
            RefreshEntryList();
            SelectEntry(entry);
        }

        private void CopyEntry()
        {
            Clipboard.SetData(ClipboardConstants.BsaEntry_CopyItem, SelectedEntry.Copy());
        }

        private void PasteEntry()
        {
            BSA_File file = GetSelectedFile();
            if (file == null || !Clipboard.ContainsData(ClipboardConstants.BsaEntry_CopyItem)) return;

            BSA_Entry entry = ((BSA_Entry)Clipboard.GetData(ClipboardConstants.BsaEntry_CopyItem)).Copy();
            if (file.BSA_Entries.Any(existing => existing.SortID == entry.SortID))
                file.AddEntry(entry);
            else
                file.AddEntry(entry.SortID, entry);

            UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_Entry>(file.BSA_Entries, entry, "BSA Entry Paste"));
            RefreshEntryList();
            SelectEntry(entry);
        }

        private void DeleteEntry()
        {
            BSA_File file = GetSelectedFile();
            if (file == null || SelectedEntry == null) return;

            BSA_Entry removedEntry = SelectedEntry;
            UndoManager.Instance.AddUndo(new UndoableListRemove<BSA_Entry>(file.BSA_Entries, removedEntry, "BSA Entry Delete"));
            file.BSA_Entries.Remove(removedEntry);
            SelectedEntry = null;
            RefreshEntryList();
        }

        private async void ReindexEntries()
        {
            BSA_File file = GetSelectedFile();
            if (file == null) return;

            MessageDialogResult result = await DialogCoordinator.Instance.ShowMessageAsync(
                this,
                "Reindex BSA Entries",
                "Reindex projectile entries by current sorted order?",
                MessageDialogStyle.AffirmativeAndNegative,
                DialogSettings.Default);

            if (result != MessageDialogResult.Affirmative) return;

            List<IUndoRedo> undos = new List<IUndoRedo>();
            int id = 0;
            foreach (BSA_Entry entry in file.BSA_Entries.OrderBy(entry => entry.SortID).ToList())
            {
                int oldId = entry.SortID;
                if (oldId != id)
                {
                    undos.Add(new UndoablePropertyGeneric(nameof(BSA_Entry.SortID), entry, oldId, id, "BSA Entry ID"));
                    entry.SortID = id;
                }
                id++;
            }

            if (undos.Count > 0) UndoManager.Instance.AddCompositeUndo(undos, "BSA Reindex");
            RefreshEntryList();
        }

    }
}
