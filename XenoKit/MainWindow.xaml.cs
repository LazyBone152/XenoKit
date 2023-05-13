using AutoUpdater;
using ControlzEx.Theming;
using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Windows;
using Xv2CoreLib;
using Xv2CoreLib.Resource.App;

namespace XenoKit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        public static System.Windows.Threading.Dispatcher dispatcher = null;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        #region Properties UI
        private string _currentLogMessage = null;
        public string CurrentLogMessage
        {
            get
            {
                return _currentLogMessage;
            }
            set
            {
                if (value != this._currentLogMessage)
                {
                    this._currentLogMessage = value;
                    NotifyPropertyChanged("CurrentLogMessage");
                }
            }
        }

        #endregion


        public MainWindow()
        {
            //Tooltips
            ToolTipService.ShowDurationProperty.OverrideMetadata(
            typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));

            DataContext = this;
            InitializeComponent();
            dispatcher = Dispatcher;

            //Init settings
            SettingsManager.Instance.CurrentApp = Xv2CoreLib.Resource.App.Application.XenoKit;
            SettingsManager.SettingsReloaded += SettingsManager_SettingsReloaded;

            InitTheme();
            InitGameDir();
            RegisterEvents();
            AsyncInit();

            //Set window size
            if (SettingsManager.Instance.Settings.XenoKit_WindowSizeX > MinWidth && SystemParameters.FullPrimaryScreenWidth >= SettingsManager.Instance.Settings.XenoKit_WindowSizeX)
                Width = SettingsManager.Instance.Settings.XenoKit_WindowSizeX;

            if (SettingsManager.Instance.Settings.XenoKit_WindowSizeY > MinHeight && SystemParameters.FullPrimaryScreenHeight >= SettingsManager.Instance.Settings.XenoKit_WindowSizeY)
                Height = SettingsManager.Instance.Settings.XenoKit_WindowSizeY;

            //Main Tab visibility. It should be invisible when nothing in the outliner is selected.
            mainTabControl.Visibility = Visibility.Hidden;
            Files.SelectedItemChanged += Files_SelectedMoveChanged;

            //Update title
            Title += $" ({SettingsManager.Instance.CurrentVersionString})";

            mainTabControl.SelectedIndex = 1;

            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void Files_SelectedMoveChanged(object sender, EventArgs e)
        {
            mainTabControl.Visibility = Files.Instance.SelectedItem != null ? Visibility.Visible : Visibility.Hidden;
        }

        #region Init
        private void SettingsManager_SettingsReloaded(object sender, EventArgs e)
        {
            InitTheme();

            if (sender is Settings oldSettings)
            {
                if (oldSettings.GameDirectory != SettingsManager.settings.GameDirectory && SettingsManager.settings.ValidGameDir)
                {
                    AsyncInit();
                }
            }
        }

        private async Task AsyncInit()
        {
            Files.Instance.Initialize(this);

            //Check for updates silently
#if !DEBUG
            CheckForUpdate(false);
#endif
        }

        private void RegisterEvents()
        {
            Log.LogEntryAddedEvent += new EventHandler(LogEntryAdded);
        }

        /// <summary>
        /// Ensure the game directory is set.
        /// </summary>
        private async void InitGameDir()
        {
            if (!SettingsManager.settings.ValidGameDir)
            {
                var gameDirWindow = new SetGameDir();
                gameDirWindow.ShowDialog();
            }

            if (!SettingsManager.settings.ValidGameDir)
            {
                await this.ShowMessageAsync("The game directory was not found. \n\nThe application will now close.", "Game Directory Not Found", MessageDialogStyle.Affirmative);
                //MessageBox.Show("The game directory was not found. \n\nThe application will now close.", "Game Directory Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // get the current app style (theme and accent) from the application
            // you can then use the current theme and custom accent instead set a new theme
            //Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);

            // now set the Green accent and dark theme
            //ThemeManager.ChangeAppStyle(Application.Current,
            //                            ThemeManager.GetAccent("Red"),
            //                           ThemeManager.GetAppTheme("BaseDark")); // or appStyle.Item1


        }

        public void InitTheme()
        {
            Dispatcher.Invoke((() =>
            {
                ThemeManager.Current.ChangeTheme(System.Windows.Application.Current, SettingsManager.Instance.GetTheme());
            }));
        }
        #endregion

        #region Events
        public void LogEntryAdded(object sender, EventArgs arg)
        {
            if (sender is LogEntry logEntry)
            {
                //Display it at bottom of screen
                CurrentLogMessage = logEntry.Message;
            }
            else
            {
                CurrentLogMessage = "";
            }
        }

        #endregion

        #region LoadCommands
        public RelayCommand LoadSuperSkillCommand => new RelayCommand(LoadSuperSkill);
        private void LoadSuperSkill()
        {
            Files.Instance.AsyncLoadSkill(Xv2CoreLib.CUS.CUS_File.SkillType.Super);
        }

        public RelayCommand LoadUltimateSkillCommand => new RelayCommand(LoadUltimateSkill);
        private void LoadUltimateSkill()
        {
            Files.Instance.AsyncLoadSkill(Xv2CoreLib.CUS.CUS_File.SkillType.Ultimate);
        }

        public RelayCommand LoadEvasiveSkillCommand => new RelayCommand(LoadEvasiveSkill);
        private void LoadEvasiveSkill()
        {
            Files.Instance.AsyncLoadSkill(Xv2CoreLib.CUS.CUS_File.SkillType.Evasive);
        }

        public RelayCommand LoadBlastSkillCommand => new RelayCommand(LoadBlastSkill);
        private void LoadBlastSkill()
        {
            Files.Instance.AsyncLoadSkill(Xv2CoreLib.CUS.CUS_File.SkillType.Blast);
        }

        public RelayCommand LoadAwokenSkillCommand => new RelayCommand(LoadAwokenSkill);
        private void LoadAwokenSkill()
        {
            Files.Instance.AsyncLoadSkill(Xv2CoreLib.CUS.CUS_File.SkillType.Awoken);
        }

        public RelayCommand LoadMovesetCommand => new RelayCommand(LoadMoveset);
        private void LoadMoveset()
        {
            Files.Instance.LoadMoveset();
        }

        public RelayCommand LoadCharacterCommand => new RelayCommand(LoadCharacter);
        private void LoadCharacter()
        {
            Files.Instance.AsyncLoadCharacter();
        }
        #endregion

        #region SaveCommands
        public RelayCommand SaveCurrentCommand => new RelayCommand(SaveCurrent, CanSaveCurrent);
        private void SaveCurrent()
        {
            Files.Instance.SaveItem(Files.Instance.SelectedItem);
        }

        public RelayCommand SaveAllCommand => new RelayCommand(SaveAll, CanSaveAll);
        private async void SaveAll()
        {
            var result = await this.ShowMessageAsync("Save All", "Save all files currently loaded in the outliner (except those marked as \"Read Only\"?", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);

            if (result == MessageDialogResult.Affirmative)
                Files.Instance.SaveAll();
        }

        private bool CanSaveAll()
        {
            return Files.Instance.OutlinerItems.Any(x => !x.ReadOnly);
        }

        private bool CanSaveCurrent()
        {
            if (Files.Instance.SelectedItem != null)
                return !Files.Instance.SelectedItem.ReadOnly;

            return false;
        }

        #endregion

        #region OtherCommands
        public RelayCommand SettingsCommand => new RelayCommand(ShowSettingsWindow);
        private void ShowSettingsWindow()
        {
            string originalGameDir = SettingsManager.Instance.Settings.GameDirectory;

            SettingsWindow settings = new SettingsWindow(this);
            settings.ShowDialog();
            SettingsManager.Instance.SaveSettings();
            InitTheme();

            //Reload game cpk stuff if directory was changed
            if (SettingsManager.Instance.Settings.GameDirectory != originalGameDir && SettingsManager.Instance.Settings.ValidGameDir)
            {
                AsyncInit();
            }
        }

        public RelayCommand FindReplaceCommand => new RelayCommand(FindReplace);
        private void FindReplace()
        {
            //Check if window is already open, and bring it into focus if it is.
            foreach (var window in App.Current.Windows)
            {
                if (window is FindAndReplace)
                {
                    ((FindAndReplace)window).Focus();
                    return;
                }
            }

            //Open a new one
            FindAndReplace find = new FindAndReplace(this);
            find.Show();
        }

        public RelayCommand CheckForUpdatesCommand => new RelayCommand(CheckForUpdates);
        private async void CheckForUpdates()
        {
            CheckForUpdate(true);
        }

        public RelayCommand GitHubCommand => new RelayCommand(GotoGitHub);
        private void GotoGitHub()
        {
            Process.Start("https://github.com/LazyBone152/XenoKit");
        }

        private async void CheckForUpdate(bool userInitiated)
        {
            //Check for update
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
                //Update is forced to appear even if notifications are disabled. Only to be used for the most vital updates.
                bool updateIsForced = appUpdate.ForceUpdate && !SettingsManager.settings.UpdateNotifications;

                var messageResult = await this.ShowMessageAsync(updateIsForced ? "Update Available (Forced)" : "Update Available", $"An update is available ({appUpdate.Version}). Do you want to download and install it?\n\nNote: All instances of the application will be closed and any unsaved work will be lost.\n\nChangelog:\n{appUpdate.Changelog}", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.ScrollDialog);

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
            }
            else if (userInitiated)
            {
                await this.ShowMessageAsync("Update", $"No update is available.", MessageDialogStyle.Affirmative, DialogSettings.Default);
            }
        }

        #endregion

        #region Exit
        public RelayCommand ExitCommand => new RelayCommand(Exit);
        private async void Exit()
        {
            SettingsManager.Instance.SaveSettings();

            var dialog = await this.ShowMessageAsync("Exit", "Do you wish to exit? Any unsaved data will be lost!", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);

            if (dialog == MessageDialogResult.Affirmative)
                Environment.Exit(0);

        }

        private async void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            SettingsManager.Instance.SaveSettings();

