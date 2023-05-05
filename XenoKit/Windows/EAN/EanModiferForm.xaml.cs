using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
        public int SmoothFrame { get; set; }
        public EAN_AnimationComponent.ComponentType Component { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }
        public float ScaleFactor { get; set; }
        public int NewDuration { get; set; }

        //Enabled
        public bool StartFrameEnabled { get; set; }
        public bool SmoothFrameEnabled { get; set; }
        public bool ComponentEnabled { get; set; }
        public bool PosEnabled { get; set; }
        public bool ScaleFactorEnabled { get; set; }
        public bool NewDurationEnabled { get; set; }

        //Success
        public bool Success { get; set; }

        //Other
        public int StartFrameMaxConstraint { get { return EndFrame - 1; } }
        public int EndFrameMinConstraint { get { return StartFrame + 1; } }

        public EanModiferForm(string formName)
        {
            InitializeComponent();
            Owner = App.Current.MainWindow;
            DataContext = this;
            Title = formName;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Success = true;
            Close();
        }
    }
}
