using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using XenoKit.Engine.Gizmo.TransformOperations;

namespace XenoKit.Engine.Gizmo
{
    public class AnimatorGizmo : GizmoBase
    {
        //Bone and character info
        private Actor character = null;
        private string boneName = string.Empty;
        private int boneIdx = -1;
        protected override Matrix WorldMatrix => character.GetAbsoluteBoneMatrix(boneIdx);

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

        public void SetContext(Actor _character, string boneName)
        {
            character = _character;
            boneIdx = _character != null ? _character.Skeleton.GetBoneIndex(boneName) : -1;

            if (boneIdx == -1)
            {
                //Bone not on this characters skeleton.
                character = null;
                Disable();
                return;
            }

            this.boneName = boneName;
            SceneManager.CurrentSelectedBoneName = boneName;

            base.SetContext();
        }

        public override bool IsContextValid()
        {
            return (character != null && !string.IsNullOrWhiteSpace(boneName) && SceneManager.IsOnTab(EditorTabs.Animation));
        }

        protected override void StartTransformOperation()
        {
            if(character?.AnimationPlayer.PrimaryAnimation != null)
                transformOperation = new AnimationTransformOperation(character?.AnimationPlayer, boneName, ActiveMode);
        }

        public override bool IsEnabledOnBone(int bone)
        {
            return (bone == boneIdx);
        }
    }

}
