using Microsoft.Xna.Framework;
using XenoKit.Engine.Shapes;

namespace XenoKit.Engine.Objects
{
	public class DrawableBoundingBox
    {
		private readonly Cube cube;

		public DrawableBoundingBox(GameBase game)
		{
			cube = new Cube(new Vector3(0.5f), new Vector3(-0.5f), new Vector3(0.5f), 0.5f, Color.Pink, true, game);
        }
		
        public void Draw(Matrix world, BoundingBox box)
        {
            cube.SetBounds(box.Min, box.Max, 0f, true);
            cube.Transform = world;
            cube.Draw();
        }
	}
}
