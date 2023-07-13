using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace XenoKit.Engine.Vertex
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexPositionTexture2 : IVertexType
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate;
        public Vector2 TextureCoordinate2;

        public static readonly VertexDeclaration VertexDeclaration;

        public VertexPositionTexture2(Vector3 position, Vector2 textureUv1)
        {
            this.Position = position;
            this.TextureCoordinate = textureUv1;
            TextureCoordinate2 = Vector2.Zero;
        }

        public VertexPositionTexture2(Vector3 position, Vector2 textureUv1, Vector2 textureUv2)
        {
            this.Position = position;
            this.TextureCoordinate = textureUv1;
            this.TextureCoordinate2 = textureUv2;
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
                return (Position.GetHashCode() * 397) ^ TextureCoordinate.GetHashCode() ^ TextureCoordinate2.GetHashCode();
            }
        }

        public override string ToString()
        {
            return "{{Position:" + this.Position + " TextureCoordinate:" + this.TextureCoordinate + "}}";
        }

        public static bool operator ==(VertexPositionTexture2 left, VertexPositionTexture2 right)
        {
            return ((left.Position == right.Position) && (left.TextureCoordinate == right.TextureCoordinate) && (left.TextureCoordinate2 == right.TextureCoordinate2));
        }

        public static bool operator !=(VertexPositionTexture2 left, VertexPositionTexture2 right)
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
            return (this == ((VertexPositionTexture2)obj));
        }

        static VertexPositionTexture2()
        {
            VertexElement[] elements = new VertexElement[] {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(20, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
            };
            VertexDeclaration declaration = new VertexDeclaration(elements);
            VertexDeclaration = declaration;
        }

    }
}
