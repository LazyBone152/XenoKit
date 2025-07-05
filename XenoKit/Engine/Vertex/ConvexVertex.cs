using MIConvexHull;
using Microsoft.Xna.Framework;


namespace XenoKit.Engine.Vertex
{
    public class ConvexVertex : IVertex
    {
        public double[] Position { get; }

        public Vector3 Original;

        public ConvexVertex(System.Numerics.Vector3 v)
        {
            Original = new Vector3(v.X, v.Y, v.Z);
            Position = new[] { (double)v.X, (double)v.Y, (double)v.Z };
        }
    }
}