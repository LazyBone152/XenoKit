using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType2ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type2 type;

        public short I_00
        {
            get => type.I_00;
            set => SetValue(nameof(type.I_00), type.I_00, value, v => type.I_00 = v, "BSA Projectile Timeline Remap I_00");
        }

        public short OutputStartFrame
        {
            get => type.I_02;
            set => SetValue(nameof(type.I_02), type.I_02, value, v => type.I_02 = v, "BSA Projectile Timeline Remap Output Start Frame");
        }

        public short OutputEndFrame
        {
            get => type.I_04;
            set => SetValue(nameof(type.I_04), type.I_04, value, v => type.I_04 = v, "BSA Projectile Timeline Remap Output End Frame");
        }

        public short I_06
        {
            get => type.I_06;
            set => SetValue(nameof(type.I_06), type.I_06, value, v => type.I_06 = v, "BSA Projectile Timeline Remap I_06");
        }

        public BsaType2ViewModel(BSA_Type2 type) : base(type)
        {
            this.type = type;
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaisePropertyChanged(() => I_00);
            RaisePropertyChanged(() => OutputStartFrame);
            RaisePropertyChanged(() => OutputEndFrame);
            RaisePropertyChanged(() => I_06);
        }
    }
}
