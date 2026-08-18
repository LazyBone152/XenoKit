using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Model;
using XenoKit.Engine.Shader;
using XenoKit.Inspector.InspectorEntities;
using Xv2CoreLib.EMM;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Rendering
{
    public partial class RenderSystem : RenderObject
    {
        public readonly PostFilter PostFilter;
        public readonly YBSPostProcess YBS;
        public readonly ParticleBatcher ParticleBatcher;
        private readonly SpriteBatch SpriteBatch;

        private readonly List<RenderObject> Reflections = new List<RenderObject>();
        private readonly List<RenderObject> Characters = new List<RenderObject>();
        private readonly List<RenderObject> Stages = new List<RenderObject>();
        private readonly List<RenderObject> Effects = new List<RenderObject>(); //PBIND, TBIND, EMO

        private readonly List<RenderObject> ReflectionsToAdd = new List<RenderObject>();
        private readonly List<RenderObject> CharasToAdd = new List<RenderObject>();
        private readonly List<RenderObject> StagesToAdd = new List<RenderObject>();
        private readonly List<RenderObject> EffectsToAdd = new List<RenderObject>();

        private readonly List<RenderObject> ReflectionsToRemove = new List<RenderObject>();
        private readonly List<RenderObject> CharasToRemove = new List<RenderObject>();
        private readonly List<RenderObject> StagesToRemove = new List<RenderObject>();
        private readonly List<RenderObject> EffectsToRemove = new List<RenderObject>();

        private RenderScene RenderScene = null;

        private int _particleCount = 0;
        public int ActiveParticleCount { get; private set; }
        public int MeshDrawCalls;
        public int Count => Characters.Count + Effects.Count;

        private readonly List<RenderTargetWrapper> registeredRenderTargets = new List<RenderTargetWrapper>();
        private readonly List<RenderTarget2D> _toBeDisposed = new List<RenderTarget2D>();

        //Render Settings
        public DrawPass CurrentDrawPass { get; private set; }
        public bool IsReflectionPass { get; private set; }
        public bool IsShadowPass { get; private set; }
        private readonly Color ReflectionBackgroundColor = new Color(0.25098f, 0.25098f, 0.25098f, 1f);
        private readonly Color NormalsBackgroundColor = new Color(0.50196f, 0.50196f, 0, 0);

        //Screenshot / RT Dump
        private ScreenshotType ScreenshotType = ScreenshotType.TransparentBackground;
        private bool ScreenshotRequested = false;
        public bool DumpRenderTargetsNextFrame = false;
        public bool DumpShadowMapNextFrame = false;
        public int RecreateRenderTargetsNextFrames = 0;

        //Render Resolution:
        public readonly float[] RenderResolution = new float[4];
        public float SuperSampleFactor { get; private set; }
        public int RenderWidth { get; private set; }
        public int RenderHeight { get; private set; }
        public int CurrentRT_Width { get; private set; }
        public int CurrentRT_Height { get; private set; }

        //RTs:
        public RenderTargetWrapper DepthBuffer;

        //Some stages have a "REF" NSK that is rendered upside down to this, and used for reflections
        private RenderTargetWrapper ReflectionRT;

        //Characters are drawn onto these RTs using the shader NORMAL_FADE_WATERDEPTH_W_M
        private RenderTargetWrapper NormalPassRT0;
        private RenderTargetWrapper NormalPassRT1;

        //Characters and the stage enviroments are drawn onto these RTs using their proper materials
        private RenderTargetWrapper ColorPassRT0;
        private RenderTargetWrapper ColorPassRT1;

        //ShaderProgram BIRD_BG_EDGELINE_RGB_HF is used with ColorPassRT0 as input, drawn onto this RT (adds black outline to charas)
        //The remaining stage elements are then drawn to this RT and ColorPassRT1
        private RenderTargetWrapper NextColorPassRT0;

        //Some BPE effects such as BodyOutline are done at this point, and drawn onto NextColorPassRT0 + ColorPassRT1
        //Next are effects, using the same RTs
        private RenderTargetWrapper LowRezRT0;
        private RenderTargetWrapper LowRezRT1;
        private RenderTargetWrapper LowRezSmokeRT0;
        private RenderTargetWrapper LowRezSmokeRT0_New;
        private RenderTargetWrapper LowRezSmokeRT1;

        //Test
        private RenderTargetWrapper ScreenshotRT;

        //The final render target that everything will be drawn onto at the end of the frame. This can also be used for the "State_SamplerSmallScene" sampler as that is for the previously rendered frame (unsure about State_SamplerCurrentScene)
        private RenderTargetWrapper FinalRenderTarget;

        //Global sampler RTs:
        private RenderTargetWrapper ShadowPassRT0; //Characters and the stage enviroments are drawn onto this RT with the different shaders (Chara: ShadowModel_W, Stage: ShadowModel, Grass: GI_ShadowModel_Grass)
        private RenderTargetWrapper SamplerAlphaDepth;
        private RenderTargetWrapper SmallSceneRT;

        //ShaderPrograms:
        public Xv2ShaderEffect ShadowModel_W { get; private set; }
        public Xv2ShaderEffect ShadowModel { get; private set; }
        public Xv2ShaderEffect GI_ShadowModel { get; private set; }
        public Xv2ShaderEffect GI_ShadowModel_Grass { get; private set; }
        public Xv2ShaderEffect NORMAL_FADE_WATERDEPTH_W_M { get; private set; }
        private PostShaderEffect AGE_TEST_EDGELINE_MRT;
        private PostShaderEffect BIRD_BG_EDGELINE_RGB_HF;
        private PostShaderEffect DepthToDepth;
        private PostShaderEffect NineConeFilter;
        private PostShaderEffect AGE_MERGE_AddLowRez_AddMrt;
        private PostShaderEffect Sampler0;
        private PostShaderEffect EDGELINE_VFX;
        private PostShaderEffect AGE_TEST_DEPTH_TO_PFXD;
        private PostShaderEffect DepthToColor;
        private PostShaderEffect AddTex; //Merge up to 2 textures into a RenderTarget

        //private Texture2D TestOutlineTexture;
        //private Texture2D TestOutlineTexture2;

        public RenderSystem(SpriteBatch spriteBatch, bool createInternalResources)
        {
            SetRenderResolution();
            CurrentRT_Width = RenderWidth;
            CurrentRT_Height = RenderHeight;

            PostFilter = new PostFilter(this);

            if (createInternalResources)
                CreateInternalResources();

            //TestOutlineTexture = Textures.TextureLoader.ConvertToTexture2D(SettingsManager.Instance.GetAbsPathInAppFolder("EdgeLineTest.dds"), GraphicsDevice);
            //TestOutlineTexture2 = Textures.TextureLoader.ConvertToTexture2D(SettingsManager.Instance.GetAbsPathInAppFolder("EdgeLineTest2.dds"), GraphicsDevice);

            if(ShaderManager.IsExtShadersLoaded)
                YBS = new YBSPostProcess(this, NextColorPassRT0, ColorPassRT1);

            ParticleBatcher = new ParticleBatcher();
            SpriteBatch = spriteBatch;
        }

        private void CreateInternalResources()
        {
            if(ShadowModel_W != null)
            {
                throw new InvalidOperationException("RenderSystem.CreateInternalResources: Internal resources have already been created!");
            }

            //Load shaders used for the shadow and normal passes. These are used instead of the regular shaders defined in EMM during those passes.
            ShadowModel_W = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(EmmMaterial.CreateDefaultMaterial("ShadowModel_W"), ShaderType.CharaShadow);
            ShadowModel = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(EmmMaterial.CreateDefaultMaterial("ShadowModel"));
            GI_ShadowModel = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(EmmMaterial.CreateDefaultMaterial("GI_ShadowModel"));
            GI_ShadowModel_Grass = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(EmmMaterial.CreateDefaultMaterial("GI_ShadowModel_Grass"));
            //GI_ShadowModel_Grass = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(EmmMaterial.CreateDefaultMaterial("GI_ShadowModel_Grass"));
            NORMAL_FADE_WATERDEPTH_W_M = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(EmmMaterial.CreateDefaultMaterial("NORMAL_FADE_WATERDEPTH_W_M"), ShaderType.CharaNormals);

            //Load all the shaders that are used in the rendering process
            AGE_TEST_EDGELINE_MRT = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("AGE_TEST_EDGELINE_MRT"));
            BIRD_BG_EDGELINE_RGB_HF = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("BIRD_BG_EDGELINE_RGB_HF"));
            DepthToDepth = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("DepthToDepth"));
            DepthToColor = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("DepthToColor"));
            AddTex = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("AddTex"));
            NineConeFilter = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("NineConeFilter"));
            AGE_MERGE_AddLowRez_AddMrt = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("AGE_MERGE_AddLowRez_AddMrt"));
            Sampler0 = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("Sampler0"));
            EDGELINE_VFX = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("EDGELINE_VFX"));
            AGE_TEST_DEPTH_TO_PFXD = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("AGE_TEST_DEPTH_TO_PFXD"));

            //Create RTs
            ReflectionRT = new RenderTargetWrapper(this, 0.25f, SurfaceFormat.Color, true, "ReflectionRT");
            ShadowPassRT0 = RenderTargetWrapper.CreateShadowMap(this);
            DepthBuffer = new RenderTargetWrapper(this, 1, SurfaceFormat.Color, true);
            NormalPassRT0 = new RenderTargetWrapper(this, 1, SurfaceFormat.Color, true, "NormalPassRT0");
            NormalPassRT1 = new RenderTargetWrapper(this, 1, SurfaceFormat.Color, false, "NormalPassRT1");
            ColorPassRT0 = new RenderTargetWrapper(this, 1, SurfaceFormat.Color, true, "ColorPassRT0");
            ColorPassRT1 = new RenderTargetWrapper(this, 1, SurfaceFormat.Color, false, "ColorPassRT1");
            NextColorPassRT0 = new RenderTargetWrapper(this, 1, SurfaceFormat.Color, true, "NextColorPassRT0");
            FinalRenderTarget = new RenderTargetWrapper(this, 1, SurfaceFormat.Color, true, "FinalRenderTarget");
            SamplerAlphaDepth = new RenderTargetWrapper(this, 1, SurfaceFormat.Single, false, "SamplerAlphaDepth");
            LowRezRT0 = new RenderTargetWrapper(this, 0.5f, SurfaceFormat.Color, true, "LowRezRT0");
            LowRezRT1 = new RenderTargetWrapper(this, 0.5f, SurfaceFormat.Color, false, "LowRezRT1");
            LowRezSmokeRT0 = new RenderTargetWrapper(this, 0.25f, SurfaceFormat.Color, true, "LowRezSmokeRT0");
            LowRezSmokeRT0_New = new RenderTargetWrapper(this, 0.25f, SurfaceFormat.Color, true, "LowRezSmokeRT0_New");
            LowRezSmokeRT1 = new RenderTargetWrapper(this, 0.25f, SurfaceFormat.Color, false, "LowRezSmokeRT1");
            ScreenshotRT = new RenderTargetWrapper(this, 1, SurfaceFormat.Color, true, "ScreenshotRT");
            SmallSceneRT = new RenderTargetWrapper(this, 0.25f, SurfaceFormat.Color, true, "SmallSceneRT");


            //Register all render targets so they get auto-updated if the viewport changes size
            RegisterRenderTarget(ReflectionRT);
            RegisterRenderTarget(ShadowPassRT0);
            RegisterRenderTarget(NormalPassRT0);
            RegisterRenderTarget(NormalPassRT1);
            RegisterRenderTarget(ColorPassRT0);
            RegisterRenderTarget(ColorPassRT1);
            RegisterRenderTarget(NextColorPassRT0);
            RegisterRenderTarget(FinalRenderTarget);
            RegisterRenderTarget(SamplerAlphaDepth);
            RegisterRenderTarget(LowRezRT0);
            RegisterRenderTarget(LowRezRT1);
            RegisterRenderTarget(LowRezSmokeRT0);
            RegisterRenderTarget(LowRezSmokeRT0_New);
            RegisterRenderTarget(LowRezSmokeRT1);
            RegisterRenderTarget(DepthBuffer);
            RegisterRenderTarget(ScreenshotRT);
            RegisterRenderTarget(SmallSceneRT);

        }

        private void SetRenderResolution()
        {
            if(ViewportInstance.IsFullScreen)
            {
                RenderResolution[0] = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                RenderResolution[1] = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            }
            else
            {
                RenderResolution[0] = GraphicsDevice.Viewport.Width * SettingsManager.settings.XenoKit_SuperSamplingFactor;
                RenderResolution[1] = GraphicsDevice.Viewport.Height * SettingsManager.settings.XenoKit_SuperSamplingFactor;
            }

            RenderWidth = (int)RenderResolution[0];
            RenderHeight = (int)RenderResolution[1];
        }







        #region Screenshot
        /// <summary>
        /// Requests a screenshot to be saved during the renderering of the next frame.
        /// </summary>


        #endregion

        #region Update









        #endregion

        #region RenderTarget
        public void TestRTMerge(RenderTarget2D renderTarget1, RenderTarget2D renderTarget2)
        {
            SetTexture(renderTarget1, 0);
            SetTexture(renderTarget2, 1);
            PostFilter.Apply(AddTex);
        }

        public void DisplayRenderTarget(RenderTarget2D renderTarget, bool scaleToViewport = false, float scale = 1f, BlendState blendState = null)
        {
            Rectangle destination = scaleToViewport ? new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height) : new Rectangle(0, 0, (int)(renderTarget.Width * scale), (int)(renderTarget.Height * scale));

            SpriteBatch.Begin(depthStencilState: DepthStencilState.DepthRead, blendState: blendState ?? BlendState.AlphaBlend);
            SpriteBatch.Draw(renderTarget, destination, Color.White);
            SpriteBatch.End();
        }

        private void DumpRenderTargets()
        {
            DumpRenderTargetsNextFrame = false;
            bool dumpShadowMap = DumpShadowMapNextFrame;
            DumpShadowMapNextFrame = false;

            //TestOutlineTexture = Textures.TextureLoader.ConvertToTexture2D(SettingsManager.Instance.GetAbsPathInAppFolder("EdgeLineTest.dds"), GraphicsDevice);
            //return;

            Directory.CreateDirectory(SettingsManager.Instance.GetAbsPathInAppFolder("RT_Dump"));

            foreach (RenderTargetWrapper renderTarget in registeredRenderTargets)
            {
                if (dumpShadowMap && renderTarget.Name != nameof(RenderSystem.ShadowPassRT0)) continue;

                if (!string.IsNullOrEmpty(renderTarget.Name))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        renderTarget.RenderTarget.SaveAsPng(ms, renderTarget.Width, renderTarget.Height);
                        File.WriteAllBytes(SettingsManager.Instance.GetAbsPathInAppFolder($"RT_Dump/{renderTarget.Name}.png"), ms.ToArray());
                    }
                }
            }

            Log.Add("Render Targets dumped", LogType.Info);
        }

        /// <summary>
        /// Register a <see cref="RenderTargetWrapper"/> with this <see cref="RenderSystem"/> instance. Registered RenderTargets will be automatically updated when the viewport changes size.
        /// </summary>

        #endregion

        #region AddRemoveEntity










        #endregion






        public RenderTargetWrapper GetColorPassRT0()
        {
            return ColorPassRT0;
        }

    }



    public enum DrawPass
    {
        Opaque,
        AlphaBlend,
        Additive,
        Subtractive
    }

    public enum RenderPipelineStage
    {
        Shadow, //Characters / Stage models with shadow shader
        Normal, //Characters with normal shader
        ModelMain, //Characters / Stage models
        EffectInitial,
        EffectLowRez,
        EffectLowRezSmoke
    }

}
