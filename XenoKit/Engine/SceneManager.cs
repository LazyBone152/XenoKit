using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using XenoKit.Engine.View;
using XenoKit.Engine;
using Xv2CoreLib.BAC;
using Xv2CoreLib.EAN;
using xv2 = Xv2CoreLib.Xenoverse2;
using XenoKit.Editor;
using XenoKit.Engine.Gizmo;
using Xv2CoreLib.Resource.App;
using XenoKit.Engine.Audio;

namespace XenoKit.Engine
{
    public enum EditorTabs
    {
        //Must match up with Tab Index!
        Nothing = -1,
        Animation = 0,
        Camera = 1,
        Action = 2, //bac
        State = 3, //bcm
        Projectile = 4,
        Hitbox = 5,
        Effect = 6,
        Audio = 7,
        System = 8
    }

    public static class SceneManager
    {
        
        public static Game gameInstance = null;
        public static Camera CameraInstance { get { return gameInstance?.camera; } }
        public static AudioEngine AudioEngine { get { return gameInstance?.audioEngine; } }
        public static SpriteBatch spriteBatch { get { return gameInstance?.spriteBatch; } }
        public static AnimatorGizmo AnimatorGizmo { get { return gameInstance?.animGizmo; } }
        public static GraphicsDevice GraphicsDeviceRef { get { return gameInstance?.GraphicsDevice; } }
        public static Matrix ViewMatrix { get { return gameInstance.camera.ViewMatrix; } }
        public static Matrix ProjectionMatrix { get { return gameInstance.camera.ProjectionMatrix; } }


        //Game loop
        private static bool forceUpdateCamera = false;

        //Other
        /// <summary>
        /// The bone currently selected in the animation tab.
        /// </summary>
        public static string CurrentSelectedBoneName = "";

        #region SceneState
        public static EditorTabs PrevSceneState = EditorTabs.Nothing;
        public static EditorTabs CurrentSceneState = 0;
        public static bool IsPlaying = false;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static bool SetSceneState(int state)
        {
            EditorTabs prevState = CurrentSceneState;
            CurrentSceneState = (EditorTabs)state;

            //Return false if state hasn't actually changed
            if (gameInstance == null || (PrevSceneState == CurrentSceneState)) return false;

            //State has changed, so update PrevSceneState
            PrevSceneState = prevState;

            return true;
        }

        private static void ResetSceneCheck()
        {
            //Reset the scene if PrevSceneState isn't equal to CurrentSceneState
            //This is called whenever a bac entry, anim, camera or something else is played, to clean up after whatever was going on beforehand.
            
            //First, special case for Animation > Camera. Anims shouldn't be reset in this case.
            if(PrevSceneState == EditorTabs.Animation && CurrentSceneState == EditorTabs.Camera)
            {
                ResetState(false);
                PrevSceneState = CurrentSceneState;
            }
            else if((PrevSceneState != CurrentSceneState && PrevSceneState != EditorTabs.Nothing))
            {
                ResetState(true);
                PrevSceneState = CurrentSceneState;
            }
        }

        private static void ResetState(bool resetAnims)
        {
            gameInstance.ResetState(resetAnims);
            BacTimeScale = 1f;
        }
        #endregion

        #region Settings

        public static bool ShowDebugBones = false;
        public static bool WireframeModeCharacters = false;
        public static bool ShowWorldAxis = true;
        public static bool ResolveLeftHandSymetry = true; //https://stackoverflow.com/questions/29370361/does-xna-opengl-use-left-handed-matrices
        public static bool Loop { get { return SettingsManager.settings.XenoKit_Loop; } }
        public static bool AutoPlay { get { return SettingsManager.settings.XenoKit_AutoPlay; } }
        public static bool UseCameras { get { return SettingsManager.settings.XenoKit_EnableCameraAnimations; } }
        public static bool ShowVisualSkeleton { get { return SettingsManager.settings.XenoKit_EnableVisualSkeleton; } }

