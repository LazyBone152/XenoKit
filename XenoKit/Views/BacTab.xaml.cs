using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using XenoKit.Editor;
using Xv2CoreLib.BAC;
using XenoKit.ViewModel.BAC;
using GalaSoft.MvvmLight.CommandWpf;
using Xv2CoreLib.Resource.UndoRedo;
using XenoKit.Windows;
using XenoKit.Engine;
using Xv2CoreLib.Resource.App;
using MahApps.Metro.Controls.Dialogs;
using Xv2CoreLib.Resource;
using XenoKit.Windows.EAN;

namespace XenoKit.Controls
{
    /// <summary>
    /// Interaction logic for BacTab.xaml
    /// </summary>
    public partial class BacTab : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        public static event EventHandler BacTypeSelectionChanged;
        public Files files => Files.Instance;

        private BAC_Entry _selectedBacEntry = null;
        public BAC_Entry SelectedBacEntry
        {
            get => _selectedBacEntry;
            set
            {
                if(_selectedBacEntry != value) 
                {
                    _selectedBacEntry = value;
                    NotifyPropertyChanged(nameof(SelectedBacEntry));

                    if (SceneManager.IsOnTab(EditorTabs.Action))
                    {
                        PlayBacEntry();
                    }
                }
            }
        }
        private IList<BAC_Entry> SelectedBacEntries => bacEntryDataGrid.SelectedItems.Cast<BAC_Entry>().ToList();
        private IBacType SelectedBacType
        {
            get
            {
                if (ViewMode == BacViewMode.TimeLine)
                    return timeline.SelectedItem as IBacType;
                else
                    return bacTypeDataGrid.SelectedItem as IBacType;
            }
        }
        private IList<IBacType> SelectedBacTypes
        {
            get
            {
                if (ViewMode == BacViewMode.TimeLine)
                    return timeline.SelectedItems.Cast<IBacType>().ToList();
                else
                    return bacTypeDataGrid.SelectedItems.Cast<IBacType>().ToList();

            }
        }

        //Selected BacTypes exposed as statics for gizmos to check against
        public static IBacBone SelectedIBacBone { get; private set; }
        public static IBacType StaticSelectedBacType { get; set; }

        #region ViewModels
        //Current view models are kept around so they can the event references can be cleared up properly
        private IDisposable _lastViewModelBase = null;
        private IDisposable _lastViewModel = null;

        private object SetBaseViewModel(IDisposable viewModel)
        {
            if(viewModel != null)
            {
                if (_lastViewModelBase != null)
                    _lastViewModelBase.Dispose();

                _lastViewModelBase = viewModel;
            }

            return viewModel;
        }

        private object SetViewModel(IDisposable viewModel)
        {
            if (viewModel != null)
            {
                if (_lastViewModel != null)
                    _lastViewModel.Dispose();

                _lastViewModel = viewModel;
            }

            return viewModel;
        }

