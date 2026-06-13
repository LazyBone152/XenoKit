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
        public override void Draw()
        {
            const int LOW_REZ_NONE = 0, LOW_REZ = 1, LOW_REZ_SMOKE = 2;

            if (!DrawThisFrame) return;
            MeshDrawCalls = 0;

            //Clear the common depth buffer
            GraphicsDevice.SetRenderTarget(RenderSystem.DepthBuffer.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);

            //Reflection Pass
            IsReflectionPass = true;
            Camera.SetReflectionView(true);
            GraphicsDevice.SetRenderTarget(ReflectionRT.RenderTarget);
            GraphicsDevice.Clear(ReflectionBackgroundColor);
            DrawEntity(Reflections, -1); //Ignore LowRez param, since reflections are already rendering at 1/4 screen res
            IsReflectionPass = false;

            //Shadow Pass (Chara + Stage Enviroment)
            IsShadowPass = true;
            Camera.SetReflectionView(false);
            GraphicsDevice.SetRenderTarget(ShadowPassRT0.RenderTarget);
            GraphicsDevice.SetDepthBuffer(ShadowPassRT0.RenderTarget);
            GraphicsDevice.Clear(Color.Red);

            //ShadowMapRes == 16 means shadows are disabled
            if (SettingsManager.settings.XenoKit_ShadowMapRes > 16)
            {
                if (SceneManager.UseRenderScene)
                    RenderScene.DrawPass(false);

                DrawSimpleEntity(Characters, false);
                DrawSimpleEntity(Stages, false);

                if (DumpShadowMapNextFrame)
                    DumpRenderTargets();
            }
            IsShadowPass = false;

            //Normals Pass (Chara)
            SetRenderTargets(NormalPassRT0.RenderTarget, NormalPassRT1.RenderTarget);
            GraphicsDevice.Clear(NormalsBackgroundColor);
            DrawSimpleEntity(Characters, true);

            if (SceneManager.UseRenderScene)
                RenderScene.DrawPass(true);

            //Color Pass (Chara + Stage Enviroment)
            SetRenderTargets(ColorPassRT0.RenderTarget, ColorPassRT1.RenderTarget, NormalPassRT1.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            GraphicsDevice.SetDepthBuffer(DepthBuffer.RenderTarget);
            DrawEntity(Characters, LOW_REZ_NONE);

            if (SceneManager.UseRenderScene)
                DrawRenderScene(RenderPipelineStage.ModelMain);

            //Create SamplerAlphaDepth
            SetRenderTargets(SamplerAlphaDepth.RenderTarget);
            GraphicsDevice.Clear(Color.Red);
            GraphicsDevice.SetDepthAsTexture(DepthBuffer.RenderTarget, 0);
            PostFilter.Apply(AGE_TEST_DEPTH_TO_PFXD);
            GraphicsDevice.Textures[0] = null;

            if (!ViewportInstance.IsBlackVoid)
            {
                //Some stage objects should be drawn AFTER SamplerAlphaDepth is created, while others are drawn before

                SetRenderTargets(ColorPassRT0.RenderTarget, ColorPassRT1.RenderTarget, NormalPassRT1.RenderTarget);
                GraphicsDevice.SetDepthBuffer(DepthBuffer.RenderTarget);
                DrawEntity(Stages, LOW_REZ_NONE);
            }

            //Black Chara Outline Shader
            if (SettingsManager.settings.XenoKit_UseOutlinePostEffect)
            {
                SetRenderTargets(ColorPassRT0.RenderTarget, ColorPassRT1.RenderTarget, NormalPassRT1.RenderTarget);
                GraphicsDevice.SetDepthBuffer(DepthBuffer.RenderTarget);
                SetTexture(NormalPassRT0.RenderTarget);
                PostFilter.Apply(AGE_TEST_EDGELINE_MRT);
            }

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
            _particleCount = ParticleBatcher.NumTotalBatched;
            DrawEntity(Effects, LOW_REZ_NONE);
            DrawParticleBatcher(LOW_REZ_NONE);

            if (SceneManager.UseRenderScene)
                DrawRenderScene(RenderPipelineStage.EffectInitial);

            //LowRez Pass
            SetRenderTargets(LowRezRT0.RenderTarget, LowRezRT1.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            UseDepthToDepth();
            DrawEntity(Effects, LOW_REZ);
            DrawParticleBatcher(LOW_REZ);
            DrawEntity(Effects, LOW_REZ_SMOKE); //LowRezSmoke pass is broken... effects dont render. So for now, render them in LowRez pass until its fixed
            DrawParticleBatcher(LOW_REZ_SMOKE);

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
            //DisplayRenderTarget(ReflectionRT.RenderTarget, true);

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

        public void CreateSmallScene(RenderTarget2D finalScene)
        {
            SetRenderTargets(SmallSceneRT.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            DisplayRenderTarget(finalScene, false, SmallSceneRT.ResolutionScale);

            SetRenderTargets(finalScene);
        }

        private void DrawSimpleEntity(List<RenderObject> entities, bool normalPass)
        {
            foreach (RenderObject entity in entities.ToArray())
            {
                if (entity.DrawThisFrame)
                {
                    if (entity.EngineObjectType != EngineObjectTypeEnum.Actor && normalPass) continue; //skip if stage and its a normal pass (stages only have a shadow pass here)
                    entity.DrawPass(normalPass);
                }
            }
        }

        private void DrawEntity(List<RenderObject> entities, int lowRezMode)
        {
            if (SceneManager.UseRenderScene) return;
            int particleCount = 0;
            RenderObject[] drawEntities = entities.ToArray();

            //OPAQUE PASS
            CurrentDrawPass = Rendering.DrawPass.Opaque;

            foreach (RenderObject entity in drawEntities)
            {
                if (entity.LowRezMode != lowRezMode && lowRezMode != -1) continue;

                if (entity.DrawThisFrame)
                {
                    entity.Draw();

                    if (entity.EngineObjectType == EngineObjectTypeEnum.VFX)
                        particleCount++;
                }
            }

            //ALPHA BLEND PASS
            CurrentDrawPass = Rendering.DrawPass.AlphaBlend;

            //foreach (Entity entity in entities.OrderByDescending(x => System.Numerics.Vector3.Distance(CameraBase.CameraState.Position, x.AbsoluteTransform.Translation)))
            foreach (RenderObject entity in drawEntities)
            {
                if (entity.LowRezMode != lowRezMode && lowRezMode != -1) continue;

                if (entity.DrawThisFrame)
                {
                    entity.Draw();
                }
            }

            //ADDITIVE PASS
            CurrentDrawPass = Rendering.DrawPass.Additive;

            foreach (RenderObject entity in drawEntities)
            {
                if (entity.LowRezMode != lowRezMode && lowRezMode != -1) continue;

                if (entity.DrawThisFrame)
                {
                    entity.Draw();
                }
            }

            //SUBTRACTIVE PASS
            CurrentDrawPass = Rendering.DrawPass.Subtractive;

            foreach (RenderObject entity in drawEntities)
            {
                if (entity.LowRezMode != lowRezMode && lowRezMode != -1) continue;

                if (entity.DrawThisFrame)
                {
                    entity.Draw();
                    entity.DrawThisFrame = false;
                }
            }

            _particleCount += particleCount;
        }

        private void DrawRenderScene(RenderPipelineStage stage)
        {
            if (RenderScene == null || RenderScene.RenderPipelineStage != stage) return;

            CurrentDrawPass = Rendering.DrawPass.Opaque;
            RenderScene.Draw();

            CurrentDrawPass = Rendering.DrawPass.AlphaBlend;
            RenderScene.Draw();

            CurrentDrawPass = Rendering.DrawPass.Additive;
            RenderScene.Draw();

            CurrentDrawPass = Rendering.DrawPass.Subtractive;
            RenderScene.Draw();
        }

        private void DrawParticleBatcher(int lowRezMode)
        {
            CurrentDrawPass = Rendering.DrawPass.Opaque;
            ParticleBatcher.Draw(lowRezMode);

            CurrentDrawPass = Rendering.DrawPass.AlphaBlend;
            ParticleBatcher.Draw(lowRezMode);

            CurrentDrawPass = Rendering.DrawPass.Additive;
            ParticleBatcher.Draw(lowRezMode);

            CurrentDrawPass = Rendering.DrawPass.Subtractive;
            ParticleBatcher.Draw(lowRezMode);
        }

        public bool CheckDrawPass(Xv2ShaderEffect material)
        {
            if (material.MatParam.AlphaBlend == 0 && CurrentDrawPass == Rendering.DrawPass.Opaque) return true;
            if (material.MatParam.AlphaBlend == 0 && CurrentDrawPass != Rendering.DrawPass.Opaque) return false;

            //Handle AlphaSortMask; alphaBlend objects shouldn't be sorted with this flag
            //todo: move to a seperate pass?
            if (material.MatParam.AlphaBlend == 1 && material.MatParam.AlphaBlendType == 0 && material.MatParam.AlphaSortMask == 1 && CurrentDrawPass == Rendering.DrawPass.Opaque) return true;
            if (material.MatParam.AlphaBlend == 1 && material.MatParam.AlphaBlendType == 0 && material.MatParam.AlphaSortMask == 1 && CurrentDrawPass != Rendering.DrawPass.Opaque) return false;

            if (material.MatParam.AlphaBlendType == 0 && CurrentDrawPass == Rendering.DrawPass.AlphaBlend) return true;
            if (material.MatParam.AlphaBlendType == 1 && CurrentDrawPass == Rendering.DrawPass.Additive) return true;
            if (material.MatParam.AlphaBlendType == 2 && CurrentDrawPass == Rendering.DrawPass.Subtractive) return true;

            return false;
        }

    }
}
