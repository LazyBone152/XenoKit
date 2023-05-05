using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType22ViewModel : ObservableObject
    {
        private BAC_Type22 bacType;

        public ushort I_08
        {
            get
            {
                return bacType.I_08;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type22>(nameof(bacType.I_08), bacType, bacType.I_08, value, "BACType22 I_08"));
                bacType.I_08 = value;
                RaisePropertyChanged(() => I_08);
            }
        }
        public ushort I_10
        {
            get
            {
                return bacType.I_10;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type22>(nameof(bacType.I_10), bacType, bacType.I_10, value, "BACType22 I_10"));
                bacType.I_10 = value;
                RaisePropertyChanged(() => I_10);
            }
        }
        public float F_12
        {
            get
            {
                return bacType.F_12;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type22>(nameof(bacType.F_12), bacType, bacType.F_12, value, "BACType22 F_12"));
                bacType.F_12 = value;
                RaisePropertyChanged(() => F_12);
            }
        }
        public string STR_16
        {
            get
            {
                return bacType.STR_16;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type22>(nameof(bacType.STR_16), bacType, bacType.STR_16, value, "BACType22 STR_16"));
                bacType.STR_16 = value;
                RaisePropertyChanged(() => STR_16);
            }
        }

        public BACType22ViewModel(BAC_Type22 _bacType)
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
            RaisePropertyChanged(() => STR_16);
            RaisePropertyChanged(() => F_12);
            RaisePropertyChanged(() => I_10);
            RaisePropertyChanged(() => I_08);

        }


    }
}
