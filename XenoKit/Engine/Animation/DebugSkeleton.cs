using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.View;
using XenoKit.Engine.Objects;
using XenoKit.Editor;

namespace XenoKit.Engine.Animation
{
    public class DebugSkeleton
    {
        public List<ObjectAxis> listAxis = new List<ObjectAxis>();

        public DebugSkeleton()
        {
            
        }

        public void Draw(Matrix[] boneMatrices, Bone[] Bones, GraphicsDevice graphicsDevice, Camera camera, Matrix transform)
        {
            for(int i = listAxis.Count, nb = boneMatrices.Length; i < nb; i++)
                listAxis.Add(new ObjectAxis(graphicsDevice));
            
            for (int i = 0, nb = boneMatrices.Length; i < nb; i++)
            {
                if (!SceneManager.ResolveLeftHandSymetry)
                    listAxis.ElementAt(i).Draw(graphicsDevice, camera, boneMatrices[i] * transform, Bones[i].Name.IndexOf("_L_") != -1);
                else
                    listAxis.ElementAt(i).Draw(graphicsDevice, camera, (boneMatrices[i] * transform) * Matrix.CreateScale(-1, 1f, 1f), Bones[i].Name.IndexOf("_L_") != -1);
            }
        }
    }
}
