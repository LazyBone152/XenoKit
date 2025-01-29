using Microsoft.Xna.Framework;
using System;

namespace XenoKit.Engine.Lighting
{
    public class DirLight : Entity
    {
        public Matrix LightMatrix { get; private set; }
        public Matrix LightBiasMatrix { get; private set; }
        private Matrix SoloLightBiasMatrix { get; set; }

        public Vector4 Position { get; protected set; } = new Vector4(-1f, 0, -1f, 0f);

        public DirLight(GameBase gameBase) : base(gameBase)
        {
            //This stuff doesn't really work yet
            Vector3 lightPosition = new Vector3(0, 1000, 0);
            Vector3 direction = new Vector3(0, -1, 0);

            float width = 100;
            float height = 100f;
            float nearPlane = 1f;
            float farPlane = 1000f;

            LightMatrix = CalculateLightMatrix(lightPosition, direction, false, width, height, nearPlane, farPlane);
            SoloLightBiasMatrix = new Matrix(
                                                0.5f, 0.0f, 0.0f, 0.0f,
                                                0.0f, -0.5f, 0.0f, 0.0f,
                                                0.0f, 0.0f, 1.0f, 0.0f,
                                                0.5f, 0.5f, 0.0f, 1.0f
                                              );
            LightBiasMatrix = SoloLightBiasMatrix * LightMatrix;
        }

        public override void Update()
        {
            Position = new Vector4(GameBase.ActiveCameraBase.CameraState.Position, 1f);
        }

        public Vector4 GetLightDirection(Matrix WVP)
        {
            //Calculate light vector per actor
            Vector4 direction = Vector4.Transform(new Vector4(-0.4f, 0.0f, -0.55f, 0), WVP);
            direction = new Vector4(direction.X, 0, MathHelper.Clamp(direction.Z, -1f, 1f), 0f);

            return direction;
        }

        #region LightMatrix
        //Shadow map experimenting
        //None of this is functional right now
        //In the future, stage lighting will be moved into its own class

        public static Matrix CreateLookAtMatrix(Vector3 eye, Vector3 target, Vector3 up)
        {
            Vector3 zAxis = Vector3.Normalize(target - eye); // Forward vector
            Vector3 xAxis = Vector3.Normalize(Vector3.Cross(up, zAxis)); // Right vector
            Vector3 yAxis = Vector3.Cross(zAxis, xAxis); // Up vector

            return new Matrix(
                xAxis.X, yAxis.X, -zAxis.X, 0,
                xAxis.Y, yAxis.Y, -zAxis.Y, 0,
                xAxis.Z, yAxis.Z, -zAxis.Z, 0,
                -Vector3.Dot(xAxis, eye), -Vector3.Dot(yAxis, eye), Vector3.Dot(zAxis, eye), 1
            );
        }

        public static Matrix CreateOrthographicMatrix(float width, float height, float nearPlane, float farPlane)
        {
            return new Matrix(
                2.0f / width, 0, 0, 0,
                0, 2.0f / height, 0, 0,
                0, 0, 1.0f / (farPlane - nearPlane), 0,
                0, 0, -nearPlane / (farPlane - nearPlane), 1
            );
        }

        public static Matrix CreatePerspectiveMatrix(float fov, float aspectRatio, float nearPlane, float farPlane)
        {
            float tanHalfFov = (float)Math.Tan(fov / 2.0);

            return new Matrix(
                1.0f / (aspectRatio * tanHalfFov), 0, 0, 0,
                0, 1.0f / tanHalfFov, 0, 0,
                0, 0, farPlane / (farPlane - nearPlane), 1,
                0, 0, -nearPlane * farPlane / (farPlane - nearPlane), 0
            );
        }

        public static Matrix CalculateLightMatrix(Vector3 lightPosition, Vector3 lightDirection, bool isDirectional,
                                                  float widthOrFov, float heightOrAspectRatio, float nearPlane, float farPlane)
        {
            // Default up vector
            Vector3 upVector = Vector3.Up;
            if (Math.Abs(Vector3.Dot(lightDirection, upVector)) > 0.99f)
            {
                upVector = Vector3.Right; // Avoid degenerate case
            }

            // Calculate the Light View Matrix
            Matrix lightViewMatrix = CreateLookAtMatrix(lightPosition, lightPosition + lightDirection, upVector);

            // Calculate the Light Projection Matrix
            Matrix lightProjectionMatrix;
            if (isDirectional)
            {
                lightProjectionMatrix = CreateOrthographicMatrix(widthOrFov, heightOrAspectRatio, nearPlane, farPlane);
            }
            else
            {
                lightProjectionMatrix = CreatePerspectiveMatrix(widthOrFov, heightOrAspectRatio, nearPlane, farPlane);
            }

            // Combine the Projection and View matrices
            return lightProjectionMatrix * lightViewMatrix;
        }
        #endregion
    }
}
