using System;

namespace WpfTest.Views
{
    /// <summary>
    /// Interaction logic for MultiSceneWindow.xaml
    /// </summary>
    public partial class MultiSceneWindow
    {
        public MultiSceneWindow()
        {
            InitializeComponent();
            // showcase FPS limit
            // WPF limits everything to 60 fps max, but we still can run with less WPF if desired
            CinematicExperience.TargetElapsedTime = TimeSpan.FromSeconds(1 / 30.0);
            LowFps.TargetElapsedTime = TimeSpan.FromSeconds(1 / 20.0);
        }
    }
}