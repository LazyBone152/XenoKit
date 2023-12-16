using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XenoKit.Editor;
using Xv2CoreLib.BAC;

namespace XenoKit.Engine.Scripting.BAC
{
    public enum SimulationType
    {
        ActionPreview, //Action Tab: previewing BAC entries
        StatePreview, //State Tab: previewing BCM entry execution and input (not implemented)
        None,
    }

    public class ActionControl : Entity
    {
        public SimulationType SimulationType
        {
            get
            {
                switch (SceneManager.CurrentSceneState)
                {
                    case EditorTabs.Action:
                        return SimulationType.ActionPreview;
                    case EditorTabs.State:
                        return SimulationType.StatePreview;
                    default:
                        return SimulationType.None;
                }
            }
        }

        public Actor Character;
        public BacPlayer BacPlayer;

        public ActionControl(Actor parent, GameBase gameBase) : base(gameBase)
        {
            Character = parent;
            BacPlayer = new BacPlayer(parent, gameBase);
            SceneManager.BacDataChanged += SceneManager_BacValuesChanged;
        }

        public override void Update()
        {
            if (SimulationType == SimulationType.None && BacPlayer.HasBacEntry && GameBase.IsPlaying)
            {
                BacPlayer.ClearBacEntry();
            }

            if (SimulationType == SimulationType.ActionPreview && BacPlayer.HasBacEntry)
            {
                if (BacPlayer.CurrentDuration <= BacPlayer.CurrentFrame && GameBase.IsPlaying)
                {
                    if (BacPlayer.BacEntryInstance.IsFinished)
                    {
                        if (SceneManager.Loop)
                        {
                            BacPlayer.ResetBacState();
                        }
                        else
                        {
                            GameBase.IsPlaying = false;
                        }
                    }
                }

                BacPlayer.Update();
            }
        }

        public override void DelayedUpdate()
        {
            if (SimulationType == SimulationType.ActionPreview && BacPlayer.HasBacEntry)
            {
                BacPlayer.DelayedUpdate();
            }
        }

        public override void Draw()
        {
            if(BacPlayer.BacEntryInstance != null)
            {
                foreach(var visualCue in BacPlayer.BacEntryInstance.VisualSimulationCues)
                {
                    visualCue.Draw();
                }
            }
        }

        public void PreviewBacEntry(BAC_File bacFile, BAC_Entry bacEntry, Move move = null, Actor user = null)
        {
            if (!BacPlayer.HasBacEntry) Character.ResetPosition();
            BacPlayer.PlayBacEntryPreview(bacFile, bacEntry, (user != null) ? user : Character, move);
        }

        public bool IsBacEntryActive(BAC_Entry bacEntry)
        {
            return BacPlayer.BacEntryInstance?.BacEntry == bacEntry;
        }

        private void SceneManager_BacValuesChanged(object sender, EventArgs e)
        {
            if (SimulationType == SimulationType.ActionPreview)
                BacPlayer.DelayedResimulate = true;
        }

        #region PlaybackControl
        public void Resume() { BacPlayer.Resume(); }
        public void Stop() { BacPlayer.Stop(); }
        public void SeekPrevFrame() { BacPlayer.SeekPrevFrame(); }
        public void SeekNextFrame() { BacPlayer.SeekNextFrame(); }
        public void ClearBacPlayer() { BacPlayer.ClearBacEntry(); }

        #endregion
    }
}
