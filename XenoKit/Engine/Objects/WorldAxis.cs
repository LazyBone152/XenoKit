using Microsoft.Xna.Framework;
using XenoKit.Engine.Shapes;

namespace XenoKit.Engine.Objects
{
    public class WorldAxis : Entity
    {
        public Cube oCube;
        public Cube yCube;
        public Cube xCube;
        public Cube zCube;

        public WorldAxis(GameBase gameBase) : base(gameBase)
        {
            oCube = new Cube(new Vector3(0, 0, 0), new Vector3(0.1f, 0.1f, 0.1f), gameBase, Color.White);
            xCube = new Cube(new Vector3(1, 0, 0), new Vector3(0.1f, 0.1f, 0.1f), gameBase, Color.Red);
            yCube = new Cube(new Vector3(0, 1, 0), new Vector3(0.1f, 0.1f, 0.1f), gameBase, Color.Green);
            zCube = new Cube(new Vector3(0, 0, 1), new Vector3(0.1f, 0.1f, 0.1f), gameBase, Color.Blue);
        }

        public override void Draw()
        {
            if (SceneManager.ShowWorldAxis)
            {
                oCube.Draw(Transform);
                xCube.Draw(Transform * Matrix.CreateScale(-1f, 1f, 1f));
                yCube.Draw(Transform);
                zCube.Draw(Transform);
            }
        }

    }
}
