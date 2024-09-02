using Xv2CoreLib.EMM;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.Resource;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticleEmissionData : EmissionDataBase
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

        protected override AsyncObservableCollection<TextureEntry_Ref> TextureEntryRef => ParticleNode.EmissionNode.Texture.TextureEntryRef;
        protected override EmmMaterial MaterialRef => ParticleNode.EmissionNode.Texture.MaterialRef;
        protected override object ContextFile => EmpFile;

        public ParticleEmissionData(ParticleNode node, GameBase gameBase) : base(gameBase)
        {
            gameBase = gameBase;
            ParticleNode = node;

            SetSamplers();
            SetMaterial();

            ParticleNode.EmissionNode.Texture.PropertyChanged += Texture_PropertyChanged;
            ParticleNode.EmissionNode.Texture.TextureEntryRef[0].PropertyChanged += ParticleEmissionData_PropertyChanged;
            ParticleNode.EmissionNode.Texture.TextureEntryRef[1].PropertyChanged += ParticleEmissionData_PropertyChanged;
        }

        public override void Dispose()
        {
            ParticleNode.EmissionNode.Texture.PropertyChanged -= Texture_PropertyChanged;
            ParticleNode.EmissionNode.Texture.TextureEntryRef[0].PropertyChanged -= ParticleEmissionData_PropertyChanged;
            ParticleNode.EmissionNode.Texture.TextureEntryRef[1].PropertyChanged -= ParticleEmissionData_PropertyChanged;

            base.Dispose();
        }

        public override void SetTextureIndex()
        {
            TextureIndex = EmpFile.Textures.IndexOf(ParticleNode.EmissionNode.Texture.TextureEntryRef[0].TextureRef);
        }

    }
}
