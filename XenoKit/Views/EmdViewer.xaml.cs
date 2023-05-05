using System;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.Collections.Generic;
using Xv2CoreLib.EMD;
using Xv2CoreLib.Resource.UndoRedo;
using XenoKit.ViewModel.EMD;
using System.Windows.Controls;
using Xv2CoreLib;
using MahApps.Metro.Controls;
using Xv2CoreLib.BCS;
using System.IO;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMM;
using GalaSoft.MvvmLight.CommandWpf;
using XenoKit.Editor;

namespace XenoKit.Views
{
    /// <summary>
    /// Interaction logic for EmdViewer.xaml
    /// </summary>
    public partial class EmdViewer : MetroWindow, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
             PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        //Other shit
        public Xv2Character ParentCharacter { get; set; }
        public IEnumerable<Xv2PartSetFile> EmbFiles => ParentCharacter != null ? ParentCharacter.PartSetFiles.Where(x => x.FileType == Xv2PartSetFile.Type.EMB) : null;
        public IEnumerable<Xv2PartSetFile> DytFiles => ParentCharacter != null ? ParentCharacter.PartSetFiles.Where(x => x.FileType == Xv2PartSetFile.Type.DYT_EMB) : null;
        public IEnumerable<Xv2PartSetFile> EmmFiles => ParentCharacter != null ? ParentCharacter.PartSetFiles.Where(x => x.FileType == Xv2PartSetFile.Type.EMM) : null;


        //Loaded Files
        public EMD_File EmdFile { get; private set; }
        public string EmdPath { get; set; }
        public string EmbPath { get; set; }
        public string DytPath { get; set; }
        public string EmmPath{ get; set; }


        //SelectedItem
        public object SelectedItem => treeView.SelectedItem;
        public EMD_Model SelectedModel => SelectedItem as EMD_Model;
        public EMD_Mesh SelectedMesh => SelectedItem as EMD_Mesh;
        public EMD_Submesh SelectedSubmesh => SelectedItem as EMD_Submesh;
        public EMD_TextureSamplerDef SelectedTexture => SelectedItem as EMD_TextureSamplerDef;

        //ViewModels
        public EmdAABBViewModel AabbViewModel { get; set; }
        public EmdTextureViewModel TextureViewModel { get; set; }

