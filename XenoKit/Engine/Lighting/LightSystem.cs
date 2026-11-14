using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Media.Media3D;
using XenoKit.Editor;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Stage;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.Resource;

namespace XenoKit.Engine.Lighting
{
    public class LightSystem : EngineObject
    {
        public DirLight SceneLight { get; private set; } = new DirLight();
        public BackgroundLight BgLight { get; private set; } = new BackgroundLight();

        private readonly DirLight ActorLight = new DirLight();
        private readonly VirtualLightSource ActorVirtualLightSource = new VirtualLightSource();
        private readonly DirLight EffectLight = new DirLight();

        private const int INITIAL_POINT_LIGHT_SIZE = 32;
        private const int MAX_POINT_LIGHTS = 128;
        private readonly List<PointLight> PointLights = new List<PointLight>(INITIAL_POINT_LIGHT_SIZE);
        private readonly List<PointLight> PointLightPool = new List<PointLight>(INITIAL_POINT_LIGHT_SIZE);

        public LightMode LightMode;

        public LightSystem()
        {
            Xv2Stage.CurrentSpmChanged += Xv2Stage_CurrentSpmChanged;
            UpdateLightsFromSpm();

            //Add a few initial point lights to the pool
            for(int i = 0; i < 4; i++)
            {
                PointLightPool.Add(new PointLight());
            }

            BgLight.PremultiplySpecular = true;
            EffectLight.PremultiplySpecular = true;
        }

        public override void Update()
        {
            BgLight.Update();
            
            SceneLight.CalculateCameraSpaceDirection(Camera.InverseViewMatrix);
            BgLight.CalculateCameraSpaceDirection(Camera.InverseViewMatrix);
        }

        /// <summary>
        /// Calculates lighting for the specified mode. Future calls to <see cref="GetActiveLight"/> with the same mode will return the calculated lighting, untill it is overwritten by another call to this method.
        /// </summary>
        /// <param name="mode">The light mode to calculate lighting for. Only <see cref="LightMode.Character"/> and <seealso cref="LightMode.Vfx"/> are accepted.</param>
        public void CalculateLighting(LightMode mode, Vector3 target = default, float radius = 1f, int actorSlot = -1)
        {
            switch (mode)
            {
                case LightMode.Character:
                    if(actorSlot >= 0 && actorSlot < SceneManager.NumActors)
                        CalculateCharacterLighting(actorSlot);
                    break;
                case LightMode.Vfx:
                    CalculateEffectLighting(target, radius);
                    break;
            }
        }

        /// <summary>
        /// Get the light for the current LightMode.
        /// </summary>
        /// <remarks>
        /// Returns already calculated lights - make sure a call to <see cref="CalculateLighting"/> is made for LightMode.Character and LightMode.Vfx before drawing.
        /// </remarks>
        /// <returns></returns>
        public DirLight GetActiveLight()
        {
            switch (LightMode)
            {
                case LightMode.Character:
                    return ActorLight;
                case LightMode.Stage:
                    return BgLight;
                case LightMode.Vfx:
                    //return BgLight;
                    return EffectLight;
                case LightMode.VfxCharaLight:
                    return SceneLight;
                default:
                    return SceneLight;
            }
        }

        public VirtualLightSource GetVirtualPointLight()
        {
            return ActorVirtualLightSource;
        }

        public PointLight RentPointLight()
        {
            if (PointLights.Count == MAX_POINT_LIGHTS)
                return null;

            if (PointLightPool.Count > 0)
            {
                var light = PointLightPool[0];

                PointLightPool.RemoveAt(0);
                PointLights.Add(light);

                return light;
            }
            else
            {
                PointLight light = new PointLight();
                PointLights.Add(light);

                return light;
            }
        }

        public void ReturnPointLight(PointLight pointLight)
        {
            if (pointLight is null) return;

            if (PointLights.Contains(pointLight))
            {
                PointLights.Remove(pointLight);
            }

            if(!PointLightPool.Contains(pointLight) && PointLightPool.Count + PointLights.Count < MAX_POINT_LIGHTS)
            {
                pointLight.Reset();
                PointLightPool.Add(pointLight);
            }
        }

