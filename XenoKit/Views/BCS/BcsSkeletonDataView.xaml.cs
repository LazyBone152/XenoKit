using System;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Helper;
using XenoKit.ViewModel.BCS;
using Xv2CoreLib;
using Xv2CoreLib.BCS;
using Xv2CoreLib.Resource.UndoRedo;
using MahApps.Metro.Controls.Dialogs;
using GalaSoft.MvvmLight.CommandWpf;

namespace XenoKit.Views
{
    /// <summary>
    /// Interaction logic for BcsSkeletonDataView.xaml
    /// </summary>
    public partial class BcsSkeletonDataView : UserControl, INotifyPropertyChanged
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

        public static readonly DependencyProperty SkeletonDataProperty = DependencyProperty.Register(
            nameof(SkeletonData), typeof(SkeletonData), typeof(BcsSkeletonDataView), new PropertyMetadata(default(SkeletonData)));

        public SkeletonData SkeletonData
        {
            get { return (SkeletonData)GetValue(SkeletonDataProperty); }
            set
            {
                SetValue(SkeletonDataProperty, value);
                NotifyPropertyChanged(nameof(SkeletonData));
            }
        }

        private Bone _selectedBone = null;
        public Bone SelectedBone
        {
            get => _selectedBone;
            set
            {
                _selectedBone = value;

                if(((SkeletonViewModel?.IsBone(_selectedBone) == false) || SkeletonViewModel == null) && _selectedBone != null)
                {
                    SkeletonViewModel = new BcsSkeletonDataViewModel(_selectedBone, SkeletonData);
                    NotifyPropertyChanged(nameof(SkeletonViewModel));
                }

                NotifyPropertyChanged(nameof(SelectedBone));
                NotifyPropertyChanged(nameof(EditorVisibility));
            }
        }
        public BcsSkeletonDataViewModel SkeletonViewModel { get; set; }

        public Visibility EditorVisibility => SelectedBone != null ? Visibility.Visible : Visibility.Collapsed;

        public BcsSkeletonDataView()
        {
            DataContext = this;
            InitializeComponent();
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            Files.SelectedItemChanged += Files_SelectedItemChanged;
        }

        private void Files_SelectedItemChanged(object sender, EventArgs e)
        {

        }

        private void Instance_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {
            //Update view model if undo context is the same
            if(e.UndoContext == SkeletonData && SkeletonData != null)
            {
                SkeletonViewModel?.UpdateProperties();
            }

            //Update all bone names if undo context is a SkeletonData
            if(e.UndoContext is Xv2CoreLib.BCS.SkeletonData)
            {
                foreach (var bone in SkeletonData.Bones)
                {
                    bone.RefreshValues();
                }
            }
        }

        #region Commands
        public RelayCommand AddBoneCommand => new RelayCommand(AddBone, HasSkeletonData);
        private void AddBone()
        {
            Bone bone = new Bone();
            bone.BoneName = "NewBone";
            SkeletonData.Bones.Add(bone);

            UndoManager.Instance.AddUndo(new UndoableListAdd<Bone>(SkeletonData.Bones, bone, "Add SkeletonData Bone"), UndoGroup.BCS);
        }

        public RelayCommand RemoveBoneCommand => new RelayCommand(RemoveBone, IsBoneSelected);
        private void RemoveBone()
        {
            List<Bone> bones = skeletonDataGrid.SelectedItems.Cast<Bone>().ToList();
            
            if(bones.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach(var bone in bones)
                {
                    undos.Add(new UndoableListRemove<Bone>(SkeletonData.Bones, bone, SkeletonData.Bones.IndexOf(bone)));
                    SkeletonData.Bones.Remove(bone);
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Remove SkeletonData Bone", UndoGroup.BCS);
            }
        }

        public RelayCommand CopyBoneCommand => new RelayCommand(CopyBone, IsBoneSelected);
        private void CopyBone()
        {
            List<Bone> bones = skeletonDataGrid.SelectedItems.Cast<Bone>().ToList();

            if(bones.Count > 0)
            {
                Clipboard.SetData(ClipboardConstants.BcsSkeletonDataBone, bones);
            }
        }

        public RelayCommand PasteBoneCommand => new RelayCommand(PasteBone, CanPasteBone);
        private void PasteBone()
        {
            List<Bone> bones = (List<Bone>)Clipboard.GetData(ClipboardConstants.BcsSkeletonDataBone);
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var bone in bones)
            {
                SkeletonData.Bones.Add(bone);
                undos.Add(new UndoableListAdd<Bone>(SkeletonData.Bones, bone));
            }

            UndoManager.Instance.AddCompositeUndo(undos, "Paste SkeletonData Bone");
        }

        public RelayCommand DuplicateBoneCommand => new RelayCommand(DuplicateBone, IsBoneSelected);
        private void DuplicateBone()
        {
            List<Bone> bones = (List<Bone>)skeletonDataGrid.SelectedItems.Cast<Bone>().ToList();

            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(var bone in bones)
            {
                SkeletonData.Bones.Add(bone);
                undos.Add(new UndoableListAdd<Bone>(SkeletonData.Bones, bone));
            }

            UndoManager.Instance.AddCompositeUndo(undos, "Duplicate SkeletonData Bone");
        }


        private bool CanPasteBone()
        {
            return Clipboard.ContainsData(ClipboardConstants.BcsSkeletonDataBone);
        }

        private bool IsBoneSelected()
        {
            return SelectedBone != null;
        }

        private bool HasSkeletonData()
        {
            return SkeletonData != null;
        }
        #endregion
    }
}
