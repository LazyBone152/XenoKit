using GalaSoft.MvvmLight;
using XenoKit.Engine;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType3ViewModel : ObservableObject
    {
        private BAC_Type3 bacType;

        public ushort Type
        {
            get
            {
                return bacType.InvulnerabilityType;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type3>(nameof(bacType.InvulnerabilityType), bacType, bacType.InvulnerabilityType, value, "Invulnerability Type"));
                bacType.InvulnerabilityType = value;
                RaisePropertyChanged(() => Type);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type3>(nameof(bacType.I_10), bacType, bacType.I_10, value, "Invulnerability I_10"));
                bacType.InvulnerabilityType = value;
                RaisePropertyChanged(() => I_10);
            }
        }


        public BACType3ViewModel(BAC_Type3 _bacType)
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
            RaisePropertyChanged(() => Type);
            RaisePropertyChanged(() => I_10);
        }

        private void UpdateBacPlayer()
        {
            SceneManager.InvokeBacValuesChangedEvent();
        }

    }
}
