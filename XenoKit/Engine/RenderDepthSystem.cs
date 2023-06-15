using System.Collections.Generic;
using System.Linq;

namespace XenoKit.Engine
{
    public class RenderDepthSystem
    {
        private List<Entity> Entities = new List<Entity>();

        public void DrawBefore()
        {
            foreach(Entity entity in Entities.OrderByDescending(x => x.RenderDepth >= 0f))
            {
                entity.Draw();
            }
        }

        public void DrawAfter()
        {
            foreach (Entity entity in Entities.OrderByDescending(x => x.RenderDepth < 0f))
            {
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

        public void Update()
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
