using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Xv2CoreLib.BCS;
using Xv2CoreLib.EMA;
using Xv2CoreLib.ESK;

namespace XenoKit.Engine.Animation
{
    public class Xv2Skeleton
    {
        private readonly Dictionary<string, int> BoneIndexCache;
        public int BaseBoneIndex { get; private set; }
        public int PelvisBoneIndex { get; private set; }

        public ESK_File EskFile { get; private set; }
        public Skeleton EmoSkeleton { get; private set; }
        public Xv2Bone[] Bones { get; set; }

        //Bone Scale:
        private Body CurrentBoneScale = null;


        public Xv2Skeleton(ESK_File eskFile)
        {
            //Init bone index cache
            //Cache size should be able to accomodate SCDs, characters and stages.
            int cacheSize = eskFile.Skeleton.NonRecursiveBones.Count;

            if (cacheSize < 64)
                cacheSize = 64;
            else if (cacheSize <= 256)
                cacheSize = 256;

            BoneIndexCache = new Dictionary<string, int>(cacheSize);

            //Init skeleton
            EskFile = eskFile;
            LoadSkeletonFromEsk();
        }

        public Xv2Skeleton(Skeleton emoSkeleton)
        {
            BoneIndexCache = new Dictionary<string, int>(emoSkeleton.Bones.Count);
            EmoSkeleton = emoSkeleton;
            LoadSkeletonFromEmo();
        }

        #region LoadSkeleton
        private void LoadSkeleton()
        {
            if(EskFile != null)
            {
                LoadSkeletonFromEsk();
            }
            else if(EmoSkeleton != null)
            {
                LoadSkeletonFromEmo();
            }
        }

        private void LoadSkeletonFromEmo()
        {
            BoneIndexCache.Clear();

            Bones = new Xv2Bone[EmoSkeleton.Bones.Count];

            for(int i = 0; i < EmoSkeleton.Bones.Count; i++)
            {
                int parentIdx = (short)EmoSkeleton.Bones[i].ParentIndex;

                Bones[i] = new Xv2Bone();
                Bones[i].Name = EmoSkeleton.Bones[i].Name;
                Bones[i].Parent = (parentIdx != -1) ? Bones[parentIdx] : null;
                Bones[i].ParentIndex = parentIdx;

                Bones[i].RelativeMatrix = ConvertEmoMatrix(EmoSkeleton.Bones[i].RelativeMatrix);
            }

            UpdateAbsoluteMatrixFromRelative();

            for (int i = 0; i < EmoSkeleton.Bones.Count; i++)
            {
                Bones[i].BindPoseMatrix = Bones[i].AbsoluteMatrix;
                Bones[i].InverseBindPoseMatrix = Matrix.Invert(Bones[i].BindPoseMatrix);

                BoneIndexCache.Add(Bones[i].Name, i);
            }
        }

        private void LoadSkeletonFromEsk()
        {
            BoneIndexCache.Clear();

            //Always recreate the bone list when this function is called, since the bone structure may have changed
            EskFile.Skeleton.CreateNonRecursiveBoneList();
            int boneCount = EskFile.Skeleton.NonRecursiveBones.Count;

            Bones = new Xv2Bone[boneCount];

            for (int i = 0; i < boneCount; i++)
            {
                int parentIdx = EskFile.Skeleton.NonRecursiveBones[i].Index1;

                Bones[i] = new Xv2Bone();
                Bones[i].Name = EskFile.Skeleton.NonRecursiveBones[i].Name;
                Bones[i].Parent = (parentIdx != -1) ? Bones[parentIdx] : null;
                Bones[i].ParentIndex = parentIdx;
                Bones[i].IsFaceBone = EskFile.Skeleton.NonRecursiveBones[i].Name.StartsWith("f_");

                Bones[i].RelativeMatrix = ConvertEskTransformToMatrix(EskFile.Skeleton.NonRecursiveBones[i].RelativeTransform);

                if(Bones[i].Name == ESK_File.BaseBone)
                {
                    BaseBoneIndex = i;
                }
                else if (Bones[i].Name == ESK_File.PelvisBone)
                {
                    PelvisBoneIndex = i;
                }
            }

            UpdateAbsoluteMatrixFromRelative();

            for (int i = 0; i < boneCount; i++)
            {
                Bones[i].BindPoseMatrix = Bones[i].AbsoluteMatrix;
                Bones[i].InverseBindPoseMatrix = Matrix.Invert(Bones[i].BindPoseMatrix);
                Bones[i].SkinningMatrix = Bones[i].AbsoluteMatrix;

                BoneIndexCache.Add(Bones[i].Name, i);
            }

            UpdateBoneScale();
        }

        private void UpdateAbsoluteMatrixFromRelative()
        {
            for (int i = 0; i < Bones.Length; i++)
            {
                if (Bones[i].Parent != null)                                //keep only root bones
                    continue;

                Bones[i].AbsoluteMatrix = Bones[i].RelativeMatrix;

                UpdateAbsoluteMatrixFromRelative_recursive(Bones[i]);
            }
        }

        private void UpdateAbsoluteMatrixFromRelative_recursive(Xv2Bone parent)
        {
            for (int i = 0; i < Bones.Length; i++)
            {
                if (Bones[i].Parent != parent)
                    continue;

                Bones[i].AbsoluteMatrix = Bones[i].RelativeMatrix * parent.AbsoluteMatrix;

                UpdateAbsoluteMatrixFromRelative_recursive(Bones[i]);
            }
        }

