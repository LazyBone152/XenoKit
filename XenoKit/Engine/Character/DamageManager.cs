using Microsoft.Xna.Framework;
using System;
using XenoKit.Editor;
using Xv2CoreLib.BDM;

namespace XenoKit.Engine.Character
{
    public class DamageManager
    {
        private ActorController controller;
        public bool HasEntry => BdmEntry != null;

        public Matrix HitPosition { get; private set; }
        public Actor Attacker { get; private set; }
        public Actor Victim => controller.Actor;
        public Move Move { get; private set; }
        public BDM_Entry BdmEntry { get; private set; }
        public Type0SubEntry BdmSubEntry => BdmEntry.Type0Entries[0];
        public float CurrentFrame = 0f;

        //Damage:
        public int HitDirectionAll = 0; //0 = Front, 1 = Back, 2 = Left, 3 = Right
        public int HitDirectionFrontBack = 0; //0 = Front, 1 = Back
        public Vector3 HitVector;

        //Pushback
        public bool UsePushback = false;
        public float PushbackStrength = 0f;

        //BAC
        private int SingleAnimation;
        private int KnockbackAnimation;
        private int FallAnimation;
        private int ImpactAnimation;
        private int RecoveryFromImpactAnimation;
        private int RecoveryBeforeImpactAnimation;

        //ActorState
        private ActorState[] ActorStates = new ActorState[4];
        private int ActorStateIdx = 0;

        public DamageManager(ActorController actorController)
        {
            controller = actorController;
        }

        public void InitBdmEntry(BDM_Entry bdmEntry, Vector3 damageDirection, Actor attacker, Move move, Matrix hitPosition)
        {
            ResetBdmEntry();
            Attacker = attacker;
            Move = move;
            BdmEntry = bdmEntry;
            HitPosition = hitPosition;
            SetDamageDirection(damageDirection);

            bool isBackHit = HitDirectionFrontBack == 1;

            switch (BdmSubEntry.DamageType)
            {
                case DamageType.Block:
                    {
                        UsePushback = true;
                        SingleAnimation = controller.IsInAir ? BLOCK_AIR_SET[HitDirectionAll] : BLOCK_SET[HitDirectionAll];
                        SetActorStates(ActorState.SingleAnimation);
                        break;
                    }
                case DamageType.GuardBreak:
                    {
                        SingleAnimation = controller.IsInAir ? BAC_STAMINA_BREAK_AIR : BAC_STAMINA_BREAK_AIR;
                        SetActorStates(ActorState.SingleAnimation);
                        break;
                    }
                case DamageType.Standard:
                    {
                        UsePushback = true;
                        int entryId = controller.IsInAir ? (HitDirectionAll * 2) + 1 : HitDirectionAll * 2;
                        SingleAnimation = GetStumbleEntry(BdmSubEntry.StumbleType) + entryId;
                        SetActorStates(ActorState.SingleAnimation);
                        break;
                    }
                case DamageType.Heavy:
                    {
                        int stumbleId = GetHeavyStumbleEntry(BdmSubEntry.StumbleType);
                        int entryId = controller.IsInAir ? (HitDirectionAll * 2) + 1 : HitDirectionAll * 2;

                        //Heavy stumble 3 only has frontal and back hit animations
                        if (stumbleId == BAC_HEAVY_STUMBLE_3 && entryId > 3)
                            entryId = 0;

                        SingleAnimation = stumbleId + entryId;
                        SetActorStates(ActorState.SingleAnimation);
                        break;
                    }
                case DamageType.HoldStomach:
                    {
                        SingleAnimation = 175;
                        SetActorStates(ActorState.SingleAnimation);
                        break;
                    }
                case DamageType.HoldEyes:
                    {
                        SingleAnimation = controller.IsInAir ? 177 : 176;
                        SetActorStates(ActorState.SingleAnimation);
                        break;
                    }
                case DamageType.Dazed:
                    {
                        SingleAnimation = 183; //Where is 184 used?
                        SetActorStates(ActorState.SingleAnimation);
                        break;
                    }
                case DamageType.Knockback:
                    {
                        KnockbackAnimation = !isBackHit ? 265 : 267;
                        FallAnimation = !isBackHit ? 266 : 268;
                        ImpactAnimation = 74;
                        SetActorStates(ActorState.Knockback, ActorState.Falling, ActorState.GroundImpact);
                        break;
                    }
                case DamageType.Knockback1:
                    {
                        //Knockback, then recover with a stumble animation. No gravity phase
                        KnockbackAnimation = !isBackHit ? 265 : 267;
                        SingleAnimation = !isBackHit ? 270 : 272;
                        break;
                    }
                case DamageType.Knockback2:
                    {
                        //Knockback using stamina break knockback animation. 
                        KnockbackAnimation = !isBackHit ? 168 : 169;
                        FallAnimation = KnockbackAnimation;
                        RecoveryFromImpactAnimation = !isBackHit ? 91 : 92;
                        break;
                    }
                case DamageType.Knockback3:
                    {
                        KnockbackAnimation = !isBackHit ? 273 : 275;
                        FallAnimation = !isBackHit ? 274 : 276;
                        ImpactAnimation = !isBackHit ? 75 : 76;
                        break;
                    }
                case DamageType.Knockback4:
                    {
                        KnockbackAnimation= 170;
                        FallAnimation = 266;
                        ImpactAnimation = 80;
                        break;
                    }
            }
        }

