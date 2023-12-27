using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Engine.Scripting.BAC;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BDM;

namespace XenoKit.Engine.Character
{
    public class ActorController
    {
        private readonly Move CMN;
        private readonly Actor Actor;

        private ActorState _state = ActorState.Null;
        public ActorState State
        {
            get => _state;
            set
            {
                if(value != _state)
                {
                    _state = value;
                    ActiveBacEntry = null;
                    BacEntriesPlayed = 0;
                }
            }
        }


        private BAC_Entry ActiveBacEntry = null;
        private int BacEntriesPlayed = 0;
        private int BacEntriesToPlay = 1;

        //General state:
        private bool IsInAir = false;
        public int InvulnerabilityFrames = 0;
        public int FreezeActionFrames = 0;


        public ActorController(Actor actor)
        {
            Actor = actor;
            CMN = Files.Instance.GetCmnMove();
            Actor.ActionControl.ActionFinished += ActionControl_ActionFinished;
        }

        public void ResetState(bool keepAnimation = false)
        {
            if (!keepAnimation)
            {
                _state = Actor.ActorSlot == 0 ? ActorState.Null : ActorState.Idle;
                ActiveBacEntry = null;
                BacEntriesPlayed = 0;
            }

            InvulnerabilityFrames = 0;
            FreezeActionFrames = 0;
        }

        #region Update
        public void Update()
        {
            if (State == ActorState.Null) return;
            UpdateIFrames();
            UpdateFreezeActionFrames();

            if (State == ActorState.Idle)
            {
                PlayCharacterBacEntry(BAC_IDLE_STANCE);
            }
            else if (State == ActorState.DamageManager)
            {
                UpdateDamageState();
            }
        }

