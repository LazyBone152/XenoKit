using GalaSoft.MvvmLight;
using Xv2CoreLib.BCS;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BCS
{
    public class BcsHeaderViewModel : ObservableObject
    {
        private BCS_File bcsFile;

        public Race Race
        {
            get
            {
                return bcsFile.Race;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bcsFile.Race), bcsFile, bcsFile.Race, value, "BCS Race"));
                bcsFile.Race = value;
                RaisePropertyChanged(() => Race);
            }
        }
        public Gender Gender
        {
            get
            {
                return bcsFile.Gender;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bcsFile.Gender), bcsFile, bcsFile.Gender, value, "BCS Gender"));
                bcsFile.Gender = value;
                RaisePropertyChanged(() => Gender);
            }
        }
        public float PositionY_Skill
        {
            get
            {
                return bcsFile.F_48[0];
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableArrayChange<float>(bcsFile.F_48, 0, bcsFile.F_48[0], value, "BCS Skill Pos Y"));
                bcsFile.F_48[0] = value;
                RaisePropertyChanged(() => PositionY_Skill);
            }
        }
        public float CameraPositionY
        {
            get
            {
                return bcsFile.F_48[1];
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableArrayChange<float>(bcsFile.F_48, 1, bcsFile.F_48[1], value, "BCS Camera pos Y"));
                bcsFile.F_48[1] = value;
                RaisePropertyChanged(() => CameraPositionY);
            }
        }
        public float F_56
        {
            get
            {
                return bcsFile.F_48[2];
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableArrayChange<float>(bcsFile.F_48, 2, bcsFile.F_48[2], value, "BCS F_56"));
                bcsFile.F_48[2] = value;
                RaisePropertyChanged(() => F_56);
            }
        }
        public float F_60
        {
            get
            {
                return bcsFile.F_48[3];
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableArrayChange<float>(bcsFile.F_48, 3, bcsFile.F_48[3], value, "BCS F_60"));
                bcsFile.F_48[3] = value;
                RaisePropertyChanged(() => F_60);
            }
        }
        public float F_64
        {
            get
            {
                return bcsFile.F_48[4];
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableArrayChange<float>(bcsFile.F_48, 4, bcsFile.F_48[4], value, "BCS F_64"));
                bcsFile.F_48[4] = value;
                RaisePropertyChanged(() => F_64);
            }
        }
        public float F_68
        {
            get
            {
                return bcsFile.F_48[5];
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableArrayChange<float>(bcsFile.F_48, 5, bcsFile.F_48[5], value, "BCS F_68"));
                bcsFile.F_48[5] = value;
                RaisePropertyChanged(() => F_68);
            }
        }
        public float F_72
        {
            get
            {
                return bcsFile.F_48[6];
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableArrayChange<float>(bcsFile.F_48, 6, bcsFile.F_48[6], value, "BCS F_72"));
                bcsFile.F_48[6] = value;
                RaisePropertyChanged(() => F_72);
            }
        }

        public BcsHeaderViewModel(BCS_File bcsFile)
        {
            this.bcsFile = bcsFile;
        }

        public void UpdateProperties()
        {
            RaisePropertyChanged(nameof(Race));
            RaisePropertyChanged(nameof(Gender));
            RaisePropertyChanged(nameof(PositionY_Skill));
            RaisePropertyChanged(nameof(CameraPositionY));
            RaisePropertyChanged(nameof(F_56));
            RaisePropertyChanged(nameof(F_60));
            RaisePropertyChanged(nameof(F_64));
            RaisePropertyChanged(nameof(F_68));
            RaisePropertyChanged(nameof(F_72));
        }
    }
}
