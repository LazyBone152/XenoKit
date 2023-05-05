using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using XenoKit.Engine.View;
using XenoKit.Engine.Objects;

namespace XenoKit.Engine.Animation
{
    public class DebugSkeleton : Entity
    {
        public List<ObjectAxis> listAxis = new List<ObjectAxis>();

        public DebugSkeleton(GameBase gameBase) : base(gameBase)
        {
            
        }

        public void Draw(Xv2Bone[] bones, Xv2Bone[] Bones, Matrix transform)
        {
            for(int i = listAxis.Count, nb = bones.Length; i < nb; i++)
                listAxis.Add(new ObjectAxis(false, GameBase));
            
            for (int i = 0, nb = bones.Length; i < nb; i++)
            {
                if (!SceneManager.ResolveLeftHandSymetry)
                    listAxis.ElementAt(i).Draw(bones[i].AbsoluteAnimationMatrix * transform, Bones[i].Name.IndexOf("_L_") != -1);
                else
                    listAxis.ElementAt(i).Draw(bones[i].AbsoluteAnimationMatrix * transform * Matrix.CreateScale(-1, 1f, 1f), Bones[i].Name.IndexOf("_L_") != -1);
            }
        }
    }
}
