using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using XenoKit.Engine.Animation;
using XenoKit.Engine.Vertex;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using Xv2CoreLib.EMO;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EMM;
using EmmMaterial = Xv2CoreLib.EMM.EmmMaterial;
using static Xv2CoreLib.EMD.EMD_TextureSamplerDef;
using System;
using Xv2CoreLib.EMG;
using System.Linq;
using Xv2CoreLib.BCS;
using System.Windows.Media.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Xv2CoreLib.NSK;

namespace XenoKit.Engine.Model
{
    public class Xv2ModelFile : Entity
    {
        public EventHandler ModelChanged;
        public NSK_File SourceNskFile = null;
        public EMD_File SourceEmdFile = null;
        public EMO_File SourceEmoFile = null;

        public ModelType Type;
        public List<Xv2Model> Models { get; set; } = new List<Xv2Model>();

        public Xv2ModelFile(GameBase gameBase) : base(gameBase)
        {
        }

        #region Load
        public static Xv2ModelFile LoadEmd(GameBase gameBase, EMD_File emdFile, string name = null)
        {
            //Create EmdModel
            Xv2ModelFile newEmdFile = new Xv2ModelFile(gameBase);
            newEmdFile.Name = name;
            newEmdFile.Type = ModelType.Emd;
            newEmdFile.SourceEmdFile = emdFile;
            newEmdFile.LoadEmd(true);

            return newEmdFile;
        }

        public static Xv2ModelFile LoadNsk(GameBase gameBase, NSK_File nskFile, string name = null)
        {
            //Create EmdModel
            Xv2ModelFile newEmdFile = new Xv2ModelFile(gameBase);
            newEmdFile.Name = name;
            newEmdFile.Type = ModelType.Nsk;
            newEmdFile.SourceNskFile = nskFile;
            newEmdFile.SourceEmdFile = nskFile.EmdFile;
            newEmdFile.LoadEmd(true);

            return newEmdFile;
        }

        public static Xv2ModelFile LoadEmo(GameBase gameBase, EMO_File emoFile)
        {
            Xv2ModelFile modelFile = new Xv2ModelFile(gameBase);
            modelFile.Type = ModelType.Emo;
            modelFile.SourceEmoFile = emoFile;
            modelFile.LoadEmo(true);

            return modelFile;
        }

        /// <summary>
        /// Creates a materials list to use for renderering. Materials are indexed by the submesh index. 
        /// </summary>
        public List<Xv2ShaderEffect> InitializeMaterials(EMM_File emmFile = null)
        {
            List<Xv2ShaderEffect> materials = new List<Xv2ShaderEffect>();

            foreach (Xv2Model model in Models)
            {
                foreach (Xv2Mesh mesh in model.Meshes)
                {
                    foreach (Xv2Submesh submesh in mesh.Submeshes)
                    {
                        EmmMaterial material = emmFile != null ? emmFile.GetMaterial(submesh.Name) : null;
                        //submesh.SetLodBias(material); //Slow

                        Xv2ShaderEffect compiledMat = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(material, GameBase);

                        if (compiledMat == null)
                        {
                            //No material was found for this Submesh. Use default.
                            compiledMat = Xv2ShaderEffect.CreateDefaultMaterial(GameBase);
                        }

                        materials.Add(compiledMat);
                    }
                }
            }

            return materials;
        }

