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
        private Matrix4x4 ResolveAttachTransform(ETR_Node node, TbindNodeState state)
        {
            Matrix4x4 baseTransform = Transform;

            if (UsesTrsAttach(node))
            {
                baseTransform = GetExternalSpawnTransform();
            }
            else if (!string.IsNullOrWhiteSpace(node.AttachBone) && !string.IsNullOrWhiteSpace(node.AttachBone2) && Actor != null)
            {
                int startBoneIdx = Actor.Skeleton.GetBoneIndex(node.AttachBone, true);
                int endBoneIdx = Actor.Skeleton.GetBoneIndex(node.AttachBone2, true);

                if (startBoneIdx != -1 && endBoneIdx != -1)
                    baseTransform = CreateTwoBoneTransform(Actor.GetAbsoluteBoneMatrix(startBoneIdx), Actor.GetAbsoluteBoneMatrix(endBoneIdx));
            }
            else if (!string.IsNullOrWhiteSpace(node.AttachBone) && Actor != null)
            {
                int boneIdx = Actor.Skeleton.GetBoneIndex(node.AttachBone, true);

                if (boneIdx != -1)
                    baseTransform = Actor.GetAbsoluteBoneMatrix(boneIdx);
            }
            else if (EffectPart.AttachementType == EffectPart.Attachment.Bone && !UsesExternalSpawn() && !string.IsNullOrWhiteSpace(EffectPart.ESK) && Actor != null)
            {
                int boneIdx = Actor.Skeleton.GetBoneIndex(EffectPart.ESK, true);

                if (boneIdx != -1)
                    baseTransform = Actor.GetAbsoluteBoneMatrix(boneIdx);
            }
            else if (UsesExternalSpawn())
            {
                baseTransform = GetExternalSpawnTransform();
            }

            if (!EffectPart.PositionUpdate && !ShouldUseContinuousTrail(node))
                return state.GetFixedAttachTransform(baseTransform);

            return baseTransform;
        }

        private Matrix4x4 CreateNodeTransform(ETR_Node node, Matrix4x4 attachTransform, float nodeFrame)
        {
            return attachTransform;
        }

        private static Matrix4x4 CreateNodeLocalTransform(ETR_Node node)
        {
            Matrix4x4 rotation =
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(node.Rotation.Z)) *
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(node.Rotation.Y)) *
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(node.Rotation.X));

            Matrix4x4 offset = Matrix4x4.CreateTranslation(new SimdVector3(node.Position.X, node.Position.Y, node.Position.Z + node.PositionExtrudeZ));
            return rotation * offset;
        }

        private Matrix4x4 CreateDrawTransform(ETR_Node node, Matrix4x4 sampleTransform, float nodeFrame)
        {
            return CreateNodeLocalTransform(node) * sampleTransform;
        }

        private static Matrix4x4 CreateTwoBoneTransform(Matrix4x4 startBone, Matrix4x4 endBone)
        {
            SimdVector3 start = startBone.Translation;
            SimdVector3 direction = endBone.Translation - start;

            if (direction.LengthSquared() < 0.000001f)
                return startBone;

            SimdVector3 yAxis = SimdVector3.Normalize(direction);
            SimdVector3 up = System.Math.Abs(SimdVector3.Dot(yAxis, SimdVector3.UnitZ)) > 0.98f ? SimdVector3.UnitX : SimdVector3.UnitZ;
            SimdVector3 xAxis = SimdVector3.Normalize(SimdVector3.Cross(up, yAxis));
            SimdVector3 zAxis = SimdVector3.Normalize(SimdVector3.Cross(xAxis, yAxis));

            return new Matrix4x4(
                xAxis.X, xAxis.Y, xAxis.Z, 0f,
                yAxis.X, yAxis.Y, yAxis.Z, 0f,
                zAxis.X, zAxis.Y, zAxis.Z, 0f,
                start.X, start.Y, start.Z, 1f);
        }

        private bool UsesTrsAttach(ETR_Node node)
        {
            if (string.Equals(node.AttachBone, "TRS", System.StringComparison.OrdinalIgnoreCase))
                return true;

            return string.IsNullOrWhiteSpace(node.AttachBone) && UsesExternalSpawn();
        }

    }
}
