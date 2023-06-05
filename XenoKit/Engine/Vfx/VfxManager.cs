using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC;
using XenoKit.Engine.Vfx.Asset;
using Xv2CoreLib.BAC;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Vfx
{
    public class VfxManager : Entity
    {
        private const int MAX_EFFECTS = 100;
        private List<VfxEffect> Effects = new List<VfxEffect>();

        //Thread Control
        //public List<Task> CurrentTasks { get; private set; } = new List<Task>();
        private List<VfxEffect> NewEffects = new List<VfxEffect>();

        /// <summary>
        /// Force effects to fully update on the next cycle, even if its via Simulate().
        /// </summary>
        public bool ForceEffectUpdate { get; set; }

        public VfxManager(GameBase gameBase) : base(gameBase)
        {

        }

        #region PlayAndStop
        public async void PlayEffect(Effect effect, Actor actor)
        {
            if (Effects.Count >= MAX_EFFECTS)
            {
                Log.Add("Maximum amount of effects that can be active at the same time reached. Cannot start new ones.", LogType.Warning);
                return;
            }

            await Task.Run(() => AddEffect(actor, effect, Matrix.Identity, GameBase));
        }

        public async void PlayEffect(BAC_Type8 bacEffect, BacEntryInstance bacInstance)
        {
            if (Effects.Count >= MAX_EFFECTS)
            {
                Log.Add("Maximum amount of effects that can be active at the same time reached. Cannot start new ones.", LogType.Warning);
                return;
            }

            EffectContainerFile eepk = Files.Instance.GetEepkFile(bacEffect.EepkType, bacEffect.SkillID, bacInstance.SkillMove, bacInstance.User, true);

            if (eepk != null)
            {
                Effect eepkEffect = eepk.GetEffect(bacEffect.EffectID);

                if (eepkEffect != null)
                {
                    //Get spawn position from declared bone and position on the bac entry
                    Matrix spawnPosition = Matrix.Identity;

                    if(bacInstance.User != null && (int)bacEffect.BoneLink < 25)
                    {
                        spawnPosition = bacInstance.User.GetAbsoluteBoneMatrix(bacInstance.User.Skeleton.BAC_BoneIndices[(int)bacEffect.BoneLink]) * Matrix.CreateTranslation(new Vector3(bacEffect.PositionX, bacEffect.PositionY, bacEffect.PositionZ));
                    }

                    await Task.Run(() => AddEffect(bacInstance.User, eepkEffect, spawnPosition, GameBase));
                }
                else
                {
                    Log.Add($"No effect at ID {bacEffect.EffectID} could be found in EEPK {bacEffect.EepkType}.");
                }
            }
        }

        private void AddEffect(Actor actor, Effect effect, Matrix world, GameBase gameBase)
        {
            VfxEffect vfxEffect = new VfxEffect(actor, effect, world, GameBase);

            lock (NewEffects)
            {
                NewEffects.Add(vfxEffect);
            }
        }

        public void StopActorEffects(Actor actor)
        {
            foreach (VfxEffect vfxEffect in Effects)
            {
                if (vfxEffect.Actor == actor)
                    vfxEffect.Terminate(true);
            }
        }

        public void StopEffect(BAC_Type8 bacEffect, BacEntryInstance bacInstance)
        {
            EffectContainerFile eepk = Files.Instance.GetEepkFile(bacEffect.EepkType, bacEffect.SkillID, bacInstance.SkillMove, bacInstance.User, true);

            if (eepk != null)
            {
                Effect eepkEffect = eepk.GetEffect(bacEffect.EffectID);

                if (eepkEffect != null)
                {
                    StopEffect(eepkEffect);
                }
            }
        }

        public void StopEffect(Effect effect)
        {
            foreach(VfxEffect vfxEffect in Effects)
            {
                if (vfxEffect.Effect == effect)
                    vfxEffect.Terminate(false);
            }

            foreach (VfxEffect vfxEffect in NewEffects)
            {
                if (vfxEffect.Effect == effect)
                    vfxEffect.Terminate(false);
            }
        }

        public void StopEffects()
        {
            foreach(var effect in Effects)
            {
                effect.Dispose();
            }

            lock (Effects)
            {
                Effects.Clear();
            }
        }

        #endregion

        #region UpdateAndRendering
        public override void Update()
        {
            Update(false);
        }

        private void Update(bool simulate)
        {
            lock (Effects)
            {
                lock (NewEffects)
                {
                    Effects.AddRange(NewEffects);
                    NewEffects.Clear();


                    for (int i = Effects.Count - 1; i >= 0; i--)
                    {
                        if (Effects[i].IsDestroyed)
                        {
                            Effects.RemoveAt(i);
                            continue;
                        }

                        if (simulate)
                        {
                            Effects[i].Simulate();
                        }
                        else
                        {
                            Effects[i].Update();
                        }
                    }
                }
            }

            ForceEffectUpdate = false;
        }

        public void Simulate()
        {
            Update(true);
        }

        public override void Draw()
        {
            if (!SettingsManager.Instance.Settings.XenoKit_VfxSimulation) return;

            foreach(VfxEffect effect in Effects)
            {
                effect.Draw();
            }
        }

        #endregion

        public void SeekPrev()
        {

        }

        public void SeekNext()
        {

        }

        /// <summary>
        /// Returns the first active <see cref="VfxColorFade"/> matching the conditions.
        /// </summary>
        public VfxColorFadeEntry GetActiveColorFade(string materialName, Actor actor)
        {
            foreach(VfxEffect effect in Effects.Where(x => x.Actor == actor))
            {
                foreach(VfxAsset asset in effect.Assets)
                {
                    if(asset is VfxColorFade colorFade)
                    {
                        VfxColorFadeEntry entry = colorFade.GetColorFadeEntry(materialName);

                        if(entry != null)
                            return entry;
                    }
                }
            }

            return null;
        }

        public VfxLight GetActiveLight(Matrix world)
        {
            foreach (VfxEffect effect in Effects)
            {
                foreach (VfxAsset asset in effect.Assets)
                {
                    if (asset is VfxLight light)
                    {
                        return light;
                    }
                }
            }

            return null;
        }
    }
}
