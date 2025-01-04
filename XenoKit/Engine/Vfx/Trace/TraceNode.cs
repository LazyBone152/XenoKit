using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using XenoKit.Engine.Pool;
using XenoKit.Engine.Vfx.Asset;
using XenoKit.Engine.Vfx.Particle;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EMP_NEW.Keyframes;
using Xv2CoreLib.ETR;

namespace XenoKit.Engine.Vfx.Trace
{
    public class TraceNode : PooledEntity
    {
        private ETR_File EtrFile;
        private ETR_Node Node;
        private VfxTrace VfxTrace;

        public TraceEmissionData EmissionData;
        public ParticleUV ParticleUV = new ParticleUV();

        public ExtrudeState TraceState { get; private set; } = ExtrudeState.NotStarted;
        public float CurrentFrame { get; private set; }
        public float CurrentTimeFactor { get; private set; }
        private float CurrentSegmentTimer = 0;
        private int ExtrudeDuration;
        private int HoldDuration;
        private int TotalLifetime;

        public int BoneIndex1 = -1;
        public int BoneIndex2 = -1;

        private readonly List<TraceSegment> Segments = new List<TraceSegment>();

        //Keyframed Values:
        public float Scale = 0.5f;
        public float[] PrimaryColor = new float[4];
        public float[] SecondaryColor = new float[4];

        public void Initialize(ETR_File etrFile, ETR_Node node, VfxTrace vfxTrace)
        {
            VfxTrace = vfxTrace;
            EtrFile = etrFile;
            Node = node;
            EmissionData = CompiledObjectManager.GetCompiledObject<TraceEmissionData>(Node, GameBase);
            ExtrudeDuration = MathHelper.Clamp(node.ExtrudeDuration, 0, int.MaxValue);
            HoldDuration = MathHelper.Clamp(node.HoldDuration, 0, int.MaxValue);
            TotalLifetime = ExtrudeDuration + HoldDuration;
            BoneIndex1 = VfxTrace.Actor.Skeleton.GetBoneIndex(Node.AttachBone);
            BoneIndex2 = VfxTrace.Actor.Skeleton.GetBoneIndex(Node.AttachBone2);
        }

        public override void ClearObjectState()
        {
            TraceState = ExtrudeState.NotStarted;
            CurrentFrame = 0;
            CurrentTimeFactor = 0;
            EmissionData = null;
            EtrFile = null;
            Node = null;
            CurrentSegmentTimer = 0;
            TotalLifetime = 0;
            BoneIndex1 = -1;
            BoneIndex2 = -1;

            Segments.Clear();
        }

        public void Release()
        {
            foreach (TraceSegment segment in Segments)
            {
                segment.Release();
            }

            ObjectPoolManager.TraceNodePool.ReleaseObject(this);
        }

        public override void Update()
        {
            //CurrentTimeFactor = CurrentFrame / Lifetime;

            if (TraceState == ExtrudeState.NotStarted)
            {
                if (CurrentFrame >= Node.StartTime)
                {
                    TraceState = ExtrudeState.Extrude;
                    CurrentFrame = 0;
                    CreateSegment();
                }
            }

            UpdateKeyframedValues();

            //Check for state transition
            if (CurrentFrame >= Node.ExtrudeDuration && TraceState == ExtrudeState.Extrude)
            {
                TraceState = ExtrudeState.Hold;
            }
            else if (CurrentFrame >= TotalLifetime && TraceState == ExtrudeState.Hold)
            {
                TraceState = ExtrudeState.Expired;
            }

            //Update
            if (TraceState == ExtrudeState.Extrude)
            {

                if (CurrentSegmentTimer >= Node.SegementFrameSize && Node.SegementFrameSize != 0)
                {
                    CurrentSegmentTimer = 0;
                    CreateSegment();
                }
                else
                {
                    CurrentSegmentTimer += VfxTrace.CurrentFrameDelta;
                }
            }

            if (TraceState == ExtrudeState.Hold)
            {
                //Does anything even need doing here...?
            }

            foreach (TraceSegment segment in Segments)
            {
                segment.Update();
            }

            CurrentFrame += VfxTrace.CurrentFrameDelta;
        }

