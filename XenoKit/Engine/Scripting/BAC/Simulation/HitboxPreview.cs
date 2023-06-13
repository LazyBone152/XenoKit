using Microsoft.Xna.Framework;
using XenoKit.Engine.Shapes;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Scripting.BAC.Simulation
{
    public class HitboxPreview : BacSimulationObject
    {
        protected Matrix WorldMatrix
        {
            get
            {
                if (Hitbox == null) return Matrix.Identity;
                Matrix world = Matrix.Identity;

                if (boneIdx != -1 && SceneManager.Actors[0] != null)
                {
                    world = SceneManager.Actors[0].GetAbsoluteBoneMatrix(boneIdx);

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
        private int boneIdx = -1;
        private bool isBaseBone = false;
        private bool RefreshHitbox = false;

        public HitboxPreview(BAC_Type1 hitbox, BacEntryInstance bacEntryInstance, GameBase gameBase) : base(hitbox, bacEntryInstance, false, gameBase)
        {
            Hitbox = hitbox;
            BoundingBox = new Cube(new Vector3(0.5f), new Vector3(-0.5f), new Vector3(0.5f), 0.5f, Color.Blue, true, gameBase);

            UpdateHitbox();
            Hitbox.PropertyChanged += Hitbox_PropertyChanged;
        }

        public override void Update()
        {
            if (!IsValidForCurrentFrame())
            {
                Destroy();
                return;
            }

            if (RefreshHitbox && IsContextValid())
            {
                UpdateHitbox();
                RefreshHitbox = false;
            }
        }

        public override void Draw()
        {
            if (IsContextValid())
            {
                BoundingBox.Draw(WorldMatrix);
            }
        }

        private void UpdateHitbox()
        {
            if (Hitbox != null && SceneManager.Actors[0] != null)
            {
                string boneName = Hitbox.BoneLink.ToString();

                isBaseBone = boneName == Xv2CoreLib.ESK.ESK_File.BaseBone;
                boneIdx = SceneManager.Actors[0].Skeleton.GetBoneIndex(boneName);

                //BAC Hitbox bounds are defined in half-metres (1.0 is actually 0.5)
                BoundingBox.SetBounds(new Vector3(Hitbox.MinX, Hitbox.MinY, Hitbox.MinZ) / 2, new Vector3(Hitbox.MaxX, Hitbox.MaxY, Hitbox.MaxZ) / 2, Hitbox.Size / 2, Hitbox.BoundingBoxType != BAC_Type1.BoundingBoxTypeEnum.Uniform);
                BoundingBox.SetPosition(new Vector3(Hitbox.PositionX, Hitbox.PositionY, Hitbox.PositionZ));
            }
        }

        private void Hitbox_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RefreshHitbox = true;
        }

        public override void Dispose()
        {
            if(Hitbox != null)
                Hitbox.PropertyChanged -= Hitbox_PropertyChanged;

            base.Dispose();
        }

        protected override bool IsContextValid()
        {
            int type = Controls.BacTab.StaticSelectedBacType != null ? Controls.BacTab.StaticSelectedBacType.TypeID : -1;

            //Valid context if IsPlaying OR selected bac type isn't a hitbox (in which case, a HitboxGizmo will be visible)
            return (GameBase.IsPlaying || type != 1) && IsValidForCurrentFrame() && SettingsManager.Instance.Settings.XenoKit_HitboxSimulation;
        }

        protected override void ActionStoppedEvent(ActionSimulationState state)
        {
            if(state == ActionSimulationState.DurationElapsed || state == ActionSimulationState.SimulationEnded)
            {
                Destroy();
            }
        }
    }
}
