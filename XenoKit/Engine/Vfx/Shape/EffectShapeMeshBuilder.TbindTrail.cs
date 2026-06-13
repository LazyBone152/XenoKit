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
        private static List<EffectShapeSegment> BuildCurvedTrailSamples(IList<EffectShapeSegment> samples, int maxRenderSections, List<EffectShapeSegment> scratch)
        {
            if (samples.Count < 3)
                return ApplyRenderBudget(samples, maxRenderSections, scratch);

            scratch.Clear();
            int targetCapacity = ((samples.Count - 1) * TbindCurveSteps) + 1;

            if (scratch.Capacity < targetCapacity)
                scratch.Capacity = targetCapacity;

            for (int sampleIndex = 0; sampleIndex < samples.Count - 1; sampleIndex++)
            {
                EffectShapeSegment previous = samples[Math.Max(0, sampleIndex - 1)];
                EffectShapeSegment current = samples[sampleIndex];
                EffectShapeSegment next = samples[sampleIndex + 1];
                EffectShapeSegment after = samples[Math.Min(samples.Count - 1, sampleIndex + 2)];
                int stepCount = GetCurveStepCount(previous, current, next, after);

                if (sampleIndex == 0)
                    scratch.Add(current);

                for (int step = 1; step <= stepCount; step++)
                {
                    float factor = step / (float)stepCount;
                    scratch.Add(InterpolateTrailSample(previous, current, next, after, factor));
                }
            }

            CapInPlace(scratch, maxRenderSections);
            return scratch;
        }

        private static int GetCurveStepCount(EffectShapeSegment previous, EffectShapeSegment current, EffectShapeSegment next, EffectShapeSegment after)
        {
            float distance = SimdVector3.Distance(current.Transform.Translation, next.Transform.Translation);

            if (distance < 0.05f)
                return 1;

            float currentAngle = GetBendAngle(previous.Transform.Translation, current.Transform.Translation, next.Transform.Translation);
            float nextAngle = GetBendAngle(current.Transform.Translation, next.Transform.Translation, after.Transform.Translation);
            float angle = Math.Max(currentAngle, nextAngle);

            if (angle < 8f)
                return 1;

            if (angle < 25f)
                return 2;

            if (angle < 50f)
                return 3;

            return 4;
        }

        private static List<EffectShapeSegment> ApplyRenderBudget(IList<EffectShapeSegment> samples, int maxRenderSections, List<EffectShapeSegment> scratch)
        {
            int maxSamples = Math.Max(2, maxRenderSections + 1);

            scratch.Clear();

            if (samples.Count <= maxSamples)
            {
                if (scratch.Capacity < samples.Count)
                    scratch.Capacity = samples.Count;

                for (int i = 0; i < samples.Count; i++)
                    scratch.Add(samples[i]);

                return scratch;
            }

            if (scratch.Capacity < maxSamples)
                scratch.Capacity = maxSamples;

            int lastSourceIndex = samples.Count - 1;
            int lastAddedIndex = -1;

            for (int i = 0; i < maxSamples; i++)
            {
                float sourceIndex = i * lastSourceIndex / (float)(maxSamples - 1);
                int roundedIndex = (int)Math.Round(sourceIndex);

                if (roundedIndex == lastAddedIndex && roundedIndex < lastSourceIndex)
                    roundedIndex++;

                scratch.Add(samples[roundedIndex]);
                lastAddedIndex = roundedIndex;
            }

            if (scratch[scratch.Count - 1] != samples[lastSourceIndex])
                scratch[scratch.Count - 1] = samples[lastSourceIndex];

            return scratch;
        }

        private static void CapInPlace(List<EffectShapeSegment> samples, int maxRenderSections)
        {
            int maxSamples = Math.Max(2, maxRenderSections + 1);

            if (samples.Count <= maxSamples)
                return;

            EffectShapeSegment[] cappedSamples = new EffectShapeSegment[maxSamples];
            int lastSourceIndex = samples.Count - 1;
            int lastAddedIndex = -1;

            for (int i = 0; i < maxSamples; i++)
            {
                float sourceIndex = i * lastSourceIndex / (float)(maxSamples - 1);
                int roundedIndex = (int)Math.Round(sourceIndex);

                if (roundedIndex == lastAddedIndex && roundedIndex < lastSourceIndex)
                    roundedIndex++;

                cappedSamples[i] = samples[roundedIndex];
                lastAddedIndex = roundedIndex;
            }

            if (cappedSamples[cappedSamples.Length - 1] != samples[lastSourceIndex])
                cappedSamples[cappedSamples.Length - 1] = samples[lastSourceIndex];

            samples.Clear();
            samples.AddRange(cappedSamples);
        }

        private static EffectShapeSegment InterpolateTrailSample(EffectShapeSegment previous, EffectShapeSegment current, EffectShapeSegment next, EffectShapeSegment after, float factor)
        {
            Matrix4x4 transform = InterpolateTrailTransform(previous.Transform, current.Transform, next.Transform, after.Transform, factor);
            return CreateInterpolatedTrailSample(current, next, factor, transform);
        }

        private static EffectShapeSegment CreateInterpolatedTrailSample(EffectShapeSegment current, EffectShapeSegment next, float factor, Matrix4x4 transform)
        {
            return new EffectShapeSegment
            {
                Transform = transform,
                Age = Lerp(current.Age, next.Age, factor),
                CreatedFrame = Lerp(current.CreatedFrame, next.CreatedFrame, factor),
                ExpireFrame = Lerp(current.ExpireFrame, next.ExpireFrame, factor),
                Life = Lerp(current.Life, next.Life, factor),
                Scale = Lerp(current.Scale, next.Scale, factor),
                U = Lerp(current.U, next.U, factor),
                V = Lerp(current.V, next.V, factor),
                UvBaseU = Lerp(current.UvBaseU, next.UvBaseU, factor),
                UvBaseV = Lerp(current.UvBaseV, next.UvBaseV, factor),
                NormalizedTrailPosition = Lerp(current.NormalizedTrailPosition, next.NormalizedTrailPosition, factor),
                DistanceFromTail = Lerp(current.DistanceFromTail, next.DistanceFromTail, factor),
                DistanceFromHead = Lerp(current.DistanceFromHead, next.DistanceFromHead, factor),
                TrailLength = Lerp(current.TrailLength, next.TrailLength, factor),
                AlphaScale = Lerp(current.AlphaScale, next.AlphaScale, factor),
                IsBootstrapSeed = current.IsBootstrapSeed && next.IsBootstrapSeed,
                IsRenderOnlyHead = current.IsRenderOnlyHead || next.IsRenderOnlyHead,
                PrimaryColor = Color.Lerp(current.PrimaryColor, next.PrimaryColor, factor),
                SecondaryColor = Color.Lerp(current.SecondaryColor, next.SecondaryColor, factor)
            };
        }

        private static Matrix4x4 InterpolateTrailTransform(Matrix4x4 previous, Matrix4x4 current, Matrix4x4 next, Matrix4x4 after, float factor)
        {
            if (!Matrix4x4.Decompose(previous, out SimdVector3 previousScale, out System.Numerics.Quaternion previousRotation, out SimdVector3 previousPosition) ||
                !Matrix4x4.Decompose(current, out SimdVector3 currentScale, out System.Numerics.Quaternion currentRotation, out SimdVector3 currentPosition) ||
                !Matrix4x4.Decompose(next, out SimdVector3 nextScale, out System.Numerics.Quaternion nextRotation, out SimdVector3 nextPosition) ||
                !Matrix4x4.Decompose(after, out SimdVector3 afterScale, out System.Numerics.Quaternion afterRotation, out SimdVector3 afterPosition))
            {
                return Matrix4x4.Lerp(current, next, factor);
            }

            SimdVector3 position = CatmullRom(previousPosition, currentPosition, nextPosition, afterPosition, factor);
            SimdVector3 scale = CatmullRom(previousScale, currentScale, nextScale, afterScale, factor);
            System.Numerics.Quaternion rotation = System.Numerics.Quaternion.Slerp(currentRotation, nextRotation, factor);

            return Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position);
        }

        private static SimdVector3 CatmullRom(SimdVector3 previous, SimdVector3 current, SimdVector3 next, SimdVector3 after, float factor)
        {
            float squared = factor * factor;
            float cubed = squared * factor;

            return 0.5f * ((2f * current) +
                ((next - previous) * factor) +
                (((2f * previous) - (5f * current) + (4f * next) - after) * squared) +
                ((-previous + (3f * current) - (3f * next) + after) * cubed));
        }

        private static EffectShapeSegment ApplyInterpolatedPathProfile(EffectShapeSegment segment, EffectPathPoint path, float normalizedPosition, float pathScale)
        {
            Matrix4x4 pathTransform = Matrix4x4.CreateTranslation(new SimdVector3(path.Offset2, 0f, -path.Offset));
            Matrix4x4 transform = pathTransform * segment.Transform;

            return new EffectShapeSegment
            {
                Transform = transform,
                Age = segment.Age,
                CreatedFrame = segment.CreatedFrame,
                ExpireFrame = segment.ExpireFrame,
                Life = segment.Life,
                Scale = pathScale,
                U = segment.U,
                V = segment.V,
                UvBaseU = segment.UvBaseU,
                UvBaseV = segment.UvBaseV,
                NormalizedTrailPosition = normalizedPosition,
                DistanceFromTail = segment.DistanceFromTail,
                DistanceFromHead = segment.DistanceFromHead,
                TrailLength = segment.TrailLength,
                AlphaScale = segment.AlphaScale,
                IsBootstrapSeed = segment.IsBootstrapSeed,
                IsRenderOnlyHead = segment.IsRenderOnlyHead,
                PrimaryColor = segment.PrimaryColor,
                SecondaryColor = segment.SecondaryColor
            };
        }

        private static EffectPathPoint[] GetPathPoints(IList<EffectPathPoint> pathProfile, IList<float> pathPositions)
        {
            EffectPathPoint[] pathPoints = new EffectPathPoint[pathPositions.Count];

            for (int i = 0; i < pathPoints.Length; i++)
                pathPoints[i] = GetInterpolatedPathProfilePoint(pathProfile, pathPositions[i]);

            return pathPoints;
        }

        private static TbindRowFrame[] BuildAutoOrientedRowFrames(IList<EffectShapeSegment> rows, SimdVector3 cameraViewForward)
        {
            TbindRowFrame[] frames = new TbindRowFrame[rows.Count];
            SimdVector3[] centers = new SimdVector3[rows.Count];
            SimdVector3 previousTangent = SimdVector3.Zero;
            SimdVector3 previousWidthAxis = SimdVector3.Zero;
            SimdVector3 viewForward = NormalizeOrFallback(cameraViewForward, SimdVector3.UnitZ);

            for (int i = 0; i < rows.Count; i++)
                centers[i] = rows[i].Transform.Translation;

            for (int i = 0; i < rows.Count; i++)
            {
                EffectShapeSegment row = rows[i];
                SimdVector3 tangent = GetTrailTangentBasis(rows, centers, i, previousTangent);
                SimdVector3 widthAxis = GetAutoOrientedWidthAxis(row.Transform, tangent, viewForward, previousWidthAxis);
                SimdVector3 heightAxis = SimdVector3.Cross(widthAxis, tangent);

                if (!TryNormalize(heightAxis, out heightAxis))
                    heightAxis = GetTransformDirection(row.Transform, SimdVector3.UnitY);

                if (!TryNormalize(heightAxis, out heightAxis))
                    heightAxis = SimdVector3.UnitY;

                SimdVector3 center = centers[i];

                frames[i] = new TbindRowFrame(center, widthAxis, heightAxis);
                previousTangent = tangent;
                previousWidthAxis = widthAxis;
            }

            return frames;
        }

        private static SimdVector3 GetTrailTangentBasis(IList<EffectShapeSegment> rows, IList<SimdVector3> centers, int rowIndex, SimdVector3 previousTangent)
        {
            SimdVector3 tangent;

            if (rowIndex == 0)
                tangent = centers[1] - centers[0];
            else if (rowIndex == rows.Count - 1)
                tangent = centers[rowIndex] - centers[rowIndex - 1];
            else
                tangent = centers[rowIndex + 1] - centers[rowIndex - 1];

            if (TryNormalize(tangent, out tangent))
                return tangent;

            if (previousTangent.LengthSquared() > MinLengthSquared)
                return previousTangent;

            tangent = GetTransformDirection(rows[rowIndex].Transform, SimdVector3.UnitY);
            return TryNormalize(tangent, out tangent) ? tangent : SimdVector3.UnitY;
        }

        private static SimdVector3 GetAutoOrientedWidthAxis(Matrix4x4 transform, SimdVector3 tangent, SimdVector3 cameraViewForward, SimdVector3 previousWidthAxis)
        {
            SimdVector3 widthAxis = SimdVector3.Cross(tangent, cameraViewForward);

            if (widthAxis.LengthSquared() > 0.0001f)
            {
                widthAxis = SimdVector3.Normalize(widthAxis);

                if (previousWidthAxis.LengthSquared() > MinLengthSquared)
                {
                    if (SimdVector3.Dot(widthAxis, previousWidthAxis) < 0f)
                        widthAxis = -widthAxis;
                }

                return widthAxis;
            }

            if (TryGetProjectedPreviousWidth(tangent, previousWidthAxis, out widthAxis))
                return widthAxis;

            widthAxis = GetTransformDirection(transform, SimdVector3.UnitX);

            return TryNormalize(widthAxis, out widthAxis) ? widthAxis : SimdVector3.UnitX;
        }

        private static bool TryGetProjectedPreviousWidth(SimdVector3 tangent, SimdVector3 previousWidthAxis, out SimdVector3 widthAxis)
        {
            if (previousWidthAxis.LengthSquared() <= MinLengthSquared)
            {
                widthAxis = SimdVector3.Zero;
                return false;
            }

            widthAxis = previousWidthAxis - (tangent * SimdVector3.Dot(previousWidthAxis, tangent));
            return TryNormalize(widthAxis, out widthAxis);
        }

        private static SimdVector3 GetAutoOrientedPoint(EffectShapePoint point, float scale, int columnIndex, int columnCount, TbindRowFrame rowFrame)
        {
            if (columnCount == 2)
                return rowFrame.Center + (rowFrame.WidthAxis * point.X * scale);

            return rowFrame.Center + (rowFrame.WidthAxis * point.X * scale) + (rowFrame.HeightAxis * point.Y * scale);
        }

        private static SimdVector3 NormalizeOrFallback(SimdVector3 value, SimdVector3 fallback)
        {
            return TryNormalize(value, out SimdVector3 normalized) ? normalized : fallback;
        }

        private static bool TryNormalize(SimdVector3 value, out SimdVector3 normalized)
        {
            if (value.LengthSquared() < MinLengthSquared)
            {
                normalized = SimdVector3.Zero;
                return false;
            }

            normalized = SimdVector3.Normalize(value);
            return true;
        }

        private static SimdVector3 GetTransformDirection(Matrix4x4 transform, SimdVector3 direction)
        {
            return SimdVector3.TransformNormal(direction, transform);
        }

        private static float[] GetEffectivePathPositions(IList<EffectShapeSegment> samples, float retractionProgress)
        {
            float[] pathPositions = new float[samples.Count];
            bool shrinkPathProfile = retractionProgress > 0f;

            for (int i = 0; i < samples.Count; i++)
            {
                float pathPosition = samples[i].NormalizedTrailPosition;
                pathPositions[i] = shrinkPathProfile ? Lerp(pathPosition, 1f, retractionProgress) : pathPosition;
            }

            return pathPositions;
        }

        private static float[] GetPathScales(IList<EffectShapeSegment> samples, IList<EffectPathPoint> pathProfile, IList<float> pathPositions)
        {
            float[] scales = new float[samples.Count];

            for (int i = 0; i < samples.Count; i++)
                scales[i] = GetNormalPathScale(samples[i], pathProfile, pathPositions[i]);

            return scales;
        }

        private static float GetNormalPathScale(EffectShapeSegment segment, IList<EffectPathPoint> pathProfile, float pathPosition)
        {
            EffectPathPoint path = GetInterpolatedPathProfilePoint(pathProfile, pathPosition);
            return Math.Max(0.0001f, segment.Scale * (path.ScaleFactor + path.ScaleAdd));
        }

        private static void SetTrailDistances(IList<EffectShapeSegment> samples)
        {
            if (samples == null || samples.Count == 0)
                return;

            float totalDistance = 0f;
            samples[0].DistanceFromTail = 0f;

            for (int i = 1; i < samples.Count; i++)
            {
                totalDistance += SimdVector3.Distance(samples[i - 1].Transform.Translation, samples[i].Transform.Translation);
                samples[i].DistanceFromTail = totalDistance;
            }

            for (int i = 0; i < samples.Count; i++)
            {
                samples[i].TrailLength = totalDistance;
                samples[i].DistanceFromHead = totalDistance - samples[i].DistanceFromTail;
            }
        }

        private static float GetBendAngle(SimdVector3 previous, SimdVector3 current, SimdVector3 next)
        {
            SimdVector3 first = current - previous;
            SimdVector3 second = next - current;

            if (first.LengthSquared() < MinLengthSquared || second.LengthSquared() < MinLengthSquared)
                return 0f;

            first = SimdVector3.Normalize(first);
            second = SimdVector3.Normalize(second);
            float dot = Math.Max(-1f, Math.Min(1f, SimdVector3.Dot(first, second)));
            return MathHelper.ToDegrees((float)Math.Acos(dot));
        }

        private static EffectPathPoint GetInterpolatedPathProfilePoint(IList<EffectPathPoint> pathProfile, float normalizedPosition)
        {
            if (pathProfile == null || pathProfile.Count == 0)
                return new EffectPathPoint(1f, 0f, 0f, 0f);

            if (pathProfile.Count == 1)
                return pathProfile[0];

            float clamped = Math.Max(0f, Math.Min(1f, normalizedPosition));
            float pathIndex = clamped * (pathProfile.Count - 1);
            int lowerIndex = (int)Math.Floor(pathIndex);
            int upperIndex = Math.Min(pathProfile.Count - 1, lowerIndex + 1);
            float factor = pathIndex - lowerIndex;

            EffectPathPoint lower = pathProfile[Math.Max(0, lowerIndex)];
            EffectPathPoint upper = pathProfile[upperIndex];

            return new EffectPathPoint(
                Lerp(lower.ScaleFactor, upper.ScaleFactor, factor),
                Lerp(lower.ScaleAdd, upper.ScaleAdd, factor),
                Lerp(lower.Offset, upper.Offset, factor),
                Lerp(lower.Offset2, upper.Offset2, factor));
        }

        private struct TbindRowFrame
        {
            public SimdVector3 Center { get; }
            public SimdVector3 WidthAxis { get; }
            public SimdVector3 HeightAxis { get; }

            public TbindRowFrame(SimdVector3 center, SimdVector3 widthAxis, SimdVector3 heightAxis)
            {
                Center = center;
                WidthAxis = widthAxis;
                HeightAxis = heightAxis;
            }
        }

    }
}
