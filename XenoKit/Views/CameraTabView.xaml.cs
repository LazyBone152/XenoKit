using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using XenoKit.Editor;
using Xv2CoreLib.Resource.UndoRedo;
using XenoKit.Helper;
using Xv2CoreLib;
using Xv2CoreLib.EAN;
using XenoKit.Engine;

namespace XenoKit.Controls
{
    /// <summary>
    /// Interaction logic for AnimationTabView.xaml
    /// </summary>
    public partial class CameraTabView : UserControl, INotifyPropertyChanged
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

        #region PropertiesAndValues
        public Files files { get { return Files.Instance; } }
        
        public Xv2File<EAN_File> SelectedEanFile
        {
            get
            {
                return files.SelectedItem?.SelectedCamFile;
            }

        }
        public EAN_Animation SelectedAnimation
        {
            get
            {
                return files.SelectedItem?.SelectedCamera;
            }
        }

        //View
        public ObservableCollection<int> CurrentKeyframes
        {
            get { return (SelectedAnimation != null) ? new ObservableCollection<int>(SelectedAnimation.GetNode(EAN_Node.CAM_NODE)?.GetAllKeyframesInt()) : null; }
        }
        

        //Insert Keyframe
        private int _currentFrame = 0;
        public int CurrentFrame
        {
            get
            {
                if (SyncCurrentFrameWithView && SceneManager.CameraInstance != null)
                {
                    _currentFrame = (int)SceneManager.CameraInstance.CurrentFrame;
                }

                return _currentFrame;
            }
            set
            {
                if(_currentFrame != value)
                {
                    _currentFrame = value;

                    if(SyncCurrentFrameWithView)
                        SceneManager.CameraChangeCurrentFrame(value);

                    NotifyPropertyChanged(nameof(CurrentFrame));
                }
            }
        }
        
        private bool _syncWithView = true;
        public bool SyncCurrentFrameWithView
        {
            get { return _syncWithView; }
            set
            {
                if(_syncWithView != value)
                {
                    _syncWithView = value;
                    NotifyPropertyChanged(nameof(SyncCurrentFrameWithView));
                    NotifyPropertyChanged(nameof(CurrentFrame));
                }
            }
        }

        //Create Noise
        public int NoiseDuration { get; set; }
        public float NoiseThreshold { get; set; }

        #endregion

        public CameraTabView()
        {
            InitializeComponent();
            DataContext = this;
            animListBox.SelectionChanged += AnimListBox_SelectionChanged;
            keyframeListBox.SelectionChanged += KeyframeListBox_SelectionChanged;
            SceneManager.CameraCurrentFrameChanged += SceneManager_CameraCurrentFrameChanged;
        }

        private void SceneManager_CameraCurrentFrameChanged(object sender, EventArgs e)
        {
            if (SceneManager.CurrentSceneState == EditorTabs.Camera && SyncCurrentFrameWithView)
            {
                _currentFrame = (int)SceneManager.gameInstance.camera.CurrentFrame;
                NotifyPropertyChanged(nameof(CurrentFrame));
            }
        }

        private void KeyframeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(keyframeListBox.SelectedItem is int value)
            {
                CurrentFrame = value;
            }

