using System.Linq;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMA;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Vfx.Asset
{
    //Only support for 1 light per actor right now

    public class VfxLight : VfxAsset
    {
        private readonly EMA_File EmaFile;

        private ushort EmaIndex;
        private EMA_Animation Animation;

        private float Time = 0f;
        public float[] RGBA = new float[4]; //g_vColor0_PS
        public float[] Light = new float[4]; //g_vParam4_PS
        public float[] LightStrength = new float[4]; //g_vParam3_PS (Ambient Light Strength = 1f, Anim Light Strength = A / 2, capped to 10 max)
        public float[] LightSourcePosition = new float[4]; //g_vParam5_PS, relative to local world

        //Keyframe index cache, for better performance when keyframe seeking
        private readonly int[] ColorKeyframeIndex = new int[4];
        private readonly int[] LightKeyframeIndex = new int[2];

        public VfxLight(EMA_File emaFile, EffectPart effectPart, Actor actor, GameBase gameBase) : base(Microsoft.Xna.Framework.Matrix.Identity, effectPart, actor, gameBase)
        {
            EmaFile = emaFile;
            SetDefaultValues();
            SetAnimation();
        }

        private void ResetKeyframeIndex()
        {
            ColorKeyframeIndex[0] = ColorKeyframeIndex[1] = ColorKeyframeIndex[2] = ColorKeyframeIndex[3] = 0;
            LightKeyframeIndex[0] = LightKeyframeIndex[1] = 0;
        }

        private void SetDefaultValues()
        {
            RGBA[0] = RGBA[1] = RGBA[2] = RGBA[3] = 0f;
            Light[0] = Light[1] = 0f;
            LightStrength[0] = 1f;
            LightStrength[1] = 0f;
            LightSourcePosition[0] = LightSourcePosition[1] = LightSourcePosition[2] = LightSourcePosition[3] = 0f;
        }

        private void SetAnimation()
        {
            Animation = EmaFile.Animations.FirstOrDefault(x => x.Index == EffectPart.EMA_AnimationIndex);
            EmaIndex = EffectPart.EMA_AnimationIndex;
        }

        public override void Update()
        {
            if (!SettingsManager.Instance.Settings.XenoKit_VfxSimulation)
            {
                SetDefaultValues();
                return;
            }

            base.Update();
            Update(false);
        }

        public override void Simulate()
        {
            Update(true);
        }

        private void Update(bool simulate)
        {
            //Animation has been changed
            if (EffectPart.EMA_AnimationIndex != EmaIndex)
            {
                SetAnimation();
            }

            SetDefaultValues();

            if (Animation == null)
                return;

            ushort loopEnd = EffectPart.EMA_LoopEndFrame != 0 ? EffectPart.EMA_LoopEndFrame : Animation.EndFrame;

            if(Time > loopEnd && EffectPart.EMA_Loop)
            {
                Time = EffectPart.EMA_LoopStartFrame;
                ResetKeyframeIndex();
            }
            else if(Time > Animation.EndFrame)
            {
                IsFinished = true;
                return;
            }

            foreach(EMA_Command comand in Animation.Nodes[0].Commands)
            {
                //RGBA
                if(comand.Parameter == 2)
                {
                    int keyframeIndex = ColorKeyframeIndex[comand.Component];
                    RGBA[comand.Component] = comand.GetKeyframeValue(Time);
                    ColorKeyframeIndex[comand.Component] = keyframeIndex;
                }

                //Light
                if (comand.Parameter == 3)
                {
                    int keyframeIndex = ColorKeyframeIndex[comand.Component];
                    Light[comand.Component] = comand.GetKeyframeValue(Time);
                    ColorKeyframeIndex[comand.Component] = keyframeIndex;
                }
            }

            //Animation light strength is always A / 2, clamped to [0,10] (at least, when theres only 1 light in play)
            LightStrength[1] = MathHelpers.Clamp(0f, 10f, RGBA[3] / 2f);

            if (simulate)
            {
                Time += 1f;
            }
            else if(SceneManager.IsPlaying)
            {
                Time += EffectPart.UseTimeScale ? SceneManager.MainAnimTimeScale * SceneManager.BacTimeScale : 1f;
            }
        }
    }
}
