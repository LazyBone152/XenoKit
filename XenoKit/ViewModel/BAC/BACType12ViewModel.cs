using GalaSoft.MvvmLight;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType12ViewModel : ObservableObject
    {
        private BAC_Type12 bacType;
        
        public ushort Axis
        {
            get
            {
                return (ushort)bacType.Axis;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type12>(nameof(bacType.Axis), bacType, bacType.Axis, (TargetingAxis)value, "Targetting Assistance"));
                bacType.Axis = (TargetingAxis)value;
                RaisePropertyChanged(() => Axis);

            }
        }


        public BACType12ViewModel(BAC_Type12 _bacType)
        {
            bacType = _bacType;
            bacType.PropertyChanged += BacType_PropertyChanged;

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
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
            RaisePropertyChanged(() => Axis);
        }
    }
}
