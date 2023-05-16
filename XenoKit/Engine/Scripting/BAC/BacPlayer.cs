using LB_Common.Numbers;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC.Simulation;
using XenoKit.Engine.View;
using Xv2CoreLib.BAC;
using Xv2CoreLib.EAN;

namespace XenoKit.Engine.Scripting.BAC
{
    public class BacPlayer : Entity
    {
        //Parent character that this BacPlayer is for.
        private Actor character;

        //Bac Entry:
        public BacEntryInstance BacEntryInstance { get; private set; }
        public bool HasBacEntry => BacEntryInstance != null;

        //Frame and Duration:
        public int CurrentFrame
        {
            get
            {
                if (BacEntryInstance == null) return 0;
                return (int)BacEntryInstance.CurrentFrame;
            }
            set
            {
                BacEntryInstance.CurrentFrame = value;
            }
        }
        public int CurrentDuration
        {
            get
            {
                if (BacEntryInstance == null) return 0;
                return BacEntryInstance.Duration;
            }
        }
        public float ScaledDuration
        {
            get => BacEntryInstance != null ? BacEntryInstance.ScaledDuration : 0f;
        }
        public bool IsScaled
        {
            get => BacEntryInstance != null ? BacEntryInstance.HasTimeScale : false;
        }

        //Keeps track of all bac entries processed in the current playing. 
        private List<IBacType> ProcessedBacTypes = new List<IBacType>();

        //Threading:
        /// <summary>
        /// For thread-safety. Updating wont happen when this is set, which prevents a BAC entry that has just been removed from being accessed.
        /// </summary>
        private bool clearing = false;

