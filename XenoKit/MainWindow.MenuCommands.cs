using AutoUpdater;
using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Windows;
using Xv2CoreLib;
using Xv2CoreLib.Resource.App;

namespace XenoKit
{
    public partial class MainWindow
    {
        public RelayCommand SettingsCommand => new RelayCommand(ShowSettingsWindow);
        public RelayCommand FindReplaceCommand => new RelayCommand(FindReplace);
        public RelayCommand CheckForUpdatesCommand => new RelayCommand(CheckForUpdates);
        public RelayCommand GitHubCommand => new RelayCommand(GotoGitHub);

        private void ShowSettingsWindow()
        {
            string originalGameDir = SettingsManager.Instance.Settings.GameDirectory;
            int shadowMapRes = SettingsManager.Instance.Settings.XenoKit_ShadowMapRes;

            SettingsWindow settings = new SettingsWindow(this);
            settings.ShowDialog();
            SettingsManager.Instance.SaveSettings();
            LocalSettings.Save();
            InitTheme();

            // Reload game CPK data if the directory was changed.
            if (SettingsManager.Instance.Settings.GameDirectory != originalGameDir && SettingsManager.Instance.Settings.ValidGameDir)
            {
                AsyncInit();
            }

            if (shadowMapRes != SettingsManager.settings.XenoKit_ShadowMapRes)
            {
                Viewport.Instance.CompiledObjectManager.ForceShaderUpdate();
            }
        }

        private void FindReplace()
        {
            foreach (var window in App.Current.Windows)
            {
                if (window is FindAndReplace findAndReplace)
                {
                    findAndReplace.Focus();
                    return;
                }
            }

            FindAndReplace find = new FindAndReplace(this);
            find.Show();
        }

        private async void CheckForUpdates()
        {
            CheckForUpdate(true);
        }

        private void GotoGitHub()
        {
            Process.Start("https://github.com/LazyBone152/XenoKit");
        }

