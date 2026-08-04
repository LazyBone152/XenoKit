using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType22ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type22 bacType;

        public ushort ObjectMode
        {
            get
            {
                return bacType.I_08;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type22>(nameof(bacType.I_08), bacType, bacType.I_08, value, "Move to Object Mode"));
                bacType.I_08 = value;
                RaisePropertyChanged(() => ObjectMode);
            }
        }
        public ushort MovementMode
        {
            get
            {
                return bacType.I_10;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type22>(nameof(bacType.I_10), bacType, bacType.I_10, value, "Move to Object Movement Mode"));
                bacType.I_10 = value;
                RaisePropertyChanged(() => MovementMode);
            }
        }
        public float MovementDistance
        {
            get
            {
                return bacType.F_12;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type22>(nameof(bacType.F_12), bacType, bacType.F_12, value, "Move to Object Distance"));
                bacType.F_12 = value;
                RaisePropertyChanged(() => MovementDistance);
            }
        }
        public string ObjectNameOrPrefix
        {
            get
            {
                return bacType.STR_16;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type22>(nameof(bacType.STR_16), bacType, bacType.STR_16, value, "Move to Object Name or Prefix"));
                bacType.STR_16 = value;
                RaisePropertyChanged(() => ObjectNameOrPrefix);
            }
        }

        public BACType22ViewModel(BAC_Type22 _bacType)
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
            RaisePropertyChanged(() => ObjectNameOrPrefix);
            RaisePropertyChanged(() => MovementDistance);
            RaisePropertyChanged(() => MovementMode);
            RaisePropertyChanged(() => ObjectMode);

        }


    }
}
