using System;
using System.Collections.Generic;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Vfx.Particle;
using Xv2CoreLib.EMP_NEW;

namespace XenoKit.Engine.Rendering
{
    public class ParticleBatcher : EngineObject
    {
        private readonly List<ParticleBatch> _batches = new List<ParticleBatch>();

        private int _totalBatchedParticles = 0;
        public int NumBatches => _batches.Count;
        public int NumTotalBatched => _totalBatchedParticles;

        public ParticleBatcher()
        {

        }

        public void SlowUpdate()
        {
            for (int i = _batches.Count - 1; i >= 0; i--)
            {
                if (_batches[i].MaxBatchSinceLastSlowUpdate > 0)
                {
                    _batches[i].SlowUpdate();
                }
                else
                {
                    _batches[i].Destroy();
                    _batches.RemoveAt(i);
                }
            }

        }

        public override void Update()
        {
            for (int i = 0; i < _batches.Count; i++)
            {
                _batches[i].Update();
            }
        }

        public void Draw(int lowRez)
        {
            if(lowRez == 0)
                _totalBatchedParticles = 0;

            for (int i = 0; i < _batches.Count; i++)
            {
                if (_batches[i].LowRezMode == lowRez)
                {
                    _totalBatchedParticles += _batches[i].NumBatch;
                    _batches[i].Draw();
                }
            }
        }

        public ParticleBatch GetBatch(ParticleNode particleNode, bool noGlare)
        {
            for(int i = 0; i < _batches.Count; i++)
            {
                if (_batches[i].ParticleNode == particleNode && _batches[i].NoGlare == noGlare)
                    return _batches[i];
            }

            //Create batch and return it
            ParticleBatch batch = new ParticleBatch(CompiledObjectManager.GetCompiledObject<ParticleEmissionData>(particleNode), particleNode, noGlare);
            _batches.Add(batch);
            return batch;
        }
    }
}
