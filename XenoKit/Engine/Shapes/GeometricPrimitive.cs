#region File Description
//-----------------------------------------------------------------------------
// GeometricPrimitive.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.Vertex;
#endregion

namespace XenoKit.Engine.Shapes
{
    public abstract class GeometricPrimitive : EngineObject, IDisposable
    {
        #region Fields

        protected bool alwaysVisible = false;

        List<VertexPositionNormal> vertices = new List<VertexPositionNormal>();
        List<ushort> indices = new List<ushort>();
        List<ushort> wireframeIndices = new List<ushort>();

        VertexBuffer vertexBuffer;
        IndexBuffer indexBuffer;
        IndexBuffer wireframeIndexBuffer;
        int wireframePrimitiveCount;
        BasicEffect basicEffect;
        RasterizerState wireframeRasterizerState;


        #endregion

        #region Initialization
        public GeometricPrimitive()
        {
        }

        protected void AddVertex(Vector3 position, Vector3 normal)
        {
            vertices.Add(new VertexPositionNormal(position, normal));
        }

        protected void AddIndex(int index)
        {
            if (index > ushort.MaxValue)
                throw new ArgumentOutOfRangeException("index");

            indices.Add((ushort)index);
        }

        protected void AddWireframeLine(int startIndex, int endIndex)
        {
            if (startIndex > ushort.MaxValue)
                throw new ArgumentOutOfRangeException("startIndex");

            if (endIndex > ushort.MaxValue)
                throw new ArgumentOutOfRangeException("endIndex");

            wireframeIndices.Add((ushort)startIndex);
            wireframeIndices.Add((ushort)endIndex);
        }

        protected int CurrentVertex
        {
            get { return vertices.Count; }
        }

        protected void InitializePrimitive(GraphicsDevice graphicsDevice)
        {
            // Create a vertex declaration, describing the format of our vertex data.

            // Create a vertex buffer, and copy our vertex data into it.
            vertexBuffer = new VertexBuffer(graphicsDevice,
                                            typeof(VertexPositionNormal),
                                            vertices.Count, BufferUsage.None);

            vertexBuffer.SetData(vertices.ToArray());

            // Create an index buffer, and copy our index data into it.
            indexBuffer = new IndexBuffer(graphicsDevice, typeof(ushort),
                                          indices.Count, BufferUsage.None);

            indexBuffer.SetData(indices.ToArray());

            if (wireframeIndices.Count > 0)
            {
                wireframeIndexBuffer = new IndexBuffer(graphicsDevice, typeof(ushort),
                                                        wireframeIndices.Count, BufferUsage.None);
                wireframeIndexBuffer.SetData(wireframeIndices.ToArray());
                wireframePrimitiveCount = wireframeIndices.Count / 2;
                wireframeIndices = null;
                wireframeRasterizerState = new RasterizerState()
                {
                    FillMode = FillMode.Solid,
                    CullMode = CullMode.None
                };
            }

            // Create a BasicEffect, which will be used to render the primitive.
            basicEffect = new BasicEffect(graphicsDevice);

            basicEffect.EnableDefaultLighting();
            basicEffect.PreferPerPixelLighting = false;
        }


        /// <summary>
        /// Finalizer.
        /// </summary>
        ~GeometricPrimitive()
        {
            Dispose(false);
        }


        /// <summary>
        /// Frees resources used by this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Frees resources used by this object.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (vertexBuffer != null)
                    vertexBuffer.Dispose();

                if (indexBuffer != null)
                    indexBuffer.Dispose();

                if (wireframeIndexBuffer != null)
                    wireframeIndexBuffer.Dispose();

                if (basicEffect != null)
                    basicEffect.Dispose();

                if (wireframeRasterizerState != null)
                    wireframeRasterizerState.Dispose();
            }
        }


        #endregion

        #region Draw


        /// <summary>
        /// Draws the primitive model, using the specified effect. Unlike the other
        /// Draw overload where you just specify the world/view/projection matrices
        /// and color, this method does not set any renderstates, so you must make
        /// sure all states are set to sensible values before you call it.
        /// </summary>
        public void Draw(Effect effect)
        {
            Draw(effect, indexBuffer, PrimitiveType.TriangleList, indices.Count / 3);
        }

        private void Draw(Effect effect, IndexBuffer buffer, PrimitiveType primitiveType, int primitiveCount)
        {
            if (buffer == null || primitiveCount <= 0)
                return;

            GraphicsDevice.SetVertexBuffer(vertexBuffer);
            GraphicsDevice.Indices = buffer;

            foreach (EffectPass effectPass in effect.CurrentTechnique.Passes)
            {
                effectPass.Apply();
                GraphicsDevice.DrawIndexedPrimitives(primitiveType, 0, 0, primitiveCount);
            }
        }


        /// <summary>
        /// Draws the primitive model, using a BasicEffect shader with default
        /// lighting. Unlike the other Draw overload where you specify a custom
        /// effect, this method sets important renderstates to sensible values
        /// for 3D model rendering, so you do not need to set these states before
        /// you call it.
        /// </summary>
        public void Draw(Matrix world, Matrix view, Matrix projection, Color color)
        {
            // Set BasicEffect parameters.
            basicEffect.World = world;
            basicEffect.View = view;
            basicEffect.Projection = projection;
            basicEffect.DiffuseColor = color.ToVector3();
            basicEffect.Alpha = color.A / 255.0f;

            GraphicsDevice device = basicEffect.GraphicsDevice;
            device.DepthStencilState = (alwaysVisible) ? DepthStencilState.None : DepthStencilState.Default;

            if (color.A < 255)
            {
                // Set renderstates for alpha blended rendering.
                device.BlendState = BlendState.AlphaBlend;
            }
            else
            {
                // Set renderstates for opaque rendering.
                device.BlendState = BlendState.Opaque;
            }

            // Draw the model, using BasicEffect.
            Draw(basicEffect);
        }

        public void DrawWireframe(Matrix world, Matrix view, Matrix projection, Color color)
        {
            if (wireframeIndexBuffer == null || wireframePrimitiveCount <= 0)
                return;

            basicEffect.World = world;
            basicEffect.View = view;
            basicEffect.Projection = projection;
            basicEffect.DiffuseColor = color.ToVector3();
            basicEffect.Alpha = color.A / 255.0f;

            GraphicsDevice device = basicEffect.GraphicsDevice;
            device.DepthStencilState = (alwaysVisible) ? DepthStencilState.None : DepthStencilState.Default;
            device.BlendState = color.A < 255 ? BlendState.AlphaBlend : BlendState.Opaque;

            RasterizerState previousRasterizerState = device.RasterizerState;
            bool previousLightingEnabled = basicEffect.LightingEnabled;
            device.RasterizerState = wireframeRasterizerState;
            basicEffect.LightingEnabled = false;

            try
            {
                Draw(basicEffect, wireframeIndexBuffer, PrimitiveType.LineList, wireframePrimitiveCount);
            }
            finally
            {
                basicEffect.LightingEnabled = previousLightingEnabled;
                device.RasterizerState = previousRasterizerState;
            }
        }


        #endregion
    }
}
