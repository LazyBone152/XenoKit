using GalaSoft.MvvmLight;
using XenoKit.Engine;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType2ViewModel : ObservableObject
    {
        private BAC_Type2 bacType;
        
        public float DirectionX
        {
            get
            {
                return bacType.DirectionX;
            }
            set
            {
                if (bacType.DirectionX != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.DirectionX), bacType, bacType.DirectionX, value, "Movement DirectionX"));
                    bacType.DirectionX = value;
                    RaisePropertyChanged(() => DirectionX);
                }
            }
        }
        public float DirectionY
        {
            get
            {
                return bacType.DirectionY;
            }
            set
            {
                if (bacType.DirectionY != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.DirectionY), bacType, bacType.DirectionY, value, "Movement DirectionY"));
                    bacType.DirectionY = value;
                    RaisePropertyChanged(() => DirectionY);
                }
            }
        }
        public float DirectionZ
        {
            get
            {
                return bacType.DirectionZ;
            }
            set
            {
                if (bacType.DirectionZ != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.DirectionZ), bacType, bacType.DirectionZ, value, "Movement DirectionZ"));
                    bacType.DirectionZ = value;
                    RaisePropertyChanged(() => DirectionZ);
                }
            }
        }
        public float DragX
        {
            get
            {
                return bacType.DragX;
            }
            set
            {
                if (bacType.DragX != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.DragX), bacType, bacType.DragX, value, "Movement DragX"));
                    bacType.DragX = value;
                    RaisePropertyChanged(() => DragX);
                }
            }
        }
        public float DragY
        {
            get
            {
                return bacType.DragY;
            }
            set
            {
                if (bacType.DragY != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.DragY), bacType, bacType.DragY, value, "Movement DragY"));
                    bacType.DragY = value;
                    RaisePropertyChanged(() => DragY);
                }
            }
        }
        public float DragZ
        {
            get
            {
                return bacType.DragZ;
            }
            set
            {
                if (bacType.DragZ != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.DragZ), bacType, bacType.DragZ, value, "Movement DragZ"));
                    bacType.DragZ = value;
                    RaisePropertyChanged(() => DragZ);
                }
            }
        }

        //MovementFlags
        public bool MovementFlags_D_1
        {
            get
            {
                return (bacType.MovementFlags & 0x0001) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0001 : 0x0000);
                ushort newFlags = (ushort)((bacType.MovementFlags & 0xFFFE) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.MovementFlags), bacType, bacType.MovementFlags, newFlags, "Movement MovementFlags"));
                bacType.MovementFlags = newFlags;
                RaisePropertyChanged(() => MovementFlags_D_1);
            }
        }
        public bool MovementFlags_D_2
        {
            get
            {
                return (bacType.MovementFlags & 0x0002) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0002 : 0x0000);
                ushort newFlags = (ushort)((bacType.MovementFlags & 0xFFFD) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.MovementFlags), bacType, bacType.MovementFlags, newFlags, "Movement MovementFlags"));
                bacType.MovementFlags = newFlags;
                RaisePropertyChanged(() => MovementFlags_D_2);
            }
        }
        public bool MovementFlags_D_3
        {
            get
            {
                return (bacType.MovementFlags & 0x0004) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0004 : 0x0000);
                ushort newFlags = (ushort)((bacType.MovementFlags & 0xFFFB) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.MovementFlags), bacType, bacType.MovementFlags, newFlags, "Movement MovementFlags"));
                bacType.MovementFlags = newFlags;
                RaisePropertyChanged(() => MovementFlags_D_3);
            }
        }
        public bool MovementFlags_D_4
        {
            get
            {
                return (bacType.MovementFlags & 0x0008) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0008 : 0x0000);
                ushort newFlags = (ushort)((bacType.MovementFlags & 0xFFF7) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.MovementFlags), bacType, bacType.MovementFlags, newFlags, "Movement MovementFlags"));
                bacType.MovementFlags = newFlags;
                RaisePropertyChanged(() => MovementFlags_D_4);
            }
        }
        public bool MovementFlags_C_1
        {
            get
            {
                return (bacType.MovementFlags & 0x0010) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0010 : 0x0000);
                ushort newFlags = (ushort)((bacType.MovementFlags & 0xFFEF) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.MovementFlags), bacType, bacType.MovementFlags, newFlags, "Movement MovementFlags"));
                bacType.MovementFlags = newFlags;
                RaisePropertyChanged(() => MovementFlags_C_1);
            }
        }
        public bool MovementFlags_C_2
        {
            get
            {
                return (bacType.MovementFlags & 0x0020) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0020 : 0x0000);
                ushort newFlags = (ushort)((bacType.MovementFlags & 0xFFDF) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.MovementFlags), bacType, bacType.MovementFlags, newFlags, "Movement MovementFlags"));
                bacType.MovementFlags = newFlags;
                RaisePropertyChanged(() => MovementFlags_C_2);
            }
        }
        public bool MovementFlags_C_3
        {
            get
            {
                return (bacType.MovementFlags & 0x0040) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0040 : 0x0000);
                ushort newFlags = (ushort)((bacType.MovementFlags & 0xFFBF) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.MovementFlags), bacType, bacType.MovementFlags, newFlags, "Movement MovementFlags"));
                bacType.MovementFlags = newFlags;
                RaisePropertyChanged(() => MovementFlags_C_3);
            }
        }
        public bool MovementFlags_C_4
        {
            get
            {
                return (bacType.MovementFlags & 0x0080) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0080 : 0x0000);
                ushort newFlags = (ushort)((bacType.MovementFlags & 0xFF7F) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.MovementFlags), bacType, bacType.MovementFlags, newFlags, "Movement MovementFlags"));
                bacType.MovementFlags = newFlags;
                RaisePropertyChanged(() => MovementFlags_C_4);
            }
        }
        public bool MovementFlags_B_1
        {
            get
            {
                return (bacType.MovementFlags & 0x0100) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0100 : 0x0000);
                ushort newFlags = (ushort)((bacType.MovementFlags & 0xFEFF) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.MovementFlags), bacType, bacType.MovementFlags, newFlags, "Movement MovementFlags"));
                bacType.MovementFlags = newFlags;
                RaisePropertyChanged(() => MovementFlags_B_1);
            }
        }
        public bool MovementFlags_B_2
        {
            get
            {
                return (bacType.MovementFlags & 0x0200) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0200 : 0x0000);
                ushort newFlags = (ushort)((bacType.MovementFlags & 0xFDFF) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.MovementFlags), bacType, bacType.MovementFlags, newFlags, "Movement MovementFlags"));
                bacType.MovementFlags = newFlags;
                RaisePropertyChanged(() => MovementFlags_B_2);
            }
        }
        public bool MovementFlags_B_3
        {
            get
            {
                return (bacType.MovementFlags & 0x0400) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0400 : 0x0000);
                ushort newFlags = (ushort)((bacType.MovementFlags & 0xFBFF) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.MovementFlags), bacType, bacType.MovementFlags, newFlags, "Movement MovementFlags"));
                bacType.MovementFlags = newFlags;
                RaisePropertyChanged(() => MovementFlags_B_3);
            }
        }
        public bool MovementFlags_B_4
        {
            get
            {
                return (bacType.MovementFlags & 0x0800) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0800 : 0x0000);
                ushort newFlags = (ushort)((bacType.MovementFlags & 0xF7FF) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.MovementFlags), bacType, bacType.MovementFlags, newFlags, "Movement MovementFlags"));
                bacType.MovementFlags = newFlags;
                RaisePropertyChanged(() => MovementFlags_B_4);
            }
        }
        public bool MovementFlags_A_1
        {
            get
            {
                return (bacType.MovementFlags & 0x1000) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0100 : 0x0000);
                ushort newFlags = (ushort)((bacType.MovementFlags & 0xEFFF) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.MovementFlags), bacType, bacType.MovementFlags, newFlags, "Movement MovementFlags"));
                bacType.MovementFlags = newFlags;
                RaisePropertyChanged(() => MovementFlags_A_1);
            }
        }
        public bool MovementFlags_A_2
        {
            get
            {
                return (bacType.MovementFlags & 0x2000) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x2000 : 0x0000);
                ushort newFlags = (ushort)((bacType.MovementFlags & 0xDFFF) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.MovementFlags), bacType, bacType.MovementFlags, newFlags, "Movement MovementFlags"));
                bacType.MovementFlags = newFlags;
                RaisePropertyChanged(() => MovementFlags_A_2);
            }
        }
        public bool MovementFlags_A_3
        {
            get
            {
                return (bacType.MovementFlags & 0x4000) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x4000 : 0x0000);
                ushort newFlags = (ushort)((bacType.MovementFlags & 0xBFFF) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.MovementFlags), bacType, bacType.MovementFlags, newFlags, "Movement MovementFlags"));
                bacType.MovementFlags = newFlags;
                RaisePropertyChanged(() => MovementFlags_A_3);
            }
        }
        public bool MovementFlags_A_4
        {
            get
            {
                return (bacType.MovementFlags & 0x8000) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x8000 : 0x0000);
                ushort newFlags = (ushort)((bacType.MovementFlags & 0x7FFF) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type2>(nameof(bacType.MovementFlags), bacType, bacType.MovementFlags, newFlags, "Movement MovementFlags"));
                bacType.MovementFlags = newFlags;
                RaisePropertyChanged(() => MovementFlags_A_4);
            }
        }

        public BACType2ViewModel(BAC_Type2 _bacType)
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
            RaisePropertyChanged(() => DirectionX);
            RaisePropertyChanged(() => DirectionY);
            RaisePropertyChanged(() => DirectionZ);
            RaisePropertyChanged(() => DragX);
            RaisePropertyChanged(() => DragY);
            RaisePropertyChanged(() => DragZ);

            RaisePropertyChanged(() => MovementFlags_D_1);
            RaisePropertyChanged(() => MovementFlags_D_2);
            RaisePropertyChanged(() => MovementFlags_D_3);
            RaisePropertyChanged(() => MovementFlags_D_4);
            RaisePropertyChanged(() => MovementFlags_C_1);
            RaisePropertyChanged(() => MovementFlags_C_2);
            RaisePropertyChanged(() => MovementFlags_C_3);
            RaisePropertyChanged(() => MovementFlags_C_4);
            RaisePropertyChanged(() => MovementFlags_B_1);
            RaisePropertyChanged(() => MovementFlags_B_2);
            RaisePropertyChanged(() => MovementFlags_B_3);
            RaisePropertyChanged(() => MovementFlags_B_4);
            RaisePropertyChanged(() => MovementFlags_A_1);
            RaisePropertyChanged(() => MovementFlags_A_2);
            RaisePropertyChanged(() => MovementFlags_A_3);
            RaisePropertyChanged(() => MovementFlags_A_4);
        }

        private void UpdateBacPlayer()
        {
            SceneManager.InvokeBacValuesChangedEvent();
        }

    }
}
