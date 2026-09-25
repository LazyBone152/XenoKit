using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using XenoKit.Editor;
using XenoKit.Editor.Data;
using XenoKit.Engine.Animation;
using XenoKit.Engine.Objects;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using Xv2CoreLib;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EMG;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMO;
using Xv2CoreLib.ESK;
using Xv2CoreLib.Resource.App;
using Xv2CoreLib.Resource.UndoRedo;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine.Model
{
    public class ModelScene : RenderScene, IDynamicTabObject
    {
        public event EventHandler SelectedSubmeshesChanged;

        private bool PathsRelativeToGame = false;
        public string ModelFilePath { get; private set; }
        public string EmbPath { get; private set; }
        public string EmmPath { get; private set; }
        public string DytPath { get; private set; }

        public EMD_File EmdFile => Model.Type == ModelType.Nsk ? Model.SourceNskFile.EmdFile : Model.SourceEmdFile;
        public EMO_File EmoFile => Model.SourceEmoFile;
        public EMB_TextureFile EmbFile { get; private set; }
        public EMM_File EmmFile { get; private set; }
        public EMB_TextureFile DytFile { get; private set; }
        public ESK_File EskFile { get; private set; }

        public Xv2ModelFile Model { get; private set; }
        private Xv2Texture[] Textures { get; set; }
        private Xv2Texture[] DytTexture { get; set; }
        private Xv2ShaderEffect[] Materials { get; set; }
        public Xv2Skeleton Skeleton => Model.Skeleton;

        private DrawableBoundingBox DrawableAABB { get; set; }
        public BoundingBox SelectedBoundingBox { get; private set; }
        public bool IsBoundingBoxDirty = false;
        public bool AlwaysUpdateBoundingBox = false;

        private SamplerInfo DytSampler;

        private ShaderType ShaderType { get; set; } = ShaderType.Chara;
        public int DytIndex = 0;
        private bool IsMaterialsDirty = false;
        private bool IsTexturesDirty = false;
        private bool IsDytDirty = false;

        public ObservableCollection<object> SelectedItems { get; private set; } = new ObservableCollection<object>();
        private readonly List<Xv2Submesh> _selectedSubmeshes = new List<Xv2Submesh>();
        private readonly ReadOnlyCollection<Xv2Submesh> _readonlySelectedSubmeshes;

        public event ModelSceneSelectEventHandler ViewportSelectEvent;
        public event ViewportInputEventHandler ViewportInputEvent;

        //Mouse-over objects
        private Xv2Model _mouseOverModel = null;
        private Xv2Mesh _mouseOverMesh = null;
        private Xv2Submesh _mouseOverSubmesh = null;

        public ModelScene(Xv2ModelFile model)
        {
            _readonlySelectedSubmeshes = new ReadOnlyCollection<Xv2Submesh>(_selectedSubmeshes);
            Model = model;
            Model.MaterialsChanged += Model_MaterialsChanged;
            Model.ModelModified += Model_ModelModified;

            RenderPipelineStage = model.Type == ModelType.Emo ? Rendering.RenderPipelineStage.EffectInitial : Rendering.RenderPipelineStage.ModelMain;

            DytSampler = new SamplerInfo()
            {
                type = SamplerType.Sampler2D,
                textureSlot = 4,
                samplerSlot = 4,
                name = ShaderManager.GetSamplerName(4),
                state = new SamplerState()
                {
                    AddressU = TextureAddressMode.Clamp,
                    AddressV = TextureAddressMode.Clamp,
                    AddressW = TextureAddressMode.Wrap,
                    BorderColor = new Color(1, 1, 1, 1),
                    Filter = TextureFilter.LinearMipPoint,
                    MaxAnisotropy = 1,
                    MaxMipLevel = 1,
                    MipMapLevelOfDetailBias = 0,
                    Name = ShaderManager.GetSamplerName(4)
                }
            };

            DrawableAABB = new DrawableBoundingBox();
            SelectedItems.CollectionChanged += SelectedItems_CollectionChanged;
        }

        private void SelectedItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            IsBoundingBoxDirty = true;

            //Create list of Xv2Submeshes from SelectedItems (which are source objects - EMD, EMO etc)
            _selectedSubmeshes.Clear();

            if(!IsTextureSelected() && !IsEmgSubmeshSelected())
                Model.GetAllSubmeshesFromSourceObjects(SelectedItems, _selectedSubmeshes);

            SelectedSubmeshesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetFiles(ShaderType type, EMB_TextureFile emb, EMM_File emm, EMB_TextureFile dyt = null, ESK_File eskFile = null)
        {
            ShaderType = type;
            EmbFile = emb;
            EmmFile = emm;
            DytFile = dyt;
            EskFile = eskFile;
            Initialize();
        }

        public void SetPaths(bool pathRelativeToGame, string modelFilePath, string embPath, string emmPath, string dytPath)
        {
            //Must be called once when the files are set, to enable saving
            PathsRelativeToGame = pathRelativeToGame;
            ModelFilePath = modelFilePath;
            EmbPath = embPath;
            EmmPath = emmPath;
            DytPath = dytPath;
        }

        public string GetSaveContextFileName() => Path.GetFileName(ModelFilePath);

        public bool CanSave() => true;

        public void Save()
        {
            string modelFilePath = PathsRelativeToGame ? FileManager.Instance.GetAbsolutePath(ModelFilePath) : ModelFilePath;
            string embPath = PathsRelativeToGame ? FileManager.Instance.GetAbsolutePath(EmbPath) : EmbPath;
            string emmPath = PathsRelativeToGame ? FileManager.Instance.GetAbsolutePath(EmmPath) : EmmPath;
            string dytPath = PathsRelativeToGame ? FileManager.Instance.GetAbsolutePath(DytPath) : DytPath;
            int numSaved = 0;

            if (!string.IsNullOrWhiteSpace(EmbPath) && EmbFile != null)
            {
                EmbFile.Save(embPath);
                numSaved++;
                Log.Add($"Saved \"{EmbPath}\"");
            }

            if (!string.IsNullOrWhiteSpace(EmmPath) && EmmPath != null)
            {
                EmmFile.SaveBinaryEmmFile(emmPath);
                numSaved++;
                Log.Add($"Saved \"{EmmPath}\"");
            }

            if (!string.IsNullOrWhiteSpace(DytPath) && DytFile != null)
            {
                DytFile.Save(dytPath);
                numSaved++;
                Log.Add($"Saved \"{DytPath}\"");
            }

            if (!string.IsNullOrWhiteSpace(ModelFilePath))
            {
                if (Path.GetExtension(ModelFilePath) == ".emd")
                {
                    Model.SourceEmdFile.Save(modelFilePath);
                }
                else if (Path.GetExtension(ModelFilePath) == ".nsk")
                {
                    Model.SourceNskFile.SaveFile(modelFilePath);
                }
                else if (Path.GetExtension(ModelFilePath) == ".emo")
                {
                    Model.SourceEmoFile.SaveFile(modelFilePath);
                }

                numSaved++;
                Log.Add($"Saved \"{ModelFilePath}\" ({numSaved} files saved)");
            }
        }

        private void Initialize()
        {
            if (EmmFile != null)
                EmmFile.MaterialsChanged += Model_MaterialsChanged;

            if (EmbFile != null)
                EmbFile.TexturesChanged += EmbFile_TexturesChanged;

            if (DytFile != null)
                DytFile.TexturesChanged += DytFile_TexturesChanged;

            InitMaterials();
            InitTextures();
            InitDyts();

        }

        private void InitDyts()
        {
            if (DytFile != null)
            {
                DytTexture = Xv2Texture.LoadTextureArray(DytFile);
            }
            else
            {
                DytTexture = null;
            }
        }

        private void InitTextures()
        {
            if (EmbFile != null)
            {
                Textures = Xv2Texture.LoadTextureArray(EmbFile);
            }
            else
            {
                Textures = null;
            }

            IsTexturesDirty = false;
        }

        private void InitMaterials()
        {
            Materials = Xv2ShaderEffect.LoadMaterials(EmmFile, ShaderType);
            Model.InitMaterialIndex(Materials);
            IsMaterialsDirty = false;
        }

        public override void Update()
        {
            if (IsMaterialsDirty)
            {
                InitMaterials();
            }

            if (IsTexturesDirty)
            {
                InitTextures();
            }

            if (IsDytDirty)
            {
                InitDyts();
            }

            if (IsBoundingBoxDirty || AlwaysUpdateBoundingBox)
            {
                CalculateBoundingBox();
            }

            if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F) && IsAnyModelObjectSelected())
            {
                Viewport.Instance.Camera.LookAt(SelectedBoundingBox);
            }

            //Handle in-viewport mesh selection
            if (Viewport.Instance.ViewportIsFocused)
            {
                Ray ray = EngineUtils.CalculateRay(Input.MousePosition);
                _mouseOverSubmesh = Model.TraverseBVH(ray);
                _mouseOverMesh = null;
                _mouseOverModel = null;

                if (_mouseOverSubmesh != null)
                {
                    string name = null;

                    switch (SceneManager.ViewportSelectionMode)
                    {
                        case ViewportSelectionMode.Model:
                            {
                                _mouseOverModel = _mouseOverSubmesh.Parent.Parent;
                                name = _mouseOverModel.Name;

                                if (Input.IsMouseLeftClickDown)
                                    ViewportSelectEvent?.Invoke(this, new ModelSceneSelectEventArgs(_mouseOverModel.GetSourceModelObject()));
                            }
                            break;
                        case ViewportSelectionMode.Mesh:
                            {
                                _mouseOverMesh = _mouseOverSubmesh.Parent;
                                name = _mouseOverMesh.Name;

                                if (Input.IsMouseLeftClickDown)
                                    ViewportSelectEvent?.Invoke(this, new ModelSceneSelectEventArgs(_mouseOverMesh.GetSourceMeshObject()));
                            }
                            break;
                        case ViewportSelectionMode.Submesh:
                            {
                                name = _mouseOverSubmesh.Name;

                                if (Input.IsMouseLeftClickDown)
                                    ViewportSelectEvent?.Invoke(this, new ModelSceneSelectEventArgs(_mouseOverSubmesh.GetSourceSubmeshObject()));
                            }
                            break;
                    }

                    if (name != null)
                        Viewport.Instance.TextRenderer.DrawOnScreenText(name, Input.ScaledMousePosition + new System.Numerics.Vector2(20 * SettingsManager.settings.XenoKit_SuperSamplingFactor, 0), Color.White, true, false);

                }
                else if(_mouseOverSubmesh == null && Input.IsMouseLeftClickDown)
                {
                    ViewportSelectEvent?.Invoke(this, new ModelSceneSelectEventArgs(null));
                }
            }
            else
            {
                _mouseOverSubmesh = null;
                _mouseOverMesh = null;
                _mouseOverModel = null;
            }

            HandleInputEvents();
        }

        public override void Draw()
        {
            if (DytTexture?.Length > 0)
            {
                GraphicsDevice.SamplerStates[4] = DytSampler.state;
                GraphicsDevice.Textures[4] = DytIndex < DytTexture.Length && DytIndex >= 0 ? DytTexture[DytIndex].Texture : DytTexture[0].Texture;
            }

            Model.Draw(Matrix4x4.Identity, 0, Materials, Textures, DytTexture, DytIndex, Skeleton);
            
            if (SceneManager.ShowModelEditorHighlights)
            {
                if (IsAnyModelObjectSelected())
                    DrawableAABB.Draw(Matrix4x4.Identity, SelectedBoundingBox);

                //Draw highlighted (selected) meshes
                Matrix4x4 world = Matrix4x4.Identity;

                switch (SceneManager.ViewportSelectionMode)
                {
                    case ViewportSelectionMode.Model:
                        if (_mouseOverModel != null)
                        {
                            _mouseOverModel.Draw(ref world, 0, DefaultShaders.WhiteWireframe);
                        }
                        break;
                    case ViewportSelectionMode.Mesh:
                        if (_mouseOverMesh != null)
                        {
                            _mouseOverMesh.Draw(ref world, 0, DefaultShaders.WhiteWireframe);
                        }
                        break;
                    case ViewportSelectionMode.Submesh:
                        if (_mouseOverSubmesh != null)
                        {
                            _mouseOverSubmesh.Draw(ref world, 0, DefaultShaders.WhiteWireframe);
                        }
                        break;
                }

                foreach (var selectedMesh in _selectedSubmeshes)
                {
                    selectedMesh.Draw(ref world, 0, DefaultShaders.RedWireframe);
                }
            }
        }

        public override void DrawPass(bool normalPass)
        {
            if (normalPass && ShaderType == ShaderType.Stage) return; //No normal pass for stages

            Xv2ShaderEffect shader = RenderSystem.NORMAL_FADE_WATERDEPTH_W_M;

            if (ShaderType == ShaderType.Stage)
            {
                shader = RenderSystem.ShadowModel;
            }
            else if (ShaderType == ShaderType.Chara)
            {
                shader = RenderSystem.ShadowModel_W;
            }

            Model.Draw(Matrix4x4.Identity, 0, shader);
        }

        private void CalculateBoundingBox()
        {
            SelectedBoundingBox = Xv2Submesh.CalculateBoundingBox(_selectedSubmeshes);
            IsBoundingBoxDirty = false;
        }

        private void HandleInputEvents()
        {
            if (Input.WasKeyReleased(Keys.Delete))
                ViewportInputEvent?.Invoke(this, new ViewportInputEventArgs(Keys.Delete));

            if (Input.WasKeyReleased(Keys.C))
                ViewportInputEvent?.Invoke(this, new ViewportInputEventArgs(Keys.C));

            if (Input.WasKeyReleased(Keys.V))
                ViewportInputEvent?.Invoke(this, new ViewportInputEventArgs(Keys.V));
        }

        #region Edit Methods
        public void DeleteSelectedEmoPart()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            List<Tuple<object, object>> deletedList = new List<Tuple<object, object>>();

            foreach (var item in SelectedItems)
            {
                if (item is EMO_Part emoPart)
                {
                    undos.Add(new UndoableListRemove<EMO_Part>(Model.SourceEmoFile.Parts, emoPart));
                    Model.SourceEmoFile.Parts.Remove(emoPart);

                    deletedList.Add(new Tuple<object, object>(emoPart, null));
                }
            }

            Tuple<object, object>[] deletedArray = deletedList.ToArray();
            undos.Add(new UndoActionDelegate(Model.SourceEmoFile, nameof(Model.SourceEmoFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Remove, deletedList)));
            Model.SourceEmoFile.TriggerModelModifiedEvent(EditTypeEnum.Remove, deletedList, null);

            UndoManager.Instance.AddCompositeUndo(undos, "Delete Model");
            SelectedItems.Clear();
        }

        public void DeleteSelectedModels()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            List<Tuple<object, object>> deletedList = new List<Tuple<object, object>>();

            foreach (var item in SelectedItems)
            {
                if (item is EMD_Model emdModel)
                {
                    undos.Add(new UndoableListRemove<EMD_Model>(EmdFile.Models, emdModel));
                    EmdFile.Models.Remove(emdModel);

                    deletedList.Add(new Tuple<object, object>(emdModel, null));
                }
                else if (item is EMG_File emgFile)
                {
                    EMO_Part emoPart = Model.SourceEmoFile.GetParentPart(emgFile);
                    undos.Add(new UndoableListRemove<EMG_File>(emoPart.EmgFiles, emgFile));
                    emoPart.EmgFiles.Remove(emgFile);

                    deletedList.Add(new Tuple<object, object>(emgFile, null));
                }
            }

            Tuple<object, object>[] deletedArray = deletedList.ToArray();
            undos.Add(new UndoActionDelegate(GetSourceModel(), nameof(EmdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Remove, deletedList)));
            GetSourceModel().TriggerModelModifiedEvent(EditTypeEnum.Remove, deletedList, null);

            UndoManager.Instance.AddCompositeUndo(undos, "Delete Model");
            SelectedItems.Clear();
        }

        public void DeleteSelectedMeshes()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            List<Tuple<object, object>> deletedList = new List<Tuple<object, object>>();

            foreach (var item in SelectedItems)
            {
                if (item is EMD_Mesh emdMesh)
                {
                    EMD_Model parentModel = EmdFile.GetParentModel(emdMesh);

                    undos.Add(new UndoableListRemove<EMD_Mesh>(parentModel.Meshes, emdMesh));
                    parentModel.Meshes.Remove(emdMesh);

                    deletedList.Add(new Tuple<object, object>(emdMesh, parentModel));
                }
                else if(item is EMG_Mesh emgMesh)
                {
                    EMG_File parentEmg = Model.SourceEmoFile.GetParentEmg(emgMesh);

                    undos.Add(new UndoableListRemove<EMG_Mesh>(parentEmg.EmgMeshes, emgMesh));
                    parentEmg.EmgMeshes.Remove(emgMesh);

                    deletedList.Add(new Tuple<object, object>(emgMesh, parentEmg));
                }
            }

            Tuple<object, object>[] deletedArray = deletedList.ToArray();
            undos.Add(new UndoActionDelegate(GetSourceModel(), nameof(EmdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Remove, deletedList)));
            GetSourceModel().TriggerModelModifiedEvent(EditTypeEnum.Remove, deletedList, null);

            UndoManager.Instance.AddCompositeUndo(undos, "Delete Mesh");
            SelectedItems.Clear();
        }

        public void DeleteSelectedSubmeshes()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            List<Tuple<object, object>> deletedList = new List<Tuple<object, object>>();

            foreach (var item in SelectedItems)
            {
                if (item is EMD_Submesh emdSubmesh)
                {
                    EMD_Mesh parentMesh = EmdFile.GetParentMesh(emdSubmesh);

                    undos.Add(new UndoableListRemove<EMD_Submesh>(parentMesh.Submeshes, emdSubmesh));
                    parentMesh.Submeshes.Remove(emdSubmesh);

                    deletedList.Add(new Tuple<object, object>(emdSubmesh, parentMesh));
                }
                else if (item is EMG_SubmeshGroup emgSubmeshGroup)
                {
                    EMG_Mesh parentMesh = Model.SourceEmoFile.GetParentMesh(emgSubmeshGroup);

                    undos.Add(new UndoableListRemove<EMG_SubmeshGroup>(parentMesh.SubmeshGroups, emgSubmeshGroup));
                    parentMesh.SubmeshGroups.Remove(emgSubmeshGroup);

                    deletedList.Add(new Tuple<object, object>(emgSubmeshGroup, parentMesh));
                }
            }

            Tuple<object, object>[] deletedArray = deletedList.ToArray();
            undos.Add(new UndoActionDelegate(GetSourceModel(), nameof(EmdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Remove, deletedList)));
            GetSourceModel().TriggerModelModifiedEvent(EditTypeEnum.Remove, deletedList, null);

            UndoManager.Instance.AddCompositeUndo(undos, "Delete Submesh");
            SelectedItems.Clear();
        }

        public void DeleteSelectedTextures()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var item in SelectedItems)
            {
                if (item is EMD_TextureSamplerDef texture)
                {
                    if(EmdFile != null)
                    {
                        EMD_Submesh parentSubmesh = EmdFile.GetParentSubmesh(texture);

                        undos.Add(new UndoableListRemove<EMD_TextureSamplerDef>(parentSubmesh.TextureSamplerDefs, texture));
                        parentSubmesh.TextureSamplerDefs.Remove(texture);

                        undos.Add(new UndoActionDelegate(GetSourceModel(), nameof(EmdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, texture, parentSubmesh)));
                        GetSourceModel().TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, parentSubmesh);
                    }
                    else if(Model.SourceEmoFile != null)
                    {
                        EMG_SubmeshGroup parentSubmesh = Model.SourceEmoFile.GetParentSubmeshGroup(texture);

                        undos.Add(new UndoableListRemove<EMD_TextureSamplerDef>(parentSubmesh.TextureSamplerDefs, texture));
                        parentSubmesh.TextureSamplerDefs.Remove(texture);

                        undos.Add(new UndoActionDelegate(GetSourceModel(), nameof(EmdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, texture, parentSubmesh)));
                        GetSourceModel().TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, parentSubmesh);
                    }

                }
            }

            UndoManager.Instance.AddCompositeUndo(undos, "Delete Texture Sampler");
            SelectedItems.Clear();
        }

        public void CopySelectedModels()
        {
            if (!IsModelSelected()) return;

            if (SelectedItems[0] is EMD_Model)
            {
                EMD_Model[] models = GetSelectedItemsAsTypeArray<EMD_Model>();
                SerializedModel serializedModel = new SerializedModel(models, EmmFile, EmbFile);
                Clipboard.SetData(SerializedModel.CLIPBOARD_MODEL, serializedModel);
            }
            else if (SelectedItems[0] is EMG_File)
            {
                EMG_File[] emgFiles = GetSelectedItemsAsTypeArray<EMG_File>();
                SerializedModel serializedModel = new SerializedModel(emgFiles, EmmFile, EmbFile, EmoFile.Skeleton);
                Clipboard.SetData(SerializedModel.CLIPBOARD_MODEL, serializedModel);
            }
        }

        public void CopySelectedMeshes()
        {
            if (!IsMeshSelected()) return;

            if (SelectedItems[0] is EMD_Mesh)
            {
                EMD_Mesh[] meshes = GetSelectedItemsAsTypeArray<EMD_Mesh>();
                SerializedModel serializedModel = new SerializedModel(meshes, EmmFile, EmbFile);
                Clipboard.SetData(SerializedModel.CLIPBOARD_MESH, serializedModel);
            }
            else if (SelectedItems[0] is EMG_Mesh)
            {
                EMG_Mesh[] meshes = GetSelectedItemsAsTypeArray<EMG_Mesh>();
                SerializedModel serializedModel = new SerializedModel(meshes, EmmFile, EmbFile, EmoFile.Skeleton);
                Clipboard.SetData(SerializedModel.CLIPBOARD_MESH, serializedModel);
            }
        }

        public void CopySelectedSubmeshes()
        {
            if (!IsSubmeshSelected()) return;

            if (SelectedItems[0] is EMD_Submesh)
            {
                EMD_Submesh[] models = GetSelectedItemsAsTypeArray<EMD_Submesh>();
                SerializedModel serializedModel = new SerializedModel(models, EmmFile, EmbFile);
                Clipboard.SetData(SerializedModel.CLIPBOARD_EMD_SUBMESH, serializedModel);
            }
        }

        public void PasteModels()
        {
            if (!Clipboard.ContainsData(SerializedModel.CLIPBOARD_MODEL)) return;

            SerializedModel serializedModel = (SerializedModel)Clipboard.GetData(SerializedModel.CLIPBOARD_MODEL);
            List<IUndoRedo> undos = serializedModel.PasteTexturesAndMaterials(EmbFile, EmmFile);
            object context = null;

            if (EmdFile != null)
            {
                context = serializedModel.EmdModel;

                if (serializedModel.EmdModel == null)
                {
                    Log.Add("EMO to EMD model paste is not currently supported. Only mesh is.", LogType.Warning);
                    return;
                }

                foreach (var model in serializedModel.EmdModel)
                {
                    undos.Add(new UndoableListAdd<EMD_Model>(EmdFile.Models, model));
                    EmdFile.Models.Add(model);
                }
            }
            else if (EmoFile != null)
            {
                context = serializedModel.EmoModel;
                EMO_Part emoPart = SelectedItems[0] is EMO_Part ? SelectedItems[0] as EMO_Part : EmoFile.GetParentPart(SelectedItems[0] as EMG_File);

                if (emoPart == null) return;

                if (serializedModel.EmoModel == null)
                {
                    Log.Add("EMD to EMO model paste is not currently supported. Only mesh is.", LogType.Warning);
                    return;
                }

                foreach (var model in serializedModel.EmoModel)
                {
                    undos.Add(new UndoableListAdd<EMG_File>(emoPart.EmgFiles, model));
                    emoPart.EmgFiles.Add(model);
                }

            }

            undos.Add(new UndoActionDelegate(GetSourceModel(), nameof(EMD_File.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Add, context)));
            GetSourceModel().TriggerModelModifiedEvent(EditTypeEnum.Add, context, null);

            UndoManager.Instance.AddCompositeUndo(undos, "Paste Model");
        }

        public void PasteMeshes()
        {
            if (!Clipboard.ContainsData(SerializedModel.CLIPBOARD_MESH)) return;

            SerializedModel serializedModel = (SerializedModel)Clipboard.GetData(SerializedModel.CLIPBOARD_MESH);
            List<IUndoRedo> undos = serializedModel.PasteTexturesAndMaterials(EmbFile, EmmFile);
            object context = null;

            if (EmdFile != null)
            {
                EMD_Model parentModel = SelectedItems[0] is EMD_Model ? SelectedItems[0] as EMD_Model : EmdFile.GetParentModel(SelectedItems[0] as EMD_Mesh);
                context = parentModel;

                if (parentModel == null)
                    return;

                EMD_Mesh[] meshes = serializedModel.GetEmdMesh(EskFile?.Skeleton);

                foreach (var mesh in meshes)
                {
                    undos.Add(new UndoableListAdd<EMD_Mesh>(parentModel.Meshes, mesh));
                    parentModel.Meshes.Add(mesh);
                }
            }
            else if (EmoFile != null)
            {
                context = serializedModel.EmoMesh;
                EMG_File parentModel = SelectedItems[0] is EMG_File ? SelectedItems[0] as EMG_File : EmoFile.GetParentEmg(SelectedItems[0] as EMG_Mesh);
                context = parentModel;

                if (parentModel == null)
                    return;

                EMG_Mesh[] meshes = serializedModel.GetEmoMesh(EmoFile.Skeleton);

                foreach (var model in meshes)
                {
                    undos.Add(new UndoableListAdd<EMG_Mesh>(parentModel.EmgMeshes, model));
                    parentModel.EmgMeshes.Add(model);
                }

            }

            undos.Add(new UndoActionDelegate(GetSourceModel(), nameof(EMD_File.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Add, serializedModel.EmdMesh, context)));
            GetSourceModel().TriggerModelModifiedEvent(EditTypeEnum.Add, serializedModel.EmoMesh, context);

            UndoManager.Instance.AddCompositeUndo(undos, "Paste Mesh");
        }

        public void PasteSubmeshes()
        {
            if (Clipboard.ContainsData(SerializedModel.CLIPBOARD_EMD_SUBMESH))
            {
                SerializedModel serializedModel = (SerializedModel)Clipboard.GetData(SerializedModel.CLIPBOARD_EMD_SUBMESH);

                List<IUndoRedo> undos = serializedModel.PasteTexturesAndMaterials(EmbFile, EmmFile);
                EMD_Mesh parentMesh = SelectedItems[0] is EMD_Mesh ? SelectedItems[0] as EMD_Mesh : EmdFile.GetParentMesh(SelectedItems[0] as EMD_Submesh);

                if (parentMesh == null)
                    return;

                foreach (var submesh in serializedModel.EmdSubmesh)
                {
                    undos.Add(new UndoableListAdd<EMD_Submesh>(parentMesh.Submeshes, submesh));
                    parentMesh.Submeshes.Add(submesh);
                }

                parentMesh.RecalculateAABB(undos);
                undos.Add(new UndoActionDelegate(EmdFile, nameof(EmdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Add, serializedModel.EmdSubmesh, parentMesh)));
                EmdFile.TriggerModelModifiedEvent(EditTypeEnum.Add, serializedModel.EmdSubmesh, parentMesh);

                UndoManager.Instance.AddCompositeUndo(undos, "Paste Submesh");
            }
        }


        private T[] GetSelectedItemsAsTypeArray<T>()
        {
            List<T> items = new List<T>(SelectedItems.Count);

            foreach (var selectedItem in SelectedItems)
            {
                if (selectedItem is T item)
                {
                    items.Add(item);
                }
            }

            return items.ToArray();
        }

        public bool IsEmoPartSelected()
        {
            if (SelectedItems.Count < 1) return false;

            return SelectedItems[0] is EMO_Part;
        }

        public bool IsModelSelected()
        {
            if (SelectedItems.Count < 1) return false;

            return (SelectedItems[0] is EMD_Model || SelectedItems[0] is EMG_File);
        }

        public bool IsMeshSelected()
        {
            if (SelectedItems.Count < 1) return false;

            return (SelectedItems[0] is EMD_Mesh || SelectedItems[0] is EMG_Mesh);
        }

        public bool IsSubmeshSelected()
        {
            if (SelectedItems.Count < 1) return false;

            return (SelectedItems[0] is EMD_Submesh || SelectedItems[0] is EMG_SubmeshGroup);
        }

        public bool IsEmgSubmeshSelected()
        {
            if (SelectedItems.Count < 1) return false;

            return (SelectedItems[0] is EMG_SubmeshGroup);
        }

        public bool IsTextureSelected()
        {
            if (SelectedItems.Count < 1) return false;

            return (SelectedItems[0] is EMD_TextureSamplerDef);
        }

        public bool IsAnyModelObjectSelected()
        {
            return IsModelSelected() || IsMeshSelected() || IsSubmeshSelected();
        }

        public bool HasOnlyOneSelectedType()
        {
            if (SelectedItems.Count < 1) return true;

            for(int i = 1; i < SelectedItems.Count; i++)
            {
                if (SelectedItems[0].GetType() != SelectedItems[i].GetType()) return false;
            }

            return true;
        }
        
        public bool CanPasteModel()
        {
            return Clipboard.ContainsData(SerializedModel.CLIPBOARD_MODEL);
        }

        public bool CanPasteMesh()
        {
            return Clipboard.ContainsData(SerializedModel.CLIPBOARD_MESH);
        }

        public bool CanPasteSubmesh()
        {
            return EmdFile != null && Clipboard.ContainsData(SerializedModel.CLIPBOARD_EMD_SUBMESH);
        }
        #endregion

        #region SourceUpdateEvents
        private void Model_ModelModified(object source, ModelModifiedEventArgs e)
        {
            IsBoundingBoxDirty = true;
        }

        private void Model_MaterialsChanged(object sender, EventArgs e)
        {
            IsMaterialsDirty = true;
        }

        private void EmbFile_TexturesChanged(object sender, EventArgs e)
        {
            IsTexturesDirty = true;
        }
        
        private void DytFile_TexturesChanged(object sender, EventArgs e)
        {
            IsDytDirty = true;
        }

        #endregion
    
        public ReadOnlyCollection<Xv2Submesh> GetSelectedSubmeshes()
        {
            return _readonlySelectedSubmeshes;
        }

        public IModelFile GetSourceModel()
        {
            switch (Model.Type)
            {
                case ModelType.Emd:
                case ModelType.Nsk:
                    return EmdFile;
                case ModelType.Emo:
                    return Model.SourceEmoFile;
            }

            return null;
        }
    }

    public enum PivotPoint
    {
        Origin,
        Center
    }

    public enum ViewportSelectionMode
    {
        Model,
        Mesh,
        Submesh
    }

    public delegate void ModelSceneSelectEventHandler(object source, ModelSceneSelectEventArgs e);

    public class ModelSceneSelectEventArgs : EventArgs
    {
        public object Object { get; private set; }
        public bool Addition { get; private set; }

        public ModelSceneSelectEventArgs(object _object)
        {
            Object = _object;
            Addition = Viewport.Instance.Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl);
        }
    }

}