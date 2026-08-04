using System;
using System.Numerics;
using XenoKit.Editor;
using XenoKit.Engine.Animation;
using Xv2CoreLib.FPF;

namespace XenoKit.Engine
{
    [Flags]
    public enum FpfPoseMatrix
    {
        None = 0,
        RelativeTransform = 1,
        LocalPoseTransform = 2,
        AbsolutePoseTransform = 4,
        AttachmentPoseTransform = 8,
        FormationSkinningTransform = 16
    }

    public enum FpfSkinOffsetMode
    {
        InverseBindOffsetPose,
        FpfFormationSkinningTransform
    }

    public enum FpfPoseBakeMode
    {
        CurrentPose
    }

    public static class FpfPosePreview
    {
        public static void Apply(Actor actor)
        {
            if (actor?.FpfPreviewFile == null)
                return;

            try
            {
                if (actor.FpfPreviewPoseMatrix == FpfPoseMatrix.None && actor.FpfPreviewSkinOffsetMatrix == FpfPoseMatrix.None)
                {
                    if (actor.FpfPreviewUsePlacementOffset)
                        ApplyPlacementOffset(actor.Skeleton, actor.FpfPreviewFile);

                    return;
                }

                FPF_Entry mainEntry = actor.FpfPreviewFile.GetMainSkeletonEntry();

                if (mainEntry?.BonePoses == null)
                {
                    Disable(actor, "The selected FPF does not have a main skeleton entry.");
                    return;
                }

                if (mainEntry.BonePoses.Count != actor.Skeleton.Bones.Length)
                {
                    Disable(actor, $"FPF bone count {mainEntry.BonePoses.Count} does not match actor skeleton bone count {actor.Skeleton.Bones.Length}.");
                    return;
                }

                ApplyEntryToSkeleton(actor.Skeleton, mainEntry, actor.FpfPreviewPoseMatrix, actor.FpfPreviewSkinOffsetMatrix, actor.FpfPreviewSkinOffsetMode);

                if (actor.FpfPreviewUsePlacementOffset)
                    ApplyPlacementOffset(actor.Skeleton, actor.FpfPreviewFile);
            }
            catch (Exception ex)
            {
                Disable(actor, ex.Message);
            }
        }

        public static void ApplyAdditionalSkeletons(Actor actor)
        {
            if (actor?.FpfPreviewFile == null)
                return;

            try
            {
                actor.PartSet?.ApplyFpfPreviewToPhysicsParts(actor.FpfPreviewFile, actor.FpfPreviewPoseMatrix, actor.FpfPreviewSkinOffsetMatrix, actor.FpfPreviewSkinOffsetMode);
            }
            catch (Exception ex)
            {
                Disable(actor, ex.Message);
            }
        }

        public static void ApplyEntryToSkeleton(Xv2Skeleton skeleton, FPF_Entry entry, FpfPoseMatrix poseMatrices, FpfPoseMatrix skinOffsetMatrices, FpfSkinOffsetMode skinOffsetMode)
        {
            if (skeleton == null || entry?.BonePoses == null || entry.BonePoses.Count != skeleton.Bones.Length)
                return;

            if (poseMatrices == FpfPoseMatrix.None && skinOffsetMatrices == FpfPoseMatrix.None)
                return;

            for (int i = 0; i < skeleton.Bones.Length; i++)
            {
                Matrix4x4 absoluteMatrix = skeleton.Bones[i].AbsoluteAnimationMatrix;

                if (poseMatrices != FpfPoseMatrix.None)
                {
                    absoluteMatrix = GetMultipliedTransform(entry.BonePoses[i], poseMatrices);
                    skeleton.Bones[i].AbsoluteAnimationMatrix = absoluteMatrix;
                }

                if (skinOffsetMatrices != FpfPoseMatrix.None)
                {
                    Matrix4x4 offsetMatrix = GetMultipliedTransform(entry.BonePoses[i], skinOffsetMatrices);
                    skeleton.Bones[i].SkinningMatrix = GetSkinningMatrix(skeleton, i, absoluteMatrix, offsetMatrix, skinOffsetMode);
                }
                else if (poseMatrices != FpfPoseMatrix.None)
                {
                    skeleton.Bones[i].SkinningMatrix = skeleton.Bones[i].InverseBindPoseMatrix * absoluteMatrix;
                }
            }
        }