        #region Light Calculation
        private const float LuminanceEpsilon = 1.1920929e-7f;
        private const float BendThreshold = 0.2f;
        private const float BlendAlphaScale = 0.5f;

        private void CalculateEffectLighting(Vector3 target, float radius)
        {
            //Handles mode 2 lighting (Effect without 0x2000000 flag)
            //Mode 2 lights the effect with the stage light (BgLight) with any valid point lights blended in (those bright and close enough and with the 0x40 flag)
            //I have opted to limit this to just EMOs for now, with EMP / ETR falling back to the character lighting (acting as if flag 0x2000000 is set)

            float bgLightLuminance = BgLight.CalcLuminance();
            Vector3 directionSum = BgLight.Direction;// * bgLightLuminance;
            Vector4 diff = new Vector4(BgLight.Diffuse, BgLight.DiffuseIntensity);
            Vector4 spc = new Vector4(BgLight.Specular, BgLight.SpecularIntensity);

            //BlendColor(ref diff, new Vector4(BgLight.Diffuse, 0), BgLight.DiffuseIntensity);
            //BlendColor(ref spc, new Vector4(BgLight.Specular, 0), BgLight.SpecularIntensity);

            foreach(var light in PointLights)
            {
                if (!light.FlagCheck(EepkEffectPartFlags2.LightApplyToVfx)) 
                    continue;

                if (!light.CalcLuminanceEffect(target, radius, out float luminance, out float attenuation))
                    continue;

                directionSum += Vector3.Normalize(target - light.Position) * luminance;

                BlendColor(ref diff, light.Color, light.Color.W * attenuation);
            }

            Vector3 dir = directionSum.X == 0 && directionSum.Y == 0 && directionSum.Z == 0 ? new Vector3(0, -1, 0) : Vector3.Normalize(directionSum);

            EffectLight.SetDirection(dir);
            EffectLight.SetDiffuse(new Vector3(diff.X, diff.Y, diff.Z), diff.W);
            EffectLight.SetSpecular(new Vector3(spc.X, spc.Y, spc.Z), spc.W);
            EffectLight.CalculateCameraSpaceDirection(Camera.InverseViewMatrix);
        }

        private void CalculateCharacterLighting(int actorSlot)
        {
            //Handles mode 0 lights (characters)
            Actor actor = SceneManager.Actors[actorSlot];

            if (actor is null)
                return;

            ActorLight.SetDiffuse(SceneLight.Diffuse, SceneLight.DiffuseIntensity);
            ActorLight.SetSpecular(SceneLight.Specular, SceneLight.SpecularIntensity);
            ActorLight.SetDirection(SceneLight.Direction);
            ActorVirtualLightSource.Reset();

            //No point lights = just use SceneLight as is
            if (PointLights.Count == 0)
            {
                ActorLight.CalculateCameraSpaceDirection(Camera.InverseViewMatrix);
                return;
            }

            Vector3 target = actor.Transform.Translation;
            Vector3 mergedPosition = new Vector3(0, 0, 0);
            Vector4 accColor = Vector4.Zero;
            float accStartRange = 0f, accEndRange = 0f, accLuminance = 0f;

            //Merge all point lights into a single source, weighted by luminance
            foreach (var light in PointLights)
            {
                if (light.UpdateTick + 1 < ViewportInstance.Tick) continue; //Skip lights that are "stale" (not updated this frame). These would be lights from an invalid context (VfxManager / VfxPreview)

                if (light.FlagCheck(EepkEffectPartFlags2.LightApplyToCharacter) && light.OwnerCheck(actor))
                {
                    if (!light.CalcLuminanceCharacter(target, out float lum, out float atten) || lum < LuminanceEpsilon)  continue;

                    float w = lum / (lum + accLuminance); 
                    float inv = 1f - w;

                    mergedPosition = Vector3.Lerp(mergedPosition, light.Position, w);
                    accEndRange = inv * accEndRange + light.RangeEnd * w;
                    accStartRange = inv * accStartRange + light.RangeStart * w;

                    //Color + intensity blend, weighted by intensity * attenuation.
                    BlendColor(ref accColor, light.Color, light.Color.W * atten);

                    accLuminance = MathF.Max(accLuminance, lum);
                }
            }

            //Early out if the point lights contribute nothing
            if (accLuminance <= LuminanceEpsilon)
            {
                ActorLight.CalculateCameraSpaceDirection(Camera.InverseViewMatrix);
                return;
            }

            Vector3 direction = SceneLight.Direction;

            //Bend main camera direction towards the point light position if luminance exceeds a threshold
            //Gives the "wavey" effect to the lighting that light.emas have
            if (accLuminance > BendThreshold)
            {
                float t = MathF.Min(accLuminance - BendThreshold, 1f);

                Vector3 toTarget = Vector3.Normalize(target - mergedPosition);
                direction = Vector3.Normalize(Vector3.Lerp(direction, toTarget, t));
            }

            ActorLight.SetDirection(Vector3.Normalize(direction));
            ActorLight.CalculateCameraSpaceDirection(Camera.InverseViewMatrix);

            ActorVirtualLightSource.SetVirtualPointLight(accColor,
                new Vector4(accStartRange, accEndRange, 0, 0),
                new Vector4(1f, MathHelpers.Clamp(0f, 1f, accColor.W * BlendAlphaScale), 0, 0),
                mergedPosition);
        }

