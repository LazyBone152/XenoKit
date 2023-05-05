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
using XenoKit.Editor;
using Xv2CoreLib;
using Xv2CoreLib.ACB;
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

        private Xv2MoveFiles moveFiles;
        private OutlinerItem outlinerItem;

        public ObservableCollection<object> MainFiles { get; set; } = new ObservableCollection<object>();
        public ObservableCollection<Xv2File<ACB_Wrapper>> AcbFiles { get; set; } = new ObservableCollection<Xv2File<ACB_Wrapper>>();

        public OutlinerItemProperties(OutlinerItem item)
        {
            moveFiles = item.GetMove().Files;
            outlinerItem = item;
            Owner = Application.Current.MainWindow;
            InitializeComponent();
            DataContext = this;
            InitFiles();

            if(outlinerItem.Type == OutlinerItem.OutlinerItemType.Character)
            {
                //Hide charaCode column when item is a character
                charaCodeColumn_Main.Visibility = Visibility.Collapsed;

                //Hide it on the audio tab only if not a CaC, since the code is used there for the different cac voices
                if (!(outlinerItem.character.CharacterData.CmsEntry.ID >= 100 && outlinerItem.character.CharacterData.CmsEntry.ID <= 107))
                {
                    charaCodeColumn_Audio.Visibility = Visibility.Collapsed;
                }
            }
        }

        public void InitFiles()
        {
            MainFiles.Clear();
            moveFiles.UpdateTypeStrings();

            if(moveFiles.BacFile != null) MainFiles.Add(moveFiles.BacFile);
            if (moveFiles.BdmFile != null) MainFiles.Add(moveFiles.BdmFile);
            if (moveFiles.ShotBdmFile != null) MainFiles.Add(moveFiles.ShotBdmFile);
            if (moveFiles.BsaFile != null) MainFiles.Add(moveFiles.BsaFile);
            if (moveFiles.EepkFile != null) MainFiles.Add(moveFiles.EepkFile);
            if (moveFiles.BcmFile != null) MainFiles.Add(moveFiles.BcmFile);

            foreach (var file in moveFiles.EanFile)
                MainFiles.Add(file);

            foreach (var file in moveFiles.CamEanFile)
                MainFiles.Add(file);

            foreach (var file in moveFiles.SeAcbFile)
                AcbFiles.Add(file);

            foreach (var file in moveFiles.VoxAcbFile)
                AcbFiles.Add(file);

        }

        private void BreakShareLinkBase(object file)
        {
            if (outlinerItem?.character?.CharacterData?.IsCaC == true)
            {
                MessageBox.Show("Operation not available for CACs!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (file != null)
            {
                if(MessageBox.Show("This will create a new unique copy of this file, ensuring that any further edits will not affect its original instance.\n\n(This is only for files that are \"borrowed\" from other skills or characters, does nothing otherwise)", "Break Share Link", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }

                List<IUndoRedo> undos = moveFiles.UnborrowFile(file);

                if (undos.Count > 0)
                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Break Share Link"));
            }

        }

        private void ReplaceFileBase(object file, bool isAcb)
        {
            if (file != null)
            {
                OpenFileDialog openFile = new OpenFileDialog();
                openFile.Title = "Replace file...";

                if (isAcb)
                {
                    openFile.Filter = "ACB Files | *acb";
                }
                else
                {
                    openFile.Filter = "XV2 File | *.bac; *bcm; *bsa; *bdm; *eepk; *bas; *ean";
                }

                if (openFile.ShowDialog() == true && File.Exists(openFile.FileName))
                {

                    var undos = moveFiles.ReplaceFile(file, openFile.FileName);

                    if (undos.Count > 0)
                        UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Replace File"));
                }
            }
        }

        #region MainCommands
        public RelayCommand BreakShareLinkCommand => new RelayCommand(BreakShareLink);
        private void BreakShareLink()
        {
            BreakShareLinkBase(dataGrid.SelectedItem);
        }

        public RelayCommand ReplaceFileCommand => new RelayCommand(ReplaceFile);
        private void ReplaceFile()
        {
            ReplaceFileBase(dataGrid.SelectedItem, false);
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
        #endregion

        #region AcbCommands
        public RelayCommand AcbBreakShareLinkCommand => new RelayCommand(AcbBreakShareLink);
        private void AcbBreakShareLink()
        {
            BreakShareLinkBase(audioDataGrid.SelectedItem);
        }

        public RelayCommand AcbReplaceFileCommand => new RelayCommand(AcbReplaceFile);
        private void AcbReplaceFile()
        {
            ReplaceFileBase(audioDataGrid.SelectedItem, true);
        }

        #endregion
    }
}
