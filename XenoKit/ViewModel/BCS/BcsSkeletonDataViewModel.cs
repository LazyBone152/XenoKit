using GalaSoft.MvvmLight;
using Xv2CoreLib.BCS;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BCS
{
    public class BcsSkeletonDataViewModel : ObservableObject
    {
        private SkeletonData skeletonData;
        private Bone bone;

        public string BoneName
        {
            get
            {
                return bone.BoneName;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.BoneName), bone, bone.BoneName, bone, "SkeletonData BoneName"), UndoGroup.BCS, undoContext: skeletonData);
                bone.BoneName = value;
                RaisePropertyChanged(() => BoneName);
                bone.RefreshValues();
            }
        }
        public int I_00
        {
            get
            {
                return bone.I_00;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.I_00), bone, bone.I_00, bone, "SkeletonData I_00"), UndoGroup.BCS, undoContext: skeletonData);
                bone.I_00 = value;
                RaisePropertyChanged(() => I_00);
            }
        }
        public int I_04
        {
            get
            {
                return bone.I_04;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.I_04), bone, bone.I_04, bone, "SkeletonData I_04"), UndoGroup.BCS, undoContext: skeletonData);
                bone.I_04 = value;
                RaisePropertyChanged(() => I_04);
            }
        }
        public float F_12
        {
            get
            {
                return bone.F_12;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.F_12), bone, bone.F_12, bone, "SkeletonData F_12"), UndoGroup.BCS, undoContext: skeletonData);
                bone.F_12 = value;
                RaisePropertyChanged(() => F_12);
            }
        }
        public float F_16
        {
            get
            {
                return bone.F_16;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.F_16), bone, bone.F_16, bone, "SkeletonData F_16"), UndoGroup.BCS, undoContext: skeletonData);
                bone.F_16 = value;
                RaisePropertyChanged(() => F_16);
            }
        }
        public float F_20
        {
            get
            {
                return bone.F_20;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.F_20), bone, bone.F_20, bone, "SkeletonData F_20"), UndoGroup.BCS, undoContext: skeletonData);
                bone.F_20 = value;
                RaisePropertyChanged(() => F_20);
            }
        }
        public float F_24
        {
            get
            {
                return bone.F_24;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.F_24), bone, bone.F_24, bone, "SkeletonData F_24"), UndoGroup.BCS, undoContext: skeletonData);
                bone.F_24 = value;
                RaisePropertyChanged(() => F_24);
            }
        }
        public float F_28
        {
            get
            {
                return bone.F_28;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.F_28), bone, bone.F_28, bone, "SkeletonData F_28"), UndoGroup.BCS, undoContext: skeletonData);
                bone.F_28 = value;
                RaisePropertyChanged(() => F_28);
            }
        }
        public float F_32
        {
            get
            {
                return bone.F_32;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.F_32), bone, bone.F_32, bone, "SkeletonData F_32"), UndoGroup.BCS, undoContext: skeletonData);
                bone.F_32 = value;
                RaisePropertyChanged(() => F_32);
            }
        }
        public float F_36
        {
            get
            {
                return bone.F_36;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.F_36), bone, bone.F_36, bone, "SkeletonData F_36"), UndoGroup.BCS, undoContext: skeletonData);
                bone.F_36 = value;
                RaisePropertyChanged(() => F_36);
            }
        }
        public float F_40
        {
            get
            {
                return bone.F_40;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.F_40), bone, bone.F_40, bone, "SkeletonData F_40"), UndoGroup.BCS, undoContext: skeletonData);
                bone.F_40 = value;
                RaisePropertyChanged(() => F_40);
            }
        }

        public BcsSkeletonDataViewModel(Bone bone, SkeletonData skeletonData)
        {
            this.bone = bone;
            this.skeletonData = skeletonData;
        }

        public void UpdateProperties()
        {
            RaisePropertyChanged(nameof(BoneName));
            RaisePropertyChanged(nameof(I_00));
            RaisePropertyChanged(nameof(I_04));
            RaisePropertyChanged(nameof(F_12));
            RaisePropertyChanged(nameof(F_16));
            RaisePropertyChanged(nameof(F_20));
            RaisePropertyChanged(nameof(F_24));
            RaisePropertyChanged(nameof(F_28));
            RaisePropertyChanged(nameof(F_32));
            RaisePropertyChanged(nameof(F_36));
            RaisePropertyChanged(nameof(F_40));
        }
        
        public bool IsBone(Bone bone)
        {
            return this.bone == bone;
        }
    }
}
