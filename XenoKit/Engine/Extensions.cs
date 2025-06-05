using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Xv2CoreLib.EMD;
using Xv2CoreLib.Resource;

namespace XenoKit.Engine
{
    public static class Extensions
    {
        public static bool IsAproxEqual(this Vector3 a, Vector3 b)
        {
            return MathHelpers.FloatEquals(a.X, b.X) && MathHelpers.FloatEquals(a.Y, b.Y) && MathHelpers.FloatEquals(a.Z, b.Z);
        }

        public static BoundingBox ConvertToBoundingBox(this EMD_AABB aabb)
        {
            Vector3 min = new Vector3(aabb.MinX, aabb.MinY, aabb.MinZ);
            Vector3 max = new Vector3(aabb.MaxX, aabb.MaxY, aabb.MaxZ);
            return new BoundingBox(min, max);
        }

        public static BoundingBox Transform(this BoundingBox box, Matrix transform)
        {
            Vector3[] corners = box.GetCorners();
            Vector3 min = Vector3.Transform(corners[0], transform);
            Vector3 max = min;

            for (int i = 1; i < corners.Length; i++)
            {
                Vector3 transformed = Vector3.Transform(corners[i], transform);
                min = Vector3.Min(min, transformed);
                max = Vector3.Max(max, transformed);
            }

            return new BoundingBox(min, max);
        }

        public static void CopyState(this BlendState blend, int copyFrom, int copyTo)
        {
            if (copyFrom < 0 || copyFrom >= 4)
                throw new ArgumentOutOfRangeException("BlendState.CopyState: copyFrom param out of range.");

            if (copyTo < 0 || copyTo >= 4)
                throw new ArgumentOutOfRangeException("BlendState.CopyState: copyTo param out of range.");

            blend[copyTo].AlphaBlendFunction = blend[copyFrom].AlphaBlendFunction;
            blend[copyTo].AlphaSourceBlend = blend[copyFrom].AlphaSourceBlend;
            blend[copyTo].AlphaDestinationBlend = blend[copyFrom].AlphaDestinationBlend;

            blend[copyTo].ColorBlendFunction = blend[copyFrom].ColorBlendFunction;
            blend[copyTo].ColorSourceBlend = blend[copyFrom].ColorSourceBlend;
            blend[copyTo].ColorDestinationBlend = blend[copyFrom].ColorDestinationBlend;

            blend[copyTo].ColorWriteChannels = blend[copyFrom].ColorWriteChannels;
        }

        public static void ApplyAlphaBlend(this BlendState blendState, int applyTo)
        {
            blendState[applyTo].ColorSourceBlend = Blend.SourceAlpha;
            blendState[applyTo].ColorDestinationBlend = Blend.InverseSourceAlpha;
            blendState[applyTo].ColorBlendFunction = BlendFunction.Add;

            blendState[applyTo].AlphaSourceBlend = Blend.SourceAlpha;
            blendState[applyTo].AlphaDestinationBlend = Blend.InverseSourceAlpha;
            blendState[applyTo].AlphaBlendFunction = BlendFunction.Add;

            blendState[applyTo].ColorWriteChannels = ColorWriteChannels.All;
        }

        public static void ApplyAdditive(this BlendState blendState, int applyTo)
        {
            blendState[applyTo].ColorSourceBlend = Blend.SourceAlpha;
            blendState[applyTo].ColorDestinationBlend = Blend.One;
            blendState[applyTo].ColorBlendFunction = BlendFunction.Add;

            blendState[applyTo].AlphaSourceBlend = Blend.SourceAlpha;
            blendState[applyTo].AlphaDestinationBlend = Blend.One;
            blendState[applyTo].AlphaBlendFunction = BlendFunction.Add;

            blendState[applyTo].ColorWriteChannels = ColorWriteChannels.All;
        }

        public static void ApplySubtractive(this BlendState blendState, int applyTo)
        {
            blendState[applyTo].AlphaBlendFunction = BlendFunction.ReverseSubtract;
            blendState[applyTo].AlphaSourceBlend = Blend.SourceAlpha;
            blendState[applyTo].AlphaDestinationBlend = Blend.One;

            blendState[applyTo].ColorBlendFunction = BlendFunction.ReverseSubtract;
            blendState[applyTo].ColorSourceBlend = Blend.SourceAlpha;
            blendState[applyTo].ColorDestinationBlend = Blend.One;

            blendState[applyTo].ColorWriteChannels = ColorWriteChannels.All;
        }

        public static void ApplyCustom(this BlendState blendState, int applyTo, BlendFunction blendFunction, Blend sourceBlend, Blend destinationBlend, ColorWriteChannels colorMask = ColorWriteChannels.All)
        {
            blendState[applyTo].AlphaBlendFunction = blendFunction;
            blendState[applyTo].AlphaSourceBlend = sourceBlend;
            blendState[applyTo].AlphaDestinationBlend = destinationBlend;

            blendState[applyTo].ColorBlendFunction = blendFunction;
            blendState[applyTo].ColorSourceBlend = sourceBlend;
            blendState[applyTo].ColorDestinationBlend = destinationBlend;

            blendState[applyTo].ColorWriteChannels = colorMask;
        }

        public static void ApplyNone(this BlendState blendState, int applyTo)
        {
            blendState[applyTo].ColorSourceBlend = Blend.One;
            blendState[applyTo].ColorDestinationBlend = Blend.Zero;
            blendState[applyTo].ColorBlendFunction = BlendFunction.Add;

            blendState[applyTo].AlphaSourceBlend = Blend.One;
            blendState[applyTo].AlphaDestinationBlend = Blend.Zero;
            blendState[applyTo].AlphaBlendFunction = BlendFunction.Add;

            blendState[applyTo].ColorWriteChannels = ColorWriteChannels.All;
        }

    }
}