        public BACTypeBaseViewModel BacTypeBaseViewModel => (BACTypeBaseViewModel)SetBaseViewModel((SelectedBacType is BAC_TypeBase) ? new BACTypeBaseViewModel(SelectedBacType as BAC_TypeBase) : null);
        public BACType0ViewModel BacType0ViewModel => (BACType0ViewModel)SetViewModel((SelectedBacType is BAC_Type0) ? new BACType0ViewModel(SelectedBacType as BAC_Type0) : null);
        public BACType1ViewModel BacType1ViewModel => (BACType1ViewModel)SetViewModel((SelectedBacType is BAC_Type1) ? new BACType1ViewModel(SelectedBacType as BAC_Type1) : null);
        public BACType2ViewModel BacType2ViewModel => (BACType2ViewModel)SetViewModel((SelectedBacType is BAC_Type2) ? new BACType2ViewModel(SelectedBacType as BAC_Type2) : null);
        public BACType3ViewModel BacType3ViewModel => (BACType3ViewModel)SetViewModel((SelectedBacType is BAC_Type3) ? new BACType3ViewModel(SelectedBacType as BAC_Type3) : null);
        public BACType4ViewModel BacType4ViewModel => (BACType4ViewModel)SetViewModel((SelectedBacType is BAC_Type4) ? new BACType4ViewModel(SelectedBacType as BAC_Type4) : null);
        public BACType5ViewModel BacType5ViewModel => (BACType5ViewModel)SetViewModel((SelectedBacType is BAC_Type5) ? new BACType5ViewModel(SelectedBacType as BAC_Type5) : null);
        public BACType6ViewModel BacType6ViewModel => (BACType6ViewModel)SetViewModel((SelectedBacType is BAC_Type6) ? new BACType6ViewModel(SelectedBacType as BAC_Type6) : null);
        public BACType7ViewModel BacType7ViewModel => (BACType7ViewModel)SetViewModel((SelectedBacType is BAC_Type7) ? new BACType7ViewModel(SelectedBacType as BAC_Type7) : null);
        public BACType8ViewModel BacType8ViewModel => (BACType8ViewModel)SetViewModel((SelectedBacType is BAC_Type8) ? new BACType8ViewModel(SelectedBacType as BAC_Type8) : null);
        public BACType9ViewModel BacType9ViewModel => (BACType9ViewModel)SetViewModel((SelectedBacType is BAC_Type9) ? new BACType9ViewModel(SelectedBacType as BAC_Type9) : null);
        public BACType10ViewModel BacType10ViewModel => (BACType10ViewModel)SetViewModel((SelectedBacType is BAC_Type10) ? new BACType10ViewModel(SelectedBacType as BAC_Type10) : null);
        public BACType11ViewModel BacType11ViewModel => (BACType11ViewModel)SetViewModel((SelectedBacType is BAC_Type11) ? new BACType11ViewModel(SelectedBacType as BAC_Type11) : null);
        public BACType12ViewModel BacType12ViewModel => (BACType12ViewModel)SetViewModel((SelectedBacType is BAC_Type12) ? new BACType12ViewModel(SelectedBacType as BAC_Type12) : null);
        public BACType13ViewModel BacType13ViewModel => (BACType13ViewModel)SetViewModel((SelectedBacType is BAC_Type13) ? new BACType13ViewModel(SelectedBacType as BAC_Type13) : null);
        public BACType14ViewModel BacType14ViewModel => (BACType14ViewModel)SetViewModel((SelectedBacType is BAC_Type14) ? new BACType14ViewModel(SelectedBacType as BAC_Type14) : null);
        public BACType15ViewModel BacType15ViewModel => (BACType15ViewModel)SetViewModel((SelectedBacType is BAC_Type15) ? new BACType15ViewModel(SelectedBacType as BAC_Type15) : null);
        public BACType16ViewModel BacType16ViewModel => (BACType16ViewModel)SetViewModel((SelectedBacType is BAC_Type16) ? new BACType16ViewModel(SelectedBacType as BAC_Type16) : null);
        public BACType17ViewModel BacType17ViewModel => (BACType17ViewModel)SetViewModel((SelectedBacType is BAC_Type17) ? new BACType17ViewModel(SelectedBacType as BAC_Type17) : null);
        public BACType18ViewModel BacType18ViewModel => (BACType18ViewModel)SetViewModel((SelectedBacType is BAC_Type18) ? new BACType18ViewModel(SelectedBacType as BAC_Type18) : null);
        public BACType19ViewModel BacType19ViewModel => (BACType19ViewModel)SetViewModel((SelectedBacType is BAC_Type19) ? new BACType19ViewModel(SelectedBacType as BAC_Type19) : null);
        public BACType20ViewModel BacType20ViewModel => (BACType20ViewModel)SetViewModel((SelectedBacType is BAC_Type20) ? new BACType20ViewModel(SelectedBacType as BAC_Type20) : null);
        public BACType21ViewModel BacType21ViewModel => (BACType21ViewModel)SetViewModel((SelectedBacType is BAC_Type21) ? new BACType21ViewModel(SelectedBacType as BAC_Type21) : null);
        public BACType22ViewModel BacType22ViewModel => (BACType22ViewModel)SetViewModel((SelectedBacType is BAC_Type22) ? new BACType22ViewModel(SelectedBacType as BAC_Type22) : null);
        public BACType23ViewModel BacType23ViewModel => (BACType23ViewModel)SetViewModel((SelectedBacType is BAC_Type23) ? new BACType23ViewModel(SelectedBacType as BAC_Type23) : null);
        public BACType24ViewModel BacType24ViewModel => (BACType24ViewModel)SetViewModel((SelectedBacType is BAC_Type24) ? new BACType24ViewModel(SelectedBacType as BAC_Type24) : null);
        public BACType25ViewModel BacType25ViewModel => (BACType25ViewModel)SetViewModel((SelectedBacType is BAC_Type25) ? new BACType25ViewModel(SelectedBacType as BAC_Type25) : null);
        public BACType26ViewModel BacType26ViewModel => (BACType26ViewModel)SetViewModel((SelectedBacType is BAC_Type26) ? new BACType26ViewModel(SelectedBacType as BAC_Type26) : null);
        public BACType27ViewModel BacType27ViewModel => (BACType27ViewModel)SetViewModel((SelectedBacType is BAC_Type27) ? new BACType27ViewModel(SelectedBacType as BAC_Type27) : null);
        public BACType28ViewModel BacType28ViewModel => (BACType28ViewModel)SetViewModel((SelectedBacType is BAC_Type28) ? new BACType28ViewModel(SelectedBacType as BAC_Type28) : null);
        public BACType29ViewModel BacType29ViewModel => (BACType29ViewModel)SetViewModel((SelectedBacType is BAC_Type29) ? new BACType29ViewModel(SelectedBacType as BAC_Type29) : null);
        public BACType30ViewModel BacType30ViewModel => (BACType30ViewModel)SetViewModel((SelectedBacType is BAC_Type30) ? new BACType30ViewModel(SelectedBacType as BAC_Type30) : null);

