using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XenoKit.Editor;
using XenoKit.Engine.Vfx.Asset;
using Xv2CoreLib.EEPK;
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

        public VfxManager(GameBase gameBase) : base(gameBase)
        {

        }

        public async void PlayEffect(Effect effect, Actor actor)
        {
            if (Effects.Count >= MAX_EFFECTS)
            {
                Log.Add("Maximum amount of effects that can be active at the same time reached. Cannot start new ones.", LogType.Warning);
                return;
            }

            await Task.Run(() => NewEffects.Add(new VfxEffect(actor, effect, GameBase)));
            //Effects.Add(new VfxEffect(actor, effect, GameBase));
        }

        public void StopActorEffects(Actor actor)
        {
            foreach (VfxEffect vfxEffect in Effects)
            {
                if (vfxEffect.Actor == actor)
                    vfxEffect.Terminate(true);
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

            Effects.Clear();
        }

        public override void Update()
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

                Effects[i].Update();
            }

            //Task task = Task.WhenAll(CurrentTasks);
            //task.Wait();

            //CurrentTasks.Clear();
        }

        public override void Draw()
        {
            if (!SettingsManager.Instance.Settings.XenoKit_VfxSimulation) return;

            foreach(VfxEffect effect in Effects)
            {
                effect.Draw();
            }
        }

        /// <summary>
        /// Returns the first active <see cref="VfxColorFade"/> matching the conditions.
        /// </summary>
        public VfxColorFadeEntry GetActiveColorFade(string materialName, Actor actor)
        {
            foreach(var effect in Effects.Where(x => x.Actor == actor))
            {
                foreach(var asset in effect.Assets)
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
            foreach (var effect in Effects)
            {
                foreach (var asset in effect.Assets)
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
