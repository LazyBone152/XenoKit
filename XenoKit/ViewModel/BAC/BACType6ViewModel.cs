using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType6ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type6 bacType;

        public ushort ChargeTime
        {
            get
            {
                return bacType.ChargeTime;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type6>(nameof(bacType.ChargeTime), bacType, bacType.ChargeTime, value, "ChargeTime"));
                bacType.ChargeTime = value;
                RaisePropertyChanged(() => ChargeTime);
            }
        }


        public BACType6ViewModel(BAC_Type6 _bacType)
        {
            bacType = _bacType;
            bacType.PropertyChanged += BacType_PropertyChanged;

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        public void Dispose()
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
            bacType.PropertyChanged -= BacType_PropertyChanged;
        }

        private void Instance_UndoOrRedoCalled(object sender, System.EventArgs e)
        {
            UpdateProperties();
        }

        private void BacType_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(e.PropertyName);
        }

        private void UpdateProperties()
        {
            //Needed for updating properties when undo/redo is called
            RaisePropertyChanged(() => ChargeTime);
        }
    }
}
