using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace XenoKit.Engine.Vertex
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexPositionTexture5 : IVertexType
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate;
        public Vector2 TextureCoordinate2;
        public Vector2 TextureCoordinate3;
        public Vector2 TextureCoordinate4;
        public Vector2 TextureCoordinate5;

        public static readonly VertexDeclaration VertexDeclaration;

        public VertexPositionTexture5(Vector3 position, Vector2 textureUv1)
        {
            this.Position = position;
            this.TextureCoordinate = textureUv1;
            TextureCoordinate2 = Vector2.Zero;
            TextureCoordinate3 = Vector2.Zero;
            TextureCoordinate4 = Vector2.Zero;
            TextureCoordinate5 = Vector2.Zero;
        }

        public VertexPositionTexture5(Vector3 position, Vector2 textureUv1, Vector2 textureUv2)
        {
            this.Position = position;
            this.TextureCoordinate = textureUv1;
            this.TextureCoordinate2 = textureUv2;
            TextureCoordinate3 = Vector2.Zero;
            TextureCoordinate4 = Vector2.Zero;
            TextureCoordinate5 = Vector2.Zero;
        }

        public VertexPositionTexture5(Vector3 position, Vector2 textureUv1, Vector2 textureUv2, Vector2 textureUv3, Vector2 textureUv4, Vector2 textureUv5)
        {
            this.Position = position;
            this.TextureCoordinate = textureUv1;
            this.TextureCoordinate2 = textureUv2;
            TextureCoordinate3 = textureUv3;
            TextureCoordinate4 = textureUv4;
            TextureCoordinate5 = textureUv5;
        }

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get
            {
                return VertexDeclaration;
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Position.GetHashCode() * 397) ^ TextureCoordinate.GetHashCode() ^ TextureCoordinate2.GetHashCode()
                    ^ TextureCoordinate3.GetHashCode() ^ TextureCoordinate4.GetHashCode() ^ TextureCoordinate5.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"Pos: {Position}, TEX0: {TextureCoordinate}, TEX1: {TextureCoordinate2}, TEX2: {TextureCoordinate3}, TEX3: {TextureCoordinate4}";
        }

        public static bool operator ==(VertexPositionTexture5 left, VertexPositionTexture5 right)
        {
            return ((left.Position == right.Position) && (left.TextureCoordinate == right.TextureCoordinate)
                && (left.TextureCoordinate2 == right.TextureCoordinate2)
                && (left.TextureCoordinate3 == right.TextureCoordinate3)
                && (left.TextureCoordinate4 == right.TextureCoordinate4)
                && (left.TextureCoordinate5 == right.TextureCoordinate5));
        }

        public static bool operator !=(VertexPositionTexture5 left, VertexPositionTexture5 right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj.GetType() != base.GetType())
            {
                return false;
            }
            return (this == ((VertexPositionTexture5)obj));
        }

        static VertexPositionTexture5()
        {
            VertexElement[] elements = new VertexElement[] {
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(20, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
                new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 2),
                new VertexElement(36, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 3),
                new VertexElement(44, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 4),
            };
            VertexDeclaration declaration = new VertexDeclaration(elements);
            VertexDeclaration = declaration;
        }

    }
}
