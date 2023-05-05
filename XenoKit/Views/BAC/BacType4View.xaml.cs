using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Controls;
using XenoKit.ViewModel.BAC;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Views.BAC
{
    /// <summary>
    /// Interaction logic for BacType4View.xaml
    /// </summary>
    public partial class BacType4View : UserControl, INotifyPropertyChanged
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
            "BacViewModel", typeof(BACType4ViewModel), typeof(BacType4View), new PropertyMetadata(default(BACType4ViewModel)));

        public BACType4ViewModel BacViewModel
        {
            get { return (BACType4ViewModel)GetValue(BacViewModelProperty); }
            set
            {
                SetValue(BacViewModelProperty, value);
                NotifyPropertyChanged("BacViewModel");
            }
        }

        public BacType4View()
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
    }
}
