using Microsoft.Xna.Framework;
using XenoKit.Engine.Shapes;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine.Objects
{
	public class DrawableBoundingBox
    {
		private readonly Cube cube;
        public BoundingBox Bounds { get; private set; }

		public DrawableBoundingBox()
		{
			cube = new Cube(new Vector3(0.5f), new Vector3(-0.5f), new Vector3(0.5f), 0.5f, Color.Pink, true, false);
        }

        public void SetBounds(BoundingBox boundingBox)
        {
            Bounds = boundingBox;
            cube.SetBounds(boundingBox.Min, boundingBox.Max, 0f, true);
        }
		
        public void Draw(Matrix4x4 world)
        {
            cube.Transform = world;
            cube.Draw();
        }

        public void Draw(Matrix4x4 world, BoundingBox box)
        {
            if (Bounds != box)
                SetBounds(box);

            cube.Transform = world;
            cube.Draw();
        }
	}
}