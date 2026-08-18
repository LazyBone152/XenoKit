using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BSA
{
    public class BsaExpirationViewModel : ObservableObject, IDisposable
    {
        private readonly BSA_Expiration expiration;

        public AcbType AcbType { get => expiration.I_00; set => SetValue(nameof(expiration.I_00), expiration.I_00, value, v => expiration.I_00 = v, "BSA Collision Sound ACB Type"); }
        public ushort I_02 { get => expiration.I_02; set => SetValue(nameof(expiration.I_02), expiration.I_02, value, v => expiration.I_02 = v, "BSA Expiration I_02"); }
        public ushort CueId { get => expiration.I_04; set => SetValue(nameof(expiration.I_04), expiration.I_04, value, v => expiration.I_04 = v, "BSA Collision Sound Cue ID"); }
        public ushort I_06 { get => expiration.I_06; set => SetValue(nameof(expiration.I_06), expiration.I_06, value, v => expiration.I_06 = v, "BSA Expiration I_06"); }

        public BsaExpirationViewModel(BSA_Expiration expiration)
        {
            this.expiration = expiration;

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
            RaisePropertyChanged(() => AcbType);
            RaisePropertyChanged(() => I_02);
            RaisePropertyChanged(() => CueId);
            RaisePropertyChanged(() => I_06);
        }

        private void SetValue<TValue>(string modelProperty, TValue oldValue, TValue newValue, Action<TValue> assign, string undoName, [CallerMemberName] string viewModelProperty = null)
        {
            if (EqualityComparer<TValue>.Default.Equals(oldValue, newValue)) return;

            UndoManager.Instance.AddUndo(new UndoableProperty<BSA_Expiration>(modelProperty, expiration, oldValue, newValue, undoName));
            assign(newValue);
            RaisePropertyChanged(viewModelProperty);
        }
    }
}
