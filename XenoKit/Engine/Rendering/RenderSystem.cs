using AForge.Math.Metrics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Model;
using XenoKit.Engine.Shader;
using Xv2CoreLib.EMM;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Rendering
{
    public class RenderSystem : Entity
    {
        public readonly PostFilter PostFilter;
        public readonly YBSPostProcess YBS;

        private readonly List<Entity> Characters = new List<Entity>();
        private readonly List<Entity> Stages = new List<Entity>();
        private readonly List<Entity> Effects = new List<Entity>(); //PBIND, TBIND, EMO

        private readonly List<Entity> CharasToRemove = new List<Entity>();
        private readonly List<Entity> StagesToRemove = new List<Entity>();
        private readonly List<Entity> EffectsToRemove = new List<Entity>();

        private int _particleCount = 0;
        public int ActiveParticleCount { get; private set; }
        public int Count => Characters.Count + Effects.Count;

        private List<RenderTargetWrapper> registeredRenderTargets = new List<RenderTargetWrapper>();
        private List<RenderTarget2D> _toBeDisposed = new List<RenderTarget2D>();

        //Render Settings:
        public DrawPass CurrentDrawPass { get; private set; }
        private readonly Color NormalsBackgroundColor = new Color(0.50196f, 0.50196f, 0, 0);
        private bool ScreenshotRequested = false;
        private ScreenshotType ScreenshotType = ScreenshotType.TransparentBackground;
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
        public RenderTargetWrapper ShadowPassRT0; //Characters and the stage enviroments are drawn onto this RT with the different shaders (Chara: ShadowModel_W, Stage: ShadowModel, Grass: GI_ShadowModel_Grass)
        public RenderTargetWrapper SamplerAlphaDepth;

        //ShaderPrograms:
        public Xv2ShaderEffect ShadowModel_W { get; private set; }
        public Xv2ShaderEffect ShadowModel { get; private set; }
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

        public RenderSystem(GameBase game, bool createInternalResources) : base(game)
        {
            SetRenderResolution();
            CurrentRT_Width = RenderWidth;
            CurrentRT_Height = RenderHeight;

            PostFilter = new PostFilter(game, this);

            if (createInternalResources)
                CreateInternalResources();

            //TestOutlineTexture = Textures.TextureLoader.ConvertToTexture2D(SettingsManager.Instance.GetAbsPathInAppFolder("EdgeLineTest.dds"), GraphicsDevice);
            //TestOutlineTexture2 = Textures.TextureLoader.ConvertToTexture2D(SettingsManager.Instance.GetAbsPathInAppFolder("EdgeLineTest2.dds"), GraphicsDevice);

            if(ShaderManager.IsExtShadersLoaded)
                YBS = new YBSPostProcess(GameBase, this, NextColorPassRT0, ColorPassRT1);

        }

        private void CreateInternalResources()
        {
            if(ShadowModel_W != null)
            {
                throw new InvalidOperationException("RenderSystem.CreateInternalResources: Internal resources have already been created!");
            }

            //Load shaders used for the shadow and normal passes. These are used instead of the regular shaders defined in EMM during those passes.
            ShadowModel_W = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(EmmMaterial.CreateDefaultMaterial("ShadowModel_W"), GameBase, ShaderType.CharaShadow);
            ShadowModel = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(EmmMaterial.CreateDefaultMaterial("ShadowModel"), GameBase);
            GI_ShadowModel_Grass = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(EmmMaterial.CreateDefaultMaterial("GI_ShadowModel_Grass"), GameBase);
            NORMAL_FADE_WATERDEPTH_W_M = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(EmmMaterial.CreateDefaultMaterial("NORMAL_FADE_WATERDEPTH_W_M"), GameBase, ShaderType.CharaNormals);

            //Load all the shaders that are used in the rendering process
            AGE_TEST_EDGELINE_MRT = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("AGE_TEST_EDGELINE_MRT"), GameBase);
            BIRD_BG_EDGELINE_RGB_HF = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("BIRD_BG_EDGELINE_RGB_HF"), GameBase);
            DepthToDepth = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("DepthToDepth"), GameBase);
            DepthToColor = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("DepthToColor"), GameBase);
            AddTex = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("AddTex"), GameBase);
            NineConeFilter = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("NineConeFilter"), GameBase);
            AGE_MERGE_AddLowRez_AddMrt = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("AGE_MERGE_AddLowRez_AddMrt"), GameBase);
            Sampler0 = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("Sampler0"), GameBase);
            EDGELINE_VFX = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("EDGELINE_VFX"), GameBase);
            AGE_TEST_DEPTH_TO_PFXD = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("AGE_TEST_DEPTH_TO_PFXD"), GameBase);

            //Create RTs
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
            ScreenshotRT = new RenderTargetWrapper(this, 1, SurfaceFormat.Color, true, "PostTestRT");


            //Register all render targets so they get auto-updated if the viewport changes size
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

        }

        private void SetRenderResolution()
        {
            if(GameBase.IsFullScreen)
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

        public override void Draw()
        {
            const int LOW_REZ_NONE = 0, LOW_REZ = 1, LOW_REZ_SMOKE = 2;

            if (!DrawThisFrame) return;

            //Clear the common depth buffer
            GraphicsDevice.SetRenderTarget(RenderSystem.DepthBuffer.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);

            //Shadow Pass (Chara + Stage Enviroment)
            GraphicsDevice.SetRenderTarget(ShadowPassRT0.RenderTarget);
            GraphicsDevice.SetDepthBuffer(ShadowPassRT0.RenderTarget);
            GraphicsDevice.Clear(Color.Red);
            DrawEntityList(Characters, false);
            DrawEntityList(Stages, false);

            if (DumpShadowMapNextFrame)
                DumpRenderTargets();

            //Normals Pass (Chara)
            SetRenderTargets(NormalPassRT0.RenderTarget, NormalPassRT1.RenderTarget);
            GraphicsDevice.Clear(new Color(0.50196f, 0.50196f, 0, 0));
            DrawEntityList(Characters, true);

            //Color Pass (Chara + Stage Enviroment)
            SetRenderTargets(ColorPassRT0.RenderTarget, ColorPassRT1.RenderTarget, NormalPassRT1.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            GraphicsDevice.SetDepthBuffer(DepthBuffer.RenderTarget);
            DrawEntityList(Characters, LOW_REZ_NONE);
            DrawEntityList(Stages, LOW_REZ_NONE);

            //Black Chara Outline Shader
            if (SettingsManager.settings.XenoKit_UseOutlinePostEffect)
            {
                SetRenderTargets(ColorPassRT0.RenderTarget, ColorPassRT1.RenderTarget, NormalPassRT1.RenderTarget);
                GraphicsDevice.SetDepthBuffer(DepthBuffer.RenderTarget);
                SetTexture(NormalPassRT0.RenderTarget);
                PostFilter.Apply(AGE_TEST_EDGELINE_MRT);
            }

            //Create SamplerAlphaDepth
            SetRenderTargets(SamplerAlphaDepth.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            GraphicsDevice.SetDepthAsTexture(DepthBuffer.RenderTarget, 0);
            PostFilter.Apply(AGE_TEST_DEPTH_TO_PFXD);
            GraphicsDevice.Textures[0] = null;

            //Stage Outline, NewColorPassRT
            GraphicsDevice.SetRenderTarget(NextColorPassRT0.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            GraphicsDevice.SetDepthBuffer(DepthBuffer.RenderTarget);
            SetTexture(ColorPassRT0.RenderTarget);
            PostFilter.SetTextureCoordinates(0.0002f, 0.00035f);
            PostFilter.Apply(BIRD_BG_EDGELINE_RGB_HF);

            //Initial Effect Pass
            SetRenderTargets(NextColorPassRT0.RenderTarget, ColorPassRT1.RenderTarget);
            GraphicsDevice.SetDepthBuffer(DepthBuffer.RenderTarget);
            _particleCount = 0;
            DrawEntityList(Effects, LOW_REZ_NONE);

            //LowRez Pass
            SetRenderTargets(LowRezRT0.RenderTarget, LowRezRT1.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            UseDepthToDepth();
            DrawEntityList(Effects, LOW_REZ);
            DrawEntityList(Effects, LOW_REZ_SMOKE); //LowRezSmoke pass is broken... effects dont render. So for now, render them in LowRez pass until its fixed

            //LowRezSmoke Pass
            SetRenderTargets(LowRezSmokeRT0.RenderTarget, LowRezSmokeRT1.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            UseDepthToDepth();
            UseDepthToDepth(); //Very weird bug... without TWO of these all rendered effects dont show on this RT? If the initial call on LowRez is removed, only 1 is needed, but that breaks that pass

            //DrawEntityList(Effects, LOW_REZ_SMOKE);

            //Render EdgeLine (BPE Outline Test)
            //SetTextures(NormalPassRT1.RenderTarget, TestOutlineTexture);
            //PostFilter.Apply(EDGELINE_VFX);

            //Apply blur filter to LowRezSmoke
            SetRenderTargets(LowRezSmokeRT0_New.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            SetTexture(LowRezSmokeRT0.RenderTarget);
            PostFilter.SetTextureCoordinates(1f / (CurrentRT_Width * 2), 1f / (CurrentRT_Height * 2));
            PostFilter.Apply(NineConeFilter);

            //Merge onto main RT
            SetRenderTargets(NextColorPassRT0.RenderTarget, ColorPassRT1.RenderTarget);
            GraphicsDevice.SetDepthBuffer(DepthBuffer.RenderTarget);
            SetTextures(LowRezRT0.RenderTarget, LowRezSmokeRT0_New.RenderTarget, LowRezRT1.RenderTarget, LowRezSmokeRT1.RenderTarget);
            PostFilter.SetDefaultTexCord2();
            PostFilter.Apply(AGE_MERGE_AddLowRez_AddMrt);

            //YBS post process effects (just glare for now)
            RenderTargetWrapper result;

            if (ShaderManager.IsExtShadersLoaded)
            {
                YBS.Draw();
                result = YBS.GetRenderTarget();
            }
            else
            {
                result = NextColorPassRT0;
            }

            //Create final RenderTarget
            SetRenderTargets(FinalRenderTarget.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);

            DisplayRenderTarget(result.RenderTarget, false);

            //Process screenshots at this stage, before merging the RT with the rest of the scene
            if (ScreenshotRequested)
            {
                ProcessScreenshot(FinalRenderTarget);
            }

#if DEBUG
            if (DumpRenderTargetsNextFrame)
            {
                DumpRenderTargets();
            }
#endif

            ActiveParticleCount = _particleCount;
        }

        private void UseDepthToDepth()
        {
            GraphicsDevice.SetDepthAsTexture(DepthBuffer.RenderTarget, 0);
            PostFilter.Apply(DepthToDepth);
            GraphicsDevice.Textures[0] = null;
        }

        public void SetTexture(Texture texture, int textureSlot = 0)
        {
            if (textureSlot > 8)
                throw new ArgumentOutOfRangeException("RenderSystem.SetTexture: textureSlot value passed into the method was 8 or greater, which is not allowed!");

            GraphicsDevice.Textures[textureSlot] = texture;
        }

        public void SetTextures(params Texture[] textures)
        {
            for(int i = 0; i < textures.Length; i++)
            {
                SetTexture(textures[i], i);
            }
        }

        public void SetRenderTargets(params RenderTargetBinding[] renderTargets)
        {
            if(renderTargets[0].RenderTarget is RenderTarget2D rt)
            {
                CurrentRT_Width = rt.Width;
                CurrentRT_Height = rt.Height;
            }

            GraphicsDevice.SetRenderTargets(renderTargets);
        }

        #region Screenshot
        /// <summary>
        /// Requests a screenshot to be saved during the renderering of the next frame.
        /// </summary>
        public void RequestScreenshot(ScreenshotType screenshotType)
        {
            if (!ShaderManager.IsExtShadersLoaded)
            {
                return;
            }
            ScreenshotRequested = true;
            ScreenshotType = screenshotType;
        }

        private void ProcessScreenshot(RenderTargetWrapper renderTarget)
        {
            ScreenshotRequested = false;

            string name = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string ext = LocalSettings.Instance.ScreenshotFormat.ToString().ToLower();
            string path = SettingsManager.Instance.GetAbsPathInAppFolder($"Screenshots/{name}_transbg.{ext}");
            string pathBlackBackground = SettingsManager.Instance.GetAbsPathInAppFolder($"Screenshots/{name}.{ext}");
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            switch (ScreenshotType)
            {
                case ScreenshotType.TransparentBackground:
                    ProcessScreenshot(renderTarget, path, Color.Transparent);
                    break;
                case ScreenshotType.CustomBackgroundColor:
                    ProcessScreenshot(renderTarget, pathBlackBackground, SceneManager.ScreenshotBackgroundColor);
                    break;
                case ScreenshotType.Both:
                    ProcessScreenshot(renderTarget, pathBlackBackground, SceneManager.ScreenshotBackgroundColor);
                    ProcessScreenshot(renderTarget, path, Color.Transparent);
                    break;
            }

            Log.Add($"Screenshot saved in the XenoKit/Screenshots folder!");

        }

        private void ProcessScreenshot(RenderTargetWrapper renderTarget, string path, Color clearColor)
        {
            //Copying it to a seperate RT before saving allows changing of the background color
            SetRenderTargets(ScreenshotRT.RenderTarget);
            SetTextures(renderTarget.RenderTarget);
            GraphicsDevice.Clear(clearColor);
            YBS.ApplyAxisCorrection(clearColor.ToVector4());

            using (MemoryStream ms = new MemoryStream())
            {
                if(LocalSettings.Instance.ScreenshotFormat == ScreenshotFormat.PNG)
                {
                    ScreenshotRT.RenderTarget.SaveAsPng(ms, ScreenshotRT.Width, ScreenshotRT.Height);
                }
                else
                {
                    ScreenshotRT.RenderTarget.SaveAsJpeg(ms, ScreenshotRT.Width, ScreenshotRT.Height);
                }
                File.WriteAllBytes(path, ms.ToArray());
            }
        }
        #endregion

        #region Update

        private void DrawEntityList(List<Entity> entities, bool normalPass)
        {
            foreach (Entity entity in entities)
            {
                if (entity.DrawThisFrame)
                {
                    if (entity.EntityType != EntityType.Actor && normalPass) continue; //skip if stage and its a normal pass (stages only have a shadow pass here)
                    entity.DrawPass(normalPass);
                }
            }
        }

#if DEBUG
        public string[] DRAW_ORDER = new string[10];
        public int CurrentDrawIdx = 0;
#endif

        private void DrawEntityList(List<Entity> entities, int lowRezMode)
        {
            int particleCount = 0;
#if DEBUG
            CurrentDrawIdx = 0;
#endif

            //OPAQUE PASS
            CurrentDrawPass = Rendering.DrawPass.Opaque;

            foreach (Entity entity in entities)
            {
                if (entity.LowRezMode != lowRezMode) continue;

                if (entity.DrawThisFrame)
                {
                    entity.Draw();

                    if (entity.EntityType == EntityType.VFX)
                        particleCount++;
                }
            }

            //ALPHA BLEND PASS
            CurrentDrawPass = Rendering.DrawPass.AlphaBlend;

            foreach (Entity entity in entities.OrderByDescending(x => Vector3.Distance(CameraBase.CameraState.Position, x.AbsoluteTransform.Translation)))
            {
                if (entity.LowRezMode != lowRezMode) continue;

                if (entity.DrawThisFrame)
                {
                    entity.Draw();
                }
            }

            //ADDITIVE PASS
            CurrentDrawPass = Rendering.DrawPass.Additive;

            foreach (Entity entity in entities)
            {
                if (entity.LowRezMode != lowRezMode) continue;

                if (entity.DrawThisFrame)
                {
                    entity.Draw();
                }
            }

            //SUBTRACTIVE PASS
            CurrentDrawPass = Rendering.DrawPass.Subtractive;

            foreach (Entity entity in entities)
            {
                if (entity.LowRezMode != lowRezMode) continue;

                if (entity.DrawThisFrame)
                {
                    entity.Draw();
                    entity.DrawThisFrame = false;
                }
            }

            _particleCount += particleCount;
        }

        public override void Update()
        {
            SetRenderResolution();

            if (RecreateRenderTargetsNextFrames > 0)
            {
                DelayedUpdate();
                RecreateRenderTargetsNextFrames -= 1;
            }

            EntityListUpdate(Characters, CharasToRemove);
            EntityListUpdate(Stages, StagesToRemove);
            EntityListUpdate(Effects, EffectsToRemove);
        }

        public override void DelayedUpdate()
        {
            DrawThisFrame = false;
            PostFilter.DelayedUpdate();

            //Dispose of previous RTs. (Apparantly this should be done on the next frame of when it was last used, so this goes before the render target update, instead of at the end of this method)
            if (_toBeDisposed.Count > 0)
            {
                foreach (RenderTarget2D rt in _toBeDisposed)
                {
                    if (!rt.IsDisposed)
                        rt.Dispose();
                }

                _toBeDisposed.Clear();
            }

            bool resolutionChanged = false;

            //Update RTs if the ViewPort size has changed.
            foreach (RenderTargetWrapper rt in registeredRenderTargets)
            {
                if (rt.ShouldUpdate() && GameIsFocused)
                {
                    resolutionChanged = true;
                    
                    if (rt.RenderTarget != null)
                        _toBeDisposed.Add(rt.RenderTarget);

                    rt.UpdateRenderTarget();
                }
            }

            if (resolutionChanged)
            {
                SuperSampleFactor = GameBase.IsFullScreen ? 1f : SettingsManager.settings.XenoKit_SuperSamplingFactor;
            }

            DrawThisFrame = true;
        }

        private void EntityListUpdate(List<Entity> entities, List<Entity> entitiesToRemove)
        {
            if (entitiesToRemove.Count > 0)
            {
                foreach (Entity entity in entitiesToRemove)
                {
                    entities.Remove(entity);
                }

                entitiesToRemove.Clear();
            }

            for (int i = entities.Count - 1; i >= 0; i--)
            {
                if (entities[i].IsDestroyed)
                {
                    entities.RemoveAt(i);
                }
            }
        }

        #endregion

        #region RenderTarget
        public void TestRTMerge(RenderTarget2D renderTarget1, RenderTarget2D renderTarget2)
        {
            SetTexture(renderTarget1, 0);
            SetTexture(renderTarget2, 1);
            PostFilter.Apply(AddTex);
        }

        public void DisplayRenderTarget(RenderTarget2D renderTarget, bool scaleToViewport = false)
        {
            Rectangle destination = scaleToViewport ? new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height) : new Rectangle(0, 0, renderTarget.Width, renderTarget.Height);

            GameBase.spriteBatch.Begin(depthStencilState: DepthStencilState.DepthRead, blendState: BlendState.AlphaBlend);
            GameBase.spriteBatch.Draw(renderTarget, destination, Color.White);
            GameBase.spriteBatch.End();
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
        public void RegisterRenderTarget(RenderTargetWrapper renderTarget)
        {
            registeredRenderTargets.Add(renderTarget);
        }
        
        public RenderTarget2D GetFinalRenderTarget()
        {
            return FinalRenderTarget.RenderTarget;
        }
        #endregion

        #region AddRemoveEntity

        public void AddRenderEntity(Entity entity)
        {
            if (entity == null) return;

            switch (entity.EntityType)
            {
                case EntityType.Actor:
                    if(!Characters.Contains(entity))
                        Characters.Add(entity);
                    break;
                case EntityType.Stage:
                    if (!Stages.Contains(entity))
                        Stages.Add(entity);
                    break;
                case EntityType.VFX:
                case EntityType.Model: //Currently Xv2Submesh is only used in this case for an EMO. If that ever changes, this will also need to be changed
                    if (!Effects.Contains(entity))
                        Effects.Add(entity);
                    break;
                default:
                    Log.Add($"RenderSystem: Cannot add EntityType {entity.EntityType}!", LogType.Debug);
                    break;
            }
        }

        public void RemoveRenderEntity(Entity entity)
        {
            switch (entity.EntityType)
            {
                case EntityType.Actor:
                    CharasToRemove.Add(entity);
                    break;
                case EntityType.Stage:
                    StagesToRemove.Add(entity);
                    break;
                case EntityType.VFX:
                case EntityType.Model: //Currently Xv2Submesh is only used in this case for an EMO. If that ever changes, this will also need to be changed
                    EffectsToRemove.Add(entity);
                    break;
                default:
                    Log.Add($"RenderSystem: Cannot remove EntityType {entity.EntityType}!", LogType.Debug);
                    break;
            }
        }

        public void AddRenderEntity<T>(IEnumerable<T> entities) where T : Entity
        {
            foreach (T entity in entities)
            {
                AddRenderEntity(entity);
            }
        }

        public void RemoveRenderEntity<T>(IEnumerable<T> entities) where T : Entity
        {
            foreach(T entity in entities)
            {
                RemoveRenderEntity(entity);
            }
        }
        
        public void MoveRenderEntityToFront(Entity entity)
        {
            if (Characters.Contains(entity))
            {
                Characters.Remove(entity);
                Characters.Add(entity);
            }
            else if (Stages.Contains(entity))
            {
                Stages.Remove(entity);
                Stages.Add(entity);
            }
            else if (Effects.Contains(entity))
            {
                Effects.Remove(entity);
                Effects.Add(entity);
            }
        }
        #endregion

        public bool CheckDrawPass(Xv2ShaderEffect material)
        {
            if (material.MatParam.AlphaBlend == 0 && CurrentDrawPass == Rendering.DrawPass.Opaque) return true;
            if (material.MatParam.AlphaBlend == 0 && CurrentDrawPass != Rendering.DrawPass.Opaque) return false;

            if (material.MatParam.AlphaBlendType == 0 && CurrentDrawPass == Rendering.DrawPass.AlphaBlend) return true;
            if (material.MatParam.AlphaBlendType == 1 && CurrentDrawPass == Rendering.DrawPass.Additive) return true;
            if (material.MatParam.AlphaBlendType == 2 && CurrentDrawPass == Rendering.DrawPass.Subtractive) return true;

            return false;
        }
    }

    public enum ScreenshotType
    {
        TransparentBackground,
        CustomBackgroundColor,
        Both
    }

    public enum ScreenshotFormat
    {
        PNG,
        JPG
    }

    public enum DrawPass
    {
        Opaque,
        AlphaBlend,
        Additive,
        Subtractive
    }
}
