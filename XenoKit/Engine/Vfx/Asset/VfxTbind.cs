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
        private const int MaxTrailSamples = 512;
        private const int MaxActiveRenderSections = 384;
        private const int MaxRetractingRenderSections = 96;
        private const int MaxSeekSnapshots = 120;
        private readonly ETR_File etrFile;
        private readonly Dictionary<ETR_Node, TbindNodeState> nodeStates = new Dictionary<ETR_Node, TbindNodeState>();
        private readonly Stack<TbindSeekSnapshot> previousFrameSnapshots = new Stack<TbindSeekSnapshot>();

        public VfxTbind(Matrix4x4 startWorld, ETR_File etrFile, EffectPart effectPart, Actor actor, bool spawnedByProjectile = false) : base(startWorld, effectPart, actor, spawnedByProjectile)
        {
            this.etrFile = etrFile;
            InitializeNodes();
            ViewportInstance.RenderSystem.AddRenderEntity(this);
        }

        public override EngineObjectTypeEnum EngineObjectType => EngineObjectTypeEnum.VFX;
        protected override bool FinishAnimationBeforeTerminating => true;

        public override int LowRezMode
        {
            get
            {
                Xv2ShaderEffect material = nodeStates.Values.FirstOrDefault()?.Material;
                if (material == null) return 0;
                if (material.MatParam.LowRez == 1) return 1;
                if (material.MatParam.LowRezSmoke == 1) return 2;
                return 0;
            }
        }

        public override void Update()
        {
            base.Update();
            if (!HasStarted || etrFile == null) return;

            float frameDelta = ViewportInstance.IsPlaying ? EffectPart.UseTimeScale ? Actor?.ActiveTimeScale ?? 1f : 1f : 0f;
            UpdateTextureScroll(frameDelta);
            UpdateNodes();

            if (ViewportInstance.IsPlaying)
                CurrentFrame += frameDelta;
        }

        public override void Simulate()
        {
            base.Update();
            if (!HasStarted || etrFile == null) return;

            UpdateTextureScroll(1f);
            UpdateNodes();
            CurrentFrame += 1f;
        }

        public override void Draw()
        {
            if (!DrawThisFrame || etrFile == null) return;

            RefreshAutoOrientedMeshes();

            foreach (TbindNodeState state in nodeStates.Values)
            {
                if (state.Material != null)
                    state.Material.SetTextureScrollOverrides(
                        null,
                        state.TextureScrollActive[1] ? state.TextureScroll[1] : null);

                state.Mesh.Draw(this, state.Material, state.Samplers, state.Textures, !EffectPart.NoGlare);
            }
        }

        protected override void OnExternalTransformChanged()
        {
            if (!HasStarted || etrFile == null)
                return;

            UpdateNodes();
        }



        public override void Dispose()
        {
            ClearSeekSnapshots();
            base.Dispose();
            ViewportInstance.RenderSystem.RemoveRenderEntity(this);
        }

        public override void Terminate()
        {
            switch (EffectPart.Deactivation)
            {
                case EffectPart.DeactivationMode.Immediate:
                    ClearAllTbindNodes();
                    IsFinished = true;
                    return;
                case EffectPart.DeactivationMode.LoopCancel:
                    IsTerminating = true;
                    StartRetractionForAllNodes();
                    return;
                case EffectPart.DeactivationMode.Never:
                    IsTerminating = true;
                    StartNaturalStopForAllNodes();
                    return;
                default:
                    ClearAllTbindNodes();
                    IsFinished = true;
                    throw new System.InvalidOperationException($"Unsupported TBIND deactivation mode: {EffectPart.Deactivation}.");
            }
        }

        private void ClearAllTbindNodes()
        {
            ClearSeekSnapshots();
            foreach (ETR_Node node in etrFile.Nodes)
            {
                TbindNodeState state = nodeStates[node];
                state.Mesh.Clear();
                state.Samples.Clear();
                state.SourceSegmentsScratch.Clear();
                state.RenderSamplesScratch.Clear();
                state.VisibleSegmentsScratch.Clear();
                state.ProfiledSegmentsScratch.Clear();
                state.CurvedSegmentsScratch.Clear();
                state.MeshBuildKey = null;
                state.HasRenderOnlyHead = false;
                state.RenderOnlyHeadFrame = 0f;
                state.LastRenderSegmentCount = 0;
                state.StopMode = TbindStopMode.None;
                state.StopStartFrame = 0f;
                state.IsRetracting = false;
                state.RetractionStartFrame = 0f;
                state.RetractionEndFrame = 0f;
                state.RetractionStartSampleCount = 0;
                state.ResetTextureScroll();
            }
        }

        private void StartRetractionForAllNodes()
        {
            foreach (ETR_Node node in etrFile.Nodes)
            {
                TbindNodeState state = nodeStates[node];
                float nodeFrame = CurrentFrame - node.StartTime;
                StartRetraction(node, state, nodeFrame);
            }
        }

        private void StartNaturalStopForAllNodes()
        {
            foreach (ETR_Node node in etrFile.Nodes)
            {
                TbindNodeState state = nodeStates[node];
                float nodeFrame = CurrentFrame - node.StartTime;
                StartNaturalStop(node, state, nodeFrame);
            }
        }

        private void InitializeNodes()
        {
            ClearSeekSnapshots();
            nodeStates.Clear();

            if (etrFile == null) return;

            foreach (ETR_Node node in etrFile.Nodes)
                nodeStates[node] = new TbindNodeState(CreateMaterial(node), CreateSamplers(node), CreateTextures(node));
        }

        private void UpdateTextureScroll(float frameDelta)
        {
            foreach (ETR_Node node in etrFile.Nodes)
                UpdateTextureScroll(node, nodeStates[node], frameDelta);
        }




























































    }
}
