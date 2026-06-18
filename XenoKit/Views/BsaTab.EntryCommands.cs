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
using Xv2CoreLib.BAC;
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

        public RelayCommand DeleteEntryCommand => new RelayCommand(DeleteEntry, () => SelectedEntries.Count > 0);

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
            if (file?.BSA_Entries == null) return;

            List<BSA_Entry> entries = SelectedEntries
                .Where(entry => file.BSA_Entries.Contains(entry))
                .Distinct()
                .OrderByDescending(entry => file.BSA_Entries.IndexOf(entry))
                .ToList();

            if (entries.Count == 0)
                return;

            List<IUndoRedo> undos = new List<IUndoRedo>();
            foreach (BSA_Entry entry in entries)
            {
                int index = file.BSA_Entries.IndexOf(entry);
                undos.Add(new UndoableListRemove<BSA_Entry>(file.BSA_Entries, entry, index, "BSA Entry Delete"));
                file.BSA_Entries.RemoveAt(index);
            }

            UndoManager.Instance.AddCompositeUndo(undos, entries.Count > 1 ? "BSA Entries Delete" : "BSA Entry Delete");
            SelectedEntry = null;
            SelectedSubtypeRow = null;
            RebuildSubtypeRows();
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
            Dictionary<int, int> idMap = new Dictionary<int, int>();
            List<BSA_Entry> sortedEntries = file.BSA_Entries.OrderBy(entry => entry.SortID).ToList();
            int id = 0;
            foreach (BSA_Entry entry in sortedEntries)
            {
                int oldId = entry.SortID;
                idMap[oldId] = id;
                if (oldId != id)
                {
                    undos.Add(new UndoablePropertyGeneric(nameof(BSA_Entry.SortID), entry, oldId, id, "BSA Entry ID"));
                    entry.SortID = id;
                }
                id++;
            }

            foreach (BSA_Entry entry in file.BSA_Entries)
            {
                RemapEntryReference(undos, entry, nameof(BSA_Entry.Expires), entry.Expires, idMap);
                RemapEntryReference(undos, entry, nameof(BSA_Entry.ImpactProjectile), entry.ImpactProjectile, idMap);
                RemapEntryReference(undos, entry, nameof(BSA_Entry.ImpactEnemy), entry.ImpactEnemy, idMap);
                RemapEntryReference(undos, entry, nameof(BSA_Entry.ImpactGround), entry.ImpactGround, idMap);

                if (entry.IBsaTypes == null)
                    entry.InitializeIBsaTypes();

                foreach (BSA_Type0 passEntry in entry.IBsaTypes.OfType<BSA_Type0>())
                    RemapPassEntryReference(undos, passEntry, idMap);
            }

            RemapBacProjectileReferences(undos, idMap);

            if (undos.Count > 0) UndoManager.Instance.AddCompositeUndo(undos, "BSA Reindex");
            RefreshEntryList();
            RebuildSubtypeRows();
            PlaySelectedEntryPreview();
        }

        private static void RemapEntryReference(List<IUndoRedo> undos, BSA_Entry entry, string propertyName, ushort oldValue, Dictionary<int, int> idMap)
        {
            if (oldValue == ushort.MaxValue || !idMap.TryGetValue(oldValue, out int mappedId))
                return;

            ushort newValue = (ushort)mappedId;
            if (oldValue == newValue)
                return;

            undos.Add(new UndoablePropertyGeneric(propertyName, entry, oldValue, newValue, "BSA Entry Reference"));
            entry.GetType().GetProperty(propertyName).SetValue(entry, newValue, null);
        }

        private static void RemapPassEntryReference(List<IUndoRedo> undos, BSA_Type0 passEntry, Dictionary<int, int> idMap)
        {
            ushort oldValue = passEntry.BSA_EntryID;
            if (oldValue == ushort.MaxValue || !idMap.TryGetValue(oldValue, out int mappedId))
                return;

            ushort newValue = (ushort)mappedId;
            if (oldValue == newValue)
                return;

            undos.Add(new UndoablePropertyGeneric(nameof(BSA_Type0.BSA_EntryID), passEntry, oldValue, newValue, "BSA Pass Entry Reference"));
            passEntry.BSA_EntryID = newValue;
            passEntry.RefreshType();
        }

        private void RemapBacProjectileReferences(List<IUndoRedo> undos, Dictionary<int, int> idMap)
        {
            if (!ReferenceEquals(files.SelectedMove?.Files?.BsaFile, files.SelectedItem?.SelectedBsaFile))
                return;

            foreach (Xv2File<BAC_File> bacFile in files.SelectedMove?.Files?.BacFiles ?? Enumerable.Empty<Xv2File<BAC_File>>())
            {
                foreach (BAC_Entry bacEntry in bacFile.File?.BacEntries ?? Enumerable.Empty<BAC_Entry>())
                {
                    if (bacEntry.IBacTypes == null)
                        bacEntry.InitializeIBacTypes();

                    foreach (BAC_Type9 projectile in bacEntry.IBacTypes.OfType<BAC_Type9>())
                        RemapBacProjectileReference(undos, projectile, idMap);
                }
            }
        }

        private static void RemapBacProjectileReference(List<IUndoRedo> undos, BAC_Type9 projectile, Dictionary<int, int> idMap)
        {
            if (!IsSkillBsaReference(projectile) || !idMap.TryGetValue(projectile.EntryID, out int mappedId))
                return;

            if (projectile.EntryID == mappedId)
                return;

            undos.Add(new UndoablePropertyGeneric(nameof(BAC_Type9.EntryID), projectile, projectile.EntryID, mappedId, "BAC Projectile BSA Entry Reference"));
            projectile.EntryID = mappedId;
            projectile.RefreshType();
        }

        private static bool IsSkillBsaReference(BAC_Type9 projectile)
        {
            switch (projectile.BsaType)
            {
                case BAC_Type9.BsaTypeEnum.AwokenSkill:
                case BAC_Type9.BsaTypeEnum.SuperSkill:
                case BAC_Type9.BsaTypeEnum.UltimateSkill:
                case BAC_Type9.BsaTypeEnum.EvasiveSkill:
                case BAC_Type9.BsaTypeEnum.KiBlastSkill:
                case BAC_Type9.BsaTypeEnum.NEW_AwokenSkill:
                    return true;
                default:
                    return false;
            }
        }

    }
}
