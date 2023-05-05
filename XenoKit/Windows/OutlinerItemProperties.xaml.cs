using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Shapes;
using Xv2CoreLib;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Windows
{
    /// <summary>
    /// Interaction logic for OutlinerItemProperties.xaml
    /// </summary>
    public partial class OutlinerItemProperties : MetroWindow, INotifyPropertyChanged
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

        Xv2MoveFiles moveFiles;

        public ObservableCollection<object> Files { get; set; } = new ObservableCollection<object>();
        

        public OutlinerItemProperties(Xv2MoveFiles moveFiles)
        {
            this.moveFiles = moveFiles;
            Owner = Application.Current.MainWindow;
            InitializeComponent();
            DataContext = this;
            InitFiles();
        }

        public void InitFiles()
        {
            Files.Clear();
            moveFiles.UpdateTypeStrings();

            if(moveFiles.BacFile != null) Files.Add(moveFiles.BacFile);
            if (moveFiles.BdmFile != null) Files.Add(moveFiles.BdmFile);
            if (moveFiles.ShotBdmFile != null) Files.Add(moveFiles.ShotBdmFile);
            if (moveFiles.BsaFile != null) Files.Add(moveFiles.BsaFile);
            if (moveFiles.EepkFile != null) Files.Add(moveFiles.EepkFile);
            if (moveFiles.SeAcbFile != null) Files.Add(moveFiles.SeAcbFile);
            if (moveFiles.BcmFile != null) Files.Add(moveFiles.BcmFile);

            foreach (var file in moveFiles.EanFile)
                Files.Add(file);

            foreach (var file in moveFiles.CamEanFile)
                Files.Add(file);
            
            foreach (var file in moveFiles.VoxAcbFile)
                Files.Add(file);
            
        }

        //Commands
        public RelayCommand BreakShareLinkCommand => new RelayCommand(BreakShareLink);
        private void BreakShareLink()
        {
            if (dataGrid.SelectedItem != null)
            {
                var undos = moveFiles.UnborrowFile(dataGrid.SelectedItem);

                if(undos.Count > 0)
                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Break Share Link"));
            }

        }

        public RelayCommand ReplaceFileCommand => new RelayCommand(ReplaceFile);
        private void ReplaceFile()
        {

            if (dataGrid.SelectedItem != null)
            {
                OpenFileDialog openFile = new OpenFileDialog();
                openFile.Title = "Replace file...";
                openFile.Filter = "XV2 File | *.bac; *bcm; *bsa; *bdm; *eepk; *acb; *bas; *ean";

                if (openFile.ShowDialog() == true && File.Exists(openFile.FileName))
                {

                    var undos = moveFiles.ReplaceFile(dataGrid.SelectedItem, openFile.FileName);

                    if (undos.Count > 0)
                        UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Replace File"));
                }
            }
        }

        public RelayCommand RemoveFileCommand => new RelayCommand(RemoveFile, CanRemoveFile);
        private void RemoveFile()
        {
            if (dataGrid.SelectedItem != null)
            {
                var undos = moveFiles.RemoveFile(dataGrid.SelectedItem);
                undos.Add(new UndoActionDelegate(this, nameof(InitFiles), true));
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Remove File"));
                InitFiles();
            }
        }

        private bool CanRemoveFile()
        {
            return (dataGrid.SelectedItem != null) ? moveFiles.CanRemoveFile(dataGrid.SelectedItem) : false;
        }

    }
}
