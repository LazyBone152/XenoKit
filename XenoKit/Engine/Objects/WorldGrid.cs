using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XenoKit.Engine.Shapes;
using Microsoft.Xna.Framework;
using XenoKit.Engine.View;
using XenoKit.Editor;

namespace XenoKit.Engine.Objects
{
    public class WorldGrid
    {
        GraphicsDevice graphicsDevice;

        private Shapes.Plane[] cubes = new Shapes.Plane[80];
        private Color GridColor = Color.White;
        private float gridWidth = 0.005f;
        private float gridSpacing = 0.2f;
        private float gridLength = 7.6f;

        public WorldGrid(GraphicsDevice graphicsDevice)
        {
            this.graphicsDevice = graphicsDevice;

            for(int i = 0; i < 20; i++)
            {
                cubes[i] = new Shapes.Plane(new Vector3((gridSpacing * i), 0f, 0f), new Vector3(gridWidth, gridWidth, gridLength), graphicsDevice, GridColor);
            }

            for (int i = 0; i < 20; i++)
            {
                cubes[i + 20] = new Shapes.Plane(new Vector3(-(gridSpacing * i), 0f, 0f), new Vector3(gridWidth, gridWidth, gridLength), graphicsDevice, GridColor);
            }

            for (int i = 0; i < 20; i++)
            {
                cubes[i + 40] = new Shapes.Plane(new Vector3(0f, 0f, (gridSpacing * i)), new Vector3(gridLength, gridWidth, gridWidth), graphicsDevice, GridColor);
            }

            for (int i = 0; i < 20; i++)
            {
                cubes[i + 60] = new Shapes.Plane(new Vector3(0f, 0f, -(gridSpacing * i)), new Vector3(gridLength, gridWidth, gridWidth), graphicsDevice, GridColor);
            }
        }


        public void Draw(Camera camera)
        {
            if (SceneManager.ShowWorldAxis)
            {
                foreach(var cube in cubes)
                {
                    cube.Draw(camera);
                }
            }
        }
    }
}
