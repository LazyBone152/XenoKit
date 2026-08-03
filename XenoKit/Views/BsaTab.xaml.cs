using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Engine.Scripting.BSA;
using XenoKit.ViewModel.BSA;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Views
{
    public partial class BsaTab : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public Files files => Files.Instance;

        private BSA_Entry selectedEntry;
        private BsaSubtypeRow selectedSubtypeRow;
        private bool isSubscribed;

        private BsaEntryViewModel entryViewModel;
        private BsaTypeBaseViewModel typeViewModel;
        private BsaTypeBaseViewModel typeBaseViewModel;
        private BsaCollisionViewModel collisionViewModel;
        private BsaExpirationViewModel expirationViewModel;
        private ListCollectionView viewBsaEntries;

        public string SelectedBsaFileName => files.SelectedItem?.SelectedBsaFile?.DisplayName ?? string.Empty;

        public ObservableCollection<BsaSubtypeRow> SubtypeRows { get; } = new ObservableCollection<BsaSubtypeRow>();

        private IList<BSA_Entry> SelectedEntries => entryGrid?.SelectedItems.Cast<BSA_Entry>().ToList() ?? new List<BSA_Entry>();

        public ListCollectionView ViewBsaEntries
        {
            get => viewBsaEntries;
            private set
            {
                if (viewBsaEntries == value) return;
                viewBsaEntries = value;
                NotifyPropertyChanged(nameof(ViewBsaEntries));
            }
        }

        public BSA_Entry SelectedEntry
        {
            get => selectedEntry;
            set
            {
                if (selectedEntry == value) return;

                selectedEntry = value;
                InitSubEntries();
                RebuildSubtypeRows();
                SetEntryViewModel(selectedEntry != null ? new BsaEntryViewModel(selectedEntry) : null);
                SelectedSubtypeRow = null;
                NotifyPropertyChanged(nameof(SelectedEntry));
                NotifyPropertyChanged(nameof(SelectedBsaID));
                PlaySelectedEntryPreview();
            }
        }

        /// <summary>
        /// Backs the editable ID column on the entry grid, mirroring BacTab.SelectedBacID.
        /// </summary>
        public int SelectedBsaID
        {
            get => selectedEntry?.SortID ?? 0;
            set
            {
                if (selectedEntry == null || selectedEntry.SortID == value) return;
                EditBsaId(value);
            }
        }

        public BsaSubtypeRow SelectedSubtypeRow
        {
            get => selectedSubtypeRow;
            set
            {
                selectedSubtypeRow = value;
                UpdateSubtypeViewModels();
                NotifyPropertyChanged(nameof(SelectedSubtypeRow));
                UpdateViewModels();
            }
        }

        private object SelectedSubtypeSource => selectedSubtypeRow?.Source;

        /// <summary>
        /// The entry sections (Projectile, Impact Properties, Pass On, Unknown) only apply to the entry
        /// itself, so they are hidden while a subtype row is selected. Clicking the already selected entry
        /// row clears the subtype selection and brings them back.
        /// </summary>
        public BsaEntryViewModel EntryViewModel => selectedSubtypeRow == null ? entryViewModel : null;

        public BsaTypeBaseViewModel TypeBaseViewModel => typeBaseViewModel;
        public BsaCollisionViewModel CollisionViewModel => collisionViewModel;
        public BsaExpirationViewModel ExpirationViewModel => expirationViewModel;

        public BsaType0ViewModel Type0ViewModel => typeViewModel as BsaType0ViewModel;
        public BsaType1ViewModel Type1ViewModel => typeViewModel as BsaType1ViewModel;
        public BsaType2ViewModel Type2ViewModel => typeViewModel as BsaType2ViewModel;
        public BsaType3ViewModel Type3ViewModel => typeViewModel as BsaType3ViewModel;
        public BsaType4ViewModel Type4ViewModel => typeViewModel as BsaType4ViewModel;
        public BsaType6ViewModel Type6ViewModel => typeViewModel as BsaType6ViewModel;
        public BsaType7ViewModel Type7ViewModel => typeViewModel as BsaType7ViewModel;
        public BsaType8ViewModel Type8ViewModel => typeViewModel as BsaType8ViewModel;
        public BsaType10ViewModel Type10ViewModel => typeViewModel as BsaType10ViewModel;
        public BsaType12ViewModel Type12ViewModel => typeViewModel as BsaType12ViewModel;
        public BsaType13ViewModel Type13ViewModel => typeViewModel as BsaType13ViewModel;
        public BsaType14ViewModel Type14ViewModel => typeViewModel as BsaType14ViewModel;

        public BsaTab()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += BsaTab_Loaded;
            Unloaded += BsaTab_Unloaded;
        }

        private void BsaTab_Loaded(object sender, RoutedEventArgs e)
        {
            SubscribeToEvents();
            RefreshSelectedMoveBsaFile();
        }

        private void BsaTab_Unloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeFromEvents();
            BsaEffectPreviewController.Instance.Stop();
            SelectedSubtypeRow = null;
            SetEntryViewModel(null);
            DisposeSubtypeRows();
        }

        private void SubscribeToEvents()
        {
            if (isSubscribed) return;

            Files.SelectedItemChanged += Files_SelectedItemChanged;
            SceneManager.EditorTabChanged += SceneManager_EditorTabChanged;
            UndoManager.Instance.UndoOrRedoCalled += UndoManager_UndoOrRedoCalled;
            isSubscribed = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (!isSubscribed) return;

            Files.SelectedItemChanged -= Files_SelectedItemChanged;
            SceneManager.EditorTabChanged -= SceneManager_EditorTabChanged;
            UndoManager.Instance.UndoOrRedoCalled -= UndoManager_UndoOrRedoCalled;
            isSubscribed = false;
        }

        /// <summary>
        /// Undo writes to the models directly, so only structural state is rebuilt here. Every viewmodel
        /// re-raises its own properties from its own UndoOrRedoCalled subscription.
        /// </summary>
        private void UndoManager_UndoOrRedoCalled(object sender, UndoEventRaisedEventArgs e)
        {
            if (!SceneManager.IsOnTab(EditorTabs.Projectile)) return;

            BSA_File file = GetSelectedFile();

            if (file == null)
            {
                ViewBsaEntries = null;
                SelectedEntry = null;
                return;
            }

            if (!ContainsEntry(file, SelectedEntry))
                SelectedEntry = file.BSA_Entries?.FirstOrDefault();

            ViewBsaEntries?.Refresh();

            if (SelectedEntry?.IBsaTypes != null)
            {
                foreach (IBsaType type in SelectedEntry.IBsaTypes)
                    type.RefreshType();
            }

            // The subtype list is structural, so it has to be rebuilt after an undo of an add or delete.
            object previousSource = SelectedSubtypeSource;
            RebuildSubtypeRows();
            SelectedSubtypeRow = SubtypeRows.FirstOrDefault(row => ReferenceEquals(row.Source, previousSource));

            PlaySelectedEntryPreview();
        }

        private void Files_SelectedItemChanged(object sender, EventArgs e)
        {
            RefreshSelectedMoveBsaFile();
        }

        private void SceneManager_EditorTabChanged(object sender, EventArgs e)
        {
            if (SceneManager.IsOnTab(EditorTabs.Projectile))
                RefreshSelectedMoveBsaFile();
            else
                BsaEffectPreviewController.Instance.Stop();
        }

        private void UpdateSubtypeViewModels()
        {
            SetTypeViewModel(SelectedSubtypeSource is IBsaType type ? BsaTypeBaseViewModel.Create(type) : null);

            SetSubEntryViewModels(
                SelectedSubtypeSource is BSA_Collision collision ? new BsaCollisionViewModel(collision) : null,
                SelectedSubtypeSource is BSA_Expiration expiration ? new BsaExpirationViewModel(expiration) : null);
        }

        private void SetEntryViewModel(BsaEntryViewModel viewModel)
        {
            entryViewModel?.Dispose();
            entryViewModel = viewModel;
            NotifyPropertyChanged(nameof(EntryViewModel));
        }

        private void SetTypeViewModel(BsaTypeBaseViewModel viewModel)
        {
            typeViewModel?.Dispose();
            typeBaseViewModel?.Dispose();

            typeViewModel = viewModel;

            // A separate instance drives the shared Activation section, the same split BacTab uses.
            typeBaseViewModel = viewModel?.SourceType is BSA_TypeBase typeBase ? new BsaTypeBaseViewModel(typeBase) : null;

            NotifyPropertyChanged(nameof(TypeBaseViewModel));
        }

        private void SetSubEntryViewModels(BsaCollisionViewModel collision, BsaExpirationViewModel expiration)
        {
            collisionViewModel?.Dispose();
            expirationViewModel?.Dispose();
            collisionViewModel = collision;
            expirationViewModel = expiration;
            NotifyPropertyChanged(nameof(CollisionViewModel));
            NotifyPropertyChanged(nameof(ExpirationViewModel));
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
