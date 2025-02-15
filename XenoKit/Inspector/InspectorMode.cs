using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Inspector.InspectorEntities;
using Xv2CoreLib;
using Xv2CoreLib.Resource;
using Xv2CoreLib.SPM;

namespace XenoKit.Inspector
{
    public class InspectorMode : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Singleton
        private static Lazy<InspectorMode> instance = new Lazy<InspectorMode>(() => new InspectorMode());
        public static InspectorMode Instance => instance.Value;

        private InspectorMode() { }
        #endregion

        private EanInspectorEntity _activeEanFile = null;
        private SkinnedInspectorEntity _activeSkinnedEntity = null;
        public EanInspectorEntity ActiveEanFile
        {
            get => _activeEanFile;
            set
            {
                _activeEanFile = value;
                NotifyPropertyChanged(nameof(ActiveEanFile));
            }
        }
        public SkinnedInspectorEntity ActiveSkinnedEntity
        {
            get => _activeSkinnedEntity;
            set
            {
                _activeSkinnedEntity = value;
                NotifyPropertyChanged(nameof(ActiveSkinnedEntity));
            }
        }

        public AsyncObservableCollection<SkinnedInspectorEntity> AllSkinnedEntities { get; private set; } = new AsyncObservableCollection<SkinnedInspectorEntity>();
        public AsyncObservableCollection<EanInspectorEntity> AllEanEntities { get; private set; } = new AsyncObservableCollection<EanInspectorEntity>();

        public AsyncObservableCollection<InspectorEntity> Entities { get; private set; } = new AsyncObservableCollection<InspectorEntity>();

        public bool SortOnLoadEnabled { get; set; } = true;

        #region File

