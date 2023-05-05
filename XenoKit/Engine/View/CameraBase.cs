using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using System;
using Xv2CoreLib.Resource;

namespace XenoKit.Engine.View
{
    /// <summary>
    /// Base Camera class that provides manual control.
    /// </summary>
    public class CameraBase : Entity, ICameraBase
    {
        //Based on the Xenoviewer camera code, ported here by Olganix
        
        public virtual Matrix TestViewMatrix
        {
            get
            {
                return Matrix.CreateLookAt(CameraState.ActualPosition, CameraState.ActualTargetPosition, Vector3.Up) * Matrix.CreateRotationZ(MathHelper.ToRadians(CameraState.Roll));
            }
        }
        public virtual Matrix ViewMatrix
        {
            get
            {
                return Matrix.CreateLookAt(CameraState.Position, CameraState.TargetPosition, Vector3.Up) * Matrix.CreateRotationZ(MathHelper.ToRadians(CameraState.Roll));
            }
        }
        public virtual Matrix ProjectionMatrix
        {
            get
            {
                float fieldOfViewRadians = (float)(Math.PI / 180 * CameraState.FieldOfView);
                //float nearClipPlane = 0.01f;
                //float farClipPlane = 5000;
                float nearClipPlane = 0.1f;
                float farClipPlane = 15000;
                float aspectRatio = GraphicsDevice.Viewport.Width / (float)GraphicsDevice.Viewport.Height;

                return Matrix.CreatePerspectiveFieldOfView(fieldOfViewRadians, aspectRatio, nearClipPlane, farClipPlane);
            }
        }
        public virtual CameraState CameraState { get; protected set; } = new CameraState();

        //camera moves
        private bool enable_spinning = false;
        private bool enable_panning = false;
        public Vector3 viewer_angle = new Vector3((float)Math.PI, 0, 0);
        private float zoom = 1;

        //mouse
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
        private Point mouse_old_position = new Point(-1, -1);

        public CameraBase(GameBase gameBase) : base(gameBase)
        {
            repositionCamera();
        }

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
            Vector2 delta_f = new Vector2(delta.X / (float)GraphicsDevice.Viewport.Width, delta.Y / (float)GraphicsDevice.Viewport.Height);

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
            viewer_angle.X += delta.X * (-4.0f);
            viewer_angle.Y += delta.Y * (-4.0f);

            const float two_pi = 2.0f * (float)Math.PI;
            const float half_pi = (float)Math.PI / 2.0f;

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

            CameraState.TargetPosition += Vector3.Transform(new Vector3((delta.X * (zoom + 0.5f) * (-2.0f)), -(delta.Y * (zoom + 0.5f) * (-2.0f)), 0.0f), orientation) / factor;

            repositionCamera();
        }
        private void zoomCamera(float delta)
        {
            float distance = Vector3.Distance(CameraState.Position, CameraState.TargetPosition);
            zoom = distance / 3f;

            float factor = 0.001f;
            if (distance > 10.0) factor = 0.01f;
            if (distance > 100.0) factor = 0.1f;
            if (distance > 1000.0) factor = 0.1f;

            float newZoom = zoom - (delta * factor);
            if (newZoom > 0.0000001f)
                zoom = newZoom;

            repositionCamera();
        }
        private void repositionCamera()
        {
            Quaternion rotation_x = Quaternion.CreateFromAxisAngle(Vector3.UnitY, viewer_angle.X);
            Quaternion rotation_y = Quaternion.CreateFromAxisAngle(Vector3.UnitX, viewer_angle.Y);

            CameraState.Position = CameraState.TargetPosition + Vector3.Transform(new Vector3(0, 0, zoom * 3f), rotation_x * rotation_y);
        }