        public BacPlayer(Actor chara, GameBase gameBase) : base(gameBase)
        {
            character = chara;
            Xv2CoreLib.Resource.UndoRedo.UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        private void Instance_UndoOrRedoCalled(object source, Xv2CoreLib.Resource.UndoRedo.UndoEventRaisedEventArgs e)
        {
            if (BacEntryInstance != null)
            {
                BacEntryInstance.RefreshDetails();
            }
        }

        public void PlayBacEntry(BAC_File bacFile, BAC_Entry bacEntry, Actor user, Move move = null, bool revertPosition = true)
        {
            if (bacEntry == null)
            {
                Log.Add("BacPlayer.PlayBacEntry: bacEntry was null.");
                return;
            }

            ClearBacEntry();
            BacEntryInstance = new BacEntryInstance(bacFile, bacEntry, move, user, revertPosition, character.BaseTransform);
        }

        public override void Update()
        {
            if (clearing || BacEntryInstance == null) return;

            if (BacEntryInstance.SimulationState != ActionSimulationState.DurationElapsed)
                UpdateBac(ref BacEntryInstance.CurrentFrame);

            BacEntryInstance.Update();
        }

        private void UpdateBac(ref float _refFrame)
        {
            float frame = _refFrame;
            SceneManager.SetBacTimeScale(1f, true);
            ushort typeFlag = (ushort)(character.CharacterData.IsCaC ? 1 : 2);

            if (clearing) return;
            IOrderedEnumerable<IBacType> validEntries = BacEntryInstance.BacEntry.IBacTypes.Where
                (b => BacEntryInstance.IsValidTime(b.StartTime, b.Duration, b.TypeID) && (b.Flags == typeFlag || b.Flags == 0))
                .OrderBy(x => x.GetType() == typeof(BAC_Type4)); //TimeScale must be resolved before animation/camera


            //Read bac types
            foreach (IBacType type in validEntries)
            {
                //Animation
                if (type is BAC_Type0 animation)
                {
                    if (ProcessedBacTypes.Contains(type)) continue;

                    EAN_File eanFile;

                    if (animation.EanType == BAC_Type0.EanTypeEnum.FaceBase || animation.EanType == BAC_Type0.EanTypeEnum.FaceForehead)
                    {
                        //Face animations are always loaded from the character FCE EAN
                        eanFile = Files.Instance.GetEanFile(animation.EanType, BacEntryInstance.SkillMove, character, true, true);
                    }
                    else
                    {
                        //For all other cases, the EANs should be from the user of the move.
                        eanFile = Files.Instance.GetEanFile(animation.EanType, BacEntryInstance.SkillMove, BacEntryInstance.User, true, true);
                    }

                    if (eanFile != null && animation.EanIndex != ushort.MaxValue)
                    {
                        switch (animation.EanType)
                        {
                            case BAC_Type0.EanTypeEnum.Character:
                            case BAC_Type0.EanTypeEnum.Common:
                            case BAC_Type0.EanTypeEnum.Skill:
                                character.AnimationPlayer.PlayPrimaryAnimation(eanFile, animation.EanIndex, animation.StartFrame, animation.EndFrame, animation.BlendWeight, animation.BlendWeightFrameStep, animation.AnimFlags, true, animation.TimeScale);
                                character.AnimationPlayer.PrimaryAnimation.EnableFaceBones = false;
                                SceneManager.MainAnimTimeScale = animation.TimeScale;

                                if (animation.StartFrame != 0 && _refFrame == 0f) //On first frame, skipping to startFrame on animations is allowed
                                    _refFrame = animation.StartFrame;
                                break;
                            case BAC_Type0.EanTypeEnum.FaceBase:
                            case BAC_Type0.EanTypeEnum.FaceForehead:
                                //I_14 tells the game to use the main animations face bones. In this case everything else on the entry is ignored.

                                if(animation.I_14 == 1)
                                {
                                    BacEntryInstance.MainFaceAnimationEndTime = animation.StartTime + animation.Duration;
                                }
                                else
                                {
                                    character.AnimationPlayer.PlaySecondaryAnimation(eanFile, animation.EanIndex, animation.StartFrame, (ushort)(animation.Duration + 1), animation.BlendWeight, animation.BlendWeightFrameStep, animation.TimeScale, true);
                                }

                                break;
                            case BAC_Type0.EanTypeEnum.CommonTail:
                                character.AnimationPlayer.PlaySecondaryAnimation(eanFile, animation.EanIndex, animation.StartFrame, animation.EndFrame, animation.BlendWeight, animation.BlendWeightFrameStep, animation.TimeScale);
                                break;
                        }
                    }
                }

                //Hitbox
                if (type is BAC_Type1 hitbox)
                {
                    if (ProcessedBacTypes.Contains(type)) continue;

                    HitboxPreview hitboxSimulation = new HitboxPreview(hitbox, BacEntryInstance, GameBase);
                    GameBase.AddEntity(hitboxSimulation);

                    ProcessedBacTypes.Add(hitbox);
                }

                //TimeScale
                if (type is BAC_Type4 timeScale)
                {
                    SceneManager.SetBacTimeScale(timeScale.TimeScale, false);
                }

                //Camera
                if (type is BAC_Type10 camera)
                {
                    if (ProcessedBacTypes.Contains(type)) continue;

                    var ean = Files.Instance.GetCamEanFile(camera.EanType, BacEntryInstance.SkillMove, BacEntryInstance.User, true, true);

                    if (ean != null && camera.EanIndex != ushort.MaxValue && SceneManager.UseCameras)
                    {
                        var cam = ean.GetAnimation(camera.EanIndex, true);

                        if (cam != null)
                            SceneManager.PlayCameraAnimation(ean, cam, camera, SceneManager.IndexOfCharacter(character, false), true);
                        else
                            Log.Add(string.Format("BacPlayer: Could not find the camera animation with the ID {0} in the {1} cam.ean.", camera.EanIndex, camera.EanType), LogType.Error);
                    }
                }

                //Sound
                if (type is BAC_Type11 sound)
                {
                    if (ProcessedBacTypes.Contains(type)) continue;

                    var acb = Files.Instance.GetAcbFile(sound.AcbType, BacEntryInstance.SkillMove, character, true);

                    if (acb != null && sound.CueId != ushort.MaxValue && SceneManager.IsPlaying)
                    {
                        SceneManager.AudioEngine.PlayCue(sound.CueId, acb, character, BacEntryInstance, sound.SoundFlags.HasFlag(SoundFlags.StopWhenParentEnds));
                    }
                }

                //Eye Movement
                if (type is BAC_Type21 eyeMovement)
                {
                    if (ProcessedBacTypes.Contains(type)) continue;
                    BacEntryInstance.ActiveEyeMovement = eyeMovement;
                }


                //Add type to processed list
                ProcessedBacTypes.Add(type);
            }

            //Update Eye Movements
            if (BacEntryInstance.ActiveEyeMovement != null)
            {
                BAC_Type21 eyeMovement = BacEntryInstance.ActiveEyeMovement;

                if (eyeMovement.StartTime + eyeMovement.EyeMovementDuration < CurrentFrame)
                {
                    BacEntryInstance.ActiveEyeMovement = null;
                }
                else
                {
                    CustomVector4 prevEyePosition = EyeMovementPositions.EyePositions[(int)eyeMovement.EyeDirectionPrev];
                    CustomVector4 nextEyePosition = EyeMovementPositions.EyePositions[(int)eyeMovement.EyeDirectionNext];

                    CustomVector4 leftEyePosition = (nextEyePosition - prevEyePosition) * eyeMovement.LeftEyeRotationPercent;
                    CustomVector4 rightEyePosition = (nextEyePosition - prevEyePosition) * eyeMovement.RightEyeRotationPercent;

                    if (eyeMovement.StartTime + eyeMovement.EyeRotationFrames >= CurrentFrame)
                    {
                        leftEyePosition.X *= 1f / eyeMovement.EyeRotationFrames * (CurrentFrame - eyeMovement.StartTime);
                        leftEyePosition.Y *= 1f / eyeMovement.EyeRotationFrames * (CurrentFrame - eyeMovement.StartTime);

                        rightEyePosition.X *= 1f / eyeMovement.EyeRotationFrames * (CurrentFrame - eyeMovement.StartTime);
                        rightEyePosition.Y *= 1f / eyeMovement.EyeRotationFrames * (CurrentFrame - eyeMovement.StartTime);
                    }

                    character.EyeIrisLeft_UV[0] = leftEyePosition.X;
                    character.EyeIrisLeft_UV[1] = leftEyePosition.Y;
                    character.EyeIrisRight_UV[0] = rightEyePosition.X;
                    character.EyeIrisRight_UV[1] = rightEyePosition.Y;
                    character.BacEyeMovementUsed = true;
                }

            }

            //Update face animation
            if(character.AnimationPlayer.PrimaryAnimation != null)
                character.AnimationPlayer.PrimaryAnimation.EnableFaceBones = BacEntryInstance.MainFaceAnimationEndTime > CurrentFrame;
        }

        /// <summary>
        /// Simulate the bac entry up to the specified frame. This will also simulate any animations, cameras, hitboxes, projectiles and so on.
        /// </summary>
        public void Seek(int frame, bool clearAnimations = true)
        {
            if (clearing) return;

            //Get camera state so it can be restored after resimulating the bac state. 
            CameraState originalCameraState = SceneManager.MainGameInstance.camera.CameraState.Copy();

            //Calculate the number of "blending" frames between animations. With this we dont need to calculate every single animation frame, just what is needed.
            int numBlendingFrames = BAC_Type0.CalculateNumOfBlendingFrames(BacEntryInstance.BacEntry.IBacTypes, frame);

            //Clean up
            RevertCharacterPosition(true);

            if (clearAnimations)
                character.AnimationPlayer.ClearCurrentAnimation(true);

            SceneManager.MainCamera.ClearCameraAnimation();
            SceneManager.MainAnimTimeScale = 1f;
            ProcessedBacTypes.Clear();
            BacEntryInstance.ResetState();

            for (int i = 0; i <= frame; i++)
            {
                BacEntryInstance.CurrentFrame = i;
                UpdateBac(ref BacEntryInstance.CurrentFrame);

                if(BacEntryInstance.CurrentFrame != i)
                {
                    i = (int)BacEntryInstance.CurrentFrame;
                    continue;
                }

                if (i < frame)
                    BacEntryInstance.Simulate(true);

                if (BacEntryInstance.CurrentFrame > frame) //Frameskip shot us past the specified frame. End seek here.
                {
                    character.Simulate(true, true);
                    SceneManager.MainGameInstance.camera.Simulate(true, true);
                    break;
                }
                else
                {
                    character.Simulate(frame - i < numBlendingFrames, true); //Fully update anim positions for as long as required for accurate blending

                    SceneManager.MainGameInstance.camera.Simulate(frame - i <= 1, true); //Only fully update camera on last frame
                }
            }

            //Remove all Type11 (Sound) bac types from the processed list, as they are not actually processed while paused
            ProcessedBacTypes.RemoveAll(x => x.GetType() == typeof(BAC_Type11));

            //Reset camera state if no cam anim is active. This allows the bac entry to be paused, then the camera moved, and the scene to resume without the camera resetting to the last simulated camera
            if (SceneManager.MainGameInstance.camera.cameraInstance == null && SceneManager.UseCameras)
            {
                SceneManager.MainGameInstance.camera.CameraState.SetState(originalCameraState);
                SceneManager.MainGameInstance.camera.ResetViewerAngles();
            }
        }

        #region Helpers
        private void RevertCharacterPosition(bool alwaysRevert)
        {
            if (BacEntryInstance == null) return;
            character.BaseTransform = (BacEntryInstance.RevertPosition || alwaysRevert) ? BacEntryInstance.OriginalMatrix : character.Transform;
            character.ActionMovementTransform = Matrix.Identity;

            if(character.AnimationPlayer.PrimaryAnimation != null)
                character.AnimationPlayer.PrimaryAnimation.hasMoved = false;
        }

        public void ResetBacState()
        {
            RevertCharacterPosition(false);
            CurrentFrame = 0;
            ProcessedBacTypes.Clear();

            BacEntryInstance?.ResetState();
        }

        public void ClearBacEntry()
        {
            clearing = true;
            BacEntryInstance?.Dispose();
            BacEntryInstance = null;
            ProcessedBacTypes.Clear();

            //Clean up used resources
            SceneManager.MainGameInstance.camera.ClearCameraAnimation();

            clearing = false;
        }

        /// <summary>
        /// Force re-simulate the current BAC entry. This should be called whenever the BAC entry is edited in a way that affects the simulation.
        /// </summary>
        public void ResimulateCurrentEntry()
        {
            //Values in the current BAC have changed and so the current frame must be re-simulated.
            if (BacEntryInstance != null)
            {
                bool wasPlaying = SceneManager.IsPlaying;
                SceneManager.IsPlaying = false;

                BacEntryInstance.CalculateEntryDuration();
                Seek((int)BacEntryInstance.CurrentFrame, true);

                SceneManager.IsPlaying = wasPlaying;
            }
        }

        #endregion

        #region PlaybackControl
        public void Resume()
        {
            if (BacEntryInstance == null) return;

            if (BacEntryInstance.CurrentFrame >= CurrentDuration)
                ResetBacState();

            //If anything was edited, it will be re-simulated
            Seek((int)BacEntryInstance.CurrentFrame, true);
        }

        public void Stop()
        {
            if (BacEntryInstance == null) return;

            character.AnimationPlayer.ClearCurrentAnimation(true);
            BacEntryInstance.ResetState();
            ProcessedBacTypes.Clear();
            character.ResetPosition();
            BacEntryInstance.OriginalMatrix = character.Transform;
            SceneManager.MainAnimTimeScale = 1f;
            SceneManager.IsPlaying = false;
        }

        public void SeekPrevFrame()
        {
            if (BacEntryInstance == null) return;

            if (CurrentFrame != 0)
            {
                Seek(CurrentFrame - 1, true);
            }
            else
            {
                Seek(CurrentDuration, true);
            }
        }

        public void SeekNextFrame()
        {
            if (BacEntryInstance == null) return;

            if (CurrentFrame < CurrentDuration)
                Seek(CurrentFrame + 1, true);
            else
                Seek(0, true);
        }
        #endregion


    }



}
