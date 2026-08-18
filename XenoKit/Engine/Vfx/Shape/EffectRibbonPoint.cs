using Microsoft.Xna.Framework;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx.Shape
{
    public struct EffectRibbonPoint
    {
        public SimdVector3 Position { get; }
        public float HalfWidth { get; }
        public float BottomWidth { get; }
        public Color TopColor { get; }
        public Color BottomColor { get; }
        public float U { get; }

        public EffectRibbonPoint(SimdVector3 position, float halfWidth, Color topColor, Color bottomColor, float u)
            : this(position, halfWidth, halfWidth, topColor, bottomColor, u)
        {
        }

        public EffectRibbonPoint(SimdVector3 position, float topWidth, float bottomWidth, Color topColor, Color bottomColor, float u)
        {
            Position = position;
            HalfWidth = topWidth;
            BottomWidth = bottomWidth;
            TopColor = topColor;
            BottomColor = bottomColor;
            U = u;
        }
    }
}
