using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type7;

namespace XenoKit.ViewModel.BAC
{
    public class BACType7ViewModel : ObservableObject
    {
        private BAC_Type7 bacType;


        //Flags
        public bool Flag_Attacks
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.Attacks);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.Attacks, value);
                RaisePropertyChanged(() => Flag_Attacks);
            }
        }
        public bool Flag_Movement
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.Movement);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.Movement, value);
                RaisePropertyChanged(() => Flag_Movement);
            }
        }
        public bool Flag_DisableKiBlast
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.DisableKiBlastLink);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.DisableKiBlastLink, value);
                RaisePropertyChanged(() => Flag_DisableKiBlast);
            }
        }
        public bool Flag_Unk4
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.Unk4);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.Unk4, value);
                RaisePropertyChanged(() => Flag_Unk4);
            }
        }
        public bool Flag_Counters
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.Counters);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.Counters, value);
                RaisePropertyChanged(() => Flag_Counters);
            }
        }
        public bool Flag_Unk6
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.Unk6);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.Unk6, value);
                RaisePropertyChanged(() => Flag_Unk6);
            }
        }
        public bool Flag_Unk7
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.Unk7);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.Unk7, value);
                RaisePropertyChanged(() => Flag_Unk7);
            }
        }
        public bool Flag_BackHits
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.BackHits);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.BackHits, value);
                RaisePropertyChanged(() => Flag_BackHits);
            }
        }
        public bool Flag_Combos
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.Combos);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.Combos, value);
                RaisePropertyChanged(() => Flag_Combos);
            }
        }
        public bool Flag_Supers
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.Supers);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.Supers, value);
                RaisePropertyChanged(() => Flag_Supers);
            }
        }
        public bool Flag_UltimatesAndEvasives
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.UltimatesAndEvasives);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.UltimatesAndEvasives, value);
                RaisePropertyChanged(() => Flag_UltimatesAndEvasives);
            }
        }
        public bool Flag_ZVanish
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.ZVanish);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.ZVanish, value);
                RaisePropertyChanged(() => Flag_ZVanish);
            }
        }
        public bool Flag_KiBlasts
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.KiBlasts);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.KiBlasts, value);
                RaisePropertyChanged(() => Flag_KiBlasts);
            }
        }
        public bool Flag_Jump
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.Jump);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.Jump, value);
                RaisePropertyChanged(() => Flag_Jump);
            }
        }
        public bool Flag_Guard
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.Guard);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.Guard, value);
                RaisePropertyChanged(() => Flag_Guard);
            }
        }
        public bool Flag_FlyingAndStepDash
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.FlyingAndStepDash);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.FlyingAndStepDash, value);
                RaisePropertyChanged(() => Flag_FlyingAndStepDash);
            }
        }
        public bool Flag_Unk17
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.Unk17);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.Unk17, value);
                RaisePropertyChanged(() => Flag_Unk17);
            }
        }
        public bool Flag_Unk18
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.Unk18);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.Unk18, value);
                RaisePropertyChanged(() => Flag_Unk18);
            }
        }
        public bool Flag_Unk19
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.Unk19);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.Unk19, value);
                RaisePropertyChanged(() => Flag_Unk19);
            }
        }
        public bool Flag_Unk20
        {
            get
            {
                return bacType.LinkFlags.HasFlag(BcmCallbackFlagsEnum.Unk20);
            }
            set
            {
                SetLinkFlags(BcmCallbackFlagsEnum.Unk20, value);
                RaisePropertyChanged(() => Flag_Unk20);
            }
        }



        public BACType7ViewModel(BAC_Type7 _bacType)
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
            RaisePropertyChanged(() => Flag_Attacks);
            RaisePropertyChanged(() => Flag_Movement);
            RaisePropertyChanged(() => Flag_DisableKiBlast);
            RaisePropertyChanged(() => Flag_Unk4);
            RaisePropertyChanged(() => Flag_Counters);
            RaisePropertyChanged(() => Flag_Unk6);
            RaisePropertyChanged(() => Flag_Unk7);
            RaisePropertyChanged(() => Flag_BackHits);
            RaisePropertyChanged(() => Flag_Combos);
            RaisePropertyChanged(() => Flag_Supers);
            RaisePropertyChanged(() => Flag_UltimatesAndEvasives);
            RaisePropertyChanged(() => Flag_ZVanish);
            RaisePropertyChanged(() => Flag_KiBlasts);
            RaisePropertyChanged(() => Flag_Jump);
            RaisePropertyChanged(() => Flag_Guard);
            RaisePropertyChanged(() => Flag_FlyingAndStepDash);
            RaisePropertyChanged(() => Flag_Unk17);
            RaisePropertyChanged(() => Flag_Unk18);
            RaisePropertyChanged(() => Flag_Unk19);
            RaisePropertyChanged(() => Flag_Unk20);
        }

        private void SetLinkFlags(BcmCallbackFlagsEnum flag, bool state)
        {
            var newFlag = bacType.LinkFlags.SetFlag(flag, state);

            if (bacType.LinkFlags != newFlag)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type7>(nameof(BAC_Type7.LinkFlags), bacType, bacType.LinkFlags, newFlag, "BcmLinkFlags"));
                bacType.LinkFlags = newFlag;
            }
        }
    }
}
