using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Windows.Documents;
using XenoKit.Engine.Animation;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using Xv2CoreLib.EMM;

namespace XenoKit.Engine.Model
{
    public class Lod
    {
        public float Distance { get; set; }
        public Xv2Skeleton Skeleton { get; set; }
        public Xv2ModelFile Model {  get; set; }
        public EMM_File MaterialFile {  get; set; }

        private readonly List<Xv2ShaderEffect> _materials;

        public Lod(float distance, Xv2ModelFile model, Xv2Skeleton skeleton, EMM_File emmFile)
        {
            Skeleton = skeleton;
            Distance = distance;
            Model = model;
            MaterialFile = emmFile;

            if(model != null)
                _materials = model.InitializeMaterials(ShaderType.Stage, emmFile);
        }

        public void Draw(Matrix world, Xv2Texture[] textures)
        {
            Model?.Draw(world, 0, _materials, textures, null, 0, Skeleton);
        }

        public void DrawSimple(Matrix world, Xv2ShaderEffect material)
        {
            Model?.Draw(world, 0, material, Skeleton);
        }
    }
}