        public void LoadFiles(string[] paths, InspectorEntity attachTo = null)
        {
            try
            {
                List<InspectorEntity> files = new List<InspectorEntity>();
                List<InspectorEntity> sortedFiles = new List<InspectorEntity>();
                List<InspectorEntity> sortAfterFiles = new List<InspectorEntity>();

                //Load files
                for (int i = 0; i < paths.Length; i++)
                {
                    switch (Path.GetExtension(paths[i]).ToLower())
                    {
                        case ".emd":
                        case ".emg":
                            files.Add(new MeshInspectorEntity(paths[i]));
                            break;
                        case ".esk":
                        case ".nsk":
                        case ".emo":
                            files.Add(new SkinnedInspectorEntity(paths[i]));
                            break;
                        case ".emb":
                            files.Add(new TextureInspectorEntity(paths[i]));
                            break;
                        case ".emm":
                            files.Add(new MaterialInspectorEntity(paths[i]));
                            break;
                        case ".ean":
                        case ".ema":
                            files.Add(new EanInspectorEntity(paths[i]));
                            break;
                        case ".spm":
                            SceneManager.SetDefaultSpm(SPM_File.Load(paths[i]));
                            Log.Add($"Default SPM set to \"{Path.GetFileName(paths[i])}\"");
                            break;
                    }
                }

                if (SortOnLoadEnabled)
                {
                    List<string> emdsLoaded = new List<string>();

                    //Sort files:
                    //-Attempt to attach character models to the ESK, and physics part EMDs to their ESKs, and then attach them to the character ESK, all based on the file name
                    //-Stages and EMOs are automatically attached since they are all packaged into the same file
                    for (int i = 0; i < files.Count; i++)
                    {
                        string extension = Path.GetExtension(files[i].Path).ToLower();

                        if (extension == ".esk")
                        {
                            bool isScd = Path.GetFileNameWithoutExtension(files[i].Path).Length != 7;

                            //Sort SCDs in a second pass, as the character ESK must be sorted first
                            if (isScd)
                            {
                                sortAfterFiles.Add(files[i]);
                                files.RemoveAt(i);
                                i--;
                                continue;
                            }

                            InspectorEntity skeleton = files[i];
                            string charaCode = Path.GetFileNameWithoutExtension(files[i].Path).Substring(0, 3);

                            //Fetch all EMDs that contain this chara code in their path and attach them to this skeleton
                            for (int a = files.Count - 1; a >= 0; a--)
                            {
                                if (Path.GetExtension(files[a].Path).ToLower() == ".emd")
                                {
                                    if (Path.GetFileNameWithoutExtension(files[a].Path).Contains($"{charaCode}_") && !Path.GetFileNameWithoutExtension(files[a].Path).Contains($"_scd"))
                                    {
                                        if (files[a] is MeshInspectorEntity mesh)
                                            mesh.Parent = skeleton as SkinnedInspectorEntity;

                                        emdsLoaded.Add(Utils.SanitizePath($"{Path.GetDirectoryName(files[a].Path)}/{Path.GetFileNameWithoutExtension(files[a].Path)}"));
                                        skeleton.ChildEntities.Add(files[a]);
                                        files.RemoveAt(a);
                                    }
                                }
                            }

                            files.Remove(skeleton);
                            sortedFiles.Add(skeleton);

                            //Reset iterator
                            i = -1;
                        }
                    }

                    //Sort SCDs:
                    for (int i = 0; i < sortAfterFiles.Count; i++)
                    {
                        string extension = Path.GetExtension(sortAfterFiles[i].Path).ToLower();

                        if (extension == ".esk")
                        {
                            bool isScd = Path.GetFileNameWithoutExtension(sortAfterFiles[i].Path).Length != 7;

                            if (!isScd)
                            {
                                throw new Exception("non SCD file");
                            }

                            InspectorEntity skeleton = sortAfterFiles[i];
                            string path = Utils.SanitizePath($"{Path.GetDirectoryName(skeleton.Path)}/{Path.GetFileNameWithoutExtension(skeleton.Path)}");
                            string charaCode = Path.GetFileNameWithoutExtension(sortAfterFiles[i].Path).Substring(0, 3);

                            //Fetch the EMD that match this file path and attach them to the SCD skeleton
                            for (int a = files.Count - 1; a >= 0; a--)
                            {
                                if (Utils.SanitizePath(files[a].Path) == $"{path}.emd")
                                {
                                    if (files[a] is MeshInspectorEntity mesh)
                                        mesh.Parent = skeleton as SkinnedInspectorEntity;

                                    emdsLoaded.Add(path);
                                    skeleton.ChildEntities.Add(files[a]);
                                    files.RemoveAt(a);
                                    break;
                                }
                            }

                            //Add SCD ESK onto a parent ESK
                            SkinnedInspectorEntity parentEsk = null;

                            foreach (var _file in sortedFiles)
                            {
                                if (Path.GetFileNameWithoutExtension(_file.Path).Substring(0, 3) == charaCode && _file is SkinnedInspectorEntity)
                                {
                                    parentEsk = _file as SkinnedInspectorEntity;
                                    break;
                                }
                            }

                            if (parentEsk != null)
                            {
                                sortAfterFiles.Remove(skeleton);
                                parentEsk.ChildEntities.Add(skeleton);
                            }
                            else
                            {
                                sortAfterFiles.Remove(skeleton);
                                sortedFiles.Add(skeleton);
                            }

                            i -= 1;
                        }
                    }

                    //Remove EMBs and EMMs from files for any loaded EMD. This is to prevent clutter when all of a characters files are selected for load
                    foreach (string emdPath in emdsLoaded)
                    {
                        files.RemoveAll(x => Utils.SanitizePath(x.Path) == $"{emdPath}.emb");
                        files.RemoveAll(x => Utils.SanitizePath(x.Path) == $"{emdPath}.dyt.emb");
                        files.RemoveAll(x => Utils.SanitizePath(x.Path) == $"{emdPath}.emm");
                    }
                }

                //Add in all unsorted files
                sortedFiles.AddRange(files);

                //Attempt to attach files to the file it was dropped onto
                if (attachTo != null)
                {
                    for (int i = sortedFiles.Count - 1; i >= 0; i--)
                    {
                        MeshInspectorEntity meshParent = attachTo as MeshInspectorEntity;
                        SkinnedInspectorEntity skinnedParent = attachTo as SkinnedInspectorEntity;

                        if (sortedFiles[i] is TextureInspectorEntity texture && meshParent != null)
                        {
                            if (texture.IsDyt)
                            {
                                meshParent.AddDyt(texture);
                            }
                            else
                            {
                                meshParent.AddTexture(texture);
                            }

                            sortedFiles.RemoveAt(i);
                        }
                        else if (sortedFiles[i] is MaterialInspectorEntity material && meshParent != null)
                        {
                            meshParent.AddMaterial(material);
                            sortedFiles.RemoveAt(i);
                        }
                        else if (sortedFiles[i] is SkinnedInspectorEntity && skinnedParent != null)
                        {
                            AddFile(sortedFiles[i], skinnedParent.ChildEntities);
                            sortedFiles.RemoveAt(i);
                        }
                        else if (sortedFiles[i] is MeshInspectorEntity mesh && skinnedParent != null)
                        {
                            mesh.Parent = skinnedParent;
                            AddFile(sortedFiles[i], skinnedParent.ChildEntities);
                            sortedFiles.RemoveAt(i);
                        }

                        //MeshInspectorEntity.CheckDrawOrder(attachTo.ChildEntities);
                    }
                }

                //Add files
                AddFiles(sortedFiles);
            }
            catch (Exception ex)
            {
                Log.Add("File Load Error: " + ex.Message, ex.ToString(), LogType.Error);
            }
        }

