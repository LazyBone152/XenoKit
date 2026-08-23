using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace XenoKit.Engine.Vertex
{
    // Native ETR trail vertices use this 36-byte order.
    [StructLayout(LayoutKind.Explicit, Size = 36)]
    public struct VertexPositionNormalColorTexture : IVertexType
    {
        [FieldOffset(0)]
        public Vector3 Position;

        [FieldOffset(12)]
        public Vector3 Normal;

        [FieldOffset(24)]
        public byte Color_R;
        [FieldOffset(25)]
        public byte Color_G;
        [FieldOffset(26)]
        public byte Color_B;
        [FieldOffset(27)]
        public byte Color_A;

        [FieldOffset(28)]
        public Vector2 TextureUV;

        public static readonly VertexDeclaration VertexDeclaration;
        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        public VertexPositionNormalColorTexture(Vector3 position, Vector3 normal, Vector2 textureUV, Color color)
        {
            Position = position;
            Normal = normal;
            TextureUV = textureUV;
            Color_R = color.R;
            Color_G = color.G;
            Color_B = color.B;
            Color_A = color.A;
        }

        static VertexPositionNormalColorTexture()
        {
            VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(24, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            };

            VertexDeclaration = new VertexDeclaration(elements);
        }
    }
}
