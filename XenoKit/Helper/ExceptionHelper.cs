using System;
using System.IO.Hashing;
using System.Collections.Generic;
using System.Text;
using XenoKit.Editor;

namespace XenoKit.Helper
{
    public static class ExceptionHelper
    {
        private static bool ErrorDialogShown = false;
        private static readonly XxHash3 XxHash = new XxHash3();
        private static readonly HashSet<ulong> ErrorHashes = new HashSet<ulong>(32);

        public static void HandleException(Exception e)
        {
            if (e == null) return;

            string exception = e.ToString();
            Log.Add($"Unhandled Exception: {e.Message}", exception, LogType.Error);

            if (!ErrorDialogShown && !Xv2CoreLib.Resource.App.SettingsManager.Instance.Settings.XenoKit_SuppressErrorsToLogOnly)
            {
                lock (XxHash)
                {
                    ulong hash = HashException(exception);

                    if (ErrorHashes.Contains(hash))
                    {
                        return;
                    }

                    ErrorHashes.Add(hash);
                }

                ErrorDialogShown = true;
                MainWindow window = (MainWindow)System.Windows.Application.Current.MainWindow;
                window.ShowException(e);
                ErrorDialogShown = false;
            }
        }

        private static ulong HashException(string exception)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(exception);
            XxHash.Reset();
            XxHash.Append(bytes);
            return XxHash.GetCurrentHashAsUInt64();
        } 
    }
}
