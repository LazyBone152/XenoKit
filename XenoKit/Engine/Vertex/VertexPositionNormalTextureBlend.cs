using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XenoKit.Engine.Vertex
{
    public struct VertexPositionNormalTextureBlend : IVertexType
    {
        public static readonly VertexDeclaration VertexDeclaration;
        public Vector3 Position;            //0 (size: 12)
        public Vector3 Normal;              //12 (size: 12)
        public Vector2 TextureCoordinate;   //24 (size: 8)
        public short BlendIndex0;           // 32 (size: 2)
        public short BlendIndex1;           // 34 (size: 2)
        public short BlendIndex2;           // 36 (size: 2)
        public short BlendIndex3;           // 38 (size: 2)
        public Vector4 BlendWeights;        //40 (size: 16)

        VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }



        public VertexPositionNormalTextureBlend(Vector3 position, Vector3 normal, Vector2 textureCoordinate, short blendIndex0, short blendIndex1, short blendIndex2, short blendIndex3, Vector4 blendWeights)
        {
            Position = position;
            Normal = normal;
            TextureCoordinate = textureCoordinate;
            BlendIndex0 = blendIndex0;
            BlendIndex1 = blendIndex1;
            BlendIndex2 = blendIndex2;
            BlendIndex3 = blendIndex3;
            BlendWeights = blendWeights;
        }
        static VertexPositionNormalTextureBlend()
        {
            VertexElement[] elements = new VertexElement[] { new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0), new VertexElement(0x18, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0), new VertexElement(0x20, VertexElementFormat.Short4, VertexElementUsage.BlendIndices, 0), new VertexElement(0x28, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0) };
            VertexDeclaration declaration = new VertexDeclaration(elements);
            VertexDeclaration = declaration;
        }

        public override int GetHashCode() { return 0; } // TODO: FIc gethashcode        
        public static bool operator ==(VertexPositionNormalTextureBlend left, VertexPositionNormalTextureBlend right) { return (((left.Position == right.Position) && (left.Normal == right.Normal)) && (left.TextureCoordinate == right.TextureCoordinate) && (left.BlendIndex0 == right.BlendIndex0) && (left.BlendIndex1 == right.BlendIndex1) && (left.BlendIndex2 == right.BlendIndex2) && (left.BlendIndex3 == right.BlendIndex3) && (left.BlendWeights == right.BlendWeights)); }
        public static bool operator !=(VertexPositionNormalTextureBlend left, VertexPositionNormalTextureBlend right) { return !(left == right); }
        public override bool Equals(object obj) { if (obj == null) { return false; } if (obj.GetType() != base.GetType()) { return false; } return (this == ((VertexPositionNormalTextureBlend)obj)); }
        public override string ToString() { return string.Format("{{Position:{0} Normal:{1} TextureCoordinate:{2} BlendIndices: {3} BlendWeights: {4}}}", new object[] { this.Position, this.Normal, this.TextureCoordinate, (BlendIndex0 + BlendIndex1 + BlendIndex2 + BlendIndex3), BlendWeights }); }
    }

}
