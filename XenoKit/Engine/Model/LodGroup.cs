using Microsoft.Xna.Framework;
using System.Collections.Generic;
using XenoKit.Engine.Animation;
using XenoKit.Engine.Textures;
using Xv2CoreLib;
using Xv2CoreLib.EMA;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMM;
using Xv2CoreLib.FMP;
using Xv2CoreLib.NSK;

namespace XenoKit.Engine.Model
{
    public class LodGroup : Entity
    {
        public override EntityType EntityType => EntityType.Stage;

        public List<Lod> LODs { get; private set; } = new List<Lod>();
        public Xv2Texture[] Textures { get; private set; }
        public EMA_File Ema { get; private set; }

        private int lodIndex = -1;

        public LodGroup (FMP_Visual visual, GameBase game) : base(game)
        {
            string embPath = $"stage/{visual.EmbFile}";
            string emaPath = $"stage/{visual.EmaFile}";

            EMB_File embFile = (EMB_File)FileManager.Instance.GetParsedFileFromGame(embPath);
            Textures = Xv2Texture.LoadTextureArray(embFile, game);

            foreach(var lod in visual.LODs)
            {
                string nskPath = $"stage/{lod.NskFile}";
                string emmPath = $"stage/{lod.EmmFile}";

                if (!string.IsNullOrWhiteSpace(lod.NskFile))
                {
                    NSK_File nskFile = (NSK_File)FileManager.Instance.GetParsedFileFromGame(nskPath);
                    EMM_File emmFile = (EMM_File)FileManager.Instance.GetParsedFileFromGame(emmPath);

                    Xv2ModelFile model = Xv2ModelFile.LoadNsk(game, nskFile);
                    Xv2Skeleton skeleton = new Xv2Skeleton(nskFile.EskFile);

                    LODs.Add(new Lod(lod.Distance, model, skeleton, emmFile));
                }
                else
                {
                    LODs.Add(new Lod(lod.Distance, null, null, null));
                }
            }
        }

        public override void Draw()
        {
            Draw(Matrix.Identity);
        }

        public void Draw(Matrix world)
        {
            Lod lod = GetCurrentLod();
            lod.Draw(world, Textures);
            DrawThisFrame = false;
        }

        public void DrawSimple(Matrix world)
        {
            Lod lod = GetCurrentLod();
            lod.DrawSimple(world, RenderSystem.ShadowModel);
        }

        private Lod GetCurrentLod()
        {
            float distanceFromCamera = Vector3.Distance(CameraBase.CameraState.Position, Transform.Translation);

            //TODO
            return LODs[GetLodIndex(distanceFromCamera)];
        }

        private bool IsLodIndexValid(float distanceFromCamera)
        {
            if (lodIndex > -1 && lodIndex < LODs.Count)
            {
                if (lodIndex > 0)
                {
                    if (distanceFromCamera < LODs[lodIndex].Distance) return false;
                }
                else
                {
                    return LODs[lodIndex].Distance < distanceFromCamera; 
                }
            }

            return false;
        }

        private int GetLodIndex(float distanceFromCamera)
        {
            for (int i = 0; i < LODs.Count; i++)
            {
                if (LODs[i].Distance < distanceFromCamera) return i;
            }

            return 0;
        }

        public void SetAsReflectionMesh(bool isReflection)
        {
            foreach(var lod in LODs)
            {
                lod.Model?.SetAsReflectionMesh(isReflection);
            }
        }
    }
}
