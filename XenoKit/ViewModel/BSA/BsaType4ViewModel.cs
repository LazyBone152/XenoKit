using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    // The type is named Deflection in the format, but none of its individual fields are identified yet.
    public class BsaType4ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type4 type;

        public int I_00
        {
            get => type.I_00;
            set => SetValue(nameof(type.I_00), type.I_00, value, v => type.I_00 = v, "BSA Deflection I_00");
        }

        public int I_04
        {
            get => type.I_04;
            set => SetValue(nameof(type.I_04), type.I_04, value, v => type.I_04 = v, "BSA Deflection I_04");
        }

        public int I_08
        {
            get => type.I_08;
            set => SetValue(nameof(type.I_08), type.I_08, value, v => type.I_08 = v, "BSA Deflection I_08");
        }

        public float F_12
        {
            get => type.F_12;
            set => SetValue(nameof(type.F_12), type.F_12, value, v => type.F_12 = v, "BSA Deflection F_12");
        }

        public float F_16
        {
            get => type.F_16;
            set => SetValue(nameof(type.F_16), type.F_16, value, v => type.F_16 = v, "BSA Deflection F_16");
        }

        public float F_20
        {
            get => type.F_20;
            set => SetValue(nameof(type.F_20), type.F_20, value, v => type.F_20 = v, "BSA Deflection F_20");
        }

        public int I_24
        {
            get => type.I_24;
            set => SetValue(nameof(type.I_24), type.I_24, value, v => type.I_24 = v, "BSA Deflection I_24");
        }

        public int I_28
        {
            get => type.I_28;
            set => SetValue(nameof(type.I_28), type.I_28, value, v => type.I_28 = v, "BSA Deflection I_28");
        }

        public int I_32
        {
            get => type.I_32;
            set => SetValue(nameof(type.I_32), type.I_32, value, v => type.I_32 = v, "BSA Deflection I_32");
        }

        public int I_36
        {
            get => type.I_36;
            set => SetValue(nameof(type.I_36), type.I_36, value, v => type.I_36 = v, "BSA Deflection I_36");
        }

        public int I_40
        {
            get => type.I_40;
            set => SetValue(nameof(type.I_40), type.I_40, value, v => type.I_40 = v, "BSA Deflection I_40");
        }

        public int I_44
        {
            get => type.I_44;
            set => SetValue(nameof(type.I_44), type.I_44, value, v => type.I_44 = v, "BSA Deflection I_44");
        }

        public ushort I_48
        {
            get => type.I_48;
            set => SetValue(nameof(type.I_48), type.I_48, value, v => type.I_48 = v, "BSA Deflection I_48");
        }

        public ushort I_50
        {
            get => type.I_50;
            set => SetValue(nameof(type.I_50), type.I_50, value, v => type.I_50 = v, "BSA Deflection I_50");
        }

        public ushort I_52
        {
            get => type.I_52;
            set => SetValue(nameof(type.I_52), type.I_52, value, v => type.I_52 = v, "BSA Deflection I_52");
        }

        public ushort I_54
        {
            get => type.I_54;
            set => SetValue(nameof(type.I_54), type.I_54, value, v => type.I_54 = v, "BSA Deflection I_54");
        }

        public BsaType4ViewModel(BSA_Type4 type) : base(type)
        {
            this.type = type;
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaisePropertyChanged(() => I_00);
            RaisePropertyChanged(() => I_04);
            RaisePropertyChanged(() => I_08);
            RaisePropertyChanged(() => F_12);
            RaisePropertyChanged(() => F_16);
            RaisePropertyChanged(() => F_20);
            RaisePropertyChanged(() => I_24);
            RaisePropertyChanged(() => I_28);
            RaisePropertyChanged(() => I_32);
            RaisePropertyChanged(() => I_36);
            RaisePropertyChanged(() => I_40);
            RaisePropertyChanged(() => I_44);
            RaisePropertyChanged(() => I_48);
            RaisePropertyChanged(() => I_50);
            RaisePropertyChanged(() => I_52);
            RaisePropertyChanged(() => I_54);
        }
    }
}
