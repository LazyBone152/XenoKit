using Microsoft.Xna.Framework;
using System;
using XenoKit.Editor;
using Xv2CoreLib;
using Xv2CoreLib.SPM;

namespace XenoKit.Engine.Stage
{
    public class Xv2Stage : Entity
    {
        public static event EventHandler CurrentStageChanged;
        public static event EventHandler CurrentSpmChanged;

        public SPM_File SpmFile { get; set; }
        public SPM_Entry CurrentSpm => SpmFile.Entries[0];

        //Fog
        public bool FogEnabled => LocalSettings.Instance.EnableFog;
        public Vector4 FogMultiColor { get; private set; }
        public Vector4 FogAddColor { get; private set; }
        public Vector4 Fog { get; private set; }

        public Xv2Stage(GameBase game) : base (game)
        {
        }

        public override void Update()
        {
            
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

        public void SetActive()
        {
            CurrentStageChanged?.Invoke(this, EventArgs.Empty);
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
