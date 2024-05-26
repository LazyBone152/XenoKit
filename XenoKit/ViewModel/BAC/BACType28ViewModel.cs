using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType28ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type28 bacType;

        public ushort I_08
        {
            get => bacType.I_08;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type28>(nameof(bacType.I_08), bacType, bacType.I_08, value, "I_08"));
                bacType.I_08 = value;
                RaisePropertyChanged(() => I_08);
            }
        }
        public ushort I_10
        {
            get => bacType.I_10;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type28>(nameof(bacType.I_10), bacType, bacType.I_10, value, "I_10"));
                bacType.I_10 = value;
                RaisePropertyChanged(() => I_10);
            }
        }
        public float F_12
        {
            get => bacType.F_12;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type28>(nameof(bacType.F_12), bacType, bacType.F_12, value, "F_12"));
                bacType.F_12 = value;
                RaisePropertyChanged(() => F_12);
            }
        }
        public float F_16
        {
            get => bacType.F_16;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type28>(nameof(bacType.F_16), bacType, bacType.F_16, value, "F_16"));
                bacType.F_16 = value;
                RaisePropertyChanged(() => F_16);
            }
        }
        public float F_20
        {
            get => bacType.F_20;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type28>(nameof(bacType.F_20), bacType, bacType.F_20, value, "F_20"));
                bacType.F_20 = value;
                RaisePropertyChanged(() => F_20);
            }
        }
        public int I_24
        {
            get => bacType.I_24;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type28>(nameof(bacType.I_24), bacType, bacType.I_24, value, "I_24"));
                bacType.I_24 = value;
                RaisePropertyChanged(() => I_24);
            }
        }
        public int I_28
        {
            get => bacType.I_28;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type28>(nameof(bacType.I_28), bacType, bacType.I_28, value, "I_28"));
                bacType.I_28 = value;
                RaisePropertyChanged(() => I_28);
            }
        }
        public int I_32
        {
            get => bacType.I_32;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type28>(nameof(bacType.I_32), bacType, bacType.I_32, value, "I_32"));
                bacType.I_32 = value;
                RaisePropertyChanged(() => I_32);
            }
        }

        public BACType28ViewModel(BAC_Type28 _bacType)
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

        private void Instance_UndoOrRedoCalled(object sender, EventArgs e)
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
            RaisePropertyChanged(() => I_08);
            RaisePropertyChanged(() => I_10);
            RaisePropertyChanged(() => F_12);
            RaisePropertyChanged(() => F_16);
            RaisePropertyChanged(() => F_20);
            RaisePropertyChanged(() => I_24);
            RaisePropertyChanged(() => I_28);
            RaisePropertyChanged(() => I_32);
        }


    }
}
