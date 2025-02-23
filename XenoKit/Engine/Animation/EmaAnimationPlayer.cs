using Microsoft.Xna.Framework;
using System;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Helper;
using Xv2CoreLib.EMA;
using Xv2CoreLib.ESK;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Engine.Animation
{
    public class EmaAnimationPlayer : AnimationPlayerBase
    {
        public EmaAnimationInstance PrimaryAnimation { get; private set; }

        protected override bool IsUsingAnimation => PrimaryAnimation != null;

        public float PrimaryCurrentFrame
        {
            get
            {
                if (PrimaryAnimation != null)
                    return PrimaryAnimation.CurrentFrame_Int;
                return 0;
            }
        }
        public float PrimaryDuration
        {
            get
            {
                if (PrimaryAnimation != null)
                    return PrimaryAnimation.EndFrame;
                return 0;
            }
        }
        private bool ExternalControl = false;

        public EmaAnimationPlayer(Xv2Skeleton skeleton, GameBase game) : base(game)
        {
            Skeleton = skeleton;
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;

            //TODO: Hook these up to EMA editor equivalents
            //SceneManager.AnimationDataChanged += SceneManager_AnimationDataChanged;
            //SceneManager.SeekOccurred += SceneManager_SeekOccurred;
        }

        private void SceneManager_SeekOccurred(object sender, EventArgs e)
        {
            //When Seek happens the FrameIndex caches are no longer valid and need to be reset.

            if (PrimaryAnimation != null)
                PrimaryAnimation.ResetFrameIndex();
        }

        private void Instance_UndoOrRedoCalled(object sender, UndoEventRaisedEventArgs e)
        {
            if (e.UndoGroup == UndoGroup.Animation)
                UpdateAnimationData();
        }

        private void SceneManager_AnimationDataChanged(object sender, EventArgs e)
        {
            UpdateAnimationData();
        }

        private void UpdateAnimationData()
        {
            if (PrimaryAnimation != null)
            {
                PrimaryAnimation.AnimationDataChanged();
            }
        }

        #region Main Methods
        /// <summary>
        /// Play an animation from a <see cref="EMA_File"/>.
        /// </summary>
        /// <param name="emaIndex">ID of the animation in the EMA file.</param>
        /// <param name="externalControl">Is the current frame controlled externally? (such as is the case with <see cref="Vfx.Asset.VfxEmo"/>). If true, then the animations current frame wont advanced internally and will only change with a call to <see cref="SetFrame(float)"/>.</param>
        public void PlayAnimation(EMA_File emaFile, ushort emaIndex, bool externalControl = false)
        {
            if(emaFile.Animations.Any(x => x.Index == emaIndex))
            {
                ExternalControl = externalControl;
                PrimaryAnimation = new EmaAnimationInstance(emaFile, emaIndex);
            }
            else
            {
                Log.Add($"[EMA] An animation could not be found at index {emaIndex}.", LogType.Warning);
            }
        }

        public void StopAnimation()
        {
            PrimaryAnimation = null;
        }

        public void SetFrame(float frame)
        {
            if (PrimaryAnimation != null)
                PrimaryAnimation.CurrentFrame = frame;
        }
        #endregion

        #region Update
        public void Update(Matrix rootTransform)
        {
            ClearPreviousFrame();

            //Update Matrices
            if (PrimaryAnimation != null)
                UpdateAnimation(PrimaryAnimation);

            //resulting of animation/relativetransform
            UpdateAbsoluteMatrix(rootTransform);

            //revert initialStateTransform when we make the link of skeleton and skin.
            UpdateSkinningMatrices();

            //Remove finished anims
            HandleFinishedAnimations();

            //Advance frame
            if (GameBase.IsPlaying && IsUsingAnimation && !ExternalControl)
            {
                AdvanceFrame();
            }
        }

        private void HandleFinishedAnimations()
        {
            if (PrimaryAnimation != null)
            {
                if (PrimaryAnimation.CurrentFrame >= PrimaryAnimation.EndFrame)
                {
                    //TODO: Add logic for EMA editor
                }

            }
        }

        private void AdvanceFrame(bool useTimeScale = true)
        {
            if (PrimaryAnimation != null)
            {
                if (PrimaryAnimation.CurrentFrame < PrimaryAnimation.EndFrame)
                {
                    //TimeScale will be handled by VfxEmo. AdvanceFrame here will just be for the EMA editor, so no time scale needed.
                    //float timeScale = (useTimeScale) ? SceneManager.BacTimeScale * SceneManager.MainAnimTimeScale : 1f;
                    //PrimaryAnimation.CurrentFrame += 1f * timeScale;
                    PrimaryAnimation.CurrentFrame += 1f;
                }
            }
        }

        private void UpdateAnimation(EmaAnimationInstance animation)
        {
            if (animation.Animation == null) return;

            for (int i = 0; i < animation.Animation.Nodes.Count; i++)
            {
                EMA_Node node = animation.Animation.Nodes[i];
                int boneIdx = Skeleton.GetBoneIndex(node.BoneName);
                if (boneIdx == -1)
                    continue;

                
                Bone bone = animation.EmaFile.Skeleton.GetBone(node.BoneName); // Bone from Ean file, we have to revert the relative Transform before we apply animation values (because animations values are for the inside ean file skeleton first)
                ESK_RelativeTransform transform = bone.EskRelativeTransform;

                Vector3 ean_initialBonePosition = new Vector3(transform.PositionX, transform.PositionY, transform.PositionZ) * transform.PositionW;
                Quaternion ean_initialBoneOrientation = new Quaternion(transform.RotationX, transform.RotationY, transform.RotationZ, transform.RotationW);
                Vector3 ean_initialBoneScale = new Vector3(transform.ScaleX, transform.ScaleY, transform.ScaleZ) * transform.ScaleW;
                Vector3 ema_initalEulerAngles = EngineUtils.QuaternionToEuler(ean_initialBoneOrientation);

                Matrix relativeMatrix_EanBone_inv = Matrix.Identity;
                relativeMatrix_EanBone_inv *= Matrix.CreateScale(ean_initialBoneScale);
                relativeMatrix_EanBone_inv *= Matrix.CreateFromQuaternion(ean_initialBoneOrientation);
                relativeMatrix_EanBone_inv *= Matrix.CreateTranslation(ean_initialBonePosition);
                relativeMatrix_EanBone_inv = Matrix.Invert(relativeMatrix_EanBone_inv);
                

                //Read components
                EMA_Command posX = node.GetCommand(0, 0);
                EMA_Command posY = node.GetCommand(0, 1);
                EMA_Command posZ = node.GetCommand(0, 2);
                EMA_Command rotX = node.GetCommand(1, 0);
                EMA_Command rotY = node.GetCommand(1, 1);
                EMA_Command rotZ = node.GetCommand(1, 2);
                EMA_Command scaleX = node.GetCommand(2, 0);
                EMA_Command scaleY = node.GetCommand(2, 1);
                EMA_Command scaleZ = node.GetCommand(2, 2);

                Matrix transformAnimation = Matrix.Identity;

                //Scale:
                float valueScaleX = scaleX != null ? scaleX.GetKeyframeValue(animation.CurrentFrame) : ean_initialBoneScale.X;
                float valueScaleY = scaleY != null ? scaleY.GetKeyframeValue(animation.CurrentFrame) : ean_initialBoneScale.Y;
                float valueScaleZ = scaleZ != null ? scaleZ.GetKeyframeValue(animation.CurrentFrame) : ean_initialBoneScale.Z;
                Vector3 scale_tmp = new Vector3(valueScaleX, valueScaleY, valueScaleZ);

                transformAnimation *= Matrix.CreateScale(scale_tmp);

                //Rotation:
                float valueRotX = rotX != null ? rotX.GetKeyframeValue(animation.CurrentFrame) : ema_initalEulerAngles.X;
                float valueRotY = rotY != null ? rotY.GetKeyframeValue(animation.CurrentFrame) : ema_initalEulerAngles.Y;
                float valueRotZ = rotZ != null ? rotZ.GetKeyframeValue(animation.CurrentFrame) : ema_initalEulerAngles.Z;
                Quaternion quat_tmp = GeneralHelpers.EulerAnglesToQuaternion(new Vector3(MathHelper.ToRadians(valueRotX), MathHelper.ToRadians(valueRotY), MathHelper.ToRadians(valueRotZ)));

                transformAnimation *= Matrix.CreateFromQuaternion(quat_tmp);

                //Position:
                float valuePosX = posX != null ? posX.GetKeyframeValue(animation.CurrentFrame) : ean_initialBonePosition.X;
                float valuePosY = posY != null ? posY.GetKeyframeValue(animation.CurrentFrame) : ean_initialBonePosition.Y;
                float valuePosZ = posZ != null ? posZ.GetKeyframeValue(animation.CurrentFrame) : ean_initialBonePosition.Z;
                Vector3 pos_tmp = new Vector3(valuePosX, valuePosY, valuePosZ);

                transformAnimation *= Matrix.CreateTranslation(pos_tmp);

                Skeleton.Bones[boneIdx].AnimationMatrix = transformAnimation * relativeMatrix_EanBone_inv;
                //Skeleton.Bones[boneIdx].AnimationMatrix = transformAnimation;
            }
        }

        #endregion
    }

    public class EmaAnimationInstance
    {
        public EMA_File EmaFile;
        public ushort EmaIndex;
        public EMA_Animation Animation;
        public float CurrentFrame;
        public int EndFrame;

        //Properties
        public int CurrentFrame_Int
        {
            get => (int)Math.Floor(CurrentFrame);
            set => CurrentFrame = value;
        }

        //Optimizations:
        //Here some animation-state values are saved for use in a later frame, instead of needlessly (and expensively) computing them each frame.
        //[Component][Command]
        public int[][] CurrentNodeFrameIndex_Pos;
        public int[][] CurrentNodeFrameIndex_Rot;
        public int[][] CurrentNodeFrameIndex_Scale;

        public EmaAnimationInstance(EMA_File emaFile, ushort emaIndex)
        {
            EmaFile = emaFile;
            EmaIndex = emaIndex;
            Animation = emaFile.Animations.FirstOrDefault(x => x.Index == emaIndex);

            if (Animation == null)
                Log.Add($"[EMA] An animation could not be found at index: {emaIndex}", LogType.Error);

            CurrentNodeFrameIndex_Pos = new int[3][];
            CurrentNodeFrameIndex_Rot = new int[3][];
            CurrentNodeFrameIndex_Scale = new int[3][];
            EndFrame = Animation.GetEndFrame();
        }

        public void AnimationDataChanged()
        {
            if (Animation == null) return;
            //Always recreate the arrays here as the node count may change
            CurrentNodeFrameIndex_Pos[0] = new int[Animation.Nodes.Count];
            CurrentNodeFrameIndex_Pos[1] = new int[Animation.Nodes.Count];
            CurrentNodeFrameIndex_Pos[2] = new int[Animation.Nodes.Count];
            CurrentNodeFrameIndex_Rot[0] = new int[Animation.Nodes.Count];
            CurrentNodeFrameIndex_Rot[1] = new int[Animation.Nodes.Count];
            CurrentNodeFrameIndex_Rot[2] = new int[Animation.Nodes.Count];
            CurrentNodeFrameIndex_Scale[0] = new int[Animation.Nodes.Count];
            CurrentNodeFrameIndex_Scale[1] = new int[Animation.Nodes.Count];
            CurrentNodeFrameIndex_Scale[2] = new int[Animation.Nodes.Count];
            EndFrame = Animation.GetEndFrame();
        }

        public void ResetFrameIndex()
        {
            if (Animation == null) return;

            //Use loops instead of recreating the arrays

            for (int i = 0; i < CurrentNodeFrameIndex_Pos[0].Length; i++)
            {
                CurrentNodeFrameIndex_Pos[0][i] = 0;
                CurrentNodeFrameIndex_Pos[1][i] = 0;
                CurrentNodeFrameIndex_Pos[2][i] = 0;
            }

            for (int i = 0; i < CurrentNodeFrameIndex_Rot.Length; i++)
            {
                CurrentNodeFrameIndex_Rot[0][i] = 0;
                CurrentNodeFrameIndex_Rot[1][i] = 0;
                CurrentNodeFrameIndex_Rot[2][i] = 0;
            }

            for (int i = 0; i < CurrentNodeFrameIndex_Scale.Length; i++)
            {
                CurrentNodeFrameIndex_Scale[0][i] = 0;
                CurrentNodeFrameIndex_Scale[1][i] = 0;
                CurrentNodeFrameIndex_Scale[2][i] = 0;
            }
        }

        public void SkipToFrame(int frame)
        {
            CurrentFrame = frame;
        }
    }
}
