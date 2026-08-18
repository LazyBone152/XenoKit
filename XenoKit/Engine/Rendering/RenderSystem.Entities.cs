using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Model;
using XenoKit.Engine.Shader;
using XenoKit.Inspector.InspectorEntities;
using Xv2CoreLib.EMM;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Rendering
{
    public partial class RenderSystem : RenderObject
    {
        public override void Update()
        {
            SetRenderResolution();

            if (RecreateRenderTargetsNextFrames > 0)
            {
                DelayedUpdate();
                RecreateRenderTargetsNextFrames -= 1;
            }

            EntityListUpdate(Characters, CharasToAdd, CharasToRemove);
            EntityListUpdate(Stages, StagesToAdd, StagesToRemove);
            EntityListUpdate(Effects, EffectsToAdd, EffectsToRemove);
            EntityListUpdate(Reflections, ReflectionsToAdd, ReflectionsToRemove);

            if (SceneManager.UseRenderScene)
                RenderScene.Update();

            ParticleBatcher.Update();
        }

        public override void DelayedUpdate()
        {
            DrawThisFrame = false;
            PostFilter.DelayedUpdate();

            //Dispose of previous RTs. (Apparantly this should be done on the next frame of when it was last used, so this goes before the render target update, instead of at the end of this method)
            if (_toBeDisposed.Count > 0)
            {
                foreach (RenderTarget2D rt in _toBeDisposed)
                {
                    if (!rt.IsDisposed)
                        rt.Dispose();
                }

                _toBeDisposed.Clear();
            }

            bool resolutionChanged = false;

            //Update RTs if the ViewPort size has changed.
            foreach (RenderTargetWrapper rt in registeredRenderTargets)
            {
                if (rt.ShouldUpdate() && ViewportIsFocused)
                {
                    resolutionChanged = true;

                    if (rt.RenderTarget != null)
                        _toBeDisposed.Add(rt.RenderTarget);

                    rt.UpdateRenderTarget();
                }
            }

            if (resolutionChanged)
            {
                SuperSampleFactor = ViewportInstance.IsFullScreen ? 1f : SettingsManager.settings.XenoKit_SuperSamplingFactor;
            }

            DrawThisFrame = true;
        }

        public void SlowUpdate()
        {
            ParticleBatcher.SlowUpdate();
        }

        private void EntityListUpdate(List<RenderObject> entities, List<RenderObject> entitiesToAdd, List<RenderObject> entitiesToRemove)
        {
            if (entitiesToRemove.Count > 0)
            {
                foreach (RenderObject entity in entitiesToRemove)
                {
                    entities.Remove(entity);
                }

                entitiesToRemove.Clear();
            }

            if (entitiesToAdd.Count > 0)
            {
                foreach (RenderObject entity in entitiesToAdd)
                {
                    if (!entities.Contains(entity))
                        entities.Add(entity);
                }

                entitiesToAdd.Clear();
            }

            for (int i = entities.Count - 1; i >= 0; i--)
            {
                if (entities[i].IsDestroyed)
                {
                    entities.RemoveAt(i);
                }
            }
        }

        public void AddReflectionRenderEntity(RenderObject entity)
        {
            if (entity == null) return;

            if(entity.EngineObjectType == EngineObjectTypeEnum.Stage)
            {
                if (!Reflections.Contains(entity))
                {
                    if (entity is MeshInspectorEntity mesh)
                    {
                        mesh.SetAsReflectionMesh(true);
                    }
                    else if (entity is LodGroup lod)
                    {
                        lod.SetAsReflectionMesh(true);
                    }

                    QueueRenderEntityAdd(Reflections, ReflectionsToAdd, entity);
                }
            }
        }

        public void RemoveReflectionRenderEntity(RenderObject entity)
        {
            if (entity == null) return;

            if (entity.EngineObjectType == EngineObjectTypeEnum.Stage)
            {
                ReflectionsToAdd.Remove(entity);

                if (Reflections.Contains(entity))
                {
                    if (entity is MeshInspectorEntity mesh)
                    {
                        mesh.SetAsReflectionMesh(false);
                    }

                    ReflectionsToRemove.Add(entity);
                }
            }
        }

        public void RemoveAllReflectionRenderEntity()
        {
            ReflectionsToAdd.Clear();
            ReflectionsToRemove.AddRange(Reflections);
        }

        public void AddRenderEntity(RenderObject entity)
        {
            if (entity == null) return;

            switch (entity.EngineObjectType)
            {
                case EngineObjectTypeEnum.Actor:
                    QueueRenderEntityAdd(Characters, CharasToAdd, entity);
                    break;
                case EngineObjectTypeEnum.Stage:
                    QueueRenderEntityAdd(Stages, StagesToAdd, entity);
                    break;
                case EngineObjectTypeEnum.VFX:
                case EngineObjectTypeEnum.Model: //Currently Xv2Submesh is only used in this case for an EMO. If that ever changes, this will also need to be changed
                    QueueRenderEntityAdd(Effects, EffectsToAdd, entity);
                    break;
                default:
                    Log.Add($"RenderSystem: Cannot add EntityType {entity.EngineObjectType}!", LogType.Debug);
                    break;
            }
        }

        public void RemoveRenderEntity(RenderObject entity)
        {
            if (entity == null) return;

            switch (entity.EngineObjectType)
            {
                case EngineObjectTypeEnum.Actor:
                    CharasToAdd.Remove(entity);
                    CharasToRemove.Add(entity);
                    break;
                case EngineObjectTypeEnum.Stage:
                    StagesToAdd.Remove(entity);
                    StagesToRemove.Add(entity);
                    break;
                case EngineObjectTypeEnum.VFX:
                case EngineObjectTypeEnum.Model: //Currently Xv2Submesh is only used in this case for an EMO. If that ever changes, this will also need to be changed
                    EffectsToAdd.Remove(entity);
                    EffectsToRemove.Add(entity);
                    break;
                default:
                    Log.Add($"RenderSystem: Cannot remove EntityType {entity.EngineObjectType}!", LogType.Debug);
                    break;
            }
        }

        public void AddRenderEntity<T>(IEnumerable<T> entities) where T : RenderObject
        {
            foreach (T entity in entities)
            {
                AddRenderEntity(entity);
            }
        }

        public void RemoveRenderEntity<T>(IEnumerable<T> entities) where T : RenderObject
        {
            foreach(T entity in entities)
            {
                RemoveRenderEntity(entity);
            }
        }

        public void MoveRenderEntityToFront(RenderObject entity)
        {
            if (Characters.Contains(entity))
            {
                Characters.Remove(entity);
                Characters.Add(entity);
            }
            else if (Stages.Contains(entity))
            {
                Stages.Remove(entity);
                Stages.Add(entity);
            }
            else if (Effects.Contains(entity))
            {
                Effects.Remove(entity);
                Effects.Add(entity);
            }
        }

        private static void QueueRenderEntityAdd(List<RenderObject> entities, List<RenderObject> entitiesToAdd, RenderObject entity)
        {
            if (!entities.Contains(entity) && !entitiesToAdd.Contains(entity))
                entitiesToAdd.Add(entity);
        }

        public void SetRenderScene(RenderScene scene)
        {
            RenderScene = scene;
        }

    }
}
