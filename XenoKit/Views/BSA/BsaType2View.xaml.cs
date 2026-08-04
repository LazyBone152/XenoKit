using System.Windows;
using System.Windows.Controls;
using XenoKit.ViewModel.BSA;

namespace XenoKit.Views.BSA
{
    public partial class BsaType2View : UserControl
    {
        public static readonly DependencyProperty BsaViewModelProperty = DependencyProperty.Register(
            nameof(BsaViewModel), typeof(BsaType2ViewModel), typeof(BsaType2View), new PropertyMetadata(null, BsaViewModelChanged));

        public BsaType2ViewModel BsaViewModel
        {
            get => (BsaType2ViewModel)GetValue(BsaViewModelProperty);
            set => SetValue(BsaViewModelProperty, value);
        }

        public BsaType2View()
        {
            InitializeComponent();
            Visibility = Visibility.Collapsed;
        }

        private static void BsaViewModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((BsaType2View)sender).Visibility = e.NewValue != null ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
