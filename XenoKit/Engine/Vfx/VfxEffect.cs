using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using XenoKit.Engine.Vfx.Asset;
using Xv2CoreLib.EEPK;

namespace XenoKit.Engine.Vfx
{
    public class VfxEffect : Entity
    {
        public Effect Effect { get; private set; }
        public Actor Actor { get; private set; }
        public List<VfxAsset> Assets { get; private set; }

        private Matrix SpawnTransform;

        public VfxEffect(Actor actor, Effect effect, Matrix world, GameBase gameBase) : base(gameBase)
        {
            Effect = effect;
            Actor = actor;
            SpawnTransform = world;

            Initialize();
            effect.EffectParts.CollectionChanged += EffectParts_CollectionChanged;
        }

        private void Initialize()
        {
            if (Assets == null)
            {
                Assets = new List<VfxAsset>(Effect.EffectParts.Count);
            }
            else
            {
                foreach (var asset in Assets)
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
                    Assets.Add(new VfxColorFade(effectPart.AssetRef.Files[0].EcfFile, effectPart, Actor, GameBase));
                }
                else if (effectPart.AssetType == AssetType.EMO)
                {
                    Assets.Add(new VfxEmo(SpawnTransform, effectPart.AssetRef, effectPart, Actor, GameBase));
                }
                else if (effectPart.AssetType == AssetType.LIGHT)
                {
                    if (effectPart.AssetRef.Files[0].EmaFile == null) continue;
                    Assets.Add(new VfxLight(effectPart.AssetRef.Files[0].EmaFile, effectPart, Actor, GameBase));
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
                Destroy();
                return;
            }

            foreach(var asset in Assets)
            {
                asset.Terminate();
            }
        }

        public override void Dispose()
        {
            Effect.EffectParts.CollectionChanged -= EffectParts_CollectionChanged;

            foreach (var asset in Assets)
            {
                asset.Dispose();
            }
        }

        public override void Draw()
        {
            foreach (VfxAsset asset in Assets)
            {
                asset.Draw();
            }
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
                //Handle end of effect:
                //--Restart the effect if editor loop option is enabled, and on Effects tab
                //--Otherwise, destroy the effect

                if (SceneManager.Loop && Effect.EffectParts.Count > 0 && SceneManager.IsOnTab(EditorTabs.Effect))
                {
                    Initialize();
                }
                else
                {
                    Destroy();
                }

            }
        }

        public void Simulate()
        {
            Update(true);
        }
    }
}
