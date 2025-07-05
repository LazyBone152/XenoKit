using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using XenoKit.Engine.Model;
using Xv2CoreLib.FMP;
using Xv2CoreLib.Havok;

namespace XenoKit.Engine.Stage
{
    public class StageCollider : Entity
    {
        public FMP_Collider Collider { get; set; }

        public List<StageCollider> Colliders = new List<StageCollider>();

        private List<CollisionMesh> CollisionMeshes = new List<CollisionMesh>();

        public StageCollider(FMP_Collider collider, GameBase game) : base(game)
        {
            Collider = collider;

            foreach (var collidor in collider.Colliders)
            {
                Colliders.Add(new StageCollider(collidor, game));
            }

            CreateMeshes();
        }

        private void CreateMeshes()
        {
            CollisionMeshes.Clear();

            if (Collider.CollisionVertexData.HasData())
            {
                //CollisionMeshes.Add(new CollisionMesh(GameBase, Collider.CollisionVertexData, Color.Green));
            }

            foreach(var havokCol in Collider.HavokColliders)
            {
                if(havokCol.HvkFile?.Length > 0)
                {
                    HavokTagFile havokFile = HavokTagFile.Load(havokCol.HvkFile);

                    if(havokFile.RootObject.TName == "hknpExternMeshShape")
                    {
                        var extractedMesh = havokFile.ExtractMesh();

                        if(extractedMesh != null && extractedMesh?.HasData() == true)
                        {
                            //CollisionMeshes.Add(new CollisionMesh(GameBase, extractedMesh, Color.Purple));
                        }
                    }
                    else if (havokFile.IsConvexMesh())
                    {
                        var convexVertices = havokFile.ExtractConvexPoints();

                        if(convexVertices.Length > 0)
                        {
                            //CollisionMeshes.Add(new CollisionMesh(GameBase, convexVertices, Color.Red));
                        }
                    }
                }
            }
        }

        public void Draw(Matrix world, bool isEnabled)
        {
            foreach(var mesh in CollisionMeshes)
            {
                //mesh.Draw(world);
            }

            //foreach (var stage in Colliders)
            {
                //Not needed since the colliders are drawn through the ColliderInstance tree
                //stage.Draw(world);
            }
        }

        public void SetColliderMeshWorld(Matrix world)
        {
            foreach(var stage in Colliders)
            {
                stage.SetColliderMeshWorld(world);
            }

            foreach(var mesh in CollisionMeshes)
            {
                mesh.SetColliderMeshWorld(world);
            }
        }

        public List<CollisionMesh> GetAllCollisionMeshes()
        {
            return CollisionMeshes;
        }
    }
}
