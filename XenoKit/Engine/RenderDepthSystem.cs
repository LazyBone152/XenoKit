using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace XenoKit.Engine
{
    public class RenderDepthSystem : Entity
    {
        private readonly List<Entity> Entities = new List<Entity>();

        public RenderDepthSystem(GameBase game) : base(game)
        {

        }

        public override void Draw()
        {
            foreach(Entity entity in Entities.OrderBy(x => Math.Abs(Vector3.Distance(CameraBase.CameraState.ActualPosition, x.Transform.Translation))).ThenByDescending(x => RenderDepth))
            {
                if(entity.DrawThisFrame)
                    entity.Draw();
            }
        }

        public void Add(Entity entity)
        {
            Entities.Add(entity);
        }

        public void Remove(Entity entity)
        {
            Entities.Remove(entity);
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