        #endregion

        //List Filter
        private ListCollectionView _viewbacTypes = null;
        public ListCollectionView ViewBacTypes
        {
            get
            {
                return _viewbacTypes;
            }
            set
            {
                if (value != _viewbacTypes)
                {
                    _viewbacTypes = value;
                    NotifyPropertyChanged(nameof(ViewBacTypes));
                }
            }
        }


        private void CreateFilteredList()
        {
            if (files.SelectedItem?.SelectedBacFile != null)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ViewBacTypes = new ListCollectionView(files.SelectedItem.SelectedBacFile.File.BacEntries.Binding);
                    ViewBacTypes.Filter = new Predicate<object>(BacFilterCheck);
                }));
            }
            else
            {
                ViewBacTypes = null;
            }
        }

        private void ReapplyBacFilter()
        {
            if (ViewBacTypes != null)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ViewBacTypes.Filter = new Predicate<object>(BacFilterCheck);
                    NotifyPropertyChanged(nameof(ViewBacTypes));

                    if (bacEntryDataGrid.SelectedItem != null)
                        bacEntryDataGrid.ScrollIntoView(bacEntryDataGrid.SelectedItem);
                }));
            }
        }

        private bool BacFilterCheck(object bacEntry)
        {
            if (bacEntry is BAC_Entry entry)
            {
                return SettingsManager.settings.XenoKit_HideEmptyBacEntries ? !entry.IsIBacEntryEmpty() : true;
            }
            else
            {
                return true;
            }
        }

        //Visibilities
        public Visibility BacTypeListVisbility => (bacEntryDataGrid.SelectedItem is BAC_Entry) ? Visibility.Visible : Visibility.Hidden;

        //BAC Type View
        private BacViewMode _viewMode = BacViewMode.TimeLine;
        public BacViewMode ViewMode
        {
            get => _viewMode;
            set
            {
                if (_viewMode != value)
                {
                    _viewMode = value;
                    NotifyPropertyChanged(nameof(ViewMode));
                    NotifyPropertyChanged(nameof(IsDataGridMode));
                    NotifyPropertyChanged(nameof(IsTimeLineMode));
                    BacType_SelectionChanged(this, null);
                }
            }
        }
        public bool IsDataGridMode
        {
            get => _viewMode == BacViewMode.DataGrid;
            set
            {
                ViewMode = value ? BacViewMode.DataGrid : BacViewMode.TimeLine;
            }
        }
        public bool IsTimeLineMode
        {
            get => _viewMode == BacViewMode.TimeLine;
            set
            {
                ViewMode = value ? BacViewMode.TimeLine : BacViewMode.DataGrid;
            }
        }

        #region BacEntryID
        public int SelectedBacID
        {
            get => SelectedBacEntry != null ? SelectedBacEntry.SortID : -1;
            set
            {
                if (SelectedBacEntry != null && value >= 0)
                {
                    EditBacId(value);
                }

                NotifyPropertyChanged(nameof(SelectedBacID));
            }
        }

        private async void EditBacId(int newId)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            var existing = Files.Instance.SelectedItem.SelectedBacFile.File.BacEntries.FirstOrDefault(a => a.SortID == newId && a != SelectedBacEntry);

            if (existing?.IsIBacEntryEmpty() == true)
            {
                //An entry already exists with this ID but it is empty. We can safely remove it and just use the ID
                undos.Add(new UndoableListRemove<BAC_Entry>(Files.Instance.SelectedItem.SelectedBacFile.File.BacEntries, existing));
                Files.Instance.SelectedItem.SelectedBacFile.File.BacEntries.Remove(existing);
            }

            if (existing != null && undos.Count == 0)
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "ID Already Used", "The entered ID is already used by another BAC entry.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                return;
            }
            else
            {
                undos.Add(new UndoableProperty<BAC_Entry>(nameof(BAC_Entry.SortID), SelectedBacEntry, SelectedBacEntry.SortID, newId));
                UndoManager.Instance.AddCompositeUndo(undos, "BAC ID", UndoGroup.Action, "ID", SelectedBacEntry);
                SelectedBacEntry.SortID = newId;
            }

            SelectedBacEntry.UpdateEntryName();
        }

        public int MaximumBacID
        {
            get
            {
                if (Files.Instance.SelectedMove != null)
                {
                    if (Files.Instance.SelectedMove.MoveType == Move.Type.Skill) return BAC_Entry.MAX_ENTRIES_SKILL - 1;
                    if (Files.Instance.SelectedMove.MoveType == Move.Type.Moveset) return BAC_Entry.MAX_ENTRIES_CHARACTER - 1;
                }

                return 10000;
            }
        }

        #endregion

        public BacTab()
        {
            InitializeComponent();
            DataContext = this;
            NotifyPropertyChanged("files");
            Files.SelectedItemChanged += Files_SelectedMoveChanged;
            UndoManager.Instance.UndoOrRedoCalled += UndoManager_UndoOrRedoCalled;
            SettingsManager.SettingsReloaded += SettingsManager_SettingsLoadOrSave;
            SettingsManager.SettingsSaved += SettingsManager_SettingsLoadOrSave;
            Engine.Animation.VisualSkeleton.SelectedBoneChanged += VisualSkeleton_SelectedBoneChanged;
        }

        #region BacEntryCommands
        public RelayCommand PlayBacEntryCommand => new RelayCommand(PlayBacEntry, IsBacEntrySelected);
        public void PlayBacEntry()
        {
            NotifyPropertyChanged(nameof(BacTypeListVisbility));
            if (SelectedBacEntry != null && files.SelectedItem?.SelectedBacFile != null)
            {
                SceneManager.PlayBacEntry(files.SelectedItem.SelectedBacFile.File, SelectedBacEntry, files.SelectedMove, 0, true);
            }

            //Default sorting:
            bacTypeDataGrid.Items.SortDescriptions.Clear();
            if (SettingsManager.Instance.Settings.XenoKit_BacTypeSortModeEnum == BacTypeSortMode.StartTime)
            {
                bacTypeDataGrid.Items.SortDescriptions.Add(new SortDescription("StartTime", ListSortDirection.Ascending));
            }
        }

        public RelayCommand AddBacEntryCommand => new RelayCommand(AddBacEntry, IsBacFileLoaded);
        private void AddBacEntry()
        {
            var newBacEntry = Files.Instance.SelectedItem.SelectedBacFile.File.AddNewEntry();
            SelectBacEntry(newBacEntry);
            UndoManager.Instance.AddUndo(new UndoableListAdd<BAC_Entry>(Files.Instance.SelectedMove.Files.BacFile.File.BacEntries, newBacEntry, "Bac Entry Add"));
        }

        public RelayCommand RemoveBacEntryCommand => new RelayCommand(RemoveBacEntry, IsBacEntrySelected);
        private void RemoveBacEntry()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var entry in SelectedBacEntries)
            {
                SceneManager.ForceStopBacPlayer();
                undos.Add(new UndoableListRemove<BAC_Entry>(Files.Instance.SelectedItem.SelectedBacFile.File.BacEntries, entry));
                Files.Instance.SelectedItem.SelectedBacFile.File.BacEntries.Remove(entry);
            }

            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Bac Entry Remove"));
        }

        public RelayCommand CopyBacEntryCommand => new RelayCommand(CopyBacEntry, IsBacEntrySelected);
        private void CopyBacEntry()
        {
            CopyItem copyItem = new CopyItem(SelectedBacEntries, Files.Instance.SelectedMove);
            Clipboard.SetData(ClipboardConstants.BacEntry_CopyItem, copyItem);
        }

        public RelayCommand PasteBacEntryCommand => new RelayCommand(PasteBacEntry, CanPasteBacEntries);
        private void PasteBacEntry()
        {
            CopyItem copyItem = (CopyItem)Clipboard.GetData(ClipboardConstants.BacEntry_CopyItem);
            PasteCopyItem pasteWindow = new PasteCopyItem(copyItem, Files.Instance.SelectedMove);
            pasteWindow.ShowDialog();
        }

        public RelayCommand PasteReplaceBacEntryCommand => new RelayCommand(PasteReplaceBacEntry, CanPasteReplaceBacEntries);
        private void PasteReplaceBacEntry()
        {
            CopyItem copyItem = (CopyItem)Clipboard.GetData(ClipboardConstants.BacEntry_CopyItem);
            PasteCopyItem pasteWindow = new PasteCopyItem(copyItem, Files.Instance.SelectedMove, SelectedBacEntry, true);
            pasteWindow.ShowDialog();

            UndoManager.Instance.ForceEventCall(UndoGroup.Action);
        }

        public RelayCommand EditBacFlagsCommand => new RelayCommand(EditBacFlags, IsBacFileLoaded);
        private void EditBacFlags()
        {
            Windows.BAC.EditBacEntryFlags form = new Windows.BAC.EditBacEntryFlags(SelectedBacEntry, System.Windows.Application.Current.MainWindow);
            form.ShowDialog();
        }

        public RelayCommand RebaseBacEntryCommand => new RelayCommand(RebaseBacEntry, IsBacEntrySelected);
        private void RebaseBacEntry()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (SelectedBacEntry != null)
            {
                EanModiferForm form = new EanModiferForm("Action Rebase");
                form.StartFrameEnabled = true;
                form.RebaseAmountEnabled = true;
                form.StartFrameConstraintEnabled = false;
                form.ShowDialog();

                if (form.Success)
                {
                    foreach (var type in SelectedBacEntry.IBacTypes)
                    {
                        if (type.StartTime >= form.StartFrame)
                        {
                            ushort newStartTime = (ushort)(type.StartTime + form.RebaseAmount);
                            undos.Add(new UndoableProperty<IBacType>(nameof(type.StartTime), type, type.StartTime, newStartTime));
                            type.StartTime = newStartTime;
                        }
                    }

                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Bac Entry Rebase"));
                }
            }
        }

        //Bools
        private bool IsBacFileLoaded()
        {
            if (Files.Instance.SelectedMove != null)
            {
                return Files.Instance.SelectedMove.Files.BacFile != null;
            }
            else
            {
                return false;
            }
        }

        private bool IsBacEntrySelected()
        {
            return bacEntryDataGrid.SelectedItem is BAC_Entry;
        }

        private bool IsBacTypeSelected()
        {
            return bacTypeDataGrid.SelectedItem is IBacType;
        }

        private bool CanFocus()
        {
            return SelectedBacType != null && SceneManager.Actors[0] != null;
        }

        private bool CanPasteBacEntries()
        {
            return Clipboard.ContainsData(ClipboardConstants.BacEntry_CopyItem) && IsBacFileLoaded();
        }

        private bool CanPasteReplaceBacEntries()
        {
            return Clipboard.ContainsData(ClipboardConstants.BacEntry_CopyItem) && IsBacFileLoaded() && IsBacEntrySelected();
        }

        private bool CanPasteBacTypes()
        {
            return Clipboard.ContainsData(ClipboardConstants.BacType_CopyItem) && IsBacEntrySelected();
        }
        #endregion

        #region BacTypeListCommands
        public RelayCommand<int> AddBacTypeCommand => new RelayCommand<int>(AddBacType);
        private void AddBacType(int bacType)
        {
            if (!IsBacEntrySelected()) return;
            bacTypeDataGrid.SelectedItem = SelectedBacEntry.UndoableAddIBacType(bacType);
            bacTypeDataGrid.ScrollIntoView(bacTypeDataGrid.SelectedItem);
            SceneManager.InvokeBacDataChangedEvent();
        }

        public RelayCommand RemoveBacTypeCommand => new RelayCommand(RemoveBacType, IsBacTypeSelected);
        private void RemoveBacType()
        {
            SelectedBacEntry.UndoableRemoveIBacType(SelectedBacTypes);
            SceneManager.InvokeBacDataChangedEvent();
        }

        public RelayCommand CopyBacTypeCommand => new RelayCommand(CopyBacType, IsBacTypeSelected);
        private void CopyBacType()
        {
            CopyItem copyItem = new CopyItem(SelectedBacTypes, Files.Instance.SelectedMove);
            Clipboard.SetData(ClipboardConstants.BacType_CopyItem, copyItem);
        }

        public RelayCommand PasteBacTypeCommand => new RelayCommand(PasteBacType, CanPasteBacTypes);
        private void PasteBacType()
        {
            CopyItem copyItem = (CopyItem)Clipboard.GetData(ClipboardConstants.BacType_CopyItem);
            PasteCopyItem pasteWindow = new PasteCopyItem(copyItem, Files.Instance.SelectedMove, SelectedBacEntry, false);
            pasteWindow.ShowDialog();
        }

        public RelayCommand DuplicateBacTypeCommand => new RelayCommand(DuplicateBacType, IsBacTypeSelected);
        private void DuplicateBacType()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var type in SelectedBacTypes)
            {
                undos.Add(SelectedBacEntry.AddEntry(type.Copy()));
            }

            UndoManager.Instance.AddCompositeUndo(undos, "Duplicate BacType");
        }

        public RelayCommand FocusBacTypeCommand => new RelayCommand(FocusBacType, CanFocus);
        private void FocusBacType()
        {
            SceneManager.Actors[0].ActionControl.BacPlayer.Seek(SelectedBacType.StartTime);
        }


        //Placeholder until Drag and Drop is implemented
        public RelayCommand MoveDownBacTypeCommand => new RelayCommand(MoveDownBacType, IsBacTypeSelected);
        private void MoveDownBacType()
        {
            if (SelectedBacType != null)
            {
                int idx = SelectedBacEntry.IBacTypes.IndexOf(SelectedBacType);

                if (idx < SelectedBacEntry.IBacTypes.Count - 1)
                {
                    SelectedBacEntry.IBacTypes.Move(idx, idx + 1);
                    UndoManager.Instance.AddUndo(new UndoableListMove<IBacType>(SelectedBacEntry.IBacTypes, idx, idx + 1, "BacType Move Up"));
                }
            }
        }

        public RelayCommand MoveUpBacTypeCommand => new RelayCommand(MoveUpBacType, IsBacTypeSelected);
        private void MoveUpBacType()
        {
            if (SelectedBacType != null)
            {
                int idx = SelectedBacEntry.IBacTypes.IndexOf(SelectedBacType);

                if (idx > 0)
                {
                    SelectedBacEntry.IBacTypes.Move(idx, idx - 1);
                    UndoManager.Instance.AddUndo(new UndoableListMove<IBacType>(SelectedBacEntry.IBacTypes, idx, idx - 1, "BacType Move Down"));
                }
            }
        }

        #endregion

        #region Events
        private void VisualSkeleton_SelectedBoneChanged(object sender, EventArgs e)
        {
            if (SceneManager.IsOnTab(EditorTabs.Action) && SelectedIBacBone != null)
            {
                if (sender is int boneIdx)
                {
                    string boneName = SceneManager.Actors[0].Skeleton.Bones[boneIdx].Name;
                    BoneLinks boneLink;

                    if (Enum.TryParse(boneName, out boneLink))
                    {
                        if (SelectedIBacBone.BoneLink != boneLink)
                        {
                            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(IBacBone.BoneLink), SelectedIBacBone, SelectedIBacBone.BoneLink, boneLink, "Bone Link"), UndoGroup.Action, "BoneLink");
                            SelectedIBacBone.BoneLink = boneLink;
                            UndoManager.Instance.ForceEventCall(UndoGroup.Action, "BoneLink");
                        }
                    }
                }
            }
        }

        private void UndoManager_UndoOrRedoCalled(object sender, UndoEventRaisedEventArgs e)
        {
            if (e.UndoArg == "ID" && e.UndoContext is BAC_Entry bacEntry)
            {
                bacEntry.UpdateEntryName();
            }

            if (e.UndoGroup == UndoGroup.Action && SelectedBacEntry != null)
            {
                SelectedBacEntry.RefreshIBacTypes();

                foreach (var type in SelectedBacEntry.IBacTypes)
                {
                    type.RefreshType();
                }

                if (e.UndoArg == "BoneLink")
                {
                    TryEnableGizmos();
                }

            }
        }

        private void SettingsManager_SettingsLoadOrSave(object sender, EventArgs e)
        {
            ReapplyBacFilter();
        }

        private void Files_SelectedMoveChanged(object sender, EventArgs e)
        {
            UpdateSelectedBacFile();
        }

        private void BacFileSelection_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectedBacFile();
        }

        private void BacType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(BacTypeListVisbility));
            UpdateViewModels();
            BacTypeSelectionChanged?.Invoke(this, new EventArgs());

            TryEnableGizmos();
            SetStaticBacTypes();
        }

        private void TimeLine_Toggled(object sender, RoutedEventArgs e)
        {
            //Going from DataGrid -> TimeLine we must sort the layers on the bac entry, assigning layers to any types added through the DataGrid add/paste methods.
            if (ViewMode == BacViewMode.DataGrid && SelectedBacEntry != null)
            {
                SelectedBacEntry.SortTimeLineLayers();
                timeline.BacEntryChanged();
            }

            ViewMode = timelineToggle.IsOn ? BacViewMode.TimeLine : BacViewMode.DataGrid;

        }
        #endregion

        private void UpdateSelectedBacFile()
        {
            NotifyPropertyChanged(nameof(BacTypeListVisbility));
            NotifyPropertyChanged(nameof(files));
            NotifyPropertyChanged(nameof(MaximumBacID));
            CreateFilteredList();

            if(files.SelectedItem != null)
            {
                if (files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Character || 
                    files.SelectedItem.SelectedBacFile?.FileType == Xv2CoreLib.Xenoverse2.MoveFileTypes.AFTER_BAC || 
                    (files.SelectedItem.Type == OutlinerItem.OutlinerItemType.CMN && bacFileComboBox.SelectedIndex == 0))
                {
                    nameColumn.Visibility = Visibility.Visible;
                }
                else
                {
                    nameColumn.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void UpdateViewModels()
        {
            NotifyPropertyChanged(nameof(BacType0ViewModel));
            NotifyPropertyChanged(nameof(BacType1ViewModel));
            NotifyPropertyChanged(nameof(BacType2ViewModel));
            NotifyPropertyChanged(nameof(BacType3ViewModel));
            NotifyPropertyChanged(nameof(BacType4ViewModel));
            NotifyPropertyChanged(nameof(BacType5ViewModel));
            NotifyPropertyChanged(nameof(BacType6ViewModel));
            NotifyPropertyChanged(nameof(BacType7ViewModel));
            NotifyPropertyChanged(nameof(BacType8ViewModel));
            NotifyPropertyChanged(nameof(BacType9ViewModel));
            NotifyPropertyChanged(nameof(BacType10ViewModel));
            NotifyPropertyChanged(nameof(BacType11ViewModel));
            NotifyPropertyChanged(nameof(BacType12ViewModel));
            NotifyPropertyChanged(nameof(BacType13ViewModel));
            NotifyPropertyChanged(nameof(BacType14ViewModel));
            NotifyPropertyChanged(nameof(BacType15ViewModel));
            NotifyPropertyChanged(nameof(BacType16ViewModel));
            NotifyPropertyChanged(nameof(BacType17ViewModel));
            NotifyPropertyChanged(nameof(BacType18ViewModel));
            NotifyPropertyChanged(nameof(BacType19ViewModel));
            NotifyPropertyChanged(nameof(BacType20ViewModel));
            NotifyPropertyChanged(nameof(BacType21ViewModel));
            NotifyPropertyChanged(nameof(BacType22ViewModel));
            NotifyPropertyChanged(nameof(BacType23ViewModel));
            NotifyPropertyChanged(nameof(BacType24ViewModel));
            NotifyPropertyChanged(nameof(BacType25ViewModel));
            NotifyPropertyChanged(nameof(BacType26ViewModel));
            NotifyPropertyChanged(nameof(BacType27ViewModel));
            NotifyPropertyChanged(nameof(BacType28ViewModel));
            NotifyPropertyChanged(nameof(BacType29ViewModel));
            NotifyPropertyChanged(nameof(BacType30ViewModel));
            NotifyPropertyChanged(nameof(BacTypeBaseViewModel));
        }

        public void TryEnableGizmos()
        {
            if (SceneManager.MainGameInstance == null) return;

            if (SelectedBacType is BAC_Type1 hitbox)
            {
                SceneManager.MainGameInstance.BacHitboxGizmo.SetContext(hitbox);
            }
            else
            {
                SceneManager.MainGameInstance.BacHitboxGizmo.RemoveContext();
            }

            if (SelectedBacType is IBacTypeMatrix bacMatrix)
            {
                bool ignoreRotationOnBaseBone = SelectedBacType is BAC_Type1;
                bool rotationEnabled = SelectedBacType is BAC_Type8 || SelectedBacType is BAC_Type9;
                string boneName = null;

                if (SelectedBacType is IBacBone iBone)
                    boneName = iBone.BoneLink.ToString();

                //Movement is always on the base bone
                if (SelectedBacType is BAC_Type2)
                    boneName = Xv2CoreLib.ESK.ESK_File.BaseBone;

                SceneManager.MainGameInstance.GetBacMatrixGizmo().SetContext(bacMatrix, true, rotationEnabled, false, boneName, ignoreRotationOnBaseBone, EditorTabs.Action);
            }
            else
            {
                SceneManager.MainGameInstance.GetBacMatrixGizmo().RemoveContext();
            }
        }

        private void SetStaticBacTypes()
        {
            if (SelectedBacType is IBacBone bacBone)
            {
                SelectedIBacBone = bacBone;
            }
            else
            {
                SelectedIBacBone = null;
            }

            StaticSelectedBacType = SelectedBacType;
        }

        private void SelectBacEntry(BAC_Entry entry)
        {
            bacEntryDataGrid.SelectedItem = entry;
            bacEntryDataGrid.ScrollIntoView(entry);
        }

        public void AutoPlayBacEntry()
        {
            //Auto plays selected bac entry IF its not already active. Used for swapping tabs.

            if(SceneManager.Actors[0] != null)
            {
                if (!SceneManager.Actors[0].ActionControl.IsBacEntryActive(SelectedBacEntry))
                {
                    PlayBacEntry();
                }
            }
        }

    }

    public enum BacViewMode
    {
        DataGrid,
        TimeLine
    }
}
