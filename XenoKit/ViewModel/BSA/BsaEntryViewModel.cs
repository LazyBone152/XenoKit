using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BSA
{
    public class BsaEntryViewModel : ObservableObject, IDisposable
    {
        private readonly BSA_Entry entry;

        public string UserDefinedName
        {
            get => entry.UserDefinedName;
            set => SetValue(nameof(entry.UserDefinedName), entry.UserDefinedName, value, v => entry.UserDefinedName = v, "BSA Name");
        }

        public int I_00 { get => entry.I_00; set => SetValue(nameof(entry.I_00), entry.I_00, value, v => entry.I_00 = v, "BSA I_00"); }
        public byte ImpactA { get => entry.I_16_a; set => SetValue(nameof(entry.I_16_a), entry.I_16_a, value, v => entry.I_16_a = v, "BSA Impact A"); }
        public byte ImpactB { get => entry.I_16_b; set => SetValue(nameof(entry.I_16_b), entry.I_16_b, value, v => entry.I_16_b = v, "BSA Impact B"); }
        public byte I_17 { get => entry.I_17; set => SetValue(nameof(entry.I_17), entry.I_17, value, v => entry.I_17 = v, "BSA I_17"); }
        public int I_18 { get => entry.I_18; set => SetValue(nameof(entry.I_18), entry.I_18, value, v => entry.I_18 = v, "BSA I_18"); }
        public ushort Lifetime { get => entry.I_22; set => SetValue(nameof(entry.I_22), entry.I_22, value, v => entry.I_22 = v, "BSA Lifetime"); }
        public ushort I_24 { get => entry.I_24; set => SetValue(nameof(entry.I_24), entry.I_24, value, v => entry.I_24 = v, "BSA I_24"); }
        public ushort Expires { get => entry.Expires; set => SetValue(nameof(entry.Expires), entry.Expires, value, v => entry.Expires = v, "BSA Expires"); }
        public ushort ImpactProjectile { get => entry.ImpactProjectile; set => SetValue(nameof(entry.ImpactProjectile), entry.ImpactProjectile, value, v => entry.ImpactProjectile = v, "BSA Impact Projectile"); }
        public ushort ImpactEnemy { get => entry.ImpactEnemy; set => SetValue(nameof(entry.ImpactEnemy), entry.ImpactEnemy, value, v => entry.ImpactEnemy = v, "BSA Impact Enemy"); }
        public ushort ImpactGround { get => entry.ImpactGround; set => SetValue(nameof(entry.ImpactGround), entry.ImpactGround, value, v => entry.ImpactGround = v, "BSA Impact Ground"); }

        public int I_40_0 { get => GetI40(0); set => SetI40(0, value); }
        public int I_40_1 { get => GetI40(1); set => SetI40(1, value); }
        public int I_40_2 { get => GetI40(2); set => SetI40(2, value); }

        public BsaEntryViewModel(BSA_Entry entry)
        {
            this.entry = entry;

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += UndoManager_UndoOrRedoCalled;
        }

        public void Dispose()
        {
            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled -= UndoManager_UndoOrRedoCalled;
        }

        private void UndoManager_UndoOrRedoCalled(object sender, EventArgs e)
        {
            UpdateProperties();
        }

        private void UpdateProperties()
        {
            RaisePropertyChanged(() => UserDefinedName);
            RaisePropertyChanged(() => I_00);
            RaisePropertyChanged(() => ImpactA);
            RaisePropertyChanged(() => ImpactB);
            RaisePropertyChanged(() => I_17);
            RaisePropertyChanged(() => I_18);
            RaisePropertyChanged(() => Lifetime);
            RaisePropertyChanged(() => I_24);
            RaisePropertyChanged(() => Expires);
            RaisePropertyChanged(() => ImpactProjectile);
            RaisePropertyChanged(() => ImpactEnemy);
            RaisePropertyChanged(() => ImpactGround);
            RaisePropertyChanged(() => I_40_0);
            RaisePropertyChanged(() => I_40_1);
            RaisePropertyChanged(() => I_40_2);
        }

        private void SetValue<TValue>(string modelProperty, TValue oldValue, TValue newValue, Action<TValue> assign, string undoName, [CallerMemberName] string viewModelProperty = null)
        {
            if (EqualityComparer<TValue>.Default.Equals(oldValue, newValue)) return;

            UndoManager.Instance.AddUndo(new UndoableProperty<BSA_Entry>(modelProperty, entry, oldValue, newValue, undoName));
            assign(newValue);
            RaisePropertyChanged(viewModelProperty);
        }

        private int GetI40(int index)
        {
            InitI40();
            return entry.I_40[index];
        }

        private void SetI40(int index, int value, [CallerMemberName] string viewModelProperty = null)
        {
            InitI40();
            if (entry.I_40[index] == value) return;

            int[] oldValue = (int[])entry.I_40.Clone();
            int[] newValue = (int[])entry.I_40.Clone();
            newValue[index] = value;

            UndoManager.Instance.AddUndo(new UndoableProperty<BSA_Entry>(nameof(entry.I_40), entry, oldValue, newValue, "BSA I_40"));
            entry.I_40 = newValue;
            RaisePropertyChanged(viewModelProperty);
        }

        private void InitI40()
        {
            if (entry.I_40 == null || entry.I_40.Length != 3)
                entry.I_40 = new int[3];
        }
    }
}
