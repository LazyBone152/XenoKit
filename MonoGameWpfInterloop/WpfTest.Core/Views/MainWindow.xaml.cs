using MonoGame.Framework.WpfInterop;
using System.Windows;
using WpfTest.Scenes;

namespace WpfTest.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Opens the window once.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private static void OpenWindow<T>() where T : Window, new()
        {
            var w = new T();
            w.Show();
        }

        private static void OpenCustomWindow(WpfGame game, string title)
        {
            var w = new SimpleWindow(game, title);
            w.Show();
        }

        private void OpenNewWindow(object sender, RoutedEventArgs e)
        {
            OpenCustomWindow(new CubeDemoScene(), "Cube demo scene");
        }

        private void OpenTextInputWindow(object sender, RoutedEventArgs e)
        {
            OpenWindow<TextInputWindow>();
        }

        private void OpenMultipleGameWindow(object sender, RoutedEventArgs e)
        {
            OpenWindow<MultiSceneWindow>();
        }

        private void OpenTabbedGameWindow(object sender, RoutedEventArgs e)
        {
            // manually reset counters so we always have the same id's per tab
            TabScene.Counter = 0;
            OpenWindow<TabWindow>();
        }

        private void OpenRendertargetGameWindow(object sender, RoutedEventArgs e)
        {
            OpenCustomWindow(new RenderTargetScene(), "Rendertarget scene");
        }

        private void OpenCloseableTabWindow(object sender, RoutedEventArgs e)
        {
            // manually reset counters so we always have the same id's per tab
            TabScene.Counter = 0;
            OpenWindow<CloseableTabWindow>();
        }

        private void OpenModelWindow(object sender, RoutedEventArgs e)
        {
            OpenCustomWindow(new ModelScene(), "Model scene");
        }

        private void OpenMsaaWindow(object sender, RoutedEventArgs e)
        {
            OpenCustomWindow(new MsaaScene(), "MSAA scene");
        }

        private void OpenMultipleMsaaWindow(object sender, RoutedEventArgs e)
        {
            OpenWindow<MultiMsaaWindow>();
        }

        private void OpenDpiScalingWindow(object sender, RoutedEventArgs e)
        {
            OpenWindow<DpiScalingWindow>();
        }

        private void OpenSpriteWindow(object sender, RoutedEventArgs e)
        {
            OpenCustomWindow(new SpriteScene(), "Sprite scene");
        }
    }
}
