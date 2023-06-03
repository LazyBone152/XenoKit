using System;
using System.IO;
using XenoKit.Engine.View;
using XenoKit.Engine.Objects;
using XenoKit.Engine.Gizmo;
using XenoKit.Engine.Audio;
using SpriteFontPlus;
using XenoKit.Engine.Text;
using XenoKit.Editor;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using MonoGame.Framework.WpfInterop.Input;
using Xv2CoreLib.EMD;
using XenoKit.Engine.Model;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMM;
using XenoKit.Engine.Textures;
using System.Collections.Generic;
using XenoKit.Engine.Shader;
using System.Threading;
using System.Threading.Tasks;
using XenoKit.Engine.Vfx;

namespace XenoKit.Engine
{
    public class EmdViewer : GameBase
    {
        public override ICameraBase ActiveCameraBase => camera;

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


        protected override void Initialize()
        {
            //Initalize base elements first
            base.Initialize();

            //Now initialize objects
            camera = new SimpleCamera(this);
            //ModelGizmo = new ModelGizmo(this);

            DytSampler = new SamplerInfo()
            {
                type = SamplerType.Sampler2D,
                textureSlot = 4,
                samplerSlot = 4,
                name = ShaderManager.Instance.GetSamplerName(4),
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
                    Name = ShaderManager.Instance.GetSamplerName(4)
                }
            };

            VfxManager = new VfxManager(this);
        }

        protected override void LoadContent()
        {
            worldGrid = new WorldGrid(this);

            base.LoadContent();
        }
        
        protected override void Update(GameTime time)
        {
            base.Update(time);

            Model?.Update(0);
            camera.Update();
        }

        protected override void Draw(GameTime time)
        {
            base.Draw(time);
            worldGrid.Draw();

            //Handle Dyt texture and Sampler
            if(DytTexture != null)
            {
                GraphicsDevice.SamplerStates[4] = DytSampler.state;
                GraphicsDevice.VertexSamplerStates[4] = DytSampler.state;
                GraphicsDevice.VertexTextures[4] = DytTexture[0].Texture;
                GraphicsDevice.Textures[4] = DytTexture[0].Texture;
            }

            Model?.Draw(Matrix.Identity, 0, Materials, Textures, DytTexture, 0);

            //ModelGizmo.Draw();
        }

        public async Task RefreshModel()
        {
            if (EmdFile == null)
            {
                Model = null;
                return;
            }

            while(GraphicsDevice == null)
            {
                await Task.Delay(100);
            }

            if(Model != null)
            {
                Model.ModelChanged -= RefreshMaterialsEvent;
            }

            //Model = Xv2ModelFile.LoadCharaEmd(this, EmdFile);
            Model = CompiledObjectManager.GetCompiledObject<Xv2ModelFile>(EmdFile, this);
            Materials = Model.InitializeMaterials(EmmFile);

            if (EmbFile != null)
            {
                Textures = new Xv2Texture[EmbFile.Entry.Count];

                for(int i = 0; i < Textures.Length; i++)
                {
                    //Textures[i] = new Xv2Texture(EmbFile.Entry[i], this);
                    Textures[i] = CompiledObjectManager.GetCompiledObject<Xv2Texture>(EmbFile.Entry[i], this);
                }
            }
            else
            {
                Textures = null;
            }

            if(DytFile != null)
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
            if(Model != null)
            {
                Model.ModelChanged -= RefreshMaterialsEvent;
            }

            if(Textures != null)
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
            Materials = Model.InitializeMaterials(EmmFile);
        }
        
    }
}
