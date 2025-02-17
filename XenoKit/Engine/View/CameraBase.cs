using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace XenoKit.Engine.View
{
    public class CameraBase : Entity, ICameraBase
    {
        public Matrix ViewMatrix { get; private set; }
        public Matrix ProjectionMatrix { get; private set; }
        public Matrix ViewProjectionMatrix { get; private set; }
        public BoundingFrustum Frustum { get; protected set; } = new BoundingFrustum(Matrix.Identity);
        public virtual CameraState CameraState { get; protected set; } = new CameraState();

        private bool IsReflectionView = false;
        private bool IsLeftClickHeldDown
        {
            get => Input.LeftClickHeldDownContext == this;
            set => Input.LeftClickHeldDownContext = value ? this : null;
        }
        private bool IsRightClickHeldDown
        {
            get => Input.RightClickHeldDownContext == this;
            set => Input.RightClickHeldDownContext = value ? this : null;
        }

        public CameraBase(GameBase game) : base(game) { }

        private Vector3 RotateCamera(Vector3 position, Vector3 target, Vector2 mouseDelta, float sensitivity)
        {
            float yaw = -mouseDelta.X * sensitivity;
            float pitch = -mouseDelta.Y * sensitivity;

            Quaternion yawRotation = Quaternion.CreateFromAxisAngle(Vector3.Up, MathHelper.ToRadians(yaw));

            Vector3 right = Vector3.Normalize(Vector3.Cross(position - target, Vector3.Up));
            Quaternion pitchRotation = Quaternion.CreateFromAxisAngle(right, MathHelper.ToRadians(pitch));

            Quaternion finalRotation = yawRotation * pitchRotation;

            Vector3 direction = position - target;
            direction = Vector3.Transform(direction, finalRotation);

            return target + direction;
        }

        private void PanCamera(Vector2 mouseDelta, float speed)
        {
            Vector3 forward = -Vector3.Normalize(CameraState.TargetPosition - CameraState.Position);
            Vector3 right = Vector3.Normalize(Vector3.Cross(Vector3.Up, forward));
            Vector3 up = Vector3.Cross(forward, right);

            Vector3 moveRight = right * mouseDelta.X * speed;
            Vector3 moveUp = up * mouseDelta.Y * speed;
            Vector3 panMovement = moveRight + moveUp;

            CameraState.Position += panMovement;
            CameraState.TargetPosition += panMovement;
        }

        private void SpinCamera(Vector2 mouseDelta, float speed, bool inverseSpin)
        {
            if (inverseSpin)
            {
                CameraState.TargetPosition = RotateCamera(CameraState.TargetPosition, CameraState.Position, new Vector2(mouseDelta.X, -mouseDelta.Y), speed * 0.75f);
            }
            else
            {
                CameraState.Position = RotateCamera(CameraState.Position, CameraState.TargetPosition, mouseDelta, speed);
            }
        }

        private void ZoomCamera(float delta)
        {
            float distance = Vector3.Distance(CameraState.Position, CameraState.TargetPosition);
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

            Vector3 forward = CameraState.TargetPosition - CameraState.Position;
            forward.Normalize();
            Vector3 translation = forward * delta * factor;
            float distanceMoved = Vector3.Distance(translation + CameraState.Position, CameraState.Position);

            if (delta > 0f && distance - distanceMoved < 0.05f)
            {
                //Move target since it is too close and slow down translation
                translation /= 2.5f;
                CameraState.TargetPosition += translation;
            }

            CameraState.Position += translation;

        }

        private void TranslateCamera(Vector3 direction, float speed)
        {
            Vector3 forward = -Vector3.Normalize(CameraState.TargetPosition - CameraState.Position);
            Vector3 right = Vector3.Normalize(Vector3.Cross(Vector3.Up, forward));
            Vector3 up = Vector3.Cross(forward, right);

            Vector3 moveRight = right * direction.X * speed;
            Vector3 moveUp = up * direction.Y * speed;
            Vector3 moveForward = forward * direction.Z * speed;

            Vector3 translation = moveRight + moveUp + moveForward;

            CameraState.Position += translation;
            CameraState.TargetPosition += translation;
        }


        private void HandleTranslation()
        {
            if (!GameIsFocused) return;

            float translateSpeed = 0.2f;

            if (Input.IsKeyDown(Keys.LeftControl))
                translateSpeed *= 0.25f;
            else if (Input.IsKeyDown(Keys.LeftShift))
                translateSpeed *= 5f;

            Vector3 translationVector = Vector3.Zero;

            if (Input.IsKeyDown(Keys.W))
            {
                translationVector += Vector3.Forward;
            }
            else if (Input.IsKeyDown(Keys.S))
            {
                translationVector += Vector3.Backward;
            }

            if (Input.IsKeyDown(Keys.A))
            {
                translationVector += Vector3.Right;
            }
            else if (Input.IsKeyDown(Keys.D))
            {
                translationVector += Vector3.Left;
            }

            if (Input.IsKeyDown(Keys.X))
            {
                translationVector += Vector3.Up;
            }
            else if (Input.IsKeyDown(Keys.C))
            {
                translationVector += Vector3.Down;
            }

            if(translationVector != Vector3.Zero)
                TranslateCamera(translationVector, translateSpeed);
        }

        private void HandlePanning()
        {
            if (!GameIsFocused)
            {
                IsRightClickHeldDown = false;
                return;
            }

            if (Input.RightClickHeldDownContext == null && Input.MouseState.RightButton == ButtonState.Pressed)
            {
                IsRightClickHeldDown = true;
            }
            else if(IsRightClickHeldDown && Input.MouseState.RightButton == ButtonState.Released)
            {
                IsRightClickHeldDown = false;
            }

            if (IsRightClickHeldDown)
            {
                float distance = Vector3.Distance(CameraState.Position, CameraState.TargetPosition);

                float factor = 0.002f;
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
            if (!GameIsFocused)
            {
                IsLeftClickHeldDown = false;
                return;
            }

            if (Input.LeftClickHeldDownContext == null && Input.MouseState.LeftButton == ButtonState.Pressed)
            {
                IsLeftClickHeldDown = true;
            }
            else if (IsLeftClickHeldDown && Input.MouseState.LeftButton == ButtonState.Released)
            {
                IsLeftClickHeldDown = false;
            }

            if (IsLeftClickHeldDown)
            {
                float factor = Input.IsKeyDown(Keys.LeftControl) ? 0.05f : 0.2f;
                SpinCamera(-Input.MouseDelta, factor, Input.IsKeyDown(Keys.LeftAlt));
            }
        }

        private void HandleZooming()
        {
            if (!GameIsFocused || Input.MouseScrollThisFrame == 0) return;

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

            if (GameIsFocused && Input.IsKeyDown(Keys.R) && Input.IsKeyDown(Keys.LeftControl))
            {
                ResetCamera();
            }

            if ((GameIsFocused) && Input.IsKeyDown(Keys.E) && Input.IsKeyDown(Keys.LeftControl))
            {
                CameraState.Roll -= 0.1f;
            }
            else if ((GameIsFocused) && Input.IsKeyDown(Keys.E))
            {
                CameraState.Roll--;
            }
            if ((GameIsFocused) && Input.IsKeyDown(Keys.Q) && Input.IsKeyDown(Keys.LeftControl))
            {
                CameraState.Roll += 0.1f;
            }
            else if ((GameIsFocused) && Input.IsKeyDown(Keys.Q))
            {
                CameraState.Roll++;
            }
        }
        
        #region Helpers
        public float DistanceFromCamera(Vector3 worldPos)
        {
            return Math.Abs(Vector3.Distance(CameraState.Position, worldPos));
        }

        public Vector2 ProjectToScreenPosition(Vector3 worldPos)
        {
            var vec3 = GraphicsDevice.Viewport.Project(worldPos, ProjectionMatrix, ViewMatrix, Matrix.Identity);
            return new Vector2(vec3.X, vec3.Y);
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

        public Vector3 TransformRelativeToCamera(Vector3 position, float distanceModifier)
        {
            Vector3 cameraForward = position - CameraState.Position;
            cameraForward.Normalize();
            return cameraForward * distanceModifier;
        }

        public virtual void ResetCamera()
        {
            IsLeftClickHeldDown = false;
            IsRightClickHeldDown = false;
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
            float nearClipPlane = 0.1f;
            float farClipPlane = 5000;
            float aspectRatio = GraphicsDevice.Viewport.Width / (float)GraphicsDevice.Viewport.Height;

            ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(fieldOfViewRadians, aspectRatio, nearClipPlane, farClipPlane);
            //ProjectionMatrix = EngineUtils.CreateInfinitePerspective(fieldOfViewRadians, aspectRatio, nearClipPlane);

            //View Matrix
            if (IsReflectionView)
            {
                //Dont think this is needed? Flipping the World matrix on the Y axis seems to do the trick
                Vector3 pos = new Vector3(CameraState.Position.X, -CameraState.Position.Y, CameraState.Position.Z);
                Vector3 target = new Vector3(CameraState.TargetPosition.X, -CameraState.TargetPosition.Y, CameraState.TargetPosition.Z);
                //ViewMatrix = Matrix.CreateLookAt(pos, target, Vector3.Down) * Matrix.CreateRotationZ(MathHelper.ToRadians(CameraState.Roll));

                ViewMatrix = Matrix.CreateLookAt(pos, target, Vector3.Up) * Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateRotationZ(MathHelper.ToRadians(CameraState.Roll));
            }
            else
            {
                ViewMatrix = Matrix.CreateLookAt(CameraState.Position, CameraState.TargetPosition, Vector3.Up) * Matrix.CreateRotationZ(MathHelper.ToRadians(CameraState.Roll));
            }

            ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;
            Frustum.Matrix = ViewProjectionMatrix;
        }

        #endregion
    }
}
