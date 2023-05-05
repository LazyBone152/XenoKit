using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Controls;
using XenoKit.ViewModel.BAC;

namespace XenoKit.Views.BAC
{
    /// <summary>
    /// Interaction logic for BacType11View.xaml
    /// </summary>
    public partial class BacType7View : UserControl, INotifyPropertyChanged
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
            nameof(BacViewModel), typeof(BACType7ViewModel), typeof(BacType7View), new PropertyMetadata(default(BACType7ViewModel)));

        public BACType7ViewModel BacViewModel
        {
            get { return (BACType7ViewModel)GetValue(BacViewModelProperty); }
            set
            {
                SetValue(BacViewModelProperty, value);
            }
        }

        public BacType7View()
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