        private Matrix ConvertEskTransformToMatrix(ESK_RelativeTransform transform)
        {
            Matrix matrix = Matrix.Identity;
            matrix *= Matrix.CreateScale(new Vector3(transform.ScaleX, transform.ScaleY, transform.ScaleZ) * transform.ScaleW);
            matrix *= Matrix.CreateFromQuaternion(new Quaternion(transform.RotationX, transform.RotationY, transform.RotationZ, transform.RotationW));
            matrix *= Matrix.CreateTranslation(new Vector3(transform.PositionX, transform.PositionY, transform.PositionZ) * transform.PositionW);
            return matrix;
        }

        private Matrix ConvertEmoMatrix(SkeletonMatrix transform)
        {
            return new Matrix(transform.M11, transform.M12, transform.M13, transform.M14, transform.M21, transform.M22, transform.M23, transform.M24, transform.M31, transform.M32, transform.M33, transform.M34, transform.M41, transform.M42, transform.M43, transform.M44);
        }

        #endregion

        #region BoneScale
        public void SetBoneScale(Body body)
        {
            CurrentBoneScale = body;
            UpdateBoneScale();
        }

        public void RemoveBoneScale()
        {
            CurrentBoneScale = null;
            UpdateBoneScale();
        }

        public void UpdateBoneScale()
        {
            //Set all to Identity
            for (int i = 0; i < Bones.Length; i++)
            {
                Bones[i].BoneScaleMatrix = Matrix.Identity;
            }

            if(CurrentBoneScale != null)
            {
                //Set scales
                for (int i = 0; i < Bones.Length; i++)
                {
                    BoneScale scale = CurrentBoneScale.BodyScales.FirstOrDefault(x => x.BoneName == Bones[i].Name);

                    if(scale != null)
                    {
                        Bones[i].BoneScaleMatrix = Matrix.CreateScale(new Vector3(scale.ScaleX, scale.ScaleY, scale.ScaleZ));
                    }
                }
            }
        }

        public bool HasBoneScale(Body body)
        {
            return CurrentBoneScale == body;
        }
        
        #endregion

        #region Helpers
        public int GetBoneIndex(string name)
        {
            if (name == null) return -1;
            int idx;

            //Bones are cached for better performance
            if (BoneIndexCache.TryGetValue(name, out idx))
                return idx;

            return -1;
        }

        public Xv2Bone GetBone(string name)
        {
            int idx = GetBoneIndex(name);
            return idx != -1 ? Bones[idx] : null;
        }

        public string GetParentBone(string name)
        {
            int idx = GetBoneIndex(name);

            if(idx != -1)
            {
                return Bones[GetBoneIndex(name)].Parent?.Name;
            }

            return null;
        }

        public Matrix[] GetAnimationMatrices()
        {
            Matrix[] bones = new Matrix[Bones.Length];

            for (int i = 0; i < Bones.Length; i++)
            {
                bones[i] = Bones[i].AnimationMatrix;
            }

            return bones;
        }

        public void CopySkinningState(Xv2Skeleton skinnedSkeleton)
        {
            Xv2Bone attach = skinnedSkeleton.GetBone("b_C_Chest");

            for (int i = 0; i < Bones.Length; i++)
            {
                Xv2Bone skinnedBone = skinnedSkeleton.GetBone(Bones[i].Name);

                if(skinnedBone != null)
                {
                    //Bone is present in skinned skeleton, so we copy from there
                    //Bones[i].SkinningMatrix = skinnedBone.AbsoluteAnimationMatrix * Bones[i].InverseBindPoseMatrix * Matrix.Invert(skinnedBone.AbsoluteMatrix);
                    //Bones[i].SkinningMatrix = Matrix.Identity;

                    int parentBone = skinnedBone.ParentIndex;
                    Bones[i].AbsoluteAnimationMatrix = Matrix.Identity;

                    while (parentBone != -1)
                    {
                        Bones[i].AbsoluteAnimationMatrix *= skinnedSkeleton.Bones[parentBone].AnimationMatrix;
                        parentBone = skinnedSkeleton.Bones[parentBone].ParentIndex;
                    }

                    //Bones[i].SkinningMatrix = skinnedBone.SkinningMatrix * attach.AbsoluteMatrix;
                    Bones[i].SkinningMatrix = Matrix.Identity;

                }
                else if(Bones[i].Parent != null)
                {
                    //Bone is a SCD-only bone. 
                    Bones[i].SkinningMatrix = Bones[i].Parent.SkinningMatrix;
                    //Bones[i].SkinningMatrix = Matrix.Identity;
                }
            }
        }
        #endregion
    }

    public class Xv2Bone
    {
        public string Name { get; set; }
        public Xv2Bone Parent { get; set; }
        public int ParentIndex { get; set; }

        //Skeleton Matrices:
        //These are created from the ESK and will remain static, unless the ESK is edited
        public Matrix AbsoluteMatrix { get; set; }          //Relative to objects space
        public Matrix RelativeMatrix { get; set; }          //Relative to parent bone
        public Matrix BindPoseMatrix { get; set; }          //AbsoluteMatrix on moment of link with skin to setup the Pose
        public Matrix InverseBindPoseMatrix { get; set; }   //Inverse of Bindpose

        //Animation Matrices:
        //These are updated each frame from the AnimationPlayer
        public Matrix SkinningMatrix { get; set; } = Matrix.Identity;
        public Matrix AnimationMatrix { get; set; } = Matrix.Identity;
        public Matrix AbsoluteAnimationMatrix { get; set; } = Matrix.Identity;

        //BoneScale Matrix:
        //This is set whenever a BCS bone scale is applied. AbsoluteAnimationMatrix will be scaled by this each frame when that is set
        public Matrix BoneScaleMatrix { get; set; } = Matrix.Identity;

        //Face bone awareness:
        public bool IsFaceBone { get; set; }
    }
}
