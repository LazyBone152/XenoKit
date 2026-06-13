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
        private const string BsaCollisionCopyItem = "XenoKit_BsaCollisionCopyItem";
        private const string BsaExpirationCopyItem = "XenoKit_BsaExpirationCopyItem";

        public static event EventHandler BsaSubtypeSelectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        public Files files => Files.Instance;

        private BSA_Entry selectedEntry;
        private BsaSubtypeRow selectedSubtypeRow;
        private BsaEntryViewModel entryViewModel;
        private BsaTypeBaseViewModel typeViewModel;
        private BsaCollisionViewModel collisionViewModel;
        private BsaExpirationViewModel expirationViewModel;
        private bool isSelectingSubtype;

        public ObservableCollection<BsaSubtypeRow> SubtypeRows { get; } = new ObservableCollection<BsaSubtypeRow>();

        public IList<Xv2File<BSA_File>> BsaFiles
        {
            get
            {
                List<Xv2File<BSA_File>> bsaFiles = new List<Xv2File<BSA_File>>();
                if (files.SelectedMove?.Files?.BsaFile != null) bsaFiles.Add(files.SelectedMove.Files.BsaFile);
                return bsaFiles;
            }
        }

        public IList Entries => files.SelectedMove?.Files?.BsaFile?.File?.BSA_Entries;
        public Visibility BsaFileSelectorVisibility => BsaFiles.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility BsaFileTextVisibility => BsaFiles.Count <= 1 ? Visibility.Visible : Visibility.Collapsed;
        public string SelectedBsaFileName => files.SelectedMove?.Files?.BsaFile?.DisplayName ?? string.Empty;

        public BSA_Entry SelectedEntry
        {
            get => selectedEntry;
            set
            {
                if (selectedEntry == value) return;

                selectedEntry = value;
                if (selectedEntry?.IBsaTypes == null)
                    selectedEntry?.InitializeIBsaTypes();
                SelectedSubtypeRow = null;
                SetEntryViewModel(selectedEntry != null ? new BsaEntryViewModel(selectedEntry) : null);
                RebuildSubtypeRows();
                NotifyPropertyChanged(nameof(SelectedEntry));
                UpdateViewModels();
                PlaySelectedEntryPreview();
            }
        }

        public BsaSubtypeRow SelectedSubtypeRow
        {
            get => selectedSubtypeRow;
            set
            {
                if (selectedSubtypeRow == value || isSelectingSubtype) return;

                isSelectingSubtype = true;
                try
                {
                    selectedSubtypeRow = value;
                    if (selectedSubtypeRow == null)
                        SetEntryViewModel(selectedEntry != null ? new BsaEntryViewModel(selectedEntry) : null);
                    else
                        SetEntryViewModel(null);

                    UpdateSubtypeViewModels();
                    NotifyPropertyChanged(nameof(SelectedSubtypeRow));
                    UpdateViewModels();
                }
                finally
                {
                    isSelectingSubtype = false;
                }
            }
        }

        public BsaEntryViewModel EntryViewModel
        {
            get => entryViewModel;
            private set
            {
                entryViewModel = value;
                NotifyPropertyChanged(nameof(EntryViewModel));
            }
        }

        public BsaTypeBaseViewModel TypeViewModel
        {
            get => typeViewModel;
            private set
            {
                typeViewModel = value;
                NotifyPropertyChanged(nameof(TypeViewModel));
            }
        }

        public BsaCollisionViewModel CollisionViewModel
        {
            get => collisionViewModel;
            private set
            {
                collisionViewModel = value;
                NotifyPropertyChanged(nameof(CollisionViewModel));
            }
        }

        public BsaExpirationViewModel ExpirationViewModel
        {
            get => expirationViewModel;
            private set
            {
                expirationViewModel = value;
                NotifyPropertyChanged(nameof(ExpirationViewModel));
            }
        }

        public BsaTypeBaseViewModel TypeBaseViewModel => TypeViewModel;
        public BsaType0ViewModel Type0ViewModel => TypeViewModel as BsaType0ViewModel;
        public BsaType1ViewModel Type1ViewModel => TypeViewModel as BsaType1ViewModel;
        public BsaType2ViewModel Type2ViewModel => TypeViewModel as BsaType2ViewModel;
        public BsaType3ViewModel Type3ViewModel => TypeViewModel as BsaType3ViewModel;
        public BsaType4ViewModel Type4ViewModel => TypeViewModel as BsaType4ViewModel;
        public BsaType6ViewModel Type6ViewModel => TypeViewModel as BsaType6ViewModel;
        public BsaType7ViewModel Type7ViewModel => TypeViewModel as BsaType7ViewModel;
        public BsaType8ViewModel Type8ViewModel => TypeViewModel as BsaType8ViewModel;
        public BsaType10ViewModel Type10ViewModel => TypeViewModel as BsaType10ViewModel;
        public BsaType12ViewModel Type12ViewModel => TypeViewModel as BsaType12ViewModel;
        public BsaType13ViewModel Type13ViewModel => TypeViewModel as BsaType13ViewModel;
        public BsaType14ViewModel Type14ViewModel => TypeViewModel as BsaType14ViewModel;

        public BsaTab()
        {
            InitializeComponent();
            DataContext = this;
            files.PropertyChanged += Files_PropertyChanged;
            Files.SelectedItemChanged += Files_SelectedItemChanged;
            SceneManager.EditorTabChanged += SceneManager_EditorTabChanged;
            UndoManager.Instance.UndoOrRedoCalled += UndoManager_UndoOrRedoCalled;
            Unloaded += BsaTab_Unloaded;
        }

        private void BsaTab_Unloaded(object sender, RoutedEventArgs e)
        {
            files.PropertyChanged -= Files_PropertyChanged;
            Files.SelectedItemChanged -= Files_SelectedItemChanged;
            SceneManager.EditorTabChanged -= SceneManager_EditorTabChanged;
            UndoManager.Instance.UndoOrRedoCalled -= UndoManager_UndoOrRedoCalled;
            SetTypeViewModel(null);
            SetCollisionViewModel(null);
            SetExpirationViewModel(null);
            EntryViewModel?.Dispose();
            EntryViewModel = null;
        }

        private void UndoManager_UndoOrRedoCalled(object sender, UndoEventRaisedEventArgs e)
        {
            RefreshAfterUndoRedo();
        }

        private void RefreshAfterUndoRedo()
        {
            BSA_File file = GetSelectedFile();
            BSA_Entry previousEntry = SelectedEntry;
            object previousSubtypeSource = SelectedSubtypeRow?.Source;

            if (file == null)
            {
                SelectedSubtypeRow = null;
                SelectedEntry = null;
                NotifyAll();
                return;
            }

            if (!ContainsEntry(file, previousEntry))
                previousEntry = file.BSA_Entries?.FirstOrDefault();

            SelectedEntry = previousEntry;
            if (SelectedEntry != null)
            {
                RebuildSubtypeRows();
                RestoreSubtypeSelection(previousSubtypeSource);
                SetEntryViewModel(new BsaEntryViewModel(SelectedEntry));
            }
            else
            {
                SelectedSubtypeRow = null;
                SetEntryViewModel(null);
                RebuildSubtypeRows();
            }

            UpdateSubtypeViewModels();
            RefreshGrids();
            NotifyAll();
            UpdateViewModels();
            PlaySelectedEntryPreview();
        }

        private void Files_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Files.SelectedItem) || e.PropertyName == nameof(Files.SelectedMove))
            {
                RefreshSelectedMoveBsaFile();
            }
        }

        private void Files_SelectedItemChanged(object sender, EventArgs e)
        {
            RefreshSelectedMoveBsaFile();
        }

        private void SceneManager_EditorTabChanged(object sender, EventArgs e)
        {
            if (SceneManager.IsOnTab(EditorTabs.Projectile))
            {
                PlaySelectedEntryPreview();
            }
            else
            {
                BsaEffectPreviewController.Instance.Stop();
            }
        }

        private void BsaFileSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            SelectedEntry = null;
            BsaEffectPreviewController.Instance.Stop();
            NotifyAll();
            RefreshGrids();
        }





































        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