#if !DEBUG
            e.Cancel = true;

            var dialog = await this.ShowMessageAsync("Exit", "Do you wish to exit? Any unsaved data will be lost!", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);

            if (dialog == MessageDialogResult.Affirmative)
                Environment.Exit(0);
#endif
        }
        #endregion

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool changed = SceneManager.SetSceneState(mainTabControl.SelectedIndex, bcsTabControl.SelectedIndex, audioControl.audioTabControl.SelectedIndex);

            //Auto play bac entry if nothing is active
            if (SceneManager.CurrentSceneState == EditorTabs.Action)
            {
                bacControlView.AutoPlayBacEntry();
            }

            if (!changed) return;

            if (SceneManager.CurrentSceneState == EditorTabs.Camera)
            {
                SceneManager.CameraSelectionChanged(cameraTabView.SelectedEanFile, cameraTabView.SelectedAnimation);
            }
        }

        private void MenuItem_ReloadSystem_Click(object sender, RoutedEventArgs e)
        {
            Xenoverse2.Instance.RefreshSkills();
            Xenoverse2.Instance.RefreshCharacters();
        }

        private void WindowResized(object sender, SizeChangedEventArgs e)
        {
            SettingsManager.Instance.Settings.XenoKit_WindowSizeX = (int)e.NewSize.Width;
            SettingsManager.Instance.Settings.XenoKit_WindowSizeY = (int)e.NewSize.Height;
        }

        private async void MetroWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];

                if (Path.GetExtension(droppedFilePaths[0]) == ".nsk")
                {
                    Files.Instance.ManualLoad(droppedFilePaths);
                    return;
                }

                bool error = false;
                foreach (var drop in droppedFilePaths)
                {
                    switch (Path.GetExtension(drop))
                    {
                        case ".ean":
                        case ".acb":
                        case ".eepk":
                            Files.Instance.ManualLoad(drop);
                            break;
                        default:
                            if (!error)
                                await this.ShowMessageAsync("File Drop", $"The filetype of \"{drop}\" is not supported.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                            error = true;
                            break;
                    }
                }
            }
        }

        public async void ShowException(Exception ex)
        {
            var dialogSettings = DialogSettings.Default;
            dialogSettings.AffirmativeButtonText = "OK";
            dialogSettings.NegativeButtonText = "OK (Copy Full Error)";

            var dialog = await this.ShowMessageAsync("Unhandled Exception", string.Format("An error has occurred with the following message: {0}\n\nIf the error keeps occuring, consider opening an issue on GitHub and posting the details of the error (plus the full error message, using the copy button).\n\n(These error messages can be disabled in the settings menu, though that's not recommended. If disabled then they will still appear in the log, and can also be copied from there by right clicking)", ex.Message), MessageDialogStyle.AffirmativeAndNegative, dialogSettings);

            if (dialog == MessageDialogResult.Negative)
            {
                Clipboard.SetText(ex.ToString());
            }
        }
    }
}
