using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.ESK;

namespace XenoKit.Engine.Animation
{
    public class Bone
    {
        public string Name { get; set; }
        public Bone Parent { get; set; }

        public Matrix AbsoluteMatrix { get; set; }          //Relative to object's space
        public Matrix RelativeMatrix { get; set; }          //Relative to parent's bone
        public Matrix BindPoseMatrix { get; set; }          //AbsoluteMatrix on moment of link with skin to setup the Pose
        public Matrix BindPoseMatrix_Inv { get; set; }      //inverse of Bindpose due to performances, because used each update.
    }


    public class CharacterSkeleton
    {
        #region Fields

        private Matrix[] _boneAbsoluteMatrices = null;
        private Matrix[] _boneRelativeMatrices = null;
        private Matrix[] _boneBindPoseMatrices = null;
        private Matrix[] _boneBindPoseMatrices_inv = null;
        private int[] _skeletonHierarchy = null;

        #endregion

        public Bone[] Bones { get; set; }
        public Matrix[] BoneAbsoluteMatrices { get { if (_boneAbsoluteMatrices == null) { _boneAbsoluteMatrices = GetBoneAbsoluteMatrices(); } return _boneAbsoluteMatrices; } }
        public Matrix[] BoneRelativeMatrices { get { if (_boneRelativeMatrices == null) { _boneRelativeMatrices = GetBoneRelativeMatrices(); } return _boneRelativeMatrices; } }
        public Matrix[] BoneBindPoseMatrices { get { if (_boneBindPoseMatrices == null) { _boneBindPoseMatrices = GetBoneBindPoseMatrices(); } return _boneBindPoseMatrices; } }
        public Matrix[] BoneBindPoseMatrices_inv { get { if (_boneBindPoseMatrices_inv == null) { _boneBindPoseMatrices_inv = GetBoneBindPoseMatrices_inv(); } return _boneBindPoseMatrices_inv; } }
        public int[] SkeletonHierarchy { get { if (_skeletonHierarchy == null) { _skeletonHierarchy = GetSkeletonHierarchy(); } return _skeletonHierarchy; } }

        
        
        public CharacterSkeleton(ESK_File eskFile)
        {
            LoadSkeleton(eskFile);
        }
        
        public void LoadSkeleton(ESK_File eskFile)
        {
            var bones = eskFile.Skeleton.GetNonHierarchalBoneList();
            int boneCount = bones.Count;

            Bones = new Bone[boneCount];

            for(int i = 0; i < boneCount; i++)
            {
                int parentIdx = bones[i].Index1;

                Bones[i] = new Bone();
                Bones[i].Name = bones[i].Name;
                Bones[i].Parent = (parentIdx != -1) ? Bones[parentIdx] : null;

                Bones[i].RelativeMatrix = ConvertEskTransformToMatrix(bones[i].RelativeTransform);
            }

            
            UpdateAbsoluteMatrixFromRelative();


            for (int i = 0; i < boneCount; i++)
            {
                Bones[i].BindPoseMatrix = Bones[i].AbsoluteMatrix;
                Bones[i].BindPoseMatrix_Inv = Matrix.Invert(Bones[i].BindPoseMatrix);
            }
        }

        private void UpdateAbsoluteMatrixFromRelative()
        {
            for (int i = 0, boneCount = Bones.Length; i < boneCount; i++)
            {
                if (Bones[i].Parent != null)                                //keep only root bones
                    continue;

                Bones[i].AbsoluteMatrix = Bones[i].RelativeMatrix;

                _UpdateAbsoluteMatrixFromRelative_recursive(Bones[i]);
            }
        }

        private void _UpdateAbsoluteMatrixFromRelative_recursive(Bone parent)
        {
            for (int i = 0, boneCount = Bones.Length; i < boneCount; i++)
            {
                if (Bones[i].Parent != parent)
                    continue;

                Bones[i].AbsoluteMatrix = Bones[i].RelativeMatrix * parent.AbsoluteMatrix;

                _UpdateAbsoluteMatrixFromRelative_recursive(Bones[i]);
            }
        }
        
        private Matrix ConvertEskTransformToMatrix(ESK_RelativeTransform transform)
        {
            Matrix matrix = Matrix.Identity;
            matrix *= Matrix.CreateScale(new Vector3(transform.F_32, transform.F_36, transform.F_40) * transform.F_44);
            matrix *= Matrix.CreateFromQuaternion(new Quaternion(transform.F_16, transform.F_20, transform.F_24, transform.F_28));
            matrix *= Matrix.CreateTranslation(new Vector3(transform.F_00, transform.F_04, transform.F_08) * transform.F_12);
            return matrix;
        }

        private Matrix ConvertEskAbsoluteTransformToMatrix(ESK_AbsoluteTransform transform)
        {
            return new Matrix(transform.F_00, transform.F_04, transform.F_08, transform.F_12, transform.F_16, transform.F_20, transform.F_24, transform.F_28, transform.F_32, transform.F_36, transform.F_40, transform.F_44, transform.F_48, transform.F_52, transform.F_56, transform.F_60);
        }


        #region Helpers
        //Helper
        public int GetBoneIndex(string name)
        {
            for (int i = 0; i < Bones.Length; i++)
            {
                if (Bones[i].Name == name)
                {
                    return i;
                }
            }
            //return 0; //WHY WAS THIS 0?
            return -1;
        }
        

        private Matrix[] GetBoneAbsoluteMatrices() { Matrix[] bones = new Matrix[Bones.Length]; for (int i = 0; i < Bones.Length; i++) { bones[i] = Bones[i].AbsoluteMatrix; } return bones; }
        private Matrix[] GetBoneRelativeMatrices() { Matrix[] bones = new Matrix[Bones.Length]; for (int i = 0; i < Bones.Length; i++) { bones[i] = Bones[i].RelativeMatrix; } return bones; }
        private Matrix[] GetBoneBindPoseMatrices() { Matrix[] bones = new Matrix[Bones.Length]; for (int i = 0; i < Bones.Length; i++) { bones[i] = Bones[i].BindPoseMatrix; } return bones; }
        private Matrix[] GetBoneBindPoseMatrices_inv() { Matrix[] bones = new Matrix[Bones.Length]; for (int i = 0; i < Bones.Length; i++) { bones[i] = Bones[i].BindPoseMatrix_Inv; } return bones; }

        private int[] GetSkeletonHierarchy()
        {
            int[] bones = new int[Bones.Length];
            for (int i = 0; i < Bones.Length; i++)
                bones[i] = (Bones[i].Parent != null) ? GetBoneIndex(Bones[i].Parent.Name) : -1;
            return bones;
        }

        #endregion
    }
}
