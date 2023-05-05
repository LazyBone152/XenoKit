using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Audio;
using Xv2CoreLib.BAC;

namespace XenoKit.Engine.Scripting
{
    public class BacPlayer
    {
        //Parent character that this BacPlayer is for.
        private Character character;

        //Bac Entry:
        private BacEntryInstance bacEntryInstance = null;
        public int CurrentFrame
        {
            get
            {
                if (bacEntryInstance == null) return 0;
                return (int)bacEntryInstance.currentFrame;
            }
            set
            {
                bacEntryInstance.currentFrame = value;
            }
        }
        public int CurrentDuration
        {
            get
            {
                if (bacEntryInstance == null) return 0;
                return GetCurrentDuration();
            }
        }

        //Keeps track of all bac entries processed in the current playing. 
        private List<IBacType> ProcessedBacTypes = new List<IBacType>();

        /// <summary>
        /// For thread-safety. Updating wont happen when this is set, which prevents a bac entry that has just been removed from being accessed.
        /// </summary>
        private bool clearing = false;


        public BacPlayer(Character chara)
        {
            character = chara;
            SceneManager.BacValuesChanged += SceneManager_BacValuesChanged;
        }

        public void PlayBacEntry(BAC_File bacFile, BAC_Entry bacEntry, Move move = null)
        {
            if (bacEntryInstance == null) character.ResetPosition();
            ClearBacEntry();
            bacEntryInstance = new BacEntryInstance(bacFile, bacEntry, move, true, character.baseTransform);
        }

        public void Update()
        {
            if (!SceneManager.IsOnTab(EditorTabs.Action) || clearing || bacEntryInstance == null) return;
            if (bacEntryInstance.BacEntry == null)
            {
                ClearBacEntry();
                return;
            }

            if (CurrentDuration <= CurrentFrame && SceneManager.IsPlaying)
            {
                if (SceneManager.Loop)
                {
                    ResetBacState();
                }
                else
                {
                    SceneManager.IsPlaying = false;
                    //Stop();
                }
            }

            UpdateBac(ref bacEntryInstance.currentFrame);

            //Advance current frame
            if (SceneManager.IsPlaying)
            {
                bacEntryInstance.currentFrame += SceneManager.MainAnimTimeScale * SceneManager.BacTimeScale;
            }
        }

        #region Simulation
        private void UpdateBac(ref float _refFrame)
        {
            float frame = _refFrame;
            SceneManager.SetBacTimeScale(1f, true);

            if (clearing) return;
            var validEntries = bacEntryInstance.BacEntry.IBacTypes.Where
                (b => b.StartTime <= frame && (b.StartTime + b.Duration >= frame) && (b.Flags == character.Type || b.Flags == 0))
                .OrderBy(x => x.GetType() == typeof(BAC_Type4)); //TimeScale must be resolved before animation/camera


            //Read bac types
            foreach (var type in validEntries)
            {
                //Animation
                if (type is BAC_Type0 animation)
                {
                    if (ProcessedBacTypes.Contains(type)) continue;
                    
                    var ean = Files.Instance.GetEanFile(animation.Ean_Type, bacEntryInstance.CurrentMove, character, true, true);

                    if (ean != null && animation.EanIndex != ushort.MaxValue)
                    {
                        switch (animation.Ean_Type)
                        {
                            case BAC_Type0.EanType.Character:
                            case BAC_Type0.EanType.Common:
                            case BAC_Type0.EanType.Skill:
                                character.animationPlayer.PlayPrimaryAnimation(ean, animation.EanIndex, animation.StartFrame, animation.EndFrame, animation.BlendWeight, animation.BlendWeightFrameStep, animation.AnimFlags, true, animation.TimeScale);
                                SceneManager.MainAnimTimeScale = animation.TimeScale;

                                if (animation.StartFrame != 0 && _refFrame == 0f) //On first frame, skipping to startFrame on animations is allowed
                                    _refFrame = animation.StartFrame;
                                break;
                            case BAC_Type0.EanType.FaceA:
                            case BAC_Type0.EanType.FaceB:
                                character.animationPlayer.PlaySecondaryAnimation(ean, animation.EanIndex, animation.StartFrame, (ushort)animation.Duration, animation.BlendWeight, animation.BlendWeightFrameStep, animation.TimeScale, true);
                                break;
                            case BAC_Type0.EanType.CommonTail:
                                character.animationPlayer.PlaySecondaryAnimation(ean, animation.EanIndex, animation.StartFrame, animation.EndFrame, animation.BlendWeight, animation.BlendWeightFrameStep, animation.TimeScale);
                                break;
                        }
                    }
                }

                //TimeScale
                if (type is BAC_Type4 timeScale)
                {
                    SceneManager.SetBacTimeScale(timeScale.TimeScale, false);
                }

                //Camera
                if(type is BAC_Type10 camera)
                {
                    if (ProcessedBacTypes.Contains(type)) continue;
                    
                    var ean = Files.Instance.GetCamEanFile(camera.Ean_Type, bacEntryInstance.CurrentMove, character, true, true);

                    if(ean != null && camera.EanIndex != ushort.MaxValue)
                    {
                        var cam = ean.GetAnimation(camera.EanIndex, true);

                        if (cam != null)
                            SceneManager.PlayCameraAnimation(cam, camera, SceneManager.IndexOfCharacter(character), true);
                        else
                            Log.Add(string.Format("BacPlayer: Could not find the camera animation with the ID {0} in the {1} cam.ean.", camera.EanIndex, camera.Ean_Type), LogType.Error);
                    }
                }

                //Sound
                if (type is BAC_Type11 sound)
                {
                    if (ProcessedBacTypes.Contains(type)) continue;

                    var acb = Files.Instance.GetAcbFile(sound.AcbType, bacEntryInstance.CurrentMove, character, true);

                    if(acb != null && sound.CueId != ushort.MaxValue && SceneManager.IsPlaying)
                    {
                        SceneManager.AudioEngine.PlayCue(sound.CueId, acb, character);

                        //Handle SoundFlags
                        if(sound.SoundFlags.HasFlag(SoundFlags.StopWhenParentEnds))
                            bacEntryInstance.AddScopedCue(sound.CueId, acb.AcbFile.Name);
                    }
                }


                //Add type to processed list
                ProcessedBacTypes.Add(type);
            }
            
        }
        
