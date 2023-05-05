using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.IconPacks;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Engine;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Controls
{
    /// <summary>
    /// Interaction logic for GameView.xaml
    /// </summary>
    public partial class GameView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public Game MonoGame;

        #region UI Properties
        public string CurrentFramePreview
        {
            get
            {
                switch (SceneManager.CurrentSceneState)
                {
                    case EditorTabs.Animation:
                        return (SceneManager.Actors[0] != null) ? $"{SceneManager.Actors[0].animationPlayer.PrimaryCurrentFrame}/{SceneManager.Actors[0].animationPlayer.PrimaryDuration}" : "--/--";
                    case EditorTabs.Camera:
                        return (monoGame.camera.cameraInstance != null) ? $"{(int)MonoGame.camera.cameraInstance.CurrentFrame}/{MonoGame.camera.cameraInstance.CurrentAnimDuration}" : "--/--";
                    case EditorTabs.Action:
                        return (SceneManager.Actors[0] != null) ? $"{SceneManager.Actors[0].bacPlayer.CurrentFrame}/{SceneManager.Actors[0].bacPlayer.CurrentDuration}" : "--/--";
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
                        return $"(TimeScale: {(SceneManager.BacTimeScale * SceneManager.MainAnimTimeScale).ToString("0.00####")})";
                }
                return "";
            }
        }
        public string GameOverlayText
        {
            get
            {
                if (MonoGame.camera == null) return null;
                return string.Format("CAMERA:\nFoV: {0}\nRoll: {1}\nPos: {2}\nTarget Pos: {3}\n\nCHARACTER:\nPosition: {4}\nBone: {5}", 
                    MonoGame.camera.FieldOfView, 
                    MonoGame.camera.Roll,
                    MonoGame.camera.ActualPosition, 
                    MonoGame.camera.ActualTargetPosition, 
                    (SceneManager.Actors[0] != null) ? SceneManager.Actors[0].Transform.Translation.ToString() : "No character loaded",
                    String.IsNullOrWhiteSpace(SceneManager.CurrentSelectedBoneName) ? "No bone selected" : SceneManager.CurrentSelectedBoneName);
            }
        }
        public int MaxFrameValue
        {
            get
            {
                switch (SceneManager.CurrentSceneState)
                {
                    case EditorTabs.Animation:
                        return (SceneManager.Actors[0] != null) ? (int)SceneManager.Actors[0].animationPlayer.PrimaryDuration : 0;
                    case EditorTabs.Camera:
                        return (monoGame.camera.cameraInstance != null) ? MonoGame.camera.cameraInstance.CurrentAnimDuration - 1 : 0;
                    case EditorTabs.Action:
                        return (SceneManager.Actors[0] != null) ? SceneManager.Actors[0].bacPlayer.CurrentDuration : 0;
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
                    case EditorTabs.Animation:
                        return (SceneManager.Actors[0] != null) ? (int)SceneManager.Actors[0].animationPlayer.PrimaryCurrentFrame : 0;
                    case EditorTabs.Camera:
                        return (monoGame.camera.cameraInstance != null) ? (int)MonoGame.camera.cameraInstance.CurrentFrame : 0;
                    case EditorTabs.Action:
                        return (SceneManager.Actors[0] != null) ? SceneManager.Actors[0].bacPlayer.CurrentFrame : 0;
                    default:
                        return 0;
                }
            }
            set
            {

                switch (SceneManager.CurrentSceneState)
                {
                    case EditorTabs.Animation:
                        if (SceneManager.Actors[0]?.animationPlayer?.PrimaryAnimation != null)
                            SceneManager.Actors[0].animationPlayer.PrimaryAnimation.CurrentFrame_Int = value;
                        break;
                    case EditorTabs.Camera:
                        if (monoGame.camera.cameraInstance != null)
                        {
                            monoGame.camera.cameraInstance.CurrentFrame = value;
                            SceneManager.UpdateCameraAnimation();
                        }
                        break;
                    case EditorTabs.Action:
                        //Set in DragComplete event so that Seek calls dont get spammed when dragging
                        break;
                }

                if (SceneManager.CurrentSceneState != EditorTabs.Action)
                    NotifyPropertyChanged(nameof(CurrentFrame));
            }
        }

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

        #endregion

        #region Buttons
        public PackIconMaterialLightKind PlayPauseButtonBinding
        {
            get
            {
                return SceneManager.IsPlaying ? PackIconMaterialLightKind.Pause : PackIconMaterialLightKind.Play;
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
        }

        private void SettingsManager_SettingsReloaded(object sender, EventArgs e)
        {
            UpdateSettings();
        }

        public void UpdateUI(object sender, EventArgs arg)
        {
            NotifyPropertyChanged(nameof(CurrentFramePreview));
            NotifyPropertyChanged(nameof(TimeScale));
            NotifyPropertyChanged(nameof(GameOverlayText));
            NotifyPropertyChanged(nameof(MaxFrameValue));
            NotifyPropertyChanged(nameof(CurrentFrame));
            NotifyPropertyChanged(nameof(PlayPauseButtonBinding));
        }

        public void UpdateSettings()
        {
            NotifyPropertyChanged(nameof(Loop));
            NotifyPropertyChanged(nameof(AutoPlay));
            NotifyPropertyChanged(nameof(UseCameras));
            NotifyPropertyChanged(nameof(ShowVisualSkeleton));
        }

        #region Commands
        public RelayCommand SeekNextCommand => new RelayCommand(SeekNextFrame, CanSeek);
        private void SeekNextFrame()
        {
            MonoGame.NextFrame();
        }

        public RelayCommand SeekPrevCommand => new RelayCommand(SeekPrevFrame, CanSeek);
        private void SeekPrevFrame()
        {
            MonoGame.PrevFrame();
        }

        private bool CanSeek()
        {
            //Can only seek in pause mode
            return !SceneManager.IsPlaying;
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
            if (gameOverlay.Visibility == Visibility.Visible)
                gameOverlay.Visibility = Visibility.Hidden;
            else
                gameOverlay.Visibility = Visibility.Visible;
        }
    }
}
