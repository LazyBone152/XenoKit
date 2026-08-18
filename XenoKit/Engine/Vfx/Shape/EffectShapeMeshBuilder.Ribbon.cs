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
        private static void AddRibbonQuad(List<VertexPositionTextureColor> vertices, SimdVector3 start, SimdVector3 end, SimdVector3 normal, float halfWidth, Matrix4x4 world, float startU, float endU, float topV, float bottomV, Color topColor, Color bottomColor)
        {
            SimdVector3 offset = normal * halfWidth;
            VertexPositionTextureColor startTop = CreateVertex(start + offset, world, topColor, startU, topV);
            VertexPositionTextureColor endTop = CreateVertex(end + offset, world, topColor, endU, topV);
            VertexPositionTextureColor startBottom = CreateVertex(start - offset, world, bottomColor, startU, bottomV);
            VertexPositionTextureColor endBottom = CreateVertex(end - offset, world, bottomColor, endU, bottomV);

            AddDoubleSidedQuad(vertices, startTop, endTop, startBottom, endBottom);
        }

        private static List<float> GetShapeDistances(IList<EffectShapePoint> points, int pointCount, bool closed)
        {
            int segmentCount = closed ? pointCount : pointCount - 1;
            List<float> distances = new List<float>(segmentCount + 1) { 0f };
            float total = 0f;

            for (int i = 0; i < segmentCount; i++)
            {
                int nextIndex = (i + 1) % pointCount;
                float dx = points[nextIndex].X - points[i].X;
                float dy = points[nextIndex].Y - points[i].Y;
                total += (float)Math.Sqrt(dx * dx + dy * dy);
                distances.Add(total);
            }

            return distances;
        }

        private static SimdVector3 Get2DNormal(SimdVector3 start, SimdVector3 end)
        {
            SimdVector3 direction = end - start;

            if (direction.LengthSquared() < MinLengthSquared)
                return SimdVector3.UnitX;

            direction = SimdVector3.Normalize(direction);
            return new SimdVector3(-direction.Y, direction.X, 0f);
        }

        private static SimdVector3 GetRibbonNormal(SimdVector3 start, SimdVector3 end)
        {
            SimdVector3 direction = end - start;

            if (direction.LengthSquared() < MinLengthSquared)
                return SimdVector3.UnitX;

            direction = SimdVector3.Normalize(direction);
            SimdVector3 normal = SimdVector3.Cross(direction, SimdVector3.UnitY);

            if (normal.LengthSquared() < MinLengthSquared)
                normal = SimdVector3.UnitX;

            return SimdVector3.Normalize(normal);
        }

        private static SimdVector3 GetJoinOffset(IList<EffectShapePoint> points, int pointCount, int index, bool closed, float halfWidth)
        {
            SimdVector3 current = ToVector(points[index]);
            SimdVector3 prev = index == 0 ? ToVector(points[closed ? pointCount - 1 : 0]) : ToVector(points[index - 1]);
            SimdVector3 next = index == pointCount - 1 ? ToVector(points[closed ? 0 : pointCount - 1]) : ToVector(points[index + 1]);

            if (!closed && index == 0)
                return Get2DNormal(current, next) * halfWidth;

            if (!closed && index == pointCount - 1)
                return Get2DNormal(prev, current) * halfWidth;

            SimdVector3 prevNormal = Get2DNormal(prev, current);
            SimdVector3 nextNormal = Get2DNormal(current, next);
            SimdVector3 miter = prevNormal + nextNormal;

            if (miter.LengthSquared() < MinLengthSquared)
                return nextNormal * halfWidth;

            miter = SimdVector3.Normalize(miter);
            float denominator = SimdVector3.Dot(miter, nextNormal);

            if (Math.Abs(denominator) < 0.1f)
                return nextNormal * halfWidth;

            float miterLength = halfWidth / denominator;
            float maxLength = halfWidth * MiterLimit;

            if (Math.Abs(miterLength) > maxLength)
                miterLength = Math.Sign(miterLength) * maxLength;

            return miter * miterLength;
        }

        private static List<EffectShapePoint> ScaleShape(IList<EffectShapePoint> shape, float scale)
        {
            List<EffectShapePoint> scaledShape = new List<EffectShapePoint>(shape.Count);

            foreach (EffectShapePoint point in shape)
                scaledShape.Add(Scale(point, scale));

            return scaledShape;
        }

        private static EffectShapePoint Scale(EffectShapePoint point, float scale)
        {
            return new EffectShapePoint(point.X * scale, point.Y * scale);
        }

        private static SimdVector3 ToVector(EffectShapePoint point)
        {
            return new SimdVector3(point.X, point.Y, 0f);
        }

    }
}