        /// <summary>
        /// Simulate the bac entry up to the specified frame. This will also simulate any animations, cameras, hitboxes, projectiles and so on.
        /// </summary>
        private void Seek(int frame, bool clearAnimations = true)
        {
            if (clearing) return;

            //Get camera state so it can be restored after resimulating the bac state. 
            Vector3 originalCameraPos = SceneManager.gameInstance.camera.ActualPosition;
            Vector3 originalCameraTargetPos = SceneManager.gameInstance.camera.ActualTargetPosition;
            float originalFov = SceneManager.gameInstance.camera.FieldOfView;
            float originalRoll = SceneManager.gameInstance.camera.Roll;

            //Calculate the number of "blending" frames between animations. With this we dont need to calculate every single animation frame, just what is needed. (big perf boost)
            int numBlendingFrames = BAC_Type0.CalculateNumOfBlendingFrames(bacEntryInstance.BacEntry.IBacTypes, frame);

            //Clean up
            RevertCharacterPosition(true);

            if (clearAnimations)
                character.animationPlayer.ClearCurrentAnimation(true);

            SceneManager.CameraInstance.ClearCameraAnimation();
            SceneManager.MainAnimTimeScale = 1f;
            ProcessedBacTypes.Clear();

            float _frame = frame;

            for (int i = 0; i <= frame; i++)
            {
                _frame = i;
                UpdateBac(ref _frame);

                if (_frame > frame) //Frameskip shot us past the specified frame. End seek here.
                {
                    character.Simulate(true, true);
                    SceneManager.gameInstance.camera.Simulate(true, true);
                    break;
                }
                else
                {
                    character.Simulate(frame - i < numBlendingFrames, true); //Fully update anim positions for as long as required for accurate blending
                    SceneManager.gameInstance.camera.Simulate(frame - i <= 1, true); //Only fully update camera on last frame
                }
            }

            bacEntryInstance.currentFrame = _frame;

            //Remove all Type11 (Sound) bac types from the processed list, as they are not actually processed while paused
            ProcessedBacTypes.RemoveAll(x => x.GetType() == typeof(BAC_Type11));

            //Reset camera state if no cam anim is active. This allows the bac entry to be paused, then the camera moved, and the scene to resume without the camera resetting to the last simulated camera
            if (SceneManager.gameInstance.camera.cameraInstance == null)
            {
                SceneManager.gameInstance.camera.ActualPosition = originalCameraPos;
                SceneManager.gameInstance.camera.ActualTargetPosition = originalCameraTargetPos;
                SceneManager.gameInstance.camera.FieldOfView = originalFov;
                SceneManager.gameInstance.camera.Roll = originalRoll;
                SceneManager.gameInstance.camera.ResetViewerAngles();
            }
        }
        
        //Helpers:
        private void RevertCharacterPosition(bool alwaysRevert)
        {
            if (bacEntryInstance == null) return;
            character.baseTransform = (bacEntryInstance.RevertPosition || alwaysRevert) ? bacEntryInstance.OriginalMatrix : character.Transform;
            character.animatedTransform = Matrix.Identity;
        }

        private void ResetBacState()
        {
            RevertCharacterPosition(false);
            CurrentFrame = 0;
            ProcessedBacTypes.Clear();
        }

        public void ClearBacEntry()
        {
            clearing = true;
            bacEntryInstance = null;
            ProcessedBacTypes.Clear();

            //Clean up used resources
            SceneManager.gameInstance.camera.ClearCameraAnimation();

            clearing = false;
        }


        #endregion


