using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.EAN;
using static XenoKit.Editor.ValuesDictionary.BAC;

namespace XenoKit.Engine.View
{
    public class Camera
    {

        GraphicsDevice graphicsDevice;
        Game gameInstance;
        MouseState currentMouseState = default(MouseState);
        KeyboardState currentKeyboardState = default(KeyboardState);

        //Constants / Default values
        private const float DefaultFieldOfView = 39.97836f;


        //Matrixes
        public Matrix ViewMatrix
        {
            get
            {
                return Matrix.CreateLookAt(Position, TargetPosition, Vector3.Up) * Matrix.CreateRotationZ(MathHelper.ToRadians(Roll));
            }
        }
        public Matrix ProjectionMatrix
        {
            get
            {
                float fieldOfViewRadians = (float)(Math.PI / 180 * FieldOfView);
                float nearClipPlane = 0.001f;
                float farClipPlane = 50000;
                float aspectRatio = graphicsDevice.Viewport.Width / (float)graphicsDevice.Viewport.Height;

                return Matrix.CreatePerspectiveFieldOfView(fieldOfViewRadians, aspectRatio, nearClipPlane, farClipPlane);
            }
        }

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
        private float _fieldOfView = DefaultFieldOfView;
        public float FieldOfView
        {
            get
            {
                return _fieldOfView;
            }
            set
            {
                if (value < 1) _fieldOfView = 1;
                else if (value > 180) _fieldOfView = 180;
                else _fieldOfView = value;
            }
        }
            
        public float Roll = 0f;


        //Animation properties
        private bool locked = false;
        public CameraInstance cameraInstance = null;
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
        

        #region CameraControlsValues
        //camera moves
        private bool enable_spinning = false;
        private bool enable_panning = false;
        public Vector3 viewer_angle = new Vector3((float)Math.PI, 0, 0);
        private float zoom = 1;

        //mouse
        private bool mouse_leftBt_hold = false;
        private bool mouse_rightBt_hold = false;
        private Point mouse_old_position = new Point(-1, -1);

        #endregion

        public Camera(GraphicsDevice graphicsDevice, Game gameInstance)
        {
            this.graphicsDevice = graphicsDevice;
            this.gameInstance = gameInstance;

            repositionCamera();
        }
        
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
        {
            currentMouseState = mouseState;
            currentKeyboardState = keyboardState;

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
            if((SceneManager.IsPlaying && cameraInstance == null) || !SceneManager.IsPlaying || !SceneManager.UseCameras)
                CameraControl();


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
                if(updateCamAnim)
                    UpdateCameraAnimation(false);

                if (advance)
                {
                    cameraInstance.CurrentFrame += 1;
                }
            }
            locked = false;
        }

        public void ResetCamera()
        {
            mouse_leftBt_hold = false;
            mouse_rightBt_hold = false;
            mouse_old_position = new Point(-1, -1);
            enable_spinning = false;
            enable_panning = false;
            viewer_angle = new Vector3((float)Math.PI, 0, 0);
            zoom = 1;
            Position = new Vector3(0, 1f, -5);
            TargetPosition = new Vector3(0, 1f, 1f);
            FieldOfView = DefaultFieldOfView;
            Roll = 0f;
            repositionCamera();
        }
        
        /// <summary>
        /// Check for NaN on camera components and reset if required.
        /// </summary>
        private void ValidateCamera()
        {
            if(float.IsNaN(Position.X) || float.IsNaN(Position.Y) || float.IsNaN(Position.Z) ||
                float.IsNaN(TargetPosition.X) || float.IsNaN(TargetPosition.Y) || float.IsNaN(TargetPosition.Z))
            {
                ResetCamera();
            }
        }

        #region CameraControlFunctions
        //Mouse Event
        private void mousePressEvent(String buttonName)
        {
            if (buttonName == "left")
                enable_spinning = true;
            if (buttonName == "right")
                enable_panning = true;
        }
        private void mouseMoveEvent(Point position, Point delta, int factor)
        {
            Vector2 delta_f = new Vector2(delta.X / (float)graphicsDevice.Viewport.Width, delta.Y / (float)graphicsDevice.Viewport.Height);

            if (enable_spinning)
                spinCamera(delta_f);

            if (enable_panning)
                panCamera(delta_f, factor);
        }
        private void mouseReleaseEvent(String buttonName)
        {
            if (buttonName == "left")
                enable_spinning = false;
            if (buttonName == "right")
                enable_panning = false;
        }
        private void wheelEvent(float delta)
        {
            zoomCamera(delta);
        }


