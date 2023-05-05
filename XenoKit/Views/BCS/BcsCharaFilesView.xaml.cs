using System;
using System.Windows;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Microsoft.Win32;
using XenoKit.Editor;
using XenoKit.Engine;
using Xv2CoreLib;
using Xv2CoreLib.BCS;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMM;
using Xv2CoreLib.Resource.UndoRedo;
using EEPK_Organiser.Forms;
using MahApps.Metro.Controls.Dialogs;
using GalaSoft.MvvmLight.CommandWpf;
using Xv2CoreLib.EMD;
using System.Threading.Tasks;

namespace XenoKit.Views
{
    /// <summary>
    /// Interaction logic for BcsCharaFilesView.xaml
    /// </summary>
    public partial class BcsCharaFilesView : UserControl, INotifyPropertyChanged
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

        public Xv2Character Character => Files.Instance.SelectedItem?.character?.CharacterData;

        public Xv2PartSetFile SelectedFile { get; set; }

        public string SelectedFileName
        {
            get => SelectedFile != null ? Path.GetFileNameWithoutExtension(SelectedFile.Name) : null;
            set
            {
                if(SelectedFile != null)
                {
                    RenameSelectedFile(value);
                }
            }
        }

        
        public BcsCharaFilesView()
        {
            DataContext = this;
            InitializeComponent();
            Files.SelectedItemChanged += Files_SelectedMoveChanged;
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        public async void RenameSelectedFile(string newName)
        {
            string fullNewName = newName + Path.GetExtension(SelectedFile.Name);

            //Check if name can be used
            if (Character.CheckCharaFilePathIsReserved(fullNewName))
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "Name Is Reserved", string.Format("The file \"{0}\" cannot be added because it would replace a vital character file.\n\nIf you must add this file then rename it first.", fullNewName), MessageDialogStyle.Affirmative, DialogSettings.Default);
                return;
            }

