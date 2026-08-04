using System.Numerics;

namespace XenoKit.Engine.Vfx
{
    public static class VfxRotation
    {
        // Xenoverse VFX stores X/Y/Z as pitch/yaw/roll, while CreateFromYawPitchRoll expects yaw/pitch/roll.
        public static Matrix4x4 Create(float rotationX, float rotationY, float rotationZ)
        {
            return Matrix4x4.CreateFromYawPitchRoll(rotationY, rotationX, rotationZ);
        }
    }
}
