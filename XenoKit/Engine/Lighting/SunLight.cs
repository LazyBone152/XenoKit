using System;
using Microsoft.Xna.Framework;

namespace XenoKit.Engine.Lighting
{
    public class SunLight : Entity
    {
        private Vector3 Position;

        private Vector3 Direction;

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
            Position = new Vector3(450.0f, 350.0f, -450.0f); //BFten
            Direction = -Position;
            Direction.Normalize();
        }

        public override void Update()
        {
            LightViewMatrix = Matrix.CreateLookAt(Position,
                        Position + Direction,
                        Vector3.Up);

            float width = 1000;
            float height = 1000;
            float nearPlane = 0.5f;
            float farPlane = 1000;
            LightProjectionMatrix = Matrix.CreateOrthographic(width, height, nearPlane, farPlane);


            //LightViewMatrix = Matrix.CreateLookAt(Vector3.Zero, Direction, Vector3.Up);
            //LightProjectionMatrix = CalculateLightProjectionMatrix();
            //LightProjectionMatrix = CalculateStableLightProjectionMatrix(CameraBase.Frustum, Direction, SettingsManager.settings.XenoKit_ShadowMapRes);
            //CalculateHybridLightProjectionMatrix(CameraBase.Frustum, Direction, SettingsManager.settings.XenoKit_ShadowMapRes);
            //LightProjectionMatrix = CreateProjectionMatrix();

            LightViewProjectionMatrix = LightViewMatrix * LightProjectionMatrix;
            LightViewProjectionBiasMatrix = LightViewProjectionMatrix * BiasMatrix;

            //LightViewProjectionMatrix = CreateLightViewProjectionMatrix();
            //LightViewProjectionBiasMatrix = LightViewProjectionMatrix * BiasMatrix;
        }

    }
}
