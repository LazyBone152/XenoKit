using Microsoft.Xna.Framework;
using XenoKit.Editor;
using XenoKit.Engine.Scripting;
using XenoKit.Engine.Scripting.BAC;
using Xv2CoreLib.BAC;

namespace XenoKit.Engine.Collision
{
    public class BacHitbox
    {
        public Matrix WorldMatrix
        {
            get
            {
                if (Hitbox == null) return Matrix.Identity;
                Matrix world = Matrix.Identity;

                if (boneIdx != -1 && SpawnActor != null)
                {
                    world = SpawnActor.GetAbsoluteBoneMatrix(boneIdx);

                    //Hitbox doesn't rotate with b_C_Base, so the rotation needs to be removed
                    if (isBaseBone)
                    {
                        world = Matrix.CreateTranslation(world.Translation);
                    }
                }

                return world;
            }
        }
        private Vector3 PreviousTranslation;

        public int Team;

        public readonly BacEntryInstance BacEntry;
        public readonly Actor OwnerActor;
        public readonly Actor SpawnActor;
        public readonly BAC_Type1 Hitbox;
        public BoundingBox BoundingBox;
        private int boneIdx = -1;
        private bool isBaseBone = false;

        private Vector3 HitboxPosition;

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

            HitboxPosition = new Vector3(Hitbox.PositionX, Hitbox.PositionY, Hitbox.PositionZ);
        }

        public void UpdateHitbox()
        {
            Matrix world = WorldMatrix;

            if (world.Translation == PreviousTranslation) return; //No need to update

            if (Hitbox.BoundingBoxType == BAC_Type1.BoundingBoxTypeEnum.MinMax)
            {
                BoundingBox = new BoundingBox((new Vector3(Hitbox.MinX, Hitbox.MinY, Hitbox.MinZ) / 2) + HitboxPosition + world.Translation, (new Vector3(Hitbox.MaxX, Hitbox.MaxY, Hitbox.MaxZ) / 2) + HitboxPosition + world.Translation);
            }
            else
            {
                BoundingBox = new BoundingBox(new Vector3(-(Hitbox.Size / 2)) + HitboxPosition + world.Translation, new Vector3((Hitbox.Size / 2)) + HitboxPosition + world.Translation);
            }

            PreviousTranslation = world.Translation;
        }

        public bool IsContextValid()
        {
            if (!BacEntry.InScope || BacEntry.IsFinished) return false;
            return BacEntry.IsValidTime(Hitbox.StartTime, Hitbox.Duration);
        }

        public Vector3 GetRelativeDirection(Matrix matrix)
        {
            Vector3 relativeDir = (Matrix.Invert(matrix) * WorldMatrix).Translation;
            relativeDir.Normalize();
            return relativeDir;
        }
    
        public Matrix GetAbsoluteHitboxMatrix()
        {
            return WorldMatrix * Matrix.CreateTranslation(HitboxPosition);
        }
    }
}
