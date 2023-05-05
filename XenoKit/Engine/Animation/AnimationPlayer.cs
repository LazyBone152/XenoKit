using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Xv2CoreLib.EAN;
using XenoKit.Editor;
using XenoKit.Helper;
using static Xv2CoreLib.BAC.BAC_Type0;
using Xv2CoreLib.ESK;

namespace XenoKit.Engine.Animation
{
    /// <summary>
    /// The animation player is in charge of decoding bone position matrices from an animation clip.
    /// </summary>
    public class AnimationPlayer
    {
        //Mostly Olga's handywork, except the world positioning, TimeScale and Blending parts

        #region Fields
        // Backlink to the bind pose and skeleton hierarchy data.
        private CharacterSkeleton skeleton;
        private Character character;

        public AnimationInstance PrimaryAnimation = null;
        public List<AnimationInstance> SecondaryAnimations = new List<AnimationInstance>();

        //private float timeScaleNextFrame = 1f;
        private bool isUsingAnimation
        {
            get
            {
                return (PrimaryAnimation != null);
            }
        }
       
        // Current animation transform matrices.
        private Matrix[] boneBindPoseMatrices_inv;
        private Matrix[] relativeMatrices;                                                  //TPose
        private Matrix[] relativeAnimationTransformMatrices;                                //animations's values are relative to current bone (not animated)
        private Matrix[] currentAbsoluteMatrices;
        private Matrix[] skinningMatrices;
        private Matrix[] debugBones;

        private int bcBase;
        #endregion