        #region Helpers
        private int GetCurrentDuration()
        {
            int duration = 0;
            if (bacEntryInstance == null || bacEntryInstance?.BacEntry == null || clearing) return 0;

            foreach(var type in bacEntryInstance.BacEntry.IBacTypes)
            {
                //Animations
                if(type is BAC_Type0 type0)
                {
                    //Dont count this if its not a full body animation
                    if (type0.Ean_Type != BAC_Type0.EanType.Character && type0.Ean_Type != BAC_Type0.EanType.Skill && type0.Ean_Type != BAC_Type0.EanType.Common)
                        continue;

                    int thisDuration = 0;
                    //Check for hard coded duration set in bac type (endFrame - startFrame).
                    //If endFrame is 0xffff, then get duration from the ean
                    if(type0.EndFrame != ushort.MaxValue)
                    {
                        thisDuration = (type0.EndFrame - type0.StartFrame) + ((type.StartTime == 0) ? type0.StartFrame : (ushort)type.StartTime);
                    }
                    else
                    {
                        var eanFile = Files.Instance.GetEanFile(type0.Ean_Type, bacEntryInstance.CurrentMove, character, true, true);

                        if(eanFile != null)
                        {
                            var anim = eanFile.GetAnimation(type0.EanIndex, true);
                            if(anim != null)
                            {
                                switch (type0.Ean_Type)
                                {
                                    case BAC_Type0.EanType.Character:
                                    case BAC_Type0.EanType.Common:
                                    case BAC_Type0.EanType.Skill:
                                        thisDuration = anim.FrameCount + type.StartTime;
                                        break;
                                }
                            }
                        }
                    }

                    if (thisDuration > duration)
                        duration = thisDuration;
                }
            }

            return duration;
        }
        
        private void SceneManager_BacValuesChanged(object sender, EventArgs e)
        {
            //Values in the current BAC have changed and so the current frame must be re-simulated.
            if (bacEntryInstance != null)
            {
                bool wasPlaying = SceneManager.IsPlaying;
                SceneManager.IsPlaying = false;

                Seek((int)bacEntryInstance.currentFrame, true);

                SceneManager.IsPlaying = wasPlaying;
            }
        }

        #endregion

        #region Control
        public void Resume()
        {
            if (bacEntryInstance == null) return;

            if (bacEntryInstance.currentFrame >= CurrentDuration)
                ResetBacState();

            //If anything was edited, it will be re-simulated
            Seek((int)bacEntryInstance.currentFrame, true); 
        }

        public void Stop()
        {
            if (bacEntryInstance == null) return;

            character.animationPlayer.ClearCurrentAnimation(true);
            bacEntryInstance.currentFrame = 0;
            ProcessedBacTypes.Clear();
            character.ResetPosition();
            bacEntryInstance.OriginalMatrix = character.Transform;
            SceneManager.MainAnimTimeScale = 1f;
            SceneManager.IsPlaying = false;
        }

        public void SeekPrevFrame()
        {
            if (bacEntryInstance == null) return;

            if (CurrentFrame != 0)
            {
                Seek(CurrentFrame - 1, true);
            }
            else
            {
                Seek(CurrentDuration, true);
            }
        }

        public void SeekNextFrame()
        {
            if (bacEntryInstance == null) return;

            if (CurrentFrame < CurrentDuration)
                Seek(CurrentFrame + 1, true);
            else
                Seek(0, true);
        }
        #endregion

        
    }

    public class BacEntryInstance
    {
        //Files:
        public Move CurrentMove; //Optional. Not needed for solo-bacs (CMN)
        public BAC_File BacFile;
        public BAC_Entry BacEntry;

        //State:
        public float currentFrame = 0;

        //Position:
        /// <summary>
        /// When the bac entry ends playback, should the character position be reset?
        /// </summary>
        public bool RevertPosition;
        /// <summary>
        /// The character position matrix when the bac entry started.
        /// </summary>
        public Matrix OriginalMatrix;

        //Scoped Cues:
        private List<ScopedCue> ScopedCues = new List<ScopedCue>();


        public BacEntryInstance(BAC_File bacFile, BAC_Entry bacEntry, Move currentMove, bool revertPosition, Matrix originalMatrix)
        {
            BacFile = bacFile;
            BacEntry = bacEntry;
            CurrentMove = currentMove;
            RevertPosition = revertPosition;
            OriginalMatrix = originalMatrix;

            if (BacEntry == null)
                Log.Add($"Bac Entry {bacEntry} not found!", LogType.Error);
        }
    

        /// <summary>
        /// Add a cue as scoped to this BacEntryInstance. When this entry ends, the Terminate function will be called on this cue.
        /// </summary>
        public void AddScopedCue(int cueId, string acbName)
        {
            ScopedCues.Add(new ScopedCue(cueId, acbName));
        }

        public void StopScopedCues()
        {
            foreach(var cue in ScopedCues)
            {
                SceneManager.AudioEngine.StopCue(cue.CueId, cue.AcbName);
            }

            ScopedCues.Clear();
        }

        private struct ScopedCue
        {
            public int CueId;
            public string AcbName;

            public ScopedCue(int cueId, string acbName)
            {
                CueId = cueId;
                AcbName = acbName;
            }
        }
    }

    
}
