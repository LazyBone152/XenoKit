using Microsoft.Xna.Framework;
using SharpDX.MediaFoundation;
using XenoKit.Engine.Vfx.Asset;
using XenoKit.Engine.Vfx.Particle;
using XenoKit.Engine.Vfx.Trace;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.ETR;

namespace XenoKit.Engine.Pool
{
    public class ObjectPoolManager : Entity
    {
        public readonly PoolInstance<ParticleNodeBase> ParticleNodeBasePool;
        public readonly PoolInstance<Vfx.Particle.ParticleEmitter> ParticleEmitterPool;
        public readonly PoolInstance<ParticlePlane> ParticlePlanePool;
        public readonly PoolInstance<ParticleMesh> ParticleMeshPool;

        public readonly PoolInstance<TraceNode> TraceNodePool;
        public readonly PoolInstance<TraceSegment> TraceSegmentPool;
        public readonly PoolInstance<TracePlane> TracePlanePool;

        public ObjectPoolManager(GameBase game) : base(game)
        {
            //Pool size for base node can be reduced when ShapeDraw, Cone Extrude and Mesh are added, as only Null will use the pool at that point
            ParticleNodeBasePool = new PoolInstance<ParticleNodeBase>(1000, game);
            ParticleEmitterPool = new PoolInstance<Vfx.Particle.ParticleEmitter>(500, game);
            ParticlePlanePool = new PoolInstance<ParticlePlane>(5000, game);
            ParticleMeshPool = new PoolInstance<ParticleMesh>(500, game);

            TraceNodePool = new PoolInstance<TraceNode>(100, game);
            TraceSegmentPool = new PoolInstance<TraceSegment>(500, game);
            TracePlanePool = new PoolInstance<TracePlane>(1500, game);
        }

        public override void DelayedUpdate()
        {
            ParticleNodeBasePool.DelayedUpdate();
            ParticleEmitterPool.DelayedUpdate();
            ParticlePlanePool.DelayedUpdate();
            TraceNodePool.DelayedUpdate();
            TraceSegmentPool.DelayedUpdate();
            TracePlanePool.DelayedUpdate();
        }


        #region Particle Methods

        public ParticleNodeBase GetParticleNodeBase(Matrix emitPoint, Vector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            ParticleNodeBase newNode = GameBase.ObjectPoolManager.ParticleNodeBasePool.GetObject();
            newNode.Initialize(emitPoint, velocity, system, node, effectPart, effect);
            newNode.Reclaim();
            return newNode;
        }

        public Vfx.Particle.ParticleEmitter GetParticleEmitter(Matrix emitPoint, Vector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            Vfx.Particle.ParticleEmitter newNode = GameBase.ObjectPoolManager.ParticleEmitterPool.GetObject();
            newNode.Initialize(emitPoint, velocity, system, node, effectPart, effect);
            newNode.Reclaim();
            return newNode;
        }

        public ParticlePlane GetParticlePlane(Matrix emitPoint, Vector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            ParticlePlane newNode = GameBase.ObjectPoolManager.ParticlePlanePool.GetObject();
            newNode.Initialize(emitPoint, velocity, system, node, effectPart, effect);
            newNode.Reclaim();
            return newNode;
        }

        public ParticleMesh GetParticleMesh(Matrix emitPoint, Vector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            ParticleMesh newNode = GameBase.ObjectPoolManager.ParticleMeshPool.GetObject();
            newNode.Initialize(emitPoint, velocity, system, node, effectPart, effect);
            newNode.Reclaim();
            return newNode;
        }
        #endregion

        #region Trace Methods
        public TraceNode GetTraceNode(ETR_File etrFile, ETR_Node node, VfxTrace vfxTrace)
        {
            TraceNode newNode = GameBase.ObjectPoolManager.TraceNodePool.GetObject();
            newNode.Initialize(etrFile, node, vfxTrace);
            newNode.Reclaim();
            return newNode;
        }

        public TraceSegment GetTraceSegment(ETR_File etrFile, ETR_Node etrNode, TraceNode node, VfxTrace vfxTrace)
        {
            TraceSegment newSegment = GameBase.ObjectPoolManager.TraceSegmentPool.GetObject();
            newSegment.Initialize(etrFile, etrNode, node, vfxTrace);
            newSegment.Reclaim();
            return newSegment;
        }


        public TracePlane GetTracePlane(ETR_File etrFile, ETR_Node etrNode, TraceNode node, VfxTrace vfxTrace, TraceSegment segment)
        {
            TracePlane newPlane = GameBase.ObjectPoolManager.TracePlanePool.GetObject();
            newPlane.Initialize(vfxTrace, node, etrFile, etrNode, segment);
            newPlane.Reclaim();
            return newPlane;
        }
        #endregion
    }
}