using System;
using System.Threading.Tasks;
using System.Windows;
using XenoKit.Editor;
using XenoKit.Helper;

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

            ExceptionHelper.HandleException(e.Exception);
            #endif
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
#if !DEBUG
            e.SetObserved();
            ExceptionHelper.HandleException(e.Exception);
#endif
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
#if !DEBUG
            ExceptionHelper.HandleException(e.ExceptionObject as Exception);
            #endif
        }
    }
}
