using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Controls;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.ViewModel.BAC;
using Xv2CoreLib.BAC;

namespace XenoKit.Views.BAC
{
    /// <summary>
    /// Interaction logic for BacType11View.xaml
    /// </summary>
    public partial class BacType11View : UserControl, INotifyPropertyChanged
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


        public static readonly DependencyProperty BacViewModelProperty = DependencyProperty.Register(
            nameof(BacViewModel), typeof(BACType11ViewModel), typeof(BacType11View), new PropertyMetadata(default(BACType11ViewModel)));

        public BACType11ViewModel BacViewModel
        {
            get { return (BACType11ViewModel)GetValue(BacViewModelProperty); }
            set
            {
                SetValue(BacViewModelProperty, value);
            }
        }

        public BacType11View()
        {
            InitializeComponent();
            BacTab.BacTypeSelectionChanged += BacTab_BacTypeSelectionChanged;
            BacTab_BacTypeSelectionChanged(null, null);
        }

        private void BacTab_BacTypeSelectionChanged(object sender, EventArgs e)
        {
            if (BacViewModel != null)
            {
                Visibility = Visibility.Visible;
            }
            else
            {
                Visibility = Visibility.Collapsed;
            }

        }

        private void SoundPreview_Click(object sender, RoutedEventArgs e)
        {
            var acb = Files.Instance.GetAcbFile((AcbType)BacViewModel.AcbType, Files.Instance.SelectedMove, SceneManager.Actors[0], true);

            if(acb == null)
            {
                Log.Add($"Could not find the ACB for AcbType {BacViewModel.AcbType}. Preview failed.");
                return;
            }

            if(BacViewModel.CueId != ushort.MaxValue)
            {
                SceneManager.AudioEngine.PreviewCue(BacViewModel.CueId, acb);
            }

        }
    }
}
