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
        private EMP_File empFile = null;
        public EMP_File EmpFile
        {
            get => empFile;
            set
            {
                if(empFile != value)
                {
                    empFile = value;
                    SetTextureIndex();
                }
            }
        }

        public readonly ParticleNode ParticleNode;

        public SamplerInfo[] Samplers { get; private set; }
        public Xv2Texture[] Textures { get; private set; }
        public Xv2ShaderEffect Material { get; private set; }

        public int TextureIndex { get; private set; }

        private bool IsTexturesDirty { get; set; }
        private bool IsMaterialsDirty { get; set; }

        private readonly EMP_TextureSamplerDef[] PreviousTextureDef = new EMP_TextureSamplerDef[2];

        public ParticleEmissionData(ParticleNode node, GameBase gameBase) : base(gameBase)
        {
            ParticleNode = node;
            SetSamplers();
            SetMaterial();

            ParticleNode.EmissionNode.Texture.PropertyChanged += Texture_PropertyChanged;
            ParticleNode.EmissionNode.Texture.TextureEntryRef[0].PropertyChanged += ParticleEmissionData_PropertyChanged;
            ParticleNode.EmissionNode.Texture.TextureEntryRef[1].PropertyChanged += ParticleEmissionData_PropertyChanged;
        }

        private void ParticleEmissionData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            
            IsTexturesDirty = true;
        }

        private void Texture_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ParticleTexture.MaterialRef))
                IsMaterialsDirty = true;
        }

        private void TextureChangedOnNode_Event(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IsTexturesDirty = true;
        }

        public override void Dispose()
        {
            ParticleNode.EmissionNode.Texture.PropertyChanged -= Texture_PropertyChanged;
            ParticleNode.EmissionNode.Texture.TextureEntryRef[0].PropertyChanged -= ParticleEmissionData_PropertyChanged;
            ParticleNode.EmissionNode.Texture.TextureEntryRef[1].PropertyChanged -= ParticleEmissionData_PropertyChanged;

            if (PreviousTextureDef[0] != null)
                PreviousTextureDef[0].PropertyChanged -= TextureChangedOnNode_Event;

            if (PreviousTextureDef[1] != null)
                PreviousTextureDef[1].PropertyChanged -= TextureChangedOnNode_Event;
        }

        private void SetSamplers()
        {
            IsTexturesDirty = false;

            if(PreviousTextureDef[0] != null)
                PreviousTextureDef[0].PropertyChanged -= TextureChangedOnNode_Event;
            
            if(PreviousTextureDef[1] != null)
                PreviousTextureDef[1].PropertyChanged -= TextureChangedOnNode_Event;

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

                    Samplers[i].name = ShaderManager.GetSamplerName(i);
                    Samplers[i].state.Name = Samplers[i].name;
                    Samplers[i].parameter = ParticleNode.EmissionNode.Texture.TextureEntryRef[i].TextureRef.EmbIndex;

                    if (ParticleNode.EmissionNode.Texture.TextureEntryRef[i].TextureRef != null)
                    {
                        Textures[i] = CompiledObjectManager.GetCompiledObject<Xv2Texture>(ParticleNode.EmissionNode.Texture.TextureEntryRef[i].TextureRef.TextureRef, GameBase);
                    }
                }
            }

            if(EmpFile != null)
            {
                SetTextureIndex();
            }

            PreviousTextureDef[0] = ParticleNode.EmissionNode.Texture.TextureEntryRef[0].TextureRef;
            PreviousTextureDef[1] = ParticleNode.EmissionNode.Texture.TextureEntryRef[1].TextureRef;

            if (PreviousTextureDef[0] != null)
                PreviousTextureDef[0].PropertyChanged += TextureChangedOnNode_Event;

            if(PreviousTextureDef[1] != null)
                PreviousTextureDef[1].PropertyChanged += TextureChangedOnNode_Event;
        }

        public void SetTextureIndex()
        {
            TextureIndex = EmpFile.Textures.IndexOf(ParticleNode.EmissionNode.Texture.TextureEntryRef[0].TextureRef);
        }

        private void SetMaterial()
        {
            IsMaterialsDirty = false;

            Xv2ShaderEffect compiledMat = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(ParticleNode.EmissionNode.Texture.MaterialRef, GameBase);

            if (compiledMat == null)
            {
                //No material was found for this Submesh. Use default.
                compiledMat = Xv2ShaderEffect.CreateDefaultMaterial(ShaderType.Default, GameBase);
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

        public override void Update()
        {
            if (IsMaterialsDirty)
                SetMaterial();

            if (IsTexturesDirty)
                SetSamplers();
        }
    }
}
