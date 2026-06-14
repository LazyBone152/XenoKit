using System;
using System.Collections.Generic;
using Xv2CoreLib.FMP;

namespace XenoKit.Engine.Stage
{
    public class StageCollisionGroup : EngineObject
    {
        public FMP_CollisionGroup CollisionGroup { get; private set; }
        public List<StageCollider> Colliders = new List<StageCollider>();

        public int CollisionGroupIndex => CollisionGroup.Index;

        public StageCollisionGroup(FMP_CollisionGroup collisionGroup)
        {
            CollisionGroup = collisionGroup;

            foreach(var collidor in CollisionGroup.Colliders)
            {
                Colliders.Add(new StageCollider(collidor));
            }
        }

        public override void Update()
        {
            foreach (var collider in Colliders)
            {
                collider.Update();
            }
        }
    }
}
