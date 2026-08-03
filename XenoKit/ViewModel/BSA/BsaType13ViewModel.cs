using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType13ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type13 type;

        public ProjectileProtectionOperation ProtectionState
        {
            get => type.I_00;
            set => SetValue(nameof(type.I_00), type.I_00, value, v => type.I_00 = v, "BSA Projectile Protection State");
        }

        public ushort I_02
        {
            get => type.I_02;
            set => SetValue(nameof(type.I_02), type.I_02, value, v => type.I_02 = v, "BSA Type13 I_02");
        }

        public float MaxHitboxPower
        {
            get => type.F_04;
            set => SetValue(nameof(type.F_04), type.F_04, value, v => type.F_04 = v, "BSA Projectile Protection Max Hitbox Power");
        }

        public bool ProtectSelectors0To3
        {
            get => type.F_08 != 0f;
            set => SetValue(nameof(type.F_08), type.F_08, value ? 1f : 0f, v => type.F_08 = v, "BSA Projectile Protection Selectors 0-3");
        }

        public float AdditionalSelectorCoverage
        {
            get => type.I_12;
            set => SetValue(nameof(type.I_12), type.I_12, value, v => type.I_12 = v, "BSA Projectile Protection Additional Selectors");
        }

        public float EntryPassingSignalValue
        {
            get => type.F_16;
            set => SetValue(nameof(type.F_16), type.F_16, value, v => type.F_16 = v, "BSA Projectile Protection Entry Passing Signal");
        }

        public bool MarkProtectedHit
        {
            get => type.I_20 != 0f;
            set => SetValue(nameof(type.I_20), type.I_20, value ? 1f : 0f, v => type.I_20 = v, "BSA Projectile Protection Mark Protected Hit");
        }

        public int I_24
        {
            get => type.I_24;
            set => SetValue(nameof(type.I_24), type.I_24, value, v => type.I_24 = v, "BSA Type13 I_24");
        }

        public int I_28
        {
            get => type.I_28;
            set => SetValue(nameof(type.I_28), type.I_28, value, v => type.I_28 = v, "BSA Type13 I_28");
        }

        public BsaType13ViewModel(BSA_Type13 type) : base(type)
        {
            this.type = type;
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaisePropertyChanged(() => ProtectionState);
            RaisePropertyChanged(() => I_02);
            RaisePropertyChanged(() => MaxHitboxPower);
            RaisePropertyChanged(() => ProtectSelectors0To3);
            RaisePropertyChanged(() => AdditionalSelectorCoverage);
            RaisePropertyChanged(() => EntryPassingSignalValue);
            RaisePropertyChanged(() => MarkProtectedHit);
            RaisePropertyChanged(() => I_24);
            RaisePropertyChanged(() => I_28);
        }
    }
}
