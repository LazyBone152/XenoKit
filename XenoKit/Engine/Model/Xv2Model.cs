using System;
using System.Linq;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using XenoKit.Engine.Animation;
using XenoKit.Engine.Vertex;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using Xv2CoreLib.EMG;
using Xv2CoreLib.EMO;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EMM;
using Xv2CoreLib.NSK;
using Xv2CoreLib.Resource.App;
using EmmMaterial = Xv2CoreLib.EMM.EmmMaterial;
using static Xv2CoreLib.EMD.EMD_TextureSamplerDef;
using XenoKit.Editor;

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
        /// Load the first submesh from an EMG, and ignore samplers and blend weights. This is intended for loading particle EMGs.
        /// </summary>
        public static Xv2Submesh LoadEmg(GameBase gameBase, EMG_File emgFile)
        {
            if (emgFile.EmgMeshes.Count == 0) return null;
            if (emgFile.EmgMeshes[0].Submeshes.Count == 0) return null;

            EMG_Mesh mesh = emgFile.EmgMeshes[0];
            EMG_Submesh submesh = emgFile.EmgMeshes[0].Submeshes[0];

            Xv2Submesh xv2Submesh = new Xv2Submesh(gameBase, submesh.MaterialName, 0, ModelType.Emg, submesh);

            //Triangles
            xv2Submesh.Indices = submesh.Faces.Copy();

            //Create vertex array
            xv2Submesh.GpuVertexes = new VertexPositionNormalTextureBlend[mesh.Vertices.Count];
            xv2Submesh.CpuVertexes = new VertexPositionNormalTextureBlend[mesh.Vertices.Count];

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

                //GpuVertexes (for rendering)
                xv2Submesh.GpuVertexes[i] = xv2Submesh.CpuVertexes[i];
            }

            return xv2Submesh;
        }

        public static Xv2ModelFile LoadEmgInContainer(GameBase gameBase, EMG_File emgFile)
        {
            Xv2ModelFile modelFile = new Xv2ModelFile(gameBase);
            modelFile.Type = ModelType.Emg;

            modelFile.Models.Add(new Xv2Model("root_model", gameBase));
            modelFile.Models[0].Meshes.Add(new Xv2Mesh("root_mesh", gameBase));
            modelFile.Models[0].Meshes[0].Submeshes.Add(LoadEmg(gameBase, emgFile));

            return modelFile;
        }

        /// <summary>
        /// Creates a materials list to use for renderering. Materials are indexed by the submesh index. 
        /// </summary>
        public List<Xv2ShaderEffect> InitializeMaterials(ShaderType shaderType, EMM_File emmFile = null)
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

                        Xv2ShaderEffect compiledMat = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(material, GameBase, shaderType);

                        if (compiledMat == null)
                        {
                            //No material was found for this Submesh. Use default.
                            compiledMat = Xv2ShaderEffect.CreateDefaultMaterial(ShaderType.Chara, GameBase);
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
            foreach (EMD_Model emdModel in SourceEmdFile.Models)
            {
                Xv2Model model = new Xv2Model(emdModel.Name, GameBase);

                //NSK files support model-level animation like EMOs
                if (Type == ModelType.Nsk)
                    model.AttachBone = SourceNskFile.EskFile.Skeleton.NonRecursiveBones.IndexOf(SourceNskFile.EskFile.Skeleton.NonRecursiveBones.FirstOrDefault(x => x.Name == emdModel.Name));

                foreach (EMD_Mesh emdMesh in emdModel.Meshes)
                {
                    Xv2Mesh mesh = new Xv2Mesh(emdMesh.Name, GameBase);

                    foreach (EMD_Submesh emdSubmesh in emdMesh.Submeshes)
                    {
                        //EACH triangle list needs to be its own Xv2Submesh. Merging them together cannot work with the vanilla game shaders as they cant accept more than 24 bones

                        foreach (EMD_Triangle triangleList in emdSubmesh.Triangles)
                        {
                            Xv2Submesh submesh = new Xv2Submesh(GameBase, emdSubmesh.Name, submeshIndex, Type, emdSubmesh);

                            //Triangles
                            submesh.Indices = new short[triangleList.Faces.Count];

                            for (int i = 0; i < triangleList.Faces.Count; i++)
                            {
                                submesh.Indices[i] = (short)triangleList.Faces[i];
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
                                    submesh.CpuVertexes[i].BlendIndex0 = emdSubmesh.Vertexes[i].BlendIndexes[0];
                                    submesh.CpuVertexes[i].BlendIndex1 = emdSubmesh.Vertexes[i].BlendIndexes[1];
                                    submesh.CpuVertexes[i].BlendIndex2 = emdSubmesh.Vertexes[i].BlendIndexes[2];
                                    submesh.CpuVertexes[i].BlendIndex3 = emdSubmesh.Vertexes[i].BlendIndexes[3];
                                    submesh.CpuVertexes[i].BlendWeights = new Vector3(emdSubmesh.Vertexes[i].BlendWeights[0], emdSubmesh.Vertexes[i].BlendWeights[1], emdSubmesh.Vertexes[i].BlendWeights[2]);
                                }

                                //GpuVertexes (for rendering)
                                submesh.GpuVertexes[i] = submesh.CpuVertexes[i];
                            }

                            submesh.InitSamplers(emdSubmesh.TextureSamplerDefs);

                            submesh.EnableSkinning = emdSubmesh.VertexFlags.HasFlag(VertexFlags.BlendWeight);
                            submesh.BoneNames = triangleList.Bones.ToArray();

                            if (submesh.CpuVertexes.Length > 0)
                            {
                                mesh.Submeshes.Add(submesh);
                                submeshIndex++;
                            }


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
                            xv2Submesh.GpuVertexes = new VertexPositionNormalTextureBlend[mesh.Vertices.Count];
                            xv2Submesh.CpuVertexes = new VertexPositionNormalTextureBlend[mesh.Vertices.Count];

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
                                    xv2Submesh.CpuVertexes[i].BlendIndex0 = mesh.Vertices[i].BlendIndexes[0];
                                    xv2Submesh.CpuVertexes[i].BlendIndex1 = mesh.Vertices[i].BlendIndexes[1];
                                    xv2Submesh.CpuVertexes[i].BlendIndex2 = mesh.Vertices[i].BlendIndexes[2];
                                    xv2Submesh.CpuVertexes[i].BlendIndex3 = mesh.Vertices[i].BlendIndexes[3];
                                    xv2Submesh.CpuVertexes[i].BlendWeights = new Vector3(mesh.Vertices[i].BlendWeights[0], mesh.Vertices[i].BlendWeights[1], mesh.Vertices[i].BlendWeights[2]);
                                }

                                //GpuVertexes (for rendering)
                                xv2Submesh.GpuVertexes[i] = xv2Submesh.CpuVertexes[i];
                            }

                            //Samplers
                            xv2Submesh.InitSamplers(mesh.TextureLists[submesh.TextureListIndex].TextureSamplerDefs);

                            xv2Submesh.EnableSkinning = mesh.VertexFlags.HasFlag(VertexFlags.BlendWeight);

                            //Generate bone index list
                            xv2Submesh.BoneNames = new string[24];

                            for (short i = 0; i < submesh.Bones.Count; i++)
                            {
                                xv2Submesh.BoneNames[i] = SourceEmoFile.Skeleton.Bones[submesh.Bones[i]].Name;
                            }

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
            else if (e.PropertyName == nameof(EMD_File.MaterialsChanged))
            {
                ModelChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ReinitializeTextureSamplers()
        {
            for (int i = 0; i < SourceEmdFile.Models.Count; i++)
            {
                for (int a = 0; a < SourceEmdFile.Models[i].Meshes.Count; a++)
                {
                    for (int s = 0; s < SourceEmdFile.Models[i].Meshes[a].Submeshes.Count; s++)
                    {
                        //Triangle lists are separated into submeshes in XenoKit, so we must account for that here.
                        for (int b = 0; b < SourceEmdFile.Models[i].Meshes[a].Submeshes[s].Triangles.Count; b++)
                        {
                            Models[i].Meshes[a].Submeshes[s + b].InitSamplers(SourceEmdFile.Models[i].Meshes[a].Submeshes[s].TextureSamplerDefs);
                        }
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

        /// <summary>
        /// Draw this model with just one material and no textures or dyts. Intended for earlier passes only (shadow/normals).
        /// </summary>
        public void Draw(Matrix world, int actor, Xv2ShaderEffect material, Xv2Skeleton skeleton = null)
        {
            foreach (Xv2Model model in Models)
            {
                Matrix animatedWorld = model.GetTransformedWorld(world, skeleton);

                foreach (Xv2Mesh mesh in model.Meshes)
                {
                    foreach (Xv2Submesh submesh in mesh.Submeshes)
                    {
                        Matrix transformedMatrix = animatedWorld * model.Transform * mesh.Transform * submesh.Transform;

                        submesh.Draw(transformedMatrix, actor, material, skeleton);
                    }
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
                //This is right and wrong depending on the case... why?
                //When return world = fixes; broken effects, like sparks, but breaks boost auras
                //When skeleton.Bones[AttachBone].SkinningMatrix * world; fixes boost auras, breaks sparks
                //Stages only work properly with AbsoluteMatrix * world

                //return world;
                return skeleton.Bones[AttachBone].AbsoluteAnimationMatrix * world;
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
        public override EntityType EntityType => EntityType.Model;

        public int SubmeshIndex { get; private set; }
        public ModelType Type { get; private set; }
        public object SourceSubmesh { get; set; }

        //Vertices:
        public VertexPositionNormalTextureBlend[] GpuVertexes { get; set; }
        public VertexPositionNormalTextureBlend[] CpuVertexes { get; set; }
        public short[] Indices { get; set; }
        public int[] UsedIndices { get; set; }


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
        public Matrix[] SkinningMatrices = new Matrix[24];
        private static Matrix[] DefaultSkinningMatrices = new Matrix[24];

        //Actor-Specific Information:
        private Matrix[] PrevWVP = new Matrix[SceneManager.NumActors];

        static Xv2Submesh()
        {
            for (int i = 0; i < 24; i++)
                DefaultSkinningMatrices[i] = Matrix.Identity;
        }

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

            if (!RenderSystem.CheckDrawPass(materials[SubmeshIndex])) return;

#if DEBUG
            if (RenderSystem.CurrentDrawIdx < RenderSystem.DRAW_ORDER.Length)
            {
                RenderSystem.DRAW_ORDER[RenderSystem.CurrentDrawIdx] = materials[SubmeshIndex].Material.Name;
                RenderSystem.CurrentDrawIdx++;
            }
#endif

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

            materials[SubmeshIndex].World = world;
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

            materials[SubmeshIndex].SetTextureTile(TexTile01, TexTile23);

            DrawEnd(actor, materials[SubmeshIndex], skeleton);
        }

        public void Draw(Matrix world, int actor, Xv2ShaderEffect material, Xv2Skeleton skeleton = null)
        {
            //if (!RenderSystem.CheckDrawPass(material)) return;

            material.World = world;
            material.PrevWVP = PrevWVP[actor];

            DrawEnd(actor, material, skeleton);
        }

        private void DrawEnd(int actor, Xv2ShaderEffect material, Xv2Skeleton skeleton)
        {
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

                pass.Apply();

                GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, CpuVertexes, 0, GpuVertexes.Length, Indices, 0, Indices.Length / 3);
            }

            PrevWVP[actor] = material.WVP;
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
                    SkinningMatrices[i] = Matrix.Identity;
                }
            }
        }

        private void UpdateVertices(Xv2Bone[] bones, int actor)
        {
            //Old CPU skinning method.The renderer has since been updated to use the shaders for skinning.
            //No longer works with the change to bone index caching
            /*
            if (UsedIndices == null)
                CreateUsedIndices();

            for (int a = 0; a < UsedIndices.Length; a++)
            {
                int i = UsedIndices[a];

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
            */
        }

        private void CreateUsedIndices()
        {
            List<int> usedIndices = new List<int>();

            foreach (short index in Indices)
            {
                if (!usedIndices.Contains(index))
                    usedIndices.Add(index);
            }

            UsedIndices = usedIndices.ToArray();
        }

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
                Samplers[i].state.FilterMode = TextureFilterMode.Default;
                Samplers[i].name = ShaderManager.GetSamplerName(i);
                Samplers[i].state.Name = Samplers[i].name;
                Samplers[i].parameter = samplerDefs[i].EmbIndex;
            }
        
            //Set the texture tile parameters. These are used by the vertex shader to apply the correct tiling to the textures
            if(samplerDefs.Count >= 1)
            {
                TexTile01[0] = samplerDefs[0].ScaleU;
                TexTile01[1] = samplerDefs[0].ScaleV;
            }

            if (samplerDefs.Count >= 2)
            {
                TexTile01[2] = samplerDefs[1].ScaleU;
                TexTile01[3] = samplerDefs[1].ScaleV;
            }

            if (samplerDefs.Count >= 3)
            {
                TexTile23[0] = samplerDefs[2].ScaleU;
                TexTile23[1] = samplerDefs[2].ScaleV;
            }

            if (samplerDefs.Count >= 4)
            {
                TexTile23[2] = samplerDefs[3].ScaleU;
                TexTile23[3] = samplerDefs[3].ScaleV;
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

    }

    public enum ModelType
    {
        Emd,
        Nsk,
        Emo,
        Emg
    }

}
