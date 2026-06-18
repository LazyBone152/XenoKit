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
            DisposeSubtypeRows();
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
            if (EntryViewModel != null)
                EntryViewModel.EntryChanged -= EntryViewModel_EntryChanged;

            EntryViewModel?.Dispose();
            EntryViewModel = viewModel;

            if (EntryViewModel != null)
                EntryViewModel.EntryChanged += EntryViewModel_EntryChanged;
        }

        private void SetTypeViewModel(BsaTypeBaseViewModel viewModel)
        {
            if (TypeViewModel != null)
                TypeViewModel.TypeChanged -= TypeViewModel_TypeChanged;
            if (TypeActivationViewModel != null)
                TypeActivationViewModel.TypeChanged -= TypeViewModel_TypeChanged;
            if (TypeUnknownViewModel != null)
                TypeUnknownViewModel.TypeChanged -= TypeViewModel_TypeChanged;

            TypeViewModel?.Dispose();
            TypeActivationViewModel?.Dispose();
            TypeUnknownViewModel?.Dispose();
            TypeViewModel = viewModel;
            TypeActivationViewModel = CreateSectionViewModel(viewModel?.SourceType, true, false, false);
            TypeUnknownViewModel = CreateSectionViewModel(viewModel?.SourceType, false, false, true);

            if (TypeViewModel != null)
                TypeViewModel.TypeChanged += TypeViewModel_TypeChanged;
            if (TypeActivationViewModel != null)
                TypeActivationViewModel.TypeChanged += TypeViewModel_TypeChanged;
            if (TypeUnknownViewModel != null)
                TypeUnknownViewModel.TypeChanged += TypeViewModel_TypeChanged;
        }

        private static BsaTypeBaseViewModel CreateSectionViewModel(IBsaType type, bool showActivation, bool showPrimaryFields, bool showUnknownFields)
        {
            if (type == null)
                return null;

            BsaTypeBaseViewModel viewModel = BsaTypeBaseViewModel.Create(type);
            viewModel.ShowActivation = showActivation;
            viewModel.ShowPrimaryFields = showPrimaryFields;
            viewModel.ShowUnknownFields = showUnknownFields;
            viewModel.RaiseSectionVisibilityProperties();
            return viewModel;
        }

        private void TypeViewModel_TypeChanged(object sender, EventArgs e)
        {
            SelectedSubtypeRow?.RefreshTiming();
            subtypeGrid?.Items.Refresh();
            PlaySelectedEntryPreview();
        }

        private void EntryViewModel_EntryChanged(object sender, EventArgs e)
        {
            RefreshEntryList();
            PlaySelectedEntryPreview();
        }

        private void SetCollisionViewModel(BsaCollisionViewModel viewModel)
        {
            if (CollisionViewModel != null)
                CollisionViewModel.CollisionChanged -= CollisionViewModel_CollisionChanged;

            CollisionViewModel?.Dispose();
            CollisionViewModel = viewModel;

            if (CollisionViewModel != null)
                CollisionViewModel.CollisionChanged += CollisionViewModel_CollisionChanged;
        }

        private void CollisionViewModel_CollisionChanged(object sender, EventArgs e)
        {
            SelectedSubtypeRow?.RefreshTiming();
            subtypeGrid?.Items.Refresh();
        }

        private void SetExpirationViewModel(BsaExpirationViewModel viewModel)
        {
            ExpirationViewModel?.Dispose();
            ExpirationViewModel = viewModel;
        }

        private void PlaySelectedEntryPreview()
        {
            BSA_Entry entry = SelectedEntry;
            BSA_File file = GetSelectedFile();

            if (entry == null || file == null || !SceneManager.IsOnTab(EditorTabs.Projectile))
                return;

            if (file.BSA_Entries?.Contains(entry) != true)
                return;

            BsaEffectPreviewController.Instance.Play(entry, files.SelectedMove, file);
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
