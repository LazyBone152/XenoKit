using System;
using Microsoft.Xna.Framework;
using XenoKit.Engine.Shapes;

namespace XenoKit.Engine.Objects
{
    public class ObjectAxis : Entity
    {
        public Cube yCube;
        public Cube xCube;
        public Cube zCube;
        public Cube leftBoneCube;
        private bool IsWorldAxis;

        public ObjectAxis(bool isWorldAxis, GameBase gameBase) : base(gameBase)
        {
            IsWorldAxis = isWorldAxis;
            xCube = new Cube(new Vector3(0, 0, 0), new Vector3(0.005f, 0.2f, 0.005f), gameBase, Color.Red, true, true);
            yCube = new Cube(new Vector3(0, 0, 0), new Vector3(0.005f, 0.2f, 0.005f), gameBase, Color.Green, true, true);
            zCube = new Cube(new Vector3(0, 0, 0), new Vector3(0.005f, 0.2f, 0.005f), gameBase, Color.Blue, true, true);
            leftBoneCube = new Cube(new Vector3(0, 0, 0), new Vector3(0.01f, 0.01f, 0.01f), gameBase, Color.Yellow, false, true);
        }

        public override void Draw()
        {
            Draw(Matrix.Identity);
        }

        public void Draw(Matrix world, bool isLeft = false)
        {
            if((IsWorldAxis && SceneManager.ShowWorldAxis) || !IsWorldAxis)
            {
                xCube.Draw(Matrix.CreateRotationZ(-(float)Math.PI / 2.0f) * world);
                yCube.Draw(world);
                zCube.Draw(Matrix.CreateRotationX((float)Math.PI / 2.0f) * world);
                if (isLeft)
                    leftBoneCube.Draw(world);
            }
        }
    }
}