        public static int BakeSkeletonPoseToEntry(Xv2Skeleton skeleton, FPF_Entry entry, FpfPoseBakeMode bakeMode)
        {
            if (skeleton == null || entry?.BonePoses == null || entry.BonePoses.Count != skeleton.Bones.Length)
                return 0;

            for (int i = 0; i < skeleton.Bones.Length; i++)
            {
                TransformMatrix4x4 relativeTransform = ToFpfMatrix(skeleton.Bones[i].RelativeMatrix);
                TransformMatrix4x4 localPoseTransform = ToFpfMatrix(GetLocalPoseMatrix(skeleton, i));
                TransformMatrix4x4 transform = ToFpfMatrix(skeleton.Bones[i].AbsoluteAnimationMatrix);
                TransformMatrix4x4 skinningTransform = ToFpfMatrix(GetFpfFormationSkinningMatrix(skeleton, i));

                entry.BonePoses[i].RelativeTransform = relativeTransform.Copy();
                entry.BonePoses[i].LocalPoseTransform = localPoseTransform.Copy();
                entry.BonePoses[i].AbsolutePoseTransform = transform.Copy();
                entry.BonePoses[i].AttachmentPoseTransform = transform.Copy();
                entry.BonePoses[i].FormationSkinningTransform = skinningTransform.Copy();
            }

            return 1;
        }

        public static int BakeScdSkeletonPoseToEntry(Xv2Skeleton skeleton, FPF_Entry entry, Matrix4x4 attachMatrix)
        {
            if (skeleton == null || entry?.BonePoses == null || entry.BonePoses.Count != skeleton.Bones.Length)
                return 0;

            Matrix4x4[] fpfAbsoluteMatrices = new Matrix4x4[skeleton.Bones.Length];
            Matrix4x4[] currentAbsoluteMatrices = new Matrix4x4[skeleton.Bones.Length];
            Matrix4x4 inverseAttachMatrix = Invert(attachMatrix);

            for (int i = 0; i < skeleton.Bones.Length; i++)
                currentAbsoluteMatrices[i] = GetCurrentScdAbsoluteMatrix(skeleton, i) * inverseAttachMatrix;

            for (int i = 0; i < skeleton.Bones.Length; i++)
            {
                Matrix4x4 fpfAbsoluteMatrix = currentAbsoluteMatrices[i];
                Matrix4x4 parentFpfMatrix = skeleton.Bones[i].ParentIndex >= 0 ? fpfAbsoluteMatrices[skeleton.Bones[i].ParentIndex] : Matrix4x4.Identity;
                Matrix4x4 localPoseMatrix = fpfAbsoluteMatrix * Invert(parentFpfMatrix);
                Matrix4x4 skinningMatrix = skeleton.Bones[i].InverseBindPoseMatrix * fpfAbsoluteMatrix;

                fpfAbsoluteMatrices[i] = fpfAbsoluteMatrix;

                TransformMatrix4x4 transform = ToFpfMatrix(fpfAbsoluteMatrix);
                TransformMatrix4x4 localPoseTransform = ToFpfMatrix(localPoseMatrix);
                TransformMatrix4x4 skinningTransform = ToFpfMatrix(Matrix4x4.Transpose(skinningMatrix));

                entry.BonePoses[i].RelativeTransform = ToFpfMatrix(skeleton.Bones[i].RelativeMatrix);
                entry.BonePoses[i].LocalPoseTransform = localPoseTransform.Copy();
                entry.BonePoses[i].AbsolutePoseTransform = transform.Copy();
                entry.BonePoses[i].AttachmentPoseTransform = transform.Copy();
                entry.BonePoses[i].FormationSkinningTransform = skinningTransform.Copy();
            }

            return 1;
        }

        private static Matrix4x4 GetCurrentScdAbsoluteMatrix(Xv2Skeleton skeleton, int boneIndex)
        {
            return skeleton.Bones[boneIndex].BindPoseMatrix * skeleton.Bones[boneIndex].SkinningMatrix;
        }

