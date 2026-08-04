using Microsoft.Xna.Framework;
using XenoKit.Engine;
using Xv2CoreLib;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.Resource;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx.Particle
{
    public static class ParticleBillboard
    {
        public static Matrix4x4 CreateWorld(ParticleEmissionBase particle, ParticleBillboardType billboardType, bool velocityOriented, float rotationAmount, bool randomDirection, float renderDepth)
        {
            Matrix4x4 world;

            if (billboardType == ParticleBillboardType.Camera)
            {
                Matrix4x4 attachBone = particle.GetParticleAttachmentBone();
                float angle = randomDirection ? -rotationAmount : rotationAmount;
                Matrix4x4 worldTranslation = particle.Transform * Matrix4x4.CreateScale(particle.ParticleSystem.Scale) * attachBone;

                if (velocityOriented)
                {
                    Matrix4x4 baseWorld = particle.Transform * attachBone;
                    world = Matrix4x4.CreateConstrainedBillboard(baseWorld.Translation, particle.Camera.CameraState.Position, baseWorld.GetUp(), -MathHelpers.Up, SimdVector3.Zero) * Matrix4x4.CreateScale(particle.ParticleSystem.Scale);
                    world.Translation = worldTranslation.Translation;
                }
                else
                {
                    world = Matrix4x4.CreateFromAxisAngle(MathHelpers.Up, MathHelper.Pi) *
                            Matrix4x4.CreateFromAxisAngle(MathHelpers.Forward, MathHelper.ToRadians(-angle)) *
                            MathHelpers.Invert(particle.Camera.ViewMatrix) *
                            Matrix4x4.CreateScale(particle.ParticleSystem.Scale);
                    world.Translation = worldTranslation.Translation;
                }
            }
            else if (billboardType == ParticleBillboardType.Front)
            {
                Matrix4x4 attachBone = particle.GetParticleAttachmentBone();
                float angle = randomDirection ? -rotationAmount : rotationAmount;
                Matrix4x4 baseWorld = particle.Transform * Matrix4x4.CreateScale(particle.ParticleSystem.Scale) * attachBone;

                world = Matrix4x4.CreateFromAxisAngle(MathHelpers.Forward, MathHelper.ToRadians(-angle)) *
                        Matrix4x4.CreateBillboard(baseWorld.Translation, attachBone.Translation, MathHelpers.Up, SimdVector3.Zero) *
                        Matrix4x4.CreateScale(particle.ParticleSystem.Scale);
                world.Translation = baseWorld.Translation;
            }
            else
            {
                world = particle.GetParticleRotationAxisWorld(false);
            }

            return ApplyRenderDepth(particle, world, renderDepth);
        }

        public static Matrix4x4 ApplyRenderDepth(EngineObject owner, Matrix4x4 world, float renderDepth)
        {
            return world * Matrix4x4.CreateTranslation(owner.Camera.TransformRelativeToCamera(world.Translation, renderDepth));
        }
    }
}
