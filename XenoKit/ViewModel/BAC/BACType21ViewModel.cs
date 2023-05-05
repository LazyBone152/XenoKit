using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type21;

namespace XenoKit.ViewModel.BAC
{
    public class BACType21ViewModel : ObservableObject
    {
        private BAC_Type21 bacType;

        public ushort EyeDirectionPrev
        {
            get
            {
                return (ushort)bacType.EyeDirectionPrev;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type21>(nameof(bacType.EyeDirectionPrev), bacType, bacType.EyeDirectionPrev, (EyeDirection)value, "EyeDirectionPrev"));
                bacType.EyeDirectionPrev = (EyeDirection)value;
                RaisePropertyChanged(() => EyeDirectionPrev);
            }
        }
        public ushort EyeDirectionNext
        {
            get
            {
                return (ushort)bacType.EyeDirectionNext;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type21>(nameof(bacType.EyeDirectionNext), bacType, bacType.EyeDirectionNext, (EyeDirection)value, "EyeDirectionNext"));
                bacType.EyeDirectionNext = (EyeDirection)value;
                RaisePropertyChanged(() => EyeDirectionNext);
            }
        }
        public int EyeRotationFrames
        {
            get
            {
                return bacType.EyeRotationFrames;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type21>(nameof(bacType.EyeRotationFrames), bacType, bacType.EyeRotationFrames, value, "EyeRotationDuration"));
                bacType.EyeRotationFrames = value;
                RaisePropertyChanged(() => EyeRotationFrames);
            }
        }
        public int EyeMovementDuration
        {
            get
            {
                return bacType.EyeMovementDuration;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type21>(nameof(bacType.EyeMovementDuration), bacType, bacType.EyeMovementDuration, value, "EyeMovementDuration"));
                bacType.EyeMovementDuration = value;
                RaisePropertyChanged(() => EyeMovementDuration);
            }
        }
        public float RightEyeRotationPercent
        {
            get
            {
                return bacType.RightEyeRotationPercent;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type21>(nameof(bacType.RightEyeRotationPercent), bacType, bacType.RightEyeRotationPercent, value, "RightEyeRotationPercent"));
                bacType.RightEyeRotationPercent = value;
                RaisePropertyChanged(() => RightEyeRotationPercent);
            }
        }
        public float LeftEyeRotationPercent
        {
            get
            {
                return bacType.LeftEyeRotationPercent;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type21>(nameof(bacType.LeftEyeRotationPercent), bacType, bacType.LeftEyeRotationPercent, value, "LeftEyeRotationPercent"));
                bacType.LeftEyeRotationPercent = value;
                RaisePropertyChanged(() => LeftEyeRotationPercent);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type21>(nameof(bacType.I_10), bacType, bacType.I_10, value, "I_10"));
                bacType.I_10 = value;
                RaisePropertyChanged(() => I_10);
            }
        }

        public BACType21ViewModel(BAC_Type21 _bacType)
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
            RaisePropertyChanged(() => EyeDirectionPrev);
            RaisePropertyChanged(() => EyeDirectionNext);
            RaisePropertyChanged(() => EyeRotationFrames);
            RaisePropertyChanged(() => EyeMovementDuration);
            RaisePropertyChanged(() => RightEyeRotationPercent);
            RaisePropertyChanged(() => LeftEyeRotationPercent);
            RaisePropertyChanged(() => I_10);
        }


    }
}
