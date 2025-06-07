using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XenoKit.Engine.Stage
{
    public class StageObject
    {
        public Matrix Transform { get; set; }
        public List<StageEntity> Entities { get; set; } = new List<StageEntity>();

        public void Draw()
        {
            foreach(var entity in Entities)
            {
                entity.Draw(Transform);
            }
        }

        public void DrawSimple()
        {
            foreach (var entity in Entities)
            {
                entity.DrawSimple(Transform);
            }
        }
    }
}
