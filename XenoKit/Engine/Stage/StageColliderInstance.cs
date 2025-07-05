using Microsoft.Xna.Framework;
using System.Collections.Generic;
using XenoKit.Engine.Model;
using Xv2CoreLib.FMP;

namespace XenoKit.Engine.Stage
{
    public class StageColliderInstance
    {
        public bool IsEnabled { get; set; }

        public Matrix Transform { get; private set; }

        public FMP_ColliderInstance ColliderInstance { get; private set; }

        public List<StageColliderInstance> ColliderInstances { get; set; } = new List<StageColliderInstance>();

        public StageCollider Collider { get; private set; }

        public StageColliderInstance(FMP_ColliderInstance colliderInstance, StageCollider stageCollider)
        {
            ColliderInstance = colliderInstance;
            Collider = stageCollider;

            for(int i = 0; i < colliderInstance.ColliderInstances.Count; i++)
            {
                ColliderInstances.Add(new StageColliderInstance(colliderInstance.ColliderInstances[i], stageCollider.Colliders[i]));
            }

            Transform = colliderInstance.Matrix != null ? colliderInstance.Matrix.ToMonoMatrix() : Matrix.Identity;
        }

        public void Draw(Matrix world, bool isEnabled)
        {
            Collider.Draw(world * Transform, isEnabled || IsEnabled);

            foreach (var collider in ColliderInstances)
            {
                collider.Draw(world * Transform, isEnabled || IsEnabled);
            }
        }

        public void SetColliderMeshWorld(Matrix world)
        {
            Collider.SetColliderMeshWorld(world * Transform);

            foreach (var collider in ColliderInstances)
            {
                collider.SetColliderMeshWorld(world * Transform);
            }
        }

        public List<CollisionMesh> GetAllCollisionMeshes()
        {
            List<CollisionMesh> meshes = new List<CollisionMesh>();

            meshes.AddRange(Collider.GetAllCollisionMeshes());

            foreach(var collider in ColliderInstances)
            {
                meshes.AddRange(collider.GetAllCollisionMeshes());
            }

            return meshes;
        }
    }
}
