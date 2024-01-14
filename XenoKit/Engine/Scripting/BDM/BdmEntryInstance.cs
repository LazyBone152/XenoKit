using Microsoft.Xna.Framework;
using System;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Character;
using XenoKit.Engine.Collision;
using Xv2CoreLib.BDM;
using Xv2CoreLib.BEV;

namespace XenoKit.Engine.Scripting.BDM
{
    public class BdmEntryInstance
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
        public int HitDirection = 0; //0 = Front, 1 = Back, 2 = Left, 3 = Right
        public Vector3 HitVector;

        //Pushback
        public bool UsePushback = false;
        public float PushbackStrength = 0f;

        public BdmEntryInstance(ActorController actorController)
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

            bool isBackHit = HitDirection == 2 || HitDirection == 3;

            switch (BdmSubEntry.DamageType)
            {
                case DamageType.Block:
                    {
                        UsePushback = true;
                        int entryId = controller.IsInAir ? BLOCK_AIR_SET[HitDirection] : BLOCK_SET[HitDirection];
                        controller.SetBacEntries(entryId);
                        break;
                    }
                case DamageType.GuardBreak:
                    {
                        controller.SetBacEntries(controller.IsInAir ? BAC_STAMINA_BREAK_AIR : BAC_STAMINA_BREAK_AIR);
                        break;
                    }
                case DamageType.Standard:
                    {
                        UsePushback = true;
                        int entryId = controller.IsInAir ? (HitDirection * 2) + 1 : HitDirection * 2;
                        controller.SetBacEntries(GetStumbleEntry(BdmSubEntry.StumbleType) + entryId);
                        break;
                    }
                case DamageType.Heavy:
                    {
                        int stumbleId = GetHeavyStumbleEntry(BdmSubEntry.StumbleType);
                        int entryId = controller.IsInAir ? (HitDirection * 2) + 1 : HitDirection * 2;

                        //Heavy stumble 3 only has frontal and back hit animations
                        if (stumbleId == BAC_HEAVY_STUMBLE_3 && entryId > 3)
                            entryId = 0;

                        controller.SetBacEntries(stumbleId + entryId);
                        break;
                    }
                case DamageType.HoldStomach:
                    {
                        controller.SetBacEntries(175);
                        break;
                    }
                case DamageType.HoldEyes:
                    {
                        controller.SetBacEntries(controller.IsInAir ? 177 : 176);
                        break;
                    }
                case DamageType.Dazed:
                    {
                        controller.SetBacEntries(183); //Where is 184 used?
                        break;
                    }
                case DamageType.Knockback:
                    {
                        int knockback = !isBackHit ? 265 : 267;
                        int gravity = !isBackHit ? 266 : 268;
                        controller.SetBacEntries(knockback, gravity, 74);
                        break;
                    }
                case DamageType.Knockback1:
                    {
                        //Knockback, then recover with a stumble animation. No gravity phase
                        int knockback = !isBackHit ? 265 : 267;
                        int stumbleEntry = !isBackHit ? 270 : 272;
                        controller.SetBacEntries(knockback, stumbleEntry);
                        break;
                    }
                case DamageType.Knockback2:
                    {
                        //Knockback using stamina break knockback animation. 
                        int knockback = !isBackHit ? 168 : 169;
                        int recovery = !isBackHit ? 91 : 92;
                        controller.SetBacEntries(knockback, knockback, recovery);
                        break;
                    }
                case DamageType.Knockback3:
                    {
                        int knockback = !isBackHit ? 273 : 275;
                        int gravity = !isBackHit ? 274 : 276;
                        int groundImpact = !isBackHit ? 75 : 76;
                        controller.SetBacEntries(knockback, gravity, groundImpact);
                        break;
                    }
                case DamageType.Knockback4:
                    {
                        int knockback = 170;
                        int gravity = 266;
                        int groundImpact = 80;
                        controller.SetBacEntries(knockback, gravity, groundImpact);
                        break;
                    }
            }

        }

        public void ResetBdmEntry()
        {
            BdmEntry = null;
            CurrentFrame = 0f;
            HitDirection = 0;
            UsePushback = false;

            if(controller.State == ActorState.DamageManager)
                controller.ClearBacEntries();
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

        #endregion
    }
}
