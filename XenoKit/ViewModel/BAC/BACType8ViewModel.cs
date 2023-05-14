using GalaSoft.MvvmLight;
using System.Collections.Generic;
using System.Windows;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type8;

namespace XenoKit.ViewModel.BAC
{
    public class BACType8ViewModel : ObservableObject
    {
        private BAC_Type8 bacType;

        public ushort EepkType
        {
            get
            {
                return (ushort)bacType.EepkType;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type8>(nameof(bacType.EepkType), bacType, bacType.EepkType, (BAC_Type8.EepkTypeEnum)value, "Effect EepkType"));
                bacType.EepkType = (BAC_Type8.EepkTypeEnum)value;
                RaisePropertyChanged(() => EepkType);
                RaisePropertyChanged(() => UseCmnList);
                RaisePropertyChanged(() => SkillIdVisibile);
                bacType.RefreshType();
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type8>(nameof(bacType.SkillID), bacType, bacType.SkillID, value, "Effect SkillID"));
                bacType.SkillID = value;
                RaisePropertyChanged(() => SkillID);
            }
        }
        public int EffectID
        {
            get
            {
                return bacType.EffectID;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type8>(nameof(bacType.EffectID), bacType, bacType.EffectID, value, "Effect EffectID"));
                bacType.EffectID = value;
                RaisePropertyChanged(() => EffectID);
                bacType.RefreshType();
            }
        }
        public int EffectState
        {
            get
            {
                return (int)bacType.EffectFlags;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type8>(nameof(bacType.EffectFlags), bacType, (BAC_Type8.EffectFlagsEnum)bacType.EffectFlags, value, "Effect EffectState"));
                bacType.EffectFlags =(BAC_Type8.EffectFlagsEnum)value;
                RaisePropertyChanged(() => EffectState);
            }
        }
        public BoneLinks BoneLink
        {
            get
            {
                return bacType.BoneLink;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type8>(nameof(bacType.BoneLink), bacType, bacType.BoneLink, value, "Effect BoneLink"), UndoGroup.Action, "BoneLink");
                bacType.BoneLink = value;
                RaisePropertyChanged(() => BoneLink);
                UndoManager.Instance.ForceEventCall(UndoGroup.Action, "BoneLink");
            }
        }
        public bool UseSkillId
        {
            get
            {
                return bacType.UseSkillId == BAC_Type8.UseSkillIdEnum.True;
            }
            set
            {
                UseSkillIdEnum _useSkillId = value ? BAC_Type8.UseSkillIdEnum.True : BAC_Type8.UseSkillIdEnum.False;

                if(bacType.UseSkillId != _useSkillId)
                {
                    List<IUndoRedo> undos = new List<IUndoRedo>();
                    undos.Add(new UndoableProperty<BAC_Type8>(nameof(bacType.UseSkillId), bacType, bacType.UseSkillId, _useSkillId));
                    undos.Add(new UndoableProperty<BAC_Type8>(nameof(bacType.SkillID), bacType, bacType.SkillID, ushort.MaxValue));

                    UndoManager.Instance.AddCompositeUndo(undos, "Effect UseSkillId");

                    bacType.UseSkillId = _useSkillId;
                    bacType.SkillID = ushort.MaxValue;
                    RaisePropertyChanged(() => UseSkillId);
                    RaisePropertyChanged(() => SkillID);
                }
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type8>(nameof(bacType.PositionX), bacType, bacType.PositionX, value, "Effect PositionX"));
                bacType.PositionX = value;
                RaisePropertyChanged(() => PositionX);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type8>(nameof(bacType.PositionY), bacType, bacType.PositionY, value, "Effect PositionY"));
                bacType.PositionY = value;
                RaisePropertyChanged(() => PositionY);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type8>(nameof(bacType.PositionZ), bacType, bacType.PositionZ, value, "Effect PositionZ"));
                bacType.PositionZ = value;
                RaisePropertyChanged(() => PositionZ);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type8>(nameof(bacType.RotationX), bacType, bacType.RotationX, value, "Effect RotationX"));
                bacType.RotationX = value;
                RaisePropertyChanged(() => RotationX);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type8>(nameof(bacType.RotationY), bacType, bacType.RotationY, value, "Effect RotationY"));
                bacType.RotationY = value;
                RaisePropertyChanged(() => RotationY);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type8>(nameof(bacType.RotationZ), bacType, bacType.RotationZ, value, "Effect RotationZ"));
                bacType.RotationZ = value;
                RaisePropertyChanged(() => RotationZ);
            }
        }

        //Flags
        public bool EffectFlag_Off
        {
            get
            {
                return bacType.EffectFlags.HasFlag(BAC_Type8.EffectFlagsEnum.Off);
            }
            set
            {
                EffectFlagsEnum flags = bacType.EffectFlags.SetFlag(EffectFlagsEnum.Off, value);

                if(flags != bacType.EffectFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type8>(nameof(bacType.EffectFlags), bacType, bacType.EffectFlags, flags, "EffectFlags Off"));
                    bacType.EffectFlags = flags;
                    RaisePropertyChanged(() => EffectFlag_Off);
                    bacType.RefreshType();
                }
            }
        }
        public bool EffectFlag_Unk2
        {
            get
            {
                return bacType.EffectFlags.HasFlag(BAC_Type8.EffectFlagsEnum.Unk2);
            }
            set
            {
                EffectFlagsEnum flags = bacType.EffectFlags.SetFlag(EffectFlagsEnum.Unk2, value);

                if (flags != bacType.EffectFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type8>(nameof(bacType.EffectFlags), bacType, bacType.EffectFlags, flags, "EffectFlags Unk2"));
                    bacType.EffectFlags = flags;
                    RaisePropertyChanged(() => EffectFlag_Unk2);
                }
            }
        }
        public bool EffectFlag_SpawnOnTarget
        {
            get
            {
                return bacType.EffectFlags.HasFlag(BAC_Type8.EffectFlagsEnum.SpawnOnTarget);
            }
            set
            {
                EffectFlagsEnum flags = bacType.EffectFlags.SetFlag(EffectFlagsEnum.SpawnOnTarget, value);

                if (flags != bacType.EffectFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type8>(nameof(bacType.EffectFlags), bacType, bacType.EffectFlags, flags, "EffectFlags SpawnOnTarget"));
                    bacType.EffectFlags = flags;
                    RaisePropertyChanged(() => EffectFlag_SpawnOnTarget);
                }
            }
        }
        public bool EffectFlag_Loop
        {
            get
            {
                return bacType.EffectFlags.HasFlag(BAC_Type8.EffectFlagsEnum.Loop);
            }
            set
            {
                EffectFlagsEnum flags = bacType.EffectFlags.SetFlag(EffectFlagsEnum.Loop, value);

                if (flags != bacType.EffectFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type8>(nameof(bacType.EffectFlags), bacType, bacType.EffectFlags, flags, "EffectFlags Loop"));
                    bacType.EffectFlags = flags;
                    RaisePropertyChanged(() => EffectFlag_Loop);
                }
            }
        }
        public bool EffectFlag_UserOnly
        {
            get
            {
                return bacType.EffectFlags.HasFlag(BAC_Type8.EffectFlagsEnum.UserOnly);
            }
            set
            {
                EffectFlagsEnum flags = bacType.EffectFlags.SetFlag(EffectFlagsEnum.UserOnly, value);

                if (flags != bacType.EffectFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type8>(nameof(bacType.EffectFlags), bacType, bacType.EffectFlags, flags, "EffectFlags UserOnly"));
                    bacType.EffectFlags = flags;
                    RaisePropertyChanged(() => EffectFlag_UserOnly);
                }
            }
        }
        public bool EffectFlag_Unk6
        {
            get
            {
                return bacType.EffectFlags.HasFlag(BAC_Type8.EffectFlagsEnum.Unk6);
            }
            set
            {
                EffectFlagsEnum flags = bacType.EffectFlags.SetFlag(EffectFlagsEnum.Unk6, value);

                if (flags != bacType.EffectFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type8>(nameof(bacType.EffectFlags), bacType, bacType.EffectFlags, flags, "EffectFlags Unk6"));
                    bacType.EffectFlags = flags;
                    RaisePropertyChanged(() => EffectFlag_Unk6);
                }
            }
        }
        public bool EffectFlag_Unk8
        {
            get
            {
                return bacType.EffectFlags.HasFlag(BAC_Type8.EffectFlagsEnum.Unk8);
            }
            set
            {
                EffectFlagsEnum flags = bacType.EffectFlags.SetFlag(EffectFlagsEnum.Unk8, value);

                if (flags != bacType.EffectFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type8>(nameof(bacType.EffectFlags), bacType, bacType.EffectFlags, flags, "EffectFlags Unk8"));
                    bacType.EffectFlags = flags;
                    RaisePropertyChanged(() => EffectFlag_Unk8);
                }
            }
        }


        //UI
        public bool UseCmnList => EepkType == 0;
        public Visibility SkillIdVisibile
        {
            get
            {
                return UseCmnList ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public BACType8ViewModel(BAC_Type8 _bacType)
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
            RaisePropertyChanged(() => EepkType);
            RaisePropertyChanged(() => SkillID);
            RaisePropertyChanged(() => EffectID);
            RaisePropertyChanged(() => EffectState);
            RaisePropertyChanged(() => BoneLink);
            RaisePropertyChanged(() => UseSkillId);
            RaisePropertyChanged(() => PositionX);
            RaisePropertyChanged(() => PositionY);
            RaisePropertyChanged(() => PositionZ);
            RaisePropertyChanged(() => RotationX);
            RaisePropertyChanged(() => RotationY);
            RaisePropertyChanged(() => RotationZ);

            RaisePropertyChanged(() => EffectFlag_Off);
            RaisePropertyChanged(() => EffectFlag_Loop);
            RaisePropertyChanged(() => EffectFlag_SpawnOnTarget);
            RaisePropertyChanged(() => EffectFlag_Unk2);
            RaisePropertyChanged(() => EffectFlag_Unk6);
            RaisePropertyChanged(() => EffectFlag_Unk8);
            RaisePropertyChanged(() => EffectFlag_UserOnly);
        }


    }
}