        private void LoadEmd(bool registerEvent = false)
        {
            Models.Clear();

            //Submesh index is used for material linkage
            int submeshIndex = 0;

            //Models
            foreach (var emdModel in SourceEmdFile.Models)
            {
                Xv2Model model = new Xv2Model(emdModel.Name, GameBase);

                //NSK files support model-level animation like EMOs
                if(Type == ModelType.Nsk)
                    model.AttachBone = SourceNskFile.EskFile.Skeleton.NonRecursiveBones.IndexOf(SourceNskFile.EskFile.Skeleton.NonRecursiveBones.FirstOrDefault(x => x.Name == emdModel.Name));

                foreach (var emdMesh in emdModel.Meshes)
                {
                    Xv2Mesh mesh = new Xv2Mesh(emdMesh.Name, GameBase);

                    foreach (var emdSubmesh in emdMesh.Submeshes)
                    {
                        Xv2Submesh submesh = new Xv2Submesh(GameBase, emdSubmesh.Name, submeshIndex, Type, emdSubmesh);

                        List<string> bones = new List<string>();

                        //Triangles
                        submesh.Indices = new short[emdSubmesh.TriangleCount];
                        int indexPos = 0;

                        foreach (var triangeList in emdSubmesh.Triangles)
                        {
                            //Merge the face indices into a single array. 
                            for (int i = 0; i < triangeList.FaceCount; i += 3)
                            {
                                submesh.Indices[indexPos + 0] = (short)triangeList.Faces[i + 0];
                                submesh.Indices[indexPos + 1] = (short)triangeList.Faces[i + 1];
                                submesh.Indices[indexPos + 2] = (short)triangeList.Faces[i + 2];
                                indexPos += 3;
                            }
                        }

                        //Create vertex array
                        submesh.CpuVertexes = new VertexPositionNormalTextureBlend[emdSubmesh.VertexCount];
                        submesh.GpuVertexes = new VertexPositionNormalTextureBlend[emdSubmesh.VertexCount];

                        for (int i = 0; i < emdSubmesh.VertexCount; i++)
                        {
                            submesh.CpuVertexes[i].Position = new Vector3(emdSubmesh.Vertexes[i].PositionX, emdSubmesh.Vertexes[i].PositionY, emdSubmesh.Vertexes[i].PositionZ);
                            submesh.CpuVertexes[i].Tangent = new Vector3(emdSubmesh.Vertexes[i].TangentX, emdSubmesh.Vertexes[i].TangentY, emdSubmesh.Vertexes[i].TangentZ);
                            submesh.CpuVertexes[i].TextureUV0 = new Vector2(emdSubmesh.Vertexes[i].TextureU, emdSubmesh.Vertexes[i].TextureV);
                            submesh.CpuVertexes[i].TextureUV1 = new Vector2(emdSubmesh.Vertexes[i].Texture2U, emdSubmesh.Vertexes[i].Texture2V);
                            submesh.CpuVertexes[i].Color_R = emdSubmesh.Vertexes[i].ColorR;
                            submesh.CpuVertexes[i].Color_G = emdSubmesh.Vertexes[i].ColorG;
                            submesh.CpuVertexes[i].Color_B = emdSubmesh.Vertexes[i].ColorB;
                            submesh.CpuVertexes[i].Color_A = emdSubmesh.Vertexes[i].ColorA;

                            if (emdSubmesh.VertexFlags.HasFlag(VertexFlags.Normal))
                            {
                                submesh.CpuVertexes[i].Normal = new Vector3(emdSubmesh.Vertexes[i].NormalX, emdSubmesh.Vertexes[i].NormalY, emdSubmesh.Vertexes[i].NormalZ);
                            }

                            if (emdSubmesh.VertexFlags.HasFlag(VertexFlags.BlendWeight))
                            {
                                submesh.CpuVertexes[i].BlendIndex0 = GetMergedBoneIndex(i, emdSubmesh.Vertexes[i].BlendIndexes[0], emdSubmesh, bones);
                                submesh.CpuVertexes[i].BlendIndex1 = GetMergedBoneIndex(i, emdSubmesh.Vertexes[i].BlendIndexes[1], emdSubmesh, bones);
                                submesh.CpuVertexes[i].BlendIndex2 = GetMergedBoneIndex(i, emdSubmesh.Vertexes[i].BlendIndexes[2], emdSubmesh, bones);
                                submesh.CpuVertexes[i].BlendIndex3 = GetMergedBoneIndex(i, emdSubmesh.Vertexes[i].BlendIndexes[3], emdSubmesh, bones);
                                //submesh.CpuVertexes[i].BlendWeights = new Vector4(emdSubmesh.Vertexes[i].BlendWeights[0], emdSubmesh.Vertexes[i].BlendWeights[1], emdSubmesh.Vertexes[i].BlendWeights[2], emdSubmesh.Vertexes[i].BlendWeights[3]);
                                submesh.CpuVertexes[i].BlendWeights = new Vector3(emdSubmesh.Vertexes[i].BlendWeights[0], emdSubmesh.Vertexes[i].BlendWeights[1], emdSubmesh.Vertexes[i].BlendWeights[2]);
                            }

                            //GpuVertexes (for rendering)
                            submesh.GpuVertexes[i] = submesh.CpuVertexes[i];
                        }

                        submesh.InitSamplers(emdSubmesh.TextureSamplerDefs);

                        submesh.EnableSkinning = emdSubmesh.VertexFlags.HasFlag(VertexFlags.BlendWeight);
                        submesh.BoneNames = bones.ToArray();

                        if (submesh.CpuVertexes.Length > 0)
                        {
                            mesh.Submeshes.Add(submesh);
                            submeshIndex++;
                        }
                    }

                    model.Meshes.Add(mesh);
                }

                Models.Add(model);
            }

            if (registerEvent)
            {
                SourceEmdFile.PropertyChanged += SourceFileChanged_Event;
            }

        }

