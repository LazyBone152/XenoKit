using Xv2CoreLib.BAC;
using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType8ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type8 type;

        public ushort BpeId
        {
            get => type.I_00;
            set => SetValue(nameof(type.I_00), type.I_00, value, v => type.I_00 = v, "BSA Screen Effect BPE ID");
        }

        public ushort ScreenEffectFlags
        {
            get => type.I_02;
            set
            {
                SetValue(nameof(type.I_02), type.I_02, value, v => type.I_02 = v, "BSA Screen Effect Flags");
                RaiseScreenEffectFlags();
            }
        }

        public bool Flag_Unk1 { get => HasFlag(BAC_Type16.ScreenEffectFlagsEnum.Unk1); set => SetFlagValue(BAC_Type16.ScreenEffectFlagsEnum.Unk1, value); }
        public bool Flag_DisableEffect { get => HasFlag(BAC_Type16.ScreenEffectFlagsEnum.DisableEffect); set => SetFlagValue(BAC_Type16.ScreenEffectFlagsEnum.DisableEffect, value); }
        public bool Flag_Unk3 { get => HasFlag(BAC_Type16.ScreenEffectFlagsEnum.Unk3); set => SetFlagValue(BAC_Type16.ScreenEffectFlagsEnum.Unk3, value); }
        public bool Flag_AllowLoop { get => HasFlag(BAC_Type16.ScreenEffectFlagsEnum.AllowLoop); set => SetFlagValue(BAC_Type16.ScreenEffectFlagsEnum.AllowLoop, value); }
        public bool Flag_Unk5 { get => HasFlag(BAC_Type16.ScreenEffectFlagsEnum.Unk5); set => SetFlagValue(BAC_Type16.ScreenEffectFlagsEnum.Unk5, value); }
        public bool Flag_Unk6 { get => HasFlag(BAC_Type16.ScreenEffectFlagsEnum.Unk6); set => SetFlagValue(BAC_Type16.ScreenEffectFlagsEnum.Unk6, value); }
        public bool Flag_Unk7 { get => HasFlag(BAC_Type16.ScreenEffectFlagsEnum.Unk7); set => SetFlagValue(BAC_Type16.ScreenEffectFlagsEnum.Unk7, value); }
        public bool Flag_Unk8 { get => HasFlag(BAC_Type16.ScreenEffectFlagsEnum.Unk8); set => SetFlagValue(BAC_Type16.ScreenEffectFlagsEnum.Unk8, value); }
        public bool Flag_Unk9 { get => HasFlag(BAC_Type16.ScreenEffectFlagsEnum.Unk9); set => SetFlagValue(BAC_Type16.ScreenEffectFlagsEnum.Unk9, value); }
        public bool Flag_Unk10 { get => HasFlag(BAC_Type16.ScreenEffectFlagsEnum.Unk10); set => SetFlagValue(BAC_Type16.ScreenEffectFlagsEnum.Unk10, value); }
        public bool Flag_Unk11 { get => HasFlag(BAC_Type16.ScreenEffectFlagsEnum.Unk11); set => SetFlagValue(BAC_Type16.ScreenEffectFlagsEnum.Unk11, value); }
        public bool Flag_Unk12 { get => HasFlag(BAC_Type16.ScreenEffectFlagsEnum.Unk12); set => SetFlagValue(BAC_Type16.ScreenEffectFlagsEnum.Unk12, value); }
        public bool Flag_Unk13 { get => HasFlag(BAC_Type16.ScreenEffectFlagsEnum.Unk13); set => SetFlagValue(BAC_Type16.ScreenEffectFlagsEnum.Unk13, value); }
        public bool Flag_Unk14 { get => HasFlag(BAC_Type16.ScreenEffectFlagsEnum.Unk14); set => SetFlagValue(BAC_Type16.ScreenEffectFlagsEnum.Unk14, value); }
        public bool Flag_Unk15 { get => HasFlag(BAC_Type16.ScreenEffectFlagsEnum.Unk15); set => SetFlagValue(BAC_Type16.ScreenEffectFlagsEnum.Unk15, value); }
        public bool Flag_Unk16 { get => HasFlag(BAC_Type16.ScreenEffectFlagsEnum.Unk16); set => SetFlagValue(BAC_Type16.ScreenEffectFlagsEnum.Unk16, value); }

        public int I_04
        {
            get => type.I_04;
            set => SetValue(nameof(type.I_04), type.I_04, value, v => type.I_04 = v, "BSA Screen Effect I_04");
        }

        public int I_08
        {
            get => type.I_08;
            set => SetValue(nameof(type.I_08), type.I_08, value, v => type.I_08 = v, "BSA Screen Effect I_08");
        }

        public int I_12
        {
            get => type.I_12;
            set => SetValue(nameof(type.I_12), type.I_12, value, v => type.I_12 = v, "BSA Screen Effect I_12");
        }

        public int I_16
        {
            get => type.I_16;
            set => SetValue(nameof(type.I_16), type.I_16, value, v => type.I_16 = v, "BSA Screen Effect I_16");
        }

        public int I_20
        {
            get => type.I_20;
            set => SetValue(nameof(type.I_20), type.I_20, value, v => type.I_20 = v, "BSA Screen Effect I_20");
        }

        public BsaType8ViewModel(BSA_Type8 type) : base(type)
        {
            this.type = type;
        }

        private bool HasFlag(BAC_Type16.ScreenEffectFlagsEnum flag)
        {
            return (type.I_02 & (ushort)flag) == (ushort)flag;
        }

        private void SetFlagValue(BAC_Type16.ScreenEffectFlagsEnum flag, bool state)
        {
            ushort newFlags = state ? (ushort)(type.I_02 | (ushort)flag) : (ushort)(type.I_02 & ~(ushort)flag);
            if (newFlags == type.I_02) return;

            SetValue(nameof(type.I_02), type.I_02, newFlags, v => type.I_02 = v, "BSA Screen Effect Flags");
            RaiseScreenEffectFlags();
        }

        private void RaiseScreenEffectFlags()
        {
            RaisePropertyChanged(() => ScreenEffectFlags);
            RaisePropertyChanged(() => Flag_Unk1);
            RaisePropertyChanged(() => Flag_DisableEffect);
            RaisePropertyChanged(() => Flag_Unk3);
            RaisePropertyChanged(() => Flag_AllowLoop);
            RaisePropertyChanged(() => Flag_Unk5);
            RaisePropertyChanged(() => Flag_Unk6);
            RaisePropertyChanged(() => Flag_Unk7);
            RaisePropertyChanged(() => Flag_Unk8);
            RaisePropertyChanged(() => Flag_Unk9);
            RaisePropertyChanged(() => Flag_Unk10);
            RaisePropertyChanged(() => Flag_Unk11);
            RaisePropertyChanged(() => Flag_Unk12);
            RaisePropertyChanged(() => Flag_Unk13);
            RaisePropertyChanged(() => Flag_Unk14);
            RaisePropertyChanged(() => Flag_Unk15);
            RaisePropertyChanged(() => Flag_Unk16);
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaisePropertyChanged(() => BpeId);
            RaiseScreenEffectFlags();
            RaisePropertyChanged(() => I_04);
            RaisePropertyChanged(() => I_08);
            RaisePropertyChanged(() => I_12);
            RaisePropertyChanged(() => I_16);
            RaisePropertyChanged(() => I_20);
        }
    }
}
