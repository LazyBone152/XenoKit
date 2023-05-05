using GalaSoft.MvvmLight;
using XenoKit.Engine;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type5;

namespace XenoKit.ViewModel.BAC
{
    public class BACType5ViewModel : ObservableObject
    {
        private BAC_Type5 bacType;

        public float Tracking
        {
            get
            {
                return bacType.Tracking;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type5>(nameof(bacType.Tracking), bacType, bacType.Tracking, value, "Tracking"));
                bacType.Tracking = value;
                RaisePropertyChanged(() => Tracking);
            }
        }

        //TrackingFlags
        public bool TrackingFlag_Unk1
        {
            get
            {
                return bacType.TrackingFlags.HasFlag(TrackingFlagsEnum.Unk1);
            }
            set
            {
                TrackingFlagsEnum flags = bacType.TrackingFlags.SetFlag(TrackingFlagsEnum.Unk1, value);

                if (flags != bacType.TrackingFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type5>(nameof(bacType.TrackingFlags), bacType, bacType.TrackingFlags, flags, "TrackingFlag"));
                    bacType.TrackingFlags = flags;
                    RaisePropertyChanged(() => TrackingFlag_Unk1);
                }
            }
        }
        public bool TrackingFlag_Unk9
        {
            get
            {
                return bacType.TrackingFlags.HasFlag(TrackingFlagsEnum.Unk9);
            }
            set
            {
                TrackingFlagsEnum flags = bacType.TrackingFlags.SetFlag(TrackingFlagsEnum.Unk9, value);

                if (flags != bacType.TrackingFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type5>(nameof(bacType.TrackingFlags), bacType, bacType.TrackingFlags, flags, "TrackingFlag"));
                    bacType.TrackingFlags = flags;
                    RaisePropertyChanged(() => TrackingFlag_Unk9);
                }
            }
        }
        public bool TrackingFlag_Unk10
        {
            get
            {
                return bacType.TrackingFlags.HasFlag(TrackingFlagsEnum.Unk10);
            }
            set
            {
                TrackingFlagsEnum flags = bacType.TrackingFlags.SetFlag(TrackingFlagsEnum.Unk10, value);

                if (flags != bacType.TrackingFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type5>(nameof(bacType.TrackingFlags), bacType, bacType.TrackingFlags, flags, "TrackingFlag"));
                    bacType.TrackingFlags = flags;
                    RaisePropertyChanged(() => TrackingFlag_Unk10);
                }
            }
        }
        public bool TrackingFlag_TrackForwardAndBackward
        {
            get
            {
                return bacType.TrackingFlags.HasFlag(TrackingFlagsEnum.TrackForwardAndBackwards);
            }
            set
            {
                TrackingFlagsEnum flags = bacType.TrackingFlags.SetFlag(TrackingFlagsEnum.TrackForwardAndBackwards, value);

                if (flags != bacType.TrackingFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type5>(nameof(bacType.TrackingFlags), bacType, bacType.TrackingFlags, flags, "TrackingFlag"));
                    bacType.TrackingFlags = flags;
                    RaisePropertyChanged(() => TrackingFlag_TrackForwardAndBackward);
                }
            }
        }
        public bool TrackingFlag_Unk12
        {
            get
            {
                return bacType.TrackingFlags.HasFlag(TrackingFlagsEnum.Unk12);
            }
            set
            {
                TrackingFlagsEnum flags = bacType.TrackingFlags.SetFlag(TrackingFlagsEnum.Unk12, value);

                if (flags != bacType.TrackingFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type5>(nameof(bacType.TrackingFlags), bacType, bacType.TrackingFlags, flags, "TrackingFlag"));
                    bacType.TrackingFlags = flags;
                    RaisePropertyChanged(() => TrackingFlag_Unk12);
                }
            }
        }
        public bool TrackingFlag_Unk13
        {
            get
            {
                return bacType.TrackingFlags.HasFlag(TrackingFlagsEnum.Unk13);
            }
            set
            {
                TrackingFlagsEnum flags = bacType.TrackingFlags.SetFlag(TrackingFlagsEnum.Unk13, value);

                if (flags != bacType.TrackingFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type5>(nameof(bacType.TrackingFlags), bacType, bacType.TrackingFlags, flags, "TrackingFlag"));
                    bacType.TrackingFlags = flags;
                    RaisePropertyChanged(() => TrackingFlag_Unk13);
                }
            }
        }
        public bool TrackingFlag_Unk14
        {
            get
            {
                return bacType.TrackingFlags.HasFlag(TrackingFlagsEnum.Unk14);
            }
            set
            {
                TrackingFlagsEnum flags = bacType.TrackingFlags.SetFlag(TrackingFlagsEnum.Unk14, value);

                if (flags != bacType.TrackingFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type5>(nameof(bacType.TrackingFlags), bacType, bacType.TrackingFlags, flags, "TrackingFlag"));
                    bacType.TrackingFlags = flags;
                    RaisePropertyChanged(() => TrackingFlag_Unk14);
                }
            }
        }


        public BACType5ViewModel(BAC_Type5 _bacType)
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
            RaisePropertyChanged(() => Tracking);
            RaisePropertyChanged(() => TrackingFlag_Unk1);
            RaisePropertyChanged(() => TrackingFlag_Unk9);
            RaisePropertyChanged(() => TrackingFlag_Unk10);
            RaisePropertyChanged(() => TrackingFlag_Unk12);
            RaisePropertyChanged(() => TrackingFlag_Unk13);
            RaisePropertyChanged(() => TrackingFlag_Unk14);
            RaisePropertyChanged(() => TrackingFlag_TrackForwardAndBackward);
        }

        private void UpdateBacPlayer()
        {
            SceneManager.InvokeBacDataChangedEvent();
        }

    }
}
