using Microsoft.Xna.Framework.Graphics;
using System;

namespace XenoKit.Engine.Textures
{
    public class CustomColorCanvas
    {
        public readonly WeakReference<CharaPart> CharaPart;

        private readonly RenderTarget2D _canvas;

        public RenderTarget2D Canvas => _canvas;

        public bool IsDirty { get; set; }
        public bool HasCustomColors { get; set; }
        public bool IsDiscarded { get; set; }
        public int DytIndex { get; private set; }

        public CustomColorCanvas(CharaPart parentPart)
        {
            CharaPart = new WeakReference<CharaPart>(parentPart);
            _canvas = new RenderTarget2D(Viewport.Instance.GraphicsDevice, 128, 32, false, SurfaceFormat.Color, DepthFormat.None);
            IsDirty = true;
        }

        public bool ParentIsAlive()
        {
            return CharaPart.TryGetTarget(out _);
        }

        public CharaPart GetCharaPart()
        {
            return CharaPart.TryGetTarget(out CharaPart part) ? part : null;
        }

        public void SetDytIndex(int dytIdx)
        {
            IsDirty = dytIdx != DytIndex;
            DytIndex = dytIdx;
        }
    }
}
