using Microsoft.Xna.Framework;
using System;
using XenoKit.Editor;
using XenoKit.Engine.Stage;
using Xv2CoreLib.Resource.App;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Lighting
{
    public class BackgroundLight : DirLight
    {
        public readonly ShadowParams ShadowParams = new ShadowParams();

        private Matrix4x4 LightViewMatrix { get; set; }
        private Matrix4x4 LightProjectionMatrix { get; set; }
        public Matrix4x4 LightViewProjectionMatrix { get; private set; }
        public Matrix4x4 BiasMatrix_SM { get; private set; }
        public Matrix4x4 BiasMatrix_PM { get; private set; }
        public float ShadowFadeStart { get; private set; }
        public float ShadowFadeEnd { get; private set; }
        public float ShadowDistance { get; private set; }

        public BoundingFrustum LightFrustum { get; private set; }
        private readonly SimdVector3[] _frustumCorners = new SimdVector3[8];

        public BackgroundLight()
        {
            Xv2Stage.CurrentSpmChanged += Xv2Stage_CurrentSpmChanged;
            SettingsManager.SettingsSaved += SettingsManager_SettingsSaved;
            SettingsManager.SettingsReloaded += SettingsManager_SettingsSaved;

            LightFrustum = new BoundingFrustum(Matrix.Identity);

            //_PM bias matrix uses a fixed size of 512
            BiasMatrix_PM = new Matrix4x4(0.5f, 0.0f, 0.0f, 0.0f,
                                       0.0f, -0.5f, 0.0f, 0.0f,
                                       0.0f, 0.0f, 1.0f, 0.0f,
                                       (0.5f / 512f) + 0.5f, (0.5f / 512f) + 0.5f, 0.0f, 1.0f
                                       );

            CreateBiasMatrix(SettingsManager.settings.XenoKit_ShadowMapRes);
        }

        public void Update()
        {
            UpdateShadows();
        }

        private void UpdateShadows()
        {
            ComputeLiSPSM();
            //ComputeShadows();
            LightFrustum.Matrix = LightViewProjectionMatrix;
        }
        
        private void ComputeLiSPSM()
        {
            const float epsilon = 1e-5f;

            SimdVector3 lightDir = SimdVector3.Normalize(Direction);
            float shadowDistance = ComputeShadowDistance();
            float depthPadding = 10f;
            var camera = Viewport.Instance.Camera;

            //Get camera frustum corners and center in world space
            float tanHalfFov = MathF.Tan(camera.FovY * 0.5f);
            SimdVector3 center = SimdVector3.Zero;

            for (int i = 0; i < _frustumCorners.Length; i++)
            {
                float distance = (i & 4) != 0 ? shadowDistance : camera.NearClip;
                float halfHeight = distance * tanHalfFov;
                float halfWidth = halfHeight * camera.AspectRatio;

                SimdVector3 viewCorner = new SimdVector3((i & 1) != 0 ? halfWidth : -halfWidth, (i & 2) != 0 ? halfHeight : -halfHeight, -distance);

                _frustumCorners[i] = SimdVector3.Transform(viewCorner, camera.InverseViewMatrix);
                center += _frustumCorners[i];
            }

            center /= _frustumCorners.Length;

            //Calculate radius, conventional light view
            float radiusSquared = 0f;
            for (int i = 0; i < _frustumCorners.Length; i++)
            {
                radiusSquared = MathF.Max(radiusSquared, (_frustumCorners[i] - center).LengthSquared());
            }

            float radius = MathF.Sqrt(radiusSquared);

            float eyeDistance = radius + depthPadding + 1f;
            SimdVector3 lightEye = center - lightDir * eyeDistance;
            SimdVector3 up = MathF.Abs(SimdVector3.Dot(lightDir, SimdVector3.UnitY)) > 0.99f ? SimdVector3.UnitZ : SimdVector3.UnitY;

            LightViewMatrix = Matrix4x4.CreateLookAt(lightEye, center, up);

            //LiSPSM warp axis
            SimdVector3 camFwdWorld = SimdVector3.Normalize(SimdVector3.TransformNormal(new SimdVector3(0f, 0f, -1f), camera.InverseViewMatrix));
            SimdVector3 camFwdLight = SimdVector3.TransformNormal(camFwdWorld, LightViewMatrix);
            float sinGamma = MathF.Sqrt(camFwdLight.X * camFwdLight.X + camFwdLight.Y * camFwdLight.Y);
            float ax, ay;

            if (sinGamma > epsilon)
            {
                ax = camFwdLight.X / sinGamma;
                ay = camFwdLight.Y / sinGamma;
            }
            else
            {
                ax = 0f;
                ay = 1f;
            }

            float sideX = ay;
            float sideY = -ax;

            //Extent of the receiver frustum along the warp axis
//#if !DEBUG
            Span<SimdVector3> lightCorners = stackalloc SimdVector3[8];
//#else
//            //Dont use stackalloc while debugging because it disables hot reload
//            SimdVector3[] lightCorners = new SimdVector3[8];
//#endif
            float aMin = float.MaxValue;
            float aMax = float.MinValue;

            for (int i = 0; i < _frustumCorners.Length; i++)
            {
                SimdVector3 p = SimdVector3.Transform(_frustumCorners[i], LightViewMatrix);
                lightCorners[i] = p;

                float pa = p.X * ax + p.Y * ay;
                aMin = MathF.Min(aMin, pa);
                aMax = MathF.Max(aMax, pa);
            }

            float focusDepth = MathF.Max(aMax - aMin, 0.001f);

            float zn = MathF.Max(camera.NearClip, 0.01f);
            float zf = MathF.Max(shadowDistance, zn + 0.01f);
            float n = (zn + MathF.Sqrt(zn * zf)) / MathF.Max(sinGamma, epsilon);

            //Fallbck to a non-warped projection when looking almost along the light
            bool useWarp = n < focusDepth * 100f;

            //Warp matrix
            Matrix4x4 warp;

            if (useWarp)
            {
                float e = aMin - n;
                float f = n + focusDepth;
                float alpha = (f + n) / (f - n);
                float beta = -2f * f * n / (f - n);

                warp = new Matrix4x4(
                    sideX, alpha * ax, 0f, ax,
                    sideY, alpha * ay, 0f, ay,
                    0f, 0f, -1f, 0f,
                    0f, -alpha * e + beta, -eyeDistance, -e);
            }
            else
            {
                //No warp
                warp = new Matrix4x4(
                    sideX, ax, 0f, 0f,
                    sideY, ay, 0f, 0f,
                    0f, 0f, -1f, 0f,
                    0f, 0f, -eyeDistance, 1f);
            }

            //Orthographic fit in post-warp space
            float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

            for (int i = 0; i < _frustumCorners.Length; i++)
            {
                for (int k = 0; k < 2; k++)
                {
                    SimdVector3 p = lightCorners[i];
                    if (k == 1)
                        p.Z += depthPadding;

                    System.Numerics.Vector4 h = System.Numerics.Vector4.Transform(new System.Numerics.Vector4(p, 1f), warp);

                    float invW = 1f / MathF.Max(h.W, epsilon);
                    float x = h.X * invW;
                    float y = h.Y * invW;
                    float z = h.Z * invW;

                    minX = MathF.Min(minX, x); 
                    maxX = MathF.Max(maxX, x);
                    minY = MathF.Min(minY, y); 
                    maxY = MathF.Max(maxY, y);
                    minZ = MathF.Min(minZ, z); 
                    maxZ = MathF.Max(maxZ, z);
                }
            }

            //Small margin (about 1%) so edge filtering has data to sample
            float padX = (maxX - minX) * 0.01f;
            float padY = (maxY - minY) * 0.01f;
            minX -= padX; 
            maxX += padX;
            minY -= padY; 
            maxY += padY;

            float rangeX = MathF.Max(maxX - minX, epsilon);
            float rangeY = MathF.Max(maxY - minY, epsilon);

            float scaleX = 2f / rangeX;
            float scaleY = 2f / rangeY;
            float scaleZ = 1f / MathF.Max(maxZ - minZ, epsilon);

            Matrix4x4 fit = Matrix4x4.Identity;
            fit.M11 = scaleX;
            fit.M22 = scaleY;
            fit.M33 = scaleZ;
            fit.M41 = -(maxX + minX) / rangeX;
            fit.M42 = -(maxY + minY) / rangeY;
            fit.M43 = -minZ * scaleZ;

            LightProjectionMatrix = warp * fit;
            LightViewProjectionMatrix = LightViewMatrix * LightProjectionMatrix;

            //Shader parameters g_vShadowParam_PS.x and g_vShadowParam_PS.y respectively
            //Because MonoGame uses a different coordinate system to Xenoverse 2, these values have to be negated. In the actual game they are positive values, but here they must be negative.
            ShadowFadeStart = -(shadowDistance * ShadowParams.FadeStartRatio);
            ShadowFadeEnd = -shadowDistance;
            ShadowDistance = shadowDistance;
        }

        private void ComputeShadows()
        {
            //Standard non-perspective shadow mapping

            SimdVector3 lightDir = SimdVector3.Normalize(Direction);
            float shadowDistance = ShadowParams.ShadowFarMax * SettingsManager.settings.XenoKit_ShadowDistanceModifier;
            float depthPadding = 25f; 
            var camera = Viewport.Instance.Camera;

            //Get camera frustum corners and center in world space
            float tanHalfFov = MathF.Tan(camera.FovY * 0.5f);
            SimdVector3 center = SimdVector3.Zero;

            for (int i = 0; i < _frustumCorners.Length; i++)
            {
                float distance = (i & 4) != 0 ? shadowDistance : camera.NearClip;

                float halfHeight = distance * tanHalfFov;
                float halfWidth = halfHeight * camera.AspectRatio;

                SimdVector3 viewCorner = new SimdVector3((i & 1) != 0 ? halfWidth : -halfWidth, (i & 2) != 0 ? halfHeight : -halfHeight, -distance);

                _frustumCorners[i] = SimdVector3.Transform(viewCorner, camera.InverseViewMatrix);
                center += _frustumCorners[i];
            }

            center /= _frustumCorners.Length;

            //Work out the radius
            float radiusSquared = 0f;

            for (int i = 0; i < _frustumCorners.Length; i++)
            {
                radiusSquared = MathF.Max(radiusSquared, (_frustumCorners[i] - center).LengthSquared());
            }

            float radius = MathF.Sqrt(radiusSquared);

            //With the radius, we can construct the light view matrix
            float eyeDistance = radius + depthPadding + 1f;
            SimdVector3 lightEye = center - lightDir * eyeDistance;
            SimdVector3 up = MathF.Abs(SimdVector3.Dot(lightDir, SimdVector3.UnitY)) > 0.99f ? SimdVector3.UnitZ : SimdVector3.UnitY; //Select an up vector that is not parallel to the light direction
            LightViewMatrix = Matrix4x4.CreateLookAt(lightEye, center, up);

            //Transform the frustum corners into light view space and get its bounds
            SimdVector3 min = new SimdVector3(float.MaxValue);
            SimdVector3 max = new SimdVector3(float.MinValue);

            for (int i = 0; i < _frustumCorners.Length; i++)
            {
                SimdVector3 p = SimdVector3.Transform(_frustumCorners[i], LightViewMatrix);

                min = SimdVector3.Min(min, p);
                max = SimdVector3.Max(max, p);
            }

            //MonoGame uses right-handed matrix coordinates where negative Z is in front of the camera
            float nearPlane = Math.Max(0.01f, -max.Z - depthPadding);
            float farPlane = -min.Z + depthPadding;

            if (farPlane <= nearPlane)
                farPlane = nearPlane + 1f;

            LightProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(min.X, max.X, min.Y, max.Y, nearPlane, farPlane);
            LightViewProjectionMatrix = LightViewMatrix * LightProjectionMatrix;

            //Shader parameters g_vShadowParam_PS.x and g_vShadowParam_PS.y respectively
            //Because MonoGame uses a different coordinate system to Xenoverse 2, these values have to be negated. In the actual game they are positive values, but here they must be negative.
            ShadowFadeStart = -(shadowDistance * ShadowParams.FadeStartRatio);
            ShadowFadeEnd = -shadowDistance;
            ShadowDistance = shadowDistance;
        }
        
        private float ComputeShadowDistance()
        {
            //Mimics the way the game calculates shadow distance
            const float HeightEps = 0.01f;

            float d = MathF.Abs(Vector3.Dot(Viewport.Instance.Camera.Forward, Direction));
            float p = MathF.Pow(1f - d, ShadowParams.ShadowFarExponent);

            float h = MathHelper.Clamp(MathF.Max(Viewport.Instance.Camera.CameraState.Position.Y - ShadowParams.ShadowHeightStart, 0f) / (ShadowParams.ShadowHeightRange + HeightEps), 0f, 1f);

            float t = (1f - p) * h + p;
            float f = (ShadowParams.ShadowFarMax - ShadowParams.ShadowFarMin) * t + ShadowParams.ShadowFarMin;

            return MathHelper.Clamp(f, ShadowParams.ShadowFarMin, ShadowParams.ShadowFarMax) * SettingsManager.settings.XenoKit_ShadowDistanceModifier;
        }

        private void CreateBiasMatrix(int size)
        {
            //_SM bias matrix takes in the shadow map size for the 2 first values on the last row
            BiasMatrix_SM = new Matrix4x4(0.5f, 0.0f, 0.0f, 0.0f,
                                       0.0f, -0.5f, 0.0f, 0.0f,
                                       0.0f, 0.0f, 1.0f, 0.0f,
                                       (0.5f / size) + 0.5f, (0.5f / size) + 0.5f, 0.0f, 1.0f
                                       );
        }

        private void SettingsManager_SettingsSaved(object sender, EventArgs e)
        {
            CreateBiasMatrix(SettingsManager.settings.XenoKit_ShadowMapRes);
        }

        private void Xv2Stage_CurrentSpmChanged(object sender, EventArgs e)
        {
            if (Viewport.Instance.CurrentStage?.CurrentSpm != null)
            {
                ShadowParams.SetFromSpm(Viewport.Instance.CurrentStage.CurrentSpm);
            }
        }
    }
}
