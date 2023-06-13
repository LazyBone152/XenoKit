using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMP_NEW;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticleEmitter : ParticleNodeBase
    {
        private int BurstCountdown = 0;
        private float SizeVariance;
        private float Size2Variance;

        public ParticleEmitter(Matrix emitLocalMatrix, Vector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, GameBase gameBase) : base(emitLocalMatrix, velocity, system, node, effectPart, gameBase)
        {
            SetValues();
        }

        private void SetValues()
        {
            SizeVariance = Xv2CoreLib.Random.Range(0, Node.EmitterNode.Size_Variance);
            Size2Variance = Xv2CoreLib.Random.Range(0, Node.EmitterNode.Size2_Variance);
        }

        public override void Update()
        {
            StartUpdate();

            if (State == NodeState.Active && GameBase.IsPlaying)
            {
                if (BurstCountdown == 0)
                {
                    Emit();
                    BurstCountdown = BurstFrequency;
                }
                else
                {
                    BurstCountdown--;
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
            velocity = new Vector3(0, velocityAmount, 0) * ParticleSystem.Scale;

            return transformMatrix * Matrix.CreateScale(ParticleSystem.Scale);
        }

        private Matrix GetEmitOnCircleMatrix()
        {
            float radius = (Node.EmitterNode.Size.GetInterpolatedValue(CurrentTimeFactor) + SizeVariance);
            float angle = Node.EmitterNode.Angle.GetInterpolatedValue(CurrentTimeFactor) + Xv2CoreLib.Random.Range(0, Node.EmitterNode.Angle_Variance);
            float positionOffset = Node.EmitterNode.Position.GetInterpolatedValue(CurrentTimeFactor) + Xv2CoreLib.Random.Range(0, Node.EmitterNode.Position_Variance);

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

            // Calculate the position based on sizeX and sizeZ
            Vector3 position;

            if (Node.EmitterNode.EmitFromArea)
            {
                // randomly choose one of the four edges
                int edge = Xv2CoreLib.Random.Range(0, 4);
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
