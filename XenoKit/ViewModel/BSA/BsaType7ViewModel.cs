using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType7ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type7 sound;

        public AcbType AcbType
        {
            get => sound.AcbType;
            set
            {
                SetValue(nameof(sound.AcbType), sound.AcbType, value, v => sound.AcbType = v, "BSA Sound ACB Type");
                RaisePropertyChanged(() => BacAcbType);
            }
        }

        public ushort CueId
        {
            get => sound.CueId;
            set => SetValue(nameof(sound.CueId), sound.CueId, value, v => sound.CueId = v, "BSA Sound Cue ID");
        }

        public ushort I_02
        {
            get => sound.I_02;
            set => SetValue(nameof(sound.I_02), sound.I_02, value, v => sound.I_02 = v, "BSA Sound I_02");
        }

        public ushort I_06
        {
            get => sound.I_06;
            set => SetValue(nameof(sound.I_06), sound.I_06, value, v => sound.I_06 = v, "BSA Sound I_06");
        }

        /// <summary>
        /// BSA and BAC number AcbType differently, so the value has to be mapped rather than cast.
        /// BSA Skill_SE is 3, but BAC 3 is Character_VOX.
        /// </summary>
        public Xv2CoreLib.BAC.AcbType BacAcbType => GetBacAcbType(sound.AcbType);

        public static Xv2CoreLib.BAC.AcbType GetBacAcbType(AcbType bsaAcbType)
        {
            switch (bsaAcbType)
            {
                case Xv2CoreLib.BSA.AcbType.Common_SE:
                    return Xv2CoreLib.BAC.AcbType.Common_SE;
                case Xv2CoreLib.BSA.AcbType.Chara_SE:
                    return Xv2CoreLib.BAC.AcbType.Character_SE;
                case Xv2CoreLib.BSA.AcbType.Skill_SE:
                    return Xv2CoreLib.BAC.AcbType.Skill_SE;
                default:
                    return Xv2CoreLib.BAC.AcbType.Common_SE;
            }
        }

        public BsaType7ViewModel(BSA_Type7 type) : base(type)
        {
            sound = type;
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaisePropertyChanged(() => AcbType);
            RaisePropertyChanged(() => CueId);
            RaisePropertyChanged(() => I_02);
            RaisePropertyChanged(() => I_06);
            RaisePropertyChanged(() => BacAcbType);
        }
    }
}