        private void LoadEmo(bool registerEvent = false)
        {
            int submeshIndex = 0;
            int partIdx = 0;

            //Models are rendered in the OPPOSITE order of which they are defined in EMO, so we have to read this backwards
            for (int a = SourceEmoFile.Parts.Count - 1; a >= 0; a--)
            {
                foreach (var model in SourceEmoFile.Parts[a].EmgFiles)
                {
                    Xv2Model xv2Model = new Xv2Model(SourceEmoFile.Parts[a].Name, GameBase);
                    xv2Model.AttachBone = SourceEmoFile.Skeleton.Bones.IndexOf(SourceEmoFile.Skeleton.Bones.FirstOrDefault(x => x.EmoPartIndex == partIdx));

                    foreach (var mesh in model.EmgMeshes)
                    {
                        Xv2Mesh xv2Mesh = new Xv2Mesh("", GameBase);

                        foreach (var submesh in mesh.Submeshes)
                        {
                            Xv2Submesh xv2Submesh = new Xv2Submesh(GameBase, submesh.MaterialName, submeshIndex, Type, submesh);

                            //Triangles
                            xv2Submesh.Indices = submesh.Faces.Copy();

                            //Create vertex array
                            xv2Submesh.CpuVertexes = new VertexPositionNormalTextureBlend[mesh.Vertices.Count];
                            xv2Submesh.GpuVertexes = new VertexPositionNormalTextureBlend[mesh.Vertices.Count];

                            for (int i = 0; i < mesh.Vertices.Count; i++)
                            {
                                xv2Submesh.CpuVertexes[i].Position = new Vector3(mesh.Vertices[i].PositionX, mesh.Vertices[i].PositionY, mesh.Vertices[i].PositionZ);
                                xv2Submesh.CpuVertexes[i].Normal = new Vector3(mesh.Vertices[i].NormalX, mesh.Vertices[i].NormalY, mesh.Vertices[i].NormalZ);
                                xv2Submesh.CpuVertexes[i].Tangent = new Vector3(mesh.Vertices[i].TangentX, mesh.Vertices[i].TangentY, mesh.Vertices[i].TangentZ);
                                xv2Submesh.CpuVertexes[i].TextureUV0 = new Vector2(mesh.Vertices[i].TextureU, mesh.Vertices[i].TextureV);
                                xv2Submesh.CpuVertexes[i].TextureUV1 = new Vector2(mesh.Vertices[i].Texture2U, mesh.Vertices[i].Texture2V);
                                xv2Submesh.CpuVertexes[i].Color_R = mesh.Vertices[i].ColorR;
                                xv2Submesh.CpuVertexes[i].Color_G = mesh.Vertices[i].ColorG;
                                xv2Submesh.CpuVertexes[i].Color_B = mesh.Vertices[i].ColorB;
                                xv2Submesh.CpuVertexes[i].Color_A = mesh.Vertices[i].ColorA;

                                if (mesh.VertexFlags.HasFlag(VertexFlags.BlendWeight))
                                {
                                    xv2Submesh.CpuVertexes[i].BlendIndex0 = GetMergedBoneIndex(i, mesh.Vertices[i].BlendIndexes[0], submesh);
                                    xv2Submesh.CpuVertexes[i].BlendIndex1 = GetMergedBoneIndex(i, mesh.Vertices[i].BlendIndexes[1], submesh);
                                    xv2Submesh.CpuVertexes[i].BlendIndex2 = GetMergedBoneIndex(i, mesh.Vertices[i].BlendIndexes[2], submesh);
                                    xv2Submesh.CpuVertexes[i].BlendIndex3 = GetMergedBoneIndex(i, mesh.Vertices[i].BlendIndexes[3], submesh);

                                    //xv2Submesh.CpuVertexes[i].BlendIndex0 = mesh.Vertices[i].BlendIndexes[0];
                                    //xv2Submesh.CpuVertexes[i].BlendIndex1 = mesh.Vertices[i].BlendIndexes[1];
                                    //xv2Submesh.CpuVertexes[i].BlendIndex2 = mesh.Vertices[i].BlendIndexes[2];
                                    //xv2Submesh.CpuVertexes[i].BlendIndex3 = mesh.Vertices[i].BlendIndexes[3];
                                    //xv2Submesh.CpuVertexes[i].BlendWeights = new Vector4(mesh.Vertices[i].BlendWeights[0], mesh.Vertices[i].BlendWeights[1], mesh.Vertices[i].BlendWeights[2], mesh.Vertices[i].BlendWeights[3]);
                                    xv2Submesh.CpuVertexes[i].BlendWeights = new Vector3(mesh.Vertices[i].BlendWeights[0], mesh.Vertices[i].BlendWeights[1], mesh.Vertices[i].BlendWeights[2]);
                                }

                                //GpuVertexes (for rendering)
                                xv2Submesh.GpuVertexes[i] = xv2Submesh.CpuVertexes[i];
                            }

                            //Samplers
                            xv2Submesh.InitSamplers(mesh.TextureLists[submesh.TextureListIndex].TextureSamplerDefs);

                            xv2Submesh.EnableSkinning = mesh.VertexFlags.HasFlag(VertexFlags.BlendWeight);

                            //Generate bone index list
                            xv2Submesh.BoneIdx[0] = new short[SourceEmoFile.Skeleton.Bones.Count];

                            //For EMOs the bone list is static, since the skeleton isn't gonna change after creation. So we can just use the skeleton from the EMO file (the OBJ.EMA skeleton should be identical).
                            for (short i = 0; i < xv2Submesh.BoneIdx[0].Length; i++)
                                xv2Submesh.BoneIdx[0][i] = i;

                            if (xv2Submesh.CpuVertexes.Length > 0)
                            {
                                xv2Mesh.Submeshes.Add(xv2Submesh);
                                submeshIndex++;
                            }
                        }

                        xv2Model.Meshes.Add(xv2Mesh);
                    }

                    Models.Add(xv2Model);
                }

                partIdx++;
            }

        }

