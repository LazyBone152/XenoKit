using Microsoft.Xna.Framework;

namespace XenoKit.Engine.Text
{
    public struct TextInstance
    {
        public readonly string Text;
        public readonly Color Color;
        public readonly Vector2 ScreenPosition;

        public TextInstance(string text, Color color, Vector2 screenPosition)
        {
            Text = text;
            Color = color;
            ScreenPosition = screenPosition;
        }

    }
}