        public static void ApplyEntryToScdSkeleton(Xv2Skeleton skeleton, FPF_Entry entry, FpfPoseMatrix poseMatrices, FpfPoseMatrix skinOffsetMatrices, FpfSkinOffsetMode skinOffsetMode, int[] parentBoneIndices)
        {
            if (skeleton == null || entry?.BonePoses == null || entry.BonePoses.Count != skeleton.Bones.Length || parentBoneIndices == null)
                return;

            if (poseMatrices == FpfPoseMatrix.None && skinOffsetMatrices == FpfPoseMatrix.None)
                return;

            if (skinOffsetMode == FpfSkinOffsetMode.FpfFormationSkinningTransform && skinOffsetMatrices != FpfPoseMatrix.None)
            {
                Matrix4x4[] fpfAbsoluteMatrices = new Matrix4x4[skeleton.Bones.Length];

                for (int i = 0; i < skeleton.Bones.Length; i++)
                {
                    Matrix4x4 offsetMatrix = GetMultipliedTransform(entry.BonePoses[i], skinOffsetMatrices);
                    fpfAbsoluteMatrices[i] = skeleton.Bones[i].BindPoseMatrix * Matrix4x4.Transpose(offsetMatrix);
                }

                for (int i = 0; i < skeleton.Bones.Length; i++)
                {
                    if (i < parentBoneIndices.Length && parentBoneIndices[i] != -1)
                    {
                        skeleton.Bones[i].AbsoluteAnimationMatrix = skeleton.Bones[i].BindPoseMatrix * skeleton.Bones[i].SkinningMatrix;
                        continue;
                    }

                    Matrix4x4 parentMatrix = skeleton.Bones[i].Parent?.AbsoluteAnimationMatrix ?? Matrix4x4.Identity;
                    Matrix4x4 parentFpfMatrix = skeleton.Bones[i].ParentIndex >= 0
                        ? fpfAbsoluteMatrices[skeleton.Bones[i].ParentIndex]
                        : Matrix4x4.Identity;
                    Matrix4x4.Invert(parentFpfMatrix, out Matrix4x4 inverseParentFpfMatrix);

                    Matrix4x4 absoluteMatrix = fpfAbsoluteMatrices[i] * inverseParentFpfMatrix * parentMatrix;
                    skeleton.Bones[i].AbsoluteAnimationMatrix = absoluteMatrix;
                    skeleton.Bones[i].SkinningMatrix = skeleton.Bones[i].InverseBindPoseMatrix * absoluteMatrix;
                }

                return;
            }

            for (int i = 0; i < skeleton.Bones.Length; i++)
            {
                if (i < parentBoneIndices.Length && parentBoneIndices[i] != -1)
                {
                    skeleton.Bones[i].AbsoluteAnimationMatrix = skeleton.Bones[i].BindPoseMatrix * skeleton.Bones[i].SkinningMatrix;
                    continue;
                }

                Matrix4x4 fpfMatrix = poseMatrices != FpfPoseMatrix.None
                    ? GetMultipliedTransform(entry.BonePoses[i], poseMatrices)
                    : Matrix4x4.Identity;

                Matrix4x4 parentMatrix = skeleton.Bones[i].Parent?.AbsoluteAnimationMatrix ?? Matrix4x4.Identity;
                Matrix4x4 parentFpfMatrix = skeleton.Bones[i].ParentIndex >= 0 && poseMatrices != FpfPoseMatrix.None
                    ? GetMultipliedTransform(entry.BonePoses[skeleton.Bones[i].ParentIndex], poseMatrices)
                    : Matrix4x4.Identity;
                Matrix4x4.Invert(parentFpfMatrix, out Matrix4x4 inverseParentFpfMatrix);
                Matrix4x4 absoluteMatrix = fpfMatrix * inverseParentFpfMatrix * parentMatrix;

                if (poseMatrices != FpfPoseMatrix.None)
                    skeleton.Bones[i].AbsoluteAnimationMatrix = absoluteMatrix;

                if (skinOffsetMatrices != FpfPoseMatrix.None)
                {
                    Matrix4x4 offsetMatrix = GetMultipliedTransform(entry.BonePoses[i], skinOffsetMatrices);
                    skeleton.Bones[i].SkinningMatrix = GetSkinningMatrix(skeleton, i, absoluteMatrix, offsetMatrix, skinOffsetMode);
                }
                else if (poseMatrices != FpfPoseMatrix.None)
                {
                    skeleton.Bones[i].SkinningMatrix = skeleton.Bones[i].InverseBindPoseMatrix * absoluteMatrix;
                }
            }
        }

