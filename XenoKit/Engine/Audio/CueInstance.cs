using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using XenoKit.Editor;
using XenoKit.Engine.Scripting;
using Xv2CoreLib.ACB;
using Xv2CoreLib.AFS2;

namespace XenoKit.Engine.Audio
{
    public class CueInstance
    {
        public event EventHandler CueEnded;

        //Cue information:
        public readonly string AcbName;
        public readonly int CueId;
        public bool IsFinished { get; private set; } = false;
        private bool locked = true;

        //State:
        private readonly ACB_Wrapper acb;
        private readonly Entity Entity;
        private readonly AudioEngine AudioEngine;

        //Deferred actions:
        private bool tracksHaveFinished = false;
        private bool terminateCue = false;

        //Script Entity:
        private ScriptEntity ScriptEntity;
        private bool HasScriptEntity;
        private bool TerminateWhenOutOfScope;

        //Audio Playback:
        private List<WorldAudioPlayer> AudioPlayers = new List<WorldAudioPlayer>();
        private List<TrackInstance> Tracks = new List<TrackInstance>();
        private SequenceType SequenceType = SequenceType.Polyphonic;
        private bool _3d_Def;
        private float Volume;
        private bool loop = false;
        private bool isPreview = false;

        //Actions:
        private List<ActionInstance> Actions = new List<ActionInstance>();

        //Statics:
        private static ACB_Waveform LastRandomWaveform = null;

        //Timing:
        private Timer Timer;
        private byte CueLengthElapsed = 0;
        private Stopwatch Stopwatch = new Stopwatch();


        public CueInstance(AudioEngine audioEngine, ACB_Wrapper acb, int cueId, Entity entity, bool _isPreview, ScriptEntity scriptEntity, bool terminateWhenOutOfScope)
        {
            this.acb = acb;
            AcbName = acb.AcbFile.Name;
            CueId = cueId;
            Entity = entity;
            AudioEngine = audioEngine;
            isPreview = _isPreview;

            //Script
            ScriptEntity = scriptEntity;
            HasScriptEntity = ScriptEntity != null;
            TerminateWhenOutOfScope = terminateWhenOutOfScope;
        }

        #region LoadEndCue
        /// <summary>
        /// Begins the CUE. Call this after CueEnded has been subscribed to!
        /// </summary>
        public void Init()
        {
            LoadCue();
            BeginPlay();
        }

        private void LoadCue()
        {
            Stopwatch.Start();

            var cue = acb.AcbFile.GetCue(CueId);

            if (cue == null)
            {
                Log.Add($"AudioEngine: Cue {CueId} not found in ACB: {AcbName}.", LogType.Warning);
                IsFinished = true;
                return;
            }
            //Volume
            _3d_Def = (!isPreview) ? acb.AcbFile.DoesCueHave3dDef(cue) : false;

            Volume = acb.AcbFile.GetCueBaseVolume(cue) + Xv2CoreLib.Random.Range(0, acb.AcbFile.GetCueRandomVolume(cue)) * acb.AcbFile.AcbVolume;

            if (Volume > 1f)
                Volume = 1f;

            var waveforms = acb.AcbFile.GetWaveformsFromCue(cue);

            if (cue.ReferenceType == ReferenceType.Sequence)
            {
                var sequence = acb.AcbFile.GetSequence(cue.ReferenceIndex.TableGuid);
                SequenceType = sequence.Type;

                //Tracks
                switch (SequenceType)
                {
                    case SequenceType.Polyphonic: //Play all tracks simultaneously
                        AddPolyphonic(waveforms);
                        break;
                    case SequenceType.Sequential:
                        AddSequential(waveforms);
                        break;
                    case SequenceType.Shuffle:
                        AddShuffle(waveforms);
                        break;
                    case SequenceType.Random:
                        AddRandom(waveforms);
                        break;
                    case SequenceType.RandomNoRepeat:
                        AddRandomNoRepeat(waveforms);
                        break;
                    case SequenceType.ComboSequential:
                    case SequenceType.Switch:
                        Log.Add($"AudioEngine: Unsupported SequenceType == {SequenceType}. Terminating cue...", LogType.Warning);
                        Terminate();
                        return;
                }

                //ActionTracks
                LoadAction(sequence.ActionTracks, CommandTableType.SequenceCommand);
            }
            else if (cue.ReferenceType == ReferenceType.Synth)
            {
                AddPolyphonic(waveforms);

                //ActionTracks
                var synth = acb.AcbFile.GetSynth(cue.ReferenceIndex.TableGuid);

                if(synth != null)
                {
                    LoadAction(synth.ActionTracks, CommandTableType.SynthCommand);
                }

            }
            else if(cue.ReferenceType == ReferenceType.Waveform)
            {
                AddPolyphonic(waveforms);
            }
            else
            {
                Log.Add($"AudioEngine: Unsupported ReferenceType == {cue.ReferenceType}. Terminating cue...", LogType.Warning);
                Terminate();
                return;
            }

            if(cue.Length == 0)
            {
                //Action
                Timer = new Timer(1000); //1 second
                Timer.Start();
                Timer.Elapsed += EndCue;
            }
            else if(cue.Length < uint.MaxValue)
            {
                //Regular track
                Timer = new Timer(cue.Length);
                Timer.Start();
                Timer.Elapsed += EndCue;
            }
            else
            {
                //Looped track
                Timer = new Timer(1000 * 60 * 5); //5 minutes
                Timer.Start();
                Timer.Elapsed += EndCue;
                loop = true;
            }
        }

