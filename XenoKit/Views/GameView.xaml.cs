using ControlzEx.Theming;
using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.IconPacks;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using XenoKit.Engine;
using XenoKit.Engine.Gizmo;
using XenoKit.Engine.Model;
using XenoKit.Engine.Scripting.BSA;
using XenoKit.Inspector;
using XenoKit.Properties;
using Xv2CoreLib.Resource.App;
using Application = System.Windows.Application;

namespace XenoKit.Controls
{
    /// <summary>
    /// Interaction logic for GameView.xaml
    /// </summary>
    public partial class GameView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Viewport MonoGame;
        private int DelayedSeekFrame = -1;
        public PackIconMaterialLightKind PlayPauseButtonBinding
        {
            get
            {
                return Viewport.Instance?.IsPlaying == true ? PackIconMaterialLightKind.Pause : PackIconMaterialLightKind.Play;
            }
        }

        #region UI Properties
        public string CurrentFramePreview
        {
            get
            {
                switch (SceneManager.CurrentSceneState)
                {
                    case EditorTabs.Inspector:
                    case EditorTabs.InspectorAnimation:
                        return (InspectorMode.Instance.ActiveSkinnedEntity != null) ? $"{InspectorMode.Instance.ActiveSkinnedEntity.AnimationPlayer.PrimaryCurrentFrame}/{InspectorMode.Instance.ActiveSkinnedEntity.AnimationPlayer.PrimaryDuration}" : "--/--";
                    case EditorTabs.Animation:
                    case EditorTabs.FPF:
                        return (SceneManager.Actors[0] != null) ? $"{SceneManager.Actors[0].AnimationPlayer.PrimaryCurrentFrame}/{SceneManager.Actors[0].AnimationPlayer.PrimaryDuration}" : "--/--";
                    case EditorTabs.Camera:
                        return (monoGame.Camera.cameraInstance != null) ? $"{(int)MonoGame.Camera.cameraInstance.CurrentFrame}/{MonoGame.Camera.cameraInstance.CurrentAnimDuration}" : "--/--";
                    case EditorTabs.Action:
                        return (SceneManager.Actors[0] != null) ? $"{SceneManager.Actors[0].ActionControl.BacPlayer.CurrentFrame}/{SceneManager.Actors[0].ActionControl.BacPlayer.CurrentDuration}" : "--/--";
                    case EditorTabs.Projectile:
                        return $"{BsaEffectPreviewController.Instance.CurrentFrame}/{BsaEffectPreviewController.Instance.Duration}";
                }
                return "--/--";
            }
        }
        public string TimeScale
        {
            get
            {
                switch (SceneManager.CurrentSceneState)
                {
                    case EditorTabs.Action:
                        if (SceneManager.Actors[0] == null) return "";
                        if (SceneManager.Actors[0].ActionControl.BacPlayer.IsScaled)
                        {
                            return $"(TimeScale: {SceneManager.Actors[0].ActiveTimeScale.ToString("0.00####")} -> {SceneManager.Actors[0].ActionControl.BacPlayer.ScaledDuration})";
                        }
                        else
                        {
                            return $"(TimeScale: {SceneManager.Actors[0].ActiveTimeScale.ToString("0.00####")})";
                        }
                }
                return "";
            }
        }
        public string StateExtraInfo
        {
            get
            {
                switch (SceneManager.CurrentSceneState)
                {
                    case EditorTabs.Action:
                        {
                            if (SceneManager.Actors[0] == null) return "";
                            switch (SceneManager.Actors[0].ActionControl.PreviewState)
                            {
                                case Engine.Scripting.BAC.ActionPreviewState.WaitingVictim:
                                    return "Waiting on victim...";
                                case Engine.Scripting.BAC.ActionPreviewState.WaitingProjectiles:
                                    return "Waiting on projectiles...";
                                default:
                                    return null;
                            }
                        }
                    default:
                        return null;
                }
            }
        }
        public int MaxFrameValue
        {
            get
            {
                switch (SceneManager.CurrentSceneState)
                {
                    case EditorTabs.Inspector:
                    case EditorTabs.InspectorAnimation:
                        return (InspectorMode.Instance.ActiveSkinnedEntity != null) ? (int)InspectorMode.Instance.ActiveSkinnedEntity.AnimationPlayer.PrimaryDuration : 0;
                    case EditorTabs.Animation:
                    case EditorTabs.FPF:
                        return (SceneManager.Actors[0] != null) ? (int)SceneManager.Actors[0].AnimationPlayer.PrimaryDuration : 0;
                    case EditorTabs.Camera:
                        return (monoGame.Camera.cameraInstance != null) ? MonoGame.Camera.cameraInstance.CurrentAnimDuration - 1 : 0;
                    case EditorTabs.Action:
                        return (SceneManager.Actors[0] != null) ? SceneManager.Actors[0].ActionControl.BacPlayer.CurrentDuration : 0;
                    case EditorTabs.Projectile:
                        return BsaEffectPreviewController.Instance.Duration;
                    default:
                        return 0;
                }
            }   
        }
        public int CurrentFrame
        {
            get
            {
                switch (SceneManager.CurrentSceneState)
                {
                    case EditorTabs.Inspector:
                    case EditorTabs.InspectorAnimation:
                        return (InspectorMode.Instance.ActiveSkinnedEntity != null) ? (int)InspectorMode.Instance.ActiveSkinnedEntity.AnimationPlayer.PrimaryCurrentFrame : 0;
                    case EditorTabs.Animation:
                    case EditorTabs.FPF:
                        return (SceneManager.Actors[0] != null) ? (int)SceneManager.Actors[0].AnimationPlayer.PrimaryCurrentFrame : 0;
                    case EditorTabs.Camera:
                        return (monoGame.Camera.cameraInstance != null) ? (int)MonoGame.Camera.cameraInstance.CurrentFrame : 0;
                    case EditorTabs.Action:
                            if(SceneManager.Actors[0] != null)
                            {
                                return DelayedSeekFrame != -1 ? DelayedSeekFrame : SceneManager.Actors[0].ActionControl.BacPlayer.CurrentFrame;
                            }
                        return 0;
                    case EditorTabs.Projectile:
                        return BsaEffectPreviewController.Instance.CurrentFrame;
                    default:
                        return 0;
                }
            }
            set
            {

                switch (SceneManager.CurrentSceneState)
                {
                    case EditorTabs.Inspector:
                    case EditorTabs.InspectorAnimation:
                        if (InspectorMode.Instance.ActiveSkinnedEntity?.AnimationPlayer?.PrimaryAnimation != null)
                            InspectorMode.Instance.ActiveSkinnedEntity.AnimationPlayer.PrimaryAnimation.CurrentFrame_Int = value;
                        break;
                    case EditorTabs.Animation:
                    case EditorTabs.FPF:
                        if (SceneManager.Actors[0]?.AnimationPlayer?.PrimaryAnimation != null)
                            SceneManager.Actors[0].AnimationPlayer.PrimaryAnimation.CurrentFrame_Int = value;
                        break;
                    case EditorTabs.Camera:
                        if (monoGame.Camera.cameraInstance != null)
                        {
                            monoGame.Camera.cameraInstance.CurrentFrame = value;
                            Viewport.Instance.ForceCameraUpdate();
                        }
                        break;
                    case EditorTabs.Action:
                        DelayedSeekFrame = Viewport.Instance?.IsPlaying == true ? -1 : value;
                        break;
                    case EditorTabs.Projectile:
                        BsaEffectPreviewController.Instance.Seek(value);
                        break;
                }

                //Consider moving to DragComplete
                SceneManager.InvokeSeekOccurredEvent();

                if (SceneManager.CurrentSceneState != EditorTabs.Action)
                    NotifyPropertyChanged(nameof(CurrentFrame));
            }
        }

