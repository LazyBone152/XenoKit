using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EEPK;
using Microsoft.Xna.Framework;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticleSystem : Entity
    {
        public EMP_File EmpFile { get; set; }
        public EffectPart EffectPart { get; set; }

        public ParticleUV[] Textures = null;

        public ParticleSystem(EMP_File empFile, GameBase gameBase) : base(gameBase)
        {
            EmpFile = empFile;
            Initialize();
        }

        private void Initialize()
        {
            Textures = new ParticleUV[EmpFile.Textures.Count];

            for(int i = 0; i < EmpFile.Textures.Count; i++)
            {
                Textures[i] = new ParticleUV(EmpFile.Textures[i]);
            }
        }

        public void Update(Matrix world)
        {
            for(int i = 0; i < Textures.Length; i++)
            {
                Textures[i].Update(SceneManager.IsPlaying, EffectPart.UseTimeScale);
            }

        }
    }
}
