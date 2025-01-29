using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Engine.Animation;
using XenoKit.Engine.Model;
using XenoKit.Engine.Shader;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EMG;
using Xv2CoreLib.EMO;
using Xv2CoreLib.NSK;

namespace XenoKit.Inspector.InspectorEntities
{
    public class MeshInspectorEntity : InspectorEntity
    {
        public override string FileType => "Model";
        private EntityType _entityType;
        public override EntityType EntityType => _entityType;
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
        private List<Xv2ShaderEffect> CompiledMaterials { get; set; }

        public MeshInspectorEntity(string path) : base(path)
        {
            Path = path;
            Load();
        }

        public MeshInspectorEntity(SkinnedInspectorEntity parent, NSK_File nskFile, string path) : base(path)
        {
            Parent = parent;
            _entityType = EntityType.Stage;
            Path = path;
            NskFile = nskFile;
            Model = Xv2ModelFile.LoadNsk(SceneManager.MainGameBase, NskFile);
            ShaderType = ShaderType.Stage;
            LoadAssets();
        }

        public MeshInspectorEntity(SkinnedInspectorEntity parent, EMO_File emoFile, string path) : base(path)
        {
            Parent = parent;
            _entityType = EntityType.Model;
            ShaderType = ShaderType.Default;
            Path = path;
            EmoFile = emoFile;
            Model = Xv2ModelFile.LoadEmo(SceneManager.MainGameBase, EmoFile);
            LoadAssets();
        }

        public override bool Load()
        {
            switch (System.IO.Path.GetExtension(Path).ToLower())
            {
                case ".emd":
                    EmdFile = EMD_File.Load(Path);
                    Model = Xv2ModelFile.LoadEmd(SceneManager.MainGameBase, EmdFile);
                    ShaderType = ShaderType.Chara;
                    _entityType = EntityType.Actor;
                    break;
                case ".nsk":
                    NskFile = NSK_File.Load(Path);
                    Model = Xv2ModelFile.LoadNsk(SceneManager.MainGameBase, NskFile);
                    ShaderType = ShaderType.Stage;
                    _entityType = EntityType.Stage;
                    break;
                case ".emo":
                    EmoFile = EMO_File.Load(Path);
                    Model = Xv2ModelFile.LoadEmo(SceneManager.MainGameBase, EmoFile);
                    ShaderType = ShaderType.Default;
                    _entityType = EntityType.Model;
                    break;
                case ".emg":
                    EmgFile = EMG_File.Load(Path);
                    Model = Xv2ModelFile.LoadEmgInContainer(SceneManager.MainGameBase, EmgFile);
                    ShaderType = ShaderType.Default;
                    _entityType = EntityType.Model;
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
            CompiledMaterials = Model.InitializeMaterials(ShaderType, MaterialFile?.EmmFile);
        }

        public override void Draw()
        {
            if (!Visible) return;

            if (DytFile != null)
            {
                GraphicsDevice.SamplerStates[4] = DytSampler.state;
                GraphicsDevice.Textures[4] = DytFile.DytIndex >= DytFile.Textures.Length ? DytFile.Textures[0].Texture : DytFile.Textures[DytFile.DytIndex].Texture;
            }

            Model.Draw(Parent != null ? Parent.Transform : Matrix.Identity, 0, CompiledMaterials, TextureFile?.Textures, DytFile?.Textures, DytFile != null ? DytFile.DytIndex : 0, Parent?.Skeleton);
        }

        public override void DrawPass(bool normalPass)
        {
            if (!Visible) return;

            if (normalPass && ShaderType != ShaderType.Stage)
            {
                Model.Draw(Parent != null ? Parent.Transform : Matrix.Identity, 0, RenderSystem.NORMAL_FADE_WATERDEPTH_W_M, Parent?.Skeleton);
            }
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
                        SceneManager.MainGameBase.RenderSystem.MoveRenderEntityToFront(mesh);
                    }
                }
            }
        }
    }
}
