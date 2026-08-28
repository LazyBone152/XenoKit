using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector2 = System.Numerics.Vector2;
using SimdVector3 = System.Numerics.Vector3;
using SimdQuaternion = System.Numerics.Quaternion;
using Xv2CoreLib.Resource;
using XenoKit.Editor;

namespace XenoKit.Engine.View
{
    public class CameraBase : EngineObject
    {
        public Matrix4x4 ViewMatrix { get; private set; }
        public Matrix4x4 ProjectionMatrix { get; private set; }
        public Matrix4x4 ViewProjectionMatrix { get; private set; }
        public BoundingFrustum Frustum { get; protected set; } = new BoundingFrustum(Matrix.Identity);
        public virtual CameraState CameraState { get; protected set; } = new CameraState();

        private bool IsReflectionView = false;

        private bool leftClickDragStarted = false;
        private bool rightClickDragStarted = false;
        private SimdVector2 leftClickDragStartMousePos;
        private SimdVector2 rightClickDragStartMousePos;

        private SimdVector3 RotateCamera(SimdVector3 position, SimdVector3 target, SimdVector2 mouseDelta, float sensitivity)
        {
            float yaw = -mouseDelta.X * sensitivity;
            float pitch = -mouseDelta.Y * sensitivity;

            SimdQuaternion yawRotation = SimdQuaternion.CreateFromAxisAngle(MathHelpers.Up, MathHelper.ToRadians(yaw));

            SimdVector3 right = SimdVector3.Normalize(SimdVector3.Cross(position - target, MathHelpers.Up));
            SimdQuaternion pitchRotation = SimdQuaternion.CreateFromAxisAngle(right, MathHelper.ToRadians(pitch));

            SimdQuaternion finalRotation = yawRotation * pitchRotation;

            SimdVector3 direction = position - target;
            direction = SimdVector3.Transform(direction, finalRotation);

            return target + direction;
        }

        private void PanCamera(SimdVector2 mouseDelta, float speed)
        {
            SimdVector3 forward = -SimdVector3.Normalize(CameraState.TargetPosition - CameraState.Position);
            SimdVector3 right = SimdVector3.Normalize(SimdVector3.Cross(MathHelpers.Up, forward));
            SimdVector3 up = SimdVector3.Cross(forward, right);

            SimdVector3 moveRight = right * mouseDelta.X * speed;
            SimdVector3 moveUp = up * mouseDelta.Y * speed;
            SimdVector3 panMovement = moveRight + moveUp;

            CameraState.Position += panMovement;
            CameraState.TargetPosition += panMovement;
        }

        private void SpinCamera(SimdVector2 mouseDelta, float speed, bool inverseSpin)
        {
            if (inverseSpin)
            {
                CameraState.TargetPosition = RotateCamera(CameraState.TargetPosition, CameraState.Position, new SimdVector2(mouseDelta.X, -mouseDelta.Y), speed * 0.75f);
            }
            else
            {
                CameraState.Position = RotateCamera(CameraState.Position, CameraState.TargetPosition, mouseDelta, speed);
            }
        }

        private void ZoomCamera(float delta)
        {
            float distance = SimdVector3.Distance(CameraState.Position, CameraState.TargetPosition);
            float factor = 0.005f;

            factor *= 1f + (MathHelper.Clamp(distance / 200f, 0f, 1f) * 15f);

            if (distance < 25)
                factor *= 0.5f;
            if (distance > 100)
                factor *= 2;
            if (distance > 500)
                factor *= 2;
            if (distance > 1000)
                factor *= 2;

            SimdVector3 forward = CameraState.TargetPosition - CameraState.Position;
            forward = SimdVector3.Normalize(forward);
            SimdVector3 translation = forward * delta * factor;
            float distanceMoved = SimdVector3.Distance(translation + CameraState.Position, CameraState.Position);

            if (delta > 0f && distance - distanceMoved < 0.05f)
            {
                //Move target since it is too close and slow down translation
                translation /= 2.5f;
                CameraState.TargetPosition += translation;
            }

            CameraState.Position += translation;

        }

