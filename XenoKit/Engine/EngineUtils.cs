using Microsoft.Xna.Framework;
using System;
using Xv2CoreLib.Resource;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector2 = System.Numerics.Vector2;
using SimdVector3 = System.Numerics.Vector3;
using SimdVector4 = System.Numerics.Vector4;
using SimdQuaternion = System.Numerics.Quaternion;
using System.Runtime.CompilerServices;

namespace XenoKit.Engine
{
    public static class EngineUtils
    {
        public static Ray TransformRay(Ray ray, Matrix world)
        {
            ray.Position = Vector3.Transform(ray.Position, world);
            ray.Direction = Vector3.TransformNormal(ray.Direction, world);
            ray.Direction.Normalize();

            return ray;
        }

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
        public static Ray CalculateRay(Vector2 mouseLocation)
        {
            mouseLocation.X = Viewport.Instance.GraphicsDevice.Viewport.Width - mouseLocation.X;

            Vector3 nearPoint = Viewport.Instance.GraphicsDevice.Viewport.Unproject(new Vector3(mouseLocation.X,
                    mouseLocation.Y, 0.0f),
                    Viewport.Instance.Camera.ProjectionMatrix,
                    Viewport.Instance.Camera.ViewMatrix,
                    Matrix.Identity);

            Vector3 farPoint = Viewport.Instance.GraphicsDevice.Viewport.Unproject(new Vector3(mouseLocation.X,
                    mouseLocation.Y, 0.5f),
                    Viewport.Instance.Camera.ProjectionMatrix,
                    Viewport.Instance.Camera.ViewMatrix,
                    Matrix.Identity);

            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();

            //Log.Add($"Near: {nearPoint}, Far: {farPoint}", LogType.Info);

            return new Ray(nearPoint, direction);
        }
        
        public static float? IntersectDistance(BoundingSphere sphere, Vector2 mouseLocation)
        {
            Ray mouseRay = CalculateRay(mouseLocation);
            return mouseRay.Intersects(sphere);
        }

        public static float? IntersectDistance(BoundingBox box, Vector2 mouseLocation)
        {
            Ray mouseRay = CalculateRay(mouseLocation);
            return mouseRay.Intersects(box);
        }

        //Math
        public static SimdVector3 QuaternionToEuler(SimdQuaternion q)
        {
            float sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            float cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            float roll = (float)Math.Atan2(sinr_cosp, cosr_cosp);

            float sinp = 2 * (q.W * q.Y - q.Z * q.X);
            float pitch = (float)(Math.Abs(sinp) >= 1 ? MathHelpers.CopySign(MathHelper.Pi / 2, sinp) : Math.Asin(sinp)); // Y-axis

            float siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            float cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            float yaw = (float)Math.Atan2(siny_cosp, cosy_cosp);

            return new SimdVector3(roll, pitch, yaw) * (180f / MathHelper.Pi);
        }

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

        //Color
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateLuminance(Vector3 rgb)
        {
            return CalculateLuminance(rgb.X, rgb.Y, rgb.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateLuminance(SimdVector3 rgb)
        {
            return CalculateLuminance(rgb.X, rgb.Y, rgb.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateLuminance(Vector3 rgb, float a)
        {
            return CalculateLuminance(rgb.X, rgb.Y, rgb.Z) * a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateLuminance(SimdVector3 rgb, float a)
        {
            return CalculateLuminance(rgb.X, rgb.Y, rgb.Z) * a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateLuminance(Vector4 color)
        {
            return CalculateLuminance(color.X, color.Y, color.Z) * color.W;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateLuminance(SimdVector4 color)
        {
            return CalculateLuminance(color.X, color.Y, color.Z) * color.W;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateLuminance(float r, float g, float b)
        {
            return r * 0.299f + g * 0.587f + b * 0.114f;
        }
    }
}