        // classic xenoverse camera:
        private void spinCamera(Vector2 delta)
        {
            /*
            float two_pi = 2.0f * (float)Math.PI;
            float half_pi = (float)Math.PI / 2.0f;
            float angleX = delta.X * (-4.0f);
            float angleY = delta.Y * (-4.0f);
            
            if (angleX >= two_pi)
                angleX -= two_pi;
            if (angleY < 0)
                angleY += two_pi;
                */
            //Rotate X axis
            //Matrix orbitX = Matrix.CreateFromAxisAngle(Vector3.UnitY, angleX);
            //Position = Vector3.Transform(Position - TargetPosition, orbitX) + TargetPosition;

            //Rotate Y axis
            //Matrix orbitY = Matrix.CreateFromAxisAngle(Vector3.UnitX, angleY);
            //Position = Vector3.Transform(Position - TargetPosition, orbitY) + TargetPosition;

            
            //Quaternion orbitX = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angleX);
            //Quaternion orbitY = Quaternion.CreateFromAxisAngle(Vector3.UnitX, angleY);
            //Position = TargetPosition + Vector3.Transform(Position - TargetPosition, orbitX * orbitY);
            

            viewer_angle.X += delta.X * (-4.0f);
            viewer_angle.Y += delta.Y * (-4.0f);

            float two_pi = 2.0f * (float)Math.PI;
            float half_pi = (float)Math.PI / 2.0f;

            if (viewer_angle.X >= two_pi)
                viewer_angle.X -= two_pi;
            if (viewer_angle.X < 0)
                viewer_angle.X += two_pi;

            if (viewer_angle.Y >= half_pi - 0.1f)
                viewer_angle.Y = half_pi - 0.1f;
            if (viewer_angle.Y < -half_pi + 0.1f)
                viewer_angle.Y = -half_pi + 0.1f;

            repositionCamera();
        }
        private void panCamera(Vector2 delta, int factor)
        {
            Matrix viewMatrix = ViewMatrix;
            viewMatrix = Matrix.Invert(viewMatrix);

            Quaternion orientation = Quaternion.CreateFromRotationMatrix(viewMatrix);
            orientation.Normalize();

            TargetPosition += Vector3.Transform(new Vector3((delta.X * (zoom + 0.5f) * (-2.0f)), -(delta.Y * (zoom + 0.5f) * (-2.0f)), 0.0f), orientation) / factor;
            
            repositionCamera();
        }
        private void zoomCamera(float delta)
        {
            float distance = Vector3.Distance(Position, TargetPosition);
            zoom = distance / 3f;

            float factor = 0.001f;
            if (distance > 10.0) factor = 0.01f;
            if (distance > 100.0) factor = 0.1f;
            if (distance > 1000.0) factor = 0.1f;

            float newZoom = zoom - (delta * factor);
            if(newZoom > 0.0000001f)
                zoom = newZoom;

            repositionCamera();
        }
        private void repositionCamera()
        {
            Quaternion rotation_x = Quaternion.CreateFromAxisAngle(Vector3.UnitY, viewer_angle.X);
            Quaternion rotation_y = Quaternion.CreateFromAxisAngle(Vector3.UnitX, viewer_angle.Y);

            Position = TargetPosition + Vector3.Transform(new Vector3(0, 0, zoom * 3f), rotation_x * rotation_y);
        }


