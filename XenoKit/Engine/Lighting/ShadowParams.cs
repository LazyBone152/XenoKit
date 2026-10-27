using System.Numerics;
using Xv2CoreLib.SPM;

namespace XenoKit.Engine.Lighting
{
    public sealed class ShadowParams
    {
        public Vector4 ShadowColor = new Vector4(0.5f, 0.5f, 0.5f, 0f); //g_vShadowColor_PS
        public float ShadowFarMax = 35f;
        public float ShadowFarMin = 2f;
        public float ShadowFarExponent = 1f;
        public float FadeStartRatio = 0.9f;
        public float DepthBias = 0.01f; //g_vShadowMap_PS 3rd parameter
        public float ShadowParam48 = 1f; //g_vShadowMap_PS 4th parameter

        public void SetFromSpm(SPM_Entry spm)
        {
            if (spm is null) return;

            ShadowColor = new Vector4(spm.ShadowColorR, spm.ShadowColorG, spm.ShadowColorB, spm.ShadowColorA);
            ShadowFarMax = spm.ShadowFarMax;
            FadeStartRatio = spm.ShadowFadeStartRatio;
            DepthBias = spm.ShadowDepthBias;
            ShadowParam48 = spm.ShadowParam48;

            if(spm.ExtendedData != null)
            {
                ShadowFarMin = spm.ExtendedData.ShadowFarMin;
                ShadowFarExponent = spm.ExtendedData.ShadowFarExponent;
            }
            else
            {
                ShadowFarMin = 2;
                ShadowFarExponent = 1;
            }
        }
    }

}
