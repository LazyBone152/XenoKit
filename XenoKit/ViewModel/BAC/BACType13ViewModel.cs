using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType13ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type13 bacType;

        public ushort Part
        {
            get
            {
                return (ushort)bacType.Part;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type13>(nameof(bacType.Part), bacType, bacType.Part, (BcsPartId)value, "BcsPartVisibility"));
                bacType.Part = (BcsPartId)value;
                RaisePropertyChanged(() => Part);
                bacType.RefreshType();
            }
        }
        public bool Hidden
        {
            get
            {
                return bacType.Visibility == BcsPartVisibilitySwitch.Off;
            }
            set
            {
                BcsPartVisibilitySwitch visiblity = value ? BcsPartVisibilitySwitch.Off : BcsPartVisibilitySwitch.On;

                if(bacType.Visibility != visiblity)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type13>(nameof(bacType.Visibility), bacType, bacType.Visibility, visiblity, "BcsPartVisibility"));
                    bacType.Visibility = visiblity;
                    RaisePropertyChanged(() => Hidden);
                    bacType.RefreshType();
                }
            }
        }


        public BACType13ViewModel(BAC_Type13 _bacType)
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
            RaisePropertyChanged(() => Part);
            RaisePropertyChanged(() => Hidden);
        }
    }
}
