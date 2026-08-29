using System;
using Microsoft.Xna.Framework;
using XenoKit.Engine.Shapes;

namespace XenoKit.Engine.Collision
{
    public class HitboxVisual : EngineObject, IDisposable
    {
        private enum ShapeType
        {
            None,
            Sphere,
            Capsule,
            Box
        }

        private readonly Color fillColor;
        private readonly Color wireColor;
        private readonly Cube boxFill;
        private readonly Cube boxWireframe;
        private Sphere sphere;
        private Capsule capsule;
        private Vector3 spherePosition;
        private Vector3 boxPosition;
        private float sphereRadius;
        private Vector3 capsuleStart;
        private Vector3 capsuleEnd;
        private float capsuleRadius;
        private ShapeType shapeType;

        public HitboxVisual(Color fillColor, Color wireColor)
        {
            this.fillColor = fillColor;
            this.wireColor = wireColor;
            //boxFill = new Cube(Vector3.Zero, new Vector3(-1f), new Vector3(1f), 0f, fillColor, false);
            boxWireframe = new Cube(Vector3.Zero, new Vector3(-1f), new Vector3(1f), 0f, wireColor, true, false);
        }

        public void Clear()
        {
            shapeType = ShapeType.None;
        }

        public void SetSphere(Vector3 position, float radius)
        {
            if (!IsFinite(position) || !IsFinite(radius) || radius <= 0f)
                return;

            if (sphere == null || !AreClose(sphereRadius, radius))
            {
                sphere?.Dispose();
                sphere = new Sphere(radius * 2f, true, 8);
                sphereRadius = radius;
            }

            spherePosition = position;
            shapeType = ShapeType.Sphere;
        }

        public void SetCapsule(Vector3 start, Vector3 end, float radius)
        {
            if (!IsFinite(start) || !IsFinite(end) || !IsFinite(radius) || radius <= 0f)
                return;

            if (Vector3.DistanceSquared(start, end) <= 0.0000001f)
            {
                SetSphere(start, radius);
                return;
            }

            if (capsule == null ||
                !AreClose(capsuleStart, start) ||
                !AreClose(capsuleEnd, end) ||
                !AreClose(capsuleRadius, radius))
            {
                capsule?.Dispose();
                capsule = new Capsule(start, end, radius, true);
                capsuleStart = start;
                capsuleEnd = end;
                capsuleRadius = radius;
            }

            shapeType = ShapeType.Capsule;
        }

        public void SetBox(Vector3 position, Vector3 halfExtents)
        {
            if (!IsFinite(position) || !IsFinite(halfExtents))
                return;

            halfExtents = new Vector3(
                Math.Abs(halfExtents.X),
                Math.Abs(halfExtents.Y),
                Math.Abs(halfExtents.Z));

            if (halfExtents.X <= 0f && halfExtents.Y <= 0f && halfExtents.Z <= 0f)
                return;

            //boxFill.SetBounds(-halfExtents, halfExtents, 0f, true);
            boxWireframe.SetBounds(-halfExtents, halfExtents, 0f, true);
            boxPosition = position;
            shapeType = ShapeType.Box;
        }

        public void Draw(Matrix world)
        {
            switch (shapeType)
            {
                case ShapeType.Sphere:
                    Matrix sphereWorld = Matrix.CreateTranslation(spherePosition) * world;
                    sphere.Draw(sphereWorld, Camera.ViewMatrix, Camera.ProjectionMatrix, fillColor);
                    sphere.DrawWireframe(sphereWorld, Camera.ViewMatrix, Camera.ProjectionMatrix, wireColor);
                    break;
                case ShapeType.Capsule:
                    Matrix capsuleWorld = capsule.LocalTransform * world;
                    capsule.Draw(capsuleWorld, Camera.ViewMatrix, Camera.ProjectionMatrix, fillColor);
                    capsule.DrawWireframe(capsuleWorld, Camera.ViewMatrix, Camera.ProjectionMatrix, wireColor);
                    break;
                case ShapeType.Box:
                    //boxFill.SetPosition(boxPosition);
                    boxWireframe.SetPosition(boxPosition);
                    //boxFill.Draw(world);
                    boxWireframe.Draw(world);
                    break;
            }
        }

        public void Dispose()
        {
            sphere?.Dispose();
            sphere = null;
            capsule?.Dispose();
            capsule = null;
            shapeType = ShapeType.None;
        }

        private static bool AreClose(float first, float second)
        {
            return Math.Abs(first - second) <= 0.000001f;
        }

        private static bool AreClose(Vector3 first, Vector3 second)
        {
            return Vector3.DistanceSquared(first, second) <= 0.0000001f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.X) && IsFinite(value.Y) && IsFinite(value.Z);
        }
    }
}
