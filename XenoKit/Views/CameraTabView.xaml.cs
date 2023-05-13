using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Xv2CoreLib;
using Xv2CoreLib.EAN;
using Xv2CoreLib.Resource.UndoRedo;
using XenoKit.Engine;
using XenoKit.Editor;
using XenoKit.Windows.EAN;
using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Media;
using Xv2CoreLib.Resource;

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

        //Shortcuts:
        public Files files { get { return Files.Instance; } }
        public EAN_File SelectedEanFile
        {
            get
            {
                return files.SelectedItem?.SelectedCamFile?.File;
            }

        }
        public EAN_Animation SelectedAnimation
        {
            get
            {
                return files.SelectedItem?.SelectedCamera;
            }
        }
        private readonly List<string> BoneFilter = new List<string>(1) { EAN_Node.CAM_NODE };

        //Components:
        private bool _posComponent = true;
        private bool _targetPosComponent = true;
        private bool _cameraComponent = true;
        public bool PositionComponentEnabled
        {
            get => _posComponent;
            set
            {
                _posComponent = value;
                NotifyPropertyChanged(nameof(PositionComponentEnabled));
                NotifyPropertyChanged(nameof(CurrentKeyframes));
            }
        }
        public bool TargetPositionComponentEnabled
        {
            get => _targetPosComponent;
            set
            {
                _targetPosComponent = value;
                NotifyPropertyChanged(nameof(TargetPositionComponentEnabled));
                NotifyPropertyChanged(nameof(CurrentKeyframes));
            }
        }
        public bool CameraComponentEnabled
        {
            get => _cameraComponent;
            set
            {
                _cameraComponent = value;
                NotifyPropertyChanged(nameof(CameraComponentEnabled));
                NotifyPropertyChanged(nameof(CurrentKeyframes));
            }
        }

        //Keyframes:
        public ObservableCollection<int> CurrentKeyframes
        {
            get { return (SelectedAnimation != null) ? new ObservableCollection<int>(SelectedAnimation.GetNode(EAN_Node.CAM_NODE)?.GetAllKeyframesInt(PositionComponentEnabled, TargetPositionComponentEnabled, CameraComponentEnabled)) : null; }
        }
        public int SelectedFrameEdit
        {
            get => CurrentFrame;
            set
            {
                if(CurrentFrame != value && IsKeyframesSelected())
                {
                    List<IUndoRedo> undos = new List<IUndoRedo>();
                    SelectedAnimation.GetNode(EAN_Node.CAM_NODE).RebaseKeyframes(new List<int>() { CurrentFrame }, value - CurrentFrame, false, PositionComponentEnabled, TargetPositionComponentEnabled, CameraComponentEnabled, undos);

                    UndoManager.Instance.AddCompositeUndo(undos, "Edit Keyframe", UndoGroup.Camera);
                    UpdateProperties();
                    SceneManager.InvokeCameraDataChangedEvent();
                }
            }
        }

        //Insert Keyframe:
        private int _currentFrame = 0;
        public int CurrentFrame
        {
            get
            {
                if (SyncCurrentFrameWithView && SceneManager.MainCamera != null)
                {
                    _currentFrame = (int)SceneManager.MainCamera.CurrentFrame;
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

        #region KeyframeValues
        public float PosX
        {
            get => GetKeyframeValue(EAN_AnimationComponent.ComponentType.Position, Axis.X);
            set => SetKeyframeValue(EAN_AnimationComponent.ComponentType.Position, Axis.X, value);
        }
        public float PosY
        {
            get => GetKeyframeValue(EAN_AnimationComponent.ComponentType.Position, Axis.Y);
            set => SetKeyframeValue(EAN_AnimationComponent.ComponentType.Position, Axis.Y, value);
        }
        public float PosZ
        {
            get => GetKeyframeValue(EAN_AnimationComponent.ComponentType.Position, Axis.Z);
            set => SetKeyframeValue(EAN_AnimationComponent.ComponentType.Position, Axis.Z, value);
        }
        public float PosW
        {
            get => GetKeyframeValue(EAN_AnimationComponent.ComponentType.Position, Axis.W);
            set => SetKeyframeValue(EAN_AnimationComponent.ComponentType.Position, Axis.W, value);
        }

        public float TargetPosX
        {
            get => GetKeyframeValue(EAN_AnimationComponent.ComponentType.Rotation, Axis.X);
            set => SetKeyframeValue(EAN_AnimationComponent.ComponentType.Rotation, Axis.X, value);
        }
        public float TargetPosY
        {
            get => GetKeyframeValue(EAN_AnimationComponent.ComponentType.Rotation, Axis.Y);
            set => SetKeyframeValue(EAN_AnimationComponent.ComponentType.Rotation, Axis.Y, value);
        }
        public float TargetPosZ
        {
            get => GetKeyframeValue(EAN_AnimationComponent.ComponentType.Rotation, Axis.Z);
            set => SetKeyframeValue(EAN_AnimationComponent.ComponentType.Rotation, Axis.Z, value);
        }
        public float TargetPosW
        {
            get => GetKeyframeValue(EAN_AnimationComponent.ComponentType.Rotation, Axis.W);
            set => SetKeyframeValue(EAN_AnimationComponent.ComponentType.Rotation, Axis.W, value);
        }

        public float Roll
        {
            get => (float)MathHelpers.ConvertRadiansToDegrees(GetKeyframeValue(EAN_AnimationComponent.ComponentType.Scale, Axis.X));
            set => SetKeyframeValue(EAN_AnimationComponent.ComponentType.Scale, Axis.X, (float)MathHelpers.ConvertDegreesToRadians(value));
        }
        public float FoV
        {
            get => (float)MathHelpers.ConvertRadiansToDegrees(GetKeyframeValue(EAN_AnimationComponent.ComponentType.Scale, Axis.Y));
            set => SetKeyframeValue(EAN_AnimationComponent.ComponentType.Scale, Axis.Y, (float)MathHelpers.ConvertDegreesToRadians(value));
        }

        //Colors
        private readonly Brush AccentBrush;
        private readonly Brush BackgroundBrush;
        public Brush PosBrush
        {
            get
            {
                if (SceneManager.IsPlaying) return BackgroundBrush;

                if (HasKeyframe(EAN_AnimationComponent.ComponentType.Position))
                    return AccentBrush;

                //Default
                return BackgroundBrush;
            }
        }
        public Brush TargetPosBrush
        {
            get
            {
                if (SceneManager.IsPlaying) return BackgroundBrush;

                if (HasKeyframe(EAN_AnimationComponent.ComponentType.Rotation))
                    return AccentBrush;

                //Default
                return BackgroundBrush;
            }
        }
        public Brush CameraBrush
        {
            get
            {
                if (SceneManager.IsPlaying) return BackgroundBrush;

                if (HasKeyframe(EAN_AnimationComponent.ComponentType.Scale))
                    return AccentBrush;

                //Default
                return BackgroundBrush;
            }
        }

        private float GetKeyframeValue(EAN_AnimationComponent.ComponentType type, Axis axis)
        {
            if (SelectedAnimation != null)
            {
                var component = SelectedAnimation.GetNode(EAN_Node.CAM_NODE).GetComponent(type);

                if (component != null)
                    return component.GetKeyframeValue(CurrentFrame, axis);
            }

            return EAN_Keyframe.GetDefaultValue(type, axis, true);
        }

        private void SetKeyframeValue(EAN_AnimationComponent.ComponentType type, Axis axis, float value)
        {
            if (SelectedAnimation == null) return;
            var undos = SelectedAnimation.GetNode(EAN_Node.CAM_NODE).SetKeyframe(CurrentFrame, type, axis, false, value);

            UndoManager.Instance.AddCompositeUndo(undos, EAN_AnimationComponent.GetCameraTypeString(type, axis), UndoGroup.Camera);
            SceneManager.UpdateCameraAnimation();

            //Update color of keyframes
            NotifyPropertyChanged(nameof(CurrentKeyframes));
            NotifyPropertyChanged(nameof(PosBrush));
            NotifyPropertyChanged(nameof(TargetPosBrush));
            NotifyPropertyChanged(nameof(CameraBrush));
        }

        private bool HasKeyframe(EAN_AnimationComponent.ComponentType type)
        {
            if (SelectedAnimation != null)
            {
                var component = SelectedAnimation.GetNode(EAN_Node.CAM_NODE).GetComponent(type);

                if (component != null)
                    return component.HasKeyframe(CurrentFrame);
            }

            return false;
        }
        #endregion

        public CameraTabView()
        {
            InitializeComponent();
            DataContext = this;
            animDataGrid.SelectionChanged += AnimListBox_SelectionChanged;
            keyframeListBox.SelectionChanged += KeyframeListBox_SelectionChanged;
            SceneManager.CameraCurrentFrameChanged += SceneManager_CameraCurrentFrameChanged;
            SceneManager.PlayStateChanged += SceneManager_PlayStateChanged;
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;

            //Load colors for keyframe values
            AccentBrush = (Brush)UserControl.FindResource("accentBrush");
            BackgroundBrush = (Brush)UserControl.FindResource("backgroundBrush");
        }

        #region AnimationValues
        public ushort SelectedAnimationID
        {
            get => (ushort)(SelectedAnimation != null ? SelectedAnimation.ID_UShort : 0);
            set
            {
                if (SelectedAnimation != null && value >= 0)
                {
                    ChangeAnimIdAsync(value);
                }

                NotifyPropertyChanged(nameof(SelectedAnimationID));
            }
        }
        
        public string SelectedAnimationName
        {
            get => (SelectedAnimation != null ? SelectedAnimation.Name : "");
            set
            {
                if (SelectedAnimation != null && !string.IsNullOrWhiteSpace(value))
                {
                    ChangeAnimName(value);
                }

                NotifyPropertyChanged(nameof(SelectedAnimationName));
            }
        }


        private async void ChangeAnimIdAsync(int id)
        {
            string result = id.ToString();
            if (files.SelectedItem?.SelectedCamFile?.File.Animations.FirstOrDefault(a => a.Index == result && a != SelectedAnimation) != null)
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "ID Already Used", "The entered ID is already used by another camera. Please enter a unique one.", MessageDialogStyle.Affirmative, DialogSettings.Default);
            }
            else
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(SelectedAnimation.Index), SelectedAnimation, SelectedAnimation.Index, result, "Camera ID"));
                SelectedAnimation.Index = result;
            }

            files.SelectedItem?.SelectedCamFile?.File.SortEntries();
        }

        private async void ChangeAnimName(string name)
        {
            if (files.SelectedItem?.SelectedCamFile?.File.Animations.FirstOrDefault(a => a.Name == name && a != SelectedAnimation) != null)
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "Name Already Used", "The entered name is already used by another camera. Please enter a unique one.", MessageDialogStyle.Affirmative, DialogSettings.Default);
            }
            else
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(SelectedAnimation.Name), SelectedAnimation, SelectedAnimation.Name, name, "Animation Name"));
                SelectedAnimation.Name = name;
            }
        }
        
        #endregion

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

            files.SelectedMove.Files.AddCamEanFile(EAN_File.DefaultCamFile(), result.ToUpper(), null, false, false, Xenoverse2.MoveType.Skill);
            var eanFile = files.SelectedMove.Files.CamEanFile[files.SelectedMove.Files.CamEanFile.Count - 1];
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
        
        public RelayCommand NewAnimationCommand => new RelayCommand(NewAnimation, IsEanFileSelected);
        private void NewAnimation()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            var anim = SelectedEanFile.AddNewCamera(undos);

            UndoManager.Instance.AddCompositeUndo(undos, "New Camera", UndoGroup.Camera);

            files.SelectedItem.SelectedCamera = anim;
            animDataGrid.ScrollIntoView(anim);
            UpdateProperties();
        }

        public RelayCommand RescaleAnimationCommand => new RelayCommand(RescaleAnimation, IsAnimationSelected);
        private void RescaleAnimation()
        {
            if (SelectedAnimation != null)
            {
                EanModiferForm form = new EanModiferForm("Rescale Camera");
                form.EndFrame = SelectedAnimation.FrameCount - 1;
                form.NewDuration = SelectedAnimation.FrameCount;
                form.NewDurationEnabled = true;
                form.StartFrameEnabled = true;
                form.EndFrameEnabled = true;
                form.ShowDialog();

                if (form.Success)
                {
                    List<IUndoRedo> undos = SelectedAnimation.RescaleAnimation(form.NewDuration, form.StartFrame, form.EndFrame);

                    UndoManager.Instance.AddCompositeUndo(undos, "Rescale Camera", UndoGroup.Camera);
                    UpdateProperties();
                    SceneManager.InvokeCameraDataChangedEvent();
                }
            }
        }

        public RelayCommand ReverseAnimationCommand => new RelayCommand(ReverseAnimation, IsAnimationSelected);
        private void ReverseAnimation()
        {
            if (SelectedAnimation != null)
            {
                EanModiferForm form = new EanModiferForm("Reverse Camera");
                form.EndFrame = SelectedAnimation.FrameCount - 1;
                form.StartFrameEnabled = true;
                form.EndFrameEnabled = true;
                form.ShowDialog();

                if (form.Success)
                {
                    List<IUndoRedo> undos = SelectedAnimation.ReverseAnimation(form.StartFrame, form.EndFrame);

                    UndoManager.Instance.AddCompositeUndo(undos, "Reverse Camera", UndoGroup.Camera);
                    UpdateProperties();
                }
            }
        }

        public RelayCommand CopyAnimationCommand => new RelayCommand(CopyAnimation, IsAnimationSelected);
        private void CopyAnimation()
        {
            List<EAN_Animation> selectedAnimations = animDataGrid.SelectedItems.Cast<EAN_Animation>().ToList();

            if (selectedAnimations.Count > 0)
            {
                Clipboard.SetData(ClipboardConstants.EanCameraAnimation, selectedAnimations);
            }
        }

        public RelayCommand PasteAnimationCommand => new RelayCommand(PasteAnimation, CanPasteAnim);
        private void PasteAnimation()
        {
            List<EAN_Animation> copiedAnims = (List<EAN_Animation>)Clipboard.GetData(ClipboardConstants.EanCameraAnimation);

            if (copiedAnims.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                EAN_Animation selected = null;

                foreach(var anim in copiedAnims)
                {
                    SelectedEanFile.AddEntry(anim, undos);
                    selected = anim;
                }

                animDataGrid.SelectedItem = copiedAnims[0];
                animDataGrid.ScrollIntoView(copiedAnims[0]);

                UndoManager.Instance.AddCompositeUndo(undos, "Paste Camera", UndoGroup.Camera);

                if(selected != null)
                {
                    files.SelectedItem.SelectedCamera = selected;
                    animDataGrid.ScrollIntoView(selected);
                    UpdateProperties();
                }
            }
        }

        public RelayCommand DuplicateAnimationCommand => new RelayCommand(DuplicateAnimation, IsAnimationSelected);
        private void DuplicateAnimation()
        {
            List<EAN_Animation> selectedAnimations = animDataGrid.SelectedItems.Cast<EAN_Animation>().ToList();

            if (selectedAnimations.Count > 0)
            {
                EAN_Animation animation = null;
                List<IUndoRedo> undos = new List<IUndoRedo>();

                for (int i = 0; i < selectedAnimations.Count; i++)
                {
                    animation = selectedAnimations[i].Copy();
                    SelectedEanFile.AddEntry(selectedAnimations[i].Copy(), undos);
                }

                animDataGrid.SelectedItem = animation;
                animDataGrid.ScrollIntoView(animation);

                UndoManager.Instance.AddCompositeUndo(undos, "Duplicate Camera", UndoGroup.Camera);
            }
        }

        public RelayCommand DeleteAnimationsCommand => new RelayCommand(DeleteAnimations, IsAnimationSelected);
        private void DeleteAnimations()
        {
            List<EAN_Animation> selectedAnimations = animDataGrid.SelectedItems.Cast<EAN_Animation>().ToList();

            if (selectedAnimations.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach(var anim in selectedAnimations)
                {
                    undos.Add(new UndoableListRemove<EAN_Animation>(SelectedEanFile.Animations, anim));
                    SelectedEanFile.Animations.Remove(anim);
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Delete Camera", UndoGroup.Camera);
            }
        }

        public RelayCommand ExtendAnimationCommand => new RelayCommand(ExtendAnimation, IsAnimationSelected);
        private void ExtendAnimation()
        {
            EanModiferForm form = new EanModiferForm("Extend Camera");
            form.NewDuration = SelectedAnimation.FrameCount;
            form.NewDurationEnabled = true;
            form.ShowDialog();

            if (form.Success)
            {
                var undos = SelectedAnimation.ExtendAnimation(form.NewDuration);

                if (undos.Count > 0)
                {
                    UndoManager.Instance.AddCompositeUndo(undos, "Extend Camera", UndoGroup.Camera);
                    UpdateProperties();
                    SceneManager.InvokeCameraDataChangedEvent();
                }
            }
        }

        public RelayCommand ApplyPositionOffsetCommand => new RelayCommand(ApplyPositionOffset, IsAnimationSelected);
        private void ApplyPositionOffset()
        {
            EanModiferForm form = new EanModiferForm("Apply Position Offset");
            form.PosEnabled = true;
            form.ShowDialog();

            if (form.Success)
            {
                List<IUndoRedo> undos = SelectedAnimation.ApplyNodeOffset(0, -1, BoneFilter, EAN_AnimationComponent.ComponentType.Position, form.X, form.Y, form.Z, form.W);

                UndoManager.Instance.AddCompositeUndo(undos, "Apply Position Offset", UndoGroup.Camera);
                UpdateProperties();
            }
        }

        public RelayCommand ApplyTargetPositionOffsetCommand => new RelayCommand(ApplyTargetPositionOffset, IsAnimationSelected);
        private void ApplyTargetPositionOffset()
        {
            EanModiferForm form = new EanModiferForm("Apply Target Position Offset");
            form.PosEnabled = true;
            form.ShowDialog();

            if (form.Success)
            {
                List<IUndoRedo> undos = SelectedAnimation.ApplyNodeOffset(0, -1, BoneFilter, EAN_AnimationComponent.ComponentType.Rotation, form.X, form.Y, form.Z, form.W);

                UndoManager.Instance.AddCompositeUndo(undos, "Apply Target Position Offset", UndoGroup.Camera);
                UpdateProperties();
            }
        }

        public RelayCommand CameraShakeCommand => new RelayCommand(ApplyCameraShake, IsAnimationSelected);
        private void ApplyCameraShake()
        {
            if (SelectedAnimation != null)
            {
                EanModiferForm form = new EanModiferForm("Camera Shake");
                form.StartFrameEnabled = true;
                form.EndFrameEnabled = true;
                form.ShakeFactorEnabled = true;
                form.ShowDialog();

                if (form.Success)
                {
                    List<IUndoRedo> undos = SelectedAnimation.ApplyCameraShake(form.StartFrame, form.EndFrame, form.ShakeFactor);

                    UndoManager.Instance.AddCompositeUndo(undos, "Camera Shake", UndoGroup.Camera);
                    UpdateProperties();
                }
            }
        }


        private bool IsEanFileSelected()
        {
            return SelectedEanFile != null;
        }

        private bool IsAnimationSelected()
        {
            return SelectedAnimation != null;
        }
        
        private bool CanPasteAnim()
        {
            if (SelectedEanFile != null && Clipboard.ContainsData(ClipboardConstants.EanCameraAnimation)) return true;

            return false;
        }
        #endregion

        #region KeyframeCommands
        public RelayCommand AddKeyframeCommand => new RelayCommand(AddKeyframeAsync, CanAddKeyframe);
        private void AddKeyframeAsync()
        {
            float posX = SceneManager.MainGameInstance.camera.CameraState.ActualPosition.X;
            float posY = SceneManager.MainGameInstance.camera.CameraState.ActualPosition.Y;
            float posZ = SceneManager.MainGameInstance.camera.CameraState.ActualPosition.Z;
            float rotX = SceneManager.MainGameInstance.camera.CameraState.ActualTargetPosition.X;
            float rotY = SceneManager.MainGameInstance.camera.CameraState.ActualTargetPosition.Y;
            float rotZ = SceneManager.MainGameInstance.camera.CameraState.ActualTargetPosition.Z;
            float scaleX = -MathHelper.ToRadians(SceneManager.MainGameInstance.camera.CameraState.Roll);
            float scaleY = MathHelper.ToRadians(SceneManager.MainGameInstance.camera.CameraState.FieldOfView);

            //"Unscale" the camera if the current EAN file is not chara unique
            if (!SelectedEanFile.IsCharaUnique && SceneManager.Actors[0] != null)
            {
                posY -= SceneManager.Actors[0].CharacterData.BcsFile.File.F_48[1] - 1f;
                rotY -= SceneManager.Actors[0].CharacterData.BcsFile.File.F_48[1] - 1f;
            }

            List<IUndoRedo> undos = SelectedAnimation.AddKeyframe(EAN_Node.CAM_NODE, CurrentFrame, posX, posY, posZ, 1f, rotX, rotY, rotZ, 1f, scaleX, scaleY, 0f, 0f, PositionComponentEnabled, TargetPositionComponentEnabled, CameraComponentEnabled);

            UndoManager.Instance.AddCompositeUndo(undos, "Add Keyframe", UndoGroup.Camera);
            UpdateProperties();
            SceneManager.InvokeCameraDataChangedEvent();
        }

        public RelayCommand RemoveKeyframeCommand => new RelayCommand(RemoveKeyframe, IsKeyframesSelected);
        private void RemoveKeyframe()
        {
            List<int> keyframes = keyframeListBox.SelectedItems.Cast<int>().ToList();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(int keyframe in keyframes)
            {
                undos.AddRange(SelectedAnimation.RemoveKeyframe(keyframe, PositionComponentEnabled, TargetPositionComponentEnabled, CameraComponentEnabled));
            }

            UpdateProperties();
            UndoManager.Instance.AddCompositeUndo(undos, "Remove Keyframe", UndoGroup.Camera);
            SceneManager.InvokeCameraDataChangedEvent();
        }

        public RelayCommand CopyKeyframeCommand => new RelayCommand(CopyKeyframe, IsKeyframesSelected);
        private void CopyKeyframe()
        {
            List<int> keyframes = keyframeListBox.SelectedItems.Cast<int>().ToList();

            if (keyframes != null)
            {
                List<SerializedBone> serializedBones = SelectedAnimation.SerializeKeyframes(keyframes, BoneFilter, PositionComponentEnabled, TargetPositionComponentEnabled, CameraComponentEnabled, true); //Only ever 1, "Node". It's just easier to reuse the animation code.
                
                if(serializedBones.Count > 0)
                {
                    Clipboard.SetData(ClipboardConstants.CameraKeyframe, serializedBones);
                }
            }
        }

        public RelayCommand PasteKeyframeCommand => new RelayCommand(PasteKeyframe, CanPasteKeyframe);
        private void PasteKeyframe()
        {
            List<SerializedBone> bones = (List<SerializedBone>)Clipboard.GetData(ClipboardConstants.CameraKeyframe);

            if (bones != null)
            {
                EanModiferForm form = new EanModiferForm("Paste Camera Keyframes");
                form.InsertEnabled = true;
                form.RemoveCollisionsEnabled = true;
                form.RebaseKeyframesEnabled = true;
                form.ShowDialog();

                if (form.Success)
                {
                    SerializedBone.StartFrameRebase(bones, form.Append ? SelectedAnimation.FrameCount : form.InsertFrame);
                    List<IUndoRedo> undos = SelectedAnimation.AddKeyframes(bones, form.RemoveCollisions, form.RebaseKeyframes, PositionComponentEnabled, TargetPositionComponentEnabled, CameraComponentEnabled);

                    UndoManager.Instance.AddCompositeUndo(undos, "Paste Keyframe", UndoGroup.Camera);
                    UpdateProperties();
                    SceneManager.InvokeCameraDataChangedEvent();
                }
            }
        }

        public RelayCommand PasteKeyframeValuesCommand => new RelayCommand(PasteKeyframeValues, CanPasteKeyframeValues);
        private void PasteKeyframeValues()
        {
            List<SerializedBone> bones = (List<SerializedBone>)Clipboard.GetData(ClipboardConstants.CameraKeyframe);

            if (bones != null)
            {
                if(bones.Count != 1)
                {
                    Log.Add($"Keyframe value paste failed. Invalid number of bones were in the clipboard ({bones.Count}). 1 was expected.", LogType.Error);
                    return;
                }
                if(bones[0].Keyframes.Length != 1)
                {
                    Log.Add($"Keyframe value paste failed. Invalid number of keyframes were in the clipboard ({bones.Count}). 1 was expected.", LogType.Error);
                    return;
                }

                SerializedKeyframe keyframe = bones[0].Keyframes[0];
                List<IUndoRedo> undos = SelectedAnimation.AddKeyframe(EAN_Node.CAM_NODE, CurrentFrame, keyframe.PosX, keyframe.PosY, keyframe.PosZ, keyframe.PosW, keyframe.RotX, keyframe.RotY, keyframe.RotZ, keyframe.RotW, keyframe.ScaleX, keyframe.ScaleY, keyframe.ScaleZ, keyframe.ScaleW,
                                                                      PositionComponentEnabled, TargetPositionComponentEnabled, CameraComponentEnabled);
                
                UpdateProperties();
                UndoManager.Instance.AddCompositeUndo(undos, "Paste Keyframe Values", UndoGroup.Camera);
            }
        }

        public RelayCommand RebaseKeyframeCommand => new RelayCommand(RebaseKeyframe, IsKeyframesSelected);
        private async void RebaseKeyframe()
        {
            List<int> keyframes = keyframeListBox.SelectedItems.Cast<int>().ToList();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            EanModiferForm form = new EanModiferForm("Rebase Keyframes");
            form.RebaseAmountEnabled = true;
            form.RemoveCollisionsEnabled = true;
            form.ShowDialog();

            if (form.Success)
            {
                if(keyframes.Min() + form.RebaseAmount < 0)
                {
                    _ = await DialogCoordinator.Instance.ShowMessageAsync(this, "Rebase Keyframes", "The specified rebase amount is invalid. The Start Frame of the selection + Rebase Amount cannot go below 0.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                    return;
                }

                SelectedAnimation.GetNode(EAN_Node.CAM_NODE).RebaseKeyframes(keyframes, form.RebaseAmount, form.RemoveCollisions, PositionComponentEnabled, TargetPositionComponentEnabled, CameraComponentEnabled, undos);
                
                UndoManager.Instance.AddCompositeUndo(undos, "Rebase Keyframe", UndoGroup.Camera);
                UpdateProperties();
                SceneManager.InvokeCameraDataChangedEvent();
            }

        }

        public RelayCommand RescaleKeyframeCommand => new RelayCommand(RescaleKeyframe, IsKeyframesSelected);
        private void RescaleKeyframe()
        {
            List<int> keyframes = keyframeListBox.SelectedItems.Cast<int>().ToList();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            EanModiferForm form = new EanModiferForm("Rescale Keyframes");
            form.StartFrame = keyframes.Min();
            form.EndFrame = keyframes.Max();
            form.NewDuration = form.EndFrame - form.StartFrame;
            form.NewDurationEnabled = true;
            form.StartFrameEnabled = true;
            form.EndFrameEnabled = true;
            form.ShowDialog();

            if (form.Success)
            {
                undos.AddRange(SelectedAnimation.GetNode(EAN_Node.CAM_NODE).RescaleNode(form.NewDuration, form.StartFrame, form.EndFrame));

                UndoManager.Instance.AddCompositeUndo(undos, "Rescale Keyframes", UndoGroup.Camera);
                UpdateProperties();
                SceneManager.InvokeCameraDataChangedEvent();
            }

        }

        public RelayCommand ApplyPosOffsetKeyframeCommand => new RelayCommand(ApplyPosOffsetKeyframe, IsKeyframesSelected);
        private void ApplyPosOffsetKeyframe()
        {
            List<int> keyframes = keyframeListBox.SelectedItems.Cast<int>().ToList();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            EanModiferForm form = new EanModiferForm("Apply Position Offset (Keyframe)");
            form.StartFrame = keyframes.Min();
            form.EndFrame = keyframes.Max();
            form.StartFrameEnabled = true;
            form.EndFrameEnabled = true;
            form.PosEnabled = true;
            form.ShowDialog();

            if (form.Success)
            {
                SelectedAnimation.GetNode(EAN_Node.CAM_NODE).ApplyNodeOffset(form.StartFrame, form.EndFrame, EAN_AnimationComponent.ComponentType.Position, form.X, form.Y, form.Z, form.W, undos);

                UndoManager.Instance.AddCompositeUndo(undos, "Apply Position Offset", UndoGroup.Camera);
                UpdateProperties();
                SceneManager.InvokeCameraDataChangedEvent();
            }

        }

        public RelayCommand ApplyTargetPosOffsetKeyframeCommand => new RelayCommand(ApplyTargetPosOffsetKeyframe, IsKeyframesSelected);
        private void ApplyTargetPosOffsetKeyframe()
        {
            List<int> keyframes = keyframeListBox.SelectedItems.Cast<int>().ToList();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            EanModiferForm form = new EanModiferForm("Apply Target Position Offset (Keyframe)");
            form.StartFrame = keyframes.Min();
            form.EndFrame = keyframes.Max();
            form.StartFrameEnabled = true;
            form.EndFrameEnabled = true;
            form.PosEnabled = true;
            form.ShowDialog();

            if (form.Success)
            {
                SelectedAnimation.GetNode(EAN_Node.CAM_NODE).ApplyNodeOffset(form.StartFrame, form.EndFrame, EAN_AnimationComponent.ComponentType.Rotation, form.X, form.Y, form.Z, form.W, undos);

                UndoManager.Instance.AddCompositeUndo(undos, "Apply Target Position Offset", UndoGroup.Camera);
                UpdateProperties();
                SceneManager.InvokeCameraDataChangedEvent();
            }

        }


        private bool IsKeyframesSelected()
        {
            return keyframeListBox.SelectedItems.Count > 0;
        }

        private bool CanAddKeyframe()
        {
            if (!IsAnimationSelected() || (!PositionComponentEnabled && !TargetPositionComponentEnabled && !CameraComponentEnabled)) return false;
            if (CurrentFrame >= 0) return true;
            return false;
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

        #region Events
        private void AnimListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(animDataGrid.SelectedIndex != -1)
            {
                PlaySelectedAnimation();
            }
        }

        private void SceneManager_CameraCurrentFrameChanged(object sender, EventArgs e)
        {
            if (SceneManager.CurrentSceneState == EditorTabs.Camera && SyncCurrentFrameWithView)
            {
                _currentFrame = (int)SceneManager.MainGameInstance.camera.CurrentFrame;
                NotifyPropertyChanged(nameof(CurrentFrame));
                UpdateKeyframeValues();
            }
        }

        private void KeyframeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (keyframeListBox.SelectedItem is int value)
            {
                CurrentFrame = value;
                UpdateKeyframeValues();
            }

            SceneManager.UpdateCameraAnimation();
        }

        private void AnimListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SceneManager.PlayCameraAnimation(SelectedEanFile, SelectedAnimation);
            UpdateProperties();
        }

        private void Instance_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {
            if (e.UndoGroup == UndoGroup.Camera)
                UpdateProperties();
        }

        private void SceneManager_PlayStateChanged(object sender, EventArgs e)
        {
            //The brushes must be updated when a play state change happens (pause/play/stop) as the brushes are always set to default when playing.

            NotifyPropertyChanged(nameof(PosBrush));
            NotifyPropertyChanged(nameof(TargetPosBrush));
            NotifyPropertyChanged(nameof(CameraBrush));
        }

        //Update Functions:
        private void UpdateKeyframeValues()
        {
            NotifyPropertyChanged(nameof(PosX));
            NotifyPropertyChanged(nameof(PosY));
            NotifyPropertyChanged(nameof(PosZ));
            NotifyPropertyChanged(nameof(PosW));
            NotifyPropertyChanged(nameof(TargetPosX));
            NotifyPropertyChanged(nameof(TargetPosY));
            NotifyPropertyChanged(nameof(TargetPosZ));
            NotifyPropertyChanged(nameof(TargetPosW));
            NotifyPropertyChanged(nameof(Roll));
            NotifyPropertyChanged(nameof(FoV));
            NotifyPropertyChanged(nameof(PosBrush));
            NotifyPropertyChanged(nameof(TargetPosBrush));
            NotifyPropertyChanged(nameof(CameraBrush));
        }

        public void UpdateProperties()
        {
            NotifyPropertyChanged(nameof(CurrentKeyframes));
            UpdateKeyframeValues();
            animDataGrid.Items.Refresh();

            //Ensure that the camera animation gets updated to reflect any changes, even if it is paused.
            SceneManager.UpdateCameraAnimation();
        }
        #endregion

    }
}
