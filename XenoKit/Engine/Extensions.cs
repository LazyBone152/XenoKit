using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XenoKit.Engine
{
    public static class Extensions
    {

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
