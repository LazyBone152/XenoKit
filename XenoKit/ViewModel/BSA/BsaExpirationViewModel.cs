using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BSA
{
    public class BsaExpirationViewModel : ObservableObject, IDisposable
    {
        private readonly BSA_Expiration expiration;

        public ushort I_00
        {
            get => expiration.I_00;
            set => SetValue(nameof(expiration.I_00), expiration.I_00, value, "BSA Expiration I_00");
        }

        public ushort I_02
        {
            get => expiration.I_02;
            set => SetValue(nameof(expiration.I_02), expiration.I_02, value, "BSA Expiration I_02");
        }

        public ushort I_04
        {
            get => expiration.I_04;
            set => SetValue(nameof(expiration.I_04), expiration.I_04, value, "BSA Expiration I_04");
        }

        public ushort I_06
        {
            get => expiration.I_06;
            set => SetValue(nameof(expiration.I_06), expiration.I_06, value, "BSA Expiration I_06");
        }

        public BsaExpirationViewModel(BSA_Expiration expiration)
        {
            this.expiration = expiration;
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

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(propertyName, expiration, oldValue, newValue, undoName));
            expiration.GetType().GetProperty(propertyName).SetValue(expiration, newValue, null);
            RaisePropertyChanged(string.Empty);
        }
    }
}
