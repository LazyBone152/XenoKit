using System;
using System.Threading.Tasks;
using System.Windows;
using XenoKit.Editor;

namespace XenoKit
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Xv2CoreLib.Resource.App.SettingsManager.Instance.CurrentApp = Xv2CoreLib.Resource.App.Application.XenoKit;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            #if !DEBUG
            e.Handled = true;

            ExceptionHandler(e.Exception);
            #endif
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
#if !DEBUG
            e.SetObserved();
            ExceptionHandler(e.Exception);
#endif
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            #if !DEBUG
            ExceptionHandler(e.ExceptionObject as Exception);
            #endif
        }

        private void ExceptionHandler(Exception e)
        {
            Log.Add($"Unhandled Exception: {e.Message}", e.ToString(), LogType.Error);

            if (!Xv2CoreLib.Resource.App.SettingsManager.Instance.Settings.XenoKit_SuppressErrorsToLogOnly)
            {
                MainWindow window = (MainWindow)Application.Current.MainWindow;
                window.ShowException(e);
            }
        }
    }
}
