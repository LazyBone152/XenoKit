using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.EAN;
using Xv2CoreLib.Resource.App;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Engine.View
{
    public class Camera : CameraBase
    {
        //Camera State:
        private CameraState BackupCameraState = null;

        //Animation properties
        private bool locked = false;
        public CameraAnimInstance cameraInstance = null;
        public float CurrentFrame
        {
            get
            {
                return (cameraInstance != null) ? cameraInstance.CurrentFrame : 0;
            }
            set
            {

                if (cameraInstance != null)
                    cameraInstance.CurrentFrame = value;
            }
        }
        
        public Camera(GameBase gameInstance) : base(gameInstance)
        {
            SceneManager.CameraDataChanged += SceneManager_CameraDataChanged;
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        public void Update(GameTime gameTime)
        {
            /*
            //Check if scene state allows animations and stop it if requried
            if(SceneManager.CurrentSceneState != SceneState.Camera && SceneManager.CurrentSceneState != SceneState.Action && cameraInstance != null)
            {
                ResetCamera();
                ClearCameraAnimation();
            }
            */
            locked = true;

            //Update camera animation only if playing
            if (cameraInstance != null && SceneManager.IsPlaying)
                UpdateCameraAnimation(SceneManager.IsPlaying);

            //Enable manual camera controls (if no anim is playing)
            if ((SceneManager.IsPlaying && cameraInstance == null) || !SceneManager.IsPlaying || !SceneManager.UseCameras)
            {
                BackupCameraState = null;
                ValidateCamera();
                ProcessCameraControl();
            }

            locked = false;
        }

        /// <summary>
        /// Simulate a frame. 
        /// </summary>
        public void Simulate(bool updateCamAnim, bool advance)
        {
#if DEBUG
            if (SceneManager.IsPlaying) throw new InvalidOperationException("DONT CALL SIMULATE WHEN PLAYING!");
#endif

            locked = true;
            if (cameraInstance != null)
            {
                if (updateCamAnim)
                    ProcessCameraFrame();

                if (advance)
                {
                    cameraInstance.CurrentFrame += 1;
                }
            }
            locked = false;
        }

        #region CamAnimation

        public void UpdateCameraAnimation(bool advance = true)
        {
            if (cameraInstance == null) return;

            //Handle camera ending
            if (CurrentFrame > cameraInstance.EndFrame)
            {
                if (cameraInstance.AutoTerminate)
                {
                    ClearCameraAnimation();
                    return;
                }
                else if (SceneManager.IsOnTab(EditorTabs.Camera) && SceneManager.Loop && SceneManager.IsPlaying)
                {
                    CurrentFrame = cameraInstance.StartFrame;
                }
                else if (SceneManager.IsOnTab(EditorTabs.Camera))
                {
                    SceneManager.Pause();
                }

                return;
            }

            //Dont update camera in this case, but do advance it
            if (!SceneManager.UseCameras && advance)
            {
                AdvanceFrame();
                return;
            }

            ProcessCameraFrame();

            if (advance)
            {
                AdvanceFrame();
            }
        }

        public void ProcessCameraFrame()
        {
            float _drawFrame = (CurrentFrame <= cameraInstance.Animation.FrameCount) ? CurrentFrame : cameraInstance.Animation.FrameCount - 1;

            //If current frame is greater than animation duration, just leave the last frame playing
            var pos = cameraInstance.Animation.GetNode("Node").GetComponent(EAN_AnimationComponent.ComponentType.Position);
            var targetPos = cameraInstance.Animation.GetNode("Node").GetComponent(EAN_AnimationComponent.ComponentType.Rotation);
            var camera = cameraInstance.Animation.GetNode("Node").GetComponent(EAN_AnimationComponent.ComponentType.Scale);

            CameraState.Position.X = (SceneManager.ResolveLeftHandSymetry) ? -pos.GetKeyframeValue(_drawFrame, Axis.X) : pos.GetKeyframeValue(_drawFrame, Axis.X);
            CameraState.Position.Y = pos.GetKeyframeValue(_drawFrame, Axis.Y);
            CameraState.Position.Z = pos.GetKeyframeValue(_drawFrame, Axis.Z);
            CameraState.TargetPosition.X = (SceneManager.ResolveLeftHandSymetry) ? -targetPos.GetKeyframeValue(_drawFrame, Axis.X) : targetPos.GetKeyframeValue(_drawFrame, Axis.X);
            CameraState.TargetPosition.Y = targetPos.GetKeyframeValue(_drawFrame, Axis.Y);
            CameraState.TargetPosition.Z = targetPos.GetKeyframeValue(_drawFrame, Axis.Z);

            if (camera != null && !cameraInstance.bacCameraSettings.Enabled)
            {
                CameraState.Roll = -MathHelper.ToDegrees(camera.GetKeyframeValue(_drawFrame, Axis.X));
                CameraState.FieldOfView = MathHelper.ToDegrees(camera.GetKeyframeValue(_drawFrame, Axis.Y));
            }
            else
            {
                CameraState.FieldOfView = EAN_File.DefaultFoV;
                CameraState.Roll = 0f;
            }

            //Bone Focus
            if (SceneManager.CharacterExists(cameraInstance.cameraTarget.CharacterIndex) && !SceneManager.IsOnTab(EditorTabs.Camera))
            {
                Vector3 bonePos = SceneManager.Actors[cameraInstance.cameraTarget.CharacterIndex].GetBoneCurrentAbsolutePosition(cameraInstance.cameraTarget.Bone);
                if (SceneManager.ResolveLeftHandSymetry)
                {
                    CameraState.Position += new Vector3(-bonePos.X, bonePos.Y, bonePos.Z);
                    CameraState.TargetPosition += new Vector3(-bonePos.X, bonePos.Y, bonePos.Z);
                }
                else
                {
                    CameraState.Position += bonePos;
                    CameraState.TargetPosition += bonePos;
                }
            }

            //Bac modifers
            if (cameraInstance.bacCameraSettings.Enabled)
            {
                Vector3 position = CameraState.ActualPosition;
                Vector3 targetPosition = CameraState.ActualTargetPosition;
                CameraState.ActualPosition += cameraInstance.bacCameraSettings.GetCurrentPosition(position, targetPosition, false);

                if (cameraInstance.bacCameraSettings.CurrentPosZ > 0f)
                    CameraState.ActualTargetPosition += cameraInstance.bacCameraSettings.GetCurrentPosition(position, targetPosition, true);
                else
                    CameraState.ActualTargetPosition += cameraInstance.bacCameraSettings.GetCurrentPosition(position, targetPosition, true);

                CameraState.Roll += cameraInstance.bacCameraSettings.GetCurrentRoll(CameraState.ActualPosition);
                CameraState.FieldOfView += cameraInstance.bacCameraSettings.GetCurrentFoV();
                CameraState.ActualPosition += cameraInstance.bacCameraSettings.GetCurrentRotation(CameraState.ActualPosition, CameraState.ActualTargetPosition);
            }

            //Scale animations to fit current actor size
            if (!cameraInstance.EanFile.IsCharaUnique && SceneManager.Actors[0] != null)
            {
                CameraState.Position.Y += SceneManager.Actors[0].CharacterData.BcsFile.File.F_48[1] - 1f;
                CameraState.TargetPosition.Y += SceneManager.Actors[0].CharacterData.BcsFile.File.F_48[1] - 1f;
            }

            ResetViewerAngles();
        }

        public void ClearCameraAnimation()
        {
            if (!locked)
            {
                cameraInstance = null;
                if (SceneManager.UseCameras)
                    RestoreCameraState(true);
            }
        }

        private void AdvanceFrame()
        {
            if (cameraInstance != null)
                cameraInstance.CurrentFrame += SceneManager.MainAnimTimeScale * SceneManager.BacTimeScale;
        }

        public void RestoreCameraState(bool removeBackup = true)
        {
            if (BackupCameraState != null)
            {
                CameraState.SetState(BackupCameraState);
                ResetViewerAngles();

                if (removeBackup)
                    BackupCameraState = null;
            }
        }
        #endregion


        #region PlaybackControl
        public void PlayCameraAnimation(EAN_File eanFile, EAN_Animation camAnim, BAC_Type10 bacCamEntry, int targetCharaIndex, bool autoTerminate, bool alwaysShowFirstFrame = true)
        {
            if (camAnim == null) return;

            if (SettingsManager.Instance.Settings.XenoKit_PreserveCameraState && BackupCameraState == null)
                BackupCameraState = CameraState.Copy();

            cameraInstance = new CameraAnimInstance(eanFile, camAnim, bacCamEntry, autoTerminate, targetCharaIndex);

            //Render first frame if not auto playing
            if (!SceneManager.IsPlaying && alwaysShowFirstFrame)
                UpdateCameraAnimation(false);
        }

        public void Resume()
        {
            if (cameraInstance?.CurrentFrame >= cameraInstance?.CurrentAnimDuration && SceneManager.IsOnTab(EditorTabs.Camera))
            {
                cameraInstance.CurrentFrame = 0;
            }
        }

        public void Stop()
        {
            if (cameraInstance != null)
            {
                CurrentFrame = cameraInstance.StartFrame;
            }
        }

        public void PrevFrame()
        {
            if (cameraInstance == null) return;
            if (CurrentFrame > cameraInstance.StartFrame)
            {
                CurrentFrame--;
            }
            else
            {
                CurrentFrame = cameraInstance.EndFrame;
            }

            UpdateCameraAnimation(false);
        }

        public void NextFrame()
        {
            if (cameraInstance == null) return;
            if (CurrentFrame < cameraInstance.EndFrame)
            {
                CurrentFrame++;
            }
            else
            {
                CurrentFrame = cameraInstance.StartFrame;
            }

            UpdateCameraAnimation(false);
        }

        public void SkipToFrame(int frame)
        {
            if (CurrentFrame != frame)
            {
                CurrentFrame = frame;
                UpdateCameraAnimation(false);
            }
        }

        #endregion

        #region Events
        private void Instance_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {
            if (e.UndoGroup == UndoGroup.Camera)
                cameraInstance?.UpdateValues();
        }

        private void SceneManager_CameraDataChanged(object sender, EventArgs e)
        {
            cameraInstance?.UpdateValues();
        }
        #endregion

    }

    [Serializable]
    public class CameraState
    {
        //Transforms
        public Vector3 ActualPosition
        {
            get
            {
                return (SceneManager.ResolveLeftHandSymetry) ? new Vector3(-Position.X, Position.Y, Position.Z) : Position;
            }
            set
            {
                Position = (SceneManager.ResolveLeftHandSymetry) ? new Vector3(-value.X, value.Y, value.Z) : value;
            }
        }
        public Vector3 ActualTargetPosition
        {
            get
            {
                return (SceneManager.ResolveLeftHandSymetry) ? new Vector3(-TargetPosition.X, TargetPosition.Y, TargetPosition.Z) : TargetPosition;
            }
            set
            {
                TargetPosition = (SceneManager.ResolveLeftHandSymetry) ? new Vector3(-value.X, value.Y, value.Z) : value;
            }
        }
        public Vector3 Position = new Vector3(0, 1f, -5);
        public Vector3 TargetPosition = new Vector3(0, 1f, 1f);
        private float _fieldOfView = EAN_File.DefaultFoV;
        public float FieldOfView
        {
            get
            {
                return _fieldOfView;
            }
            set
            {
                _fieldOfView = MathHelper.Clamp(value, 1, 180);
            }
        }

        public float Roll = 0f;

        public void Reset()
        {
            Position = new Vector3(0, 1f, -5);
            TargetPosition = new Vector3(0, 1f, 1f);
            _fieldOfView = EAN_File.DefaultFoV;
            Roll = 0f;
        }

        public CameraState Copy()
        {
            return new CameraState()
            {
                Position = Position,
                TargetPosition = TargetPosition,
                FieldOfView = FieldOfView,
                Roll = Roll
            };
        }

        public void SetState(CameraState state)
        {
            Position = state.Position;
            TargetPosition = state.TargetPosition;
            FieldOfView = state.FieldOfView;
            Roll = state.Roll;
        }

        public void SetFocus(Actor actor)
        {
            Reset();
            ActualPosition += actor.Transform.Translation;
            ActualTargetPosition += actor.Transform.Translation;
        }
    }

}