        public static float BacTimeScale = 1f; //TimeScale specified by a TimeScale bacType
        public static float MainAnimTimeScale = 1f;
        #endregion
        
        #region Characters & Objects
        /// <summary>
        /// All characters active in the current scene. (0 = Primary, 1 = Target, 2 = Partner)
        /// </summary>
        public static Character[] Actors = new Character[3];

        public static bool[] ActorsEnable = new bool[3] { true, false, false };

        /// <summary>
        /// All entities active in the current scene (excluding Actors). 
        /// </summary>
        public static List<IEntity> Entities = new List<IEntity>();
        

        /// <summary>
        /// Checks if the specified character index is loaded, and if its not then loads a default character.
        /// </summary>
        /// <param name="idx">An index in ActiveCharacters (0 - 2).</param>
        private static void EnsureCharacterIsSet(int idx = 0)
        {
            if (idx >= Actors.Length) throw new InvalidOperationException($"SceneManager.EnsureCharacterIsSet: idx {idx} is greater than the maximum amount of Actors.");
            
            if(Actors[idx] == null)
            {
                var characters = Files.Instance.GetLoadedCharacters();
                Character chara = characters.FirstOrDefault(x => !Actors.Contains(x));

                if(chara != null)
                {
                    Log.Add($"{GetActorName(idx)} Actor was not set. Defaulting to the first free loaded character ({characters[0].Name}).", LogType.Info);
                    Actors[idx] = characters[0];
                }
                else
                {
                    Log.Add($"{GetActorName(idx)} Actor was not set. Loading the default...", LogType.Info);

                    //Select a character based on idx. This way we dont populate the scene with multiple Gokus.
                    int charId = 0;

                    switch (idx)
                    {
                        case 1: //Target
                            charId = 16;
                            break;
                    }

                    //Load character
                    chara = Files.Instance.LoadCharacter(charId, 0, null, true);
                    Actors[idx] = chara;
                }
            }
        }
        
        public static bool CharacterExists(int index)
        {
            return Actors[index] != null;
        }

        public static int IndexOfCharacter(Character character)
        {
            for(int i = 0; i < Actors.Length; i++)
                if (Actors[i] == character) return i;

            Log.Add("SceneManager.IndexOfCharacter: Cannot find character in the active scene.", LogType.Warning);

            return 0;
        }
        
        public static void DestroyEntity(IEntity entity)
        {
            Entities.Remove(entity);
        }

        public static void DestroyAllEntities()
        {
            Entities.Clear();
        }
       
        public static void SetActor(Character character, int charaIdx)
        {
            if (Actors[charaIdx] == character)
                return;

            for(int i = 0; i < Actors.Length; i++)
            {
                if (Actors[i] == character)
                    Actors[i] = null;
            }

            Actors[charaIdx] = character;
            Stop();

            Log.Add($"{character.Name} set as the {GetActorName(charaIdx)} actor.");
        }
        
        private static string GetActorName(int charaIdx)
        {
            switch (charaIdx)
            {
                case 0:
                    return "Primary";
                case 1:
                    return "Target";
                default:
                    return charaIdx.ToString();
            }
        }
        #endregion

        #region Events

        public static event EventHandler BacValuesChanged;

        public static event EventHandler CameraCurrentFrameChanged;

        public static event EventHandler AnimationDurationChanged;

        public static void InvokeBacValuesChangedEvent()
        {
            BacValuesChanged?.Invoke(null, null);
        }

        public static void InvokeCameraCurrentFrameChangedEvent()
        {
            CameraCurrentFrameChanged?.Invoke(null, null);
        }

        public static void InvokeAnimationDurationChangedEvent()
        {
            AnimationDurationChanged?.Invoke(null, null);
        }

        #endregion


        public static void Update(GameTime time)
        {
            //Force update camera this frame
            if (forceUpdateCamera && CameraInstance.cameraInstance != null && !SceneManager.IsPlaying)
            {
                CameraInstance.UpdateCameraAnimation(false);
                forceUpdateCamera = false;
            }


        }

