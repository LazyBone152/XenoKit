using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Helper;
using Xv2CoreLib;
using Xv2CoreLib.EAN;
using Xv2CoreLib.Resource.UndoRedo;
using XenoKit.Windows.EAN;
using System.Collections.ObjectModel;
using System.Windows;
using Xv2CoreLib.Resource;

namespace XenoKit.Controls
{
    /// <summary>
    /// Interaction logic for AnimationTabView.xaml
    /// </summary>
    public partial class AnimationTabView : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion


        public Files files { get { return Files.Instance; } }

        //Values
        private EAN_Node _selectedBone = null;

        //Editing
        private List<string> BoneFilter = new List<string>();
        public int SelectedFrame
        {
            get 
            {

                if (SceneManager.Actors[0]?.animationPlayer?.PrimaryAnimation != null)
                {
                    return SceneManager.Actors[0].animationPlayer.PrimaryAnimation.CurrentFrame_Int;
                }
                return 0;
            }
            set
            {
                if(SceneManager.Actors[0]?.animationPlayer?.PrimaryAnimation != null)
                {
                    SceneManager.Actors[0].animationPlayer.PrimaryAnimation.CurrentFrame_Int = value;
                    NotifyPropertyChanged(nameof(SelectedFrame));
                }
            }
        }

        //View
        public ObservableCollection<int> CurrentKeyframes
        {
            get { return (_selectedBone != null) ? new ObservableCollection<int>(SelectedBone.GetAllKeyframesInt()) : null; }
        }
        public EAN_Node SelectedBone
        {
            get { return _selectedBone; }
            set
            {
                if (_selectedBone != value)
                {
                    _selectedBone = value;
                    NotifyPropertyChanged(nameof(SelectedBone));
                    NotifyPropertyChanged(nameof(CurrentKeyframes));

                    //Propagate changes to the Editor side
                    SelectedBoneChanged(_selectedBone?.BoneName);
                }
            }
        }

        public AnimationTabView()
        {
            InitializeComponent();
            DataContext = this;
            Game.GameUpdate += SceneManager_UpdateEvent;
            Engine.Animation.VisualSkeleton.SelectedBoneChanged += VisualSkeleton_SelectedBoneChanged;
        }

        private void VisualSkeleton_SelectedBoneChanged(object sender, EventArgs e)
        {
            if (sender is int value && SceneManager.Actors[0] != null)
            {
                string boneName = SceneManager.Actors[0].Skeleton.Bones[value].Name;
                SelectedBoneChanged(boneName);
            }
        }

        private void SelectedBoneChanged(string boneName)
        {
            if (files.SelectedItem?.SelectedAnimation == null) return;
            if(string.IsNullOrWhiteSpace(boneName) && SelectedBone != null)
            {
                SelectedBone = null;
                return;
            }

            if(SelectedBone?.BoneName != boneName)
            {
                EAN_Node node = files.SelectedItem.SelectedAnimation.GetNode(boneName);
                SelectedBone = node;

                if(SelectedBone != null)
                    boneDataGrid.ScrollIntoView(SelectedBone);
            }

            //Update AnimatorGizmo
            if (files.SelectedItem?.SelectedEanFile?.File.Skeleton?.Exists(boneName) == true && Files.Instance.SelectedItem?.SelectedAnimation != null)
            {
                SceneManager.AnimatorGizmo.Enable(SceneManager.Actors[0], boneName);
            }
            else
            {
                SceneManager.AnimatorGizmo.Disbable();
            }
        }

        private void SceneManager_UpdateEvent(object sender, EventArgs e)
        {
            //Update certain properties every frame, as they may change externally
            if (SceneManager.CurrentSceneState == EditorTabs.Animation)
            {
                NotifyPropertyChanged(nameof(SelectedFrame));
            }
        }

        #region FileSelection
        public RelayCommand AddEanFileCommand => new RelayCommand(AddEanFile, CanAddEanFile);
        private async void AddEanFile()
        {
            var result = await DialogCoordinator.Instance.ShowInputAsync(this, "New EAN", "Enter the character ID (3-letter code) that the EAN is for.", DialogSettings.Default);

            if (string.IsNullOrWhiteSpace(result))
            {
                return;
            }

            if(result.Length != 3)
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "New EAN", "The entered ID contained too many or too few letters.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                return;
            }

