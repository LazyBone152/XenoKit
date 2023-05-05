using GalaSoft.MvvmLight;
using XenoKit.Engine;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType4ViewModel : ObservableObject
    {
        private BAC_Type4 bacType;

        public float TimeScale
        {
            get
            {
                return bacType.TimeScale;
            }
            set
            {
                if (bacType.TimeScale != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type4>(nameof(bacType.TimeScale), bacType, bacType.TimeScale, value, "BAC TimeScale"));
                    bacType.TimeScale = value;
                    RaisePropertyChanged(() => TimeScale);
                    UpdateBacPlayer();
                }
            }
        }


        public BACType4ViewModel(BAC_Type4 _bacType)
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
            RaisePropertyChanged(() => TimeScale);
            UpdateBacPlayer();
        }


        private void UpdateBacPlayer()
        {
            SceneManager.InvokeBacValuesChangedEvent();
        }
    }
}
