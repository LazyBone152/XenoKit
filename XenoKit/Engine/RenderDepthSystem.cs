using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace XenoKit.Engine
{
    public class RenderDepthSystem : Entity
    {
        private readonly List<Entity> Entities = new List<Entity>();
        private readonly List<Entity> EntitiesToRemove = new List<Entity>();

        public int ActiveParticleCount { get; private set; }
        public int Count => Entities.Count;

        public RenderDepthSystem(GameBase game) : base(game)
        {

        }

        public override void Draw()
        {
            if(EntitiesToRemove.Count > 0)
            {
                foreach (Entity entity in EntitiesToRemove)
                {
                    Entities.Remove(entity);
                }

                EntitiesToRemove.Clear();
            }

            int particleCount = 0;

            foreach(Entity entity in Entities.OrderByDescending(x => Vector3.Distance(CameraBase.CameraState.ActualPosition, x.AbsoluteTransform.Translation)))
            {
                if (entity.DrawThisFrame && entity.AlphaBlendType <= 1)
                {
                    entity.Draw();

                    if (entity.EntityType == EntityType.Particle)
                        particleCount++;
                }
            }

            //Render subtractive blend type last
            foreach (Entity entity in Entities.OrderByDescending(x => Vector3.Distance(CameraBase.CameraState.ActualPosition, x.AbsoluteTransform.Translation)))
            {
                if (entity.DrawThisFrame && entity.AlphaBlendType == 2)
                {
                    entity.Draw();

                    if (entity.EntityType == EntityType.Particle)
                        particleCount++;
                }
            }

            ActiveParticleCount = particleCount;
        }

        public void Add(Entity entity)
        {
            if(entity != null)
                Entities.Add(entity);
        }

        public void Remove(Entity entity)
        {
            EntitiesToRemove.Add(entity);
        }

        public override void Update()
        {
            for (int i = Entities.Count - 1; i >= 0; i--)
            {
                if (Entities[i].IsDestroyed)
                {
                    Entities.RemoveAt(i);
                }
            }
        }
    }
}
