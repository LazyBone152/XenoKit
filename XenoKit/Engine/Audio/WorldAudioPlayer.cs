using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using AudioCueEditor.Audio;
using Microsoft.Xna.Framework;
using NAudio.Wave;
using Xv2CoreLib.AFS2;
using Xv2CoreLib.HCA;

namespace XenoKit.Engine.Audio
{
    public class WorldAudioPlayer
    {
        public WaveOut wavePlayer = new WaveOut();
        public WavStream CurrentWav { get; private set; }
        
        //State
        public bool IsFinished { get; private set; }

        //World object that the sound plays on, if 3D_Def is enabled.
        private Entity entity = null;
        private bool _3d_Def = false;

        private float volume = 1f;


        public WorldAudioPlayer(Entity entity, bool _3d)
        {
            wavePlayer.DesiredLatency = 800;
            wavePlayer.NumberOfBuffers = 2;
            this.entity = entity;
            _3d_Def = _3d;
            wavePlayer.PlaybackStopped += WavePlayer_PlaybackStopped;
        }

        //Update
        public void Update()
        {
            if (wavePlayer == null || CurrentWav == null) return;

            if (_3d_Def && entity != null)
            {
                //Stupid approximation for now
                float distance = Vector3.Distance(SceneManager.MainCamera.CameraState.Position, entity.Transform.Translation);

                //BUG: This sets the devices output volume, which causes the choppy audio with multiple sounds playing
                if (distance < 1f)
                {
                    if(wavePlayer.Volume != 1f)
                        wavePlayer.Volume = volume;
                }
                else
                {
                    float volTemp = (volume / distance) * 2;
                    float volFinal = (volTemp > 1f) ? 1f : volTemp;

                    //Update volume only if it is sufficently different.
                    if(Math.Abs(wavePlayer.Volume - volFinal) > 0.001f)
                        wavePlayer.Volume = volFinal;
                }

            }
        }

        //Set Audio
        public async Task AsyncSetHcaAudio(AFS2_Entry awbEntry, float volume)
        {
            HcaMetadata meta = new HcaMetadata(awbEntry.bytes);

            WavStream wav = null;

            if (awbEntry.WavBytes != null)
            {
                await Task.Run(() => wav = new WavStream(awbEntry.WavBytes));
            }
            else
            {
                awbEntry.WavBytes = HCA.DecodeToWav(awbEntry.bytes);
                await Task.Run(() => wav = new WavStream(awbEntry.WavBytes));

                //await Task.Run(() => wav = HCA.DecodeToWavStream(awbEntry.bytes));
            }

            if (wavePlayer.PlaybackState != PlaybackState.Stopped)
                wavePlayer.Stop();

            if (CurrentWav != null)
                CurrentWav.Dispose();
            CurrentWav = wav;

            //Load wav
            await Task.Run(() => wavePlayer.Init(wav.waveStream));
            

            //Initial volume
            wavePlayer.Volume = volume;
            this.volume = volume;

            //Set loop
            if(meta.HasLoopData)
            {
                SetLoop(meta.LoopStartMs, meta.LoopEndMs);
            }

            wavePlayer.Play();
        }

        private void SetLoop(uint start, uint end)
        {
            CurrentWav?.waveStream.SetLoop(start, end);
        }


        //Terminate
        public void Terminate()
        {
            wavePlayer.PlaybackStopped -= WavePlayer_PlaybackStopped;

            IsFinished = true;

            wavePlayer.Stop();

            try
            {
                wavePlayer.Dispose();

                if (CurrentWav != null)
                    CurrentWav.Dispose();
            }
            catch { }

            CurrentWav = null;
        }

        private void WavePlayer_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            Terminate();
        }
    }
}
