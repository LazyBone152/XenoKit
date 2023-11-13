using Microsoft.Xna.Framework;
using System;

namespace XenoKit.Engine.Gizmo.TransformOperations
{
    public class EntityTransformOperation : TransformOperation
    {
        private Entity entity;
        private Matrix originalMatrix;

        public EntityTransformOperation(Entity entity)
        {
            this.entity = entity;
            originalMatrix = entity.Transform;
        }

        public override void Confirm()
        {
            if (IsFinished)
                throw new InvalidOperationException($"EntityTransformOperation.Confirm: This transformation has already been finished, cannot add undo step or cancel at this point.");

            IsFinished = true;
        }

        public override void Cancel()
        {
            if (IsFinished)
                throw new InvalidOperationException($"EntityTransformOperation.Cancel: This transformation has already been finished, cannot add undo step or cancel at this point.");

            entity.Transform = originalMatrix;

            IsFinished = true;
        }

        public override void UpdatePos(Vector3 delta)
        {
            if (delta != Vector3.Zero)
            {
                Modified = true;
                entity.Transform *= Matrix.CreateTranslation(new Vector3(-delta.X, delta.Y, delta.Z));
            }
        }
    }
}
