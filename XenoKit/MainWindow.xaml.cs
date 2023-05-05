using ControlzEx.Theming;
using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.ComponentModel;
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
            if(sender is LogEntry logEntry)
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
            Files.Instance.LoadSkill(Xv2CoreLib.CUS.CUS_File.SkillType.Super);
        }

        public RelayCommand LoadUltimateSkillCommand => new RelayCommand(LoadUltimateSkill);
        private void LoadUltimateSkill()
        {
            Files.Instance.LoadSkill(Xv2CoreLib.CUS.CUS_File.SkillType.Ultimate);
        }

        public RelayCommand LoadEvasiveSkillCommand => new RelayCommand(LoadEvasiveSkill);
        private void LoadEvasiveSkill()
        {
            Files.Instance.LoadSkill(Xv2CoreLib.CUS.CUS_File.SkillType.Evasive);
        }

        public RelayCommand LoadBlastSkillCommand => new RelayCommand(LoadBlastSkill);
        private void LoadBlastSkill()
        {
            Files.Instance.LoadSkill(Xv2CoreLib.CUS.CUS_File.SkillType.Blast);
        }

        public RelayCommand LoadAwokenSkillCommand => new RelayCommand(LoadAwokenSkill);
        private void LoadAwokenSkill()
        {
            Files.Instance.LoadSkill(Xv2CoreLib.CUS.CUS_File.SkillType.Awoken);
        }

        public RelayCommand LoadMovesetCommand => new RelayCommand(LoadMoveset);
        private void LoadMoveset()
        {
            Files.Instance.LoadMoveset();
        }

        public RelayCommand LoadCharacterCommand => new RelayCommand(LoadCharacter);
        private void LoadCharacter()
        {
            Files.Instance.LoadCharacter();
        }
        #endregion

        #region SaveCommands
        public RelayCommand SaveCurrentCommand => new RelayCommand(SaveCurrent, CanSaveCurrent);
        private void SaveCurrent()
        {
            Files.Instance.SaveItem(Files.Instance.SelectedItem);
        }

        public RelayCommand SaveAllCommand => new RelayCommand(SaveAll, CanSaveAll);
        private void SaveAll()
        {
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

        #endregion

        #region Exit
        public RelayCommand ExitCommand => new RelayCommand(Exit);
        private async void Exit()
        {
            SettingsManager.Instance.SaveSettings();

            var dialog = await this.ShowMessageAsync("Exit", "Do you wish to exit? Any unsaved data will be lost!", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);

            if(dialog == MessageDialogResult.Affirmative)
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
            bool changed = SceneManager.SetSceneState(((TabControl)sender).SelectedIndex);

            if (!changed) return;

            if(SceneManager.CurrentSceneState == EditorTabs.Camera)
            {
                SceneManager.CameraSelectionChanged(cameraTabView.SelectedAnimation);
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
    }
}