        //Overlays
        public string StandardOverlay
        {
            get
            {
                if (MonoGame?.Camera == null) return null;
                return string.Format("CAMERA:\nFoV: {0}\nRoll: {1}\nPos: {2}\nTarget Pos: {3}\n\nCHARACTER:\nPosition: {4}\nBone: {5}\n\nPERFORMANCE:\nFPS (current/avg): {6} / {7}\nResolution (Viewport): {8}\nRender Resolution: {9}",
                    MonoGame.Camera.CameraState.FieldOfView,
                    MonoGame.Camera.CameraState.Roll,
                    MonoGame.Camera.CameraState.Position,
                    MonoGame.Camera.CameraState.TargetPosition,
                    (SceneManager.Actors[0] != null) ? SceneManager.Actors[0].Transform.Translation.ToString() : "No character loaded",
                    GetSelectedBoneName(),
                    MonoGame.FrameRate.CurrentFramesPerSecond.ToString("0.00"),
                    MonoGame.FrameRate.AverageFramesPerSecond.ToString("0.00"),
                    $"{MonoGame.GraphicsDevice.Viewport.Width}x{MonoGame.GraphicsDevice.Viewport.Height}",
                    $"{MonoGame.RenderSystem.RenderWidth}x{MonoGame.RenderSystem.RenderHeight},");
            }
        }
        public string VfxOverlay
        {
            get
            {
                if (MonoGame?.Camera == null || (!SceneManager.IsOnTab(EditorTabs.Action) && !SceneManager.IsOnEffectTab)) return null;
                return string.Format("\nVFX:\nParticles: {0}\nEmitters: {1}", MonoGame.RenderSystem.ActiveParticleCount, MonoGame.ObjectPoolManager.ParticleEmitterPool.UsedObjectCount);
            }
        }
        public string DebugOverlay
        {
            get
            {
                if (MonoGame?.Camera == null) return null;
#if DEBUG
                return string.Format("\nDEBUG:\nCompiled Objects: {0}\nPooled Objects (Active): {1}\nPooled Objects (Free): {2}\nRender Objects: {3}\nDraw Calls: {4}\nParticle Batcher: {5} (batches) / {6} (total batched)\nMouse Pos: {7}\nTest: {8}",
                    MonoGame.CompiledObjectManager.ObjectCount,
                    MonoGame.ObjectPoolManager.ParticleEmitterPool.UsedObjectCount + MonoGame.ObjectPoolManager.ParticleNodeBasePool.UsedObjectCount + MonoGame.ObjectPoolManager.ParticlePlanePool.UsedObjectCount + MonoGame.ObjectPoolManager.ParticleMeshPool.UsedObjectCount,
                    MonoGame.ObjectPoolManager.ParticleEmitterPool.FreeObjectCount + MonoGame.ObjectPoolManager.ParticleNodeBasePool.FreeObjectCount + MonoGame.ObjectPoolManager.ParticlePlanePool.FreeObjectCount + MonoGame.ObjectPoolManager.ParticleMeshPool.FreeObjectCount,
                    MonoGame.RenderSystem.Count,
                    MonoGame.RenderSystem.MeshDrawCalls,
                    MonoGame.RenderSystem.ParticleBatcher.NumBatches,
                    MonoGame.RenderSystem.ParticleBatcher.NumTotalBatched,
                    MonoGame.Input.MousePosition,
                    SceneManager.DebugTestValue);
#else
                return null;
#endif
            }
        }
        public Visibility OverlayVisibility { get; set; } = Visibility.Collapsed;

