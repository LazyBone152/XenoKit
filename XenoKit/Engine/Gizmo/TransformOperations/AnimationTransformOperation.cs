using System;
using System.Linq;
using System.Collections.Generic;
using XenoKit.Controls;
using XenoKit.Engine.Animation;
using Xv2CoreLib.EAN;
using Xv2CoreLib.Resource.UndoRedo;
using Microsoft.Xna.Framework;

namespace XenoKit.Engine.Gizmo.TransformOperations
{
    public class AnimationTransformOperation : TransformOperation
    {
        private List<IUndoRedo> undos = new List<IUndoRedo>();
        public override RotationType RotationType => RotationType.Quaternion;

        public EAN_Keyframe PosKeyframe;
        public EAN_Keyframe RotKeyframe;
        public EAN_Keyframe ScaleKeyframe;

        private List<EAN_Keyframe> PosKeyframes = new List<EAN_Keyframe>();
        private List<EAN_Keyframe> RotKeyframes = new List<EAN_Keyframe>();
        private List<EAN_Keyframe> ScaleKeyframes = new List<EAN_Keyframe>();

        private float[] OriginalKeyframeValues;

        public EAN_Node node;
        private int frame;
        GizmoMode mode;

        //Deltas:
        private Vector3 totalPosDetla = Vector3.Zero;
        private Quaternion totalRot;
        private Vector3 totalScaleDetla = Vector3.Zero;


        public AnimationTransformOperation(AnimationPlayer animator, string nodeName, GizmoMode mode)
        {
            //Nodes, components and keyframes will all be created on the animation as an undoable operation.
            this.mode = mode;
            frame = animator.PrimaryAnimation.CurrentFrame_Int;
            node = animator.PrimaryAnimation.Animation.GetNode(nodeName, true, undos);

            PosKeyframe = node.GetKeyframe(frame, EAN_AnimationComponent.ComponentType.Position, mode == GizmoMode.Translate, undos);
            RotKeyframe = node.GetKeyframe(frame, EAN_AnimationComponent.ComponentType.Rotation, mode == GizmoMode.Rotate, undos);
            ScaleKeyframe = node.GetKeyframe(frame, EAN_AnimationComponent.ComponentType.Scale, mode == GizmoMode.Scale, undos);

            //Create dummy keyframes.
            if (PosKeyframe == null)
                PosKeyframe = EAN_AnimationComponent.GetDefaultKeyframe(node.EskRelativeTransform, EAN_AnimationComponent.ComponentType.Position, false);

            if (RotKeyframe == null)
                RotKeyframe = EAN_AnimationComponent.GetDefaultKeyframe(node.EskRelativeTransform, EAN_AnimationComponent.ComponentType.Rotation, false);

            if (ScaleKeyframe == null)
                ScaleKeyframe = EAN_AnimationComponent.GetDefaultKeyframe(node.EskRelativeTransform, EAN_AnimationComponent.ComponentType.Scale, false);

            OriginalKeyframeValues = node.GetKeyframeValues(frame);

            //Get all selected keyframes
            List<int> keyframes = AnimationTabView.Instance.keyframeDataGrid.SelectedItems.Cast<int>().ToList();
            keyframes.Remove(frame);

            foreach (var keyframe in keyframes)
            {
                var pos = node.GetKeyframe(keyframe, EAN_AnimationComponent.ComponentType.Position, mode == GizmoMode.Translate, undos);
                var rot = node.GetKeyframe(keyframe, EAN_AnimationComponent.ComponentType.Rotation, mode == GizmoMode.Rotate, undos);
                var scale = node.GetKeyframe(keyframe, EAN_AnimationComponent.ComponentType.Scale, mode == GizmoMode.Scale, undos);

                if (pos != null)
                    PosKeyframes.Add(pos);

                if (rot != null)
                    RotKeyframes.Add(rot);

                if (scale != null)
                    ScaleKeyframes.Add(scale);
            }


            SceneManager.InvokeAnimationDataChangedEvent();
            UndoManager.Instance.ForceEventCall();
        }