        //Control Loop
        protected void ProcessCameraControl()
        {
            if(GameIsFocused && Input.IsKeyDown(Keys.W))
            {
                Translate(-1f, 0f, Input.IsKeyDown(Keys.LeftControl), Input.IsKeyDown(Keys.LeftShift));
            }
            else if (GameIsFocused && Input.IsKeyDown(Keys.S))
            {
                Translate(1f, 0f, Input.IsKeyDown(Keys.LeftControl), Input.IsKeyDown(Keys.LeftShift));
            }

            if (GameIsFocused && Input.IsKeyDown(Keys.A))
            {
                Translate(0f, 1f, Input.IsKeyDown(Keys.LeftControl), Input.IsKeyDown(Keys.LeftShift));
            }
            else if (GameIsFocused && Input.IsKeyDown(Keys.D))
            {
                Translate(0f, -1f, Input.IsKeyDown(Keys.LeftControl), Input.IsKeyDown(Keys.LeftShift));
            }

            if (GameIsFocused && Input.IsKeyDown(Keys.R) && Input.IsKeyDown(Keys.LeftControl))
            {
                ResetCamera();
            }

            if ((GameIsFocused) && Input.IsKeyDown(Keys.Q) && Input.IsKeyDown(Keys.LeftControl))
            {
                CameraState.Roll -= 0.1f;
            }
            else if ((GameIsFocused) && Input.IsKeyDown(Keys.Q))
            {
                CameraState.Roll--;
            }
            if ((GameIsFocused) && Input.IsKeyDown(Keys.E) && Input.IsKeyDown(Keys.LeftControl))
            {
                CameraState.Roll += 0.1f;
            }
            else if ((GameIsFocused) && Input.IsKeyDown(Keys.E))
            {
                CameraState.Roll++;
            }

            if ((GameIsFocused) && Input.MouseScrollThisFrame != 0 && Input.IsKeyDown(Keys.LeftAlt))
            {
                int factor = (Input.MouseScrollThisFrame) / 100;
                if (CameraState.FieldOfView - factor > 0 && CameraState.FieldOfView - factor < 180)
                    CameraState.FieldOfView -= factor;
            }

            if ((GameIsFocused) && (Input.MouseState.LeftButton == ButtonState.Pressed) && IsLeftClickHeldDown == false && Input.LeftClickHeldDownContext == null)
            {
                IsLeftClickHeldDown = true;
                mousePressEvent("left");
            }
            if ((GameIsFocused) && (Input.MouseState.LeftButton != ButtonState.Pressed) && IsLeftClickHeldDown == true)
            {
                IsLeftClickHeldDown = false;
                mouse_old_position = Input.MouseState.Position;
                mouseReleaseEvent("left");
            }
            if ((GameIsFocused) && (Input.MouseState.RightButton == ButtonState.Pressed) && IsRightClickHeldDown == false && Input.RightClickHeldDownContext == null)
            {
                IsRightClickHeldDown = true;
                mouse_old_position = Input.MouseState.Position;
                mousePressEvent("right");
            }
            if ((GameIsFocused) && (Input.MouseState.RightButton != ButtonState.Pressed) && IsRightClickHeldDown == true)
            {
                IsRightClickHeldDown = false;
                mouseReleaseEvent("right");
            }

            if ((GameIsFocused) && (Input.MouseScrollThisFrame != 0) && Input.IsKeyUp(Keys.LeftAlt))
            {
                if (Input.IsKeyDown(Keys.LeftControl))
                {
                    //Move slowly while ctrl is held down
                    wheelEvent((Input.MouseScrollThisFrame / 50));
                }
                else
                {
                    wheelEvent(Input.MouseScrollThisFrame);
                }
            }

            Point pos_tmp = Input.MouseState.Position;

            if (mouse_old_position.X == -1)                             //avoid moving on initialisation.
                mouse_old_position = pos_tmp;

            if ((GameIsFocused) && ((pos_tmp.X != mouse_old_position.X) || (pos_tmp.Y != mouse_old_position.Y)))
            {
                if (Input.IsKeyDown(Keys.LeftControl))
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

        //WSAD Movement
        private void Translate(float foward, float left, bool preciseMode, bool fastMode)
        {
            float factor = preciseMode ? 0.25f: 1f;

            if (fastMode)
                factor = 10f;

            factor *= 0.1f;

            foward *= factor;
            left *= factor;

            float distance = Vector3.Distance(CameraState.Position, CameraState.TargetPosition);

            //Forwards and backwards
            if (!MathHelpers.FloatEquals(foward, 0f))
            {
                float zoom = distance + foward;

                Quaternion rotation_x = Quaternion.CreateFromAxisAngle(Vector3.UnitY, viewer_angle.X);
                Quaternion rotation_y = Quaternion.CreateFromAxisAngle(Vector3.UnitX, viewer_angle.Y);
                Vector3 initialPos = CameraState.Position;

                CameraState.Position = CameraState.TargetPosition + Vector3.Transform(new Vector3(0, 0, zoom), rotation_x * rotation_y);
                CameraState.TargetPosition += CameraState.Position - initialPos;
            }

            if(!MathHelpers.FloatEquals(left, 0f))
            {
                //Left and right
                float altFactor = distance < 1f ? 1f : distance;
                panCamera(new Vector2(left, 0), (int)(1 * altFactor));
            }

        }

        //Reset
        public void ResetViewerAngles()
        {
            viewer_angle.Z = CameraState.Roll * (float)Math.PI / 180.0f;

            Vector3 dir = CameraState.Position - CameraState.TargetPosition;
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

        public virtual void ResetCamera()
        {
            IsLeftClickHeldDown = false;
            IsRightClickHeldDown = false;
            mouse_old_position = new Point(-1, -1);
            enable_spinning = false;
            enable_panning = false;
            viewer_angle = new Vector3((float)Math.PI, 0, 0);
            zoom = 1;
            CameraState.Reset();
            repositionCamera();
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

        #endregion
    }
}
