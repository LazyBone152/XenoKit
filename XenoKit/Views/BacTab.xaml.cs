using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using XenoKit.Editor;
using Xv2CoreLib.BAC;
using XenoKit.ViewModel.BAC;
using GalaSoft.MvvmLight.CommandWpf;
using Xv2CoreLib.Resource.UndoRedo;
using XenoKit.Windows;
using XenoKit.Engine;
using Xv2CoreLib.Resource.App;

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
        private IList<IBacType> SelectedBacTypes { get { return bacTypeDataGrid.SelectedItems.Cast<IBacType>().ToList();  } }

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
                }));
            }
        }

        private bool BacFilterCheck(object bacEntry)
        {
            if(bacEntry is BAC_Entry entry)
            {
                return (SettingsManager.settings.XenoKit_HideEmptyBacEntries) ? !entry.IsIBacEntryEmpty() : true;
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

        #region Commands
        //Bac Entry
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
            
            foreach(var entry in SelectedBacEntries)
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

        //Bac Type
        public RelayCommand<int> AddBacTypeCommand => new RelayCommand<int>(AddBacType);
        private void AddBacType(int bacType)
        {
            if (!IsBacEntrySelected()) return;
            SelectedBacEntry.UndoableAddIBacType(bacType);
            SceneManager.InvokeBacValuesChangedEvent();
        }

        public RelayCommand RemoveBacTypeCommand => new RelayCommand(RemoveBacType, IsBacTypeSelected);
        private void RemoveBacType()
        {
            SelectedBacEntry.UndoableRemoveIBacType(SelectedBacTypes);
            SceneManager.InvokeBacValuesChangedEvent();
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
            PasteCopyItem pasteWindow = new PasteCopyItem(copyItem, Files.Instance.SelectedMove, SelectedBacEntry);
            pasteWindow.ShowDialog();
        }



        //Bools
        private bool IsBacFileLoaded()
        {
            if(Files.Instance.SelectedMove != null)
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
        
        private bool CanPasteBacEntries()
        {
            return Clipboard.ContainsData(ClipboardConstants.BacEntry_CopyItem) && IsBacFileLoaded();
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
            Files.SelectedMoveChanged += Files_SelectedMoveChanged;
            SettingsManager.SettingsReloaded += SettingsManager_SettingsReloaded;
        }

        private void SettingsManager_SettingsReloaded(object sender, EventArgs e)
        {
            ReapplyBacFilter();
        }

        private void Files_SelectedMoveChanged(object sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(BacTypeListVisbility));
            NotifyPropertyChanged(nameof(files));
            CreateFilteredList();
        }

        private void BacEntryDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(BacTypeListVisbility));
            if (bacEntryDataGrid.SelectedItem is BAC_Entry bacEntry)
            {
                SceneManager.PlayBacEntry(files.SelectedMove.Files.BacFile.File, bacEntry, files.SelectedMove, 0, true);
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
            NotifyPropertyChanged("BacTypeBaseViewModel");
        }
        
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(BacTypeListVisbility));
            UpdateViewModels();
            BacTypeSelectionChanged?.Invoke(this, new EventArgs());
        }

        private void SelectBacEntry(BAC_Entry entry)
        {
            bacEntryDataGrid.SelectedItem = entry;
            bacEntryDataGrid.ScrollIntoView(entry);
        }
    }
}
