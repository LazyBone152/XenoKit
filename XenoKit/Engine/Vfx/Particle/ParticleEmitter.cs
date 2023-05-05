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

        public ParticleEmitter(Matrix emitLocalMatrix, ParticleSystem system, ParticleNode node, EffectPart effectPart, GameBase gameBase) : base(emitLocalMatrix, system, node, effectPart, gameBase)
        {
            SetValues();
        }

        private void SetValues()
        {

        }

        protected override void GetEmitPositionAndVector(ref Vector3 position, ref Vector3 direction)
        {
            direction = new Vector3(RandF(0, 1), 0, 0);

            if (Node.EmitterNode.Shape == Xv2CoreLib.EMP_NEW.ParticleEmitter.ParticleEmitterShape.Circle)
            {
                Matrix randomMatrix = Matrix.CreateFromYawPitchRoll(RandF(-MathHelper.Pi, MathHelper.Pi), RandF(-MathHelper.Pi, MathHelper.Pi), 0);
                position = Vector3.Transform(direction, randomMatrix);

                float radius = Node.EmitterNode.Size.GetInterpolatedValue(CurrentTimeFactor) + Xv2CoreLib.Random.Range(0, Node.EmitterNode.Size_Variance);
                float angle = Node.EmitterNode.Angle.GetInterpolatedValue(CurrentTimeFactor) + Xv2CoreLib.Random.Range(0, Node.EmitterNode.Angle_Variance);
                float positionOffset = Node.EmitterNode.Position.GetInterpolatedValue(CurrentTimeFactor) + Xv2CoreLib.Random.Range(0, Node.EmitterNode.Position_Variance);

                if (Node.EmitterNode.EmitFromArea)
                    position.Normalize();
                position *= radius;

                direction = new Vector3();
                direction.Y += RandF(0, Lifetime / 60); //What does this do?
                float offset = direction.Y * (float)Math.Tan(angle * MathHelper.Pi / 180.0f);
                direction.X += offset * position.X / position.Length();
                direction.Z += offset * position.Z / position.Length();

                //Add position offset onto position. This is done at the end because it happens along the up vector.
                position += Vector3.Transform(new Vector3(0, positionOffset, 0), Matrix.CreateTranslation(direction));

                if (Node.EmitterNode.EmitFromArea)
                    position += direction;

                direction.Normalize();
                return;
            }

            /*
            if (Node.EmitterNode.Shape == Xv2CoreLib.EMP_NEW.ParticleEmitter.ParticleEmitterShape.Circle)
            {
                Matrix3x3 randomMatrix = Matrix3x3.RotationYawPitchRoll(RandF(-MathHelper.Pi, MathHelper.Pi), RandF(-MathHelper.Pi, MathHelper.Pi), 0);
                direction = Vector3.Transform(direction, randomMatrix);

                if (MainViewModel.MainParticleSystem.Shape.EmitFromShell)
                    direction.Normalize();
                position = MainViewModel.MainParticleSystem.Shape.Radius * direction;
                direction.Normalize();
                return;
            }
            if (MainViewModel.MainParticleSystem.Shape.ShapeType == ParticleSystem.ShapeType.SPHERE)
            {
                Matrix3x3 randomMatrix = Matrix3x3.RotationYawPitchRoll(RandF(-MathHelper.Pi, MathHelper.Pi), RandF(-MathHelper.Pi, MathHelper.Pi), RandF(-MathHelper.Pi, MathHelper.Pi));
                direction = Vector3.Transform(direction, randomMatrix);

                if (MainViewModel.MainParticleSystem.Shape.EmitFromShell)
                    direction.Normalize();
                position = MainViewModel.MainParticleSystem.Shape.Radius * direction;
                return;
            }
            if (MainViewModel.MainParticleSystem.Shape.ShapeType == ParticleSystem.ShapeType.CONE)
            {
                Matrix3x3 randomMatrix = Matrix3x3.RotationYawPitchRoll(RandF(-MathHelper.Pi, MathHelper.Pi), RandF(-MathHelper.Pi, MathHelper.Pi), 0);
                position = Vector3.Transform(direction, randomMatrix);

                if (MainViewModel.MainParticleSystem.Shape.EmitFromShell)
                    position.Normalize();
                position *= MainViewModel.MainParticleSystem.Shape.Radius;

                direction = new Vector3();
                direction.Y += RandF(0, MainViewModel.MainParticleSystem.Lifetime);
                float offset = direction.Y * (float)Math.Tan(MainViewModel.MainParticleSystem.Shape.Angle * MathHelper.Pi / 180.0f);
                direction.X += offset * position.X / position.Length();
                direction.Z += offset * position.Z / position.Length();

                if (MainViewModel.MainParticleSystem.Shape.EmitFromVolume)
                    position += direction;

                direction.Normalize();
                return;
            }
            if (MainViewModel.MainParticleSystem.Shape.ShapeType == ParticleSystem.ShapeType.EDGE)
            {
                position = new Vector3(RandF(-MainViewModel.MainParticleSystem.Shape.Radius, MainViewModel.MainParticleSystem.Shape.Radius), 0, 0);
                direction = new Vector3(0, 0, 1);
            }
            */
        }

        private float RandF(float a, float b)
        {
            return Xv2CoreLib.Random.Range(a, b);
        }

    }
}
