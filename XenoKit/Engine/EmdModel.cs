using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XenoKit.Editor;
using Xv2CoreLib.EMD;
using XenoKit.Engine.Animation;
using XenoKit.Engine.View;
using XenoKit.Engine.Vertex;

namespace XenoKit.Engine
{
    public class EmdFile
    {
        public string Name { get; set; }
        public List<EmdModel> Models { get; set; }
        
        public static EmdFile Load(string path, CharacterSkeleton skeleton, GraphicsDevice graphicsDevice)
        {
            return Load(File.ReadAllBytes(path), graphicsDevice, skeleton, Path.GetFileNameWithoutExtension(path));
        }

        public static EmdFile Load(byte[] bytes, GraphicsDevice graphicsDevice, CharacterSkeleton skeleton = null, string name = null)
        {
            //Load the binary emd file
            EMD_File emdFile = new Parser(bytes).emdFile;

            //Create EmdModel
            EmdFile newEmdFile = new EmdFile() { Models = new List<EmdModel>(), Name = name };

            //Models
            foreach (var emdModel in emdFile.Models)
            {
                EmdModel model = new EmdModel() { Meshes = new List<EmdMesh>(), Name = emdModel.Name };

                foreach (var emdMesh in emdModel.Meshes)
                {
                    EmdMesh mesh = new EmdMesh() { Submeshes = new List<EmdSubmesh>(), Name = emdMesh.Name };

                    foreach (var emdSubmesh in emdMesh.Submeshes)
                    {
                        EmdSubmesh submesh = new EmdSubmesh();

                        //Create vertex array
                        submesh.CpuVertexes = new VertexPositionNormalTextureBlend[emdSubmesh.VertexCount];
                        submesh.GpuVertexes = new VertexPositionNormalTextureBlend[emdSubmesh.VertexCount];
                        submesh.Indices = new short[emdSubmesh.TriangleCount];

                        for (int i = 0; i < emdSubmesh.VertexCount; i++)
                        {
                            submesh.CpuVertexes[i].Position = new Vector3(emdSubmesh.Vertexes[i].PositionX, emdSubmesh.Vertexes[i].PositionY, emdSubmesh.Vertexes[i].PositionZ);
                            submesh.CpuVertexes[i].Normal = new Vector3(emdSubmesh.Vertexes[i].NormalX, emdSubmesh.Vertexes[i].NormalY, emdSubmesh.Vertexes[i].NormalZ);
                            submesh.CpuVertexes[i].TextureCoordinate = new Vector2(emdSubmesh.Vertexes[i].TextureU, emdSubmesh.Vertexes[i].TextureV);
                            submesh.CpuVertexes[i].BlendIndex0 = GetBoneIndex(skeleton, emdSubmesh.GetBoneName(i, emdSubmesh.Vertexes[i].BlendIndexes[0]));
                            submesh.CpuVertexes[i].BlendIndex1 = GetBoneIndex(skeleton, emdSubmesh.GetBoneName(i, emdSubmesh.Vertexes[i].BlendIndexes[1]));
                            submesh.CpuVertexes[i].BlendIndex2 = GetBoneIndex(skeleton, emdSubmesh.GetBoneName(i, emdSubmesh.Vertexes[i].BlendIndexes[2]));
                            submesh.CpuVertexes[i].BlendIndex3 = GetBoneIndex(skeleton, emdSubmesh.GetBoneName(i, emdSubmesh.Vertexes[i].BlendIndexes[3]));
                            submesh.CpuVertexes[i].BlendWeights = new Vector4(emdSubmesh.Vertexes[i].BlendWeights[0], emdSubmesh.Vertexes[i].BlendWeights[1], emdSubmesh.Vertexes[i].BlendWeights[2], emdSubmesh.Vertexes[i].BlendWeights[3]);

                            //GpuVertexes (for rendering)
                            submesh.GpuVertexes[i] = submesh.CpuVertexes[i];
                        }

                        int indexPos = 0;
                        foreach (var triangeList in emdSubmesh.Triangles)
                        {
                            for (int i = 0; i < triangeList.FaceCount; i += 3)
                            {
                                submesh.Indices[indexPos + 0] = (short)triangeList.Faces[i + 0];
                                submesh.Indices[indexPos + 1] = (short)triangeList.Faces[i + 1];
                                submesh.Indices[indexPos + 2] = (short)triangeList.Faces[i + 2];
                                indexPos += 3;

                                //Load blend weights
                            }
                        }

                        //Texture
                        submesh.effect = new BasicEffect(graphicsDevice);
                        submesh.effect.Alpha = 1f;
                        submesh.effect.EnableDefaultLighting();

                        mesh.Submeshes.Add(submesh);
                    }

                    model.Meshes.Add(mesh);
                }

                newEmdFile.Models.Add(model);
            }

            return newEmdFile;
        }
        
        private static short GetBoneIndex(CharacterSkeleton skeleton, string name)
        {
            if (skeleton == null)          //If skeleton is null we will just return a default value.
                return 0;
            return (short)skeleton.GetBoneIndex(name);
        }

        public void Draw(GraphicsDevice graphicsDevice, Camera camera, Matrix world, Matrix[] skinningBones = null)
        {
            foreach (var model in Models)
            {
                foreach (var mesh in model.Meshes)
                {
                    foreach (var submesh in mesh.Submeshes)
                        submesh.Draw(graphicsDevice, camera, world, skinningBones);
                }
            }
        }

        
    }
    
