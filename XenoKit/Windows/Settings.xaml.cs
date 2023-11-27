using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using XenoKit.Engine;
using XenoKit.Engine.Shader;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Windows
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : MetroWindow, INotifyPropertyChanged
    {

        #region NotPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private MainWindow _parent;
        public Settings settings { get; set; }

        public Visibility LightAccentVisibility { get { return (settings.GetCurrentTheme() == AppTheme.Light) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility DarkAccentVisibility { get { return (settings.GetCurrentTheme() == AppTheme.Dark) ? Visibility.Visible : Visibility.Collapsed; } }

        public bool EnableRimlight
        {
            get => SettingsManager.settings.XenoKit_RimLightingEnabled;
            set
            {
                if (SettingsManager.settings.XenoKit_RimLightingEnabled != value)
                {
                    SettingsManager.settings.XenoKit_RimLightingEnabled = value;
                    SceneManager.MainGameBase.ShaderManager.ClearGlobalSampler(15);
                }
            }
        }
        public bool EnableOutline
        {
            get => SettingsManager.settings.XenoKit_UseOutlinePostEffect;
            set
            {
                if (SettingsManager.settings.XenoKit_UseOutlinePostEffect != value)
                {
                    SettingsManager.settings.XenoKit_UseOutlinePostEffect = value;
                }
            }
        }
        public bool DynamicLighting
        {
            get => SettingsManager.settings.XenoKit_EnableDynamicLighting;
            set
            {
                if (SettingsManager.settings.XenoKit_EnableDynamicLighting != value)
                {
                    SettingsManager.settings.XenoKit_EnableDynamicLighting = value;
                }
            }
        }
        public bool SuppressErrors
        {
            get => SettingsManager.settings.XenoKit_SuppressErrorsToLogOnly;
            set
            {
                if (SettingsManager.settings.XenoKit_SuppressErrorsToLogOnly != value)
                {
                    SettingsManager.settings.XenoKit_SuppressErrorsToLogOnly = value;
                }
            }
        }

        public int ShadowMapRes
        {
            get
            {
                switch (settings.XenoKit_ShadowMapRes)
                {
                    case 2048:
                        return 0;
                    case 4096:
                        return 1;
                    case 8192:
                        return 2;
                    default:
                        return 0;
                }
            }
            set
            {
                switch (value)
                {
                    case 0:
                        settings.XenoKit_ShadowMapRes = 2048;
                        break;
                    case 1:
                        settings.XenoKit_ShadowMapRes = 4096;
                        break;
                    case 2:
                        settings.XenoKit_ShadowMapRes = 8192;
                        break;
                }
            }
        }
        public int SuperSampling
        {
            get
            {
                switch (settings.XenoKit_SuperSamplingFactor)
                {
                    case 1:
                        return 0;
                    case 2:
                        return 1;
                    case 4:
                        return 2;
                    //case 8:
                    //    return 3;
                    default:
                        return 0;
                }
            }
            set
            {
                switch (value)
                {
                    case 0:
                        settings.XenoKit_SuperSamplingFactor = 1;
                        break;
                    case 1:
                        settings.XenoKit_SuperSamplingFactor = 2;
                        break;
                    case 2:
                        settings.XenoKit_SuperSamplingFactor = 4;
                        break;
                    case 3:
                        settings.XenoKit_SuperSamplingFactor = 8;
                        break;
                }
            }
        }

        private string OriginalGameDir;

        public SettingsWindow(MainWindow parent)
        {
            settings = SettingsManager.Instance.Settings;
            _parent = parent;
            InitializeComponent();
            Owner = System.Windows.Application.Current.MainWindow;
            DataContext = this;
            SettingsManager.SettingsReloaded += SettingsManager_SettingsReloaded;
            OriginalGameDir = settings.GameDirectory;
        }

        ~SettingsWindow()
        {
            SettingsManager.SettingsReloaded -= SettingsManager_SettingsReloaded;
        }

        private void SettingsManager_SettingsReloaded(object sender, EventArgs e)
        {
            settings = SettingsManager.Instance.Settings;
            NotifyPropertyChanged(nameof(settings));
            ThemeRadioButtons_CheckChanged(null, null);
        }


        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            Ookii.Dialogs.Wpf.VistaFolderBrowserDialog _browser = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            _browser.UseDescriptionForTitle = true;
            _browser.Description = "Browse for DBXV2 Directory";
            _browser.ShowDialog();

            if (!String.IsNullOrEmpty(_browser.SelectedPath))
            {
                if (File.Exists(String.Format("{0}/bin/DBXV2.exe", _browser.SelectedPath)))
                {
                    settings.GameDirectory = _browser.SelectedPath;
                }
                else
                {
                    MessageBox.Show(this, "The entered game directory is not valid.\n\nPlease enter a valid directory. It should be the folder named \"DB Xenoverse 2\", and contain the bin and cpk folders within. You must select this FOLDER, not the game exe!", "Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BrowseSave_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog _browser = new OpenFileDialog();
            _browser.Title = "Browse for DBXV2 Save File";
            _browser.Filter = "DNXV2 save file | *.sav";
            _browser.ShowDialog();

            if (!String.IsNullOrEmpty(_browser.FileName))
            {
                settings.SaveFile = _browser.FileName;
            }

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!settings.ValidGameDir)
            {
                settings.GameDirectory = OriginalGameDir;
                MessageBox.Show("The entered game directory is not valid and it will be removed, with the original directory being restored.", "Settings", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ThemeRadioButtons_CheckChanged(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(nameof(LightAccentVisibility));
            NotifyPropertyChanged(nameof(DarkAccentVisibility));

            _parent.InitTheme();
        }

        private void ThemeAccentComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _parent.InitTheme();
        }
    }
}
