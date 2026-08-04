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
        private void UpdateNodes()
        {
            bool anyWaiting = false;
            bool anyActive = false;
            bool anySamples = false;
            int timelineEnd = GetEffectTimelineEnd();

            foreach (ETR_Node node in etrFile.Nodes)
            {
                TbindNodeState state = nodeStates[node];
                float nodeFrame = CurrentFrame - node.StartTime;
                TbindNodePhase phase = GetNodePhase(node, nodeFrame);

                if (phase == TbindNodePhase.Waiting)
                {
                    if (!IsTerminating)
                        anyWaiting = true;

                    state.Mesh.Clear();
                    continue;
                }

                Matrix4x4 attachTransform = ResolveAttachTransform(node, state);
                state.SetAttachTransform(attachTransform);

                if (IsTerminating)
                {
                    if (state.StopMode == TbindStopMode.Retract)
                    {
                        StartRetraction(node, state, nodeFrame);
                    }
                }
                else if (phase == TbindNodePhase.Extruding)
                {
                    EmitDueSamples(node, state, nodeFrame);
                    anyActive = true;
                }
                else if (phase == TbindNodePhase.Holding)
                {
                    StartRetraction(node, state, nodeFrame);
                    state.FinishedExtruding = true;
                }

                PruneExpiredSamples(state, nodeFrame);
                BuildNodeMesh(node, state);

                if (state.Samples.Count > 0)
                {
                    anySamples = true;

                    if (IsTerminating || phase == TbindNodePhase.Holding)
                        anyActive = true;
                }
            }

            if (EffectPart.Deactivation == EffectPart.DeactivationMode.Never)
            {
                if (IsTerminating && !anyActive && !anySamples)
                {
                    IsFinished = true;
                    return;
                }

                if (!anyWaiting && !anyActive && !anySamples && CurrentFrame > timelineEnd)
                    ResetTimeline();

                return;
            }

            if (IsTerminating && !anyActive && !anySamples)
            {
                IsFinished = true;
                return;
            }

            if (!anyWaiting && !anyActive && !anySamples && CurrentFrame > timelineEnd)
                IsFinished = true;
        }

        private TbindNodePhase GetNodePhase(ETR_Node node, float nodeFrame)
        {
            if (nodeFrame < 0f)
                return TbindNodePhase.Waiting;

            if (ShouldUseContinuousTrail(node))
                return TbindNodePhase.Extruding;

            if (node.ExtrudeDuration < 0)
                return TbindNodePhase.Extruding;

            if (node.ExtrudeDuration == 0)
                return nodeFrame <= node.HoldDuration ? TbindNodePhase.Extruding : TbindNodePhase.Finished;

            if (nodeFrame <= node.ExtrudeDuration)
                return TbindNodePhase.Extruding;

            return nodeFrame <= node.ExtrudeDuration + node.HoldDuration ? TbindNodePhase.Holding : TbindNodePhase.Finished;
        }

        private int GetNodeTimelineEnd(ETR_Node node)
        {
            if (node.ExtrudeDuration < 0)
                return int.MaxValue;

            return node.StartTime + node.ExtrudeDuration + node.HoldDuration;
        }

        private int GetEffectTimelineEnd()
        {
            int endFrame = 0;

            foreach (ETR_Node node in etrFile.Nodes)
            {
                int nodeEnd = GetNodeTimelineEnd(node);

                if (nodeEnd == int.MaxValue)
                    return int.MaxValue;

                endFrame = System.Math.Max(endFrame, nodeEnd);
            }

            return endFrame;
        }

        private void ResetTimeline()
        {
            ClearSeekSnapshots();

            foreach (TbindNodeState state in nodeStates.Values)
                state.Reset();

            CurrentFrame = 0f;
        }

        private float GetSegmentDelay(ETR_Node node)
        {
            if (node.SegementFrameSize == 0)
                return 1f;

            return System.Math.Max(0.1f, node.SegementFrameSize / 10f);
        }

        private bool UsesFiniteExtrusion(ETR_Node node)
        {
            return !ShouldUseContinuousTrail(node) && node.ExtrudeDuration > 0;
        }

        private float GetNodeScale(ETR_Node node, TbindNodeState state, float nodeFrame)
        {
            float scale = node.Scale.GetInterpolatedValue(GetNodeAnimationFrame(node, state, nodeFrame));
            return System.Math.Max(0.0001f, scale);
        }

        private Color GetPrimaryColor(ETR_Node node, TbindNodeState state, float nodeFrame)
        {
            float colorFrame = GetKeyframedValueTime(node, state, node.Color1, nodeFrame);
            float alphaFrame = GetKeyframedValueTime(node, state, node.Color1_Transparency, nodeFrame);
            return GetColor(node.Color1.GetInterpolatedValue(colorFrame), node.Color1_Transparency.GetInterpolatedValue(alphaFrame));
        }

        private Color GetSecondaryColor(ETR_Node node, TbindNodeState state, float nodeFrame, Color primary)
        {
            if (node.Flags.HasFlag(ETR_Node.ExtrudeFlags.NoDegrade))
                return primary;

            float colorFrame = GetKeyframedValueTime(node, state, node.Color2, nodeFrame);
            float alphaFrame = GetKeyframedValueTime(node, state, node.Color2_Transparency, nodeFrame);
            return GetColor(node.Color2.GetInterpolatedValue(colorFrame), node.Color2_Transparency.GetInterpolatedValue(alphaFrame));
        }

        private float GetNodeAnimationFrame(ETR_Node node, TbindNodeState state, float nodeFrame)
        {
            return GetKeyframedValueTime(node, state, node.Scale, nodeFrame);
        }

        private float GetKeyframedValueTime(ETR_Node node, TbindNodeState state, KeyframedBaseValue value, float nodeFrame)
        {
            int duration = GetKeyframedValueDuration(value);

            if (duration <= 0)
                return nodeFrame;

            if (EffectPart.Deactivation == EffectPart.DeactivationMode.LoopCancel)
                return GetLoopCancelKeyframeTime(node, state, nodeFrame);

            return value.Loop ? WrapKeyframedTime(nodeFrame, duration) : nodeFrame;
        }

        private float GetLoopCancelKeyframeTime(ETR_Node node, TbindNodeState state, float nodeFrame)
        {
            if (state != null && (state.StopMode == TbindStopMode.Retract || state.IsRetracting))
                return 1f + System.Math.Max(0f, nodeFrame - state.RetractionStartFrame);

            if (node.ExtrudeDuration < 0)
                return System.Math.Min(nodeFrame, 1f);

            float holdEndFrame = System.Math.Max(1f, node.ExtrudeDuration);

            if (nodeFrame <= holdEndFrame)
                return System.Math.Min(nodeFrame, 1f);

            return System.Math.Max(1f, nodeFrame - holdEndFrame + 1f);
        }

        private float GetRetractionDuration(ETR_Node node)
        {
            float duration = GetHoldFrames(node);

            duration = System.Math.Max(duration, GetKeyframedValueDuration(node.Scale));
            duration = System.Math.Max(duration, GetKeyframedValueDuration(node.Color1));
            duration = System.Math.Max(duration, GetKeyframedValueDuration(node.Color1_Transparency));
            duration = System.Math.Max(duration, GetKeyframedValueDuration(node.Color2));
            duration = System.Math.Max(duration, GetKeyframedValueDuration(node.Color2_Transparency));

            return System.Math.Max(1f, duration);
        }

        private static int GetKeyframedValueDuration(KeyframedBaseValue value)
        {
            if (value == null || !value.IsAnimated)
                return 0;

            if (value is KeyframedFloatValue floatValue)
            {
                if (floatValue.Keyframes.Count < 2)
                    return 0;

                return (int)System.Math.Ceiling(floatValue.Keyframes.Max(keyframe => keyframe.Time));
            }

            if (value is KeyframedColorValue colorValue)
            {
                if (colorValue.Keyframes.Count < 2)
                    return 0;

                return (int)System.Math.Ceiling(colorValue.Keyframes.Max(keyframe => keyframe.Time));
            }

            return 0;
        }

        private static float WrapKeyframedTime(float nodeFrame, int duration)
        {
            if (duration <= 0)
                return nodeFrame;

            float wrapped = nodeFrame % duration;
            return wrapped < 0f ? wrapped + duration : wrapped;
        }

        private static float GetSegmentV(ETR_Node node, float nodeFrame)
        {
            int timeline = System.Math.Max(1, GetTimelineLength(node));

            if (node.Flags.HasFlag(ETR_Node.ExtrudeFlags.UVPauseOnExtrude) && node.ExtrudeDuration > 0 && nodeFrame <= node.ExtrudeDuration)
                return 0f;

            if (node.Flags.HasFlag(ETR_Node.ExtrudeFlags.UVPauseOnHold) && node.ExtrudeDuration >= 0 && nodeFrame > node.ExtrudeDuration)
                return 1f;

            return MathHelper.Clamp(nodeFrame / timeline, 0f, 1f);
        }

        private static int GetTimelineLength(ETR_Node node)
        {
            if (node.ExtrudeDuration < 0)
                return System.Math.Max(1, (int)node.HoldDuration);

            return System.Math.Max(1, node.ExtrudeDuration + node.HoldDuration);
        }

    }
}
