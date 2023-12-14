using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type17;

namespace XenoKit.ViewModel.BAC
{
    public class BACType17ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type17 bacType;

        public BoneLinks UserBone
        {
            get
            {
                return bacType.BoneLink;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type17>(nameof(bacType.BoneLink), bacType, bacType.BoneLink, value, "ThrowHandler UserBone"));
                bacType.BoneLink = value;
                RaisePropertyChanged(() => UserBone);
            }
        }
        public BoneLinks VictimBone
        {
            get
            {
                return bacType.VictimBone;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type17>(nameof(bacType.VictimBone), bacType, bacType.VictimBone, value, "ThrowHandler VictimBone"));
                bacType.VictimBone = value;
                RaisePropertyChanged(() => VictimBone);
            }
        }
        public ushort BacEntryID
        {
            get
            {
                return bacType.BacEntryId;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type17>(nameof(bacType.BacEntryId), bacType, bacType.BacEntryId, value, "ThrowHandler BacEntryId"));
                bacType.BacEntryId = value;
                RaisePropertyChanged(() => BacEntryID);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type17>(nameof(bacType.I_10), bacType, bacType.I_10, value, "ThrowHandler I_10"));
                bacType.I_10 = value;
                RaisePropertyChanged(() => I_10);
            }
        }
        public ushort I_18
        {
            get
            {
                return bacType.I_18;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type17>(nameof(bacType.I_18), bacType, bacType.I_18, value, "ThrowHandler I_18"));
                bacType.I_18 = value;
                RaisePropertyChanged(() => I_18);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type17>(nameof(bacType.DisplacementX), bacType, bacType.DisplacementX, value, "ThrowHandler DisplacementX"));
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type17>(nameof(bacType.DisplacementY), bacType, bacType.DisplacementY, value, "ThrowHandler DisplacementY"));
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type17>(nameof(bacType.DisplacementZ), bacType, bacType.DisplacementZ, value, "ThrowHandler DisplacementZ"));
                bacType.DisplacementZ = value;
                RaisePropertyChanged(() => DisplacementZ);
            }
        }


        //Flags
        public bool Flag_FixedDir_BoneEnabled
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.FixedDir_BoneEnabled);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.FixedDir_BoneEnabled, value);
                RaisePropertyChanged(() => Flag_FixedDir_BoneEnabled);
            }
        }
        public bool Flag_FixedDir_BoneDisabled
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.FixedDir_BoneDisabled);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.FixedDir_BoneDisabled, value);
                RaisePropertyChanged(() => Flag_FixedDir_BoneDisabled);
            }
        }
        public bool Flag_FreeDir_BoneEnabled
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.FreeDir_BoneEnabled);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.FreeDir_BoneEnabled, value);
                RaisePropertyChanged(() => Flag_FreeDir_BoneEnabled);
            }
        }
        public bool Flag_FreeDir_BoneDisabled
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.FreeDir_BoneDisabled);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.FreeDir_BoneDisabled, value);
                RaisePropertyChanged(() => Flag_FreeDir_BoneDisabled);
            }
        }
        public bool Flag_Unk5
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.Unk5);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.Unk5, value);
                RaisePropertyChanged(() => Flag_Unk5);
            }
        }
        public bool Flag_BacJump_AfterDuration
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.BacJump_AfterDuration);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.BacJump_AfterDuration, value);
                RaisePropertyChanged(() => Flag_BacJump_AfterDuration);
            }
        }
        public bool Flag_Unk7
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.Unk7);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.Unk7, value);
                RaisePropertyChanged(() => Flag_Unk7);
            }
        }
        public bool Flag_BacJump_ReachGround
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.BacJump_ReachGround);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.BacJump_ReachGround, value);
                RaisePropertyChanged(() => Flag_BacJump_ReachGround);
            }
        }
        public bool Flag_MoveVictimToUser
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.MoveVictimToUser);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.MoveVictimToUser, value);
                RaisePropertyChanged(() => Flag_MoveVictimToUser);
            }
        }
        public bool Flag_MoveVictimToUser_RelativeDir
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.MoveVictimToUser_RelativeDir);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.MoveVictimToUser_RelativeDir, value);
                RaisePropertyChanged(() => Flag_MoveVictimToUser_RelativeDir);
            }
        }
        public bool Flag_Unk11
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.Unk11);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.Unk11, value);
                RaisePropertyChanged(() => Flag_Unk11);
            }
        }
        public bool Flag_Unk12
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.Unk12);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.Unk12, value);
                RaisePropertyChanged(() => Flag_Unk12);
            }
        }
        public bool Flag_Unk13
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.Unk13);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.Unk13, value);
                RaisePropertyChanged(() => Flag_Unk13);
            }
        }
        public bool Flag_Unk14
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.Unk14);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.Unk14, value);
                RaisePropertyChanged(() => Flag_Unk14);
            }
        }
        public bool Flag_Unk15
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.Unk15);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.Unk15, value);
                RaisePropertyChanged(() => Flag_Unk15);
            }
        }
        public bool Flag_Unk16
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.Unk16);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.Unk16, value);
                RaisePropertyChanged(() => Flag_Unk16);
            }
        }



        public BACType17ViewModel(BAC_Type17 _bacType)
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
            RaisePropertyChanged(() => UserBone);
            RaisePropertyChanged(() => VictimBone);
            RaisePropertyChanged(() => BacEntryID);
            RaisePropertyChanged(() => I_10);
            RaisePropertyChanged(() => I_18);
            RaisePropertyChanged(() => DisplacementX);
            RaisePropertyChanged(() => DisplacementY);
            RaisePropertyChanged(() => DisplacementZ);

            RaisePropertyChanged(() => Flag_FixedDir_BoneEnabled);
            RaisePropertyChanged(() => Flag_FixedDir_BoneDisabled);
            RaisePropertyChanged(() => Flag_FreeDir_BoneEnabled);
            RaisePropertyChanged(() => Flag_FreeDir_BoneDisabled);
            RaisePropertyChanged(() => Flag_Unk5);
            RaisePropertyChanged(() => Flag_BacJump_AfterDuration);
            RaisePropertyChanged(() => Flag_Unk7);
            RaisePropertyChanged(() => Flag_BacJump_ReachGround);
            RaisePropertyChanged(() => Flag_MoveVictimToUser);
            RaisePropertyChanged(() => Flag_MoveVictimToUser_RelativeDir);
            RaisePropertyChanged(() => Flag_Unk11);
            RaisePropertyChanged(() => Flag_Unk12);
            RaisePropertyChanged(() => Flag_Unk13);
            RaisePropertyChanged(() => Flag_Unk14);
            RaisePropertyChanged(() => Flag_Unk15);
            RaisePropertyChanged(() => Flag_Unk16);
        }

        private void SetThrowHandlerFlags(ThrowHandlerFlagsEnum flag, bool state)
        {
            var newFlag = bacType.ThrowHandlerFlags.SetFlag(flag, state);

            if (bacType.ThrowHandlerFlags != newFlag)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type17>(nameof(BAC_Type17.ThrowHandlerFlags), bacType, bacType.ThrowHandlerFlags, newFlag, "ThrowHandlerFlags"));
                bacType.ThrowHandlerFlags = newFlag;
            }
        }
    }
}
