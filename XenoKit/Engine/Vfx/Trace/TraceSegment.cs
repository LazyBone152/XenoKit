using Microsoft.Xna.Framework;
using System.Collections.Generic;
using XenoKit.Engine.Pool;
using Xv2CoreLib.ETR;

namespace XenoKit.Engine.Vfx.Trace
{
    public class TraceSegment : PooledEntity
    {
        protected ETR_File EtrFile;
        protected ETR_Node EtrNode;
        protected TraceNode Node;
        protected VfxTrace VfxTrace;

        private readonly List<TracePlane> Planes = new List<TracePlane>();

        public bool IsExtruding { get; set; }

        public Matrix originPoint;
        public Matrix endPoint;

        //Animatied vertex colors when using "Start To End" interpolation type
        public float[] color1AtSegmentCreation;
        public float[] color1AtSegmentHold;
        public float[] color2AtSegmentCreation;
        public float[] color2AtSegmentHold;

        public void Initialize(ETR_File etrFile, ETR_Node etrNode, TraceNode node, VfxTrace vfxTrace)
        {
            EtrFile = etrFile;
            EtrNode = etrNode;
            Node = node;
            VfxTrace = vfxTrace;

            originPoint = VfxTrace.Actor.GetAbsoluteBoneMatrix(Node.BoneIndex1);
            endPoint = originPoint * Matrix.CreateTranslation(new Vector3(0, 0, EtrNode.PositionExtrudeZ));

            TracePlane plane = ObjectPoolManager.GetTracePlane(etrFile, EtrNode, Node, VfxTrace, this);
            Planes.Add(plane);
            RenderSystem.AddRenderEntity(plane);
        }

        public override void Update()
        {
            if (IsExtruding)
            {
                endPoint = originPoint * Matrix.CreateTranslation(new Vector3(0, 0, EtrNode.PositionExtrudeZ));
            }

            foreach(TracePlane plane in Planes)
            {
                plane.Update();
            }
        }

        public override void ClearObjectState()
        {
            IsExtruding = false;
            EtrFile = null;
            EtrNode = null;
            Node = null;
            originPoint = default;
            endPoint = default;
            color1AtSegmentCreation = default;
            color2AtSegmentCreation = default;
            color1AtSegmentHold = default;
            color2AtSegmentHold = default;
        }

        public void Release()
        {
            foreach (TracePlane plane in Planes)
            {
                plane.Release();
            }

            Planes.Clear();

            ObjectPoolManager.TraceSegmentPool.ReleaseObject(this);
        }
    }
}