        #region Properties
        public Matrix[] GetSkinningMatrices() { return skinningMatrices; }          // current matrix to update vertex, about bone move (from initiaState for link skin)
        public Matrix[] GetDebugBoneMatrices() { return debugBones; }          // current matrix to update vertex, about bone move (from initiaState for link skin)
        public Matrix[] SkinningMatrices { get { return skinningMatrices; } }

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
                    return PrimaryAnimation.CurrentAnimDuration;
                return 0;
            }
        }

        #endregion


        public AnimationPlayer(CharacterSkeleton skeleton, Character chara)
        {
            this.skeleton = skeleton ?? throw new ArgumentNullException("skeleton", "AnimationPlayer: skeleton was null. Cannot construct an AnimationPlayer.");
            character = chara;

            int nbBones = skeleton.Bones.Length;
            skinningMatrices = new Matrix[nbBones];
            relativeAnimationTransformMatrices = new Matrix[nbBones];
            currentAbsoluteMatrices = new Matrix[nbBones];
            debugBones = new Matrix[nbBones];
            
            for (int i = 0; i < nbBones; i++)
            {
                skinningMatrices[i] = Matrix.Identity;
                debugBones[i] = Matrix.Identity;
            }

            boneBindPoseMatrices_inv = this.skeleton != null ? this.skeleton.BoneBindPoseMatrices_inv : null;
            relativeMatrices = this.skeleton != null ? this.skeleton.BoneRelativeMatrices : null;

            bcBase = skeleton.GetBoneIndex("b_C_Base");
            SceneManager.AnimationDurationChanged += SceneManager_AnimationDurationChanged;
        }

        private void SceneManager_AnimationDurationChanged(object sender, EventArgs e)
        {
            //Update EndFrame to match Animation.FrameCount
            if(SceneManager.CurrentSceneState == EditorTabs.Animation && PrimaryAnimation != null)
            {
                PrimaryAnimation.EndFrame = PrimaryAnimation.Animation.FrameCount;
            }
        }

        #region FrameUpdating
        public void Update(Matrix rootTransform) 
        {
            ClearPreviousFrame();

            //Update Matrices
            //Do Secondary first, as they have lower priority than PrimaryAnimation
            for (int i = 0; i < SecondaryAnimations.Count; i++)
                UpdateAnimation(SecondaryAnimations[i]);

            //todo: Test ingame and see if secondary face anims take priority over primary face anims when started on same frame, or later
            if (PrimaryAnimation != null)
                UpdateAnimation(PrimaryAnimation);


            //resulting of animation/relativetransform
            UpdateAbsoluteMatrix(rootTransform);

            //revert initialStateTransform when we make the link of skeleton and skin.
            UpdateSkinningMatrices();

            //Remove finished anims
            HandleFinishedAnimations();

            //Advance frame
            if (SceneManager.IsPlaying && isUsingAnimation && PrimaryAnimation?.CurrentFrame < PrimaryAnimation?.EndFrame && 
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
            if (advance && isUsingAnimation)
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
                    if (SceneManager.Loop && PrimaryAnimation.AutoTerminate)
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
                if(PrimaryAnimation.CurrentFrame < PrimaryAnimation.EndFrame)
                {
                    PrimaryAnimation.PreviousFrame = PrimaryAnimation.CurrentFrame;

                    //We dont use TimeScale when simulating frames, so this is required
                    float timeScale = (useTimeScale) ? SceneManager.BacTimeScale * PrimaryAnimation.timeScale : 1f;
                    PrimaryAnimation.CurrentFrame += 1f * timeScale;
                }
                
            }

            for(int i = 0; i < SecondaryAnimations.Count; i++)
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
                character.animatedTransform = Matrix.Identity;
                return;
            }

            //Undo all rotation
            //Undo the positions (as required by flags)
            //Redo the rotation

            var node = PrimaryAnimation.Animation.GetNode("b_C_Base");
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

            character.animatedTransform *= transformSum;
            character.MergeTransforms();
        }
        
        #endregion

        #region Public Methods
        /// <summary>
        /// Clear the current animation and its state.
        /// </summary>
        public void ClearCurrentAnimation(bool removeSecondary = false, bool undoPosition = false)
        {
            if (PrimaryAnimation != null && undoPosition)
                UndoBasePosition(PrimaryAnimation.CurrentFrame, true);

            PrimaryAnimation = null;

            if(removeSecondary)
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

            if(PrimaryAnimation != null)
                UndoBasePosition(PrimaryAnimation.CurrentFrame, false);

            Matrix[] prevMatrices = (PrimaryAnimation != null) ? (Matrix[])(relativeAnimationTransformMatrices.Clone()) : null;

            PrimaryAnimation = new AnimationInstance(_eanFile, eanIndex, startFrame, endFrame, blendWeight, blendWeightIncrease, prevMatrices, _animFlags, useTransform, timeScale, autoTerminate);

            //Render first frame if not auto playing
            if (!SceneManager.IsPlaying)
                UpdateAnimation(PrimaryAnimation);
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

            Matrix[] prevMatrices = (PrimaryAnimation != null) ? (Matrix[])(relativeAnimationTransformMatrices.Clone()) : null;

            SecondaryAnimations.Add(new AnimationInstance(_eanFile, eanIndex, startFrame, endFrame, blendWeight, blendWeightIncrease, prevMatrices, 0, true, timeScale, autoTerminate));
        }
        
        #endregion

        #region BoneUpdating
        private void ClearPreviousFrame()
        {
            for (int i = 0, nbBones = relativeAnimationTransformMatrices.Length; i < nbBones; i++)  //first clean previous animations:
                relativeAnimationTransformMatrices[i] = Matrix.Identity;
        }

        private void UpdateAnimation(AnimationInstance animation)
        {
            if (animation.Animation == null) return;

            foreach (var node in animation.Animation.Nodes)
            {
                int boneIdx = skeleton.GetBoneIndex(node.BoneName);
                if (boneIdx == -1)                                                      //If it is -1 then that bone doesn't exist in the character skeleton, so we wont be animating it.
                    continue;


                ESK_Bone bone = animation.eanFile.Skeleton.GetBone(node.BoneName);                        // bone from Ean file, we have to revert the relative Transform before apply animation values (because animations values are for the inside ean file skeleton first)
                ESK_RelativeTransform transform = bone.RelativeTransform;

                Vector3 ean_initialBonePosition = new Vector3(transform.F_00, transform.F_04, transform.F_08) * transform.F_12;
                Vector3 ean_initialBonePosition_inv = -ean_initialBonePosition;

                Quaternion ean_initialBoneOrientation = new Quaternion(transform.F_16, transform.F_20, transform.F_24, transform.F_28);
                Quaternion ean_initialBoneOrientation_inv = Quaternion.Inverse(ean_initialBoneOrientation);

                Vector3 ean_initialBoneScale = new Vector3(transform.F_32, transform.F_36, transform.F_40) * transform.F_44;
                Vector3 ean_initialBoneScale_inv = ean_initialBoneScale;
                if ((!ean_initialBoneScale_inv.Equals(Vector3.Zero)) && (!ean_initialBoneScale_inv.Equals(Vector3.One)))           //test Vector3(1,1,1) to avoid approximations
                    ean_initialBoneScale_inv = new Vector3(1.0f / ean_initialBoneScale_inv.X, 1.0f / ean_initialBoneScale_inv.Y, 1.0f / ean_initialBoneScale_inv.Z);

                Matrix relativeMatrix_EanBone_inv = Matrix.Identity;
                relativeMatrix_EanBone_inv *= Matrix.CreateScale(ean_initialBoneScale);
                relativeMatrix_EanBone_inv *= Matrix.CreateFromQuaternion(ean_initialBoneOrientation);
                relativeMatrix_EanBone_inv *= Matrix.CreateTranslation(ean_initialBonePosition);
                relativeMatrix_EanBone_inv = Matrix.Invert(relativeMatrix_EanBone_inv);

                if (node.BoneName == "b_C_Base" && animation.useTransform)
                {
                    UpdateBasePosition(node, relativeMatrix_EanBone_inv, ean_initialBoneScale, ean_initialBoneOrientation, ean_initialBonePosition);
                    relativeAnimationTransformMatrices[boneIdx] = Matrix.Identity;
                }
                else if (node.BoneName == "b_C_Pelvis" && animation.AnimFlags.HasFlag(AnimationFlags.UseRootMotion))
                {
                    relativeAnimationTransformMatrices[boneIdx] = Matrix.Identity;
                }
                else
                {
                    //Read components

                    var pos = node.GetComponent(EAN_AnimationComponent.ComponentType.Position);
                    var rot = node.GetComponent(EAN_AnimationComponent.ComponentType.Rotation);
                    var scale = node.GetComponent(EAN_AnimationComponent.ComponentType.Scale);

                    Matrix transformAnimation = Matrix.Identity;

                    {
                        Vector3 scale_tmp = ean_initialBoneScale;

                        if (scale?.Keyframes.Count > 0)
                        {
                            float x = scale.GetKeyframeValue(animation.CurrentFrame, Axis.X);
                            float y = scale.GetKeyframeValue(animation.CurrentFrame, Axis.Y);
                            float z = scale.GetKeyframeValue(animation.CurrentFrame, Axis.Z);
                            float w = scale.GetKeyframeValue(animation.CurrentFrame, Axis.W);

                            scale_tmp = new Vector3(x, y, z) * w;
                        }

                        transformAnimation *= Matrix.CreateScale(scale_tmp);
                    }

                    {
                        Quaternion quat_tmp = ean_initialBoneOrientation;
                        if (rot?.Keyframes.Count > 0)
                        {
                            float x = rot.GetKeyframeValue(animation.CurrentFrame, Axis.X);
                            float y = rot.GetKeyframeValue(animation.CurrentFrame, Axis.Y);
                            float z = rot.GetKeyframeValue(animation.CurrentFrame, Axis.Z);
                            float w = rot.GetKeyframeValue(animation.CurrentFrame, Axis.W);

                            quat_tmp = new Quaternion(x, y, z, w);
                        }

                        transformAnimation *= Matrix.CreateFromQuaternion(quat_tmp);
                    }

                    {
                        Vector3 pos_tmp = ean_initialBonePosition;
                        if (pos?.Keyframes.Count > 0)
                        {
                            float x = pos.GetKeyframeValue(animation.CurrentFrame, Axis.X);
                            float y = pos.GetKeyframeValue(animation.CurrentFrame, Axis.Y);
                            float z = pos.GetKeyframeValue(animation.CurrentFrame, Axis.Z);
                            float w = pos.GetKeyframeValue(animation.CurrentFrame, Axis.W);

                            pos_tmp = new Vector3(x, y, z) * w;
                        }

                        transformAnimation *= Matrix.CreateTranslation(pos_tmp);
                    }

                    if (animation.PreviousAnimRelativeMatrices == null || animation.CurrentBlendWeight >= 1f || boneIdx == bcBase)
                    {
                        //No blending if blendWeight is 1, is on b_C_Base or if no previous matrices data is present
                        relativeAnimationTransformMatrices[boneIdx] = transformAnimation * relativeMatrix_EanBone_inv;
                    }
                    else
                    {
                        //Enable animation blending with previous animation matrices
                        relativeAnimationTransformMatrices[boneIdx] = GeneralHelpers.SlerpMatrix(animation.PreviousAnimRelativeMatrices[boneIdx], transformAnimation * relativeMatrix_EanBone_inv, animation.CurrentBlendWeight);
                    }
                }
                
            }
        }
        
        private void UpdateAbsoluteMatrix(Matrix rootTransform)
        {
            int parentBone = -1;
            for (int i = 0, nbBones = currentAbsoluteMatrices.Length; i < nbBones; i++)
            {
                parentBone = i;
                currentAbsoluteMatrices[i] = Matrix.Identity;

                while (parentBone != -1)
                {
                    currentAbsoluteMatrices[i] *= (relativeAnimationTransformMatrices[parentBone] * relativeMatrices[parentBone]);
                    parentBone = skeleton.SkeletonHierarchy[parentBone];
                }
            }
        }
        
        private void UpdateSkinningMatrices()
        {
            if (!isUsingAnimation)
            {
                for (int i = 0, nb = skinningMatrices.Length; i < nb; i++)
                {
                    skinningMatrices[i] = Matrix.Identity;

                    debugBones[i] = skeleton.BoneAbsoluteMatrices[i];
                }   

            }
            else
            {
                for (int i = 0, nb = skinningMatrices.Length; i < nb; i++)
                {
                    skinningMatrices[i] = boneBindPoseMatrices_inv[i] * currentAbsoluteMatrices[i];

                    debugBones[i] = currentAbsoluteMatrices[i];
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

            if(rot != null)
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
                
                transformSum *= Matrix.CreateFromQuaternion(new Quaternion(RotX, RotY, RotZ , RotW));
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
            character.animatedTransform = transformSum;
            PrimaryAnimation.hasMoved = true;
        }
        #endregion

        #region Helpers
        
        public Matrix GetCurrentAbsoluteMatrix(string boneName)
        {
            return currentAbsoluteMatrices[skeleton.GetBoneIndex(boneName)];
        }
        
        public void NextFrame()
        {
            if (PrimaryAnimation == null) return;
            if(PrimaryAnimation.CurrentFrame_Int < PrimaryAnimation.EndFrame)
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
            else if(PrimaryAnimation.CurrentFrame_Int <= PrimaryAnimation.StartFrame)
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
                Log.Add($"An animation could be found at index: {index}", LogType.Error);

            if ((EndFrame == -1 || EndFrame == ushort.MaxValue) && Animation != null)
                EndFrame = Animation.FrameCount - 1;
        }

    }
}
