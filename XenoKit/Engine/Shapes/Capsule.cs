using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace XenoKit.Engine.Shapes
{
    public class Capsule : GeometricPrimitive
    {
        public Matrix LocalTransform { get; private set; }

        public Capsule(Vector3 start, Vector3 end, float radius, bool alwaysVisible = false, int radialSegments = 12, int hemisphereSegments = 3)
        {
            if (radialSegments < 3)
                throw new ArgumentOutOfRangeException("radialSegments");

            if (hemisphereSegments < 1)
                throw new ArgumentOutOfRangeException("hemisphereSegments");

            base.alwaysVisible = alwaysVisible;

            Vector3 direction = end - start;
            float length = direction.Length();
            float halfLength = length / 2f;
            float step = MathHelper.PiOver2 / hemisphereSegments;
            var rings = new List<int>(hemisphereSegments * 2);

            AddVertex(new Vector3(0f, -halfLength - radius, 0f), Vector3.Down);

            for (int i = 1; i <= hemisphereSegments; i++)
            {
                float latitude = -MathHelper.PiOver2 + i * step;
                float normalY = (float)Math.Sin(latitude);
                float normalRadius = (float)Math.Cos(latitude);
                rings.Add(AddRing(-halfLength + normalY * radius, radius * normalRadius, normalRadius, normalY, radialSegments));
            }

            rings.Add(AddRing(halfLength, radius, 1f, 0f, radialSegments));

            for (int i = 1; i < hemisphereSegments; i++)
            {
                float latitude = i * step;
                float normalY = (float)Math.Sin(latitude);
                float normalRadius = (float)Math.Cos(latitude);
                rings.Add(AddRing(halfLength + normalY * radius, radius * normalRadius, normalRadius, normalY, radialSegments));
            }

            for (int i = 0; i < radialSegments; i++)
            {
                AddIndex(0);
                AddIndex(rings[0] + (i + 1) % radialSegments);
                AddIndex(rings[0] + i);
            }

            for (int i = 0; i < rings.Count - 1; i++)
            {
                ConnectRings(rings[i], rings[i + 1], radialSegments);
            }

            int topPole = CurrentVertex;
            AddVertex(new Vector3(0f, halfLength + radius, 0f), Vector3.Up);

            for (int i = 0; i < radialSegments; i++)
            {
                AddIndex(topPole);
                AddIndex(rings[rings.Count - 1] + (radialSegments - 2 - i + radialSegments) % radialSegments);
                AddIndex(rings[rings.Count - 1] + (radialSegments - 1 - i + radialSegments) % radialSegments);
            }

            foreach (int ring in rings)
            {
                for (int i = 0; i < radialSegments; i++)
                    AddWireframeLine(ring + i, ring + (i + 1) % radialSegments);
            }

            for (int i = 0; i < radialSegments; i++)
            {
                AddWireframeLine(0, rings[0] + i);

                for (int ring = 0; ring < rings.Count - 1; ring++)
                    AddWireframeLine(rings[ring] + i, rings[ring + 1] + i);

                AddWireframeLine(rings[rings.Count - 1] + i, topPole);
            }

            InitializePrimitive(GraphicsDevice);
            LocalTransform = CreateLocalTransform(start, end, length);
        }

        private int AddRing(float y, float ringRadius, float normalRadius, float normalY, int radialSegments)
        {
            int ringStart = CurrentVertex;

            for (int i = 0; i < radialSegments; i++)
            {
                float longitude = i * MathHelper.TwoPi / radialSegments;
                float x = (float)Math.Cos(longitude);
                float z = (float)Math.Sin(longitude);
                AddVertex(
                    new Vector3(x * ringRadius, y, z * ringRadius),
                    new Vector3(x * normalRadius, normalY, z * normalRadius));
            }

            return ringStart;
        }

        private void ConnectRings(int lowerRing, int upperRing, int radialSegments)
        {
            for (int i = 0; i < radialSegments; i++)
            {
                int next = (i + 1) % radialSegments;

                AddIndex(lowerRing + i);
                AddIndex(lowerRing + next);
                AddIndex(upperRing + i);

                AddIndex(lowerRing + next);
                AddIndex(upperRing + next);
                AddIndex(upperRing + i);
            }
        }

        private static Matrix CreateLocalTransform(Vector3 start, Vector3 end, float length)
        {
            Vector3 midpoint = (start + end) / 2f;
            if (length <= 0.0001f)
                return Matrix.CreateTranslation(midpoint);

            Vector3 direction = (end - start) / length;
            float dot = MathHelper.Clamp(Vector3.Dot(Vector3.Up, direction), -1f, 1f);
            Quaternion rotation;

            if (dot > 0.9999f)
            {
                rotation = Quaternion.Identity;
            }
            else if (dot < -0.9999f)
            {
                rotation = Quaternion.CreateFromAxisAngle(Vector3.Right, MathHelper.Pi);
            }
            else
            {
                Vector3 axis = Vector3.Cross(Vector3.Up, direction);
                axis.Normalize();
                rotation = Quaternion.CreateFromAxisAngle(axis, (float)Math.Acos(dot));
            }

            return Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(midpoint);
        }
    }
}
