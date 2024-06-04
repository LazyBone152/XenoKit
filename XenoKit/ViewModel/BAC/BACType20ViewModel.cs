using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type20;

namespace XenoKit.ViewModel.BAC
{
    public class BACType20ViewModel : ObservableObject, IDisposable
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
                return (ushort)bacType.HomingFlags;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type20>(nameof(bacType.HomingFlags), bacType, bacType.HomingFlags, (HomingFlagsEnum)value, "HomingArcDirection"));
                bacType.HomingFlags = (HomingFlagsEnum)value;
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
        public BoneLinks UserBone
        {
            get
            {
                return bacType.BoneLink;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type20>(nameof(bacType.BoneLink), bacType, bacType.BoneLink, value, "HomingMovement UserBone"));
                bacType.BoneLink = value;
                RaisePropertyChanged(() => UserBone);
            }
        }
        public BoneLinks TargetBone
        {
            get
            {
                return bacType.TargetBone;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type20>(nameof(bacType.TargetBone), bacType, bacType.TargetBone, value, "HomingMovement TargetBone"));
                bacType.TargetBone = value;
                RaisePropertyChanged(() => TargetBone);
            }
        }

        //Flags
        public bool Flag_EnableAutoTracking
        {
            get
            {
                return bacType.HomingFlags.HasFlag(HomingFlagsEnum.EnableAutoTracking);
            }
            set
            {
                SetHomingFlags(HomingFlagsEnum.EnableAutoTracking, value);
                RaisePropertyChanged(() => Flag_EnableAutoTracking);
            }
        }
        public bool Flag_Float
        {
            get
            {
                return bacType.HomingFlags.HasFlag(HomingFlagsEnum.UseFloatSpeedModifier);
            }
            set
            {
                SetHomingFlags(HomingFlagsEnum.UseFloatSpeedModifier, value);
                RaisePropertyChanged(() => Flag_Float);
                RefreshUI();
            }
        }
        public bool Flag_Unk3
        {
            get
            {
                return bacType.HomingFlags.HasFlag(HomingFlagsEnum.Unk3);
            }
            set
            {
                SetHomingFlags(HomingFlagsEnum.Unk3, value);
                RaisePropertyChanged(() => Flag_Unk3);
            }
        }
        public bool Flag_UseBones
        {
            get
            {
                return bacType.HomingFlags.HasFlag(HomingFlagsEnum.UseBones);
            }
            set
            {
                SetHomingFlags(HomingFlagsEnum.UseBones, value);
                RaisePropertyChanged(() => Flag_UseBones);
            }
        }
        public bool Flag_Unk5
        {
            get
            {
                return bacType.HomingFlags.HasFlag(HomingFlagsEnum.Unk5);
            }
            set
            {
                SetHomingFlags(HomingFlagsEnum.Unk5, value);
                RaisePropertyChanged(() => Flag_Unk5);
            }
        }
        public bool Flag_Unk6
        {
            get
            {
                return bacType.HomingFlags.HasFlag(HomingFlagsEnum.Unk6);
            }
            set
            {
                SetHomingFlags(HomingFlagsEnum.Unk6, value);
                RaisePropertyChanged(() => Flag_Unk6);
            }
        }

        public string SpeedModifierLabel => Flag_Float ? "Speed Modifier" : "Frame Duration";

        public BACType20ViewModel(BAC_Type20 _bacType)
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
            RaisePropertyChanged(() => HomingType);
            RaisePropertyChanged(() => HomingArcDirection);
            RaisePropertyChanged(() => SpeedModifier);
            RaisePropertyChanged(() => FrameThreshold);
            RaisePropertyChanged(() => DisplacementX);
            RaisePropertyChanged(() => DisplacementY);
            RaisePropertyChanged(() => DisplacementZ);
            RaisePropertyChanged(() => UserBone);
            RaisePropertyChanged(() => TargetBone);

            RaisePropertyChanged(() => Flag_EnableAutoTracking);
            RaisePropertyChanged(() => Flag_Float);
            RaisePropertyChanged(() => Flag_Unk3);
            RaisePropertyChanged(() => Flag_UseBones);
            RaisePropertyChanged(() => Flag_Unk5);
            RaisePropertyChanged(() => Flag_Unk6);
            RefreshUI();
        }

        private void SetHomingFlags(HomingFlagsEnum flag, bool state)
        {
            var newFlag = bacType.HomingFlags.SetFlag(flag, state);

            if (bacType.HomingFlags != newFlag)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type20>(nameof(BAC_Type20.HomingFlags), bacType, bacType.HomingFlags, newFlag, "HomingFlags"));
                bacType.HomingFlags = newFlag;
            }
        }

        private void RefreshUI()
        {
            RaisePropertyChanged(() => SpeedModifierLabel);
        }
    }
}
