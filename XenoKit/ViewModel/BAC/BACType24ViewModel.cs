using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type24;

namespace XenoKit.ViewModel.BAC
{
    public class BACType24ViewModel : ObservableObject
    {
        private BAC_Type24 bacType;

        //User
        public ushort I_12
        {
            get
            {
                return bacType.I_12;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type24>(nameof(bacType.I_12), bacType, bacType.I_12, value, "I_12"));
                bacType.I_12 = value;
                RaisePropertyChanged(() => I_12);
            }
        }
        public ushort I_14
        {
            get
            {
                return bacType.I_14;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type24>(nameof(bacType.I_14), bacType, bacType.I_14, value, "I_14"));
                bacType.I_14 = value;
                RaisePropertyChanged(() => I_14);
            }
        }
        public int I_16
        {
            get
            {
                return bacType.I_16;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type24>(nameof(bacType.I_16), bacType, bacType.I_16, value, "I_16"));
                bacType.I_16 = value;
                RaisePropertyChanged(() => I_16);
            }
        }
        public float InitiatorPositionX
        {
            get
            {
                return bacType.InitiatorPositionX;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type24>(nameof(bacType.InitiatorPositionX), bacType, bacType.InitiatorPositionX, value, "InitiatorPositionX"));
                bacType.InitiatorPositionX = value;
                RaisePropertyChanged(() => InitiatorPositionX);
            }
        }
        public float InitiatorPositionY
        {
            get
            {
                return bacType.InitiatorPositionY;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type24>(nameof(bacType.InitiatorPositionY), bacType, bacType.InitiatorPositionY, value, "InitiatorPositionY"));
                bacType.InitiatorPositionY = value;
                RaisePropertyChanged(() => InitiatorPositionY);
            }
        }
        public float InitiatorPositionZ
        {
            get
            {
                return bacType.InitiatorPositionZ;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type24>(nameof(bacType.InitiatorPositionZ), bacType, bacType.InitiatorPositionZ, value, "InitiatorPositionZ"));
                bacType.InitiatorPositionZ = value;
                RaisePropertyChanged(() => InitiatorPositionZ);
            }
        }

        //Partner
        public ushort I_32
        {
            get
            {
                return bacType.I_32;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type24>(nameof(bacType.I_32), bacType, bacType.I_32, value, "I_32"));
                bacType.I_32 = value;
                RaisePropertyChanged(() => I_32);
            }
        }
        public ushort I_34
        {
            get
            {
                return bacType.I_34;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type24>(nameof(bacType.I_34), bacType, bacType.I_34, value, "I_34"));
                bacType.I_34 = value;
                RaisePropertyChanged(() => I_34);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type24>(nameof(bacType.I_36), bacType, bacType.I_36, value, "I_36"));
                bacType.I_36 = value;
                RaisePropertyChanged(() => I_36);
            }
        }
        public float PartnerPositionX
        {
            get
            {
                return bacType.PartnerPositionX;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type24>(nameof(bacType.PartnerPositionX), bacType, bacType.PartnerPositionX, value, "PartnerPositionX"));
                bacType.PartnerPositionX = value;
                RaisePropertyChanged(() => PartnerPositionX);
            }
        }
        public float PartnerPositionY
        {
            get
            {
                return bacType.PartnerPositionY;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type24>(nameof(bacType.PartnerPositionY), bacType, bacType.PartnerPositionY, value, "PartnerPositionY"));
                bacType.PartnerPositionY = value;
                RaisePropertyChanged(() => PartnerPositionY);
            }
        }
        public float PartnerPositionZ
        {
            get
            {
                return bacType.PartnerPositionZ;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type24>(nameof(bacType.PartnerPositionZ), bacType, bacType.PartnerPositionZ, value, "PartnerPositionZ"));
                bacType.PartnerPositionZ = value;
                RaisePropertyChanged(() => PartnerPositionZ);
            }
        }

        //Other
        public ushort I_10
        {
            get
            {
                return bacType.I_10;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type24>(nameof(bacType.I_10), bacType, bacType.I_10, value, "I_10"));
                bacType.I_10 = value;
                RaisePropertyChanged(() => I_10);
            }
        }
        public ushort I_52
        {
            get
            {
                return bacType.I_52;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type24>(nameof(bacType.I_52), bacType, bacType.I_52, value, "I_52"));
                bacType.I_52 = value;
                RaisePropertyChanged(() => I_52);
            }
        }
        public ushort I_54
        {
            get
            {
                return bacType.I_54;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type24>(nameof(bacType.I_54), bacType, bacType.I_54, value, "I_54"));
                bacType.I_54 = value;
                RaisePropertyChanged(() => I_54);
            }
        }

        //Flags
        public bool Flag_Unk1
        {
            get
            {
                return bacType.DualSkillFlags.HasFlag(DualSkillHandlerFlagsEnum.Unk1);
            }
            set
            {
                SetDualSkillFlag(DualSkillHandlerFlagsEnum.Unk1, value);
                RaisePropertyChanged(() => Flag_Unk1);
            }
        }
        public bool Flag_Unk2
        {
            get
            {
                return bacType.DualSkillFlags.HasFlag(DualSkillHandlerFlagsEnum.Unk2);
            }
            set
            {
                SetDualSkillFlag(DualSkillHandlerFlagsEnum.Unk2, value);
                RaisePropertyChanged(() => Flag_Unk2);
            }
        }
        public bool Flag_Unk3
        {
            get
            {
                return bacType.DualSkillFlags.HasFlag(DualSkillHandlerFlagsEnum.Unk3);
            }
            set
            {
                SetDualSkillFlag(DualSkillHandlerFlagsEnum.Unk3, value);
                RaisePropertyChanged(() => Flag_Unk3);
            }
        }
        public bool Flag_Unk4
        {
            get
            {
                return bacType.DualSkillFlags.HasFlag(DualSkillHandlerFlagsEnum.Unk4);
            }
            set
            {
                SetDualSkillFlag(DualSkillHandlerFlagsEnum.Unk4, value);
                RaisePropertyChanged(() => Flag_Unk4);
            }
        }
        public bool Flag_Unk5
        {
            get
            {
                return bacType.DualSkillFlags.HasFlag(DualSkillHandlerFlagsEnum.Unk5);
            }
            set
            {
                SetDualSkillFlag(DualSkillHandlerFlagsEnum.Unk5, value);
                RaisePropertyChanged(() => Flag_Unk5);
            }
        }
        public bool Flag_Unk6
        {
            get
            {
                return bacType.DualSkillFlags.HasFlag(DualSkillHandlerFlagsEnum.Unk6);
            }
            set
            {
                SetDualSkillFlag(DualSkillHandlerFlagsEnum.Unk6, value);
                RaisePropertyChanged(() => Flag_Unk6);
            }
        }
        public bool Flag_Unk7
        {
            get
            {
                return bacType.DualSkillFlags.HasFlag(DualSkillHandlerFlagsEnum.Unk7);
            }
            set
            {
                SetDualSkillFlag(DualSkillHandlerFlagsEnum.Unk7, value);
                RaisePropertyChanged(() => Flag_Unk7);
            }
        }
        public bool Flag_Unk8
        {
            get
            {
                return bacType.DualSkillFlags.HasFlag(DualSkillHandlerFlagsEnum.Unk8);
            }
            set
            {
                SetDualSkillFlag(DualSkillHandlerFlagsEnum.Unk8, value);
                RaisePropertyChanged(() => Flag_Unk8);
            }
        }
        public bool Flag_Unk9
        {
            get
            {
                return bacType.DualSkillFlags.HasFlag(DualSkillHandlerFlagsEnum.Unk9);
            }
            set
            {
                SetDualSkillFlag(DualSkillHandlerFlagsEnum.Unk9, value);
                RaisePropertyChanged(() => Flag_Unk9);
            }
        }
        public bool Flag_Unk10
        {
            get
            {
                return bacType.DualSkillFlags.HasFlag(DualSkillHandlerFlagsEnum.Unk10);
            }
            set
            {
                SetDualSkillFlag(DualSkillHandlerFlagsEnum.Unk10, value);
                RaisePropertyChanged(() => Flag_Unk10);
            }
        }
        public bool Flag_Unk11
        {
            get
            {
                return bacType.DualSkillFlags.HasFlag(DualSkillHandlerFlagsEnum.Unk11);
            }
            set
            {
                SetDualSkillFlag(DualSkillHandlerFlagsEnum.Unk11, value);
                RaisePropertyChanged(() => Flag_Unk11);
            }
        }
        public bool Flag_Unk12
        {
            get
            {
                return bacType.DualSkillFlags.HasFlag(DualSkillHandlerFlagsEnum.Unk12);
            }
            set
            {
                SetDualSkillFlag(DualSkillHandlerFlagsEnum.Unk12, value);
                RaisePropertyChanged(() => Flag_Unk12);
            }
        }
        public bool Flag_Unk13
        {
            get
            {
                return bacType.DualSkillFlags.HasFlag(DualSkillHandlerFlagsEnum.Unk13);
            }
            set
            {
                SetDualSkillFlag(DualSkillHandlerFlagsEnum.Unk13, value);
                RaisePropertyChanged(() => Flag_Unk13);
            }
        }
        public bool Flag_Unk14
        {
            get
            {
                return bacType.DualSkillFlags.HasFlag(DualSkillHandlerFlagsEnum.Unk14);
            }
            set
            {
                SetDualSkillFlag(DualSkillHandlerFlagsEnum.Unk14, value);
                RaisePropertyChanged(() => Flag_Unk14);
            }
        }
        public bool Flag_Unk15
        {
            get
            {
                return bacType.DualSkillFlags.HasFlag(DualSkillHandlerFlagsEnum.Unk15);
            }
            set
            {
                SetDualSkillFlag(DualSkillHandlerFlagsEnum.Unk15, value);
                RaisePropertyChanged(() => Flag_Unk15);
            }
        }
        public bool Flag_Unk16
        {
            get
            {
                return bacType.DualSkillFlags.HasFlag(DualSkillHandlerFlagsEnum.Unk16);
            }
            set
            {
                SetDualSkillFlag(DualSkillHandlerFlagsEnum.Unk16, value);
                RaisePropertyChanged(() => Flag_Unk16);
            }
        }

        public BACType24ViewModel(BAC_Type24 _bacType)
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
            RaisePropertyChanged(() => I_12);
            RaisePropertyChanged(() => I_14);
            RaisePropertyChanged(() => I_16);
            RaisePropertyChanged(() => InitiatorPositionX);
            RaisePropertyChanged(() => InitiatorPositionY);
            RaisePropertyChanged(() => InitiatorPositionZ);
            RaisePropertyChanged(() => I_32);
            RaisePropertyChanged(() => I_34);
            RaisePropertyChanged(() => I_36);
            RaisePropertyChanged(() => PartnerPositionX);
            RaisePropertyChanged(() => PartnerPositionY);
            RaisePropertyChanged(() => PartnerPositionZ);
            RaisePropertyChanged(() => I_10);
            RaisePropertyChanged(() => I_52);
            RaisePropertyChanged(() => I_54);
            RaisePropertyChanged(() => Flag_Unk1);
            RaisePropertyChanged(() => Flag_Unk2);
            RaisePropertyChanged(() => Flag_Unk3);
            RaisePropertyChanged(() => Flag_Unk4);
            RaisePropertyChanged(() => Flag_Unk5);
            RaisePropertyChanged(() => Flag_Unk6);
            RaisePropertyChanged(() => Flag_Unk7);
            RaisePropertyChanged(() => Flag_Unk8);
            RaisePropertyChanged(() => Flag_Unk9);
            RaisePropertyChanged(() => Flag_Unk10);
            RaisePropertyChanged(() => Flag_Unk11);
            RaisePropertyChanged(() => Flag_Unk12);
            RaisePropertyChanged(() => Flag_Unk13);
            RaisePropertyChanged(() => Flag_Unk14);
            RaisePropertyChanged(() => Flag_Unk15);
            RaisePropertyChanged(() => Flag_Unk16);
        }

        private void SetDualSkillFlag(DualSkillHandlerFlagsEnum flag, bool state)
        {
            var newFlag = bacType.DualSkillFlags.SetFlag(flag, state);

            if (bacType.DualSkillFlags != newFlag)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type24>(nameof(BAC_Type24.DualSkillFlags), bacType, bacType.DualSkillFlags, newFlag, "DualSkillFlags"));
                bacType.DualSkillFlags = newFlag;
            }
        }

    }
}
