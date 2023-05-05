using GalaSoft.MvvmLight;
using XenoKit.Engine;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType1ViewModel : ObservableObject
    {
        private BAC_Type1 bacType;

        public byte BdmType
        {
            get
            {
                return (byte)bacType.bdmFile;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.bdmFile), bacType, bacType.bdmFile, (BAC_Type1.BdmType)value, "Hitbox BdmType"));
                bacType.bdmFile = (BAC_Type1.BdmType)value;
                RaisePropertyChanged(() => BdmType);
            }
        }
        public ushort BdmEntryID
        {
            get
            {
                return bacType.BdmEntryID;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.BdmEntryID), bacType, bacType.BdmEntryID, value, "Hitbox BdmEntryID"));
                bacType.BdmEntryID = value;
                RaisePropertyChanged(() => BdmEntryID);
            }
        }

        //HitboxFlags (damage properties)
        public ushort ImpactType
        {
            get
            {
                return (ushort)(bacType.HitboxFlags & 0xF000);
            }
            set
            {
                ushort newFlags = (ushort)((bacType.HitboxFlags & 0x0FFF) | value);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.HitboxFlags), bacType, bacType.HitboxFlags, newFlags, "Hitbox ImpactType"));
                bacType.HitboxFlags = newFlags;
                RaisePropertyChanged(() => ImpactType);
                RaisePropertyChanged(() => DamageType);
                RaisePropertyChanged(() => HitboxSpawnSource);
            }
        }
        public ushort DamageType
        {
            get
            {
                return (ushort)(bacType.HitboxFlags & 0x0F00);
            }
            set
            {
                ushort newFlags = (ushort)((bacType.HitboxFlags & 0xF0FF) | value);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.HitboxFlags), bacType, bacType.HitboxFlags, newFlags, "Hitbox DamageType"));
                bacType.HitboxFlags = newFlags;
                RaisePropertyChanged(() => ImpactType);
                RaisePropertyChanged(() => DamageType);
                RaisePropertyChanged(() => HitboxSpawnSource);
            }
        }
        public ushort HitboxSpawnSource
        {
            get
            {
                return (ushort)(bacType.HitboxFlags & 0x00F0);
            }
            set
            {
                ushort newFlags = (ushort)((bacType.HitboxFlags & 0xFF0F) | value);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.HitboxFlags), bacType, bacType.HitboxFlags, newFlags, "Hitbox SpawnSource"));
                bacType.HitboxFlags = newFlags;
                RaisePropertyChanged(() => ImpactType);
                RaisePropertyChanged(() => DamageType);
                RaisePropertyChanged(() => HitboxSpawnSource);
            }
        }

        //HitboxFlags (bools, first flag)
        public bool HitboxFlagA_Unk1
        {
            get
            {
                return (bacType.HitboxFlags & 0x0001) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0001 : 0x0000);
                ushort newFlags = (ushort)((bacType.HitboxFlags & 0xFFFE) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.HitboxFlags), bacType, bacType.HitboxFlags, newFlags, "Hitbox FlagsUnk1"));
                bacType.HitboxFlags = newFlags;
                RaisePropertyChanged(() => HitboxFlagA_Unk1);
                RaisePropertyChanged(() => ImpactType);
                RaisePropertyChanged(() => DamageType);
                RaisePropertyChanged(() => HitboxSpawnSource);
            }
        }
        public bool HitboxFlagA_Unk2
        {
            get
            {
                return (bacType.HitboxFlags & 0x0002) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0002 : 0x0000);
                ushort newFlags = (ushort)((bacType.HitboxFlags & 0xFFFD) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.HitboxFlags), bacType, bacType.HitboxFlags, newFlags, "Hitbox FlagsUnk2"));
                bacType.HitboxFlags = newFlags;
                RaisePropertyChanged(() => HitboxFlagA_Unk2);
                RaisePropertyChanged(() => ImpactType);
                RaisePropertyChanged(() => DamageType);
                RaisePropertyChanged(() => HitboxSpawnSource);
            }
        }
        public bool HitboxFlagA_Unk3
        {
            get
            {
                return (bacType.HitboxFlags & 0x0004) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0004 : 0x0000);
                ushort newFlags = (ushort)((bacType.HitboxFlags & 0xFFFB) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.HitboxFlags), bacType, bacType.HitboxFlags, newFlags, "Hitbox FlagsUnk3"));
                bacType.HitboxFlags = newFlags;
                RaisePropertyChanged(() => HitboxFlagA_Unk3);
                RaisePropertyChanged(() => ImpactType);
                RaisePropertyChanged(() => DamageType);
                RaisePropertyChanged(() => HitboxSpawnSource);
            }
        }
        public bool HitboxFlagA_Unk4
        {
            get
            {
                return (bacType.HitboxFlags & 0x0008) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0008 : 0x0000);
                ushort newFlags = (ushort)((bacType.HitboxFlags & 0xFFF7) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.HitboxFlags), bacType, bacType.HitboxFlags, newFlags, "Hitbox FlagsUnk4"));
                bacType.HitboxFlags = newFlags;
                RaisePropertyChanged(() => HitboxFlagA_Unk4);
                RaisePropertyChanged(() => ImpactType);
                RaisePropertyChanged(() => DamageType);
                RaisePropertyChanged(() => HitboxSpawnSource);
            }
        }

        //Unknown Flags
        public byte I_18_a
        {
            get
            {
                return bacType.I_18_a;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.I_18_a), bacType, bacType.I_18_a, value, "Hitbox I_18_a"));
                bacType.I_18_a = value;
                RaisePropertyChanged(() => I_18_a);
            }
        }

        //Damage
        public ushort Damage
        {
            get
            {
                return bacType.Damage;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.Damage), bacType, bacType.Damage, value, "Hitbox Damage"));
                bacType.Damage = value;
                RaisePropertyChanged(() => Damage);
            }
        }
        public ushort DamageWhenBlocked
        {
            get
            {
                return bacType.DamageWhenBlocked;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.DamageWhenBlocked), bacType, bacType.DamageWhenBlocked, value, "Hitbox DamageWhenBlocked"));
                bacType.DamageWhenBlocked = value;
                RaisePropertyChanged(() => DamageWhenBlocked);
            }
        }
        public ushort StaminaBlockedConsume
        {
            get
            {
                return bacType.StaminaTakenWhenBlocked;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.StaminaTakenWhenBlocked), bacType, bacType.StaminaTakenWhenBlocked, value, "Hitbox StaminaBlocked"));
                bacType.StaminaTakenWhenBlocked = value;
                RaisePropertyChanged(() => StaminaBlockedConsume);
            }
        }

        //Unknown integers. According to the BAC manual these are supposed to be flags, but they are not.
        public ushort I_20
        {
            get
            {
                return bacType.I_20;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.I_20), bacType, bacType.I_20, value, "Hitbox I_20"));
                bacType.I_20 = value;
                RaisePropertyChanged(() => I_20);
            }
        }
        public ushort I_22
        {
            get
            {
                return bacType.I_22;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.I_22), bacType, bacType.I_22, value, "Hitbox I_22"));
                bacType.I_22 = value;
                RaisePropertyChanged(() => I_22);
            }
        }

        public float F_24
        {
            get
            {
                return bacType.F_24;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.F_24), bacType, bacType.F_24, value, "Hitbox F_24"));
                bacType.F_24 = value;
                RaisePropertyChanged(() => F_24);
            }
        }

        //Matrix3x3
        public float PositionX
        {
            get
            {
                return bacType.PositionX;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.PositionX), bacType, bacType.PositionX, value, "Hitbox PositionX"));
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.PositionY), bacType, bacType.PositionY, value, "Hitbox PositionY"));
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.PositionZ), bacType, bacType.PositionZ, value, "Hitbox PositionZ"));
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.RotationX), bacType, bacType.RotationX, value, "Hitbox RotationX"));
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.RotationY), bacType, bacType.RotationY, value, "Hitbox RotationY"));
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.RotationZ), bacType, bacType.RotationZ, value, "Hitbox RotationZ"));
                bacType.RotationZ = value;
                RaisePropertyChanged(() => RotationZ);
            }
        }
        public float ScaleX
        {
            get
            {
                return bacType.ScaleX;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.ScaleX), bacType, bacType.ScaleX, value, "Hitbox ScaleX"));
                bacType.ScaleX = value;
                RaisePropertyChanged(() => ScaleX);
            }
        }
        public float ScaleY
        {
            get
            {
                return bacType.ScaleY;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.ScaleY), bacType, bacType.ScaleY, value, "Hitbox ScaleY"));
                bacType.ScaleY = value;
                RaisePropertyChanged(() => ScaleY);
            }
        }
        public float ScaleZ
        {
            get
            {
                return bacType.ScaleZ;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.ScaleZ), bacType, bacType.ScaleZ, value, "Hitbox ScaleZ"));
                bacType.ScaleZ = value;
                RaisePropertyChanged(() => ScaleZ);
            }
        }



        public BACType1ViewModel(BAC_Type1 _bacType)
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
            RaisePropertyChanged(() => BdmType);
            RaisePropertyChanged(() => BdmEntryID);
            RaisePropertyChanged(() => ImpactType);
            RaisePropertyChanged(() => DamageType);
            RaisePropertyChanged(() => HitboxSpawnSource);
            RaisePropertyChanged(() => HitboxFlagA_Unk1);
            RaisePropertyChanged(() => HitboxFlagA_Unk2);
            RaisePropertyChanged(() => HitboxFlagA_Unk3);
            RaisePropertyChanged(() => HitboxFlagA_Unk4);
            RaisePropertyChanged(() => I_18_a);
            RaisePropertyChanged(() => Damage);
            RaisePropertyChanged(() => DamageWhenBlocked);
            RaisePropertyChanged(() => StaminaBlockedConsume);
            RaisePropertyChanged(() => I_20);
            RaisePropertyChanged(() => I_22);
            RaisePropertyChanged(() => F_24);
            RaisePropertyChanged(() => PositionX);
            RaisePropertyChanged(() => PositionY);
            RaisePropertyChanged(() => PositionZ);
            RaisePropertyChanged(() => RotationX);
            RaisePropertyChanged(() => RotationY);
            RaisePropertyChanged(() => RotationZ);
            RaisePropertyChanged(() => ScaleX);
            RaisePropertyChanged(() => ScaleY);
            RaisePropertyChanged(() => ScaleZ);
            UpdateBacPlayer();
        }

        private void UpdateBacPlayer()
        {
            SceneManager.InvokeBacValuesChangedEvent();
        }

    }
}
