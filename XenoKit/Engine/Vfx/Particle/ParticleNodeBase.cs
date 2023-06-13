using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using XenoKit.Helper;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.Resource;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticleNodeBase : Entity
    {
        public ParticleSystem ParticleSystem { get; private set; }
        public readonly ParticleNode Node;
        private readonly EffectPart EffectPart;

        public readonly int StartTime;
        public readonly int Lifetime;
        public readonly int Burst;
        public readonly int BurstFrequency;
        public bool Loop;

        private readonly float PositionX_Variance;
        private readonly float PositionY_Variance;
        private readonly float PositionZ_Variance;
        private readonly float RotationX_Variance;
        private readonly float RotationY_Variance;
        private readonly float RotationZ_Variance;

        protected float StartRotation_Variance = 0f;
        protected float ActiveRotation_Variance = 0f;
        protected float RotationAmount = 0f;
        public Vector3 Velocity;

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
        /// <summary>
        /// The current absolute transform of this node, relative to the world.
        /// </summary>
        public Matrix AbsoluteTransform;
        protected Matrix ScaleAdjustment = Matrix.Identity;

        public NodeState State { get; protected set; }
        public float CurrentFrame { get; private set; }
        public float CurrentTimeFactor { get; private set; }

        //Children Nodes
        public List<ParticleNodeBase> Nodes = new List<ParticleNodeBase>();

        //Active child node instances
        protected List<int> ActiveInstances = new List<int>();
        protected bool ActiveInstancesUpdatedThisFrame = false;
        protected virtual bool IsRootNode => false;

        public ParticleNodeBase(Matrix emitPoint, Vector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, GameBase gameBase) : base(gameBase)
        {
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

        public ParticleNodeBase(Matrix emitPoint, ParticleSystem system, EffectPart effectPart, GameBase gameBase) : base(gameBase)
        {
            EmitPointTransform = emitPoint;
            ParticleSystem = system;
            EffectPart = effectPart;
            Node = new ParticleNode();
            Node.Burst = 1;
        }

        public override void Update()
        {
            StartUpdate();
            UpdateChildrenNodes();
            EndUpdate();
        }

        public override void Draw()
        {
            for(int i = 0; i < Nodes.Count; i++)
            {
                Nodes[i].Draw();
            }
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
            }

            if (CurrentFrame >= Lifetime)
            {
                if (Loop && !ParticleSystem.IsTerminating)
                {
                    CurrentFrame = 0f;
                }
                else
                {
                    State = NodeState.WaitingOnChildren;

                    //Emission and Null nodes only emit when they expire
                    if (Node.NodeType != ParticleNodeType.Emitter)
                    {
                        Emit();
                    }
                }
            }

            //Main logic loop
            if (State == NodeState.Active)
            {
                CurrentTimeFactor = CurrentFrame / Lifetime;

                //Update movement
                if (SceneManager.IsPlaying)
                {
                    UpdateModifiers();

                    //Change position based on current velocity
                    MovementTransform *= Matrix.CreateTranslation((Velocity / 60f) * ((EffectPart.UseTimeScale) ? SceneManager.MainAnimTimeScale * SceneManager.BacTimeScale : 1f));
                }

                //Update position and rotation
                float[] position = Node.Position.GetInterpolatedValue(CurrentTimeFactor);
                float[] rotation = Node.Rotation.GetInterpolatedValue(CurrentTimeFactor);

                Transform = MovementTransform * ScaleAdjustment * EmitPointTransform;
                Transform *= Matrix.CreateFromQuaternion(GeneralHelpers.EulerAnglesToQuaternion(new Vector3(MathHelper.ToRadians(rotation[0] + RotationX_Variance), MathHelper.ToRadians(rotation[1] + RotationY_Variance), MathHelper.ToRadians(rotation[2] + RotationZ_Variance))));
                Transform *= Matrix.CreateTranslation(new Vector3(position[0] + PositionX_Variance, position[1] + PositionY_Variance, position[2] + PositionZ_Variance) * ParticleSystem.Scale);
                //Transform = MovementTransform * Transform;

                AbsoluteTransform = EffectPart.InstantUpdate ? Transform * ParticleSystem.AttachmentBone : Transform * EmitBoneTransform;
            }

        }

        private void UpdateModifiers()
        {
            foreach(EMP_Modifier modifier in Node.Modifiers)
            {
                switch (modifier.EmpType)
                {
                    case EMP_Modifier.EmpModifierType.Acceleration:
                        {
                            var values = modifier.Axis.GetInterpolatedValue(CurrentTimeFactor);
                            Velocity += (new Vector3(values[0] / 60f, values[1] / 60f, values[2] / 60f) * modifier.Factor.GetInterpolatedValue(CurrentTimeFactor)) * ParticleSystem.Scale;
                        }
                        break;
                }
            }
        }

        protected void UpdateRotation()
        {
            if(SceneManager.IsPlaying)
                RotationAmount += (Node.EmissionNode.ActiveRotation.GetInterpolatedValue(CurrentTimeFactor) + ActiveRotation_Variance) / 60f;
        }

        protected void UpdateChildrenNodes()
        {
            for (int i = Nodes.Count - 1; i >= 0; i--)
            {
                Nodes[i].Update();

                if (Nodes[i].State == NodeState.Expired)
                {
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
            if (SceneManager.IsPlaying)
                CurrentFrame += EffectPart.UseTimeScale ? 1f * SceneManager.BacTimeScale * SceneManager.MainAnimTimeScale : 1f;

            if (State == NodeState.WaitingOnChildren && Nodes.Count == 0)
            {
                State = NodeState.Expired;
            }

            ActiveInstancesUpdatedThisFrame = false;
        }

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
                    //Maximum amount of instances of this node reached, so cannot create more
                    if (Node.ChildParticleNodes[i].MaxInstances <= ActiveInstances[i] && !IsRootNode)
                        break;

                    //Position where the node is to be emitted. 
                    Vector3 velocity = Vector3.Zero;
                    Matrix emitTransform = GetEmitTransformationMatrix(ref velocity) * Transform;

                    //IF node is an emission (NOT an emitter), then any children nodes will inherit its velocity.
                    if (Node.IsEmission)
                        velocity = Velocity;

                    if (Node.ChildParticleNodes[i].NodeType == ParticleNodeType.Emitter)
                    {
                        Nodes.Add(new ParticleEmitter(emitTransform, velocity, ParticleSystem, Node.ChildParticleNodes[i], EffectPart, GameBase));

                    }
                    else if (Node.ChildParticleNodes[i].NodeType == ParticleNodeType.Emission)
                    {
                        if (Node.ChildParticleNodes[i].EmissionNode.EmissionType == ParticleEmission.ParticleEmissionType.Plane)
                        {
                            Nodes.Add(new Particle(emitTransform, velocity, ParticleSystem, Node.ChildParticleNodes[i], EffectPart, GameBase));
                        }
                        else
                        {
                            //placeholder
                            Nodes.Add(new ParticleNodeBase(emitTransform, velocity, ParticleSystem, Node.ChildParticleNodes[i], EffectPart, GameBase));
                        }
                    }
                    else
                    {
                        //"null" node.
                        Nodes.Add(new ParticleNodeBase(emitTransform, velocity, ParticleSystem, Node.ChildParticleNodes[i], EffectPart, GameBase));
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

                    ActiveInstances[i] = Nodes.Where(x => x.Node == Node.ChildParticleNodes[i]).Count();
                }
            }
        }

        /// <summary>
        /// Terminate the node, according to <see cref="EffectPart.DeactivationMode.AfterAnimLoop"/>.
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
