using GalaSoft.MvvmLight;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type16;

namespace XenoKit.ViewModel.BAC
{
    public class BACType16ViewModel : ObservableObject
    {
        private BAC_Type16 bacType;

        public ushort BpeID
        {
            get
            {
                return bacType.BpeIndex;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type16>(nameof(bacType.BpeIndex), bacType, bacType.BpeIndex, value, "ScreenEffect BpeID"));
                bacType.BpeIndex = value;
                RaisePropertyChanged(() => BpeID);
            }
        }
        public BoneLinks Bone
        {
            get
            {
                return bacType.Bone;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type16>(nameof(bacType.Bone), bacType, bacType.Bone, value, "ScreenEffect Bone"));
                bacType.Bone = value;
                RaisePropertyChanged(() => Bone);
            }
        }

        public float F_20
        {
            get
            {
                return bacType.F_20;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type16>(nameof(bacType.F_20), bacType, bacType.F_20, value, "ScreenEffect F_20"));
                bacType.F_20 = value;
                RaisePropertyChanged(() => F_20);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type16>(nameof(bacType.F_24), bacType, bacType.F_24, value, "ScreenEffect F_24"));
                bacType.F_24 = value;
                RaisePropertyChanged(() => F_24);
            }
        }
        public float F_28
        {
            get
            {
                return bacType.F_28;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type16>(nameof(bacType.F_28), bacType, bacType.F_28, value, "ScreenEffect F_28"));
                bacType.F_28 = value;
                RaisePropertyChanged(() => F_28);
            }
        }

        //Flags
        public bool Flag_Unk1
        {
            get
            {
                return bacType.ScreenEffectFlags.HasFlag(ScreenEffectFlagsEnum.Unk1);
            }
            set
            {
                SetScreenEffectFlag(ScreenEffectFlagsEnum.Unk1, value);
                RaisePropertyChanged(() => Flag_Unk1);
            }
        }
        public bool Flag_DisableEffect
        {
            get
            {
                return bacType.ScreenEffectFlags.HasFlag(ScreenEffectFlagsEnum.DisableEffect);
            }
            set
            {
                SetScreenEffectFlag(ScreenEffectFlagsEnum.DisableEffect, value);
                RaisePropertyChanged(() => Flag_DisableEffect);
            }
        }
        public bool Flag_Unk4
        {
            get
            {
                return bacType.ScreenEffectFlags.HasFlag(ScreenEffectFlagsEnum.Unk4);
            }
            set
            {
                SetScreenEffectFlag(ScreenEffectFlagsEnum.Unk4, value);
                RaisePropertyChanged(() => Flag_Unk4);
            }
        }
        public bool Flag_Unk5
        {
            get
            {
                return bacType.ScreenEffectFlags.HasFlag(ScreenEffectFlagsEnum.Unk5);
            }
            set
            {
                SetScreenEffectFlag(ScreenEffectFlagsEnum.Unk5, value);
                RaisePropertyChanged(() => Flag_Unk5);
            }
        }


        public BACType16ViewModel(BAC_Type16 _bacType)
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
            RaisePropertyChanged(() => BpeID);
            RaisePropertyChanged(() => Bone);
            RaisePropertyChanged(() => F_20);
            RaisePropertyChanged(() => F_24);
            RaisePropertyChanged(() => F_28);
            RaisePropertyChanged(() => Flag_Unk1);
            RaisePropertyChanged(() => Flag_Unk4);
            RaisePropertyChanged(() => Flag_Unk5);
            RaisePropertyChanged(() => Flag_DisableEffect);
        }

        private void SetScreenEffectFlag(ScreenEffectFlagsEnum flag, bool state)
        {
            var newFlag = bacType.ScreenEffectFlags.SetFlag(flag, state);

            if (bacType.ScreenEffectFlags != newFlag)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type16>(nameof(BAC_Type16.ScreenEffectFlags), bacType, bacType.ScreenEffectFlags, newFlag, "ScreenEffectFlags"));
                bacType.ScreenEffectFlags = newFlag;
            }
        }
    }
}
