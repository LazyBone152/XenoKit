using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Engine.Gizmo.TransformOperations
{
    public class BacMatrixTransformOperation : TransformOperation
    {
        public override RotationType RotationType => RotationType.EulerAngles;

        IBacTypeMatrix bacMatrix;
        IBacTypeMatrix originalBacMatrix;
        GizmoMode GizmoMode;
        GizmoAxis GizmoAxis;

        public BacMatrixTransformOperation(IBacTypeMatrix bacMatrix, GizmoMode gizmoMode, GizmoAxis axis)
        {
            this.bacMatrix = bacMatrix;
            originalBacMatrix = bacMatrix.Copy();
            GizmoMode = gizmoMode;
            GizmoAxis = axis;

            if(gizmoMode == GizmoMode.Scale)
                throw new InvalidOperationException($"BacMatrixTransformOperation: Scale operation not supported.");

        }


        public override void Confirm()
        {
            if (IsFinished)
                throw new InvalidOperationException($"BacMatrixTransformOperation.Confirm: This transformation has already been finished, cannot add undo step or cancel at this point.");

            List<IUndoRedo> undos = new List<IUndoRedo>();

            undos.Add(new UndoablePropertyGeneric(nameof(bacMatrix.PositionX), bacMatrix, originalBacMatrix.PositionX, bacMatrix.PositionX));
            undos.Add(new UndoablePropertyGeneric(nameof(bacMatrix.PositionY), bacMatrix, originalBacMatrix.PositionY, bacMatrix.PositionY));
            undos.Add(new UndoablePropertyGeneric(nameof(bacMatrix.PositionZ), bacMatrix, originalBacMatrix.PositionZ, bacMatrix.PositionZ));
            undos.Add(new UndoablePropertyGeneric(nameof(bacMatrix.RotationX), bacMatrix, originalBacMatrix.RotationX, bacMatrix.RotationX));
            undos.Add(new UndoablePropertyGeneric(nameof(bacMatrix.RotationY), bacMatrix, originalBacMatrix.RotationY, bacMatrix.RotationY));
            undos.Add(new UndoablePropertyGeneric(nameof(bacMatrix.RotationZ), bacMatrix, originalBacMatrix.RotationZ, bacMatrix.RotationZ));

            UndoManager.Instance.AddCompositeUndo(undos, $"BAC {GizmoMode} {GizmoAxis}", UndoGroup.Action);
            UndoManager.Instance.ForceEventCall(UndoGroup.Action);

            IsFinished = true;
        }

        public override void Cancel()
        {
            if (IsFinished)
                throw new InvalidOperationException($"BacMatrixTransformOperation.Cancel: This transformation has already been finished, cannot add undo step or cancel at this point.");

            bacMatrix.PositionX = originalBacMatrix.PositionX;
            bacMatrix.PositionY = originalBacMatrix.PositionY;
            bacMatrix.PositionZ = originalBacMatrix.PositionZ;
            bacMatrix.RotationX = originalBacMatrix.RotationX;
            bacMatrix.RotationY = originalBacMatrix.RotationY;
            bacMatrix.RotationZ = originalBacMatrix.RotationZ;

            IsFinished = true;
        }

        public static Matrix GetLocalMatrix(IBacTypeMatrix bacMatrix)
        {
            Matrix local = Matrix.Identity;

            //local *= Matrix.CreateScale(new Vector3(bacMatrix.ScaleX, bacMatrix.ScaleY, bacMatrix.ScaleZ));
            local *= Matrix.CreateFromYawPitchRoll(MathHelper.ToRadians(bacMatrix.RotationX), MathHelper.ToRadians(bacMatrix.RotationY), MathHelper.ToRadians(bacMatrix.RotationZ));
            local *= Matrix.CreateTranslation(new Vector3(bacMatrix.PositionX, bacMatrix.PositionY, bacMatrix.PositionZ));

            return local;
        }

        public override Matrix GetLocalMatrix()
        {
            return GetLocalMatrix(bacMatrix);
        }

        public override Matrix GetWorldMatrix()
        {
            return GetLocalMatrix();
        }

        public override Matrix GetRotationMatrix()
        {
            return Matrix.CreateFromYawPitchRoll(MathHelper.ToRadians(bacMatrix.RotationX), MathHelper.ToRadians(bacMatrix.RotationY), MathHelper.ToRadians(bacMatrix.RotationZ));
        }

        public override Vector3 GetRotationAngles()
        {
            return new Vector3(bacMatrix.RotationX, bacMatrix.RotationY, bacMatrix.RotationZ);
        }

        public override void UpdatePos(Vector3 delta)
        {
            if(delta != Vector3.Zero)
            {
                Modified = true;

                bacMatrix.PositionX += (-delta.X);
                bacMatrix.PositionY += delta.Y;
                bacMatrix.PositionZ += delta.Z;
            }
        }

        public override void UpdateRot(Vector3 newRot)
        {
            Modified = true;

            bacMatrix.RotationX = newRot.X;
            bacMatrix.RotationY = newRot.Y;
            bacMatrix.RotationZ = newRot.Z;
        }

    }
}
