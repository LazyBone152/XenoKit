using GalaSoft.MvvmLight;
using XenoKit.Editor;
using XenoKit.Engine;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.EAN;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type0;

namespace XenoKit.ViewModel.BAC
{
    public class BACType0ViewModel : ObservableObject
    {
        private BAC_Type0 bacType;

        public ushort EanType
        {
            get
            {
                return (ushort)bacType.EanType;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.EanType), bacType, bacType.EanType, (BAC_Type0.EanTypeEnum)value, "Animation EanType"));
                bacType.EanType = (BAC_Type0.EanTypeEnum)value;
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
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.EanIndex), bacType, bacType.EanIndex, value, "Animation Ean Index"));
                bacType.EanIndex = value;
                RaisePropertyChanged(() => EanIndex);
                UpdateBacPlayer();
                bacType.RefreshType();
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
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.StartFrame), bacType, bacType.StartFrame, value, "Animation Start Frame"));
                    bacType.StartFrame = value;
                    RaisePropertyChanged(() => StartFrame);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort EndFrame
        {
            get
            {
                return bacType.EndFrame;
            }
            set
            {
                if (bacType.EndFrame != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.EndFrame), bacType, bacType.EndFrame, value, "Animation End Frame"));
                    bacType.EndFrame = value;
                    RaisePropertyChanged(() => EndFrame);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort LoopStartFrame
        {
            get
            {
                return bacType.LoopStartFrame;
            }
            set
            {
                if (bacType.LoopStartFrame != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.LoopStartFrame), bacType, bacType.LoopStartFrame, value, "Animation Loop Start Frame"));
                    bacType.LoopStartFrame = value;
                    RaisePropertyChanged(() => LoopStartFrame);
                    UpdateBacPlayer();
                }
            }
        }
        public float TimeScale
        {
            get
            {
                return bacType.TimeScale;
            }
            set
            {
                if (bacType.TimeScale != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.TimeScale), bacType, bacType.TimeScale, value, "Animation TimeScale"));
                    bacType.TimeScale = value;
                    RaisePropertyChanged(() => TimeScale);
                    UpdateBacPlayer();
                }
            }
        }
        public float StartBlendWeight
        {
            get
            {
                return bacType.BlendWeight;
            }
            set
            {
                if (bacType.BlendWeight != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.BlendWeight), bacType, bacType.BlendWeight, value, "Animation StartBlendWeight"));
                    bacType.BlendWeight = value;
                    RaisePropertyChanged(() => StartBlendWeight);
                    UpdateBacPlayer();
                }
            }
        }
        public float BlendWeightIncreasePerFrame
        {
            get
            {
                return bacType.BlendWeightFrameStep;
            }
            set
            {
                if (bacType.BlendWeightFrameStep != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.BlendWeightFrameStep), bacType, bacType.BlendWeightFrameStep, value, "Animation BlendWeightIncreasePerFrame"));
                    bacType.BlendWeightFrameStep = value;
                    RaisePropertyChanged(() => BlendWeightIncreasePerFrame);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort I_14
        {
            get
            {
                return bacType.I_14;
            }
            set
            {
                if (bacType.I_14 != value)
                {
                    // For some reason the undo and redo don't work for this value, probably overlooking something but the old/new value doesn't get applied to the checkbox - Pandas
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.I_14), bacType, bacType.I_14, value, "Animation Face Animation from EAN"));
                    bacType.I_14 = value;
                    RaisePropertyChanged(() => I_14);
                    UpdateBacPlayer();
                }
            }
        }

        //Flags
        public bool MoveWithX
        {
            get
            {
                return bacType.AnimFlags.HasFlag(AnimationFlags.MoveWithAxis_X);
            }
            set
            {
                AnimationFlags flags = bacType.AnimFlags.SetFlag(AnimationFlags.MoveWithAxis_X, value);

                if (bacType.AnimFlags != flags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.AnimFlags), bacType, bacType.AnimFlags, flags, "Animation MoveWithX"));
                    bacType.AnimFlags = flags;
                    RaisePropertyChanged(() => MoveWithX);
                    UpdateBacPlayer();
                }
            }
        }
        public bool MoveWithY
        {
            get
            {
                return bacType.AnimFlags.HasFlag(AnimationFlags.MoveWithAxis_Y);
            }
            set
            {
                AnimationFlags flags = bacType.AnimFlags.SetFlag(AnimationFlags.MoveWithAxis_Y, value);

                if (bacType.AnimFlags != flags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.AnimFlags), bacType, bacType.AnimFlags, flags, "Animation MoveWithY"));
                    bacType.AnimFlags = flags;
                    RaisePropertyChanged(() => MoveWithY);
                    UpdateBacPlayer();
                }
            }
        }
        public bool MoveWithZ
        {
            get
            {
                return bacType.AnimFlags.HasFlag(AnimationFlags.MoveWithAxis_Z);
            }
            set
            {
                AnimationFlags flags = bacType.AnimFlags.SetFlag(AnimationFlags.MoveWithAxis_Z, value);

                if (bacType.AnimFlags != flags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.AnimFlags), bacType, bacType.AnimFlags, flags, "Animation MoveWithZ"));
                    bacType.AnimFlags = flags;
                    RaisePropertyChanged(() => MoveWithZ);
                    UpdateBacPlayer();
                }
            }
        }
        public bool Flags_Unk4
        {
            get
            {
                return bacType.AnimFlags.HasFlag(AnimationFlags.Unk4);
            }
            set
            {
                AnimationFlags flags = bacType.AnimFlags.SetFlag(AnimationFlags.Unk4, value);

                if (bacType.AnimFlags != flags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.AnimFlags), bacType, bacType.AnimFlags, flags, "AnimationFlags"));
                    bacType.AnimFlags = flags;
                    RaisePropertyChanged(() => Flags_Unk4);
                }
            }
        }
        public bool UseRootMotion
        {
            get
            {
                return bacType.AnimFlags.HasFlag(AnimationFlags.UseRootMotion);
            }
            set
            {
                AnimationFlags flags = bacType.AnimFlags.SetFlag(AnimationFlags.UseRootMotion, value);

                if (bacType.AnimFlags != flags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.AnimFlags), bacType, bacType.AnimFlags, flags, "AnimationFlags"));
                    bacType.AnimFlags = flags;
                    RaisePropertyChanged(() => UseRootMotion);
                    RaisePropertyChanged(() => MoveWithFlagsEnabled);
                }
            }
        }
        public bool Flags_Unk6
        {
            get
            {
                return bacType.AnimFlags.HasFlag(AnimationFlags.Unk6);
            }
            set
            {
                AnimationFlags flags = bacType.AnimFlags.SetFlag(AnimationFlags.Unk6, value);

                if (bacType.AnimFlags != flags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.AnimFlags), bacType, bacType.AnimFlags, flags, "AnimationFlags"));
                    bacType.AnimFlags = flags;
                    RaisePropertyChanged(() => Flags_Unk6);
                }
            }
        }
        public bool FullRootMotion
        {
            get
            {
                return bacType.AnimFlags.HasFlag(AnimationFlags.FullRootMotion);
            }
            set
            {
                AnimationFlags flags = bacType.AnimFlags.SetFlag(AnimationFlags.FullRootMotion, value);

                if (bacType.AnimFlags != flags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.AnimFlags), bacType, bacType.AnimFlags, flags, "AnimationFlags"));
                    bacType.AnimFlags = flags;
                    RaisePropertyChanged(() => FullRootMotion);
                }
            }
        }
        public bool Flags_Unk8
        {
            get
            {
                return bacType.AnimFlags.HasFlag(AnimationFlags.Unk8);
            }
            set
            {
                AnimationFlags flags = bacType.AnimFlags.SetFlag(AnimationFlags.Unk8, value);

                if (bacType.AnimFlags != flags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.AnimFlags), bacType, bacType.AnimFlags, flags, "AnimationFlags"));
                    bacType.AnimFlags = flags;
                    RaisePropertyChanged(() => Flags_Unk8);
                }
            }
        }
        public bool Flags_Unk9
        {
            get
            {
                return bacType.AnimFlags.HasFlag(AnimationFlags.Unk9);
            }
            set
            {
                AnimationFlags flags = bacType.AnimFlags.SetFlag(AnimationFlags.Unk9, value);

                if (bacType.AnimFlags != flags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.AnimFlags), bacType, bacType.AnimFlags, flags, "AnimationFlags"));
                    bacType.AnimFlags = flags;
                    RaisePropertyChanged(() => Flags_Unk9);
                }
            }
        }
        public bool Flags_Unk10
        {
            get
            {
                return bacType.AnimFlags.HasFlag(AnimationFlags.Unk10);
            }
            set
            {
                AnimationFlags flags = bacType.AnimFlags.SetFlag(AnimationFlags.Unk10, value);

                if (bacType.AnimFlags != flags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.AnimFlags), bacType, bacType.AnimFlags, flags, "AnimationFlags"));
                    bacType.AnimFlags = flags;
                    RaisePropertyChanged(() => Flags_Unk10);
                }
            }
        }
        public bool Flags_Unk11
        {
            get
            {
                return bacType.AnimFlags.HasFlag(AnimationFlags.Unk11);
            }
            set
            {
                AnimationFlags flags = bacType.AnimFlags.SetFlag(AnimationFlags.Unk11, value);

                if (bacType.AnimFlags != flags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.AnimFlags), bacType, bacType.AnimFlags, flags, "AnimationFlags"));
                    bacType.AnimFlags = flags;
                    RaisePropertyChanged(() => Flags_Unk11);
                }
            }
        }
        public bool Flags_Unk12
        {
            get
            {
                return bacType.AnimFlags.HasFlag(AnimationFlags.Unk12);
            }
            set
            {
                AnimationFlags flags = bacType.AnimFlags.SetFlag(AnimationFlags.Unk12, value);

                if (bacType.AnimFlags != flags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.AnimFlags), bacType, bacType.AnimFlags, flags, "AnimationFlags1"));
                    bacType.AnimFlags = flags;
                    RaisePropertyChanged(() => Flags_Unk12);
                }
            }
        }
        public bool Flags_Unk13
        {
            get
            {
                return bacType.AnimFlags.HasFlag(AnimationFlags.Unk13);
            }
            set
            {
                AnimationFlags flags = bacType.AnimFlags.SetFlag(AnimationFlags.Unk13, value);

                if (bacType.AnimFlags != flags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.AnimFlags), bacType, bacType.AnimFlags, flags, "AnimationFlags"));
                    bacType.AnimFlags = flags;
                    RaisePropertyChanged(() => Flags_Unk13);
                }
            }
        }
        public bool Flags_Unk14
        {
            get
            {
                return bacType.AnimFlags.HasFlag(AnimationFlags.Unk14);
            }
            set
            {
                AnimationFlags flags = bacType.AnimFlags.SetFlag(AnimationFlags.Unk14, value);

                if (bacType.AnimFlags != flags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.AnimFlags), bacType, bacType.AnimFlags, flags, "AnimationFlags"));
                    bacType.AnimFlags = flags;
                    RaisePropertyChanged(() => Flags_Unk14);
                }
            }
        }
        public bool Flags_Unk15
        {
            get
            {
                return bacType.AnimFlags.HasFlag(AnimationFlags.Unk15);
            }
            set
            {
                AnimationFlags flags = bacType.AnimFlags.SetFlag(AnimationFlags.Unk15, value);

                if (bacType.AnimFlags != flags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.AnimFlags), bacType, bacType.AnimFlags, flags, "AnimationFlags"));
                    bacType.AnimFlags = flags;
                    RaisePropertyChanged(() => Flags_Unk15);
                }
            }
        }
        public bool Flags_Unk16
        {
            get
            {
                return bacType.AnimFlags.HasFlag(AnimationFlags.Unk16);
            }
            set
            {
                AnimationFlags flags = bacType.AnimFlags.SetFlag(AnimationFlags.Unk16, value);

                if (bacType.AnimFlags != flags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type0>(nameof(bacType.AnimFlags), bacType, bacType.AnimFlags, flags, "AnimationFlags"));
                    bacType.AnimFlags = flags;
                    RaisePropertyChanged(() => Flags_Unk16);
                }
            }
        }

        //Ean
        public EAN_File SpecifiedEan
        {
            get
            {
                if ((BAC_Type0.EanTypeEnum)EanType == BAC_Type0.EanTypeEnum.Skill && Files.Instance.SelectedMove.MoveType != Move.Type.Skill) return null;
                return Files.Instance.GetEanFile((BAC_Type0.EanTypeEnum)EanType, Files.Instance.SelectedMove, SceneManager.Actors[0], false, true);
            }
        }

        //UI
        public bool UseEanList { get { return SpecifiedEan != null; } }
        public bool MoveWithFlagsEnabled => !UseRootMotion;

        public BACType0ViewModel(BAC_Type0 _bacType)
        {
            bacType = _bacType;
            bacType.PropertyChanged += BacType_PropertyChanged;

            if(UndoManager.Instance != null)
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
            RaisePropertyChanged(() => EndFrame);
            RaisePropertyChanged(() => LoopStartFrame);
            RaisePropertyChanged(() => TimeScale);
            RaisePropertyChanged(() => StartBlendWeight);
            RaisePropertyChanged(() => BlendWeightIncreasePerFrame);
            RaisePropertyChanged(() => MoveWithX);
            RaisePropertyChanged(() => MoveWithY);
            RaisePropertyChanged(() => MoveWithZ);
            RaisePropertyChanged(() => Flags_Unk4);
            RaisePropertyChanged(() => UseRootMotion);
            RaisePropertyChanged(() => Flags_Unk6);
            RaisePropertyChanged(() => FullRootMotion);
            RaisePropertyChanged(() => Flags_Unk8);
            RaisePropertyChanged(() => Flags_Unk9);
            RaisePropertyChanged(() => Flags_Unk10);
            RaisePropertyChanged(() => Flags_Unk11);
            RaisePropertyChanged(() => Flags_Unk12);
            RaisePropertyChanged(() => Flags_Unk13);
            RaisePropertyChanged(() => Flags_Unk14);
            RaisePropertyChanged(() => Flags_Unk15);
            RaisePropertyChanged(() => Flags_Unk16);
            RaisePropertyChanged(() => SpecifiedEan);
            RaisePropertyChanged(() => UseEanList);
            UpdateBacPlayer();
        }

        private void UpdateBacPlayer()
        {
            SceneManager.InvokeBacDataChangedEvent();
        }
    }
}
