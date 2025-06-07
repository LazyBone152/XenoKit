using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XenoKit.Engine.Model;

namespace XenoKit.Engine.Stage
{
    public class StageVisual
    {
        public LodGroup LodGroup { get; set; }

        public void Draw(Matrix world)
        {
            LodGroup.Draw(world);
        }

        public void DrawSimple(Matrix world)
        {
            LodGroup.DrawSimple(world);
        }
    }
}
