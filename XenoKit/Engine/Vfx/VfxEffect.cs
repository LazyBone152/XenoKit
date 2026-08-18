using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using XenoKit.Engine.Vfx.Asset;
using XenoKit.Engine.Vfx.Particle;
using Xv2CoreLib.EEPK;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine.Vfx
{
    public class VfxEffect : RenderObject, IDisposable
    {
        public Effect Effect { get; private set; }
        public Actor Actor { get; private set; }
        public List<VfxAsset> Assets { get; private set; }

        private Matrix4x4 SpawnTransform;

        private readonly bool IsAssetPreview = false;
        private readonly EffectPart EffectPart = null;
        private readonly bool SpawnedByProjectile;
        private bool isDisposed;

        public VfxEffect(Actor actor, Effect effect, Matrix4x4 world, bool spawnedByProjectile = false)
        {
            Effect = effect;
            Actor = actor;
            SpawnTransform = world;
            SpawnedByProjectile = spawnedByProjectile;

            Initialize();
            effect.EffectParts.CollectionChanged += EffectParts_CollectionChanged;
        }

        public VfxEffect(Actor actor, EffectPart effectPart)
        {
            IsAssetPreview = true;
            EffectPart = effectPart;
            Actor = actor;
            SpawnTransform = Matrix4x4.Identity;
            InitializeFromAsset();
        }

        public void InitializeFromAsset()
        {
            if (Assets == null)
            {
                Assets = new List<VfxAsset>(1);
            }
            else
            {
                foreach (VfxAsset asset in Assets)
                {
                    asset.Dispose();
                }

                Assets.Clear();
            }

            if (EffectPart.AssetType == AssetType.CBIND)
            {
                if (EffectPart.AssetRef.Files[0].EcfFile == null) return;
                Assets.Add(new VfxColorFade(EffectPart.AssetRef.Files[0].EcfFile, EffectPart, Actor, SpawnedByProjectile));
            }
            else if (EffectPart.AssetType == AssetType.EMO)
            {
                Assets.Add(new VfxEmo(SpawnTransform, EffectPart.AssetRef, EffectPart, Actor, SpawnedByProjectile));
            }
            else if (EffectPart.AssetType == AssetType.LIGHT)
            {
                if (EffectPart.AssetRef.Files[0].EmaFile == null) return;
                Assets.Add(new VfxLight(SpawnTransform, EffectPart.AssetRef.Files[0].EmaFile, EffectPart, Actor, SpawnedByProjectile));
            }
            else if (EffectPart.AssetType == AssetType.PBIND)
            {
                if (EffectPart.AssetRef.Files[0].EmpFile == null) return;
                Assets.Add(new ParticleSystem(SpawnTransform, Actor, EffectPart, EffectPart.AssetRef.Files[0].EmpFile, this, SpawnedByProjectile));
            }
            else if (EffectPart.AssetType == AssetType.TBIND)
            {
                if (EffectPart.AssetRef.Files[0].EtrFile == null) return;
                Assets.Add(new VfxTbind(SpawnTransform, EffectPart.AssetRef.Files[0].EtrFile, EffectPart, Actor, SpawnedByProjectile));
            }
        }

        public void Initialize()
        {
            if (Assets == null)
            {
                Assets = new List<VfxAsset>(Effect.EffectParts.Count);
            }
            else
            {
                foreach (VfxAsset asset in Assets)
                {
                    asset.Dispose();
                }

                Assets.Clear();
            }

            foreach (EffectPart effectPart in Effect.EffectParts)
            {
                if (effectPart.AssetRef == null) continue;

                if (effectPart.AssetType == AssetType.CBIND)
                {
                    if (effectPart.AssetRef.Files[0].EcfFile == null) continue;
                    Assets.Add(new VfxColorFade(effectPart.AssetRef.Files[0].EcfFile, effectPart, Actor, SpawnedByProjectile));
                }
                else if (effectPart.AssetType == AssetType.EMO)
                {
                    Assets.Add(new VfxEmo(SpawnTransform, effectPart.AssetRef, effectPart, Actor, SpawnedByProjectile));
                }
                else if (effectPart.AssetType == AssetType.LIGHT)
                {
                    if (effectPart.AssetRef.Files[0].EmaFile == null) continue;
                    Assets.Add(new VfxLight(SpawnTransform, effectPart.AssetRef.Files[0].EmaFile, effectPart, Actor, SpawnedByProjectile));
                }
                else if (effectPart.AssetType == AssetType.PBIND)
                {
                    if (effectPart.AssetRef.Files[0].EmpFile == null) continue;
                    Assets.Add(new ParticleSystem(SpawnTransform, Actor, effectPart, effectPart.AssetRef.Files[0].EmpFile, this, SpawnedByProjectile));
                }
                else if (effectPart.AssetType == AssetType.TBIND)
                {
                    if (effectPart.AssetRef.Files[0].EtrFile == null) continue;
                    Assets.Add(new VfxTbind(SpawnTransform, effectPart.AssetRef.Files[0].EtrFile, effectPart, Actor, SpawnedByProjectile));
                }
            }
        }

        private void EffectParts_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Initialize();
        }

        public void Terminate(bool force)
        {
            if (force)
            {
                Dispose();
                Destroy();
                return;
            }

            foreach (VfxAsset asset in Assets)
            {
                asset.Terminate();
            }
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            if (Effect != null)
                Effect.EffectParts.CollectionChanged -= EffectParts_CollectionChanged;

            if (Assets == null)
                return;

            foreach (VfxAsset asset in Assets)
            {
                asset.Dispose();
            }

            Assets.Clear();
        }

        public void SetExternalTransform(Matrix4x4 transform)
        {
            SpawnTransform = transform;

            foreach (VfxAsset asset in Assets)
                asset.SetExternalTransform(transform);
        }

        public override void Update()
        {
            Update(false);
        }

        private void Update(bool simulate)
        {
            for (int i = Assets.Count - 1; i >= 0; i--)
            {
                if (Assets[i].IsFinished)
                {
                    Assets[i].Dispose();
                    Assets.RemoveAt(i);
                    continue;
                }

                if (Assets[i].AssetTypeChanged)
                {
                    //An assets type was changed
                    //End frame update and reinitialize the effect
                    Initialize();
                    break;
                }

                //var asset = Assets[i];
                //Task updateTask = new Task(() => asset.Update());
                //VfxManager.CurrentTasks.Add(updateTask);
                //updateTask.Start();

                if (simulate)
                    Assets[i].Simulate();
                else
                    Assets[i].Update();
            }

            if (Assets.Count == 0)
            {
                if (!IsAssetPreview)
                {
                    //Handle end of effect:
                    //--Restart the effect if editor loop option is enabled, and on Effects tab
                    //--Otherwise, destroy the effect

                    if (Effect.EffectParts.Count > 0 && SceneManager.IsOnTab(EditorTabs.Effect))
                    {
                        Initialize();

                        if (!SceneManager.Loop)
                            ViewportInstance.IsPlaying = false;
                    }
                    else
                    {
                        Destroy();
                    }
                }
                else
                {
                    InitializeFromAsset();
                }

            }
        }

        public void Simulate()
        {
            Update(true);
        }

        public void SeekNextFrame()
        {
            foreach(VfxAsset asset in Assets)
            {
                asset.SeekNextFrame();
            }
        }

        public void SeekPrevFrame()
        {
            foreach (VfxAsset asset in Assets)
            {
                asset.SeekPrevFrame();
            }
        }
    }
}