        private void UpdateKeyframedValues()
        {
            //ETR_InterpolationType.ShapeStartToEnd will be handled in TracePlane per vertex

            if (Node.Scale.ETR_InterpolationType != ETR_InterpolationType.ShapeStartToEnd)
                Scale = Node.Scale.GetInterpolatedValue(GetCurrentFrameForAnimation(Node.Scale));

            if (Node.Color1.ETR_InterpolationType != ETR_InterpolationType.ShapeStartToEnd)
                PrimaryColor = Node.Color1.GetInterpolatedValue(GetCurrentFrameForAnimation(Node.Color1));

            if (Node.Color2.ETR_InterpolationType != ETR_InterpolationType.ShapeStartToEnd)
                SecondaryColor = Node.Color2.GetInterpolatedValue(GetCurrentFrameForAnimation(Node.Color2));

            if (Node.Color1_Transparency.ETR_InterpolationType != ETR_InterpolationType.ShapeStartToEnd)
                PrimaryColor[3] = Node.Color1_Transparency.GetInterpolatedValue(GetCurrentFrameForAnimation(Node.Color1_Transparency));

            if (Node.Color2_Transparency.ETR_InterpolationType != ETR_InterpolationType.ShapeStartToEnd)
                SecondaryColor[3] = Node.Color2_Transparency.GetInterpolatedValue(GetCurrentFrameForAnimation(Node.Color2_Transparency));
        }

        private void CreateSegment()
        {
            if (BoneIndex1 == -1) return; //Cant create a segment if the attach bone isn't valid

            TraceSegment segment = GameBase.ObjectPoolManager.GetTraceSegment(EtrFile, Node, this, VfxTrace);
            segment.IsExtruding = true;

            if (Segments.Count > 0)
                Segments[Segments.Count - 1].IsExtruding = false;

            Segments.Add(segment);
        }

        protected void UpdateScale()
        {
            Scale = Node.Scale.GetInterpolatedValue(CurrentTimeFactor);
        }

        protected void UpdateColor()
        {
            float[] primaryColor = Node.Color1.GetInterpolatedValue(CurrentTimeFactor);
            PrimaryColor[3] = Node.Color1_Transparency.GetInterpolatedValue(CurrentTimeFactor);

            PrimaryColor[0] = MathHelper.Clamp(primaryColor[0], 0f, 1f);
            PrimaryColor[1] = MathHelper.Clamp(primaryColor[1], 0f, 1f);
            PrimaryColor[2] = MathHelper.Clamp(primaryColor[2], 0f, 1f);
            PrimaryColor[3] = MathHelper.Clamp(PrimaryColor[3], 0f, 1f);

            if (!Node.Flags.HasFlag(ETR_Node.ExtrudeFlags.NoDegrade))
            {
                float[] secondaryColor = Node.Color2.GetInterpolatedValue(CurrentTimeFactor);
                SecondaryColor[3] = Node.Color2_Transparency.GetInterpolatedValue(CurrentTimeFactor);

                SecondaryColor[0] = MathHelper.Clamp(secondaryColor[0], 0f, 1f);
                SecondaryColor[1] = MathHelper.Clamp(secondaryColor[1], 0f, 1f);
                SecondaryColor[2] = MathHelper.Clamp(secondaryColor[2], 0f, 1f);
                SecondaryColor[3] = MathHelper.Clamp(SecondaryColor[3], 0f, 1f);
            }
        }
    
        private float GetCurrentFrameForAnimation(KeyframedBaseValue value)
        {
            //ShapeStartToEnd must be done at the vertex level

            switch (value.ETR_InterpolationType)
            {
                case ETR_InterpolationType.DefaultEnd:
                    return CurrentFrame - ExtrudeDuration;
                default:
                    return CurrentFrame;
            }
        }
    }

    public enum ExtrudeState
    {
        NotStarted,
        Extrude,
        Hold,
        Expired
    }
}
