using Xv2CoreLib.EMD;
using Xv2CoreLib.Resource.UndoRedo;
using GalaSoft.MvvmLight;

namespace XenoKit.ViewModel.EMD
{
    public class EmdAABBViewModel : ObservableObject
    {
        private EMD_AABB aabb;
        
        public float MinX
        {
            get
            {
                return aabb.MinX;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EMD_AABB>(nameof(EMD_AABB.MinX), aabb, aabb.MinX, value, "AABB MinX"), UndoGroup.EMD);
                aabb.MinX = value;
                RaisePropertyChanged(() => MinX);
            }
        }
        public float MinY
        {
            get
            {
                return aabb.MinY;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EMD_AABB>(nameof(EMD_AABB.MinY), aabb, aabb.MinY, value, "AABB MinY"), UndoGroup.EMD);
                aabb.MinY = value;
                RaisePropertyChanged(() => MinY);
            }
        }
        public float MinZ
        {
            get
            {
                return aabb.MinZ;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EMD_AABB>(nameof(EMD_AABB.MinZ), aabb, aabb.MinZ, value, "AABB MinZ"), UndoGroup.EMD);
                aabb.MinZ = value;
                RaisePropertyChanged(() => MinZ);
            }
        }
        public float MinW
        {
            get
            {
                return aabb.MinW;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EMD_AABB>(nameof(EMD_AABB.MinW), aabb, aabb.MinW, value, "AABB MinW"), UndoGroup.EMD);
                aabb.MinW = value;
                RaisePropertyChanged(() => MinW);
            }
        }
        public float MaxX
        {
            get
            {
                return aabb.MaxX;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EMD_AABB>(nameof(EMD_AABB.MaxX), aabb, aabb.MaxX, value, "AABB MaxX"), UndoGroup.EMD);
                aabb.MaxX = value;
                RaisePropertyChanged(() => MaxX);
            }
        }
        public float MaxY
        {
            get
            {
                return aabb.MaxY;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EMD_AABB>(nameof(EMD_AABB.MaxY), aabb, aabb.MaxY, value, "AABB MaxY"), UndoGroup.EMD);
                aabb.MaxY = value;
                RaisePropertyChanged(() => MaxY);
            }
        }
        public float MaxZ
        {
            get
            {
                return aabb.MaxZ;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EMD_AABB>(nameof(EMD_AABB.MaxZ), aabb, aabb.MaxZ, value, "AABB MaxZ"), UndoGroup.EMD);
                aabb.MaxZ = value;
                RaisePropertyChanged(() => MaxZ);
            }
        }
        public float MaxW
        {
            get
            {
                return aabb.MaxW;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EMD_AABB>(nameof(EMD_AABB.MaxW), aabb, aabb.MaxW, value, "AABB MaxW"), UndoGroup.EMD);
                aabb.MaxW = value;
                RaisePropertyChanged(() => MaxW);
            }
        }
        public float CenterX
        {
            get
            {
                return aabb.CenterX;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EMD_AABB>(nameof(EMD_AABB.CenterX), aabb, aabb.CenterX, value, "AABB CenterX"), UndoGroup.EMD);
                aabb.CenterX = value;
                RaisePropertyChanged(() => CenterX);
            }
        }
        public float CenterY
        {
            get
            {
                return aabb.CenterY;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EMD_AABB>(nameof(EMD_AABB.CenterY), aabb, aabb.CenterY, value, "AABB CenterY"), UndoGroup.EMD);
                aabb.CenterY = value;
                RaisePropertyChanged(() => CenterY);
            }
        }
        public float CenterZ
        {
            get
            {
                return aabb.CenterZ;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EMD_AABB>(nameof(EMD_AABB.CenterZ), aabb, aabb.CenterZ, value, "AABB CenterZ"), UndoGroup.EMD);
                aabb.CenterZ = value;
                RaisePropertyChanged(() => CenterZ);
            }
        }
        public float CenterW
        {
            get
            {
                return aabb.CenterW;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EMD_AABB>(nameof(EMD_AABB.CenterW), aabb, aabb.CenterW, value, "AABB CenterW"), UndoGroup.EMD);
                aabb.CenterW = value;
                RaisePropertyChanged(() => CenterW);
            }
        }



        public EmdAABBViewModel(EMD_AABB aabb)
        {
            this.aabb = aabb;
        }

        public void UpdateProperties()
        {
            RaisePropertyChanged(() => MinX);
            RaisePropertyChanged(() => MinY);
            RaisePropertyChanged(() => MinZ);
            RaisePropertyChanged(() => MinW);
            RaisePropertyChanged(() => MaxX);
            RaisePropertyChanged(() => MaxY);
            RaisePropertyChanged(() => MaxZ);
            RaisePropertyChanged(() => MaxW);
            RaisePropertyChanged(() => CenterX);
            RaisePropertyChanged(() => CenterY);
            RaisePropertyChanged(() => CenterZ);
            RaisePropertyChanged(() => CenterW);
        }
    }
}