            SceneManager.UpdateCameraAnimation();
        }

        private void AnimListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SceneManager.PlayCameraAnimation(SelectedAnimation);
            UpdateKeyframeList();
        }

        #region FileSelection
        public RelayCommand AddCamFileCommand => new RelayCommand(AddCamFile, CanAddCamFile);
        private async void AddCamFile()
        {
            var result = await DialogCoordinator.Instance.ShowInputAsync(this, "New CAM.EAN", "Enter the character ID (3-letter code) that the EAN is for.", DialogSettings.Default);

            if (string.IsNullOrWhiteSpace(result))
            {
                return;
            }

            if (result.Length != 3)
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "New CAM.EAN", "The entered ID contained too many or too few letters.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                return;
            }

            if (Xv2File<EAN_File>.IsCharaCodeUsed(files.SelectedMove.Files.CamEanFile, result.ToUpper()))
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "New CAM.EAN", "The entered ID is already in use.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                return;
            }

            var eanFile = new Xv2File<EAN_File>(EAN_File.DefaultCamFile(), null, false, result.ToUpper(), false, Xenoverse2.MoveFileTypes.CAM_EAN, 0, false);
            files.SelectedMove.Files.CamEanFile.Add(eanFile);
            files.SelectedItem.SelectedCamFile = eanFile;

            UndoManager.Instance.AddUndo(new UndoableListAdd<Xv2File<EAN_File>>(files.SelectedMove.Files.CamEanFile, eanFile, $"New CAM.EAN ({result})"));
        }

        public RelayCommand DeleteCamFileCommand => new RelayCommand(DeleteCamFile, CanDeleteCamFile);
        private void DeleteCamFile()
        {
            UndoManager.Instance.AddUndo(new UndoableListRemove<Xv2File<EAN_File>>(files.SelectedMove.Files.CamEanFile, files.SelectedItem.SelectedCamFile, $"Remove CAM.EAN ({files.SelectedItem.SelectedCamFile.CharaCode})"));
            files.SelectedMove.Files.CamEanFile.Remove(files.SelectedItem.SelectedCamFile);
        }

        public RelayCommand RenameCamFileCommand => new RelayCommand(RenameCamFile, CanRenameCamFile);
        private async void RenameCamFile()
        {
            var dialogSettings = DialogSettings.Default;
            string originalCode = files.SelectedItem.SelectedCamFile.CharaCode;
            dialogSettings.DefaultText = files.SelectedItem.SelectedCamFile.CharaCode;

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

            if (Xv2File<EAN_File>.IsCharaCodeUsed(files.SelectedMove.Files.CamEanFile, result, files.SelectedMove.Files.CamEanFile.IndexOf(files.SelectedItem.SelectedCamFile)))
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "Change ID", "The entered ID is already in use.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                return;
            }


            files.SelectedItem.SelectedCamFile.CharaCode = result;
            UndoManager.Instance.AddUndo(new UndoableProperty<Xv2File<EAN_File>>(nameof(Xv2File<EAN_File>.CharaCode), files.SelectedItem.SelectedCamFile, originalCode, files.SelectedItem.SelectedCamFile.CharaCode, $"EAN ID ({originalCode} -> {result})"));

        }


        private bool CanDeleteCamFile()
        {
            if (files.SelectedItem?.SelectedCamFile != null)
            {
                return files.SelectedItem.SelectedCamFile.IsNotDefault;
            }

            return false;
        }

        private bool CanAddCamFile()
        {
            if (files.SelectedItem != null)
            {
                if (files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Character || files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Moveset || files.SelectedItem.Type == OutlinerItem.OutlinerItemType.CMN)
                    return false;

                if (files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Skill)
                    return true;
            }

            return false;
        }

        private bool CanRenameCamFile()
        {
            if (files.SelectedItem != null)
            {
                if (files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Character || files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Moveset || files.SelectedItem.Type == OutlinerItem.OutlinerItemType.CMN)
                    return false;

                if (files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Skill && (files.SelectedItem.SelectedCamFile != null) ? !files.SelectedItem.SelectedCamFile.IsDefault : false)
                    return true;
            }

            return false;
        }
        #endregion

        #region AnimationCommands
        public RelayCommand PlayAnimationCommand => new RelayCommand(PlaySelectedAnimation, IsAnimationSelected);
        private void PlaySelectedAnimation()
        {
            SceneManager.Play();
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

            if (SelectedEanFile?.File.Animations.FirstOrDefault(a => a.Index == result && a != selectedEntry) != null)
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "ID Already Used", "The entered ID is already used by another animation. Please enter a unique one.", MessageDialogStyle.Affirmative, new MetroDialogSettings() { AnimateShow = false, AnimateHide = false });
                goto start;
            }
            else
            {
                selectedEntry.Index = result;
            }

            SelectedEanFile?.File.SortEntries();
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

            if (SelectedEanFile?.File.Animations.FirstOrDefault(a => a.Name == result && a != selectedEntry) != null)
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "Name Already Used", "The entered name is already used by another animation. Please enter a unique one.", MessageDialogStyle.Affirmative, new MetroDialogSettings() { AnimateShow = false, AnimateHide = false });
                goto start;
            }
            else
            {
                selectedEntry.Name = result;
            }
        }

        public RelayCommand ChangeAnimDurationCommand => new RelayCommand(ChangeAnimDurationAsync, IsAnimationSelected);
        private async void ChangeAnimDurationAsync()
        {
            EAN_Animation selectedEntry = animListBox.SelectedItem as EAN_Animation;
            start:
            var result = await DialogCoordinator.Instance.ShowInputAsync(this, "Change Duration", "Enter the duration (in frames).", new MetroDialogSettings() { DefaultText = selectedEntry.FrameCount.ToString(), AnimateShow = false, AnimateHide = false });

            if (string.IsNullOrWhiteSpace(result))
            {
                return;
            }

            int num;
            if (!int.TryParse(result, out num))
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "Invalid Characters", "The entered duration contains invalid characters.", MessageDialogStyle.Affirmative, new MetroDialogSettings() { AnimateShow = false, AnimateHide = false });
                goto start;
            }

            selectedEntry.FrameCount = num;
        }

        

        private bool IsAnimationSelected()
        {
            return (animListBox.SelectedItem as EAN_Animation) != null;
        }
        
        #endregion

        #region KeyframeCommands
        public RelayCommand AddKeyframeCommand => new RelayCommand(AddKeyframeAsync, CanAddKeyframe);
        private void AddKeyframeAsync()
        {
            float posX = SceneManager.gameInstance.camera.ActualPosition.X;
            float posY = SceneManager.gameInstance.camera.ActualPosition.Y;
            float posZ = SceneManager.gameInstance.camera.ActualPosition.Z;
            float rotX = SceneManager.gameInstance.camera.ActualTargetPosition.X;
            float rotY = SceneManager.gameInstance.camera.ActualTargetPosition.Y;
            float rotZ = SceneManager.gameInstance.camera.ActualTargetPosition.Z;
            float scaleX = MathHelper.ToRadians(SceneManager.gameInstance.camera.Roll);
            float scaleY = MathHelper.ToRadians(SceneManager.gameInstance.camera.FieldOfView);

            List<IUndoRedo> undos = SelectedAnimation.AddKeyframe(EAN_Node.CAM_NODE, CurrentFrame, posX, posY, posZ, 1f, rotX, rotY, rotZ, 1f, scaleX, scaleY, 0f, 0f);

            undos.Add(new UndoActionDelegate(this, nameof(UpdateCameraAnimation), true));
            UpdateCameraAnimation();

            UndoManager.Instance.AddCompositeUndo(undos, "Add Keyframe");

        }

        public RelayCommand RemoveKeyframeCommand => new RelayCommand(RemoveKeyframe, IsAnimationSelected);
        private void RemoveKeyframe()
        {
            List<int> keyframes = keyframeListBox.SelectedItems.Cast<int>().ToList();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(int keyframe in keyframes)
            {
                undos.AddRange(SelectedAnimation.RemoveKeyframe(keyframe));
            }

            undos.Add(new UndoActionDelegate(this, nameof(UpdateKeyframeList), true));
            undos.Add(new UndoActionDelegate(this, nameof(UpdateCameraAnimation), true));

            UpdateKeyframeList();
            UpdateCameraAnimation();

            UndoManager.Instance.AddCompositeUndo(undos, "Remove Keyframe");
        }

        public RelayCommand CopyKeyframeCommand => new RelayCommand(CopyKeyframe, CanCopyKeyframe);
        private void CopyKeyframe()
        {
            SerializedKeyframe keyframe = SelectedAnimation.GetNode(EAN_Node.CAM_NODE).GetSerializedKeyframe(CurrentFrame);
            Clipboard.SetData(ClipboardConstants.CameraKeyframe, keyframe);
        }

        public RelayCommand PasteKeyframeCommand => new RelayCommand(PasteKeyframe, CanPasteKeyframe);
        private void PasteKeyframe()
        {
            SerializedKeyframe keyframe = (SerializedKeyframe)Clipboard.GetData(ClipboardConstants.CameraKeyframe);

            if (keyframe != null)
            {
                List<IUndoRedo> undos = SelectedAnimation.AddKeyframe(EAN_Node.CAM_NODE, CurrentFrame, keyframe.PosX, keyframe.PosY, keyframe.PosZ, keyframe.PosW, keyframe.RotX, keyframe.RotY, keyframe.RotZ, keyframe.RotW, keyframe.ScaleX, keyframe.ScaleY, keyframe.ScaleZ, keyframe.ScaleW);
                UpdateKeyframeList();
                
                undos.Add(new UndoActionDelegate(this, nameof(UpdateKeyframeList), true));

                undos.Add(new UndoActionDelegate(this, nameof(UpdateCameraAnimation), true));
                UpdateCameraAnimation();

                UndoManager.Instance.AddCompositeUndo(undos, "Paste Keyframe");
            }
        }

        public RelayCommand PasteKeyframeValuesCommand => new RelayCommand(PasteKeyframeValues, CanPasteKeyframeValues);
        private void PasteKeyframeValues()
        {
            SerializedKeyframe keyframe = (SerializedKeyframe)Clipboard.GetData(ClipboardConstants.CameraKeyframe);

            if (keyframe != null)
            {
                List<IUndoRedo> undos = SelectedAnimation.AddKeyframe(EAN_Node.CAM_NODE, CurrentFrame, keyframe.PosX, keyframe.PosY, keyframe.PosZ, keyframe.PosW, keyframe.RotX, keyframe.RotY, keyframe.RotZ, keyframe.RotW, keyframe.ScaleX, keyframe.ScaleY, keyframe.ScaleZ, keyframe.ScaleW);
                UpdateKeyframeList();

                undos.Add(new UndoActionDelegate(this, nameof(UpdateKeyframeList), true));
                undos.Add(new UndoActionDelegate(this, nameof(UpdateCameraAnimation), true));
                UpdateCameraAnimation();

                UndoManager.Instance.AddCompositeUndo(undos, "Paste Keyframe Values");

            }
        }

        public RelayCommand CreateNoiseCommand => new RelayCommand(CreateNoise, IsAnimationSelected);
        private async void CreateNoise()
        {
            if (CurrentFrame + NoiseDuration > SelectedAnimation.FrameCount)
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "Invalid Duration", "The specified noise duration exceeds the total animation duration.", MessageDialogStyle.Affirmative);
                return;
            }


            var pos = SelectedAnimation.GetNode("Node").GetComponent(EAN_AnimationComponent.ComponentType.Position);
            var rot = SelectedAnimation.GetNode("Node").GetComponent(EAN_AnimationComponent.ComponentType.Rotation);
            var scale = SelectedAnimation.GetNode("Node").GetComponent(EAN_AnimationComponent.ComponentType.Scale);

            pos.ApplyCameraShake(CurrentFrame, NoiseDuration, NoiseThreshold, false);
            rot.ApplyCameraShake(CurrentFrame, NoiseDuration, NoiseThreshold, false);
            scale.ApplyCameraShake(CurrentFrame, NoiseDuration, NoiseThreshold, true);

            //KeyframeList = KeyframeListEntry.GetKeyframeList(SelectedAnimation.GetNode("Node"));
        }


        private bool CanAddKeyframe()
        {
            if (!IsAnimationSelected()) return false;
            if (CurrentFrame >= 0) return true;
            return false;
        }

        private bool CanCopyKeyframe()
        {
            return (keyframeListBox.SelectedItem is int);
        }

        private bool CanPasteKeyframe()
        {
            return Clipboard.ContainsData(ClipboardConstants.CameraKeyframe);
        }

        private bool CanPasteKeyframeValues()
        {
            if (!Clipboard.ContainsData(ClipboardConstants.CameraKeyframe)) return false;
            if (!(keyframeListBox.SelectedItem is int)) return false;
            return true;
        }
        #endregion

        private void AnimListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(animListBox.SelectedIndex != -1)
            {
                PlaySelectedAnimation();
            }
        }

        //KeyframeListBox
        
    
        public void UpdateKeyframeList()
        {
            NotifyPropertyChanged(nameof(CurrentKeyframes));
        }

        public void UpdateCameraAnimation()
        {
            SceneManager.UpdateCameraAnimation();
        }
    }
}
