﻿using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.IconPacks;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Engine;
using XenoKit.Inspector;
using Xv2CoreLib.Resource.App;

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

        public Game MonoGame;
        private int DelayedSeekFrame = -1;
        public PackIconMaterialLightKind PlayPauseButtonBinding
        {
            get
            {
                return SceneManager.IsPlaying == true ? PackIconMaterialLightKind.Pause : PackIconMaterialLightKind.Play;
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
                        return (SceneManager.Actors[0] != null) ? $"{SceneManager.Actors[0].AnimationPlayer.PrimaryCurrentFrame}/{SceneManager.Actors[0].AnimationPlayer.PrimaryDuration}" : "--/--";
                    case EditorTabs.Camera:
                        return (monoGame.camera.cameraInstance != null) ? $"{(int)MonoGame.camera.cameraInstance.CurrentFrame}/{MonoGame.camera.cameraInstance.CurrentAnimDuration}" : "--/--";
                    case EditorTabs.Action:
                        return (SceneManager.Actors[0] != null) ? $"{SceneManager.Actors[0].ActionControl.BacPlayer.CurrentFrame}/{SceneManager.Actors[0].ActionControl.BacPlayer.CurrentDuration}" : "--/--";
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
                        return (SceneManager.Actors[0] != null) ? (int)SceneManager.Actors[0].AnimationPlayer.PrimaryDuration : 0;
                    case EditorTabs.Camera:
                        return (monoGame.camera.cameraInstance != null) ? MonoGame.camera.cameraInstance.CurrentAnimDuration - 1 : 0;
                    case EditorTabs.Action:
                        return (SceneManager.Actors[0] != null) ? SceneManager.Actors[0].ActionControl.BacPlayer.CurrentDuration : 0;
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
                        return (SceneManager.Actors[0] != null) ? (int)SceneManager.Actors[0].AnimationPlayer.PrimaryCurrentFrame : 0;
                    case EditorTabs.Camera:
                        return (monoGame.camera.cameraInstance != null) ? (int)MonoGame.camera.cameraInstance.CurrentFrame : 0;
                    case EditorTabs.Action:
                            if(SceneManager.Actors[0] != null)
                            {
                                return DelayedSeekFrame != -1 ? DelayedSeekFrame : SceneManager.Actors[0].ActionControl.BacPlayer.CurrentFrame;
                            }
                        return 0;
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
                        if (SceneManager.Actors[0]?.AnimationPlayer?.PrimaryAnimation != null)
                            SceneManager.Actors[0].AnimationPlayer.PrimaryAnimation.CurrentFrame_Int = value;
                        break;
                    case EditorTabs.Camera:
                        if (monoGame.camera.cameraInstance != null)
                        {
                            monoGame.camera.cameraInstance.CurrentFrame = value;
                            SceneManager.UpdateCameraAnimation();
                        }
                        break;
                    case EditorTabs.Action:
                        DelayedSeekFrame = SceneManager.IsPlaying ? -1 : value;
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
                if (MonoGame?.camera == null) return null;
                return string.Format("CAMERA:\nFoV: {0}\nRoll: {1}\nPos: {2}\nTarget Pos: {3}\n\nCHARACTER:\nPosition: {4}\nBone: {5}\n\nPERFORMANCE:\nFPS (current/avg): {6} / {7}\nResolution (Viewport): {8}\nRender Resolution: {9}",
                    MonoGame.camera.CameraState.FieldOfView,
                    MonoGame.camera.CameraState.Roll,
                    MonoGame.camera.CameraState.Position,
                    MonoGame.camera.CameraState.TargetPosition,
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
                if (MonoGame?.camera == null || (!SceneManager.IsOnTab(EditorTabs.Action) && !SceneManager.IsOnEffectTab)) return null;
                return string.Format("\nVFX:\nParticles: {0}\nEmitters: {1}", MonoGame.RenderSystem.ActiveParticleCount, MonoGame.ObjectPoolManager.ParticleEmitterPool.UsedObjectCount);
            }
        }
        public string DebugOverlay
        {
            get
            {
                if (MonoGame?.camera == null) return null;
#if DEBUG
                return string.Format("\nDEBUG:\nCompiled Objects: {0}\nPooled Objects (Active): {1}\nPooled Objects (Free): {2}\nRender Objects: {3}\nDraw Calls: {4}",
                    MonoGame.CompiledObjectManager.ObjectCount,
                    MonoGame.ObjectPoolManager.ParticleEmitterPool.UsedObjectCount + MonoGame.ObjectPoolManager.ParticleNodeBasePool.UsedObjectCount + MonoGame.ObjectPoolManager.ParticlePlanePool.UsedObjectCount + MonoGame.ObjectPoolManager.ParticleMeshPool.UsedObjectCount,
                    MonoGame.ObjectPoolManager.ParticleEmitterPool.FreeObjectCount + MonoGame.ObjectPoolManager.ParticleNodeBasePool.FreeObjectCount + MonoGame.ObjectPoolManager.ParticlePlanePool.FreeObjectCount + MonoGame.ObjectPoolManager.ParticleMeshPool.FreeObjectCount,
                    MonoGame.RenderSystem.Count,
                    MonoGame.RenderSystem.MeshDrawCalls);
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
                return (SceneManager.MainGameInstance != null) ? SceneManager.MainGameInstance.RenderCharacters : true;
            }
            set
            {
                if (SceneManager.MainGameInstance != null)
                {
                    SceneManager.MainGameInstance.RenderCharacters = value;
                }
            }
        }

#endregion

        public GameView()
        {
            InitializeComponent();
            MonoGame = monoGame;
            DataContext = this;
            Game.GameUpdate += new EventHandler(UpdateUI);
            SettingsManager.SettingsReloaded += SettingsManager_SettingsReloaded;
            SceneManager.DelayedUpdate += DelayedUpdate;
            SceneManager.EditorTabChanged += SceneManager_EditorTabChanged;
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
            bacLoopCheckBox.Visibility = Visibility.Collapsed;
            cameraCheckBox.Visibility = Visibility.Collapsed;
            audioCheckBox.Visibility = Visibility.Collapsed;
            bonesCheckBox.Visibility = Visibility.Collapsed;
            hitboxCheckBox.Visibility = Visibility.Collapsed;
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

#region Commands
        public RelayCommand SeekNextCommand => new RelayCommand(SeekNextFrame, CanSeek);
        private void SeekNextFrame()
        {
            SceneManager.InvokeSeekOccurredEvent();
            MonoGame.NextFrame();
        }

        public RelayCommand SeekPrevCommand => new RelayCommand(SeekPrevFrame, CanSeek);
        private void SeekPrevFrame()
        {
            SceneManager.InvokeSeekOccurredEvent();
            MonoGame.PrevFrame();
        }

        private bool CanSeek()
        {
            //Can only seek in pause mode
            return SceneManager.MainGameBase?.IsPlaying == false;
        }
#endregion


        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (SceneManager.IsPlaying)
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
            MonoGame.camera.ResetCamera();
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
                boneName = SceneManager.CurrentSelectedBoneName;
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
    }
}