        private void TranslateCamera(SimdVector3 direction, float speed)
        {
            SimdVector3 forward = -SimdVector3.Normalize(CameraState.TargetPosition - CameraState.Position);
            SimdVector3 right = SimdVector3.Normalize(SimdVector3.Cross(MathHelpers.Up, forward));
            SimdVector3 up = SimdVector3.Cross(forward, right);

            SimdVector3 moveRight = right * direction.X * speed;
            SimdVector3 moveUp = up * direction.Y * speed;
            SimdVector3 moveForward = forward * direction.Z * speed;

            SimdVector3 translation = moveRight + moveUp + moveForward;

            CameraState.Position += translation;
            CameraState.TargetPosition += translation;
        }


        private void HandleTranslation()
        {
            if (!ViewportIsFocused) return;

            float translateSpeed = 0.2f * LocalSettings.Instance.CameraTranslateSpeed;

            if (Input.IsKeyDown(Keys.LeftControl))
                translateSpeed *= 0.25f;
            else if (Input.IsKeyDown(Keys.LeftShift))
                translateSpeed *= 5f;

            SimdVector3 translationVector = SimdVector3.Zero;

            if (Input.IsKeyDown(Keys.W))
            {
                translationVector += MathHelpers.Forward;
            }
            else if (Input.IsKeyDown(Keys.S))
            {
                translationVector += MathHelpers.Backward;
            }

            if (Input.IsKeyDown(Keys.A))
            {
                translationVector += MathHelpers.Right;
            }
            else if (Input.IsKeyDown(Keys.D))
            {
                translationVector += MathHelpers.Left;
            }

            if (Input.IsKeyDown(Keys.X))
            {
                translationVector += MathHelpers.Up;
            }
            else if (Input.IsKeyDown(Keys.C))
            {
                translationVector += MathHelpers.Down;
            }

            if(translationVector != SimdVector3.Zero)
                TranslateCamera(translationVector, translateSpeed);
        }

        private void HandlePanning()
        {
            if (!ViewportIsFocused)
            {
                Input.ClearDragEvent(MouseButtons.Right, this);
                return;
            }

            bool hasDragEvent = Input.HasDragEvent(MouseButtons.Right);

            if (hasDragEvent && Input.MouseState.RightButton == ButtonState.Released)
            {
                Input.ClearDragEvent(MouseButtons.Right);
            }
            else if (!hasDragEvent)
            {
                if (!rightClickDragStarted && Input.MouseState.RightButton == ButtonState.Pressed)
                {
                    rightClickDragStarted = true;
                    rightClickDragStartMousePos = Input.MousePosition;
                }
                else if (rightClickDragStarted && Input.MouseState.RightButton == ButtonState.Released)
                {
                    rightClickDragStarted = false;
                }
                else if (rightClickDragStarted && Input.CheckMousePositionForDrag(rightClickDragStartMousePos))
                {
                    rightClickDragStarted = false;
                    Input.RegisterDragEvent(MouseButtons.Right, this);
                }
            }

            if (Input.HasDragEvent(MouseButtons.Right, this))
            {
                float distance = SimdVector3.Distance(CameraState.Position, CameraState.TargetPosition);

                float factor = 0.005f * LocalSettings.Instance.CameraPanSpeed;
                if(distance > 10f)
                    factor *= 1f + (MathHelper.Clamp(distance / 200f, 0f, 1f) * 50f);

                if (Input.IsKeyDown(Keys.LeftControl))
                    factor *= 0.2f;

                //float factor = Input.IsKeyDown(Keys.LeftControl) ? 0.0005f : 0.002f;
                PanCamera(Input.MouseDelta, factor);
            }
        }

