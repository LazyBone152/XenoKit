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
        private void RebuildSubtypeRows()
        {
            object selectedSource = SelectedSubtypeRow?.Source;
            SubtypeRows.Clear();

            if (SelectedEntry == null)
                return;

            if (SelectedEntry.IBsaTypes == null)
                SelectedEntry.InitializeIBsaTypes();
            EnsureSubEntries();

            foreach (IBsaType type in SelectedEntry.IBsaTypes)
                SubtypeRows.Add(new BsaSubtypeRow(type));

            for (int i = 0; i < SelectedEntry.SubEntries.CollisionEntries.Count; i++)
                SubtypeRows.Add(new BsaSubtypeRow(SelectedEntry.SubEntries.CollisionEntries[i], i));

            for (int i = 0; i < SelectedEntry.SubEntries.ExpirationEntries.Count; i++)
                SubtypeRows.Add(new BsaSubtypeRow(SelectedEntry.SubEntries.ExpirationEntries[i], i));

            if (selectedSource != null)
                SelectedSubtypeRow = SubtypeRows.FirstOrDefault(row => ReferenceEquals(row.Source, selectedSource));
        }

        private void SelectSubtypeSource(object source)
        {
            RebuildSubtypeRows();
            SelectedSubtypeRow = SubtypeRows.FirstOrDefault(row => ReferenceEquals(row.Source, source));
            if (SelectedSubtypeRow != null)
                subtypeGrid.ScrollIntoView(SelectedSubtypeRow);
            RefreshGrids();
        }

        private void SelectEntry(BSA_Entry entry)
        {
            SelectedEntry = entry;
            entryGrid.SelectedItem = entry;
            entryGrid.ScrollIntoView(entry);
            RefreshGrids();
        }

        private void UpdateSubtypeViewModels()
        {
            SetTypeViewModel(null);
            SetCollisionViewModel(null);
            SetExpirationViewModel(null);

            if (SelectedSubtypeRow?.Source is IBsaType type)
                SetTypeViewModel(BsaTypeBaseViewModel.Create(type));
            else if (SelectedSubtypeRow?.Source is BSA_Collision collision)
                SetCollisionViewModel(new BsaCollisionViewModel(collision));
            else if (SelectedSubtypeRow?.Source is BSA_Expiration expiration)
                SetExpirationViewModel(new BsaExpirationViewModel(expiration));
        }

        private void SetEntryViewModel(BsaEntryViewModel viewModel)
        {
            EntryViewModel?.Dispose();
            EntryViewModel = viewModel;
        }

        private void SetTypeViewModel(BsaTypeBaseViewModel viewModel)
        {
            if (TypeViewModel != null)
                TypeViewModel.TypeChanged -= TypeViewModel_TypeChanged;

            TypeViewModel?.Dispose();
            TypeViewModel = viewModel;

            if (TypeViewModel != null)
                TypeViewModel.TypeChanged += TypeViewModel_TypeChanged;
        }

        private void TypeViewModel_TypeChanged(object sender, EventArgs e)
        {
            SelectedSubtypeRow?.RefreshTiming();
            subtypeGrid?.Items.Refresh();
            PlaySelectedEntryPreview();
        }

        private void SetCollisionViewModel(BsaCollisionViewModel viewModel)
        {
            CollisionViewModel?.Dispose();
            CollisionViewModel = viewModel;
        }

        private void SetExpirationViewModel(BsaExpirationViewModel viewModel)
        {
            ExpirationViewModel?.Dispose();
            ExpirationViewModel = viewModel;
        }

        private void PlaySelectedEntryPreview()
        {
            if (SelectedEntry == null || !SceneManager.IsOnTab(EditorTabs.Projectile)) return;
            BsaEffectPreviewController.Instance.Play(SelectedEntry, files.SelectedMove);
        }

        private void EntryGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                e.Handled = true;
        }

        private void EntryGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = FindParent<DataGridRow>((DependencyObject)e.OriginalSource);

            if (row?.Item is BSA_Entry entry && ReferenceEquals(entry, SelectedEntry))
                SelectedSubtypeRow = null;
        }

    }
}
