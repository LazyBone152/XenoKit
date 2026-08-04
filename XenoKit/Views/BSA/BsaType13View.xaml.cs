using System.Windows;
using System.Windows.Controls;
using XenoKit.ViewModel.BSA;

namespace XenoKit.Views.BSA
{
    public partial class BsaType13View : UserControl
    {
        public static readonly DependencyProperty BsaViewModelProperty = DependencyProperty.Register(
            nameof(BsaViewModel), typeof(BsaType13ViewModel), typeof(BsaType13View), new PropertyMetadata(null, BsaViewModelChanged));

        public BsaType13ViewModel BsaViewModel
        {
            get => (BsaType13ViewModel)GetValue(BsaViewModelProperty);
            set => SetValue(BsaViewModelProperty, value);
        }

        public BsaType13View()
        {
            InitializeComponent();
            Visibility = Visibility.Collapsed;
        }

        private static void BsaViewModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((BsaType13View)sender).Visibility = e.NewValue != null ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
