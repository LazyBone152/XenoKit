using Microsoft.Xna.Framework;
using XenoKit.Editor;
using XenoKit.Engine.Shader;
using Xv2CoreLib;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMP_NEW;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx.Particle
{
    /// <summary>
    /// Base class for all emission particle nodes.
    /// </summary>
    public abstract class ParticleEmissionBase : ParticleNodeBase
    {
        public virtual System.Numerics.Matrix4x4 AbsoluteTransform { get; protected set; }

        protected ParticleEmissionData EmissionData;
        protected ParticleUV ParticleUV = new ParticleUV();

        protected float ColorR_Variance = 0f;
        protected float ColorG_Variance = 0f;
        protected float ColorB_Variance = 0f;
        protected float ColorA_Variance = 0f;
        protected float ScaleBase_Variance = 0f;
        protected float ScaleU_Variance = 0f;
        protected float ScaleV_Variance = 0f;
        protected float RandomRotX = 0f;
        protected float RandomRotY = 0f;
        protected float RandomRotZ = 0f;
        protected bool RandomDirection = false;

        //Keyframed Values:
        protected float ScaleBase = 0.5f;
        protected float ScaleU = 0.5f;
        protected float ScaleV = 0.5f;
        protected float[] PrimaryColor = new float[4];
        protected float[] SecondaryColor = new float[4];

        public override int LowRezMode
        {
            get
            {
                if (EmissionData?.Material == null) return 0;
                if (UsesSubtractiveMaterial(EmissionData.Material)) return 0;
                if (EmissionData.Material.MatParam.LowRez == 1) return 1;
                if (EmissionData.Material.MatParam.LowRezSmoke == 1) return 2;
                return 0;
            }
        }

        public override void Initialize(Matrix4x4 emitPoint, SimdVector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            base.Initialize(emitPoint, velocity, system, node, effectPart, effect);
            EmissionData = CompiledObjectManager.GetCompiledObject<ParticleEmissionData>(node);
            EmissionData.EmpFile = system.EmpFile;
            SetValues();
        }

        public override void ClearObjectState()
        {
            base.ClearObjectState();
            EmissionData = null;
        }

        public virtual void SetValues()
        {
            ColorR_Variance = Xv2CoreLib.Random.Range(0, Node.EmissionNode.Texture.Color_Variance.R);
            ColorG_Variance = Xv2CoreLib.Random.Range(0, Node.EmissionNode.Texture.Color_Variance.G);
            ColorB_Variance = Xv2CoreLib.Random.Range(0, Node.EmissionNode.Texture.Color_Variance.B);
            ColorA_Variance = Xv2CoreLib.Random.Range(0, Node.EmissionNode.Texture.Color_Variance.A);

            //Scale variances are a generated together, using the same factor. This is so they all scale uniformly
            float scaleFactor = Xv2CoreLib.Random.Range(0, 1f);
            ScaleBase_Variance = Node.EmissionNode.Texture.ScaleBase_Variance * scaleFactor;
            ScaleU_Variance = Node.EmissionNode.Texture.ScaleXY_Variance.X * scaleFactor;
            ScaleV_Variance = Node.EmissionNode.Texture.ScaleXY_Variance.Y * scaleFactor;

            StartRotation_Variance = Xv2CoreLib.Random.Range(0, Node.EmissionNode.StartRotation_Variance);
            ActiveRotation_Variance = Xv2CoreLib.Random.Range(0, Node.EmissionNode.ActiveRotation_Variance);
            RotationAmount = Node.EmissionNode.StartRotation + StartRotation_Variance;
            RandomDirection = (Node.NodeFlags2 & NodeFlags2.RandomRotationDir) == NodeFlags2.RandomRotationDir ? Xv2CoreLib.Random.RandomBool() : false;

            if ((Node.NodeFlags2 & NodeFlags2.RandomUpVector) == NodeFlags2.RandomUpVector)
            {
                RandomRotX = Xv2CoreLib.Random.Range(0, 1f);
                RandomRotY = Xv2CoreLib.Random.Range(0, 1f);
                RandomRotZ = Xv2CoreLib.Random.Range(0, 1f);
            }

            if(Node.EmissionNode.Texture.TextureEntryRef[0].TextureRef != null)
            {
                ParticleUV.SetTexture(Node.EmissionNode.Texture.TextureEntryRef[0].TextureRef);
            }

            if (Node.EmissionNode.Texture.TextureEntryRef[1].TextureRef != null)
            {
                //Only a grand total of 2 EMPs use the second texture slot, hardly worth the effort of supporting it
                Log.Add($"WARNING: Particle Node ({Node.Name}) uses 2 textures. This is not supported and wont be reflected in the viewport!", LogType.Warning);
            }
        }

        protected void UpdateScale()
        {
            ScaleBase = (Node.EmissionNode.Texture.ScaleBase.GetInterpolatedValue(CurrentTimeFactor) + ScaleBase_Variance);

            if ((Node.NodeFlags & NodeFlags1.EnableScaleXY) == NodeFlags1.EnableScaleXY)
            {
                float[] values = Node.EmissionNode.Texture.ScaleXY.GetInterpolatedValue(CurrentTimeFactor);

                ScaleU = (values[0] + ScaleU_Variance);
                ScaleV = (values[1] + ScaleV_Variance);
            }
            else
            {
                ScaleU = ScaleBase;
                ScaleV = ScaleBase;
            }
        }

        protected void UpdateColor()
        {
            float[] primaryColor = Node.EmissionNode.Texture.Color1.GetInterpolatedValue(CurrentTimeFactor);
            PrimaryColor[3] = Node.EmissionNode.Texture.Color1_Transparency.GetInterpolatedValue(CurrentTimeFactor);

            PrimaryColor[0] = MathHelper.Clamp(primaryColor[0] + ColorR_Variance, 0f, 1f);
            PrimaryColor[1] = MathHelper.Clamp(primaryColor[1] + ColorG_Variance, 0f, 1f);
            PrimaryColor[2] = MathHelper.Clamp(primaryColor[2] + ColorB_Variance, 0f, 1f);
            PrimaryColor[3] = MathHelper.Clamp(PrimaryColor[3] + ColorA_Variance, 0f, 1f);

            if ((Node.NodeFlags & NodeFlags1.EnableSecondaryColor) == NodeFlags1.EnableSecondaryColor)
            {
                float[] secondaryColor = Node.EmissionNode.Texture.Color2.GetInterpolatedValue(CurrentTimeFactor);
                SecondaryColor[3] = Node.EmissionNode.Texture.Color2_Transparency.GetInterpolatedValue(CurrentTimeFactor);

                SecondaryColor[0] = MathHelper.Clamp(secondaryColor[0] + ColorR_Variance, 0f, 1f);
                SecondaryColor[1] = MathHelper.Clamp(secondaryColor[1] + ColorG_Variance, 0f, 1f);
                SecondaryColor[2] = MathHelper.Clamp(secondaryColor[2] + ColorB_Variance, 0f, 1f);
                SecondaryColor[3] = MathHelper.Clamp(SecondaryColor[3] + ColorA_Variance, 0f, 1f);
            }
        }

        internal static bool UsesSubtractiveMaterial(Xv2ShaderEffect material)
        {
            return material?.MatParam != null &&
                   material.MatParam.AlphaBlend == 1 &&
                   material.MatParam.AlphaBlendType == 2;
        }

        protected Matrix4x4 GetRotationAxisWorld(bool isRotPerSecond)
        {
            Matrix4x4 attachBone = GetAttachmentBone();
            float rotAmount = RandomDirection ? -RotationAmount : RotationAmount;

            if (isRotPerSecond)
                rotAmount /= 60f;

            Matrix4x4.Decompose(Transform, out SimdVector3 scale, out _, out SimdVector3 translation);
            Matrix4x4 world = Matrix4x4.CreateTranslation(translation) * Matrix4x4.CreateScale(scale) * Matrix4x4.CreateScale(ParticleSystem.Scale) * attachBone;
            SimdVector3 rotAxis;

            if ((Node.NodeFlags2 & NodeFlags2.RandomUpVector) == NodeFlags2.RandomUpVector)
            {
                rotAxis = new SimdVector3(RandomRotX + Node.EmissionNode.RotationAxis.X, RandomRotY + Node.EmissionNode.RotationAxis.Y, RandomRotZ + Node.EmissionNode.RotationAxis.Z) * rotAmount;
                return VfxRotation.Create(rotAxis.X, rotAxis.Y, rotAxis.Z) * Rotation * world;
            }
            else
            {
                rotAxis = new SimdVector3(Node.EmissionNode.RotationAxis.X, Node.EmissionNode.RotationAxis.Y, Node.EmissionNode.RotationAxis.Z);
                return Matrix4x4.CreateFromAxisAngle(rotAxis, MathHelper.ToRadians(rotAmount)) * Rotation * world;
            }

            //There is one case where this is different from the game:
            //IF RotAxis is 0 and some rotation value is set, the plane will disappear in XenoKit, but still be visible ingame
            //But it then disappears ingame anyway if a very low RotAxis is used
            //Can be fixed with an additional check against RotZero being zero, then removing the rotation from the matrix multiplication, but not sure if worth it

        }

        public Matrix4x4 GetParticleRotationAxisWorld(bool isRotPerSecond)
        {
            return GetRotationAxisWorld(isRotPerSecond);
        }

        public Matrix4x4 GetParticleAttachmentBone()
        {
            return GetAttachmentBone();
        }

        public float GetParticleRotationAmount()
        {
            return RotationAmount;
        }

        public bool GetParticleRandomDirection()
        {
            return RandomDirection;
        }
    
        protected bool FrustumIntersects(Matrix4x4 world, BoundingBox boundingBox)
        {
#if DEBUG
            if (!SceneManager.FrustumCullEnabled) return true;
#endif
            if (SimdVector3.Distance(world.Translation, ViewportInstance.Camera.CameraState.Position) < 3f) return true;

            return ViewportInstance.Camera.Frustum.Intersects(boundingBox.Transform(world));
        }
    }
}
