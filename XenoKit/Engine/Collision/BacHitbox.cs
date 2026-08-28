using Microsoft.Xna.Framework;
using System;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC;
using Xv2CoreLib.BAC;
using Xv2CoreLib.CBS;
using Xv2CoreLib.Resource;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdQuaternion = System.Numerics.Quaternion;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Collision
{
    public class BacHitbox
    {
        public Matrix4x4 WorldMatrix
        {
            get
            {
                if (Hitbox == null) return Matrix4x4.Identity;
                Matrix4x4 world = Matrix4x4.Identity;

                if (boneIdx != -1 && SpawnActor != null)
                {
                    world = SpawnActor.GetAbsoluteBoneMatrix(boneIdx);

                    //Hitbox doesn't rotate with b_C_Base, so the rotation needs to be removed
                    if (isBaseBone)
                    {
                        world = Matrix4x4.CreateTranslation(world.Translation);
                    }
                }

                return world;
            }
        }
        public int Team;

        public readonly BacEntryInstance BacEntry;
        public readonly Actor OwnerActor;
        public readonly Actor SpawnActor;
        public readonly BAC_Type1 Hitbox;
        public BoundingBox BoundingBox;
        public bool IsSupported { get; private set; }
        private int boneIdx = -1;
        private bool isBaseBone = false;

        private SimdVector3 HitboxPosition;

        public BacHitbox(BacEntryInstance bacEntry, BAC_Type1 bacHitbox, Actor spawnOnActor, Actor owner, int team)
        {
            Team = team;
            BacEntry = bacEntry;
            Hitbox = bacHitbox;
            BoundingBox = new BoundingBox();
            SpawnActor = spawnOnActor;
            OwnerActor = owner;

            string boneName = Hitbox.BoneLink.ToString();
            isBaseBone = boneName == Xv2CoreLib.ESK.ESK_File.BaseBone;

            if (SpawnActor != null)
            {
                boneIdx = SpawnActor.Skeleton.GetBoneIndex(boneName);
            }
            else
            {
                Log.Add($"Hitbox tried spawning on actor, but no actor was found in the scene!", LogType.Warning);
            }

            HitboxPosition = new SimdVector3(Hitbox.PositionX, Hitbox.PositionY, Hitbox.PositionZ);
        }

        public void UpdateHitbox()
        {
            IsSupported = false;
            if (SpawnActor == null)
                return;

            CBS_Entry cbsEntry = SpawnActor.CharacterData.CbsEntry.Find(x => x.BodyId == SpawnActor.Skeleton.GetActiveBoneScaleId());
            float cbsScaling = 1f;

            if (cbsEntry != null)
            {
                switch (BacEntry.SkillMove.MoveType)
                {
                    case Move.Type.Moveset:
                        cbsScaling = cbsEntry.F_04;
                        break;
                    case Move.Type.Skill:
                        cbsScaling = cbsEntry.F_12;
                        break;
                    default:
                        cbsScaling = 1f;
                        break;
                }
            }


            if (!IsFinite(cbsScaling))
                return;

            HitboxPosition = new SimdVector3(Hitbox.PositionX, Hitbox.PositionY, Hitbox.PositionZ);
            Matrix4x4 world = WorldMatrix;
            SimdVector3 center = SimdVector3.Transform(HitboxPosition, world);

            if (!IsFinite(center))
                return;

            switch ((int)Hitbox.BoundingBoxType)
            {
                case 0:
                case 3:
                    SetBoundsFromCenter(center, new SimdVector3(GetRadius(cbsScaling)));
                    break;
                case 1:
                case 4:
                    SimdVector3 endpointA = SimdVector3.Transform(
                        HitboxPosition + new SimdVector3(Hitbox.MinX, Hitbox.MinY, Hitbox.MinZ) * cbsScaling,
                        world);
                    SimdVector3 endpointB = SimdVector3.Transform(
                        HitboxPosition + new SimdVector3(Hitbox.MaxX, Hitbox.MaxY, Hitbox.MaxZ) * cbsScaling,
                        world);
                    float radius = GetRadius(cbsScaling);

                    if (!IsFinite(endpointA) || !IsFinite(endpointB) || !IsFinite(radius) || radius <= 0f)
                        return;

                    SetBoundsFromMinMax(
                        SimdVector3.Min(endpointA, endpointB) - new SimdVector3(radius),
                        SimdVector3.Max(endpointA, endpointB) + new SimdVector3(radius));
                    break;
                case 2:
                    SimdVector3 halfExtents = new SimdVector3(
                        Math.Abs(Hitbox.Size),
                        Math.Abs(Hitbox.MinX),
                        Math.Abs(Hitbox.MinY)) * Math.Abs(cbsScaling);

                    SetBoundsFromCenter(center, halfExtents);
                    break;
                default:
                    return;
            }
        }

        private float GetRadius(float cbsScaling)
        {
            return Math.Abs(Hitbox.Size * cbsScaling);
        }

        private void SetBoundsFromCenter(SimdVector3 center, SimdVector3 halfExtents)
        {
            if (!IsFinite(halfExtents))
                return;

            BoundingBox = new BoundingBox(
                Extensions.ToXna(center - halfExtents),
                Extensions.ToXna(center + halfExtents));
            IsSupported = true;
        }

        private void SetBoundsFromMinMax(SimdVector3 min, SimdVector3 max)
        {
            if (!IsFinite(min) || !IsFinite(max))
                return;

            BoundingBox = new BoundingBox(Extensions.ToXna(min), Extensions.ToXna(max));
            IsSupported = true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFinite(SimdVector3 value)
        {
            return IsFinite(value.X) && IsFinite(value.Y) && IsFinite(value.Z);
        }

        public bool IsContextValid()
        {
            if (!BacEntry.InScope || BacEntry.IsFinished) return false;
            return BacEntry.IsValidTime(Hitbox.StartTime, Hitbox.Duration);
        }

        public SimdVector3 GetRelativeDirection(Matrix4x4 matrix)
        {
            SimdVector3 relativeDir = (MathHelpers.Invert(matrix) * WorldMatrix).Translation;
            relativeDir = SimdVector3.Normalize(relativeDir);
            return relativeDir;
        }

        public Matrix4x4 GetAbsoluteHitboxMatrix()
        {
            return WorldMatrix * Matrix4x4.CreateTranslation(HitboxPosition);
        }
    }
}
