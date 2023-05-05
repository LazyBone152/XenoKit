using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using XenoKit.ViewModel.BAC;
using XenoKit.Controls;

namespace XenoKit.Views.BAC
{
    /// <summary>
    /// Interaction logic for BacType0View.xaml
    /// </summary>
    public partial class BacType18View : UserControl, INotifyPropertyChanged
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
            "BacViewModel", typeof(BACType18ViewModel), typeof(BacType18View), new PropertyMetadata(default(BACType18ViewModel)));

        public BACType18ViewModel BacViewModel
        {
            get { return (BACType18ViewModel)GetValue(BacViewModelProperty); }
            set
            {
                SetValue(BacViewModelProperty, value);
                NotifyPropertyChanged("BacViewModel");
            }
        }

        public BacType18View()
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
