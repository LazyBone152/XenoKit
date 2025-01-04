using System.Collections.Generic;
using Microsoft.Xna.Framework;
using XenoKit.Engine.Vfx.Asset;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.ETR;

namespace XenoKit.Engine.Vfx.Trace
{
    public class VfxTrace : VfxAsset
    {
        public ETR_File EtrFile { get; set; }
        public bool IsSimulating { get; set; }

        private float PreviousFrame = 0f;
        public float CurrentFrameDelta { get; private set; }

        private bool IsDirty { get; set; }

        private readonly List<TraceNode> Nodes = new List<TraceNode>();

        public VfxTrace(Matrix startWorld, Actor actor, EffectPart effectPart, ETR_File etrFile, GameBase gameBase) : base(startWorld, effectPart, actor, gameBase)
        {
            EtrFile = etrFile;
            InitializeTraceEffect();
        }

        private void InitializeTraceEffect()
        {
            Nodes.Clear();

            foreach (ETR_Node node in EtrFile.Nodes)
            {
                Nodes.Add(GameBase.ObjectPoolManager.GetTraceNode(EtrFile, node, this));
            }
        }

        public override void Update()
        {
            base.Update();

            if (!HasStarted) return;

            //Calculate frame delta (change in frames).
            CurrentFrameDelta = CurrentFrame - PreviousFrame;

            if (IsDirty)
            {
                RestartTraceEffect();
            }

            foreach(TraceNode node in Nodes)
            {
                node.Update();
            }

            for (int i = Nodes.Count - 1; i >= 0; i--)
            {
                if (Nodes[i].TraceState == ExtrudeState.Expired)
                {
                    Nodes[i].Release();
                    Nodes.RemoveAt(i);
                }
            }

            PreviousFrame = CurrentFrame;

            if (GameBase.IsPlaying)
                CurrentFrame += EffectPart.UseTimeScale ? Actor.ActiveTimeScale : 1f;
        }
    
        public void RestartTraceEffect()
        {
            PreviousFrame = 0;
            CurrentFrame = 0;

            foreach(TraceNode node in Nodes)
            {
                node.Release();
            }

            InitializeTraceEffect();
        }
    }
}