        public static void ApplyPlacementOffset(Xv2Skeleton skeleton, FPF_File fpfFile)
        {
            if (skeleton?.Bones == null || fpfFile == null)
                return;

            Matrix4x4 placementMatrix = Matrix4x4.CreateTranslation(fpfFile.FigurePositionX, fpfFile.FigurePositionY, fpfFile.FigurePositionZ);

            for (int i = 0; i < skeleton.Bones.Length; i++)
            {
                skeleton.Bones[i].AbsoluteAnimationMatrix *= placementMatrix;
                skeleton.Bones[i].SkinningMatrix *= placementMatrix;
            }
        }

        private static void Disable(Actor actor, string reason)
        {
            actor.FpfPreviewPath = null;
            actor.FpfPreviewFile = null;
            Log.Add($"FPF pose preview disabled: {reason}", LogType.Error);
        }

        private static Matrix4x4 GetSkinningMatrix(Xv2Skeleton skeleton, int boneIndex, Matrix4x4 absoluteMatrix, Matrix4x4 offsetMatrix, FpfSkinOffsetMode mode)
        {
            Matrix4x4 inverseBindPoseMatrix = skeleton.Bones[boneIndex].InverseBindPoseMatrix;

            switch (mode)
            {
                case FpfSkinOffsetMode.InverseBindOffsetPose:
                    return inverseBindPoseMatrix * offsetMatrix * absoluteMatrix;
                case FpfSkinOffsetMode.FpfFormationSkinningTransform:
                    return Matrix4x4.Transpose(offsetMatrix);
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        private static Matrix4x4 GetFpfFormationSkinningMatrix(Xv2Skeleton skeleton, int boneIndex)
        {
            return Matrix4x4.Transpose(skeleton.Bones[boneIndex].InverseBindPoseMatrix * skeleton.Bones[boneIndex].AbsoluteAnimationMatrix);
        }

        private static Matrix4x4 GetLocalPoseMatrix(Xv2Skeleton skeleton, int boneIndex)
        {
            Matrix4x4 parentMatrix = skeleton.Bones[boneIndex].Parent?.AbsoluteAnimationMatrix ?? Matrix4x4.Identity;
            return skeleton.Bones[boneIndex].AbsoluteAnimationMatrix * Invert(parentMatrix);
        }

        private static Matrix4x4 Invert(Matrix4x4 matrix)
        {
            Matrix4x4.Invert(matrix, out Matrix4x4 inverse);
            return inverse;
        }

        private static Matrix4x4 GetMultipliedTransform(FPF_BonePose bonePose, FpfPoseMatrix matrices)
        {
            Matrix4x4 matrix = Matrix4x4.Identity;

            if (matrices.HasFlag(FpfPoseMatrix.RelativeTransform))
                matrix *= ToMatrix(bonePose.RelativeTransform);

            if (matrices.HasFlag(FpfPoseMatrix.LocalPoseTransform))
                matrix *= ToMatrix(bonePose.LocalPoseTransform);

            if (matrices.HasFlag(FpfPoseMatrix.AbsolutePoseTransform))
                matrix *= ToMatrix(bonePose.AbsolutePoseTransform);

            if (matrices.HasFlag(FpfPoseMatrix.AttachmentPoseTransform))
                matrix *= ToMatrix(bonePose.AttachmentPoseTransform);

            if (matrices.HasFlag(FpfPoseMatrix.FormationSkinningTransform))
                matrix *= ToMatrix(bonePose.FormationSkinningTransform);

            return matrix;
        }

        private static Matrix4x4 ToMatrix(TransformMatrix4x4 matrix)
        {
            return new Matrix4x4(
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44);
        }

        private static TransformMatrix4x4 ToFpfMatrix(Matrix4x4 matrix)
        {
            return new TransformMatrix4x4
            {
                M11 = matrix.M11,
                M12 = matrix.M12,
                M13 = matrix.M13,
                M14 = matrix.M14,
                M21 = matrix.M21,
                M22 = matrix.M22,
                M23 = matrix.M23,
                M24 = matrix.M24,
                M31 = matrix.M31,
                M32 = matrix.M32,
                M33 = matrix.M33,
                M34 = matrix.M34,
                M41 = matrix.M41,
                M42 = matrix.M42,
                M43 = matrix.M43,
                M44 = matrix.M44
            };
        }

    }
}

