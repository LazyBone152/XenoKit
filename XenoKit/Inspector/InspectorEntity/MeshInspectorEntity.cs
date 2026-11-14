using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Engine.Model;
using XenoKit.Engine.Shader;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EMG;
using Xv2CoreLib.EMO;
using Xv2CoreLib.NSK;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Inspector.InspectorEntities
{
    public class MeshInspectorEntity : InspectorEntity
    {
        public override string FileType => "Model";
        private EngineObjectTypeEnum _entityType;
        public override EngineObjectTypeEnum EngineObjectType => _entityType;
        public override bool DrawThisFrame => Files.Instance.SelectedItem?.Type == OutlinerItem.OutlinerItemType.Inspector;

        public SkinnedInspectorEntity Parent { get; set; }

        public EMD_File EmdFile { get; private set; }
        public NSK_File NskFile { get; private set; }
        public EMO_File EmoFile { get; private set; }
        public EMG_File EmgFile { get; private set; }

        public Xv2ModelFile Model { get; private set; }
        private ShaderType ShaderType { get; set; }
        private SamplerInfo DytSampler;

        public TextureInspectorEntity TextureFile { get; private set; }
        public TextureInspectorEntity DytFile { get; private set; }
        public MaterialInspectorEntity MaterialFile { get; private set; }
        private Xv2ShaderEffect[] CompiledMaterials { get; set; }

        private bool IsMaterialsDirty = false;
        private bool IsTexturesDirty = false;
        private bool IsDytDirty = false;

        public MeshInspectorEntity(string path) : base(path)
        {
            Path = path;
            Load();
        }

        public MeshInspectorEntity(SkinnedInspectorEntity parent, NSK_File nskFile, string path) : base(path)
        {
            Parent = parent;
            _entityType = EngineObjectTypeEnum.Stage;
            Path = path;
            NskFile = nskFile;
            Model = Xv2ModelFile.LoadNsk(NskFile);
            ShaderType = ShaderType.Stage;
            LoadAssets();
        }

        public MeshInspectorEntity(SkinnedInspectorEntity parent, EMO_File emoFile, string path) : base(path)
        {
            Parent = parent;
            _entityType = EngineObjectTypeEnum.Model;
            ShaderType = ShaderType.Default;
            Path = path;
            EmoFile = emoFile;
            Model = Xv2ModelFile.LoadEmo(EmoFile);
            LoadAssets();
        }

        public override bool Load()
        {
            switch (System.IO.Path.GetExtension(Path).ToLower())
            {
                case ".emd":
                    EmdFile = EMD_File.Load(Path);
                    Model = Xv2ModelFile.LoadEmd(EmdFile);
                    ShaderType = ShaderType.Chara;
                    _entityType = EngineObjectTypeEnum.Actor;
                    break;
                case ".nsk":
                    NskFile = NSK_File.Load(Path);
                    Model = Xv2ModelFile.LoadNsk(NskFile);
                    ShaderType = ShaderType.Stage;
                    _entityType = EngineObjectTypeEnum.Stage;
                    break;
                case ".emo":
                    EmoFile = EMO_File.Load(Path);
                    Model = Xv2ModelFile.LoadEmo(EmoFile);
                    ShaderType = ShaderType.Default;
                    _entityType = EngineObjectTypeEnum.Model;
                    break;
                case ".emg":
                    EmgFile = EMG_File.Load(Path);
                    Model = Xv2ModelFile.LoadEmgInContainer(EmgFile);
                    ShaderType = ShaderType.Default;
                    _entityType = EngineObjectTypeEnum.Model;
                    break;
                default:
                    throw new ArgumentException($"Unexpected model file type: {Path}");
            }

            LoadAssets();
            return true;
        }

        public override bool Save()
        {
            switch (System.IO.Path.GetExtension(Path).ToLower())
            {
                case ".emd":
                    EmdFile.Save(Path);
                    break;
                case ".nsk":
                    NskFile.SaveFile(Path);
                    break;
                case ".emo":
                    EmoFile.SaveFile(Path);
                    break;
                case ".emg":
                    EmgFile.Save(Path);
                    break;
                default:
                    throw new ArgumentException($"Unexpected model file type: {Path}");
            }

            return true;
        }

        private void LoadAssets()
        {
            //Load other assets:
            string embPath = $"{System.IO.Path.GetDirectoryName(Path)}/{System.IO.Path.GetFileNameWithoutExtension(Path)}.emb";
            string dytPath = $"{System.IO.Path.GetDirectoryName(Path)}/{System.IO.Path.GetFileNameWithoutExtension(Path)}.dyt.emb";
            string emmPath = $"{System.IO.Path.GetDirectoryName(Path)}/{System.IO.Path.GetFileNameWithoutExtension(Path)}.emm";

            if (System.IO.File.Exists(embPath) && Model.Type != ModelType.Emg)
            {
                AddTexture(new TextureInspectorEntity(embPath));
            }

            if (System.IO.File.Exists(dytPath) && Model.Type == ModelType.Emd)
            {
                AddDyt(new TextureInspectorEntity(dytPath));
            }

            if (System.IO.File.Exists(emmPath) && Model.Type != ModelType.Emg)
            {
                AddMaterial(new MaterialInspectorEntity(emmPath));
            }
            else
            {
                AddMaterial(null);
            }

            Model.MaterialsChanged += MaterialsChangedEvent;
        }

        public void AddTexture(TextureInspectorEntity texture)
        {
            if (TextureFile == texture) return;

            if (TextureFile != null)
            {
                ChildEntities.Remove(TextureFile);
            }

            TextureFile = texture;

            if(texture != null)
                ChildEntities.Add(texture);
        }

        public void AddDyt(TextureInspectorEntity texture)
        {
            if (DytFile == texture) return;

            if (DytFile != null)
            {
                ChildEntities.Remove(DytFile);
            }

            DytFile = texture;
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
                    BorderColor = new Microsoft.Xna.Framework.Color(1, 1, 1, 1),
                    Filter = TextureFilter.LinearMipPoint,
                    MaxAnisotropy = 1,
                    MaxMipLevel = 1,
                    MipMapLevelOfDetailBias = 0,
                    Name = ShaderManager.GetSamplerName(4)
                }
            };

            if(texture != null)
                ChildEntities.Add(texture);
        }

        public void AddMaterial(MaterialInspectorEntity material)
        {
            if (MaterialFile == material && material != null) return;

            if (MaterialFile != null)
            {
                ChildEntities.Remove(MaterialFile);
            }

            if(material != null)
                ChildEntities.Add(material);

            MaterialFile = material;
            CompiledMaterials = Xv2ShaderEffect.LoadMaterials(MaterialFile?.EmmFile, ShaderType);
            Model.InitMaterialIndex(CompiledMaterials);
        }

        public override void Draw()
        {
            if (!Visible && !RenderSystem.IsReflectionPass) return;
            HandleSourceUpdating();

            if (DytFile != null)
            {
                GraphicsDevice.SamplerStates[4] = DytSampler.state;
                GraphicsDevice.Textures[4] = DytFile.DytIndex >= DytFile.Textures.Length ? DytFile.Textures[0].Texture : DytFile.Textures[DytFile.DytIndex].Texture;
            }

            Model.Draw(Parent != null ? Parent.Transform : Matrix4x4.Identity, -1, CompiledMaterials, TextureFile?.Textures, DytFile?.Textures, DytFile != null ? DytFile.DytIndex : 0, Parent?.Skeleton);
        }

        public override void DrawPass(bool normalPass)
        {
            if (!Visible) return;
            if (normalPass && ShaderType == ShaderType.Stage) return; //No normal pass for stages

            Xv2ShaderEffect shader = RenderSystem.NORMAL_FADE_WATERDEPTH_W_M;

            if(ShaderType == ShaderType.Stage)
            {
                shader = RenderSystem.ShadowModel;
            }
            else if(!normalPass && ShaderType == ShaderType.Chara)
            {
                shader = RenderSystem.ShadowModel_W;
            }

            Model.Draw(Parent != null ? Parent.Transform : Matrix4x4.Identity, 0, shader, Parent?.Skeleton);
        }
    
        /// <summary>
        /// Hacky method for drawing transparent parts in the correct order.
        /// </summary>
        public static void CheckDrawOrder(IList<InspectorEntity> files)
        {
            foreach(InspectorEntity file in files)
            {
                if(file.Path.Contains("Face_Ear.emd") || file.Path.Contains("Face_ear.emd") || file.Path.Contains("face_ear.emd"))
                {
                    if(file is MeshInspectorEntity mesh)
                    {
                        Engine.Viewport.Instance.RenderSystem.MoveRenderEntityToFront(mesh);
                    }
                }
            }
        }
    
        public void SetAsReflectionMesh(bool isReflection)
        {
            Model?.SetAsReflectionMesh(isReflection);
        }

        #region SourceUpdateEvents
        private void HandleSourceUpdating()
        {
            if (IsMaterialsDirty)
            {
                CompiledMaterials = Xv2ShaderEffect.LoadMaterials(MaterialFile?.EmmFile, ShaderType);
                Model.InitMaterialIndex(CompiledMaterials);
                IsMaterialsDirty = false;
            }

            if (IsTexturesDirty && TextureFile != null)
            {
                TextureFile.RecreateTextureArray();
                IsTexturesDirty = false;
            }

            if (IsDytDirty && DytFile != null)
            {
                DytFile.RecreateTextureArray();
                IsDytDirty = false;
            }
        }

        private void MaterialsChangedEvent(object sender, EventArgs e)
        {
            IsMaterialsDirty = true;
        }

        private void TexturesChangedEvent(object sender, EventArgs e)
        {
            IsTexturesDirty = true;
        }

        private void DytsChangedEvent(object sender, EventArgs e)
        {
            IsDytDirty = true;
        }
        #endregion
    }
}
