using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Engine.Collision;
using XenoKit.Engine.Scripting.BAC;
using XenoKit.Engine.Scripting.BDM;
using XenoKit.Engine.Vfx;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BDM;
using Xv2CoreLib.DEM;
using Xv2CoreLib.Resource;

namespace XenoKit.Engine.Character
{
    public class ActorController
    {
        private readonly Move CMN;
        public readonly Actor Actor;

        private ActorState _state = ActorState.Null;
        public ActorState State
        {
            get => _state;
            set
            {
                _state = value;
                SetStateBacEntries(value);
            }
        }

        //General state:
        public bool IsInAir = false;
        public int InvulnerabilityFrames = 0;
        public int FreezeActionFrames = 0;

        //BAC Entry
        private BAC_Entry ActiveBacEntry = null;
        public readonly int[] BacEntries = new int[4];
        public int BacEntryCount = 0;
        public int CurrentBacEntry = 0;
        public bool EndCurrentBacEntry = false;
        public bool LoopBacEntries = false;

        private readonly BdmEntryInstance BdmEntry;
        public ActorController(Actor actor)
        {
            Actor = actor;
            BdmEntry = new BdmEntryInstance(this);
            CMN = Files.Instance.GetCmnMove();
            Actor.ActionControl.ActionFinished += ActionControl_ActionFinished;
        }

        public void ResetState(bool keepAnimation = false)
        {
            if (!keepAnimation)
            {
                ClearBacEntries();
                State = Actor.ActorSlot == 0 ? ActorState.Null : ActorState.Idle;
            }

            InvulnerabilityFrames = 0;
            FreezeActionFrames = 0;
        }

        public void SetBacEntries(params int[] bacEntries)
        {
#if DEBUG
            if (bacEntries.Length > BacEntries.Length) throw new ArgumentOutOfRangeException("SetBacEntries: BacEntries array is not large enough!");
#endif

            for (int i = 0; i < BacEntries.Length; i++)
            {
                if (i < bacEntries.Length)
                {
                    BacEntries[i] = bacEntries[i];
                }
                else
                {
                    BacEntries[i] = -1;
                }
            }

            BacEntryCount = bacEntries.Length;
        }

        public void ClearBacEntries()
        {
            CurrentBacEntry = 0;
            BacEntryCount = 0;
            ActiveBacEntry = null;
            EndCurrentBacEntry = false;
            LoopBacEntries = false;

            for (int i = 0; i < BacEntries.Length; i++)
                BacEntries[i] = -1;
        }

        private void SetStateBacEntries(ActorState state)
        {
            ActiveBacEntry = null;

            if(state == ActorState.Null)
            {
                ClearBacEntries();
            }
            else if(state == ActorState.Idle)
            {
                SetBacEntries(BAC_IDLE_STANCE);
                LoopBacEntries = true;
            }
        }

        #region Update
        public void Update()
        {
            UpdateIFrames();
            UpdateFreezeActionFrames();

            if (State == ActorState.DamageManager)
            {
                UpdateDamageState(false);
            }
            
            if (State != ActorState.Null)
            {
                UpdateBacEntry();
            }
        }

        private void UpdateBacEntry()
        {
            if (ActiveBacEntry == null && CurrentBacEntry == BacEntryCount)
            {
                if (LoopBacEntries)
                {
                    CurrentBacEntry = 0;
                }
                else
                {
                    return;
                }
            }
            
            if(ActiveBacEntry == null || EndCurrentBacEntry)
            {
                PlayCharacterBacEntry(BacEntries[CurrentBacEntry], EndCurrentBacEntry);

                CurrentBacEntry++;
                EndCurrentBacEntry = false;
            }
        }

        public void Simulate()
        {
            UpdateIFrames();
            UpdateFreezeActionFrames();

            if (State == ActorState.DamageManager)
            {
                UpdateDamageState(true);
            }

            if (State != ActorState.Null)
            {
                UpdateBacEntry();
            }
        }

        private void UpdateIFrames()
        {
            if (InvulnerabilityFrames > 0)
            {
                Actor.HitboxEnabled = false;
                InvulnerabilityFrames--;
            }
        }

        private void UpdateFreezeActionFrames()
        {
            if (FreezeActionFrames > 0)
            {
                FreezeActionFrames--;
            }
        }
        #endregion

        private void PlayCharacterBacEntry(int bacIndex, bool forcePlay = false)
        {
            if(ActiveBacEntry == null || forcePlay)
            {
                Actor.MergeTransforms();

                BAC_File bacFile = GetBacFileWhereEntryExists(bacIndex);
                BAC_Entry bacEntry = bacFile.GetEntry(bacIndex);

                Actor.ActionControl.PlayBacEntry(bacFile, bacEntry, Actor.Moveset, Actor);
                ActiveBacEntry = bacEntry;
            }
        }

