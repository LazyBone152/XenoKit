using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using Xv2CoreLib.EMP_NEW;
using static Xv2CoreLib.EMP_NEW.EMP_TextureSamplerDef;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticleEmissionData : Entity
    {
        public readonly ParticleNode ParticleNode;

        public SamplerInfo[] Samplers { get; private set; }
        public Xv2Texture[] Textures { get; private set; }
        public Xv2ShaderEffect Material { get; private set; }

        public ParticleEmissionData(ParticleNode node, GameBase gameBase) : base(gameBase)
        {
            ParticleNode = node;
            SetSamplers();
            SetMaterial();
        }

        private void SetSamplers()
        {
            Samplers = new SamplerInfo[ParticleNode.EmissionNode.Texture.TextureEntryRef.Count];
            Textures = new Xv2Texture[ParticleNode.EmissionNode.Texture.TextureEntryRef.Count];

            for (int i = 0; i < ParticleNode.EmissionNode.Texture.TextureEntryRef.Count; i++)
            {
                if (i == 2) return;

                Samplers[i].type = SamplerType.Sampler2D;
                Samplers[i].textureSlot = i;
                Samplers[i].samplerSlot = i;

                Samplers[i].state = new SamplerState();

                if (ParticleNode.EmissionNode.Texture.TextureEntryRef[i].TextureRef != null)
                {
                    Samplers[i].state.AddressU = GetTextureAddressMode(ParticleNode.EmissionNode.Texture.TextureEntryRef[i].TextureRef.RepetitionU);
                    Samplers[i].state.AddressV = GetTextureAddressMode(ParticleNode.EmissionNode.Texture.TextureEntryRef[i].TextureRef.RepetitionV);
                    Samplers[i].state.AddressW = TextureAddressMode.Wrap;
                    Samplers[i].state.BorderColor = new Color(1, 1, 1, 1);
                    Samplers[i].state.Filter = GetTextureFilter(ParticleNode.EmissionNode.Texture.TextureEntryRef[i].TextureRef.FilteringMin, ParticleNode.EmissionNode.Texture.TextureEntryRef[i].TextureRef.FilteringMag);
                    Samplers[i].state.MaxAnisotropy = 1;
                    Samplers[i].state.MaxMipLevel = 1;

                    Samplers[i].name = ShaderManager.Instance.GetSamplerName(i);
                    Samplers[i].state.Name = Samplers[i].name;
                    Samplers[i].parameter = ParticleNode.EmissionNode.Texture.TextureEntryRef[i].TextureRef.EmbIndex;

                    if (ParticleNode.EmissionNode.Texture.TextureEntryRef[i].TextureRef != null)
                    {
                        Textures[i] = CompiledObjectManager.GetCompiledObject<Xv2Texture>(ParticleNode.EmissionNode.Texture.TextureEntryRef[i].TextureRef.TextureRef, GameBase);
                    }
                }
            }
        }

        private void SetMaterial()
        {
            var compiledMat = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(ParticleNode.EmissionNode.Texture.MaterialRef, GameBase);

            if (compiledMat == null)
            {
                //No material was found for this Submesh. Use default.
                compiledMat = Xv2ShaderEffect.CreateDefaultMaterial(GameBase);
            }

            Material = compiledMat;
        }

        private TextureFilter GetTextureFilter(TextureFiltering min, TextureFiltering mag)
        {
            //Mip always linear
            if (min == TextureFiltering.Linear && mag == TextureFiltering.Linear)
            {
                return TextureFilter.Linear;
            }
            else if (min == TextureFiltering.Linear && mag == TextureFiltering.Point)
            {
                return TextureFilter.MinLinearMagPointMipLinear;
            }
            else if (min == TextureFiltering.Point && mag == TextureFiltering.Point)
            {
                return TextureFilter.PointMipLinear;
            }
            else if (min == TextureFiltering.Point && mag == TextureFiltering.Linear)
            {
                return TextureFilter.MinPointMagLinearMipLinear;
            }

            return TextureFilter.Linear;
        }

        private TextureAddressMode GetTextureAddressMode(TextureRepitition mode)
        {
            switch (mode)
            {
                case TextureRepitition.Clamp:
                    return TextureAddressMode.Clamp;
                case TextureRepitition.Mirror:
                    return TextureAddressMode.Mirror;
                case TextureRepitition.Wrap:
                default:
                    return TextureAddressMode.Wrap;
            }
        }

    }
}
