using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using XenoKit.Editor;
using XenoKit.Engine.Model;
using XenoKit.Engine.Scripting.BAC;
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

            bool hasActiveBodyOutline = UpdateBodyOutlinePalette();
            if (hasActiveBodyOutline)
                CopyRenderTarget(NormalPassRT1, BodyOutlineSourceRT);

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
            if (SettingsManager.settings.XenoKit_UseOutlinePostEffect || hasActiveBodyOutline)
            {
                SetRenderTargets(
                    ColorPassRT0.RenderTarget,
                    ColorPassRT1.RenderTarget,
                    hasActiveBodyOutline ? BodyOutlineSourceRT.RenderTarget : NormalPassRT1.RenderTarget);
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

            if (hasActiveBodyOutline)
            {
                SetTextures(BodyOutlineSourceRT.RenderTarget, BodyOutlinePaletteTexture);
                PostFilter.Apply(EDGELINE_VFX);
            }

            //Apply blur filter to LowRezSmoke
            SetRenderTargets(LowRezSmokeRT0_New.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            SetTexture(LowRezSmokeRT0.RenderTarget);
            PostFilter.SetNineConeOffsets(1f / (CurrentRT_Width * 2), 1f / (CurrentRT_Height * 2));
            PostFilter.Apply(NineConeFilter);

            //Merge onto main RT
            SetRenderTargets(NextColorPassRT0.RenderTarget, ColorPassRT1.RenderTarget);
            GraphicsDevice.SetDepthBuffer(DepthBuffer.RenderTarget);
            SetTextures(LowRezRT0.RenderTarget, LowRezSmokeRT0_New.RenderTarget, LowRezRT1.RenderTarget, LowRezSmokeRT1.RenderTarget);
            PostFilter.SetDefaultTexCord2();
            PostFilter.Apply(AGE_MERGE_AddLowRez_AddMrt);

            ApplyScreenEffects();

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

        private bool UpdateBodyOutlinePalette()
        {
            bool changed = false;
            bool hasActiveBodyOutline = false;
            bool renderBpeEffects = SettingsManager.settings.XenoKit_BpeSimulation;

            for (int actorSlot = 0; actorSlot < SceneManager.NumActors; actorSlot++)
            {
                Actor actor = SceneManager.Actors[actorSlot];
                int paletteIndex = 1 + actorSlot;
                Color color = new Color(0, 0, 0, 0);

                if (renderBpeEffects && actor?.ShaderParameters.BodyOutlineActive == true)
                {
                    hasActiveBodyOutline = true;
                    System.Numerics.Vector4 outlineColor = actor.ShaderParameters.BodyOutlineColor;
                    color = new Color(outlineColor.X, outlineColor.Y, outlineColor.Z, outlineColor.W);
                }

                if (!BodyOutlinePalette[paletteIndex].Equals(color))
                {
                    BodyOutlinePalette[paletteIndex] = color;
                    changed = true;
                }
            }

            if (changed)
                BodyOutlinePaletteTexture.SetData(BodyOutlinePalette);

            return hasActiveBodyOutline;
        }

        private void ApplyScreenEffects()
        {
            if (!SettingsManager.settings.XenoKit_BpeSimulation)
                return;

            for (int actorSlot = 0; actorSlot < SceneManager.NumActors; actorSlot++)
            {
                BacScreenEffectState state = SceneManager.Actors[actorSlot]?.ActionControl?.BacPlayer?.BacEntryInstance?.ScreenEffectState;
                ApplyScreenEffects(state);
            }
        }

        private void ApplyScreenEffects(BacScreenEffectState state)
        {
            if (state == null)
                return;

            bool hasScreenEffect = state.BlurAmount > 0.0001f ||
                                   state.WhiteShineAmount > 0.0001f ||
                                   state.HasColorMultiply ||
                                   state.HasColorAdd ||
                                   state.HasHue ||
                                   Math.Abs(state.ZoomLevel) > 0.0001f;
            if (!hasScreenEffect)
                return;

            RenderTargetWrapper source = NextColorPassRT0;
            RenderTargetWrapper target = ScreenEffectRT;

            ApplyScreenEffectSpritePass(ref source, ref target, 1f, Vector2.Zero);

            if (state.BlurAmount > 0.0001f)
            {
                float blurU = MathHelper.Clamp(state.BlurAmount / (MathHelper.Pi * 2f), 1f / CurrentRT_Width, 0.02f);
                float blurV = blurU * CurrentRT_Width / CurrentRT_Height;

                PostFilter.SetNineConeOffsets(blurU, blurV);
                ApplyScreenEffectPass(ref source, ref target, NineConeFilter);
            }

            if (state.HasColorMultiply)
                ApplyDtmapsPass(ref source, ref target, DTMAP_BLEND_CST_MUL, state.ColorMultiply);

            if (state.HasColorAdd)
                ApplyDtmapsPass(ref source, ref target, DTMAP_BLEND_CST_ADD, state.ColorAdd);

            if (state.WhiteShineAmount > 0.0001f)
                ApplyDtmapsPass(ref source, ref target, DTMAP_BLEND_CST_ADD, new System.Numerics.Vector4(state.WhiteShineAmount, state.WhiteShineAmount, state.WhiteShineAmount, 0f));

            if (state.HasHue)
                ApplyHuePass(ref source, ref target, state);

            if (Math.Abs(state.ZoomLevel) > 0.0001f)
            {
                float zoom = MathHelper.Clamp(state.ZoomLevel, -0.9f, 1f);
                ApplyScreenEffectSpritePass(ref source, ref target, 1f + zoom, Vector2.Zero);
            }

            if (source != NextColorPassRT0)
            {
                ApplyScreenEffectSpritePass(ref source, ref target, 1f, Vector2.Zero);
            }

        }

        private void ApplyHuePass(ref RenderTargetWrapper source, ref RenderTargetWrapper target, BacScreenEffectState state)
        {
            PostShaderEffect effect;
            System.Numerics.Vector4 color = state.HueColor;
            color.W *= state.HueStrength;

            switch (state.HueMode)
            {
                case 2:
                    effect = DTMAP_BLEND_CST_HUE;
                    break;
                case 3:
                    effect = DTMAP_BLEND_CST_MUL;
                    float brightness = 1f - MathHelper.Clamp(color.W, 0f, 1f);
                    color = new System.Numerics.Vector4(brightness, brightness, brightness, 1f);
                    break;
                case 6:
                    effect = DTMAP_BLEND_CST_HUE;
                    color = new System.Numerics.Vector4(0.5f * MathHelper.Clamp(color.W, 0f, 1f), 0f, 0f, 1f);
                    break;
                case 7:
                case 8:
                    effect = DTMAP_BLEND_CST_HSV;
                    color = new System.Numerics.Vector4(color.X, 0.5f + color.Y * 0.5f, 0.5f + color.Z * 0.5f, 1f);
                    break;
                default:
                    effect = DTMAP_BLEND_CST_MUL;
                    color = System.Numerics.Vector4.Lerp(
                        System.Numerics.Vector4.One,
                        new System.Numerics.Vector4(color.X, color.Y, color.Z, 1f),
                        MathHelper.Clamp(color.W, 0f, 1f));
                    break;
            }

            ApplyDtmapsPass(ref source, ref target, effect, color, true);
        }

        private void ApplyDtmapsPass(ref RenderTargetWrapper source, ref RenderTargetWrapper target, PostShaderEffect effect, System.Numerics.Vector4 color, bool backgroundOnly = false)
        {
            if (backgroundOnly)
                CopyRenderTarget(source, target);

            SetRenderTargets(target.RenderTarget);
            GraphicsDevice.SetDepthBuffer(backgroundOnly ? DepthBuffer.RenderTarget : null);
            SetTextures(source.RenderTarget, source.RenderTarget);
            PostFilter.SetVertexColor(new Color(color.X, color.Y, color.Z, color.W));
            PostFilter.Apply(effect, backgroundOnly ? BackgroundOnlyDepthState : null);
            PostFilter.SetVertexColor(Color.White);
            SwapScreenEffectTargets(ref source, ref target);
        }

        private void ApplyScreenEffectPass(ref RenderTargetWrapper source, ref RenderTargetWrapper target, PostShaderEffect effect)
        {
            SetRenderTargets(target.RenderTarget);
            GraphicsDevice.SetDepthBuffer(null);
            SetTexture(source.RenderTarget);
            PostFilter.Apply(effect);
            SwapScreenEffectTargets(ref source, ref target);
        }

        private void ApplyScreenEffectSpritePass(ref RenderTargetWrapper source, ref RenderTargetWrapper target, float scale, Vector2 offset)
        {
            SetRenderTargets(target.RenderTarget);
            GraphicsDevice.SetDepthBuffer(null);
            GraphicsDevice.Clear(Color.Transparent);

            Vector2 targetCenter = new Vector2(target.Width * 0.5f, target.Height * 0.5f);
            Vector2 sourceOrigin = new Vector2(source.Width * 0.5f, source.Height * 0.5f);
            Vector2 position = targetCenter + new Vector2(offset.X * target.Width, offset.Y * target.Height);

            SpriteBatch.Begin(
                blendState: BlendState.Opaque,
                samplerState: SamplerState.LinearClamp,
                depthStencilState: DepthStencilState.None);
            SpriteBatch.Draw(source.RenderTarget, position, null, Color.White, 0f, sourceOrigin, scale, SpriteEffects.None, 0f);
            SpriteBatch.End();

            SwapScreenEffectTargets(ref source, ref target);
        }

        private void CopyRenderTarget(RenderTargetWrapper source, RenderTargetWrapper target)
        {
            SetRenderTargets(target.RenderTarget);
            GraphicsDevice.SetDepthBuffer(null);
            GraphicsDevice.Clear(Color.Transparent);

            SpriteBatch.Begin(
                blendState: BlendState.Opaque,
                samplerState: SamplerState.PointClamp,
                depthStencilState: DepthStencilState.None);
            SpriteBatch.Draw(source.RenderTarget, new Rectangle(0, 0, target.Width, target.Height), Color.White);
            SpriteBatch.End();
        }

        private static void SwapScreenEffectTargets(ref RenderTargetWrapper source, ref RenderTargetWrapper target)
        {
            RenderTargetWrapper oldSource = source;
            source = target;
            target = oldSource;
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
