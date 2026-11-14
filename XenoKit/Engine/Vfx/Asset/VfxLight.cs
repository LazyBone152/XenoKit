using System.Linq;
using XenoKit.Engine.Lighting;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMA;
using Xv2CoreLib.Resource.App;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine.Vfx.Asset
{
    public class VfxLight : VfxAsset
    {
        private readonly EMA_File EmaFile;

        private ushort EmaIndex;
        private EMA_Animation Animation;
        private ushort animationEndFrame = 0;

        private float Time = 0f;
        private ushort AnimationLoopEndFrame
        {
            get
            {
                if (EffectPart.EMA_LoopEndFrame != 0) return EffectPart.EMA_LoopEndFrame;
                return AnimationEndFrame;
            }
        }
        private ushort AnimationEndFrame
        {
            get
            {
                if (Animation != null) return animationEndFrame;
                return 1000;
            }
        }

        private readonly PointLight PointLight;

        private readonly float[] color = new float[4];
        private readonly float[] range = new float[2];

        //Keyframe index cache, for better performance when keyframe seeking
        private readonly int[] ColorKeyframeIndex = new int[4];
        private readonly int[] LightKeyframeIndex = new int[2];

        public VfxLight(Matrix4x4 startWorld, EMA_File emaFile, EffectPart effectPart, Actor actor, bool spawnedByProjectile = false) : base(startWorld, effectPart, actor, spawnedByProjectile)
        {
            PointLight = LightSystem.RentPointLight();
            PointLight?.SetInitialState(actor, effectPart);
            EmaFile = emaFile;
            SetDefaultValues();
            SetAnimation();

            //PointLight can be null if there are too many PointLights active at once (limit of 128, defined in LightSystem)
            if (PointLight is null)
                IsFinished = true;
        }

        public override void Dispose()
        {
            if(PointLight != null)
                LightSystem.ReturnPointLight(PointLight);

            base.Dispose();
        }

        private void ResetKeyframeIndex()
        {
            ColorKeyframeIndex[0] = ColorKeyframeIndex[1] = ColorKeyframeIndex[2] = ColorKeyframeIndex[3] = 0;
            LightKeyframeIndex[0] = LightKeyframeIndex[1] = 0;
        }

        private void SetDefaultValues()
        {
            PointLight?.Reset(false);
        }

        private void SetAnimation()
        {
            Animation = EmaFile.Animations.FirstOrDefault(x => x.Index == EffectPart.EMA_AnimationIndex);
            EmaIndex = EffectPart.EMA_AnimationIndex;
            animationEndFrame = (ushort)(Animation?.GetEndFrame() ?? 0);
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
            if (!HasStarted) return;

            Update(true);
        }

        private void Update(bool simulate)
        {
            if (PointLight is null) return;

            if (Time > AnimationLoopEndFrame && EffectPart.EMA_Loop)
            {
                Time = EffectPart.EMA_LoopStartFrame;
                ResetKeyframeIndex();
            }
            else if (Time >= AnimationEndFrame)
            {
                IsFinished = true;

                if(Time > AnimationEndFrame)
                    return;
            }

            //Animation has been changed
            if (EffectPart.EMA_AnimationIndex != EmaIndex)
            {
                SetAnimation();
            }

            SetDefaultValues();

            if (Animation == null)
                return;

            foreach(EMA_Command comand in Animation.Nodes[0].Commands)
            {
                //RGBA
                if(comand.Parameter == 2)
                {
                    if (comand.Component < 0 || comand.Component > 3) continue;

                    int keyframeIndex = ColorKeyframeIndex[comand.Component];
                    color[comand.Component] = comand.GetKeyframeValue(Time);
                    ColorKeyframeIndex[comand.Component] = keyframeIndex;
                }

                //Light
                if (comand.Parameter == 3)
                {
                    if (comand.Component < 0 || comand.Component > 1) continue;

                    int keyframeIndex = ColorKeyframeIndex[comand.Component];
                    range[comand.Component] = comand.GetKeyframeValue(Time);
                    ColorKeyframeIndex[comand.Component] = keyframeIndex;
                }
            }

            PointLight.SetValues(Transform.Translation, new System.Numerics.Vector4(color[0], color[1], color[2], color[3]), range[0], range[1]);

            if (simulate)
            {
                Time += 1f;
            }
            else if(ViewportInstance.IsPlaying)
            {
                Time += EffectPart.UseTimeScale ? Actor.ActiveTimeScale : 1f;
            }
        }

        public override void SeekNextFrame()
        {
            base.SeekNextFrame();
            Time++;
        }

        public override void SeekPrevFrame()
        {
            base.SeekPrevFrame();
            Time = Time == 0 ? AnimationLoopEndFrame : Time - 1;
        }
    }
}
