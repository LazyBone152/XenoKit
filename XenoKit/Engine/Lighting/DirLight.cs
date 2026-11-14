using System.Numerics;
using System.Runtime.CompilerServices;

namespace XenoKit.Engine.Lighting
{
    public class DirLight
    {
        public Vector3 Direction { get; private set; }
        public Vector3 CameraSpaceDirection { get; private set; }
        public Vector3 Diffuse { get; private set; }
        public float DiffuseIntensity { get; private set; }
        public Vector3 Specular { get; private set; }
        public float SpecularIntensity { get; private set; }
        public bool PremultiplySpecular { get; set; }
        public bool PremultiplyDiffuse { get; set; }

        /*
        //LIGHT.EMA related; just let VfxLight handle that for now
        public Vector4 Position { get; private set; }
        public uint EnableMask { get; private set; } = uint.MaxValue;
        public float AttenuationC { get; private set; }
        public float AttenuationL_Start { get; private set; }
        public float AttenuationQ_End { get; private set; }
        public float InnerCone { get; private set; }
        public float OuterCone { get; private set; }
        */

        public void Reset()
        {
            Direction = new Vector3(0, 1, 0);
            Diffuse = Vector3.Zero;
            DiffuseIntensity = 0f;
            Specular = Vector3.Zero;
            SpecularIntensity = 0f;
        }

        public void SetDirection(in Vector3 direction)
        {
            Direction = Vector3.Normalize(direction);
        }

        public void SetDiffuse(in Vector3 diffuse, float intensity)
        {
            Diffuse = diffuse;
            DiffuseIntensity = intensity;
        }

        public void SetSpecular(in Vector3 specular, float intensity)
        {
            Specular = specular;
            SpecularIntensity = intensity;
        }

        public void CalculateCameraSpaceDirection(in Matrix4x4 invViewMatrix)
        {
            CameraSpaceDirection = Vector3.Normalize(Vector3.TransformNormal(Direction, invViewMatrix));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4 GetDiffuseVec4()
        {
            return PremultiplyDiffuse ?
                new Vector4(Diffuse.X * DiffuseIntensity, Diffuse.Y * DiffuseIntensity, Diffuse.Z * DiffuseIntensity, 1f) :
                new Vector4(Diffuse.X, Diffuse.Y, Diffuse.Z, DiffuseIntensity);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4 GetSpecularVec4()
        {
            return PremultiplySpecular ?
                new Vector4(Specular.X * SpecularIntensity, Specular.Y * SpecularIntensity, Specular.Z * SpecularIntensity, 1f) :
                new Vector4(Specular.X, Specular.Y, Specular.Z, SpecularIntensity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float CalcLuminance()
        {
            return EngineUtils.CalculateLuminance(Diffuse, DiffuseIntensity);
        }
    }
}
