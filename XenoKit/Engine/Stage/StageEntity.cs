using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XenoKit.Engine.Stage
{
    public class StageEntity
    {
        public Matrix Transform { get; set; }
        public StageVisual Visual { get; set; }

        public void Draw(Matrix world)
        {
            Visual?.Draw(world * Transform);
        }

        public void DrawSimple(Matrix world)
        {
            Visual?.DrawSimple(world * Transform);
        }
    }
}
