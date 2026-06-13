using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using XenoKit.Engine.Vfx.Shape;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EMP_NEW.Keyframes;
using Xv2CoreLib.ETR;
using Xv2CoreLib.Resource;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx.Asset
{
    public partial class VfxTbind : VfxAsset
    {
        private void EmitDueSamples(ETR_Node node, TbindNodeState state, float nodeFrame)
        {
            float segmentDelay = GetSegmentDelay(node);

            if (!state.Started)
            {
                state.Started = true;
                state.NextEmitFrame = 0f;
            }

            if (node.ExtrudeDuration == 0)
            {
                if (state.Samples.Count == 0)
                    BootstrapTrailSamples(node, state, segmentDelay, true, false);

                state.FinishedExtruding = true;
                return;
            }

            bool useContinuousTrail = ShouldUseContinuousTrail(node) || node.ExtrudeDuration < 0;
            float extrudeEnd = useContinuousTrail ? nodeFrame : node.ExtrudeDuration;

            if (state.Samples.Count == 0 && nodeFrame >= 0f)
            {
                bool includeSecondSample = useContinuousTrail || segmentDelay <= extrudeEnd;
                BootstrapTrailSamples(node, state, segmentDelay, includeSecondSample, useContinuousTrail);
            }

            while (nodeFrame >= state.NextEmitFrame && state.NextEmitFrame <= extrudeEnd)
            {
                AddOrReplaceDueSample(node, state, state.NextEmitFrame);
                state.NextEmitFrame += segmentDelay;
            }

            while (state.Samples.Count > MaxTrailSamples)
                state.Samples.RemoveAt(0);
        }

        private void BootstrapTrailSamples(ETR_Node node, TbindNodeState state, float segmentDelay, bool includeSecondSample, bool secondSampleIsSeed)
        {
            state.Samples.Add(CreateTrailSample(node, state, 0f));

            if (includeSecondSample)
            {
                state.Samples.Add(CreateTrailSample(
                    node,
                    state,
                    segmentDelay,
                    secondSampleIsSeed,
                    secondSampleIsSeed ? 0f : (float?)null));
            }

            state.NextEmitFrame = segmentDelay;
        }

        private void AddOrReplaceDueSample(ETR_Node node, TbindNodeState state, float sampleFrame)
        {
            int lastIndex = state.Samples.Count - 1;

            if (lastIndex >= 0 &&
                state.Samples[lastIndex].IsBootstrapSeed &&
                System.Math.Abs(state.Samples[lastIndex].NodeFrame - sampleFrame) < 0.001f)
            {
                state.Samples[lastIndex] = CreateTrailSample(node, state, sampleFrame);
                return;
            }

            state.Samples.Add(CreateTrailSample(node, state, sampleFrame));
        }

        private TbindTrailSample CreateTrailSample(ETR_Node node, TbindNodeState state, float sampleFrame, bool isBootstrapSeed = false, float? createdFrame = null)
        {
            Matrix4x4 attachTransform = state.GetAttachTransform();
            Matrix4x4 transform = CreateNodeTransform(node, attachTransform, sampleFrame);
            Color primary = GetPrimaryColor(node, state, sampleFrame);
            Color secondary = GetSecondaryColor(node, state, sampleFrame, primary);

            float actualCreatedFrame = createdFrame ?? sampleFrame;
            float expireFrame = GetSampleExpireFrame(node, sampleFrame, actualCreatedFrame);

            if (isBootstrapSeed)
                expireFrame = System.Math.Max(expireFrame, sampleFrame);

            // Samples only keep the followed transform. ETR position, rotation, scale, color, and path are applied at draw time so keyframes affect the whole trail.
            return new TbindTrailSample
            {
                Transform = transform,
                NodeFrame = sampleFrame,
                CreatedFrame = actualCreatedFrame,
                ExpireFrame = expireFrame,
                Scale = 1f,
                EmittedV = GetSegmentV(node, sampleFrame),
                IsBootstrapSeed = isBootstrapSeed,
                PrimaryColor = primary,
                SecondaryColor = secondary
            };
        }

        private float GetSampleExpireFrame(ETR_Node node, float sampleFrame, float createdFrame)
        {
            if (UsesBeamTbindMode(node) && (ShouldUseContinuousTrail(node) || node.ExtrudeDuration < 0))
                return float.MaxValue;

            if (ShouldUseContinuousTrail(node) || node.ExtrudeDuration < 0)
                return createdFrame + GetSampleLifetimeFrames(node);

            return sampleFrame + node.HoldDuration;
        }

        private float GetSampleLifetimeFrames(ETR_Node node)
        {
            return System.Math.Max(System.Math.Max(1f, node.HoldDuration), GetSegmentDelay(node));
        }

        private static float GetHoldFrames(ETR_Node node)
        {
            return System.Math.Max(1, (int)node.HoldDuration);
        }

        private void StartRetraction(ETR_Node node, TbindNodeState state, float nodeFrame)
        {
            if (state.IsRetracting)
                return;

            state.StopMode = TbindStopMode.Retract;
            state.StopStartFrame = nodeFrame;

            if (state.Samples.Count == 0)
                return;

            float duration = GetRetractionDuration(node);
            state.IsRetracting = true;
            state.RetractionStartFrame = nodeFrame;
            state.RetractionEndFrame = nodeFrame + duration;
            state.RetractionStartSampleCount = state.Samples.Count;
        }

        private static void StartNaturalStop(ETR_Node node, TbindNodeState state, float nodeFrame)
        {
            state.StopMode = TbindStopMode.Natural;
            state.StopStartFrame = nodeFrame;
            state.IsRetracting = false;
            state.RetractionStartFrame = 0f;
            state.RetractionEndFrame = 0f;
            state.RetractionStartSampleCount = 0;

            float stopExpireFrame = nodeFrame + GetHoldFrames(node);

            for (int i = 0; i < state.Samples.Count; i++)
            {
                if (float.IsPositiveInfinity(state.Samples[i].ExpireFrame) || state.Samples[i].ExpireFrame == float.MaxValue)
                {
                    state.Samples[i].ExpireFrame = stopExpireFrame;
                }
                else
                {
                    state.Samples[i].ExpireFrame = System.Math.Min(state.Samples[i].ExpireFrame, stopExpireFrame);
                }
            }
        }

        private static void PruneExpiredSamples(TbindNodeState state, float nodeFrame)
        {
            if (state.StopMode == TbindStopMode.Retract || state.IsRetracting)
            {
                if (GetRetractionProgress(state, nodeFrame) >= 1f)
                    state.Samples.Clear();

                return;
            }

            for (int i = state.Samples.Count - 1; i >= 0; i--)
            {
                if (state.Samples[i].ExpireFrame < nodeFrame)
                    state.Samples.RemoveAt(i);
            }
        }

        private static float GetRetractionProgress(TbindNodeState state, float nodeFrame)
        {
            float duration = System.Math.Max(1f, state.RetractionEndFrame - state.RetractionStartFrame);
            return MathHelper.Clamp((nodeFrame - state.RetractionStartFrame) / duration, 0f, 1f);
        }

        private List<EffectShapeSegment> CreateDrawSegments(ETR_Node node, TbindNodeState state, float nodeFrame)
        {
            List<TbindTrailSample> samples = CreateRenderSamples(node, state, nodeFrame);
            List<EffectShapeSegment> segments = state.SourceSegmentsScratch;
            segments.Clear();

            if (segments.Capacity < samples.Count)
                segments.Capacity = samples.Count;

            float maxIndex = System.Math.Max(1, samples.Count - 1);
            float scale = GetNodeScale(node, state, nodeFrame);

            for (int i = 0; i < samples.Count; i++)
            {
                TbindTrailSample sample = samples[i];
                float normalizedPosition = samples.Count == 1 ? 0f : i / maxIndex;
                segments.Add(CreateDrawSegment(node, state, sample, CreateDrawTransform(node, sample.Transform, nodeFrame), normalizedPosition, scale, nodeFrame));
            }

            state.LastRenderSegmentCount = segments.Count;
            return segments;
        }

        private List<TbindTrailSample> CreateRenderSamples(ETR_Node node, TbindNodeState state, float nodeFrame)
        {
            List<TbindTrailSample> renderSamples = state.RenderSamplesScratch;
            renderSamples.Clear();
            state.HasRenderOnlyHead = false;
            state.RenderOnlyHeadFrame = 0f;

            if (state.Samples.Count == 0)
                return renderSamples;

            bool isActivelyExtruding = !UsesBeamTbindMode(node) && !state.IsRetracting && GetNodePhase(node, nodeFrame) == TbindNodePhase.Extruding;
            int lastIndex = state.Samples.Count - 1;
            TbindTrailSample lastSample = state.Samples[lastIndex];
            bool replaceBootstrapSeed = isActivelyExtruding &&
                lastSample.IsBootstrapSeed &&
                state.Samples.Count > 1 &&
                nodeFrame > state.Samples[lastIndex - 1].NodeFrame &&
                nodeFrame < lastSample.NodeFrame;
            bool appendRenderHead = isActivelyExtruding &&
                !replaceBootstrapSeed &&
                nodeFrame >= lastSample.NodeFrame &&
                (state.Samples.Count == 1 || nodeFrame > lastSample.NodeFrame + 0.001f);

            int copyCount = replaceBootstrapSeed ? lastIndex : state.Samples.Count;

            for (int i = 0; i < copyCount; i++)
                renderSamples.Add(state.Samples[i]);

            if (replaceBootstrapSeed || appendRenderHead)
            {
                TbindTrailSample renderHead = CreateTrailSample(node, state, nodeFrame, false, nodeFrame);
                renderHead.IsRenderOnlyHead = true;
                renderSamples.Add(renderHead);
                state.HasRenderOnlyHead = true;
                state.RenderOnlyHeadFrame = nodeFrame;
            }

            return renderSamples;
        }

        private EffectShapeSegment CreateDrawSegment(ETR_Node node, TbindNodeState state, TbindTrailSample sample, Matrix4x4 transform, float normalizedPosition, float scale, float nodeFrame)
        {
            float pathPosition = GetSamplePathPosition(node, sample, normalizedPosition);
            Color primary = sample.PrimaryColor;
            Color secondary = sample.SecondaryColor;

            if (state.IsRetracting)
            {
                primary = GetPrimaryColor(node, state, nodeFrame);
                secondary = GetSecondaryColor(node, state, nodeFrame, primary);
            }

            return new EffectShapeSegment
            {
                Transform = transform,
                Age = sample.NodeFrame,
                CreatedFrame = sample.CreatedFrame,
                ExpireFrame = sample.ExpireFrame,
                Life = System.Math.Max(1, GetTimelineLength(node)),
                Scale = scale,
                U = normalizedPosition,
                V = GetSegmentV(node, sample.NodeFrame),
                UvBaseU = 0f,
                UvBaseV = sample.EmittedV,
                NormalizedTrailPosition = pathPosition,
                AlphaScale = 1f,
                PrimaryColor = primary,
                SecondaryColor = secondary,
                IsBootstrapSeed = sample.IsBootstrapSeed,
                IsRenderOnlyHead = sample.IsRenderOnlyHead
            };
        }

        private static List<EffectShapeSegment> CreateVisibleSegments(List<EffectShapeSegment> sourceSegments)
        {
            SetVisibleTrailPositions(sourceSegments);
            return sourceSegments;
        }

        private static void SetVisibleTrailPositions(IList<EffectShapeSegment> visibleSegments)
        {
            float maxIndex = System.Math.Max(1, visibleSegments.Count - 1);

            for (int i = 0; i < visibleSegments.Count; i++)
            {
                visibleSegments[i].U = i / maxIndex;
                visibleSegments[i].NormalizedTrailPosition = i / maxIndex;
            }
        }

        private float GetSamplePathPosition(ETR_Node node, TbindTrailSample sample, float visiblePosition)
        {
            if (!UsesFiniteExtrusion(node))
                return visiblePosition;

            return MathHelper.Clamp(sample.NodeFrame / node.ExtrudeDuration, 0f, 1f);
        }

        private bool ShouldUseContinuousTrail(ETR_Node node)
        {
            // TRS and projectile TBINDs are trails from previous transforms. Dense samples keep motion history from collapsing to static ETR path points.
            if (UsesExternalSpawn() || UsesTrsAttach(node))
                return true;

            return false;
        }

        private static bool UsesBeamTbindMode(ETR_Node node)
        {
            const ETR_Node.ExtrudeFlags beamFlags = ETR_Node.ExtrudeFlags.Unk17 | ETR_Node.ExtrudeFlags.Unk18;
            return (node.Flags & beamFlags) != 0;
        }

        private static List<EffectShapePoint> GetShape(ETR_Node node)
        {
            if (node.ExtrudeShapePoints.Count < 2)
                return EffectShapeMeshBuilder.CreateDefaultRibbonShape();

            List<EffectShapePoint> points = new List<EffectShapePoint>(node.ExtrudeShapePoints.Count);

            foreach (ShapeDrawPoint point in node.ExtrudeShapePoints)
                points.Add(new EffectShapePoint(point.X, point.Y));

            return points;
        }

        private static List<EffectPathPoint> GetPathProfile(ETR_Node node)
        {
            List<EffectPathPoint> pathProfile = new List<EffectPathPoint>();

            if (node.ExtrudePaths.Count == 0)
            {
                pathProfile.Add(new EffectPathPoint(1f, 0f, 0f, 0f));
                return pathProfile;
            }

            foreach (ConeExtrudePoint point in node.ExtrudePaths)
                pathProfile.Add(new EffectPathPoint(point.WorldScaleFactor, point.WorldScaleAdd, point.WorldOffsetFactor, point.WorldOffsetFactor2));

            return pathProfile;
        }

    }
}
