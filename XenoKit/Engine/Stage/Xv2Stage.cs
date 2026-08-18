using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class Xv2Stage : RenderObject
    {
        public static event EventHandler CurrentStageChanged;
        public static event EventHandler CurrentSpmChanged;

        private int _drawThisFrame = 0;
        public override bool DrawThisFrame
        {
            //Draw twice per frame: ReflectionPass and regular pass
            set
            {
                if (value)
                    _drawThisFrame = 2;
                else
                    MathHelper.Clamp(--_drawThisFrame, 0, 2);
            }
            get
            {
                return _drawThisFrame > 0;
            }
        }

        public override EngineObjectTypeEnum EngineObjectType => EngineObjectTypeEnum.Stage;

        public const string ENV_NAME = "ENVTEX";

        public bool IsDefaultStage { get; private set; }
        public string StageName { get; set; }
        public StageDef StageDefEntry { get; private set; }
        public FMP_File FmpFile { get; private set; }
        public SPM_File SpmFile { get; set; }
        public SPM_Entry CurrentSpm => SpmFile.Entries[0];

        //Stage Settings
        public float NearClip => FmpFile != null ? FmpFile.SettingsA.NearDistance : 0.1f;
        public float FarClip => FmpFile != null ? FmpFile.SettingsA.FarDistance : 5000f;

        //Fog
        public bool FogEnabled => LocalSettings.Instance.EnableFog;
        public Vector4 FogMultiColor { get; private set; }
        public Vector4 FogAddColor { get; private set; }
        public Vector4 Fog { get; private set; }
        public Vector4 BackgroundGlareTone { get; private set; }
        public Vector4 EffectGlareTone { get; private set; }
        public Vector4 EdgeColor { get; private set; }

        //Reflections
        public TextureCube EnvTexture { get; private set; }

        //Objects
        public List<StageObject> Objects { get; private set; } = new List<StageObject>();
        public List<StageCollisionGroup> CollisionGroups { get; private set; } = new List<StageCollisionGroup>();

        //Collision mesh
        //private CollisionMeshBatchDraw batchedCollisionMesh;

        public Xv2Stage() { }

        public Xv2Stage(string stageCode)
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

            //Load collision
            //foreach(var collisionGroup in FmpFile.CollisionGroups)
            //{
            //    CollisionGroups.Add(new StageCollisionGroup(collisionGroup));
            //}

            bool hasWaterEntry = FmpFile.Objects.Any(x => x.Name == "WATER");

            //Load assets
            foreach (var _object in FmpFile.Objects)
            {
                bool isEnabled = (_object.Flags & ObjectFlags.Enabled) != 0;
                bool isRef = (hasWaterEntry && _object.Name.StartsWith("REF")) || _object.HasCommand("MIRROR OBJECT");
                if (!isRef && !isEnabled) continue;

                StageObject stageObj = new StageObject();
                stageObj.Object = _object;
                stageObj.Transform = _object.Transform.ToMatrix();

                //When a map file has a WATER object entry, any object that starts with "REF" is considered a water reflection
                //Objects can also have a "MIRROR OBJECT" command which sets them up as a reflection (does not require a WATER entry)
                stageObj.IsReflection = isRef;

                if (_object.Entities?.Count - 1 >= _object.InitialEntityIndex)
                {
                    //Just load the initial entity for now
                    FMP_Entity entity = _object.Entities[_object.InitialEntityIndex];

                    StageEntity stageEntity = new StageEntity();
                    stageEntity.Transform = entity.Transform.ToMatrix();

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
                            stageEntity.Visual.LodGroup = new LodGroup(entity.Visual, _object, stageObj);

                            stageObj.Entities.Add(stageEntity);
                        }

                    }

                }

                /*
                if (_object.CollisionGroupInstance != null)
                {
                    StageCollisionGroup collisionGroup = CollisionGroups.FirstOrDefault(x => x.CollisionGroupIndex == _object.CollisionGroupInstance.CollisionGroupIndex);

                    if(collisionGroup != null)
                    {
                        for (int i = 0; i < _object.CollisionGroupInstance.ColliderInstances.Count; i++)
                        {
                            stageObj.ColliderInstances.Add(new StageColliderInstance(_object.CollisionGroupInstance.ColliderInstances[i], collisionGroup.Colliders[i]));
                        }
                    }
                }
                */

                
                Objects.Add(stageObj);
            }
            /*
            //Create collision mesh
            foreach(var obj in Objects)
            {
                obj.SetColliderMeshWorld();
            }

            var collisionMeshes = GetAllCollisionMeshes();
            batchedCollisionMesh = new CollisionMeshBatchDraw(collisionMeshes);
            */
        }

        public void DrawReflection()
        {
            foreach (StageObject obj in Objects)
            {
                obj.DrawReflection();
            }
        }

        public override void Draw()
        {
            if (RenderSystem.IsReflectionPass)
            {
                DrawReflection();
                return;
            }

            foreach(StageObject obj in Objects)
            {
                obj.Draw();
            }

            //if (SceneManager.CollisionMeshVisible)
            //{
            //    batchedCollisionMesh.Draw();
            //}
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
        }

        private void UpdateStageLighting()
        {
            FogMultiColor = new Vector4(CurrentSpm.FogMultiColorR, CurrentSpm.FogMultiColorG, CurrentSpm.FogMultiColorB, CurrentSpm.FogMultiColorA);
            FogAddColor = new Vector4(CurrentSpm.FogAddColorR, CurrentSpm.FogAddColorG, CurrentSpm.FogAddColorB, CurrentSpm.FogAddColorA);
            Fog = new Vector4(CurrentSpm.FogStartDist, CurrentSpm.FogEndDist, 1.11111f, -0.0037f);
            BackgroundGlareTone = new Vector4(CurrentSpm.F_416[0], CurrentSpm.F_416[0], CurrentSpm.F_416[0], CurrentSpm.BackgroundGlareAdditiveColorR);
            EffectGlareTone = new Vector4(CurrentSpm.EffectGlareTone, CurrentSpm.EffectGlareTone, CurrentSpm.EffectGlareTone, CurrentSpm.F_416[2]);
            EdgeColor = new Vector4(CurrentSpm.EdgeStrokesColorR, CurrentSpm.EdgeStrokesColorG, CurrentSpm.EdgeStrokesColorB, CurrentSpm.EdgeStrokesWidth);
        }

        public void SetSpmFile(SPM_File spmFile)
        {
            SpmFile = spmFile;
            UpdateStageLighting();
            CurrentSpmChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetActiveStage()
        {
            if(EnvTexture != null)
                ShaderManager.SetSceneCubeMap(EnvTexture);

            CurrentStageChanged?.Invoke(this, EventArgs.Empty);
            CurrentSpmChanged?.Invoke(this, EventArgs.Empty);
        }

        public void UnsetActiveStage()
        {

        }

        public static Xv2Stage CreateDefaultStage()
        {
            var stage = new Xv2Stage()
            {
                IsDefaultStage = true,
                SpmFile = (SPM_File)FileManager.Instance.GetParsedFileFromGame("stage/BFten/BFten.spm")
            };

            stage.UpdateStageLighting();

            return stage;
        }
    
        private List<CollisionMesh> GetAllCollisionMeshes()
        {
            List<CollisionMesh> meshes = new List<CollisionMesh>();

            foreach(var obj in Objects)
            {
                meshes.AddRange(obj.GetAllCollisionMeshes());
            }

            return meshes;
        }
    }
}
