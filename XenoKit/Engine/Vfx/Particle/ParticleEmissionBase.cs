using Microsoft.Xna.Framework;
using XenoKit.Editor;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMP_NEW;

namespace XenoKit.Engine.Vfx.Particle
{
    /// <summary>
    /// Base class for all emission particle nodes.
    /// </summary>
    public abstract class ParticleEmissionBase : ParticleNodeBase
    {
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

        public override int AlphaBlendType
        {
            get
            {
                if (EmissionData?.Material == null) return -1;
                if (EmissionData.Material.MatParam.AlphaBlend == 0 || EmissionData.Material.MatParam.AlphaBlendType == 3) return -1;
                return EmissionData.Material.MatParam.AlphaBlendType;
            }
        }
        public override int LowRezMode
        {
            get
            {
                if (EmissionData?.Material == null) return 0;
                if (EmissionData.Material.MatParam.LowRez == 1) return 1;
                if (EmissionData.Material.MatParam.LowRezSmoke == 1) return 2;
                return 0;
            }
        }

        public override void Initialize(Matrix emitPoint, Vector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            base.Initialize(emitPoint, velocity, system, node, effectPart, effect);
            EmissionData = CompiledObjectManager.GetCompiledObject<ParticleEmissionData>(node, GameBase);
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
            RandomDirection = Node.NodeFlags2.HasFlag(NodeFlags2.RandomRotationDir) ? Xv2CoreLib.Random.RandomBool() : false;

            if (Node.NodeFlags2.HasFlag(NodeFlags2.RandomUpVector))
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

            if (Node.NodeFlags.HasFlag(NodeFlags1.EnableScaleXY))
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

            if (Node.NodeFlags.HasFlag(NodeFlags1.EnableSecondaryColor))
            {
                float[] secondaryColor = Node.EmissionNode.Texture.Color2.GetInterpolatedValue(CurrentTimeFactor);
                SecondaryColor[3] = Node.EmissionNode.Texture.Color2_Transparency.GetInterpolatedValue(CurrentTimeFactor);

                SecondaryColor[0] = MathHelper.Clamp(secondaryColor[0] + ColorR_Variance, 0f, 1f);
                SecondaryColor[1] = MathHelper.Clamp(secondaryColor[1] + ColorG_Variance, 0f, 1f);
                SecondaryColor[2] = MathHelper.Clamp(secondaryColor[2] + ColorB_Variance, 0f, 1f);
                SecondaryColor[3] = MathHelper.Clamp(SecondaryColor[3] + ColorA_Variance, 0f, 1f);
            }
        }

        protected Matrix GetRotationAxisWorld(bool isRotPerSecond)
        {
            Matrix attachBone = GetAttachmentBone();
            float rotAmount = RandomDirection ? -RotationAmount : RotationAmount;

            if (isRotPerSecond)
                rotAmount /= 60f;

            _ = Transform.Decompose(out Vector3 scale, out _, out Vector3 translation);
            Matrix world = Matrix.CreateTranslation(translation) * Matrix.CreateScale(scale) * Matrix.CreateScale(ParticleSystem.Scale) * attachBone;
            Vector3 rotAxis;

            if (Node.NodeFlags2.HasFlag(NodeFlags2.RandomUpVector))
            {
                rotAxis = new Vector3(RandomRotX + Node.EmissionNode.RotationAxis.X, RandomRotY + Node.EmissionNode.RotationAxis.Y, RandomRotZ + Node.EmissionNode.RotationAxis.Z) * rotAmount;
                return Matrix.CreateFromYawPitchRoll(rotAxis.X, rotAxis.Y, rotAxis.Z) * Rotation * world;
            }
            else
            {
                rotAxis = new Vector3(Node.EmissionNode.RotationAxis.X, Node.EmissionNode.RotationAxis.Y, Node.EmissionNode.RotationAxis.Z);
                return Matrix.CreateFromAxisAngle(rotAxis, MathHelper.ToRadians(rotAmount)) * Rotation * world;
            }

            //There is one case where this is different from the game:
            //IF RotAxis is 0 and some rotation value is set, the plane will disappear in XenoKit, but still be visible ingame
            //But it then disappears ingame anyway if a very low RotAxis is used
            //Can be fixed with an additional check against RotZero being zero, then removing the rotation from the matrix multiplication, but not sure if worth it

        }
    }
}
