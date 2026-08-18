using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Globalization;
using XenoKit.Engine.Vfx.Shape;
using Xv2CoreLib.ETR;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx.Asset
{
    public partial class VfxTbind : VfxAsset
    {
        private void BuildNodeMesh(ETR_Node node, TbindNodeState state)
        {
            float nodeFrame = CurrentFrame - node.StartTime;
            List<EffectShapeSegment> sourceSegments = CreateDrawSegments(node, state, nodeFrame);
            float retractionProgress = state.IsRetracting ? GetRetractionProgress(state, nodeFrame) : 0f;
            List<EffectShapeSegment> visibleSegments = CreateVisibleSegments(sourceSegments);

            if (visibleSegments.Count >= 2)
            {
                List<EffectShapePoint> shape = GetShape(node);
                GetGeometryTextureScroll(state, out float uvScrollU, out float uvScrollV, out float uvStepU, out float uvStepV);
                string meshBuildKey = CreateMeshBuildKey(node, state, visibleSegments, shape, retractionProgress, uvScrollU, uvScrollV, uvStepU, uvStepV);

                if (state.MeshBuildKey == meshBuildKey && state.Mesh.HasVertices)
                    return;

                int renderSectionLimit = state.IsRetracting ? MaxRetractingRenderSections : MaxActiveRenderSections;
                bool autoOrientation = node.Flags.HasFlag(ETR_Node.ExtrudeFlags.AutoOrientation);
                bool usePathOffsetAsWidth = node.ExtrudeShapePoints.Count == 0 && node.ExtrudePaths.Count > 1;
                SimdVector3 cameraViewForward = GetCameraViewForward();
                EffectShapeMeshData meshData = EffectShapeMeshBuilder.BuildTbindTrailMesh(shape, visibleSegments, GetPathProfile(node), retractionProgress, renderSectionLimit, autoOrientation, usePathOffsetAsWidth, cameraViewForward, uvScrollU, uvScrollV, uvStepU, uvStepV, state.ProfiledSegmentsScratch, state.CurvedSegmentsScratch);
                state.Mesh.SetMeshData(meshData);
                state.MeshBuildKey = meshBuildKey;
            }
            else
            {
                state.Mesh.Clear();
                state.MeshBuildKey = null;
            }
        }

        private string CreateMeshBuildKey(ETR_Node node, TbindNodeState state, IList<EffectShapeSegment> visibleSegments, IList<EffectShapePoint> shape, float retractionProgress, float uvScrollU, float uvScrollV, float uvStepU, float uvStepV)
        {
            EffectShapeSegment first = visibleSegments[0];
            EffectShapeSegment last = visibleSegments[visibleSegments.Count - 1];
            float nodeFrame = CurrentFrame - node.StartTime;
            float nodeScale = GetNodeScale(node, state, nodeFrame);
            bool autoOrientation = node.Flags.HasFlag(ETR_Node.ExtrudeFlags.AutoOrientation);
            string cameraKey = autoOrientation ? GetCameraMeshKey() : string.Empty;
            string primaryColorKey = string.Empty;
            string secondaryColorKey = string.Empty;

            if (state.IsRetracting)
            {
                Color primary = GetPrimaryColor(node, state, nodeFrame);
                Color secondary = GetSecondaryColor(node, state, nodeFrame, primary);
                primaryColorKey = primary.PackedValue.ToString(CultureInfo.InvariantCulture);
                secondaryColorKey = secondary.PackedValue.ToString(CultureInfo.InvariantCulture);
            }

            return string.Join("|",
                state.StopMode,
                state.IsRetracting,
                visibleSegments.Count,
                state.Samples.Count,
                state.HasRenderOnlyHead,
                state.RenderOnlyHeadFrame.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                first.CreatedFrame.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                last.CreatedFrame.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                GetTranslationKey(first.Transform),
                GetTranslationKey(last.Transform),
                nodeScale.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                retractionProgress.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                uvScrollU.ToString("0.####", CultureInfo.InvariantCulture),
                uvScrollV.ToString("0.####", CultureInfo.InvariantCulture),
                uvStepU.ToString("0.####", CultureInfo.InvariantCulture),
                uvStepV.ToString("0.####", CultureInfo.InvariantCulture),
                primaryColorKey,
                secondaryColorKey,
                shape?.Count ?? 0,
                node.ExtrudeShapePoints.Count == 0 && node.ExtrudePaths.Count > 1,
                autoOrientation,
                cameraKey);
        }

        private string GetCameraMeshKey()
        {
            SimdVector3 position = ViewportInstance.Camera.CameraState.Position;
            SimdVector3 target = ViewportInstance.Camera.CameraState.TargetPosition;
            return $"{GetVectorKey(position)}>{GetVectorKey(target)}";
        }

        private static string GetTranslationKey(Matrix4x4 matrix)
        {
            return GetVectorKey(matrix.Translation);
        }

        private static string GetVectorKey(SimdVector3 vector)
        {
            return $"{vector.X:0.###},{vector.Y:0.###},{vector.Z:0.###}";
        }

        private SimdVector3 GetCameraViewForward()
        {
            SimdVector3 forward = new SimdVector3(
                ViewportInstance.Camera.ViewMatrix.M31,
                ViewportInstance.Camera.ViewMatrix.M32,
                ViewportInstance.Camera.ViewMatrix.M33);

            return forward.LengthSquared() > 0.000001f ? SimdVector3.Normalize(forward) : SimdVector3.UnitZ;
        }

        private void RefreshAutoOrientedMeshes()
        {
            foreach (ETR_Node node in etrFile.Nodes)
            {
                if (!node.Flags.HasFlag(ETR_Node.ExtrudeFlags.AutoOrientation))
                    continue;

                TbindNodeState state = nodeStates[node];

                if (state.Samples.Count > 0)
                    BuildNodeMesh(node, state);
            }
        }
    }
}
