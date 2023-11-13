using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Xv2CoreLib.EAN;
using Xv2CoreLib.ESK;
using static Xv2CoreLib.BAC.BAC_Type0;
using XenoKit.Editor;
using XenoKit.Helper;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Engine.Animation
{
    public abstract class AnimationPlayerBase : Entity
    {
        protected Xv2Skeleton Skeleton;
        protected virtual bool IsUsingAnimation => false;

        public AnimationPlayerBase(GameBase game) : base(game)
        {

        }

        protected void ClearPreviousFrame()
        {
            for (int i = 0; i < Skeleton.Bones.Length; i++)
            {
                Skeleton.Bones[i].AnimationMatrix = Matrix.Identity;
            }
        }

        protected void UpdateAbsoluteMatrix(Matrix rootTransform)
        {
            for (int i = 0; i < Skeleton.Bones.Length; i++)
            {
                int parentBone = i;
                Skeleton.Bones[i].AbsoluteAnimationMatrix = Matrix.Identity;

                while (parentBone != -1)
                {
                    Skeleton.Bones[i].AbsoluteAnimationMatrix *= Skeleton.Bones[parentBone].AnimationMatrix * Skeleton.Bones[parentBone].RelativeMatrix * Skeleton.Bones[parentBone].BoneScaleMatrix;
                    parentBone = Skeleton.Bones[parentBone].ParentIndex;
                }
            }
        }

        protected void UpdateSkinningMatrices()
        {
            if (!IsUsingAnimation)
            {
                for (int i = 0; i < Skeleton.Bones.Length; i++)
                {
                    Skeleton.Bones[i].SkinningMatrix = Matrix.Identity;
                }
            }
            else
            {
                for (int i = 0; i < Skeleton.Bones.Length; i++)
                {
                    Skeleton.Bones[i].SkinningMatrix = Skeleton.Bones[i].InverseBindPoseMatrix * Skeleton.Bones[i].AbsoluteAnimationMatrix;
                }
            }
        }

        #region Helpers
        public Matrix GetCurrentAbsoluteMatrix(string boneName)
        {
            int idx = Skeleton.GetBoneIndex(boneName);
            return Skeleton.Bones[idx > -1 ? idx : 0].AbsoluteAnimationMatrix;
        }

        /// <summary>
        /// Returns the parent absolute matrix for the specified bone, or in the case of the root bone that has no parent, <see cref="Matrix.Identity"/>.
        /// </summary>
        /// <param name="boneName"></param>
        /// <returns></returns>
        public Matrix GetCurrentParentAbsoluteMatrix(string boneName)
        {
            string parentBone = Skeleton.GetParentBone(boneName);
            return parentBone != null ? GetCurrentAbsoluteMatrix(parentBone) : Matrix.Identity;
        }
        #endregion
    }
}
