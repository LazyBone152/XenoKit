using GalaSoft.MvvmLight.CommandWpf;
using LB_Common.Numbers;
using System;
using System.ComponentModel;
using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.Engine;
using Xv2CoreLib.EAN;
using Xv2CoreLib.EffectContainer;

namespace XenoKit.Views
{
    /// <summary>
    /// Interaction logic for SceneCamera.xaml
    /// </summary>
    public partial class Screenshot : UserControl, INotifyPropertyChanged
    {
        #region NotPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private float _roll, _fieldOfView = EAN_File.DefaultFoV;

        public LocalSettings LocalSettings => LocalSettings.Instance;

        public CustomVector4 CameraPos { get; set; } = new CustomVector4(0, 1f, -5, 1);
        public CustomVector4 CameraTargetPos { get; set; } = new CustomVector4(0, 1f, 1f, 1);
        public float Roll
        {
            get => _roll;
            set
            {
                if (_roll != value)
                {
                    _roll = value;
                    NotifyPropertyChanged(nameof(Roll));
                }
            }
        }
        public float FieldOfView
        {
            get => _fieldOfView;
            set
            {
                if( _fieldOfView != value)
                {
                    _fieldOfView = value;
                    NotifyPropertyChanged(nameof(FieldOfView));
                }
            }
        }
        public System.Windows.Media.Color BackgroundColor
        {
            get => System.Windows.Media.Color.FromScRgb(SceneManager.ScreenshotBackgroundColor.A / 255f, SceneManager.ScreenshotBackgroundColor.R / 255f, SceneManager.ScreenshotBackgroundColor.G / 255f, SceneManager.ScreenshotBackgroundColor.B / 255f);
            set
            {
                SceneManager.ScreenshotBackgroundColor = new Microsoft.Xna.Framework.Color(value.R, value.G, value.B, value.A);
            }
        }

        private bool cameraUpdateFromView = false;
        private int cameraUpdateFromValues = 0;

        public Screenshot()
        {
            InitializeComponent();
            CameraPos.PropertyChanged += CameraProperty_Changed;
            CameraTargetPos.PropertyChanged += CameraProperty_Changed;
            PropertyChanged += CameraProperty_Changed;

            SceneManager.DelayedUpdate += SceneManager_DelayedUpdate;
        }

        private void SceneManager_DelayedUpdate(object sender, EventArgs e)
        {
            if(cameraUpdateFromValues > 0)
            {
                cameraUpdateFromValues--;
                return;
            }

            cameraUpdateFromView = false;

            if(SceneManager.MainGameBase != null)
            {
                UpdateCameraValuesFromView();
            }
        }

        private void UpdateCameraValuesFromView()
        {
            if(SceneManager.MainGameBase.ActiveCameraBase.CameraState.Position.X != CameraPos.X ||
               SceneManager.MainGameBase.ActiveCameraBase.CameraState.Position.Y != CameraPos.Y ||
               SceneManager.MainGameBase.ActiveCameraBase.CameraState.Position.Z != CameraPos.Z ||
               SceneManager.MainGameBase.ActiveCameraBase.CameraState.TargetPosition.X != CameraTargetPos.X ||
               SceneManager.MainGameBase.ActiveCameraBase.CameraState.TargetPosition.Y != CameraTargetPos.Y ||
               SceneManager.MainGameBase.ActiveCameraBase.CameraState.TargetPosition.Z != CameraTargetPos.Z ||
               SceneManager.MainGameBase.ActiveCameraBase.CameraState.Roll != Roll ||
               SceneManager.MainGameBase.ActiveCameraBase.CameraState.FieldOfView != FieldOfView)
            {
                cameraUpdateFromView = true;

                CameraPos.X = SceneManager.MainGameBase.ActiveCameraBase.CameraState.Position.X;
                CameraPos.Y = SceneManager.MainGameBase.ActiveCameraBase.CameraState.Position.Y;
                CameraPos.Z = SceneManager.MainGameBase.ActiveCameraBase.CameraState.Position.Z;

                CameraTargetPos.X = SceneManager.MainGameBase.ActiveCameraBase.CameraState.TargetPosition.X;
                CameraTargetPos.Y = SceneManager.MainGameBase.ActiveCameraBase.CameraState.TargetPosition.Y;
                CameraTargetPos.Z = SceneManager.MainGameBase.ActiveCameraBase.CameraState.TargetPosition.Z;

                Roll = SceneManager.MainGameBase.ActiveCameraBase.CameraState.Roll;
                FieldOfView = SceneManager.MainGameBase.ActiveCameraBase.CameraState.FieldOfView;
            }
        }

        private void CameraProperty_Changed(object sender, PropertyChangedEventArgs e)
        {
            if(cameraUpdateFromView) return;

            UpdateCamera();
        }

        private void UpdateCamera()
        {
            if(SceneManager.MainGameBase != null)
            {
                SceneManager.MainGameBase.ActiveCameraBase.CameraState.SetState(CameraPos, CameraTargetPos, _roll, _fieldOfView);
                cameraUpdateFromValues = 10;
                Log.Add("Updating camera state");
            }
        }


        public RelayCommand<int> ApplyCameraPresetCommand => new RelayCommand<int>(ApplyCameraPreset);
        private void ApplyCameraPreset(int slot)
        {
            if (slot < 0 || slot >= LocalSettings.Instance.CameraStates.Length) return;
            if (LocalSettings.Instance.CameraStates[slot] == null)
            {
                Log.Add("Cannot apply camera state as none exists in this slot.");
                return;
            }

            SceneManager.MainGameBase.ActiveCameraBase.CameraState.SetState(LocalSettings.Instance.CameraStates[slot]);
        }

        public RelayCommand<int> SaveCameraPresetCommand => new RelayCommand<int>(SaveCameraPreset);
        private void SaveCameraPreset(int slot)
        {
            if (slot < 0 || slot >= LocalSettings.Instance.CameraStates.Length) return;
            LocalSettings.Instance.CameraStates[slot] = new SerializedCameraState(SceneManager.MainGameBase.ActiveCameraBase.CameraState);
        }
    }
}
