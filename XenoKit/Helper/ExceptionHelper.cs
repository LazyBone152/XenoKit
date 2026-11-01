using System;
using System.IO.Hashing;
using System.Collections.Generic;
using System.Text;
using XenoKit.Editor;
using Xv2CoreLib.Resource.App;
using XenoKit.Engine;

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

            if (e.InnerException is SharpDX.SharpDXException)
            {
                //GPU Device crashes can occur if shadow res is too high.
                int newShadowMapRes = Viewport.Instance.GetMaximumTextureSize();

                if (newShadowMapRes < SettingsManager.Instance.Settings.XenoKit_ShadowMapRes)
                {
                    SettingsManager.Instance.Settings.XenoKit_ShadowMapRes = newShadowMapRes;
                    Log.Add("Shadow map resolution was too high and caused the graphics device to crash. It has been reset to a safe level, but XenoKit needs to be restarted.", LogType.Error);
                }
            }

            if (!ErrorDialogShown && !SettingsManager.Instance.Settings.XenoKit_SuppressErrorsToLogOnly)
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
