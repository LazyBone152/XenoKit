using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType29ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type29 bacType;

        public ushort I_08
        {
            get => bacType.I_08;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.I_08), bacType, bacType.I_08, value, "I_08"));
                bacType.I_08 = value;
                RaisePropertyChanged(() => I_08);
            }
        }
        public ushort I_10
        {
            get => bacType.I_10;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.I_10), bacType, bacType.I_10, value, "I_10"));
                bacType.I_10 = value;
                RaisePropertyChanged(() => I_10);
            }
        }
        public int I_12
        {
            get => bacType.I_12;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.I_12), bacType, bacType.I_12, value, "I_12"));
                bacType.I_12 = value;
                RaisePropertyChanged(() => I_12);
            }
        }
        public float F_16
        {
            get => bacType.F_16;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.F_16), bacType, bacType.F_16, value, "F_16"));
                bacType.F_16 = value;
                RaisePropertyChanged(() => F_16);
            }
        }
        public float F_20
        {
            get => bacType.F_20;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.F_20), bacType, bacType.F_20, value, "F_20"));
                bacType.F_20 = value;
                RaisePropertyChanged(() => F_20);
            }
        }
        public float F_24
        {
            get => bacType.F_24;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.F_24), bacType, bacType.F_24, value, "F_24"));
                bacType.F_24 = value;
                RaisePropertyChanged(() => F_24);
            }
        }
        public float F_28
        {
            get => bacType.F_28;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.F_28), bacType, bacType.F_28, value, "F_28"));
                bacType.F_28 = value;
                RaisePropertyChanged(() => F_28);
            }
        }
        public float F_32
        {
            get => bacType.F_32;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.F_32), bacType, bacType.F_32, value, "F_32"));
                bacType.F_32 = value;
                RaisePropertyChanged(() => F_32);
            }
        }
        public float F_36
        {
            get => bacType.F_36;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.F_36), bacType, bacType.F_36, value, "F_36"));
                bacType.F_36 = value;
                RaisePropertyChanged(() => F_36);
            }
        }
        public float F_40
        {
            get => bacType.F_40;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.F_40), bacType, bacType.F_40, value, "F_40"));
                bacType.F_40 = value;
                RaisePropertyChanged(() => F_40);
            }
        }
        public float F_44
        {
            get => bacType.F_44;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.F_44), bacType, bacType.F_44, value, "F_44"));
                bacType.F_44 = value;
                RaisePropertyChanged(() => F_44);
            }
        }
        public int I_48
        {
            get => bacType.I_48;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.I_48), bacType, bacType.I_48, value, "I_48"));
                bacType.I_48 = value;
                RaisePropertyChanged(() => I_48);
            }
        }
        public int I_52
        {
            get => bacType.I_52;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.I_52), bacType, bacType.I_52, value, "I_52"));
                bacType.I_52 = value;
                RaisePropertyChanged(() => I_52);
            }
        }
        public int I_56
        {
            get => bacType.I_56;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.I_56), bacType, bacType.I_56, value, "I_56"));
                bacType.I_56 = value;
                RaisePropertyChanged(() => I_56);
            }
        }


        public BACType29ViewModel(BAC_Type29 _bacType)
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
            RaisePropertyChanged(() => I_12);
            RaisePropertyChanged(() => F_16);
            RaisePropertyChanged(() => F_20);
            RaisePropertyChanged(() => F_24);
            RaisePropertyChanged(() => F_28);
            RaisePropertyChanged(() => F_32);
            RaisePropertyChanged(() => F_36);
            RaisePropertyChanged(() => F_40);
            RaisePropertyChanged(() => F_44);
            RaisePropertyChanged(() => I_48);
            RaisePropertyChanged(() => I_52);
            RaisePropertyChanged(() => I_56);
        }


    }
}
