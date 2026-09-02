using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using MahApps.Metro.Controls;
using Xv2CoreLib.Resource.App;
using LB_Common.Forms;

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

            if (!string.IsNullOrEmpty(_browser.SelectedPath))
            {
                if (File.Exists(string.Format("{0}/bin/DBXV2.exe", _browser.SelectedPath)))
                {
                    settings.GameDirectory = _browser.SelectedPath;
                }
                else
                {
                    MessagePrompt.Show("The entered game directory is not valid.\n\nPlease enter a valid directory. It should be the folder named \"DB Xenoverse 2\", and contain the bin and cpk folders within. You must select this FOLDER, not the game exe!", "Settings", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                }
            }
        }

        private void BrowseSave_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog _browser = new OpenFileDialog();
            _browser.Title = "Browse for DBXV2 Save File";
            _browser.Filter = "DNXV2 save file | *.sav";
            _browser.ShowDialog();

            if (!string.IsNullOrEmpty(_browser.FileName))
            {
                settings.SaveFile = _browser.FileName;
            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(string.Format("{0}/bin/DBXV2.exe", settings.GameDirectory)) || string.IsNullOrWhiteSpace(settings.GameDirectory))
            {
                MessagePrompt.Show("The entered game directory is not valid.\n\nPlease enter a valid directory. It should be the folder named \"DB Xenoverse 2\", and contain the bin and cpk folders within. You must select this FOLDER, not the game exe!", "Settings", MessagePromptButtons.OK, MessagePromptIcon.Error);
                return;
            }

            SettingsManager.Instance.SaveSettings(false);
            Close();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!File.Exists(string.Format("{0}/bin/DBXV2.exe", settings.GameDirectory)) || string.IsNullOrWhiteSpace(settings.GameDirectory))
            {
                MessagePrompt.Show("The entered game directory is not valid!\n\nSince XenoKit cannot function without knowing where the game is installed, the application will now exit.", "Settings", MessagePromptButtons.OK, MessagePromptIcon.Error);
                Environment.Exit(0);
            }
        }
    }
}
