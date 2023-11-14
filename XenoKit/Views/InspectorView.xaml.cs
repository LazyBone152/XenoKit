using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using GalaSoft.MvvmLight.CommandWpf;
using GongSolutions.Wpf.DragDrop;
using EEPK_Organiser.Forms;
using EEPK_Organiser.View;
using XenoKit.Engine;
using XenoKit.Inspector;
using XenoKit.Inspector.InspectorEntities;

namespace XenoKit.Views
{
    /// <summary>
    /// Interaction logic for InspectorView.xaml
    /// </summary>
    public partial class InspectorView : UserControl, INotifyPropertyChanged, IDropTarget
    {
        #region INotify
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion

        public InspectorMode Inspector => InspectorMode.Instance;
        public InspectorEntity SelectedItem { get; private set; }


        public InspectorView()
        {
            InitializeComponent();
            DataContext = this;
        }

        #region Buttons
        public RelayCommand AddFileCommand => new RelayCommand(AddFile);
        private void AddFile()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Add File(s)";
            openFile.Filter = "DBXV2 files | *.emd; *.esk; *.nsk; *.emo; *.emg; *.ean";
            openFile.Multiselect = true;

            if(openFile.ShowDialog() == true)
            {
                Inspector.LoadFiles(openFile.FileNames);
            }
        }

        public RelayCommand ClearFilesCommand => new RelayCommand(ClearFiles);
        private void ClearFiles()
        {
            Inspector.ClearFiles();
        }

        public RelayCommand ReloadFilesCommand => new RelayCommand(ReloadFiles);
        private void ReloadFiles()
        {
            Inspector.ReloadFiles();
        }

        public RelayCommand SaveAllFilesCommand => new RelayCommand(SaveAllFiles);
        private void SaveAllFiles()
        {
            Inspector.SaveFiles();
        }

        #endregion

        #region ContextMenu
        public Visibility TextureVisibility => CanDytBeSet() ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EanVisibility => SelectedItem is EanInspectorEntity ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MeshVisibility => SelectedItem is MeshInspectorEntity ? Visibility.Visible : Visibility.Collapsed;

