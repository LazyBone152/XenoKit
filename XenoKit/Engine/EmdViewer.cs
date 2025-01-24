using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMM;
using XenoKit.Engine.Vfx;
using XenoKit.Engine.Rendering;
using XenoKit.Engine.View;
using XenoKit.Engine.Objects;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using XenoKit.Engine.Model;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine
{
    public class EmdViewer : GameBase
    {
        public override ICameraBase ActiveCameraBase => camera;
        public override int SuperSamplingFactor => SettingsManager.settings.XenoKit_SuperSamplingFactor;

        //Objects:
        public SimpleCamera camera { get; private set; }
        private WorldGrid worldGrid { get; set; }

        //Files
        public EMD_File EmdFile { get; set; }
        public EMB_File EmbFile { get; set; }
        public EMM_File EmmFile { get; set; }
        public EMB_File DytFile { get; set; }

        //Compiled Objects
        public Xv2ModelFile Model { get; private set; }
        private Xv2Texture[] Textures { get; set; }
        private Xv2Texture[] DytTexture { get; set; }
        private List<Xv2ShaderEffect> Materials { get; set; }

        //Sampler
        private SamplerInfo DytSampler;

        private RenderTargetWrapper RenderTargetModel { get; set; }
        private RenderTargetWrapper RenderTargetPost { get; set; }

        protected override void Initialize()
        {
            //Initalize base elements first
            base.Initialize();

            //Now initialize objects
            camera = new SimpleCamera(this);
            RenderSystem = new RenderSystem(this, false);
            //ModelGizmo = new ModelGizmo(this);

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

            VfxManager = new VfxManager(this);
            worldGrid = new WorldGrid(this);
            camera.CameraState.FieldOfView = 30;

            RenderTargetModel = new RenderTargetWrapper(RenderSystem, 1, SurfaceFormat.Color, true, "ModelViewerMainRT");
            RenderTargetPost = new RenderTargetWrapper(RenderSystem, 1, SurfaceFormat.Color, true, "ModelViewerPostRT");
            RenderSystem.RegisterRenderTarget(RenderTargetModel);
            RenderSystem.RegisterRenderTarget(RenderTargetPost);
        }

        protected override void Update(GameTime time)
        {
            base.Update(time);

            RenderSystem.Update();
            Model?.Update(0);
            camera.Update();
        }

        protected override void Draw(GameTime time)
        {
            GraphicsDevice.SetRenderTarget(RenderTargetModel.RenderTarget);
            GraphicsDevice.Clear(SceneManager.ViewportBackgroundColor);
            worldGrid.Draw();

            //Handle Dyt texture and Sampler
            if (DytTexture != null)
            {
                GraphicsDevice.SamplerStates[4] = DytSampler.state;
                GraphicsDevice.VertexSamplerStates[4] = DytSampler.state;
                GraphicsDevice.VertexTextures[4] = DytTexture[0].Texture;
                GraphicsDevice.Textures[4] = DytTexture[0].Texture;
            }

            Model?.Draw(Matrix.Identity, 0, Materials, Textures, DytTexture, 0);

            //Now apply axis correction
            GraphicsDevice.SetRenderTarget(RenderTargetPost.RenderTarget);
            RenderSystem.SetTextures(RenderTargetModel.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            RenderSystem.YBS.ApplyAxisCorrection(Vector4.Zero);

            //Present on screen
            GraphicsDevice.SetRenderTarget(InternalRenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            RenderSystem.DisplayRenderTarget(RenderTargetPost.RenderTarget, true);
        }

        public async Task RefreshModel()
        {
            if (EmdFile == null)
            {
                Model = null;
                return;
            }

            while (GraphicsDevice == null)
            {
                await Task.Delay(100);
            }

            if (Model != null)
            {
                Model.ModelChanged -= RefreshMaterialsEvent;
            }

            //Model = Xv2ModelFile.LoadCharaEmd(this, EmdFile);
            Model = CompiledObjectManager.GetCompiledObject<Xv2ModelFile>(EmdFile, this);
            Materials = Model.InitializeMaterials(ShaderType.Chara, EmmFile);

            if (EmbFile != null)
            {
                Textures = new Xv2Texture[EmbFile.Entry.Count];

                for (int i = 0; i < Textures.Length; i++)
                {
                    //Textures[i] = new Xv2Texture(EmbFile.Entry[i], this);
                    Textures[i] = CompiledObjectManager.GetCompiledObject<Xv2Texture>(EmbFile.Entry[i], this);
                }
            }
            else
            {
                Textures = null;
            }

            if (DytFile != null)
            {
                DytTexture = new Xv2Texture[1];
                //DytTexture[0] = new Xv2Texture(DytFile.Entry[0], this);
                DytTexture[0] = CompiledObjectManager.GetCompiledObject<Xv2Texture>(DytFile.Entry[0], this);
            }
            else
            {
                DytTexture = null;
            }

            Model.ModelChanged += RefreshMaterialsEvent;
        }

        public void ClearInstance()
        {
            if (Model != null)
            {
                Model.ModelChanged -= RefreshMaterialsEvent;
            }

            if (Textures != null)
            {
                foreach (var texture in Textures)
                    texture.Dispose();
            }

            if (DytTexture != null)
            {
                DytTexture[0].Dispose();
            }

            CompiledObjectManager.Dispose();
        }

        private void RefreshMaterialsEvent(object sender, EventArgs e)
        {
            Materials = Model.InitializeMaterials(ShaderType.Chara, EmmFile);
        }

    }
}
