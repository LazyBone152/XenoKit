using GalaSoft.MvvmLight;
using System.Collections.ObjectModel;
using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType1ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type1 movement;

        public ObservableCollection<BsaMotionFlagGroupViewModel> MotionFlagGroups { get; }
        public string MotionFlagsHex => $"0x{unchecked((uint)movement.I_00):X}";

        public int MotionFlags
        {
            get => movement.I_00;
            set
            {
                SetValue(nameof(movement.I_00), movement.I_00, value, v => movement.I_00 = v, "BSA Motion Flags");
                RaisePropertyChanged(() => MotionFlagsHex);
                RefreshMotionFlags();
            }
        }

        public float SpeedX { get => movement.F_08; set => SetValue(nameof(movement.F_08), movement.F_08, value, v => movement.F_08 = v, "BSA Speed X"); }
        public float SpeedY { get => movement.F_12; set => SetValue(nameof(movement.F_12), movement.F_12, value, v => movement.F_12 = v, "BSA Speed Y"); }
        public float SpeedZ { get => movement.F_04; set => SetValue(nameof(movement.F_04), movement.F_04, value, v => movement.F_04 = v, "BSA Speed Z"); }
        public float AccelerationX { get => movement.F_24; set => SetValue(nameof(movement.F_24), movement.F_24, value, v => movement.F_24 = v, "BSA Acceleration X"); }
        public float AccelerationY { get => movement.F_28; set => SetValue(nameof(movement.F_28), movement.F_28, value, v => movement.F_28 = v, "BSA Acceleration Y"); }
        public float AccelerationZ { get => movement.F_20; set => SetValue(nameof(movement.F_20), movement.F_20, value, v => movement.F_20 = v, "BSA Acceleration Z"); }
        public float FalloffStrength { get => movement.F_32; set => SetValue(nameof(movement.F_32), movement.F_32, value, v => movement.F_32 = v, "BSA Falloff Strength"); }
        public float SpreadX { get => movement.F_36; set => SetValue(nameof(movement.F_36), movement.F_36, value, v => movement.F_36 = v, "BSA Spread X"); }
        public float SpreadY { get => movement.F_40; set => SetValue(nameof(movement.F_40), movement.F_40, value, v => movement.F_40 = v, "BSA Spread Y"); }
        public float SpreadZ { get => movement.F_44; set => SetValue(nameof(movement.F_44), movement.F_44, value, v => movement.F_44 = v, "BSA Spread Z"); }
        public float F_16 { get => movement.F_16; set => SetValue(nameof(movement.F_16), movement.F_16, value, v => movement.F_16 = v, "BSA Movement F_16"); }

        public BsaType1ViewModel(BSA_Type1 type) : base(type)
        {
            movement = type;
            MotionFlagGroups = CreateMotionFlagGroups();
        }

        private ObservableCollection<BsaMotionFlagGroupViewModel> CreateMotionFlagGroups()
        {
            ObservableCollection<BsaMotionFlagGroupViewModel> groups = new ObservableCollection<BsaMotionFlagGroupViewModel>();

            for (int group = 0; group < 8; group++)
            {
                ObservableCollection<BsaMotionFlagOptionViewModel> options = new ObservableCollection<BsaMotionFlagOptionViewModel>();

                for (int bit = 0; bit < 4; bit++)
                {
                    int bitValue = 1 << bit;
                    int mask = unchecked(bitValue << (group * 4));
                    string name = GetMotionFlagName(group, bitValue);
                    options.Add(new BsaMotionFlagOptionViewModel(this, name, mask));
                }

                groups.Add(new BsaMotionFlagGroupViewModel($"Options #{group + 1}", options));
            }

            return groups;
        }

        private static string GetMotionFlagName(int group, int bitValue)
        {
            if (group == 4 && bitValue == 0x2)
                return "Opponent Tracking";

            if (group == 5 && bitValue == 0x2)
                return "Free Movement";

            return $"Unknown (0x{bitValue:X})";
        }

        private void RefreshMotionFlags()
        {
            foreach (BsaMotionFlagGroupViewModel group in MotionFlagGroups)
            {
                foreach (BsaMotionFlagOptionViewModel option in group.Options)
                    option.Refresh();
            }
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaisePropertyChanged(() => MotionFlags);
            RaisePropertyChanged(() => MotionFlagsHex);
            RaisePropertyChanged(() => SpeedX);
            RaisePropertyChanged(() => SpeedY);
            RaisePropertyChanged(() => SpeedZ);
            RaisePropertyChanged(() => AccelerationX);
            RaisePropertyChanged(() => AccelerationY);
            RaisePropertyChanged(() => AccelerationZ);
            RaisePropertyChanged(() => FalloffStrength);
            RaisePropertyChanged(() => SpreadX);
            RaisePropertyChanged(() => SpreadY);
            RaisePropertyChanged(() => SpreadZ);
            RaisePropertyChanged(() => F_16);
            RefreshMotionFlags();
        }
    }

    public class BsaMotionFlagGroupViewModel
    {
        public string Name { get; }
        public ObservableCollection<BsaMotionFlagOptionViewModel> Options { get; }

        public BsaMotionFlagGroupViewModel(string name, ObservableCollection<BsaMotionFlagOptionViewModel> options)
        {
            Name = name;
            Options = options;
        }
    }

    public class BsaMotionFlagOptionViewModel : ObservableObject
    {
        private readonly BsaType1ViewModel owner;
        private readonly int mask;

        public string Name { get; }

        public bool IsChecked
        {
            get => (owner.MotionFlags & mask) == mask;
            set
            {
                owner.MotionFlags = value ? owner.MotionFlags | mask : owner.MotionFlags & ~mask;
                RaisePropertyChanged(nameof(IsChecked));
            }
        }

        public BsaMotionFlagOptionViewModel(BsaType1ViewModel owner, string name, int mask)
        {
            this.owner = owner;
            this.mask = mask;
            Name = name;
        }

        public void Refresh()
        {
            RaisePropertyChanged(nameof(IsChecked));
        }
    }
}