        public bool Loop
        {
            get
            {
                return SettingsManager.settings.XenoKit_Loop;
            }
            set
            {
                if(SettingsManager.settings.XenoKit_Loop != value)
                {
                    SettingsManager.settings.XenoKit_Loop = value;
                    SettingsManager.Instance.SaveSettings();
                }
            }
        }
        public bool BacLoop
        {
            get
            {
                return SceneManager.AllowBacLoop;
            }
            set
            {
                SceneManager.AllowBacLoop = value;
            }
        }
        public bool AutoPlay
        {
            get
            {
                return SettingsManager.settings.XenoKit_AutoPlay;
            }
            set
            {
                if(SettingsManager.settings.XenoKit_AutoPlay != value)
                {
                    SettingsManager.settings.XenoKit_AutoPlay = value;
                    SettingsManager.Instance.SaveSettings();
                }
            }
        }
        public bool UseCameras
        {
            get
            {
                return SettingsManager.settings.XenoKit_EnableCameraAnimations;
            }
            set
            {
                if(SettingsManager.settings.XenoKit_EnableCameraAnimations != value)
                {
                    SettingsManager.settings.XenoKit_EnableCameraAnimations = value;
                    SettingsManager.Instance.SaveSettings();
                }
            }
        }
        public bool ShowVisualSkeleton
        {
            get
            {
                return SettingsManager.settings.XenoKit_EnableVisualSkeleton;
            }
            set
            {
                if(SettingsManager.settings.XenoKit_EnableVisualSkeleton != value)
                {
                    SettingsManager.settings.XenoKit_EnableVisualSkeleton = value;
                    SettingsManager.Instance.SaveSettings();
                }
            }
        }
        public bool AudioSimulation
        {
            get
            {
                return SettingsManager.settings.XenoKit_AudioSimulation;
            }
            set
            {
                if (SettingsManager.settings.XenoKit_AudioSimulation != value)
                {
                    SettingsManager.settings.XenoKit_AudioSimulation = value;
                    SettingsManager.Instance.SaveSettings();
                }
            }
        }
        public bool HitboxSimulation
        {
            get
            {
                return SettingsManager.settings.XenoKit_HitboxSimulation;
            }
            set
            {
                if (SettingsManager.settings.XenoKit_HitboxSimulation != value)
                {
                    SettingsManager.settings.XenoKit_HitboxSimulation = value;
                    SettingsManager.Instance.SaveSettings();
                }
            }
        }
        public bool ProjectileSimulation
        {
            get
            {
                return SettingsManager.settings.XenoKit_ProjectileSimulation;
            }
            set
            {
                if (SettingsManager.settings.XenoKit_ProjectileSimulation != value)
                {
                    SettingsManager.settings.XenoKit_ProjectileSimulation = value;
                    SettingsManager.Instance.SaveSettings();
                }
            }
        }
        public bool VfxSimulation
        {
            get
            {
                return SettingsManager.settings.XenoKit_VfxSimulation;
            }
            set
            {
                if (SettingsManager.settings.XenoKit_VfxSimulation != value)
                {
                    SettingsManager.settings.XenoKit_VfxSimulation = value;
                    SettingsManager.Instance.SaveSettings();
                }
            }
        }
        public bool RenderCharacters
        {
            get
            {
                return (Viewport.Instance != null) ? Viewport.Instance.RenderCharacters : true;
            }
            set
            {
                if (Viewport.Instance != null)
                {
                    Viewport.Instance.RenderCharacters = value;
                }
            }
        }
        public int PivotPoint
        {
            get => (int)SceneManager.PivotPoint;
            set
            {
                SceneManager.PivotPoint = value != -1 ? (PivotPoint)value : Engine.Model.PivotPoint.Center;
                NotifyPropertyChanged(nameof(PivotPoint));
            }
        }
        public int ViewportSelectionMode
        {
            get => (int)SceneManager.ViewportSelectionMode;
            set
            {
                SceneManager.ViewportSelectionMode = value != -1 ? (ViewportSelectionMode)value : Engine.Model.ViewportSelectionMode.Model;
                NotifyPropertyChanged(nameof(ViewportSelectionMode));
            }
        }

