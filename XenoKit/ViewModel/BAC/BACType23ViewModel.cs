using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type23;

namespace XenoKit.ViewModel.BAC
{
    public class BACType23ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type23 bacType;

        public byte HorizontalLineSize
        {
            get
            {
                return bacType.HorizontalLineSize;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.HorizontalLineSize), bacType, bacType.HorizontalLineSize, value, "Horizontal Line Size"));
                bacType.HorizontalLineSize = value;
                RaisePropertyChanged(() => HorizontalLineSize);
            }
        }
        public byte VerticalLineSize
        {
            get
            {
                return bacType.VerticalLineSize;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.VerticalLineSize), bacType, bacType.VerticalLineSize, value, "Vertical Line Size"));
                bacType.VerticalLineSize = value;
                RaisePropertyChanged(() => VerticalLineSize);
            }
        }
        public byte HorizontalLineSpacing
        {
            get
            {
                return bacType.HorizontalLineSpacing;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.HorizontalLineSpacing), bacType, bacType.HorizontalLineSpacing, value, "Horizontal Line Spacing"));
                bacType.HorizontalLineSpacing = value;
                RaisePropertyChanged(() => HorizontalLineSpacing);
            }
        }
        public ushort VerticalLineSpacing
        {
            get
            {
                return bacType.VerticalLineSpacing;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.VerticalLineSpacing), bacType, bacType.VerticalLineSpacing, value, "Vertical Line Spacing"));
                bacType.VerticalLineSpacing = value;
                RaisePropertyChanged(() => VerticalLineSpacing);
            }
        }
        public byte I_14
        {
            get
            {
                return bacType.I_14;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.I_14), bacType, bacType.I_14, value, "Transparency I_14"));
                bacType.I_14 = value;
                RaisePropertyChanged(() => I_14);
            }
        }
        public byte I_15
        {
            get
            {
                return bacType.I_15;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.I_15), bacType, bacType.I_15, value, "Transparency I_15"));
                bacType.I_15 = value;
                RaisePropertyChanged(() => I_15);
            }
        }
        
        public Color Tint
        {
            get
            {
                return Color.FromArgb(Xv2ColorConverter.ConvertColor(bacType.Tint_A), Xv2ColorConverter.ConvertColor(bacType.Tint_R), Xv2ColorConverter.ConvertColor(bacType.Tint_G), Xv2ColorConverter.ConvertColor(bacType.Tint_B));
            }
            set
            {
                float r = Xv2ColorConverter.ConvertColor(value.R);
                float g = Xv2ColorConverter.ConvertColor(value.G);
                float b = Xv2ColorConverter.ConvertColor(value.B);
                float a = Xv2ColorConverter.ConvertColor(value.A);

                if(r != bacType.Tint_R || g != bacType.Tint_G || b != bacType.Tint_B || a != bacType.Tint_A)
                {
                    List<IUndoRedo> undos = new List<IUndoRedo>();
                    undos.Add(new UndoableProperty<BAC_Type23>(nameof(bacType.Tint_R), bacType, bacType.Tint_R, r));
                    undos.Add(new UndoableProperty<BAC_Type23>(nameof(bacType.Tint_G), bacType, bacType.Tint_G, g));
                    undos.Add(new UndoableProperty<BAC_Type23>(nameof(bacType.Tint_B), bacType, bacType.Tint_B, b));
                    undos.Add(new UndoableProperty<BAC_Type23>(nameof(bacType.Tint_A), bacType, bacType.Tint_A, a));

                    bacType.Tint_R = r;
                    bacType.Tint_G = g;
                    bacType.Tint_B = b;
                    bacType.Tint_A = a;

                    UndoManager.Instance.AddCompositeUndo(undos, "Transparency Tint Color");

                    RaisePropertyChanged(() => Tint);
                }
            }
        }

        //Unknown floats
        public float F_36
        {
            get
            {
                return bacType.F_36;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.F_36), bacType, bacType.F_36, value, "Transparency F_36"));
                bacType.F_36 = value;
                RaisePropertyChanged(() => F_36);
            }
        }
        public float F_40
        {
            get
            {
                return bacType.F_40;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.F_40), bacType, bacType.F_40, value, "Transparency F_40"));
                bacType.F_40 = value;
                RaisePropertyChanged(() => F_40);
            }
        }
        public float F_44
        {
            get
            {
                return bacType.F_44;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.F_44), bacType, bacType.F_44, value, "Transparency F_44"));
                bacType.F_44 = value;
                RaisePropertyChanged(() => F_44);
            }
        }
        public float F_48
        {
            get
            {
                return bacType.F_48;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.F_48), bacType, bacType.F_48, value, "Transparency F_48"));
                bacType.F_48 = value;
                RaisePropertyChanged(() => F_48);
            }
        }

        //Shader Path Options
        public ShaderPathOptions ShaderOptions
        {
            get
            {
                return bacType.ShaderOptions;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.ShaderOptions), bacType, bacType.ShaderOptions, value, "Shader Path Options"));
                bacType.ShaderOptions = value;
                RaisePropertyChanged(() => ShaderOptions);
            }
        }

        public BACType23ViewModel(BAC_Type23 _bacType)
        {
            bacType = _bacType;
            bacType.PropertyChanged += BacType_PropertyChanged;

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        public void Dispose()
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
            bacType.PropertyChanged -= BacType_PropertyChanged;
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
            RaisePropertyChanged(() => HorizontalLineSize);
            RaisePropertyChanged(() => VerticalLineSize);
            RaisePropertyChanged(() => HorizontalLineSpacing);
            RaisePropertyChanged(() => VerticalLineSpacing);
            RaisePropertyChanged(() => I_14);
            RaisePropertyChanged(() => I_15);
            RaisePropertyChanged(() => F_36);
            RaisePropertyChanged(() => F_40);
            RaisePropertyChanged(() => F_44);
            RaisePropertyChanged(() => F_48);
            RaisePropertyChanged(() => Tint);
            RaisePropertyChanged(() => ShaderOptions);
        }
    }
}
