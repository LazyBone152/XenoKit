using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XenoKit.Controls;

namespace XenoKit.Engine.Animation
{
    public interface ISkinned
    {
        AnimationTabView AnimationViewInstance { get; }
        Xv2Skeleton Skeleton { get; set; }
        AnimationPlayer AnimationPlayer { get; set; }

        Matrix GetAbsoluteBoneMatrix(int boneIdx);
    }
}
