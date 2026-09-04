using GalaSoft.MvvmLight.CommandWpf;
using LB_Common.Forms;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.Windows;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BEV;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Views
{
    public partial class BsaTab : UserControl, INotifyPropertyChanged
    {
        public RelayCommand AddEntryCommand => new RelayCommand(AddEntry, IsBsaFileLoaded);
        public RelayCommand AddEntryAtSpecificIdCommand => new RelayCommand(AddEntryAtSpecificId, IsBsaFileLoaded);
        public RelayCommand DuplicateEntryCommand => new RelayCommand(DuplicateEntry, () => SelectedEntry != null);
        public RelayCommand CopyEntryCommand => new RelayCommand(CopyEntry, () => SelectedEntry != null && IsBsaCopyPasteAvailable());
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

            Log.Add("BSA entry added with ID: " + entry.SortID);
        }

        private void AddEntryAtSpecificId()
        {
            BSA_File file = GetSelectedFile();
            if (file == null) return;

            ValueSelector selector = new ValueSelector("Add Projectile Entry", "ID", null, GetFreeEntryId(file), 0, 10000);
            selector.ShowDialog();
            if (!selector.IsFinished) return;

            if (file.BSA_Entries.Any(entry => entry.SortID == selector.Parameter))
            {
                MessagePrompt.Show("The entered ID is already used by another BSA entry.", "ID Already Used", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                return;
            }

            BSA_Entry newEntry = new BSA_Entry();
            newEntry.InitializeIBsaTypes();
            file.AddEntry(selector.Parameter, newEntry);
            UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_Entry>(file.BSA_Entries, newEntry, "BSA Entry Add"));
            RefreshEntryList();
            SelectEntry(newEntry);

            Log.Add("BSA entry added with ID: " + newEntry.SortID);
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
            if (files.SelectedMove == null) return;

            List<BSA_Entry> entries = SelectedEntries.Distinct().ToList();
            if (entries.Count == 0) return;

            CopyItem copyItem = new CopyItem(entries, files.SelectedMove);
            XenoKitClipboard.SetData(ClipboardConstants.BsaEntry_CopyItem, copyItem);
        }

        private void PasteEntry()
        {
            if (files.SelectedMove == null) return;
            if (!XenoKitClipboard.TryGetData(ClipboardConstants.BsaEntry_CopyItem, out CopyItem copyItem)) return;

            new PasteCopyItem(copyItem, files.SelectedMove).ShowDialog();
            RefreshEntryList();
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

            if (entries.Count == 0) return;

            List<IUndoRedo> undos = new List<IUndoRedo>();
            foreach (BSA_Entry entry in entries)
            {
                int index = file.BSA_Entries.IndexOf(entry);
                undos.Add(new UndoableListRemove<BSA_Entry>(file.BSA_Entries, entry, index, "BSA Entry Delete"));
                file.BSA_Entries.RemoveAt(index);
            }

            UndoManager.Instance.AddCompositeUndo(undos, entries.Count > 1 ? "BSA Entries Delete" : "BSA Entry Delete");
            SelectedEntry = null;
            RefreshEntryList();
        }

        private void ReindexEntries()
        {
            BSA_File file = GetSelectedFile();
            if (file == null) return;

            MessagePromptResult result = MessagePrompt.Show("Reindex projectile entries by current sorted order?", "Reindex BSA Entries", MessagePromptButtons.YesNo, MessagePromptIcon.Question);

            if (result != MessagePromptResult.Yes) return;

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
                    undos.Add(new UndoableProperty<BSA_Entry>(nameof(BSA_Entry.SortID), entry, oldId, id, "BSA Entry ID"));
                    entry.SortID = id;
                }
                id++;
            }

            foreach (BSA_Entry entry in file.BSA_Entries)
            {
                RemapEntryReferences(undos, entry, idMap);

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

        private static void RemapEntryReferences(List<IUndoRedo> undos, BSA_Entry entry, Dictionary<int, int> idMap)
        {
            if (TryRemap(entry.Expires, idMap, out ushort expires))
            {
                undos.Add(new UndoableProperty<BSA_Entry>(nameof(BSA_Entry.Expires), entry, entry.Expires, expires, "BSA Entry Reference"));
                entry.Expires = expires;
            }

            if (TryRemap(entry.ImpactProjectile, idMap, out ushort impactProjectile))
            {
                undos.Add(new UndoableProperty<BSA_Entry>(nameof(BSA_Entry.ImpactProjectile), entry, entry.ImpactProjectile, impactProjectile, "BSA Entry Reference"));
                entry.ImpactProjectile = impactProjectile;
            }

            if (TryRemap(entry.ImpactEnemy, idMap, out ushort impactEnemy))
            {
                undos.Add(new UndoableProperty<BSA_Entry>(nameof(BSA_Entry.ImpactEnemy), entry, entry.ImpactEnemy, impactEnemy, "BSA Entry Reference"));
                entry.ImpactEnemy = impactEnemy;
            }

            if (TryRemap(entry.ImpactGround, idMap, out ushort impactGround))
            {
                undos.Add(new UndoableProperty<BSA_Entry>(nameof(BSA_Entry.ImpactGround), entry, entry.ImpactGround, impactGround, "BSA Entry Reference"));
                entry.ImpactGround = impactGround;
            }
        }

        private static bool TryRemap(ushort oldValue, Dictionary<int, int> idMap, out ushort newValue)
        {
            newValue = oldValue;

            if (oldValue == ushort.MaxValue || !idMap.TryGetValue(oldValue, out int mappedId))
                return false;

            newValue = (ushort)mappedId;
            return newValue != oldValue;
        }

        private static void RemapPassEntryReference(List<IUndoRedo> undos, BSA_Type0 passEntry, Dictionary<int, int> idMap)
        {
            if (!TryRemap(passEntry.BSA_EntryID, idMap, out ushort newValue))
                return;

            undos.Add(new UndoableProperty<BSA_Type0>(nameof(BSA_Type0.BSA_EntryID), passEntry, passEntry.BSA_EntryID, newValue, "BSA Pass Entry Reference"));
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

            undos.Add(new UndoableProperty<BAC_Type9>(nameof(BAC_Type9.EntryID), projectile, projectile.EntryID, mappedId, "BAC Projectile BSA Entry Reference"));
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