        public static void SetBacTimeScale(float timeScale, bool reset)
        {
            if (reset) BacTimeScale = 1f;
            BacTimeScale *= timeScale;
        }

        public static void Play()
        {
            if (IsPlaying) return;

            if(CurrentSceneState == EditorTabs.Action)
            {
                //Resimulate bac entry (if loaded)
                for (int i = 0; i < Actors.Length; i++)
                {
                    if (Actors[i] != null)
                        Actors[i].bacPlayer.Resume();
                }
            }
            
            IsPlaying = true;
        }

        public static void Pause()
        {
            IsPlaying = false;
        }

        public static void Stop()
        {
            AudioEngine.StopCues();
            AnimatorGizmo.Disbable();

            //Goto first frame of animation
            for (int i = 0; i < Actors.Length; i++)
            {
                if (Actors[i] != null)
                {
                    Actors[i].animationPlayer.FirstFrame();
                    Actors[i].bacPlayer.Stop();
                }
            }

            //Do same for camera
            gameInstance.camera.Stop();

            DestroyAllEntities();

            IsPlaying = false;
        }

        #region SceneControl
        /// <summary>
        /// Plays an animation with default settings.
        /// </summary>
        public static void PlayAnimation(EAN_File eanFile, int eanIndex, int charIndex, bool forceAutoPlay)
        {
            ResetSceneCheck();
            EnsureCharacterIsSet(charIndex);

            Actors[charIndex].animationPlayer.PlayPrimaryAnimation(eanFile, eanIndex, 0, ushort.MaxValue, 1, 0, 0, false, 1f, true);

            if (forceAutoPlay)
                IsPlaying = true;
        }

        public static void PlayBacEntry(BAC_File bacFile, BAC_Entry bacEntry, Move move, int charIndex, bool resetPosition)
        {
            ResetSceneCheck();
            EnsureCharacterIsSet(charIndex);
            ResetState(true);

            if (resetPosition)
                Actors[charIndex].ResetPosition();

            Actors[charIndex].bacPlayer.PlayBacEntry(bacFile, bacEntry, move);
            IsPlaying = AutoPlay;
        }

        public static void PlayCameraAnimation(EAN_Animation animation, BAC_Type10 bacCamEntry, int targetCharaIndex, bool autoTerminate = true)
        {
            gameInstance.camera.PlayCameraAnimation(animation, bacCamEntry, targetCharaIndex, autoTerminate);
        }

        /// <summary>
        /// Plays a camera with default settings, focused on Actor[0].
        /// </summary>
        /// <param name="camera"></param>
        public static void PlayCameraAnimation(EAN_Animation camera)
        {
            ResetSceneCheck();
            EnsureCharacterIsSet(0);
            gameInstance.camera.PlayCameraAnimation(camera, null, 0, false, false);

            if (AutoPlay)
                IsPlaying = true;
        }

        public static void ForceStopBacPlayer()
        {
            foreach (var chara in Actors)
                if (chara != null) chara.bacPlayer.ClearBacEntry();
        }
        
        public static bool IsOnTab(params EditorTabs[] tabs)
        {
            if (tabs == null) return false;
            return tabs.Contains(CurrentSceneState);
        }
        #endregion

        #region CameraControl
        public static void CameraSelectionChanged(EAN_Animation camera)
        {
            if(AutoPlay)
                gameInstance.camera.PlayCameraAnimation(camera, null, 0, false);
        }
        
        public static void CameraChangeCurrentFrame(int frame)
        {
            gameInstance.camera.SkipToFrame(frame);
        }

        /// <summary>
        /// Ensures the camera gets updated this frame, even if it is currently paused.
        /// </summary>
        public static void UpdateCameraAnimation()
        {
            forceUpdateCamera = true;
        }
        #endregion

        public static void RefreshVisualSkeletonVisibility()
        {
            Actors[0]?.visualSkeleton?.RefreshVisibilities();
        }
    }
}
