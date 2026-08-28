using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace XenoKit.Engine.Shapes
{
    public class Sphere : GeometricPrimitive
    {
        public Sphere(float diameter, bool alwaysVisible = false, int tessellation = 16)
        {
            base.alwaysVisible = alwaysVisible;

            if (tessellation < 3)
                throw new ArgumentOutOfRangeException("tessellation");

            int verticalSegments = tessellation;
            int horizontalSegments = tessellation * 2;
            var ringStarts = new List<int>(verticalSegments - 1);

            float radius = diameter / 2;

            // Start with a single vertex at the bottom of the sphere.
            AddVertex(Vector3.Down * radius, Vector3.Down);

            // Create rings of vertices at progressively higher latitudes.
            for (int i = 0; i < verticalSegments - 1; i++)
            {
                float latitude = ((i + 1) * MathHelper.Pi / verticalSegments) - MathHelper.PiOver2;

                float dy = (float)Math.Sin(latitude);
                float dxz = (float)Math.Cos(latitude);

                // Create a single ring of vertices at this latitude.
                ringStarts.Add(CurrentVertex);
                for (int j = 0; j < horizontalSegments; j++)
                {
                    float longitude = j * MathHelper.TwoPi / horizontalSegments;

                    float dx = (float)Math.Cos(longitude) * dxz;
                    float dz = (float)Math.Sin(longitude) * dxz;

                    Vector3 normal = new Vector3(dx, dy, dz);

                    AddVertex(normal * radius, normal);
                }
            }

            // Finish with a single vertex at the top of the sphere.
            int topPole = CurrentVertex;
            AddVertex(Vector3.Up * radius, Vector3.Up);

            // Create a fan connecting the bottom vertex to the bottom latitude ring.
            for (int i = 0; i < horizontalSegments; i++)
            {
                AddIndex(0);
                AddIndex(ringStarts[0] + (i + 1) % horizontalSegments);
                AddIndex(ringStarts[0] + i);
            }

            // Fill the sphere body with triangles joining each pair of latitude rings.
            for (int i = 0; i < ringStarts.Count - 1; i++)
            {
                for (int j = 0; j < horizontalSegments; j++)
                {
                    int nextJ = (j + 1) % horizontalSegments;
                    int lowerRing = ringStarts[i];
                    int upperRing = ringStarts[i + 1];

                    AddIndex(lowerRing + j);
                    AddIndex(lowerRing + nextJ);
                    AddIndex(upperRing + j);

                    AddIndex(lowerRing + nextJ);
                    AddIndex(upperRing + nextJ);
                    AddIndex(upperRing + j);
                }
            }

            // Create a fan connecting the top vertex to the top latitude ring.
            for (int i = 0; i < horizontalSegments; i++)
            {
                AddIndex(topPole);
                AddIndex(topPole - 1 - (i + 1) % horizontalSegments);
                AddIndex(topPole - 1 - i);
            }

            foreach (int ringStart in ringStarts)
            {
                for (int i = 0; i < horizontalSegments; i++)
                    AddWireframeLine(ringStart + i, ringStart + (i + 1) % horizontalSegments);
            }

            for (int i = 0; i < horizontalSegments; i++)
            {
                AddWireframeLine(0, ringStarts[0] + i);

                for (int ring = 0; ring < ringStarts.Count - 1; ring++)
                    AddWireframeLine(ringStarts[ring] + i, ringStarts[ring + 1] + i);

                AddWireframeLine(ringStarts[ringStarts.Count - 1] + i, topPole);
            }

            InitializePrimitive(GraphicsDevice);
        }
    
        
    }
}
