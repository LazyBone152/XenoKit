using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type20;

namespace XenoKit.ViewModel.BAC
{
    public class BACType20ViewModel : ObservableObject
    {
        private BAC_Type20 bacType;

        public ushort HomingType
        {
            get
            {
                return (ushort)bacType.HomingMovementType;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type20>(nameof(bacType.HomingMovementType), bacType, bacType.HomingMovementType, (HomingType)value, "HomingType"));
                bacType.HomingMovementType = (HomingType)value;
                RaisePropertyChanged(() => HomingType);
            }
        }
        public ushort HomingArcDirection
        {
            get
            {
                return (ushort)bacType.HomingArcDirection;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type20>(nameof(bacType.HomingArcDirection), bacType, bacType.HomingArcDirection, (HomingArcDirectionEnum)value, "HomingArcDirection"));
                bacType.HomingArcDirection = (HomingArcDirectionEnum)value;
                RaisePropertyChanged(() => HomingArcDirection);
            }
        }
        public float SpeedModifier
        {
            get
            {
                return bacType.SpeedModifier;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type20>(nameof(bacType.SpeedModifier), bacType, bacType.SpeedModifier, value, "SpeedModifier"));
                bacType.SpeedModifier = value;
                RaisePropertyChanged(() => SpeedModifier);
            }
        }
        public int FrameThreshold
        {
            get
            {
                return bacType.FrameThreshold;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type20>(nameof(bacType.FrameThreshold), bacType, bacType.FrameThreshold, value, "FrameThreshold"));
                bacType.FrameThreshold = value;
                RaisePropertyChanged(() => FrameThreshold);
            }
        }
        public float DisplacementX
        {
            get
            {
                return bacType.DisplacementX;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type20>(nameof(bacType.DisplacementX), bacType, bacType.DisplacementX, value, "HomingMovement DisplacementX"));
                bacType.DisplacementX = value;
                RaisePropertyChanged(() => DisplacementX);
            }
        }
        public float DisplacementY
        {
            get
            {
                return bacType.DisplacementY;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type20>(nameof(bacType.DisplacementY), bacType, bacType.DisplacementY, value, "HomingMovement DisplacementY"));
                bacType.DisplacementY = value;
                RaisePropertyChanged(() => DisplacementY);
            }
        }
        public float DisplacementZ
        {
            get
            {
                return bacType.DisplacementZ;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type20>(nameof(bacType.DisplacementZ), bacType, bacType.DisplacementZ, value, "HomingMovement DisplacementZ"));
                bacType.DisplacementZ = value;
                RaisePropertyChanged(() => DisplacementZ);
            }
        }
        public int I_32
        {
            get
            {
                return bacType.I_32;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type20>(nameof(bacType.I_32), bacType, bacType.I_32, value, "HomingMovement I_32"));
                bacType.I_32 = value;
                RaisePropertyChanged(() => I_32);
            }
        }
        public int I_36
        {
            get
            {
                return bacType.I_36;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type20>(nameof(bacType.I_36), bacType, bacType.I_36, value, "HomingMovement I_36"));
                bacType.I_36 = value;
                RaisePropertyChanged(() => I_36);
            }
        }


        public BACType20ViewModel(BAC_Type20 _bacType)
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
            RaisePropertyChanged(() => HomingType);
            RaisePropertyChanged(() => HomingArcDirection);
            RaisePropertyChanged(() => SpeedModifier);
            RaisePropertyChanged(() => FrameThreshold);
            RaisePropertyChanged(() => DisplacementX);
            RaisePropertyChanged(() => DisplacementY);
            RaisePropertyChanged(() => DisplacementZ);
            RaisePropertyChanged(() => I_32);
            RaisePropertyChanged(() => I_36);
        }


    }
}
