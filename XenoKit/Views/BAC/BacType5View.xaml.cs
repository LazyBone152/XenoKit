using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Controls;
using XenoKit.ViewModel.BAC;

namespace XenoKit.Views.BAC
{
    /// <summary>
    /// Interaction logic for BacType5View.xaml
    /// </summary>
    public partial class BacType5View : UserControl, INotifyPropertyChanged
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
            "BacViewModel", typeof(BACType5ViewModel), typeof(BacType5View), new PropertyMetadata(default(BACType5ViewModel)));

        public BACType5ViewModel BacViewModel
        {
            get { return (BACType5ViewModel)GetValue(BacViewModelProperty); }
            set
            {
                SetValue(BacViewModelProperty, value);
                NotifyPropertyChanged("BacViewModel");
            }
        }

        public BacType5View()
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
