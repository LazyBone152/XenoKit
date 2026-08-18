using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using XenoKit.Engine.Vfx.Shape;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EMP_NEW.Keyframes;
using Xv2CoreLib.ETR;
using Xv2CoreLib.Resource;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx.Asset
{
    public partial class VfxTbind : VfxAsset
    {
        private Xv2ShaderEffect CreateMaterial(ETR_Node node)
        {
            Xv2ShaderEffect material = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(node.MaterialRef);
            return material ?? DefaultShaders.Red;
        }

        private SamplerInfo[] CreateSamplers(ETR_Node node)
        {
            int count = System.Math.Min(2, node.TextureEntryRef.Count);
            SamplerInfo[] samplers = new SamplerInfo[count];

            for (int i = 0; i < count; i++)
            {
                samplers[i].type = SamplerType.Sampler2D;
                samplers[i].textureSlot = i;
                samplers[i].samplerSlot = i;
                samplers[i].name = ShaderManager.GetSamplerName(i);
                samplers[i].state = new SamplerState
                {
                    AddressU = GetTextureAddressMode(node.TextureEntryRef[i].TextureRef?.RepetitionU),
                    AddressV = GetTextureAddressMode(node.TextureEntryRef[i].TextureRef?.RepetitionV),
                    AddressW = TextureAddressMode.Wrap,
                    BorderColor = Color.White,
                    Filter = GetTextureFilter(node.TextureEntryRef[i].TextureRef?.FilteringMin, node.TextureEntryRef[i].TextureRef?.FilteringMag),
                    MaxAnisotropy = 1,
                    MaxMipLevel = 1
                };

                if (node.TextureEntryRef[i].TextureRef != null)
                    samplers[i].parameter = node.TextureEntryRef[i].TextureRef.EmbIndex;
            }

            return samplers;
        }

        private Xv2Texture[] CreateTextures(ETR_Node node)
        {
            int count = System.Math.Min(2, node.TextureEntryRef.Count);
            Xv2Texture[] textures = new Xv2Texture[count];

            for (int i = 0; i < count; i++)
            {
                if (node.TextureEntryRef[i].TextureRef?.TextureRef != null)
                    textures[i] = CompiledObjectManager.GetCompiledObject<Xv2Texture>(node.TextureEntryRef[i].TextureRef.TextureRef);
            }

            return textures;
        }

        private void UpdateTextureScroll(ETR_Node node, TbindNodeState state, float frameDelta)
        {
            for (int slot = 0; slot < 2; slot++)
            {
                EMP_TextureSamplerDef texture = slot < node.TextureEntryRef.Count ? node.TextureEntryRef[slot].TextureRef : null;

                if (texture?.ScrollState == null || texture.ScrollState.ScrollType != EMP_ScrollState.ScrollTypeEnum.Speed)
                {
                    state.TextureScrollActive[slot] = false;
                    continue;
                }

                if (!state.TextureScrollActive[slot])
                    SetInitialTextureScroll(state, slot);

                state.TextureScrollActive[slot] = true;
                state.TextureScroll[slot][0] += texture.ScrollState.ScrollSpeed_U * frameDelta;
                state.TextureScroll[slot][1] += texture.ScrollState.ScrollSpeed_V * frameDelta;
            }
        }

        private static void SetInitialTextureScroll(TbindNodeState state, int slot)
        {
            float[] values = slot == 0
                ? state.Material?.MatParam?.TexScrl0?.Values
                : state.Material?.MatParam?.TexScrl1?.Values;

            for (int i = 0; i < state.TextureScroll[slot].Length; i++)
                state.TextureScroll[slot][i] = values != null && i < values.Length ? values[i] : 0f;
        }

        private void GetGeometryTextureScroll(TbindNodeState state, out float scrollU, out float scrollV, out float stepU, out float stepV)
        {
            if (!state.TextureScrollActive[0])
            {
                scrollU = 0f;
                scrollV = 0f;
                stepU = 1f;
                stepV = 1f;
                return;
            }

            scrollU = state.TextureScroll[0][0];
            scrollV = state.TextureScroll[0][1];
            stepU = state.TextureScroll[0][2] != 0f ? state.TextureScroll[0][2] : 1f;
            stepV = state.TextureScroll[0][3] != 0f ? state.TextureScroll[0][3] : 1f;
        }

        private static TextureFilter GetTextureFilter(EMP_TextureSamplerDef.TextureFiltering? min, EMP_TextureSamplerDef.TextureFiltering? mag)
        {
            if (min == EMP_TextureSamplerDef.TextureFiltering.Linear && mag == EMP_TextureSamplerDef.TextureFiltering.Linear)
                return TextureFilter.Linear;

            if (min == EMP_TextureSamplerDef.TextureFiltering.Linear && mag == EMP_TextureSamplerDef.TextureFiltering.Point)
                return TextureFilter.MinLinearMagPointMipLinear;

            if (min == EMP_TextureSamplerDef.TextureFiltering.Point && mag == EMP_TextureSamplerDef.TextureFiltering.Point)
                return TextureFilter.PointMipLinear;

            if (min == EMP_TextureSamplerDef.TextureFiltering.Point && mag == EMP_TextureSamplerDef.TextureFiltering.Linear)
                return TextureFilter.MinPointMagLinearMipLinear;

            return TextureFilter.Linear;
        }

        private static TextureAddressMode GetTextureAddressMode(EMP_TextureSamplerDef.TextureRepitition? mode)
        {
            switch (mode)
            {
                case EMP_TextureSamplerDef.TextureRepitition.Clamp:
                case EMP_TextureSamplerDef.TextureRepitition.Border:
                    return TextureAddressMode.Clamp;
                case EMP_TextureSamplerDef.TextureRepitition.Mirror:
                    return TextureAddressMode.Mirror;
                case EMP_TextureSamplerDef.TextureRepitition.Wrap:
                default:
                    return TextureAddressMode.Wrap;
            }
        }

        private static Color GetColor(float[] rgb, float alpha)
        {
            if (rgb == null || rgb.Length < 3)
                return new Color(1f, 1f, 1f, alpha);

            return new Color(rgb[0], rgb[1], rgb[2], alpha);
        }

    }
}