        private void LoadAction(List<AcbTableReference> refs, CommandTableType type)
        {
            //if (isPreview) return; 

            foreach (var trackIndex in refs)
            {
                ACB_Track track = acb.AcbFile.GetActionTrack(trackIndex.TableGuid);

                if (track != null)
                {
                    ACB_CommandGroup commands = acb.AcbFile.CommandTables.GetCommand(track.CommandIndex.TableGuid, type);

                    if (commands != null)
                    {
                        ActionInstance action = new ActionInstance(track.TargetType, track.TargetId, track.TargetName, track.TargetAcbName, commands.GetActionType());

                        if (action.ActionType != CommandType.Null)
                            Actions.Add(action);
                    }
                }
            }
        }

        private void EndCue(object sender, ElapsedEventArgs e)
        {
            CueLengthElapsed++;

            if(CueLengthElapsed >= 2)
            {
                //Cue can stay around for up to 2 times the declared length
                //This will force-kill the cue if it hasn't already ended
                Terminate();
            }
        }
        
        public void Terminate()
        {
            terminateCue = true;
        }

        private void KillCue()
        {
            foreach (var audioPlayer in AudioPlayers)
            {
                audioPlayer.Terminate();
            }

            IsFinished = true;

            if (Timer != null)
                Timer.Elapsed -= EndCue;

            CueEnded?.Invoke(this, EventArgs.Empty);
        }

        private void WavePlayer_PlaybackStopped(object sender, NAudio.Wave.StoppedEventArgs e)
        {
            tracksHaveFinished = true;
        }

        #endregion

        #region TrackPlayback
        private async void BeginPlay()
        {
            if(SequenceType == SequenceType.Sequential || SequenceType == SequenceType.Shuffle)
            {
                //Play one by one
                if(Tracks.Count > 0)
                {
                    for (int i = 0; i < Tracks[0].awbEntries.Count; i++)
                    {
                        WorldAudioPlayer player = new WorldAudioPlayer(Entity, _3d_Def);
                        player.wavePlayer.PlaybackStopped += WavePlayer_PlaybackStopped;
                        await player.AsyncSetHcaAudio(Tracks[0].awbEntries[i], Volume);

                        AudioPlayers.Add(player);
                    }

                    Tracks.RemoveAt(0);
                }
            }
            else
            {
                //Play all at once
                for (int a = Tracks.Count - 1; a >= 0; a--)
                {
                    for (int i = 0; i < Tracks[a].awbEntries.Count; i++)
                    {
                        WorldAudioPlayer player = new WorldAudioPlayer(Entity, _3d_Def);
                        player.wavePlayer.PlaybackStopped += WavePlayer_PlaybackStopped;
                        await player.AsyncSetHcaAudio(Tracks[a].awbEntries[i], Volume);

                        AudioPlayers.Add(player);
                    }

                    Tracks.RemoveAt(a);
                }
            }

            Stopwatch.Stop();
            //Log.Add($"AudioEngine: Cue {CueId} loaded in {Stopwatch.ElapsedMilliseconds} ms.", LogType.Info);
            locked = false;
        }

        private async Task<bool> TryPlaySeq()
        {
            locked = true;

            if (Tracks.Count > 0)
            {
                for (int i = 0; i < Tracks[0].awbEntries.Count; i++)
                {
                    WorldAudioPlayer player = new WorldAudioPlayer(Entity, _3d_Def);
                    player.wavePlayer.PlaybackStopped += WavePlayer_PlaybackStopped;
                    await player.AsyncSetHcaAudio(Tracks[0].awbEntries[i], Volume);
                }

                Tracks.RemoveAt(0);
                locked = false;
                return true;
            }
            else
            {
                locked = false;
                return false;
            }

        }

        private async void TryPlaySeqTracks()
        {
            await TryPlaySeq();
        }
        #endregion

