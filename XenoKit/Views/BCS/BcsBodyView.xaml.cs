using System;
using System.Windows;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using XenoKit.Editor;
using XenoKit.Engine;
using Xv2CoreLib;
using Xv2CoreLib.BCS;
using Xv2CoreLib.Resource.UndoRedo;
using MahApps.Metro.Controls.Dialogs;
using GalaSoft.MvvmLight.CommandWpf;
using XenoKit.ViewModel.BCS;

namespace XenoKit.Views
{
    /// <summary>
    /// Interaction logic for BcsBodyView.xaml
    /// </summary>
    public partial class BcsBodyView : UserControl, INotifyPropertyChanged
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

        public const string UNDO_BODY_ARG = "UNDO_BODY_ARG";

        public static BoneScale CurrentBoneScale = null;

        public BCS_File BcsFile => Files.Instance.SelectedItem?.character?.CharacterData?.BcsFile?.File;
        public Xv2Character Character => Files.Instance.SelectedItem?.character?.CharacterData;

        public int SelectedBodyID
        {
            get => SelectedBody != null ? SelectedBody.ID : -1;
            set
            {
                if (SelectedBody?.ID != value)
                    EditBodyID(value);
            }
        }

        public Body SelectedBody { get; set; }
        private BoneScale _selectedBoneScale = null;
        public BoneScale SelectedBoneScale
        {
            get => _selectedBoneScale;
            set
            {
                CurrentBoneScale = value;
                _selectedBoneScale = value;
                SelectedBoneScaleViewModel = _selectedBoneScale != null ? new BcsBodyViewModel(_selectedBoneScale, SelectedBody) : null;
                NotifyPropertyChanged(nameof(SelectedBoneScale));
                NotifyPropertyChanged(nameof(SelectedBoneScaleViewModel));
                NotifyPropertyChanged(nameof(EditorVisibility));

                if(SelectedBody != null && _selectedBoneScale != null && SceneManager.MainGameInstance != null)
                {
                    SceneManager.MainGameInstance.GetBoneScaleGizmo().SetContext(SelectedBoneScale, SelectedBody, SceneManager.Actors[0], SelectedBoneScale.BoneName);
                }
                else if(SceneManager.MainGameInstance != null)
                {
                    SceneManager.MainGameInstance.BoneScaleGizmo.RemoveContext();
                }

            }
        }
        public BcsBodyViewModel SelectedBoneScaleViewModel { get; set; }

        public Visibility EditorVisibility => SelectedBoneScale != null ? Visibility.Visible : Visibility.Collapsed;

        public BcsBodyView()
        {
            DataContext = this;
            InitializeComponent();
            Files.SelectedItemChanged += Files_SelectedMoveChanged;
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            Engine.Animation.VisualSkeleton.SelectedBoneChanged += VisualSkeleton_SelectedBoneChanged;
        }

        private void VisualSkeleton_SelectedBoneChanged(object sender, EventArgs e)
        {
            if(SceneManager.CurrentSceneState == EditorTabs.BCS_Bodies && SelectedBody != null && sender is int boneIdx)
            {
                string boneName = SceneManager.Actors[0].Skeleton.Bones[boneIdx].Name;
                if (string.IsNullOrWhiteSpace(boneName)) return;

                BoneScale boneScale = SelectedBody.BodyScales.FirstOrDefault(x => x.BoneName == boneName);

                if(boneScale == null)
                {
                    boneScale = new BoneScale();
                    boneScale.BoneName = boneName;
                    boneScale.ScaleX = 1f;
                    boneScale.ScaleY = 1f;
                    boneScale.ScaleZ = 1f;
                    SelectedBody.BodyScales.Add(boneScale);

                    UndoManager.Instance.AddUndo(new UndoableListAdd<BoneScale>(SelectedBody.BodyScales, boneScale, $"Add Bone Scale ({boneName})"), UndoGroup.BCS);
                }

                SelectedBoneScale = boneScale;
            }
        }

