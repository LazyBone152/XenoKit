using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType1ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type1 movement;
        private static readonly IReadOnlyCollection<string> TypedFields = new[]
        {
            nameof(BSA_Type1.I_00),
            nameof(BSA_Type1.F_04),
            nameof(BSA_Type1.F_08),
            nameof(BSA_Type1.F_12),
            nameof(BSA_Type1.F_20),
            nameof(BSA_Type1.F_24),
            nameof(BSA_Type1.F_28),
            nameof(BSA_Type1.F_32),
            nameof(BSA_Type1.F_36),
            nameof(BSA_Type1.F_40),
            nameof(BSA_Type1.F_44)
        };

        protected override IReadOnlyCollection<string> TypedFieldNames => TypedFields;

        public ObservableCollection<BsaMotionFlagGroupViewModel> MotionFlagGroups { get; }
        public string MotionFlagsHex => $"0x{unchecked((uint)movement.I_00):X}";

        public int MotionFlags
        {
            get => movement.I_00;
            set
            {
                SetMovementValue(nameof(movement.I_00), movement.I_00, value, "BSA Motion Flags");
                RaisePropertyChanged(nameof(MotionFlagsHex));
                RefreshMotionFlags();
            }
        }

        public float SpeedX
        {
            get => movement.F_08;
            set => SetMovementValue(nameof(movement.F_08), movement.F_08, value, "BSA Speed X");
        }

        public float SpeedY
        {
            get => movement.F_12;
            set => SetMovementValue(nameof(movement.F_12), movement.F_12, value, "BSA Speed Y");
        }

        public float SpeedZ
        {
            get => movement.F_04;
            set => SetMovementValue(nameof(movement.F_04), movement.F_04, value, "BSA Speed Z");
        }

        public float AccelerationX
        {
            get => movement.F_24;
            set => SetMovementValue(nameof(movement.F_24), movement.F_24, value, "BSA Acceleration X");
        }

        public float AccelerationY
        {
            get => movement.F_28;
            set => SetMovementValue(nameof(movement.F_28), movement.F_28, value, "BSA Acceleration Y");
        }

        public float AccelerationZ
        {
            get => movement.F_20;
            set => SetMovementValue(nameof(movement.F_20), movement.F_20, value, "BSA Acceleration Z");
        }

        public float FalloffStrength
        {
            get => movement.F_32;
            set => SetMovementValue(nameof(movement.F_32), movement.F_32, value, "BSA Falloff Strength");
        }

        public float SpreadX
        {
            get => movement.F_36;
            set => SetMovementValue(nameof(movement.F_36), movement.F_36, value, "BSA Spread X");
        }

        public float SpreadY
        {
            get => movement.F_40;
            set => SetMovementValue(nameof(movement.F_40), movement.F_40, value, "BSA Spread Y");
        }

        public float SpreadZ
        {
            get => movement.F_44;
            set => SetMovementValue(nameof(movement.F_44), movement.F_44, value, "BSA Spread Z");
        }

        public BsaType1ViewModel(BSA_Type1 type) : base(type)
        {
            movement = type;
            MotionFlagGroups = CreateMotionFlagGroups();
            UndoManager.Instance.UndoOrRedoCalled += UndoManager_UndoOrRedoCalled;
        }

        public override void Dispose()
        {
            UndoManager.Instance.UndoOrRedoCalled -= UndoManager_UndoOrRedoCalled;
            base.Dispose();
        }

        private void UndoManager_UndoOrRedoCalled(object sender, EventArgs e)
        {
            RaisePropertyChanged(nameof(MotionFlagsHex));
            RefreshMotionFlags();
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

        private void SetMovementValue<T>(string propertyName, T oldValue, T newValue, string undoName)
        {
            if (Equals(oldValue, newValue))
                return;

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(propertyName, movement, oldValue, newValue, undoName));
            movement.GetType().GetProperty(propertyName).SetValue(movement, newValue, null);
            RaisePropertyChanged(string.Empty);
            NotifyTypeChanged();
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

    public class BsaMotionFlagOptionViewModel : GalaSoft.MvvmLight.ObservableObject
    {
        private readonly BsaType1ViewModel owner;
        private readonly int mask;

        public string Name { get; }

        public bool IsChecked
        {
            get => (owner.MotionFlags & mask) == mask;
            set
            {
                int newFlags = value ? owner.MotionFlags | mask : owner.MotionFlags & ~mask;
                owner.MotionFlags = newFlags;
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
