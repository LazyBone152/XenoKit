using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using XenoKit.Engine.Vertex;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx.Shape
{
    internal enum ShapeDrawStripMode
    {
        UprightWidth,
        PathNormalWidth,
        PathNormalDepthBand,
        PathNormalGroundBand
    }

    public static partial class EffectShapeMeshBuilder
    {
        private const float MinLengthSquared = 0.000001f;
        private const float MiterLimit = 4f;
        private const int TbindCurveSteps = 4;

        internal static EffectShapeMeshData BuildShapeDrawRibbonMesh(IList<EffectShapePoint> points, Matrix4x4 world, float halfWidth, float depthWidth, float scrollU, float scrollV, float stepU, float stepV, Color topColor, Color bottomColor, bool closed, ShapeDrawStripMode mode)
        {
            if (points == null || points.Count < 2)
                return new EffectShapeMeshData(new VertexPositionTextureColor[0], null, PrimitiveType.TriangleStrip, 0);

            bool sourceCloses = closed && points.Count > 2 && AreClose(points[0], points[points.Count - 1]);
            int pointCount = sourceCloses ? points.Count - 1 : points.Count;
            bool isClosed = sourceCloses && pointCount > 2;

            if (pointCount < 2)
                return new EffectShapeMeshData(new VertexPositionTextureColor[0], null, PrimitiveType.TriangleStrip, 0);

            List<float> distances = GetShapeDistances(points, pointCount, isClosed);
            float totalDistance = distances[distances.Count - 1];
            float width = Math.Max(0.0001f, halfWidth);
            int pairCount = isClosed ? pointCount + 1 : pointCount;
            List<VertexPositionTextureColor> vertices = new List<VertexPositionTextureColor>((pairCount * 2) + 2);
            VertexPositionTextureColor lastBottom = default(VertexPositionTextureColor);

            for (int i = 0; i < pointCount; i++)
            {
                GetShapeDrawPair(points, pointCount, i, isClosed, width, depthWidth, mode, out SimdVector3 topPosition, out SimdVector3 bottomPosition);
                float u = WrapUnit(scrollU + (stepU * SafeDivide(distances[i], totalDistance)));
                VertexPositionTextureColor top = CreateVertex(topPosition, world, topColor, u, scrollV);
                VertexPositionTextureColor bottom = CreateVertex(bottomPosition, world, bottomColor, u, scrollV + stepV);

                vertices.Add(top);
                vertices.Add(bottom);
                lastBottom = bottom;
            }

            if (isClosed)
            {
                GetShapeDrawPair(points, pointCount, 0, isClosed, width, depthWidth, mode, out SimdVector3 topPosition, out SimdVector3 bottomPosition);
                float u = WrapUnit(scrollU + stepU);
                VertexPositionTextureColor top = CreateVertex(topPosition, world, topColor, u, scrollV);
                VertexPositionTextureColor bottom = CreateVertex(bottomPosition, world, bottomColor, u, scrollV + stepV);

                vertices.Add(top);
                vertices.Add(bottom);
                lastBottom = bottom;
            }

            vertices.Add(lastBottom);
            vertices.Add(lastBottom);

            return new EffectShapeMeshData(vertices.ToArray(), null, PrimitiveType.TriangleStrip, Math.Max(0, vertices.Count - 2));
        }

        private static void GetShapeDrawPair(IList<EffectShapePoint> points, int pointCount, int index, bool isClosed, float width, float depthWidth, ShapeDrawStripMode mode, out SimdVector3 topPosition, out SimdVector3 bottomPosition)
        {
            SimdVector3 center;
            SimdVector3 offset;

            if (mode == ShapeDrawStripMode.PathNormalDepthBand)
            {
                center = ToVector(points[index]);
                offset = GetJoinOffset(points, pointCount, index, isClosed, width);
                SimdVector3 depth = new SimdVector3(0f, 0f, depthWidth);
                topPosition = center + offset - (depth * 0.5f);
                bottomPosition = center - offset + (depth * 0.5f);
                return;
            }

            if (mode == ShapeDrawStripMode.PathNormalGroundBand)
            {
                center = new SimdVector3(points[index].X, 0f, points[index].Y);
                SimdVector3 joinOffset = GetJoinOffset(points, pointCount, index, isClosed, width);
                offset = new SimdVector3(joinOffset.X, 0f, joinOffset.Y);
                SimdVector3 depth = new SimdVector3(0f, depthWidth, 0f);
                topPosition = center + offset + (depth * 0.5f);
                bottomPosition = center - offset - (depth * 0.5f);
                return;
            }

            if (mode == ShapeDrawStripMode.PathNormalWidth)
            {
                center = ToVector(points[index]);
                offset = GetJoinOffset(points, pointCount, index, isClosed, width);
                topPosition = center + offset;
                bottomPosition = center - offset;
                return;
            }

            center = new SimdVector3(points[index].X, 0f, points[index].Y);
            offset = new SimdVector3(0f, width, 0f);
            topPosition = center + offset;
            bottomPosition = center - offset;
        }

        private static float WrapUnit(float value)
        {
            value = value - (float)Math.Floor(value);
            return value == 0f ? 0f : value;
        }

        internal static EffectShapeMeshData BuildConeExtrudeRibbonMesh(IList<EffectRibbonPoint> points, Matrix4x4 world, float scrollU, float scrollV, float stepU, float stepV)
        {
            if (points == null || points.Count < 2)
                return new EffectShapeMeshData(new VertexPositionTextureColor[0], null, PrimitiveType.TriangleStrip, 0);

            List<VertexPositionTextureColor> vertices = new List<VertexPositionTextureColor>((points.Count * 2) + 2);
            VertexPositionTextureColor lastBottom = default(VertexPositionTextureColor);

            for (int i = 0; i < points.Count; i++)
            {
                EffectRibbonPoint point = points[i];
                float v = scrollV + (stepV * point.U);
                float topWidth = Math.Max(0.0001f, point.HalfWidth);
                float bottomWidth = Math.Max(0.0001f, point.BottomWidth);
                VertexPositionTextureColor top = CreateVertex(point.Position + new SimdVector3(topWidth, 0f, 0f), world, point.TopColor, scrollU, v);
                VertexPositionTextureColor bottom = CreateVertex(point.Position - new SimdVector3(bottomWidth, 0f, 0f), world, point.BottomColor, scrollU + stepU, v);

                vertices.Add(top);
                vertices.Add(bottom);
                lastBottom = bottom;
            }

            vertices.Add(lastBottom);
            vertices.Add(lastBottom);

            return new EffectShapeMeshData(vertices.ToArray(), null, PrimitiveType.TriangleStrip, Math.Max(0, vertices.Count - 2));
        }

        internal static EffectShapeMeshData BuildTbindTrailMesh(IList<EffectShapePoint> shape, IList<EffectShapeSegment> samples, IList<EffectPathPoint> pathProfile, float retractionProgress, int maxRenderSections, bool autoOrientation, bool usePathOffsetAsWidth, SimdVector3 cameraViewForward, float uvScrollU, float uvScrollV, float uvStepU, float uvStepV, List<EffectShapeSegment> profiledScratch, List<EffectShapeSegment> meshScratch)
        {
            if (shape == null || shape.Count < 2 || samples == null || samples.Count < 2)
                return CreateEmptyTbindMesh();

            List<EffectShapeSegment> baseRows = pathProfile != null && pathProfile.Count > 1
                ? BuildPathProfileRows(samples, pathProfile.Count, maxRenderSections, profiledScratch)
                : ApplyRenderBudget(samples, maxRenderSections, profiledScratch);
            List<EffectShapeSegment> profiledSamples = meshScratch;
            float[] pathPositions = GetEffectivePathPositions(baseRows, retractionProgress);
            float[] pathScales = GetPathScales(baseRows, pathProfile, pathPositions);
            EffectPathPoint[] pathPoints = GetPathPoints(pathProfile, pathPositions);

            profiledSamples.Clear();

            for (int i = 0; i < baseRows.Count; i++)
            {
                EffectShapeSegment sample = baseRows[i];
                profiledSamples.Add(usePathOffsetAsWidth
                    ? ApplyPathScale(sample, pathPositions[i], pathScales[i])
                    : ApplyInterpolatedPathProfile(sample, pathPoints[i], pathPositions[i], pathScales[i]));
            }

            SetTrailDistances(profiledSamples);

            int rowCount = profiledSamples.Count;
            int columnCount = shape.Count;
            int vertexCount = rowCount * columnCount;

            if (vertexCount > ushort.MaxValue)
                throw new InvalidOperationException($"TBIND strip mesh has {vertexCount} vertices, which exceeds the 16-bit index limit.");

            VertexPositionTextureColor[] vertices = new VertexPositionTextureColor[vertexCount];
            TbindRowFrame[] rowFrames = autoOrientation ? BuildAutoOrientedRowFrames(profiledSamples, cameraViewForward) : null;

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                EffectShapeSegment row = profiledSamples[rowIndex];

                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    int vertexIndex = (rowIndex * columnCount) + columnIndex;
                    Vector2 uv = GetTbindGridUv(profiledSamples[0], row, rowIndex, rowCount, columnIndex, columnCount, uvScrollU, uvScrollV, uvStepU, uvStepV);
                    Color color = GetTbindColumnColor(row, columnIndex, columnCount);

                    if (usePathOffsetAsWidth && columnCount == 2)
                    {
                        SimdVector3 point = GetPathWidthPoint(shape[columnIndex], row.Scale, pathPoints[rowIndex], row.Transform);
                        vertices[vertexIndex] = CreateWorldVertex(point, color, uv.X, uv.Y);
                    }
                    else if (autoOrientation)
                    {
                        SimdVector3 point = GetAutoOrientedPoint(shape[columnIndex], row.Scale, columnIndex, columnCount, rowFrames[rowIndex]);
                        vertices[vertexIndex] = CreateWorldVertex(point, color, uv.X, uv.Y);
                    }
                    else
                    {
                        vertices[vertexIndex] = CreateVertex(Scale(shape[columnIndex], row.Scale), row.Transform, color, uv.X, uv.Y);
                    }
                }
            }

            if (columnCount > 2)
            {
                ushort[] indices = BuildTbindGridIndices(rowCount, columnCount);
                return new EffectShapeMeshData(vertices, indices, PrimitiveType.TriangleStrip, Math.Max(0, indices.Length - 2));
            }

            return new EffectShapeMeshData(vertices, null, PrimitiveType.TriangleStrip, Math.Max(0, vertices.Length - 2));
        }

        private static EffectShapeMeshData CreateEmptyTbindMesh()
        {
            return new EffectShapeMeshData(new VertexPositionTextureColor[0], null, PrimitiveType.TriangleStrip, 0);
        }

        private static ushort[] BuildTbindGridIndices(int rowCount, int columnCount)
        {
            ushort[] indices = new ushort[(rowCount - 1) * columnCount * 2];
            int index = 0;

            for (int rowIndex = 0; rowIndex < rowCount - 1; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    indices[index++] = (ushort)((rowIndex * columnCount) + columnIndex);
                    indices[index++] = (ushort)(((rowIndex + 1) * columnCount) + columnIndex);
                }
            }

            return indices;
        }

        private static Color GetTbindColumnColor(EffectShapeSegment segment, int columnIndex, int columnCount)
        {
            if (columnCount == 2 && columnIndex == 1)
                return ApplyAlphaScale(segment.SecondaryColor, segment.AlphaScale);

            return ApplyAlphaScale(segment.PrimaryColor, segment.AlphaScale);
        }

        private static Vector2 GetTbindGridUv(EffectShapeSegment firstRow, EffectShapeSegment row, int rowIndex, int rowCount, int columnIndex, int columnCount, float scrollU, float scrollV, float stepU, float stepV)
        {
            if (columnCount == 2)
            {
                float baseU = SafeDivide(columnIndex, columnCount - 1);
                float baseV = SafeDivide(rowIndex, rowCount - 1);
                return new Vector2(scrollU + (stepU * baseU), scrollV + (stepV * baseV));
            }

            float u = firstRow.UvBaseU + SafeDivide(columnIndex, columnCount - 1);
            float v = firstRow.UvBaseV + rowIndex;
            return new Vector2(scrollU + (stepU * u), scrollV + (stepV * v));
        }

        public static List<EffectShapePoint> CreateDefaultRibbonShape()
        {
            return new List<EffectShapePoint>
            {
                new EffectShapePoint(-0.5f, 0f),
                new EffectShapePoint(0.5f, 0f)
            };
        }
    }
}
