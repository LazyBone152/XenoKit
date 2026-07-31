using Microsoft.Xna.Framework;
using System;
using System.Linq;
using XenoKit.Engine.Shapes;
using Xv2CoreLib.BAC;
using Xv2CoreLib.CBS;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Scripting.BAC.Simulation
{
    public class HitboxPreview : BacVisualCueObject, IDisposable
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

        private readonly BAC_Type1 Hitbox;
        private readonly Cube BoundingBox;
        private Actor actor;
        private int boneIdx = -1;
        private bool isBaseBone = false;
        private bool RefreshHitbox = false;

        public HitboxPreview(BAC_Type1 hitbox, BacEntryInstance bacEntryInstance) : base(hitbox, bacEntryInstance)
        {
            Hitbox = hitbox;
            BoundingBox = new Cube(new Vector3(0.5f), new Vector3(-0.5f), new Vector3(0.5f), 0.5f, Color.Blue, true);

            UpdateHitbox();
            Hitbox.PropertyChanged += Hitbox_PropertyChanged;
        }

        public override void Update()
        {
            if (RefreshHitbox && IsContextValid())
            {
                UpdateHitbox();
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

        private void UpdateHitbox()
        {
            if (Hitbox != null && SceneManager.Actors[0] != null)
            {
                string boneName = Hitbox.BoneLink.ToString();
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
                    CBS_Entry cbsEntry = actor.CharacterData.CbsEntry.Find(x => x.BodyId == actor.Skeleton.GetActiveBoneScaleId());
                    float cbsScaling = 1f;

                    if (cbsEntry != null)
                    {
                        switch (ParentBacInstance.SkillMove.MoveType)
                        {
                            case Editor.Move.Type.Moveset:
                                cbsScaling = cbsEntry.F_04;
                                break;
                            case Editor.Move.Type.Skill:
                                cbsScaling = cbsEntry.F_12;
                                break;
                            default:
                                cbsScaling = 1f;
                                break;
                        }
                    }

                    //BAC Hitbox bounds are defined in half-metres (1.0 is actually 0.5)
                    BoundingBox.SetBounds(
                        new Vector3(Hitbox.MinX, (Hitbox.MinY + 0.5f), Hitbox.MinZ) * cbsScaling, 
                        new Vector3(Hitbox.MaxX, Hitbox.MaxY, Hitbox.MaxZ) * cbsScaling,
                        Hitbox.Size * cbsScaling / 2f, 
                        Hitbox.BoundingBoxType != BAC_Type1.BoundingBoxTypeEnum.Uniform
                    );
                    BoundingBox.SetPosition(new Vector3(Hitbox.PositionX, Hitbox.PositionY, Hitbox.PositionZ));
                }
            }
        }

        private void Hitbox_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RefreshHitbox = true;
        }

        public void Dispose()
        {
            if(Hitbox != null)
                Hitbox.PropertyChanged -= Hitbox_PropertyChanged;
        }

        protected override bool IsContextValid()
        {
            int type = Controls.BacTab.StaticSelectedBacType != null ? Controls.BacTab.StaticSelectedBacType.TypeID : -1;

            //Valid context if IsPlaying OR selected bac type isn't a hitbox (in which case, a HitboxGizmo will be visible)
            return (ViewportInstance.IsPlaying || type != 1) && IsValidForCurrentFrame() && SettingsManager.Instance.Settings.XenoKit_HitboxSimulation;
        }

    }
}
