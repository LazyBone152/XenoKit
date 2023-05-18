using System.Threading.Tasks;
using AudioCueEditor.Audio;
using Microsoft.Xna.Framework;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Xv2CoreLib.AFS2;
using Xv2CoreLib.HCA;

namespace XenoKit.Engine.Audio
{
    public class WorldAudioPlayer
    {
        public WaveOut wavePlayer = new WaveOut();
        public WavStream CurrentWav { get; private set; }
        private VolumeSampleProvider volumeSampleProvider = null;

        //State
        public bool IsFinished { get; private set; }

        //World object that the sound plays on, if 3D_Def is enabled.
        private Entity entity = null;
        private bool is3D_Def = false;


        public WorldAudioPlayer(Entity entity, bool _3d)
        {
            wavePlayer.DesiredLatency = 800;
            wavePlayer.NumberOfBuffers = 2;
            this.entity = entity;
            is3D_Def = _3d;
            wavePlayer.PlaybackStopped += WavePlayer_PlaybackStopped;
        }

        private float Calculate3DVolume(float initialVolume)
        {
            if (is3D_Def && entity != null)
            {
                //Stupid approximation for now
                float distance = Vector3.Distance(SceneManager.MainCamera.CameraState.Position, entity.Transform.Translation);

                if (distance < 1f)
                {
                    return initialVolume;
                }
                else
                {
                    float volTemp = (initialVolume / distance) * 2;
                    return (volTemp > 1f) ? 1f : volTemp;
                }

            }

            return 1f;
        }

        //Set Audio
        public async Task AsyncSetHcaAudio(AFS2_Entry awbEntry, float initialVolume)
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

            //Calculate volume
            volumeSampleProvider = new VolumeSampleProvider(wav.waveStream.ToSampleProvider());
            volumeSampleProvider.Volume = Calculate3DVolume(initialVolume);

            //Load wav
            await Task.Run(() => wavePlayer.Init(volumeSampleProvider));
            
            //Set loop
            if (meta.HasLoopData)
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
