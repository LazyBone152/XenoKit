using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using XenoKit.ViewModel.BSA;
using Xv2CoreLib.BSA;

namespace XenoKit.Views.BSA
{
    public partial class BsaCollisionView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel), typeof(BsaCollisionViewModel), typeof(BsaCollisionView), new PropertyMetadata(null, ViewModelChanged));

        public Array EepkTypes { get; } = Enum.GetValues(typeof(EepkType));

        public BsaCollisionViewModel ViewModel
        {
            get => (BsaCollisionViewModel)GetValue(ViewModelProperty);
            set
            {
                SetValue(ViewModelProperty, value);
                NotifyPropertyChanged(nameof(ViewModel));
            }
        }

        public BsaCollisionView()
        {
            InitializeComponent();
            DataContext = ViewModel;
            BsaTab.BsaSubtypeSelectionChanged += BsaTab_BsaSubtypeSelectionChanged;
            RefreshVisibility();
        }

        private static void ViewModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            BsaCollisionView view = (BsaCollisionView)sender;
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
