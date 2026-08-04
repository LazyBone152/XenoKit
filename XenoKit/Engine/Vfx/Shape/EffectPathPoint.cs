namespace XenoKit.Engine.Vfx.Shape
{
    public struct EffectPathPoint
    {
        public float ScaleFactor { get; }
        public float ScaleAdd { get; }
        public float Offset { get; }
        public float Offset2 { get; }

        public EffectPathPoint(float scaleFactor, float scaleAdd, float offset, float offset2)
        {
            ScaleFactor = scaleFactor;
            ScaleAdd = scaleAdd;
            Offset = offset;
            Offset2 = offset2;
        }
    }
}
