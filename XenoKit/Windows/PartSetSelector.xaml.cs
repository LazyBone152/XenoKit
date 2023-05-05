using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Xv2CoreLib.BCS;

namespace XenoKit.Windows
{
    public partial class PartSetSelector : MetroWindow
    {

        public BCS_File bcsFile { get; set; }

        /// <summary>
        /// Base PartSet
        /// </summary>
        public PartSet SelectedPartSet { get; set; }
        /// <summary>
        /// Transformed PartSet
        /// </summary>
        private PartSet SelectedSecondaryPartSet { get; set; }

        public bool OnlyLoadFromCPK { get; set; }

        public PartSetSelector(BCS_File _bcsFile, Window parent)
        {
            InitializeComponent();
            DataContext = this;
            Owner = parent;
            bcsFile = _bcsFile;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Validate();
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && (listBox.SelectedItem as PartSet) != null)
            {
                Validate();
            }
        }

        private void ListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter) && (listBox.SelectedItem as PartSet) != null)
            {
                Validate();
            }
        }

        private void Validate()
        {
            if(listBox.SelectedItems.Count == 2)
            {
                SelectedPartSet = listBox.SelectedItems[0] as PartSet;
                SelectedSecondaryPartSet = listBox.SelectedItems[1] as PartSet;

                //Overwrite entries from first with the second
                if (SelectedSecondaryPartSet.FaceBase != null)
                    SelectedPartSet.FaceBase = SelectedSecondaryPartSet.FaceBase;
                if (SelectedSecondaryPartSet.FaceEar != null)
                    SelectedPartSet.FaceEar = SelectedSecondaryPartSet.FaceEar;
                if (SelectedSecondaryPartSet.FaceEye != null)
                    SelectedPartSet.FaceEye = SelectedSecondaryPartSet.FaceEye;
                if (SelectedSecondaryPartSet.FaceForehead != null)
                    SelectedPartSet.FaceForehead = SelectedSecondaryPartSet.FaceForehead;
                if (SelectedSecondaryPartSet.FaceNose != null)
                    SelectedPartSet.FaceNose = SelectedSecondaryPartSet.FaceNose;
                if (SelectedSecondaryPartSet.Boots != null)
                    SelectedPartSet.Boots = SelectedSecondaryPartSet.Boots;
                if (SelectedSecondaryPartSet.Bust != null)
                    SelectedPartSet.Bust = SelectedSecondaryPartSet.Bust;
                if (SelectedSecondaryPartSet.Rist != null)
                    SelectedPartSet.Rist = SelectedSecondaryPartSet.Rist;
                if (SelectedSecondaryPartSet.Pants != null)
                    SelectedPartSet.Pants = SelectedSecondaryPartSet.Pants;
                if (SelectedSecondaryPartSet.Hair != null)
                    SelectedPartSet.Hair = SelectedSecondaryPartSet.Hair;

                Close();
            }
            else if (listBox.SelectedItems.Count == 1)
            {
                SelectedPartSet = listBox.SelectedItems[0] as PartSet;
                Close();
            }
            else if (listBox.SelectedItems.Count > 2)
            {
                MessageBox.Show("Only one or two PartSets may be selected.\n\nSelect the base form PartSet first, and the transformed one last.", "PartSet Selector", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