        private void HandleSpinning()
        {
            if (!ViewportIsFocused)
            {
                Input.ClearDragEvent(MouseButtons.Left, this);
                return;
            }

            bool hasDragEvent = Input.HasDragEvent(MouseButtons.Left);

            if (hasDragEvent && Input.MouseState.LeftButton == ButtonState.Released)
            {
                Input.ClearDragEvent(MouseButtons.Left);
            }
            else if(!hasDragEvent)
            {
                if (!leftClickDragStarted && Input.MouseState.LeftButton == ButtonState.Pressed)
                {
                    leftClickDragStarted = true;
                    leftClickDragStartMousePos = Input.MousePosition;
                }
                else if (leftClickDragStarted && Input.MouseState.LeftButton == ButtonState.Released)
                {
                    leftClickDragStarted = false;
                }
                else if(leftClickDragStarted && Input.CheckMousePositionForDrag(leftClickDragStartMousePos))
                {
                    leftClickDragStarted = false;
                    Input.RegisterDragEvent(MouseButtons.Left, this);
                }
            }

            if (Input.HasDragEvent(MouseButtons.Left, this))
            {
                float factor = Input.IsKeyDown(Keys.LeftControl) ? 0.05f : 0.2f;
                factor *= LocalSettings.Instance.CameraSpinSpeed;
                SpinCamera(-Input.MouseDelta, factor, Input.IsKeyDown(Keys.LeftAlt));
            }
        }

        private void HandleZooming()
        {
            if (!ViewportIsFocused || Input.MouseScrollThisFrame == 0) return;

            if (Input.IsKeyUp(Keys.LeftAlt))
            {
                //Move Position
                float factor = Input.IsKeyDown(Keys.LeftControl) ? 1 / 50f : 1f;
                ZoomCamera(Input.MouseScrollThisFrame * factor);
            }
            else if (Input.IsKeyDown(Keys.LeftAlt))
            {
                //Change FOV
                int factor = (Input.MouseScrollThisFrame) / 100;
                if (CameraState.FieldOfView - factor > 0 && CameraState.FieldOfView - factor < 180)
                    CameraState.FieldOfView -= factor;
            }
        }

        protected void ProcessCameraControl()
        {
            HandleTranslation();
            HandlePanning();
            HandleSpinning();
            HandleZooming();

            if ((ViewportIsFocused) && Input.IsKeyDown(Keys.E) && Input.IsKeyDown(Keys.LeftControl))
            {
                CameraState.Roll -= 0.1f;
                Input.ExclusiveKeyDown(Keys.E);
            }
            else if ((ViewportIsFocused) && Input.IsKeyDown(Keys.E))
            {
                CameraState.Roll--;
                Input.ExclusiveKeyDown(Keys.E);
            }
            if ((ViewportIsFocused) && Input.IsKeyDown(Keys.Q) && Input.IsKeyDown(Keys.LeftControl))
            {
                CameraState.Roll += 0.1f;
                Input.ExclusiveKeyDown(Keys.Q);
            }
            else if ((ViewportIsFocused) && Input.IsKeyDown(Keys.Q))
            {
                CameraState.Roll++;
                Input.ExclusiveKeyDown(Keys.Q);
            }
        }
        
        public void EarlyUpdate()
        {
            if (ViewportIsFocused && Input.IsKeyDown(Keys.R) && Input.IsKeyDown(Keys.LeftControl))
            {
                ResetCamera();
                Input.ExclusiveKeyDown(Keys.R);
            }
        }

        #region Helpers
        public float DistanceFromCamera(SimdVector3 worldPos)
        {
            return Math.Abs(SimdVector3.Distance(CameraState.Position, worldPos));
        }

        public SimdVector2 ProjectToScreenPosition(SimdVector3 worldPos)
        {
            var vec3 = GraphicsDevice.Viewport.Project(worldPos, ProjectionMatrix, ViewMatrix, Matrix.Identity);
            return new SimdVector2(vec3.X, vec3.Y);
        }

        /// <summary>
        /// Check for NaN on camera components and reset if required.
        /// </summary>
        public void ValidateCamera()
        {
            if (float.IsNaN(CameraState.Position.X) || float.IsNaN(CameraState.Position.Y) || float.IsNaN(CameraState.Position.Z) ||
                float.IsNaN(CameraState.TargetPosition.X) || float.IsNaN(CameraState.TargetPosition.Y) || float.IsNaN(CameraState.TargetPosition.Z))
            {
                ResetCamera();
            }
        }