        public void SaveFiles()
        {
            InternalSaveFiles(Entities);
        }

        public void ReloadFiles()
        {
            InternalReloadFiles(Entities);
        }

        public void AddFiles(IList<InspectorEntity> files)
        {
            foreach (InspectorEntity file in files)
            {
                AddFile(file);

                if(file is SkinnedInspectorEntity skinned)
                {
                    //MeshInspectorEntity.CheckDrawOrder(skinned.ChildEntities);
                }
            }

            //MeshInspectorEntity.CheckDrawOrder(Entities);
        }

        public void AddFile(InspectorEntity file, IList<InspectorEntity> entities = null)
        {
            if (file is MeshInspectorEntity)
            {
                SceneManager.MainGameBase.RenderSystem.AddRenderEntity(file);
            }
            else if (file is SkinnedInspectorEntity skinned)
            {
                InternalAddAllRenderEntities(file.ChildEntities);

                //AllSkinnedEntities should only contain the root level skeletons, not SCDs
                if ((entities == null || entities == Entities) && !AllSkinnedEntities.Contains(skinned))
                {
                    AllSkinnedEntities.Add(skinned);

                    if (ActiveSkinnedEntity == null)
                        ActiveSkinnedEntity = skinned;
                }
            }
            else if (file is EanInspectorEntity ean)
            {
                if(!AllEanEntities.Contains(ean))
                    AllEanEntities.Add(ean);

                if (ActiveEanFile == null)
                    ActiveEanFile = ean;
            }

            if (entities == null)
            {
                Entities.Add(file);
            }
            else
            {
                entities.Add(file);
            }
        }

        public void RemoveFiles(IList<InspectorEntity> files)
        {
            foreach (InspectorEntity file in files)
            {
                RemoveFile(file);
            }
        }

        public void RemoveFile(InspectorEntity file)
        {
            //Before the entity can be removed, any meshes attached to it need to be removed from the renderer, and if it is a Texture/Material then it needs to be removed from its parent mesh
            if (file is MeshInspectorEntity)
            {
                SceneManager.MainGameBase.RenderSystem.RemoveRenderEntity(file);
            }
            else if (file is SkinnedInspectorEntity skinned)
            {
                AllSkinnedEntities.Remove(skinned);
                InternalRemoveAllRenderEntities(file.ChildEntities);
            }
            else if (file is MaterialInspectorEntity)
            {
                MeshInspectorEntity mesh = GetParent(file) as MeshInspectorEntity;

                if(mesh != null)
                {
                    mesh.AddMaterial(null);
                }
            }
            else if (file is TextureInspectorEntity texture)
            {
                MeshInspectorEntity mesh = GetParent(file) as MeshInspectorEntity;

                if (mesh != null)
                {
                    if (texture.IsDyt)
                    {
                        mesh.AddDyt(null);
                    }
                    else
                    {
                        mesh.AddTexture(null);
                    }
                }
            }
            else if (file is EanInspectorEntity ean)
            {
                AllEanEntities.Remove(ean);
            }

            file.Dispose();
            InternalRemoveFile(file, Entities);
        }

        public void ClearFiles()
        {
            InternalRemoveAllRenderEntities(Entities);
            InternalDisposeAllFiles(Entities);
            Entities.Clear();
            AllSkinnedEntities.Clear();
            AllEanEntities.Clear();
        }

        public InspectorEntity GetParent(InspectorEntity file)
        {
            return InternalGetParent(Entities, file);
        }

