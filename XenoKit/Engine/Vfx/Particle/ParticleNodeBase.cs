using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Pool;
using XenoKit.Engine.Vfx.Asset;
using XenoKit.Helper;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.Resource;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticleNodeBase : PooledEntity
    {
        public WeakReference Effect;
        public override bool IsAlive => Effect.IsAlive;
        public override EntityType EntityType => EntityType.VFX;

        public ParticleSystem ParticleSystem { get; private set; }
        public ParticleNode Node;
        private EffectPart EffectPart;

        public int StartTime;
        public int Lifetime;
        public int Burst;
        public int BurstFrequency;
        public bool Loop;

        private float PositionX_Variance;
        private float PositionY_Variance;
        private float PositionZ_Variance;
        private float RotationX_Variance;
        private float RotationY_Variance;
        private float RotationZ_Variance;

        protected float StartRotation_Variance = 0f;
        protected float ActiveRotation_Variance = 0f;
        protected float RotationAmount = 0f;
        public Vector3 Velocity;

        public Matrix Rotation = Matrix.Identity;
        /// <summary>
        /// Transform encasing all movement of this node (velocity).
        /// </summary>
        public Matrix MovementTransform = Matrix.Identity;
        //Transform = relative to ParticleSystem
        /// <summary>
        /// Where the node was created. Used for calculating current transform.
        /// </summary>
        protected Matrix EmitPointTransform;
        /// <summary>
        /// A snapshot of the attachment bone at the time this node was created. Used for calculating AbsoluteTransform, based on EEPK flags.
        /// </summary>
        private Matrix EmitBoneTransform;
        protected Matrix VelocityOrientedAdjustment = Matrix.Identity;

        public NodeState State { get; protected set; }
        public float CurrentFrame { get; private set; }
        public float CurrentTimeFactor { get; private set; }

        //Children Nodes
        public List<ParticleNodeBase> Nodes = new List<ParticleNodeBase>();
        public List<ParticleNodeBase> PreviousNodes = new List<ParticleNodeBase>();

        //Active child node instances
        protected List<int> ActiveInstances = new List<int>();
        protected bool ActiveInstancesUpdatedThisFrame = false;
        protected virtual bool IsRootNode => false;

        #region Initialize
        public ParticleNodeBase()
        {
            Effect = new WeakReference(null);
        }

        public ParticleNodeBase(Matrix emitPoint, ParticleSystem system, EffectPart effectPart, GameBase game)
        {
            SetGameBaseInstance(game);
            EmitPointTransform = emitPoint;
            ParticleSystem = system;
            EffectPart = effectPart;
            Node = new ParticleNode();
            Node.Burst = 1;
        }

        public virtual void Initialize(Matrix emitPoint, Vector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            Effect.Target = effect;
            Velocity = velocity;
            ParticleSystem = system;
            Node = node;
            EffectPart = effectPart;
            EmitPointTransform = emitPoint;
            EmitBoneTransform = system.AttachmentBone;

            StartTime = node.StartTime + Xv2CoreLib.Random.Range(0, node.StartTime_Variance);
            Lifetime = node.Lifetime + Xv2CoreLib.Random.Range(0, node.Lifetime_Variance);
            Burst = node.Burst + Xv2CoreLib.Random.Range(0, node.Burst_Variance);
            BurstFrequency = node.BurstFrequency + Xv2CoreLib.Random.Range(0, node.BurstFrequency_Variance);
            Loop = node.NodeFlags.HasFlag(NodeFlags1.Loop);

            PositionX_Variance = Xv2CoreLib.Random.Range(0, node.Position_Variance.X);
            PositionY_Variance = Xv2CoreLib.Random.Range(0, node.Position_Variance.Y);
            PositionZ_Variance = Xv2CoreLib.Random.Range(0, node.Position_Variance.Z);
            RotationX_Variance = Xv2CoreLib.Random.Range(0, node.Rotation_Variance.X);
            RotationY_Variance = Xv2CoreLib.Random.Range(0, node.Rotation_Variance.Y);
            RotationZ_Variance = Xv2CoreLib.Random.Range(0, node.Rotation_Variance.Z);
        }

        public override void ClearObjectState()
        {
            ParticleSystem = null;
            EffectPart = null;
            State = NodeState.Created;
            CurrentFrame = 0f;
            StartTime = 0;
            Lifetime = 0;
            Burst = 0;
            BurstFrequency = 0;
            Loop = false;
            PositionX_Variance = 0f;
            PositionY_Variance = 0f;
            PositionZ_Variance = 0f;
            RotationX_Variance = 0f;
            RotationY_Variance = 0f;
            RotationZ_Variance = 0f;

            Nodes.Clear();
            ActiveInstances.Clear();
            MovementTransform = Matrix.Identity;
            VelocityOrientedAdjustment = Matrix.Identity;
            Velocity = Vector3.Zero;
        }

        public virtual void Release()
        {
            ObjectPoolManager.ParticleNodeBasePool.ReleaseObject(this);
        }

        public void ReleaseAll()
        {
            lock (Nodes)
            {
                for (int i = 0; i < PreviousNodes.Count; i++)
                {
                    PreviousNodes[i].ReleaseAll();
                }

                PreviousNodes.Clear();

                for (int i = 0; i < Nodes.Count; i++)
                {
                    Nodes[i].ReleaseAll();
                }

                Nodes.Clear();
            }

            Release();
        }
        #endregion

        #region Update
        public override void Update()
        {
            StartUpdate();
            UpdateChildrenNodes();
            EndUpdate();
        }

        /// <summary>
        /// Start of the particle update loop. This should be called right at the start of the main Update() method.
        /// </summary>
        public void StartUpdate()
        {
            //Handle Start Time
            if (CurrentFrame >= StartTime && State == NodeState.Created)
            {
                State = NodeState.Active;
                CurrentFrame = 0f;
                NullEmitter();
            }

            //Handle DeactivationMode == LoopCancel.
            //Looped nodes are not valid in this state and are ended immediately, but should still follow the normal node destroy rules
            if (ParticleSystem.IsTerminating && Loop)
            {
                State = NodeState.WaitingOnChildren;
                Loop = false;

                if (Node.NodeType == ParticleNodeType.Emission)
                {
                    Emit();
                }

                return;
            }

            //Main logic loop
            if (State == NodeState.Active)
            {
                if (CurrentFrame >= Lifetime)
                {
                    if (Loop)
                    {
                        CurrentFrame = 0f;
                        NullEmitter();

                        //NOTE: Removed this code for now as this needs more research. Some EMPs seem to not follow how I had this documented, and caused a bug where particles were spawned endlessly
                        //The nodes created on a previous loop don't count as active children of the node (as above, not always true?)
                        //PreviousNodes.AddRange(Nodes);
                        //Nodes.Clear();
                    }
                    else
                    {
                        State = NodeState.WaitingOnChildren;

                        //Emission and Null nodes only emit when they expire
                        if (Node.NodeType == ParticleNodeType.Emission)
                        {
                            Emit();
                        }

                        NullEmitter();

                        return;
                    }
                }

                CurrentTimeFactor = CurrentFrame / Lifetime;

                //Update movement
                UpdateModifiers();

                //Change position based on current velocity
                MovementTransform *= Matrix.CreateTranslation((Velocity / 60f) * ParticleSystem.CurrentFrameDelta);

                //Update position and rotation
                float[] position = Node.Position.GetInterpolatedValue(CurrentTimeFactor);
                float[] rotation = Node.Rotation.GetInterpolatedValue(CurrentTimeFactor);

                Transform = MovementTransform * VelocityOrientedAdjustment * EmitPointTransform;
                //Transform *= Matrix.CreateFromQuaternion(GeneralHelpers.EulerAnglesToQuaternion(new Vector3(MathHelper.ToRadians(rotation[0] + RotationX_Variance), MathHelper.ToRadians(rotation[1] + RotationY_Variance), MathHelper.ToRadians(rotation[2] + RotationZ_Variance))));
                Transform *= Matrix.CreateTranslation(new Vector3(position[0] + PositionX_Variance, position[1] + PositionY_Variance, position[2] + PositionZ_Variance));

                //For now, splitting rotation out from Transform to fix an annoying bug with emissions.
                //Rotation is only relevant for 2 things: 1. Its applied to emitted nodes (if this is an Emitter), rotating them. 2. For BillboardType=None, it rotates the texture in addition to the defined Rotation Axis
                //It doesn't actually rotate the node, and so doesn't change the way position or a modifier is applied.
                Rotation = Matrix.CreateFromQuaternion(GeneralHelpers.EulerAnglesToQuaternion(new Vector3(MathHelper.ToRadians(rotation[0] + RotationX_Variance), MathHelper.ToRadians(rotation[1] + RotationY_Variance), MathHelper.ToRadians(rotation[2] + RotationZ_Variance))));

                /*
                //AbsoluteTransform is used for Draw Order purposes only, so it only needs to be calculated on emission type nodes
                if (Node.IsEmission)
                {
                    AbsoluteTransform = EffectPart.InstantUpdate ? Rotation * Transform * ParticleSystem.AttachmentBone : Rotation * Transform * EmitBoneTransform;
                    AbsoluteTransform *= Matrix.CreateTranslation(CameraBase.TransformRelativeToCamera(AbsoluteTransform.Translation, Node.EmissionNode.Texture.RenderDepth));
                }
                */
            }

        }

        private void UpdateModifiers()
        {
            foreach (EMP_Modifier modifier in Node.Modifiers)
            {
                switch (modifier.EmpType)
                {
                    case EMP_Modifier.EmpModifierType.Acceleration:
                        {
                            var values = modifier.Axis.GetInterpolatedValue(CurrentTimeFactor);
                            Velocity += (new Vector3(values[0] / 60f, values[1] / 60f, values[2] / 60f) * modifier.Factor.GetInterpolatedValue(CurrentTimeFactor)) * ParticleSystem.CurrentFrameDelta;
                        }
                        break;
                }
            }
        }

        protected void UpdateRotation()
        {
            //For some reason, FlashOnGen (whatever that is) disables ActiveRotation

            if (!Node.NodeFlags.HasFlag(NodeFlags1.FlashOnGen))
            {
                //Rotation amount is PER frame, despite what the old tooltip said

                RotationAmount += (Node.EmissionNode.ActiveRotation.GetInterpolatedValue(CurrentTimeFactor) + ActiveRotation_Variance) * ParticleSystem.CurrentFrameDelta;
            }
        }

        protected void UpdateChildrenNodes()
        {
            for (int i = PreviousNodes.Count - 1; i >= 0; i--)
            {
                PreviousNodes[i].Update();

                if (PreviousNodes[i].State == NodeState.Expired)
                {
                    PreviousNodes[i].Release();
                    PreviousNodes.RemoveAt(i);
                }
            }

            for (int i = Nodes.Count - 1; i >= 0; i--)
            {
                Nodes[i].Update();

                if (Nodes[i].State == NodeState.Expired)
                {
                    Nodes[i].Release();
                    Nodes.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// End of particle update loop. Call this at the end of the Update() method.
        /// </summary>
        protected void EndUpdate()
        {
            //Handle particle lifetime
            CurrentFrame += ParticleSystem.CurrentFrameDelta;

            if (State == NodeState.WaitingOnChildren && Nodes.Count == 0 && PreviousNodes.Count == 0)
            {
                State = NodeState.Expired;
            }

            ActiveInstancesUpdatedThisFrame = false;
        }

        /// <summary>
        /// Update the number of active child instances. This method will only actually execute once per frame, any subsequent calls on the same frame will be skipped.
        /// </summary>
        private void UpdateActiveInstancesCount()
        {
            if (!ActiveInstancesUpdatedThisFrame)
            {
                ActiveInstancesUpdatedThisFrame = true;

                for (int i = 0; i < Node.ChildParticleNodes.Count; i++)
                {
                    if (ActiveInstances.Count - 1 < i)
                        ActiveInstances.Add(0);

                    ActiveInstances[i] = 0;

                    for (int a = 0; a < Nodes.Count; a++)
                    {
                        if (Nodes[a].Node == Node.ChildParticleNodes[i])
                        {
                            ActiveInstances[i]++;
                        }
                    }
                    //ActiveInstances[i] = Nodes.Where(x => x.Node == Node.ChildParticleNodes[i]).Count();
                }
            }
        }


        public virtual void Simulate()
        {
            StartUpdate();
            UpdateChildrenNodes();
            EndUpdate();
        }
        #endregion

        /// <summary>
        /// Emit a burst of particles. 
        /// </summary>
        protected virtual void Emit()
        {
            UpdateActiveInstancesCount();

            for (int i = 0; i < Node.ChildParticleNodes.Count; i++)
            {
                for (int b = 0; b < Node.Burst; b++)
                {
                    //Maximum amount of instances of this node reached, so cannot create more.
                    //If MaxInstances is 0, then there is no limit
                    if (Node.ChildParticleNodes[i].MaxInstances > 0 && Node.ChildParticleNodes[i].MaxInstances <= ActiveInstances[i] && !IsRootNode)
                        break;

                    if (IsRootNode && Node.ChildParticleNodes[i].NodeType == ParticleNodeType.Null)
                    {
                        Log.Add("ParticleSystem: Null Node type cannot be at root level. The game will crash!", LogType.Error);
                        return;
                    }

                    //Position where the node is to be emitted. 
                    Vector3 velocity = Vector3.Zero;
                    Matrix emitTransform = GetEmitTransformationMatrix(ref velocity) * Rotation * Transform;

                    //IF node is an emission (NOT an emitter), then any children nodes will inherit its velocity.
                    if (Node.IsEmission)
                        velocity = Velocity;

                    if (Node.ChildParticleNodes[i].NodeType == ParticleNodeType.Emitter)
                    {
                        Nodes.Add(ObjectPoolManager.GetParticleEmitter(emitTransform, velocity, ParticleSystem, Node.ChildParticleNodes[i], EffectPart, ParticleSystem.Effect.Target));

                    }
                    else if (Node.ChildParticleNodes[i].NodeType == ParticleNodeType.Emission)
                    {
                        ParticleNodeBase newNode;

                        if (Node.ChildParticleNodes[i].EmissionNode.EmissionType == ParticleEmission.ParticleEmissionType.Plane)
                        {
                            newNode = ObjectPoolManager.GetParticlePlane(emitTransform, velocity, ParticleSystem, Node.ChildParticleNodes[i], EffectPart, ParticleSystem.Effect.Target);
                            GameBase.RenderSystem.AddRenderEntity(newNode);
                        }
                        else if (Node.ChildParticleNodes[i].EmissionNode.EmissionType == ParticleEmission.ParticleEmissionType.Mesh)
                        {
                            newNode = ObjectPoolManager.GetParticleMesh(emitTransform, velocity, ParticleSystem, Node.ChildParticleNodes[i], EffectPart, ParticleSystem.Effect.Target);
                            GameBase.RenderSystem.AddRenderEntity(newNode);
                        }
                        else
                        {
                            //placeholder
                            newNode = ObjectPoolManager.GetParticleNodeBase(emitTransform, velocity, ParticleSystem, Node.ChildParticleNodes[i], EffectPart, ParticleSystem.Effect.Target);
                        }

                        Nodes.Add(newNode);
                    }
                    else
                    {
                        //"null" node.
                        Nodes.Add(ObjectPoolManager.GetParticleNodeBase(emitTransform, velocity, ParticleSystem, Node.ChildParticleNodes[i], EffectPart, ParticleSystem.Effect.Target));
                    }

                    ActiveInstances[i]++;
                }
            }
        }

        protected virtual Matrix GetEmitTransformationMatrix(ref Vector3 velocity)
        {
            return Matrix.Identity;
        }

        /// <summary>
        /// Terminate the node, according to <see cref="EffectPart.DeactivationMode"/>.
        /// </summary>
        public void Deactivate()
        {
            Loop = false;

            foreach (ParticleNodeBase node in Nodes)
            {
                node.Deactivate();
            }
        }

        protected Matrix GetAttachmentBone()
        {
            return EffectPart.InstantUpdate ? ParticleSystem.AttachmentBone : EmitBoneTransform;
        }

        private void NullEmitter()
        {
            if (Node.NodeType == ParticleNodeType.Null && !Loop)
            {
                Emit();
            }
        }

    }

    public enum NodeState : byte
    {
        /// <summary>
        /// The node has been created, but hasn't yet started.
        /// </summary>
        Created,
        /// <summary>
        /// The node is currently active.
        /// </summary>
        Active,
        /// <summary>
        /// The node has reached the end of its life, but still has active children.
        /// </summary>
        WaitingOnChildren,
        /// <summary>
        /// The node and its children are all expired.
        /// </summary>
        Expired
    }
}