            if(Xv2File<EAN_File>.IsCharaCodeUsed(files.SelectedMove.Files.EanFile, result.ToUpper()))
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "New EAN", "The entered ID is already in use.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                return;
            }

            var eanFile = new Xv2File<EAN_File>(EAN_File.DefaultFile(Xenoverse2.Instance.CmnEan.Skeleton), null, false, result.ToUpper(), false, Xenoverse2.MoveFileTypes.EAN, 0, false);
            files.SelectedMove.Files.EanFile.Add(eanFile);
            files.SelectedItem.SelectedEanFile = eanFile;

            UndoManager.Instance.AddUndo(new UndoableListAdd<Xv2File<EAN_File>>(files.SelectedMove.Files.EanFile, eanFile, $"New EAN ({result})"));
        }

        public RelayCommand DeleteEanFileCommand => new RelayCommand(DeleteEanFile, CanDeleteEanFile);
        private void DeleteEanFile()
        {
            UndoManager.Instance.AddUndo(new UndoableListRemove<Xv2File<EAN_File>>(files.SelectedMove.Files.EanFile, files.SelectedItem.SelectedEanFile, $"Remove EAN ({files.SelectedItem.SelectedEanFile.CharaCode})"));
            files.SelectedMove.Files.EanFile.Remove(files.SelectedItem.SelectedEanFile);
        }

        public RelayCommand RenameEanFileCommand => new RelayCommand(RenameEanFile, CanRenameEanFile);
        private async void RenameEanFile()
        {
            var dialogSettings = DialogSettings.Default;
            string originalCode = files.SelectedItem.SelectedEanFile.CharaCode;
            dialogSettings.DefaultText = files.SelectedItem.SelectedEanFile.CharaCode;

            var result = await DialogCoordinator.Instance.ShowInputAsync(this, "Change ID", "Enter the character ID (3-letter code).", dialogSettings);

            if (string.IsNullOrWhiteSpace(result))
            {
                return;
            }

            result = result.ToUpper();

            if (result.Length != 3)
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "Change ID", "The entered ID contained too many or too few letters.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                return;
            }

            if (Xv2File<EAN_File>.IsCharaCodeUsed(files.SelectedMove.Files.EanFile, result, files.SelectedMove.Files.EanFile.IndexOf(files.SelectedItem.SelectedEanFile)))
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "Change ID", "The entered ID is already in use.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                return;
            }

            
            files.SelectedItem.SelectedEanFile.CharaCode = result;
            UndoManager.Instance.AddUndo(new UndoableProperty<Xv2File<EAN_File>>(nameof(Xv2File<EAN_File>.CharaCode), files.SelectedItem.SelectedEanFile, originalCode, files.SelectedItem.SelectedEanFile.CharaCode, $"EAN ID ({originalCode} -> {result})"));

        }


        private bool CanDeleteEanFile()
        {
            if(files.SelectedItem?.SelectedEanFile != null)
            {
                return files.SelectedItem.SelectedEanFile.IsNotDefault;
            }

            return false;
        }

        private bool CanAddEanFile()
        {
            if(files.SelectedItem != null)
            {
                if (files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Character || files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Moveset || files.SelectedItem.Type == OutlinerItem.OutlinerItemType.CMN)
                    return false;

                if (files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Skill)
                    return true;
            }

            return false;
        }
       
        private bool CanRenameEanFile()
        {
            if (files.SelectedItem != null)
            {
                if (files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Character || files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Moveset || files.SelectedItem.Type == OutlinerItem.OutlinerItemType.CMN)
                    return false;

                if (files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Skill && (files.SelectedItem.SelectedEanFile != null) ? !files.SelectedItem.SelectedEanFile.IsDefault : false)
                    return true;
            }

            return false;
        }
        #endregion

        #region AnimationCommands
        public RelayCommand PlayAnimationCommand => new RelayCommand(PlaySelectedAnimation, IsAnimationSelected);
        private void PlaySelectedAnimation()
        {
            if(animListBox.SelectedItem is EAN_Animation anim)
            {
                SceneManager.PlayAnimation(files.SelectedItem?.SelectedEanFile?.File, anim.ID_UShort, 0, true);
            }
        }

        public RelayCommand ChangeAnimIdCommand => new RelayCommand(ChangeAnimIdAsync, IsAnimationSelected);
        private async void ChangeAnimIdAsync()
        {
            EAN_Animation selectedEntry = animListBox.SelectedItem as EAN_Animation;
            start:
            var result = await DialogCoordinator.Instance.ShowInputAsync(this, "Change ID", "Enter the new ID.", new MetroDialogSettings() { DefaultText = selectedEntry.Index, AnimateShow = false, AnimateHide = false });

            if (string.IsNullOrWhiteSpace(result))
            {
                return;
            }

            int num;
            if(!int.TryParse(result, out num))
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "Invalid Characters", "The entered ID contains invalid characters.", MessageDialogStyle.Affirmative, new MetroDialogSettings() { AnimateShow = false, AnimateHide = false });
                goto start;
            }

            if (files.SelectedItem?.SelectedEanFile?.File.Animations.FirstOrDefault(a => a.Index == result && a != selectedEntry) != null)
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "ID Already Used", "The entered ID is already used by another animation. Please enter a unique one.", MessageDialogStyle.Affirmative, new MetroDialogSettings() { AnimateShow = false, AnimateHide = false });
                goto start;
            }
            else
            {
                selectedEntry.Index = result;
            }

            files.SelectedItem?.SelectedEanFile?.File.SortEntries();
        }

        public RelayCommand ChangeAnimNameCommand => new RelayCommand(ChangeAnimName, IsAnimationSelected);
        private async void ChangeAnimName()
        {
            EAN_Animation selectedEntry = animListBox.SelectedItem as EAN_Animation;
            start:
            var result = await DialogCoordinator.Instance.ShowInputAsync(this, "Change Name", "Enter the new name for the animation.", new MetroDialogSettings() { DefaultText = selectedEntry.Name, AnimateShow = false, AnimateHide = false });

            if (string.IsNullOrWhiteSpace(result))
            {
                return;
            }

            if (files.SelectedItem?.SelectedEanFile?.File.Animations.FirstOrDefault(a => a.Name == result && a != selectedEntry) != null)
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "Name Already Used", "The entered name is already used by another animation. Please enter a unique one.", MessageDialogStyle.Affirmative, new MetroDialogSettings() { AnimateShow = false, AnimateHide = false });
                goto start;
            }
            else
            {
                selectedEntry.Name = result;
            }
        }

        public RelayCommand RescaleAnimationCommand => new RelayCommand(RescaleAnimation, IsAnimationSelected);
        private void RescaleAnimation()
        {
            if (files.SelectedItem?.SelectedAnimation != null)
            {
                EanModiferForm form = new EanModiferForm("Rescale Animation");
                form.NewDuration = files.SelectedItem.SelectedAnimation.FrameCount;
                form.NewDurationEnabled = true;
                form.ShowDialog();

                if (form.Success)
                {
                    List<IUndoRedo> undos = files.SelectedItem.SelectedAnimation.RescaleAnimation(form.NewDuration);

                    undos.Add(new UndoActionDelegate(typeof(SceneManager), nameof(SceneManager.InvokeAnimationDurationChangedEvent), true));
                    undos.Add(new UndoActionDelegate(this, nameof(NotifyPropertyChanged), true, "", new object[1] { nameof(CurrentKeyframes) }));
                    UndoManager.Instance.AddCompositeUndo(undos, "Rescale Animation");

                    SceneManager.InvokeAnimationDurationChangedEvent();
                    NotifyPropertyChanged(nameof(CurrentKeyframes));
                }
            }
        }

        private bool IsAnimationSelected()
        {
            return (animListBox.SelectedItem as EAN_Animation) != null;
        }

        #endregion

        #region BoneCommands
        public RelayCommand CopyBoneCommand => new RelayCommand(CopyBone, IsBoneSelected);
        private void CopyBone()
        {
            List<EAN_Node> bones = boneDataGrid.SelectedItems.Cast<EAN_Node>().ToList();

            if (bones != null)
            {
                Clipboard.SetData(ClipboardConstants.EanNode, bones);
            }
        }

        public RelayCommand PasteBoneCommand => new RelayCommand(PasteBone, CanPasteNode);
        private async void PasteBone()
        {
            List<EAN_Node> bones = (List<EAN_Node>)Clipboard.GetData(ClipboardConstants.EanNode);

            //Validate bones
            var result = await DialogCoordinator.Instance.ShowMessageAsync(this, "Rescale?", "Do you want to rescale the pasted keyframes to match the animation duration?", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);

            foreach (var bone in bones)
            {
                if(result == MessageDialogResult.Affirmative)
                    bone.RescaleNode(files.SelectedItem.SelectedAnimation.FrameCount);
            }

            //Paste
            List<IUndoRedo> undos = files.SelectedItem.SelectedAnimation.PasteNodes(bones);
            UndoManager.Instance.AddCompositeUndo(undos, "Paste Bones");
        }

        public RelayCommand DeleteBoneCommand => new RelayCommand(DeleteBone, IsBoneSelected);
        private void DeleteBone()
        {
            List<EAN_Node> bones = boneDataGrid.SelectedItems.Cast<EAN_Node>().ToList();

            if (bones != null)
            {
                List<IUndoRedo> undos = files.SelectedItem.SelectedAnimation.RemoveNodes(bones);
                UndoManager.Instance.AddCompositeUndo(undos, "Delete Bones");
            }
        }


        private bool IsBoneSelected()
        {
            return SelectedBone != null;
        }
        
        private bool CanPasteNode()
        {
            return Clipboard.ContainsData(ClipboardConstants.EanNode) && files.SelectedItem?.SelectedAnimation != null;
        }
        #endregion

        #region KeyframeCommands
        public RelayCommand CopyKeyframeCommand => new RelayCommand(CopyKeyframe, IsKeyframeSelected);
        private void CopyKeyframe()
        {
            List<int> keyframes = keyframeDataGrid.SelectedItems.Cast<int>().ToList();

            if (keyframes != null)
            {
                List<SerializedKeyframe> serializedKeyframes = new List<SerializedKeyframe>();

                foreach(var keyframe in keyframes)
                {
                    serializedKeyframes.Add(SelectedBone.GetSerializedKeyframe(keyframe));
                }

                Clipboard.SetData(ClipboardConstants.EanAnimationKeyframe, serializedKeyframes);
            }
        }

        public RelayCommand PasteKeyframeCommand => new RelayCommand(PasteKeyframe, CanPasteKeyframes);
        private void PasteKeyframe()
        {
            List<SerializedKeyframe> keyframes = (List<SerializedKeyframe>)Clipboard.GetData(ClipboardConstants.EanAnimationKeyframe);

            if (keyframes != null)
            {
                List<IUndoRedo> undos = SelectedBone.PasteKeyframes(keyframes);
                UndoManager.Instance.AddCompositeUndo(undos, "Paste Keyframes");
            }
        }
        
        public RelayCommand DeleteKeyframeCommand => new RelayCommand(DeleteKeyframe, IsKeyframeSelected);
        private async void DeleteKeyframe()
        {
            List<int> keyframes = keyframeDataGrid.SelectedItems.Cast<int>().ToList();

            if (keyframes != null)
            {
                var undos = SelectedBone.DeleteKeyframes(keyframes);

                undos.Add(new UndoActionDelegate(this, nameof(NotifyPropertyChanged), true, "", new object[1] { nameof(CurrentKeyframes) }));
                UndoManager.Instance.AddCompositeUndo(undos, "Delete Keyframes");

                NotifyPropertyChanged(nameof(CurrentKeyframes));
            }
        }


        private bool CanPasteKeyframes()
        {
            return Clipboard.ContainsData(ClipboardConstants.EanAnimationKeyframe) && IsBoneSelected();
        }

        private bool IsKeyframeSelected()
        {
            return (keyframeDataGrid.SelectedItem is int value && value >= 0 && IsBoneSelected());
        }
        #endregion


        #region Modifers
        public RelayCommand RemoveKeyframesCommand => new RelayCommand(RemoveKeyframes, IsAnimationSelected);
        private void RemoveKeyframes()
        {
            EAN_Animation selectedEntry = animListBox.SelectedItem as EAN_Animation;

            DisplayBoneFilter(selectedEntry.Nodes);

            EanModiferForm form = new EanModiferForm("Remove Keyframes");
            form.StartFrameEnabled = true;
            form.ShowDialog();

            if (form.Success)
            {
                List<IUndoRedo> undos = selectedEntry.RemoveKeyframes(BoneFilter, form.StartFrame, form.EndFrame);
                UndoManager.Instance.AddCompositeUndo(undos, "Remove Keyframes");
            }
        }

        #endregion

        private void DisplayBoneFilter(IList<EAN_Node> eanNodes)
        {
            Windows.EAN.BoneFilter boneFilterForm = new Windows.EAN.BoneFilter(eanNodes, BoneFilter);
            boneFilterForm.ShowDialog();
            BoneFilter = boneFilterForm.GetBoneFilter();
        }

        private void AnimListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(IsAnimationSelected())
            {
                PlaySelectedAnimation();
            }
        }

        private void AnimListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsAnimationSelected())
            {
                SceneManager.PlayAnimation(files.SelectedItem?.SelectedEanFile?.File, files.SelectedItem.SelectedAnimation.ID_UShort, 0, SceneManager.AutoPlay);
            }

            SceneManager.RefreshVisualSkeletonVisibility();
        }

        private void KeyframeDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(keyframeDataGrid.SelectedItem is int value && value >= 0)
            {
                SelectedFrame = value;
            }
        }
    }
}
