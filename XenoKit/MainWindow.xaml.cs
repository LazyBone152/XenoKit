using ControlzEx.Theming;
using GalaSoft.MvvmLight.CommandWpf;
using LB_Common.Forms;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Windows;
using Xv2CoreLib.Resource.App;

namespace XenoKit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
        public Visibility DebugMenuVisible { get; set; } = Visibility.Hidden;

        #endregion

        public Stopwatch sw;

        public MainWindow()
        {
#if DEBUG
            DebugMenuVisible = Visibility.Visible;
#endif
            sw = Stopwatch.StartNew();
            //Force en-US culture accross whole application to ensure error messages will always be in english
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            //Tooltips
            ToolTipService.ShowDurationProperty.OverrideMetadata(
            typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));

            DataContext = this;
            InitializeComponent();

            //Init settings
            SettingsManager.Instance.CurrentApp = Xv2CoreLib.Resource.App.Application.XenoKit;
            SettingsManager.SettingsReloaded += SettingsManager_SettingsReloaded;

            InitTheme();
            InitGameDir();
            Log.LogEntryAddedEvent += LogEntryAdded;

            //Set window size
            if (SettingsManager.Instance.Settings.XenoKit_WindowSizeX > MinWidth && SystemParameters.FullPrimaryScreenWidth >= SettingsManager.Instance.Settings.XenoKit_WindowSizeX)
                Width = SettingsManager.Instance.Settings.XenoKit_WindowSizeX;

            if (SettingsManager.Instance.Settings.XenoKit_WindowSizeY > MinHeight && SystemParameters.FullPrimaryScreenHeight >= SettingsManager.Instance.Settings.XenoKit_WindowSizeY)
                Height = SettingsManager.Instance.Settings.XenoKit_WindowSizeY;

            if (SettingsManager.Instance.Settings.XenoKit_WindowMaximized)
                WindowState = WindowState.Maximized;

            //Main Tab visibility. It should be invisible when nothing in the outliner is selected.
            mainTabControl.Visibility = Visibility.Hidden;
            Files.SelectedItemChanged += Files_SelectedMoveChanged;
            TabManager.SetTabContext(mainTabControl);

            //Update title
            Title += $" ({SettingsManager.Instance.CurrentVersionString})";

            mainTabControl.SelectedIndex = 1;
            eepkEditor.SelectedEffectTabChanged += EepkEditor_SelectedEffectTabChanged;
            Closing += MainWindow_Closing;
        }

        private async void MetroWindow_ContentRendered(object sender, EventArgs e)
        {
            await AsyncInit();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Exit();
        }

        private void Files_SelectedMoveChanged(object sender, EventArgs e)
        {
            mainTabControl.Visibility = Files.Instance.SelectedItem != null ? Visibility.Visible : Visibility.Hidden;

            //Change tabs based on selected item type
            if (Files.Instance.SelectedItem != null)
            {
                switch (Files.Instance.SelectedItem.Type)
                {
                    case OutlinerItem.OutlinerItemType.CaC:
                        mainTabControl.SelectedIndex = (int)MainEditorTabs.CAC;
                        break;
                    case OutlinerItem.OutlinerItemType.Inspector:
                        mainTabControl.SelectedIndex = (int)MainEditorTabs.Inspector;
                        break;
                }

                effectTab_EepkComboBox.Visibility = Files.Instance.SelectedItem.Type == OutlinerItem.OutlinerItemType.CMN ? Visibility.Visible : Visibility.Collapsed;

                //Set visibility of the bac file selection combobox on the bac tab. This should only appear for CMN and awoken skills.
                bacControlView.bacFileSelector_StackPanel.Visibility = Files.Instance.SelectedItem.Type == OutlinerItem.OutlinerItemType.CMN ||
                    (Files.Instance.SelectedItem.Type == OutlinerItem.OutlinerItemType.Skill && Files.Instance.SelectedItem.move.SkillType == Xv2CoreLib.CUS.CUS_File.SkillType.Awoken)
                    ? Visibility.Visible : Visibility.Collapsed;
            }

            DetectInvalidTab();
            UpdateSelectedTab();
        }

        private void DetectInvalidTab()
        {
            if (mainTabControl.SelectedIndex != -1)
            {
                if(mainTabControl.Items[mainTabControl.SelectedIndex] is TabItem tabItem)
                {
                    if(tabItem.Visibility == Visibility.Collapsed)
                    {
                        foreach(var item in mainTabControl.Items)
                        {
                            if(item is TabItem _tabItem)
                            {
                                if(_tabItem.Visibility == Visibility.Visible)
                                {
                                    mainTabControl.SelectedIndex = mainTabControl.Items.IndexOf(item);
                                    return;
                                }
                            }
                        }

                        mainTabControl.SelectedIndex = -1;
                    }

                }
            }
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
            Log.Add("Finished init at " + sw.Elapsed, LogType.Debug);

        }

        /// <summary>
        /// Ensure the game directory is set.
        /// </summary>
        private async void InitGameDir()
        {
            if (!SettingsManager.settings.ValidGameDir)
            {
                InitialSetup setup = new InitialSetup();
                setup.ShowDialog();
            }

            if (!SettingsManager.settings.ValidGameDir)
            {
                MessagePrompt.Show("The game directory was not found. \n\nThe application will now close.", "Game Directory Not Found", MessagePromptButtons.OK, MessagePromptIcon.Error);
                Environment.Exit(0);
            }
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

        private void LogMessage_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            sceneTab.SelectedIndex = 0;
            logView.SelectedEntry = Log.Entries.Count > 0 ? Log.Entries[0] : null;
            logView.dataGrid.ScrollIntoView(logView.SelectedEntry);
        }

        #endregion

        #region Exit
        public RelayCommand ExitCommand => new RelayCommand(Exit);
        private void Exit()
        {
            if (MessagePrompt.Show("Do you wish to exit? Any unsaved data will be lost!", "Exit", MessagePromptButtons.YesNo, MessagePromptIcon.Question, true) == MessagePromptResult.Yes)
            {
                SettingsManager.Instance.SaveSettings(false);
                LocalSettings.Save();
                Environment.Exit(0);
            }
        }
        #endregion

        private void EepkEditor_SelectedEffectTabChanged(object sender, EventArgs e)
        {
            UpdateSelectedTab();
        }

        private void WindowResized(object sender, SizeChangedEventArgs e)
        {
            SettingsManager.Instance.Settings.XenoKit_WindowSizeX = (int)e.NewSize.Width;
            SettingsManager.Instance.Settings.XenoKit_WindowSizeY = (int)e.NewSize.Height;
            SettingsManager.Instance.Settings.XenoKit_WindowMaximized = WindowState == WindowState.Maximized;
        }

        private void MetroWindow_StateChanged(object sender, EventArgs e)
        {
            SettingsManager.Instance.Settings.XenoKit_WindowMaximized = WindowState == WindowState.Maximized;
        }

        private void MetroWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                bool hasModelFiles = droppedFilePaths.Any(x => Path.GetExtension(x) == ".emd" || Path.GetExtension(x) == ".esk" || Path.GetExtension(x) == ".emo" || Path.GetExtension(x) == ".emm" || Path.GetExtension(x) == ".emb" || Path.GetExtension(x) == ".nsk" || Path.GetExtension(x) == ".emg");

                if (hasModelFiles || SceneManager.IsOnInspectorTab)
                {
                    inspectorView.ProcessFileDrop(droppedFilePaths);

                    if (!SceneManager.IsOnInspectorTab)
                    {
                        //Go to inspector tab if not already there
                        Files.Instance.SelectedItem = Files.Instance.OutlinerItems[0];
                        mainTabControl.SelectedIndex = (int)MainEditorTabs.Inspector;
                    }
                }
                else
                {
                    Files.Instance.ProcessFileDrop(droppedFilePaths);
                }
            }
        }

        public void ShowTextForm()
        {
            MessagePromptResult result = MessagePrompt.Show("Exception Thrown", "The program has encountered an exception with the following error message. (These error messages can be disabled in the settings menu. When disabled they will still appear in the log, and can also be copied from there by right clicking)",
                MessagePromptButtons.OK, MessagePromptIcon.Error, "ajsbfjkasdbfjkasdbfkjasfdlknaslfk/nafsfasfasfasfas/nasdfasfsafsaf", "OK", null, "Copy Message", null);

            if (result == MessagePromptResult.Negative)
            {
                //Clipboard.SetText("ajsbfjkasdbfjkasdbfkjasfdlknaslfk/nafsfasfasfasfas/nasdfasfsafsaf");
            }
        }

        public async void ShowException(Exception ex)
        {
            MessagePromptResult result = MessagePrompt.Show("Exception Thrown", "The program has encountered an exception with the following error message. (These error messages can be disabled in the settings menu. When disabled they will still appear in the log, and can also be copied from there by right clicking)",
                MessagePromptButtons.OK, MessagePromptIcon.Error, ex.Message, "OK", null, "Copy Message", null);

            if(result == MessagePromptResult.Negative)
            {
                Clipboard.SetText(ex.ToString());
            }
        }

    }
}