        private BAC_File GetBacFileWhereEntryExists(int idx)
        {
            var entry = Actor.Moveset.Files.BacFile.File.GetEntry(idx);

            if(entry != null)
            {
                if(!entry.Flag.HasFlag(BAC_Entry.Flags.Empty))
                    return Actor.Moveset.Files.BacFile.File;
            }

            entry = CMN.Files.BacFile.File.GetEntry(idx);

            if (entry != null)
            {
                if (!entry.Flag.HasFlag(BAC_Entry.Flags.Empty))
                    return CMN.Files.BacFile.File;
            }

            Log.Add($"ActorController: BAC entry {idx} not found in both character and CMN BAC!", LogType.Error);
            return null;
        }

        private void ActionControl_ActionFinished(object source, ActionFinishedEventArgs e)
        {
            if (ActiveBacEntry == e.BacEntry && ActiveBacEntry != null)
            {
                ActiveBacEntry = null;
            }
        }

        #region Damage

        public void ApplyDamageState(BDM_Entry bdmEntry, Vector3 damageDir, BacHitbox hitbox)
        {
            if (bdmEntry != null)
            {
                ClearBacEntries();
                BdmEntry.InitBdmEntry(bdmEntry, damageDir, hitbox.OwnerActor, hitbox.BacEntry.SkillMove, hitbox.GetAbsoluteHitboxMatrix());
                State = ActorState.DamageManager;

                if(BdmEntry.BdmSubEntry.DamageType == DamageType.Grab)
                {
                    Log.Add("Grab moves are not implemented yet.", LogType.Warning);
                    BdmEntry.ResetBdmEntry();
                    State = ActorState.Idle;
                }
            }
        }

        private void UpdateDamageState(bool simulate)
        {
            if (BdmEntry.HasEntry)
            {
                //Return to idle state when damage animations have finished 
                if (CurrentBacEntry >= BacEntryCount && ActiveBacEntry == null)
                {
                    BdmEntry.ResetBdmEntry();
                    State = ActorState.Idle;
                    return;
                }

                //On the first frame, activate the effects and sounds declared on the BDM entry and initialize any other settings
                //The current animation will be frozen for 1 frame while this happens
                if (BdmEntry.CurrentFrame == 0 && (Actor.GameBase.IsPlaying || simulate))
                {
                    FreezeActionFrames = BdmEntry.BdmSubEntry.VictimStun + 1;
                    BdmEntry.Attacker.Controller.FreezeActionFrames = BdmEntry.BdmSubEntry.UserStun + 1;

                    InvulnerabilityFrames = BdmEntry.BdmSubEntry.VictimInvincibilityTime + FreezeActionFrames;
                    BdmEntry.Attacker.Controller.InvulnerabilityFrames = BdmEntry.Attacker.Controller.FreezeActionFrames;

                    Actor.BdmTimeScale = BdmEntry.BdmSubEntry.VictimAnimationSpeed;
                    Actor.BdmTimeScaleDuration = BdmEntry.BdmSubEntry.VictimAnimationTime;

                    BdmEntry.Attacker.BdmTimeScale = BdmEntry.BdmSubEntry.UserAnimationSpeed;
                    BdmEntry.Attacker.BdmTimeScaleDuration = BdmEntry.BdmSubEntry.UserAnimationTIme;

                    Actor.VfxManager.PlayEffect(BdmEntry);

                    BdmEntry.PushbackStrength = BdmEntry.BdmSubEntry.PushbackStrength;

                    if (!simulate)
                    {
                        Xv2CoreLib.ACB.ACB_Wrapper acb = Files.Instance.GetAcbFile((Xv2CoreLib.BAC.AcbType)BdmEntry.BdmSubEntry.AcbType, BdmEntry.Move, Actor, true);

                        if (acb != null && BdmEntry.BdmSubEntry.CueId != -1 && Actor.GameBase.IsPlaying && Actor.AnimationPlayer.PrimaryAnimation != null)
                        {
                            SceneManager.AudioEngine.PlayCue(BdmEntry.BdmSubEntry.CueId, acb, Actor);
                        }
                    }
                }

                if (FreezeActionFrames == 0)
                {
                    //Pushback
                    if (!MathHelpers.FloatEquals(BdmEntry.PushbackStrength, 0f) && BdmEntry.UsePushback)
                    {
                        Vector3 pushbackVector = BdmEntry.Victim.Transform.Translation - BdmEntry.Attacker.Transform.Translation;
                        pushbackVector.Normalize();
                        Actor.ApplyTranslation(pushbackVector * BdmEntry.PushbackStrength);

                        //PushbackStrength may need to be clamped with high accerlerations
                        BdmEntry.PushbackStrength *= BdmEntry.BdmSubEntry.PushbackAcceleration;
                    }
                }

                //Advance bdm frame if playing, or simulating a frame. This does not need to be time scaled
                if (Actor.GameBase.IsPlaying || simulate)
                    BdmEntry.CurrentFrame++;
            }
        }
        
        #endregion

        #region BAC IDs
        private const int BAC_IDLE_STANCE = 0;
        private const int BAC_IDLE_STANCE_AIR = 1;

        #endregion
    }

    public enum ActorState
    {
        Null,
        Idle,
        DamageManager
    }
}