    public class EmdModel
    {
        public string Name { get; set; }
        public List<EmdMesh> Meshes { get; set; }
    }

    public class EmdMesh
    {
        public string Name { get; set; }
        public List<EmdSubmesh> Submeshes { get; set; }
    }

    public class EmdSubmesh
    {
        public string Name { get; set; }
        public VertexPositionNormalTextureBlend[] GpuVertexes { get; set; }
        public VertexPositionNormalTextureBlend[] CpuVertexes { get; set; }
        public short[] Indices { get; set; }
        public BasicEffect effect { get; set; }

        
        public void Draw(GraphicsDevice graphicsDevice, Camera camera, Matrix world, Matrix[] skinningBones = null)
        {
            //bool resolveLeftHand_symetry = true;                   //use true, when we want symetry solved, false when debug (LeftHand instead of rightHand). https://stackoverflow.com/questions/29370361/does-xna-opengl-use-left-handed-matrices
            

            effect.World = (SceneManager.ResolveLeftHandSymetry) ? (world * Matrix.CreateScale(-1, 1f, 1f)) : world;
            effect.View = camera.ViewMatrix;
            effect.Projection = camera.ProjectionMatrix;

            /*
            effect.AmbientLightColor = new Vector3(0.3568f, 0.2313f, 0.2509f);
            effect.DiffuseColor = new Vector3(0.7764f, 0.509f, 0.501f);
            effect.SpecularColor = new Vector3(0.3f, 0.3f, 0.3f);
            */

            //Grey, slightly blueish
            effect.AmbientLightColor = new Vector3(0.34f, 0.48f, 0.484f);
            effect.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f);
            effect.SpecularColor = new Vector3(0.3f, 0.3f, 0.3f);
            
            if (skinningBones != null)
                UpdateVertices(skinningBones);

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                graphicsDevice.DepthStencilState = DepthStencilState.Default;
                graphicsDevice.BlendState = BlendState.Opaque;

                if (SceneManager.WireframeModeCharacters)
                {
                    RasterizerState rasterizerState = new RasterizerState();
                    rasterizerState.FillMode = FillMode.WireFrame;
                    graphicsDevice.RasterizerState = rasterizerState;
                }
                else
                {
                    graphicsDevice.RasterizerState = (SceneManager.ResolveLeftHandSymetry) ? RasterizerState.CullCounterClockwise : RasterizerState.CullClockwise;
                }

                pass.Apply();

                graphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, GpuVertexes, 0, GpuVertexes.Length, Indices, 0, Indices.Length / 3);
            }
        }

        private void UpdateVertices(Matrix[] boneTransforms)
        {
            int startIndex = 0;
            int elementCount = CpuVertexes.Length;
            Matrix transformSum = Matrix.Identity;

            // skin all of the vertices
            for (int i = startIndex; i < (startIndex + elementCount); i++)
            {
                GpuVertexes[i].Position = new Vector3(0, 0, 0);
                GpuVertexes[i].Position += Vector3.Transform(CpuVertexes[i].Position, boneTransforms[CpuVertexes[i].BlendIndex0]) * CpuVertexes[i].BlendWeights.X;          //todo save blend indice and weight by array to just use a for with 2 line for position and normal
                GpuVertexes[i].Position += Vector3.Transform(CpuVertexes[i].Position, boneTransforms[CpuVertexes[i].BlendIndex1]) * CpuVertexes[i].BlendWeights.Y;
                GpuVertexes[i].Position += Vector3.Transform(CpuVertexes[i].Position, boneTransforms[CpuVertexes[i].BlendIndex2]) * CpuVertexes[i].BlendWeights.Z;
                GpuVertexes[i].Position += Vector3.Transform(CpuVertexes[i].Position, boneTransforms[CpuVertexes[i].BlendIndex3]) * CpuVertexes[i].BlendWeights.W;

                GpuVertexes[i].Normal = new Vector3(0, 0, 0);
                GpuVertexes[i].Normal += Vector3.Transform(CpuVertexes[i].Normal, boneTransforms[CpuVertexes[i].BlendIndex0]) * CpuVertexes[i].BlendWeights.X;
                GpuVertexes[i].Normal += Vector3.Transform(CpuVertexes[i].Normal, boneTransforms[CpuVertexes[i].BlendIndex1]) * CpuVertexes[i].BlendWeights.Y;
                GpuVertexes[i].Normal += Vector3.Transform(CpuVertexes[i].Normal, boneTransforms[CpuVertexes[i].BlendIndex2]) * CpuVertexes[i].BlendWeights.Z;
                GpuVertexes[i].Normal += Vector3.Transform(CpuVertexes[i].Normal, boneTransforms[CpuVertexes[i].BlendIndex3]) * CpuVertexes[i].BlendWeights.W;
            }
        }
    }

    public struct PhysicsObject
    {
        public string Bone;
        public EmdFile Model;

        public PhysicsObject(string bone, byte[] modelBytes, GraphicsDevice graphicsDevice, CharacterSkeleton skeleton)
        {
            Bone = bone;
            Model = EmdFile.Load(modelBytes, graphicsDevice, skeleton);
        }

        public void Draw(GraphicsDevice graphicsDevice, Camera camera, Matrix world, Matrix[] skinningBones = null)
        {
            Model.Draw(graphicsDevice, camera, world, skinningBones);
        }

    }
}
