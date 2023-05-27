using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType11ViewModel : ObservableObject
    {
        private BAC_Type11 bacType;

        public ushort AcbType
        {
            get
            {
                return (ushort)bacType.AcbType;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type11>(nameof(bacType.AcbType), bacType, bacType.AcbType, (AcbType)value, "Sound AcbType"));
                bacType.AcbType = (AcbType)value;
                RaisePropertyChanged(() => AcbType);
                RaisePropertyChanged(() => CueId);
                bacType.RefreshType();
            }
        }
        public ushort CueId
        {
            get
            {
                return bacType.CueId;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type11>(nameof(bacType.CueId), bacType, bacType.CueId, value, "Sound CueId"));
                bacType.CueId = value;
                RaisePropertyChanged(() => CueId);
                bacType.RefreshType();
            }
        }
        

        //Flags
        public bool Flag_Unk1
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk1);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk1, value);
                RaisePropertyChanged(() => Flag_Unk1);
            }
        }
        public bool Flag_Unk2
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk2);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk2, value);
                RaisePropertyChanged(() => Flag_Unk2);
            }
        }
        public bool Flag_Unk3
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk3);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk3, value);
                RaisePropertyChanged(() => Flag_Unk3);
            }
        }
        public bool Flag_StopWhenParentEnds
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.StopWhenParentEnds);
            }
            set
            {
                SetSoundFlags(SoundFlags.StopWhenParentEnds, value);
                RaisePropertyChanged(() => Flag_StopWhenParentEnds);
            }
        }
        public bool Flag_Unk5
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk5);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk5, value);
                RaisePropertyChanged(() => Flag_Unk5);
            }
        }
        public bool Flag_Unk6
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk6);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk6, value);
                RaisePropertyChanged(() => Flag_Unk6);
            }
        }
        public bool Flag_Unk7
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk7);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk7, value);
                RaisePropertyChanged(() => Flag_Unk7);
            }
        }
        public bool Flag_Unk8
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk8);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk8, value);
                RaisePropertyChanged(() => Flag_Unk8);
            }
        }
        public bool Flag_Unk9
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk9);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk9, value);
                RaisePropertyChanged(() => Flag_Unk9);
            }
        }
        public bool Flag_Unk10
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk10);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk10, value);
                RaisePropertyChanged(() => Flag_Unk10);
            }
        }
        public bool Flag_Unk11
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk11);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk11, value);
                RaisePropertyChanged(() => Flag_Unk11);
            }
        }
        public bool Flag_Unk12
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk12);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk12, value);
                RaisePropertyChanged(() => Flag_Unk12);
            }
        }
        public bool Flag_Unk13
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk13);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk13, value);
                RaisePropertyChanged(() => Flag_Unk13);
            }
        }
        public bool Flag_Unk14
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk14);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk14, value);
                RaisePropertyChanged(() => Flag_Unk14);
            }
        }
        public bool Flag_Unk15
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk15);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk16, value);
                RaisePropertyChanged(() => Flag_Unk15);
            }
        }
        public bool Flag_Unk16
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk16);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk16, value);
                RaisePropertyChanged(() => Flag_Unk16);
            }
        }

        public BACType11ViewModel(BAC_Type11 _bacType)
        {
            bacType = _bacType;
            bacType.PropertyChanged += BacType_PropertyChanged;

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        private void Instance_UndoOrRedoCalled(object sender, EventArgs e)
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
            RaisePropertyChanged(() => AcbType);
            RaisePropertyChanged(() => CueId);
            RaisePropertyChanged(() => Flag_Unk1);
            RaisePropertyChanged(() => Flag_Unk2);
            RaisePropertyChanged(() => Flag_Unk3);
            RaisePropertyChanged(() => Flag_StopWhenParentEnds);
            RaisePropertyChanged(() => Flag_Unk5);
            RaisePropertyChanged(() => Flag_Unk6);
            RaisePropertyChanged(() => Flag_Unk7);
            RaisePropertyChanged(() => Flag_Unk8);
            RaisePropertyChanged(() => Flag_Unk9);
            RaisePropertyChanged(() => Flag_Unk10);
            RaisePropertyChanged(() => Flag_Unk11);
            RaisePropertyChanged(() => Flag_Unk12);
            RaisePropertyChanged(() => Flag_Unk13);
            RaisePropertyChanged(() => Flag_Unk14);
            RaisePropertyChanged(() => Flag_Unk15);
            RaisePropertyChanged(() => Flag_Unk16);
        }

        private void SetSoundFlags(SoundFlags flag, bool state)
        {
            var newFlag = bacType.SoundFlags.SetFlag(flag, state);

            if (bacType.SoundFlags != newFlag)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type11>(nameof(BAC_Type11.SoundFlags), bacType, bacType.SoundFlags, newFlag, "SoundFlags"));
                bacType.SoundFlags = newFlag;
            }
        }
    }
}