        public void ChangeParent(InspectorEntity file, InspectorEntity newParent)
        {
            //Remove from old parent
            InspectorEntity parent = GetParent(file);

            if(parent != null)
            {
                parent.ChildEntities.Remove(file);

                //Remove texture/dyt/mat references on old parent mesh
                if (parent is MeshInspectorEntity oldParentMesh)
                {
                    if (file is TextureInspectorEntity texture)
                    {
                        if (texture.IsDyt)
                            oldParentMesh.AddDyt(null);
                        else
                            oldParentMesh.AddTexture(null);
                    }
                    else if (file is MaterialInspectorEntity)
                    {
                        oldParentMesh.AddMaterial(null);
                    }
                }
            }
            else
            {
                Entities.Remove(file);
            }

            //Add to new parent
            if (newParent is MeshInspectorEntity newParentMesh)
            {
                //Add texture/dyt/mat references to new parent mesh
                if (file is TextureInspectorEntity texture)
                {
                    if (texture.IsDyt)
                        newParentMesh.AddDyt(texture);
                    else
                        newParentMesh.AddTexture(texture);
                }
                else if (file is MaterialInspectorEntity mat)
                {
                    newParentMesh.AddMaterial(mat);
                }
            }
            else
            {
                //No special references, so we just need to add it to the child list
                IList<InspectorEntity> siblings = (newParent != null) ? newParent.ChildEntities : Entities;
                siblings.Add(file);
            }

            //Set skinned parent reference
            if (file is MeshInspectorEntity mesh)
            {
                mesh.Parent = newParent != null ? newParent as SkinnedInspectorEntity : null;
            }

            //Add/remove from AllSkinnedEntities
            if(file is SkinnedInspectorEntity skinned)
            {
                if (newParent == null && !AllSkinnedEntities.Contains(skinned))
                    AllSkinnedEntities.Add(skinned);

                if (newParent != null && AllSkinnedEntities.Contains(skinned))
                    AllSkinnedEntities.Remove(skinned);
            }

            /*
            //Check draw order
            if(newParent != null && file is MeshInspectorEntity)
            {
                MeshInspectorEntity.CheckDrawOrder(newParent.ChildEntities);
            }
            else
            {
                MeshInspectorEntity.CheckDrawOrder(Entities);
            }
            */
        }
        #endregion

        #region RecursiveFunctions
        private void InternalRemoveFile(InspectorEntity entity, IList<InspectorEntity> entities)
        {
            if (entities.Contains(entity))
            {
                entities.Remove(entity);
            }
            else
            {
                foreach(InspectorEntity item in entities)
                {
                    InternalRemoveFile(entity, item.ChildEntities);
                }
            }
        }

        private void InternalRemoveAllRenderEntities(IList<InspectorEntity> entities)
        {
            //Remove ALL meshes attached to this entity from the render system

            foreach(InspectorEntity item in entities)
            {
                if(item is MeshInspectorEntity)
                {
                    SceneManager.MainGameBase.RenderSystem.RemoveRenderEntity(item);
                }

                if(item.ChildEntities != null)
                    InternalRemoveAllRenderEntities(item.ChildEntities);
            }
        }

        public void InternalAddAllRenderEntities(IList<InspectorEntity> entities)
        {
            foreach (InspectorEntity item in entities)
            {
                if (item is MeshInspectorEntity)
                {
                    SceneManager.MainGameBase.RenderSystem.AddRenderEntity(item);
                }

                if (item.ChildEntities != null)
                    InternalAddAllRenderEntities(item.ChildEntities);
            }
        }

        private void InternalSaveFiles(IList<InspectorEntity> entities)
        {
            foreach(InspectorEntity file in entities)
            {
                file.Save();

                if (file.ChildEntities != null)
                    InternalSaveFiles(file.ChildEntities);
            }
        }

        private void InternalReloadFiles(IList<InspectorEntity> entities)
        {
            foreach (InspectorEntity file in entities)
            {
                file.Load();

                if (file.ChildEntities != null)
                    InternalReloadFiles(file.ChildEntities);
            }
        }

        private void InternalDisposeAllFiles(IList<InspectorEntity> entities)
        {
            foreach (InspectorEntity file in entities)
            {
                file.Dispose();

                if (file.ChildEntities != null)
                    InternalDisposeAllFiles(file.ChildEntities);
            }
        }

        public void InternalSetDyt(IList<InspectorEntity> entities, int dyt)
        {
            foreach (InspectorEntity file in entities)
            {
                if (file is TextureInspectorEntity texture)
                    texture.DytIndex = dyt;

                if (file.ChildEntities != null)
                    InternalSetDyt(file.ChildEntities, dyt);
            }
        }

        private InspectorEntity InternalGetParent(IList<InspectorEntity> entities, InspectorEntity entity)
        {
            foreach (InspectorEntity file in entities)
            {
                if(file.ChildEntities.Contains(entity))
                {
                    return file;
                }

                InspectorEntity parent = InternalGetParent(file.ChildEntities, entity);

                if (parent != null)
                    return parent;
            }

            return null;
        }
        #endregion

        public void Update()
        {
            foreach(InspectorEntity entity in Entities)
            {
                entity.Update();
            }

            //Update animations next
        }

    }

    public enum DrawStage
    {
        Normals,
        Chara,
        Stage
    }
}