        private void CameraControl()
        {
            ValidateCamera();

            if ((gameInstance.IsActive) && currentKeyboardState.IsKeyDown(Keys.Q) && currentKeyboardState.IsKeyDown(Keys.LeftControl))
            {
                Roll -= 0.1f;
            }
            else if ((gameInstance.IsActive) && currentKeyboardState.IsKeyDown(Keys.Q))
            {
                Roll--;
            }
            if ((gameInstance.IsActive) && currentKeyboardState.IsKeyDown(Keys.E) && currentKeyboardState.IsKeyDown(Keys.LeftControl))
            {
                Roll += 0.1f;
            }
            else if ((gameInstance.IsActive) && currentKeyboardState.IsKeyDown(Keys.E))
            {
                Roll++;
            }

            if ((gameInstance.IsActive) && Input.MouseScrollThisFrame != 0 && currentKeyboardState.IsKeyDown(Keys.LeftAlt))
            {
                int factor = (Input.MouseScrollThisFrame) / 100;
                if (FieldOfView - factor > 0 && FieldOfView - factor < 180)
                    FieldOfView -= factor;
            }

            if ((gameInstance.IsActive) && (currentMouseState.LeftButton == ButtonState.Pressed) && (mouse_leftBt_hold == false) && Input.IsLeftClickHeldDown == false)
            {
                Input.IsLeftClickHeldDown = true;
                mouse_leftBt_hold = true;
                mousePressEvent("left");
            }
            if ((gameInstance.IsActive) && (currentMouseState.LeftButton != ButtonState.Pressed) && (mouse_leftBt_hold == true) && !(Input.IsLeftClickHeldDown && !mouse_leftBt_hold))
            {
                Input.IsLeftClickHeldDown = false;
                mouse_leftBt_hold = false;
                mouse_old_position = currentMouseState.Position;
                mouseReleaseEvent("left");
            }
            if ((gameInstance.IsActive) && (currentMouseState.RightButton == ButtonState.Pressed) && (mouse_rightBt_hold == false) && Input.IsRightClickHeldDown == false)
            {
                Input.IsRightClickHeldDown = true;
                mouse_rightBt_hold = true;
                mouse_old_position = currentMouseState.Position;
                mousePressEvent("right");
            }
            if ((gameInstance.IsActive) && (currentMouseState.RightButton != ButtonState.Pressed) && (mouse_rightBt_hold == true) && !(Input.IsRightClickHeldDown && !mouse_rightBt_hold))
            {
                Input.IsRightClickHeldDown = false;
                mouse_rightBt_hold = false;
                mouseReleaseEvent("right");
            }

            if ((gameInstance.IsActive) && (Input.MouseScrollThisFrame != 0) && currentKeyboardState.IsKeyUp(Keys.LeftAlt))
            {
                if (currentKeyboardState.IsKeyDown(Keys.LeftControl))
                {
                    //Move slowly while ctrl is held down
                    wheelEvent((Input.MouseScrollThisFrame / 50));
                }
                else
                {
                    wheelEvent(Input.MouseScrollThisFrame);
                }
            }

            Point pos_tmp = currentMouseState.Position;

            if (mouse_old_position.X == -1)                             //avoid moving on initialisation.
                mouse_old_position = pos_tmp;

            if ((gameInstance.IsActive) && ((pos_tmp.X != mouse_old_position.X) || (pos_tmp.Y != mouse_old_position.Y)))
            {
                if (currentKeyboardState.IsKeyDown(Keys.LeftControl))
                {
                    mouseMoveEvent(pos_tmp, pos_tmp - mouse_old_position, 10);
                    mouse_old_position = pos_tmp;
                }
                else
                {
                    mouseMoveEvent(pos_tmp, pos_tmp - mouse_old_position, 1);
                    mouse_old_position = pos_tmp;
                }
            }
        }

        public void ResetViewerAngles()
        {
            viewer_angle.Z = Roll * (float)Math.PI / 180.0f;

            Vector3 dir = Position - TargetPosition;
            zoom = (dir.Length()) / 3.0f;
            dir.Normalize();
            
            viewer_angle.X = 0;
            viewer_angle.Y = 0;

            //1) calcul yaw
            Vector2 vectproj = new Vector2(dir.X, -dir.Z);       //projection of the result on (O,x,-z) plane
            if (vectproj.Length() > 0.000001)           //if undefined => by defaut 0;
            {
                vectproj.Normalize();

                viewer_angle.X = (float)Math.Acos(vectproj.X);
                if (vectproj.Y < 0)
                    viewer_angle.X = -viewer_angle.X;
            }


            //2) calcul pitch
            Quaternion rotationInv_Yrot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -viewer_angle.X);

            Vector3 dir_tmp = Vector3.Transform(dir, rotationInv_Yrot);       //we cancel yaw rotation, the point must be into (O,x,y) plane
            viewer_angle.Y = (float)Math.Acos(dir_tmp.X);
            if (dir_tmp.Y > 0)
                viewer_angle.Y = -viewer_angle.Y;


            viewer_angle.X += (float)Math.PI / 2.0f;



            float two_pi = 2.0f * (float)Math.PI;
            float half_pi = (float)Math.PI / 2.0f;

            if (viewer_angle.X >= two_pi)
                viewer_angle.X -= two_pi;
            if (viewer_angle.X < 0)
                viewer_angle.X += two_pi;

