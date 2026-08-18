using XenoKit.Engine.Vfx.Asset;
using Xv2CoreLib.EEPK;

namespace XenoKit.Engine.Vfx
{
    /// <summary>
    /// This is a light weight class intended for simple effect and asset previewing, for use when editing effects (Effect Tab).
    /// </summary>
    public class VfxPreview : EngineObject
    {
        public VfxEffect Effect = null;
        public VfxEffect Asset = null;

        private readonly EffectPart DefaultEffectPart;

        public VfxPreview()
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
            /*
            switch (SceneManager.CurrentSceneState)
            {
                case EditorTabs.Effect:
                    Effect?.Draw();
                    break;
                default:
                    Asset?.Draw();
                    break;
            }
            */
        }

        public async void PreviewEffect(Effect effect)
        {
            await SceneManager.AsyncEnsureActorIsSet(0);

            if (Effect != null)
                Effect.Dispose();

            Effect = new VfxEffect(SceneManager.Actors[0], effect, System.Numerics.Matrix4x4.Identity);
        }

        public async void PreviewAsset(Xv2CoreLib.EffectContainer.Asset asset)
        {
            await SceneManager.AsyncEnsureActorIsSet(0);

            if (Asset != null)
                Asset.Dispose();

            DefaultEffectPart.AssetRef = asset;
            DefaultEffectPart.AssetType = asset.assetType;
            Asset = new VfxEffect(SceneManager.Actors[0], DefaultEffectPart);
        }

        public void Stop()
        {
            //Reset effect to first frame and pause

            if (SceneManager.IsOnTab(EditorTabs.Effect))
            {
                Effect?.Initialize();
                ViewportInstance.IsPlaying = false;
            }
            else
            {
                Asset?.InitializeFromAsset();
                ViewportInstance.IsPlaying = false;
            }
        }

        public void SeekPrev()
        {
            switch (SceneManager.CurrentSceneState)
            {
                case EditorTabs.Effect:
                    Effect?.SeekPrevFrame();
                    break;
                default:
                    Asset?.SeekPrevFrame();
                    break;
            }
        }

        public void SeekNext()
        {
            switch (SceneManager.CurrentSceneState)
            {
                case EditorTabs.Effect:
                    Effect?.SeekNextFrame();
                    break;
                default:
                    Asset?.SeekNextFrame();
                    break;
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
