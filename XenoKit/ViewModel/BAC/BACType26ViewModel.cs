using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type20;

namespace XenoKit.ViewModel.BAC
{
    public class BACType26ViewModel : ObservableObject
    {
        private BAC_Type26 bacType;

        public int I_08
        {
            get
            {
                return bacType.I_08;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type26>(nameof(bacType.I_08), bacType, bacType.I_08, value, "ExtendedCameraControl I_08"));
                bacType.I_08 = value;
                RaisePropertyChanged(() => I_08);
            }
        }
        public int I_12
        {
            get
            {
                return bacType.I_12;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type26>(nameof(bacType.I_12), bacType, bacType.I_12, value, "ExtendedCameraControl I_12"));
                bacType.I_12 = value;
                RaisePropertyChanged(() => I_12);
            }
        }
        public float F_16
        {
            get
            {
                return bacType.F_16;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type26>(nameof(bacType.F_16), bacType, bacType.F_16, value, "ExtendedCameraControl F_16"));
                bacType.F_16 = value;
                RaisePropertyChanged(() => F_16);
            }
        }
        public int I_20
        {
            get
            {
                return bacType.I_20;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type26>(nameof(bacType.I_20), bacType, bacType.I_20, value, "ExtendedCameraControl I_20"));
                bacType.I_20 = value;
                RaisePropertyChanged(() => I_20);
            }
        }
        public int I_24
        {
            get
            {
                return bacType.I_24;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type26>(nameof(bacType.I_24), bacType, bacType.I_24, value, "ExtendedCameraControl I_24"));
                bacType.I_24 = value;
                RaisePropertyChanged(() => I_24);
            }
        }
        public int I_28
        {
            get
            {
                return bacType.I_28;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type26>(nameof(bacType.I_28), bacType, bacType.I_28, value, "ExtendedCameraControl I_28"));
                bacType.I_28 = value;
                RaisePropertyChanged(() => I_28);
            }
        }
        public int I_32
        {
            get
            {
                return bacType.I_32;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type26>(nameof(bacType.I_32), bacType, bacType.I_32, value, "ExtendedCameraControl I_32"));
                bacType.I_32 = value;
                RaisePropertyChanged(() => I_32);
            }
        }
        public int I_36
        {
            get
            {
                return bacType.I_36;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type26>(nameof(bacType.I_36), bacType, bacType.I_36, value, "ExtendedCameraControl I_36"));
                bacType.I_36 = value;
                RaisePropertyChanged(() => I_36);
            }
        }
        public int I_40
        {
            get
            {
                return bacType.I_40;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type26>(nameof(bacType.I_40), bacType, bacType.I_40, value, "ExtendedCameraControl I_40"));
                bacType.I_40 = value;
                RaisePropertyChanged(() => I_40);
            }
        }
        public int I_44
        {
            get
            {
                return bacType.I_44;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type26>(nameof(bacType.I_44), bacType, bacType.I_44, value, "ExtendedCameraControl I_44"));
                bacType.I_44 = value;
                RaisePropertyChanged(() => I_44);
            }
        }
        public int I_48
        {
            get
            {
                return bacType.I_48;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type26>(nameof(bacType.I_48), bacType, bacType.I_48, value, "ExtendedCameraControl I_48"));
                bacType.I_48 = value;
                RaisePropertyChanged(() => I_48);
            }
        }
        public int I_52
        {
            get
            {
                return bacType.I_52;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type26>(nameof(bacType.I_52), bacType, bacType.I_52, value, "ExtendedCameraControl I_52"));
                bacType.I_52 = value;
                RaisePropertyChanged(() => I_52);
            }
        }
        public int I_56
        {
            get
            {
                return bacType.I_56;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type26>(nameof(bacType.I_56), bacType, bacType.I_56, value, "ExtendedCameraControl I_56"));
                bacType.I_56 = value;
                RaisePropertyChanged(() => I_56);
            }
        }
        public int I_60
        {
            get
            {
                return bacType.I_60;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type26>(nameof(bacType.I_60), bacType, bacType.I_60, value, "ExtendedCameraControl I_60"));
                bacType.I_60 = value;
                RaisePropertyChanged(() => I_60);
            }
        }
        public int I_64
        {
            get
            {
                return bacType.I_64;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type26>(nameof(bacType.I_64), bacType, bacType.I_64, value, "ExtendedCameraControl I_64"));
                bacType.I_64 = value;
                RaisePropertyChanged(() => I_64);
            }
        }
        public int I_68
        {
            get
            {
                return bacType.I_68;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type26>(nameof(bacType.I_68), bacType, bacType.I_68, value, "ExtendedCameraControl I_68"));
                bacType.I_68 = value;
                RaisePropertyChanged(() => I_68);
            }
        }
        public int I_72
        {
            get
            {
                return bacType.I_72;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type26>(nameof(bacType.I_72), bacType, bacType.I_72, value, "ExtendedCameraControl I_72"));
                bacType.I_72 = value;
                RaisePropertyChanged(() => I_72);
            }
        }
        public int I_76
        {
            get
            {
                return bacType.I_76;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type26>(nameof(bacType.I_76), bacType, bacType.I_76, value, "ExtendedCameraControl I_76"));
                bacType.I_76 = value;
                RaisePropertyChanged(() => I_76);
            }
        }



        public BACType26ViewModel(BAC_Type26 _bacType)
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
            RaisePropertyChanged(() => I_08);
            RaisePropertyChanged(() => I_12);
            RaisePropertyChanged(() => F_16);
            RaisePropertyChanged(() => I_20);
            RaisePropertyChanged(() => I_24);
            RaisePropertyChanged(() => I_28);
            RaisePropertyChanged(() => I_32);
            RaisePropertyChanged(() => I_36);
            RaisePropertyChanged(() => I_40);
            RaisePropertyChanged(() => I_44);
            RaisePropertyChanged(() => I_48);
            RaisePropertyChanged(() => I_52);
            RaisePropertyChanged(() => I_56);
            RaisePropertyChanged(() => I_60);
            RaisePropertyChanged(() => I_64);
            RaisePropertyChanged(() => I_68);
            RaisePropertyChanged(() => I_72);
            RaisePropertyChanged(() => I_76);
        }


    }
}
