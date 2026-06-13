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
        private void EnsureSubEntries()
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

        private BSA_File GetSelectedFile()
        {
            return files.SelectedMove?.Files?.BsaFile?.File;
        }

        private void SelectCurrentMoveBsaFile()
        {
            if (files.SelectedItem == null)
                return;

            files.SelectedItem.SelectedBsaFile = files.SelectedMove?.Files?.BsaFile;
        }

        private void RefreshSelectedMoveBsaFile()
        {
            SelectCurrentMoveBsaFile();
            SelectedEntry = null;
            BsaEffectPreviewController.Instance.Stop();
            NotifyAll();
            RefreshGrids();
        }

        private bool IsBsaFileLoaded()
        {
            return GetSelectedFile() != null;
        }

        private bool ContainsEntry(BSA_File file, BSA_Entry entry)
        {
            return file?.BSA_Entries != null && entry != null && file.BSA_Entries.Contains(entry);
        }

        private bool ContainsSubtypeSource(BSA_Entry entry, object source)
        {
            if (entry == null || source == null)
                return false;

            if (entry.IBsaTypes?.Contains(source) == true)
                return true;

            if (entry.SubEntries?.CollisionEntries?.Contains(source) == true)
                return true;

            return entry.SubEntries?.ExpirationEntries?.Contains(source) == true;
        }

        private void RestoreSubtypeSelection(object previousSource)
        {
            if (ContainsSubtypeSource(SelectedEntry, previousSource))
            {
                SelectedSubtypeRow = SubtypeRows.FirstOrDefault(row => ReferenceEquals(row.Source, previousSource));
                return;
            }

            SelectedSubtypeRow = null;
        }

        private bool CanPasteEntry()
        {
            return IsBsaFileLoaded() && Clipboard.ContainsData(ClipboardConstants.BsaEntry_CopyItem);
        }

        private bool CanPasteSubtype()
        {
            return SelectedEntry != null &&
                   (Clipboard.ContainsData(ClipboardConstants.BsaType_CopyItem) ||
                    Clipboard.ContainsData(BsaCollisionCopyItem) ||
                    Clipboard.ContainsData(BsaExpirationCopyItem));
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
                case 0:
                    return new BSA_Type0();
                case 1:
                    return new BSA_Type1();
                case 2:
                    return new BSA_Type2();
                case 3:
                    return new BSA_Type3();
                case 4:
                    return new BSA_Type4();
                case 6:
                    return new BSA_Type6();
                case 7:
                    return new BSA_Type7();
                case 8:
                    return new BSA_Type8();
                case 10:
                    return new BSA_Type10();
                case 12:
                    return new BSA_Type12();
                case 13:
                    return new BSA_Type13();
                case 14:
                    return new BSA_Type14();
                default:
                    return null;
            }
        }

        private static IBsaType CloneBsaType(IBsaType type)
        {
            return type.Copy();
        }

        private void RefreshGrids()
        {
            entryGrid?.Items.Refresh();
            subtypeGrid?.Items.Refresh();
        }

        private void NotifyAll()
        {
            NotifyPropertyChanged(nameof(BsaFiles));
            NotifyPropertyChanged(nameof(BsaFileSelectorVisibility));
            NotifyPropertyChanged(nameof(BsaFileTextVisibility));
            NotifyPropertyChanged(nameof(SelectedBsaFileName));
            NotifyPropertyChanged(nameof(Entries));
            NotifyPropertyChanged(nameof(SelectedEntry));
            NotifyPropertyChanged(nameof(SubtypeRows));
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
            BsaSubtypeSelectionChanged?.Invoke(this, EventArgs.Empty);
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
