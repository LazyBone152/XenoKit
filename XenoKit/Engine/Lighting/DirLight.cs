using Microsoft.Xna.Framework;
using System;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Lighting
{
    public class DirLight : Entity
    {
        public Vector3 SpotLightPos = new Vector3(5, 10, 10);

        public Vector4 Position { get; protected set; } = new Vector4(-1f, 0, -1f, 0f);

        public DirLight(GameBase gameBase) : base(gameBase)
        {

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

    }
}
