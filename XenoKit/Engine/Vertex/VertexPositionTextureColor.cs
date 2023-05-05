using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace XenoKit.Engine.Vertex
{
    //Vertex definition optimized for particles.

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public struct VertexPositionTextureColor : IVertexType
    {
        [FieldOffset(0)]
        public Vector3 Position;

        [FieldOffset(12)]
        public byte Color_R;
        [FieldOffset(13)]
        public byte Color_G;
        [FieldOffset(14)]
        public byte Color_B;
        [FieldOffset(15)]
        public byte Color_A;

        /*
        [FieldOffset(12)]
        public byte Color_B;
        [FieldOffset(13)]
        public byte Color_G;
        [FieldOffset(14)]
        public byte Color_R;
        [FieldOffset(15)]
        public byte Color_A;
        */

        [FieldOffset(16)]
        public Vector2 TextureUV;

        //Vertex Type:
        public static readonly VertexDeclaration VertexDeclaration;
        VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="color">Color in float format (0 to 1).</param>
        public VertexPositionTextureColor(Vector3 position, Vector2 textureUV, Vector4 color)
        {
            Position = position;
            TextureUV = textureUV;
            Color_R = (byte)(color.X * 255);
            Color_G = (byte)(color.Y * 255);
            Color_B = (byte)(color.Z * 255);
            Color_A = (byte)(color.W * 255);
        }

        public VertexPositionTextureColor(Vector3 position, Vector2 textureUV, float[] color)
        {
            Position = position;
            TextureUV = textureUV;
            Color_R = (byte)(color[0] * 255);
            Color_G = (byte)(color[1] * 255);
            Color_B = (byte)(color[2] * 255);
            Color_A = (byte)(color[3] * 255);
        }

        public void SetColor(float[] rgba)
        {
            Color_R = (byte)(rgba[0] * 255);
            Color_G = (byte)(rgba[1] * 255);
            Color_B = (byte)(rgba[2] * 255);
            Color_A = (byte)(rgba[3] * 255);
        }

        static VertexPositionTextureColor()
        {
            VertexElement[] elements = new VertexElement[] 
            {    
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            };
            VertexDeclaration declaration = new VertexDeclaration(elements);
            VertexDeclaration = declaration;
        }

        public override int GetHashCode() { return 0; } // TODO: Fix        
        public static bool operator ==(VertexPositionTextureColor left, VertexPositionTextureColor right) { return (((left.Position == right.Position) && (left.TextureUV == right.TextureUV) && left.Color_A == right.Color_A && left.Color_R == right.Color_R && left.Color_G == right.Color_G && left.Color_B == right.Color_B)); }
        public static bool operator !=(VertexPositionTextureColor left, VertexPositionTextureColor right) { return !(left == right); }
        public override bool Equals(object obj) { if (obj == null) { return false; } if (obj.GetType() != base.GetType()) { return false; } return (this == ((VertexPositionTextureColor)obj)); }
        public override string ToString() { return string.Format("{{Position:{0} Texture UV:{1} Color RGBA: {2}}", new object[] { this.Position, this.TextureUV, $"{Color_R}, {Color_G}, {Color_B}, {Color_A}" }); }

    }
}
