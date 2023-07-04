using Xv2CoreLib.EMP_NEW;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticleUV
    {
        public EMP_TextureSamplerDef TextureDef { get; private set; }

        private float CurrentTime = 0f;
        private float CurrentKeyframeIndex = 0;

        public float ScrollU = 0f;
        public float ScrollV = 0f;
        public float StepU = 1f;
        public float StepV = 1f;

        public ParticleUV() { }

        public ParticleUV(EMP_TextureSamplerDef texture)
        {
            TextureDef = texture;
        }

        public void Update(float frameDelta)
        {
            if (TextureDef != null)
            {
                if (TextureDef.ScrollState.ScrollType == EMP_ScrollState.ScrollTypeEnum.Speed)
                {
                    ScrollU += TextureDef.ScrollState.ScrollSpeed_U * frameDelta;
                    ScrollV += TextureDef.ScrollState.ScrollSpeed_V * frameDelta;
                    StepU = 1f;
                    StepV = 1f;
                }
                else
                {
                    if(TextureDef.ScrollState.Keyframes.Count > 0)
                    {
                        float keyframeIndex = CurrentKeyframeIndex;

                        if (CurrentTime > TextureDef.ScrollState.Keyframes[(int)CurrentKeyframeIndex].Time && TextureDef.ScrollState.ScrollType == EMP_ScrollState.ScrollTypeEnum.SpriteSheet)
                        {
                            if (TextureDef.KeyframeType == EMP_TextureSamplerDef.KeyframeTypeEnum.Random)
                            {
                                keyframeIndex = Xv2CoreLib.Random.Range(0, TextureDef.ScrollState.Keyframes.Count - 1);
                            }
                            else if (TextureDef.KeyframeType == EMP_TextureSamplerDef.KeyframeTypeEnum.SequentialLoop)
                            {
                                keyframeIndex = (CurrentKeyframeIndex < TextureDef.ScrollState.Keyframes.Count - 1) ? keyframeIndex + 1 : 0;
                            }
                            else if (CurrentKeyframeIndex < TextureDef.ScrollState.Keyframes.Count - 1)
                            {
                                keyframeIndex += frameDelta;
                            }

                            CurrentTime = 0;
                        }

                        //Sanity check
                        if (keyframeIndex > TextureDef.ScrollState.Keyframes.Count - 1)
                            keyframeIndex = 0;

                        ScrollU = TextureDef.ScrollState.Keyframes[(int)keyframeIndex].ScrollU;
                        ScrollV = TextureDef.ScrollState.Keyframes[(int)keyframeIndex].ScrollV;
                        StepU = TextureDef.ScrollState.Keyframes[(int)keyframeIndex].ScaleU;
                        StepV = TextureDef.ScrollState.Keyframes[(int)keyframeIndex].ScaleV;

                        CurrentKeyframeIndex = keyframeIndex;
                    }
                    else
                    {
                        //No keyframes defined in EMP. Use default values.

                        ScrollU = 0f;
                        ScrollV = 0f;
                        StepU = 1f;
                        StepV = 1f;
                    }


                    CurrentTime += frameDelta;
                }
            }
        }
    
        public void SetTexture(EMP_TextureSamplerDef texture)
        {
            TextureDef = texture;
            CurrentKeyframeIndex = 0;
            CurrentTime = 0;
        }

        public void Restart()
        {
            CurrentTime = 0;
            CurrentKeyframeIndex = 0;
        }
    }
}
