using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Engine.Gizmo.TransformOperations
{
    public class EffectPartTransformOperation : TransformOperation
    {
        public override RotationType RotationType => RotationType.EulerAngles;

        private readonly EffectPart effectPart;
        private readonly GizmoMode gizmoMode;
        private readonly GizmoAxis gizmoAxis;

        private readonly float originalPositionX;
        private readonly float originalPositionY;
        private readonly float originalPositionZ;
        private readonly float originalRotationXMin;
        private readonly float originalRotationXMax;
        private readonly float originalRotationYMin;
        private readonly float originalRotationYMax;
        private readonly float originalRotationZMin;
        private readonly float originalRotationZMax;
        private readonly Vector3 originalAngles;

        public EffectPartTransformOperation(EffectPart effectPart, GizmoMode gizmoMode, GizmoAxis axis)
        {
            if (gizmoMode == GizmoMode.Scale)
                throw new InvalidOperationException("EffectPartTransformOperation: Scale operation not supported.");

            this.effectPart = effectPart;
            this.gizmoMode = gizmoMode;
            gizmoAxis = axis;

            originalPositionX = effectPart.PositionX;
            originalPositionY = effectPart.PositionY;
            originalPositionZ = effectPart.PositionZ;
            originalRotationXMin = effectPart.RotationX_Min;
            originalRotationXMax = effectPart.RotationX_Max;
            originalRotationYMin = effectPart.RotationY_Min;
            originalRotationYMax = effectPart.RotationY_Max;
            originalRotationZMin = effectPart.RotationZ_Min;
            originalRotationZMax = effectPart.RotationZ_Max;
            originalAngles = GetAngles(effectPart);
        }

        public override void Confirm()
        {
            if (IsFinished)
                throw new InvalidOperationException("EffectPartTransformOperation.Confirm: This transformation has already been finished, cannot add undo step or cancel at this point.");

            List<IUndoRedo> undos = new List<IUndoRedo>
            {
                new UndoablePropertyGeneric(nameof(effectPart.PositionX), effectPart, originalPositionX, effectPart.PositionX),
                new UndoablePropertyGeneric(nameof(effectPart.PositionY), effectPart, originalPositionY, effectPart.PositionY),
                new UndoablePropertyGeneric(nameof(effectPart.PositionZ), effectPart, originalPositionZ, effectPart.PositionZ),
                new UndoablePropertyGeneric(nameof(effectPart.RotationX_Min), effectPart, originalRotationXMin, effectPart.RotationX_Min),
                new UndoablePropertyGeneric(nameof(effectPart.RotationX_Max), effectPart, originalRotationXMax, effectPart.RotationX_Max),
                new UndoablePropertyGeneric(nameof(effectPart.RotationY_Min), effectPart, originalRotationYMin, effectPart.RotationY_Min),
                new UndoablePropertyGeneric(nameof(effectPart.RotationY_Max), effectPart, originalRotationYMax, effectPart.RotationY_Max),
                new UndoablePropertyGeneric(nameof(effectPart.RotationZ_Min), effectPart, originalRotationZMin, effectPart.RotationZ_Min),
                new UndoablePropertyGeneric(nameof(effectPart.RotationZ_Max), effectPart, originalRotationZMax, effectPart.RotationZ_Max)
            };

            UndoManager.Instance.AddCompositeUndo(undos, $"Effect Part {gizmoMode} {gizmoAxis}", UndoGroup.Effect);
            UndoManager.Instance.ForceEventCall(UndoGroup.Effect);

            IsFinished = true;
        }

        public override void Cancel()
        {
            if (IsFinished)
                throw new InvalidOperationException("EffectPartTransformOperation.Cancel: This transformation has already been finished, cannot add undo step or cancel at this point.");

            effectPart.PositionX = originalPositionX;
            effectPart.PositionY = originalPositionY;
            effectPart.PositionZ = originalPositionZ;
            effectPart.RotationX_Min = originalRotationXMin;
            effectPart.RotationX_Max = originalRotationXMax;
            effectPart.RotationY_Min = originalRotationYMin;
            effectPart.RotationY_Max = originalRotationYMax;
            effectPart.RotationZ_Min = originalRotationZMin;
            effectPart.RotationZ_Max = originalRotationZMax;

            IsFinished = true;
        }

        /// <summary>
        /// Basis the Position X/Y/Z values are applied in. World deltas are converted into it before being written.
        /// </summary>
        public Matrix PositionSpace { get; set; } = Matrix.Identity;

        /// <summary>
        /// True when the incoming delta is in world space, false when it already matches <see cref="PositionSpace"/>.
        /// </summary>
        public bool DeltaIsWorldSpace { get; set; } = true;

        public override void UpdatePos(Vector3 delta)
        {
            if (delta == Vector3.Zero) return;

            Modified = true;

            //Position X/Y/Z are relative to the attachment, so a world space drag has to be brought into that basis
            //first. Without this, an attachment whose axes differ from world drags the effect the wrong way.
            if (DeltaIsWorldSpace)
                delta = Vector3.Transform(delta, Matrix.Invert(PositionSpace));

            effectPart.PositionX += delta.X;
            effectPart.PositionY += delta.Y;
            effectPart.PositionZ += delta.Z;
        }

        public override void UpdateRot(Vector3 newRot)
        {
            Modified = true;

            //Shift min and max by the same amount so the random spread between them is kept, just re-aimed.
            Vector3 delta = newRot - originalAngles;

            effectPart.RotationX_Min = originalRotationXMin + delta.X;
            effectPart.RotationX_Max = originalRotationXMax + delta.X;
            effectPart.RotationY_Min = originalRotationYMin + delta.Y;
            effectPart.RotationY_Max = originalRotationYMax + delta.Y;
            effectPart.RotationZ_Min = originalRotationZMin + delta.Z;
            effectPart.RotationZ_Max = originalRotationZMax + delta.Z;
        }

        public override Vector3 GetRotationAngles()
        {
            return GetAngles(effectPart);
        }

        public override Matrix GetRotationMatrix()
        {
            return GetRotationMatrix(effectPart);
        }

        public override Matrix GetLocalMatrix()
        {
            return GetLocalMatrix(effectPart);
        }

        public override Matrix GetWorldMatrix()
        {
            return GetLocalMatrix();
        }

        /// <summary>
        /// The centre of the min/max spread. This is the angle the gizmo shows and drags.
        /// </summary>
        public static Vector3 GetAngles(EffectPart effectPart)
        {
            return new Vector3(
                (effectPart.RotationX_Min + effectPart.RotationX_Max) * 0.5f,
                (effectPart.RotationY_Min + effectPart.RotationY_Max) * 0.5f,
                (effectPart.RotationZ_Min + effectPart.RotationZ_Max) * 0.5f);
        }

        public static Matrix GetRotationMatrix(EffectPart effectPart)
        {
            if (!effectPart.EnableRotationValues) return Matrix.Identity;

            Vector3 angles = GetAngles(effectPart);

            //Matches VfxRotation.Create: X/Y/Z are pitch/yaw/roll.
            return Matrix.CreateFromYawPitchRoll(
                MathHelper.ToRadians(angles.Y),
                MathHelper.ToRadians(angles.X),
                MathHelper.ToRadians(angles.Z));
        }

        public static Matrix GetLocalMatrix(EffectPart effectPart)
        {
            return GetRotationMatrix(effectPart) * Matrix.CreateTranslation(effectPart.PositionX, effectPart.PositionY, effectPart.PositionZ);
        }
    }
}
