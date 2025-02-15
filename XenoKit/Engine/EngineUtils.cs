using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XenoKit.Editor;
using Xv2CoreLib.Resource;

namespace XenoKit.Engine
{
    public static class EngineUtils
    {
        public static Matrix CreateInfinitePerspective(float fov, float aspect, float nearPlane)
        {
            float yScale = 1.0f / (float)Math.Tan(fov / 2.0f);
            float xScale = yScale / aspect;

            return new Matrix(
                xScale, 0, 0, 0,
                0, yScale, 0, 0,
                0, 0, -1, -1,
                0, 0, -nearPlane, 0
            );
        }

        //Mouse Picking
        public static Ray CalculateRay(Vector2 mouseLocation, GameBase gameBase)
        {
            mouseLocation.X = gameBase.GraphicsDevice.Viewport.Width - mouseLocation.X;

            Vector3 nearPoint = gameBase.GraphicsDevice.Viewport.Unproject(new Vector3(mouseLocation.X,
                    mouseLocation.Y, 0.0f),
                    gameBase.ActiveCameraBase.ProjectionMatrix,
                    gameBase.ActiveCameraBase.ViewMatrix,
                    Matrix.Identity);

            Vector3 farPoint = gameBase.GraphicsDevice.Viewport.Unproject(new Vector3(mouseLocation.X,
                    mouseLocation.Y, 0.5f),
                    gameBase.ActiveCameraBase.ProjectionMatrix,
                    gameBase.ActiveCameraBase.ViewMatrix,
                    Matrix.Identity);

            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();

            //Log.Add($"Near: {nearPoint}, Far: {farPoint}", LogType.Info);

            return new Ray(nearPoint, direction);
        }

        public static float? IntersectDistance(BoundingSphere sphere, Vector2 mouseLocation, GameBase gameBase)
        {
            Ray mouseRay = CalculateRay(mouseLocation, gameBase);
            return mouseRay.Intersects(sphere);
        }

        public static float? IntersectDistance(BoundingBox box, Vector2 mouseLocation, GameBase gameBase)
        {
            Ray mouseRay = CalculateRay(mouseLocation, gameBase);
            return mouseRay.Intersects(box);
        }

        //Math
        public static Vector3 QuaternionToEuler(Quaternion q)
        {
            float sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            float cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            float roll = (float)Math.Atan2(sinr_cosp, cosr_cosp);

            float sinp = 2 * (q.W * q.Y - q.Z * q.X);
            float pitch = (float)(Math.Abs(sinp) >= 1 ? MathHelpers.CopySign(MathHelper.Pi / 2, sinp) : Math.Asin(sinp)); // Y-axis

            float siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            float cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            float yaw = (float)Math.Atan2(siny_cosp, cosy_cosp);

            return new Vector3(roll, pitch, yaw) * (180f / MathHelper.Pi);
        }
    }
}