        public bool TranslateAllowed => CurrentGizmo.AllowTranslation && CurrentGizmo.Current?.IsContextValid() == true;
        public bool RotateAllowed => CurrentGizmo.AllowRotation && CurrentGizmo.Current?.IsContextValid() == true;
        public bool ScaleAllowed => CurrentGizmo.AllowScale && CurrentGizmo.Current?.IsContextValid() == true;

        public Visibility ModelEditorVisibility => SceneManager.CurrentDynamicTab == DynamicTabs.ModelScene ? Visibility.Visible : Visibility.Collapsed;
        #endregion

        public GameView()
        {
            InitializeComponent();
            MonoGame = monoGame;
            DataContext = this;
            Viewport.UpdateEvent += UpdateUI;
            SettingsManager.SettingsReloaded += SettingsManager_SettingsReloaded;
            Viewport.DelayedEventUpdateEvent += DelayedUpdate;
            SceneManager.EditorTabChanged += SceneManager_EditorTabChanged;
            CurrentGizmo.CurrentGizmoChanged += CurrentGizmo_CurrentGizmoChanged;
            CurrentGizmo.CurrentGizmoModeChanged += CurrentGizmo_CurrentGizmoModeChanged;
            ThemeManager.Current.ThemeChanged += Metro_ThemeChanged;

            UpdateCheckedButtons();
        }

        private void CurrentGizmo_CurrentGizmoModeChanged(object sender, EventArgs e)
        {
            UpdateGizmoValues();
        }