        public void ResetBdmEntry()
        {
            if (HasEntry && BdmSubEntry.DamageType != DamageType.Grab)
                controller.ClearBacEntries();

            BdmEntry = null;
            CurrentFrame = 0f;
            HitDirectionAll = 0;
            HitDirectionFrontBack = 0;
            UsePushback = false;
        }

        private void SetDamageDirection(Vector3 directionVector)
        {
            HitVector = directionVector;
            float xAbs = Math.Abs(directionVector.X);
            float zAbs = Math.Abs(directionVector.Z);

            if (directionVector.Z < 0 && zAbs > xAbs)
            {
                HitDirectionAll = 0; //Front
                HitDirectionFrontBack = 0;
            }
            else if (directionVector.Z > 0 && zAbs > xAbs)
            {
                HitDirectionAll = 1; //Back
                HitDirectionFrontBack = 1;
            }
            else if (directionVector.X < 0 && xAbs > zAbs)
            {
                HitDirectionAll = 2; //Left
            }
            else if (directionVector.X > 0 && xAbs > zAbs)
            {
                HitDirectionAll = 3; //Right
            }

            //If direction was left or right, then set the front and back direction to the next closest
            if(HitDirectionAll > 1 && directionVector.Z < 0)
            {
                HitDirectionFrontBack = 0;
            }
            else if (HitDirectionAll > 1 && directionVector.Z > 0)
            {
                HitDirectionFrontBack = 1;
            }
        }

        public void SetActorStates(params ActorState[] states)
        {
            for (int i = 0; i < ActorStates.Length; i++)
            {
                if (i < states.Length)
                {
                    ActorStates[i] = states[i];
                }
                else
                {
                    ActorStates[i] = ActorState.Null;
                }
            }
        }

        public ActorState GetInitialActorState()
        {
            return ActorStates[0];
        }

        public ActorState GetNextActorState()
        {
            ActorStateIdx++;
            return ActorStateIdx >= ActorStates.Length ? ActorState.Null : ActorStates[ActorStateIdx];
        }

        #region BAC IDs

        private const int BAC_BLOCK_STANDING = 60;
        private const int BAC_BLOCK_AIR = 61;
        private const int BAC_BLOCK_LEFT_STANDING = 62;
        private const int BAC_BLOCK_LEFT_AIR = 63;
        private const int BAC_BLOCK_RIGHT_STANDING = 64;
        private const int BAC_BLOCK_RIGHT_AIR = 65;

        private readonly int[] BLOCK_SET = new int[] { BAC_BLOCK_STANDING, BAC_BLOCK_STANDING, BAC_BLOCK_LEFT_STANDING, BAC_BLOCK_RIGHT_STANDING };
        private readonly int[] BLOCK_AIR_SET = new int[] { BAC_BLOCK_AIR, BAC_BLOCK_AIR, BAC_BLOCK_LEFT_AIR, BAC_BLOCK_RIGHT_AIR };

        private const int BAC_STAMINA_BREAK_GROUND = 66;
        private const int BAC_STAMINA_BREAK_AIR = 67;

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

        private readonly int[] STUMBLE_SET_1 = new int[] { BAC_STUMBLE_3, BAC_STUMBLE_4, BAC_STUMBLE_7 };
        private readonly int[] STUMBLE_SET_2 = new int[] { BAC_STUMBLE_1, BAC_STUMBLE_2, BAC_STUMBLE_7 }; //3rd one could be stumble 8.. but i seem to have lost my original testing notes for this so i cant check right now
        private readonly int[] STUMBLE_SET_3 = new int[] { BAC_STUMBLE_5, BAC_STUMBLE_6, BAC_STUMBLE_9 };
        private readonly int[] STUMBLE_SET_4 = new int[] { BAC_STUMBLE_1, BAC_STUMBLE_3, BAC_STUMBLE_5 };
        private readonly int[] STUMBLE_SET_5 = new int[] { BAC_STUMBLE_2, BAC_STUMBLE_4, BAC_STUMBLE_6 };
        private readonly int[] STUMBLE_SET_6 = new int[] { BAC_STUMBLE_7, BAC_STUMBLE_8, BAC_STUMBLE_9 };
        private readonly int[] STUMBLE_SET_ALL = new int[] { BAC_STUMBLE_1, BAC_STUMBLE_2, BAC_STUMBLE_3, BAC_STUMBLE_4, BAC_STUMBLE_5, BAC_STUMBLE_6, BAC_STUMBLE_7, BAC_STUMBLE_8, BAC_STUMBLE_9 };
        private readonly int[] HEAVY_STUMBLE_SET_ALL = new int[] { BAC_HEAVY_STUMBLE_1, BAC_HEAVY_STUMBLE_2, BAC_HEAVY_STUMBLE_3 };

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

        public int GetBacEntryForActorState(ActorState state)
        {
            switch(state)
            {
                case ActorState.SingleAnimation:
                    return SingleAnimation;
                case ActorState.Knockback:
                    return KnockbackAnimation;
                case ActorState.Falling:
                    return FallAnimation;
                case ActorState.GroundImpact:
                    return ImpactAnimation;
                case ActorState.RecoveryFromGround:
                    return RecoveryFromImpactAnimation;
                case ActorState.RecoveryFromFalling:
                    return RecoveryBeforeImpactAnimation;
                default:
                    Log.Add($"ActorState {state} is not a valid damage state.", LogType.Error);
                    return -1;
            }
        }

        #endregion
    }
}
