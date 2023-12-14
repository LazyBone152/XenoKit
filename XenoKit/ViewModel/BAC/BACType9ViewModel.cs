using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type9;

namespace XenoKit.ViewModel.BAC
{
    public class BACType9ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type9 bacType;

        public byte BsaType
        {
            get
            {
                return (byte)bacType.BsaType;
            }
            set
            {
                if(bacType.BsaType != (BAC_Type9.BsaTypeEnum)value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type9>(nameof(bacType.BsaType), bacType, bacType.BsaType, (BAC_Type9.BsaTypeEnum)value, "Projectile BsaType"));
                    bacType.BsaType = (BAC_Type9.BsaTypeEnum)value;
                    RaisePropertyChanged(() => BsaType);
                    bacType.RefreshType();
                }
            }
        }
        public ushort SkillID
        {
            get
            {
                return bacType.SkillID;
            }
            set
            {
                if(bacType.SkillID != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type9>(nameof(bacType.SkillID), bacType, bacType.SkillID, value, "Projectile SkillID"));
                    bacType.SkillID = value;
                    RaisePropertyChanged(() => SkillID);
                }
            }
        }
        public bool CanUseCmnBsa
        {
            get
            {
                return bacType.CanUseCmnBsa == BAC_Type9.CanUseCmnBsaEnum.True;
            }
            set
            {
                BAC_Type9.CanUseCmnBsaEnum canUseCmn = value ? BAC_Type9.CanUseCmnBsaEnum.True : BAC_Type9.CanUseCmnBsaEnum.False;

                if (bacType.CanUseCmnBsa != canUseCmn)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type9>(nameof(bacType.CanUseCmnBsa), bacType, bacType.CanUseCmnBsa, canUseCmn, "Projectile CanUseCmnBsa"));
                    bacType.CanUseCmnBsa = canUseCmn;
                    RaisePropertyChanged(() => CanUseCmnBsa);
                }
            }
        }
        public int EntryID
        {
            get
            {
                return bacType.EntryID;
            }
            set
            {
                if (bacType.EntryID != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type9>(nameof(bacType.EntryID), bacType, bacType.EntryID, value, "Projectile EntryID"));
                    bacType.EntryID = value;
                    RaisePropertyChanged(() => EntryID);
                }
            }
        }
        public BoneLinks Bone
        {
            get
            {
                return bacType.BoneLink;
            }
            set
            {
                if (bacType.BoneLink != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type9>(nameof(bacType.BoneLink), bacType, bacType.BoneLink, value, "Projectile Bone"), UndoGroup.Action, "BoneLink");
                    bacType.BoneLink = value;
                    RaisePropertyChanged(() => Bone);
                    UndoManager.Instance.ForceEventCall(UndoGroup.Action, "BoneLink");
                }
            }
        }
        

        //Flaggy stuff
        public byte SpawnSource
        {
            get
            {
                return bacType.SpawnSource;
            }
            set
            {
                if (bacType.SpawnSource != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type9>(nameof(bacType.SpawnSource), bacType, bacType.SpawnSource, value, "Projectile SpawnSource"));
                    bacType.SpawnSource = value;
                    RaisePropertyChanged(() => SpawnSource);
                }
            }
        }
        public byte SpawnOrientation
        {
            get
            {
                return bacType.SpawnOrientation;
            }
            set
            {
                if (bacType.SpawnOrientation != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type9>(nameof(bacType.SpawnOrientation), bacType, bacType.SpawnOrientation, value, "Projectile SpawnOrientation"));
                    bacType.SpawnOrientation = value;
                    RaisePropertyChanged(() => SpawnOrientation);
                }
            }
        }

        //Flags
        public bool Flag_TerminatePrev
        {
            get
            {
                return bacType.BsaFlags.HasFlag(BsaFlagsEnum.TerminatePreviousProjectile);
            }
            set
            {
                SetBsaFlag(BsaFlagsEnum.TerminatePreviousProjectile, value);
                RaisePropertyChanged(() => Flag_TerminatePrev);
            }
        }
        public bool Flag_Unk2
        {
            get
            {
                return bacType.BsaFlags.HasFlag(BsaFlagsEnum.Unk2);
            }
            set
            {
                SetBsaFlag(BsaFlagsEnum.Unk2, value);
                RaisePropertyChanged(() => Flag_Unk2);
            }
        }
        public bool Flag_Unk3
        {
            get
            {
                return bacType.BsaFlags.HasFlag(BsaFlagsEnum.Unk3);
            }
            set
            {
                SetBsaFlag(BsaFlagsEnum.Unk3, value);
                RaisePropertyChanged(() => Flag_Unk3);
            }
        }
        public bool Flag_Unk4
        {
            get
            {
                return bacType.BsaFlags.HasFlag(BsaFlagsEnum.Unk4);
            }
            set
            {
                SetBsaFlag(BsaFlagsEnum.Unk4, value);
                RaisePropertyChanged(() => Flag_Unk4);
            }
        }
        public bool Flag_BcmCondition
        {
            get
            {
                return bacType.BsaFlags.HasFlag(BsaFlagsEnum.BcmCondition);
            }
            set
            {
                SetBsaFlag(BsaFlagsEnum.BcmCondition, value);
                RaisePropertyChanged(() => Flag_BcmCondition);
            }
        }
        public bool Flag_Unk10
        {
            get
            {
                return bacType.BsaFlags.HasFlag(BsaFlagsEnum.Unk10);
            }
            set
            {
                SetBsaFlag(BsaFlagsEnum.Unk10, value);
                RaisePropertyChanged(() => Flag_Unk10);
            }
        }
        public bool Flag_Unk11
        {
            get
            {
                return bacType.BsaFlags.HasFlag(BsaFlagsEnum.Unk11);
            }
            set
            {
                SetBsaFlag(BsaFlagsEnum.Unk11, value);
                RaisePropertyChanged(() => Flag_Unk11);
            }
        }
        public bool Flag_Loop
        {
            get
            {
                return bacType.BsaFlags.HasFlag(BsaFlagsEnum.Loop);
            }
            set
            {
                SetBsaFlag(BsaFlagsEnum.Loop, value);
                RaisePropertyChanged(() => Flag_Loop);
            }
        }
        public bool Flag_DuplicateForAll
        {
            get
            {
                return bacType.BsaFlags.HasFlag(BsaFlagsEnum.DuplicateForAllOpponents);
            }
            set
            {
                SetBsaFlag(BsaFlagsEnum.DuplicateForAllOpponents, value);
                RaisePropertyChanged(() => Flag_DuplicateForAll);
            }
        }
        public bool Flag_Unk14
        {
            get
            {
                return bacType.BsaFlags.HasFlag(BsaFlagsEnum.Unk14);
            }
            set
            {
                SetBsaFlag(BsaFlagsEnum.Unk14, value);
                RaisePropertyChanged(() => Flag_Unk14);
            }
        }
        public bool Flag_MarkRandomID
        {
            get
            {
                return bacType.BsaFlags.HasFlag(BsaFlagsEnum.MarkRandomID);
            }
            set
            {
                SetBsaFlag(BsaFlagsEnum.MarkRandomID, value);
                RaisePropertyChanged(() => Flag_MarkRandomID);
            }
        }
        public bool Flag_MarkUniqueID
        {
            get
            {
                return bacType.BsaFlags.HasFlag(BsaFlagsEnum.MarkUniqueID);
            }
            set
            {
                SetBsaFlag(BsaFlagsEnum.MarkUniqueID, value);
                RaisePropertyChanged(() => Flag_MarkUniqueID);
            }
        }

        //Positioning
        public float PositionX
        {
            get
            {
                return bacType.PositionX;
            }
            set
            {
                if (bacType.PositionX != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type9>(nameof(bacType.PositionX), bacType, bacType.PositionX, value, "Projectile PositionX"));
                    bacType.PositionX = value;
                    RaisePropertyChanged(() => PositionX);
                }
            }
        }
        public float PositionY
        {
            get
            {
                return bacType.PositionY;
            }
            set
            {
                if (bacType.PositionY != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type9>(nameof(bacType.PositionY), bacType, bacType.PositionY, value, "Projectile PositionY"));
                    bacType.PositionY = value;
                    RaisePropertyChanged(() => PositionY);
                }
            }
        }
        public float PositionZ
        {
            get
            {
                return bacType.PositionZ;
            }
            set
            {
                if (bacType.PositionZ != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type9>(nameof(bacType.PositionZ), bacType, bacType.PositionZ, value, "Projectile PositionZ"));
                    bacType.PositionZ = value;
                    RaisePropertyChanged(() => PositionZ);
                }
            }
        }
        public float RotationX
        {
            get
            {
                return bacType.RotationX;
            }
            set
            {
                if (bacType.RotationX != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type9>(nameof(bacType.RotationX), bacType, bacType.RotationX, value, "Projectile RotationX"));
                    bacType.RotationX = value;
                    RaisePropertyChanged(() => RotationX);
                }
            }
        }
        public float RotationY
        {
            get
            {
                return bacType.RotationY;
            }
            set
            {
                if (bacType.RotationY != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type9>(nameof(bacType.RotationY), bacType, bacType.RotationY, value, "Projectile RotationY"));
                    bacType.RotationY = value;
                    RaisePropertyChanged(() => RotationY);
                }
            }
        }
        public float RotationZ
        {
            get
            {
                return bacType.RotationZ;
            }
            set
            {
                if (bacType.RotationZ != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type9>(nameof(bacType.RotationZ), bacType, bacType.RotationZ, value, "Projectile RotationZ"));
                    bacType.RotationZ = value;
                    RaisePropertyChanged(() => RotationZ);
                }
            }
        }

        public float Health
        {
            get
            {
                return bacType.ProjectileHealth;
            }
            set
            {
                if (bacType.ProjectileHealth != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type9>(nameof(bacType.ProjectileHealth), bacType, bacType.ProjectileHealth, value, "Projectile Health"));
                    bacType.ProjectileHealth = value;
                    RaisePropertyChanged(() => Health);
                }
            }
        }
        public int UniqueID
        {
            get
            {
                return bacType.UniqueID;
            }
            set
            {
                if (bacType.UniqueID != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type9>(nameof(bacType.UniqueID), bacType, bacType.UniqueID, value, "Projectile UniqueID"));
                    bacType.UniqueID = value;
                    RaisePropertyChanged(() => UniqueID);
                }
            }
        }


        public BACType9ViewModel(BAC_Type9 _bacType)
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
            RaisePropertyChanged(() => BsaType);
            RaisePropertyChanged(() => SkillID);
            RaisePropertyChanged(() => CanUseCmnBsa);
            RaisePropertyChanged(() => EntryID);
            RaisePropertyChanged(() => Bone);
            RaisePropertyChanged(() => SpawnSource);
            RaisePropertyChanged(() => SpawnOrientation);
            RaisePropertyChanged(() => Flag_TerminatePrev);
            RaisePropertyChanged(() => Flag_Unk2);
            RaisePropertyChanged(() => Flag_Unk3);
            RaisePropertyChanged(() => Flag_Unk4);
            RaisePropertyChanged(() => Flag_BcmCondition);
            RaisePropertyChanged(() => Flag_Unk10);
            RaisePropertyChanged(() => Flag_Unk11);
            RaisePropertyChanged(() => Flag_Loop);
            RaisePropertyChanged(() => Flag_DuplicateForAll);
            RaisePropertyChanged(() => Flag_Unk14);
            RaisePropertyChanged(() => Flag_MarkRandomID);
            RaisePropertyChanged(() => Flag_MarkUniqueID);
            RaisePropertyChanged(() => PositionX);
            RaisePropertyChanged(() => PositionY);
            RaisePropertyChanged(() => PositionZ);
            RaisePropertyChanged(() => RotationX);
            RaisePropertyChanged(() => RotationY);
            RaisePropertyChanged(() => RotationZ);
            RaisePropertyChanged(() => Health);
            RaisePropertyChanged(() => UniqueID);
        }


        private void SetBsaFlag(BsaFlagsEnum flag, bool state)
        {
            var newFlag = bacType.BsaFlags.SetFlag(flag, state);

            if (bacType.BsaFlags != newFlag)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type9>(nameof(BAC_Type9.BsaFlags), bacType, bacType.BsaFlags, newFlag, "BsaFlags"));
                bacType.BsaFlags = newFlag;
            }
        }
    }
}
