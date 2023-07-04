using System;
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

        private float PreviousFrame = 0f;
        public float CurrentFrameDelta { get; private set; }
        public bool IsSimulating { get; private set; }
        public Matrix AttachmentBone { get; private set; } = Matrix.Identity;

        protected override bool FinishAnimationBeforeTerminating => true;

        private bool IsDirty { get; set; }

        public ParticleSystem(Matrix startWorld, Actor actor, EffectPart effectPart, EMP_File empFile, VfxEffect effect, GameBase gameBase) : base(startWorld, effectPart, actor, gameBase)
        {
            Effect = new WeakReference(effect);
            EmpFile = empFile;
            EmpFile.PropertyChanged += EmpFile_PropertyChanged;
            InitializeParticleSystem();
        }

        private void EmpFile_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IsDirty = true;
        }

        private void InitializeParticleSystem()
        {
            AttachmentBone = GetAdjustedTransform();
            RootNode = new ParticleRootNode(EmpFile, this, EffectPart, GameBase);
            RootNode.Play();
        }

        /// <summary>
        /// Release all the Particle Systems objects back into the pool.
        /// </summary>
        public void DestroyParticleSystem()
        {
            EmpFile.PropertyChanged -= EmpFile_PropertyChanged;

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

            if (!HasStarted) return;

            //Calculate frame delta (change in frames).
            CurrentFrameDelta = CurrentFrame - PreviousFrame;

            AttachmentBone = GetAdjustedTransform();

            if (IsDirty)
            {
                RestartParticleSystem();
            }

            RootNode.Update();

            if (RootNode.State == NodeState.Expired)
            {
                IsFinished = true;
                DestroyParticleSystem();
                return;
            }

            PreviousFrame = CurrentFrame;

            if (GameBase.IsPlaying)
                CurrentFrame += EffectPart.UseTimeScale ? GameBase.ActiveTimeScale : 1f;
        }

        public override void Simulate()
        {
            base.Update();

            if (!HasStarted) return;

            CurrentFrameDelta = 1f;

            RootNode.Update();

            if (RootNode.State == NodeState.Expired)
            {
                IsFinished = true;
                DestroyParticleSystem();
                return;
            }

            PreviousFrame = CurrentFrame;
            CurrentFrame++;
        }

        public void RestartParticleSystem()
        {
            IsDirty = false;
            RootNode.ReleaseAll();
            InitializeParticleSystem();
        }

        public override void SeekNextFrame()
        {
            base.SeekNextFrame();
            Simulate();
        }

        public override void SeekPrevFrame()
        {
            //Cant really seek backwards in a particle system.
        }

    }
}