        private async void CheckForUpdate(bool userInitiated)
        {
            AppUpdate appUpdate = default;

            await Task.Run(() =>
            {
                appUpdate = Update.CheckForUpdate(AutoUpdater.App.XenoKit);
            });

            await Task.Delay(1000);

            if (Update.UpdateState == UpdateState.XmlDownloadFailed && userInitiated)
            {
                await this.ShowMessageAsync("Update Failed", "The AppUpdate XML file failed to download.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                return;
            }

            if (Update.UpdateState == UpdateState.XmlParseFailed && userInitiated)
            {
                await this.ShowMessageAsync("Update Failed", $"The AppUpdate XML file could not be parsed.\n\n{Update.FailedErrorMessage}", MessageDialogStyle.Affirmative, DialogSettings.Default);
                return;
            }

            if (!appUpdate.ForceUpdate && !SettingsManager.settings.UpdateNotifications && !userInitiated)
            {
                return;
            }

            if (appUpdate.HasUpdate)
            {
                MetroDialogSettings dialogSettings = DialogSettings.ScrollDialog;
                dialogSettings.FirstAuxiliaryButtonText = "Ignore";
                dialogSettings.AffirmativeButtonText = "Update";
                dialogSettings.NegativeButtonText = "Open in Browser";
                dialogSettings.DefaultButtonFocus = MessageDialogResult.Affirmative;

                MessageDialogResult messageResult = await this.ShowMessageAsync("Update Available", $"An update is available ({appUpdate.Version}). The application can automatically download and update itself (confirmation may be required), or you may also open the website in a browser and download the update manually. \n\nNote: All instances of the application will be closed and any unsaved work will be lost if Update is selected.\n\nChangelog:\n{appUpdate.Changelog}", MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, dialogSettings);

                if (messageResult == MessageDialogResult.Affirmative)
                {
                    var controller = await this.ShowProgressAsync("Update Available", "Downloading...", false, DialogSettings.Default);
                    controller.SetIndeterminate();

                    try
                    {
                        await Task.Run(() =>
                        {
                            Update.DownloadUpdate();
                        });
                    }
                    finally
                    {
                        await controller.CloseAsync();
                    }

                    if (Update.UpdateState == UpdateState.DownloadSuccess)
                    {
                        Update.UpdateApplication();
                    }
                    else if (Update.UpdateState == UpdateState.DownloadFail)
                    {
                        await this.ShowMessageAsync("Download Failed", Update.FailedErrorMessage, MessageDialogStyle.Affirmative, DialogSettings.Default);
                    }

                }
                else if (messageResult == MessageDialogResult.Negative)
                {
                    Process.Start("https://github.com/LazyBone152/XenoKit/releases");
                }
            }
            else if (userInitiated)
            {
                await this.ShowMessageAsync("Update", $"No update is available.", MessageDialogStyle.Affirmative, DialogSettings.Default);
            }
        }

        private void MenuItem_ReloadSystem_Click(object sender, RoutedEventArgs e)
        {
            Xenoverse2.Instance.RefreshSkills();
            Xenoverse2.Instance.RefreshCharacters();
        }

        private void DebugMenu_ForceGC(object sender, RoutedEventArgs e)
        {
            Viewport.Instance.CompiledObjectManager.RemoveDeadObjects();
            GC.Collect();

            Log.Add("GC initiated", LogType.Debug);
        }

        private void DebugMenu_ReloadShaders_Click(object sender, RoutedEventArgs e)
        {
            Viewport.Instance.CompiledObjectManager.ForceShaderUpdate();
        }

        private void DebugMenu_DumpRenderTargets_Click(object sender, RoutedEventArgs e)
        {
            Viewport.Instance.RenderSystem.DumpRenderTargetsNextFrame = true;
        }

        private void DebugMenu_DumpShadowMap_Click(object sender, RoutedEventArgs e)
        {
            Viewport.Instance.RenderSystem.DumpShadowMapNextFrame = true;
        }

        private void DebugMenu_TestClick(object sender, RoutedEventArgs e)
        {
            int count = 250000000;

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                System.Numerics.Vector3 vector1 = new System.Numerics.Vector3(5f, -2f, 10f);
                System.Numerics.Vector3 vector2 = new System.Numerics.Vector3(50f, 2f, 20f);
                System.Numerics.Vector3 vector4 = new System.Numerics.Vector3(2, 2f, 2);
                Vector3 vector3 = Vector3.Add(vector1, vector2);
                Vector3 vector5 = Vector3.Multiply(vector3, vector4);

                System.Numerics.Matrix4x4 mat = Matrix4x4.CreateTranslation(vector5);
                var mat2 = Matrix4x4.CreateScale(new Vector3(1, 2, 4));
                var mat3 = Matrix4x4.Multiply(mat, mat2);
            }

            sw.Stop();
            TimeSpan standardElapsed = sw.Elapsed;
            sw.Restart();

            for (int i = 0; i < count; i++)
            {
                System.Numerics.Vector3 vector1 = new System.Numerics.Vector3(5f, -2f, 10f);
                System.Numerics.Vector3 vector2 = new System.Numerics.Vector3(50f, 2f, 20f);
                System.Numerics.Vector3 vector4 = new System.Numerics.Vector3(2, 2f, 2);
                Vector3 vector3 = vector1 + vector2;
                Vector3 vector5 = vector3 * vector4;

                System.Numerics.Matrix4x4 mat = Matrix4x4.CreateTranslation(vector5);
                var mat2 = Matrix4x4.CreateScale(new Vector3(1, 2, 4));
                var mat3 = mat * mat2;
            }

            sw.Stop();
            TimeSpan methodsElapsed = sw.Elapsed;
            sw.Restart();

            for (int i = 0; i < count; i++)
            {
                Microsoft.Xna.Framework.Vector3 vector1 = new Microsoft.Xna.Framework.Vector3(5f, -2f, 10f);
                Microsoft.Xna.Framework.Vector3 vector2 = new Microsoft.Xna.Framework.Vector3(50f, 2f, 20f);
                Microsoft.Xna.Framework.Vector3 vector4 = new Microsoft.Xna.Framework.Vector3(2, 2f, 2);
                Microsoft.Xna.Framework.Vector3 vector3 = vector1 + vector2;
                Microsoft.Xna.Framework.Vector3 vector5 = vector3 * vector4;

                Microsoft.Xna.Framework.Matrix mat = Microsoft.Xna.Framework.Matrix.CreateTranslation(vector5);
                var mat2 = Microsoft.Xna.Framework.Matrix.CreateScale(new Microsoft.Xna.Framework.Vector3(1, 2, 4));
                var mat3 = mat * Microsoft.Xna.Framework.Matrix.CreateScale(new Microsoft.Xna.Framework.Vector3(1, 2, 4));
            }

            sw.Stop();
            TimeSpan xnaElapsed = sw.Elapsed;
            sw.Restart();

            for (int i = 0; i < count; i++)
            {
                Microsoft.Xna.Framework.Vector3 vector1 = new Microsoft.Xna.Framework.Vector3(5f, -2f, 10f);
                Microsoft.Xna.Framework.Vector3 vector2 = new Microsoft.Xna.Framework.Vector3(50f, 2f, 20f);
                Microsoft.Xna.Framework.Vector3 vector4 = new Microsoft.Xna.Framework.Vector3(2, 2f, 2);
                Microsoft.Xna.Framework.Vector3.Add(ref vector1, ref vector2, out Microsoft.Xna.Framework.Vector3 vector3);
                Microsoft.Xna.Framework.Vector3.Multiply(ref vector3, ref vector4, out Microsoft.Xna.Framework.Vector3 vector5);

                Microsoft.Xna.Framework.Matrix mat = Microsoft.Xna.Framework.Matrix.CreateTranslation(vector5);
                var mat2 = Microsoft.Xna.Framework.Matrix.CreateScale(new Microsoft.Xna.Framework.Vector3(1, 2, 4));

                Microsoft.Xna.Framework.Matrix.Multiply(ref mat, ref mat2, out Microsoft.Xna.Framework.Matrix mat3);
            }

            sw.Stop();
            TimeSpan xnaMethodsElapsed = sw.Elapsed;

            Log.Add($"SIMD: {System.Numerics.Vector.IsHardwareAccelerated}");
            Log.Add($"Numerics = Standard: {standardElapsed}, Method: {methodsElapsed}");
            Log.Add($"XNA = Standard: {xnaElapsed}, Method: {xnaMethodsElapsed}");
        }
    }
}
