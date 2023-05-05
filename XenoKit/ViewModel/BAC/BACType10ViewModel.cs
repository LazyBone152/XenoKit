using GalaSoft.MvvmLight;
using XenoKit.Editor;
using XenoKit.Engine;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.EAN;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type10;

namespace XenoKit.ViewModel.BAC
{
    public class BACType10ViewModel : ObservableObject
    {

        private BAC_Type10 bacType;

        public ushort EanType
        {
            get
            {
                return (ushort)bacType.Ean_Type;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.Ean_Type), bacType, bacType.Ean_Type, (BAC_Type10.EanType)value, "Camera EanType"));
                bacType.Ean_Type = (BAC_Type10.EanType)value;
                RaisePropertyChanged(() => EanType);
                RaisePropertyChanged(() => SpecifiedEan);
                RaisePropertyChanged(() => UseEanList);
                RaisePropertyChanged(() => EanIndex);
                UpdateBacPlayer();

            }
        }
        public ushort EanIndex
        {
            get
            {
                return bacType.EanIndex;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.EanIndex), bacType, bacType.EanIndex, value, "Camera Ean Index"));
                bacType.EanIndex = value;
                RaisePropertyChanged(() => EanIndex);
                UpdateBacPlayer();

            }
        }
        public BoneLinks BoneToFocusOn
        {
            get
            {
                return bacType.BoneLink;
            }
            set
            {
                if (bacType.BoneLink != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.BoneLink), bacType, bacType.BoneLink, value, "BoneToFocusOn"));
                    bacType.BoneLink = value;
                    RaisePropertyChanged(() => BoneToFocusOn);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort StartFrame
        {
            get
            {
                return bacType.StartFrame;
            }
            set
            {
                if (bacType.StartFrame != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.StartFrame), bacType, bacType.StartFrame, value, "Camera StartFrame"));
                    bacType.StartFrame = value;
                    RaisePropertyChanged(() => StartFrame);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort I_16
        {
            get
            {
                return bacType.I_16;
            }
            set
            {
                if (bacType.I_16 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("I_16", bacType, bacType.I_16, value, "Camera I_16"));
                    bacType.I_16 = value;
                    RaisePropertyChanged(() => I_16);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort GlobalModiferDuration
        {
            get
            {
                return bacType.GlobalModiferDuration;
            }
            set
            {
                if (bacType.GlobalModiferDuration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("I_18", bacType, bacType.GlobalModiferDuration, value, "Camera GlobalModiferDuration"));
                    bacType.GlobalModiferDuration = value;
                    RaisePropertyChanged(() => GlobalModiferDuration);
                    UpdateBacPlayer();
                }
            }
        }
        public float PositionX
        {
            get
            {
                return bacType.F_40;
            }
            set
            {
                if (bacType.F_40 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("F_40", bacType, bacType.F_40, value, "Camera PositionX"));
                    bacType.F_40 = value;
                    RaisePropertyChanged(() => PositionX);
                    UpdateBacPlayer();
                }
            }
        }
        public float PositionY
        {
            get
            {
                return bacType.F_44;
            }
            set
            {
                if (bacType.F_44 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("F_44", bacType, bacType.F_44, value, "Camera PositionY"));
                    bacType.F_44 = value;
                    RaisePropertyChanged(() => PositionY);
                    UpdateBacPlayer();
                }
            }
        }
        public float PositionZ
        {
            get
            {
                return bacType.F_20;
            }
            set
            {
                if (bacType.F_20 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("F_20", bacType, bacType.F_20, value, "Camera PositionZ"));
                    bacType.F_20 = value;
                    RaisePropertyChanged(() => PositionZ);
                    UpdateBacPlayer();
                }
            }
        }
        public float RotationX
        {
            get
            {
                return bacType.F_32;
            }
            set
            {
                if (bacType.F_32 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("F_32", bacType, bacType.F_32, value, "Camera RotationX"));
                    bacType.F_32 = value;
                    RaisePropertyChanged(() => RotationX);
                    UpdateBacPlayer();
                }
            }
        }
        public float RotationY
        {
            get
            {
                return bacType.F_36;
            }
            set
            {
                if (bacType.F_36 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("F_36", bacType, bacType.F_36, value, "Camera RotationY"));
                    bacType.F_36 = value;
                    RaisePropertyChanged(() => RotationY);
                    UpdateBacPlayer();
                }
            }
        }
        public float RotationZ
        {
            get
            {
                return bacType.F_52;
            }
            set
            {
                if (bacType.F_52 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("F_52", bacType, bacType.F_52, value, "Camera RotationZ"));
                    bacType.F_52 = value;
                    RaisePropertyChanged(() => RotationZ);
                    UpdateBacPlayer();
                }
            }
        }
        public float DisplacementXZ
        {
            get
            {
                return bacType.F_24;
            }
            set
            {
                if (bacType.F_24 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("F_24", bacType, bacType.F_24, value, "Camera DisplacementXZ"));
                    bacType.F_24 = value;
                    RaisePropertyChanged(() => DisplacementXZ);
                    UpdateBacPlayer();
                }
            }
        }
        public float DisplacementZY
        {
            get
            {
                return bacType.F_28;
            }
            set
            {
                if (bacType.F_28 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("F_28", bacType, bacType.F_28, value, "Camera DisplacementZY"));
                    bacType.F_28 = value;
                    RaisePropertyChanged(() => DisplacementZY);
                    UpdateBacPlayer();
                }
            }
        }
        public float FieldOfView
        {
            get
            {
                return bacType.F_48;
            }
            set
            {
                if (bacType.F_48 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("F_48", bacType, bacType.F_48, value, "Camera FieldOfView"));
                    bacType.F_48 = value;
                    RaisePropertyChanged(() => FieldOfView);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort PositionDurationX
        {
            get
            {
                return bacType.I_66;
            }
            set
            {
                if (bacType.I_66 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("I_66", bacType, bacType.I_66, value, "Camera PositionDurationX"));
                    bacType.I_66 = value;
                    RaisePropertyChanged(() => PositionDurationX);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort PositionDurationY
        {
            get
            {
                return bacType.I_68;
            }
            set
            {
                if (bacType.I_68 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("I_68", bacType, bacType.I_68, value, "Camera PositionDurationY"));
                    bacType.I_68 = value;
                    RaisePropertyChanged(() => PositionDurationY);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort PositionDurationZ
        {
            get
            {
                return bacType.I_56;
            }
            set
            {
                if (bacType.I_56 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("I_56", bacType, bacType.I_56, value, "Camera PositionDurationZ"));
                    bacType.I_56 = value;
                    RaisePropertyChanged(() => PositionDurationZ);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort RotationDurationX
        {
            get
            {
                return bacType.I_62;
            }
            set
            {
                if (bacType.I_62 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("I_62", bacType, bacType.I_62, value, "Camera RotationDurationX"));
                    bacType.I_62 = value;
                    RaisePropertyChanged(() => RotationDurationX);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort RotationDurationY
        {
            get
            {
                return bacType.I_64;
            }
            set
            {
                if (bacType.I_64 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("I_64", bacType, bacType.I_64, value, "Camera RotationDurationY"));
                    bacType.I_64 = value;
                    RaisePropertyChanged(() => RotationDurationY);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort RotationDurationZ
        {
            get
            {
                return bacType.I_72;
            }
            set
            {
                if (bacType.I_72 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("I_72", bacType, bacType.I_72, value, "Camera RotationDurationZ"));
                    bacType.I_72 = value;
                    RaisePropertyChanged(() => RotationDurationZ);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort DisplacementDurationXZ
        {
            get
            {
                return bacType.I_58;
            }
            set
            {
                if (bacType.I_58 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("I_58", bacType, bacType.I_58, value, "Camera DisplacementDurationXZ"));
                    bacType.I_58 = value;
                    RaisePropertyChanged(() => DisplacementDurationXZ);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort DisplacementDurationZY
        {
            get
            {
                return bacType.I_60;
            }
            set
            {
                if (bacType.I_60 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("I_60", bacType, bacType.I_60, value, "Camera DisplacementDurationZY"));
                    bacType.I_60 = value;
                    RaisePropertyChanged(() => DisplacementDurationZY);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort FieldOfViewDuration
        {
            get
            {
                return bacType.I_70;
            }
            set
            {
                if (bacType.I_70 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("I_70", bacType, bacType.I_70, value, "Camera FieldOfViewDuration"));
                    bacType.I_70 = value;
                    RaisePropertyChanged(() => FieldOfViewDuration);
                    UpdateBacPlayer();
                }
            }
        }


        //Flags
        public bool EnableTransformModifers
        {
            get
            {
                return bacType.I_74_7;
            }
            set
            {
                if (bacType.I_74_7 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("I_74_7", bacType, bacType.I_74_7, value, "Camera EnableTransformModifers"));
                    bacType.I_74_7 = value;
                    RaisePropertyChanged(() => EnableTransformModifers);
                    UpdateBacPlayer();
                }
            }
        }
        public bool EnableCameraForAllPlayers
        {
            get
            {
                return bacType.I_74_0;
            }
            set
            {
                if (bacType.I_74_0 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("I_74_0", bacType, bacType.I_74_0, value, "Camera EnableCameraForAllPlayers"));
                    bacType.I_74_0 = value;
                    RaisePropertyChanged(() => EnableCameraForAllPlayers);
                    UpdateBacPlayer();
                }
            }
        }
        public bool Flag_74_1
        {
            get
            {
                return bacType.I_74_1;
            }
            set
            {
                if (bacType.I_74_1 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("I_74_1", bacType, bacType.I_74_1, value, "Camera Flag_74_1"));
                    bacType.I_74_1 = value;
                    RaisePropertyChanged(() => Flag_74_1);
                    UpdateBacPlayer();
                }
            }
        }
        public bool FocusOnTarget
        {
            get
            {
                return bacType.I_74_2;
            }
            set
            {
                if (bacType.I_74_2 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("I_74_2", bacType, bacType.I_74_2, value, "Camera FocusOnTarget"));
                    bacType.I_74_2 = value;
                    RaisePropertyChanged(() => FocusOnTarget);
                    UpdateBacPlayer();
                }
            }
        }
        public bool UseCharacterSpecificCameraEan
        {
            get
            {
                return bacType.I_74_3;
            }
            set
            {
                if (bacType.I_74_3 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("I_74_3", bacType, bacType.I_74_3, value, "Camera UseCharacterSpecificCameraEan"));
                    bacType.I_74_3 = value;
                    RaisePropertyChanged(() => UseCharacterSpecificCameraEan);
                    UpdateBacPlayer();
                }
            }
        }
        public bool Flag_74_4
        {
            get
            {
                return bacType.I_74_4;
            }
            set
            {
                if (bacType.I_74_4 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("I_74_4", bacType, bacType.I_74_4, value, "Camera Flag_74_4"));
                    bacType.I_74_4 = value;
                    RaisePropertyChanged(() => Flag_74_4);
                    UpdateBacPlayer();
                }
            }
        }
        public bool Flag_74_5
        {
            get
            {
                return bacType.I_74_5;
            }
            set
            {
                if (bacType.I_74_5 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("I_74_5", bacType, bacType.I_74_5, value, "Camera Flag_74_5"));
                    bacType.I_74_5 = value;
                    RaisePropertyChanged(() => Flag_74_5);
                    UpdateBacPlayer();
                }
            }
        }
        public bool DontOverrideActiveCameras
        {
            get
            {
                return bacType.I_74_6;
            }
            set
            {
                if (bacType.I_74_6 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>("I_74_6", bacType, bacType.I_74_6, value, "Camera DontOverrideActiveCameras"));
                    bacType.I_74_6 = value;
                    RaisePropertyChanged(() => DontOverrideActiveCameras);
                    UpdateBacPlayer();
                }
            }
        }

        public bool CameraFlags_Unk9
        {
            get
            {
                return bacType.cameraFlags2.HasFlag(BAC_Type10.CameraFlags2.Unk9);
            }
            set
            {
                CameraFlags2 flags = bacType.cameraFlags2.SetFlag(BAC_Type10.CameraFlags2.Unk9, value);

                if (flags != bacType.cameraFlags2)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.cameraFlags2), bacType, bacType.cameraFlags2, flags, "CameraFlags Unk9"));
                    bacType.cameraFlags2 = flags;
                    RaisePropertyChanged(() => CameraFlags_Unk9);
                }
            }
        }
        public bool CameraFlags_Unk10
        {
            get
            {
                return bacType.cameraFlags2.HasFlag(BAC_Type10.CameraFlags2.Unk10);
            }
            set
            {
                CameraFlags2 flags = bacType.cameraFlags2.SetFlag(BAC_Type10.CameraFlags2.Unk10, value);

                if (flags != bacType.cameraFlags2)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.cameraFlags2), bacType, bacType.cameraFlags2, flags, "CameraFlags Unk10"));
                    bacType.cameraFlags2 = flags;
                    RaisePropertyChanged(() => CameraFlags_Unk10);
                }
            }
        }
        public bool CameraFlags_Unk11
        {
            get
            {
                return bacType.cameraFlags2.HasFlag(BAC_Type10.CameraFlags2.Unk11);
            }
            set
            {
                CameraFlags2 flags = bacType.cameraFlags2.SetFlag(BAC_Type10.CameraFlags2.Unk11, value);

                if (flags != bacType.cameraFlags2)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.cameraFlags2), bacType, bacType.cameraFlags2, flags, "CameraFlags Unk11"));
                    bacType.cameraFlags2 = flags;
                    RaisePropertyChanged(() => CameraFlags_Unk11);
                }
            }
        }
        public bool CameraFlags_Unk12
        {
            get
            {
                return bacType.cameraFlags2.HasFlag(BAC_Type10.CameraFlags2.Unk12);
            }
            set
            {
                CameraFlags2 flags = bacType.cameraFlags2.SetFlag(BAC_Type10.CameraFlags2.Unk12, value);

                if (flags != bacType.cameraFlags2)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.cameraFlags2), bacType, bacType.cameraFlags2, flags, "CameraFlags Unk12"));
                    bacType.cameraFlags2 = flags;
                    RaisePropertyChanged(() => CameraFlags_Unk12);
                }
            }
        }
        public bool CameraFlags_Unk13
        {
            get
            {
                return bacType.cameraFlags2.HasFlag(BAC_Type10.CameraFlags2.Unk13);
            }
            set
            {
                CameraFlags2 flags = bacType.cameraFlags2.SetFlag(BAC_Type10.CameraFlags2.Unk13, value);

                if (flags != bacType.cameraFlags2)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.cameraFlags2), bacType, bacType.cameraFlags2, flags, "CameraFlags Unk13"));
                    bacType.cameraFlags2 = flags;
                    RaisePropertyChanged(() => CameraFlags_Unk13);
                }
            }
        }
        public bool CameraFlags_Unk14
        {
            get
            {
                return bacType.cameraFlags2.HasFlag(BAC_Type10.CameraFlags2.Unk14);
            }
            set
            {
                CameraFlags2 flags = bacType.cameraFlags2.SetFlag(BAC_Type10.CameraFlags2.Unk14, value);

                if (flags != bacType.cameraFlags2)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.cameraFlags2), bacType, bacType.cameraFlags2, flags, "CameraFlags Unk14"));
                    bacType.cameraFlags2 = flags;
                    RaisePropertyChanged(() => CameraFlags_Unk14);
                }
            }
        }
        public bool CameraFlags_Unk15
        {
            get
            {
                return bacType.cameraFlags2.HasFlag(BAC_Type10.CameraFlags2.Unk15);
            }
            set
            {
                CameraFlags2 flags = bacType.cameraFlags2.SetFlag(BAC_Type10.CameraFlags2.Unk15, value);

                if (flags != bacType.cameraFlags2)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.cameraFlags2), bacType, bacType.cameraFlags2, flags, "CameraFlags Unk15"));
                    bacType.cameraFlags2 = flags;
                    RaisePropertyChanged(() => CameraFlags_Unk15);
                }
            }
        }
        public bool CameraFlags_Unk16
        {
            get
            {
                return bacType.cameraFlags2.HasFlag(BAC_Type10.CameraFlags2.Unk16);
            }
            set
            {
                CameraFlags2 flags = bacType.cameraFlags2.SetFlag(BAC_Type10.CameraFlags2.Unk16, value);

                if (flags != bacType.cameraFlags2)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.cameraFlags2), bacType, bacType.cameraFlags2, flags, "CameraFlags Unk16"));
                    bacType.cameraFlags2 = flags;
                    RaisePropertyChanged(() => CameraFlags_Unk16);
                }
            }
        }


        //Ean
        public EAN_File SpecifiedEan
        {
            get
            {
                if ((BAC_Type10.EanType)EanType == BAC_Type10.EanType.Skill && Files.Instance.SelectedMove.MoveType != Move.Type.Skill) return null;
                return Files.Instance.GetCamEanFile((BAC_Type10.EanType)EanType, Files.Instance.SelectedMove, SceneManager.Actors[0], false, true);
            }
        }
        public bool UseEanList => SpecifiedEan != null;

        public BACType10ViewModel(BAC_Type10 _bacType)
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
            RaisePropertyChanged(() => EanType);
            RaisePropertyChanged(() => EanIndex);
            RaisePropertyChanged(() => StartFrame);
            RaisePropertyChanged(() => BoneToFocusOn);
            RaisePropertyChanged(() => I_16);
            RaisePropertyChanged(() => GlobalModiferDuration);
            RaisePropertyChanged(() => PositionX);
            RaisePropertyChanged(() => PositionY);
            RaisePropertyChanged(() => PositionZ);
            RaisePropertyChanged(() => RotationX);
            RaisePropertyChanged(() => RotationY);
            RaisePropertyChanged(() => RotationZ);
            RaisePropertyChanged(() => DisplacementXZ);
            RaisePropertyChanged(() => DisplacementZY);
            RaisePropertyChanged(() => FieldOfView);
            RaisePropertyChanged(() => PositionDurationX);
            RaisePropertyChanged(() => PositionDurationY);
            RaisePropertyChanged(() => PositionDurationZ);
            RaisePropertyChanged(() => RotationDurationX);
            RaisePropertyChanged(() => RotationDurationY);
            RaisePropertyChanged(() => RotationDurationZ);
            RaisePropertyChanged(() => DisplacementDurationXZ);
            RaisePropertyChanged(() => DisplacementDurationZY);
            RaisePropertyChanged(() => EnableTransformModifers);
            RaisePropertyChanged(() => EnableCameraForAllPlayers);
            RaisePropertyChanged(() => Flag_74_1);
            RaisePropertyChanged(() => FocusOnTarget);
            RaisePropertyChanged(() => UseCharacterSpecificCameraEan);
            RaisePropertyChanged(() => Flag_74_4);
            RaisePropertyChanged(() => Flag_74_5);
            RaisePropertyChanged(() => DontOverrideActiveCameras);
            RaisePropertyChanged(() => SpecifiedEan);
            RaisePropertyChanged(() => UseEanList);

            RaisePropertyChanged(() => CameraFlags_Unk9);
            RaisePropertyChanged(() => CameraFlags_Unk10);
            RaisePropertyChanged(() => CameraFlags_Unk11);
            RaisePropertyChanged(() => CameraFlags_Unk12);
            RaisePropertyChanged(() => CameraFlags_Unk13);
            RaisePropertyChanged(() => CameraFlags_Unk14);
            RaisePropertyChanged(() => CameraFlags_Unk15);
            RaisePropertyChanged(() => CameraFlags_Unk16);

            UpdateBacPlayer();
        }


        private void UpdateBacPlayer()
        {
            SceneManager.InvokeBacValuesChangedEvent();
        }
    }
}
