using Microsoft.Xna.Framework;

namespace XenoKit.Engine.Gizmo.TransformOperations
{
    public interface ITransformOperation
    {
        RotationType RotationType { get; }
        bool IsFinished { get; }
        bool Modified { get; }

        void Confirm();

        void Cancel();

        void UpdatePos(Vector3 delta);

        void UpdateRot(Quaternion newRot);

        void UpdateRot(Vector3 newRot);

        void UpdateScale(Vector3 delta);

        Matrix GetLocalMatrix();

        Matrix GetRotationMatrix();

        Matrix GetWorldMatrix();

        Quaternion GetRotation();

        Vector3 GetRotationAngles();
    }

    public abstract class TransformOperation : ITransformOperation
    {
        public virtual RotationType RotationType => RotationType.EulerAngles;
        public bool IsFinished { get; protected set; }
        public bool Modified { get; protected set; }


        public abstract void Confirm();

        public abstract void Cancel();

        public virtual void UpdatePos(Vector3 delta)
        {
        }

        public virtual void UpdateRot(Quaternion newRot)
        {

        }

        public virtual void UpdateRot(Vector3 newRot)
        {

        }

        public virtual void UpdateScale(Vector3 delta)
        {

        }

        public virtual Matrix GetLocalMatrix()
        {
            return Matrix.Identity;
        }

        public virtual Matrix GetRotationMatrix()
        {
            return Matrix.Identity;
        }

        public virtual Matrix GetWorldMatrix()
        {
            return Matrix.Identity;
        }

        public virtual Quaternion GetRotation()
        {
            return Quaternion.Identity;
        }

        public virtual Vector3 GetRotationAngles()
        {
            return Vector3.Zero;
        }
    }

    public enum RotationType
    {
        Quaternion,
        EulerAngles
    }
}
