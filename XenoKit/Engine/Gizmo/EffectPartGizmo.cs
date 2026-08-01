using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using XenoKit.Engine.Gizmo.TransformOperations;
using XenoKit.Engine.Vfx.Asset;
using Xv2CoreLib.EEPK;

namespace XenoKit.Engine.Gizmo
{
    public class EffectPartGizmo : GizmoBase
    {
        protected override Matrix WorldMatrix
        {
            get
            {
                VfxAsset asset = GetPlayingAsset();
                if (asset == null) return Matrix.Identity;

                Matrix visual = Extensions.ToXna(asset.VisualTransform);
                Matrix position = Matrix.CreateTranslation(visual.Translation);

                Matrix attachSpace = Extensions.ToXna(asset.PositionSpace);
                attachSpace.Translation = Vector3.Zero;

                //Rotation X/Y/Z are euler angles relative to the attachment, so the rings have to be drawn in that
                //basis. Drawing them world aligned, or on the effect's already rotated axes, would edit an axis other
                //than the one grabbed.
                if (ActiveMode == GizmoMode.Rotate) return attachSpace * position;

                //Translate defaults to world so dragging up moves up. Shift switches to the basis the Position values
                //are stored in, so a drag maps straight onto them.
                if (!LocalTranslate) return position;

                return attachSpace * position;
            }
        }

        protected override ITransformOperation TransformOperation
        {
            get => transformOperation;
            set => transformOperation = value as EffectPartTransformOperation;
        }
        private EffectPartTransformOperation transformOperation = null;

        private EffectPart effectPart = null;

        public override bool AllowScale => false;
        public override bool AllowRotation => effectPart != null && effectPart.EnableRotationValues;
        public override bool AllowTranslate => true;

        //Selecting a part happens before its effect has spawned, so the gizmo cannot turn itself on yet. Enabling is
        //retried each frame until the effect is actually playing.
        private bool waitingForEffect;

        public void SetContext(EffectPart effectPart)
        {
            this.effectPart = effectPart;
            waitingForEffect = effectPart != null;
            base.SetContext();
        }

        public void RemoveContext()
        {
            SetContext(null);
            Disable();
        }

        //Hold shift to drag along the effect's own axes instead of world axes, like Blender's local transform toggle.
        protected override bool LocalTranslate => Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift);

        public override void Update()
        {
            if (waitingForEffect && IsContextValid())
            {
                Enable();

                if (IsVisible)
                    waitingForEffect = false;
            }

            if (transformOperation != null)
            {
                VfxAsset asset = GetPlayingAsset();

                transformOperation.PositionSpace = asset != null ? Extensions.ToXna(asset.PositionSpace) : Matrix.Identity;
                transformOperation.DeltaIsWorldSpace = !LocalTranslate;
            }

            base.Update();
        }

        public override bool IsContextValid()
        {
            return effectPart != null && SceneManager.IsOnTab(EditorTabs.Effect) && GetPlayingAsset() != null;
        }

        protected override void StartTransformOperation()
        {
            if (IsContextValid())
            {
                transformOperation = new EffectPartTransformOperation(effectPart, ActiveMode, ActiveAxis);
            }
        }

        private VfxAsset GetPlayingAsset()
        {
            if (effectPart == null) return null;

            //The Effect tab previews through VfxPreview, so check there first, then the effects spawned by BAC playback.
            return ViewportInstance?.VfxPreview?.FindAsset(effectPart)
                ?? ViewportInstance?.VfxManager?.FindPlayingAsset(effectPart);
        }
    }
}