            if (Character.PartSetFiles.Any(x => x.Name.Equals(fullNewName, StringComparison.OrdinalIgnoreCase) && x != SelectedFile))
            {
                string autoName = Character.GetUnusedCharaFileName(fullNewName);
                var result = await DialogCoordinator.Instance.ShowMessageAsync(this, string.Format("Rename file to \"{0}\"?", autoName), "The entered name is already used by another file.", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);

                if (result == MessageDialogResult.Affirmative)
                {
                    fullNewName = autoName;
                }
                else
                {
                    return;
                }
            }

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(SelectedFile.Name), SelectedFile, SelectedFile.Name, fullNewName, "File Name"), UndoGroup.BCS);
            SelectedFile.Name = fullNewName;

            NotifyPropertyChanged(nameof(SelectedFileName));
            SelectedFile?.RefreshValues();
        }

        private void Instance_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {
            if(Character != null)
            {
                foreach (var file in Character.PartSetFiles)
                {
                    file.RefreshValues();
                }
            }
        }

        private void Files_SelectedMoveChanged(object sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(Character));

            FilteredFiles = null;
            NotifyPropertyChanged(nameof(FilteredFiles));
        }

        #region FilteredFileList
        private string _searchFilter = null;
        public string SearchFilter
        {
            get
            {
                return this._searchFilter;
            }
            set
            {
                if (value != this._searchFilter)
                {
                    this._searchFilter = value;
                    NotifyPropertyChanged(nameof(SearchFilter));
                }
            }
        }

        [NonSerialized]
        private ListCollectionView _filteredFiles = null;
        public ListCollectionView FilteredFiles
        {
            get
            {
                if (Character == null) return null;
                if (_filteredFiles != null && _filteredFiles?.SourceCollection == Character?.PartSetFiles)
                {
                    return _filteredFiles;
                }
                _filteredFiles = new ListCollectionView(Character.PartSetFiles.Binding);
                _filteredFiles.Filter = new Predicate<object>(FilesFilterCheck);
                return _filteredFiles;
            }
            set
            {
                if (value != _filteredFiles)
                {
                    _filteredFiles = value;
                    NotifyPropertyChanged(nameof(FilteredFiles));
                }
            }
        }

        public bool FilesFilterCheck(object file)
        {
            if (String.IsNullOrWhiteSpace(SearchFilter)) return true;

            if (file is Xv2PartSetFile partSetFile)
            {
                if (partSetFile.Name.IndexOf(SearchFilter, StringComparison.OrdinalIgnoreCase) != -1) return true;
            }

            return false;
        }

        public RelayCommand ClearSearchCommand => new RelayCommand(ClearSearch);
        private void ClearSearch()
        {
            if (Character == null) return;

            SearchFilter = string.Empty;
            UpdateFilesFilter();
        }

        public RelayCommand SearchCommand => new RelayCommand(Search);
        private void Search()
        {
            if (Character == null) return;
            UpdateFilesFilter();
        }

        private void UpdateFilesFilter()
        {
            if (Character == null) return;
            if (_filteredFiles == null)
                _filteredFiles = new ListCollectionView(Character.PartSetFiles.Binding);

            _filteredFiles.CommitEdit();
            _filteredFiles.Filter = new Predicate<object>(FilesFilterCheck);
            NotifyPropertyChanged(nameof(FilteredFiles));
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Search();
        }

        #endregion

        #region Commands
        public RelayCommand AddFileCommand => new RelayCommand(AddFile, IsCharaSelected);
        private void AddFile()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Add Chara File...";
            openFile.Filter = "XV2 Chara File | *.emd; *emb; *emm; *ean; *esk; *scd";
            openFile.Multiselect = true;

            if (openFile.ShowDialog() == true)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach(var filePath in openFile.FileNames)
                {
                    if (Character.CheckCharaFilePathIsReserved(Path.GetFileName(filePath))) continue;

                    if (File.Exists(filePath))
                    {
                        undos.Add(Character.AddCharaFile(filePath, true));
                    }
                }

                UndoManager.Instance.AddCompositeUndo(undos, openFile.FileNames.Length > 1 ? "Add Chara Files" : "Add Chara File", UndoGroup.BCS);
            }
        }

        public RelayCommand CopyFileCommand => new RelayCommand(CopyFile, IsFileSelected);
        private void CopyFile()
        {
            List<Xv2PartSetFile> selectedItems = filesDataGrid.SelectedItems.Cast<Xv2PartSetFile>().ToList();

            for (int i = 0; i < selectedItems.Count; i++)
            {
                selectedItems[i].Load(false);
                selectedItems[i] = selectedItems[i].SoftCopy();
            }

            Clipboard.SetData(ClipboardConstants.BcsCharaFile, selectedItems);
        }

        public RelayCommand PasteFileCommand => new RelayCommand(PasteFile, CanPasteFile);
        private void PasteFile()
        {
            List<Xv2PartSetFile> files = (List<Xv2PartSetFile>)Clipboard.GetData(ClipboardConstants.BcsCharaFile);
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(var file in files)
            {
                file.Name = Character.GetUnusedCharaFileName(file.Name);
                file.Owner = Character;
                file.IsEdited = true;

                undos.Add(new UndoableListAdd<Xv2PartSetFile>(Character.PartSetFiles, file));
                Character.PartSetFiles.Add(file);
            }

            UndoManager.Instance.AddCompositeUndo(undos, files.Count > 1 ? "Paste Chara Files" : "Paste Chara File", UndoGroup.BCS);
        }

        public RelayCommand DuplicateFileCommand => new RelayCommand(DuplicateFile, IsFileSelected);
        private void DuplicateFile()
        {
            var selectedItems = filesDataGrid.SelectedItems.Cast<Xv2PartSetFile>().ToList();

            List<IUndoRedo> undos = new List<IUndoRedo>();

            for (int i = 0; i < selectedItems.Count; i++)
            {
                selectedItems[i].Load(false);
                selectedItems[i] = selectedItems[i].HardCopy();
                selectedItems[i].Owner = Character;
                selectedItems[i].Name = Character.GetUnusedCharaFileName(selectedItems[i].Name);
                selectedItems[i].IsEdited = true;

                undos.Add(new UndoableListAdd<Xv2PartSetFile>(Character.PartSetFiles, selectedItems[i]));
                Character.PartSetFiles.Add(selectedItems[i]);
            }

            UndoManager.Instance.AddCompositeUndo(undos, selectedItems.Count > 1 ? "Duplicate Chara Files" : "Duplicate Chara File", UndoGroup.BCS);
        }

        public RelayCommand DeleteFileCommand => new RelayCommand(DeleteFile, IsFileSelected);
        private void DeleteFile()
        {
            var selectedItems = filesDataGrid.SelectedItems.Cast<Xv2PartSetFile>().ToList();

            List<IUndoRedo> undos = new List<IUndoRedo>();

            for (int i = 0; i < selectedItems.Count; i++)
            {
                undos.Add(new UndoableListRemove<Xv2PartSetFile>(Character.PartSetFiles, selectedItems[i], Character.PartSetFiles.IndexOf(selectedItems[i])));
                Character.PartSetFiles.Remove(selectedItems[i]);
            }

            UndoManager.Instance.AddCompositeUndo(undos, selectedItems.Count > 1 ? "Delete Chara Files" : "Delete Chara File", UndoGroup.BCS);
        }
        
        public RelayCommand SaveFileCommand => new RelayCommand(SaveFile, CanSaveFile);
        private void SaveFile()
        {
            var selectedItems = filesDataGrid.SelectedItems.Cast<Xv2PartSetFile>().ToList();

            for (int i = 0; i < selectedItems.Count; i++)
            {
                if(selectedItems[i].IsLoaded)
                    selectedItems[i].Save();
            }
        }

        public RelayCommand EditFileCommand => new RelayCommand(EditFile, CanEditFile);
        private async void EditFile()
        {
            SelectedFile.Load(false);
            SelectedFile.IsEdited = true;

            Window window = null;

            if (SelectedFile.FileType == Xv2PartSetFile.Type.DYT_EMB || SelectedFile.FileType == Xv2PartSetFile.Type.EMB)
            {
                if(SelectedFile.File is EMB_File embFile)
                {
                    window = GetActiveEmbForm(embFile);

                    if (window == null)
                    {
                        window = new EmbEditForm(embFile, EEPK_Organiser.View.TextureEditorType.Character, SelectedFile.Name);
                        window.Show();
                    }
                }
            }
            else if(SelectedFile.FileType == Xv2PartSetFile.Type.EMM)
            {
                if (SelectedFile.File is EMM_File emmFile)
                {
                    window = GetActiveEmmForm(emmFile);

                    if (window == null)
                    {
                        window = new MaterialsEditorForm(emmFile, SelectedFile.Name);
                        window.Show();
                    }
                }
            }
            else if (SelectedFile.FileType == Xv2PartSetFile.Type.EMD)
            {
                if (SelectedFile.File is EMD_File emdFile)
                {
                    window = GetActiveEmdForm(emdFile);

                    if (window == null)
                    {
                        window = new EmdViewer(emdFile, SelectedFile.Name, Character);
                        window.Show();
                    }
                }
            }

            //Set focus to window if it is already open
            if (window != null)
            {
                await Task.Delay(200);
                window.Focus();
                
                //If still not focused, try again
                if (!window.IsFocused)
                {
                    await Task.Delay(400);
                    window.Focus();
                }
            }
        }


        private bool CanPasteFile()
        {
            return Clipboard.ContainsData(ClipboardConstants.BcsCharaFile);
        }

        private bool IsFileSelected()
        {
            return SelectedFile != null;
        }

        private bool IsCharaSelected()
        {
            return Character != null;
        }
        
        private bool CanSaveFile()
        {
            if (!IsFileSelected()) return false;
            return SelectedFile.IsLoaded;
        }

        private bool CanEditFile()
        {
            if (!IsFileSelected()) return false;
            return SelectedFile.FileType == Xv2PartSetFile.Type.EMB || SelectedFile.FileType == Xv2PartSetFile.Type.DYT_EMB || SelectedFile.FileType == Xv2PartSetFile.Type.EMM || SelectedFile.FileType == Xv2PartSetFile.Type.EMD;
        }
        #endregion

        public EmbEditForm GetActiveEmbForm(EMB_File _embFile)
        {
            foreach (var window in App.Current.Windows)
            {
                if (window is EmbEditForm)
                {
                    EmbEditForm _form = (EmbEditForm)window;

                    if (_form.EmbFile == _embFile)
                        return _form;
                }
            }

            return null;
        }

        public MaterialsEditorForm GetActiveEmmForm(EMM_File _emmFile)
        {
            foreach (var window in App.Current.Windows)
            {
                if (window is MaterialsEditorForm)
                {
                    MaterialsEditorForm _form = (MaterialsEditorForm)window;

                    if (_form.EmmFile == _emmFile)
                        return _form;
                }
            }

            return null;
        }
        
        public EmdViewer GetActiveEmdForm(EMD_File _emdFile)
        {
            foreach (var window in App.Current.Windows)
            {
                if (window is EmdViewer)
                {
                    EmdViewer _form = (EmdViewer)window;

                    if (_form.EmdFile == _emdFile)
                        return _form;
                }
            }

            return null;
        }

        private void filesDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!CanEditFile()) return;
            DependencyObject dp = (DependencyObject)e.OriginalSource;

            while ((dp != null) & !(dp is DataGridCell) & !(dp is DataGridColumnHeader))
            {
                dp = VisualTreeHelper.GetParent(dp);
            }

            if (dp is DataGridCell cell)
            {
                //Dont open the editors when on the "Name" column, as double clicking on this will rename it
                if((string)cell.Column.Header != "Name")
                {
                    EditFile();
                }
            }
        }

        private void filesDataGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];

                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach (var filePath in droppedFilePaths)
                {
                    string ext = Path.GetExtension(filePath);

                    if (ext != ".emd" && ext != ".emb" && ext != ".emm" && ext != ".ean" && ext != ".esk" && ext != ".scd") continue;
                    if (Character.CheckCharaFilePathIsReserved(Path.GetFileName(filePath))) continue;

                    if (File.Exists(filePath))
                    {
                        undos.Add(Character.AddCharaFile(filePath, true));
                    }
                }

                UndoManager.Instance.AddCompositeUndo(undos, droppedFilePaths.Length > 1 ? "Add Chara Files" : "Add Chara File", UndoGroup.BCS);

                e.Handled = true;
            }
        }
    }
}
