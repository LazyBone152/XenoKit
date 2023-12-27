using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC.Simulation;
using Xv2CoreLib.BAC;
using Xv2CoreLib.EAN;

namespace XenoKit.Engine.Scripting.BAC
{
    public class BacEntryInstance : ScriptEntity
    {
        //Files:
        /// <summary>
        /// The user of this BAC entry. Required for retrieving the Character EAN/CAM/BDM/ACBs. (The character the BAC entry is playing on may differ from the User, such as in Throw moves)
        /// </summary>
        public Actor User;
        /// <summary>
        /// The actor this BAC entry is playing on.
        /// </summary>
        public Actor Actor;
        /// <summary>
        /// The Move that this BAC entry belongs to. Required for retrieving skill EAN/CAM/BDM/BSA/ACBs. (Not required for Character/CMN)
        /// </summary>
        public Move SkillMove;
        /// <summary>
        /// The BAC_File that this entry belongs to. Required for Functions: GoTo BAC Entry types (0x1d, 0x25) and ThrowHandler.
        /// </summary>
        public BAC_File BacFile;
        public BAC_Entry BacEntry;

        //State:
        public float PreviousFrame = 0;
        public float CurrentFrame = 0;
        public int Duration { get; private set; }
        public float ScaledDuration { get; private set; }
        public bool HasTimeScale { get; private set; }
        public bool IsPreview { get; private set; }

        //Loop:
        public int CurrentLoop { get; set; } //0 = Initial, 1+ loops
        public bool LoopEnabled = false;
        public int LoopStartFrame = 0;
        public int LoopEndFrame = 0;
        public int LoopAnimationStartFrame = 0;
        public int LoopCameraStartFrame = 0;
        public bool LoopAllowIncomplete = true;

        //Position:
        /// <summary>
        /// The character position matrix when the BAC entry started.
        /// </summary>
        public Matrix[] OriginalMatrix;

        //Visual objects that render on screen as the entry plays. Currently only covers hitbox previews.
        //These have no actual logic and dont interact with anything. They are purely visual helpers.
        public List<BacVisualCueObject> VisualSimulationCues;

        /// <summary>
        /// The currently active eye movement entry. Only one of these can be active at any given time.
        /// </summary>
        public BAC_Type21 ActiveEyeMovement = null;
        /// <summary>
        /// Acts as the duration of how long the face animation in the currently active main animation can be played.
        /// </summary>
        public int MainFaceAnimationEndTime = -1;

        public BacEntryInstance(BAC_File bacFile, BAC_Entry bacEntry, Move currentMove, Actor user, Actor actor, bool isPreview)
        {
            BacFile = bacFile;
            BacEntry = bacEntry;
            SkillMove = currentMove;
            User = user;
            Actor = actor;
            CalculateEntryDuration();

            IsPreview = isPreview;

            if (IsPreview)
            {
                VisualSimulationCues = new List<BacVisualCueObject>(16);
                OriginalMatrix = new Matrix[3];
                CreateMatrixRestorePoint();
            }
            else
            {
                VisualSimulationCues = null;
                OriginalMatrix = null;
            }

            if (BacEntry == null)
                Log.Add($"Bac Entry {bacEntry} not found!", LogType.Error);
        }

        public void CreateMatrixRestorePoint()
        {
            for (int i = 0; i < 3; i++)
            {
                OriginalMatrix[i] = SceneManager.Actors[i] != null ? SceneManager.Actors[i].ActionMovementTransform * SceneManager.Actors[i].BaseTransform : Matrix.Identity;
            }
        }

        #region AnimationLengthCalculation
        public void CalculateEntryDuration()
        {
            if (BacEntry == null) return;

            int duration = 0;
            float scaledDuration = 0;
            bool hasTimeScale = false;
            CalculateEntryDuration(BacEntry, SkillMove, User, true, ref duration, ref scaledDuration, ref hasTimeScale);

            Duration = duration;
            ScaledDuration = scaledDuration;
            HasTimeScale = hasTimeScale;
        }

        public static int CalculateEntryDuration(BAC_Entry bacEntry, Move files, Actor user)
        {
            int duration = 0;
            float scaled = 0;
            bool hasScale = false;
            CalculateEntryDuration(bacEntry, files, user, false, ref duration, ref scaled, ref hasScale);

            return duration;
        }

