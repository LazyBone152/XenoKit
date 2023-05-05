using Xv2CoreLib.EMP_NEW;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticleUV
    {
        public EMP_TextureSamplerDef TextureDef { get; private set; }

        private float CurrentTime = 0f;
        private int CurrentKeyframeIndex = 0;

        public float ScrollU = 0f;
        public float ScrollV = 0f;
        public float StepU = 1f;
        public float StepV = 1f;

        public ParticleUV(EMP_TextureSamplerDef texture)
        {
            TextureDef = texture;
        }

        public void Update(bool isPlaying, bool useTimeScale)
        {
            if (isPlaying)
            {
                if (TextureDef.ScrollState.ScrollType == EMP_ScrollState.ScrollTypeEnum.Speed)
                {
                    ScrollU += TextureDef.ScrollState.ScrollSpeed_U;
                    ScrollV += TextureDef.ScrollState.ScrollSpeed_V;
                    StepU = 1f;
                    StepV = 1f;
                }
                else
                {
                    int keyframeIndex = CurrentKeyframeIndex;

                    if (CurrentTime >= TextureDef.ScrollState.Keyframes[CurrentKeyframeIndex].Time && TextureDef.ScrollState.ScrollType == EMP_ScrollState.ScrollTypeEnum.SpriteSheet)
                    {
                        if (TextureDef.KeyframeType == EMP_TextureSamplerDef.KeyframeTypeEnum.Random)
                        {
                            keyframeIndex = Xv2CoreLib.Random.Range(0, TextureDef.ScrollState.Keyframes.Count - 1);
                        }
                        else if (TextureDef.KeyframeType == EMP_TextureSamplerDef.KeyframeTypeEnum.SequentialLoop)
                        {
                            keyframeIndex = (CurrentKeyframeIndex < TextureDef.ScrollState.Keyframes.Count - 1) ? keyframeIndex + 1 : 0;
                        }
                        else if(CurrentKeyframeIndex < TextureDef.ScrollState.Keyframes.Count - 1)
                        {
                            keyframeIndex++;
                        }

                        CurrentTime = 0;
                    }

                    //Sanity check
                    if (keyframeIndex > TextureDef.ScrollState.Keyframes.Count - 1)
                        keyframeIndex = 0;

                    ScrollU = TextureDef.ScrollState.Keyframes[keyframeIndex].ScrollU;
                    ScrollV = TextureDef.ScrollState.Keyframes[keyframeIndex].ScrollV;
                    StepU = TextureDef.ScrollState.Keyframes[keyframeIndex].ScaleU;
                    StepV = TextureDef.ScrollState.Keyframes[keyframeIndex].ScaleV;

                    CurrentKeyframeIndex = keyframeIndex;

                    CurrentTime += useTimeScale ? 1f * SceneManager.BacTimeScale * SceneManager.MainAnimTimeScale : 1f;
                }
            }
        }
    }
}
