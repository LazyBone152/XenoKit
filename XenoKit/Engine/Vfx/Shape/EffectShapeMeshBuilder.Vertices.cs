using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using XenoKit.Engine.Vertex;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx.Shape
{
    public static partial class EffectShapeMeshBuilder
    {
        private static void AddDoubleSidedQuad(List<VertexPositionTextureColor> vertices, VertexPositionTextureColor topLeft, VertexPositionTextureColor topRight, VertexPositionTextureColor bottomLeft, VertexPositionTextureColor bottomRight)
        {
            AddDoubleSidedTriangle(vertices, topLeft, topRight, bottomLeft);
            AddDoubleSidedTriangle(vertices, bottomLeft, topRight, bottomRight);
        }

        private static void AddDoubleSidedTriangle(List<VertexPositionTextureColor> vertices, VertexPositionTextureColor a, VertexPositionTextureColor b, VertexPositionTextureColor c)
        {
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(c);
            vertices.Add(b);
            vertices.Add(a);
        }

        private static bool AreClose(EffectShapePoint a, EffectShapePoint b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return dx * dx + dy * dy < 0.0001f;
        }

        private static Color ApplyAlphaScale(Color color, float alphaScale)
        {
            if (alphaScale >= 0.999f)
                return color;

            Vector4 vector = color.ToVector4();
            vector.W *= Math.Max(0f, Math.Min(1f, alphaScale));
            return new Color(vector);
        }

        private static float Lerp(float start, float end, float factor)
        {
            return start + ((end - start) * factor);
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Math.Abs(divisor) < 0.000001f ? 0f : value / divisor;
        }

        private static VertexPositionTextureColor CreateVertex(EffectShapePoint point, Matrix4x4 world, Color color, float u, float v)
        {
            return CreateVertex(new SimdVector3(point.X, point.Y, 0f), world, color, u, v);
        }

        private static VertexPositionTextureColor CreateVertex(SimdVector3 point, Matrix4x4 world, Color color, float u, float v)
        {
            SimdVector3 position = SimdVector3.Transform(point, world);
            return CreateWorldVertex(position, color, u, v);
        }

        private static VertexPositionTextureColor CreateWorldVertex(SimdVector3 position, Color color, float u, float v)
        {
            return new VertexPositionTextureColor(new Vector3(position.X, position.Y, position.Z), new Vector2(u, v), color.ToVector4());
        }

        private struct VertexPair
        {
            public VertexPositionTextureColor Top { get; set; }
            public VertexPositionTextureColor Bottom { get; set; }
        }

    }
}
