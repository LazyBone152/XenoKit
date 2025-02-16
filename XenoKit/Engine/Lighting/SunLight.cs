using System;
using Microsoft.Xna.Framework;
using XenoKit.Engine.Stage;
using XenoKit.Engine.View;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Lighting
{
    public class SunLight : Entity
    {
        public Vector3 Direction { get; private set; }

        private Matrix LightViewMatrix { get; set; }
        private Matrix LightProjectionMatrix { get; set; }
        public Matrix LightViewProjectionMatrix { get; private set; }
        public Matrix LightViewProjectionBiasMatrix { get; private set; }

        private readonly Matrix BiasMatrix = new Matrix(
                                                0.5f, 0.0f, 0.0f, 0.0f,
                                                0.0f, -0.5f, 0.0f, 0.0f,
                                                0.0f, 0.0f, 1.0f, 0.0f,
                                                0.5f, 0.5f, 0.0f, 1.0f
                                              );

        public SunLight(GameBase game) : base(game) 
        {
            Xv2Stage.CurrentSpmChanged += Xv2Stage_CurrentSpmChanged;
        }

        private void Xv2Stage_CurrentSpmChanged(object sender, EventArgs e)
        {
            UpdateLight();
        }

        public override void Update()
        {
            //UpdateLight();
        }

        private void UpdateLight()
        {
            Direction = new Vector3(GameBase.CurrentStage.CurrentSpm.ShadowDirX, GameBase.CurrentStage.CurrentSpm.ShadowDirY, GameBase.CurrentStage.CurrentSpm.ShadowDirZ);
            //LightViewMatrix = Matrix.CreateLookAt(position, position + direction, Vector3.Up);
            LightViewMatrix = CreateDirectionalLightView(Direction, Vector3.Zero, 100f);

            float width = 500;
            float height = 500;
            float nearPlane = 0.5f;
            float farPlane = 500;

            LightProjectionMatrix = Matrix.CreateOrthographic(width, height, nearPlane, farPlane);

            LightViewProjectionMatrix = LightViewMatrix * LightProjectionMatrix;
            //LightViewProjectionMatrix = CreateLightViewProjectionMatrix(Direction, CameraBase.Frustum);
            //LightViewProjectionMatrix = CreateLightViewProjectionMatrix();
            LightViewProjectionBiasMatrix = LightViewProjectionMatrix * BiasMatrix;
        }

        private Matrix CreateLightViewProjectionMatrix()
        {
            // Matrix with that will rotate in points the direction of the light
            Matrix lightRotation = Matrix.CreateLookAt(Vector3.Zero,
                                                       -Direction,
                                                       Vector3.Up);

            // Get the corners of the frustum
            Vector3[] frustumCorners = CameraBase.Frustum.GetCorners();

            // Transform the positions of the corners into the direction of the light
            for (int i = 0; i < frustumCorners.Length; i++)
            {
                frustumCorners[i] = Vector3.Transform(frustumCorners[i], lightRotation);
            }

            // Find the smallest box around the points
            BoundingBox lightBox = BoundingBox.CreateFromPoints(frustumCorners);

            Vector3 boxSize = lightBox.Max - lightBox.Min;
            Vector3 halfBoxSize = boxSize * 0.5f;

            // The position of the light should be in the center of the back
            // pannel of the box. 
            Vector3 lightPosition = lightBox.Min + halfBoxSize;
            lightPosition.Z = lightBox.Min.Z;

            // We need the position back in world coordinates so we transform 
            // the light position by the inverse of the lights rotation
            lightPosition = Vector3.Transform(lightPosition,
                                              Matrix.Invert(lightRotation));

            // Create the view matrix for the light
            Matrix lightView = Matrix.CreateLookAt(lightPosition,
                                                   lightPosition + Direction,
                                                   Vector3.Up);

            // Create the projection matrix for the light
            // The projection is orthographic since we are using a directional light
            Matrix lightProjection = Matrix.CreateOrthographic(boxSize.X, boxSize.Y,
                                                               -boxSize.Z, boxSize.Z);

            return lightView * lightProjection;
        }

        public static Matrix CreateDirectionalLightView(Vector3 lightDirection, Vector3 sceneCenter, float sceneRadius)
        {
            lightDirection = Vector3.Normalize(lightDirection);

            // Create a light position sufficently far away, in the opposite direction
            Vector3 lightPosition = sceneCenter - lightDirection * sceneRadius * 2f;

            // Up vector - must not be parallel to light direction to avoid artifacts
            Vector3 up = Vector3.Up;
            if (Vector3.Dot(up, lightDirection) > 0.99f) // If too parallel, pick another
                up = Vector3.Right;

            return Matrix.CreateLookAt(lightPosition, sceneCenter, up);
        }

    }
}
