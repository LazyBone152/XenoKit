using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace XenoKit.Engine.Vertex
{
    [StructLayout(LayoutKind.Explicit, Size = 72)]
    public struct VertexPositionNormalTextureBlend : IVertexType
    {
        [FieldOffset(0)]
        public Vector3 Position;
        [FieldOffset(12)]
        public Vector3 Normal;
        [FieldOffset(24)]
        public Vector3 Tangent;
        [FieldOffset(36)]
        public Vector2 TextureUV0;
        [FieldOffset(44)]
        public Vector2 TextureUV1;

        [FieldOffset(52)]
        public byte Color_B;
        [FieldOffset(53)]
        public byte Color_G;
        [FieldOffset(54)]
        public byte Color_R;
        [FieldOffset(55)]
        public byte Color_A;

        [FieldOffset(56)]
        public byte BlendIndex0;
        [FieldOffset(57)]
        public byte BlendIndex1;
        [FieldOffset(58)]
        public byte BlendIndex2;
        [FieldOffset(59)]
        public byte BlendIndex3;
        [FieldOffset(60)]
        public Vector3 BlendWeights;

        //Vertex Type:
        public static readonly VertexDeclaration VertexDeclaration;
        VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }



        public VertexPositionNormalTextureBlend(Vector3 position, Vector3 normal, Vector3 tangent, Vector2 texUV0, Vector2 texUV1, Vector4 color, byte blendIndex0, byte blendIndex1, byte blendIndex2, byte blendIndex3, Vector3 blendWeights)
        {
            Position = position;
            Normal = normal;
            Tangent = tangent;
            TextureUV0 = texUV0;
            TextureUV1 = texUV1;
            Color_R = (byte)color.X;
            Color_G = (byte)color.Y;
            Color_B = (byte)color.Z;
            Color_A = (byte)color.W;
            BlendIndex0 = blendIndex0;
            BlendIndex1 = blendIndex1;
            BlendIndex2 = blendIndex2;
            BlendIndex3 = blendIndex3;
            BlendWeights = blendWeights;
        }

        static VertexPositionNormalTextureBlend()
        {
            VertexElement[] elements = new VertexElement[] {    new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                                                                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                                                                new VertexElement(24, VertexElementFormat.Vector3, VertexElementUsage.Tangent, 0),
                                                                new VertexElement(36, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                                                                new VertexElement(44, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
                                                                new VertexElement(52, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                                                                new VertexElement(56, VertexElementFormat.Byte4, VertexElementUsage.BlendIndices, 0),
                                                                new VertexElement(60, VertexElementFormat.Vector3, VertexElementUsage.BlendWeight, 0) };
            VertexDeclaration declaration = new VertexDeclaration(elements);
            VertexDeclaration = declaration;
        }

        public override int GetHashCode() { return 0; } // TODO: Fix        
        public static bool operator ==(VertexPositionNormalTextureBlend left, VertexPositionNormalTextureBlend right) { return (((left.Position == right.Position) && (left.Normal == right.Normal)) && (left.TextureUV0 == right.TextureUV0) && (left.BlendIndex0 == right.BlendIndex0) && (left.BlendIndex1 == right.BlendIndex1) && (left.BlendIndex2 == right.BlendIndex2) && (left.BlendIndex3 == right.BlendIndex3) && (left.BlendWeights == right.BlendWeights) && left.Color_A == right.Color_A && left.Color_R == right.Color_R && left.Color_G == right.Color_G && left.Color_B == right.Color_B); }
        public static bool operator !=(VertexPositionNormalTextureBlend left, VertexPositionNormalTextureBlend right) { return !(left == right); }
        public override bool Equals(object obj) { if (obj == null) { return false; } if (obj.GetType() != base.GetType()) { return false; } return (this == ((VertexPositionNormalTextureBlend)obj)); }
        public override string ToString() { return string.Format("{{Position:{0} Normal:{1} TextureCoordinate:{2} BlendIndices: {3} BlendWeights: {4}}}", new object[] { this.Position, this.Normal, this.TextureUV0, (BlendIndex0 + BlendIndex1 + BlendIndex2 + BlendIndex3), BlendWeights }); }
    }

}
