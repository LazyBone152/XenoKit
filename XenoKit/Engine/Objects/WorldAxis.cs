using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Editor;
using XenoKit.Engine.Shapes;
using XenoKit.Engine.View;

namespace XenoKit.Engine.Objects
{
    public class WorldAxis
    {
        GraphicsDevice graphicsDevice;

        public Cube oCube;
        public Cube yCube;
        public Cube xCube;
        public Cube zCube;

        public WorldAxis(GraphicsDevice graphicsDevice)
        {
            this.graphicsDevice = graphicsDevice;
            oCube = new Cube(new Vector3(0, 0, 0), new Vector3(0.1f, 0.1f, 0.1f), graphicsDevice, Color.White);
            xCube = new Cube(new Vector3(1, 0, 0), new Vector3(0.1f, 0.1f, 0.1f), graphicsDevice, Color.Red);
            yCube = new Cube(new Vector3(0, 1, 0), new Vector3(0.1f, 0.1f, 0.1f), graphicsDevice, Color.Green);
            zCube = new Cube(new Vector3(0, 0, 1), new Vector3(0.1f, 0.1f, 0.1f), graphicsDevice, Color.Blue);
        }

        public void Draw(Camera camera)
        {
            if (SceneManager.ShowWorldAxis)
            {
                oCube.Draw(camera);
                xCube.Draw(camera);
                yCube.Draw(camera);
                zCube.Draw(camera);
            }
        }
    }
}
