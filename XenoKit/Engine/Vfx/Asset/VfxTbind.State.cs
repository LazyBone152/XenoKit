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
        private enum TbindNodePhase
        {
            Waiting,
            Extruding,
            Holding,
            Finished
        }

        private enum TbindStopMode
        {
            None,
            Natural,
            Retract
        }

        private sealed class TbindNodeState
        {
            public EffectShapeMesh Mesh { get; } = new EffectShapeMesh();
            public List<TbindTrailSample> Samples { get; } = new List<TbindTrailSample>();
            public List<EffectShapeSegment> SourceSegmentsScratch { get; } = new List<EffectShapeSegment>();
            public List<TbindTrailSample> RenderSamplesScratch { get; } = new List<TbindTrailSample>();
            public List<EffectShapeSegment> VisibleSegmentsScratch { get; } = new List<EffectShapeSegment>();
            public List<EffectShapeSegment> ProfiledSegmentsScratch { get; } = new List<EffectShapeSegment>();
            public List<EffectShapeSegment> CurvedSegmentsScratch { get; } = new List<EffectShapeSegment>();
            public Xv2ShaderEffect Material { get; }
            public SamplerInfo[] Samplers { get; }
            public Xv2Texture[] Textures { get; }
            public float[][] TextureScroll { get; } = { new float[4], new float[4] };
            public bool[] TextureScrollActive { get; } = new bool[2];
            public float NextEmitFrame { get; set; }
            public bool Started { get; set; }
            public bool FinishedExtruding { get; set; }
            public TbindStopMode StopMode { get; set; }
            public float StopStartFrame { get; set; }
            public bool IsRetracting { get; set; }
            public float RetractionStartFrame { get; set; }
            public float RetractionEndFrame { get; set; }
            public int RetractionStartSampleCount { get; set; }
            public string MeshBuildKey { get; set; }
            public bool HasRenderOnlyHead { get; set; }
            public float RenderOnlyHeadFrame { get; set; }
            public int LastRenderSegmentCount { get; set; }
            private Matrix4x4 fixedAttachTransform = Matrix4x4.Identity;
            private Matrix4x4 currentAttachTransform = Matrix4x4.Identity;
            private bool hasFixedAttachTransform;
            private bool hasAttachTransform;

            public TbindNodeState(Xv2ShaderEffect material, SamplerInfo[] samplers, Xv2Texture[] textures)
            {
                Material = material;
                Samplers = samplers;
                Textures = textures;
            }

            public void Reset()
            {
                Mesh.Clear();
                Samples.Clear();
                SourceSegmentsScratch.Clear();
                RenderSamplesScratch.Clear();
                VisibleSegmentsScratch.Clear();
                ProfiledSegmentsScratch.Clear();
                CurvedSegmentsScratch.Clear();
                NextEmitFrame = 0f;
                Started = false;
                FinishedExtruding = false;
                StopMode = TbindStopMode.None;
                StopStartFrame = 0f;
                IsRetracting = false;
                RetractionStartFrame = 0f;
                RetractionEndFrame = 0f;
                RetractionStartSampleCount = 0;
                MeshBuildKey = null;
                HasRenderOnlyHead = false;
                RenderOnlyHeadFrame = 0f;
                LastRenderSegmentCount = 0;
                ResetTextureScroll();
                hasFixedAttachTransform = false;
                fixedAttachTransform = Matrix4x4.Identity;
                hasAttachTransform = false;
                currentAttachTransform = Matrix4x4.Identity;
            }

            public void ResetTextureScroll()
            {
                for (int slot = 0; slot < TextureScroll.Length; slot++)
                {
                    TextureScrollActive[slot] = false;

                    for (int component = 0; component < TextureScroll[slot].Length; component++)
                        TextureScroll[slot][component] = 0f;
                }
            }

            public Matrix4x4 GetFixedAttachTransform(Matrix4x4 fallback)
            {
                if (!hasFixedAttachTransform)
                {
                    fixedAttachTransform = fallback;
                    hasFixedAttachTransform = true;
                }

                return fixedAttachTransform;
            }

            public void SetAttachTransform(Matrix4x4 attachTransform)
            {
                currentAttachTransform = attachTransform;
                hasAttachTransform = true;
            }

            public Matrix4x4 GetAttachTransform()
            {
                return hasAttachTransform ? currentAttachTransform : Matrix4x4.Identity;
            }
        }

        private sealed class TbindTrailSample
        {
            public Matrix4x4 Transform { get; set; }
            public float NodeFrame { get; set; }
            public float CreatedFrame { get; set; }
            public float ExpireFrame { get; set; }
            public float NormalizedTrailPosition { get; set; }
            public float Scale { get; set; }
            public float EmittedV { get; set; }
            public bool IsBootstrapSeed { get; set; }
            public bool IsRenderOnlyHead { get; set; }
            public Color PrimaryColor { get; set; } = Color.White;
            public Color SecondaryColor { get; set; } = Color.White;

            public TbindTrailSample Clone()
            {
                return new TbindTrailSample
                {
                    Transform = Transform,
                    NodeFrame = NodeFrame,
                    CreatedFrame = CreatedFrame,
                    ExpireFrame = ExpireFrame,
                    NormalizedTrailPosition = NormalizedTrailPosition,
                    Scale = Scale,
                    EmittedV = EmittedV,
                    IsBootstrapSeed = IsBootstrapSeed,
                    IsRenderOnlyHead = IsRenderOnlyHead,
                    PrimaryColor = PrimaryColor,
                    SecondaryColor = SecondaryColor
                };
            }
        }

        private sealed class TbindSeekSnapshot
        {
            public float CurrentFrame { get; set; }
            public Dictionary<ETR_Node, TbindNodeSnapshot> Nodes { get; } = new Dictionary<ETR_Node, TbindNodeSnapshot>();
        }

        private sealed class TbindNodeSnapshot
        {
            public List<TbindTrailSample> Samples { get; } = new List<TbindTrailSample>();
            public float NextEmitFrame { get; set; }
            public bool Started { get; set; }
            public bool FinishedExtruding { get; set; }
            public TbindStopMode StopMode { get; set; }
            public float StopStartFrame { get; set; }
            public bool IsRetracting { get; set; }
            public float RetractionStartFrame { get; set; }
            public float RetractionEndFrame { get; set; }
            public int RetractionStartSampleCount { get; set; }
        }

    }
}
