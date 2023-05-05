using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using XenoKit.Editor;
using XenoKit.Engine;
using Xv2CoreLib;
using Xv2CoreLib.EAN;
using Xv2CoreLib.Resource.UndoRedo;
using XenoKit.Windows.EAN;
using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Media;
using XenoKit.Helper;

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

        public static AnimationTabView Instance = null;
        public Files files { get { return Files.Instance; } }
        public EAN_File SelectedEanFile => files.SelectedItem?.SelectedEanFile?.File;
        public EAN_Animation SelectedAnimation => files.SelectedItem?.SelectedAnimation;
        public float CurrentFrame => SceneManager.Actors[0] != null ? SceneManager.Actors[0].AnimationPlayer.PrimaryCurrentFrame : 0;
        public float PreviousFrame => SceneManager.Actors[0] != null ? SceneManager.Actors[0].AnimationPlayer.PrimaryPrevFrame : 0;

        //Values
        private EAN_Node _selectedBone = null;

        //Editing
        public int SelectedFrame
        {
            get 
            {

                if (SceneManager.Actors[0]?.AnimationPlayer?.PrimaryAnimation != null)
                {
                    return SceneManager.Actors[0].AnimationPlayer.PrimaryAnimation.CurrentFrame_Int;
                }
                return 0;
            }
            set
            {
                if(SceneManager.Actors[0]?.AnimationPlayer?.PrimaryAnimation != null)
                {
                    SceneManager.Actors[0].AnimationPlayer.PrimaryAnimation.SkipToFrame(value);
                    NotifyPropertyChanged(nameof(SelectedFrame));
                }
            }
        }
        public int SelectedFrameEdit
        {
            get => SelectedFrame;
            set
            {
                if (SelectedFrame != value && IsKeyframeSelected())
                {
                    var selectedBones = SelectedBones;
                    List<IUndoRedo> undos = new List<IUndoRedo>();

                    foreach(var bone in selectedBones)
                        bone.RebaseKeyframes(new List<int>() { SelectedFrame }, value - SelectedFrame, false, PositionComponentEnabled, RotationComponentEnabled, ScaleComponentEnabled, undos);

                    UndoManager.Instance.AddCompositeUndo(undos, "Edit Keyframe", UndoGroup.Animation);
                    UpdateProperties();
                    SceneManager.InvokeAnimationDataChangedEvent();
                }
            }
        }

        //View
        public ObservableCollection<int> CurrentKeyframes
        {
            get { return (_selectedBone != null) ? new ObservableCollection<int>(EAN_Node.GetAllKeyframes(SelectedBones, PositionComponentEnabled, RotationComponentEnabled, ScaleComponentEnabled)) : null; }
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
                    //NotifyPropertyChanged(nameof(CurrentKeyframes));

                    //Propagate changes to the Editor side
                    SelectedBoneChanged(_selectedBone?.BoneName);
                    UpdateKeyframeValues();
                }
            }
        }
        public List<EAN_Node> SelectedBones
        {
            get
            {
                return boneDataGrid.SelectedItems.Cast<EAN_Node>().ToList();
            }
        }
        

        //ContextMenu options
        public List<string> AddBones
        {
            get
            {
                if (SelectedEanFile == null) return null;
                var selectedBones = SelectedAnimation.Nodes;
                List<string> addBones = new List<string>();

                foreach(var bone in SelectedEanFile.Skeleton.NonRecursiveBones.Where(x => selectedBones.FirstOrDefault(y => x.Name == y.BoneName) == null))
                {
                    addBones.Add(bone.Name);
                }

                return addBones;
            }
        }
        public List<string> AddBoneComponent
        {
            get
            {
                if (SelectedBone == null) return null;

                List<string> types = new List<string>();
                types.Add(EAN_AnimationComponent.ComponentType.Position.ToString());
                types.Add(EAN_AnimationComponent.ComponentType.Rotation.ToString());
                types.Add(EAN_AnimationComponent.ComponentType.Scale.ToString());

                types.RemoveAll(x => SelectedBone.AnimationComponents.Any(y => y.Type.ToString() == x));

                return types;
            }
        }
        public List<string> RemoveBoneComponent
        {
            get
            {
                if (SelectedBone == null) return null;
                var selectedBones = SelectedBones;

                List<string> types = new List<string>();

                foreach(var bone in selectedBones)
                {
                    foreach (var comp in bone.AnimationComponents)
                    {
                        string val = comp.Type.ToString();
                        if (!types.Contains(val))
                            types.Add(val);
                    }
                }

                return types;
            }
        }

        //Components:
        private bool _posComponent = true;
        private bool _rotComponent = true;
        private bool _scaleComponent = true;
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
        public bool RotationComponentEnabled
        {
            get => _rotComponent;
            set
            {
                _rotComponent = value;
                NotifyPropertyChanged(nameof(RotationComponentEnabled));
                NotifyPropertyChanged(nameof(CurrentKeyframes));
            }
        }
        public bool ScaleComponentEnabled
        {
            get => _scaleComponent;
            set
            {
                _scaleComponent = value;
                NotifyPropertyChanged(nameof(ScaleComponentEnabled));
                NotifyPropertyChanged(nameof(CurrentKeyframes));
            }
        }

        #region KeyframeValues
        public bool KeyframeValuesEnabled => SelectedBone != null;

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

        public float RotX
        {
            get => GetKeyframeValue(EAN_AnimationComponent.ComponentType.Rotation, Axis.X);
            set => SetKeyframeValue(EAN_AnimationComponent.ComponentType.Rotation, Axis.X, value);
        }
        public float RotY
        {
            get => GetKeyframeValue(EAN_AnimationComponent.ComponentType.Rotation, Axis.Y);
            set => SetKeyframeValue(EAN_AnimationComponent.ComponentType.Rotation, Axis.Y, value);
        }
        public float RotZ
        {
            get => GetKeyframeValue(EAN_AnimationComponent.ComponentType.Rotation, Axis.Z);
            set => SetKeyframeValue(EAN_AnimationComponent.ComponentType.Rotation, Axis.Z, value);
        }
        public float RotW
        {
            get => GetKeyframeValue(EAN_AnimationComponent.ComponentType.Rotation, Axis.W);
            set => SetKeyframeValue(EAN_AnimationComponent.ComponentType.Rotation, Axis.W, value);
        }

        public float ScaleX
        {
            get => GetKeyframeValue(EAN_AnimationComponent.ComponentType.Scale, Axis.X);
            set => SetKeyframeValue(EAN_AnimationComponent.ComponentType.Scale, Axis.X, value);
        }
        public float ScaleY
        {
            get => GetKeyframeValue(EAN_AnimationComponent.ComponentType.Scale, Axis.Y);
            set => SetKeyframeValue(EAN_AnimationComponent.ComponentType.Scale, Axis.Y, value);
        }
        public float ScaleZ
        {
            get => GetKeyframeValue(EAN_AnimationComponent.ComponentType.Scale, Axis.Z);
            set => SetKeyframeValue(EAN_AnimationComponent.ComponentType.Scale, Axis.Z, value);
        }
        public float ScaleW
        {
            get => GetKeyframeValue(EAN_AnimationComponent.ComponentType.Scale, Axis.W);
            set => SetKeyframeValue(EAN_AnimationComponent.ComponentType.Scale, Axis.W, value);
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
        public Brush RotBrush
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
        public Brush ScaleBrush
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
            if (SelectedBone == null) return 0;
            var component = SelectedBone.GetComponent(type);

            if (component != null)
                return component.GetKeyframeValue(CurrentFrame, axis);

            return EAN_Keyframe.GetDefaultValue(type, axis);
        }

        private void SetKeyframeValue(EAN_AnimationComponent.ComponentType type, Axis axis, float value)
        {
            if (SelectedBone == null) return;
            var undos = SelectedBone.SetKeyframe((int)CurrentFrame, type, axis, false, value);

            UndoManager.Instance.AddCompositeUndo(undos, $"{type} {axis}");
            SceneManager.InvokeAnimationDataChangedEvent();

            NotifyPropertyChanged(nameof(CurrentKeyframes));
            NotifyPropertyChanged(nameof(PosBrush));
            NotifyPropertyChanged(nameof(RotBrush));
            NotifyPropertyChanged(nameof(ScaleBrush));
        }

        private bool HasKeyframe(EAN_AnimationComponent.ComponentType type)
        {
            if (SelectedBone != null)
            {
                var selectedBones = SelectedBones;

                foreach(var bone in selectedBones)
                {
                    var component = bone.GetComponent(type);

                    if (component != null)
                        return component.HasKeyframe((int)CurrentFrame);
                }
            }

            return false;
        }
        #endregion

        public AnimationTabView()
        {
            Instance = this;
            InitializeComponent();
            DataContext = this;
            Game.GameUpdate += SceneManager_UpdateEvent;
            Engine.Animation.VisualSkeleton.SelectedBoneChanged += VisualSkeleton_SelectedBoneChanged;
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            SceneManager.AnimationDataChanged += SceneManager_AnimationDataChanged;
            SceneManager.PlayStateChanged += SceneManager_PlayStateChanged;

            //Load colors for keyframe values
            AccentBrush = (Brush)UserControl.FindResource("accentBrush");
            BackgroundBrush = (Brush)UserControl.FindResource("backgroundBrush");
        }


        #region SelectedAnimationValues
        public ushort SelectedAnimationID
        {
            get => (ushort)(SelectedAnimation != null ? SelectedAnimation.ID_UShort : 0);
            set
            {
                if(SelectedAnimation != null && value >= 0)
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
            if (files.SelectedItem?.SelectedEanFile?.File.Animations.FirstOrDefault(a => a.Index == result && a != SelectedAnimation) != null)
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "ID Already Used", "The entered ID is already used by another animation. Please enter a unique one.", MessageDialogStyle.Affirmative, DialogSettings.Default);
            }
            else
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(SelectedAnimation.Index), SelectedAnimation, SelectedAnimation.Index, result, "Animation ID"));
                SelectedAnimation.Index = result;
            }

            files.SelectedItem?.SelectedEanFile?.File.SortEntries();
        }

        private async void ChangeAnimName(string name)
        {
            if (files.SelectedItem?.SelectedEanFile?.File.Animations.FirstOrDefault(a => a.Name == name && a != SelectedAnimation) != null)
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "Name Already Used", "The entered name is already used by another animation. Please enter a unique one.", MessageDialogStyle.Affirmative, DialogSettings.Default);
            }
            else
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(SelectedAnimation.Name), SelectedAnimation, SelectedAnimation.Name, name, "Animation Name"));
                SelectedAnimation.Name = name;
            }
        }

        #endregion

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

        public RelayCommand NewAnimationCommand => new RelayCommand(NewAnimation, IsEanFileSelected);
        private void NewAnimation()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            var anim = SelectedEanFile.AddNewAnimation(undos);

            UndoManager.Instance.AddCompositeUndo(undos, "New Animation", UndoGroup.Animation);

            files.SelectedItem.SelectedAnimation = anim;
            animListBox.ScrollIntoView(anim);
        }

        public RelayCommand RescaleAnimationCommand => new RelayCommand(RescaleAnimation, IsAnimationSelected);
        private void RescaleAnimation()
        {
            if (SelectedAnimation != null)
            {
                EanModiferForm form = new EanModiferForm("Rescale Animation");
                form.EndFrame = SelectedAnimation.FrameCount - 1;
                form.NewDuration = SelectedAnimation.FrameCount;
                form.NewDurationEnabled = true;
                form.StartFrameEnabled = true;
                form.EndFrameEnabled = true;
                form.ShowDialog();

                if (form.Success)
                {
                    List<IUndoRedo> undos = SelectedAnimation.RescaleAnimation(form.NewDuration, form.StartFrame, form.EndFrame);

                    UndoManager.Instance.AddCompositeUndo(undos, "Rescale Animation", UndoGroup.Animation);

                    SceneManager.InvokeAnimationDataChangedEvent();
                }
            }
        }

        public RelayCommand ReverseAnimationCommand => new RelayCommand(ReverseAnimation, IsAnimationSelected);
        private void ReverseAnimation()
        {
            if (SelectedAnimation != null)
            {
                EanModiferForm form = new EanModiferForm("Reverse Animation");
                form.EndFrame = SelectedAnimation.FrameCount - 1;
                form.StartFrameEnabled = true;
                form.EndFrameEnabled = true;
                form.ShowDialog();

                if (form.Success)
                {
                    List<IUndoRedo> undos = SelectedAnimation.ReverseAnimation(form.StartFrame, form.EndFrame);

                    UndoManager.Instance.AddCompositeUndo(undos, "Reverse Animation", UndoGroup.Animation);

                    SceneManager.InvokeAnimationDataChangedEvent();
                }
            }
        }

        public RelayCommand CopyAnimationCommand => new RelayCommand(CopyAnimation, IsAnimationSelected);
        private void CopyAnimation()
        {
            List<EAN_Animation> selectedAnimations = animListBox.SelectedItems.Cast<EAN_Animation>().ToList();

            if (selectedAnimations.Count > 0)
            {
                var serializedAnimations = SerializedAnimation.Serialize(selectedAnimations, SelectedEanFile.Skeleton);
                Clipboard.SetData(ClipboardConstants.EanAnimation, serializedAnimations);
            }
        }

        public RelayCommand PasteAnimationCommand => new RelayCommand(PasteAnimation, CanPasteAnim);
        private void PasteAnimation()
        {
            List<SerializedAnimation> copiedAnims = (List<SerializedAnimation>)Clipboard.GetData(ClipboardConstants.EanAnimation);

            if (copiedAnims.Count > 0)
            {
                List<EAN_Animation> anims = SerializedAnimation.Deserialize(copiedAnims, SelectedEanFile.Skeleton);
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach (var anim in anims)
                {
                    SelectedEanFile.AddEntry(anim, undos);
                }

                animListBox.SelectedItem = anims[0];
                animListBox.ScrollIntoView(anims[0]);

                UndoManager.Instance.AddCompositeUndo(undos, "Paste Animation", UndoGroup.Animation);
            }
        }

        public RelayCommand DuplicateAnimationCommand => new RelayCommand(DuplicateAnimation, IsAnimationSelected);
        private void DuplicateAnimation()
        {
            List<EAN_Animation> selectedAnimations = animListBox.SelectedItems.Cast<EAN_Animation>().ToList();

            if (selectedAnimations.Count > 0)
            {
                EAN_Animation animation = null;
                List<IUndoRedo> undos = new List<IUndoRedo>();

                for(int i = 0; i < selectedAnimations.Count; i++)
                {
                    animation = selectedAnimations[i].Copy();
                    SelectedEanFile.AddEntry(animation, undos);
                }

                animListBox.SelectedItem = animation;
                animListBox.ScrollIntoView(animation);

                UndoManager.Instance.AddCompositeUndo(undos, "Duplicate Animation", UndoGroup.Animation);
            }
        }

        public RelayCommand DeleteAnimationsCommand => new RelayCommand(DeleteAnimations, IsAnimationSelected);
        private void DeleteAnimations()
        {
            List<EAN_Animation> selectedAnimations = animListBox.SelectedItems.Cast<EAN_Animation>().ToList();

            if (selectedAnimations.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach (var anim in selectedAnimations)
                {
                    undos.Add(new UndoableListRemove<EAN_Animation>(SelectedEanFile.Animations, anim));
                    SelectedEanFile.Animations.Remove(anim);
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Delete Animation");
            }
        }

        public RelayCommand ExtendAnimationCommand => new RelayCommand(ExtendAnimation, IsAnimationSelected);
        private void ExtendAnimation()
        {
            EanModiferForm form = new EanModiferForm("Extend Animation");
            form.NewDuration = files.SelectedItem.SelectedAnimation.FrameCount;
            form.NewDurationEnabled = true;
            form.ShowDialog();

            if (form.Success)
            {
                var undos = SelectedAnimation.ExtendAnimation(form.NewDuration);

                if (undos.Count > 0)
                {
                    UndoManager.Instance.AddCompositeUndo(undos, "Extend Animation", UndoGroup.Animation);
                    SceneManager.InvokeAnimationDataChangedEvent();
                }
            }
        }

        public RelayCommand RemoveKeyframeRangeCommand => new RelayCommand(RemoveKeyframeRange, IsAnimationSelected);
        private void RemoveKeyframeRange()
        {
            EanModiferForm form = new EanModiferForm("Remove Keyframe Range");
            form.StartFrameEnabled = true;
            form.EndFrameEnabled = true;
            form.RebaseKeyframesEnabled = true;
            form.ShowDialog();

            if (form.Success)
            {
                List<IUndoRedo> undos = SelectedAnimation.RemoveKeyframeRange(form.StartFrame, form.EndFrame, form.RebaseKeyframes);
                UndoManager.Instance.AddCompositeUndo(undos, "Remove Keyframes", UndoGroup.Animation);
                SceneManager.InvokeAnimationDataChangedEvent();
            }
        }

        public RelayCommand CopyKeyframeRangeCommand => new RelayCommand(CopyKeyframeRange, IsAnimationSelected);
        private void CopyKeyframeRange()
        {
            EanModiferForm form = new EanModiferForm("Copy Keyframe Range");
            form.StartFrameEnabled = true;
            form.EndFrameEnabled = true;
            form.ShowDialog();

            if (form.Success)
            {
                List<SerializedBone> serialziedBones = SelectedAnimation.SerializeKeyframeRange(form.StartFrame, form.EndFrame, SelectedEanFile.Skeleton.GetBoneList());
                Clipboard.SetData(ClipboardConstants.EanAnimationKeyframe, serialziedBones);
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
            if (SelectedEanFile != null && Clipboard.ContainsData(ClipboardConstants.EanAnimation)) return true;

            return false;
        }

        #endregion

        #region BoneFunctions
        //Commands:
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
            UndoManager.Instance.AddCompositeUndo(undos, "Paste Bones", UndoGroup.Animation);
            SceneManager.InvokeAnimationDataChangedEvent();
        }

        public RelayCommand DeleteBoneCommand => new RelayCommand(DeleteBone, IsBoneSelected);
        private void DeleteBone()
        {
            List<EAN_Node> bones = boneDataGrid.SelectedItems.Cast<EAN_Node>().ToList();

            if (bones != null)
            {
                List<IUndoRedo> undos = files.SelectedItem.SelectedAnimation.RemoveNodes(bones);
                UndoManager.Instance.AddCompositeUndo(undos, "Delete Bones", UndoGroup.Animation);
                SceneManager.InvokeAnimationDataChangedEvent();
            }
        }

        public RelayCommand RescaleBoneCommand => new RelayCommand(RescaleBone, IsBoneSelected);
        private void RescaleBone()
        {
            List<EAN_Node> bones = boneDataGrid.SelectedItems.Cast<EAN_Node>().ToList();

            if (bones != null)
            {
                EanModiferForm form = new EanModiferForm("Rescale Bone");
                form.EndFrame = SelectedAnimation.FrameCount - 1;
                form.NewDuration = files.SelectedItem.SelectedAnimation.FrameCount;
                form.StartFrameEnabled = true;
                form.EndFrameEnabled = true;
                form.NewDurationEnabled = true;
                form.ShowDialog();

                if (form.Success)
                {
                    List<IUndoRedo> undos = new List<IUndoRedo>();

                    foreach (var bone in bones)
                    {
                        undos.AddRange(bone.RescaleNode(form.NewDuration, form.StartFrame, form.EndFrame));
                    }

                    UndoManager.Instance.AddCompositeUndo(undos, "Rescale Bone", UndoGroup.Animation);

                    SceneManager.InvokeAnimationDataChangedEvent();
                }
            }
        }

        public RelayCommand ApplyBoneOffsetCommand => new RelayCommand(ApplyBoneOffset, IsBoneSelected);
        private void ApplyBoneOffset()
        {
            List<EAN_Node> bones = boneDataGrid.SelectedItems.Cast<EAN_Node>().ToList();

            if(bones != null)
            {
                EanModiferForm form = new EanModiferForm("Apply Bone Offset");
                form.ComponentEnabled = true;
                form.PosEnabled = true;
                form.ShowDialog();

                if (form.Success)
                {
                    List<IUndoRedo> undos = new List<IUndoRedo>();

                    foreach (var bone in bones)
                    {
                        bone.ApplyNodeOffset(0, -1, form.Component, form.X, form.Y, form.Z, form.W, undos);
                    }

                    UndoManager.Instance.AddCompositeUndo(undos, "Apply Bone Offset", UndoGroup.Animation);
                    SceneManager.InvokeAnimationDataChangedEvent();
                }
            }
        }

        public RelayCommand RebaseBoneCommand => new RelayCommand(RebaseBone, IsBoneSelected);
        private void RebaseBone()
        {
            List<EAN_Node> bones = boneDataGrid.SelectedItems.Cast<EAN_Node>().ToList();

            if (bones != null)
            {
                EanModiferForm form = new EanModiferForm("Rebase Bone");
                form.StartFrameEnabled = true;
                form.RebaseAmountEnabled = true;
                form.RemoveCollisionsEnabled = true;
                form.StartFrameConstraintEnabled = false;
                form.ShowDialog();

                if (form.Success)
                {
                    List<IUndoRedo> undos = new List<IUndoRedo>();

                    foreach (var bone in bones)
                    {
                        bone.RebaseKeyframes(form.StartFrame, form.RebaseAmount, form.RemoveCollisions, PositionComponentEnabled, RotationComponentEnabled, ScaleComponentEnabled, undos);
                    }

                    UndoManager.Instance.AddCompositeUndo(undos, "Rebase Bone", UndoGroup.Animation);
                    SceneManager.InvokeAnimationDataChangedEvent();
                }
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

        //Events:
        private void AddBone_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedAnimation == null) return;

            string boneName = ViewHelpers.GetMenuItemString(e);

            if (!string.IsNullOrWhiteSpace(boneName))
            {
                IUndoRedo undo = SelectedAnimation.AddBone(boneName);

                if (undo != null)
                    UndoManager.Instance.AddCompositeUndo(new List<IUndoRedo>() { undo }, "Add Bone", UndoGroup.Animation);

                SceneManager.InvokeAnimationDataChangedEvent();
            }
        }

        private void AddBoneComponent_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedBone == null) return;

            List<IUndoRedo> undos = new List<IUndoRedo>();

            string animComponent = ViewHelpers.GetMenuItemString(e);
            EAN_AnimationComponent.ComponentType type;

            if (Enum.TryParse(animComponent, out type))
            {
                SelectedBone.GetComponent(type, true, undos);
                SceneManager.InvokeAnimationDataChangedEvent();
            }

            if (undos.Count > 0)
                UndoManager.Instance.AddCompositeUndo(undos, "Add Component", UndoGroup.Animation);
        }

        private void RemoveBoneComponent_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedBone == null) return;
            var selectedBones = SelectedBones;

            List<IUndoRedo> undos = new List<IUndoRedo>();

            string animComponent = ViewHelpers.GetMenuItemString(e);
            EAN_AnimationComponent.ComponentType type;

            if (Enum.TryParse(animComponent, out type))
            {
                foreach(var bone in selectedBones)
                    bone.RemoveComponent(type, undos);

                SceneManager.InvokeAnimationDataChangedEvent();
            }

            if (undos.Count > 0)
                UndoManager.Instance.AddCompositeUndo(undos, "Remove Component", UndoGroup.Animation);
        }

        #endregion

        #region KeyframeCommands
        public RelayCommand CopyKeyframeCommand => new RelayCommand(CopyKeyframe, IsKeyframeSelected);
        private void CopyKeyframe()
        {
            List<int> keyframes = keyframeDataGrid.SelectedItems.Cast<int>().ToList();

            if (keyframes != null)
            {
                int startFrame = keyframes.Min(x => x);
                int endFrame = keyframes.Max(x => x);

                List<SerializedBone> serialziedBones = SelectedAnimation.SerializeKeyframeRange(startFrame, endFrame, GetSelectedBoneNames(), PositionComponentEnabled, RotationComponentEnabled, ScaleComponentEnabled);

                Clipboard.SetData(ClipboardConstants.EanAnimationKeyframe, serialziedBones);
            }
        }

        public RelayCommand PasteKeyframeCommand => new RelayCommand(PasteKeyframes, CanPasteKeyframes);
        private void PasteKeyframes()
        {
            List<SerializedBone> bones = (List<SerializedBone>)Clipboard.GetData(ClipboardConstants.EanAnimationKeyframe);

            if (bones != null && SelectedAnimation != null)
            {
                EanModiferForm form = new EanModiferForm("Paste Keyframes");
                form.StartFrame = SelectedFrame;
                form.SmoothFrameEnabled = true;
                form.InsertEnabled = true;
                form.RemoveCollisionsEnabled = true;
                form.RebaseKeyframesEnabled = true;
                form.ShowDialog();

                SerializedBone.StartFrameRebase(bones, form.Append ? SelectedAnimation.FrameCount + form.SmoothFrame : form.InsertFrame + form.SmoothFrame);

                List<IUndoRedo> undos = SelectedAnimation.AddKeyframes(bones, form.RemoveCollisions, form.RebaseKeyframes, PositionComponentEnabled, RotationComponentEnabled, ScaleComponentEnabled);
                UndoManager.Instance.AddCompositeUndo(undos, "Paste Keyframes", UndoGroup.Animation);
                SceneManager.InvokeAnimationDataChangedEvent();
            }
        }

        public RelayCommand PasteKeyframeValuesCommand => new RelayCommand(PasteKeyframesValues, CanPasteKeyframes);
        private void PasteKeyframesValues()
        {
            List<SerializedBone> bones = (List<SerializedBone>)Clipboard.GetData(ClipboardConstants.EanAnimationKeyframe);

            if (bones != null && SelectedAnimation != null)
            {
                foreach(var bone in bones)
                {
                    if (bone.Keyframes.Length > 1)
                    {
                        Log.Add($"Cannot paste keyframe values. There were too many keyframes found in the clipboard (expected just 1). ", LogType.Error);
                        return;
                    }
                }

                SerializedBone.StartFrameRebase(bones, SelectedFrame);

                List<IUndoRedo> undos = SelectedAnimation.AddKeyframes(bones, false, false, PositionComponentEnabled, RotationComponentEnabled, ScaleComponentEnabled);
                UndoManager.Instance.AddCompositeUndo(undos, "Paste Keyframe Values", UndoGroup.Animation);
                SceneManager.InvokeAnimationDataChangedEvent();
            }
        }

        public RelayCommand DeleteKeyframeCommand => new RelayCommand(DeleteKeyframe, IsKeyframeSelected);
        private void DeleteKeyframe()
        {
            List<int> keyframes = keyframeDataGrid.SelectedItems.Cast<int>().ToList();

            if (keyframes != null)
            {
                var selectedBones = SelectedBones;
                List<IUndoRedo> undos = new List<IUndoRedo>();

                if(selectedBones != null)
                {
                    foreach(var selectedBone in selectedBones)
                    {
                        undos.AddRange(selectedBone.DeleteKeyframes(keyframes, PositionComponentEnabled, RotationComponentEnabled, ScaleComponentEnabled));
                    }
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Delete Keyframes", UndoGroup.Animation);

                SceneManager.InvokeAnimationDataChangedEvent();
            }
        }

        public RelayCommand RebaseKeyframeCommand => new RelayCommand(RebaseKeyframe, IsKeyframeSelected);
        private void RebaseKeyframe()
        {
            List<int> keyframes = keyframeDataGrid.SelectedItems.Cast<int>().ToList();

            if (keyframes != null)
            {
                var selectedBones = SelectedBones;
                List<IUndoRedo> undos = new List<IUndoRedo>();

                if (selectedBones != null)
                {
                    EanModiferForm form = new EanModiferForm("Rebase Keyframes");
                    form.RebaseAmountEnabled = true;
                    form.RemoveCollisionsEnabled = true;
                    form.StartFrameConstraintEnabled = false;
                    form.ShowDialog();

                    if (form.Success)
                    {
                        foreach (var selectedBone in selectedBones)
                        {
                            selectedBone.RebaseKeyframes(keyframes, form.RebaseAmount, form.RemoveCollisions, PositionComponentEnabled, RotationComponentEnabled, ScaleComponentEnabled, undos);
                        }
                    }
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Rebase Keyframes", UndoGroup.Animation);

                SceneManager.InvokeAnimationDataChangedEvent();
            }
        }

        public RelayCommand RescaleKeyframeCommand => new RelayCommand(RescaleKeyframe, IsKeyframeSelected);
        private void RescaleKeyframe()
        {
            List<int> keyframes = keyframeDataGrid.SelectedItems.Cast<int>().ToList();

            if (keyframes != null)
            {
                var selectedBones = SelectedBones;
                List<IUndoRedo> undos = new List<IUndoRedo>();

                if (selectedBones != null)
                {
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
                        foreach (var selectedBone in selectedBones)
                        {
                            undos.AddRange(selectedBone.RescaleNode(form.NewDuration, form.StartFrame, form.EndFrame));
                        }
                    }
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Rescale Keyframes", UndoGroup.Animation);

                SceneManager.InvokeAnimationDataChangedEvent();
            }
        }

        public RelayCommand ShakeKeyframeCommand => new RelayCommand(ShakeKeyframe, CanShakeKeyframes);
        private void ShakeKeyframe()
        {
            List<int> keyframes = keyframeDataGrid.SelectedItems.Cast<int>().ToList();

            if (keyframes != null)
            {
                var selectedBones = SelectedBones;
                List<IUndoRedo> undos = new List<IUndoRedo>();

                if (selectedBones != null)
                {
                    EanModiferForm form = new EanModiferForm("Shake Keyframes");
                    form.StartFrame = keyframes.Min();
                    form.EndFrame = keyframes.Max();
                    form.ShakeFactorEnabled = true;
                    form.StartFrameEnabled = true;
                    form.EndFrameEnabled = true;
                    form.ShowDialog();

                    if (form.Success)
                    {
                        foreach (var selectedBone in selectedBones)
                        {
                            selectedBone.ApplyBoneShake(form.StartFrame, form.EndFrame, form.ShakeFactor, PositionComponentEnabled, RotationComponentEnabled, true, undos);
                        }
                    }
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Shake Keyframes", UndoGroup.Animation);

                SceneManager.InvokeAnimationDataChangedEvent();
            }
        }


        private bool CanShakeKeyframes()
        {
            if (!IsKeyframeSelected()) return false;
            return RotationComponentEnabled || PositionComponentEnabled;
        }

        private bool CanPasteKeyframes()
        {
            return Clipboard.ContainsData(ClipboardConstants.EanAnimationKeyframe);
        }

        private bool IsKeyframeSelected()
        {
            return (keyframeDataGrid.SelectedItem is int value && value >= 0 && IsBoneSelected());
        }
        #endregion

        #region Events
        private void Instance_UndoOrRedoCalled(object sender, UndoEventRaisedEventArgs e)
        {
            if(e.UndoGroup == UndoGroup.Animation)
                UpdateProperties();
        }

        private void SceneManager_AnimationDataChanged(object sender, EventArgs e)
        {
            UpdateProperties();
        }

        private void VisualSkeleton_SelectedBoneChanged(object sender, EventArgs e)
        {
            if (sender is int value && SceneManager.Actors[0] != null && SceneManager.IsOnTab(EditorTabs.Animation))
            {
                string boneName = SceneManager.Actors[0].Skeleton.Bones[value].Name;
                SelectedBoneChanged(boneName);
            }
        }

        private void SelectedBoneChanged(string boneName)
        {
            if (SelectedAnimation == null) return;
            if (string.IsNullOrWhiteSpace(boneName) && SelectedBone != null)
            {
                SelectedBone = null;
                return;
            }

            if (SelectedBone?.BoneName != boneName)
            {
                EAN_Node node = files.SelectedItem.SelectedAnimation.GetNode(boneName);
                SelectedBone = node;

                if (SelectedBone != null)
                    boneDataGrid.ScrollIntoView(SelectedBone);
            }

            //Update AnimatorGizmo
            if (files.SelectedItem?.SelectedEanFile?.File.Skeleton?.Exists(boneName) == true && Files.Instance.SelectedItem?.SelectedAnimation != null)
            {
                SceneManager.AnimatorGizmo.SetContext(SceneManager.Actors[0], boneName);
            }
            else
            {
                SceneManager.AnimatorGizmo.RemoveContext();
            }

            UpdateKeyframeValues();
        }

        private void SceneManager_UpdateEvent(object sender, EventArgs e)
        {
            //Update certain properties every frame, as they may change externally
            if (SceneManager.CurrentSceneState == EditorTabs.Animation)
            {
                NotifyPropertyChanged(nameof(SelectedFrame));

                if (CurrentFrame != PreviousFrame)
                    UpdateKeyframeValues();
            }
        }

        private void AnimListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (IsAnimationSelected())
            {
                PlaySelectedAnimation();
            }
        }

        private void AnimListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateKeyframeValues();
            if (IsAnimationSelected())
            {
                SceneManager.PlayAnimation(files.SelectedItem?.SelectedEanFile?.File, files.SelectedItem.SelectedAnimation.ID_UShort, 0, SceneManager.AutoPlay);
            }
        }

        private void KeyframeDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (keyframeDataGrid.SelectedItem is int value && value >= 0)
            {
                SelectedFrame = value;
            }

            UpdateKeyframeValues();
        }

        private void boneDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(CurrentKeyframes));
        }

        private void Bone_ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(nameof(AddBones));
            NotifyPropertyChanged(nameof(AddBoneComponent));
            NotifyPropertyChanged(nameof(RemoveBoneComponent));
        }

        private void SceneManager_PlayStateChanged(object sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(PosBrush));
            NotifyPropertyChanged(nameof(RotBrush));
            NotifyPropertyChanged(nameof(ScaleBrush));
        }

        private void UpdateProperties()
        {
            NotifyPropertyChanged(nameof(CurrentKeyframes));
            UpdateKeyframeValues();
            animListBox.Items.Refresh();
        }

        private void UpdateKeyframeValues()
        {
            NotifyPropertyChanged(nameof(KeyframeValuesEnabled));
            NotifyPropertyChanged(nameof(PosX));
            NotifyPropertyChanged(nameof(PosY));
            NotifyPropertyChanged(nameof(PosZ));
            NotifyPropertyChanged(nameof(PosW));
            NotifyPropertyChanged(nameof(RotX));
            NotifyPropertyChanged(nameof(RotY));
            NotifyPropertyChanged(nameof(RotZ));
            NotifyPropertyChanged(nameof(RotW));
            NotifyPropertyChanged(nameof(ScaleX));
            NotifyPropertyChanged(nameof(ScaleY));
            NotifyPropertyChanged(nameof(ScaleZ));
            NotifyPropertyChanged(nameof(ScaleW));
            NotifyPropertyChanged(nameof(PosBrush));
            NotifyPropertyChanged(nameof(RotBrush));
            NotifyPropertyChanged(nameof(ScaleBrush));
        }
        #endregion

        private List<string> GetSelectedBoneNames()
        {
            var selectedBones = SelectedBones;
            List<string> bones = new List<string>();

            if (selectedBones != null)
            {
                for (int i = 0; i < selectedBones.Count; i++)
                    bones.Add(selectedBones[i].BoneName);
            }

            return bones;
        }


        
    }
}
