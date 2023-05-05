using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.Shapes;
using XenoKit.Engine.View;

namespace XenoKit.Engine.Objects
{
    public class ObjectAxis
    {
        public Cube yCube;
        public Cube xCube;
        public Cube zCube;
        public Cube leftBoneCube;

        public ObjectAxis(GraphicsDevice graphicsDevice)
        {
            xCube = new Cube(new Vector3(0, 0, 0), new Vector3(0.005f, 0.2f, 0.005f), graphicsDevice, Color.Red, true, true);
            yCube = new Cube(new Vector3(0, 0, 0), new Vector3(0.005f, 0.2f, 0.005f), graphicsDevice, Color.Green, true, true);
            zCube = new Cube(new Vector3(0, 0, 0), new Vector3(0.005f, 0.2f, 0.005f), graphicsDevice, Color.Blue, true, true);
            leftBoneCube = new Cube(new Vector3(0, 0, 0), new Vector3(0.01f, 0.01f, 0.01f), graphicsDevice, Color.Yellow, false, true);
        }

        public void Draw(GraphicsDevice graphicsDevice, Camera camera, Matrix world, bool isLeft = false)
        {
            xCube.Draw(camera, Matrix.CreateRotationZ(-(float)Math.PI / 2.0f) * world);
            yCube.Draw(camera, world);
            zCube.Draw(camera, Matrix.CreateRotationX((float)Math.PI / 2.0f) * world);
            if(isLeft)
                leftBoneCube.Draw(camera, world);
            
        }
    }
}
