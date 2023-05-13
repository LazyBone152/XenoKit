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
    public class AnimationPlayer : AnimationPlayerBase
    {
        private readonly Actor Character;

        public AnimationInstance PrimaryAnimation = null;
        public List<AnimationInstance> SecondaryAnimations = new List<AnimationInstance>();

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
        public float PrimaryPrevFrame
        {
            get
            {
                if (PrimaryAnimation != null)
                    return PrimaryAnimation.PreviousFrame;
                return 0;
            }
        }
        public float PrimaryDuration
        {
            get
            {
                if (PrimaryAnimation != null)
                    return PrimaryAnimation.CurrentAnimDuration;
                return 0;
            }
        }


        public AnimationPlayer(Xv2Skeleton skeleton, Actor chara)
        {
            Skeleton = skeleton ?? throw new ArgumentNullException("skeleton", "AnimationPlayer: skeleton was null. Cannot construct an AnimationPlayer.");
            Character = chara;

            SceneManager.AnimationDataChanged += SceneManager_AnimationDataChanged;
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            SceneManager.SeekOccurred += SceneManager_SeekOccurred;
        }


        private void SceneManager_SeekOccurred(object sender, EventArgs e)
        {
            //When Seek happens the FrameIndex caches are no longer valid and need to be reset.

            if (PrimaryAnimation != null)
                PrimaryAnimation.ResetFrameIndex();

            foreach (var anim in SecondaryAnimations)
                anim.ResetFrameIndex();
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
            //Update EndFrame to match Animation.FrameCount
            if (PrimaryAnimation != null)
            {
                PrimaryAnimation.EndFrame = (PrimaryAnimation.Animation.FrameCount > 0) ? PrimaryAnimation.Animation.FrameCount - 1 : 0;
                PrimaryAnimation.AnimationDataChanged();
            }
        }

        #region FrameUpdating
        public void Update(Matrix rootTransform)
        {
            ClearPreviousFrame();

            //Update Matrices
            //todo: Test ingame and see if secondary face anims take priority over primary face anims when started on same frame, or later
            if (PrimaryAnimation != null)
                UpdateAnimation(PrimaryAnimation);

            //Do Secondary first, as they have lower priority than PrimaryAnimation
            for (int i = 0; i < SecondaryAnimations.Count; i++)
                UpdateAnimation(SecondaryAnimations[i]);

            //resulting of animation/relativetransform
            UpdateAbsoluteMatrix(rootTransform);

            //revert initialStateTransform when we make the link of skeleton and skin.
            UpdateSkinningMatrices();

            //Remove finished anims
            HandleFinishedAnimations();

            //Advance frame
            if (SceneManager.IsPlaying && IsUsingAnimation && PrimaryAnimation?.CurrentFrame < PrimaryAnimation?.EndFrame &&
                SceneManager.IsOnTab(EditorTabs.Animation, EditorTabs.Action))
            {
                AdvanceFrame();
            }
        }

        public void Simulate(bool fullUpdate, bool advance)
        {
            if (fullUpdate)
            {
                Update(Matrix.Identity);
            }
            else
            {
                HandleFinishedAnimations();
            }

            //Advance frame
            if (advance && IsUsingAnimation)
            {
                AdvanceFrame(false);
            }
        }

        private void HandleFinishedAnimations()
        {
            if (PrimaryAnimation != null)
            {
                if (PrimaryAnimation.CurrentFrame >= PrimaryAnimation.EndFrame)
                {
                    if (SceneManager.Loop && PrimaryAnimation.AutoTerminate && ((SceneManager.IsPlaying && SceneManager.IsOnTab(EditorTabs.Animation) || !SceneManager.IsOnTab(EditorTabs.Animation))))
                    {
                        PrimaryAnimation.CurrentFrame = PrimaryAnimation.StartFrame;
                    }
                    else if (SceneManager.IsOnTab(EditorTabs.Animation))
                    {
                        SceneManager.Pause();
                    }
                }

            }

            for (int i = SecondaryAnimations.Count - 1; i >= 0; i--)
            {
                if (SecondaryAnimations[i].CurrentFrame >= SecondaryAnimations[i].EndFrame)
                {
                    if (SecondaryAnimations[i].AutoTerminate)
                    {
                        SecondaryAnimations.RemoveAt(i);
                        continue;
                    }
                }
            }

        }

        private void AdvanceFrame(bool useTimeScale = true)
        {
            if (PrimaryAnimation != null)
            {
                if (PrimaryAnimation.CurrentFrame < PrimaryAnimation.EndFrame)
                {
                    PrimaryAnimation.PreviousFrame = PrimaryAnimation.CurrentFrame;

                    //We dont use TimeScale when simulating frames, so this is required
                    float timeScale = (useTimeScale) ? SceneManager.BacTimeScale * PrimaryAnimation.timeScale : 1f;
                    PrimaryAnimation.CurrentFrame += 1f * timeScale;
                }

            }

            for (int i = 0; i < SecondaryAnimations.Count; i++)
            {
                if (SecondaryAnimations[i].CurrentFrame < SecondaryAnimations[i].EndFrame)
                {
                    SecondaryAnimations[i].PreviousFrame = SecondaryAnimations[i].CurrentFrame;

                    float timeScale = (useTimeScale) ? SceneManager.BacTimeScale * SecondaryAnimations[i].timeScale : 1f;
                    SecondaryAnimations[i].CurrentFrame += 1f * timeScale;
                }

            }
        }

        /// <summary>
        /// Undo current animation b_C_Base transformation changes.
        /// </summary>
        /// <param name="frame">The frame from which to undo from.</param>
        /// <param name="undoAll">Undo everything, including ignoring bac flags.</param>
        private void UndoBasePosition(float frame, bool undoAll = false)
        {
            //Undo animation movement, if needed.
            if (PrimaryAnimation == null) return;
            if (!PrimaryAnimation.hasMoved) return; //Safeguard

            if (undoAll)
            {
                Character.animatedTransform = Matrix.Identity;
                return;
            }

            //Undo all rotation
            //Undo the positions (as required by flags)
            //Redo the rotation

            var node = PrimaryAnimation.Animation.GetNode(ESK_File.BaseBone);
            if (node == null) return;

            var pos = node.GetComponent(EAN_AnimationComponent.ComponentType.Position);

            float firstPosX = 0;
            float firstPosY = 0;
            float firstPosZ = 0;
            float firstPosW = 0;
            float PosX = 0;
            float PosY = 0;
            float PosZ = 0;
            float PosW = 0;

            if (pos != null)
            {
                firstPosX = pos.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.X);
                firstPosY = pos.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.Y);
                firstPosZ = pos.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.Z);
                firstPosW = pos.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.W);
                PosX = pos.GetKeyframeValue(frame, Axis.X);
                PosY = pos.GetKeyframeValue(frame, Axis.Y);
                PosZ = pos.GetKeyframeValue(frame, Axis.Z);
                PosW = pos.GetKeyframeValue(frame, Axis.W);
            }

            Matrix transformSum = Matrix.Identity;

            //Position
            if (PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.UseRootMotion) || !PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.MoveWithAxis_X) || !PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.MoveWithAxis_Y) || !PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.MoveWithAxis_Z))
            {

                if (!PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.MoveWithAxis_X) || PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.UseRootMotion))
                {
                    transformSum *= (PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.UseRootMotion)) ? Matrix.CreateTranslation(-PosX, 0, 0) : Matrix.CreateTranslation(-(PosX - firstPosX), 0, 0);
                }

                if (!PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.MoveWithAxis_Y) || PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.UseRootMotion))
                {
                    transformSum *= (PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.UseRootMotion)) ? Matrix.CreateTranslation(0, -PosY, 0) : Matrix.CreateTranslation(0, -(PosY - firstPosY), 0);
                }

                if (!PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.MoveWithAxis_Z) || PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.UseRootMotion))
                {
                    transformSum *= (PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.UseRootMotion)) ? Matrix.CreateTranslation(0, 0, -PosZ) : Matrix.CreateTranslation(0, 0, -(PosZ - firstPosZ));
                }

            }

            Character.animatedTransform *= transformSum;
            Character.MergeTransforms();
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Clear the current animation and its state.
        /// </summary>
        public void ClearCurrentAnimation(bool removeSecondary = false, bool undoPosition = false)
        {
            if (PrimaryAnimation != null && undoPosition && Character != null)
                UndoBasePosition(PrimaryAnimation.CurrentFrame, true);

            PrimaryAnimation = null;

            if (removeSecondary)
                SecondaryAnimations.Clear();
        }

        /// <summary>
        /// Load and play an animation.
        /// </summary>
        /// <param name="_eanFile">ean file to use animation from.</param>
        /// <param name="eanIndex">Index of animation in eanFile.</param>
        /// <param name="startFrame">Frame to start playback from.</param>
        /// <param name="endFrame">Frame to end the animation at. Note: -1 = play until animation ends.</param>
        /// <param name="timeScale">Playback speed of the animation. A value of 1 means normal playback.</param>
        public void PlayPrimaryAnimation(EAN_File _eanFile, int eanIndex, ushort startFrame = 0, ushort endFrame = ushort.MaxValue, float blendWeight = 1f, float blendWeightIncrease = 0f, AnimationFlags _animFlags = 0, bool useTransform = true, float timeScale = 1f, bool autoTerminate = false)
        {
            if (_eanFile == null) throw new ArgumentNullException("eanFile");

            if (_eanFile.AnimationExists(eanIndex))
            {
                if (PrimaryAnimation != null && Character != null)
                    UndoBasePosition(PrimaryAnimation.CurrentFrame, false);

                Matrix[] prevMatrices = (PrimaryAnimation != null) ? Skeleton.GetAnimationMatrices() : null;

                PrimaryAnimation = new AnimationInstance(_eanFile, eanIndex, startFrame, endFrame, blendWeight, blendWeightIncrease, prevMatrices, _animFlags, useTransform, timeScale, autoTerminate);

                //Render first frame if not auto playing
                if (!SceneManager.IsPlaying)
                    UpdateAnimation(PrimaryAnimation);
            }
            else
            {
                Log.Add($"An animation could not be found at index: {eanIndex}", LogType.Error);
            }
        }

        /// <summary>
        /// Load and play a secondary animation.
        /// </summary>
        /// <param name="_eanFile">ean file to use animation from.</param>
        /// <param name="eanIndex">Index of animation in eanFile.</param>
        /// <param name="startFrame">Frame to start playback from.</param>
        /// <param name="endFrame">Frame to end the animation at. Note: -1 = play until animation ends.</param>
        /// <param name="timeScale">Playback speed of the animation. A value of 1 means normal playback.</param>
        public void PlaySecondaryAnimation(EAN_File _eanFile, int eanIndex, ushort startFrame = 0, ushort endFrame = ushort.MaxValue, float blendWeight = 1f, float blendWeightIncrease = 0f, float timeScale = 1f, bool autoTerminate = false)
        {
            if (_eanFile == null) throw new ArgumentNullException("eanFile");


            Matrix[] prevMatrices = (PrimaryAnimation != null) ? Skeleton.GetAnimationMatrices() : null;

            SecondaryAnimations.Add(new AnimationInstance(_eanFile, eanIndex, startFrame, endFrame, blendWeight, blendWeightIncrease, prevMatrices, 0, true, timeScale, autoTerminate));
        }

        #endregion

        #region BoneUpdating
        private void UpdateAnimation(AnimationInstance animation)
        {
            if (animation.Animation == null) return;

            for (int i = 0; i < animation.Animation.Nodes.Count; i++)
            {
                var node = animation.Animation.Nodes[i];
                int boneIdx = Skeleton.GetBoneIndex(node.BoneName);
                if (boneIdx == -1) //Bone doesn't exist in character ESK, so skip
                    continue;

                ESK_Bone bone = animation.eanFile.Skeleton.GetBone(node.BoneName); // Bone from Ean file, we have to revert the relative Transform before we apply animation values (because animations values are for the inside ean file skeleton first)
                ESK_RelativeTransform transform = bone.RelativeTransform;

                Vector3 ean_initialBonePosition = new Vector3(transform.PositionX, transform.PositionY, transform.PositionZ) * transform.PositionW;
                Quaternion ean_initialBoneOrientation = new Quaternion(transform.RotationX, transform.RotationY, transform.RotationZ, transform.RotationW);
                Vector3 ean_initialBoneScale = new Vector3(transform.ScaleX, transform.ScaleY, transform.ScaleZ) * transform.ScaleW;

                //Scale animations to fit current actor size
                if (!animation.eanFile.IsCharaUnique && i == animation.b_C_Pelvis_Index)
                {
                    ean_initialBonePosition.Y -= (SceneManager.Actors[0].CharacterData.BcsFile.File.F_48[0] - 1f) / 2f;
                }

                Matrix relativeMatrix_EanBone_inv = Matrix.Identity;
                relativeMatrix_EanBone_inv *= Matrix.CreateScale(ean_initialBoneScale);
                relativeMatrix_EanBone_inv *= Matrix.CreateFromQuaternion(ean_initialBoneOrientation);
                relativeMatrix_EanBone_inv *= Matrix.CreateTranslation(ean_initialBonePosition);
                relativeMatrix_EanBone_inv = Matrix.Invert(relativeMatrix_EanBone_inv);


                if (i == animation.b_C_Base_Index && animation.useTransform)
                {
                    UpdateBasePosition(node, relativeMatrix_EanBone_inv, ean_initialBoneScale, ean_initialBoneOrientation, ean_initialBonePosition);
                    Skeleton.Bones[boneIdx].AnimationMatrix = Matrix.Identity;
                }
                else if (i == animation.b_C_Pelvis_Index && animation.AnimFlags.HasFlag(AnimationFlags.UseRootMotion))
                {
                    Skeleton.Bones[boneIdx].AnimationMatrix = Matrix.Identity;
                }
                else
                {
                    //Return values for current frame index
                    int frameIndex_Pos = 0;
                    int frameIndex_Rot = 0;
                    int frameIndex_Scale = 0;

                    //Read components

                    var pos = node.GetComponent(EAN_AnimationComponent.ComponentType.Position);
                    var rot = node.GetComponent(EAN_AnimationComponent.ComponentType.Rotation);
                    var scale = node.GetComponent(EAN_AnimationComponent.ComponentType.Scale);

                    Matrix transformAnimation = Matrix.Identity;

                    //Scale:
                    Vector3 scale_tmp = ean_initialBoneScale;

                    if (scale?.Keyframes.Count > 0)
                    {
                        float x = scale.GetKeyframeValue(animation.CurrentFrame, Axis.X, ref frameIndex_Scale, animation.CurrentNodeFrameIndex_Scale[i]);
                        float y = scale.GetKeyframeValue(animation.CurrentFrame, Axis.Y, ref frameIndex_Scale, animation.CurrentNodeFrameIndex_Scale[i]);
                        float z = scale.GetKeyframeValue(animation.CurrentFrame, Axis.Z, ref frameIndex_Scale, animation.CurrentNodeFrameIndex_Scale[i]);
                        float w = scale.GetKeyframeValue(animation.CurrentFrame, Axis.W, ref frameIndex_Scale, animation.CurrentNodeFrameIndex_Scale[i]);

                        scale_tmp = new Vector3(x, y, z) * w;
                    }

                    transformAnimation *= Matrix.CreateScale(scale_tmp);

                    //Rotation:
                    Quaternion quat_tmp = ean_initialBoneOrientation;
                    if (rot?.Keyframes.Count > 0)
                    {
                        float x = rot.GetKeyframeValue(animation.CurrentFrame, Axis.X, ref frameIndex_Rot, animation.CurrentNodeFrameIndex_Rot[i]);
                        float y = rot.GetKeyframeValue(animation.CurrentFrame, Axis.Y, ref frameIndex_Rot, animation.CurrentNodeFrameIndex_Rot[i]);
                        float z = rot.GetKeyframeValue(animation.CurrentFrame, Axis.Z, ref frameIndex_Rot, animation.CurrentNodeFrameIndex_Rot[i]);
                        float w = rot.GetKeyframeValue(animation.CurrentFrame, Axis.W, ref frameIndex_Rot, animation.CurrentNodeFrameIndex_Rot[i]);

                        quat_tmp = new Quaternion(x, y, z, w);
                    }

                    transformAnimation *= Matrix.CreateFromQuaternion(quat_tmp);

                    //Position:
                    Vector3 pos_tmp = ean_initialBonePosition;
                    if (pos?.Keyframes.Count > 0)
                    {
                        float x = pos.GetKeyframeValue(animation.CurrentFrame, Axis.X, ref frameIndex_Pos, animation.CurrentNodeFrameIndex_Pos[i]);
                        float y = pos.GetKeyframeValue(animation.CurrentFrame, Axis.Y, ref frameIndex_Pos, animation.CurrentNodeFrameIndex_Pos[i]);
                        float z = pos.GetKeyframeValue(animation.CurrentFrame, Axis.Z, ref frameIndex_Pos, animation.CurrentNodeFrameIndex_Pos[i]);
                        float w = pos.GetKeyframeValue(animation.CurrentFrame, Axis.W, ref frameIndex_Pos, animation.CurrentNodeFrameIndex_Pos[i]);

                        pos_tmp = new Vector3(x, y, z) * w;
                    }

                    transformAnimation *= Matrix.CreateTranslation(pos_tmp);

                    //Previous animation blending:
                    if (animation.PreviousAnimRelativeMatrices == null || animation.CurrentBlendWeight >= 1f || boneIdx == Skeleton.BaseBoneIndex)
                    {
                        //No blending if blendWeight is 1, is on b_C_Base or if no previous matrices data is present
                        Skeleton.Bones[boneIdx].AnimationMatrix = transformAnimation * relativeMatrix_EanBone_inv;
                    }
                    else
                    {
                        //Enable animation blending with previous animation matrices
                        Skeleton.Bones[boneIdx].AnimationMatrix = GeneralHelpers.SlerpMatrix(animation.PreviousAnimRelativeMatrices[boneIdx], transformAnimation * relativeMatrix_EanBone_inv, animation.CurrentBlendWeight);
                    }

                    //Update saved frame index.
                    animation.CurrentNodeFrameIndex_Pos[i] = frameIndex_Pos;
                    animation.CurrentNodeFrameIndex_Rot[i] = frameIndex_Rot;
                    animation.CurrentNodeFrameIndex_Scale[i] = frameIndex_Scale;

                    //Handle EYE animations
                    if (!Character.BacEyeMovementUsed)
                    {
                        if (i == animation.LeftEye_Index)
                        {
                            Character.EyeIrisLeft_UV[0] = -(pos_tmp.X * 10);
                            Character.EyeIrisLeft_UV[1] = -(pos_tmp.Y * 10);
                        }

                        if (i == animation.RightEye_Index)
                        {
                            Character.EyeIrisRight_UV[0] = -(pos_tmp.X * 10);
                            Character.EyeIrisRight_UV[1] = -(pos_tmp.Y * 10);
                        }
                    }
                }

            }
        }

        private void UpdateBasePosition(EAN_Node _base, Matrix relativeMatrix_EanBone_inv, Vector3 ean_initialBoneScale, Quaternion ean_initialBoneOrientation, Vector3 ean_initialBonePosition)
        {
            //Update character.Transform
            var pos = _base.GetComponent(EAN_AnimationComponent.ComponentType.Position);
            var rot = _base.GetComponent(EAN_AnimationComponent.ComponentType.Rotation);

            float firstPosX = 0;
            float firstPosY = 0;
            float firstPosZ = 0;
            float firstPosW = 0;
            float firstRotX = 0;
            float firstRotY = 0;
            float firstRotZ = 0;
            float firstRotW = 0;
            float PosX = 0;
            float PosY = 0;
            float PosZ = 0;
            float PosW = 0;
            float RotX = 0;
            float RotY = 0;
            float RotZ = 0;
            float RotW = 0;
            float PrevPosX = 0;
            float PrevPosY = 0;
            float PrevPosZ = 0;
            float PrevPosW = 0;
            float PrevRotX = 0;
            float PrevRotY = 0;
            float PrevRotZ = 0;
            float PrevRotW = 0;

            if (pos != null)
            {
                firstPosX = pos.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.X);
                firstPosY = pos.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.Y);
                firstPosZ = pos.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.Z);
                firstPosW = pos.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.W);
                PosX = pos.GetKeyframeValue(PrimaryAnimation.CurrentFrame, Axis.X);
                PosY = pos.GetKeyframeValue(PrimaryAnimation.CurrentFrame, Axis.Y);
                PosZ = pos.GetKeyframeValue(PrimaryAnimation.CurrentFrame, Axis.Z);
                PosW = pos.GetKeyframeValue(PrimaryAnimation.CurrentFrame, Axis.W);
                PrevPosX = pos.GetKeyframeValue(PrimaryAnimation.PreviousFrame, Axis.X);
                PrevPosY = pos.GetKeyframeValue(PrimaryAnimation.PreviousFrame, Axis.Y);
                PrevPosZ = pos.GetKeyframeValue(PrimaryAnimation.PreviousFrame, Axis.Z);
                PrevPosW = pos.GetKeyframeValue(PrimaryAnimation.PreviousFrame, Axis.W);
            }

            if (rot != null)
            {
                firstRotX = rot.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.X);
                firstRotY = rot.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.Y);
                firstRotZ = rot.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.Z);
                firstRotW = rot.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.W);
                RotX = rot.GetKeyframeValue(PrimaryAnimation.CurrentFrame, Axis.X);
                RotY = rot.GetKeyframeValue(PrimaryAnimation.CurrentFrame, Axis.Y);
                RotZ = rot.GetKeyframeValue(PrimaryAnimation.CurrentFrame, Axis.Z);
                RotW = rot.GetKeyframeValue(PrimaryAnimation.CurrentFrame, Axis.W);
                PrevRotX = rot.GetKeyframeValue(PrimaryAnimation.PreviousFrame, Axis.X);
                PrevRotY = rot.GetKeyframeValue(PrimaryAnimation.PreviousFrame, Axis.Y);
                PrevRotZ = rot.GetKeyframeValue(PrimaryAnimation.PreviousFrame, Axis.Z);
                PrevRotW = rot.GetKeyframeValue(PrimaryAnimation.PreviousFrame, Axis.W);
            }

            Matrix transformSum = Matrix.Identity;
            //UndoBasePosition(PrimaryAnimation.PreviousFrame, true);

            transformSum *= Matrix.CreateScale(ean_initialBoneScale);

            if (PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.FullRootMotion))
            {
                //Use full b_C_Base position
                //transformSum *= Matrix.CreateFromQuaternion(new Quaternion(RotX - PrevRotX, RotY - PrevRotY, RotZ - PrevRotZ, RotW - PrevRotW));
                //transformSum *= Matrix.CreateTranslation(new Vector3(PosX - PrevPosX, PosY - PrevPosY, PosZ - PrevPosZ) * PosW);

                transformSum *= Matrix.CreateFromQuaternion(new Quaternion(RotX, RotY, RotZ, RotW));
                transformSum *= Matrix.CreateTranslation(new Vector3(PosX, PosY, PosZ) * PosW);
            }
            else
            {
                //Use subtracted position

                //Set absolute values
                transformSum *= Matrix.CreateFromQuaternion(new Quaternion(RotX, RotY, RotZ, RotW));
                transformSum *= Matrix.CreateTranslation(new Vector3(PosX, PosY, PosZ) * PosW);

                //Subtract the first frame
                transformSum *= Matrix.CreateFromQuaternion(new Quaternion(-firstRotX, -firstRotY, -firstRotZ, -firstRotW));
                transformSum *= Matrix.CreateTranslation(new Vector3(-firstPosX, -firstPosY, -firstPosZ) * firstPosW);

                //buggy... will teleport
                //transformSum *= Matrix.CreateFromQuaternion(new Quaternion((RotX), (RotY), (RotZ), (RotW)));
                //transformSum *= Matrix.CreateTranslation((new Vector3((PosX), (PosY), (PosZ)) * (PosW)) - (new Vector3(firstPosX, firstPosY, firstPosZ) * firstPosW));

            }

            //character.Transform = ((transformSum * relativeMatrix_EanBone_inv) * boneBindPoseMatrices_inv[bcBase]);
            //character.Transform *= ((transformSum * relativeMatrix_EanBone_inv) * boneBindPoseMatrices_inv[bcBase]);
            Character.animatedTransform = transformSum;
            PrimaryAnimation.hasMoved = true;
        }
        #endregion

        #region Helpers

        public void Resume()
        {
            if (PrimaryCurrentFrame >= PrimaryDuration && PrimaryAnimation != null && SceneManager.IsOnTab(EditorTabs.Animation))
            {
                PrimaryAnimation.CurrentFrame_Int = 0;
            }
        }

        public void NextFrame()
        {
            if (PrimaryAnimation == null) return;
            if (PrimaryAnimation.CurrentFrame_Int < PrimaryAnimation.EndFrame)
            {
                PrimaryAnimation.PreviousFrame = PrimaryAnimation.CurrentFrame;
                PrimaryAnimation.CurrentFrame_Int++;
            }
            else if (PrimaryAnimation.CurrentFrame_Int >= PrimaryAnimation.EndFrame)
            {
                PrimaryAnimation.PreviousFrame = PrimaryAnimation.CurrentFrame;
                PrimaryAnimation.CurrentFrame_Int = PrimaryAnimation.StartFrame;
            }

            Update(Matrix.Identity);
        }

        public void PrevFrame()
        {
            if (PrimaryAnimation == null) return;
            if (PrimaryAnimation.CurrentFrame_Int > PrimaryAnimation.StartFrame)
            {
                PrimaryAnimation.PreviousFrame = PrimaryAnimation.CurrentFrame;
                PrimaryAnimation.CurrentFrame_Int--;
            }
            else if (PrimaryAnimation.CurrentFrame_Int <= PrimaryAnimation.StartFrame)
            {
                PrimaryAnimation.PreviousFrame = PrimaryAnimation.CurrentFrame;
                PrimaryAnimation.CurrentFrame_Int = PrimaryAnimation.EndFrame;
            }

            Update(Matrix.Identity);
        }

        public void FirstFrame()
        {
            if (PrimaryAnimation?.CurrentFrame > PrimaryAnimation?.StartFrame)
                PrimaryAnimation.CurrentFrame = PrimaryAnimation.StartFrame;
        }

        #endregion
    }

    public class AnimationInstance
    {
        public EAN_File eanFile;
        public int EanIndex;
        public EAN_Animation Animation;
        public float CurrentFrame;
        public float PreviousFrame;
        public int StartFrame;
        public int EndFrame;
        public float timeScale;
        public bool AutoTerminate;

        //Blending
        public Matrix[] PreviousAnimRelativeMatrices = null;
        public float BlendWeight = 1f;
        public float BlendWeightIncreasePerFrame = 0f;
        public float CurrentBlendWeight
        {
            get
            {
                return MathHelper.Clamp(BlendWeight + (BlendWeightIncreasePerFrame * (CurrentFrame - StartFrame)), 0f, 1f);
            }
        }

        //World Movement
        public AnimationFlags AnimFlags;
        public bool hasMoved = false;
        public bool useTransform;

        //Properties
        public int CurrentFrame_Int
        {
            get
            {
                return (int)Math.Floor(CurrentFrame);
            }
            set
            {
                if (CurrentFrame != value)
                {
                    CurrentFrame = value;
                }
            }
        }
        public int CurrentAnimDuration
        {
            get
            {
                return EndFrame - StartFrame;
            }
        }

        //Optimizations:
        //Here some animation-state values are saved for use in a later frame, instead of needlessly (and expensively) computing them each frame.
        /// <summary>
        /// Per animation node. This is the Keyframe index in the keyframe list (not actual frame)
        /// </summary>
        public int[] CurrentNodeFrameIndex_Pos;
        /// <summary>
        /// Per animation node. This is the Keyframe index in the keyframe list (not actual frame)
        /// </summary>
        public int[] CurrentNodeFrameIndex_Rot;
        /// <summary>
        /// Per animation node. This is the Keyframe index in the keyframe list (not actual frame)
        /// </summary>
        public int[] CurrentNodeFrameIndex_Scale;

        /// <summary>
        /// Index of b_C_Base node.
        /// </summary>
        public int b_C_Base_Index;
        /// <summary>
        /// Index of b_C_Pelvis node.
        /// </summary>
        public int b_C_Pelvis_Index;
        /// <summary>
        /// Index of f_L_EyeIris
        /// </summary>
        public int LeftEye_Index;
        /// <summary>
        /// Index of f_R_EyeIris
        /// </summary>
        public int RightEye_Index;

        public AnimationInstance(EAN_File _eanFile, int index, int startFrame = 0, int endFrame = -1, float blendWeight = 1f, float blendWeightIncrease = 0f, Matrix[] previousMatrices = null, AnimationFlags _animFlags = 0, bool _useTransform = true, float timeScale = 1f, bool autoTerminate = false)
        {
            eanFile = _eanFile;
            EanIndex = index;
            StartFrame = startFrame;
            EndFrame = endFrame;
            CurrentFrame = startFrame;
            PreviousFrame = startFrame;
            Animation = eanFile.GetAnimation(index, true);
            BlendWeight = MathHelper.Clamp(blendWeight, 0f, 1f);
            BlendWeightIncreasePerFrame = MathHelper.Clamp(blendWeightIncrease, 0f, 1f);
            PreviousAnimRelativeMatrices = previousMatrices;
            AnimFlags = _animFlags;
            useTransform = _useTransform;
            this.timeScale = timeScale;
            AutoTerminate = autoTerminate;

            if (Animation == null)
                Log.Add($"An animation could not be found at index: {index}", LogType.Error);

            if ((EndFrame == -1 || EndFrame == ushort.MaxValue) && Animation != null)
                EndFrame = Animation.FrameCount - 1;

            AnimationDataChanged();
        }

        public void AnimationDataChanged()
        {
            //Set indexes
            b_C_Base_Index = Animation.Nodes.IndexOf(Animation.Nodes.FirstOrDefault(x => x.BoneName == ESK_File.BaseBone));
            b_C_Pelvis_Index = Animation.Nodes.IndexOf(Animation.Nodes.FirstOrDefault(x => x.BoneName == ESK_File.PelvisBone));
            LeftEye_Index = Animation.Nodes.IndexOf(Animation.Nodes.FirstOrDefault(x => x.BoneName == ESK_File.LeftEyeIrisBone));
            RightEye_Index = Animation.Nodes.IndexOf(Animation.Nodes.FirstOrDefault(x => x.BoneName == ESK_File.RightEyeIrisBone));

            //Nodes:
            //Always recreate the arrays here as the node count may change
            CurrentNodeFrameIndex_Pos = new int[Animation.Nodes.Count];
            CurrentNodeFrameIndex_Rot = new int[Animation.Nodes.Count];
            CurrentNodeFrameIndex_Scale = new int[Animation.Nodes.Count];

        }

        public void ResetFrameIndex()
        {
            //Use loops instead of recreating the arrays

            for (int i = 0; i < CurrentNodeFrameIndex_Pos.Length; i++)
                CurrentNodeFrameIndex_Pos[i] = 0;

            for (int i = 0; i < CurrentNodeFrameIndex_Rot.Length; i++)
                CurrentNodeFrameIndex_Rot[i] = 0;

            for (int i = 0; i < CurrentNodeFrameIndex_Scale.Length; i++)
                CurrentNodeFrameIndex_Scale[i] = 0;
        }

        public void SkipToFrame(int frame)
        {
            PreviousFrame = frame;
            CurrentFrame = frame;
        }
    }
}
