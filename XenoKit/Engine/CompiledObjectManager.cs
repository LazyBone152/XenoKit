using System;
using System.Collections.Generic;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Model;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using Xv2CoreLib.EMA;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMO;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.ESK;

namespace XenoKit.Engine
{
    public class CompiledObjectManager : IDisposable
    {
        private readonly Dictionary<object, CompiledObjectCacheEntry> CachedObjects = new Dictionary<object, CompiledObjectCacheEntry>();

        public CompiledObjectManager()
        {
            SceneManager.SlowUpdate += SceneManager_SlowUpdate;
        }

        private void SceneManager_SlowUpdate(object sender, EventArgs e)
        {
            RemoveDeadObjects();
        }

        public void Dispose()
        {
            SceneManager.SlowUpdate -= SceneManager_SlowUpdate;
        }

        #region AddGet
        /// <summary>
        /// Converts a source object into a compiled object, for valid types. All objects are cached.
        /// </summary>
        /// <typeparam name="T">The compiled object type</typeparam>
        /// <param name="key">The source object</param>
        /// <returns></returns>
        public T GetCompiledObject<T>(object key, GameBase gameInstance, bool firstAttempt = true) where T : class
        {
            if (key == null) return null;

            lock (CachedObjects)
            {

                CachedObjects.TryGetValue(key, out CompiledObjectCacheEntry cacheEntry);

                object result = cacheEntry?.CachedObject?.Target;

                if (cacheEntry?.IsAlive() == false)
                    RemoveDeadObjects();

                if (result == null)
                {
                    if (typeof(T) == typeof(Xv2ShaderEffect) && key is EmmMaterial material)
                    {
                        result = new Xv2ShaderEffect(material, gameInstance);
                    }
                    else if (typeof(T) == typeof(Xv2Texture) && key is EmbEntry embEntry)
                    {
                        result = new Xv2Texture(embEntry, gameInstance);
                    }
                    else if (typeof(T) == typeof(Xv2ModelFile) && key is EMD_File emdFile)
                    {
                        result = Xv2ModelFile.LoadEmd(gameInstance, emdFile);
                    }
                    else if (typeof(T) == typeof(Xv2ModelFile) && key is EMO_File emoFile)
                    {
                        result = Xv2ModelFile.LoadEmo(gameInstance, emoFile);
                    }
                    else if (typeof(T) == typeof(Animation.Xv2Skeleton) && key is ESK_File eskFile)
                    {
                        result = new Animation.Xv2Skeleton(eskFile);
                    }
                    else if (typeof(T) == typeof(Animation.Xv2Skeleton) && key is Skeleton emoSkeleton)
                    {
                        result = new Animation.Xv2Skeleton(emoSkeleton);
                    }
                    else if (typeof(T) == typeof(Vfx.Particle.ParticleEmissionData) && key is ParticleNode particleNode)
                    {
                        result = new Vfx.Particle.ParticleEmissionData(particleNode, gameInstance);
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
                            CachedObjects.Add(key, new CompiledObjectCacheEntry(key, result, gameInstance));
                        }
                        catch
                        {
                            return GetCompiledObject<T>(key, gameInstance, false);
                        }
                    }
                    else
                    {
                        CachedObjects.Add(key, new CompiledObjectCacheEntry(key, result, gameInstance));
                    }
                }

                return result as T;
            }

        }

        private void RemoveDeadObjects()
        {
            int removed = 0;

            foreach (KeyValuePair<object, CompiledObjectCacheEntry> item in CachedObjects.Where(x => !x.Value.IsAlive()).ToList())
            {
                CachedObjects.Remove(item.Key);
                removed++;
            }

            if(removed > 0)
                Log.Add($"Removed {removed} dead objects", LogType.Debug);
        }

        #endregion

        #region ModelFunctions
        public void UnsetActorOnModels(int actor)
        {
            foreach(KeyValuePair<object, CompiledObjectCacheEntry> model in CachedObjects.Where(x => x.Value.IsOfType(typeof(Xv2ModelFile))))
            {
                if(model.Value.CachedObject.Target is Xv2ModelFile xv2Model)
                {
                    xv2Model.UnsetActor(actor);
                }
            }
        }
        #endregion

        public void ForceShaderUpdate()
        {
            foreach(var obj in CachedObjects)
            {
                if (obj.Value.CachedObject.IsAlive && obj.Value.CachedObject.Target is Xv2ShaderEffect shader)
                {
                    shader.InitTechnique();
                }
            }
        }
    }

    public class CompiledObjectCacheEntry
    {
        public GameBase GameInstance { get; private set; }
        public object Key;
        public WeakReference CachedObject;

        public CompiledObjectCacheEntry (object key, object obj, GameBase gameInstance)
        {
            Key = key;
            CachedObject = new WeakReference(obj);
            GameInstance = gameInstance;
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
