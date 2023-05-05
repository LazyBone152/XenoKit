using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XenoKit.Editor;
using XenoKit.Engine.Scripting;
using Xv2CoreLib.ACB;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Audio
{
    public class AudioEngine
    {
        private List<CueInstance> Cues = new List<CueInstance>();

        //Deferred tasks. These will be executed at the start of the next update cycle.
        private List<Task> DeferredTasks = new List<Task>();

        private void CueEnded_Event(object sender, EventArgs e)
        {
            if(sender is CueInstance cue)
            {
                cue.CueEnded -= CueEnded_Event;
                Cues.Remove(cue);
            }
        }

        public void Update()
        {
            //Execute deferred tasks before updating the cues
            if(DeferredTasks.Count > 0)
            {
                foreach (var task in DeferredTasks)
                    task.RunSynchronously();

                DeferredTasks.Clear();
            }

            //Update cues
            for(int i = 0; i < Cues.Count; i++)
            {
                Cues[i].Update();
            }
        }

        #region Play
        public void PreviewCue(int cueId, ACB_Wrapper acbFile)
        {
            Action action = new Action(() =>
            {
                var cue = new CueInstance(this, acbFile, cueId, null, true, null, false);
                cue.CueEnded += CueEnded_Event;
                Cues.Add(cue);
                cue.Init();
            });

            DeferredTasks.Add(new Task(action));
        }

        public void PlayCue(int cueId, ACB_Wrapper acbFile, Entity entity, ScriptEntity scriptEntity = null, bool terminateWhenOutOfScope = false)
        {
            if (!SettingsManager.settings.XenoKit_AudioSimulation) return;

            Action action = new Action(() =>
            {
                var cue = new CueInstance(this, acbFile, cueId, entity, false, scriptEntity, terminateWhenOutOfScope);
                cue.CueEnded += CueEnded_Event;
                Cues.Add(cue);
                cue.Init();

                //Log.Add($"AudioEngine: Playing cue {cueId} in  {acbFile.AcbFile.Name}");
            });

            DeferredTasks.Add(new Task(action));
        }

        public void PlayCue(AcbTableReference targetId, TargetType targetType, string acbName, ACB_Wrapper thisAcb, Entity entity)
        {
            if (!SettingsManager.settings.XenoKit_AudioSimulation) return;

            if (targetType == TargetType.SpecificAcb && acbName == thisAcb.AcbFile.Name)
            {
                PlayCue((int)thisAcb.AcbFile.GetCueId(targetId.TableGuid), thisAcb, entity);
            }
            else if(targetType == TargetType.SpecificAcb)
            {
                Log.Add($"AudioEngine: Action_Play currently only possible with self.");
            }
            else
            {
                Log.Add($"AudioEngine: Unsupported Action TargetType == {targetType}.");
            }
        }

        #endregion

        #region Stop
        public void StopCues()
        {
            foreach(var cue in Cues)
            {
                StopCue(cue.CueId);
            }
        }

        /// <summary>
        /// Stops all cues matching CueId
        /// </summary>
        public void StopCue(int cueId)
        {
            Action action = new Action(() =>
            {
                foreach (var cue in Cues.Where(x => x.CueId == cueId))
                {
                    //cue.CueEnded -= CueEnded_Event;
                    cue.Terminate();
                }
            });

            DeferredTasks.Add(new Task(action));
        }

        /// <summary>
        /// Stops the cueId in the specified ACB.
        /// </summary>
        /// <param name="acbName">This is the AcbName declared in the ACB file.</param>
        public void StopCue(int cueId, string acbName)
        {
            Action action = new Action(() =>
            {
                foreach (var cue in Cues.Where(x => x.CueId == cueId && x.AcbName == acbName))
                {
                    //cue.CueEnded -= CueEnded_Event;
                    cue.Terminate();
                }
            });

            DeferredTasks.Add(new Task(action));
        }

        public void StopCue(AcbTableReference targetId, TargetType targetType, string acbName)
        {
            switch (targetType)
            {
                case TargetType.AnyAcb:
                    StopCue(targetId.TableIndex_Int);
                    break;
                case TargetType.SpecificAcb:
                    StopCue(targetId.TableIndex_Int, acbName);
                    break;
                default:
                    Log.Add($"AudioEngine: Unsupported Action TargetType == {targetType}");
                    break;
            }
        }

        #endregion

    }

}