        private void Metro_ThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            UpdateCheckedButtons();
        }

        private void SceneManager_EditorTabChanged(object sender, EventArgs e)
        {
            UpdateOptions();
        }

        private void DelayedUpdate(object sender, EventArgs e)
        {
            if(DelayedSeekFrame != -1 && SceneManager.Actors[0] != null)
            {
                switch (SceneManager.CurrentSceneState)
                {
                    case EditorTabs.Action:
                        SceneManager.Actors[0].ActionControl.BacPlayer.Seek(CurrentFrame);
                        break;
                }

                DelayedSeekFrame = -1;
            }
        }

        private void SettingsManager_SettingsReloaded(object sender, EventArgs e)
        {
            UpdateSettings();
        }

        public void UpdateUI(object sender, EventArgs arg)
        {
            NotifyPropertyChanged(nameof(CurrentFramePreview));
            NotifyPropertyChanged(nameof(TimeScale));
            NotifyPropertyChanged(nameof(StateExtraInfo));
            NotifyPropertyChanged(nameof(StandardOverlay));
            NotifyPropertyChanged(nameof(VfxOverlay));
            NotifyPropertyChanged(nameof(DebugOverlay));
            NotifyPropertyChanged(nameof(MaxFrameValue));
            NotifyPropertyChanged(nameof(CurrentFrame));
            NotifyPropertyChanged(nameof(PlayPauseButtonBinding));
        }

        private void UpdateOptions()
        {
            UpdateCheckedButtons();
            bacLoopCheckBox.Visibility = Visibility.Collapsed;
            cameraCheckBox.Visibility = Visibility.Collapsed;
            audioCheckBox.Visibility = Visibility.Collapsed;
            bonesCheckBox.Visibility = Visibility.Collapsed;
            hitboxCheckBox.Visibility = Visibility.Collapsed;
            projectileCheckBox.Visibility = Visibility.Collapsed;
            effectCheckBox.Visibility = Visibility.Collapsed;

            if (SceneManager.IsOnTab(EditorTabs.Action, EditorTabs.Camera))
            {
                cameraCheckBox.Visibility = Visibility.Visible;
            }

            if (SceneManager.IsOnTab(EditorTabs.Action, EditorTabs.Animation, EditorTabs.BCS_Bodies))
            {
                bonesCheckBox.Visibility = Visibility.Visible;
            }

            if (SceneManager.IsOnTab(EditorTabs.Action))
            {
                bacLoopCheckBox.Visibility = Visibility.Visible;
                audioCheckBox.Visibility = Visibility.Visible;
                hitboxCheckBox.Visibility = Visibility.Visible;
                projectileCheckBox.Visibility = Visibility.Visible;
            }

            if (SceneManager.IsOnTab(EditorTabs.Action, EditorTabs.Effect))
            {
                effectCheckBox.Visibility = Visibility.Visible;
            }
        }

        public void UpdateSettings()
        {
            NotifyPropertyChanged(nameof(Loop));
            NotifyPropertyChanged(nameof(AutoPlay));
            NotifyPropertyChanged(nameof(UseCameras));
            NotifyPropertyChanged(nameof(ShowVisualSkeleton));
            NotifyPropertyChanged(nameof(AudioSimulation));
            NotifyPropertyChanged(nameof(HitboxSimulation));
            NotifyPropertyChanged(nameof(ProjectileSimulation));
            NotifyPropertyChanged(nameof(VfxSimulation));
        }

        private void UpdateCheckedButtons()
        {
            UpdateGizmoValues();
            SetButtonColor(boundingBoxButton, SceneManager.ShowModelEditorHighlights);
            NotifyPropertyChanged(nameof(ModelEditorVisibility));
        }

