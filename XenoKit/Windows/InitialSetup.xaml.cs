using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
using Xv2CoreLib.Resource.App;

namespace XenoKit.Windows
{
    /// <summary>
    /// Interaction logic for InitialSetup.xaml
    /// </summary>
    public partial class InitialSetup : MetroWindow
    {
        public Settings settings { get; set; }

        public InitialSetup()
        {
            settings = SettingsManager.settings;
            InitializeComponent();
            DataContext = this;
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(String.Format("{0}/bin/DBXV2.exe", settings.GameDirectory)) || String.IsNullOrWhiteSpace(settings.GameDirectory))
            {
                MessageBox.Show("The entered game directory is not valid.\n\nPlease enter a valid directory. It should be the folder named \"DB Xenoverse 2\", and contain the bin and cpk folders within. You must select this FOLDER, not the game exe!", "Settings", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SettingsManager.Instance.SaveSettings(false);
            Close();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!File.Exists(String.Format("{0}/bin/DBXV2.exe", settings.GameDirectory)) || String.IsNullOrWhiteSpace(settings.GameDirectory))
            {
                MessageBox.Show("The entered game directory is not valid!\n\nSince XenoKit cannot function without knowing where the game is installed, the application will now exit.", "Settings", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
        }
    }
}
