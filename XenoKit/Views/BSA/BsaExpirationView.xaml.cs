using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using XenoKit.ViewModel.BSA;

namespace XenoKit.Views.BSA
{
    public partial class BsaExpirationView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel), typeof(BsaExpirationViewModel), typeof(BsaExpirationView), new PropertyMetadata(null, ViewModelChanged));

        public BsaExpirationViewModel ViewModel
        {
            get => (BsaExpirationViewModel)GetValue(ViewModelProperty);
            set
            {
                SetValue(ViewModelProperty, value);
                NotifyPropertyChanged(nameof(ViewModel));
            }
        }

        public BsaExpirationView()
        {
            InitializeComponent();
            DataContext = ViewModel;
            BsaTab.BsaSubtypeSelectionChanged += BsaTab_BsaSubtypeSelectionChanged;
            RefreshVisibility();
        }

        private static void ViewModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            BsaExpirationView view = (BsaExpirationView)sender;
            view.DataContext = e.NewValue;
            view.RefreshVisibility();
        }

        private void BsaTab_BsaSubtypeSelectionChanged(object sender, EventArgs e)
        {
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            Visibility = ViewModel != null ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