        public static void CalculateEntryDuration(BAC_Entry bacEntry, Move files, Actor user, bool calculateScaledDuration, ref int duration, ref float scaledDuration, ref bool hasTimeScale)
        {
            int currentStartTime = 0;
            int currentAnimDuration = 0;

            //Calulcate base duration
            foreach (IBacType type in bacEntry.IBacTypes.OrderBy(x => x.StartTime))
            {
                //Animations
                if (type is BAC_Type0 animation)
                {
                    //Dont count this if its not a full body animation
                    if (!BAC_Type0.IsFullBodyAnimation(animation.EanType))
                        continue;

                    int animTypeDuration = CalculateAnimationDuration(files, user, animation);

                    currentStartTime = animation.StartTime;
                    currentAnimDuration = animTypeDuration;
                    animation.CachedActualDuration = animTypeDuration;
                }
            }

            //Calculate scaled duration
            if (calculateScaledDuration)
            {
                hasTimeScale = false;
                scaledDuration = currentStartTime + currentAnimDuration;

                foreach (IBacType type in bacEntry.IBacTypes)
                {
                    if (type is BAC_Type0 type0)
                    {
                        //Dont count this if its not a full body animation
                        if (!BAC_Type0.IsFullBodyAnimation(type0.EanType))
                            continue;

                        if (type0.TimeScale != 1f)
                        {
                            scaledDuration -= type0.CachedActualDuration;
                            scaledDuration += type0.CachedActualDuration / type0.TimeScale;
                            hasTimeScale = true;
                        }
                    }

                    if (type is BAC_Type4 timeScale)
                    {
                        scaledDuration -= timeScale.Duration;
                        scaledDuration += timeScale.Duration / timeScale.TimeScale;
                        hasTimeScale = true;
                    }
                }

            }

            duration = currentStartTime + currentAnimDuration;
        }

        public static int CalculateAnimationDuration(Move files, Actor user, BAC_Type0 animationBacType)
        {
            int animTypeDuration = 0;

            //Check for hard coded duration set in bac type (endFrame - startFrame).
            //If endFrame is 0xffff, then get duration from the ean
            if (animationBacType.EndFrame != ushort.MaxValue)
            {
                //animTypeDuration = (type0.EndFrame - type0.StartFrame) + ((type.StartTime == 0) ? type0.StartFrame : (ushort)type.StartTime);
                animTypeDuration = animationBacType.StartTime == 0 ? animationBacType.EndFrame : animationBacType.EndFrame - animationBacType.StartFrame;
            }
            else
            {
                EAN_File eanFile = Files.Instance.GetEanFile(animationBacType.EanType, files, user, true, true);

                if (eanFile != null)
                {
                    EAN_Animation anim = eanFile.GetAnimation(animationBacType.EanIndex, true);

                    if (anim != null && BAC_Type0.IsFullBodyAnimation(animationBacType.EanType))
                    {
                        animTypeDuration = anim.FrameCount;
                    }
                }
            }

            return animTypeDuration;
        }
        #endregion

        public void Update()
        {
            InScope = CurrentFrame < Duration;

            UpdateVisualObjects();

            //Advance current frame
            if (User.GameBase.IsPlaying)
            {
                PreviousFrame = CurrentFrame;
                CurrentFrame += Actor.ActiveTimeScale;
            }

            IsFinished = CurrentFrame >= Duration;
        }

        public void Simulate(bool advance = true)
        {
            InScope = CurrentFrame < Duration;
            PreviousFrame = CurrentFrame;

            //Simulate only goes forward in whole frames (no Time Scale)
            if (advance)
            {
                CurrentFrame = (int)CurrentFrame + 1;
            }

            IsFinished = CurrentFrame >= Duration;
        }

        /// <summary>
        /// Checks if a StartTime is valid within the current context. 
        /// </summary>
        public bool IsValidTime(ushort startFrame, ushort duration)
        {
            //If TimeScale skips a frame then still play any bac types that were missed.
            if (PreviousFrame < startFrame && CurrentFrame > startFrame && duration > 0) return true;
            return ((startFrame <= PreviousFrame && startFrame >= CurrentFrame) || startFrame <= CurrentFrame) && startFrame + duration > CurrentFrame;
        }

        public void RefreshDetails()
        {
            CalculateEntryDuration();
        }

        public void ResetState()
        {
            ClearVisualObjects();

            IsFinished = false;
            ActiveEyeMovement = null;
            MainFaceAnimationEndTime = -1;
            CurrentFrame = 0;
            PreviousFrame = 0;
            CurrentLoop = 0;
            LoopEnabled = false;
            LoopStartFrame = 0;
            LoopEndFrame = 0;

            if (SceneManager.RetainActionMovement && IsPreview)
            {
                SceneManager.Actors[1]?.Controller.ResetState(false);
                CreateMatrixRestorePoint();
            }
        }

        public void Dispose()
        {
            InScope = false;
            ClearVisualObjects();
        }

        public void AddVisualObject(BAC_Type1 hitbox, GameBase game)
        {
            if (IsPreview)
            {
                if(VisualSimulationCues.FirstOrDefault(x => x.BacType == hitbox) == null)
                {
                    VisualSimulationCues.Add(new HitboxPreview(hitbox, this, game));
                }
            }
        }

        private void UpdateVisualObjects()
        {
            if (!IsPreview) return;

            for (int i = VisualSimulationCues.Count - 1; i >= 0; i--)
            {
                if (VisualSimulationCues[i].IsValidForCurrentFrame())
                {
                    VisualSimulationCues[i].Update();
                }
                else
                {
                    VisualSimulationCues[i].Dispose();
                    VisualSimulationCues.RemoveAt(i);
                }
            }
        }

        private void ClearVisualObjects()
        {
            if (!IsPreview) return;

            foreach(BacVisualCueObject visualCue in VisualSimulationCues)
            {
                visualCue.Dispose();
            }

            VisualSimulationCues.Clear();
        }
    }

}