        #region LoadTracks
        private void AddPolyphonic(List<ACB_Waveform> waveforms)
        {
            List<AFS2_Entry> awbEntries = new List<AFS2_Entry>();

            foreach(var waveform in waveforms)
            {
                var awb = acb.AcbFile.GetAfs2Entry(waveform.AwbId);

                if(awb != null)
                {
                    awbEntries.Add(awb);
                }

            }

            Tracks.Add(new TrackInstance(awbEntries));
        }

        private void AddSequential(List<ACB_Waveform> waveforms)
        {
            foreach (var waveform in waveforms)
            {
                var awb = acb.AcbFile.GetAfs2Entry(waveform.AwbId);

                if (awb != null)
                {
                    Tracks.Add(new TrackInstance(new List<AFS2_Entry>() { awb }));
                }

            }
        }

        private void AddShuffle(List<ACB_Waveform> waveforms)
        {
            foreach (var waveform in waveforms.OrderBy(x => Xv2CoreLib.Random.Range(0, int.MaxValue)))
            {
                var awb = acb.AcbFile.GetAfs2Entry(waveform.AwbId);

                if (awb != null)
                {
                    Tracks.Add(new TrackInstance(new List<AFS2_Entry>() { awb }));
                }
            }
        }

        private void AddRandom(List<ACB_Waveform> waveforms)
        {
            ACB_Waveform randomWaveform = GetRandomWaveform(waveforms, true);
            var awb = acb.AcbFile.GetAfs2Entry(randomWaveform.AwbId);

            Tracks.Add(new TrackInstance(new List<AFS2_Entry>() { awb }));
        }

        private void AddRandomNoRepeat(List<ACB_Waveform> waveforms)
        {
            ACB_Waveform randomWaveform = GetRandomWaveform(waveforms, false);
            var awb = acb.AcbFile.GetAfs2Entry(randomWaveform.AwbId);

            Tracks.Add(new TrackInstance(new List<AFS2_Entry>() { awb }));
        }
        
        private ACB_Waveform GetRandomWaveform(List<ACB_Waveform> waveforms, bool allowRepeats)
        {
            ACB_Waveform random = null;

            foreach (var waveform in waveforms.OrderBy(x => Xv2CoreLib.Random.Range(0, int.MaxValue)))
            {
                random = waveform;

                if(LastRandomWaveform != waveform || allowRepeats)
                    return waveform;
            }

            return random;
        }
        #endregion

        #region ActionTrack
        private void ProcessActionTracks()
        {
            for (int i = Actions.Count - 1; i >= 0; i--)
            {
                switch (Actions[i].ActionType)
                {
                    case CommandType.Action_Play:
                        AudioEngine.PlayCue(Actions[i].TargetId, Actions[i].TargetType, Actions[i].TargetAcbName, acb, Entity);
                        break;
                    case CommandType.Action_Stop:
                        AudioEngine.StopCue(Actions[i].TargetId, Actions[i].TargetType, Actions[i].TargetAcbName);
                        break;
                    default:
                        Log.Add($"AudioEngine: Unsupported ActionType == {Actions[i].ActionType}");
                        break;
                }

                //Action executed, now remove
                Actions.RemoveAt(i);
            }
        }
        #endregion

        public void Update()
        {
            if (terminateCue)
            {
                KillCue();
                return;
            }

            if (tracksHaveFinished)
            {
                AudioPlayers.RemoveAll(x => x.IsFinished);
                tracksHaveFinished = false;
            }

            //If CUE is out of scope, terminate it
            if(HasScriptEntity)
            {
                if (!ScriptEntity.InScope && TerminateWhenOutOfScope)
                {
                    Terminate();
                    return;
                }
            }
                

            //Terminate the cue if its finished
            if((loop || CueLengthElapsed > 0) && AudioPlayers.Count == 0 && Tracks.Count == 0)
            {
                KillCue();
                return;
            }

            //Play the next sequential track
            if(AudioPlayers.Count == 0 && Tracks.Count > 0 && !locked)
            {
                TryPlaySeqTracks();
            }

            if (!locked && Actions.Count > 0)
            {
                ProcessActionTracks();
            }
        }

        }

    public struct TrackInstance
    {
        public List<AFS2_Entry> awbEntries;

        public TrackInstance(List<AFS2_Entry> _awbEntries)
        {
            awbEntries = _awbEntries;
        }

    }

    public struct ActionInstance
    {
        public TargetType TargetType;
        public AcbTableReference TargetId;
        public string TargetName;
        public string TargetAcbName;
        public CommandType ActionType;

        public ActionInstance(TargetType targetType, AcbTableReference targetId, string targetName, string targetAcbName, CommandType actionType)
        {
            TargetType = targetType;
            TargetId = targetId;
            TargetName = targetName;
            TargetAcbName = targetAcbName;
            ActionType = actionType;
        }
    }
}
