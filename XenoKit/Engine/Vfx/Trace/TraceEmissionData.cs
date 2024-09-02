using Xv2CoreLib.ETR;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EMM;
using Xv2CoreLib.Resource;

namespace XenoKit.Engine.Vfx.Trace
{
    public class TraceEmissionData : EmissionDataBase
    {
        private ETR_File etrFile = null;
        public ETR_File EtrFile
        {
            get => etrFile;
            set
            {
                if (etrFile != value)
                {
                    etrFile = value;
                    SetTextureIndex();
                }
            }
        }

        public readonly ETR_Node EtrNode;

        protected override AsyncObservableCollection<TextureEntry_Ref> TextureEntryRef => EtrNode.TextureEntryRef;
        protected override EmmMaterial MaterialRef => EtrNode.MaterialRef;
        protected override object ContextFile => EtrFile;

        public TraceEmissionData(ETR_Node node, GameBase gameBase) : base(gameBase)
        {
            GameBase = gameBase;
            EtrNode = node;

            SetSamplers();
            SetMaterial();

            EtrNode.PropertyChanged += Texture_PropertyChanged;
            EtrNode.TextureEntryRef[0].PropertyChanged += ParticleEmissionData_PropertyChanged;
            EtrNode.TextureEntryRef[1].PropertyChanged += ParticleEmissionData_PropertyChanged;
        }

        public override void Dispose()
        {
            EtrNode.PropertyChanged -= Texture_PropertyChanged;
            EtrNode.TextureEntryRef[0].PropertyChanged -= ParticleEmissionData_PropertyChanged;
            EtrNode.TextureEntryRef[1].PropertyChanged -= ParticleEmissionData_PropertyChanged;

            base.Dispose();
        }

        public override void SetTextureIndex()
        {
            TextureIndex = EtrFile.Textures.IndexOf(EtrNode.TextureEntryRef[0].TextureRef);
        }
    }
}
