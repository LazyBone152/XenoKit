using GalaSoft.MvvmLight;
using System;
using XenoKit.Engine;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACTypeBaseViewModel : ObservableObject, IDisposable
    {
        private BAC_TypeBase bacType;

        public ushort StartTime
        {
            get
            {
                return bacType.StartTime;
            }
            set
            {
                if (bacType.StartTime != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_TypeBase>(nameof(bacType.StartTime), bacType, bacType.StartTime, value, "BAC Start Time"), UndoGroup.Action, nameof(bacType.StartTime), bacType);
                    bacType.StartTime = value;
                    RaisePropertyChanged(() => StartTime);
                    UpdateBacPlayer();
                    UndoManager.Instance.ForceEventCall(UndoGroup.Action, nameof(bacType.StartTime), bacType);
                }
            }
        }
        public ushort Duration
        {
            get
            {
                return bacType.Duration;
            }
            set
            {
                if (bacType.Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_TypeBase>(nameof(bacType.Duration), bacType, bacType.Duration, value, "BAC Duration"), UndoGroup.Action, nameof(bacType.Duration), bacType);
                    bacType.Duration = value;
                    RaisePropertyChanged(() => Duration);
                    UpdateBacPlayer();
                    UndoManager.Instance.ForceEventCall(UndoGroup.Action, nameof(bacType.Duration), bacType);
                }
            }
        }
        public ushort Flags
        {
            get
            {
                return bacType.Flags;
            }
            set
            {
                if (bacType.Flags != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_TypeBase>(nameof(bacType.Flags), bacType, bacType.Flags, value, "BAC Character Type"), UndoGroup.Action, nameof(bacType.Flags), bacType);
                    bacType.Flags = value;
                    RaisePropertyChanged(() => Flags);
                    UpdateBacPlayer();
                }
            }
        }
        

        public BACTypeBaseViewModel(BAC_TypeBase _bacType)
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
            //RaisePropertyChanged(e.PropertyName);
            RaisePropertyChanged(nameof(StartTime));
            RaisePropertyChanged(nameof(Duration));
            RaisePropertyChanged(nameof(Flags));
        }

        public void Dispose()
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
            bacType.PropertyChanged -= BacType_PropertyChanged;
        }

        private void UpdateProperties()
        {
            //Needed for updating properties when undo/redo is called
            RaisePropertyChanged(() => StartTime);
            RaisePropertyChanged(() => Duration);
            RaisePropertyChanged(() => Flags);
            UpdateBacPlayer();
        }


        private void UpdateBacPlayer()
        {
            SceneManager.InvokeBacDataChangedEvent();
        }
    }
}
