using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using XenoKit.ViewModel.BSA;

namespace XenoKit.Views.BSA
{
    public partial class BsaType6View : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel), typeof(BsaType6ViewModel), typeof(BsaType6View), new PropertyMetadata(null, ViewModelChanged));

        public BsaType6ViewModel ViewModel
        {
            get => (BsaType6ViewModel)GetValue(ViewModelProperty);
            set
            {
                SetValue(ViewModelProperty, value);
                NotifyPropertyChanged(nameof(ViewModel));
            }
        }

        public BsaType6View()
        {
            InitializeComponent();
            DataContext = ViewModel;
            BsaTab.BsaSubtypeSelectionChanged += BsaTab_BsaSubtypeSelectionChanged;
            RefreshVisibility();
        }

        private static void ViewModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            BsaType6View view = (BsaType6View)sender;
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
