using GalaSoft.MvvmLight;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type14;

namespace XenoKit.ViewModel.BAC
{
    public class BACType14ViewModel : ObservableObject
    {
        private BAC_Type14 bacType;

        public bool Flag_Head
        {
            get
            {
                return bacType.ModificationFlags.HasFlag(AnimationModFlags.Head_HorVert);
            }
            set
            {
                SetBsaFlag(AnimationModFlags.Head_HorVert, value);
                RaisePropertyChanged(() => Flag_Head);
            }
        }
        public bool Flag_SpineVert
        {
            get
            {
                return bacType.ModificationFlags.HasFlag(AnimationModFlags.Spine_Vert);
            }
            set
            {
                SetBsaFlag(AnimationModFlags.Spine_Vert, value);
                RaisePropertyChanged(() => Flag_SpineVert);
            }
        }
        public bool Flag_SpineHor
        {
            get
            {
                return bacType.ModificationFlags.HasFlag(AnimationModFlags.Spine_Hor);
            }
            set
            {
                SetBsaFlag(AnimationModFlags.Spine_Hor, value);
                RaisePropertyChanged(() => Flag_SpineHor);
            }
        }
        public bool Flag_Unk4
        {
            get
            {
                return bacType.ModificationFlags.HasFlag(AnimationModFlags.Unk4);
            }
            set
            {
                SetBsaFlag(AnimationModFlags.Unk4, value);
                RaisePropertyChanged(() => Flag_Unk4);
            }
        }
        public bool Flag_Unk5
        {
            get
            {
                return bacType.ModificationFlags.HasFlag(AnimationModFlags.Unk5);
            }
            set
            {
                SetBsaFlag(AnimationModFlags.Unk5, value);
                RaisePropertyChanged(() => Flag_Unk5);
            }
        }
        public bool Flag_Unk6
        {
            get
            {
                return bacType.ModificationFlags.HasFlag(AnimationModFlags.Unk6);
            }
            set
            {
                SetBsaFlag(AnimationModFlags.Unk4, value);
                RaisePropertyChanged(() => Flag_Unk6);
            }
        }
        public bool Flag_Unk7
        {
            get
            {
                return bacType.ModificationFlags.HasFlag(AnimationModFlags.Unk7);
            }
            set
            {
                SetBsaFlag(AnimationModFlags.Unk4, value);
                RaisePropertyChanged(() => Flag_Unk7);
            }
        }
        public bool Flag_Unk8
        {
            get
            {
                return bacType.ModificationFlags.HasFlag(AnimationModFlags.Unk8);
            }
            set
            {
                SetBsaFlag(AnimationModFlags.Unk4, value);
                RaisePropertyChanged(() => Flag_Unk8);
            }
        }



        public BACType14ViewModel(BAC_Type14 _bacType)
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
            RaisePropertyChanged(() => Flag_Head);
            RaisePropertyChanged(() => Flag_SpineHor);
            RaisePropertyChanged(() => Flag_SpineVert);
            RaisePropertyChanged(() => Flag_Unk4);
            RaisePropertyChanged(() => Flag_Unk5);
            RaisePropertyChanged(() => Flag_Unk6);
            RaisePropertyChanged(() => Flag_Unk7);
            RaisePropertyChanged(() => Flag_Unk8);
        }


        private void SetBsaFlag(AnimationModFlags flag, bool state)
        {
            var newFlag = bacType.ModificationFlags.SetFlag(flag, state);

            if (bacType.ModificationFlags != newFlag)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type14>(nameof(BAC_Type14.ModificationFlags), bacType, bacType.ModificationFlags, newFlag, "ModificationFlags"));
                bacType.ModificationFlags = newFlag;
            }
        }

    }
}
