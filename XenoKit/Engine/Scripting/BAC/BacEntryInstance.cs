using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC.Simulation;
using Xv2CoreLib.BAC;
using Xv2CoreLib.EAN;

namespace XenoKit.Engine.Scripting.BAC
{
    public class BacEntryInstance : ScriptEntity
    {
        public event ActionStoppedEventHandler ActionStoppedEvent;

        //Files:
        /// <summary>
        /// The user of this BAC entry. Required for retrieving the Character EAN/CAM/BDM/ACBs. (The character the BAC entry is playing on may differ from the User, such as in Throw moves)
        /// </summary>
        public Actor User;
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
        public ActionSimulationState SimulationState { get; private set; } = ActionSimulationState.SimulationActive;

        //Position:
        /// <summary>
        /// When the BAC entry ends playback, should the character position be reset?
        /// </summary>
        public bool RevertPosition;
        /// <summary>
        /// The character position matrix when the BAC entry started.
        /// </summary>
        public Matrix OriginalMatrix;

        //Simulation Objects:
        public List<BacSimulationObject> SimulationEntities = new List<BacSimulationObject>(64);

        /// <summary>
        /// The currently active eye movement entry. Only one of these can be active at any given time.
        /// </summary>
        public BAC_Type21 ActiveEyeMovement = null;
        /// <summary>
        /// Acts as the duration of how long the face animation in the currently active main animation can be played.
        /// </summary>
        public int MainFaceAnimationEndTime = -1;

        public BacEntryInstance(BAC_File bacFile, BAC_Entry bacEntry, Move currentMove, Actor user, bool revertPosition, Matrix originalMatrix)
        {
            BacFile = bacFile;
            BacEntry = bacEntry;
            SkillMove = currentMove;
            RevertPosition = revertPosition;
            OriginalMatrix = originalMatrix;
            User = user;
            CalculateEntryDuration();

            if (BacEntry == null)
                Log.Add($"Bac Entry {bacEntry} not found!", LogType.Error);
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

            //Advance current frame
            if (User.GameBase.IsPlaying)
            {
                PreviousFrame = CurrentFrame;
                CurrentFrame += User.GameBase.ActiveTimeScale;
            }

            DetermineActionState();
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

            DetermineActionState();
        }

        /// <summary>
        /// Checks if a StartTime is valid within the current context. 
        /// </summary>
        public bool IsValidTime(ushort startFrame, ushort duration, int bacType)
        {
            //If TimeScale skips a frame then still play any bac types that were missed.
            if (PreviousFrame < startFrame && CurrentFrame > startFrame && duration > 0) return true;
            return ((startFrame <= PreviousFrame && startFrame >= CurrentFrame) || startFrame <= CurrentFrame) && startFrame + duration > CurrentFrame;
        }

        public void RefreshDetails()
        {
            CalculateEntryDuration();
        }

        private void DetermineActionState()
        {
            if (CurrentFrame >= 0 && CurrentFrame < Duration)
            {
                SetActionState(ActionSimulationState.SimulationActive);
            }
            else if (CurrentFrame >= Duration && SimulationEntities.Count == 0)
            {
                SetActionState(ActionSimulationState.SimulationEnded);
            }
            else if (CurrentFrame >= Duration)
            {
                SetActionState(ActionSimulationState.DurationElapsed);
            }
        }

        private void SetActionState(ActionSimulationState state)
        {
            SimulationState = state;

            if (state == ActionSimulationState.DurationElapsed || state == ActionSimulationState.SimulationEnded)
            {
                ActionStoppedEvent?.Invoke(this, new ActionStoppedEventArgs(state));
            }
        }

        public void ResetState()
        {
            ActionStoppedEvent?.Invoke(this, new ActionStoppedEventArgs(ActionSimulationState.SimulationEnded));

            SimulationState = ActionSimulationState.SimulationActive;
            ActiveEyeMovement = null;
            MainFaceAnimationEndTime = -1;
            CurrentFrame = 0;
            PreviousFrame = 0;

            if (SceneManager.RetainActionMovement)
            {
                OriginalMatrix = User.Transform * Matrix.Invert(User.RootMotionTransform);
            }
        }

        public void Dispose()
        {
            ActionStoppedEvent?.Invoke(this, new ActionStoppedEventArgs(ActionSimulationState.SimulationEnded));
        }
    }

    public enum ActionSimulationState
    {
        /// <summary>
        /// The main simulation is active.
        /// </summary>
        SimulationActive,
        /// <summary>
        /// The duration has elapsed, but there are still simulation objects running (e.g projectiles).
        /// </summary>
        DurationElapsed,
        /// <summary>
        /// The simulation has entirely ended, either by running its duration or forcefully.
        /// </summary>
        SimulationEnded
    }

    public delegate void ActionStoppedEventHandler(object source, ActionStoppedEventArgs e);

    public class ActionStoppedEventArgs : EventArgs
    {
        public ActionSimulationState State { get; private set; }

        public ActionStoppedEventArgs(ActionSimulationState state)
        {
            State = state;
        }
    }

}
