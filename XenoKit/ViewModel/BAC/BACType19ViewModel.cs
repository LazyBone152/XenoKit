using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type19;

namespace XenoKit.ViewModel.BAC
{
    public class BACType19ViewModel : ObservableObject
    {
        private BAC_Type19 bacType;

        public ushort AuraType
        {
            get
            {
                return (ushort)bacType.AuraType;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type19>(nameof(bacType.AuraType), bacType, bacType.AuraType, (AuraType)value, "AuraType"));
                bacType.AuraType = (AuraType)value;
                RaisePropertyChanged(() => AuraType);
            }
        }
        public bool Flag_DisableAura
        {
            get
            {
                return bacType.AuraFlags.HasFlag(AuraFlagsEnum.DisableAura);
            }
            set
            {
                SetAuraFlags(AuraFlagsEnum.DisableAura, value);
                RaisePropertyChanged(() => Flag_DisableAura);
            }
        }
        public bool Flag_Unk2
        {
            get
            {
                return bacType.AuraFlags.HasFlag(AuraFlagsEnum.Unk2);
            }
            set
            {
                SetAuraFlags(AuraFlagsEnum.Unk2, value);
                RaisePropertyChanged(() => Flag_Unk2);
            }
        }
        public bool Flag_Unk3
        {
            get
            {
                return bacType.AuraFlags.HasFlag(AuraFlagsEnum.Unk3);
            }
            set
            {
                SetAuraFlags(AuraFlagsEnum.Unk3, value);
                RaisePropertyChanged(() => Flag_Unk3);
            }
        }
        public bool Flag_Unk4
        {
            get
            {
                return bacType.AuraFlags.HasFlag(AuraFlagsEnum.Unk4);
            }
            set
            {
                SetAuraFlags(AuraFlagsEnum.Unk4, value);
                RaisePropertyChanged(() => Flag_Unk4);
            }
        }

        public BACType19ViewModel(BAC_Type19 _bacType)
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
            RaisePropertyChanged(() => AuraType);
            RaisePropertyChanged(() => Flag_DisableAura);
            RaisePropertyChanged(() => Flag_Unk2);
            RaisePropertyChanged(() => Flag_Unk3);
            RaisePropertyChanged(() => Flag_Unk4);
        }

        private void SetAuraFlags(AuraFlagsEnum flag, bool state)
        {
            var newFlag = bacType.AuraFlags.SetFlag(flag, state);

            if (bacType.AuraFlags != newFlag)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type19>(nameof(BAC_Type19.AuraFlags), bacType, bacType.AuraFlags, newFlag, "AuraFlags"));
                bacType.AuraFlags = newFlag;
            }
        }

    }
}
