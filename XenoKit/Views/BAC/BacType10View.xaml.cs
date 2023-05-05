using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Controls;
using XenoKit.ViewModel.BAC;

namespace XenoKit.Views.BAC
{
    /// <summary>
    /// Interaction logic for BacType10View.xaml
    /// </summary>
    public partial class BacType10View : UserControl, INotifyPropertyChanged
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
            "BacViewModel", typeof(BACType10ViewModel), typeof(BacType10View), new PropertyMetadata(default(BACType10ViewModel)));

        public BACType10ViewModel BacViewModel
        {
            get { return (BACType10ViewModel)GetValue(BacViewModelProperty); }
            set
            {
                SetValue(BacViewModelProperty, value);
                NotifyPropertyChanged("BacViewModel");
            }
        }

        public BacType10View()
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
