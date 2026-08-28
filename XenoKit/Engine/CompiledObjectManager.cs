using System;
using System.Collections.Generic;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Model;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EMG;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMO;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.ESK;
using Xv2CoreLib.NSK;

namespace XenoKit.Engine
{
    public class CompiledObjectManager
    {
        private readonly Dictionary<object, CompiledObjectCacheEntry> CachedObjects = new Dictionary<object, CompiledObjectCacheEntry>();

        public int ObjectCount => CachedObjects.Count;

        public void SlowUpdate()
        {
            RemoveDeadObjects();
        }

        #region AddGet
        /// <summary>
        /// Converts a source object into a compiled object, for valid types. All objects are cached.
        /// </summary>
        /// <typeparam name="T">The compiled object type</typeparam>
        /// <param name="key">The source object</param>
        /// <returns></returns>
        public T GetCompiledObject<T>(object key, ShaderType shaderType = ShaderType.Default, bool firstAttempt = true) where T : class
        {
            if (key == null) return null;

            object cacheKey = key;
            if (typeof(T) == typeof(Xv2ShaderEffect))
                cacheKey = Tuple.Create(key, shaderType);

            lock (CachedObjects)
            {
                CachedObjects.TryGetValue(cacheKey, out CompiledObjectCacheEntry cacheEntry);

                object result = cacheEntry?.CachedObject?.Target;

                if (cacheEntry?.IsAlive() == false)
                    RemoveDeadObjects();

                if (result == null)
                {
                    if (typeof(T) == typeof(PostShaderEffect) && key is ShaderProgram shaderProgram)
                    {
                        result = new PostShaderEffect(shaderProgram);
                    }
                    else if (typeof(T) == typeof(Xv2ShaderEffect) && key is EmmMaterial material)
                    {
                        result = new Xv2ShaderEffect(material, shaderType);
                    }
                    else if (typeof(T) == typeof(Xv2Texture) && key is EmbEntry embEntry)
                    {
                        result = new Xv2Texture(embEntry);
                    }
                    else if (typeof(T) == typeof(Xv2ModelFile) && key is EMD_File emdFile)
                    {
                        result = Xv2ModelFile.LoadEmd(emdFile);
                    }
                    else if (typeof(T) == typeof(Xv2ModelFile) && key is NSK_File nskFile)
                    {
                        result = Xv2ModelFile.LoadNsk(nskFile);
                    }
                    else if (typeof(T) == typeof(Xv2ModelFile) && key is EMO_File emoFile)
                    {
                        result = Xv2ModelFile.LoadEmo(emoFile);
                    }
                    else if (typeof(T) == typeof(Xv2Submesh) && key is EMG_File emgFile)
                    {
                        result = Xv2ModelFile.LoadEmg(emgFile);
                    }
                    else if (typeof(T) == typeof(Animation.Xv2Skeleton) && key is ESK_File eskFile)
                    {
                        result = new Animation.Xv2Skeleton(eskFile);
                    }
                    else if (typeof(T) == typeof(Vfx.Particle.ParticleEmissionData) && key is ParticleNode particleNode)
                    {
                        result = new Vfx.Particle.ParticleEmissionData(particleNode);
                    }
                    else if (typeof(T) == typeof(ModelScene) && key is Xv2ModelFile model)
                    {
                        result = new ModelScene(model);
                    }
                    else
                    {
                        Log.Add($"GetCompiledObject<{typeof(T)}>: key and source combination not valid (source: {key.GetType()}).", LogType.Error);
                        return null;
                    }


                    //Wacky thread safe guard
                    if (firstAttempt)
                    {
                        try
                        {
                            CachedObjects.Add(cacheKey, new CompiledObjectCacheEntry(cacheKey, result));
                        }
                        catch
                        {
                            return GetCompiledObject<T>(key, shaderType, firstAttempt: false);
                        }
                    }
                    else
                    {
                        CachedObjects.Add(cacheKey, new CompiledObjectCacheEntry(cacheKey, result));
                    }
                }

                return result as T;
            }

        }

        public void RemoveDeadObjects()
        {
            try
            {
                //int removed = 0;

                foreach (KeyValuePair<object, CompiledObjectCacheEntry> item in CachedObjects.Where(x => !x.Value.IsAlive()).ToList())
                {
                    CachedObjects.Remove(item.Key);
                    //removed++;
                }

                //if (removed > 0)
               //    Log.Add($"Removed {removed} dead objects", LogType.Debug);
            }
            catch { }
        }

        #endregion

        public void ForceShaderUpdate()
        {
            foreach(KeyValuePair<object, CompiledObjectCacheEntry> obj in CachedObjects)
            {
                if (obj.Value.CachedObject.IsAlive && obj.Value.CachedObject.Target is Xv2ShaderEffect shader)
                {
                    if(shader.ShaderType != ShaderType.CharaNormals)
                        shader.InitTechnique();
                }
            }
        }

        public void ForceShaderUpdate(string shaderProgram)
        {
            foreach (KeyValuePair<object, CompiledObjectCacheEntry> obj in CachedObjects)
            {
                if (obj.Value.CachedObject.IsAlive && obj.Value.CachedObject.Target is Xv2ShaderEffect shader)
                {
                    if (shader.shaderProgram != null)
                    {
                        if(shader.shaderProgram.Name == shaderProgram)
                        {
                            shader.InitTechnique();
                        }
                    }
                }
            }
        }

        public void ForceShaderUpdate(List<ShaderProgram> modifiedShaderPrograms)
        {
            foreach (KeyValuePair<object, CompiledObjectCacheEntry> obj in CachedObjects)
            {
                if (obj.Value.CachedObject.IsAlive && obj.Value.CachedObject.Target is Xv2ShaderEffect shader)
                {
                    if (shader.shaderProgram != null)
                    {
                        ShaderProgram newShaderProgram = modifiedShaderPrograms.FirstOrDefault(x => x.Name ==  shader.shaderProgram.Name);

                        if (newShaderProgram != null)
                        {
                            shader.SetShaderProgram(newShaderProgram);
                        }
                    }
                }
            }
        }
    }

    public class CompiledObjectCacheEntry
    {
        public object Key;
        public WeakReference CachedObject;

        public CompiledObjectCacheEntry (object key, object obj)
        {
            Key = key;
            CachedObject = new WeakReference(obj);
        }

        public bool IsAlive()
        {
            return CachedObject.IsAlive;
        }

        public bool CompareKey(object key)
        {
            return Key == key;
        }

        public bool IsOfType(Type type)
        {
            if (IsAlive())
            {
                return CachedObject.Target.GetType() == type;
            }
            return false;
        }
    }
}
