using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.Engine.Model;
using XenoKit.Helper;
using XenoKit.ViewModel.EMD;
using Xv2CoreLib.EMD;
using Xv2CoreLib.Resource.UndoRedo;
using EEPK_Organiser.Forms;
using EEPK_Organiser.View;
using GalaSoft.MvvmLight.CommandWpf;
using XenoKit.Engine;
using XenoKit.Engine.Animation;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Collections;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;
using SimdQuaternion = System.Numerics.Quaternion;
using Xv2CoreLib.Resource;
using XenoKit.Engine.Gizmo.TransformOperations;
using Xv2CoreLib.EMO;
using Xv2CoreLib.EMG;
using Xv2CoreLib.EMA;
using LB_Common.Forms;

namespace XenoKit.Views
{
    /// <summary>
    /// Interaction logic for ModelScene.xaml
    /// </summary>
    public partial class ModelSceneView : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        public ModelScene ModelScene { get; private set; }
        public EMD_File EmdFile => ModelScene.Model.Type == ModelType.Nsk ? ModelScene.Model.SourceNskFile.EmdFile : ModelScene.Model.SourceEmdFile;
        public EMO_File EmoFile => ModelScene.Model.SourceEmoFile;

        public ObservableCollection<object> SelectedItems => ModelScene.SelectedItems;
        public object SelectedItem => SelectedItems.Count > 0 ? SelectedItems[0] : null;
        public EMD_Model SelectedModel => SelectedItem as EMD_Model;
        public EMD_Mesh SelectedMesh => SelectedItem as EMD_Mesh;
        public EMD_Submesh SelectedSubmesh => SelectedItem as EMD_Submesh;
        public EMD_TextureSamplerDef SelectedTexture => SelectedItem as EMD_TextureSamplerDef;
        public EMO_Part EMO_SelectedPart => SelectedItem as EMO_Part;
        public EMG_File EMO_SelectedEmgFile => SelectedItem as EMG_File;
        public EMG_Mesh EMO_SelectedMesh => SelectedItem as EMG_Mesh;
        public EMG_SubmeshGroup EMO_SelectedSubmesh => SelectedItem as EMG_SubmeshGroup;


        public EmdTextureViewModel TextureViewModel { get; set; }

