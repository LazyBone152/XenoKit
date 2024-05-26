using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Engine.Collision;
using XenoKit.Engine.Scripting.BAC;
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

        private readonly DamageManager DamageManager;

        public ActorController(Actor actor)
        {
            Actor = actor;
            DamageManager = new DamageManager(this);
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
            ClearBacEntries();

            if(state == ActorState.Idle)
            {
                SetBacEntries(BAC_IDLE_STANCE);
                LoopBacEntries = true;
            }
            else if (DamageManager.HasEntry)
            {
                SetBacEntries(DamageManager.GetBacEntryForActorState(state));
            }
        }

        #region Update
        public void Update()
        {
            UpdateIFrames();
            UpdateFreezeActionFrames();

            if (DamageManager.HasEntry)
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

            if (DamageManager.HasEntry)
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
                DamageManager.InitBdmEntry(bdmEntry, damageDir, hitbox.OwnerActor, hitbox.BacEntry.SkillMove, hitbox.GetAbsoluteHitboxMatrix());

                if(DamageManager.BdmSubEntry.DamageType == DamageType.Grab)
                {
                    Log.Add("Grab moves are not implemented yet.", LogType.Warning);
                    DamageManager.ResetBdmEntry();
                    return;
                }

                State = DamageManager.GetInitialActorState();
            }
        }

        private void UpdateDamageState(bool simulate)
        {
            if (DamageManager.HasEntry)
            {
                //Return to idle state when damage animations have finished 
                if (CurrentBacEntry >= BacEntryCount && ActiveBacEntry == null)
                {
                    DamageManager.ResetBdmEntry();
                    State = ActorState.Idle;
                    return;
                }

                //On the first frame, activate the effects and sounds declared on the BDM entry and initialize any other settings
                //The current animation will be frozen for 1 frame while this happens
                if (DamageManager.CurrentFrame == 0 && (Actor.GameBase.IsPlaying || simulate))
                {
                    FreezeActionFrames = DamageManager.BdmSubEntry.VictimStun + 1;
                    DamageManager.Attacker.Controller.FreezeActionFrames = DamageManager.BdmSubEntry.UserStun + 1;

                    InvulnerabilityFrames = DamageManager.BdmSubEntry.VictimInvincibilityTime + FreezeActionFrames;
                    DamageManager.Attacker.Controller.InvulnerabilityFrames = DamageManager.Attacker.Controller.FreezeActionFrames;

                    Actor.BdmTimeScale = DamageManager.BdmSubEntry.VictimAnimationSpeed;
                    Actor.BdmTimeScaleDuration = DamageManager.BdmSubEntry.VictimAnimationTime;

                    DamageManager.Attacker.BdmTimeScale = DamageManager.BdmSubEntry.UserAnimationSpeed;
                    DamageManager.Attacker.BdmTimeScaleDuration = DamageManager.BdmSubEntry.UserAnimationTIme;

                    Actor.VfxManager.PlayEffect(DamageManager);

                    DamageManager.PushbackStrength = DamageManager.BdmSubEntry.PushbackStrength;

                    if (!simulate)
                    {
                        Xv2CoreLib.ACB.ACB_Wrapper acb = Files.Instance.GetAcbFile((Xv2CoreLib.BAC.AcbType)DamageManager.BdmSubEntry.AcbType, DamageManager.Move, Actor, true);

                        if (acb != null && DamageManager.BdmSubEntry.CueId != -1 && Actor.GameBase.IsPlaying && Actor.AnimationPlayer.PrimaryAnimation != null)
                        {
                            SceneManager.AudioEngine.PlayCue(DamageManager.BdmSubEntry.CueId, acb, Actor);
                        }
                    }
                }

                if (FreezeActionFrames == 0)
                {
                    //Pushback
                    if (!MathHelpers.FloatEquals(DamageManager.PushbackStrength, 0f) && DamageManager.UsePushback)
                    {
                        Vector3 pushbackVector = DamageManager.Victim.Transform.Translation - DamageManager.Attacker.Transform.Translation;
                        pushbackVector.Normalize();
                        Actor.ApplyTranslation(pushbackVector * DamageManager.PushbackStrength);

                        //PushbackStrength may need to be clamped with high accerlerations
                        DamageManager.PushbackStrength *= DamageManager.BdmSubEntry.PushbackAcceleration;
                    }

                    if(State == ActorState.Knockback)
                    {

                    }
                }

                //Advance bdm frame if playing, or simulating a frame. This does not need to be time scaled
                if (Actor.GameBase.IsPlaying || simulate)
                    DamageManager.CurrentFrame++;
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
        Null = -1,
        Idle,
        SingleAnimation,
        Knockback,
        Falling,
        GroundImpact,
        RecoveryFromGround,
        RecoveryFromFalling
    }
}