        public void Simulate()
        {
            if (State == ActorState.Null) return;
            UpdateIFrames();
            UpdateFreezeActionFrames();

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
        //BDM:
        private BDM_Entry BdmEntry = null;
        private Type0SubEntry BdmSubEntry = null;
        private float BdmFrame = 0f;

        //Damage:
        private int HitDirection = 0; //0 = Front, 1 = Back, 2 = Left, 3 = Right
        private Vector3 HitVector;

        //Pushback
        private bool UsePushback = false;

        public void ApplyDamageState(BDM_Entry bdmEntry, Vector3 damageDriection)
        {
            if (bdmEntry != null)
            {
                ResetDamageState();
                SetDamageDirection(damageDriection);
                BdmEntry = bdmEntry;
                BdmSubEntry = bdmEntry.Type0Entries[0];
                ActiveBacEntry = null;
                BacEntriesPlayed = 0;
                State = ActorState.DamageManager;
            }
        }

        private void SetDamageDirection(Vector3 directionVector)
        {
            HitVector = directionVector;
            float xAbs = Math.Abs(directionVector.X);
            float zAbs = Math.Abs(directionVector.Z);

            if (directionVector.Z < 0 && zAbs > xAbs)
            {
                HitDirection = 0; //Front
            }
            else if (directionVector.Z > 0 && zAbs > xAbs)
            {
                HitDirection = 1; //Back
            }
            else if (directionVector.X < 0 && xAbs > zAbs)
            {
                HitDirection = 2; //Left
            }
            else if (directionVector.X > 0 && xAbs > zAbs)
            {
                HitDirection = 3; //Right
            }
        }

        private void UpdateDamageState()
        {
            if (ActiveBacEntry == null)
            {
                //Return to idle state when damage animations have finished 
                if (BacEntriesPlayed >= BacEntriesToPlay)
                {
                    State = ActorState.Idle;
                    return;
                }

                UsePushback = false;

                switch (BdmSubEntry.DamageType)
                {
                    case DamageType.Standard:
                        {
                            BacEntriesToPlay = 1;
                            UsePushback = true;
                            int entryId = IsInAir ? (HitDirection * 2) + 1 : HitDirection * 2;
                            PlayCharacterBacEntry(GetStumbleEntry(BdmSubEntry.StumbleType) + entryId);
                            break;
                        }
                    case DamageType.Heavy:
                        {
                            BacEntriesToPlay = 1;
                            int stumbleId = GetHeavyStumbleEntry(BdmSubEntry.StumbleType);
                            int entryId = IsInAir ? (HitDirection * 2) + 1 : HitDirection * 2;

                            //Heavy stumble 3 only has frontal and back hit animations
                            if (stumbleId == BAC_HEAVY_STUMBLE_3 && entryId > 3)
                                entryId = 0;

                            PlayCharacterBacEntry(stumbleId + entryId);
                            break;
                        }
                }

                BacEntriesPlayed++;
            }
        }
        
        private void ResetDamageState()
        {
            UsePushback = false;
            BdmFrame = 0f;
        }
        #endregion

        #region BAC IDs
        private const int BAC_IDLE_STANCE = 0;
        private const int BAC_IDLE_STANCE_AIR = 1;

        private const int BAC_STUMBLE_1 = 93;
        private const int BAC_STUMBLE_2 = 101;
        private const int BAC_STUMBLE_3 = 109;
        private const int BAC_STUMBLE_4 = 117;
        private const int BAC_STUMBLE_5 = 125;
        private const int BAC_STUMBLE_6 = 133;
        private const int BAC_STUMBLE_7 = 226;
        private const int BAC_STUMBLE_8 = 234;
        private const int BAC_STUMBLE_9 = 242;
        private const int BAC_HEAVY_STUMBLE_1 = 141;
        private const int BAC_HEAVY_STUMBLE_2 = 149;
        private const int BAC_HEAVY_STUMBLE_3 = 157;

        private readonly int[] STUMBLE_SET_1 = new int[] { 109, 117, 226 };
        private readonly int[] STUMBLE_SET_2 = new int[] { 93, 101, 226 };
        private readonly int[] STUMBLE_SET_3 = new int[] { 125, 133, 242 };
        private readonly int[] STUMBLE_SET_4 = new int[] { 93, 109, 125 };
        private readonly int[] STUMBLE_SET_5 = new int[] { 101, 117, 133 };
        private readonly int[] STUMBLE_SET_6 = new int[] { 226, 234, 242 };
        private readonly int[] STUMBLE_SET_ALL = new int[] { 93, 101, 109, 117, 125, 133, 226, 234, 242 };
        private readonly int[] HEAVY_STUMBLE_SET_ALL = new int[] { 141, 149, 157 };

        private int GetStumbleEntry(Stumble stumbleFlags)
        {
            int rnd = Xv2CoreLib.Random.Range(0, 2);
            int result = -1;

            //TODO: Flags can be mixed together
            if (stumbleFlags.HasFlag(Stumble.StumbleSet1))
            {
                result = STUMBLE_SET_1[rnd];
            }
            else if (stumbleFlags.HasFlag(Stumble.StumbleSet2))
            {
                result = STUMBLE_SET_2[rnd];
            }
            else if (stumbleFlags.HasFlag(Stumble.StumbleSet3))
            {
                result = STUMBLE_SET_3[rnd];
            }
            else if (stumbleFlags.HasFlag(Stumble.StumbleSet4))
            {
                result = STUMBLE_SET_4[rnd];
            }
            else if (stumbleFlags.HasFlag(Stumble.StumbleSet5))
            {
                result = STUMBLE_SET_5[rnd];
            }
            else if (stumbleFlags.HasFlag(Stumble.StumbleSet6))
            {
                result = STUMBLE_SET_6[rnd];
            }

            if (stumbleFlags.HasFlag(Stumble.AllStumbleSets) || result == -1)
            {
                result = STUMBLE_SET_ALL[Xv2CoreLib.Random.Range(0, STUMBLE_SET_ALL.Length)];
            }

            return result;
        }

        private int GetHeavyStumbleEntry(Stumble stumbleFlags)
        {
            int result;

            //TODO: Flags can be mixed together
            if (stumbleFlags.HasFlag(Stumble.StumbleSet1))
            {
                result = BAC_HEAVY_STUMBLE_2;
            }
            else if (stumbleFlags.HasFlag(Stumble.StumbleSet2))
            {
                result = BAC_HEAVY_STUMBLE_1;
            }
            else if (stumbleFlags.HasFlag(Stumble.StumbleSet3))
            {
                result = BAC_HEAVY_STUMBLE_3;
            }
            else
            {
                result = HEAVY_STUMBLE_SET_ALL[Xv2CoreLib.Random.Range(0, 2)];
            }

            return result;
        }

        #endregion
    }

    public enum ActorState
    {
        Null,
        Idle,
        DamageManager
    }
}
