using System.Linq;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using Xv2CoreLib.EMM;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Model
{
    public class Lod
    {
        public float Distance { get; set; }
        public Xv2ModelFile Model {  get; set; }
        public EMM_File MaterialFile {  get; set; }

        private readonly Xv2ShaderEffect[] _materials;
        public bool IsGrassSimulation { get; private set; }

        public Lod(float distance, Xv2ModelFile model, EMM_File emmFile)
        {
            Distance = distance;
            Model = model;
            MaterialFile = emmFile;

            if(model != null)
            {
                _materials = Xv2ShaderEffect.LoadMaterials(emmFile, ShaderType.Stage);
                Model.InitMaterialIndex(_materials);
                IsGrassSimulation = emmFile.Materials.Any(x => x.ShaderProgram.Contains("GrassSimulation"));
            }
        }

        public void DrawReflection(Matrix4x4 world, Xv2Texture[] textures, ModelInstanceData instanceData)
        {
            Model?.SetAsReflectionMesh(true);
            Model?.Draw(world, 0, _materials, textures, null, 0, null, instanceData);
        }

        public void Draw(Matrix4x4 world, Xv2Texture[] textures, ModelInstanceData instanceData)
        {
            Model?.Draw(world, 0, _materials, textures, null, 0, null, instanceData);
        }

        public void DrawSimple(Matrix4x4 world, Xv2ShaderEffect material, ModelInstanceData instanceData)
        {
            Model?.Draw(world, 0, material, null, instanceData);
        }
    }
}
