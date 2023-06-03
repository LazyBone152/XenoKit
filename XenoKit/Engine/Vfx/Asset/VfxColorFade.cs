using Xv2CoreLib.EEPK;
using Xv2CoreLib.ECF;
using LB_Common.Numbers;
using System.Collections.Generic;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Vfx.Asset
{
    public class VfxColorFade : VfxAsset
    {
        private readonly ECF_File EcfFile;

        private List<VfxColorFadeEntry> Values;

        public VfxColorFade(ECF_File ecfFile, EffectPart effectPart, Actor actor, GameBase gameBase) : base(Microsoft.Xna.Framework.Matrix.Identity, effectPart, actor, gameBase)
        {
            EcfFile = ecfFile;
            Values = new List<VfxColorFadeEntry>(ecfFile.Nodes.Count);
            SetDefaultValues();

            if (EcfFile.Nodes.Count == 0)
                IsFinished = true;
        }

        private void SetDefaultValues()
        {
            for(int i = 0; i < EcfFile.Nodes.Count; i++)
            {
                if (Values.Count <= i)
                    Values.Add(new VfxColorFadeEntry());

                Values[i].Multi[0] = 1;
                Values[i].Multi[1] = 1;
                Values[i].Multi[2] = 1;
                Values[i].Multi[3] = 1;
                Values[i].RimColor[0] = 0;
                Values[i].RimColor[1] = 0;
                Values[i].RimColor[2] = 0;
                Values[i].AddColor[0] = 0;
                Values[i].AddColor[1] = 0;
                Values[i].AddColor[2] = 0;
            }
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
            int active = 0;

            for (int i = 0; i < EcfFile.Nodes.Count; i++)
            {
                if (Values.Count <= i)
                    Values.Add(new VfxColorFadeEntry());

                if (Values[i].IsFinished) continue;
                active++;

                if (Values[i].Time > EcfFile.Nodes[i].EndTime)
                {
                    if (EcfFile.Nodes[i].Loop && !IsTerminating)
                    {
                        Values[i].Time = EcfFile.Nodes[i].StartTime;
                    }
                    else
                    {
                        Values[i].IsFinished = true;
                    }
                }

                if (Values[i].Time >= EcfFile.Nodes[i].StartTime && !simulate)
                {
                    float time = Values[i].Time / (float)EcfFile.Nodes[i].EndTime;

                    float[] multi = EcfFile.Nodes[i].MultiColor.GetInterpolatedValue(time);
                    float[] rim = EcfFile.Nodes[i].RimColor.GetInterpolatedValue(time);
                    float[] add = EcfFile.Nodes[i].AddColor.GetInterpolatedValue(time);
                    float multi_A = EcfFile.Nodes[i].MultiColor_Transparency.GetInterpolatedValue(time);
                    float rim_A = EcfFile.Nodes[i].RimColor_Transparency.GetInterpolatedValue(time);
                    float add_A = EcfFile.Nodes[i].AddColor_Transparency.GetInterpolatedValue(time);

                    Values[i].Multi[0] = multi[0] * multi_A;
                    Values[i].Multi[1] = multi[1] * multi_A;
                    Values[i].Multi[2] = multi[2] * multi_A;
                    Values[i].Multi[3] = EcfFile.Nodes[0].BlendingFactor.GetInterpolatedValue(time);
                    Values[i].RimColor[0] = rim[0] * rim_A;
                    Values[i].RimColor[1] = rim[1] * rim_A;
                    Values[i].RimColor[2] = rim[2] * rim_A;
                    Values[i].AddColor[0] = add[0] * add_A;
                    Values[i].AddColor[1] = add[1] * add_A;
                    Values[i].AddColor[2] = add[2] * add_A;
                }

                if (simulate)
                {
                    Values[i].Time += 1f;
                }
                else if (SceneManager.IsPlaying)
                {
                    Values[i].Time += EffectPart.UseTimeScale ? SceneManager.MainAnimTimeScale * SceneManager.BacTimeScale : 1f;
                }
            }

            if (active == 0)
            {
                IsFinished = true;
            }
        }

        public VfxColorFadeEntry GetColorFadeEntry(string material)
        {
            for(int i = 0; i < EcfFile.Nodes.Count; i++)
            {
                if (Values.Count <= i) continue;

                if((EcfFile.Nodes[i].UseMaterial && EcfFile.Nodes[i].Material.Equals(material,System.StringComparison.OrdinalIgnoreCase)) || !EcfFile.Nodes[i].UseMaterial)
                {
                    return Values[i];
                }
            }

            return null;
        }
    }

    public class VfxColorFadeEntry
    {
        public float Time = 0f;
        public bool IsFinished = false;
        public float[] Multi = new float[4];
        public float[] RimColor = new float[4];
        public float[] AddColor = new float[4];
    }
}
