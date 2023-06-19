using XenoKit.Engine.Vfx.Asset;
using Xv2CoreLib.EEPK;

namespace XenoKit.Engine.Vfx
{
    /// <summary>
    /// This is a light weight class intended for simple effect and asset previewing, for use with the EEPK Organiser.
    /// </summary>
    public class VfxPreview : Entity
    {
        public VfxEffect Effect = null;
        public VfxEffect Asset = null;

        private readonly EffectPart DefaultEffectPart;

        public VfxPreview(GameBase game) : base(game)
        {
            DefaultEffectPart = new EffectPart();
        }

        public override void Update()
        {
            switch (SceneManager.CurrentSceneState)
            {
                case EditorTabs.Effect:
                    Effect?.Update();
                    break;
                default:
                    Asset?.Update();
                    break;
            }
        }

        public override void Draw()
        {
            switch (SceneManager.CurrentSceneState)
            {
                case EditorTabs.Effect:
                    Effect?.Draw();
                    break;
                default:
                    Asset?.Draw();
                    break;
            }
        }

        public void PreviewEffect(Effect effect)
        {
            SceneManager.EnsureActorIsSet(0);

            if (Effect != null)
                Effect.Dispose();

            Effect = new VfxEffect(SceneManager.Actors[0], effect, Microsoft.Xna.Framework.Matrix.Identity, GameBase);
        }

        public void PreviewAsset(Xv2CoreLib.EffectContainer.Asset asset)
        {
            SceneManager.EnsureActorIsSet(0);

            if (Asset != null)
                Asset.Dispose();

            DefaultEffectPart.AssetRef = asset;
            DefaultEffectPart.AssetType = asset.assetType;
            Asset = new VfxEffect(SceneManager.Actors[0], DefaultEffectPart, GameBase);
        }

        public void Stop()
        {
            //Reset effect to first frame and pause

            if (SceneManager.IsOnTab(EditorTabs.Effect))
            {
                Effect.Initialize();
                GameBase.IsPlaying = false;
            }
            else
            {
                Asset.InitializeFromAsset();
                GameBase.IsPlaying = false;
            }
        }

        public VfxColorFadeEntry GetActiveColorFade(string materialName, Actor actor)
        {
            VfxEffect effect = SceneManager.IsOnTab(EditorTabs.Effect) ? Effect : Asset;

            if (effect == null) return null;

            foreach (VfxAsset asset in effect.Assets)
            {
                if (asset is VfxColorFade colorFade)
                {
                    VfxColorFadeEntry entry = colorFade.GetColorFadeEntry(materialName);

                    if (entry != null)
                        return entry;
                }
            }

            return null;
        }

        public VfxLight GetActiveLight()
        {
            VfxEffect effect = SceneManager.IsOnTab(EditorTabs.Effect) ? Effect : Asset;

            if (effect == null) return null;

            foreach (VfxAsset asset in effect.Assets)
            {
                if (asset is VfxLight light)
                {
                    return light;
                }
            }
            return null;
        }

    }
}