        //Names
        public string SelectedModelName
        {
            get => SelectedModel?.Name;
            set
            {
                if (SelectedModel != null && SelectedModel?.Name != value)
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
                if (SelectedMesh != null && SelectedMesh.Name != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(SelectedMesh.Name), SelectedMesh, SelectedMesh.Name, value, "Mesh Name"));

                    SelectedMesh.Name = value;
                    NotifyPropertyChanged(nameof(SelectedMeshName));
                    SelectedMesh.RefreshValues();
                }
            }
        }
        public string SelectedMeshBoneName
        {
            get => SelectedMesh?.Name;
            set
            {
                if (SelectedMesh != null && SelectedMesh.Name != value)
                {
                    var bone = ModelScene.Model.SourceNskFile.EskFile.Skeleton.GetBone(value);
                    string boneName = value;

                    if(bone == null)
                    {
                        boneName = ModelScene.Model.SourceNskFile.EskFile.Skeleton.ESKBones[0].Name;
                    }

                    UndoManager.Instance.AddCompositeUndo(new List<IUndoRedo>()
                        {
                            new UndoablePropertyGeneric(nameof(SelectedMesh.Name), SelectedMesh, SelectedMesh.Name, boneName),
                            new UndoActionDelegate(EmdFile, nameof(EmdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Bone, SelectedMesh))
                        }, "Bone Name");

                    SelectedMesh.Name = boneName;
                    NotifyPropertyChanged(nameof(SelectedMeshBoneName));
                    SelectedMesh.RefreshValues();
                    EmdFile.TriggerModelModifiedEvent(EditTypeEnum.Bone, SelectedMesh, null);
                } 
            }
        }
        public string SelectedSubmeshName
        {
            get => SelectedSubmesh?.Name;
            set
            {
                if (SelectedSubmesh != null && SelectedSubmesh?.Name != value)
                {
                    UndoManager.Instance.AddCompositeUndo(new List<IUndoRedo>()
                    {
                        new UndoablePropertyGeneric(nameof(SelectedSubmesh.Name), SelectedSubmesh, SelectedSubmesh.Name, value),
                        new UndoActionDelegate(EmdFile, nameof(EmdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Material, SelectedSubmesh)),
                        new UndoActionDelegate(SelectedSubmesh, nameof(SelectedSubmesh.RefreshValues), true)
                    }, "Submesh Name", UndoGroup.EMD);

                    SelectedSubmesh.Name = value;
                    NotifyPropertyChanged(nameof(SelectedSubmeshName));
                    SelectedSubmesh.RefreshValues();
                    EmdFile.TriggerModelModifiedEvent(EditTypeEnum.Material, SelectedSubmesh, null);
                }
            }
        }
        public string EMO_SelectedSubmeshName
        {
            get => EMO_SelectedSubmesh?.MaterialName;
            set
            {
                if (EMO_SelectedSubmesh != null && EMO_SelectedSubmesh?.MaterialName != value)
                {
                    UndoManager.Instance.AddCompositeUndo(new List<IUndoRedo>()
                    {
                        new UndoablePropertyGeneric(nameof(EMO_SelectedSubmesh.MaterialName), EMO_SelectedSubmesh, EMO_SelectedSubmesh.MaterialName, value),
                        new UndoActionDelegate(EmoFile, nameof(EmoFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Material, EMO_SelectedSubmesh)),
                        new UndoActionDelegate(EMO_SelectedSubmesh, nameof(EMO_SelectedSubmesh.RefreshValues), true)
                    }, "Submesh Name", UndoGroup.EMD);

                    EMO_SelectedSubmesh.MaterialName = value;
                    NotifyPropertyChanged(nameof(EMO_SelectedSubmesh));
                    EMO_SelectedSubmesh.RefreshValues();
                    EmoFile.TriggerModelModifiedEvent(EditTypeEnum.Material, EMO_SelectedSubmesh, null);
                }
            }
        }
        public int DytIndex
        {
            get => ModelScene.DytIndex;
            set => ModelScene.DytIndex = value;
        }

        public Bone EMO_SelectedBone
        {
            get => EMO_SelectedPart?.LinkedBone;
            set
            {
                if (EMO_SelectedPart != null && EMO_SelectedPart.LinkedBone != value)
                {
                    UndoManager.Instance.AddCompositeUndo(new List<IUndoRedo>()
                        {
                            new UndoablePropertyGeneric(nameof(EMO_SelectedPart.LinkedBone), EMO_SelectedPart, EMO_SelectedPart.LinkedBone, value),
                            new UndoActionDelegate(EmoFile, nameof(EmoFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Bone, EMO_SelectedPart))
                        }, "Bone Name");

                    EMO_SelectedPart.LinkedBone = value;
                    NotifyPropertyChanged(nameof(EMO_SelectedBone));
                    EmoFile.TriggerModelModifiedEvent(EditTypeEnum.Bone, EMO_SelectedPart, null);
                }
            }
        }


        //Visibilities
        public Visibility ModelNameVisibility => SelectedModel != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MeshNameVisibility => SelectedMesh != null && ModelScene.Model.Type == ModelType.Emd ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MeshBoneNameVisibility => SelectedMesh != null && ModelScene.Model.Type == ModelType.Nsk ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SubmeshNameVisibility => SelectedSubmesh != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TransformVisibility => SelectedModel != null || SelectedMesh != null || SelectedSubmesh != null || EMO_SelectedEmgFile != null || EMO_SelectedMesh != null || EMO_SelectedPart != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TextureVisibility => SelectedTexture != null ? Visibility.Visible : Visibility.Collapsed;

        public Visibility EMO_PartVisibility => EMO_SelectedPart != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EMO_EmgFileVisibility => EMO_SelectedEmgFile != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EMO_MeshVisibility => EMO_SelectedMesh != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EMO_SubmeshVisibility => EMO_SelectedSubmesh != null ? Visibility.Visible : Visibility.Collapsed;

        public Visibility EmdNskVisibility => ModelScene.Model.Type == ModelType.Emd || ModelScene.Model.Type == ModelType.Nsk ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmdVisibility => ModelScene.Model.Type == ModelType.Emd ? Visibility.Visible : Visibility.Collapsed;
        public Visibility NskVisibility => ModelScene.Model.Type == ModelType.Nsk ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmoVisibility => ModelScene.Model.Type == ModelType.Emo ? Visibility.Visible : Visibility.Collapsed;

        //Pos/Rot/Scale deltas
        private SimdVector3 _currentPos = SimdVector3.Zero;
        private SimdVector3 _currentRot = SimdVector3.Zero;
        private SimdVector3 _initialRot = SimdVector3.Zero;
        private SimdVector3 _currentScale = SimdVector3.One;

        public float PosX
        {
            get => _currentPos.X;
            set 
            {
                if (_currentPos.X != value)
                {
                    _currentPos.X = value;
                    if(!DelayedTransformTimer.IsEnabled)
                        DelayedTransformTimer.Start();
                }
            }
        }
        public float PosY
        {
            get => _currentPos.Y;
            set
            {
                if (_currentPos.Y != value)
                {
                    _currentPos.Y = value;
                    if (!DelayedTransformTimer.IsEnabled)
                        DelayedTransformTimer.Start();
                }
            }
        }
        public float PosZ
        {
            get => _currentPos.Z;
            set
            {
                if (_currentPos.Z != value)
                {
                    _currentPos.Z = value;
                    if (!DelayedTransformTimer.IsEnabled)
                        DelayedTransformTimer.Start();
                }
            }
        }
        public float RotX
        {
            get => _currentRot.X;
            set
            {
                if (_currentRot.X != value)
                {
                    _currentRot.X = value;
                    if (!DelayedTransformTimer.IsEnabled)
                        DelayedTransformTimer.Start();
                }
            }
        }
        public float RotY
        {
            get => _currentRot.Y;
            set
            {
                if (_currentRot.Y != value)
                {
                    _currentRot.Y = value;
                    if (!DelayedTransformTimer.IsEnabled)
                        DelayedTransformTimer.Start();
                }
            }
        }
        public float RotZ
        {
            get => _currentRot.Z;
            set
            {
                if (_currentRot.Z != value)
                {
                    _currentRot.Z = value;
                    if (!DelayedTransformTimer.IsEnabled)
                        DelayedTransformTimer.Start();
                }
            }
        }
        public float ScaleX
        {
            get => _currentScale.X;
            set
            {
                if (_currentScale.X != value)
                {
                    _currentScale.X = value;
                }
            }
        }
        public float ScaleY
        {
            get => _currentScale.Y;
            set
            {
                if (_currentScale.Y != value)
                {
                    _currentScale.Y = value;
                }
            }
        }
        public float ScaleZ
        {
            get => _currentScale.Z;
            set
            {
                if (_currentScale.Z != value)
                {
                    _currentScale.Z = value;
                }
            }
        }

        private DispatcherTimer DelayedSelectedEventTimer { get; set; }
        private DispatcherTimer DelayedTransformTimer { get; set; }

        public ModelSceneView(ModelScene modelScene)
        {
            DataContext = this;
            ModelScene = modelScene;
            ModelScene.SelectedSubmeshesChanged += ModelScene_SelectedSubmeshesChanged;
            ModelScene.ViewportSelectEvent += ModelScene_ViewportSelectEvent;
            ModelScene.ViewportInputEvent += ModelScene_ViewportInputEvent;

            //Delayed event timer for reacting to selected item changes. This is needed because the TreeView events fire off BEFORE the attached property (SelectedItems) does its thing, so we need another event to fire of after a small delay
            DelayedSelectedEventTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(50) };
            DelayedSelectedEventTimer.Tick += (s, e) =>
            {
                DelayedSelectedEventTimer.Stop();
                OnSelectedItemChanged();
            };

            DelayedTransformTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(500) };
            DelayedTransformTimer.Tick += (s, e) =>
            {
                DoDirectTransform();
                DelayedTransformTimer.Stop();
            };

            InitializeComponent();
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;

            treeView.IsEnabled = (EmdFile != null);
            emoTreeView.IsEnabled = (EmoFile != null);
        }

        private void ModelScene_SelectedSubmeshesChanged(object sender, EventArgs e)
        {
            TryEnableModelGizmo();
        }

        private void ModelScene_ViewportInputEvent(object source, ViewportInputEventArgs e)
        {
            if (e.Key == Microsoft.Xna.Framework.Input.Keys.Delete)
            {
                DeleteAnything();
            }
            else if (e.Key == Microsoft.Xna.Framework.Input.Keys.C && e.IsCtrlHeld)
            {
                CopyAnything();
            }
            else if(e.Key == Microsoft.Xna.Framework.Input.Keys.V && e.IsCtrlHeld)
            {
                PasteAnything();
            }
        }

        private void ModelScene_ViewportSelectEvent(object source, ModelSceneSelectEventArgs e)
        {
            if(e.Object == null)
            {
                SelectedItems.Clear();
            }
            else if (e.Addition && !SelectedItems.Contains(e.Object))
            {
                SelectedItems.Add(e.Object);
            }
            else if (e.Addition && SelectedItems.Contains(e.Object))
            {
                SelectedItems.Remove(e.Object);
            }
            else
            {
                SelectedItems.Clear();
                SelectedItems.Add(e.Object);
            }

            //RefreshSelectedItems(treeView);
            DelayedSelectedEventTimer.Start();
        }

        private void Instance_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {
            EmdFile?.RefreshValues();
            TextureViewModel?.UpdateProperties();

            if(e.UndoGroup == UndoGroup.EMD)
                UpdateTransformValues();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            DelayedSelectedEventTimer.Start();
        }

        private void OnSelectedItemChanged()
        {
            SortSelectedItems();

            if (SelectedTexture != null)
            {
                if(EmdFile != null)
                {
                    TextureViewModel = new EmdTextureViewModel(SelectedTexture, EmdFile.GetParentSubmesh(SelectedTexture), EmdFile, ModelScene.EmbFile);
                }
                else if(EmoFile != null)
                {
                    TextureViewModel = new EmdTextureViewModel(SelectedTexture, EmoFile.GetParentSubmeshGroup(SelectedTexture), EmoFile, ModelScene.EmbFile);
                }
                NotifyPropertyChanged(nameof(TextureViewModel));
            }

            if(EmdFile != null)
            {
                NotifyPropertyChanged(nameof(ModelNameVisibility));
                NotifyPropertyChanged(nameof(MeshNameVisibility));
                NotifyPropertyChanged(nameof(MeshBoneNameVisibility));
                NotifyPropertyChanged(nameof(SubmeshNameVisibility));
                NotifyPropertyChanged(nameof(SelectedModelName));
                NotifyPropertyChanged(nameof(SelectedMeshName));
                NotifyPropertyChanged(nameof(SelectedMeshBoneName));
                NotifyPropertyChanged(nameof(SelectedSubmeshName));
            }
            else
            {
                NotifyPropertyChanged(nameof(EMO_SelectedPart));
                NotifyPropertyChanged(nameof(EMO_SelectedEmgFile));
                NotifyPropertyChanged(nameof(EMO_SelectedMesh));
                NotifyPropertyChanged(nameof(EMO_SelectedSubmesh));
                NotifyPropertyChanged(nameof(EMO_PartVisibility));
                NotifyPropertyChanged(nameof(EMO_EmgFileVisibility));
                NotifyPropertyChanged(nameof(EMO_MeshVisibility));
                NotifyPropertyChanged(nameof(EMO_SubmeshVisibility));
                NotifyPropertyChanged(nameof(EMO_SelectedBone));
                NotifyPropertyChanged(nameof(EMO_SelectedSubmeshName));
            }

            NotifyPropertyChanged(nameof(TransformVisibility));
            NotifyPropertyChanged(nameof(TextureVisibility));

            _initialRot = SimdVector3.Zero;
            TryEnableModelGizmo();
        }

        private void TryEnableModelGizmo()
        {
            if (Viewport.Instance != null)
            {
                Viewport.Instance.ModelGizmo.SetCallback(TransformOperationBegin, TransformOperationComplete);
                UpdateTransformValues();

                var submeshes = ModelScene.GetSelectedSubmeshes();

                if(submeshes?.Count > 0 && EMO_SelectedSubmesh == null) //EMO has its vertex data on the parent mesh - not the submesh like in EMD. So EMO submeshes shouldn't be transformed by themselves.
                {
                    Viewport.Instance.ModelGizmo.SetContext(ModelScene.GetSourceModel(), submeshes, GetAttachBone(submeshes));
                }
                else
                {
                    Viewport.Instance.ModelGizmo.RemoveContext();
                }
            }
        }

        private void TransformOperationBegin()
        {
            ModelScene.AlwaysUpdateBoundingBox = true;
        }

        private void TransformOperationComplete()
        {
            ModelScene.AlwaysUpdateBoundingBox = false;
            UpdateTransformValues();
        }

        private void UpdateTransformValues()
        {
            var submeshes = ModelScene.Model.GetAllSubmeshesFromSourceObject(SelectedItem);

            if(submeshes?.Count > 0 )
            {
                SimdVector3 centerPos = Xv2Submesh.CalculateCenter(submeshes);

                Matrix4x4 matrix = submeshes[0].Transform;

                if(ModelScene.Skeleton != null)
                {
                    var attachBone = GetAttachBone(submeshes);

                    if(attachBone != null)
                        matrix *= attachBone.AbsoluteAnimationMatrix;
                }

                if (Matrix4x4.Decompose(matrix, out SimdVector3 _scale, out SimdQuaternion _rot, out SimdVector3 _translation))
                {
                    _currentPos = _translation + centerPos;
                    _currentScale = _scale;
                    _currentRot = _rot.ToEuler();
                }

                NotifyPropertyChanged(nameof(PosX));
                NotifyPropertyChanged(nameof(PosY));
                NotifyPropertyChanged(nameof(PosZ));
                NotifyPropertyChanged(nameof(RotX));
                NotifyPropertyChanged(nameof(RotY));
                NotifyPropertyChanged(nameof(RotZ));
                NotifyPropertyChanged(nameof(ScaleX));
                NotifyPropertyChanged(nameof(ScaleY));
                NotifyPropertyChanged(nameof(ScaleZ));
                ModelScene.IsBoundingBoxDirty = true;
            }
        }

        private void DoDirectTransform()
        {
            SimdVector3 newPos = _currentPos;
            SimdVector3 newRot = _currentRot - _initialRot;
            SimdVector3 newScale = _currentScale;
            _initialRot = _currentRot;

            var submeshes = ModelScene.GetSelectedSubmeshes();
            SimdVector3 center = Xv2Submesh.CalculateCenter(submeshes);
            newPos -= center;

            if (submeshes?.Count > 0)
            {
                //Create a inverse matrix so we can strip the skeleton from the values if one exists (NSK/EMO)
                Matrix4x4 invSkeletonMarix = Matrix4x4.Identity;

                if (ModelScene.Skeleton != null)
                {
                    var attachBone = GetAttachBone(submeshes);

                    if (attachBone != null)
                    {
                        invSkeletonMarix = attachBone.AbsoluteAnimationMatrix;
                    }
                }

                if (SceneManager.PivotPoint == PivotPoint.Center)
                    invSkeletonMarix *= Matrix4x4.CreateTranslation(center);

                newRot.ClampEuler();
                newScale.ClampScale();

                Matrix4x4 deltaMatrix = MathHelpers.Invert(submeshes[0].Transform * invSkeletonMarix);
                deltaMatrix *= Matrix4x4.CreateScale(newScale);
                deltaMatrix *= Matrix4x4.CreateFromQuaternion(newRot.EulerToQuaternion());
                deltaMatrix *= Matrix4x4.CreateTranslation(newPos);
                deltaMatrix *= invSkeletonMarix;

                foreach(var submesh in submeshes)
                {
                    submesh.Transform *= deltaMatrix;
                }

                ModelTransformOperation.ApplyTransformation(submeshes, ModelScene.GetSourceModel());

                UpdateTransformValues();
            }
        }

        private Xv2Bone GetAttachBone(IList<Xv2Submesh> submeshes)
        {
            return submeshes[0].Parent.AttachBone;
        }

        #region Commands
        public RelayCommand DeleteEmoPartCommand => new RelayCommand(ModelScene.DeleteSelectedEmoPart, ModelScene.IsEmoPartSelected);

        public RelayCommand DeleteModelCommand => new RelayCommand(ModelScene.DeleteSelectedModels, ModelScene.IsModelSelected);

        public RelayCommand DeleteMeshCommand => new RelayCommand(ModelScene.DeleteSelectedMeshes, ModelScene.IsMeshSelected);
        
        public RelayCommand DeleteSubmeshCommand => new RelayCommand(ModelScene.DeleteSelectedSubmeshes, ModelScene.IsSubmeshSelected);
        
        public RelayCommand DeleteTextureCommand => new RelayCommand(ModelScene.DeleteSelectedTextures, ModelScene.IsTextureSelected);

        public RelayCommand CopyModelCommand => new RelayCommand(ModelScene.CopySelectedModels, ModelScene.IsModelSelected);

        public RelayCommand CopyMeshCommand => new RelayCommand(ModelScene.CopySelectedMeshes, ModelScene.IsMeshSelected);
 
        public RelayCommand CopySubmeshCommand => new RelayCommand(ModelScene.CopySelectedSubmeshes, ModelScene.IsSubmeshSelected);

        public RelayCommand CopyTextureCommand => new RelayCommand(CopyTexture, ModelScene.IsTextureSelected);
        private void CopyTexture()
        {
            Clipboard.SetData(ClipboardConstants.EmdTextureSampler, SelectedTexture);
        }

        public RelayCommand PasteModelCommand => new RelayCommand(ModelScene.PasteModels, ModelScene.CanPasteModel);
        
        public RelayCommand PasteMeshCommand => new RelayCommand(ModelScene.PasteMeshes, ModelScene.CanPasteMesh);
        
        public RelayCommand PasteSubmeshCommand => new RelayCommand(ModelScene.PasteSubmeshes, ModelScene.CanPasteSubmesh);
        
        public RelayCommand PasteTextureCommand => new RelayCommand(PasteTexture, CanPasteTexture);
        private void PasteTexture()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (EmdFile != null)
            {
                EMD_Submesh submesh = SelectedSubmesh != null ? SelectedSubmesh : EmdFile.GetParentSubmesh(SelectedTexture);

                if (submesh.TextureSamplerDefs.Count >= 4)
                {
                    MessagePrompt.Show("Cannot add anymore Texture Samplers.", "Maximum Texture Samplers", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                    return;
                }

                EMD_TextureSamplerDef texture = (EMD_TextureSamplerDef)Clipboard.GetData(ClipboardConstants.EmdTextureSampler);

                undos.Add(new UndoableListAdd<EMD_TextureSamplerDef>(submesh.TextureSamplerDefs, texture));
                submesh.TextureSamplerDefs.Add(texture);

                undos.Add(new UndoActionDelegate(EmdFile, nameof(EmdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, texture, submesh)));
                EmdFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submesh);
            }
            else if(EmoFile != null)
            {
                EMG_SubmeshGroup submesh = EMO_SelectedSubmesh != null ? EMO_SelectedSubmesh : EmoFile.GetParentSubmeshGroup(SelectedTexture);

                if (submesh.TextureSamplerDefs.Count >= 4)
                {
                    MessagePrompt.Show("Cannot add anymore Texture Samplers.", "Maximum Texture Samplers", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                    return;
                }

                EMD_TextureSamplerDef texture = (EMD_TextureSamplerDef)Clipboard.GetData(ClipboardConstants.EmdTextureSampler);

                undos.Add(new UndoableListAdd<EMD_TextureSamplerDef>(submesh.TextureSamplerDefs, texture));
                submesh.TextureSamplerDefs.Add(texture);

                undos.Add(new UndoActionDelegate(EmoFile, nameof(EmoFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, texture, submesh)));
                EmoFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submesh);
            }
                

            UndoManager.Instance.AddCompositeUndo(undos, "Paste Texture Sampler");
        }

        public RelayCommand AddTextureCommand => new RelayCommand(AddTexture, CanAddTexture);
        private void AddTexture()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (EmdFile != null)
            {
                EMD_Submesh submesh = SelectedSubmesh != null ? SelectedSubmesh : EmdFile.GetParentSubmesh(SelectedTexture);

                if (submesh.TextureSamplerDefs.Count >= 4)
                {
                    MessagePrompt.Show("Cannot add anymore Texture Samplers.", "Maximum Texture Samplers", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                    return;
                }

                EMD_TextureSamplerDef texture = new EMD_TextureSamplerDef();

                undos.Add(new UndoableListAdd<EMD_TextureSamplerDef>(submesh.TextureSamplerDefs, texture));
                submesh.TextureSamplerDefs.Add(texture);

                undos.Add(new UndoActionDelegate(EmdFile, nameof(EmdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, texture, submesh)));
                EmdFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submesh);
            }
            else if(EmoFile != null)
            {
                EMG_SubmeshGroup submesh = EMO_SelectedSubmesh != null ? EMO_SelectedSubmesh : EmoFile.GetParentSubmeshGroup(SelectedTexture);

                if (submesh.TextureSamplerDefs.Count >= 4)
                {
                    MessagePrompt.Show("Cannot add anymore Texture Samplers.", "Maximum Texture Samplers", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                    return;
                }

                EMD_TextureSamplerDef texture = new EMD_TextureSamplerDef();

                undos.Add(new UndoableListAdd<EMD_TextureSamplerDef>(submesh.TextureSamplerDefs, texture));
                submesh.TextureSamplerDefs.Add(texture);

                undos.Add(new UndoActionDelegate(EmoFile, nameof(EmoFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, texture, submesh)));
                EmoFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submesh);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "Add Texture Sampler");
        }

        public RelayCommand GotoTextureCommand => new RelayCommand(GotoTexture, () => TextureViewModel?.SelectedEmbEntry != null);
        private void GotoTexture()
        {
            var window = WindowHelper.GetActiveEmbForm(ModelScene.EmbFile);

            if (window == null)
            {
                window = new EmbEditForm(ModelScene.EmbFile, TextureEditorType.Character, Path.GetFileName(ModelScene.EmbPath));
                window.Show();
            }
            else
            {
                window.Focus();
            }

            window.SelectTexture(ModelScene.EmbFile.GetEntry(SelectedTexture.EmbIndex));
        }

        public RelayCommand GotoMaterialCommand => new RelayCommand(GotoMaterial, CanGotoMaterial);
        private void GotoMaterial()
        {
            var window = WindowHelper.GetActiveEmmForm(ModelScene.EmmFile);

            if (window == null)
            {
                window = new MaterialsEditorForm(ModelScene.EmmFile, Path.GetFileName(ModelScene.EmmPath));
                window.Show();
            }
            else
            {
                window.Focus();
            }

            if(EmdFile != null)
            {
                window.SelectMaterial(ModelScene.EmmFile.GetMaterial(SelectedSubmeshName));
            }
            else if(EmoFile != null && EMO_SelectedSubmesh != null)
            {
                window.SelectMaterial(ModelScene.EmmFile.GetMaterial(EMO_SelectedSubmesh.MaterialName));
            }
        }

        public RelayCommand LookAtObjectCommand => new RelayCommand(LookAtObject, IsAnythingSelected);
        private void LookAtObject()
        {
            if(SelectedTexture == null)
            {
                Viewport.Instance.Camera.LookAt(ModelScene.SelectedBoundingBox);
            }
        }
        
        public RelayCommand ApplyScaleCommand => new RelayCommand(DoDirectTransform, () => _currentScale != SimdVector3.One);
        
        private bool CanPasteTexture()
        {
            return Clipboard.ContainsData(ClipboardConstants.EmdTextureSampler) && (SelectedSubmesh != null || SelectedTexture != null || EMO_SelectedSubmesh != null);
        }

        private bool CanAddTexture()
        {
            return SelectedSubmesh != null || SelectedTexture != null || EMO_SelectedSubmesh != null;
        }
        
        private bool CanGotoMaterial()
        {
            //Checks if submesh is selected, and then if a material exists for it
            if (!ModelScene.IsSubmeshSelected() && !ModelScene.IsEmgSubmeshSelected()) return false;
            string matNam = EmdFile != null ? SelectedSubmeshName : EMO_SelectedSubmesh?.MaterialName;
            return ModelScene.EmmFile?.GetMaterial(matNam) != null;
        }
        #endregion


        #region InputCommands
        public RelayCommand DeleteCommand => new RelayCommand(DeleteAnything, IsAnythingSelected);
        private void DeleteAnything()
        {
            if (!ModelScene.HasOnlyOneSelectedType()) return;

            if (ModelScene.IsEmoPartSelected())
            {
                ModelScene.DeleteSelectedEmoPart();
            }
            else if (ModelScene.IsModelSelected())
            {
                ModelScene.DeleteSelectedModels();
            }
            else if (ModelScene.IsMeshSelected())
            {
                ModelScene.DeleteSelectedMeshes();
            }
            else if (ModelScene.IsSubmeshSelected())
            {
                ModelScene.DeleteSelectedSubmeshes();
            }
            else if (ModelScene.IsTextureSelected())
            {
                ModelScene.DeleteSelectedTextures();
            }
        }

        public RelayCommand CopyCommand => new RelayCommand(CopyAnything, IsAnythingSelected);
        private void CopyAnything()
        {
            if (ModelScene.IsModelSelected())
            {
                ModelScene.CopySelectedModels();
            }
            else if (ModelScene.IsMeshSelected())
            {
                ModelScene.CopySelectedMeshes();
            }
            else if (ModelScene.IsSubmeshSelected())
            {
                ModelScene.CopySelectedSubmeshes();
            }
            else if (SelectedTexture != null)
            {
                CopyTexture();
            }
        }

        public RelayCommand PasteCommand => new RelayCommand(PasteAnything, CanPasteAnything);
        private void PasteAnything()
        {
            if (ModelScene.CanPasteModel())
            {
                ModelScene.PasteModels();
            }
            else if (ModelScene.CanPasteMesh())
            {
                ModelScene.PasteMeshes();
            }
            else if (ModelScene.CanPasteSubmesh())
            {
                ModelScene.PasteSubmeshes();
            }
            else if (CanPasteTexture())
            {
                PasteTexture();
            }
        }


        private bool CanPasteAnything()
        {
            return ModelScene.CanPasteModel() || ModelScene.CanPasteMesh() || ModelScene.CanPasteSubmesh() || CanPasteTexture();
        }

        private bool IsAnythingSelected()
        {
            return ModelScene.SelectedItems.Count > 0;
        }
        #endregion

        #region SelectedItemSorting
        private void SortSelectedItems()
        {
            //Sort selected items so it only contains one type of object (no mixing of model, mesh, texture etc)
            if (SelectedItems.Count > 0)
            {
                Type currentType = SelectedItems[SelectedItems.Count - 1].GetType();

                for (int i = SelectedItems.Count - 2; i >= 0; i--)
                {
                    if (SelectedItems[i].GetType() != currentType)
                    {
                        SelectedItems.RemoveAt(i);
                    }
                }
            }

            RefreshSelectedItems(EmdFile != null ? treeView : emoTreeView);
        }

        private static void RefreshSelectedItems(TreeView treeView)
        {
            IList selectedItems = LB_Common.Utils.TreeViewMultipleSelectionAttached.GetSelectedItems(treeView);
            RefreshSelectedItems(selectedItems, treeView.Items, treeView.ItemContainerGenerator);
        }

        private static void RefreshSelectedItems(IList selectedItems, IEnumerable items, ItemContainerGenerator treeView)
        {
            foreach (var item in items)
            {
                TreeViewItem tvi = treeView.ContainerFromItem(item) as TreeViewItem;

                if (tvi != null)
                {
                    LB_Common.Utils.TreeViewMultipleSelectionAttached.SetIsItemSelected(tvi, selectedItems.Contains(item));

                    if (item is EMD_Model emdModel)
                    {
                        RefreshSelectedItems(selectedItems, emdModel.Meshes, tvi.ItemContainerGenerator);
                    }
                    else if (item is EMD_Mesh emdMesh)
                    {
                        RefreshSelectedItems(selectedItems, emdMesh.Submeshes, tvi.ItemContainerGenerator);
                    }
                    else if (item is EMD_Submesh emdSubmesh)
                    {
                        RefreshSelectedItems(selectedItems, emdSubmesh.TextureSamplerDefs, tvi.ItemContainerGenerator);
                    }
                    else if (item is EMO_Part emoPart)
                    {
                        RefreshSelectedItems(selectedItems, emoPart.EmgFiles, tvi.ItemContainerGenerator);
                    }
                    else if (item is EMG_File emg)
                    {
                        RefreshSelectedItems(selectedItems, emg.EmgMeshes, tvi.ItemContainerGenerator);
                    }
                    else if (item is EMG_Mesh emgMesh)
                    {
                        RefreshSelectedItems(selectedItems, emgMesh.SubmeshGroups, tvi.ItemContainerGenerator);
                    }
                    else if (item is EMG_SubmeshGroup emgSubmesh)
                    {
                        RefreshSelectedItems(selectedItems, emgSubmesh.TextureSamplerDefs, tvi.ItemContainerGenerator);
                    }
                }


            }
        }
        #endregion

        private void treeView_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DelayedSelectedEventTimer.Start();
        }
    }
}