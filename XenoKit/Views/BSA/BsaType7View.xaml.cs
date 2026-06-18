using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.Engine;
using Xv2CoreLib;
using XenoKit.ViewModel.BSA;

namespace XenoKit.Views.BSA
{
    public partial class BsaType7View : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel), typeof(BsaType7ViewModel), typeof(BsaType7View), new PropertyMetadata(null, ViewModelChanged));

        public BsaType7ViewModel ViewModel
        {
            get => (BsaType7ViewModel)GetValue(ViewModelProperty);
            set
            {
                SetValue(ViewModelProperty, value);
                NotifyPropertyChanged(nameof(ViewModel));
            }
        }

        public BsaType7View()
        {
            InitializeComponent();
            DataContext = ViewModel;
            BsaTab.BsaSubtypeSelectionChanged += BsaTab_BsaSubtypeSelectionChanged;
            RefreshVisibility();
        }

        private static void ViewModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            BsaType7View view = (BsaType7View)sender;
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

        private void SoundPreview_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
                return;

            var acb = Files.Instance.GetAcbFile(GetBacAcbType(ViewModel.AcbType), Files.Instance.SelectedMove, SceneManager.Actors[0], true);

            if (acb == null)
            {
                Log.Add($"Could not find the ACB for AcbType {ViewModel.AcbType}. Preview failed.");
                return;
            }

            if (ViewModel.CueId != ushort.MaxValue)
                Viewport.Instance.AudioEngine.PreviewCue(ViewModel.CueId, acb);
        }

        private static Xv2CoreLib.BAC.AcbType GetBacAcbType(Xv2CoreLib.BSA.AcbType acbType)
        {
            switch (acbType)
            {
                case Xv2CoreLib.BSA.AcbType.Chara_SE:
                    return Xv2CoreLib.BAC.AcbType.Character_SE;
                case Xv2CoreLib.BSA.AcbType.Skill_SE:
                    return Xv2CoreLib.BAC.AcbType.Skill_SE;
                default:
                    return Xv2CoreLib.BAC.AcbType.Common_SE;
            }
        }
    }
}
