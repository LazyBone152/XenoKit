using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.ComponentModel;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.Resource;
using static Xv2CoreLib.EMP_NEW.EMP_TextureSamplerDef;

namespace XenoKit.Engine.Vfx
{
    public abstract class EmissionDataBase : Entity
    {
        public SamplerInfo[] Samplers { get; protected set; }
        public Xv2Texture[] Textures { get; protected set; }
        public Xv2ShaderEffect Material { get; protected set; }

        public int TextureIndex { get; protected set; }

        protected bool IsTexturesDirty { get; set; }
        protected bool IsMaterialsDirty { get; set; }

        protected readonly EMP_TextureSamplerDef[] PreviousTextureDef = new EMP_TextureSamplerDef[2];

        protected virtual AsyncObservableCollection<TextureEntry_Ref> TextureEntryRef => null;
        protected virtual EmmMaterial MaterialRef => null;
        protected virtual object ContextFile => null; //EMP_File, ETR_File

        public EmissionDataBase(GameBase gameBase) : base(gameBase)
        {

        }

        public override void Dispose()
        {
            if (PreviousTextureDef[0] != null)
                PreviousTextureDef[0].PropertyChanged -= TextureChangedOnNode_Event;

            if (PreviousTextureDef[1] != null)
                PreviousTextureDef[1].PropertyChanged -= TextureChangedOnNode_Event;
        }

        protected void ParticleEmissionData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IsTexturesDirty = true;
        }

        protected void Texture_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ParticleTexture.MaterialRef))
                IsMaterialsDirty = true;
        }

        protected void TextureChangedOnNode_Event(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IsTexturesDirty = true;
        }

        protected void SetSamplers()
        {
            IsTexturesDirty = false;

            if (PreviousTextureDef[0] != null)
                PreviousTextureDef[0].PropertyChanged -= TextureChangedOnNode_Event;

            if (PreviousTextureDef[1] != null)
                PreviousTextureDef[1].PropertyChanged -= TextureChangedOnNode_Event;

            Samplers = new SamplerInfo[TextureEntryRef.Count];
            Textures = new Xv2Texture[TextureEntryRef.Count];

            for (int i = 0; i < TextureEntryRef.Count; i++)
            {
                if (i == 2) return;

                Samplers[i].type = SamplerType.Sampler2D;
                Samplers[i].textureSlot = i;
                Samplers[i].samplerSlot = i;

                Samplers[i].state = new SamplerState();

                if (TextureEntryRef[i].TextureRef != null)
                {
                    Samplers[i].state.AddressU = GetTextureAddressMode(TextureEntryRef[i].TextureRef.RepetitionU);
                    Samplers[i].state.AddressV = GetTextureAddressMode(TextureEntryRef[i].TextureRef.RepetitionV);
                    Samplers[i].state.AddressW = TextureAddressMode.Wrap;
                    Samplers[i].state.BorderColor = new Color(1, 1, 1, 1);
                    Samplers[i].state.Filter = GetTextureFilter(TextureEntryRef[i].TextureRef.FilteringMin, TextureEntryRef[i].TextureRef.FilteringMag);
                    Samplers[i].state.MaxAnisotropy = 1;
                    Samplers[i].state.MaxMipLevel = 1;

                    Samplers[i].name = ShaderManager.GetSamplerName(i);
                    Samplers[i].state.Name = Samplers[i].name;
                    Samplers[i].parameter = TextureEntryRef[i].TextureRef.EmbIndex;

                    if (TextureEntryRef[i].TextureRef != null)
                    {
                        Textures[i] = CompiledObjectManager.GetCompiledObject<Xv2Texture>(TextureEntryRef[i].TextureRef.TextureRef, GameBase);
                    }
                }
            }

            if (ContextFile != null)
            {
                SetTextureIndex();
            }

            PreviousTextureDef[0] = TextureEntryRef[0].TextureRef;
            PreviousTextureDef[1] = TextureEntryRef[1].TextureRef;

            if (PreviousTextureDef[0] != null)
                PreviousTextureDef[0].PropertyChanged += TextureChangedOnNode_Event;

            if (PreviousTextureDef[1] != null)
                PreviousTextureDef[1].PropertyChanged += TextureChangedOnNode_Event;
        }

        public abstract void SetTextureIndex();

        protected void SetMaterial()
        {
            IsMaterialsDirty = false;

            Xv2ShaderEffect compiledMat = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(MaterialRef, GameBase);

            if (compiledMat == null)
            {
                //No material was found for this Submesh. Use default.
                compiledMat = Xv2ShaderEffect.CreateDefaultMaterial(ShaderType.Default, GameBase);
            }

            Material = compiledMat;
        }

        protected TextureFilter GetTextureFilter(TextureFiltering min, TextureFiltering mag)
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

        protected TextureAddressMode GetTextureAddressMode(TextureRepitition mode)
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
