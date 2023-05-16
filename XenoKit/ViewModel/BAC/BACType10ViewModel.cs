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
                return (ushort)bacType.EanType;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.EanType), bacType, bacType.EanType, (BAC_Type10.EanTypeEnum)value, "Camera EanType"));
                bacType.EanType = (BAC_Type10.EanTypeEnum)value;
                RaisePropertyChanged(() => EanType);
                RaisePropertyChanged(() => SpecifiedEan);
                RaisePropertyChanged(() => UseEanList);
                RaisePropertyChanged(() => EanIndex);
                bacType.RefreshType();
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
                bacType.RefreshType();
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
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.I_16), bacType, bacType.I_16, value, "Camera I_16"));
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
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.GlobalModiferDuration), bacType, bacType.GlobalModiferDuration, value, "Camera GlobalModiferDuration"));
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
                return bacType.PositionX;
            }
            set
            {
                if (bacType.PositionX != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.PositionX), bacType, bacType.PositionX, value, "Camera PositionX"));
                    bacType.PositionX = value;
                    RaisePropertyChanged(() => PositionX);
                    UpdateBacPlayer();
                }
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
                if (bacType.PositionY != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.PositionY), bacType, bacType.PositionY, value, "Camera PositionY"));
                    bacType.PositionY = value;
                    RaisePropertyChanged(() => PositionY);
                    UpdateBacPlayer();
                }
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
                if (bacType.PositionZ != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.PositionZ), bacType, bacType.PositionZ, value, "Camera PositionZ"));
                    bacType.PositionZ = value;
                    RaisePropertyChanged(() => PositionZ);
                    UpdateBacPlayer();
                }
            }
        }
        public float RotationX
        {
            get
            {
                return bacType.RotationX;
            }
            set
            {
                if (bacType.RotationX != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.RotationX), bacType, bacType.RotationX, value, "Camera RotationX"));
                    bacType.RotationX = value;
                    RaisePropertyChanged(() => RotationX);
                    UpdateBacPlayer();
                }
            }
        }
        public float RotationY
        {
            get
            {
                return bacType.RotationY;
            }
            set
            {
                if (bacType.RotationY != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.RotationY), bacType, bacType.RotationY, value, "Camera RotationY"));
                    bacType.RotationY = value;
                    RaisePropertyChanged(() => RotationY);
                    UpdateBacPlayer();
                }
            }
        }
        public float RotationZ
        {
            get
            {
                return bacType.RotationZ;
            }
            set
            {
                if (bacType.RotationZ != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.RotationZ), bacType, bacType.RotationZ, value, "Camera RotationZ"));
                    bacType.RotationZ = value;
                    RaisePropertyChanged(() => RotationZ);
                    UpdateBacPlayer();
                }
            }
        }
        public float DisplacementXZ
        {
            get
            {
                return bacType.DisplacementXZ;
            }
            set
            {
                if (bacType.DisplacementXZ != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.DisplacementXZ), bacType, bacType.DisplacementXZ, value, "Camera DisplacementXZ"));
                    bacType.DisplacementXZ = value;
                    RaisePropertyChanged(() => DisplacementXZ);
                    UpdateBacPlayer();
                }
            }
        }
        public float DisplacementZY
        {
            get
            {
                return bacType.DisplacementZY;
            }
            set
            {
                if (bacType.DisplacementZY != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.DisplacementZY), bacType, bacType.DisplacementZY, value, "Camera DisplacementZY"));
                    bacType.DisplacementZY = value;
                    RaisePropertyChanged(() => DisplacementZY);
                    UpdateBacPlayer();
                }
            }
        }
        public float FieldOfView
        {
            get
            {
                return bacType.FieldOfView;
            }
            set
            {
                if (bacType.FieldOfView != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.FieldOfView), bacType, bacType.FieldOfView, value, "Camera FieldOfView"));
                    bacType.FieldOfView = value;
                    RaisePropertyChanged(() => FieldOfView);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort PositionDurationX
        {
            get
            {
                return bacType.PositionX_Duration;
            }
            set
            {
                if (bacType.PositionX_Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.PositionX_Duration), bacType, bacType.PositionX_Duration, value, "Camera PositionDurationX"));
                    bacType.PositionX_Duration = value;
                    RaisePropertyChanged(() => PositionDurationX);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort PositionDurationY
        {
            get
            {
                return bacType.PositionY_Duration;
            }
            set
            {
                if (bacType.PositionY_Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.PositionY_Duration), bacType, bacType.PositionY_Duration, value, "Camera PositionDurationY"));
                    bacType.PositionY_Duration = value;
                    RaisePropertyChanged(() => PositionDurationY);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort PositionDurationZ
        {
            get
            {
                return bacType.PositionZ_Duration;
            }
            set
            {
                if (bacType.PositionZ_Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.PositionZ_Duration), bacType, bacType.PositionZ_Duration, value, "Camera PositionDurationZ"));
                    bacType.PositionZ_Duration = value;
                    RaisePropertyChanged(() => PositionDurationZ);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort RotationDurationX
        {
            get
            {
                return bacType.RotationX_Duration;
            }
            set
            {
                if (bacType.RotationX_Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.RotationX_Duration), bacType, bacType.RotationX_Duration, value, "Camera RotationDurationX"));
                    bacType.RotationX_Duration = value;
                    RaisePropertyChanged(() => RotationDurationX);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort RotationDurationY
        {
            get
            {
                return bacType.RotationY_Duration;
            }
            set
            {
                if (bacType.RotationY_Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.RotationY_Duration), bacType, bacType.RotationY_Duration, value, "Camera RotationDurationY"));
                    bacType.RotationY_Duration = value;
                    RaisePropertyChanged(() => RotationDurationY);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort RotationDurationZ
        {
            get
            {
                return bacType.RotationZ_Duration;
            }
            set
            {
                if (bacType.RotationZ_Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.RotationZ_Duration), bacType, bacType.RotationZ_Duration, value, "Camera RotationDurationZ"));
                    bacType.RotationZ_Duration = value;
                    RaisePropertyChanged(() => RotationDurationZ);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort DisplacementDurationXZ
        {
            get
            {
                return bacType.DisplacementXZ_Duration;
            }
            set
            {
                if (bacType.DisplacementXZ_Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.DisplacementXZ_Duration), bacType, bacType.DisplacementXZ_Duration, value, "Camera DisplacementDurationXZ"));
                    bacType.DisplacementXZ_Duration = value;
                    RaisePropertyChanged(() => DisplacementDurationXZ);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort DisplacementDurationZY
        {
            get
            {
                return bacType.DisplacementZY_Duration;
            }
            set
            {
                if (bacType.DisplacementZY_Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.DisplacementZY_Duration), bacType, bacType.DisplacementZY_Duration, value, "Camera DisplacementDurationZY"));
                    bacType.DisplacementZY_Duration = value;
                    RaisePropertyChanged(() => DisplacementDurationZY);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort FieldOfViewDuration
        {
            get
            {
                return bacType.FieldOfView_Duration;
            }
            set
            {
                if (bacType.FieldOfView_Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.FieldOfView_Duration), bacType, bacType.FieldOfView_Duration, value, "Camera FieldOfViewDuration"));
                    bacType.FieldOfView_Duration = value;
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
                return bacType.EnableTransformModifiers;
            }
            set
            {
                if (bacType.EnableTransformModifiers != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.EnableTransformModifiers), bacType, bacType.EnableTransformModifiers, value, "Camera EnableTransformModifers"));
                    bacType.EnableTransformModifiers = value;
                    RaisePropertyChanged(() => EnableTransformModifers);
                    UpdateBacPlayer();
                }
            }
        }
        public bool EnableCameraForAllPlayers
        {
            get
            {
                return bacType.EnableCameraForAllPlayers;
            }
            set
            {
                if (bacType.EnableCameraForAllPlayers != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.EnableCameraForAllPlayers), bacType, bacType.EnableCameraForAllPlayers, value, "Camera EnableCameraForAllPlayers"));
                    bacType.EnableCameraForAllPlayers = value;
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
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.I_74_1), bacType, bacType.I_74_1, value, "Camera Flag_74_1"));
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
                return bacType.FocusOnTarget;
            }
            set
            {
                if (bacType.FocusOnTarget != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.FocusOnTarget), bacType, bacType.FocusOnTarget, value, "Camera FocusOnTarget"));
                    bacType.FocusOnTarget = value;
                    RaisePropertyChanged(() => FocusOnTarget);
                    UpdateBacPlayer();
                }
            }
        }
        public bool UseCharacterSpecificCameraEan
        {
            get
            {
                return bacType.UseCharacterSpecificCameraEan;
            }
            set
            {
                if (bacType.UseCharacterSpecificCameraEan != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.UseCharacterSpecificCameraEan), bacType, bacType.UseCharacterSpecificCameraEan, value, "Camera UseCharacterSpecificCameraEan"));
                    bacType.UseCharacterSpecificCameraEan = value;
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
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.I_74_4), bacType, bacType.I_74_4, value, "Camera Flag_74_4"));
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
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.I_74_5), bacType, bacType.I_74_5, value, "Camera Flag_74_5"));
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
                return bacType.DontOverrideActiveCameras;
            }
            set
            {
                if (bacType.DontOverrideActiveCameras != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.DontOverrideActiveCameras), bacType, bacType.DontOverrideActiveCameras, value, "Camera DontOverrideActiveCameras"));
                    bacType.DontOverrideActiveCameras = value;
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
                if ((BAC_Type10.EanTypeEnum)EanType == BAC_Type10.EanTypeEnum.Skill && Files.Instance.SelectedMove.MoveType != Move.Type.Skill) return null;
                return Files.Instance.GetCamEanFile((BAC_Type10.EanTypeEnum)EanType, Files.Instance.SelectedMove, SceneManager.Actors[0], false, true);
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
            SceneManager.InvokeBacDataChangedEvent();
        }
    }
}
