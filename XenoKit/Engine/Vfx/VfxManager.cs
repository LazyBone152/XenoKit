﻿using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC;
using XenoKit.Engine.Character;
using XenoKit.Engine.Vfx.Asset;
using Xv2CoreLib.BAC;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Vfx
{
    /// <summary>
    /// This is the main effect manager class. This can play multiple effects simultaneously, and handles loops and deactivations.
    /// </summary>
    public class VfxManager : Entity
    {
        private const int MAX_EFFECTS = 100;
        private List<VfxEffect> Effects = new List<VfxEffect>();

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

        public async void PlayEffect(BAC_Type8 bacEffect, BacEntryInstance bacInstance, Actor actor)
        {
            if (Effects.Count >= MAX_EFFECTS)
            {
                Log.Add("Maximum amount of effects that can be active at the same time reached. Cannot start new ones.", LogType.Warning);
                return;
            }

            int skillId = bacEffect.UseSkillId == BAC_Type8.UseSkillIdEnum.True ? bacEffect.SkillID : 0;

            EffectContainerFile eepk = Files.Instance.GetEepkFile(bacEffect.EepkType, (ushort)skillId, bacInstance.SkillMove, bacInstance.User, true);

            if (eepk != null)
            {
                Effect eepkEffect = eepk.GetEffect(bacEffect.EffectID);

                if (eepkEffect != null)
                {
                    //Get spawn position from declared bone and position on the bac entry
                    Matrix spawnPosition = Matrix.Identity;

                    if(actor != null && (int)bacEffect.BoneLink < 25)
                    {
                        spawnPosition = actor.GetAbsoluteBoneMatrix(actor.Skeleton.BAC_BoneIndices[(int)bacEffect.BoneLink]) * Matrix.CreateTranslation(new Vector3(bacEffect.PositionX, bacEffect.PositionY, bacEffect.PositionZ));
                    }

                    await Task.Run(() => AddEffect(bacInstance.User, eepkEffect, spawnPosition, GameBase));
                }
                else
                {
                    Log.Add($"No effect at ID {bacEffect.EffectID} could be found in EEPK {bacEffect.EepkType}.");
                }
            }
        }

        public void PlayEffect(DamageManager bdmInstance)
        {
            PlayEffect((BAC_Type8.EepkTypeEnum)bdmInstance.BdmSubEntry.Effect1_EepkType, bdmInstance.BdmSubEntry.Effect1_SkillID, bdmInstance.BdmSubEntry.Effect1_ID, bdmInstance);
            PlayEffect((BAC_Type8.EepkTypeEnum)bdmInstance.BdmSubEntry.Effect2_EepkType, bdmInstance.BdmSubEntry.Effect2_SkillID, bdmInstance.BdmSubEntry.Effect2_ID, bdmInstance);
            PlayEffect((BAC_Type8.EepkTypeEnum)bdmInstance.BdmSubEntry.Effect3_EepkType, bdmInstance.BdmSubEntry.Effect3_SkillID, bdmInstance.BdmSubEntry.Effect3_ID, bdmInstance);
        }

        private async void PlayEffect(BAC_Type8.EepkTypeEnum eepkType, ushort skillId, short effectId, DamageManager bdmInstance)
        {
            if (Effects.Count >= MAX_EFFECTS)
            {
                Log.Add("Maximum amount of effects that can be active at the same time reached. Cannot start new ones.", LogType.Warning);
                return;
            }

            if (effectId == -1) return;

            EffectContainerFile eepk = Files.Instance.GetEepkFile(eepkType, skillId, bdmInstance.Move, bdmInstance.Victim, true);

            if (eepk != null)
            {
                Effect eepkEffect = eepk.GetEffect(effectId);

                if (eepkEffect != null)
                {
                    await Task.Run(() => AddEffect(bdmInstance.Victim, eepkEffect, bdmInstance.HitPosition, GameBase));
                }
                else
                {
                    Log.Add($"No effect at ID {effectId} could be found in EEPK {eepkType}.");
                }
            }
        }

        private void AddEffect(Actor actor, Effect effect, Matrix world, GameBase gameBase)
        {
            VfxEffect vfxEffect = new VfxEffect(actor, effect, world, GameBase);

            lock (Effects)
            {
                NewEffects.Add(vfxEffect);
            }
        }

        public void StopActorEffects(Actor actor)
        {
            lock (Effects)
            {
                foreach (VfxEffect vfxEffect in Effects)
                {
                    if (vfxEffect.Actor == actor)
                        vfxEffect.Terminate(true);
                }
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
            lock (Effects)
            {
                foreach (VfxEffect vfxEffect in Effects)
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
        }

        public void StopEffects()
        {
            lock (Effects)
            {
                foreach (VfxEffect effect in Effects)
                {
                    effect.Dispose();
                }

                NewEffects.Clear();
                Effects.Clear();
            }

        }

        public void RestartEffect()
        {
            if(Effects.Count == 1)
            {
                Effects[0].Initialize();
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

            ForceEffectUpdate = false;
        }

        public void Simulate()
        {
            Update(true);
        }

        public override void Draw()
        {
            /*
            if (!SettingsManager.Instance.Settings.XenoKit_VfxSimulation) return;

            
            foreach(VfxEffect effect in Effects)
            {
                effect.Draw();
            }
            */
        }

        #endregion

        /// <summary>
        /// Returns the first active <see cref="VfxColorFade"/> matching the conditions.
        /// </summary>
        public VfxColorFadeEntry GetActiveColorFade(string materialName, Actor actor)
        {
            if(SceneManager.IsOnEffectTab && GameBase.IsMainInstance)
            {
                return SceneManager.MainGameInstance.VfxPreview.GetActiveColorFade(materialName, actor);
            }

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
            if (SceneManager.IsOnEffectTab && GameBase.IsMainInstance)
            {
                return SceneManager.MainGameInstance.VfxPreview.GetActiveLight();
            }

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
