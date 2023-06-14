using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EEPK;
using Microsoft.Xna.Framework;
using XenoKit.Engine.Vfx.Asset;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticleSystem : VfxAsset
    {
        private bool IsParticleSystemDestroyed = false;
        public WeakReference Effect;
        public EMP_File EmpFile { get; set; }

        private ParticleRootNode RootNode;
        public ParticleUV[] Textures = null;

        public float ActiveTimeScale = 1f;
        public Matrix AttachmentBone { get; private set; } = Matrix.Identity;

        protected override bool FinishAnimationBeforeTerminating => true;

        public ParticleSystem(Matrix startWorld, Actor actor, EffectPart effectPart, EMP_File empFile, VfxEffect effect, GameBase gameBase) : base(startWorld, effectPart, actor, gameBase)
        {
            Effect = new WeakReference(effect);
            EmpFile = empFile;
            InitializeParticleSystem();
        }

        private void InitializeParticleSystem()
        {
            Textures = new ParticleUV[EmpFile.Textures.Count];

            for(int i = 0; i < EmpFile.Textures.Count; i++)
            {
                Textures[i] = new ParticleUV(EmpFile.Textures[i]);
            }

            RootNode = new ParticleRootNode(EmpFile, this, EffectPart, GameBase);
            RootNode.Play();
        }

        /// <summary>
        /// Release all the Particle Systems objects back into the pool.
        /// </summary>
        public void DestroyParticleSystem()
        {
            if (!IsParticleSystemDestroyed)
            {
                IsParticleSystemDestroyed = true;
                RootNode.ReleaseAll();
            }
        }

        public override void Terminate()
        {
            base.Terminate();

            if (IsFinished)
            {
                DestroyParticleSystem();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            DestroyParticleSystem();
        }

        public override void Update()
        {
            base.Update();

            ActiveTimeScale = EffectPart.UseTimeScale ? GameBase.ActiveTimeScale : 1f;
            AttachmentBone = GetAdjustedTransform();

            RootNode.Update();

            for(int i = 0; i < Textures.Length; i++)
            {
                Textures[i].Update(GameBase.IsPlaying, ActiveTimeScale);
            }

            if (RootNode.State == NodeState.Expired)
            {
                IsFinished = true;
                DestroyParticleSystem();
            }
        }

        public override void Draw()
        {
            RootNode.Draw();
        }

    }
}