        public RelayCommand SaveFileCommand => new RelayCommand(SaveFile, IsFileSelected);
        private void SaveFile()
        {
            if (SelectedItem != null)
            {
                if (!SelectedItem.Save())
                {
                    MessageBox.Show("This file does not support saving.", "Save Not Possible", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public RelayCommand ReloadFileCommand => new RelayCommand(ReloadFile, IsFileSelected);
        private void ReloadFile()
        {
            if(SelectedItem != null)
            {
                if (!SelectedItem.Load())
                {
                    MessageBox.Show("This file does not support reloading.", "Reload Not Possible", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public RelayCommand ReloadChildrenFileCommand => new RelayCommand(ReloadChildrenFile, IsFileSelected);
        private void ReloadChildrenFile()
        {
            if (SelectedItem != null)
            {
                SelectedItem.Load();
                SelectedItem.ReloadChildren();
            }
        }

        public RelayCommand RemoveFileCommand => new RelayCommand(RemoveFile, IsFileSelected);
        private void RemoveFile()
        {
            if (SelectedItem != null)
            {
                Inspector.RemoveFile(SelectedItem);
            }
        }

        public RelayCommand<int> SetDytLevelCommand => new RelayCommand<int>(SetDytLevel);
        private void SetDytLevel(int dyt)
        {
            if(SelectedItem != null)
            {
                if (SelectedItem is TextureInspectorEntity texture)
                    texture.DytIndex = dyt;

                InspectorMode.Instance.InternalSetDyt(SelectedItem.ChildEntities, dyt);
            }
        }

        public RelayCommand EditFileCommand => new RelayCommand(EditFile, CanEditFile);
        private void EditFile()
        {
            if(SelectedItem is MaterialInspectorEntity material)
            {
                MaterialsEditorForm materialsEditor = new MaterialsEditorForm(material.EmmFile, SelectedItem.Name);
                materialsEditor.Show();
            }
            else if (SelectedItem is TextureInspectorEntity texture)
            {
                EmbEditForm textureEditor = new EmbEditForm(texture.EmbFile, TextureEditorType.Character, SelectedItem.Name);
                textureEditor.Show();
            }
            else if (SelectedItem is MeshInspectorEntity mesh)
            {
                if(mesh.EmdFile == null)
                {
                    MessageBox.Show("The Model Viewer currently only supports EMD models.", "Not Supported", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                EmdViewer modelViewer = new EmdViewer(mesh.EmdFile, mesh.TextureFile?.EmbFile, mesh.DytFile?.EmbFile, mesh.MaterialFile?.EmmFile, mesh.Name);
                modelViewer.Show();
            }
        }

        public RelayCommand DrawLastCommand => new RelayCommand(DrawLast, IsModelFile);
        private void DrawLast()
        {
            if(SelectedItem is MeshInspectorEntity mesh)
            {
                SceneManager.MainGameBase.RenderSystem.MoveRenderEntityToFront(mesh);
            }
        }


        private bool IsModelFile()
        {
            return SelectedItem is MeshInspectorEntity;
        }

        private bool CanEditFile()
        {
            return SelectedItem is MaterialInspectorEntity || SelectedItem is TextureInspectorEntity || SelectedItem is MeshInspectorEntity;
        }

        private bool CanDytBeSet()
        {
            return SelectedItem is TextureInspectorEntity || SelectedItem is SkinnedInspectorEntity || SelectedItem is MeshInspectorEntity;
        }

        private bool IsFileSelected() => SelectedItem != null;
        #endregion


        #region DropHandler
        private static string[] AllowedDropFiles = new string[]
        {
            ".esk",
            ".emd",
            ".emo",
            ".nsk",
            ".emg",
            ".emb",
            ".emm",
            ".ean",
        };

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            if(dropInfo.Data is DataObject data)
            {
                InspectorEntity targetItem = dropInfo.TargetItem as InspectorEntity;
                bool allowDrop = true;
                bool allowDropOnTarget = true;

                if (data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] droppedFilePaths = data.GetData(DataFormats.FileDrop, true) as string[];

                    foreach (string path in droppedFilePaths)
                    {
                        string ext = System.IO.Path.GetExtension(path);

                        //Can file be dropped at all?
                        if (!AllowedDropFiles.Contains(ext))
                        {
                            allowDrop = false;
                            break;
                        }

                        //Can file be dropped on the target item?
                        if ((targetItem is SkinnedInspectorEntity && ext != ".emd" && ext != ".esk" && ext != "nsk") ||
                           (targetItem is MeshInspectorEntity && ext != ".emb" && ext != ".emm") ||
                           targetItem is TextureInspectorEntity || targetItem is MaterialInspectorEntity || targetItem is EanInspectorEntity)
                        {
                            allowDropOnTarget = false;
                            break;
                        }
                    }

                    dropInfo.Effects = allowDrop ? DragDropEffects.Copy : DragDropEffects.None;

                    if (allowDropOnTarget)
                    {
                        dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                    }
                }
            }
            else
            {
                InspectorEntity sourceItem = dropInfo.Data as InspectorEntity;
                InspectorEntity targetItem = dropInfo.TargetItem as InspectorEntity;

                if (sourceItem != null && sourceItem != targetItem)
                {
                    if(sourceItem is SkinnedInspectorEntity sourceSkinned && targetItem is SkinnedInspectorEntity)
                    {
                        if (!sourceSkinned.HasSkinnedChildren)
                        {
                            dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                            dropInfo.Effects = DragDropEffects.Copy;
                        }
                    }
                    else if ((sourceItem is MeshInspectorEntity) && (targetItem is SkinnedInspectorEntity) ||
                        ((sourceItem is TextureInspectorEntity || sourceItem is MaterialInspectorEntity) && targetItem is MeshInspectorEntity) ||
                        targetItem == null)
                    {
                        dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                        dropInfo.Effects = DragDropEffects.Copy;
                    }
                }
            }
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is DataObject data)
            {
                InspectorEntity targetItem = dropInfo.TargetItem as InspectorEntity;

                if (data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] droppedFilePaths = data.GetData(DataFormats.FileDrop, true) as string[];
                    Inspector.LoadFiles(droppedFilePaths, targetItem);
                }
            }
            else
            {
                InspectorEntity sourceItem = dropInfo.Data as InspectorEntity;
                InspectorEntity targetItem = dropInfo.TargetItem as InspectorEntity;

                if (Keyboard.IsKeyDown(Key.LeftCtrl) && (sourceItem is TextureInspectorEntity || sourceItem is MaterialInspectorEntity))
                {
                    //Copy file instead of moving it
                    InspectorEntity newItem = sourceItem.Clone();
                    Inspector.ChangeParent(newItem, targetItem);
                    Inspector.InternalAddAllRenderEntities(targetItem != null ? targetItem.ChildEntities : Inspector.Entities);
                }
                else
                {
                    Inspector.ChangeParent(sourceItem, targetItem);
                }
            }
        }

        void IDropTarget.DragEnter(IDropInfo dropInfo)
        {
        }

        void IDropTarget.DragLeave(IDropInfo dropInfo)
        {

        }

        public void ProcessFileDrop(string[] files)
        {
            Inspector.LoadFiles(files, null);
        }

        private void Grid_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];

                Inspector.LoadFiles(droppedFilePaths, null);

                e.Handled = true;
            }
            else
            {
                e.Handled = false;
            }
        }
        #endregion

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is InspectorEntity entity)
            {
                SelectedItem = entity;
                NotifyPropertyChanged(nameof(SelectedItem));
                NotifyPropertyChanged(nameof(TextureVisibility));
                NotifyPropertyChanged(nameof(EanVisibility));
                NotifyPropertyChanged(nameof(MeshVisibility));
            }

            //Set context for transform gizmo (move skeletons around)
            if(SceneManager.MainGameInstance != null)
            {
                SceneManager.MainGameInstance.EntityTransformGizmo.SetContext(null, 0);

                if (e.NewValue is SkinnedInspectorEntity skinned)
                {
                    //Only root level skeletons can be moved (SCDs will get set to parents transform)
                    if (Inspector.Entities.Contains(skinned))
                    {
                        SceneManager.MainGameInstance.EntityTransformGizmo.SetContext(skinned, EditorTabs.Inspector);
                        SceneManager.MainGameInstance.EntityTransformGizmo.Enable();
                    }
                }
            }
        }

    }
}
