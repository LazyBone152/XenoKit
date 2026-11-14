using System.Numerics;

namespace XenoKit.Engine.Lighting
{
    public class VirtualLightSource
    {
        public Vector3 PointLightPosition { get; private set; }
        public Vector4 PointLightStrength { get; private set; }
        public Vector4 PointLightColor { get; private set; }
        public Vector4 PointLightAttenuation { get; private set; }

        public void Reset()
        {
            PointLightPosition = Vector3.Zero;
            PointLightStrength = Vector4.One;
            PointLightColor = Vector4.Zero;
            PointLightAttenuation = Vector4.Zero;
        }

        public void SetVirtualPointLight(in Vector4 color, in Vector4 attenuation, in Vector4 strength, in Vector3 position)
        {
            PointLightColor = color;
            PointLightAttenuation = attenuation;
            PointLightStrength = strength;
            PointLightPosition = position;
        }
    }
}
