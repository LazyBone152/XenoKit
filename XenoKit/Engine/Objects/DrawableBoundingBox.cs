using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XenoKit.Engine.Objects
{
	public class BoundingBoxBuffers
	{
		public VertexBuffer Vertices;
		public int VertexCount;
		public IndexBuffer Indices;
		public int PrimitiveCount;
	}

	public static class DrawableBoundingBox
    {
		public static BoundingBoxBuffers CreateBoundingBoxBuffers(BoundingBox boundingBox, GraphicsDevice graphicsDevice)
		{
			BoundingBoxBuffers boundingBoxBuffers = new BoundingBoxBuffers();

			boundingBoxBuffers.PrimitiveCount = 24;
			boundingBoxBuffers.VertexCount = 48;

			VertexBuffer vertexBuffer = new VertexBuffer(graphicsDevice,
				typeof(VertexPositionColor), boundingBoxBuffers.VertexCount,
				BufferUsage.WriteOnly);
			List<VertexPositionColor> vertices = new List<VertexPositionColor>();

			const float ratio = 5.0f;

			Vector3 xOffset = new Vector3((boundingBox.Max.X - boundingBox.Min.X) / ratio, 0, 0);
			Vector3 yOffset = new Vector3(0, (boundingBox.Max.Y - boundingBox.Min.Y) / ratio, 0);
			Vector3 zOffset = new Vector3(0, 0, (boundingBox.Max.Z - boundingBox.Min.Z) / ratio);
			Vector3[] corners = boundingBox.GetCorners();

			// Corner 1.
			AddVertex(vertices, corners[0]);
			AddVertex(vertices, corners[0] + xOffset);
			AddVertex(vertices, corners[0]);
			AddVertex(vertices, corners[0] - yOffset);
			AddVertex(vertices, corners[0]);
			AddVertex(vertices, corners[0] - zOffset);

			// Corner 2.
			AddVertex(vertices, corners[1]);
			AddVertex(vertices, corners[1] - xOffset);
			AddVertex(vertices, corners[1]);
			AddVertex(vertices, corners[1] - yOffset);
			AddVertex(vertices, corners[1]);
			AddVertex(vertices, corners[1] - zOffset);

			// Corner 3.
			AddVertex(vertices, corners[2]);
			AddVertex(vertices, corners[2] - xOffset);
			AddVertex(vertices, corners[2]);
			AddVertex(vertices, corners[2] + yOffset);
			AddVertex(vertices, corners[2]);
			AddVertex(vertices, corners[2] - zOffset);

			// Corner 4.
			AddVertex(vertices, corners[3]);
			AddVertex(vertices, corners[3] + xOffset);
			AddVertex(vertices, corners[3]);
			AddVertex(vertices, corners[3] + yOffset);
			AddVertex(vertices, corners[3]);
			AddVertex(vertices, corners[3] - zOffset);

			// Corner 5.
			AddVertex(vertices, corners[4]);
			AddVertex(vertices, corners[4] + xOffset);
			AddVertex(vertices, corners[4]);
			AddVertex(vertices, corners[4] - yOffset);
			AddVertex(vertices, corners[4]);
			AddVertex(vertices, corners[4] + zOffset);

			// Corner 6.
			AddVertex(vertices, corners[5]);
			AddVertex(vertices, corners[5] - xOffset);
			AddVertex(vertices, corners[5]);
			AddVertex(vertices, corners[5] - yOffset);
			AddVertex(vertices, corners[5]);
			AddVertex(vertices, corners[5] + zOffset);

			// Corner 7.
			AddVertex(vertices, corners[6]);
			AddVertex(vertices, corners[6] - xOffset);
			AddVertex(vertices, corners[6]);
			AddVertex(vertices, corners[6] + yOffset);
			AddVertex(vertices, corners[6]);
			AddVertex(vertices, corners[6] + zOffset);

			// Corner 8.
			AddVertex(vertices, corners[7]);
			AddVertex(vertices, corners[7] + xOffset);
			AddVertex(vertices, corners[7]);
			AddVertex(vertices, corners[7] + yOffset);
			AddVertex(vertices, corners[7]);
			AddVertex(vertices, corners[7] + zOffset);

			vertexBuffer.SetData(vertices.ToArray());
			boundingBoxBuffers.Vertices = vertexBuffer;

			IndexBuffer indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, boundingBoxBuffers.VertexCount,
				BufferUsage.WriteOnly);
			indexBuffer.SetData(Enumerable.Range(0, boundingBoxBuffers.VertexCount).Select(i => (short)i).ToArray());
			boundingBoxBuffers.Indices = indexBuffer;

			return boundingBoxBuffers;
		}

		private static void AddVertex(List<VertexPositionColor> vertices, Vector3 position)
		{
			vertices.Add(new VertexPositionColor(position, Color.White));
		}

		public static void DrawBoundingBox(BoundingBoxBuffers buffers, BasicEffect effect, GraphicsDevice graphicsDevice, Matrix view, Matrix projection)
		{
			graphicsDevice.SetVertexBuffer(buffers.Vertices);
			graphicsDevice.Indices = buffers.Indices;

			effect.World = Matrix.Identity;
			effect.View = view;
			effect.Projection = projection;

			foreach (EffectPass pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				graphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, buffers.VertexCount, 0, buffers.PrimitiveCount);
			}
		}



	}
}
