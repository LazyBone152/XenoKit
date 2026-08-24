using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType31ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type31 bacType;

        public int I_08
        {
            get => bacType.I_08;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type31>(nameof(bacType.I_08), bacType, bacType.I_08, value, "ProjectileCallback I_08"));
                bacType.I_08 = value;
                RaisePropertyChanged(() => I_08);
            }
        }
        public int I_12
        {
            get => bacType.I_12;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type31>(nameof(bacType.I_12), bacType, bacType.I_12, value, "ProjectileCallback I_12"));
                bacType.I_12 = value;
                RaisePropertyChanged(() => I_12);
            }
        }
        public ushort BACEntryPass
        {
            get => bacType.I_16;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type31>(nameof(bacType.I_16), bacType, bacType.I_16, value, "ProjectileCallback BACEntryPass"));
                bacType.I_16 = value;
                RaisePropertyChanged(() => BACEntryPass);
            }
        }
        public byte I_18
        {
            get => bacType.I_18a;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type31>(nameof(bacType.I_18a), bacType, bacType.I_18a, value, "ProjectileCallback I_18"));
                bacType.I_18a = value;
                RaisePropertyChanged(() => I_18);
            }
        }
        public ushort SkillType
        {
            get
            {
                return (ushort)bacType.SkillType;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type8>(nameof(bacType.SkillType), bacType, bacType.SkillType, (BAC_Type8.EepkTypeEnum)value, "ProjectileCallback SkillType"));
                bacType.SkillType = (BAC_Type8.EepkTypeEnum)value;
                RaisePropertyChanged(() => SkillType);
                bacType.RefreshType();
            }
        }
        public ushort SkillID
        {
            get => bacType.I_20;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type31>(nameof(bacType.I_20), bacType, bacType.I_20, value, "ProjectileCallback SkillID"));
                bacType.I_20 = value;
                RaisePropertyChanged(() => SkillID);
            }
        }
        public ushort I_22
        {
            get => bacType.I_22;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type31>(nameof(bacType.I_22), bacType, bacType.I_22, value, "ProjectileCallback I_22"));
                bacType.I_22 = value;
                RaisePropertyChanged(() => I_22);
            }
        }

        public float HitboxSize
        {
            get => bacType.F_24;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type31>(nameof(bacType.F_24), bacType, bacType.F_24, value, "ProjectileCallback Hitbox Size"));
                bacType.F_24 = value;
                RaisePropertyChanged(() => HitboxSize);
            }
        }
        public float F_28
        {
            get => bacType.F_28;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type31>(nameof(bacType.F_28), bacType, bacType.F_28, value, "ProjectileCallback F_28"));
                bacType.F_28 = value;
                RaisePropertyChanged(() => F_28);
            }
        }
        public int I_32
        {
            get => bacType.I_32;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type31>(nameof(bacType.I_32), bacType, bacType.I_32, value, "ProjectileCallback I_32"));
                bacType.I_32 = value;
                RaisePropertyChanged(() => I_32);
            }
        }
        public int I_36
        {
            get => bacType.I_36;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type31>(nameof(bacType.I_36), bacType, bacType.I_36, value, "ProjectileCallback I_36"));
                bacType.I_36 = value;
                RaisePropertyChanged(() => I_36);
            }
        }
        public int I_40
        {
            get => bacType.I_40;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type31>(nameof(bacType.I_40), bacType, bacType.I_40, value, "ProjectileCallback I_40"));
                bacType.I_40 = value;
                RaisePropertyChanged(() => I_40);
            }
        }
        public int I_44
        {
            get => bacType.I_44;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type31>(nameof(bacType.I_44), bacType, bacType.I_44, value, "ProjectileCallback I_44"));
                bacType.I_44 = value;
                RaisePropertyChanged(() => I_44);
            }
        }
        public int I_48
        {
            get => bacType.I_48;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type31>(nameof(bacType.I_48), bacType, bacType.I_48, value, "ProjectileCallback I_48"));
                bacType.I_48 = value;
                RaisePropertyChanged(() => I_48);
            }
        }
        public int I_52
        {
            get => bacType.I_52;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type31>(nameof(bacType.I_52), bacType, bacType.I_52, value, "ProjectileCallback I_52"));
                bacType.I_52 = value;
                RaisePropertyChanged(() => I_52);
            }
        }
        public int I_56
        {
            get => bacType.I_56;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type31>(nameof(bacType.I_56), bacType, bacType.I_56, value, "ProjectileCallback I_56"));
                bacType.I_56 = value;
                RaisePropertyChanged(() => I_56);
            }
        }
        public int I_60
        {
            get => bacType.I_60;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type31>(nameof(bacType.I_60), bacType, bacType.I_60, value, "ProjectileCallback I_60"));
                bacType.I_60 = value;
                RaisePropertyChanged(() => I_60);
            }
        }

        public BACType31ViewModel(BAC_Type31 _bacType)
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
            RaisePropertyChanged(() => I_08);
            RaisePropertyChanged(() => I_12);
            RaisePropertyChanged(() => BACEntryPass);
            RaisePropertyChanged(() => I_18);
            RaisePropertyChanged(() => SkillType);
            RaisePropertyChanged(() => SkillID);
            RaisePropertyChanged(() => I_22);
            RaisePropertyChanged(() => HitboxSize);
            RaisePropertyChanged(() => F_28);
            RaisePropertyChanged(() => I_32);
            RaisePropertyChanged(() => I_36);
            RaisePropertyChanged(() => I_40);
            RaisePropertyChanged(() => I_44);
            RaisePropertyChanged(() => I_48);
            RaisePropertyChanged(() => I_52);
            RaisePropertyChanged(() => I_56);
            RaisePropertyChanged(() => I_60);
        }

    }
}
