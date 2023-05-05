using GalaSoft.MvvmLight;
using XenoKit.Engine;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type1;

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
                bacType.RefreshType();
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
        public BoundingBoxTypeEnum BoundingBoxType
        {
            get
            {
                return bacType.BoundingBoxType;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.BoundingBoxType), bacType, bacType.BoundingBoxType, value, "Hitbox BoundingBoxType"));
                bacType.BoundingBoxType = value;
                RaisePropertyChanged(() => BoundingBoxType);
                RaisePropertyChanged(() => BoundsEnabled);
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

        //Unknown integer. Not a flag.
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
        public BoneLinks BoneLink
        {
            get
            {
                return bacType.BoneLink;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.BoneLink), bacType, bacType.BoneLink, value, "Hitbox BoneLink"), UndoGroup.Action, "BoneLink");
                bacType.BoneLink = value;
                RaisePropertyChanged(() => BoneLink);
                UndoManager.Instance.ForceEventCall(UndoGroup.Action, "BoneLink");
            }
        }

        public float Size
        {
            get
            {
                return bacType.Size;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.Size), bacType, bacType.Size, value, "Hitbox Size"));
                bacType.Size = value;
                RaisePropertyChanged(() => Size);
            }
        }

        //BoundingBox
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
        public float MaxX
        {
            get
            {
                return bacType.MaxX;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.MaxX), bacType, bacType.MaxX, value, "Hitbox MaxX"));
                bacType.MaxX = value;
                RaisePropertyChanged(() => MaxX);
            }
        }
        public float MaxY
        {
            get
            {
                return bacType.MaxY;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.MaxY), bacType, bacType.MaxY, value, "Hitbox MaxY"));
                bacType.MaxY = value;
                RaisePropertyChanged(() => MaxY);
            }
        }
        public float MaxZ
        {
            get
            {
                return bacType.MaxZ;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.MaxZ), bacType, bacType.MaxZ, value, "Hitbox MaxZ"));
                bacType.MaxZ = value;
                RaisePropertyChanged(() => MaxZ);
            }
        }
        public float MinX
        {
            get
            {
                return bacType.MinX;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.MinX), bacType, bacType.MinX, value, "Hitbox MinX"));
                bacType.MinX = value;
                RaisePropertyChanged(() => MinX);
            }
        }
        public float MinY
        {
            get
            {
                return bacType.MinY;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.MinY), bacType, bacType.MinY, value, "Hitbox MinY"));
                bacType.MinY = value;
                RaisePropertyChanged(() => MinY);
            }
        }
        public float MinZ
        {
            get
            {
                return bacType.MinZ;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.MinZ), bacType, bacType.MinZ, value, "Hitbox MinZ"));
                bacType.MinZ = value;
                RaisePropertyChanged(() => MinZ);
            }
        }

        public bool BoundsEnabled => bacType.BoundingBoxType != BoundingBoxTypeEnum.Uniform;

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
            RaisePropertyChanged(() => BoundingBoxType);
            RaisePropertyChanged(() => Damage);
            RaisePropertyChanged(() => DamageWhenBlocked);
            RaisePropertyChanged(() => StaminaBlockedConsume);
            RaisePropertyChanged(() => I_20);
            RaisePropertyChanged(() => BoneLink);
            RaisePropertyChanged(() => Size);
            RaisePropertyChanged(() => PositionX);
            RaisePropertyChanged(() => PositionY);
            RaisePropertyChanged(() => PositionZ);
            RaisePropertyChanged(() => MaxX);
            RaisePropertyChanged(() => MaxY);
            RaisePropertyChanged(() => MaxZ);
            RaisePropertyChanged(() => MinX);
            RaisePropertyChanged(() => MinY);
            RaisePropertyChanged(() => MinZ);
            RaisePropertyChanged(() => BoundsEnabled);
            UpdateBacPlayer();
        }

        private void UpdateBacPlayer()
        {
            SceneManager.InvokeBacDataChangedEvent();
        }

        //Flags
        private bool GetHitboxFlags(ushort mask)
        {
            return (bacType.HitboxFlags & mask) != 0;
        }


        public void SetHitboxFlags(ushort mask, bool state)
        {
            ushort result = (ushort)((state) ? mask : 0x0000);
            ushort newFlags = (ushort)((bacType.I_18_b & (0xFFFF - mask)) | result);

            UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.HitboxFlags), bacType, bacType.HitboxFlags, newFlags, "Hitbox HitboxFlags"));
            bacType.HitboxFlags = newFlags;
        }
   
        public bool GetHitboxProperties1(ushort mask)
        {
            return (bacType.I_20 & mask) != 0;
        }

        public void SetHitboxProperties1(ushort mask, bool state)
        {
            ushort result = (ushort)((state) ? mask : 0x0000);
            ushort newFlags = (ushort)((bacType.I_20 & (0xFFFF - mask)) | result);

            UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.I_20), bacType, bacType.I_20, newFlags, "Hitbox PropertyFlags 1"));
            bacType.I_20 = newFlags;
        }

        /*
         
        //Never used in game
        public bool MatrixFlags_5
        {
            get
            {
                return GetMatrixFlagsB(0x01);
            }
            set
            {
                SetMatrixFlagsB(0x01, value);
                RaisePropertyChanged(() => MatrixFlags_5);
            }
        }
        public bool MatrixFlags_6
        {
            get
            {
                return GetMatrixFlagsB(0x02);
            }
            set
            {
                SetMatrixFlagsB(0x02, value);
                RaisePropertyChanged(() => MatrixFlags_6);
            }
        }
        public bool MatrixFlags_7
        {
            get
            {
                return GetMatrixFlagsB(0x04);
            }
            set
            {
                SetMatrixFlagsB(0x04, value);
                RaisePropertyChanged(() => MatrixFlags_7);
            }
        }
        public bool MatrixFlags_8
        {
            get
            {
                return GetMatrixFlagsB(0x08);
            }
            set
            {
                SetMatrixFlagsB(0x08, value);
                RaisePropertyChanged(() => MatrixFlags_8);
            }
        }



        //These are apparantly not even flags
        //HitboxProperties
        public bool HitboxProps_1
        {
            get
            {
                return GetHitboxProperties1(0x1);
            }
            set
            {
                SetHitboxProperties1(0x1, value);
                RaisePropertyChanged(() => HitboxProps_1);
            }
        }
        public bool HitboxProps_2
        {
            get
            {
                return GetHitboxProperties1(0x2);
            }
            set
            {
                SetHitboxProperties1(0x2, value);
                RaisePropertyChanged(() => HitboxProps_2);
            }
        }
        public bool HitboxProps_3
        {
            get
            {
                return GetHitboxProperties1(0x4);
            }
            set
            {
                SetHitboxProperties1(0x4, value);
                RaisePropertyChanged(() => HitboxProps_3);
            }
        }
        public bool HitboxProps_4
        {
            get
            {
                return GetHitboxProperties1(0x8);
            }
            set
            {
                SetHitboxProperties1(0x8, value);
                RaisePropertyChanged(() => HitboxProps_4);
            }
        }
        public bool HitboxProps_5
        {
            get
            {
                return GetHitboxProperties1(0x10);
            }
            set
            {
                SetHitboxProperties1(0x10, value);
                RaisePropertyChanged(() => HitboxProps_5);
            }
        }
        public bool HitboxProps_6
        {
            get
            {
                return GetHitboxProperties1(0x20);
            }
            set
            {
                SetHitboxProperties1(0x20, value);
                RaisePropertyChanged(() => HitboxProps_6);
            }
        }
        public bool HitboxProps_7
        {
            get
            {
                return GetHitboxProperties1(0x40);
            }
            set
            {
                SetHitboxProperties1(0x40, value);
                RaisePropertyChanged(() => HitboxProps_7);
            }
        }
        public bool HitboxProps_8
        {
            get
            {
                return GetHitboxProperties1(0x80);
            }
            set
            {
                SetHitboxProperties1(0x80, value);
                RaisePropertyChanged(() => HitboxProps_8);
            }
        }
        public bool HitboxProps_9
        {
            get
            {
                return GetHitboxProperties1(0x100);
            }
            set
            {
                SetHitboxProperties1(0x100, value);
                RaisePropertyChanged(() => HitboxProps_9);
            }
        }
        public bool HitboxProps_10
        {
            get
            {
                return GetHitboxProperties1(0x200);
            }
            set
            {
                SetHitboxProperties1(0x200, value);
                RaisePropertyChanged(() => HitboxProps_10);
            }
        }
        public bool HitboxProps_11
        {
            get
            {
                return GetHitboxProperties1(0x400);
            }
            set
            {
                SetHitboxProperties1(0x400, value);
                RaisePropertyChanged(() => HitboxProps_11);
            }
        }
        public bool HitboxProps_12
        {
            get
            {
                return GetHitboxProperties1(0x800);
            }
            set
            {
                SetHitboxProperties1(0x800, value);
                RaisePropertyChanged(() => HitboxProps_12);
            }
        }
        public bool HitboxProps_13
        {
            get
            {
                return GetHitboxProperties1(0x1000);
            }
            set
            {
                SetHitboxProperties1(0x1000, value);
                RaisePropertyChanged(() => HitboxProps_13);
            }
        }
        public bool HitboxProps_14
        {
            get
            {
                return GetHitboxProperties1(0x2000);
            }
            set
            {
                SetHitboxProperties1(0x2000, value);
                RaisePropertyChanged(() => HitboxProps_14);
            }
        }
        public bool HitboxProps_15
        {
            get
            {
                return GetHitboxProperties1(0x4000);
            }
            set
            {
                SetHitboxProperties1(0x4000, value);
                RaisePropertyChanged(() => HitboxProps_15);
            }
        }
        public bool HitboxProps_16
        {
            get
            {
                return GetHitboxProperties1(0x8000);
            }
            set
            {
                SetHitboxProperties1(0x8000, value);
                RaisePropertyChanged(() => HitboxProps_16);
            }
        }

        //HitboxProperties2
        public bool HitboxProps2_1
        {
            get
            {
                return GetHitboxProperties2(0x1);
            }
            set
            {
                SetHitboxProperties2(0x1, value);
                RaisePropertyChanged(() => HitboxProps2_1);
            }
        }
        public bool HitboxProps2_2
        {
            get
            {
                return GetHitboxProperties2(0x2);
            }
            set
            {
                SetHitboxProperties2(0x2, value);
                RaisePropertyChanged(() => HitboxProps2_2);
            }
        }
        public bool HitboxProps2_3
        {
            get
            {
                return GetHitboxProperties2(0x4);
            }
            set
            {
                SetHitboxProperties2(0x4, value);
                RaisePropertyChanged(() => HitboxProps2_3);
            }
        }
        public bool HitboxProps2_4
        {
            get
            {
                return GetHitboxProperties2(0x8);
            }
            set
            {
                SetHitboxProperties2(0x8, value);
                RaisePropertyChanged(() => HitboxProps2_4);
            }
        }
        public bool HitboxProps2_5
        {
            get
            {
                return GetHitboxProperties2(0x10);
            }
            set
            {
                SetHitboxProperties2(0x10, value);
                RaisePropertyChanged(() => HitboxProps2_5);
            }
        }
        public bool HitboxProps2_6
        {
            get
            {
                return GetHitboxProperties2(0x20);
            }
            set
            {
                SetHitboxProperties2(0x20, value);
                RaisePropertyChanged(() => HitboxProps2_6);
            }
        }
        public bool HitboxProps2_7
        {
            get
            {
                return GetHitboxProperties2(0x40);
            }
            set
            {
                SetHitboxProperties2(0x40, value);
                RaisePropertyChanged(() => HitboxProps2_7);
            }
        }
        public bool HitboxProps2_8
        {
            get
            {
                return GetHitboxProperties2(0x80);
            }
            set
            {
                SetHitboxProperties2(0x80, value);
                RaisePropertyChanged(() => HitboxProps2_8);
            }
        }
        public bool HitboxProps2_9
        {
            get
            {
                return GetHitboxProperties2(0x100);
            }
            set
            {
                SetHitboxProperties2(0x100, value);
                RaisePropertyChanged(() => HitboxProps2_9);
            }
        }
        public bool HitboxProps2_10
        {
            get
            {
                return GetHitboxProperties2(0x200);
            }
            set
            {
                SetHitboxProperties2(0x200, value);
                RaisePropertyChanged(() => HitboxProps2_10);
            }
        }
        public bool HitboxProps2_11
        {
            get
            {
                return GetHitboxProperties2(0x400);
            }
            set
            {
                SetHitboxProperties2(0x400, value);
                RaisePropertyChanged(() => HitboxProps2_11);
            }
        }
        public bool HitboxProps2_12
        {
            get
            {
                return GetHitboxProperties2(0x800);
            }
            set
            {
                SetHitboxProperties2(0x800, value);
                RaisePropertyChanged(() => HitboxProps2_12);
            }
        }
        public bool HitboxProps2_13
        {
            get
            {
                return GetHitboxProperties2(0x1000);
            }
            set
            {
                SetHitboxProperties2(0x1000, value);
                RaisePropertyChanged(() => HitboxProps2_13);
            }
        }
        public bool HitboxProps2_14
        {
            get
            {
                return GetHitboxProperties2(0x2000);
            }
            set
            {
                SetHitboxProperties2(0x2000, value);
                RaisePropertyChanged(() => HitboxProps2_14);
            }
        }
        public bool HitboxProps2_15
        {
            get
            {
                return GetHitboxProperties2(0x4000);
            }
            set
            {
                SetHitboxProperties2(0x4000, value);
                RaisePropertyChanged(() => HitboxProps2_15);
            }
        }
        public bool HitboxProps2_16
        {
            get
            {
                return GetHitboxProperties2(0x8000);
            }
            set
            {
                SetHitboxProperties2(0x8000, value);
                RaisePropertyChanged(() => HitboxProps2_16);
            }
        }

        */
    }
}
