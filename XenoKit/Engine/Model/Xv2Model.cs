using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using XenoKit.Editor;
using XenoKit.Engine.Animation;
using XenoKit.Engine.Collision;
using XenoKit.Engine.Objects;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using XenoKit.Engine.Vertex;
using Xv2CoreLib;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EMG;
using Xv2CoreLib.EMO;
using Xv2CoreLib.NSK;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.EMD.EMD_TextureSamplerDef;
using EmmMaterial = Xv2CoreLib.EMM.EmmMaterial;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdQuaternion = System.Numerics.Quaternion;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Model
{
    public class Xv2ModelFile : RenderObject, IDisposable
    {
        public event EventHandler MaterialsChanged;
        public event ModelModifiedEventHandler ModelModified;

        public NSK_File SourceNskFile { get; private set; }
        public EMD_File SourceEmdFile { get; private set; }
        public EMO_File SourceEmoFile { get; private set; }
        public EMG_File SourceEmgFile { get; private set; }
        public bool IsReflectionMesh { get; private set; }

        public ModelType Type { get; private set; }
        public List<Xv2Model> Models { get; private set; } = new List<Xv2Model>();
        public Xv2Skeleton Skeleton { get; private set; }

        private AabbTree<Xv2Submesh> BVH = null;
        private bool _bvhNeedsUpdate = false;
        private bool _bvhIsUpdating = false;

        private Xv2ModelFile()
        {
        }

        #region Load
        public static Xv2ModelFile LoadEmd(EMD_File emdFile, string name = null)
        {
            //Create EmdModel
            Xv2ModelFile newEmdFile = new Xv2ModelFile();
            newEmdFile.Name = name;
            newEmdFile.Type = ModelType.Emd;
            newEmdFile.SourceEmdFile = emdFile;
            newEmdFile.LoadEmd(true);

            return newEmdFile;
        }

        public static Xv2ModelFile LoadNsk(NSK_File nskFile, string name = null)
        {
            //Create EmdModel
            Xv2ModelFile modelFile = new Xv2ModelFile();
            modelFile.Name = name;
            modelFile.Type = ModelType.Nsk;
            modelFile.SourceNskFile = nskFile;
            modelFile.SourceEmdFile = nskFile.EmdFile;
            modelFile.LoadInternalSkeleton();
            modelFile.LoadEmd(true);

            return modelFile;
        }

        public static Xv2ModelFile LoadEmo(EMO_File emoFile)
        {
            Xv2ModelFile modelFile = new Xv2ModelFile();
            modelFile.Type = ModelType.Emo;
            modelFile.SourceEmoFile = emoFile;
            modelFile.LoadInternalSkeleton();
            modelFile.LoadEmo(true);

            return modelFile;
        }

        /// <summary>
        /// Load the first submesh from an EMG, and ignore samplers and blend weights. This is intended for loading particle EMGs.
        /// </summary>
        public static Xv2Submesh LoadEmg(EMG_File emgFile)
        {
            if (emgFile.EmgMeshes.Count == 0) return null;
            if (emgFile.EmgMeshes[0].SubmeshGroups.Count == 0) return null;
            if (emgFile.EmgMeshes[0].SubmeshGroups[0].Submeshes.Count == 0) return null;

            EMG_Mesh mesh = emgFile.EmgMeshes[0];
            EMG_Submesh submesh = emgFile.EmgMeshes[0].SubmeshGroups[0].Submeshes[0];
            EMG_SubmeshGroup submeshGroup = emgFile.EmgMeshes[0].SubmeshGroups[0];

            Xv2Submesh xv2Submesh = new Xv2Submesh(mesh, submesh, submeshGroup, false, null, null);

            return xv2Submesh;
        }

        public static Xv2ModelFile LoadEmgInContainer(EMG_File emgFile)
        {
            Xv2ModelFile modelFile = new Xv2ModelFile();
            modelFile.Type = ModelType.Emg;
            modelFile.SourceEmgFile = emgFile;
            modelFile.LoadEmg();

            return modelFile;
        }

        private void LoadEmd(bool registerEvent = false, bool reloadAll = true)
        {
            for (int i = Models.Count - 1; i >= 0; i--)
            {
                if (!SourceEmdFile.Models.Exists(Models[i].SourceEmdModel))
                {
                    Models[i].Dispose();
                    Models.RemoveAt(i);
                }
            }

            for (int i = 0; i < SourceEmdFile.Models.Count; i++)
            {
                Xv2Model model = GetModel(SourceEmdFile.Models[i]);

                if (model != null)
                {
                    model.ReloadModel(reloadAll);
                }
                else
                {
                    model = new Xv2Model(SourceEmdFile.Models[i], this);
                    Models.Add(model);
                }
            }

            if (registerEvent)
            {
                SourceEmdFile.ModelModified += SourceEmdFile_ModelModified;
            }

            CalculateAABBs();
        }

        private void LoadEmo(bool registerEvent = false, bool reloadAll = true)
        {
            for (int i = Models.Count - 1; i >= 0; i--)
            {
                if (!SourceEmoFile.ModelExists(Models[i].SourceEmoPart, Models[i].SourceEmgModel))
                {
                    Models[i].Dispose();
                    Models.RemoveAt(i);
                }
            }

            for (int i = 0; i < SourceEmoFile.Parts.Count; i++)
            {
                for(int a = 0; a < SourceEmoFile.Parts[i].EmgFiles.Count; a++)
                {
                    Xv2Model model = GetModel(SourceEmoFile.Parts[i], SourceEmoFile.Parts[i].EmgFiles[a]);

                    if (model != null)
                    {
                        model.ReloadModel(reloadAll);
                    }
                    else
                    {
                        model = new Xv2Model(SourceEmoFile.Parts[i], SourceEmoFile.Parts[i].EmgFiles[a], this);
                        Models.Add(model);
                    }
                }
            }

            CalculateAABBs();

            if (registerEvent)
                SourceEmoFile.ModelModified += SourceEmdFile_ModelModified;
        }

        private void LoadEmg(bool registerEvent = false)
        {
            if(Models.Count == 0)
            {
                Models.Add(new Xv2Model(SourceEmgFile, this));
            }
            else
            {
                Models[0].ReloadModel();
            }

            if(Models.Count > 1)
            {
                Log.Add($"Xv2ModelFile.LoadEmg: more than one model detected", LogType.Debug);
            }

            CalculateAABBs();
        }

        private void LoadInternalSkeleton()
        {
            switch (Type)
            {
                case ModelType.Nsk:
                    Skeleton = new Xv2Skeleton(SourceNskFile.EskFile);
                    break;
                case ModelType.Emo:
                    Skeleton = new Xv2Skeleton(SourceEmoFile.Skeleton);
                    break;
                default:
                    break;
                    throw new InvalidOperationException($"Xv2ModelFile.LoadInternalSkeleton: Model type {Type} does not have a internal skeleton!");
            }
        }

        private void SourceEmdFile_ModelModified(object source, ModelModifiedEventArgs e)
        {
            if(e.EditType == EditTypeEnum.Vertex)
            {
                if (e.Context is IList<object> sourceSubmeshs)
                {
                    foreach (var sourceSubmesh in sourceSubmeshs)
                    {
                        if(sourceSubmesh is EMD_Submesh emdSubmesh)
                        {
                            foreach (var compiledSubmesh in GetSubmeshes(emdSubmesh))
                            {
                                compiledSubmesh.ReloadSubmesh();
                            }
                        }
                        else if (sourceSubmesh is EMG_Mesh emgMesh)
                        {
                            foreach (var compiledSubmesh in GetSubmeshesFromEmgMesh(emgMesh))
                            {
                                compiledSubmesh.ReloadSubmesh();
                            }
                        }

                    }

                    _bvhNeedsUpdate = true;
                }
            }
            else if (e.EditType == EditTypeEnum.Remove || e.EditType == EditTypeEnum.Add)
            {
                if (SourceEmdFile != null)
                {
                    //LoadInternalSkeleton();
                    LoadEmd(false, false);
                }
                else if (SourceEmoFile != null)
                {
                    //LoadInternalSkeleton();
                    LoadEmo(false, false);
                }

                _bvhNeedsUpdate = true;
                MaterialsChanged?.Invoke(this, EventArgs.Empty);
            }
            else if(e.EditType == EditTypeEnum.Sampler)
            {
                if (e.Context is EMD_Submesh emdSubmesh)
                {
                    foreach(var submesh in GetSubmeshes(emdSubmesh))
                    {
                        submesh.InitSamplers();
                    }
                }
                else if (e.Context is EMG_SubmeshGroup emgSubmesh)
                {
                    foreach (var submesh in GetSubmeshesFromEmoGroup(emgSubmesh))
                    {
                        submesh.InitSamplers();
                    }
                }
                else
                {
                    ReinitializeTextureSamplers();
                }
            }
            else if(e.EditType == EditTypeEnum.Material)
            {
                MaterialsChanged?.Invoke(this, EventArgs.Empty);
            }
            else if(e.EditType == EditTypeEnum.Bone)
            {
                if(e.Context is EMD_Mesh emdMesh)
                {
                    Xv2Mesh xv2Mesh = GetMesh(emdMesh);
                    xv2Mesh.SetAttachBone();
                }
                else if (e.Context is EMO_Part emoPart)
                {
                    foreach(var model in GetModelsFromEmoPart(emoPart))
                    {
                        model.SetAttachBone();
                    }
                }
            }

            ModelModified?.Invoke(this, e);
        }

        private void ReinitializeTextureSamplers()
        {
            foreach(var model in Models)
            {
                foreach(var mesh in model.Meshes)
                {
                    foreach(var submesh in mesh.Submeshes)
                    {
                        submesh.InitSamplers();
                    }
                }
            }
        }

        #endregion

        #region Rendering

        public void Draw(Matrix4x4 world, int actor, Xv2ShaderEffect[] materials, Xv2Texture[] textures, Xv2Texture[] dyts, int dytIdx, Xv2Skeleton skeleton = null, ModelInstanceData instanceData = null, bool? glareOutputAllowed = null)
        {
            if (skeleton == null)
                skeleton = Skeleton;

            foreach (Xv2Model model in Models)
            {
                foreach (Xv2Mesh mesh in model.Meshes)
                {
                    foreach (Xv2Submesh submesh in mesh.Submeshes)
                    {
                        submesh.Draw(ref world, actor, materials, textures, dyts, dytIdx, skeleton, instanceData, glareOutputAllowed);
                    }
                }
            }
        
            if(BVH != null)
            {
                //BVH.DebugDraw(world);
            }
        }

        /// <summary>
        /// Draw this model with just one material and no textures or dyts. Intended for earlier passes only (shadow/normals).
        /// </summary>
        public void Draw(Matrix4x4 world, int actor, Xv2ShaderEffect material, Xv2Skeleton skeleton = null, ModelInstanceData instanceData = null)
        {
            if (skeleton == null)
                skeleton = Skeleton;

            foreach (Xv2Model model in Models)
            {
                foreach (Xv2Mesh mesh in model.Meshes)
                {
                    foreach (Xv2Submesh submesh in mesh.Submeshes)
                    {
                        submesh.Draw(ref world, actor, material, skeleton, instanceData);
                    }
                }
            }
        }

        #endregion

        public void SetAsReflectionMesh(bool isReflection)
        {
            IsReflectionMesh = isReflection;

            //Invert normal Y so they are correct in the reflection
            foreach(Xv2Model model in Models)
            {
                foreach(Xv2Mesh mesh in model.Meshes)
                {
                    foreach(Xv2Submesh submesh in mesh.Submeshes)
                    {
                        submesh.SetReflection(isReflection);
                    }
                }
            }
        }

        public void CreateBuffers()
        {
            foreach (Xv2Model model in Models)
            {
                foreach (Xv2Mesh mesh in model.Meshes)
                {
                    foreach (Xv2Submesh submesh in mesh.Submeshes)
                    {
                        submesh.CreateBuffers();
                    }
                }
            }
        }

        public void InitMaterialIndex(Xv2ShaderEffect[] materials)
        {
            foreach (Xv2Model model in Models)
            {
                foreach (Xv2Mesh mesh in model.Meshes)
                {
                    foreach (Xv2Submesh submesh in mesh.Submeshes)
                    {
                        submesh.InitMaterialIndex(materials);
                    }
                }
            }
        }

        #region Helpers
        public Xv2Model GetModel(EMD_Model emdModel)
        {
            for(int i = 0;  i < Models.Count; i++)
            {
                if (Models[i].SourceEmdModel == emdModel) return Models[i];
            }
            return null;
        }

        public Xv2Model GetModel(EMO_Part emoPart, EMG_File emgModel)
        {
            for (int i = 0; i < Models.Count; i++)
            {
                if (Models[i].SourceEmgModel == emgModel && Models[i].SourceEmoPart == emoPart) return Models[i];
            }
            return null;
        }

        public Xv2Model GetModel(object sourceModelObject)
        {
            foreach (var model in Models)
            {
                if (model.IsSourceModel(sourceModelObject)) return model;
            }

            return null;
        }
        
        public IEnumerable<Xv2Model> GetModelsFromEmoPart(EMO_Part emoPart)
        {
            foreach (var model in Models)
            {
                if (model.SourceEmoPart == emoPart) yield return model;
            }
        }

        public Xv2Mesh GetMesh(object sourceMeshObject)
        {
            foreach (var model in Models)
            {
                foreach(var mesh in model.Meshes)
                {
                    if (mesh.IsSourceMesh(sourceMeshObject)) return mesh;
                }
            }

            return null;
        }

        public IEnumerable<Xv2Submesh> GetSubmeshesFromEmoPart(EMO_Part emoPart)
        {
            foreach (var model in Models)
            {
                if(model.SourceEmoPart == emoPart)
                {
                    foreach (var mesh in model.Meshes)
                    {
                        foreach (var submesh in mesh.Submeshes)
                        {
                            yield return submesh;
                        }
                    }
                }
            }
        }

        public IEnumerable<Xv2Submesh> GetSubmeshesFromEmg(EMG_File emgFile)
        {
            foreach (var model in Models)
            {
                if (model.SourceEmgModel == emgFile)
                {
                    foreach (var mesh in model.Meshes)
                    {
                        foreach (var submesh in mesh.Submeshes)
                        {
                            yield return submesh;
                        }
                    }
                }
            }
        }

        public IEnumerable<Xv2Submesh> GetSubmeshesFromEmgMesh(EMG_Mesh emgMesh)
        {
            foreach (var model in Models)
            {
                foreach (var mesh in model.Meshes)
                {
                    if(mesh.SourceEmgMesh == emgMesh)
                    {
                        foreach (var submesh in mesh.Submeshes)
                        {
                            yield return submesh;
                        }
                    }
                }
            }
        }

        public IEnumerable<Xv2Submesh> GetSubmeshesFromEmoGroup(EMG_SubmeshGroup group)
        {
            foreach (var model in Models)
            {
                foreach(var mesh in model.Meshes)
                {
                    foreach(var submesh in mesh.Submeshes)
                    {
                        if(submesh.SourceEmgSubmeshGroup == group) yield return submesh;
                    }
                }
            }
        }

        public IEnumerable<Xv2Submesh> GetSubmeshes(object sourceSubmesh)
        {
            foreach (var model in Models)
            {
                foreach (var mesh in model.Meshes)
                {
                    foreach (var submesh in mesh.Submeshes)
                    {
                        if (submesh.IsSourceSubmesh(sourceSubmesh))
                            yield return submesh;
                    }
                }
            }
        }

        public IEnumerable<Xv2Submesh> GetSubmeshes()
        {
            foreach (var model in Models)
            {
                foreach (var mesh in model.Meshes)
                {
                    foreach (var submesh in mesh.Submeshes)
                    {
                         yield return submesh;
                    }
                }
            }
        }

        public void GetAllSubmeshesFromSourceObjects(IList<object> sourceObjects, List<Xv2Submesh> outputList)
        {
            foreach(var obj in sourceObjects)
            {
                var submeshes = GetAllSubmeshesFromSourceObject(obj);

                if(submeshes != null)
                    outputList.AddRange(submeshes);
            }
        }

        public List<Xv2Submesh> GetAllSubmeshesFromSourceObject(object sourceObject)
        {
            if (sourceObject is EMD_Model model)
            {
                var _model = GetModel(model);
                if(_model != null)
                    return new List<Xv2Submesh>(_model.GetSubmeshes());
            }
            else if (sourceObject is EMD_Mesh mesh)
            {
                var _mesh = GetMesh(mesh);
                if (_mesh != null)
                    return new List<Xv2Submesh>(_mesh.GetSubmeshes());
            }
            else if (sourceObject is EMD_Submesh submesh)
            {
                return new List<Xv2Submesh>(GetSubmeshes(submesh));
            }
            else if(sourceObject is EMO_Part emoPart)
            {
                return new List<Xv2Submesh>(GetSubmeshesFromEmoPart(emoPart));
            }
            else if (sourceObject is EMG_File emgFile)
            {
                return new List<Xv2Submesh>(GetSubmeshesFromEmg(emgFile));
            }
            else if (sourceObject is EMG_Mesh emgMesh)
            {
                return new List<Xv2Submesh>(GetSubmeshesFromEmgMesh(emgMesh));
            }

            return null;
        }
        
        public void CalculateAABBs()
        {
            foreach(var model in Models)
            {
                model.CalculateBounds();
            }
        }
        #endregion

        public void ApplyTransformations(bool addUndoStep = true)
        {

        }

        public void Dispose()
        {
            for (int i = 0; i < Models.Count; i++)
            {
                Models[i].Dispose();
            }
        }
    
        public Xv2Submesh TraverseBVH(Ray ray)
        {
            if(BVH == null || _bvhNeedsUpdate)
            {
                InitBVH();
            }

            if (BVH == null) return null;

            var hits = BVH.Intersects(ray);

            for(int i = 0; i < hits.Count; i++)
            {
                Ray transformedRay = hits[i].Parent.AttachBone != null ? EngineUtils.TransformRay(ray, Matrix.Invert(hits[i].Parent.AttachBone.AbsoluteAnimationMatrix)) : ray;

                if (hits[i].BVH.IntersectsAny(transformedRay))
                    return hits[i];
            }

            return null;
            //return hits.Count > 0 ? hits[0] : null;
            //bool success = BVH.Intersects(ray, out Xv2Submesh hit);
            //return success ? hit : null;
        }
    
        private async void InitBVH()
        {
            if(_bvhIsUpdating) return;
            _bvhIsUpdating = true;
            _bvhNeedsUpdate = false;

            var submeshes = GetSubmeshes().ToArray();

            await Task.Run(() =>
            {
                foreach (Xv2Submesh submesh in submeshes)
                {
                    submesh.InitBVH();
                }

                BVH = new AabbTree<Xv2Submesh>(GetSubmeshes().ToArray());

            });

            _bvhIsUpdating = false;
        }
    }

    public class Xv2Model : RenderObject, IDisposable
    {
        public Xv2Bone AttachBone { get; private set; }

        public List<Xv2Mesh> Meshes { get; private set; } = new List<Xv2Mesh>();

        public Xv2ModelFile Root { get; private set; }
        public EMD_Model SourceEmdModel { get; private set; }
        public EMG_File SourceEmgModel { get; private set; }
        public EMO_Part SourceEmoPart { get; private set; }

        public BoundingBox BoundingBox { get; set; }
        public SimdVector3 BoundingBoxCenter { get; set; }

        public Xv2Model(EMD_Model emdModel, Xv2ModelFile root)
        {
            Name = emdModel.Name;
            SourceEmdModel = emdModel;
            Root = root;
            CreateFromEmd();
        }

        public Xv2Model(EMO_Part emoPart, EMG_File emgModel, Xv2ModelFile root)
        {
            Name = "";
            SourceEmgModel = emgModel;
            SourceEmoPart = emoPart;
            Root = root;
            CreateFromEmg();
        }

        public Xv2Model(EMG_File emg, Xv2ModelFile root)
        {
            Name = "";
            SourceEmgModel = emg;
            Root = root;
            CreateFromEmg();
        }

        public void ReloadModel(bool reloadAll = true)
        {
            if (Root.Type == ModelType.Emd || Root.Type == ModelType.Nsk)
            {
                CreateFromEmd(reloadAll);
            }
            else if (Root.Type == ModelType.Emo || Root.Type == ModelType.Emg)
            {
                CreateFromEmg(reloadAll);
            }
        }

        private void CreateFromEmd(bool reloadAll = true)
        {
            for (int i = Meshes.Count - 1; i >= 0; i--)
            {
                if (!SourceEmdModel.Meshes.Exists(Meshes[i].SourceEmdMesh))
                {
                    Meshes[i].Dispose();
                    Meshes.RemoveAt(i);
                }
            }

            for (int i = 0; i < SourceEmdModel.Meshes.Count; i++)
            {
                Xv2Mesh mesh = GetMesh(SourceEmdModel.Meshes[i]);

                if (mesh != null)
                {
                    mesh.ReloadMesh(reloadAll);
                }
                else
                {
                    mesh = new Xv2Mesh(SourceEmdModel.Meshes[i], this, Root);
                    Meshes.Add(mesh);
                }
            }

            SetAttachBone();
        }

        private void CreateFromEmg(bool reloadAll = true)
        {
            for (int i = Meshes.Count - 1; i >= 0; i--)
            {
                if (!SourceEmgModel.EmgMeshes.Exists(Meshes[i].SourceEmgMesh))
                {
                    Meshes[i].Dispose();
                    Meshes.RemoveAt(i);
                }
            }

            for (int i = 0; i < SourceEmgModel.EmgMeshes.Count; i++)
            {
                Xv2Mesh mesh = GetMesh(SourceEmgModel.EmgMeshes[i]);

                if (mesh != null)
                {
                    mesh.ReloadMesh(reloadAll);
                }
                else
                {
                    mesh = new Xv2Mesh(SourceEmgModel.EmgMeshes[i], this, Root);
                    Meshes.Add(mesh);
                }
            }

            SetAttachBone();
        }

        public void Draw(ref Matrix4x4 world, int actor, Xv2ShaderEffect material, Xv2Skeleton skeleton = null, ModelInstanceData instanceData = null)
        {
            for(int a = 0; a < Meshes.Count; a++)
            {
                for (int i = 0; i < Meshes[a].Submeshes.Count; i++)
                {
                    Meshes[a].Submeshes[i].Draw(ref world, actor, material, skeleton, instanceData);
                }
            }
        }

        #region Helpers
        private Xv2Mesh GetMesh(EMD_Mesh mesh)
        {
            for (int i = 0; i < Meshes.Count; i++)
            {
                if (Meshes[i].IsSourceMesh(mesh))
                    return Meshes[i];
            }

            return null;
        }

        private Xv2Mesh GetMesh(EMG_Mesh mesh)
        {
            for (int i = 0; i < Meshes.Count; i++)
            {
                if (Meshes[i].IsSourceMesh(mesh))
                    return Meshes[i];
            }

            return null;
        }
        
        public bool IsSourceModel(object sourceModel)
        {
            return (((Root.Type == ModelType.Emd || Root.Type == ModelType.Nsk) && SourceEmdModel == sourceModel) ||
                ((Root.Type == ModelType.Emo || Root.Type == ModelType.Emg) && SourceEmgModel == sourceModel));
        }

        public object GetSourceModelObject()
        {
            if (Root.Type == ModelType.Emd || Root.Type == ModelType.Nsk)
                return SourceEmdModel;

            else
                return SourceEmgModel;
        }
        
        public void SetAttachBone()
        {
            if(Root.Type == ModelType.Emo)
            {
                AttachBone = Root.Skeleton.GetBone(SourceEmoPart.LinkedBone);
            }
        }

        private Matrix GetTransformedWorld(ref Matrix world, Xv2Skeleton skeleton)
        {
            /*
            if (AttachBoneIdx != -1 && skeleton != null)
            {
                //This is right and wrong depending on the case... why?
                //When return world = fixes; broken effects, like sparks, but breaks boost auras
                //When skeleton.Bones[AttachBone].SkinningMatrix * world; fixes boost auras, breaks sparks
                //Stages only work properly with AbsoluteMatrix * world

                //return world;
                return skeleton.Bones[AttachBoneIdx].AbsoluteAnimationMatrix * world;
            }
            */
            return world;
        }

        public IEnumerable<Xv2Submesh> GetSubmeshes()
        {
            foreach (var mesh in Meshes)
            {
                foreach (var submesh in mesh.Submeshes)
                {
                    yield return submesh;
                }
            }
        }

        public void CalculateBounds()
        {
            SimdVector3 min = new SimdVector3(float.PositiveInfinity);
            SimdVector3 max = new SimdVector3(float.NegativeInfinity);

            foreach (var mesh in Meshes)
            {
                min = SimdVector3.Min(mesh.BoundingBox.Min.ToNumerics(), min);
                max = SimdVector3.Max(mesh.BoundingBox.Max.ToNumerics(), max);
            }

            BoundingBox = new BoundingBox(min, max);
            BoundingBoxCenter = (min + max) * 0.5f;
        }
        #endregion

        public void Dispose()
        {
            for (int i = 0; i < Meshes.Count; i++)
            {
                Meshes[i].Dispose();
            }
        }
    }

    public class Xv2Mesh : RenderObject, IDisposable
    {
        private Xv2Bone _attachBone = null;
        public Xv2Bone AttachBone => SourceEmgMesh != null ? Parent?.AttachBone : _attachBone;

        public List<Xv2Submesh> Submeshes { get; set; } = new List<Xv2Submesh>();

        public Xv2ModelFile Root { get; private set; }
        public Xv2Model Parent { get; private set; }
        public EMD_Mesh SourceEmdMesh { get; private set; }
        public EMG_Mesh SourceEmgMesh { get; private set; }

        public BoundingBox BoundingBox { get; set; }
        public SimdVector3 BoundingBoxCenter { get; set; }

        public Xv2Mesh(EMD_Mesh sourceEmdMesh, Xv2Model parent, Xv2ModelFile root)
        {
            Name = sourceEmdMesh.Name;
            SourceEmdMesh = sourceEmdMesh;
            Parent = parent;
            Root = root;
            CreateFromEmd();
        }

        public Xv2Mesh(EMG_Mesh sourceEmgMesh, Xv2Model parent, Xv2ModelFile root)
        {
            Name = "";
            SourceEmgMesh = sourceEmgMesh;
            Parent = parent;
            Root = root;
            CreateFromEmg();
        }

        public void ReloadMesh(bool reloadAll = true)
        {
            if (Root.Type == ModelType.Emd || Root.Type == ModelType.Nsk)
            {
                CreateFromEmd(reloadAll);
            }
            else if (Root.Type == ModelType.Emo || Root.Type == ModelType.Emg)
            {
                CreateFromEmg(reloadAll);
            }
        }

        private void CreateFromEmd(bool reloadAll = true)
        {
            //Remove all submeshes that are no longer on the EMD_Mesh object
            //This is for updating the Xv2Mesh when changes are made
            for (int i = Submeshes.Count - 1; i >= 0; i--)
            {
                if (!SourceEmdMesh.Submeshes.Exists(Submeshes[i].SourceEmdSubmesh))
                {
                    Submeshes[i].Dispose();
                    Submeshes.RemoveAt(i);
                }
            }

            for(int i = 0; i < SourceEmdMesh.Submeshes.Count; i++)
            {
                foreach(var emdTriangle in SourceEmdMesh.Submeshes[i].Triangles)
                {
                    Xv2Submesh submesh = GetSubmesh(SourceEmdMesh.Submeshes[i], emdTriangle);

                    if (submesh != null)
                    {
                        if(reloadAll)
                           submesh.ReloadSubmesh();
                    }
                    else
                    {
                        submesh = new Xv2Submesh(SourceEmdMesh.Submeshes[i], emdTriangle, Root.Type == ModelType.Nsk, this, Root);
                        Submeshes.Add(submesh);
                    }
                }
            }

            SetAttachBone();
        }

        private void CreateFromEmg(bool reloadAll = true)
        {
            //Remove all submeshes that are no longer on the EMG_Mesh object
            //This is for updating the Xv2Mesh when changes are made
            for (int i = Submeshes.Count - 1; i >= 0; i--)
            {
                if (!SourceEmgMesh.SubmeshGroups.Exists(Submeshes[i].SourceEmgSubmeshGroup) || !Submeshes[i].SourceEmgSubmeshGroup.Submeshes.Exists(Submeshes[i].SourceEmgSubmesh))
                {
                    Submeshes[i].Dispose();
                    Submeshes.RemoveAt(i);
                }
            }

            for (int i = 0; i < SourceEmgMesh.SubmeshGroups.Count; i++)
            {
                for(int a = 0; a < SourceEmgMesh.SubmeshGroups[i].Submeshes.Count; a++)
                {
                    Xv2Submesh submesh = GetSubmesh(SourceEmgMesh.SubmeshGroups[i].Submeshes[a]);

                    if (submesh != null)
                    {
                        if (reloadAll)
                            submesh.ReloadSubmesh();
                    }
                    else
                    {
                        submesh = new Xv2Submesh(SourceEmgMesh, SourceEmgMesh.SubmeshGroups[i].Submeshes[a], SourceEmgMesh.SubmeshGroups[i], Root.Type == ModelType.Emo, this, Root);
                        Submeshes.Add(submesh);
                    }
                }
            }
        }

        public void SetAttachBone()
        {
            if (Root.Type == ModelType.Nsk)
            {
                int boneIdx = Root.Skeleton.GetBoneIndex(SourceEmdMesh.Name);
                _attachBone = Root.Skeleton.Bones.Length > boneIdx && boneIdx != -1 ? Root.Skeleton.Bones[boneIdx] : null;
            }
        }

        public void Draw(ref Matrix4x4 world, int actor, Xv2ShaderEffect material, Xv2Skeleton skeleton = null, ModelInstanceData instanceData = null)
        {
            for(int i = 0; i < Submeshes.Count; i++)
            {
                Submeshes[i].Draw(ref world, actor, material, skeleton, instanceData);
            }
        }

        #region Helpers
        private Xv2Submesh GetSubmesh(EMD_Submesh submesh, EMD_Triangle emdTriangle)
        {
            for(int i = 0; i < Submeshes.Count; i++)
            {
                if (Submeshes[i].SourceEmdSubmesh == submesh && Submeshes[i].SourceEmdTriangleList == emdTriangle)
                    return Submeshes[i];
            }

            return null;
        }

        private Xv2Submesh GetSubmesh(EMG_Submesh submesh)
        {
            for (int i = 0; i < Submeshes.Count; i++)
            {
                if (Submeshes[i].IsSourceSubmesh(submesh))
                    return Submeshes[i];
            }

            return null;
        }

        public bool IsSourceMesh(object sourceMesh)
        {
            return (((Root.Type == ModelType.Emd || Root.Type == ModelType.Nsk) && SourceEmdMesh == sourceMesh) ||
                ((Root.Type == ModelType.Emo || Root.Type == ModelType.Emg) && SourceEmgMesh == sourceMesh));
        }

        public object GetSourceMeshObject()
        {
            if (Root.Type == ModelType.Emd || Root.Type == ModelType.Nsk)
                return SourceEmdMesh;

            else
                return SourceEmgMesh;
        }

        public List<Xv2Submesh> GetSubmeshes(EMD_Submesh submesh)
        {
            List<Xv2Submesh> submeshes = new List<Xv2Submesh>();

            foreach(var _submesh in Submeshes)
            {
                if(_submesh.SourceEmdSubmesh == submesh)
                {
                    submeshes.Add(_submesh);
                }
            }

            return submeshes;
        }
    
        public IEnumerable<Xv2Submesh> GetSubmeshes()
        {
            foreach (var submesh in Submeshes)
            {
                yield return submesh;
            }
        }
    
        public void CalculateBounds()
        {
            SimdVector3 min = new SimdVector3(float.PositiveInfinity);
            SimdVector3 max = new SimdVector3(float.NegativeInfinity);

            foreach(var submesh in Submeshes)
            {
                min = SimdVector3.Min(submesh.BoundingBox.Min.ToNumerics(), min);
                max = SimdVector3.Max(submesh.BoundingBox.Max.ToNumerics(), max);
            }

            BoundingBox = new BoundingBox(min, max);
            BoundingBoxCenter = (min + max) * 0.5f;
        }
    
        public void UpdateParent(Xv2Model parent)
        {
            Parent = parent;
        }
        #endregion

        public void Dispose()
        {
            for(int i = 0; i < Submeshes.Count; i++)
            {
                Submeshes[i].Dispose();
            }
        }
    }

    public class Xv2Submesh : RenderObject, IDisposable, IBvhNode
    {
        public override EngineObjectTypeEnum EngineObjectType => EngineObjectTypeEnum.Model;

        public int MaterialIndex { get; private set; }
        public ModelType Type { get; private set; }

        //Source:
        public Xv2ModelFile Root { get; private set; }
        public Xv2Mesh Parent { get; private set; }
        public EMD_Submesh SourceEmdSubmesh { get; private set; }
        public EMD_Triangle SourceEmdTriangleList { get; private set; }
        public EMG_SubmeshGroup SourceEmgSubmeshGroup { get; private set; }
        public EMG_Submesh SourceEmgSubmesh { get; private set; }
        public EMG_Mesh SourceEmgMesh { get; private set; }
        public AsyncObservableCollection<EMD_TextureSamplerDef> SamplerDefs { get; set; } //From EMD_Submesh or EMG_Mesh


        //Vertices:
        public VertexBuffer VertexBuffer { get; set; }
        public IndexBuffer IndexBuffer { get; set; }
        public VertexBufferBinding VertexBufferBinding { get; set; }
        public VertexPositionNormalTextureBlend[] Vertices { get; set; }
        public int[] Indices { get; set; }
        private bool IsDisposed = false;
        private bool IsReflection = false;


        //AABB
        public BoundingBox BoundingBox { get; set; }
        public SimdVector3 BoundingBoxCenter { get; set; }
        private readonly DrawableBoundingBox VisibleAABB;

        //Samplers:
        public SamplerInfo[] Samplers { get; set; }
        public float[] TexTile01 { get; private set; } = new float[4];
        public float[] TexTile23 { get; private set; } = new float[4];

        //Custom Color DYT
        private int CustomColrDytCreatedFromIndex = 0;
        private int CustomColorIndex = -1;
        private int CustomColorGroup = -1;
        public Xv2Texture CustomColorDyt = null;

        //Skinning:
        public bool EnableSkinning { get; set; }
        public string[] BoneNames;
        public readonly Dictionary<Xv2Skeleton, short[]> BoneIdx = new Dictionary<Xv2Skeleton, short[]>(); //Bone indices are cached per skeleton instance
        public Matrix4x4[] SkinningMatrices = new Matrix4x4[24];
        private static readonly Matrix4x4[] DefaultSkinningMatrices = new Matrix4x4[24];

        //Actor-Specific Information:
        private readonly Matrix4x4[] PrevWVP = new Matrix4x4[SceneManager.NumActors];

        public AabbTree<TriangleBvhNode> BVH { get; private set; }
        public bool IsBvhDirty { get; set; }

        static Xv2Submesh()
        {
            for (int i = 0; i < 24; i++)
                DefaultSkinningMatrices[i] = Matrix4x4.Identity;
        }

        public Xv2Submesh(EMD_Submesh submesh, EMD_Triangle triangleList, bool isNsk, Xv2Mesh parent, Xv2ModelFile root)
        {
            Name = submesh.Name;
            SourceEmdSubmesh = submesh;
            SourceEmdTriangleList = triangleList;
            Type = isNsk ? ModelType.Nsk : ModelType.Emd;
            Parent = parent;
            Root = root;
            CreateFromEmd();

#if DEBUG
            VisibleAABB = new DrawableBoundingBox();
#endif
        }

        public Xv2Submesh(EMG_Mesh emgMesh, EMG_Submesh emgSubmesh, EMG_SubmeshGroup emgSubmeshGroup, bool isEmo, Xv2Mesh parent, Xv2ModelFile root)
        {
            Name = emgSubmeshGroup.MaterialName;
            SourceEmgMesh = emgMesh;
            SourceEmgSubmesh = emgSubmesh;
            SourceEmgSubmeshGroup = emgSubmeshGroup;
            Root = root;
            Type = isEmo ? ModelType.Emo : ModelType.Emg;
            Parent = parent;
            CreateFromEmg();

#if DEBUG
            VisibleAABB = new DrawableBoundingBox();
#endif
        }

        public void ReloadSubmesh()
        {
            if(Type == ModelType.Emd || Type == ModelType.Nsk)
            {
                CreateFromEmd();
            }
            else if (Type == ModelType.Emo || Type == ModelType.Emg)
            {
                CreateFromEmg();
            }

            IsBvhDirty = true;
        }

        private void CreateFromEmd()
        {
            BoundingBox = SourceEmdSubmesh.AABB.ConvertToBoundingBox();
            BoundingBoxCenter = SourceEmdSubmesh.AABB.GetCenter();

            //Create vertex array
            Vertices = new VertexPositionNormalTextureBlend[SourceEmdSubmesh.VertexCount];

            bool hasTangents = (SourceEmdSubmesh.VertexFlags & VertexFlags.Tangent) != 0;
            bool hasNormals = (SourceEmdSubmesh.VertexFlags & VertexFlags.Normal) != 0;
            bool hasColor = (SourceEmdSubmesh.VertexFlags & VertexFlags.Color) != 0;
            bool hasUv1 = (SourceEmdSubmesh.VertexFlags & VertexFlags.TexUV) != 0;
            bool hasUv2 = (SourceEmdSubmesh.VertexFlags & VertexFlags.Tex2UV) != 0;
            bool hasBlendWeights = (SourceEmdSubmesh.VertexFlags & VertexFlags.BlendWeight) != 0;

            for (int i = 0; i < SourceEmdSubmesh.VertexCount; i++)
            {
                Vertices[i].Position = new Vector3(SourceEmdSubmesh.Vertexes[i].PositionX, SourceEmdSubmesh.Vertexes[i].PositionY, SourceEmdSubmesh.Vertexes[i].PositionZ);

                if (hasTangents)
                    Vertices[i].Tangent = new Vector3(SourceEmdSubmesh.Vertexes[i].TangentX, SourceEmdSubmesh.Vertexes[i].TangentY, SourceEmdSubmesh.Vertexes[i].TangentZ);

                if (hasUv1)
                    Vertices[i].TextureUV0 = new Vector2(SourceEmdSubmesh.Vertexes[i].TextureU, SourceEmdSubmesh.Vertexes[i].TextureV);

                if (hasUv2)
                    Vertices[i].TextureUV1 = new Vector2(SourceEmdSubmesh.Vertexes[i].Texture2U, SourceEmdSubmesh.Vertexes[i].Texture2V);

                if (hasColor)
                {
                    Vertices[i].Color_R = SourceEmdSubmesh.Vertexes[i].ColorR;
                    Vertices[i].Color_G = SourceEmdSubmesh.Vertexes[i].ColorG;
                    Vertices[i].Color_B = SourceEmdSubmesh.Vertexes[i].ColorB;
                    Vertices[i].Color_A = SourceEmdSubmesh.Vertexes[i].ColorA;
                }

                if (hasNormals)
                    Vertices[i].Normal = new Vector3(SourceEmdSubmesh.Vertexes[i].NormalX, SourceEmdSubmesh.Vertexes[i].NormalY, SourceEmdSubmesh.Vertexes[i].NormalZ);

                if (hasBlendWeights)
                {
                    Vertices[i].BlendIndex0 = SourceEmdSubmesh.Vertexes[i].BlendIndexes[0];
                    Vertices[i].BlendIndex1 = SourceEmdSubmesh.Vertexes[i].BlendIndexes[1];
                    Vertices[i].BlendIndex2 = SourceEmdSubmesh.Vertexes[i].BlendIndexes[2];
                    Vertices[i].BlendIndex3 = SourceEmdSubmesh.Vertexes[i].BlendIndexes[3];
                    Vertices[i].BlendWeights = new Vector3(SourceEmdSubmesh.Vertexes[i].BlendWeights[0], SourceEmdSubmesh.Vertexes[i].BlendWeights[1], SourceEmdSubmesh.Vertexes[i].BlendWeights[2]);
                }
            }

            SamplerDefs = SourceEmdSubmesh.TextureSamplerDefs;
            InitSamplers();

            EnableSkinning = hasBlendWeights;
            BoneNames = SourceEmdTriangleList.Bones.ToArray();
            Indices = SourceEmdTriangleList.Faces.ToArray();

            bool wasReflection = IsReflection || Root?.IsReflectionMesh == true;
            IsReflection = false;
            if (!SetReflection(wasReflection))
            {
                CreateBuffers();
            }
        }

        private void CreateFromEmg()
        {
            BoundingBox = SourceEmgMesh.AABB.ConvertToBoundingBox();
            BoundingBoxCenter = SourceEmgMesh.AABB.GetCenter();

            //Triangles
            Indices = ArrayConvert.ConvertToIntArray(SourceEmgSubmesh.Faces);

            //Create vertex array
            Vertices = new VertexPositionNormalTextureBlend[SourceEmgMesh.Vertices.Count];

            bool hasTangents = (SourceEmgMesh.VertexFlags & VertexFlags.Tangent) != 0;
            bool hasNormals = (SourceEmgMesh.VertexFlags & VertexFlags.Normal) != 0;
            bool hasColor = (SourceEmgMesh.VertexFlags & VertexFlags.Color) != 0;
            bool hasUv1 = (SourceEmgMesh.VertexFlags & VertexFlags.TexUV) != 0;
            bool hasUv2 = (SourceEmgMesh.VertexFlags & VertexFlags.Tex2UV) != 0;
            bool hasBlendWeights = (SourceEmgMesh.VertexFlags & VertexFlags.BlendWeight) != 0;

            for (int i = 0; i < SourceEmgMesh.Vertices.Count; i++)
            {
                Vertices[i].Position = new Vector3(SourceEmgMesh.Vertices[i].PositionX, SourceEmgMesh.Vertices[i].PositionY, SourceEmgMesh.Vertices[i].PositionZ);

                if(hasNormals)
                    Vertices[i].Normal = new Vector3(SourceEmgMesh.Vertices[i].NormalX, SourceEmgMesh.Vertices[i].NormalY, SourceEmgMesh.Vertices[i].NormalZ);

                if(hasTangents)
                    Vertices[i].Tangent = new Vector3(SourceEmgMesh.Vertices[i].TangentX, SourceEmgMesh.Vertices[i].TangentY, SourceEmgMesh.Vertices[i].TangentZ);

                if(hasUv1)
                    Vertices[i].TextureUV0 = new Vector2(SourceEmgMesh.Vertices[i].TextureU, SourceEmgMesh.Vertices[i].TextureV);

                if(hasUv2)
                    Vertices[i].TextureUV1 = new Vector2(SourceEmgMesh.Vertices[i].Texture2U, SourceEmgMesh.Vertices[i].Texture2V);

                if (hasColor)
                {
                    Vertices[i].Color_R = SourceEmgMesh.Vertices[i].ColorR;
                    Vertices[i].Color_G = SourceEmgMesh.Vertices[i].ColorG;
                    Vertices[i].Color_B = SourceEmgMesh.Vertices[i].ColorB;
                    Vertices[i].Color_A = SourceEmgMesh.Vertices[i].ColorA;
                }

                if (hasBlendWeights)
                {
                    Vertices[i].BlendIndex0 = SourceEmgMesh.Vertices[i].BlendIndexes[0];
                    Vertices[i].BlendIndex1 = SourceEmgMesh.Vertices[i].BlendIndexes[1];
                    Vertices[i].BlendIndex2 = SourceEmgMesh.Vertices[i].BlendIndexes[2];
                    Vertices[i].BlendIndex3 = SourceEmgMesh.Vertices[i].BlendIndexes[3];
                    Vertices[i].BlendWeights = new Vector3(SourceEmgMesh.Vertices[i].BlendWeights[0], SourceEmgMesh.Vertices[i].BlendWeights[1], SourceEmgMesh.Vertices[i].BlendWeights[2]);
                }
            }

            //Samplers
            SamplerDefs = SourceEmgSubmeshGroup.TextureSamplerDefs;
            InitSamplers();

            EnableSkinning = hasBlendWeights;

            //Generate bone index list
            BoneNames = new string[24];

            if(Type == ModelType.Emo && hasBlendWeights)
            {
                for (short i = 0; i < SourceEmgSubmesh.Bones.Count; i++)
                {
                    BoneNames[i] = Root.SourceEmoFile.Skeleton.Bones[SourceEmgSubmesh.Bones[i]].Name;
                }
            }

            bool wasReflection = IsReflection || Root?.IsReflectionMesh == true;
            IsReflection = false;
            if (!SetReflection(wasReflection))
            {
                CreateBuffers();
            }
        }

        #region Draw / Update 
        public void Draw(ref Matrix4x4 world, int actor, Xv2ShaderEffect[] materials, Xv2Texture[] textures, Xv2Texture[] dyts, int dytIdx, Xv2Skeleton skeleton = null, ModelInstanceData instanceData = null, bool? glareOutputAllowed = null)
        {
            if (materials == null) return;

            Xv2Bone attachBone = Parent.AttachBone;
            Matrix4x4 newWorld = attachBone != null ? Transform * attachBone.AbsoluteAnimationMatrix * world : Transform * world;

            Xv2ShaderEffect material = MaterialIndex != -1 ? materials[MaterialIndex] : (EnableSkinning ? DefaultShaders.VertexColor_W : DefaultShaders.WhiteWireframe);


            if (!RenderSystem.CheckDrawPass(material)) return;

            //Only do frustum culling for non-instanced draws
            if (!FrustumIntersects(newWorld) && instanceData == null)
                return;

            //Handle BCS Colors
            if (dyts != null)
            {
                UpdateCustomColorDyt(actor, dytIdx, dyts, material);

                if (CustomColorDyt != null)
                {
                    GraphicsDevice.Textures[4] = CustomColorDyt.Texture;
                    GraphicsDevice.VertexTextures[4] = CustomColorDyt.Texture;
                }
            }

            material.World = newWorld;
            material.PrevWVP = PrevWVP[actor];

            //Set samplers/textures
            foreach (SamplerInfo sampler in Samplers)
            {
                GraphicsDevice.SamplerStates[sampler.samplerSlot] = sampler.state;
                //GraphicsDevice.VertexSamplerStates[sampler.samplerSlot] = sampler.state;

                //Set textures if index is valid.
                if (sampler.parameter <= textures?.Length - 1 && sampler.parameter >= 0)
                {
                    //GraphicsDevice.VertexTextures[sampler.textureSlot] = textures[sampler.parameter].Texture;
                    GraphicsDevice.Textures[sampler.textureSlot] = textures[sampler.parameter].Texture;
                }
            }

            material.SetTextureTile(TexTile01, TexTile23);

            DrawEnd(actor, material, skeleton, instanceData, glareOutputAllowed);

            //Draw AABBs
            if(SceneManager.BoundingBoxVisible && VisibleAABB != null)
            {
                VisibleAABB.Draw(world, BoundingBox);
            }
        }

        public void Draw(ref Matrix4x4 world, int actor, Xv2ShaderEffect material, Xv2Skeleton skeleton = null, ModelInstanceData instanceData = null)
        {
            //if (!RenderSystem.CheckDrawPass(material)) return;

            Xv2Bone attachBone = Parent.AttachBone;
            Matrix4x4 newWorld = attachBone != null ? Transform * attachBone.AbsoluteAnimationMatrix * world : Transform * world;

            if (!FrustumIntersects(newWorld) && instanceData == null)
                return;

            material.World = newWorld;
            material.PrevWVP = PrevWVP[actor];

            DrawEnd(actor, material, skeleton, instanceData);
        }

        private void DrawEnd(int actor, Xv2ShaderEffect material, Xv2Skeleton skeleton, ModelInstanceData instanceData = null, bool? glareOutputAllowed = null)
        {
            if (IsDisposed) return;
            if(MathHelpers.FloatEquals(material.World.Translation.X, -191.0107) &&
                MathHelpers.FloatEquals(material.World.Translation.Y, -30.51358) &&
                MathHelpers.FloatEquals(material.World.Translation.Z, 161.74263))
            {
                Vector4 test = new Vector4(Vector3.Transform(Viewport.Instance.Camera.CameraState.Position, Matrix.Invert(material.World)), 1f);
                SceneManager.DebugTestValue = test.ToString();
            }
            RenderSystem.MeshDrawCalls++;
            material.ActorSlot = actor;

            if (EnableSkinning && skeleton != null)
            {
                CreateSkinningMatrices(skeleton);
                material.SetSkinningMatrices(SkinningMatrices);
            }
            else
            {
                material.SetSkinningMatrices(DefaultSkinningMatrices);
            }

            //Shader passes and vertex drawing
            foreach (EffectPass pass in material.CurrentTechnique.Passes)
            {
                if (Type == ModelType.Emd)
                    material.SetColorFade(SceneManager.Actors[actor]);

                material.SetVfxLight();

                if (glareOutputAllowed.HasValue)
                    material.SetGlareOutputAllowed(glareOutputAllowed.Value);

                pass.Apply();

                if (!material.shaderProgram.IsHardwareInstanceShader)
                {
                    //GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Vertices, 0, Vertices.Length, Indices, 0, Indices.Length / 3);
                    GraphicsDevice.SetVertexBuffers(VertexBufferBinding);
                    GraphicsDevice.Indices = IndexBuffer;
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexBuffer.IndexCount / 3);
                }
                else
                {
                    //Instanced draw call
                    if (instanceData == null)
                    {
                        GraphicsDevice.SetVertexBuffers(VertexBufferBinding, ModelInstanceData.DefaultInstanceData.InstanceBufferBinding);
                        GraphicsDevice.Indices = IndexBuffer;
                        GraphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexBuffer.IndexCount / 3, ModelInstanceData.DefaultInstanceData.NumInstances);
                    }
                    else
                    {
                        GraphicsDevice.SetVertexBuffers(VertexBufferBinding, instanceData.InstanceBufferBinding);
                        GraphicsDevice.Indices = IndexBuffer;
                        GraphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexBuffer.IndexCount / 3, instanceData.NumInstances);
                    }

                }

            }

            PrevWVP[actor] = material.WVP;
        }

        private bool FrustumIntersects(Matrix world)
        {
            if (Type != ModelType.Nsk) return true; //Only do culling for NSKs right now. EMD and EMO can be animated, which causes problems for calculating the bounding boxes since the final transformation is done in shader code (on GPU), and not known before drawing. Not worth the effort to workaround
#if DEBUG
            if (!SceneManager.FrustumCullEnabled) return true;
#endif
            if (Vector3.Distance(world.Translation, ViewportInstance.Camera.CameraState.Position) < 1f) return true;

            BoundingFrustum frustum = RenderSystem.IsShadowPass ? ViewportInstance.SunLight.LightFrustum : ViewportInstance.Camera.Frustum;

            return frustum.Intersects(BoundingBox.Transform(world));
        }

        private short[] GetBoneIndices(Xv2Skeleton skeleton)
        {
            short[] indices;

            if (BoneIdx.TryGetValue(skeleton, out indices))
                return indices;

            indices = new short[BoneNames.Length];

            for (int i = 0; i < BoneNames.Length; i++)
            {
                indices[i] = (short)skeleton.GetBoneIndex(BoneNames[i]);
            }

            BoneIdx.Add(skeleton, indices);

            return indices;
        }

        private void CreateSkinningMatrices(Xv2Skeleton skeleton)
        {
            short[] baseBoneIndices = GetBoneIndices(skeleton);

            for (int i = 0; i < BoneNames.Length; i++)
            {
                int boneIdx = baseBoneIndices[i];

                if (boneIdx != -1)
                {
                    SkinningMatrices[i] = skeleton.Bones[boneIdx].SkinningMatrix;
                }
                else
                {
                    SkinningMatrices[i] = Matrix4x4.Identity;
                }
            }
        }

        #endregion

        #region CustomColor
        private void UpdateCustomColorDyt(int actor, int dytIdx, Xv2Texture[] Dyts, Xv2ShaderEffect material)
        {
            if ((CustomColrDytCreatedFromIndex != dytIdx || CustomColorDyt == null) && CustomColorIndex != -1 && Dyts.Length > CustomColrDytCreatedFromIndex)
            {
                CustomColorDyt?.Dispose();

                var colors = SceneManager.Actors[actor].CharacterData.BcsFile.File.GetColor(CustomColorGroup, CustomColorIndex);

                if (colors == null)
                {
                    CustomColorDyt = null;
                    return;
                }

                var dyt = Dyts[dytIdx].HardCopy();
                int dytLineBase = (int)(material.Material.DecompiledParameters.MatScale1.X) * 16;

                for (int c = 0; c < 4; c++)
                {
                    //As far as I can tell, Color 2 isn't used by the game... so skip
                    if (c == 1) continue;

                    int dytLine = c * 4;
                    int height = dytLineBase + dytLine;

                    for (int width = 0; width < dyt.EmbEntry.Texture.PixelWidth; width++)
                    {
                        var bcsColor = colors.GetColor(c);
                        Xv2CoreLib.HslColor.HslColor color = new Xv2CoreLib.HslColor.RgbColor(bcsColor).ToHsl();

                        var pixelRgb = dyt.EmbEntry.Texture.GetPixel(width, height + dytLine);
                        Xv2CoreLib.HslColor.HslColor pixelColor = new Xv2CoreLib.HslColor.RgbColor(pixelRgb.R, pixelRgb.G, pixelRgb.B).ToHsl();
                        pixelColor.SetHue(color.Hue);
                        pixelColor.Saturation = color.Saturation;
                        pixelColor.Lightness = (color.Lightness * 0.8f) + (pixelColor.Lightness * 0.2f); //BCS colors only keep 20% of the pixels original lightness

                        var newPixelRgb = pixelColor.ToRgb();

                        //The pixel is colored by a factor of the original color and that defined in the BCS color (e.g: if BCS A is 0, then only the original pixels color is kept, but if its 1, then it should only be the BCS color, or if its somewhere inbetween, then they are merged)
                        float originalFactor = 1f - bcsColor.A;
                        byte r = (byte)((newPixelRgb.R_int * bcsColor.A) + (pixelRgb.R * originalFactor));
                        byte g = (byte)((newPixelRgb.G_int * bcsColor.A) + (pixelRgb.G * originalFactor));
                        byte b = (byte)((newPixelRgb.B_int * bcsColor.A) + (pixelRgb.B * originalFactor));

                        dyt.EmbEntry.Texture.SetPixel(width, height + dytLine, pixelRgb.A, r, g, b);
                        dyt.EmbEntry.Texture.SetPixel(width, height + dytLine + 1, pixelRgb.A, r, g, b);
                        dyt.EmbEntry.Texture.SetPixel(width, height + dytLine + 2, pixelRgb.A, r, g, b);
                        dyt.EmbEntry.Texture.SetPixel(width, height + dytLine + 3, pixelRgb.A, r, g, b);

                        //dyt.EmbEntry.Texture.SetPixel(width, height + dytLine, pixelRgb.A, newPixelRgb.R_int, newPixelRgb.G_int, newPixelRgb.B_int);
                        //dyt.EmbEntry.Texture.SetPixel(width, height + dytLine + 1, pixelRgb.A, newPixelRgb.R_int, newPixelRgb.G_int, newPixelRgb.B_int);
                        //dyt.EmbEntry.Texture.SetPixel(width, height + dytLine + 2, pixelRgb.A, newPixelRgb.R_int, newPixelRgb.G_int, newPixelRgb.B_int);
                        //dyt.EmbEntry.Texture.SetPixel(width, height + dytLine + 3, pixelRgb.A, newPixelRgb.R_int, newPixelRgb.G_int, newPixelRgb.B_int);

                    }
                }

                dyt.EmbEntry.SaveDds(false);
                dyt.IsDirty = true;
                CustomColorDyt = dyt;

                //System.IO.File.WriteAllBytes("OG.dds", Dyts[dytIdx].EmbEntry.Data);
                //System.IO.File.WriteAllBytes("NEW.dds", dyt.EmbEntry.Data);

                CustomColrDytCreatedFromIndex = dytIdx;
            }
        }

        public void ApplyCustomColor(int colorGroup, int colorIndex)
        {
            if (CustomColorGroup != colorGroup || CustomColorIndex != colorIndex)
            {
                CustomColorDyt = null;
                CustomColorIndex = colorIndex;
                CustomColorGroup = colorGroup;
            }
        }

        public void ResetCustomColor()
        {
            CustomColorDyt = null;
            CustomColorGroup = -1;
            CustomColorIndex = -1;
        }
        #endregion

        #region Init
        public void InitBVH()
        {
            if(BVH == null || IsBvhDirty)
            {
                BVH = new AabbTree<TriangleBvhNode>(TriangleBvhNode.GetNodes(Vertices, Indices));
                IsBvhDirty = false;
            }
        }

        public void UpdateParent(Xv2Mesh parent)
        {
            Parent = parent;
        }

        public bool SetReflection(bool isReflection)
        {
            if (IsReflection == isReflection) return false;
            IsReflection = isReflection;

            for (int i = 0; i < Vertices.Length; i++)
            {
                Vertices[i].Normal = new Vector3(Vertices[i].Normal.X, -Vertices[i].Normal.Y, Vertices[i].Normal.Z);
                Vertices[i].Tangent = new Vector3(Vertices[i].Tangent.X, -Vertices[i].Tangent.Y, Vertices[i].Tangent.Z);
            }

            CreateBuffers();
            return true;
        }

        public void CreateBuffers()
        {
            if (VertexBuffer == null)
            {
                VertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalTextureBlend), Vertices.Length, BufferUsage.WriteOnly);
                VertexBufferBinding = new VertexBufferBinding(VertexBuffer, 0, 0);
            }
            VertexBuffer.SetData(Vertices);

            if (IndexBuffer == null)
            {
                IndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, Indices.Length, BufferUsage.WriteOnly);
            }
            IndexBuffer.SetData(Indices);
        }

        public void Dispose()
        {
            IsDisposed = true;

            if (VertexBuffer != null && VertexBuffer.IsDisposed == false)
                VertexBuffer.Dispose();

            if (IndexBuffer != null && IndexBuffer.IsDisposed == false)
                IndexBuffer.Dispose();
        }

        public void InitSamplers()
        {
            if (SamplerDefs == null)
            {
                throw new Exception($"Xv2Submesh.InitSamplers: Samplers have not been set");
            }
            Samplers = new SamplerInfo[SamplerDefs.Count];

            for (int i = 0; i < SamplerDefs.Count; i++)
            {
                Samplers[i].type = SamplerType.Sampler2D; //todo: shadow maps are a different sampler type (sampler_c... SamplerCube?)
                Samplers[i].textureSlot = i;
                Samplers[i].samplerSlot = i;

                Samplers[i].state = new SamplerState();
                Samplers[i].state.AddressU = GetTextureAddressMode(SamplerDefs[i].AddressModeU);
                Samplers[i].state.AddressV = GetTextureAddressMode(SamplerDefs[i].AddressModeV);
                Samplers[i].state.AddressW = TextureAddressMode.Wrap;
                Samplers[i].state.BorderColor = new Color(1, 1, 1, 1);
                Samplers[i].state.Filter = GetTextureFilter(SamplerDefs[i].FilteringMin, SamplerDefs[i].FilteringMag);
                Samplers[i].state.MaxAnisotropy = 1;
                Samplers[i].state.MaxMipLevel = 1;
                Samplers[i].state.FilterMode = TextureFilterMode.Default;
                Samplers[i].name = ShaderManager.GetSamplerName(i);
                Samplers[i].state.Name = Samplers[i].name;
                Samplers[i].parameter = SamplerDefs[i].EmbIndex;
            }
        
            //Set the texture tile parameters. These are used by the vertex shader to apply the correct tiling to the textures
            if(SamplerDefs.Count >= 1)
            {
                TexTile01[0] = SamplerDefs[0].ScaleU;
                TexTile01[1] = SamplerDefs[0].ScaleV;
            }

            if (SamplerDefs.Count >= 2)
            {
                TexTile01[2] = SamplerDefs[1].ScaleU;
                TexTile01[3] = SamplerDefs[1].ScaleV;
            }

            if (SamplerDefs.Count >= 3)
            {
                TexTile23[0] = SamplerDefs[2].ScaleU;
                TexTile23[1] = SamplerDefs[2].ScaleV;
            }

            if (SamplerDefs.Count >= 4)
            {
                TexTile23[2] = SamplerDefs[3].ScaleU;
                TexTile23[3] = SamplerDefs[3].ScaleV;
            }
        }

        public void InitMaterialIndex(Xv2ShaderEffect[] materials)
        {
            MaterialIndex = -1;
            string matName = GetMaterialName();

            for(int i = 0; i < materials.Length; i++)
            {
                if (materials[i].Material.Name.Equals(matName, StringComparison.OrdinalIgnoreCase))
                {
                    MaterialIndex = i;
                    break;
                }
            }
        }

        private string GetMaterialName()
        {
            if(Root.Type == ModelType.Emd || Root.Type == ModelType.Nsk)
            {
                return SourceEmdSubmesh.Name;
            }
            else if (Root.Type == ModelType.Emo || Root.Type == ModelType.Emg)
            {
                return SourceEmgSubmeshGroup.MaterialName;
            }
            else
            {
                return null;
            }
        }

        public void SetLodBias(EmmMaterial material)
        {
            for (int i = 0; i < Samplers.Length; i++)
            {
                //Cant modify sampler after it is bound to the GPU (exception)
                try
                {
                    Samplers[i].state.MipMapLevelOfDetailBias = material != null ? material.DecompiledParameters.GetMipMapLod(i) : 0f;
                }
                catch { }
            }
        }

        private TextureFilter GetTextureFilter(Filtering min, Filtering mag)
        {
            //Mip always linear
            if (min == Filtering.Linear && mag == Filtering.Linear)
            {
                return TextureFilter.Linear;
            }
            else if (min == Filtering.Linear && mag == Filtering.Point)
            {
                return TextureFilter.MinLinearMagPointMipLinear;
            }
            else if (min == Filtering.Point && mag == Filtering.Point)
            {
                return TextureFilter.PointMipLinear;
            }
            else if (min == Filtering.Point && mag == Filtering.Linear)
            {
                return TextureFilter.MinPointMagLinearMipLinear;
            }

            return TextureFilter.Linear;
        }

        private TextureAddressMode GetTextureAddressMode(AddressMode mode)
        {
            switch (mode)
            {
                case AddressMode.Clamp:
                    return TextureAddressMode.Clamp;
                case AddressMode.Mirror:
                    return TextureAddressMode.Mirror;
                case AddressMode.Wrap:
                default:
                    return TextureAddressMode.Wrap;
            }
        }
        #endregion

        public void ApplyTransformationToSource(List<IUndoRedo> undos = null)
        {
            if (Transform.IsIdentity) return;

            if (Root.Type == ModelType.Emd || Root.Type == ModelType.Nsk)
            {
                SourceEmdSubmesh.TransformVertices(Transform, undos);
                Parent.SourceEmdMesh.RecalculateAABB(undos);
            }
            else if (Root.Type == ModelType.Emo || Root.Type == ModelType.Emg)
            {
                SourceEmgMesh.TransformVertices(Transform, undos);
                Parent.SourceEmgMesh.RecalculateAABB(undos);
            }

            Transform = Matrix4x4.Identity;
        }

        public bool IsSourceSubmesh(object sourceSubmesh)
        {
            return (((Type == ModelType.Emd || Type == ModelType.Nsk) && SourceEmdSubmesh == sourceSubmesh) ||
                ((Type == ModelType.Emo || Type == ModelType.Emg) && SourceEmgSubmesh == sourceSubmesh));
        }

        public object GetSourceSubmeshObject()
        {
            if (Type == ModelType.Emd || Type == ModelType.Nsk)
                return SourceEmdSubmesh;

            else
                return SourceEmgSubmeshGroup;
        }

        public static SimdVector3 CalculateCenter(IList<Xv2Submesh> submeshes)
        {
            SimdVector3 min = new SimdVector3(float.PositiveInfinity);
            SimdVector3 max = new SimdVector3(float.NegativeInfinity);

            if (submeshes?.Count > 0)
            {
                foreach (var submesh in submeshes)
                {
                    min = SimdVector3.Min(submesh.BoundingBox.Min.ToNumerics(), min);
                    max = SimdVector3.Max(submesh.BoundingBox.Max.ToNumerics(), max);
                }
            }

            return (min + max) * 0.5f;
        }
        
        public static BoundingBox CalculateBoundingBox(IList<Xv2Submesh> submeshes)
        {
            CalculateAABB(submeshes, out BoundingBox aabb, out _);
            return aabb;
        }

        private static void CalculateAABB(IList<Xv2Submesh> submeshes, out BoundingBox aabb, out SimdVector3 center)
        {
            SimdVector3 min = new SimdVector3(float.PositiveInfinity);
            SimdVector3 max = new SimdVector3(float.NegativeInfinity);

            if (submeshes?.Count > 0)
            {
                foreach (var submesh in submeshes)
                {
                    Matrix world = submesh.Transform;

                    if (submesh.Parent.AttachBone != null)
                    {
                        ;
                        world *= submesh.Parent.AttachBone.AbsoluteAnimationMatrix;
                        BoundingBox transformedBoundingBox = submesh.BoundingBox.Transform(world);
                        min = SimdVector3.Min(transformedBoundingBox.Min.ToNumerics(), min);
                        max = SimdVector3.Max(transformedBoundingBox.Max.ToNumerics(), max);
                    }
                    else
                    {
                        BoundingBox transformedBoundingBox = submesh.BoundingBox.Transform(world);
                        min = SimdVector3.Min(transformedBoundingBox.Min.ToNumerics(), min);
                        max = SimdVector3.Max(transformedBoundingBox.Max.ToNumerics(), max);
                    }
                }
            }

            center = (min + max) * 0.5f;
            aabb = new BoundingBox(min, max);
        }
    
        public BoundingBox GetAABB()
        {
            return Parent.AttachBone != null ? BoundingBox.Transform(Parent.AttachBone.AbsoluteAnimationMatrix) : BoundingBox;
        }

        public Vector3 GetAABBCenter()
        {
            return Parent.AttachBone != null ? Vector3.Transform(BoundingBoxCenter, Parent.AttachBone.AbsoluteAnimationMatrix) : BoundingBoxCenter;
        }
    }

    public enum ModelType
    {
        Emd,
        Nsk,
        Emo,
        Emg
    }
}
