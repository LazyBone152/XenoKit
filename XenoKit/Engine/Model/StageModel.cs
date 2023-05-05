using System;
using System.Collections.Generic;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using Xv2CoreLib.NSK;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMM;
using Microsoft.Xna.Framework;
using XenoKit.Engine.Animation;

namespace XenoKit.Engine.Model
{
    /// <summary>
    /// Basic class for stage previewing. Doesn't support editing or skeleton/animations.
    /// </summary>
    public class StageModel : Entity
    {
        public Xv2Skeleton Skeleton;
        public Xv2ModelFile Model;
        public List<Xv2ShaderEffect> Materials;
        public Xv2Texture[] Textures;

        public StageModel(byte[] nsk, byte[] emm, byte[] emb, GameBase gameBase) : base(gameBase)
        {
            NSK_File nskFile = NSK_File.Load(nsk);
            EMB_File embFile = EMB_File.LoadEmb(emb);
            EMM_File emmFile = EMM_File.LoadEmm(emm);

            Skeleton = new Xv2Skeleton(nskFile.EskFile);
            Model = Xv2ModelFile.LoadNsk(gameBase, nskFile);
            Materials = Model.InitializeMaterials(emmFile);
            Textures = Xv2Texture.LoadTextureArray(embFile, gameBase);

        }

        public override void Update()
        {
            Model.Update(0);
        }

        public override void Draw()
        {
            Model.Draw(Matrix.Identity, 0, Materials, Textures, null, 0, Skeleton);
        }
    }
}
