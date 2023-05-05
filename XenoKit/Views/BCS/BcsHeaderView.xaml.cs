using System;
using System.ComponentModel;
using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.ViewModel.BCS;
using Xv2CoreLib.BCS;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Views
{
    /// <summary>
    /// Interaction logic for BcsHeaderView.xaml
    /// </summary>
    public partial class BcsHeaderView : UserControl, INotifyPropertyChanged
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
        
        public BCS_File BcsFile => Files.Instance.SelectedItem?.character?.CharacterData?.BcsFile?.File;

        public BcsHeaderViewModel ViewModel { get; private set; }

        public BcsHeaderView()
        {
            DataContext = this;
            InitializeComponent();
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            Files.SelectedItemChanged += Files_SelectedItemChanged; ;
        }

        private void Files_SelectedItemChanged(object sender, EventArgs e)
        {
            ViewModel = BcsFile != null ? new BcsHeaderViewModel(BcsFile) : null;
            NotifyPropertyChanged(nameof(ViewModel));
        }

        private void Instance_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {
            ViewModel?.UpdateProperties();
        }
    }
}
