using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls;
using System;
using System.Linq;
using System.Windows;
using Xv2CoreLib.BCS;

namespace XenoKit.Windows
{
    /// <summary>
    /// Interaction logic for BcsColorSelector.xaml
    /// </summary>
    public partial class BcsColorSelector : MetroWindow
    {
        public PartColor ColorGroup { get; set; }
        public bool Finished { get; private set; }
        public int SelectedValue { get; set; } = -1;

        public BcsColorSelector(BCS_File bcsFile, int colorGroup, Window parent = null) : base()
        {
            ColorGroup = bcsFile.PartColors.FirstOrDefault(x => x.ID == colorGroup);
            Title += $" ({ColorGroup?.Name})";
            Owner = parent != null ? parent : Application.Current.MainWindow;
            InitializeComponent();
        }

        public RelayCommand DoneCommand => new RelayCommand(Done, CanBeDone);
        private void Done()
        {
            Finished = true;
            Close();
        }

        private bool CanBeDone()
        {
            return colorListBox.SelectedItem != null;
        }

        private void colorListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if(colorListBox?.SelectedItem != null && SelectedValue != -1)
            {
                Finished = true;
                Close();
            }
        }
    }
}