#region Commands
        public RelayCommand SeekNextCommand => new RelayCommand(SeekNextFrame, CanSeek);
        private void SeekNextFrame()
        {
            SceneManager.InvokeSeekOccurredEvent();
            MonoGame.SeekNextFrame();
        }

        public RelayCommand SeekPrevCommand => new RelayCommand(SeekPrevFrame, CanSeek);
        private void SeekPrevFrame()
        {
            SceneManager.InvokeSeekOccurredEvent();
            MonoGame.SeekPrevFrame();
        }

        private bool CanSeek()
        {
            //Can only seek in pause mode
            return Viewport.Instance?.IsPlaying == false;
        }
        #endregion

        #region TransformGizmo
        public RelayCommand<int> TransformGizmoChangeCommand => new RelayCommand<int>(TransformGizmoChange);
        private void TransformGizmoChange(int parameter)
        {
            GizmoMode gizmoMode = (GizmoMode)parameter;
            SetGizmoMode(gizmoMode);
        }

        private void CurrentGizmo_CurrentGizmoChanged(object sender, EventArgs e)
        {
            UpdateGizmoValues();
        }

        private void SetGizmoMode(GizmoMode gizmoMode)
        {
            CurrentGizmo.SetGizmoMode(gizmoMode);
        }

        private void UpdateGizmoValues()
        {
            Brush defaultBrush = (Brush)Application.Current.FindResource("MahApps.Brushes.Menu.Background");
            Brush selectedBrush = (Brush)Application.Current.FindResource("MahApps.Brushes.Accent");

            translateButton.Background = defaultBrush;
            rotateButton.Background = defaultBrush;
            scaleButton.Background = defaultBrush;
            noneButton.Background = defaultBrush;

            NotifyPropertyChanged(nameof(TranslateAllowed));
            NotifyPropertyChanged(nameof(RotateAllowed));
            NotifyPropertyChanged(nameof(ScaleAllowed));

            if (CurrentGizmo.Current?.IsContextValid() == true)
            {
                switch (CurrentGizmo.CurrentGizmoMode)
                {
                    case GizmoMode.Translate:
                        translateButton.Background = selectedBrush;
                        break;
                    case GizmoMode.Rotate:
                        rotateButton.Background = selectedBrush;
                        break;
                    case GizmoMode.Scale:
                        scaleButton.Background = selectedBrush;
                        break;
                    case GizmoMode.None:
                        noneButton.Background = selectedBrush;
                        break;
                }
            }
            else
            {
                noneButton.Background = selectedBrush;
            }

        }
        #endregion

        private void SetButtonColor(Button button, bool isChecked)
        {
            button.Background = isChecked ? (Brush)Application.Current.FindResource("MahApps.Brushes.Accent") : (Brush)Application.Current.FindResource("MahApps.Brushes.Menu.Background");
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (Viewport.Instance?.IsPlaying == true)
            {
                SceneManager.Pause();
            }
            else
            {
                SceneManager.Play();
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            SceneManager.Stop();
        }

        private void ResetCamera_Click(object sender, RoutedEventArgs e)
        {
            MonoGame.Camera.ResetCamera();
        }

        private void ToggleGrid_Click(object sender, RoutedEventArgs e)
        {
            SceneManager.ShowWorldAxis = !SceneManager.ShowWorldAxis;
        }

        private void GameOverlayToggle_Click(object sender, RoutedEventArgs e)
        {
            if (OverlayVisibility == Visibility.Visible)
                OverlayVisibility = Visibility.Hidden;
            else
                OverlayVisibility = Visibility.Visible;

            NotifyPropertyChanged(nameof(OverlayVisibility));
        }

        private string GetSelectedBoneName()
        {
            string boneName = "N/A";

            if(SceneManager.CurrentSceneState == EditorTabs.Animation)
            {
                boneName = XenoKit.Engine.Gizmo.AnimatorGizmo.CurrentSelectedBoneName;
            }
            if(SceneManager.CurrentSceneState == EditorTabs.Action)
            {
                boneName = BacTab.SelectedIBacBone != null ? BacTab.SelectedIBacBone.BoneLink.ToString() : null;
            }
            if (SceneManager.CurrentSceneState == EditorTabs.BCS_Bodies)
            {
                boneName = Views.BcsBodyView.CurrentBoneScale != null ? Views.BcsBodyView.CurrentBoneScale.BoneName : null;
            }

            return string.IsNullOrWhiteSpace(boneName) ? "None selected" : boneName;
        }

        private void boundingBoxButton_Click(object sender, RoutedEventArgs e)
        {
            SceneManager.ShowModelEditorHighlights = !SceneManager.ShowModelEditorHighlights;
            UpdateCheckedButtons();
        }
    }
}
