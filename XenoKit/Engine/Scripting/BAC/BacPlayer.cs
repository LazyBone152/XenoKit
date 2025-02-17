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
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Scripting.BAC
{
    public class BacPlayer : Entity
    {
        //Parent character that this BacPlayer is for
        private readonly Actor character;

        //Bac Entry:
        public BacEntryInstance BacEntryInstance { get; private set; }
        public bool HasBacEntry => BacEntryInstance != null;
        public bool IsPreview => BacEntryInstance?.IsPreview == true;
        public bool DelayedResimulate { get; set; }

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

        public void PlayBacEntry(BAC_File bacFile, BAC_Entry bacEntry, Actor user, Move move)
        {
            if (bacEntry == null)
            {
                Log.Add("BacPlayer.PlayBacEntry: bacEntry was null.");
                return;
            }

            ClearBacEntry();
            BacEntryInstance = new BacEntryInstance(bacFile, bacEntry, move, user, character, false);
        }

        public void PlayBacEntryPreview(BAC_File bacFile, BAC_Entry bacEntry, Actor user, Move move = null)
        {
            if (bacEntry == null)
            {
                Log.Add("BacPlayer.PlayBacEntryPreview: bacEntry was null.");
                return;
            }

            ResetBacPreviewState();
            ClearBacEntry();
            BacEntryInstance = new BacEntryInstance(bacFile, bacEntry, move, user, character, true);
        }

        public override void Update()
        {
            if (clearing || BacEntryInstance == null || character.Controller.FreezeActionFrames > 0) return;

            UpdateBacLoop();

            if (!BacEntryInstance.IsFinished)
                UpdateBac(true, ref BacEntryInstance.CurrentFrame);

            BacEntryInstance.Update();
        }

        public override void DelayedUpdate()
        {
            if (DelayedResimulate && IsPreview)
            {
                DelayedResimulate = false;
                ResimulateCurrentEntry();
            }
        }

        private void UpdateBac(bool allowTimeSkip, ref float _refFrame)
        {
            float frame = _refFrame;
            bool timeSkip = false;
            character.SetBacTimeScale(1f, true);
            ushort typeFlag = (ushort)(character.CharacterData.IsCaC ? 1 : 2);

            if (clearing) return;
            IOrderedEnumerable<IBacType> validEntries = BacEntryInstance.BacEntry.IBacTypes.Where
                (b => BacEntryInstance.IsValidTime(b.StartTime, b.Duration) && (b.Flags == typeFlag || b.Flags == 0))
                .OrderBy(x => x.GetType() == typeof(BAC_Type4)); //TimeScale must be resolved before animation/camera

            //Read bac types
            foreach (IBacType type in validEntries)
            {
                //Animation
                if (type is BAC_Type0 animation)
                {
                    if (!ActivationCheck(type)) continue;
                    if (BAC_Type0.IsFullBodyAnimation(animation.EanType) && animation.StartTime < BacEntryInstance.LoopStartFrame && type.TimesActivated > 0) continue; //Animations have special handling in loops

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
                            case BAC_Type0.EanTypeEnum.MCM_DBA:
                            case BAC_Type0.EanTypeEnum.Skill:
                                character.AnimationPlayer.PlayPrimaryAnimation(eanFile, animation.EanIndex, animation.StartFrame, animation.EndFrame, animation.BlendWeight, animation.BlendWeightFrameStep, animation.AnimFlags, true, animation.TimeScale, false, true);
                                character.AnimationTimeScale = animation.TimeScale;

                                if (animation.StartFrame != 0 && _refFrame == 0f) //On first frame, skipping to startFrame on animations is allowed
                                {
                                    SetLoop(animation.LoopStartFrame, character.AnimationPlayer.PrimaryAnimation.EndFrame, animation.StartTime, true, true);
                                    _refFrame = animation.StartFrame;
                                    timeSkip = true;
                                }
                                else
                                {
                                    SetLoop(animation.LoopStartFrame, character.AnimationPlayer.PrimaryAnimation.EndFrame, animation.StartTime, true, true);
                                }
                                break;
                            case BAC_Type0.EanTypeEnum.FaceBase:
                            case BAC_Type0.EanTypeEnum.FaceForehead:
                                //I_14 tells the game to use the main animations face bones. In this case everything else on the entry is ignored.

                                if (animation.I_14 == 1)
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
                    var impactType = hitbox.GetImpactType();
                    var spawnSource = hitbox.GetSpawnSource();
                    //var damageType = hitbox.GetDamageType();

                    //Only allow hitbox looping when impact type is set to continuous
                    if (!ActivationCheck(type) && impactType != BAC_Type1.HitboxFlagsEnum.ImpactType_Continuous) continue;

                    //Determine the actor the hitbox should spawn on from the flags
                    Actor spawnActor = null;

                    switch (spawnSource)
                    {
                        case BAC_Type1.HitboxFlagsEnum.SpawnSource_User:
                            spawnActor = BacEntryInstance.User;
                            break;
                        case BAC_Type1.HitboxFlagsEnum.SpawnSource_Target:
                            spawnActor = SceneManager.Actors[1];
                            break;
                        default:
                            continue;
                    }

                    BacEntryInstance.AddVisualObject(hitbox, GameBase);

                    if (GameBase.Simulation.ActiveHitboxes.FirstOrDefault(x => x.Hitbox == hitbox && x.BacEntry == BacEntryInstance) == null)
                    {
                        GameBase.Simulation.ActiveHitboxes.Add(new Collision.BacHitbox(BacEntryInstance, hitbox, spawnActor, BacEntryInstance.User, BacEntryInstance.User.Team));
                    }
                }

                //TimeScale
                if (type is BAC_Type4 timeScale)
                {
                    character.SetBacTimeScale(timeScale.TimeScale, false);
                }

                //Effect
                if (type is BAC_Type8 effect)
                {
                    if (!effect.EffectFlags.HasFlag(BAC_Type8.EffectFlagsEnum.Loop) && type.TimesActivated > 0) continue;
                    if (!ActivationCheck(type) || !SettingsManager.Instance.Settings.XenoKit_VfxSimulation) continue;

                    if (effect.EffectFlags.HasFlag(BAC_Type8.EffectFlagsEnum.Off))
                    {
                        VfxManager.StopEffect(effect, BacEntryInstance);
                    }
                    else
                    {
                        VfxManager.PlayEffect(effect, BacEntryInstance, character);
                    }
                }

                //Camera
                if (type is BAC_Type10 camera)
                {
                    if (!ActivationCheck(type) || character.ActorSlot != 0) continue;

                    EAN_File ean = Files.Instance.GetCamEanFile(camera.EanType, BacEntryInstance.SkillMove, BacEntryInstance.User, true, true);

                    if (ean != null && camera.EanIndex != ushort.MaxValue && SceneManager.UseCameras)
                    {
                        EAN_Animation cam = ean.GetAnimation(camera.EanIndex, true);

                        if (cam != null)
                        {
                            int actorFocusIdx = camera.FocusOnTarget ? 1 : character.ActorSlot;

                            SceneManager.PlayCameraAnimation(ean, cam, camera, character, actorFocusIdx, true);
                        }
                        else
                        {
                            Log.Add(string.Format("BacPlayer: Could not find the camera animation with the ID {0} in the {1} cam.ean.", camera.EanIndex, camera.EanType), LogType.Error);
                        }
                    }
                }

                //Sound
                if (type is BAC_Type11 sound)
                {
                    if (!ActivationCheck(type)) continue;

                    Xv2CoreLib.ACB.ACB_Wrapper acb = Files.Instance.GetAcbFile(sound.AcbType, BacEntryInstance.SkillMove, character, true);

                    //I've made it only play sounds if a primary animation is current playing - this prevents some audio crashes/errors
                    if (acb != null && sound.CueId != ushort.MaxValue && GameBase.IsPlaying && character.AnimationPlayer.PrimaryAnimation != null)
                    {
                        SceneManager.AudioEngine.PlayCue(sound.CueId, acb, character, BacEntryInstance, sound.SoundFlags.HasFlag(SoundFlags.StopWhenParentEnds));
                    }
                }

                //Functions
                if (type is BAC_Type15 function)
                {
                    switch (function.FunctionType)
                    {
                        case 0x0: //BAC Loop. When conditions are no longer true, the loop will stop when the current loop cycle ends.
                        case 0x22: //BAC Loop, but ends instantly when conditions are no longer true
                            if (!ActivationCheck(type) || BacEntryInstance.CurrentLoop > 0) continue;
                            SetLoop(type.StartTime, type.StartTime + type.Duration, 0, function.FunctionType == 0x22);
                            break;
                        case 0x13: //Sets BCS PartSet (temp).
                        case 0x14: //Sets BCS PartSet (permanent).
                            if (!ActivationCheck(type)) continue;
                            character.PartSet.ApplyBacPartSetSwap((int)function.Param1, function.FunctionType == 0x14);
                            break;
                        case 0x6: //Invisibility
                            character.IsVisible = false;
                            break;
                    }
                }

                //Eye Movement
                if (type is BAC_Type21 eyeMovement)
                {
                    if (!ActivationCheck(type)) continue;

                    BacEntryInstance.ActiveEyeMovement = eyeMovement;
                }

                //Transparency Effect
                if(type is BAC_Type23 transparency)
                {
                    //Shader path is activated, and values faded into over duration
                    //Shader reverts to normal when BAC entry ends or overriden by another BAC_Type23

                    switch (transparency.ShaderOptions)
                    {
                        case BAC_Type23.ShaderPathOptions.Vanish:
                        case BAC_Type23.ShaderPathOptions.Vanish2:
                        case BAC_Type23.ShaderPathOptions.Vanish3:
                            character.ShaderParameters.ShaderPath = Shader.ActorShaderPath.Vanish;
                            float fadeInFactor = (CurrentFrame - type.StartTime + 1) / (float)type.Duration;

                            Vector4 color = new Vector4(transparency.Tint_R, transparency.Tint_G, transparency.Tint_B, transparency.Tint_A);
                            //Vector4 startColor = color * 0.5f;
                            Vector4 startColor = Vector4.One - color;

                            character.ShaderParameters.g_vColor4_PS = Vector4.Lerp(startColor, color, fadeInFactor);
                            //character.ShaderParameters.g_vColor4_PS = new Vector4(transparency.Tint_R, transparency.Tint_G, transparency.Tint_B, transparency.Tint_A) * fadeInFactor;
                            character.ShaderParameters.g_vParam9_PS = new Vector4(
                                MathHelper.Max((int)(transparency.HorizontalLineSize * fadeInFactor), transparency.HorizontalLineSize > 0 ? 1 : 0),
                                MathHelper.Max((int)(transparency.VerticalLineSize * fadeInFactor), transparency.VerticalLineSize > 0 ? 1 : 0),
                                MathHelper.Max((int)(transparency.HorizontalLineSpacing * fadeInFactor), transparency.HorizontalLineSpacing > 0 ? 1 : 0),
                                MathHelper.Max((int)(transparency.VerticalLineSpacing * fadeInFactor), transparency.VerticalLineSpacing > 1 ? 0 : 0));
                            break;
                        case BAC_Type23.ShaderPathOptions.HC:
                            character.ShaderParameters.ShaderPath = Shader.ActorShaderPath.HC;
                            break;
                        default:
                            character.ShaderParameters.ShaderPath = Shader.ActorShaderPath.Default;
                            break;
                    }

                    //character.ShaderParameters.g_vParam9_PS = new Vector4(0, 10, 0, 30);
                }
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

                    CustomVector4 leftEyePosition = nextEyePosition * eyeMovement.LeftEyeRotationPercent;
                    CustomVector4 rightEyePosition = nextEyePosition * eyeMovement.RightEyeRotationPercent;


                    if (eyeMovement.StartTime + eyeMovement.EyeRotationFrames >= CurrentFrame)
                    {
                        float factor = 1f / eyeMovement.EyeRotationFrames * (CurrentFrame - eyeMovement.StartTime);
                        leftEyePosition = CustomVector4.Lerp(prevEyePosition, nextEyePosition, factor) * eyeMovement.LeftEyeRotationPercent;
                        rightEyePosition = CustomVector4.Lerp(prevEyePosition, nextEyePosition, factor) * eyeMovement.RightEyeRotationPercent;
                    }

                    character.EyeIrisLeft_UV[0] = leftEyePosition.X;
                    character.EyeIrisLeft_UV[1] = leftEyePosition.Y;
                    character.EyeIrisRight_UV[0] = rightEyePosition.X;
                    character.EyeIrisRight_UV[1] = rightEyePosition.Y;
                    character.BacEyeMovementUsed = true;
                }

            }

            //Update face animation
            if (character.AnimationPlayer.PrimaryAnimation != null)
                character.AnimationPlayer.PrimaryAnimation.EnableFaceBones = BacEntryInstance.MainFaceAnimationEndTime > CurrentFrame;

            //If the frame was skipped to a latter frame by an animation, then process this later frame as well.
            if (timeSkip && allowTimeSkip)
            {
                UpdateBac(false, ref _refFrame);
            }
        }

        private void UpdateBacLoop()
        {
            if (BacEntryInstance.LoopIsDirty)
            {
                bool wasPlaying = GameBase.IsPlaying;
                GameBase.IsPlaying = false;
                BacEntryInstance.LoopIsDirty = false;
                Seek((int)BacEntryInstance.CurrentFrame);
                GameBase.IsPlaying = wasPlaying;
            }
            else if (BacEntryInstance.LoopEnabled && BacEntryInstance.CurrentFrame >= BacEntryInstance.LoopEndFrame && (!IsPreview || SceneManager.AllowBacLoop))
            {
                BacEntryInstance.CurrentFrame = BacEntryInstance.LoopStartFrame;
                BacEntryInstance.CurrentLoop++;

                //Both the current animation the camera get restored back to the frame they were at when the loop was first started.
                //If the camera changes, then the wrong camera frame will be used. This is how it works in game too.
                //However, it is impossible for the animation to change within a loop, as each new primary animation will also start a new loop, effectively overwriting any Function Loops
                character.AnimationPlayer.GoToFrame(BacEntryInstance.LoopAnimationStartFrame, false);
                SceneManager.MainCamera.SkipToFrame(BacEntryInstance.LoopCameraStartFrame);
            }
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
            SceneManager.Actors[1]?.ResetState(false);
            RevertCharacterPosition(true);

            if (clearAnimations)
                character.AnimationPlayer.ClearCurrentAnimation(true);

            Xv2CoreLib.Random.ResetWithCurrentSeed();
            VfxManager.StopEffects();
            VfxManager.ForceEffectUpdate = false;
            SceneManager.MainCamera.ClearCameraAnimation();
            character.AnimationTimeScale = 1f;
            BacEntryInstance.BacEntry.ResetTimesActivated();
            BacEntryInstance.ResetState();

            for (int i = 0; i <= frame; i++)
            {
                BacEntryInstance.CurrentFrame = i;
                UpdateBac(true, ref BacEntryInstance.CurrentFrame);

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
                    SceneManager.Actors[1]?.Simulate(true, true);
                    SceneManager.MainGameInstance.camera.Simulate(true, true);
                    GameBase.Simulation.Simulate();

                    VfxManager.ForceEffectUpdate = true;
                    VfxManager.Simulate();
                    break;
                }
                else
                {
                    do
                    {
                        character.Simulate(frame - i < numBlendingFrames, true); //Fully update anim positions for as long as required for accurate blending
                        SceneManager.Actors[1]?.Simulate(frame - i < 10, true);
                        SceneManager.MainGameInstance.camera.Simulate(frame - i <= 1, true); //Only fully update camera on last frame
                        GameBase.Simulation.Simulate();

                        //If this is the last seek frame, then set this to true so that effects fully update
                        if (i == frame && character.Controller.FreezeActionFrames == 0)
                            VfxManager.ForceEffectUpdate = true;

                        VfxManager.Simulate();
                    }
                    while (character.Controller.FreezeActionFrames > 0);
                }
            }

            //Reset camera state if no cam anim is active. This allows the bac entry to be paused, then the camera moved, and the scene to resume without the camera resetting to the last simulated camera
            if (SceneManager.MainGameInstance.camera.cameraInstance == null && SceneManager.UseCameras)
            {
                SceneManager.MainGameInstance.camera.CameraState.SetState(originalCameraState);
            }
        }

        private void SetLoop(int startFrame, int endFrame, int relativeTo, bool allowIncompleteLoop, bool isAnimationLoop = false)
        {
            if (endFrame <= startFrame) return;

            int newStart = startFrame + relativeTo;
            int newEnd = endFrame + relativeTo;
            int animStart = isAnimationLoop ? startFrame : (int)character.AnimationPlayer.PrimaryCurrentFrame;

            if (BacEntryInstance.CurrentLoop == 0)
            {
                BacEntryInstance.LoopEnabled = true;
                BacEntryInstance.LoopStartFrame = newStart;
                BacEntryInstance.LoopEndFrame = newEnd;
                BacEntryInstance.LoopAnimationStartFrame = animStart;
                BacEntryInstance.LoopCameraStartFrame = (int)SceneManager.MainCamera.CurrentFrame;
                BacEntryInstance.LoopAllowIncomplete = allowIncompleteLoop;
            }
        }

        /// <summary>
        /// Checks if a BAC Type can be activated on the current loop, and increments the activation count if it can.
        /// </summary>
        private bool ActivationCheck(IBacType type)
        {
            if (type.TimesActivated > BacEntryInstance.CurrentLoop) return false;
                type.TimesActivated++;

            return true;
        }
        
        #region Helpers
        private void RevertCharacterPosition(bool alwaysRevert)
        {
            if (BacEntryInstance == null) return;

            //IMPORTANT: When adding SEEK for future actors (1 and 2), the PRIMARY animation on them must be removed BEFORE their original matrices are restored here
            for(int i = 0; i < 3; i++)
            {
                if(SceneManager.Actors[i] != null)
                {
                    if (BacEntryInstance.IsPreview || alwaysRevert)
                    {
                        SceneManager.Actors[i].BaseTransform = BacEntryInstance.OriginalMatrix[i];
                        SceneManager.Actors[i].ActionMovementTransform = Matrix.Identity;

                        if (SceneManager.Actors[i].AnimationPlayer.PrimaryAnimation != null)
                            SceneManager.Actors[i].AnimationPlayer.PrimaryAnimation.hasMoved = false;
                    }
                }
            }
        }

        public void ResetBacPreviewState()
        {
            if (!SceneManager.RetainActionMovement)
            {
                RevertCharacterPosition(false);
                character.PartSet.ResetBacPartSetSwap(false);
            }

            character.Controller.ResetState(SceneManager.RetainActionMovement);
            SceneManager.Actors[1]?.ResetState(SceneManager.RetainActionMovement);

            //Xv2CoreLib.Random.ResetWithCurrentSeed();
            Xv2CoreLib.Random.GenerateNewSeed();
            VfxManager.StopEffects();

            if(BacEntryInstance != null)
            {
                CurrentFrame = 0;
                BacEntryInstance.BacEntry.ResetTimesActivated();
                BacEntryInstance.ResetState();
            }
        }

        public void ClearBacEntry()
        {
            clearing = true;

            if (IsPreview)
            {
                SceneManager.MainGameInstance.camera.ClearCameraAnimation();
            }

            character.AnimationPlayer.SecondaryAnimations.Clear();
            BacEntryInstance?.BacEntry.ResetTimesActivated();
            BacEntryInstance?.Dispose();
            BacEntryInstance = null;

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
                bool wasPlaying = GameBase.IsPlaying;
                GameBase.IsPlaying = false;

                BacEntryInstance.CalculateEntryDuration();
                Seek((int)BacEntryInstance.CurrentFrame, true);

                GameBase.IsPlaying = wasPlaying;
            }
        }

        #endregion

        #region PlaybackControl
        public void Resume()
        {
            if (BacEntryInstance == null || SceneManager.CurrentSceneState != EditorTabs.Action) return;

            if (BacEntryInstance.CurrentFrame >= CurrentDuration)
                ResetBacPreviewState();

            //If anything was edited, it will be re-simulated
            Seek((int)BacEntryInstance.CurrentFrame, true);
        }

        public void Stop()
        {
            if (BacEntryInstance == null || SceneManager.CurrentSceneState != EditorTabs.Action) return;

            character.AnimationPlayer.ClearCurrentAnimation(true);
            BacEntryInstance.ResetState();
            BacEntryInstance?.BacEntry.ResetTimesActivated();
            character.ResetPosition();
            character.PartSet.ResetBacPartSetSwap(false);
            SceneManager.Actors[1]?.ResetState();
            BacEntryInstance.CreateMatrixRestorePoint();
            character.AnimationTimeScale = 1f;
            GameBase.IsPlaying = false;
        }

        public void SeekPrevFrame()
        {
            if (BacEntryInstance == null || SceneManager.CurrentSceneState != EditorTabs.Action) return;

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
            if (BacEntryInstance == null || SceneManager.CurrentSceneState != EditorTabs.Action) return;

            if (CurrentFrame < CurrentDuration)
                Seek(CurrentFrame + 1, true);
            else
                Seek(0, true);
        }
        #endregion

    }
}
