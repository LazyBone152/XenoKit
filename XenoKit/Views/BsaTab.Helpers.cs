using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BSA;
using XenoKit.ViewModel.BSA;
using Xv2CoreLib.BSA;

namespace XenoKit.Views
{
    public partial class BsaTab : UserControl, INotifyPropertyChanged
    {
        private void InitSubEntries()
        {
            if (SelectedEntry == null) return;

            if (SelectedEntry.SubEntries == null)
                SelectedEntry.SubEntries = new BSA_SubEntries();
            if (SelectedEntry.SubEntries.CollisionEntries == null)
                SelectedEntry.SubEntries.CollisionEntries = new List<BSA_Collision>();
            if (SelectedEntry.SubEntries.ExpirationEntries == null)
                SelectedEntry.SubEntries.ExpirationEntries = new List<BSA_Expiration>();
            if (SelectedEntry.IBsaTypes == null)
                SelectedEntry.InitializeIBsaTypes();
        }

        /// <summary>
        /// Collision and Expiration are listed as their own types after the IBsaTypes, even though the format
        /// stores them in separate arrays.
        /// </summary>
        private void RebuildSubtypeRows()
        {
            DisposeSubtypeRows();
            SubtypeRows.Clear();

            if (SelectedEntry == null) return;

            InitSubEntries();

            foreach (IBsaType type in SelectedEntry.IBsaTypes)
                SubtypeRows.Add(new BsaSubtypeRow(type));

            foreach (BSA_Collision collision in SelectedEntry.SubEntries.CollisionEntries)
                SubtypeRows.Add(new BsaSubtypeRow(collision));

            foreach (BSA_Expiration expiration in SelectedEntry.SubEntries.ExpirationEntries)
                SubtypeRows.Add(new BsaSubtypeRow(expiration));
        }

        private void DisposeSubtypeRows()
        {
            foreach (BsaSubtypeRow row in SubtypeRows)
                row.Dispose();
        }

        private BSA_File GetSelectedFile()
        {
            return files.SelectedItem?.SelectedBsaFile?.File;
        }

        private void SelectCurrentMoveBsaFile()
        {
            if (files.SelectedItem == null) return;

            files.SelectedItem.SelectedBsaFile = files.SelectedMove?.Files?.BsaFile;
        }

        private void RefreshSelectedMoveBsaFile()
        {
            SelectCurrentMoveBsaFile();
            SelectedEntry = null;
            BsaEffectPreviewController.Instance.Stop();
            CreateEntryList();
            NotifyAll();
        }

        private void CreateEntryList()
        {
            BSA_File file = GetSelectedFile();

            if (file?.BSA_Entries != null)
            {
                ViewBsaEntries = new ListCollectionView(file.BSA_Entries);
                ViewBsaEntries.SortDescriptions.Add(new SortDescription(nameof(BSA_Entry.SortID), ListSortDirection.Ascending));
            }
            else
            {
                ViewBsaEntries = null;
            }
        }

        /// <summary>
        /// Only called for structural changes (add, delete, paste, duplicate, reindex, file switch).
        /// Value edits reach the grid through INotifyPropertyChanged on the models, so refreshing here
        /// would reset the grid selection and destroy an in-progress edit.
        /// </summary>
        private void RefreshEntryList()
        {
            ViewBsaEntries?.Refresh();
            NotifyPropertyChanged(nameof(ViewBsaEntries));
        }

        private bool IsBsaFileLoaded()
        {
            return GetSelectedFile() != null;
        }

        private bool ContainsEntry(BSA_File file, BSA_Entry entry)
        {
            return file?.BSA_Entries != null && entry != null && file.BSA_Entries.Contains(entry);
        }

        /// <summary>
        /// CopyItem paste requires the move being pasted into to be the one selected in the outliner,
        /// and it only handles BSA for Skill and CMN moves.
        /// </summary>
        private bool IsBsaCopyPasteAvailable()
        {
            Move move = files.SelectedMove;

            if (move == null || GetSelectedFile() == null) return false;
            if (!ReferenceEquals(move.Files?.BsaFile?.File, GetSelectedFile())) return false;

            return move.MoveType == Move.Type.Skill || move.MoveType == Move.Type.CMN;
        }

        private bool CanPasteEntry()
        {
            return IsBsaCopyPasteAvailable() && XenoKitClipboard.ContainsData(ClipboardConstants.BsaEntry_CopyItem);
        }

        private bool CanPasteSubtype()
        {
            if (SelectedEntry == null) return false;

            return (IsBsaCopyPasteAvailable() && XenoKitClipboard.ContainsData(ClipboardConstants.BsaType_CopyItem))
                || XenoKitClipboard.ContainsData(ClipboardConstants.BsaCollision_CopyItem)
                || XenoKitClipboard.ContainsData(ClipboardConstants.BsaExpiration_CopyItem);
        }

        private static int GetFreeEntryId(BSA_File file)
        {
            int id = 0;
            while (file.BSA_Entries.Any(entry => entry.SortID == id))
                id++;
            return id;
        }

        private static IBsaType CreateBsaType(int typeId)
        {
            switch (typeId)
            {
                case 0: return new BSA_Type0();
                case 1: return new BSA_Type1();
                case 2: return new BSA_Type2();
                case 3: return new BSA_Type3();
                case 4: return new BSA_Type4();
                case 6: return new BSA_Type6();
                case 7: return new BSA_Type7();
                case 8: return new BSA_Type8();
                case 10: return new BSA_Type10();
                case 12: return new BSA_Type12();
                case 13: return new BSA_Type13();
                case 14: return new BSA_Type14();
                default: return null;
            }
        }

        private void NotifyAll()
        {
            NotifyPropertyChanged(nameof(SelectedBsaFileName));
            NotifyPropertyChanged(nameof(ViewBsaEntries));
            NotifyPropertyChanged(nameof(SelectedEntry));
            UpdateViewModels();
        }

        private void UpdateViewModels()
        {
            NotifyPropertyChanged(nameof(EntryViewModel));
            NotifyPropertyChanged(nameof(TypeBaseViewModel));
            NotifyPropertyChanged(nameof(Type0ViewModel));
            NotifyPropertyChanged(nameof(Type1ViewModel));
            NotifyPropertyChanged(nameof(Type2ViewModel));
            NotifyPropertyChanged(nameof(Type3ViewModel));
            NotifyPropertyChanged(nameof(Type4ViewModel));
            NotifyPropertyChanged(nameof(Type6ViewModel));
            NotifyPropertyChanged(nameof(Type7ViewModel));
            NotifyPropertyChanged(nameof(Type8ViewModel));
            NotifyPropertyChanged(nameof(Type10ViewModel));
            NotifyPropertyChanged(nameof(Type12ViewModel));
            NotifyPropertyChanged(nameof(Type13ViewModel));
            NotifyPropertyChanged(nameof(Type14ViewModel));
            NotifyPropertyChanged(nameof(CollisionViewModel));
            NotifyPropertyChanged(nameof(ExpirationViewModel));
        }

        private static T FindParent<T>(DependencyObject source) where T : DependencyObject
        {
            while (source != null)
            {
                if (source is T match)
                    return match;

                source = VisualTreeHelper.GetParent(source);
            }

            return null;
        }
    }
}
