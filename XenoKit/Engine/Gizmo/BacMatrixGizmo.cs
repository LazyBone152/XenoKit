using Microsoft.Xna.Framework;
using XenoKit.Engine.Gizmo.TransformOperations;
using Xv2CoreLib.BAC;

namespace XenoKit.Engine.Gizmo
{
    public class BacMatrixGizmo : GizmoBase
    {
        protected override Matrix WorldMatrix
        {
            get
            {
                if (matrix == null) return Matrix.Identity;
                Matrix world = Matrix.Identity;

                if(boneIdx != -1 && SceneManager.Actors[0] != null)
                {
                    world = SceneManager.Actors[0].GetAbsoluteBoneMatrix(boneIdx);

                    //Hitbox doesn't rotate with b_C_Base, so the rotation needs to be removed
                    if (IgnoreRotationOnBaseBone && IsBaseBone)
                    {
                        world = Matrix.CreateTranslation(world.Translation);
                    }
                }

                return BacMatrixTransformOperation.GetLocalMatrix(matrix) * world;
            }
        }

        protected override ITransformOperation TransformOperation
        {
            get => transformOperation;
            set
            {
                if (value is BacMatrixTransformOperation transformOp)
                {
                    transformOperation = transformOp;
                }
                else
                {
                    transformOperation = null;
                }
            }
        }
        private BacMatrixTransformOperation transformOperation = null;

        private IBacTypeMatrix matrix = null;
        private bool PositionEnabled = true;
        private bool RotationEnabled = true;
        private bool ScaleEnabled = false;
        private string boneName = null;
        private int boneIdx = -1;
        private bool IsBaseBone = false;
        private bool IgnoreRotationOnBaseBone = false;
        private EditorTabs contextTab = EditorTabs.Action;

        //Settings
        protected override bool AllowScale => ScaleEnabled;
        protected override bool AllowRotation => RotationEnabled;
        protected override bool AllowTranslate => PositionEnabled;

        public BacMatrixGizmo(GameBase gameBase) : base(gameBase)
        {

        }

        public void SetContext(IBacTypeMatrix matrix, bool pos, bool rot, bool scale, string boneName, bool ignoreRotationOnBase, EditorTabs contextTab)
        {
            this.matrix = matrix;
            PositionEnabled = pos;
            RotationEnabled = rot;
            ScaleEnabled = scale;
            IgnoreRotationOnBaseBone = ignoreRotationOnBase;
            this.boneName = boneName;
            this.contextTab = contextTab;
            boneIdx = SceneManager.Actors[0] != null ? SceneManager.Actors[0].Skeleton.GetBoneIndex(boneName) : -1;
            IsBaseBone = boneName == Xv2CoreLib.ESK.ESK_File.BaseBone;

            base.SetContext();
        }

        public void RemoveContext()
        {
            SetContext(null, true, true, false, null, false, EditorTabs.Action);
        }

        public override bool IsContextValid()
        {
            return matrix != null && SceneManager.IsOnTab(contextTab) && !GameBase.IsPlaying;
        }

        protected override void StartTransformOperation()
        {
            if (matrix != null)
            {
                transformOperation = new BacMatrixTransformOperation(matrix, ActiveMode, ActiveAxis);
            }
        }

        public override bool IsEnabledOnBone(int bone)
        {
            return (bone == boneIdx);
        }
    }
}
