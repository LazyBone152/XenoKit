using System;
using System.Collections.Generic;
using System.Linq;
using Xv2CoreLib.EMA;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EMG;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMO;
using Xv2CoreLib.ESK;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Editor.Data
{
    [Serializable]
    public class SerializedModel
    {
        public const string CLIPBOARD_MODEL = "EmdModelSerialized";
        public const string CLIPBOARD_MESH = "EmdMeshSerialized";
        public const string CLIPBOARD_EMD_SUBMESH = "EmdSubmeshSerialized";

        //The model object to be serialized. Only one of these will be used, the rest will be null
        public EMD_Model[] EmdModel { get; set; }
        public EMD_Mesh[] EmdMesh { get; set; }
        public EMD_Submesh[] EmdSubmesh { get; set; }
        public EMG_File[] EmoModel { get; set; }
        public EMG_Mesh[] EmoMesh { get; set; }
        public string[] Bones { get; set; }

        //Material and textures (only those used by the model object will be serialized)
        public List<EmmMaterial> Materials { get; set; } = new List<EmmMaterial>();
        public List<EMB_TextureEntry> Textures { get; set; } = new List<EMB_TextureEntry>();

        public SerializedModel() { }

        public SerializedModel(EMD_Model[] emdModel, EMM_File emmFile, EMB_TextureFile embFile)
        {
            EmdModel = emdModel;

            foreach(var model in EmdModel)
            {
                foreach(var mesh in model.Meshes)
                {
                    foreach(var submesh in mesh.Submeshes)
                    {
                        CreateTextureAndMaterials(submesh, emmFile, embFile);
                    }
                }
            }
        }

        public SerializedModel(EMD_Mesh[] emdMesh, EMM_File emmFile, EMB_TextureFile embFile)
        {
            EmdMesh = emdMesh;

            foreach (var mesh in EmdMesh)
            {
                foreach (var submesh in mesh.Submeshes)
                {
                    CreateTextureAndMaterials(submesh, emmFile, embFile);
                }
            }
        }

        public SerializedModel(EMD_Submesh[] emdSubmesh, EMM_File emmFile, EMB_TextureFile embFile)
        {
            EmdSubmesh = emdSubmesh;

            foreach (var submesh in EmdSubmesh)
            {
                CreateTextureAndMaterials(submesh, emmFile, embFile);
            }
        }

        public SerializedModel(EMG_Mesh[] emgMesh, EMM_File emmFile, EMB_TextureFile embFile, Skeleton skeleton)
        {
            EmoMesh = emgMesh;
            Bones = skeleton.GetBoneNames();

            foreach (var mesh in EmoMesh)
            {
                foreach (var submeshGroup in mesh.SubmeshGroups)
                {
                    CreateTextureAndMaterials(submeshGroup, emmFile, embFile);
                }
            }
        }

        public SerializedModel(EMG_File[] emgFile, EMM_File emmFile, EMB_TextureFile embFile, Skeleton skeleton)
        {
            EmoModel = emgFile;
            Bones = skeleton.GetBoneNames();

            foreach (var emg in EmoModel)
            {
                foreach (var mesh in emg.EmgMeshes)
                {
                    foreach (var submeshGroup in mesh.SubmeshGroups)
                    {
                        CreateTextureAndMaterials(submeshGroup, emmFile, embFile);
                    }
                }
            }
        }

        private void CreateTextureAndMaterials(EMD_Submesh submesh, EMM_File emmFile, EMB_TextureFile embFile)
        {
            CreateTextures(submesh.TextureSamplerDefs, embFile);

            if(Materials.FirstOrDefault(x => x.Name == submesh.Name) == null)
            {
                var material = emmFile.GetMaterial(submesh.Name);

                if (material != null)
                    Materials.Add(material);
            }
        }

        private void CreateTextureAndMaterials(EMG_SubmeshGroup submeshGroup, EMM_File emmFile, EMB_TextureFile embFile)
        {
            CreateTextures(submeshGroup.TextureSamplerDefs, embFile);

            if (Materials.FirstOrDefault(x => x.Name == submeshGroup.MaterialName) == null)
            {
                var material = emmFile.GetMaterial(submeshGroup.MaterialName);

                if(material != null)
                    Materials.Add(material);
            }
        }

        private void CreateTextures(IList<EMD_TextureSamplerDef> textureDefs, EMB_TextureFile embFile)
        {
            foreach(var textureDef in textureDefs)
            {
                if (Textures.Any(x => x.ID == textureDef.EmbIndex)) continue;
                Textures.Add(embFile.GetEntry(textureDef.EmbIndex));
            }
        }

        #region Paste Methods
        public List<IUndoRedo> PasteTexturesAndMaterials(EMB_TextureFile embFile, EMM_File emmFile)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if(EmdModel != null)
            {
                foreach(var model in EmdModel)
                {
                    foreach(var mesh in model.Meshes)
                    {
                        PasteMaterials(mesh.Submeshes, emmFile, undos);

                        foreach(var submesh in mesh.Submeshes)
                        {
                            PasteTextures(submesh.TextureSamplerDefs, embFile, undos);
                        }
                    }
                }
            }
            else if(EmdMesh != null)
            {
                foreach (var mesh in EmdMesh)
                {
                    PasteMaterials(mesh.Submeshes, emmFile, undos);

                    foreach (var submesh in mesh.Submeshes)
                    {
                        PasteTextures(submesh.TextureSamplerDefs, embFile, undos);
                    }
                }
            }
            else if (EmdSubmesh != null)
            {
                PasteMaterials(EmdSubmesh, emmFile, undos);

                foreach (var submesh in EmdSubmesh)
                {
                    PasteTextures(submesh.TextureSamplerDefs, embFile, undos);
                }
            }
            else if (EmoMesh != null)
            {
                PasteMaterials(EmoMesh, emmFile, undos);

                foreach (var mesh in EmoMesh)
                {
                    foreach(var submesh in mesh.SubmeshGroups)
                    {
                        PasteTextures(submesh.TextureSamplerDefs, embFile, undos);
                    }
                }
            }

            return undos;
        }

        private void PasteMaterials(IEnumerable<EMD_Submesh> submeshes, EMM_File emmFile, List<IUndoRedo> undos)
        {
            foreach(var submesh in  submeshes)
            {
                if(emmFile.Materials.FirstOrDefault(x => x.Name == submesh.Name) == null)
                {
                    EmmMaterial material = Materials.FirstOrDefault(x => x.Name == submesh.Name);

                    if(material != null)
                    {
                        material.DecompileParameters();
                        emmFile.Materials.Add(material);
                        undos.Add(new UndoableListAdd<EmmMaterial>(emmFile.Materials, material));
                    }
                }
            }
        }

        private void PasteMaterials(IEnumerable<EMG_Mesh> meshes, EMM_File emmFile, List<IUndoRedo> undos)
        {
            foreach (var mesh in meshes)
            {
                foreach(var submesh in mesh.SubmeshGroups)
                {
                    if (emmFile.Materials.FirstOrDefault(x => x.Name == submesh.MaterialName) == null)
                    {
                        EmmMaterial material = Materials.FirstOrDefault(x => x.Name == submesh.MaterialName);

                        if (material != null)
                        {
                            material.DecompileParameters();
                            emmFile.Materials.Add(material);
                            undos.Add(new UndoableListAdd<EmmMaterial>(emmFile.Materials, material));
                        }
                    }
                }
            }
        }

        private void PasteTextures(IList<EMD_TextureSamplerDef> textures, EMB_TextureFile embFile, List<IUndoRedo> undos)
        {
            foreach(var texture in textures)
            {
                EMB_TextureEntry serializedEmb = Textures.FirstOrDefault(x => x.ID == texture.EmbIndex);

                if (serializedEmb != null)
                {
                    EMB_TextureEntry existingEntry = embFile.Compare(serializedEmb);

                    if(existingEntry != null)
                    {
                        texture.EmbIndex = (byte)embFile.Entry.IndexOf(existingEntry);
                    }
                    else
                    {
                        if(embFile.Entry.Count < EMB_File.MAX_EFFECT_TEXTURES)
                        {
                            texture.EmbIndex = (byte)embFile.Entry.Count;
                            embFile.AddTexture(serializedEmb);
                            undos.Add(new UndoableListAdd<EMB_TextureEntry>(embFile.Entry, serializedEmb));
                        }
                        else
                        {
                            Log.Add("Exceeded texture limit on paste operation, additional textures were NOT added.", LogType.Warning);
                        }
                    }
                }
            }
        }

        #endregion

        public EMD_Mesh[] GetEmdMesh(ESK_Skeleton targetSkeleton)
        {
            if (EmdMesh != null)
                return EmdMesh;

            if(EmoMesh != null)
            {
                return EMG_Mesh.ConvertToEmd(EmoMesh, targetSkeleton, Bones, targetSkeleton != null ? targetSkeleton.ESKBones[0].Name : null);
            }

            throw new Exception("SerializedModel.GetEmdMesh: this SerializedModel does not contain EmdMesh data.");
        }

        public EMG_Mesh[] GetEmoMesh(Skeleton targetSkeleton)
        {
            if (EmoMesh != null)
                return EmoMesh;

            if (EmdMesh != null)
            {
                return EMD_Mesh.ConvertToEmg(EmdMesh, targetSkeleton);
            }

            throw new Exception("SerializedModel.GetEmoMesh: this SerializedModel does not contain EmoMesh data.");
        }
    }
}