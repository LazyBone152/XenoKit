using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType30ViewModel : ObservableObject
    {
        private BAC_Type30 bacType;

        public float F_08
        {
            get => bacType.F_08;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type30>(nameof(bacType.F_08), bacType, bacType.F_08, value, "F_08"));
                bacType.F_08 = value;
                RaisePropertyChanged(() => F_08);
            }
        }
        public int I_12
        {
            get => bacType.I_12;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type30>(nameof(bacType.I_12), bacType, bacType.I_12, value, "I_12"));
                bacType.I_12 = value;
                RaisePropertyChanged(() => I_12);
            }
        }
        public int I_16
        {
            get => bacType.I_16;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type30>(nameof(bacType.I_16), bacType, bacType.I_16, value, "I_16"));
                bacType.I_16 = value;
                RaisePropertyChanged(() => I_16);
            }
        }
        public int I_20
        {
            get => bacType.I_20;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type30>(nameof(bacType.I_20), bacType, bacType.I_20, value, "I_20"));
                bacType.I_20 = value;
                RaisePropertyChanged(() => I_20);
            }
        }

        public int I_24
        {
            get => bacType.I_24;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type30>(nameof(bacType.I_24), bacType, bacType.I_24, value, "I_24"));
                bacType.I_24 = value;
                RaisePropertyChanged(() => I_24);
            }
        }
        public int I_28
        {
            get => bacType.I_28;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type30>(nameof(bacType.I_28), bacType, bacType.I_28, value, "I_28"));
                bacType.I_28 = value;
                RaisePropertyChanged(() => I_28);
            }
        }
        public int I_32
        {
            get => bacType.I_32;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type30>(nameof(bacType.I_32), bacType, bacType.I_32, value, "I_32"));
                bacType.I_32 = value;
                RaisePropertyChanged(() => I_32);
            }
        }
        public int I_36
        {
            get => bacType.I_36;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type30>(nameof(bacType.I_36), bacType, bacType.I_36, value, "I_36"));
                bacType.I_36 = value;
                RaisePropertyChanged(() => I_36);
            }
        }
        public int I_40
        {
            get => bacType.I_40;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type30>(nameof(bacType.I_40), bacType, bacType.I_40, value, "I_40"));
                bacType.I_40 = value;
                RaisePropertyChanged(() => I_40);
            }
        }
        public int I_44
        {
            get => bacType.I_44;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type30>(nameof(bacType.I_44), bacType, bacType.I_44, value, "I_44"));
                bacType.I_44 = value;
                RaisePropertyChanged(() => I_44);
            }
        }

        public BACType30ViewModel(BAC_Type30 _bacType)
        {
            bacType = _bacType;
            bacType.PropertyChanged += BacType_PropertyChanged;

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
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
            RaisePropertyChanged(() => F_08);
            RaisePropertyChanged(() => I_12);
            RaisePropertyChanged(() => I_16);
            RaisePropertyChanged(() => I_20);
            RaisePropertyChanged(() => I_24);
            RaisePropertyChanged(() => I_28);
            RaisePropertyChanged(() => I_32);
            RaisePropertyChanged(() => I_36);
            RaisePropertyChanged(() => I_40);
            RaisePropertyChanged(() => I_44);
        }

    }
}
