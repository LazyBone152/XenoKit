using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Xv2CoreLib.EAN;

namespace XenoKit.Windows.EAN
{
    /// <summary>
    /// Interaction logic for BoneFilter.xaml
    /// </summary>
    public partial class BoneFilter : MetroWindow
    {
        public List<BoneFilterEntry> Bones { get; set; }
        private List<string> originalBoneFilter;
        public bool ChangesAccepted { get; set; }

        public BoneFilter(IList<EAN_Node> nodes, List<string> boneFilter)
        {
            DataContext = this;
            originalBoneFilter = boneFilter;
            Bones = BoneFilterEntry.GetSelectedBones(boneFilter, nodes);
            InitializeComponent();
            Owner = App.Current.MainWindow;
        }

        public List<string> GetBoneFilter()
        {
            return (ChangesAccepted) ? BoneFilterEntry.GetBoneFilter(Bones) : originalBoneFilter;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(!Bones.Any(x => x.IsChecked))
            {
                MessageBox.Show("No bones were selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ChangesAccepted = true;
            Close();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var bone in Bones)
                bone.IsChecked = true;
        }

        private void UnselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var bone in Bones)
                bone.IsChecked = false;
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            foreach(var item in dataGrid.SelectedItems)
            {
                if (item is BoneFilterEntry bone)
                    bone.IsChecked = true;
            }
        }

        private void Unselect_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in dataGrid.SelectedItems)
            {
                if (item is BoneFilterEntry bone)
                    bone.IsChecked = false;
            }
        }
    }

    public class BoneFilterEntry : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private bool _isChecked = false;

        public string Bone { get; set; }
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if(_isChecked != value)
                {
                    _isChecked = value;
                    NotifyPropertyChanged(nameof(IsChecked));
                }
            }
        }

        public BoneFilterEntry(string bone, bool isSelected)
        {
            Bone = bone;
            IsChecked = isSelected;
        }

        public static List<string> GetBoneFilter(List<BoneFilterEntry> bones)
        {
            List<string> boneFilter = new List<string>();

            foreach (var bone in bones.Where(x => x.IsChecked))
                boneFilter.Add(bone.Bone);

            return boneFilter;
        }

        public static List<BoneFilterEntry> GetSelectedBones(List<string> boneFilter, IList<EAN_Node> nodes)
        {
            List<BoneFilterEntry> bones = new List<BoneFilterEntry>();

            foreach(var node in nodes)
            {
                bones.Add(new BoneFilterEntry(node.BoneName, (boneFilter.Contains(node.BoneName) || boneFilter.Count == 0)));
            }


            return bones;
        }
    }
}
