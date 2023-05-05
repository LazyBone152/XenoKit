using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using XenoKit.Editor;
using XenoKit.Views;
using Xv2CoreLib.BCS;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Engine.Gizmo.TransformOperations
{
    class BoneScaleTransformOperation : TransformOperation
    {
        //Body
        private BoneScale bodyScale;
        private float originalScaleX;
        private float originalScaleY;
        private float originalScaleZ;

        private bool isBodyActive = true;

        public BoneScaleTransformOperation(BoneScale bone, Body body)
        {
            bodyScale = bone;
            originalScaleX = bone.ScaleX;
            originalScaleY = bone.ScaleY;
            originalScaleZ = bone.ScaleZ;

            if (SceneManager.Actors[0]?.Skeleton?.HasBoneScale(body) == false)
            {
                isBodyActive = false;
                Log.Add("Body being edited is not currently applied to actor. Changes will not be visible.", LogType.Warning);
            }
        }

        public override void Confirm()
        {
            if (IsFinished)
                throw new InvalidOperationException($"BoneScaleTransformOperation.Confirm: This transformation has already been finished, cannot add undo step or cancel at this point.");

            List<IUndoRedo> undos = new List<IUndoRedo>();

            undos.Add(new UndoablePropertyGeneric(nameof(bodyScale.ScaleX), bodyScale, originalScaleX, bodyScale.ScaleX));
            undos.Add(new UndoablePropertyGeneric(nameof(bodyScale.ScaleY), bodyScale, originalScaleX, bodyScale.ScaleY));
            undos.Add(new UndoablePropertyGeneric(nameof(bodyScale.ScaleZ), bodyScale, originalScaleX, bodyScale.ScaleZ));

            UndoManager.Instance.AddCompositeUndo(undos, "Bone Scale", UndoGroup.BCS, BcsBodyView.UNDO_BODY_ARG);
            UndoManager.Instance.ForceEventCall(UndoGroup.BCS, BcsBodyView.UNDO_BODY_ARG);
            
            IsFinished = true;
        }

        public override void Cancel()
        {
            if (IsFinished)
                throw new InvalidOperationException($"BoneScaleTransformOperation.Cancel: This transformation has already been finished, cannot add undo step or cancel at this point.");

            bodyScale.ScaleX = originalScaleX;
            bodyScale.ScaleY = originalScaleY;
            bodyScale.ScaleZ = originalScaleZ;

            if (isBodyActive)
                SceneManager.Actors[0].Skeleton.UpdateBoneScale();

            IsFinished = true;
        }

        public override void UpdateScale(Vector3 delta)
        {
            if (delta != Vector3.Zero)
            {
                Modified = true;
                bodyScale.ScaleX += delta.X;
                bodyScale.ScaleY += delta.Y;
                bodyScale.ScaleZ += delta.Z;

                if(isBodyActive)
                    SceneManager.Actors[0].Skeleton.UpdateBoneScale();
            }
        }

    }
}
