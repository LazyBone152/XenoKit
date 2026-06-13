using GalaSoft.MvvmLight;
using System;
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
            set => SetValue(nameof(entry.UserDefinedName), entry.UserDefinedName, value, "BSA Name");
        }

        public int SortID
        {
            get => entry.SortID;
            set => SetValue(nameof(entry.SortID), entry.SortID, value, "BSA ID");
        }

        public int I_00
        {
            get => entry.I_00;
            set => SetValue(nameof(entry.I_00), entry.I_00, value, "BSA I_00");
        }

        public byte I_16_a
        {
            get => entry.I_16_a;
            set => SetValue(nameof(entry.I_16_a), entry.I_16_a, value, "BSA Impact A");
        }

        public byte I_16_b
        {
            get => entry.I_16_b;
            set => SetValue(nameof(entry.I_16_b), entry.I_16_b, value, "BSA Impact B");
        }

        public byte I_17
        {
            get => entry.I_17;
            set => SetValue(nameof(entry.I_17), entry.I_17, value, "BSA I_17");
        }

        public int I_18
        {
            get => entry.I_18;
            set => SetValue(nameof(entry.I_18), entry.I_18, value, "BSA I_18");
        }

        public ushort Lifetime
        {
            get => entry.I_22;
            set => SetValue(nameof(entry.I_22), entry.I_22, value, "BSA Lifetime");
        }

        public ushort I_24
        {
            get => entry.I_24;
            set => SetValue(nameof(entry.I_24), entry.I_24, value, "BSA I_24");
        }

        public ushort Expires
        {
            get => entry.Expires;
            set => SetValue(nameof(entry.Expires), entry.Expires, value, "BSA Expires");
        }

        public ushort ImpactProjectile
        {
            get => entry.ImpactProjectile;
            set => SetValue(nameof(entry.ImpactProjectile), entry.ImpactProjectile, value, "BSA Impact Projectile");
        }

        public ushort ImpactEnemy
        {
            get => entry.ImpactEnemy;
            set => SetValue(nameof(entry.ImpactEnemy), entry.ImpactEnemy, value, "BSA Impact Enemy");
        }

        public ushort ImpactGround
        {
            get => entry.ImpactGround;
            set => SetValue(nameof(entry.ImpactGround), entry.ImpactGround, value, "BSA Impact Ground");
        }

        public int I_40_0
        {
            get => GetI40(0);
            set => SetI40(0, value);
        }

        public int I_40_1
        {
            get => GetI40(1);
            set => SetI40(1, value);
        }

        public int I_40_2
        {
            get => GetI40(2);
            set => SetI40(2, value);
        }

        public BsaEntryViewModel(BSA_Entry entry)
        {
            this.entry = entry;
            UndoManager.Instance.UndoOrRedoCalled += UndoManager_UndoOrRedoCalled;
        }

        public void Dispose()
        {
            UndoManager.Instance.UndoOrRedoCalled -= UndoManager_UndoOrRedoCalled;
        }

        private void UndoManager_UndoOrRedoCalled(object sender, EventArgs e)
        {
            RaisePropertyChanged(string.Empty);
        }

        private void SetValue<T>(string propertyName, T oldValue, T newValue, string undoName)
        {
            if (Equals(oldValue, newValue)) return;

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(propertyName, entry, oldValue, newValue, undoName));
            entry.GetType().GetProperty(propertyName).SetValue(entry, newValue, null);
            RaisePropertyChanged(string.Empty);
        }

        private int GetI40(int index)
        {
            EnsureI40();
            return entry.I_40[index];
        }

        private void SetI40(int index, int value)
        {
            EnsureI40();
            if (entry.I_40[index] == value) return;

            int[] oldValue = (int[])entry.I_40.Clone();
            int[] newValue = (int[])entry.I_40.Clone();
            newValue[index] = value;

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(entry.I_40), entry, oldValue, newValue, "BSA I_40"));
            entry.I_40 = newValue;
            RaisePropertyChanged(string.Empty);
        }

        private void EnsureI40()
        {
            if (entry.I_40 == null || entry.I_40.Length != 3)
                entry.I_40 = new int[3];
        }
    }
}