        private void SourceFileChanged_Event(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EMD_File.ModelChanged))
            {
                if (SourceEmdFile != null)
                {
                    LoadEmd(false);
                }
                else if (SourceEmoFile != null)
                {
                    LoadEmo(false);
                }

                ModelChanged?.Invoke(this, EventArgs.Empty);
            }
            else if (e.PropertyName == nameof(EMD_File.TextureSamplersChanged))
            {
                ReinitializeTextureSamplers();
            }
        }

        private void ReinitializeTextureSamplers()
        {
            for (int i = 0; i < Models.Count; i++)
            {
                for (int a = 0; a < Models[i].Meshes.Count; a++)
                {
                    for (int s = 0; s < Models[i].Meshes[a].Submeshes.Count; s++)
                    {
                        Models[i].Meshes[a].Submeshes[s].InitSamplers(SourceEmdFile.Models[i].Meshes[a].Submeshes[s].TextureSamplerDefs);
                    }
                }
            }
        }
        
        #endregion

        #region Rendering

        public void Update(int actor, Xv2Skeleton skeleton = null)
        {
            foreach (Xv2Model model in Models)
            {
                foreach (Xv2Mesh mesh in model.Meshes)
                {
                    foreach (Xv2Submesh submesh in mesh.Submeshes)
                    {
                        submesh.Update(actor, skeleton);
                    }
                }
            }
        }

        public void Draw(Matrix world, int actor, List<Xv2ShaderEffect> materials, Xv2Texture[] textures, Xv2Texture[] dyts, int dytIdx, Xv2Skeleton skeleton = null)
        {
            foreach (Xv2Model model in Models)
            {
                Matrix animatedWorld = model.GetTransformedWorld(world, skeleton);

                foreach (Xv2Mesh mesh in model.Meshes)
                {
                    foreach (Xv2Submesh submesh in mesh.Submeshes)
                    {
                        Matrix transformedMatrix = animatedWorld * model.Transform * mesh.Transform * submesh.Transform;

                        submesh.Draw(transformedMatrix, actor, materials, textures, dyts, dytIdx, skeleton);
                    }
                }
            }
        }

        #endregion

        #region HelperFunctions
        private static byte GetMergedBoneIndex(int vertexIdx, byte idx, EMD_Submesh emdSubmesh, List<string> boneNames)
        {
            string name = emdSubmesh.GetBoneName(vertexIdx, idx);

            if (!boneNames.Contains(name))
                boneNames.Add(name);

            return (byte)boneNames.IndexOf(name);
        }

        private static byte GetMergedBoneIndex(int vertexIdx, byte idx, EMG_Submesh emgSubmesh)
        {
            return (byte)emgSubmesh.GetBoneIndex(vertexIdx, idx);
        }

        public void UnsetActor(int actor)
        {
            foreach (var model in Models)
            {
                foreach (var mesh in model.Meshes)
                {
                    foreach (var submesh in mesh.Submeshes)
                        submesh.UnsetActor(actor);
                }
            }
        }
        #endregion

        public List<Xv2Submesh> GetCompiledSubmeshes(IList<EMD_Submesh> sourceSubmeshes)
        {
            List<Xv2Submesh> submeshes = new List<Xv2Submesh>();

            foreach (var sourceSubmesh in sourceSubmeshes)
            {
                Xv2Submesh submesh = GetSubmesh(sourceSubmesh);

                if (submesh == null)
                    throw new InvalidDataException("Xv2Model.GetCompiledSubmeshes: Could not find the matching compiled submesh.");

                submeshes.Add(submesh);
            }

            return submeshes;
        }

        public Xv2Submesh GetSubmesh(EMD_Submesh sourceSubmesh)
        {
            foreach (var model in Models)
            {
                foreach (var mesh in model.Meshes)
                {
                    foreach (var submesh in mesh.Submeshes)
                    {
                        if (submesh.SourceSubmesh == sourceSubmesh) return submesh;
                    }
                }
            }

            return null;
        }
    }

    public class Xv2Model : Entity
    {
        public List<Xv2Mesh> Meshes { get; set; } = new List<Xv2Mesh>();

        //Model-Level Animation:
        public int AttachBone { get; set; } = -1;

        public Matrix GetTransformedWorld(Matrix world, Xv2Skeleton skeleton)
        {
            if (AttachBone != -1 && skeleton != null)
            {
                return world;
                //return world * skeleton.Bones[AttachBone].SkinningMatrix;
                return skeleton.Bones[AttachBone].SkinningMatrix * world;
            }

            return world;
        }

        public Xv2Model(string name, GameBase gameBase) : base(gameBase)
        {
            Name = name;
        }

    }

    public class Xv2Mesh : Entity
    {
        public List<Xv2Submesh> Submeshes { get; set; } = new List<Xv2Submesh>();

        public Xv2Mesh(string name, GameBase gameBase) : base(gameBase)
        {
            Name = name;
        }
    }

    public class Xv2Submesh : Entity
    {
        public int SubmeshIndex { get; private set; }
        public ModelType Type { get; private set; }
        public object SourceSubmesh { get; set; }

        //Vertices:
        public VertexPositionNormalTextureBlend[] GpuVertexes { get; set; }
        public VertexPositionNormalTextureBlend[] CpuVertexes { get; set; }
        public short[] Indices { get; set; }

        //Samplers:
        public SamplerInfo[] Samplers { get; set; }

        //Custom Color DYT
        private int CustomColrDytCreatedFromIndex = 0;
        private int CustomColorIndex = -1;
        private int CustomColorGroup = -1;
        public Xv2Texture CustomColorDyt = null;

        //Skinning:
        public bool EnableSkinning { get; set; }
        public string[] BoneNames;
        public short[][] BoneIdx = new short[SceneManager.NumActors][]; //[Actor][BoneIdxInVertex]
        private readonly Task[] SkinningThreads = new Task[Environment.ProcessorCount > 1 ? (int)(Environment.ProcessorCount * 0.8f) : 1];

        //Actor-Specific Information:
        private Matrix[] PrevWVP = new Matrix[SceneManager.NumActors];

        public Xv2Submesh(GameBase gameBase, string name, int submeshIndex, ModelType type, object sourceSubmesh) : base(gameBase)
        {
            Name = name;
            SubmeshIndex = submeshIndex;
            Type = type;
            SourceSubmesh = sourceSubmesh;
        }

        public void Update(int actor, Xv2Skeleton skeleton = null)
        {
        }

        public void Draw(Matrix world, int actor, List<Xv2ShaderEffect> materials, Xv2Texture[] textures, Xv2Texture[] dyts, int dytIdx, Xv2Skeleton skeleton = null)
        {
            if (materials == null) return;

            //Handle BCS Colors
            if (dyts != null)
            {
                UpdateCustomColorDyt(actor, dytIdx, dyts, materials[SubmeshIndex]);

                if (CustomColorDyt != null)
                {
                    GraphicsDevice.Textures[4] = CustomColorDyt.Texture;
                    GraphicsDevice.VertexTextures[4] = CustomColorDyt.Texture;
                }
            }

            materials[SubmeshIndex].World = (SceneManager.ResolveLeftHandSymetry) ? (world * Matrix.CreateScale(-1, 1f, 1f)) : world;
            materials[SubmeshIndex].PrevWVP = PrevWVP[actor];

            //Set samplers/textures
            foreach (SamplerInfo sampler in Samplers)
            {
                GraphicsDevice.SamplerStates[sampler.samplerSlot] = sampler.state;
                GraphicsDevice.VertexSamplerStates[sampler.samplerSlot] = sampler.state;

                //Set textures if index is valid.
                if (sampler.parameter <= textures?.Length - 1 && sampler.parameter >= 0)
                {
                    GraphicsDevice.VertexTextures[sampler.textureSlot] = textures[sampler.parameter].Texture;
                    GraphicsDevice.Textures[sampler.textureSlot] = textures[sampler.parameter].Texture;
                }
            }

            if (Type == ModelType.Emd)
            {
                if (BoneIdx[actor] == null && skeleton != null)
                    SetBoneIndices(actor, skeleton);
                /*
                //Set bone indices for this actor if needed. These will be cached so it will only actually happen once per actor (BoneIdx[actor] will only become null on an actor change).
                if (actor == -1)
                {
                    //Mesh is for a physics part, so we must use the SCD ESK instead of the main character esk
                    SetBoneIndices(charaSkeleton);
                }
                else
                {
                    if (BoneIdx[actor] == null)
                        SetBoneIndices(actor);
                }
                */
            }

            //Perform skinning on the vertices (animation). 
            //Currently, dont apply to SCDs
            if (EnableSkinning && skeleton != null)
            {
                /*
                if(Type == ModelType.Emo)
                {
                    CreateSkinningMatrices(skeleton.Bones);
                }
                */

                UpdateVertices(skeleton.Bones, actor);

            }

            //Shader passes and vertex drawing
            foreach (EffectPass pass in materials[SubmeshIndex].CurrentTechnique.Passes)
            {
                if (Type == ModelType.Emd)
                    materials[SubmeshIndex].SetColorFade(SceneManager.Actors[actor]);

                materials[SubmeshIndex].SetVfxLight();

                //if (Type == ModelType.Emo && EnableSkinning)
                //    materials[SubmeshIndex].SetSkinningMatrices(SkinningMatrices);

                pass.Apply();

                GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, GpuVertexes, 0, GpuVertexes.Length, Indices, 0, Indices.Length / 3);
            }

            PrevWVP[actor] = materials[SubmeshIndex].PrevWVP;
        }

        private void UpdateVertices(Xv2Bone[] bones, int actor)
        {
            int vertexPerThread = CpuVertexes.Length / SkinningThreads.Length;
            //bool singleThread = CpuVertexes.Length * 2 <= SkinningThreads.Length;
            const bool singleThread = true;

            //Multithreading disabled for now
            //It provides a sizable performance uplift when there's lots of animated models, but is way less efficent when theres just 1 (< 10% vs 20% cpu usage)

            if (singleThread)
            {
                UpdateVertices(bones, actor, 0, CpuVertexes.Length);
                return;
            }

            for(int i = 0; i < SkinningThreads.Length; i++)
            {
                SkinningThreads[i] = Task.CompletedTask;

                int startIndex = vertexPerThread * i;

                //Split the load evenly except on the last thread, where it will just finish the remaining amount
                int count = i == SkinningThreads.Length - 1 ? CpuVertexes.Length - startIndex : vertexPerThread;

                SkinningThreads[i] = Task.Run(() => UpdateVertices(bones, actor, startIndex, count));
                //SkinningThreads[i] = new Task(() => UpdateVertices(bones, actor, startIndex, count), TaskCreationOptions.RunContinuationsAsynchronously);
                //SkinningThreads[i].Start();
            }

            
            Task task = Task.WhenAll(SkinningThreads);

            while (!task.IsCompleted) { }
            //task.Wait();
        }

        private void UpdateVertices(Xv2Bone[] bones, int actor, int startIndex, int count)
        {
            //Performs quite poorly... majority of CPU time is spent on this even for just 1 animated character
            //Need to figure out how the shader skinning works

            for (int i = startIndex; i < startIndex + count; i++)
            {
                GpuVertexes[i].Position = Vector3.Transform(CpuVertexes[i].Position, bones[BoneIdx[actor][CpuVertexes[i].BlendIndex0]].SkinningMatrix) * CpuVertexes[i].BlendWeights.X;
                GpuVertexes[i].Normal = Vector3.Transform(CpuVertexes[i].Normal, bones[BoneIdx[actor][CpuVertexes[i].BlendIndex0]].SkinningMatrix) * CpuVertexes[i].BlendWeights.X;


                //Only perform skinning when its needed. Performance is still lousy but it provides a decent increase
                if (CpuVertexes[i].BlendWeights.Y > 0f)
                {
                    GpuVertexes[i].Position += Vector3.Transform(CpuVertexes[i].Position, bones[BoneIdx[actor][CpuVertexes[i].BlendIndex1]].SkinningMatrix) * CpuVertexes[i].BlendWeights.Y;
                    GpuVertexes[i].Normal += Vector3.Transform(CpuVertexes[i].Normal, bones[BoneIdx[actor][CpuVertexes[i].BlendIndex1]].SkinningMatrix) * CpuVertexes[i].BlendWeights.Y;
                }

                if (CpuVertexes[i].BlendWeights.Z > 0f)
                {
                    GpuVertexes[i].Position += Vector3.Transform(CpuVertexes[i].Position, bones[BoneIdx[actor][CpuVertexes[i].BlendIndex2]].SkinningMatrix) * CpuVertexes[i].BlendWeights.Z;
                    GpuVertexes[i].Normal += Vector3.Transform(CpuVertexes[i].Normal, bones[BoneIdx[actor][CpuVertexes[i].BlendIndex2]].SkinningMatrix) * CpuVertexes[i].BlendWeights.Z;
                }

                float w = 1f - (CpuVertexes[i].BlendWeights.X + CpuVertexes[i].BlendWeights.Y + CpuVertexes[i].BlendWeights.Z);

                if (w > 0f)
                {
                    GpuVertexes[i].Position += Vector3.Transform(CpuVertexes[i].Position, bones[BoneIdx[actor][CpuVertexes[i].BlendIndex3]].SkinningMatrix) * w;
                    GpuVertexes[i].Normal += Vector3.Transform(CpuVertexes[i].Normal, bones[BoneIdx[actor][CpuVertexes[i].BlendIndex3]].SkinningMatrix) * w;
                }

            }
        }

        #region CustomColor
        private void UpdateCustomColorDyt(int actor, int dytIdx, Xv2Texture[] Dyts, Xv2ShaderEffect material)
        {
            if ((CustomColrDytCreatedFromIndex != dytIdx || CustomColorDyt == null) && CustomColorIndex != -1 && Dyts.Length > CustomColrDytCreatedFromIndex)
            {
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
                        Xv2CoreLib.HslColor.HslColor color = new Xv2CoreLib.HslColor.RgbColor(colors.GetColor(c)).ToHsl();

                        var pixel = dyt.EmbEntry.Texture.GetPixel(width, height + dytLine);
                        Xv2CoreLib.HslColor.HslColor pixelColor = new Xv2CoreLib.HslColor.RgbColor(pixel.R, pixel.G, pixel.B).ToHsl();
                        pixelColor.SetHue(color.Hue);
                        pixelColor.Saturation = color.Saturation;
                        //pixelColor.Saturation = (pixelColor.Saturation * 0.5) + (color.Saturation * 0.5);
                        //pixelColor.Lightness = (pixelColor.Lightness * 0.75) + (color.Lightness * 0.25);

                        var newPixelRgb = pixelColor.ToRgb();

                        dyt.EmbEntry.Texture.SetPixel(width, height + dytLine, pixel.A, newPixelRgb.R_int, newPixelRgb.G_int, newPixelRgb.B_int);
                        dyt.EmbEntry.Texture.SetPixel(width, height + dytLine + 1, pixel.A, newPixelRgb.R_int, newPixelRgb.G_int, newPixelRgb.B_int);
                        dyt.EmbEntry.Texture.SetPixel(width, height + dytLine + 2, pixel.A, newPixelRgb.R_int, newPixelRgb.G_int, newPixelRgb.B_int);
                        dyt.EmbEntry.Texture.SetPixel(width, height + dytLine + 3, pixel.A, newPixelRgb.R_int, newPixelRgb.G_int, newPixelRgb.B_int);

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
        public void InitSamplers(IList<EMD_TextureSamplerDef> samplerDefs)
        {
            Samplers = new SamplerInfo[samplerDefs.Count];

            for (int i = 0; i < samplerDefs.Count; i++)
            {
                Samplers[i].type = SamplerType.Sampler2D; //todo: shadow maps are a different sampler type (sampler_c... SamplerCube?)
                Samplers[i].textureSlot = i;
                Samplers[i].samplerSlot = i;

                Samplers[i].state = new SamplerState();
                Samplers[i].state.AddressU = GetTextureAddressMode(samplerDefs[i].AddressModeU);
                Samplers[i].state.AddressV = GetTextureAddressMode(samplerDefs[i].AddressModeV);
                Samplers[i].state.AddressW = TextureAddressMode.Wrap;
                Samplers[i].state.BorderColor = new Color(1, 1, 1, 1);
                Samplers[i].state.Filter = GetTextureFilter(samplerDefs[i].FilteringMin, samplerDefs[i].FilteringMag);
                Samplers[i].state.MaxAnisotropy = 1;
                Samplers[i].state.MaxMipLevel = 1;

                Samplers[i].name = ShaderManager.Instance.GetSamplerName(i);
                Samplers[i].state.Name = Samplers[i].name;
                Samplers[i].parameter = samplerDefs[i].EmbIndex;

                //Force AF x16
                Samplers[i].state.Filter = TextureFilter.Anisotropic;
                Samplers[i].state.MaxAnisotropy = 16;
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

        //Bone Index:
        private void SetBoneIndices(int actor, Xv2Skeleton skeleton)
        {
            if (actor < 0 || actor > SceneManager.NumActors - 1) return;

            if (SceneManager.Actors[actor] != null)
            {
                BoneIdx[actor] = new short[BoneNames.Length];

                for (int i = 0; i < BoneNames.Length; i++)
                {
                    BoneIdx[actor][i] = (short)skeleton.GetBoneIndex(BoneNames[i]);
                }
            }
        }

        public void UnsetActor(int actor)
        {
            if (actor < 0 || actor > SceneManager.NumActors - 1) return;

            BoneIdx[actor] = null;
        }
    }

    public enum ModelType
    {
        Emd,
        Nsk,
        Emo
    }

}
