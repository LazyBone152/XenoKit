using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Xv2CoreLib.EMB_CLASS;

namespace XenoKit.Engine.Textures
{
    [Serializable]
    public class Xv2Texture : EngineObject, IDisposable
    {
        #region DefaultTexture
        public static Xv2Texture DefaultTexture { get; private set; }

        public static void InitDefaultTexture()
        {
            if(DefaultTexture == null)
            {
                EMB_TextureFile defaultEmb = EMB_TextureFile.Load(Properties.Resources.DefaultEmb);
                DefaultTexture = new Xv2Texture(defaultEmb.GetEntry(0), false);
            }
        }
        #endregion

        [field: NonSerialized]
        private Texture2D _texture = null;

        public Texture2D Texture
        {
            get
            {
                if ((_texture == null || IsDirty) && EmbEntry != null)
                {
                    _texture = TextureLoader.ConvertToTexture2D(EmbEntry, null, ViewportInstance.GraphicsDevice);
                    IsDirty = false;
                }
                return _texture;
            }
            private set
            {
                IsDirty = false;
                _texture = value;
            }
        }
        public EMB_TextureEntry EmbEntry { get; private set; }

        //TODO: logic for this
        public bool IsDirty { get; set; }

        public Xv2Texture(EMB_TextureEntry embEntry, bool autoUpdate = true)
        {
            EmbEntry = embEntry;
            Texture = TextureLoader.ConvertToTexture2D(embEntry, null, Viewport.Instance.GraphicsDevice);

            if (autoUpdate)
                EmbEntry.PropertyChanged += EmbEntry_PropertyChanged;
        }

        private void EmbEntry_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EmbEntry.Data))
            {
                IsDirty = true;
            }
        }

        public void Dispose()
        {
            if (EmbEntry != null)
                EmbEntry.PropertyChanged -= EmbEntry_PropertyChanged;
        }

        public Xv2Texture HardCopy()
        {
            return new Xv2Texture(EmbEntry.Copy(), false);
        }

        public static Xv2Texture[] LoadTextureArray(EMB_TextureFile embFile)
        {
            int count = embFile.Entry.Count > 0 ? embFile.Entry.Max(x => x.ID) + 1 : 0;
            Xv2Texture[] textures = new Xv2Texture[count];

            for (int i = 0; i < textures.Length; i++)
            {
                EMB_TextureEntry entry = embFile.GetEntry(i);

                if(entry != null)
                {
                    textures[i] = Viewport.Instance.CompiledObjectManager.GetCompiledObject<Xv2Texture>(entry);
                }
                else
                {
                    textures[i] = DefaultTexture;
                }
            }

            return textures;
        }
    }
}
