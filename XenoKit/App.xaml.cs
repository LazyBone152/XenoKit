using System.Windows;
using XenoKit.Editor;

namespace XenoKit
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
#if !DEBUG
            Log.Add($"Unhandled Exception: {e.Exception.Message}", e.Exception.ToString(), LogType.Error);
            e.Handled = true;

            if (!Xv2CoreLib.Resource.App.SettingsManager.Instance.Settings.XenoKit_SuppressErrorsToLogOnly)
            {
                MainWindow window = (MainWindow)Application.Current.MainWindow;
                window.ShowException(e.Exception);

            }
#endif
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Xv2CoreLib.Resource.App.SettingsManager.Instance.CurrentApp = Xv2CoreLib.Resource.App.Application.XenoKit;
        }
    }
}