        public SimdVector3 TransformRelativeToCamera(SimdVector3 position, float distanceModifier)
        {
            SimdVector3 cameraForward = position - CameraState.Position;
            cameraForward = SimdVector3.Normalize(cameraForward);
            return cameraForward * distanceModifier;
        }

        public virtual void ResetCamera()
        {
            Input.ClearDragEvent(MouseButtons.Right, this);
            Input.ClearDragEvent(MouseButtons.Left, this);
            CameraState.Reset();
        }

        public void SetReflectionView(bool reflectionEnabled)
        {
            IsReflectionView = reflectionEnabled;
            RecalculateMatrices();
        }

        public void RecalculateMatrices()
        {
            //Projection Matrix
            float fieldOfViewRadians = (float)(Math.PI / 180 * CameraState.FieldOfView);
            float nearClipPlane = ViewportInstance.CurrentStage.NearClip;
            float farClipPlane = ViewportInstance.CurrentStage.FarClip;
            float aspectRatio = RenderSystem.RenderWidth / (float)RenderSystem.RenderHeight;

            ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(fieldOfViewRadians, aspectRatio, nearClipPlane, farClipPlane);
            //ProjectionMatrix = EngineUtils.CreateInfinitePerspective(fieldOfViewRadians, aspectRatio, nearClipPlane);

            //View Matrix
            if (IsReflectionView)
            {
                //Dont think this is needed? Flipping the World matrix on the Y axis seems to do the trick
                SimdVector3 pos = new SimdVector3(CameraState.Position.X, -CameraState.Position.Y, CameraState.Position.Z);
                SimdVector3 target = new SimdVector3(CameraState.TargetPosition.X, -CameraState.TargetPosition.Y, CameraState.TargetPosition.Z);
                //ViewMatrix = Matrix.CreateLookAt(pos, target, Vector3.Down) * Matrix.CreateRotationZ(MathHelper.ToRadians(CameraState.Roll));

                ViewMatrix = Matrix4x4.CreateLookAt(pos, target, MathHelpers.Up) * Matrix4x4.CreateScale(1f, -1f, 1f) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(CameraState.Roll));
            }
            else
            {
                ViewMatrix = Matrix4x4.CreateLookAt(CameraState.Position, CameraState.TargetPosition, MathHelpers.Up) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(CameraState.Roll));
            }

            ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;

            if(SceneManager.FrustumUpdateEnabled)
                Frustum.Matrix = ViewProjectionMatrix;
        }

        public void LookAt(BoundingBox box)
        {
            float fieldOfViewRadians = (float)(Math.PI / 180 * CameraState.FieldOfView);
            float aspectRatio = RenderSystem.RenderWidth / (float)RenderSystem.RenderHeight;

            Vector3 boxCenter = (box.Min + box.Max) * 0.5f;
            Vector3 boxExtents = (box.Max - box.Min) * 0.5f;

            //Determine the forward direction
            Vector3 forward = Vector3.Normalize(boxCenter - CameraState.Position);
            if (forward.LengthSquared() < 1e-6f)
                forward = Vector3.Backward; // fallback if camera is at box center

            Vector3 right = Vector3.Normalize(Vector3.Cross(Vector3.Up, forward));
            Vector3 up = Vector3.Normalize(Vector3.Cross(forward, right));

            float extentX = Math.Abs(Vector3.Dot(right, boxExtents));
            float extentY = Math.Abs(Vector3.Dot(up, boxExtents));
            float tanFovY = (float)Math.Tan(fieldOfViewRadians * 0.5f);
            float tanFovX = tanFovY * aspectRatio;

            float requiredDistanceX = extentX / tanFovX;
            float requiredDistanceY = extentY / tanFovY;
            float requiredDistance = Math.Max(requiredDistanceX, requiredDistanceY);

            CameraState.TargetPosition = boxCenter.ToNumerics();
            CameraState.Position = (boxCenter - forward * requiredDistance).ToNumerics();
        }
        #endregion
    }
}
