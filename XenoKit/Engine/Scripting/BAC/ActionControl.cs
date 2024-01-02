using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC.Simulation;
using Xv2CoreLib.BAC;

namespace XenoKit.Engine.Scripting.BAC
{
    public enum SimulationType
    {
        ActionDirect, //Playing a BAC entry directly (used for previewing and by ActorController)
        BCM, //Full simulation of a BCM chain (currently not implemented)
        None,
    }

    public class ActionControl : Entity
    {
        public SimulationType SimulationType { get; private set; }
        public ActionPreviewState PreviewState { get; private set; }

        public Actor Character;
        public BacPlayer BacPlayer;

        public event ActionFinishedEventHandler ActionFinished;

        public ActionControl(Actor parent, GameBase gameBase) : base(gameBase)
        {
            Character = parent;
            BacPlayer = new BacPlayer(parent, gameBase);
            SceneManager.BacDataChanged += SceneManager_BacValuesChanged;
        }

        public override void Update()
        {
            //Check Preview Scope
            if (GameBase.IsPlaying && BacPlayer.IsPreview && SceneManager.CurrentSceneState != EditorTabs.Action)
            {
                BacPlayer.ClearBacEntry();
                SimulationType = SimulationType.None;
            }

            PreviewState = BacPlayer.IsPreview ? DeterminePreviewState() : ActionPreviewState.Finished;

            if (SimulationType == SimulationType.ActionDirect && BacPlayer.HasBacEntry)
            {
                if (BacPlayer.CurrentDuration <= BacPlayer.CurrentFrame && (GameBase.IsPlaying || Character.ActorSlot != 0))
                {
                    if (BacPlayer.BacEntryInstance.IsFinished && PreviewState == ActionPreviewState.Finished)
                    {
                        if (BacPlayer.IsPreview)
                        {
                            if (SceneManager.Loop)
                            {
                                BacPlayer.ResetBacPreviewState();
                            }
                            else
                            {
                                GameBase.IsPlaying = false;
                            }
                        }
                        else
                        {
                            BAC_Entry entry = BacPlayer.BacEntryInstance.BacEntry;
                            SimulationType = SimulationType.None;
                            BacPlayer.ClearBacEntry();
                            ActionFinished?.Invoke(this, new ActionFinishedEventArgs(entry));
                        }
                    }
                }

                BacPlayer.Update();
            }
        }

        public override void DelayedUpdate()
        {
            if (SimulationType == SimulationType.ActionDirect && BacPlayer.HasBacEntry)
            {
                BacPlayer.DelayedUpdate();
            }
        }

        public override void Draw()
        {
            if(BacPlayer.BacEntryInstance?.IsPreview == true)
            {
                foreach(BacVisualCueObject visualCue in BacPlayer.BacEntryInstance.VisualSimulationCues)
                {
                    visualCue.Draw();
                }
            }
        }

        public void PlayBacEntry(BAC_File bacFile, BAC_Entry bacEntry, Move move = null, Actor user = null)
        {
            BacPlayer.PlayBacEntry(bacFile, bacEntry, (user != null) ? user : Character, move);
            SimulationType = SimulationType.ActionDirect;
        }

        public void PreviewBacEntry(BAC_File bacFile, BAC_Entry bacEntry, Move move = null, Actor user = null)
        {
            if (!BacPlayer.HasBacEntry) Character.ResetPosition();
            BacPlayer.PlayBacEntryPreview(bacFile, bacEntry, (user != null) ? user : Character, move);
            SimulationType = SimulationType.ActionDirect;
        }

        public bool IsBacEntryActive(BAC_Entry bacEntry)
        {
            return BacPlayer.BacEntryInstance?.BacEntry == bacEntry;
        }

        private ActionPreviewState DeterminePreviewState()
        {
            if(BacPlayer.BacEntryInstance == null) return ActionPreviewState.Finished;
            if (BacPlayer.CurrentDuration > BacPlayer.CurrentFrame) return ActionPreviewState.Running;

            if (SceneManager.Actors[1] != null)
            {
                if (SceneManager.Actors[1].Controller.State != Engine.Character.ActorState.Idle) 
                    return ActionPreviewState.WaitingVictim;
            }

            if (BacPlayer.BacEntryInstance.LoopEnabled && SceneManager.AllowBacLoop) return ActionPreviewState.Looping;

            return ActionPreviewState.Finished;
        }

        private void SceneManager_BacValuesChanged(object sender, EventArgs e)
        {
            if (SimulationType == SimulationType.ActionDirect && Character.ActorSlot == 0)
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

    public delegate void ActionFinishedEventHandler(object source, ActionFinishedEventArgs e);

    public class ActionFinishedEventArgs : EventArgs
    {
        public BAC_Entry BacEntry { get; private set; }

        public ActionFinishedEventArgs(BAC_Entry bacEntry)
        {
            BacEntry = bacEntry;
        }
    }

    public enum ActionPreviewState
    {
        Running,
        Looping,
        Finished,
        WaitingProjectiles,
        WaitingVictim
    }
}
