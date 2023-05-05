using Xv2CoreLib.BCS;
using Xv2CoreLib.Resource.UndoRedo;
using GalaSoft.MvvmLight;
using XenoKit.Editor;
using XenoKit.Views;

namespace XenoKit.ViewModel.BCS
{
    public class BcsBodyViewModel : ObservableObject
    {
        private Body body;
        private BoneScale bodyScale;

        public string BoneName
        {
            get
            {
                return bodyScale.BoneName;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<ColorSelector>(nameof(bodyScale.BoneName), bodyScale, bodyScale.BoneName, value, "BodyScale Bone"), UndoGroup.BCS, BcsBodyView.UNDO_BODY_ARG);
                bodyScale.BoneName = value;
                RaisePropertyChanged(() => BoneName);
                bodyScale.RefreshValues();
                UpdateBoneScale();
            }
        }
        public float ScaleX
        {
            get
            {
                return bodyScale.ScaleX;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<ColorSelector>(nameof(bodyScale.ScaleX), bodyScale, bodyScale.ScaleX, value, "BodyScale X"), UndoGroup.BCS, BcsBodyView.UNDO_BODY_ARG);
                bodyScale.ScaleX = value;
                RaisePropertyChanged(() => ScaleX);
                bodyScale.RefreshValues();
                UpdateBoneScale();
            }
        }
        public float ScaleY
        {
            get
            {
                return bodyScale.ScaleY;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<ColorSelector>(nameof(bodyScale.ScaleY), bodyScale, bodyScale.ScaleY, value, "BodyScale Y"), UndoGroup.BCS, BcsBodyView.UNDO_BODY_ARG);
                bodyScale.ScaleY = value;
                RaisePropertyChanged(() => ScaleY);
                bodyScale.RefreshValues();
                UpdateBoneScale();
            }
        }
        public float ScaleZ
        {
            get
            {
                return bodyScale.ScaleZ;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<ColorSelector>(nameof(bodyScale.ScaleZ), bodyScale, bodyScale.ScaleZ, value, "BodyScale Z"), UndoGroup.BCS, BcsBodyView.UNDO_BODY_ARG);
                bodyScale.ScaleZ = value;
                RaisePropertyChanged(() => ScaleZ);
                bodyScale.RefreshValues();
                UpdateBoneScale();
            }
        }

        public BcsBodyViewModel(BoneScale bodyScale, Body body)
        {
            this.bodyScale = bodyScale;
            this.body = body;
        }

        public void UpdateProperties()
        {
            //Needed for updating properties when undo/redo is called
            RaisePropertyChanged(() => BoneName);
            RaisePropertyChanged(() => ScaleX);
            RaisePropertyChanged(() => ScaleY);
            RaisePropertyChanged(() => ScaleZ);
        }

        private void UpdateBoneScale()
        {
            if(Files.Instance.SelectedItem?.character?.Skeleton.HasBoneScale(body) == true)
            {
                Files.Instance.SelectedItem.character.Skeleton.UpdateBoneScale();
            }
        }
    }
}