        private void Instance_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {
            //Selectively update bone scale if Undo Event is related
            if(e.UndoArg == UNDO_BODY_ARG)
            {
                Files.Instance.SelectedItem?.character?.Skeleton?.UpdateBoneScale();
                SelectedBoneScaleViewModel?.UpdateProperties();
            }

            //Update properties on BCS body data
            if (BcsFile?.Bodies != null)
            {
                foreach (var body in BcsFile.Bodies)
                {
                    body.RefreshValues();

                    if (e.UndoArg == UNDO_BODY_ARG)
                    {
                        foreach (var scale in body.BodyScales)
                        {
                            scale.RefreshValues();
                        }
                    } 
                }
            }
        }

        private void Files_SelectedMoveChanged(object sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(BcsFile));
            NotifyPropertyChanged(nameof(Character));
            NotifyPropertyChanged(nameof(SelectedBody));
        }

        private async void EditBodyID(int newId)
        {
            if (BcsFile.Bodies.Any(x => x.SortID == newId && x != SelectedBody))
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "ID Already Used", "The entered ID is already used by another body.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                return;
            }

            UndoManager.Instance.AddUndo(new UndoableProperty<PartColor>(nameof(PartColor.ID), SelectedBody, SelectedBody.ID, newId, "Body ID"));
            SelectedBody.ID = newId;
            NotifyPropertyChanged(nameof(SelectedBodyID));
            SelectedBody.RefreshValues();
        }

        private void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled)
            {
                return;
            }
            Control control = sender as Control;
            if (control == null)
            {
                return;
            }
            e.Handled = true;
            var wheelArgs = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = MouseWheelEvent,
                Source = control
            };
            var parent = VisualTreeHelper.GetParent(control) as UIElement;
            //var parent = control.Parent as UIElement;
            parent?.RaiseEvent(wheelArgs);
        }

        #region Commands
        public RelayCommand SetBodyScaleCommand => new RelayCommand(SetBodyScale, CanSetBodyScale);
        private void SetBodyScale()
        {
            Files.Instance.SelectedItem.character.Skeleton.SetBoneScale(SelectedBody);

            if (SceneManager.Actors[0].AnimationPlayer.PrimaryAnimation == null)
                Log.Add("BCS Body scaling will only apply with an active animation. T-Pose will have default scale.");
        }

        public RelayCommand UnsetBodyScaleCommand => new RelayCommand(UnsetBodyScale);
        private void UnsetBodyScale()
        {
            Files.Instance.SelectedItem.character.Skeleton.RemoveBoneScale();
        }

        public RelayCommand AddBodyCommand => new RelayCommand(AddBody);
        private void AddBody()
        {
            Body body = new Body();
            body.ID = BcsFile.NewBodyID();
            BcsFile.Bodies.Add(body);

            bodyDataGrid.SelectedItem = body;
            bodyDataGrid.ScrollIntoView(body);
            UndoManager.Instance.AddUndo(new UndoableListAdd<Body>(BcsFile.Bodies, body, "Add BCS Body"), UndoGroup.BCS);
        }

        public RelayCommand RemoveBodyCommand => new RelayCommand(RemoveBody, IsBodySelected);
        private void RemoveBody()
        {
            List<Body> selectedItems = bodyDataGrid.SelectedItems.Cast<Body>().ToList();

            if(selectedItems?.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach(var item in selectedItems)
                {
                    undos.Add(new UndoableListRemove<Body>(BcsFile.Bodies, item, BcsFile.Bodies.IndexOf(item)));
                    BcsFile.Bodies.Remove(item);
                }

                UndoManager.Instance.AddCompositeUndo(undos, selectedItems.Count > 1 ? "Remove BCS Bodies" : "Remove BCS Body");
            }
        }

        public RelayCommand CopyBodyCommand => new RelayCommand(CopyBody, IsBodySelected);
        private void CopyBody()
        {
            List<Body> selectedItems = bodyDataGrid.SelectedItems.Cast<Body>().ToList();

            if (selectedItems?.Count > 0)
            {
                Clipboard.SetData(ClipboardConstants.BcsBody, selectedItems);
            }
        }
        
        public RelayCommand PasteBodyCommand => new RelayCommand(PasteBody, CanPasteBodies);
        private void PasteBody()
        {
            var bodies = (List<Body>)Clipboard.GetData(ClipboardConstants.BcsBody);

            if(bodies?.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach(var body in bodies)
                {
                    body.ID = BcsFile.NewBodyID();
                    undos.Add(new UndoableListAdd<Body>(BcsFile.Bodies, body));
                    BcsFile.Bodies.Add(body);
                }

                UndoManager.Instance.AddCompositeUndo(undos, bodies.Count > 1 ? "Paste BCS Bodies" : "Paste BCS Body");
            }
        }

        public RelayCommand DuplicateBodyCommand => new RelayCommand(DuplicateBody, IsBodySelected);
        private void DuplicateBody()
        {
            List<Body> selectedItems = bodyDataGrid.SelectedItems.Cast<Body>().ToList();

            if (selectedItems?.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach(var item in selectedItems)
                {
                    Body newBody = item.Copy();
                    newBody.ID = BcsFile.NewBodyID();
                    BcsFile.Bodies.Add(newBody);
                    undos.Add(new UndoableListAdd<Body>(BcsFile.Bodies, newBody));
                }

                UndoManager.Instance.AddCompositeUndo(undos, selectedItems.Count > 1 ? "Duplicate BCS Bodies" : "Duplicate BCS Body");
            }
        }

        public RelayCommand AddBoneCommand => new RelayCommand(AddBone, IsBodySelected);
        private void AddBone()
        {
            BoneScale bone = new BoneScale();
            bone.ScaleX = 1f;
            bone.ScaleY = 1f;
            bone.ScaleZ = 1f;

            SelectedBody.BodyScales.Add(bone);

            UndoManager.Instance.AddUndo(new UndoableListAdd<BoneScale>(SelectedBody.BodyScales, bone, "Add BoneScale"), UndoGroup.BCS);
        }

        public RelayCommand RemoveBoneCommand => new RelayCommand(RemoveBone, IsBoneSelected);
        private void RemoveBone()
        {
            UndoManager.Instance.AddUndo(new UndoableListRemove<BoneScale>(SelectedBody.BodyScales, SelectedBoneScale, SelectedBody.BodyScales.IndexOf(SelectedBoneScale), "Remove BoneScale"));
            SelectedBody.BodyScales.Remove(SelectedBoneScale);
        }

        public RelayCommand CopyBoneCommand => new RelayCommand(CopyBone, IsBoneSelected);
        private void CopyBone()
        {
            if (SelectedBoneScale != null)
            {
                Clipboard.SetData(ClipboardConstants.BcsBodyBone, SelectedBoneScale);
            }
        }

        public RelayCommand PasteBoneCommand => new RelayCommand(PasteBone, CanPasteBones);
        private void PasteBone()
        {
            var bone = (BoneScale)Clipboard.GetData(ClipboardConstants.BcsBodyBone);

            SelectedBody.BodyScales.Add(bone);
                
            UndoManager.Instance.AddUndo(new UndoableListAdd<BoneScale>(SelectedBody.BodyScales, bone, "Paste BoneScale"));
        }

        public RelayCommand DuplicateBoneCommand => new RelayCommand(DuplicateBone, IsBoneSelected);
        private void DuplicateBone()
        {
            BoneScale newBone = SelectedBoneScale.Copy();
            SelectedBody.BodyScales.Add(newBone);
            UndoManager.Instance.AddUndo(new UndoableListAdd<BoneScale>(SelectedBody.BodyScales, newBone, "Duplicate BoneScale"));
        }


        private bool IsBodySelected()
        {
            return SelectedBody != null;
        }

        private bool IsBoneSelected()
        {
            return SelectedBoneScale != null;
        }

        private bool CanSetBodyScale()
        {
            if (!IsBodySelected()) return false;

            //Body scales can only be equipped on their owner actors
            return Files.Instance.SelectedItem?.character == SceneManager.Actors[0] && BcsFile != null;
        }

        private bool CanPasteBodies()
        {
            return Clipboard.ContainsData(ClipboardConstants.BcsBody) && BcsFile != null;
        }

        private bool CanPasteBones()
        {
            return Clipboard.ContainsData(ClipboardConstants.BcsBodyBone) && BcsFile != null;
        }
        #endregion
    }
}