        public override void Confirm()
        {
            if (IsFinished)
                throw new InvalidOperationException($"TransformOperation.Confirm: This transformation has already been finished, cannot add undo step or cancel at this point.");

            if (mode == GizmoMode.Translate)
            {
                undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.X), PosKeyframe, OriginalKeyframeValues[0], PosKeyframe.X));
                undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Y), PosKeyframe, OriginalKeyframeValues[1], PosKeyframe.Y));
                undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Z), PosKeyframe, OriginalKeyframeValues[2], PosKeyframe.Z));
                undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.W), PosKeyframe, OriginalKeyframeValues[3], PosKeyframe.W));

                foreach (var keyframe in PosKeyframes)
                    UpdatePos(keyframe, totalPosDetla, true);
            }

            if (mode == GizmoMode.Rotate)
            {
                undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.X), RotKeyframe, OriginalKeyframeValues[4], RotKeyframe.X));
                undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Y), RotKeyframe, OriginalKeyframeValues[5], RotKeyframe.Y));
                undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Z), RotKeyframe, OriginalKeyframeValues[6], RotKeyframe.Z));
                undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.W), RotKeyframe, OriginalKeyframeValues[7], RotKeyframe.W));

                foreach (var keyframe in RotKeyframes)
                    UpdateRot(keyframe, totalRot, true);
            }

            if (mode == GizmoMode.Scale)
            {
                undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.X), ScaleKeyframe, OriginalKeyframeValues[8], ScaleKeyframe.X));
                undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Y), ScaleKeyframe, OriginalKeyframeValues[9], ScaleKeyframe.Y));
                undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Z), ScaleKeyframe, OriginalKeyframeValues[10], ScaleKeyframe.Z));
                undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.W), ScaleKeyframe, OriginalKeyframeValues[11], ScaleKeyframe.W));

                foreach (var keyframe in ScaleKeyframes)
                    UpdateScale(keyframe, totalScaleDetla, true);
            }

            UndoManager.Instance.AddCompositeUndo(undos, $"Animator (_{node.BoneName} - {mode})");
            IsFinished = true;

            UndoManager.Instance.ForceEventCall();
        }

        public override void Cancel()
        {
            if (IsFinished)
                throw new InvalidOperationException($"TransformOperation.Cancel: This transformation has already been finished, cannot add undo step or cancel at this point.");

            CompositeUndo undo = new CompositeUndo(undos, "");
            undo.Undo();

            IsFinished = true;

            UndoManager.Instance.ForceEventCall();
            SceneManager.InvokeAnimationDataChangedEvent();
        }

        public override Matrix GetLocalMatrix()
        {
            Matrix local = Matrix.Identity;

            local *= Matrix.CreateScale(new Vector3(ScaleKeyframe.X, ScaleKeyframe.Y, ScaleKeyframe.Z) * ScaleKeyframe.W);
            local *= Matrix.CreateFromQuaternion(new Quaternion(RotKeyframe.X, RotKeyframe.Y, RotKeyframe.Z, RotKeyframe.W));
            local *= Matrix.CreateTranslation(new Vector3(PosKeyframe.X, PosKeyframe.Y, PosKeyframe.Z) * PosKeyframe.W);

            return local;
        }

        public override Matrix GetRotationMatrix()
        {
            return Matrix.CreateFromQuaternion(new Quaternion(RotKeyframe.X, RotKeyframe.Y, RotKeyframe.Z, RotKeyframe.W));
        }

        public override Matrix GetWorldMatrix()
        {
            return SceneManager.Actors[0].AnimationPlayer.GetCurrentParentAbsoluteMatrix(node.BoneName) * GetLocalMatrix();
        }

        public override Quaternion GetRotation()
        {
            return new Quaternion(RotKeyframe.X, RotKeyframe.Y, RotKeyframe.Z, RotKeyframe.W);
        }

        public override void UpdatePos(Vector3 delta)
        {
            totalPosDetla += delta;
            UpdatePos(PosKeyframe, delta, false);
        }

        public override void UpdateRot(Quaternion newRot)
        {
            totalRot = newRot;
            UpdateRot(RotKeyframe, newRot, false);
        }

        public override void UpdateScale(Vector3 delta)
        {
            totalScaleDetla += delta;
            UpdateScale(ScaleKeyframe, delta, false);
        }

        private void UpdatePos(EAN_Keyframe keyframe, Vector3 delta, bool addUndo)
        {
            if (delta != Vector3.Zero)
            {
                Modified = true;

                keyframe.ScaleByWorld(addUndo ? undos : null);

                //Get absolute matrix of parent so that we can translate the keyframe from local space into world space
                Matrix absoluteMatrix = SceneManager.Actors[0].AnimationPlayer.GetCurrentParentAbsoluteMatrix(node.BoneName);

                Vector3 pos = Vector3.Transform(new Vector3(keyframe.X, keyframe.Y, keyframe.Z), absoluteMatrix);

                //Increment the keyframe values (world space).
                pos.X += -delta.X;
                pos.Y += delta.Y;
                pos.Z += delta.Z;

                //Translate the keyframe values back to local space
                pos = Vector3.Transform(pos, Matrix.Invert(absoluteMatrix));

                if (addUndo)
                {
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.X), keyframe, keyframe.X, pos.X));
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Y), keyframe, keyframe.Y, pos.Y));
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Z), keyframe, keyframe.Z, pos.Z));
                }

                //Update the keyframe values
                keyframe.X = pos.X;
                keyframe.Y = pos.Y;
                keyframe.Z = pos.Z;
            }
        }

        private void UpdateRot(EAN_Keyframe keyframe, Quaternion newRot, bool addUndo)
        {
            var rot = new Quaternion(keyframe.X, keyframe.Y, keyframe.Z, keyframe.W);
            if (newRot != rot)
            {
                Modified = true;

                if (addUndo)
                {
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.X), keyframe, keyframe.X, newRot.X));
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Y), keyframe, keyframe.Y, newRot.Y));
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Z), keyframe, keyframe.Z, newRot.Z));
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.W), keyframe, keyframe.W, newRot.W));
                }

                keyframe.X = newRot.X;
                keyframe.Y = newRot.Y;
                keyframe.Z = newRot.Z;
                keyframe.W = newRot.W;
            }
        }

        private void UpdateScale(EAN_Keyframe keyframe, Vector3 delta, bool addUndo)
        {
            if (delta != Vector3.Zero)
            {
                Modified = true;
                keyframe.ScaleByWorld(addUndo ? undos : null);

                float newX = keyframe.X + delta.X;
                float newY = keyframe.Y + delta.Y;
                float newZ = keyframe.Z + delta.Z;

                if (addUndo)
                {
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.X), keyframe, keyframe.X, newX));
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Y), keyframe, keyframe.Y, newY));
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Z), keyframe, keyframe.Z, newZ));
                }

                keyframe.X += delta.X;
                keyframe.Y += delta.Y;
                keyframe.Z += delta.Z;
            }
        }
    }

}
