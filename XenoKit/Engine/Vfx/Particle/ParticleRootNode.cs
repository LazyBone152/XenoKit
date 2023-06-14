using Microsoft.Xna.Framework;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMP_NEW;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticleRootNode : ParticleNodeBase
    {
        private EMP_File EmpFile;
        protected override bool IsRootNode => true;

        public ParticleRootNode(EMP_File empFile, ParticleSystem particleSystem, EffectPart effectPart, GameBase gameBase) : base(Matrix.Identity, particleSystem, effectPart, gameBase)
        {
            EmpFile = empFile;
            Node.ChildParticleNodes = EmpFile.ParticleNodes;
        }

        public void Play()
        {
            State = NodeState.Active;
            Nodes.Clear();
            Emit();
        }

        public void Stop()
        {
            State = NodeState.Expired;
            Nodes.Clear();
        }

        public override void Update()
        {
            UpdateChildrenNodes();

            if(Nodes.Count == 0)
            {
                State = NodeState.Expired;
            }

            ActiveInstancesUpdatedThisFrame = false;
        }

    }
}
