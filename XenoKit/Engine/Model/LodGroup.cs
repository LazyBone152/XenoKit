using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Windows.Documents;
using XenoKit.Engine.Animation;
using XenoKit.Engine.Stage;
using XenoKit.Engine.Textures;
using Xv2CoreLib;
using Xv2CoreLib.EMA;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EMM;
using Xv2CoreLib.FMP;
using Xv2CoreLib.NSK;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Model
{
    public class LodGroup : RenderObject
    {
        public override EngineObjectTypeEnum EngineObjectType => EngineObjectTypeEnum.Stage;

        public StageObject ParentStageObject { get; private set; }
        public FMP_Object ParentObject { get; private set; }
        public List<Lod> LODs { get; private set; } = new List<Lod>();
        public Xv2Texture[] Textures { get; private set; }
        public EMA_File Ema { get; private set; }

        private ModelInstanceTree ModelInstanceTree;

        private int lodIndex = -1;

        public LodGroup (FMP_Visual visual, FMP_Object parentObject, StageObject parent)
        {
            ParentStageObject = parent;
            ParentObject = parentObject;

            string embPath = $"stage/{visual.EmbFile}";
            string emaPath = $"stage/{visual.EmaFile}";

            EMB_TextureFile embFile = FileManager.Instance.LoadFile<EMB_TextureFile>(embPath);
            Textures = Xv2Texture.LoadTextureArray(embFile);

            foreach(var lod in visual.LODs)
            {
                string nskPath = $"stage/{lod.NskFile}";
                string emmPath = $"stage/{lod.EmmFile}";

                if (!string.IsNullOrWhiteSpace(lod.NskFile))
                {
                    NSK_File nskFile = FileManager.Instance.LoadFile<NSK_File>(nskPath);
                    EMM_File emmFile = FileManager.Instance.LoadFile<EMM_File>(emmPath);

                    Xv2ModelFile model = CompiledObjectManager.GetCompiledObject<Xv2ModelFile>(nskFile);

                    LODs.Add(new Lod(lod.Distance, model, emmFile));
                }
                else
                {
                    LODs.Add(new Lod(lod.Distance, null, null));
                }
            }
        
            if(ParentObject.InstanceData != null)
            {
                ModelInstanceTree = new ModelInstanceTree(ParentObject.InstanceData);
            }
        }

        public void DrawReflection(Matrix4x4 world)
        {
            Lod lod = GetCurrentLod();

            if (ModelInstanceTree != null)
            {
                for (int i = 0; i < ModelInstanceTree.InstanceGroups.Length; i++)
                {
                    if (ModelInstanceTree.InstanceGroups[i].FrustumIntersects())
                        lod.DrawReflection(world, Textures, ModelInstanceTree.InstanceGroups[i]);
                }
            }
            else
            {
                lod.DrawReflection(world, Textures, null);
            }
            DrawThisFrame = false;
        }

        public override void Draw()
        {
            Draw(Matrix4x4.Identity);
        }

        public void Draw(Matrix4x4 world)
        {
            Lod lod = GetCurrentLod();

            if(ModelInstanceTree != null)
            {
                for(int i = 0; i < ModelInstanceTree.InstanceGroups.Length; i++)
                {
                    if (ModelInstanceTree.InstanceGroups[i].FrustumIntersects())
                        lod.Draw(world, Textures, ModelInstanceTree.InstanceGroups[i]);
                }
            }
            else
            {
                lod.Draw(world, Textures, null);
            }
            DrawThisFrame = false;
        }

        public void DrawSimple(Matrix4x4 world)
        {
            Lod lod = GetCurrentLod();
            //bool isInstanced = (ParentObject.Flags & ObjectFlags.Instancing) != 0;

            if (ModelInstanceTree != null)
            {
                for (int i = 0; i < ModelInstanceTree.InstanceGroups.Length; i++)
                {
                    if (ModelInstanceTree.InstanceGroups[i].FrustumIntersects())
                        lod.DrawSimple(world, RenderSystem.GI_ShadowModel, ModelInstanceTree.InstanceGroups[i]);
                }
            }
            else
            {
                lod.DrawSimple(world, RenderSystem.ShadowModel, null);
            }
        }

        private Lod GetCurrentLod()
        {
            float distanceFromCamera = SimdVector3.Distance(Camera.CameraState.Position, Transform.Translation);

            //TODO
            return LODs[GetLodIndex(distanceFromCamera)];
        }

        private bool IsLodIndexValid(float distanceFromCamera)
        {
            if (lodIndex > -1 && lodIndex < LODs.Count)
            {
                if (lodIndex > 0)
                {
                    if (distanceFromCamera < LODs[lodIndex].Distance) return false;
                }
                else
                {
                    return LODs[lodIndex].Distance < distanceFromCamera; 
                }
            }

            return false;
        }

        private int GetLodIndex(float distanceFromCamera)
        {
            for (int i = 0; i < LODs.Count; i++)
            {
                if (LODs[i].Distance < distanceFromCamera) return i;
            }

            return 0;
        }

        public void SetAsReflectionMesh(bool isReflection)
        {
            foreach(var lod in LODs)
            {
                lod.Model?.SetAsReflectionMesh(isReflection);
            }
        }
    }
}
