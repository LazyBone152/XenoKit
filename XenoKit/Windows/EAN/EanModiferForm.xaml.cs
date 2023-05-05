using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using Xv2CoreLib.EAN;

namespace XenoKit.Windows.EAN
{
    /// <summary>
    /// Interaction logic for EanModiferForm.xaml
    /// </summary>
    public partial class EanModiferForm : MetroWindow, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private int _startFrame = 0;
        private int _endFrame = 0;
        private int _insertFrame = 0;
        private bool _append = false;

        //Values
        public int StartFrame 
        {
            get { return _startFrame; }
            set
            {
                if(_startFrame != value)
                {
                    _startFrame = value;
                    NotifyPropertyChanged(nameof(StartFrame));
                    NotifyPropertyChanged(nameof(StartFrameMaxConstraint));
                    NotifyPropertyChanged(nameof(EndFrameMinConstraint));
                }
            }
        }
        public int EndFrame
        {
            get { return _endFrame; }
            set
            {
                if (_endFrame != value)
                {
                    _endFrame = value;
                    NotifyPropertyChanged(nameof(EndFrame));
                    NotifyPropertyChanged(nameof(StartFrameMaxConstraint));
                    NotifyPropertyChanged(nameof(EndFrameMinConstraint));
                }
            }
        }
        public int InsertFrame
        {
            get { return _insertFrame; }
            set
            {
                if (_insertFrame != value)
                {
                    _insertFrame = value;
                    NotifyPropertyChanged(nameof(InsertFrame));
                    NotifyPropertyChanged(nameof(EndFrameMinConstraint));
                }
            }
        }
        public bool Append
        {
            get => _append;
            set
            {
                _append = value;
                NotifyPropertyChanged(nameof(Append));
                NotifyPropertyChanged(nameof(NotAppend));
            }
        }
        public int SmoothFrame { get; set; }
        public EAN_AnimationComponent.ComponentType Component { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }
        public float ShakeFactor { get; set; } = 1f;
        public float BlendFactor { get; set; } = 1f;
        public float BlendFactorStep { get; set; } = 0f;
        public float ScaleFactor { get; set; }
        public int NewDuration { get; set; }
        public bool RemoveCollisions { get; set; } = false;
        public bool RebaseKeyframes { get; set; } = false;
        public int RebaseAmount { get; set; }

        //Enabled
        public bool StartFrameEnabled { get; set; }
        public bool EndFrameEnabled { get; set; }
        public bool SmoothFrameEnabled { get; set; }
        public bool ComponentEnabled { get; set; }
        public bool PosEnabled { get; set; }
        public bool ScaleFactorEnabled { get; set; }
        public bool BlendFactorEnabled { get; set; }
        public bool NewDurationEnabled { get; set; }
        public bool InsertEnabled { get; set; }
        public bool RemoveCollisionsEnabled { get; set; }
        public bool RebaseKeyframesEnabled { get; set; }
        public bool RebaseAmountEnabled { get; set; }
        public bool ShakeFactorEnabled { get; set; }

        //Success
        public bool Success { get; set; }

        //Other
        public int StartFrameMaxConstraint { get { return (StartFrameConstraintEnabled) ? EndFrame - 1 : ushort.MaxValue; } }
        public int EndFrameMinConstraint { get { return (EndFrameConstraintEnabled) ? StartFrame + 1 : 0; } }
        public bool NotAppend { get { return !Append; } }

        //Settings
        public bool StartFrameConstraintEnabled { get; set; } = true;
        public bool EndFrameConstraintEnabled { get; set; } = true;

        public EanModiferForm(string formName)
        {
            InitializeComponent();
            Owner = App.Current.MainWindow;
            DataContext = this;
            Title = formName;
        }

        public void SetFocus()
        {
            if (StartFrameEnabled)
                startFrame.Focus();

            if (ScaleFactorEnabled)
                scaleFactor.Focus();

            if (NewDurationEnabled)
                newDuration.Focus();
        }

        public RelayCommand DoneCommand => new RelayCommand(Done);
        private void Done()
        {
            Success = true;
            Close();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            SetFocus();
        }
    }
}