        //Names
        public string SelectedModelName
        {
            get => SelectedModel?.Name;
            set
            {
                if(SelectedModel != null && SelectedModel?.Name != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(SelectedModel.Name), SelectedModel, SelectedModel.Name, value, "Model Name"));
                    SelectedModel.Name = value;
                    NotifyPropertyChanged(nameof(SelectedModelName));
                    SelectedModel.RefreshValues();
                }
            }
        }
        public string SelectedMeshName
        {
            get => SelectedMesh?.Name;
            set
            {
                if (SelectedMesh != null && SelectedMesh?.Name != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(SelectedMesh.Name), SelectedMesh, SelectedMesh.Name, value, "Mesh Name"));
                    SelectedMesh.Name = value;
                    NotifyPropertyChanged(nameof(SelectedMeshName));
                    SelectedMesh.RefreshValues();
                }
            }
        }
        public string SelecteSubmeshName
        {
            get => SelectedSubmesh?.Name;
            set
            {
                if (SelectedSubmesh != null && SelectedSubmesh?.Name != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(SelectedSubmesh.Name), SelectedSubmesh, SelectedSubmesh.Name, value, "Mesh Name"));
                    SelectedSubmesh.Name = value;
                    NotifyPropertyChanged(nameof(SelecteSubmeshName));
                    SelectedSubmesh.RefreshValues();
                }
            }
        }

        //Visibilities
        public Visibility ModelNameVisibility => SelectedModel != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MeshNameVisibility => SelectedMesh != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SubmeshNameVisibility => SelectedSubmesh != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility AabbVisibility => SelectedMesh != null || SelectedSubmesh != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TextureVisibility => SelectedTexture != null ? Visibility.Visible : Visibility.Collapsed;

        public EmdViewer(EMD_File emdFile, string emdPath, Xv2Character character, Window owner = null)
        {
            DataContext = this;
            EmdPath = emdPath;
            EmdFile = emdFile;
            ParentCharacter = character;
            //Owner = owner != null ? owner : Application.Current.MainWindow;
            InitializeComponent();
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            Closed += EmdViewer_Closed;

            Title += $" ({emdPath})";

            AutoSetFiles();
            LoadFiles();
        }

        private void EmdViewer_Closed(object sender, EventArgs e)
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
            viewer?.ClearInstance();
        }

        private void Instance_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {
            EmdFile?.RefreshValues();
            AabbViewModel?.UpdateProperties();
            TextureViewModel?.UpdateProperties();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //viewer.ModelGizmo.Disable();

            if(SelectedMesh != null)
            {
                AabbViewModel = new EmdAABBViewModel(SelectedMesh.AABB);
                NotifyPropertyChanged(nameof(AabbViewModel));

                //viewer.ModelGizmo.Enable(SelectedMesh.Submeshes, viewer.Model.GetCompiledSubmeshes(SelectedMesh.Submeshes), viewer.EmdFile);
            }

            if (SelectedSubmesh != null)
            {
                AabbViewModel = new EmdAABBViewModel(SelectedSubmesh.AABB);
                NotifyPropertyChanged(nameof(AabbViewModel));
            }

            if (SelectedTexture != null)
            {
                TextureViewModel = new EmdTextureViewModel(SelectedTexture, EmdFile);
                NotifyPropertyChanged(nameof(TextureViewModel));
            }

            NotifyPropertyChanged(nameof(ModelNameVisibility));
            NotifyPropertyChanged(nameof(MeshNameVisibility));
            NotifyPropertyChanged(nameof(SubmeshNameVisibility));
            NotifyPropertyChanged(nameof(AabbVisibility));
            NotifyPropertyChanged(nameof(TextureVisibility));
            NotifyPropertyChanged(nameof(SelectedModelName));
            NotifyPropertyChanged(nameof(SelectedMeshName));
            NotifyPropertyChanged(nameof(SelecteSubmeshName));
        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            TreeViewItem tvi = e.OriginalSource as TreeViewItem;

            if (tvi == null || e.Handled) return;

            if (!tvi.IsExpanded)
                tvi.IsExpanded = true;

            //tvi.IsExpanded = !tvi.IsExpanded;
            e.Handled = true;
        }
   
        private void AutoSetFiles()
        {
            Part part = ParentCharacter.BcsFile.File.GetPartWithEmdPath(EmdPath);
            PhysicsPart physicsPart = ParentCharacter.BcsFile.File.GetPhysicsPartWithEmdPath(EmdPath);

            if (part != null)
            {
                EmbPath = Path.GetFileNameWithoutExtension(part.GetEmbPath(part.PartType));
                DytPath = Path.GetFileNameWithoutExtension(part.GetDytPath(part.PartType));
                EmmPath = Path.GetFileNameWithoutExtension(part.GetEmmPath(part.PartType));
            }
            else if(physicsPart != null)
            {
                string emb = Path.GetFileNameWithoutExtension(physicsPart.GetEmbPath());
                string dyt = Path.GetFileNameWithoutExtension(physicsPart.GetDytPath());
                string emm = Path.GetFileNameWithoutExtension(physicsPart.GetEmmPath());

                EmbPath = ParentCharacter.PartSetFiles.Any(x => x.NameNoExt == emb && x.FileType == Xv2PartSetFile.Type.EMB) ? emb : part.GetEmbPath(part.PartType);
                EmmPath = ParentCharacter.PartSetFiles.Any(x => x.NameNoExt == emm && x.FileType == Xv2PartSetFile.Type.EMM) ? emm : part.GetEmmPath(part.PartType);
                DytPath = ParentCharacter.PartSetFiles.Any(x => x.NameNoExt == dyt && x.FileType == Xv2PartSetFile.Type.DYT_EMB) ? dyt : part.GetDytPath(part.PartType);
            }
            else
            {
                EmmPath = $"{Path.GetFileNameWithoutExtension(EmdPath)}";
                EmbPath = $"{Path.GetFileNameWithoutExtension(EmdPath)}";
                DytPath = $"{Path.GetFileNameWithoutExtension(EmdPath)}.dyt";
            }

            NotifyPropertyChanged(nameof(EmmPath));
            NotifyPropertyChanged(nameof(EmbPath));
            NotifyPropertyChanged(nameof(DytPath));
        }

        private async void LoadFiles()
        {
            Xv2PartSetFile emb = ParentCharacter.PartSetFiles.FirstOrDefault(x => x.NameNoExt == EmbPath && x.FileType == Xv2PartSetFile.Type.EMB);

            if(emb != null)
            {
                emb.Load();
                viewer.EmbFile = (EMB_File)emb.File;
            }
            else
            {
                viewer.EmbFile = null;
            }

            Xv2PartSetFile dyt = ParentCharacter.PartSetFiles.FirstOrDefault(x => x.NameNoExt == DytPath && x.FileType == Xv2PartSetFile.Type.DYT_EMB);

            if (dyt != null)
            {
                dyt.Load();
                viewer.DytFile = (EMB_File)dyt.File;
            }
            else
            {
                viewer.DytFile = null;
            }

            Xv2PartSetFile emm = ParentCharacter.PartSetFiles.FirstOrDefault(x => x.NameNoExt == EmmPath && x.FileType == Xv2PartSetFile.Type.EMM);

            if (emm != null)
            {
                emm.Load();
                viewer.EmmFile = (EMM_File)emm.File;
            }
            else
            {
                viewer.EmmFile = null;
            }

            viewer.EmdFile = EmdFile;
            await viewer.RefreshModel();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadFiles();
        }

        #region Commands
        public RelayCommand DeleteModelCommand => new RelayCommand(DeleteModel, IsModelSelected);
        private void DeleteModel()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            undos.Add(new UndoableListRemove<EMD_Model>(EmdFile.Models, SelectedModel));
            EmdFile.Models.Remove(SelectedModel);

            undos.Add(new UndoActionDelegate(EmdFile, nameof(EmdFile.TriggerModelChanged), true));
            EmdFile.TriggerModelChanged();

            UndoManager.Instance.AddCompositeUndo(undos, "Delete Model");
        }

        public RelayCommand DeleteMeshCommand => new RelayCommand(DeleteMesh, IsMeshSelected);
        private void DeleteMesh()
        {
            EMD_Model model = EmdFile.GetParentModel(SelectedMesh);
            List<IUndoRedo> undos = new List<IUndoRedo>();

            undos.Add(new UndoableListRemove<EMD_Mesh>(model.Meshes, SelectedMesh));
            model.Meshes.Remove(SelectedMesh);

            undos.Add(new UndoActionDelegate(EmdFile, nameof(EmdFile.TriggerModelChanged), true));
            EmdFile.TriggerModelChanged();

            UndoManager.Instance.AddCompositeUndo(undos, "Delete Mesh");
        }

        public RelayCommand DeleteSubmeshCommand => new RelayCommand(DeleteSubmesh, IsSubmeshSelected);
        private void DeleteSubmesh()
        {
            EMD_Mesh mesh = EmdFile.GetParentMesh(SelectedSubmesh);
            List<IUndoRedo> undos = new List<IUndoRedo>();

            undos.Add(new UndoableListRemove<EMD_Submesh>(mesh.Submeshes, SelectedSubmesh));
            mesh.Submeshes.Remove(SelectedSubmesh);

            undos.Add(new UndoActionDelegate(EmdFile, nameof(EmdFile.TriggerModelChanged), true));
            EmdFile.TriggerModelChanged();

            UndoManager.Instance.AddCompositeUndo(undos, "Delete Submesh");
        }

        public RelayCommand DeleteTextureCommand => new RelayCommand(DeleteTexture, IsTextureSelected);
        private void DeleteTexture()
        {
            EMD_Submesh submesh = EmdFile.GetParentSubmesh(SelectedTexture);
            List<IUndoRedo> undos = new List<IUndoRedo>();

            undos.Add(new UndoableListRemove<EMD_TextureSamplerDef>(submesh.TextureSamplerDefs, SelectedTexture));
            submesh.TextureSamplerDefs.Remove(SelectedTexture);

            undos.Add(new UndoActionDelegate(EmdFile, nameof(EmdFile.TriggerModelChanged), true));
            EmdFile.TriggerModelChanged();

            UndoManager.Instance.AddCompositeUndo(undos, "Delete Texture Sampler");
        }

        public RelayCommand CopyModelCommand => new RelayCommand(CopyModel, IsModelSelected);
        private void CopyModel()
        {
            Clipboard.SetData(ClipboardConstants.EmdModel, SelectedModel);
        }

        public RelayCommand CopyMeshCommand => new RelayCommand(CopyMesh, IsMeshSelected);
        private void CopyMesh()
        {
            Clipboard.SetData(ClipboardConstants.EmdMesh, SelectedMesh);
        }

        public RelayCommand CopySubmeshCommand => new RelayCommand(CopySubmesh, IsSubmeshSelected);
        private void CopySubmesh()
        {
            Clipboard.SetData(ClipboardConstants.EmdSubmesh, SelectedSubmesh);
        }

        public RelayCommand CopyTextureCommand => new RelayCommand(CopyTexture, IsTextureSelected);
        private void CopyTexture()
        {
            Clipboard.SetData(ClipboardConstants.EmdTextureSampler, SelectedTexture);
        }

        public RelayCommand PasteModelCommand => new RelayCommand(PasteModel, CanPasteModel);
        private void PasteModel()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            EMD_Model model = (EMD_Model)Clipboard.GetData(ClipboardConstants.EmdModel);

            undos.Add(new UndoableListAdd<EMD_Model>(EmdFile.Models, model));
            EmdFile.Models.Add(model);

            undos.Add(new UndoActionDelegate(EmdFile, nameof(EmdFile.TriggerModelChanged), true));
            EmdFile.TriggerModelChanged();

            UndoManager.Instance.AddCompositeUndo(undos, "Paste Model");
        }

        public RelayCommand PasteMeshCommand => new RelayCommand(PasteMesh, CanPasteMesh);
        private void PasteMesh()
        {
            EMD_Model model = SelectedModel != null ? SelectedModel : EmdFile.GetParentModel(SelectedMesh);

            List<IUndoRedo> undos = new List<IUndoRedo>();

            EMD_Mesh mesh = (EMD_Mesh)Clipboard.GetData(ClipboardConstants.EmdMesh);

            undos.Add(new UndoableListAdd<EMD_Mesh>(model.Meshes, mesh));
            model.Meshes.Add(mesh);

            undos.Add(new UndoActionDelegate(EmdFile, nameof(EmdFile.TriggerModelChanged), true));
            EmdFile.TriggerModelChanged();

            UndoManager.Instance.AddCompositeUndo(undos, "Paste Mesh");
        }

        public RelayCommand PasteSubmeshCommand => new RelayCommand(PasteSubmesh, CanPasteSubmesh);
        private void PasteSubmesh()
        {
            EMD_Mesh mesh = SelectedMesh != null ? SelectedMesh : EmdFile.GetParentMesh(SelectedSubmesh);

            List<IUndoRedo> undos = new List<IUndoRedo>();

            EMD_Submesh submesh = (EMD_Submesh)Clipboard.GetData(ClipboardConstants.EmdSubmesh);

            undos.Add(new UndoableListAdd<EMD_Submesh>(mesh.Submeshes, submesh));
            mesh.Submeshes.Add(submesh);

            undos.Add(new UndoActionDelegate(EmdFile, nameof(EmdFile.TriggerModelChanged), true));
            EmdFile.TriggerModelChanged();

            UndoManager.Instance.AddCompositeUndo(undos, "Paste Submesh");
        }

        public RelayCommand PasteTextureCommand => new RelayCommand(PasteTexture, CanPasteTexture);
        private void PasteTexture()
        {
            EMD_Submesh submesh = SelectedSubmesh != null ? SelectedSubmesh : EmdFile.GetParentSubmesh(SelectedTexture);

            if (submesh.TextureSamplerDefs.Count >= 4)
            {
                MessageBox.Show("Cannot add anymore Texture Samplers.", "Maximum Texture Samplers", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            List<IUndoRedo> undos = new List<IUndoRedo>();

            EMD_TextureSamplerDef texture = (EMD_TextureSamplerDef)Clipboard.GetData(ClipboardConstants.EmdTextureSampler);

            undos.Add(new UndoableListAdd<EMD_TextureSamplerDef>(submesh.TextureSamplerDefs, texture));
            submesh.TextureSamplerDefs.Add(texture);

            undos.Add(new UndoActionDelegate(EmdFile, nameof(EmdFile.TriggerModelChanged), true));
            EmdFile.TriggerModelChanged();

            UndoManager.Instance.AddCompositeUndo(undos, "Paste Texture Sampler");
        }

        public RelayCommand AddTextureCommand => new RelayCommand(AddTexture, CanAddTexture);
        private void AddTexture()
        {
            EMD_Submesh submesh = SelectedSubmesh != null ? SelectedSubmesh : EmdFile.GetParentSubmesh(SelectedTexture);

            if(submesh.TextureSamplerDefs.Count >= 4)
            {
                MessageBox.Show("Cannot add anymore Texture Samplers.", "Maximum Texture Samplers", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            List<IUndoRedo> undos = new List<IUndoRedo>();

            EMD_TextureSamplerDef texture = new EMD_TextureSamplerDef();

            undos.Add(new UndoableListAdd<EMD_TextureSamplerDef>(submesh.TextureSamplerDefs, texture));
            submesh.TextureSamplerDefs.Add(texture);

            undos.Add(new UndoActionDelegate(EmdFile, nameof(EmdFile.TriggerModelChanged), true));
            EmdFile.TriggerModelChanged();

            UndoManager.Instance.AddCompositeUndo(undos, "Add Texture Sampler");
        }

        private bool IsModelSelected()
        {
            return SelectedModel != null;
        }

        private bool IsMeshSelected()
        {
            return SelectedMesh != null;
        }

        private bool IsSubmeshSelected()
        {
            return SelectedSubmesh != null;
        }

        private bool IsTextureSelected()
        {
            return SelectedTexture != null;
        }

        private bool CanPasteModel()
        {
            return Clipboard.ContainsData(ClipboardConstants.EmdModel);
        }

        private bool CanPasteMesh()
        {
            return Clipboard.ContainsData(ClipboardConstants.EmdMesh) && (SelectedModel != null || SelectedMesh != null);
        }

        private bool CanPasteSubmesh()
        {
            return Clipboard.ContainsData(ClipboardConstants.EmdSubmesh) && (SelectedMesh != null || SelectedSubmesh != null);
        }

        private bool CanPasteTexture()
        {
            return Clipboard.ContainsData(ClipboardConstants.EmdTextureSampler) && (SelectedSubmesh != null || SelectedTexture != null);
        }

        private bool CanAddTexture()
        {
            return SelectedSubmesh != null || SelectedTexture != null;
        }
        #endregion

        #region InputCommands
        public RelayCommand DeleteCommand => new RelayCommand(DeleteAnything, IsAnythingSelected);
        private void DeleteAnything()
        {
            if(SelectedModel != null)
            {
                DeleteModel();
            }
            else if (SelectedMesh != null)
            {
                DeleteMesh();
            }
            else if (SelectedSubmesh != null)
            {
                DeleteSubmesh();
            }
            else if (SelectedTexture != null)
            {
                DeleteTexture();
            }
        }

        public RelayCommand CopyCommand => new RelayCommand(CopyAnything, IsAnythingSelected);
        private void CopyAnything()
        {
            if (SelectedModel != null)
            {
                CopyModel();
            }
            else if (SelectedMesh != null)
            {
                CopyMesh();
            }
            else if (SelectedSubmesh != null)
            {
                CopySubmesh();
            }
            else if (SelectedTexture != null)
            {
                CopyTexture();
            }
        }

        public RelayCommand PasteCommand => new RelayCommand(PasteAnything, CanPasteAnything);
        private void PasteAnything()
        {
            if (CanPasteModel())
            {
                PasteModel();
            }
            else if (CanPasteMesh())
            {
                PasteMesh();
            }
            else if (CanPasteSubmesh())
            {
                PasteSubmesh();
            }
            else if (CanPasteTexture())
            {
                PasteTexture();
            }
        }


        private bool CanPasteAnything()
        {
            return CanPasteModel() || CanPasteMesh() || CanPasteSubmesh() || CanPasteTexture();
        }

        private bool IsAnythingSelected()
        {
            return IsModelSelected() || IsMeshSelected() || IsSubmeshSelected() || IsTextureSelected();
        }
        #endregion
    }
}
