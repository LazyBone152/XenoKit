using MonoGame.Framework.WpfInterop;

namespace WpfTest.Views
{
    /// <summary>
    /// Interaction logic for SimpleWindow.xaml
    /// </summary>
    public partial class SimpleWindow
    {
        #region Constructors

        public SimpleWindow(WpfGame scene, string title)
        {
            InitializeComponent();

            Title = title;
            RootGrid.Children.Add(scene);
        }

        #endregion
    }
}