        private static void BlendColor(ref Vector4 accumulation, Vector4 light, float intensity)
        {
            if (intensity <= 0f) return;

            float wc = intensity / (intensity + accumulation.W);

            float inv = 1f - wc;
            accumulation = new Vector4(
                MathF.Max(0f, accumulation.X * inv + light.X * wc),
                MathF.Max(0f, accumulation.Y * inv + light.Y * wc),
                MathF.Max(0f, accumulation.Z * inv + light.Z * wc),
                MathF.Max(0f, intensity * wc + inv * accumulation.W));
        }

        #endregion

        #region SPM Updating

        private void Xv2Stage_CurrentSpmChanged(object sender, EventArgs e)
        {
            UpdateLightsFromSpm();
        }

        private void UpdateLightsFromSpm()
        {
            if (ViewportInstance.CurrentStage is null || ViewportInstance.CurrentStage.CurrentSpm is null)
            {
                Log.Add("LightSystem: No SPM found; cannot set light values", LogType.Error);
                return;
            }

            //Note: light1 from SPM is not actually used, only light0 and BgLight are
            var spm = ViewportInstance.CurrentStage.CurrentSpm;

            SceneLight.SetDirection(new Vector3(spm.Light0DirX, spm.Light0DirY, spm.Light0DirZ));
            SceneLight.SetDiffuse(new Vector3(spm.Light0DiffuseColorR, spm.Light0DiffuseColorG, spm.Light0DiffuseColorB), spm.Light0DiffuseColorA);
            SceneLight.SetSpecular(new Vector3(spm.Light0SpecularColorR, spm.Light0SpecularColorG, spm.Light0SpecularColorB), spm.Light0SpecularColorA);

            BgLight.SetDirection(new Vector3(spm.BgLightDirectionX, spm.BgLightDirectionY, spm.BgLightDirectionZ));
            BgLight.SetDiffuse(new Vector3(spm.BgLightDiffuseR, spm.BgLightDiffuseG, spm.BgLightDiffuseB), spm.BgLightDiffuseA);
            BgLight.SetSpecular(new Vector3(spm.BgLightSpecularR, spm.BgLightSpecularG, spm.BgLightSpecularB), spm.BgLightSpecularA);
        }

        #endregion
    }

    public enum LightMode
    {
        Character = 0, //SceneLight (light0 from SPM), with direction modified by active point lights. Animated point lights are merged into a single source (VirtualLightSource) for use in character shaders.
        Stage = 1, //BgLight
        Vfx = 2, //BgLight, with active point lights blended into its diffuse color
        VfxCharaLight = 3 //SceneLight, requires effect flag 0x2000000
    }
}
