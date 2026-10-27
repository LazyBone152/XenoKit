using Microsoft.Xna.Framework;
using System;
using XenoKit.Editor;
using XenoKit.Engine.Stage;
using Xv2CoreLib.Resource.App;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Lighting
{
    public class StageLight : EngineObject
    {
        public readonly ShadowParams ShadowParams = new ShadowParams();

        public SimdVector3 ShadowDirection { get; private set; }
        public SimdVector3 LightDirection { get; private set; }

        private Matrix4x4 LightViewMatrix { get; set; }
        private Matrix4x4 LightProjectionMatrix { get; set; }
        public Matrix4x4 LightViewProjectionMatrix { get; private set; }
        public Matrix4x4 BiasMatrix_SM { get; private set; }
        public Matrix4x4 BiasMatrix_PM { get; private set; }
        public float ShadowFadeStart { get; private set; }
        public float ShadowFadeEnd { get; private set; }

        public BoundingFrustum LightFrustum { get; private set; }
        private readonly SimdVector3[] _frustumCorners = new SimdVector3[8];


        public StageLight()
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

        public override void Update()
        {
            LightDirection = new SimdVector3(Viewport.Instance.CurrentStage.CurrentSpm.LightDirX, Viewport.Instance.CurrentStage.CurrentSpm.LightDirY, Viewport.Instance.CurrentStage.CurrentSpm.LightDirZ);

            UpdateShadows();
        }

        private void UpdateShadows()
        {
            ShadowDirection = new SimdVector3(ViewportInstance.CurrentStage.CurrentSpm.ShadowDirX, ViewportInstance.CurrentStage.CurrentSpm.ShadowDirY, ViewportInstance.CurrentStage.CurrentSpm.ShadowDirZ);
            ShadowDirection = SimdVector3.Normalize(ShadowDirection);

            ComputeShadows();
            LightFrustum.Matrix = LightViewProjectionMatrix;
        }

        private void ComputeShadows()
        {
            //Computes orthographic shadow mapping matrices
            //This is not the same setup as the game - Xenoverse 2 uses LiSPSM (light space perspective shadow maps)... but this is much simpler to implement, works well enough, and more importantly... is significantly better than the previous setup
            //Implementing full LiSPSM could be something to do later

            SimdVector3 lightDir = SimdVector3.Normalize(ShadowDirection);
            float shadowDistance = ShadowParams.ShadowFarMax * LocalSettings.Instance.ShadowDistanceModifier;
            float depthPadding = 25f; 
            var camera = Viewport.Instance.Camera;

            //Get camera frustum corners and center in world space
            Matrix4x4.Invert(camera.ViewMatrix, out Matrix4x4 inverseCameraView);

            float tanHalfFov = MathF.Tan(camera.FovY * 0.5f);
            SimdVector3 center = SimdVector3.Zero;

            for (int i = 0; i < _frustumCorners.Length; i++)
            {
                float distance = (i & 4) != 0 ? shadowDistance : camera.NearClip;

                float halfHeight = distance * tanHalfFov;
                float halfWidth = halfHeight * camera.AspectRatio;

                SimdVector3 viewCorner = new SimdVector3((i & 1) != 0 ? halfWidth : -halfWidth, (i & 2) != 0 ? halfHeight : -halfHeight, -distance);

                _frustumCorners[i] = SimdVector3.Transform(viewCorner, inverseCameraView);
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
            //SetFadeRange();
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
                LightDirection = new SimdVector3(Viewport.Instance.CurrentStage.CurrentSpm.LightDirX, Viewport.Instance.CurrentStage.CurrentSpm.LightDirY, Viewport.Instance.CurrentStage.CurrentSpm.LightDirZ);
                ShadowParams.SetFromSpm(Viewport.Instance.CurrentStage.CurrentSpm);
            }

            UpdateShadows();
        }
    }
}
