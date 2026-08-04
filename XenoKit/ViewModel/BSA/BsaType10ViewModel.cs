using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType10ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type10 type;

        public int SkillID
        {
            get => type.I_00;
            set => SetValue(nameof(type.I_00), type.I_00, value, v => type.I_00 = v, "BSA Type10 Skill ID");
        }

        public ushort I_04
        {
            get => type.I_04;
            set => SetValue(nameof(type.I_04), type.I_04, value, v => type.I_04 = v, "BSA Type10 I_04");
        }

        public ushort I_06
        {
            get => type.I_06;
            set => SetValue(nameof(type.I_06), type.I_06, value, v => type.I_06 = v, "BSA Type10 I_06");
        }

        public BsaType10ViewModel(BSA_Type10 type) : base(type)
        {
            this.type = type;
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaisePropertyChanged(() => SkillID);
            RaisePropertyChanged(() => I_04);
            RaisePropertyChanged(() => I_06);
        }
    }
}
