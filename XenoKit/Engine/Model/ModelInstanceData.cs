using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Windows.Documents;
using XenoKit.Engine.Rendering;
using Xv2CoreLib.FMP;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdQuaternion = System.Numerics.Quaternion;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Model
{
    public class ModelInstanceTree : IDisposable
    {
        private FMP_InstanceData fmpInstanceData;
        public ModelInstanceData[] InstanceGroups;

        public ModelInstanceTree(FMP_InstanceData fmpInstanceData)
        {
            this.fmpInstanceData = fmpInstanceData;
            CreateInstanceGroups();
        }

        private void CreateInstanceGroups()
        {
            Dispose();
            InstanceGroups = new ModelInstanceData[fmpInstanceData.InstanceGroups.Count];

            for(int i = 0; i < InstanceGroups.Length; i++)
            {
                InstanceGroups[i] = new ModelInstanceData(fmpInstanceData.InstanceGroups[i]);
            }
        }

        public void Dispose()
        {
            if(InstanceGroups?.Length > 0)
            {
                foreach(var group in InstanceGroups)
                {
                    group.Dispose();
                }

                InstanceGroups = null;
            }
        }
    }

    public class ModelInstanceData : IDisposable
    {
        private static ModelInstanceData _defaultData = null;
        public static ModelInstanceData DefaultInstanceData => _defaultData;

        private VertexBuffer instanceBuffer;
        private Vector4[] instanceData;
        private Matrix4x4[] transforms;

        private BoundingBox BoundingBox;
        private Vector3 Center;
        private float MaxDistance;

        public VertexBufferBinding InstanceBufferBinding { get; private set; }
        public int NumInstances => transforms.Length;
        

        private FMP_InstanceGroup fmpInstanceGroup;

        private ModelInstanceData()
        {
            transforms = new Matrix4x4[1] { Matrix4x4.Identity };
            CreateInstanceBuffer();
        }

        public ModelInstanceData(FMP_InstanceGroup _instanceGroup)
        {
            fmpInstanceGroup = _instanceGroup;
            InitFmp();
        }

        public static void InitDefaultInstanceData()
        {
            _defaultData = new ModelInstanceData();
        }

        private void InitFmp()
        {
            var fmpTransforms = fmpInstanceGroup.Transforms.ToArray();
            transforms = new Matrix4x4[fmpTransforms.Length];

            for(int i = 0; i < fmpTransforms.Length; i++)
            {
                transforms[i] = fmpTransforms[i].ToMatrix();
            }

            CreateInstanceBuffer();

            BoundingBox = new BoundingBox(new Vector3(fmpInstanceGroup.MinX, fmpInstanceGroup.MinY, fmpInstanceGroup.MinZ), new Vector3(fmpInstanceGroup.MaxX, fmpInstanceGroup.MaxY, fmpInstanceGroup.MaxZ));
            Center = new Vector3(fmpInstanceGroup.CenterX, fmpInstanceGroup.CenterY, fmpInstanceGroup.CenterZ);
            MaxDistance = fmpInstanceGroup.MaxDistanceFromCenter;
        }

        private void CreateInstanceBuffer()
        {
            instanceBuffer = new VertexBuffer(Viewport.Instance.GraphicsDevice,
                                                new VertexDeclaration(
                                                    new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.InstanceData, 0),
                                                    new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.InstanceData, 1),
                                                    new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.InstanceData, 2),
                                                    new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.InstanceData, 3)
                                                ), transforms.Length, BufferUsage.WriteOnly);

            instanceData = new Vector4[transforms.Length * 4];
            for (int i = 0; i < transforms.Length; i++)
            {
                instanceData[i * 4 + 0] = new Vector4(transforms[i].M11, transforms[i].M12, transforms[i].M13, transforms[i].M14);
                instanceData[i * 4 + 1] = new Vector4(transforms[i].M21, transforms[i].M22, transforms[i].M23, transforms[i].M24);
                instanceData[i * 4 + 2] = new Vector4(transforms[i].M31, transforms[i].M32, transforms[i].M33, transforms[i].M34);
                instanceData[i * 4 + 3] = new Vector4(transforms[i].M41, transforms[i].M42, transforms[i].M43, transforms[i].M44);
            }

            instanceBuffer.SetData(instanceData);
            InstanceBufferBinding = new VertexBufferBinding(instanceBuffer, 0, 1);
        }
    
        public void Dispose()
        {
            if(instanceBuffer != null && instanceBuffer.IsDisposed == false)
            {
                instanceBuffer.Dispose();
            }
        }
    
        public bool FrustumIntersects()
        {
            //return true;
#if DEBUG
            if (!SceneManager.FrustumCullEnabled) return true;
#endif
            if (Vector3.Distance(Center, Viewport.Instance.Camera.CameraState.Position) < MaxDistance) return true;

            BoundingFrustum frustum = Viewport.Instance.RenderSystem.IsShadowPass ? Viewport.Instance.StageLight.LightFrustum : Viewport.Instance.Camera.Frustum;

            return frustum.Intersects(BoundingBox);
        }
    }
}
