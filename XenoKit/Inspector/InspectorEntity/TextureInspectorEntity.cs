using System;
using XenoKit.Engine;
using XenoKit.Engine.Textures;
using Xv2CoreLib.EMB_CLASS;

namespace XenoKit.Inspector.InspectorEntities
{
    public class TextureInspectorEntity : InspectorEntity
    {
        public override string FileType => "Textures";
        public EMB_File EmbFile { get; private set; }
        public Xv2Texture[] Textures { get; private set; }

        public bool IsDyt { get; private set; }
        public int DytIndex { get; set; } = 0;

        public TextureInspectorEntity(string path) : base(path)
        {
            Path = path;
            IsDyt = Xv2CoreLib.Utils.SanitizePath(Path).Contains(".dyt.emb");
            Load();
        }

        private TextureInspectorEntity(TextureInspectorEntity entity) : base(entity.Path)
        {
            IsDyt = entity.IsDyt;
            EmbFile = entity.EmbFile;
            Textures = entity.Textures;
        }

        public override bool Load()
        {
            EmbFile = EMB_File.LoadEmb(Path);
            Textures = Xv2Texture.LoadTextureArray(EmbFile, SceneManager.MainGameBase);
            return true;
        }

        public override bool Save()
        {
            EmbFile.SaveBinaryEmbFile(Path);
            return true;
        }

        public override InspectorEntity Clone()
        {
            return new TextureInspectorEntity(this);
        }
    }
}
