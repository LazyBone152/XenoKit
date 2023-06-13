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
        public EMP_File EmpFile { get; set; }

        private ParticleRootNode RootNode;
        public ParticleUV[] Textures = null;

        private int BoneIdx = -1;
        public Matrix AttachmentBone { get; private set; } = Matrix.Identity;

        protected override bool FinishAnimationBeforeTerminating => true;

        public ParticleSystem(Matrix startWorld, Actor actor, EffectPart effectPart, EMP_File empFile, GameBase gameBase) : base(startWorld, effectPart, actor, gameBase)
        {
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

        public override void Update()
        {
            base.Update();
            AttachmentBone = GetAdjustedTransform();

            RootNode.Update();

            for(int i = 0; i < Textures.Length; i++)
            {
                Textures[i].Update(SceneManager.IsPlaying, EffectPart.UseTimeScale);
            }

            if (RootNode.State == NodeState.Expired)
            {
                IsFinished = true;
            }
        }

        public override void Draw()
        {
            RootNode.Draw();
        }

    }
}
