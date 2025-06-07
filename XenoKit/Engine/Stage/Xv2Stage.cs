using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using XenoKit.Editor;
using XenoKit.Engine.Model;
using XenoKit.Engine.Textures;
using Xv2CoreLib;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.Eternity;
using Xv2CoreLib.FMP;
using Xv2CoreLib.SPM;

namespace XenoKit.Engine.Stage
{
    public class Xv2Stage : Entity
    {
        public static event EventHandler CurrentStageChanged;
        public static event EventHandler CurrentSpmChanged;

        public override EntityType EntityType => EntityType.Stage;

        public const string REF_NAME = "REF00";
        public const string REF_NAME_ALT = "REF";
        public const string ENV_NAME = "ENVTEX";

        public string StageName { get; set; }
        public StageDef StageDefEntry { get; private set; }
        public FMP_File FmpFile { get; private set; }
        public SPM_File SpmFile { get; set; }
        public SPM_Entry CurrentSpm => SpmFile.Entries[0];

        //Fog
        public bool FogEnabled => LocalSettings.Instance.EnableFog;
        public Vector4 FogMultiColor { get; private set; }
        public Vector4 FogAddColor { get; private set; }
        public Vector4 Fog { get; private set; }

        //Reflections
        public TextureCube EnvTexture { get; private set; }
        public LodGroup ReflectionModel { get; private set; }

        //Objects
        public List<StageObject> Objects { get; private set; } = new List<StageObject>();

        public Xv2Stage(GameBase game) : base (game)
        {
        }

        public Xv2Stage(GameBase game, string stageCode) : base(game)
        {
            StageDefEntry = Xenoverse2.Instance.StageDefFile.GetStage(stageCode);
            StageName = Xenoverse2.Instance.GetStageName(stageCode);
            LoadStage();
        }

        private void LoadStage()
        {
            //Load fmp
            FmpFile = (FMP_File)FileManager.Instance.GetParsedFileFromGame($"stage/{StageDefEntry.CODE}.map", false, false);

            if(FmpFile == null)
            {
                Log.Add($"No .map file could be found for stage {StageDefEntry.CODE}.", LogType.Error);
                return;
            }

            //Load spm
            SpmFile = (SPM_File)FileManager.Instance.GetParsedFileFromGame($"stage/{StageDefEntry.DIR}/{StageDefEntry.STR4}.spm", false, false);

            //Some stages (such as BFwis) have an incorrect DIR value set for some reason. In this case, we can try using STR4 as the DIR to find the spm
            if (SpmFile == null)
                SpmFile = (SPM_File)FileManager.Instance.GetParsedFileFromGame($"stage/{StageDefEntry.STR4}/{StageDefEntry.STR4}.spm", false, false);

            if (SpmFile == null)
            {
                Log.Add($"No .spm file could be found for stage {StageDefEntry.CODE}.", LogType.Error);
                return;
            }

            UpdateStageLighting();

            //Load assets
            foreach (var _object in FmpFile.Objects)
            {
                StageObject stageObj = new StageObject();
                stageObj.Transform = FmpMatrixToMatrix(_object.Matrix);

                if(_object.Entities != null)
                {
                    foreach (var entity in _object.Entities)
                    {
                        StageEntity stageEntity = new StageEntity();
                        stageEntity.Transform = FmpMatrixToMatrix(entity.Matrix);

                        if (entity.Visual != null)
                        {
                            if (_object.Name == ENV_NAME)
                            {
                                string embPath = $"stage/{entity.Visual.EmbFile}";
                                EMB_File embFile = (EMB_File)FileManager.Instance.GetParsedFileFromGame(embPath);
                                EnvTexture = TextureLoader.ConvertToTextureCube(embFile.Entry[0], ShaderManager.GetTextureName(5), GraphicsDevice);
                            }
                            else
                            {
                                stageEntity.Visual = new StageVisual();
                                stageEntity.Visual.LodGroup = new Model.LodGroup(entity.Visual, GameBase);

                                if (_object.Name == REF_NAME || _object.Name == REF_NAME_ALT)
                                {
                                    ReflectionModel = stageEntity.Visual.LodGroup;
                                }
                                else
                                {
                                    //Only add the entity if its not a reflection/env/otherspecial entry type
                                    stageObj.Entities.Add(stageEntity);
                                }
                            }

                        }

                    }
                }

                Objects.Add(stageObj);
            }
        }

        private Matrix FmpMatrixToMatrix(FMP_Matrix matrix)
        {
            return new Matrix(matrix.L0[0], matrix.L0[1], matrix.L0[2], 0f,
                              matrix.L1[0], matrix.L1[1], matrix.L1[2], 0f,
                              matrix.L2[0], matrix.L2[1], matrix.L2[2], 0f,
                              matrix.L3[0], matrix.L3[1], matrix.L3[2], 1f);

        }

        public override void Draw()
        {
            foreach(StageObject obj in Objects)
            {
                obj.Draw();
            }
        }

        public override void DrawPass(bool normalPass)
        {
            if (normalPass) return;

            foreach (StageObject obj in Objects)
            {
                obj.DrawSimple();
            }
        }

        public override void Update()
        {
            DrawThisFrame = true;

            if (ReflectionModel != null)
            {
                ReflectionModel.DrawThisFrame = true;
            }
        }

        private void UpdateStageLighting()
        {
            FogMultiColor = new Vector4(CurrentSpm.FogMultiColorR, CurrentSpm.FogMultiColorG, CurrentSpm.FogMultiColorB, CurrentSpm.FogMultiColorA);
            FogAddColor = new Vector4(CurrentSpm.FogAddColorR, CurrentSpm.FogAddColorG, CurrentSpm.FogAddColorB, CurrentSpm.FogAddColorA);
            Fog = new Vector4(CurrentSpm.FogStartDist, CurrentSpm.FogEndDist, 1.11111f, -0.0037f);
        }

        public void SetSpmFile(SPM_File spmFile)
        {
            SpmFile = spmFile;
            UpdateStageLighting();
            CurrentSpmChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetActiveStage()
        {
            if (ReflectionModel != null)
                RenderSystem.AddReflectionRenderEntity(ReflectionModel);

            if(EnvTexture != null)
                ShaderManager.SetSceneCubeMap(EnvTexture);

            CurrentStageChanged?.Invoke(this, EventArgs.Empty);
            CurrentSpmChanged?.Invoke(this, EventArgs.Empty);
        }

        public void UnsetActiveStage()
        {
            if (ReflectionModel != null)
                RenderSystem.RemoveReflectionRenderEntity(ReflectionModel);
        }

        public static Xv2Stage CreateDefaultStage(GameBase game)
        {
            var stage = new Xv2Stage(game)
            {
                SpmFile = (SPM_File)FileManager.Instance.GetParsedFileFromGame("stage/BFten/BFten.spm")
            };

            stage.UpdateStageLighting();

            return stage;
        }
    }
}
