using System.Windows;
using System.Windows.Controls;
using XenoKit.ViewModel.BCM;

namespace XenoKit.Views.BCM
{
    public partial class BcmEntryEditor : UserControl
    {
        public static readonly DependencyProperty BcmViewModelProperty = DependencyProperty.Register(
            nameof(BcmViewModel), typeof(BcmEntryViewModel), typeof(BcmEntryEditor), new PropertyMetadata(null, BcmViewModelChanged));

        public BcmEntryViewModel BcmViewModel
        {
            get => (BcmEntryViewModel)GetValue(BcmViewModelProperty);
            set => SetValue(BcmViewModelProperty, value);
        }

        public BcmEntryEditor()
        {
            InitializeComponent();
            RefreshVisibility();
        }

        private static void BcmViewModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((BcmEntryEditor)sender).RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            bool canEdit = BcmViewModel != null;
            emptyTextBlock.Visibility = canEdit ? Visibility.Collapsed : Visibility.Visible;
            tabControl.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
