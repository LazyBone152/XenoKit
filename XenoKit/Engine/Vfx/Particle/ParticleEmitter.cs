using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.Resource;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticleEmitter : ParticleNodeBase
    {
        private float BurstCountdown = 0;
        private float SizeVariance;
        private float Size2Variance;
        private float EdgeIncrement;

        public override void Initialize(Matrix emitLocalMatrix, Vector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            base.Initialize(emitLocalMatrix, velocity, system, node, effectPart, effect);
            SetValues();
        }

        public override void ClearObjectState()
        {
            base.ClearObjectState();
            BurstCountdown = 0;
        }

        public override void Release()
        {
            ObjectPoolManager.ParticleEmitterPool.ReleaseObject(this);
        }

        private void SetValues()
        {
            SizeVariance = Xv2CoreLib.Random.Range(0, Node.EmitterNode.Size_Variance);
            Size2Variance = Xv2CoreLib.Random.Range(0, Node.EmitterNode.Size2_Variance);
            EdgeIncrement = 0f;
        }

        public override void Update()
        {
            StartUpdate();

            if (State == NodeState.Active && ParticleSystem.CurrentFrameDelta > 0f)
            {
                if (BurstCountdown <= 0f)
                {
                    Emit();
                    BurstCountdown = BurstFrequency;
                }
                else
                {
                    BurstCountdown -= ParticleSystem.CurrentFrameDelta;
                }
            }

            UpdateChildrenNodes();
            EndUpdate();
        }

        protected override Matrix GetEmitTransformationMatrix(ref Vector3 velocity)
        {
            Matrix transformMatrix = Matrix.Identity;

            switch (Node.EmitterNode.Shape)
            {
                case Xv2CoreLib.EMP_NEW.ParticleEmitter.ParticleEmitterShape.Circle:
                    transformMatrix = GetEmitOnCircleMatrix();
                    break;
                case Xv2CoreLib.EMP_NEW.ParticleEmitter.ParticleEmitterShape.Square:
                    transformMatrix = GetEmitOnSquareMatrix();
                    break;
                case Xv2CoreLib.EMP_NEW.ParticleEmitter.ParticleEmitterShape.Sphere:
                    transformMatrix = GetEmitOnSphereMatrix();
                    break;
                case Xv2CoreLib.EMP_NEW.ParticleEmitter.ParticleEmitterShape.Point:
                    transformMatrix = GetEmitOnPointMatrix();
                    break;
            }

            //Calculate the velocity of the emitted node.
            float velocityAmount = Node.EmitterNode.Velocity.GetInterpolatedValue(CurrentTimeFactor) + Xv2CoreLib.Random.Range(0, Node.EmitterNode.Velocity_Variance);
            velocity = new Vector3(0, velocityAmount, 0);

            return transformMatrix;
        }

        private Matrix GetEmitOnCircleMatrix()
        {
            float radius = (Node.EmitterNode.Size.GetInterpolatedValue(CurrentTimeFactor) + SizeVariance);
            float angle = Node.EmitterNode.Angle.GetInterpolatedValue(CurrentTimeFactor) + Xv2CoreLib.Random.Range(0, Node.EmitterNode.Angle_Variance);
            float positionOffset = Node.EmitterNode.Position.GetInterpolatedValue(CurrentTimeFactor) + Xv2CoreLib.Random.Range(0, Node.EmitterNode.Position_Variance);

            bool isEdgeIncrement = Node.NodeFlags2.HasFlag(NodeFlags2.RandomRotationDir) && Node.EmitterNode.EmitFromArea;
            Vector3 position;

            if (isEdgeIncrement)
            {
                position = Vector3.Transform(new Vector3(radius, 0, 0), Matrix.CreateRotationY(MathHelper.ToRadians(-EdgeIncrement * 360f)));

                //Increment the value, and cap it between 0 and 1
                EdgeIncrement += Node.EmitterNode.EdgeIncrement;

                if (EdgeIncrement > 1f)
                    EdgeIncrement -= 1f;
            }
            else
            {
                //Random position in circle
                float randomPosition = isEdgeIncrement ? radius : Xv2CoreLib.Random.Range(0, radius);

                // Generate a random angle within the specified range
                float randomAngle = isEdgeIncrement ? EdgeIncrement * 360f : Xv2CoreLib.Random.Range(0, 360);

                // Calculate the position based on the random angle and radius
                position = new Vector3(
                        randomPosition * (float)Math.Cos(randomAngle),
                        0f,
                        randomPosition * (float)Math.Sin(randomAngle)
                    );
            }

            Vector3 positionOffsetVector = new Vector3(0, positionOffset, 0);

            // If emitFromEdge is true, adjust the position to be on the edge of the circle
            if (Node.EmitterNode.EmitFromArea)
            {
                position.Normalize();
                position *= radius;
            }

            // Calculate the rotation based on the specified angle
            float rotation = MathHelper.ToRadians(-angle);

            // Determine the rotation axis based on the position within the circle
            Vector3 rotationAxis = new Vector3(-position.Z, 0f, position.X);
            rotationAxis.Normalize();

            // Create the transformation matrix using the position and rotation
            Matrix transform = Matrix.CreateTranslation(positionOffsetVector)
                * Matrix.CreateFromAxisAngle(rotationAxis, rotation);

            transform.Translation += position;

            return transform;
        }

        private Matrix GetEmitOnSquareMatrix()
        {
            float sizeX = (Node.EmitterNode.Size.GetInterpolatedValue(CurrentTimeFactor) + SizeVariance);
            float sizeZ = (Node.EmitterNode.Size2.GetInterpolatedValue(CurrentTimeFactor) + Size2Variance);
            float angle = Node.EmitterNode.Angle.GetInterpolatedValue(CurrentTimeFactor) + Xv2CoreLib.Random.Range(0, Node.EmitterNode.Angle_Variance);
            float positionOffset = Node.EmitterNode.Position.GetInterpolatedValue(CurrentTimeFactor) + Xv2CoreLib.Random.Range(0, Node.EmitterNode.Position_Variance);

            bool isEdgeIncrement = Node.NodeFlags2.HasFlag(NodeFlags2.RandomRotationDir) && Node.EmitterNode.EmitFromArea;

            // Calculate the position based on sizeX and sizeZ
            Vector3 position;

            if (isEdgeIncrement)
            {
                int edge = (int)(4f * EdgeIncrement);
                float posX;
                float posZ;
                float factor;

                switch (edge)
                {
                    case 0: // left edge
                        factor = EdgeIncrement * 4f;
                        posX = -sizeX;
                        posZ = MathHelpers.Lerp(sizeZ, -sizeZ, factor);
                        break;
                    case 1: // bottom edge
                        factor = (EdgeIncrement - 0.25f) * 4f;
                        posX = MathHelpers.Lerp(-sizeX, sizeX, factor);
                        posZ = -sizeZ;
                        break;
                    case 2: // right edge
                        factor = (EdgeIncrement - 0.5f) * 4f;
                        posX = sizeX;
                        posZ = MathHelpers.Lerp(-sizeZ, sizeZ, factor);
                        break;
                    case 3: // top edge
                        factor = (EdgeIncrement - 0.75f) * 4f;
                        posX = MathHelpers.Lerp(sizeX, -sizeX, factor);
                        posZ = sizeZ;
                        break;
                    default: // shouldn't happen
                        posX = 0f;
                        posZ = 0f;
                        break;
                }

                //Increment the value, and cap it between 0 and 1
                EdgeIncrement += Node.EmitterNode.EdgeIncrement;

                if (EdgeIncrement > 1f)
                    EdgeIncrement -= 1f;

                position = new Vector3(posX, 0f, posZ);
            }
            else if (Node.EmitterNode.EmitFromArea)
            {
                // randomly choose one of the four edges
                int edge = Xv2CoreLib.Random.Range(0, 3);
                float posX;
                float posZ;

                switch (edge)
                {
                    case 0: // left edge
                        posX = -sizeX;
                        posZ = Xv2CoreLib.Random.Range(-sizeZ, sizeZ);
                        break;
                    case 1: // right edge
                        posX = sizeX;
                        posZ = Xv2CoreLib.Random.Range(-sizeZ, sizeZ);
                        break;
                    case 2: // bottom edge
                        posX = Xv2CoreLib.Random.Range(-sizeX, sizeX);
                        posZ = -sizeZ;
                        break;
                    case 3: // top edge
                        posX = Xv2CoreLib.Random.Range(-sizeX, sizeX);
                        posZ = sizeZ;
                        break;
                    default: // shouldn't happen
                        posX = 0f;
                        posZ = 0f;
                        break;
                }

                position = new Vector3(posX, 0f, posZ);
            }
            else
            {
                position = new Vector3(Xv2CoreLib.Random.Range(-sizeX, sizeX), 0f, Xv2CoreLib.Random.Range(-sizeZ, sizeZ));
            }

            Vector3 positionOffsetVector = new Vector3(0, positionOffset, 0);

            // Calculate the rotation based on the specified angle
            float rotation = MathHelper.ToRadians(-angle);

            // Determine the rotation axis based on the position within the square
            Vector3 rotationAxis = new Vector3(-position.Z, 0f, position.X);
            rotationAxis.Normalize();

            // Create the transformation matrix using the position and rotation
            Matrix transform = Matrix.CreateTranslation(positionOffsetVector)
                * Matrix.CreateFromAxisAngle(rotationAxis, rotation);

            transform.Translation += position;

            return transform;
        }

        private Matrix GetEmitOnSphereMatrix()
        {
            //Lazy reuse of circle method.

            float radius = 0.00001f;
            float positionOffset = (Node.EmitterNode.Size.GetInterpolatedValue(CurrentTimeFactor) + SizeVariance);

            //Random position in circle
            float randomPosition = Xv2CoreLib.Random.Range(0, radius);

            // Generate a random angle within the specified range
            float randomAngle = Xv2CoreLib.Random.Range(0, 360);

            // Calculate the position based on the random angle and radius
            Vector3 position = new Vector3(
                    randomPosition * (float)Math.Cos(randomAngle),
                    0f,
                    randomPosition * (float)Math.Sin(randomAngle)
                );

            Vector3 positionOffsetVector = new Vector3(0, positionOffset, 0);

            // Calculate the rotation based on the specified angle
            float rotation = MathHelper.ToRadians(Xv2CoreLib.Random.Range(0, 360));

            // Determine the rotation axis based on the position within the circle
            Vector3 rotationAxis = new Vector3(-position.Z, 0f, position.X);
            rotationAxis.Normalize();

            // Create the transformation matrix using the position and rotation
            Matrix transform = Matrix.CreateTranslation(positionOffsetVector)
                * Matrix.CreateFromAxisAngle(rotationAxis, rotation);

            transform.Translation += position;

            return transform;
            //return Matrix.CreateTranslation(new Vector3(0, 10, 0)) * transform;
        }

        private Matrix GetEmitOnPointMatrix()
        {
            //Repurposed from cirlce. NOT quite 100% accurate, but close enough for now

            float radius = 1f;
            float angle = Node.EmitterNode.Angle.GetInterpolatedValue(CurrentTimeFactor) + Xv2CoreLib.Random.Range(0, Node.EmitterNode.Angle_Variance);
            float positionOffset = Node.EmitterNode.Position.GetInterpolatedValue(CurrentTimeFactor) + Xv2CoreLib.Random.Range(0, Node.EmitterNode.Position_Variance);

            if (angle > 0)
                angle = Xv2CoreLib.Random.Range(5, angle);
            else
                angle = Xv2CoreLib.Random.Range(angle, -5);

            //Random position in circle
            float randomPosition = Xv2CoreLib.Random.Range(0, radius);

            // Generate a random angle within the specified range
            float randomAngle = Xv2CoreLib.Random.Range(0, 360);

            // Calculate the position based on the random angle and radius
            Vector3 position = new Vector3(
                    randomPosition * (float)Math.Cos(randomAngle),
                    0f,
                    randomPosition * (float)Math.Sin(randomAngle)
                );

            Vector3 positionOffsetVector = new Vector3(0, positionOffset, 0);

            // Calculate the rotation based on the specified angle
            float rotation = MathHelper.ToRadians(-angle);

            // Determine the rotation axis based on the position within the circle
            Vector3 rotationAxis = new Vector3(-position.Z, 0f, position.X);
            rotationAxis.Normalize();

            // Create the transformation matrix using the position and rotation
            Matrix transform = Matrix.CreateTranslation(positionOffsetVector)
                * Matrix.CreateFromAxisAngle(rotationAxis, rotation);

            return  transform;
        }


    }
}
