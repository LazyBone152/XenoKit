using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type18;

namespace XenoKit.ViewModel.BAC
{
    public class BACType18ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type18 bacType;

        public ushort PhysicsFunction
        {
            get
            {
                return (ushort)bacType.Function;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type18>(nameof(bacType.Function), bacType, bacType.Function, (FunctionType)value, "PhysicsFunction"));
                bacType.Function = (FunctionType)value;
                RaisePropertyChanged(() => PhysicsFunction);
                bacType.RefreshType();
            }
        }
        public ushort EanID
        {
            get
            {
                return bacType.EanIndex;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type18>(nameof(bacType.EanIndex), bacType, bacType.EanIndex, value, "PhysicsObjectControl EanID"));
                bacType.EanIndex = value;
                RaisePropertyChanged(() => EanID);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type18>(nameof(bacType.I_10), bacType, bacType.I_10, value, "PhysicsObjectControl I_10"));
                bacType.I_10 = value;
                RaisePropertyChanged(() => I_10);
            }
        }
        public ushort I_14
        {
            get
            {
                return bacType.I_14;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type18>(nameof(bacType.I_14), bacType, bacType.I_14, value, "PhysicsObjectControl I_14"));
                bacType.I_14 = value;
                RaisePropertyChanged(() => I_14);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type18>(nameof(bacType.F_16), bacType, bacType.F_16, value, "PhysicsObjectControl F_16"));
                bacType.F_16 = value;
                RaisePropertyChanged(() => F_16);
            }
        }
        public float F_20
        {
            get
            {
                return bacType.F_20;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type18>(nameof(bacType.F_20), bacType, bacType.F_20, value, "PhysicsObjectControl F_20"));
                bacType.F_20 = value;
                RaisePropertyChanged(() => F_20);
            }
        }
        public float F_24
        {
            get
            {
                return bacType.F_24;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type18>(nameof(bacType.F_24), bacType, bacType.F_24, value, "PhysicsObjectControl F_24"));
                bacType.F_24 = value;
                RaisePropertyChanged(() => F_24);
            }
        }



        public BACType18ViewModel(BAC_Type18 _bacType)
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
            RaisePropertyChanged(() => PhysicsFunction);
            RaisePropertyChanged(() => EanID);
            RaisePropertyChanged(() => I_10);
            RaisePropertyChanged(() => I_14);
            RaisePropertyChanged(() => F_16);
            RaisePropertyChanged(() => F_20);
            RaisePropertyChanged(() => F_24);
        }

    }
}
