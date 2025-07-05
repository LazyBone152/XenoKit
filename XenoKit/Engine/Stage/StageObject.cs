using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XenoKit.Engine.Model;
using Xv2CoreLib.FMP;

namespace XenoKit.Engine.Stage
{
    public class StageObject
    {
        public FMP_Object Object { get; set; }
        public Matrix Transform { get; set; }
        public List<StageEntity> Entities { get; set; } = new List<StageEntity>();
        public List<StageColliderInstance> ColliderInstances { get; set; } = new List<StageColliderInstance>();

        public void Draw()
        {
            if (SceneManager.StageGeometryVisible)
            {
                foreach (var entity in Entities)
                {
                    entity.Draw(Transform);
                }
            }

            if (SceneManager.CollisionMeshVisible)
            {
                foreach(var collider in ColliderInstances)
                {
                    collider.Draw(Transform, true);
                }
            }
        }

        public void DrawSimple()
        {
            foreach (var entity in Entities)
            {
                entity.DrawSimple(Transform);
            }
        }

        public void SetColliderMeshWorld()
        {
            foreach(var collider in ColliderInstances)
            {
                collider.SetColliderMeshWorld(Transform);
            }
        }

        public List<CollisionMesh> GetAllCollisionMeshes()
        {
            List<CollisionMesh> meshes = new List<CollisionMesh>();

            foreach(var collider in ColliderInstances)
            {
                meshes.AddRange(collider.GetAllCollisionMeshes());
            }

            return meshes;
        }

        public override string ToString()
        {
            return Object.Name;
        }
    }
}
