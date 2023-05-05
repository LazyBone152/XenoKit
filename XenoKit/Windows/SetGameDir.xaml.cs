using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using XenoKit.Editor;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Windows
{
    /// <summary>
    /// Interaction logic for SetGameDir.xaml
    /// </summary>
    public partial class SetGameDir : MetroWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string _gameDir = null;

        public string GameDir
        {
            get
            {
                return this._gameDir;
            }
            set
            {
                if (value != this._gameDir)
                {
                    this._gameDir = value;
                    NotifyPropertyChanged(nameof(GameDir));
                }
            }
        }

        public SetGameDir()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsManager.IsGameDirValid(GameDir))
            {
                SettingsManager.Instance.Settings.GameDirectory = GameDir;
                SettingsManager.Instance.SaveSettings();
                Close();
            }
            else
            {
                MessageBox.Show("The entered directory is invalid.\n\nThe selected directory should be named \"DB Xenoverse 2\".", "Set Game Directory", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }

        private void Button_Browse_Click(object sender, RoutedEventArgs e)
        {
            var _browser = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            _browser.UseDescriptionForTitle = true;
            _browser.Description = "Browse for DBXV2 Directory";
            _browser.ShowDialog();

            if (!String.IsNullOrEmpty(_browser.SelectedPath))
            {
                if (File.Exists(String.Format("{0}/bin/DBXV2.exe", _browser.SelectedPath)))
                {
                    GameDir = _browser.SelectedPath;
                }
                else
                {
                    MessageBox.Show(this, "The entered game directory was not valid.\n\nThe selected directory should be named \"DB Xenoverse 2\".", "Set Game Directory", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}
