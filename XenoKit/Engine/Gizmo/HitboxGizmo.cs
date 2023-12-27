using System;
using Microsoft.Xna.Framework;
using XenoKit.Engine.Shapes;
using XenoKit.Engine.View;
using Xv2CoreLib.BAC;

namespace XenoKit.Engine.Gizmo
{
    public class HitboxGizmo : Entity
    {
        protected Matrix WorldMatrix
        {
            get
            {
                if (Hitbox == null) return Matrix.Identity;
                Matrix world = Matrix.Identity;

                if (boneIdx != -1 && actor != null)
                {
                    world = actor.GetAbsoluteBoneMatrix(boneIdx);

                    //Hitbox doesn't rotate with b_C_Base, so the rotation needs to be removed
                    if (isBaseBone)
                    {
                        world = Matrix.CreateTranslation(world.Translation);
                    }
                }

                return world;
            }
        }
        private Cube BoundingBox;

        private BAC_Type1 Hitbox = null;
        private Actor actor;
        private int boneIdx = -1;
        private bool isBaseBone = false;
        private bool RefreshHitbox = false;

        public HitboxGizmo(GameBase gameBase) : base(gameBase)
        {
            BoundingBox = new Cube(new Vector3(0.5f), new Vector3(-0.5f), new Vector3(0.5f), 0.5f, Color.Green, true, gameBase);
            SceneManager.ActorChanged += SceneManager_ActorChanged;
        }

        public override void Update()
        {
            if (RefreshHitbox && IsContextValid())
            {
                SetContext(Hitbox);
                RefreshHitbox = false;
            }
        }

        public override void Draw()
        {
            if (IsContextValid() && actor != null)
            {
                BoundingBox.Draw(WorldMatrix);
            }
        }

        public void SetContext(BAC_Type1 hitbox)
        {
            RemoveContext();
            Hitbox = hitbox;

            if(hitbox != null && SceneManager.Actors[0] != null)
            {
                string boneName = hitbox.BoneLink.ToString();
                var spawnSource = Hitbox.GetSpawnSource();

                switch (spawnSource)
                {
                    case BAC_Type1.HitboxFlagsEnum.SpawnSource_User:
                        actor = SceneManager.Actors[0];
                        break;
                    case BAC_Type1.HitboxFlagsEnum.SpawnSource_Target:
                        actor = SceneManager.Actors[1];
                        break;
                    default:
                        actor = null;
                        break;
                }

                if(actor != null)
                {
                    isBaseBone = boneName == Xv2CoreLib.ESK.ESK_File.BaseBone;
                    boneIdx = SceneManager.Actors[0].Skeleton.GetBoneIndex(boneName);

                    //BAC Hitbox bounds are defined in half-metres (1.0 is actually 0.5)
                    BoundingBox.SetBounds(new Vector3(hitbox.MinX, hitbox.MinY, hitbox.MinZ) / 2, new Vector3(hitbox.MaxX, hitbox.MaxY, hitbox.MaxZ) / 2, hitbox.Size / 2, hitbox.BoundingBoxType != BAC_Type1.BoundingBoxTypeEnum.Uniform);
                    BoundingBox.SetPosition(new Vector3(hitbox.PositionX, hitbox.PositionY, hitbox.PositionZ));
                }

                hitbox.PropertyChanged += Hitbox_PropertyChanged;
            }
        }

        public void RemoveContext()
        {
            if(Hitbox != null)
            {
                Hitbox.PropertyChanged -= Hitbox_PropertyChanged;
                Hitbox = null;
                boneIdx = -1;
                isBaseBone = false;
                RefreshHitbox = false;
                actor = null;
            }
        }

        public bool IsContextValid()
        {
            return Hitbox != null && Controls.BacTab.StaticSelectedBacType == Hitbox && SceneManager.CurrentSceneState == EditorTabs.Action && !GameBase.IsPlaying;
        }

        private void Hitbox_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RefreshHitbox = true;
        }

        private void SceneManager_ActorChanged(object source, ActorChangedEventArgs e)
        {
            //Use this to update character bone reference
            if (Hitbox != null)
            {
                SetContext(Hitbox);
            }
        }
    }
}
