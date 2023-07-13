using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Shader;
using Xv2CoreLib.EMM;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Rendering
{
    public class RenderSystem : Entity
    {
        public readonly PostFilter PostFilter;

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

        private const int ShadowMapSize = 2048;
        private readonly Color NormalsBackgroundColor = new Color(0.50196f, 0.50196f, 0, 0);
        public bool DumpRenderTargetsNextFrame = false;

        //RTs:
        //Characters and the stage enviroments are drawn onto this RT with the different shaders (Chara: ShadowModel_W, Stage: ShadowModel, Grass: GI_ShadowModel_Grass)
        private readonly RenderTarget2D ShadowPassRT0; //Static resolution of 8192x8192 (or whatever the shadow map res is set to), so no need for auto-updating

        //Characters are drawn onto these RTs using the shader NORMAL_FADE_WATERDEPTH_W_M
        private readonly RenderTargetWrapper NormalPassRT0;
        private readonly RenderTargetWrapper NormalPassRT1;

        //Characters and the stage enviroments are drawn onto these RTs using their proper materials
        private readonly RenderTargetWrapper ColorPassRT0;
        private readonly RenderTargetWrapper ColorPassRT1;

        //ShaderProgram BIRD_BG_EDGELINE_RGB_HF is used with ColorPassRT0 as input, drawn onto this RT (adds black outline to charas)
        //The remaining stage elements are then drawn to this RT and ColorPassRT1
        private readonly RenderTargetWrapper NextColorPassRT0;

        //Some BPE effects such as BodyOutline are done at this point, and drawn onto NextColorPassRT0 + ColorPassRT1
        //Next are effects, using the same RTs

        //The final render target that everything will be drawn onto at the end of the frame. This can also be used for the "State_SamplerSmallScene" sampler as that is for the previously rendered frame (unsure about State_SamplerCurrentScene)
        private readonly RenderTargetWrapper FinalRenderTarget;

        //Global sampler RTs:
        public readonly RenderTargetWrapper SamplerAlphaDepth;

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

        public RenderSystem(GameBase game) : base(game)
        {
            PostFilter = new PostFilter(game);

            //Load shaders used for the shadow and normal passes. These are used instead of the regular shaders defined in EMM during those passes.
            ShadowModel_W = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(EmmMaterial.CreateDefaultMaterial("ShadowModel_W"), game);
            ShadowModel = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(EmmMaterial.CreateDefaultMaterial("ShadowModel"), game);
            GI_ShadowModel_Grass = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(EmmMaterial.CreateDefaultMaterial("GI_ShadowModel_Grass"), game);
            NORMAL_FADE_WATERDEPTH_W_M = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(EmmMaterial.CreateDefaultMaterial("NORMAL_FADE_WATERDEPTH_W_M"), game, ShaderType.CharaNormals);

            //Load all the shaders that are used in the rendering process
            AGE_TEST_EDGELINE_MRT = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("AGE_TEST_EDGELINE_MRT"), game);
            BIRD_BG_EDGELINE_RGB_HF = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("BIRD_BG_EDGELINE_RGB_HF"), game);
            DepthToDepth = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("DepthToDepth"), game);
            DepthToColor = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("DepthToColor"), game);
            AddTex = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("AddTex"), game);
            NineConeFilter = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("NineConeFilter"), game);
            AGE_MERGE_AddLowRez_AddMrt = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("AGE_MERGE_AddLowRez_AddMrt"), game);
            Sampler0 = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("Sampler0"), game);
            EDGELINE_VFX = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("EDGELINE_VFX"), game);
            AGE_TEST_DEPTH_TO_PFXD = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("AGE_TEST_DEPTH_TO_PFXD"), game);

            //Create RTs
            ShadowPassRT0 = new RenderTarget2D(GraphicsDevice, ShadowMapSize, ShadowMapSize, false, SurfaceFormat.Single, DepthFormat.Depth16, 0, RenderTargetUsage.PreserveContents);
            NormalPassRT0 = new RenderTargetWrapper(GraphicsDevice, 1, SurfaceFormat.Color, true, "NormalPassRT0");
            NormalPassRT1 = new RenderTargetWrapper(GraphicsDevice, 1, SurfaceFormat.Color, false, "NormalPassRT1");
            ColorPassRT0 = new RenderTargetWrapper(GraphicsDevice, 1, SurfaceFormat.Color, true, "ColorPassRT0");
            ColorPassRT1 = new RenderTargetWrapper(GraphicsDevice, 1, SurfaceFormat.Color, false, "ColorPassRT1");
            NextColorPassRT0 = new RenderTargetWrapper(GraphicsDevice, 1, SurfaceFormat.Color, true, "NextColorPassRT0");
            FinalRenderTarget = new RenderTargetWrapper(GraphicsDevice, 1, SurfaceFormat.Color, true, "FinalRenderTarget");
            SamplerAlphaDepth = new RenderTargetWrapper(GraphicsDevice, 1, SurfaceFormat.Single, false, "SamplerAlphaDepth");

            //Register all render targets so they get auto-updated if the viewport changes size
            RegisterRenderTarget(NormalPassRT0);
            RegisterRenderTarget(NormalPassRT1);
            RegisterRenderTarget(ColorPassRT0);
            RegisterRenderTarget(ColorPassRT1);
            RegisterRenderTarget(NextColorPassRT0);
            RegisterRenderTarget(FinalRenderTarget);
            RegisterRenderTarget(SamplerAlphaDepth);
        }

        public override void Draw()
        {
            //Shadow Pass (Chara + Stage Enviroment)
            GraphicsDevice.SetRenderTarget(ShadowPassRT0);
            GraphicsDevice.Clear(Color.White);
            DrawEntityList(Characters, true, false);
            DrawEntityList(Stages, true, false);

            //Normals Pass (Chara)
            GraphicsDevice.SetRenderTargets(NormalPassRT0.RenderTarget, NormalPassRT1.RenderTarget);
            GraphicsDevice.Clear(new Color(0.50196f, 0.50196f, 0, 0));
            DrawEntityList(Characters, true, true);

            //Color Pass (Chara + Stage Enviroment)
            GraphicsDevice.SetRenderTargets(ColorPassRT0.RenderTarget, ColorPassRT1.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            DrawEntityList(Characters, false, false);
            DrawEntityList(Stages, false, false);

            //Black Chara Outline Shader
            GraphicsDevice.SetRenderTargets(ColorPassRT0.RenderTarget, ColorPassRT1.RenderTarget, NormalPassRT1.RenderTarget);
            SetTexture(NormalPassRT0.RenderTarget);
            PostFilter.DisplayPostFilter(AGE_TEST_EDGELINE_MRT);

            //Create SamplerAlphaDepth
            GraphicsDevice.SetRenderTarget(SamplerAlphaDepth.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            GraphicsDevice.SetDepthAsTexture(ColorPassRT0.RenderTarget, 0);
            PostFilter.DisplayPostFilter(AGE_TEST_DEPTH_TO_PFXD);
            GraphicsDevice.Textures[0] = null;

            //SetTexture(ColorPassRT0.RenderTarget);
            //GraphicsDevice.SetRenderTarget(NextColorPassRT0.RenderTarget);
            //GraphicsDevice.Clear(Color.Transparent);
            //QuadRenderer.SetTextureCoordinates(0.02220052f, 0.0002f);
            //QuadRenderer.DrawQuad(NineConeFilter);

            //Black Stage Outline Shader
            //GraphicsDevice.SetRenderTarget(NextColorPassRT0.RenderTarget);
            //GraphicsDevice.Clear(Color.Transparent);
            //SetTexture(ColorPassRT0.RenderTarget);
            //QuadRenderer.DrawQuad(BIRD_BG_EDGELINE_RGB_HF);

            //Copy Depth from ColorRT0 onto NextColorRT0
            //DrawQuad(null, DepthToDepth);

            //Next Color Pass (Remaining Stage Elements, props, billboards...)
            //GraphicsDevice.SetRenderTargets(NextColorPassRT0.RenderTarget, ColorPassRT1.RenderTarget);
            //....

            //Effects

            /*
            //TESTING CODE:
            GraphicsDevice.SetRenderTarget(NextColorPassRT0.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);

            SetTexture(ColorPassRT0.RenderTarget);
            //QuadRenderer.SetTextureCoordinates(0.0002f, 0.00035f);
            QuadRenderer.DrawQuad(Sampler0);
            */

            //-----------------------------
            //Old Code
            GraphicsDevice.SetRenderTargets(ColorPassRT0.RenderTarget, ColorPassRT1.RenderTarget);
            _particleCount = 0;

            DrawEntityList(Effects);

            ActiveParticleCount = _particleCount;


            //Create final RenderTarget
            GraphicsDevice.SetRenderTarget(FinalRenderTarget.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            //GraphicsDevice.BlendState = BlendState.AlphaBlend;

            DisplayRenderTarget(ColorPassRT0.RenderTarget);
            //DrawQuad(NormalPassRT1.RenderTarget);
            //DrawQuad(NextColorPassRT0.RenderTarget);
            //DrawQuad(SamplerAlphaDepth.RenderTarget);

            if (DumpRenderTargetsNextFrame)
            {
                DumpRenderTargets();
            }
        }

        public void DrawParticlesTest()
        {
            _particleCount = 0;

            DrawEntityList(Effects);

            ActiveParticleCount = _particleCount;
        }

        private void DrawEntityList(List<Entity> entities, bool simpleDraw, bool normalPass)
        {
            foreach (Entity entity in entities.OrderByDescending(x => Vector3.Distance(CameraBase.CameraState.ActualPosition, x.AbsoluteTransform.Translation)))
            {
                if (entity.DrawThisFrame)
                {
                    if (simpleDraw)
                    {
                        if (entity.EntityType != EntityType.Actor && normalPass) continue;
                        entity.DrawPass(normalPass);
                    }
                    else
                    {
                        entity.Draw();
                    }
                }
            }
        }

        private void DrawEntityList(List<Entity> entities)
        {
            int particleCount = 0;

            foreach (Entity entity in entities.OrderByDescending(x => Vector3.Distance(CameraBase.CameraState.ActualPosition, x.AbsoluteTransform.Translation)))
            {
                if (entity.DrawThisFrame && entity.AlphaBlendType <= 1)
                {
                    entity.Draw();

                    if (entity.EntityType == EntityType.VFX)
                        particleCount++;
                }
            }

            //Render subtractive blend type last
            foreach (Entity entity in entities.OrderByDescending(x => Vector3.Distance(CameraBase.CameraState.ActualPosition, x.AbsoluteTransform.Translation)))
            {
                if (entity.DrawThisFrame && entity.AlphaBlendType == 2)
                {
                    entity.Draw();

                    if (entity.EntityType == EntityType.VFX)
                        particleCount++;
                }
            }

            _particleCount += particleCount;
        }

        public override void Update()
        {
            EntityListUpdate(Characters, CharasToRemove);
            EntityListUpdate(Stages, StagesToRemove);
            EntityListUpdate(Effects, EffectsToRemove);
        }

        public override void DelayedUpdate()
        {
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

            //Update RTs if the ViewPort size has changed.
            foreach (RenderTargetWrapper rt in registeredRenderTargets)
            {
                if (rt.ShouldUpdate() && GameIsFocused)
                {
                    if (rt.RenderTarget != null)
                        _toBeDisposed.Add(rt.RenderTarget);

                    rt.UpdateRenderTarget();
                }
            }
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

        public void SetTexture(Texture texture, int textureSlot = 0)
        {
            if (textureSlot >= 4)
                throw new ArgumentOutOfRangeException("RenderSystem.SetTexture: textureSlot value passed into the method was 4 or greater, which is not allowed!");

            GraphicsDevice.Textures[textureSlot] = texture;
        }

        #region RenderTarget
        public void DisplayRenderTarget(RenderTarget2D renderTarget)
        {
            GameBase.spriteBatch.Begin();
            GameBase.spriteBatch.Draw(renderTarget, new Rectangle(0, 0, renderTarget.Width, renderTarget.Height), Color.White);
            GameBase.spriteBatch.End();
        }

        private void DumpRenderTargets()
        {
            DumpRenderTargetsNextFrame = false;

            Directory.CreateDirectory(SettingsManager.Instance.GetAbsPathInAppFolder("RT_Dump"));

            using (MemoryStream ms = new MemoryStream())
            {
                ShadowPassRT0.SaveAsPng(ms, ShadowPassRT0.Width, ShadowPassRT0.Height);
                File.WriteAllBytes(SettingsManager.Instance.GetAbsPathInAppFolder($"RT_Dump/ShadowPassRT0.png"), ms.ToArray());
            }

            foreach (RenderTargetWrapper renderTarget in registeredRenderTargets)
            {
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
                    Characters.Add(entity);
                    break;
                case EntityType.Stage:
                    Stages.Add(entity);
                    break;
                case EntityType.VFX:
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
        #endregion
    }
}
