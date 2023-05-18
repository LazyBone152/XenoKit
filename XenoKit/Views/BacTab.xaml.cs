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
        public Files files { get { return Files.Instance; } }

        private BAC_Entry SelectedBacEntry { get { return bacEntryDataGrid.SelectedItem as BAC_Entry; } }
        private IList<BAC_Entry> SelectedBacEntries { get { return bacEntryDataGrid.SelectedItems.Cast<BAC_Entry>().ToList(); } }
        private IBacType SelectedBacType { get { return bacTypeDataGrid.SelectedItem as IBacType; } }
        private IList<IBacType> SelectedBacTypes { get { return bacTypeDataGrid.SelectedItems.Cast<IBacType>().ToList(); } }

        //Selected BacTypes exposed as statics for gizmos to check against
        public static IBacBone SelectedIBacBone { get; private set; }
        public static IBacType StaticSelectedBacType { get; set; }

        //ViewModels
        public BACTypeBaseViewModel BacTypeBaseViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_TypeBase) ? new BACTypeBaseViewModel(bacTypeDataGrid.SelectedItem as BAC_TypeBase) : null;
            }
        }
        public BACType0ViewModel BacType0ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type0) ? new BACType0ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type0) : null;
            }
        }
        public BACType1ViewModel BacType1ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type1) ? new BACType1ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type1) : null;
            }
        }
        public BACType2ViewModel BacType2ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type2) ? new BACType2ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type2) : null;
            }
        }
        public BACType3ViewModel BacType3ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type3) ? new BACType3ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type3) : null;
            }
        }
        public BACType4ViewModel BacType4ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type4) ? new BACType4ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type4) : null;
            }
        }
        public BACType5ViewModel BacType5ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type5) ? new BACType5ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type5) : null;
            }
        }
        public BACType6ViewModel BacType6ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type6) ? new BACType6ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type6) : null;
            }
        }
        public BACType7ViewModel BacType7ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type7) ? new BACType7ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type7) : null;
            }
        }
        public BACType8ViewModel BacType8ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type8) ? new BACType8ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type8) : null;
            }
        }
        public BACType9ViewModel BacType9ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type9) ? new BACType9ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type9) : null;
            }
        }
        public BACType10ViewModel BacType10ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type10) ? new BACType10ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type10) : null;
            }
        }
        public BACType11ViewModel BacType11ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type11) ? new BACType11ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type11) : null;
            }
        }
        public BACType12ViewModel BacType12ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type12) ? new BACType12ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type12) : null;
            }
        }
        public BACType13ViewModel BacType13ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type13) ? new BACType13ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type13) : null;
            }
        }
        public BACType14ViewModel BacType14ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type14) ? new BACType14ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type14) : null;
            }
        }
        public BACType15ViewModel BacType15ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type15) ? new BACType15ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type15) : null;
            }
        }
        public BACType16ViewModel BacType16ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type16) ? new BACType16ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type16) : null;
            }
        }
        public BACType17ViewModel BacType17ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type17) ? new BACType17ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type17) : null;
            }
        }
        public BACType18ViewModel BacType18ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type18) ? new BACType18ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type18) : null;
            }
        }
        public BACType19ViewModel BacType19ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type19) ? new BACType19ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type19) : null;
            }
        }
        public BACType20ViewModel BacType20ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type20) ? new BACType20ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type20) : null;
            }
        }
        public BACType21ViewModel BacType21ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type21) ? new BACType21ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type21) : null;
            }
        }
        public BACType22ViewModel BacType22ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type22) ? new BACType22ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type22) : null;
            }
        }
        public BACType23ViewModel BacType23ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type23) ? new BACType23ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type23) : null;
            }
        }
        public BACType24ViewModel BacType24ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type24) ? new BACType24ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type24) : null;
            }
        }
        public BACType25ViewModel BacType25ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type25) ? new BACType25ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type25) : null;
            }
        }
        public BACType26ViewModel BacType26ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type26) ? new BACType26ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type26) : null;
            }
        }
        public BACType27ViewModel BacType27ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type27) ? new BACType27ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type27) : null;
            }
        }
        public BACType28ViewModel BacType28ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type28) ? new BACType28ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type28) : null;
            }
        }
        public BACType29ViewModel BacType29ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type29) ? new BACType29ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type29) : null;
            }
        }
        public BACType30ViewModel BacType30ViewModel
        {
            get
            {
                return (bacTypeDataGrid.SelectedItem is BAC_Type30) ? new BACType30ViewModel(bacTypeDataGrid.SelectedItem as BAC_Type30) : null;
            }
        }


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
            if (files.SelectedMove?.Files?.BacFile?.File?.BacEntries != null)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ViewBacTypes = new ListCollectionView(files.SelectedMove.Files.BacFile.File.BacEntries.Binding);
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
        public Visibility BacTypeListVisbility
        {
            get
            {
                return (bacEntryDataGrid.SelectedItem is BAC_Entry bacEntry) ? Visibility.Visible : Visibility.Hidden;
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
            var existing = Files.Instance.SelectedMove.Files.BacFile.File.BacEntries.FirstOrDefault(a => a.SortID == newId && a != SelectedBacEntry);

            if (existing?.IsIBacEntryEmpty() == true)
            {
                //An entry already exists with this ID but it is empty. We can safely remove it and just use the ID
                undos.Add(new UndoableListRemove<BAC_Entry>(Files.Instance.SelectedMove.Files.BacFile.File.BacEntries, existing));
                Files.Instance.SelectedMove.Files.BacFile.File.BacEntries.Remove(existing);
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

        #region Commands
        //Bac Entry
        public RelayCommand PlayBacEntryCommand => new RelayCommand(PlayBacEntry, IsBacEntrySelected);
        public void PlayBacEntry()
        {
            NotifyPropertyChanged(nameof(BacTypeListVisbility));
            if (bacEntryDataGrid.SelectedItem is BAC_Entry bacEntry)
            {
                SceneManager.PlayBacEntry(files.SelectedMove.Files.BacFile.File, bacEntry, files.SelectedMove, 0, true);
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
            var newBacEntry = Files.Instance.SelectedMove.Files.BacFile.File.AddNewEntry();
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
                undos.Add(new UndoableListRemove<BAC_Entry>(Files.Instance.SelectedMove.Files.BacFile.File.BacEntries, entry));
                Files.Instance.SelectedMove.Files.BacFile.File.BacEntries.Remove(entry);
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


        //Bac Type
        public RelayCommand<int> AddBacTypeCommand => new RelayCommand<int>(AddBacType);
        private void AddBacType(int bacType)
        {
            if (!IsBacEntrySelected()) return;
            SelectedBacEntry.UndoableAddIBacType(bacType);
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
            return bacTypeDataGrid.SelectedItem is IBacType && SceneManager.Actors[0] != null;
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
            NotifyPropertyChanged(nameof(BacTypeListVisbility));
            NotifyPropertyChanged(nameof(files));
            NotifyPropertyChanged(nameof(MaximumBacID));
            CreateFilteredList();

            if (files.SelectedItem?.Type == OutlinerItem.OutlinerItemType.Character || files.SelectedItem?.Type == OutlinerItem.OutlinerItemType.Moveset)
            {
                nameColumn.Visibility = Visibility.Visible;
            }
            else
            {
                nameColumn.Visibility = Visibility.Collapsed;
            }
        }

        private void BacEntryDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SceneManager.IsOnTab(EditorTabs.Action))
            {
                PlayBacEntry();
            }
        }

        private void UpdateViewModels()
        {
            NotifyPropertyChanged("BacType0ViewModel");
            NotifyPropertyChanged("BacType1ViewModel");
            NotifyPropertyChanged("BacType2ViewModel");
            NotifyPropertyChanged("BacType3ViewModel");
            NotifyPropertyChanged("BacType4ViewModel");
            NotifyPropertyChanged("BacType5ViewModel");
            NotifyPropertyChanged("BacType6ViewModel");
            NotifyPropertyChanged("BacType7ViewModel");
            NotifyPropertyChanged("BacType8ViewModel");
            NotifyPropertyChanged("BacType9ViewModel");
            NotifyPropertyChanged("BacType10ViewModel");
            NotifyPropertyChanged("BacType11ViewModel");
            NotifyPropertyChanged("BacType12ViewModel");
            NotifyPropertyChanged("BacType13ViewModel");
            NotifyPropertyChanged("BacType14ViewModel");
            NotifyPropertyChanged("BacType15ViewModel");
            NotifyPropertyChanged("BacType16ViewModel");
            NotifyPropertyChanged("BacType17ViewModel");
            NotifyPropertyChanged("BacType18ViewModel");
            NotifyPropertyChanged("BacType19ViewModel");
            NotifyPropertyChanged("BacType20ViewModel");
            NotifyPropertyChanged("BacType21ViewModel");
            NotifyPropertyChanged("BacType22ViewModel");
            NotifyPropertyChanged("BacType23ViewModel");
            NotifyPropertyChanged("BacType24ViewModel");
            NotifyPropertyChanged("BacType25ViewModel");
            NotifyPropertyChanged("BacType26ViewModel");
            NotifyPropertyChanged(nameof(BacType27ViewModel));
            NotifyPropertyChanged(nameof(BacType28ViewModel));
            NotifyPropertyChanged(nameof(BacType29ViewModel));
            NotifyPropertyChanged(nameof(BacType30ViewModel));
            NotifyPropertyChanged(nameof(BacTypeBaseViewModel));
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(BacTypeListVisbility));
            UpdateViewModels();
            BacTypeSelectionChanged?.Invoke(this, new EventArgs());

            TryEnableGizmos();
            SetStaticBacTypes();
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
}
