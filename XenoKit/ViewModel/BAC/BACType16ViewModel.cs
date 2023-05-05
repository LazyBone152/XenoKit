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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type16>(nameof(bacType.BpeIndex), bacType, bacType.BpeIndex, value, "PostEffect BpeID"));
                bacType.BpeIndex = value;
                RaisePropertyChanged(() => BpeID);
                bacType.RefreshType();
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type16>(nameof(bacType.BoneLink), bacType, bacType.BoneLink, value, "PostEffect Bone"));
                bacType.BoneLink = value;
                RaisePropertyChanged(() => Bone);
            }
        }

        public float PositionX
        {
            get
            {
                return bacType.PositionX;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type16>(nameof(bacType.PositionX), bacType, bacType.PositionX, value, "PostEffect F_20"));
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type16>(nameof(bacType.PositionY), bacType, bacType.PositionY, value, "PostEffect F_24"));
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type16>(nameof(bacType.PositionZ), bacType, bacType.PositionZ, value, "PostEffect F_28"));
                bacType.PositionZ = value;
                RaisePropertyChanged(() => PositionZ);
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
            RaisePropertyChanged(() => PositionX);
            RaisePropertyChanged(() => PositionY);
            RaisePropertyChanged(() => PositionZ);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type16>(nameof(BAC_Type16.ScreenEffectFlags), bacType, bacType.ScreenEffectFlags, newFlag, "PostEffectFlags"));
                bacType.ScreenEffectFlags = newFlag;
            }
        }
    }
}
