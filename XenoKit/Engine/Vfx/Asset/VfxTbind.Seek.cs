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
        public override void SeekNextFrame()
        {
            if (HasStarted)
            {
                SaveSeekSnapshot();
                CurrentFrame += 1f;
                UpdateNodes();
            }
            else
            {
                base.SeekNextFrame();
            }
        }

        public override void SeekPrevFrame()
        {
            // These snapshots cover direct VFX preview stepping. BAC action seeking still resimulates the action entry from frame 0.
            if (previousFrameSnapshots.Count > 0)
            {
                RestoreSeekSnapshot(previousFrameSnapshots.Pop());
                return;
            }

            if (CurrentFrame > 0f)
            {
                CurrentFrame -= 1f;
                TrimToCurrentFrame();
            }
        }


        private void TrimToCurrentFrame()
        {
            foreach (ETR_Node node in etrFile.Nodes)
            {
                TbindNodeState state = nodeStates[node];
                float nodeFrame = CurrentFrame - node.StartTime;

                if (nodeFrame < 0f)
                {
                    state.Reset();
                    continue;
                }

                for (int i = state.Samples.Count - 1; i >= 0; i--)
                {
                    if (state.Samples[i].NodeFrame > nodeFrame)
                        state.Samples.RemoveAt(i);
                }

                ResetEmissionAfterTrim(node, state);

                if (state.Samples.Count > 0)
                    BuildNodeMesh(node, state);
                else
                    state.Mesh.Clear();
            }
        }

        private void ResetEmissionAfterTrim(ETR_Node node, TbindNodeState state)
        {
            state.IsRetracting = false;
            state.StopMode = TbindStopMode.None;
            state.StopStartFrame = 0f;
            state.RetractionStartFrame = 0f;
            state.RetractionEndFrame = 0f;
            state.RetractionStartSampleCount = 0;

            if (state.Samples.Count > 0 && state.Samples[state.Samples.Count - 1].IsBootstrapSeed)
                state.Samples.RemoveAt(state.Samples.Count - 1);

            if (state.Samples.Count == 0)
            {
                state.Started = false;
                state.FinishedExtruding = false;
                state.NextEmitFrame = 0f;
                return;
            }

            if (state.Samples.Count == 1)
            {
                state.Samples.Clear();
                state.Started = false;
                state.FinishedExtruding = false;
                state.NextEmitFrame = 0f;
                return;
            }

            float segmentDelay = GetSegmentDelay(node);
            float lastSampleFrame = state.Samples[state.Samples.Count - 1].NodeFrame;
            state.Started = true;
            state.FinishedExtruding = false;
            state.NextEmitFrame = lastSampleFrame + segmentDelay;
        }

        private void SaveSeekSnapshot()
        {
            if (etrFile == null || nodeStates.Count == 0)
                return;

            TbindSeekSnapshot snapshot = new TbindSeekSnapshot
            {
                CurrentFrame = CurrentFrame
            };

            foreach (ETR_Node node in etrFile.Nodes)
            {
                if (!nodeStates.TryGetValue(node, out TbindNodeState state))
                    continue;

                TbindNodeSnapshot nodeSnapshot = new TbindNodeSnapshot
                {
                    NextEmitFrame = state.NextEmitFrame,
                    Started = state.Started,
                    FinishedExtruding = state.FinishedExtruding,
                    StopMode = state.StopMode,
                    StopStartFrame = state.StopStartFrame,
                    IsRetracting = state.IsRetracting,
                    RetractionStartFrame = state.RetractionStartFrame,
                    RetractionEndFrame = state.RetractionEndFrame,
                    RetractionStartSampleCount = state.RetractionStartSampleCount
                };

                foreach (TbindTrailSample sample in state.Samples)
                    nodeSnapshot.Samples.Add(sample.Clone());

                snapshot.Nodes[node] = nodeSnapshot;
            }

            previousFrameSnapshots.Push(snapshot);
            TrimSeekSnapshots();
        }

        private void RestoreSeekSnapshot(TbindSeekSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            CurrentFrame = snapshot.CurrentFrame;

            foreach (ETR_Node node in etrFile.Nodes)
            {
                TbindNodeState state = nodeStates[node];

                if (!snapshot.Nodes.TryGetValue(node, out TbindNodeSnapshot nodeSnapshot))
                {
                    state.Reset();
                    continue;
                }

                state.Samples.Clear();

                foreach (TbindTrailSample sample in nodeSnapshot.Samples)
                    state.Samples.Add(sample.Clone());

                state.NextEmitFrame = nodeSnapshot.NextEmitFrame;
                state.Started = nodeSnapshot.Started;
                state.FinishedExtruding = nodeSnapshot.FinishedExtruding;
                state.StopMode = nodeSnapshot.StopMode;
                state.StopStartFrame = nodeSnapshot.StopStartFrame;
                state.IsRetracting = nodeSnapshot.IsRetracting;
                state.RetractionStartFrame = nodeSnapshot.RetractionStartFrame;
                state.RetractionEndFrame = nodeSnapshot.RetractionEndFrame;
                state.RetractionStartSampleCount = nodeSnapshot.RetractionStartSampleCount;

                if (state.Samples.Count > 0)
                    BuildNodeMesh(node, state);
                else
                    state.Mesh.Clear();
            }
        }

        private void TrimSeekSnapshots()
        {
            if (previousFrameSnapshots.Count <= MaxSeekSnapshots)
                return;

            TbindSeekSnapshot[] snapshots = previousFrameSnapshots.ToArray();
            previousFrameSnapshots.Clear();

            int count = System.Math.Min(MaxSeekSnapshots, snapshots.Length);

            for (int i = count - 1; i >= 0; i--)
                previousFrameSnapshots.Push(snapshots[i]);
        }

        private void ClearSeekSnapshots()
        {
            previousFrameSnapshots.Clear();
        }

    }
}