            if (viewer_angle.Y >= half_pi - 0.1f)
                viewer_angle.Y = half_pi - 0.1f;
            if (viewer_angle.Y < -half_pi + 0.1f)
                viewer_angle.Y = -half_pi + 0.1f;
        }
        
        #endregion
        
        #region CamAnimation

        public void UpdateCameraAnimation(bool advance = true)
        {
            if (cameraInstance == null) return;

            //Handle camera ending
            if (CurrentFrame >= cameraInstance.EndFrame)
            {
                if (cameraInstance.AutoTerminate)
                {
                    ClearCameraAnimation();
                    return;
                }
                else if (SceneManager.IsOnTab(EditorTabs.Camera) && SceneManager.Loop)
                {
                    CurrentFrame = cameraInstance.StartFrame;
                }
                else if(SceneManager.IsOnTab(EditorTabs.Camera))
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

            float _drawFrame = (CurrentFrame <= cameraInstance.Animation.FrameCount) ? CurrentFrame : cameraInstance.Animation.FrameCount - 1;

            //If current frame is greater than animation duration, just leave the last frame playing
            var pos = cameraInstance.Animation.GetNode("Node").GetComponent(EAN_AnimationComponent.ComponentType.Position);
            var targetPos = cameraInstance.Animation.GetNode("Node").GetComponent(EAN_AnimationComponent.ComponentType.Rotation);
            var camera = cameraInstance.Animation.GetNode("Node").GetComponent(EAN_AnimationComponent.ComponentType.Scale);

            Position.X = (SceneManager.ResolveLeftHandSymetry) ? -pos.GetKeyframeValue(_drawFrame, Axis.X) : pos.GetKeyframeValue(_drawFrame, Axis.X);
            Position.Y = pos.GetKeyframeValue(_drawFrame, Axis.Y);
            Position.Z = pos.GetKeyframeValue(_drawFrame, Axis.Z);
            TargetPosition.X = (SceneManager.ResolveLeftHandSymetry) ? -targetPos.GetKeyframeValue(_drawFrame, Axis.X) : targetPos.GetKeyframeValue(_drawFrame, Axis.X);
            TargetPosition.Y = targetPos.GetKeyframeValue(_drawFrame, Axis.Y);
            TargetPosition.Z = targetPos.GetKeyframeValue(_drawFrame, Axis.Z);

            if (camera != null)
            {
                Roll = MathHelper.ToDegrees(camera.GetKeyframeValue(_drawFrame, Axis.X));
                FieldOfView = MathHelper.ToDegrees(camera.GetKeyframeValue(_drawFrame, Axis.Y));
            }
            else
            {
                FieldOfView = DefaultFieldOfView;
                Roll = 0f;
            }

            //Bone Focus
            if (SceneManager.CharacterExists(cameraInstance.cameraTarget.CharacterIndex))
            {
                Vector3 bonePos = SceneManager.Actors[cameraInstance.cameraTarget.CharacterIndex].GetBoneCurrentAbsolutePosition(cameraInstance.cameraTarget.Bone);
                if (SceneManager.ResolveLeftHandSymetry)
                {
                    Position += new Vector3(-bonePos.X, bonePos.Y, bonePos.Z);
                    TargetPosition += new Vector3(-bonePos.X, bonePos.Y, bonePos.Z);
                }
                else
                {
                    Position += bonePos;
                    TargetPosition += bonePos;
                }
            }
            
            //Redraw last frame if above condition fails. otherwise, bac cameras will break.

            //Bac modifers
            if (cameraInstance.bacCameraSettings.Enabled)
            {
                Vector3 position = ActualPosition;
                Vector3 targetPosition = ActualTargetPosition;
                ActualPosition += cameraInstance.bacCameraSettings.GetCurrentPosition(position, targetPosition, false);

                if(cameraInstance.bacCameraSettings.CurrentPosZ > 0f)
                    ActualTargetPosition += cameraInstance.bacCameraSettings.GetCurrentPosition(position, targetPosition, true);
                else
                    ActualTargetPosition += cameraInstance.bacCameraSettings.GetCurrentPosition(position, targetPosition, true);
                
                Roll += cameraInstance.bacCameraSettings.GetCurrentRoll();
                FieldOfView += cameraInstance.bacCameraSettings.GetCurrentFoV();
                ActualPosition += cameraInstance.bacCameraSettings.GetCurrentRotation(ActualPosition, ActualTargetPosition);
            }

            ResetViewerAngles();

            if (advance)
            {
                AdvanceFrame();
            }
        }
        
        public void ClearCameraAnimation()
        {
            if (!locked)
            {
                cameraInstance = null;
            }
        }

        private void AdvanceFrame()
        {
            if(cameraInstance != null)
                cameraInstance.CurrentFrame += SceneManager.MainAnimTimeScale * SceneManager.BacTimeScale;
        }
        #endregion

        #region PlaybackControl
        public void PlayCameraAnimation(EAN_Animation camAnim, BAC_Type10 bacCamEntry, int targetCharaIndex, bool autoTerminate, bool alwaysShowFirstFrame = true)
        {
            if (camAnim == null) return;

            cameraInstance = new CameraInstance(camAnim, bacCamEntry, autoTerminate, targetCharaIndex);

            //Render first frame if not auto playing
            if (!SceneManager.IsPlaying && alwaysShowFirstFrame)
                UpdateCameraAnimation(false);
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
            if(CurrentFrame != frame)
            {
                CurrentFrame = frame;
                UpdateCameraAnimation(false);
            }
        }

        #endregion
    }

    public class CameraInstance
    {
        public readonly bool AutoTerminate;
        private float _currentFrame = 0;
        public int StartFrame = 0;
        public int EndFrame = 0;
        public readonly EAN_Animation Animation;
        public readonly CameraTarget cameraTarget;
        public readonly BacCameraSettings bacCameraSettings;
        
        public float CurrentFrame
        {
            get
            {
                return _currentFrame;
            }
            set
            {
                _currentFrame = value;
                SceneManager.InvokeCameraCurrentFrameChangedEvent();
            }
        }
        public int CurrentAnimDuration
        {
            get
            {
                if (Animation != null)
                    return Animation.FrameCount - 1;
                return 0;
            }
        }
     
        public CameraInstance(EAN_Animation anim, BAC_Type10 bacCamEntry, bool autoTerminate, int targetCharacterIndex)
        {
            AutoTerminate = autoTerminate;
            Animation = anim;
            StartFrame = (bacCamEntry != null) ? bacCamEntry.StartFrame : 0;
            EndFrame = (bacCamEntry != null) ? bacCamEntry.StartFrame + bacCamEntry.Duration - 1 : anim.FrameCount - 1;
            _currentFrame = StartFrame;
            
            if(bacCamEntry != null)
            {
                bacCameraSettings = new BacCameraSettings(this, bacCamEntry);
                cameraTarget = new CameraTarget(targetCharacterIndex, bacCamEntry.BoneLink);
            }
            else
            {
                bacCameraSettings = new BacCameraSettings(this);
                cameraTarget = new CameraTarget(targetCharacterIndex, 0);
            }

        }
    }

    public struct CameraTarget
    {
        public readonly int CharacterIndex;
        public readonly string _bone;
        public string Bone
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_bone)) return "b_C_Base";
                return _bone;
            }
        }

        public CameraTarget(int charIndex, BoneLinks bone)
        {
            CharacterIndex = charIndex;
            if (!BoneNames.TryGetValue(bone, out _bone))
                _bone = "b_C_Base";
        }
    }

    public struct BacCameraSettings
    {
        private readonly CameraInstance ParentInstance;
        public bool Enabled;
        public ushort GlobalDuration;
        public ushort PosXDuration;
        public ushort PosYDuration;
        public ushort PosZDuration;
        public ushort RotXDuration;
        public ushort RotYDuration;
        public ushort RotZDuration;
        public ushort DispXZDuration;
        public ushort DispZYDuration;
        public ushort FovDuration;

        public float PosX;
        public float PosY;
        public float PosZ;
        public float RotX;
        public float RotY;
        public float RotZ; //Roll
        public float FoV;
        public float DispXZ; //Not implementing
        public float DispZY; //Not implementing

        //Interpolated Values
        public float CurrentFoV
        {
            get
            {
                return FoV * GetFactor(FovDuration);
            }
        }
        public float CurrentPosX
        {
            get
            {
                return PosX * GetFactor(PosXDuration);
            }
        }
        public float CurrentPosY
        {
            get
            {
                return PosY * GetFactor(PosYDuration);
            }
        }
        public float CurrentPosZ
        {
            get
            {
                return PosZ * GetFactor(PosZDuration);
            }
        }
        public float CurrentRotX
        {
            get
            {
                return RotX * GetFactor(RotXDuration);
            }
        }
        public float CurrentRotY
        {
            get
            {
                return RotY * GetFactor(RotYDuration);
            }
        }
        public float CurrentRotZ
        {
            get
            {
                return RotZ * GetFactor(RotZDuration);
            }
        }

        public BacCameraSettings(CameraInstance camera)
        {
            ParentInstance = camera;
            Enabled = false;
            PosX = 0;
            PosY = 0;
            PosZ = 0;
            RotX = 0;
            RotY = 0;
            RotZ = 0;
            DispXZ = 0;
            DispZY = 0;
            FoV = 0;
            GlobalDuration = 0;
            PosXDuration = 0;
            PosYDuration = 0;
            PosZDuration = 0;
            RotXDuration = 0;
            RotYDuration = 0;
            RotZDuration = 0;
            FovDuration = 0;
            DispXZDuration = 0;
            DispZYDuration = 0;
        }
        
        public BacCameraSettings(CameraInstance camera, BAC_Type10 bacCameraEntry)
        {
            ParentInstance = camera;
            Enabled = bacCameraEntry.I_74_7;
            PosX = bacCameraEntry.F_40;
            PosY = bacCameraEntry.F_44;
            PosZ = bacCameraEntry.F_20;
            RotX = bacCameraEntry.F_32;
            RotY = bacCameraEntry.F_36;
            RotZ = bacCameraEntry.F_52;
            DispXZ = bacCameraEntry.F_24;
            DispZY = bacCameraEntry.F_28;
            FoV = bacCameraEntry.F_48;
            GlobalDuration = bacCameraEntry.GlobalModiferDuration;
            PosXDuration = bacCameraEntry.I_66;
            PosYDuration = bacCameraEntry.I_68;
            PosZDuration = bacCameraEntry.I_56;
            RotXDuration = bacCameraEntry.I_62;
            RotYDuration = bacCameraEntry.I_64;
            RotZDuration = bacCameraEntry.I_72;
            FovDuration = bacCameraEntry.I_70;
            DispXZDuration = bacCameraEntry.I_58;
            DispZYDuration = bacCameraEntry.I_60;
        }

        public Vector3 GetCurrentPosition(Vector3 position, Vector3 targetPosition, bool isTargetPosition)
        {
            var forward = targetPosition - position;
            forward.Normalize();

            var posToMove = (isTargetPosition) ? new Vector3(CurrentPosX, CurrentPosY, 0) : new Vector3(CurrentPosX, CurrentPosY, CurrentPosZ);
            //var posToMove = (CurrentPosZ > 0f && !isTargetPosition) ? new Vector3(CurrentPosX, CurrentPosY, CurrentPosZ) : new Vector3(CurrentPosX, CurrentPosY, 0);
            posToMove = Vector3.Transform(posToMove, Matrix.CreateWorld(position, forward, Vector3.Up));
            return (posToMove - position) * CurrentGlobalFactor();
        }

        public float GetCurrentFoV()
        {
            return CurrentFoV * CurrentGlobalFactor();
        }

        public float GetCurrentRoll()
        {
            return CurrentRotZ * CurrentGlobalFactor();
        }

        public Vector3 GetCurrentRotation(Vector3 position, Vector3 targetPosition)
        {
            //Calculate the extra position needed to perform the rotation
            //Calculate the WHOLE thing first, then multiply it by CurrentFactor
            //This is how it is done in XV2. The result is multiplied, not the RotX,Y,Z values. (which results in the interpolation not actually rotating... just goes from point a to point b)

            Vector3 newPosition;

            //Rotate X
            Vector3 temp = position - targetPosition;
            temp = Vector3.Transform(temp, Matrix.CreateRotationY(MathHelper.ToRadians(CurrentRotX)));
            newPosition = targetPosition + temp;
            
            //Rotate Y
            temp = newPosition - targetPosition;
            temp = Vector3.Transform(temp, Matrix.CreateRotationX(MathHelper.ToRadians(CurrentRotY)));
            newPosition = targetPosition + temp;

            return (newPosition - position) * CurrentGlobalFactor();
        }

        private float CurrentGlobalFactor()
        {
            return GetFactor(GlobalDuration);
        }

        private float GetFactor(float duration)
        {
            if (ParentInstance.CurrentFrame - ParentInstance.StartFrame > duration || duration == 0) return 1f;
            return (ParentInstance.CurrentFrame - ParentInstance.StartFrame) / duration;
        }
    }
    

}
