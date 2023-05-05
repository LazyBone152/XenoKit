using Microsoft.Xna.Framework;
using XenoKit.Engine.Gizmo.TransformOperations;
using Xv2CoreLib.BCS;

namespace XenoKit.Engine.Gizmo
{
    public class BoneScaleGizmo : GizmoBase
    {
        //Bone and character info
        private Actor character = null;
        private string boneName = string.Empty;
        private int boneIdx = -1;
        protected override Matrix WorldMatrix => boneIdx != -1 ? character.GetAbsoluteBoneMatrix(boneIdx) : Matrix.Identity;

        protected override ITransformOperation TransformOperation
        {
            get => transformOperation;
            set
            {
                if (value is BoneScaleTransformOperation transformOp)
                {
                    transformOperation = transformOp;
                }
                else
                {
                    transformOperation = null;
                }
            }
        }
        private BoneScaleTransformOperation transformOperation = null;

        private Body body = null;
        private BoneScale boneScale = null;

        //Settings
        protected override bool AllowRotation => false;
        protected override bool AllowTranslate => false;

        public BoneScaleGizmo(GameBase gameBase) : base(gameBase)
        {

        }

        public void SetContext(BoneScale boneScale, Body body, Actor _character, string boneName)
        {
            character = _character;
            this.boneName = boneName;

            if(_character?.Skeleton != null)
                boneIdx = _character.Skeleton.GetBoneIndex(boneName);

            this.boneScale = boneScale;
            this.body = body;

            base.SetContext();
        }

        public void RemoveContext()
        {
            SetContext(null, null, null, null);
        }

        public override bool IsContextValid()
        {
            return body != null && boneScale != null && SceneManager.IsOnTab(EditorTabs.BCS_Bodies);
        }

        protected override void StartTransformOperation()
        {
            if (body != null && boneScale != null)
            {
                transformOperation = new BoneScaleTransformOperation(boneScale, body);
            }
        }

        public override bool IsEnabledOnBone(int bone)
        {
            return (bone == boneIdx);
        }
    }
}
