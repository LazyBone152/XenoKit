using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using XenoKit.Engine.Animation;
using XenoKit.Engine.Gizmo.TransformOperations;

namespace XenoKit.Engine.Gizmo
{
    public class AnimatorGizmo : GizmoBase
    {
        //Bone and skinned entity info
        private ISkinned skinnedEntity = null;
        private string boneName = string.Empty;
        private int boneIdx = -1;
        protected override Matrix WorldMatrix => skinnedEntity.GetAbsoluteBoneMatrix(boneIdx);

        //Keyframe
        protected override ITransformOperation TransformOperation
        {
            get => transformOperation;
            set
            {
                if(value is AnimationTransformOperation animTransformOp) 
                {
                    transformOperation = animTransformOp;
                }
                else
                {
                    transformOperation = null;
                }
            }
        }
        private AnimationTransformOperation transformOperation = null;

        //Settings
        protected override bool AutoPause => true;

        public AnimatorGizmo(GameBase gameBase) : base(gameBase)
        {
        }

        public void RemoveContext()
        {
            SetContext(null, null);
        }

        public void SetContext(ISkinned _character, string boneName)
        {
            skinnedEntity = _character;
            boneIdx = _character != null ? _character.Skeleton.GetBoneIndex(boneName) : -1;

            if (boneIdx == -1)
            {
                //Bone not on this characters skeleton.
                skinnedEntity = null;
                Disable();
                return;
            }

            this.boneName = boneName;
            SceneManager.CurrentSelectedBoneName = boneName;

            base.SetContext();
        }

        public override bool IsContextValid()
        {
            return (skinnedEntity != null && !string.IsNullOrWhiteSpace(boneName) && SceneManager.IsOnTab(EditorTabs.Animation, EditorTabs.InspectorAnimation));
        }

        protected override void StartTransformOperation()
        {
            if(skinnedEntity?.AnimationPlayer.PrimaryAnimation != null)
                transformOperation = new AnimationTransformOperation(skinnedEntity, boneName, ActiveMode);
        }

        public override bool IsEnabledOnBone(int bone)
        {
            return (bone == boneIdx);
        }
    }

}
