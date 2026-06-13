using Microsoft.Xna.Framework;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine.Vfx.Shape
{
    public class EffectShapeSegment
    {
        public Matrix4x4 Transform { get; set; }
        public float Age { get; set; }
        public float CreatedFrame { get; set; }
        public float ExpireFrame { get; set; }
        public float Life { get; set; }
        public float Scale { get; set; } = 1f;
        public float U { get; set; }
        public float V { get; set; }
        public float UvBaseU { get; set; }
        public float UvBaseV { get; set; }
        public float NormalizedTrailPosition { get; set; }
        public float DistanceFromTail { get; set; }
        public float DistanceFromHead { get; set; }
        public float TrailLength { get; set; }
        public float AlphaScale { get; set; } = 1f;
        public bool IsBootstrapSeed { get; set; }
        public bool IsRenderOnlyHead { get; set; }
        public Color PrimaryColor { get; set; } = Color.White;
        public Color SecondaryColor { get; set; } = Color.White;
    }
}
