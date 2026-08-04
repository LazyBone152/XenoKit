using System.Windows;
using System.Windows.Controls;
using XenoKit.ViewModel.BSA;

namespace XenoKit.Views.BSA
{
    public partial class BsaExpirationView : UserControl
    {
        public static readonly DependencyProperty BsaViewModelProperty = DependencyProperty.Register(
            nameof(BsaViewModel), typeof(BsaExpirationViewModel), typeof(BsaExpirationView), new PropertyMetadata(null, BsaViewModelChanged));

        public BsaExpirationViewModel BsaViewModel
        {
            get => (BsaExpirationViewModel)GetValue(BsaViewModelProperty);
            set => SetValue(BsaViewModelProperty, value);
        }

        public BsaExpirationView()
        {
            InitializeComponent();
            Visibility = Visibility.Collapsed;
        }

        private static void BsaViewModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((BsaExpirationView)sender).Visibility = e.NewValue != null ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
