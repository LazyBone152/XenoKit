using Microsoft.Xna.Framework;

namespace XenoKit.Engine.Lighting
{
    public class DirLight : Entity
    {
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

            //LightDir from the SPM is used by the game, but it looks... wrong here? Inverting the Z axis gets an okay result
            //Vector4 baseDir = new Vector4(GameBase.CurrentStage.CurrentSpm.LightDirX, GameBase.CurrentStage.CurrentSpm.LightDirY, -GameBase.CurrentStage.CurrentSpm.LightDirZ, GameBase.CurrentStage.CurrentSpm.LightDirW);
            Vector4 baseDir = new Vector4(-0.4f, 0.0f, -0.55f, 0);
            Vector4 direction = Vector4.Transform(baseDir, WVP);
            direction = new Vector4(direction.X, 0, MathHelper.Clamp(direction.Z, -1f, 1f), 0f);

            return direction;
        }
    }
}
