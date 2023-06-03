using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using XenoKit.Editor;
using XenoKit.Engine.Audio;
using XenoKit.Engine.Gizmo;
using XenoKit.Engine.View;
using Xv2CoreLib.BAC;
using Xv2CoreLib.EAN;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine
{
    public enum MainEditorTabs
    {
        //Must match up with Tab Index!
        Nothing = -1,
        BCS = 0,
        Animation = 1,
        Camera = 2,
        Action = 3, //bac
        State = 4, //bcm
        Projectile = 5,
        Hitbox = 6,
        Effect = 7,
        Audio = 8,
        System = 9
    }

    public enum BcsEditorTabs
    {
        //Must match up with Tab Index!
        Nothing = -1,
        PartSet = 0,
        Colors = 1,
        Bodies = 2,
        Header = 3,
        SkeletonData1 = 4,
        SkeletonData2 = 5,
        Files = 6
    }

    public enum EditorTabs
    {
        //Not related to Tab Index
        Nothing,
        BCS_PartSet,
        BCS_Colors,
        BCS_Bodies,
        BCS_Header,
        BCS_SkeletonData1,
        BCS_SkeletonData2,
        BCS_Files,
        Animation,
        Camera,
        Action,
        State,
        Projectile,
        Hitbox,
        Effect,
        Audio_VOX,
        Audio_SE,
        System
    }

    public static class SceneManager
    {
        public static GameBase MainGameBase => MainGameInstance;
        public static Game MainGameInstance = null;

        public static Camera MainCamera => MainGameInstance != null ? MainGameInstance.camera : null;
        public static AudioEngine AudioEngine { get { return MainGameInstance?.AudioEngine; } }
        public static AnimatorGizmo AnimatorGizmo { get { return MainGameInstance?.GetAnimatorGizmo(); } }
        public static GraphicsDevice GraphicsDeviceRef = null;

        //Matrix
        public static Matrix ViewMatrix { get { return MainGameInstance.camera.ViewMatrix; } }
        public static Matrix ProjectionMatrix { get { return MainGameInstance.camera.ProjectionMatrix; } }

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
        public static Vector4 SystemTime; //Seconds elsapsed while "IsPlaying". For use in DBXV2 Shaders.
        public static Vector4 ScreenSize;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mainTabIdx"></param>
        /// <returns></returns>
        public static bool SetSceneState(int mainTabIdx, int bcsTabIdx, int audioTabIdx)
        {
            EditorTabs prevTab = CurrentSceneState;
            MainEditorTabs mainTab = (MainEditorTabs)mainTabIdx;
            BcsEditorTabs bcsTab = (BcsEditorTabs)bcsTabIdx;

            switch (mainTab)
            {
                case MainEditorTabs.Action:
                    CurrentSceneState = EditorTabs.Action;
                    break;
                case MainEditorTabs.Animation:
                    CurrentSceneState = EditorTabs.Animation;
                    break;
                case MainEditorTabs.Audio:
                    CurrentSceneState = audioTabIdx == 0 ? EditorTabs.Audio_VOX : EditorTabs.Audio_SE;
                    break;
                case MainEditorTabs.BCS:
                    switch (bcsTab)
                    {
                        case BcsEditorTabs.Bodies:
                            CurrentSceneState = EditorTabs.BCS_Bodies;
                            break;
                        case BcsEditorTabs.Colors:
                            CurrentSceneState = EditorTabs.BCS_Colors;
                            break;
                        case BcsEditorTabs.Files:
                            CurrentSceneState = EditorTabs.BCS_Files;
                            break;
                        case BcsEditorTabs.Header:
                            CurrentSceneState = EditorTabs.BCS_Header;
                            break;
                        case BcsEditorTabs.PartSet:
                            CurrentSceneState = EditorTabs.BCS_PartSet;
                            break;
                        case BcsEditorTabs.SkeletonData1:
                            CurrentSceneState = EditorTabs.BCS_SkeletonData1;
                            break;
                        case BcsEditorTabs.SkeletonData2:
                            CurrentSceneState = EditorTabs.BCS_SkeletonData2;
                            break;
                    }
                    break;
                case MainEditorTabs.Camera:
                    CurrentSceneState = EditorTabs.Camera;
                    break;
                case MainEditorTabs.Effect:
                    CurrentSceneState = EditorTabs.Effect;
                    break;
                case MainEditorTabs.Hitbox:
                    CurrentSceneState = EditorTabs.Hitbox;
                    break;
                case MainEditorTabs.Projectile:
                    CurrentSceneState = EditorTabs.Projectile;
                    break;
                case MainEditorTabs.State:
                    CurrentSceneState = EditorTabs.State;
                    break;
                case MainEditorTabs.System:
                    CurrentSceneState = EditorTabs.System;
                    break;
            }

            if (CurrentSceneState == EditorTabs.Action)
            {
                EnsureActorIsSet(0);
            }

            //Needed because for SOME reason the tab changed event gets fired randomly when no change actually occured...
            if (prevTab == CurrentSceneState)
            {
                return false;
            }

            //Changing tabs with an active bac entry will put the simulation in a bac state, best to stop it
            if (prevTab == EditorTabs.Action && IsPlaying)
            {
                Stop();
                IsPlaying = true;
            }

            EditorTabChanged?.Invoke(null, EventArgs.Empty);

            //Return false if state hasn't actually changed
            if (MainGameInstance == null || (PrevSceneState == CurrentSceneState)) return false;

            //State has changed, so update PrevSceneState
            PrevSceneState = prevTab;

            return true;
        }

        private static void ResetSceneCheck()
        {
            //Reset the scene if PrevSceneState isn't equal to CurrentSceneState
            //This is called whenever a bac entry, anim, camera or something else is played, to clean up after whatever was going on beforehand.

            //First, special case for Animation > Camera. Anims shouldn't be reset in this case.
            if (PrevSceneState == EditorTabs.Animation && CurrentSceneState == EditorTabs.Camera)
            {
                ResetState(false);
                PrevSceneState = CurrentSceneState;
            }
            else if ((PrevSceneState != CurrentSceneState && PrevSceneState != EditorTabs.Nothing))
            {
                ResetState(true);
                PrevSceneState = CurrentSceneState;
            }
        }

        private static void ResetState(bool resetAnims)
        {
            MainGameInstance.ResetState(resetAnims);
            BacTimeScale = 1f;
        }
        #endregion

        #region Settings

        public static bool ShowDebugBones = false;
        public static bool ShowWorldAxis = true;
        public static bool ResolveLeftHandSymetry = true; //https://stackoverflow.com/questions/29370361/does-xna-opengl-use-left-handed-matrices //Legacy option. Disabling this will break stuff, as older code will respect it, while newer code wont (auto-resolved is the default now - because why not?).
        /// <summary>
        /// Movement that occurs in a BAC entry is reverted by default when the entry ends. With this setting, that is disabled.
        /// </summary>
        public static bool RetainActionMovement = false;

        public static bool WireframeModeCharacters => SettingsManager.settings.XenoKit_WireframeMode;
        public static bool Loop => SettingsManager.settings.XenoKit_Loop;
        public static bool AutoPlay => SettingsManager.settings.XenoKit_AutoPlay;
        public static bool UseCameras => SettingsManager.settings.XenoKit_EnableCameraAnimations;
        public static bool ShowVisualSkeleton => SettingsManager.settings.XenoKit_EnableVisualSkeleton;

        public static float BacTimeScale = 1f; //TimeScale specified by a TimeScale bacType
        public static float MainAnimTimeScale = 1f;
        #endregion

        #region Actors
        public const int NumActors = 3;
        /// <summary>
        /// All characters active in the current scene. (0 = Primary, 1 = Target, 2 = Partner)
        /// </summary>
        public static Actor[] Actors = new Actor[NumActors];

        public static bool[] ActorsEnable = new bool[3] { true, false, false };


        /// <summary>
        /// Checks if the specified character index is loaded, and if its not then loads a default character.
        /// </summary>
        /// <param name="actorSlot">An index in ActiveCharacters (0 - 2).</param>
        public static void EnsureActorIsSet(int actorSlot = 0)
        {
            if (actorSlot >= Actors.Length) throw new InvalidOperationException($"SceneManager.EnsureActorIsSet: idx {actorSlot} is greater than the maximum amount of Actors.");

            if (Actors[actorSlot] == null)
            {
                var characters = Files.Instance.GetLoadedCharacters();
                Actor chara = characters.FirstOrDefault(x => !Actors.Contains(x));

                if (chara != null)
                {
                    Log.Add($"{GetActorName(actorSlot)} Actor was not set. Defaulting to the first free loaded character ({characters[0].Name}).", LogType.Info);
                    Actors[actorSlot] = characters[0];
                }
                else
                {
                    Log.Add($"{GetActorName(actorSlot)} Actor was not set. Loading the default...", LogType.Info);

                    //Select a character based on idx. This way we dont populate the scene with multiple Gokus.
                    int charId = 0;

                    switch (actorSlot)
                    {
                        case 1: //Target
                            charId = 16;
                            break;
                    }

                    //Load character
                    Actor defaultActor = Files.Instance.LoadCharacter(charId, 0, null, true);
                    SetActor(defaultActor, actorSlot);
                }
            }
        }

        private static void WaitUntilActorIsSet(int actorSlot)
        {
            while (Actors[actorSlot] == null)
            {
                Task.Delay(100);
            }
        }

        public static bool CharacterExists(int index)
        {
            return Actors[index] != null;
        }

        public static int IndexOfCharacter(Actor character, bool allowNull)
        {
            for (int i = 0; i < Actors.Length; i++)
                if (Actors[i] == character) return i;

            if (allowNull)
                return -1;

            Log.Add("SceneManager.IndexOfCharacter: Cannot find character in the active scene.", LogType.Warning);

            return 0;
        }

        public static void SetActor(Actor character, int actorSlot)
        {
            if (Actors[actorSlot] == character)
                return;

            //Unset actor if this character is already used, to prevent duplicate actors.
            for (int i = 0; i < Actors.Length; i++)
            {
                if (Actors[i] == character)
                    Actors[i] = null;
            }

            Actors[actorSlot] = character;
            character.ActorSlot = actorSlot;
            character.ResetPosition();
            Stop();

            //Remove all current bone indices for this actorSlots character models. This is required because the skeletons are different and bone idx wont point to the same thing.
            MainGameBase.CompiledObjectManager.UnsetActorOnModels(actorSlot);

            Log.Add($"{character.Name} set as the {GetActorName(actorSlot)} actor.");

            ActorChanged?.Invoke(character, new ActorChangedEventArgs(character, actorSlot));
        }

        public static void UnsetActor(Actor actor)
        {
            if (actor == null) return;
            int actorSlot = IndexOfCharacter(actor, true);

            if (actorSlot != -1)
            {
                Actors[actorSlot] = null;

                //Remove all current bone indices for this actorSlots character models. This is required because the skeletons are different and bone idx wont point to the same thing.
                MainGameBase.CompiledObjectManager.UnsetActorOnModels(actorSlot);

                Log.Add($"{actor.Name} removed as the {GetActorName(actorSlot)} actor.");

                ActorChanged?.Invoke(null, new ActorChangedEventArgs(null, actorSlot));
            }
        }

        public static void FocusActor(Actor actor)
        {
            MainCamera.CameraState.SetFocus(actor);
            MainCamera.ResetViewerAngles();
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

        public static event EventHandler PlayStateChanged;
        public static event EventHandler CameraCurrentFrameChanged;
        public static event EventHandler AnimationDataChanged;
        public static event EventHandler CameraDataChanged;
        public static event EventHandler BacDataChanged;
        public static event EventHandler SeekOccurred;
        public static event EventHandler EditorTabChanged;
        public static event ActorChangedEventHandler ActorChanged;

        public static void InvokeBacDataChangedEvent()
        {
            BacDataChanged?.Invoke(null, null);
        }

        public static void InvokeCameraCurrentFrameChangedEvent()
        {
            CameraCurrentFrameChanged?.Invoke(null, null);
        }

        public static void InvokeAnimationDataChangedEvent()
        {
            AnimationDataChanged?.Invoke(null, null);
        }

        public static void InvokeCameraDataChangedEvent()
        {
            CameraDataChanged?.Invoke(null, null);
        }

        public static void InvokeSeekOccurredEvent()
        {
            SeekOccurred?.Invoke(null, null);
        }

        #endregion

        #region Update
        public static event EventHandler DelayedUpdate;
        public static event EventHandler SlowUpdate;
        private static int DelayedUpdateTimer = 0;

        private static int SlowUpdateTimer = 0;
        private const int SlowUpdateTimerAmount = 18000; //Every 5 minutes


        public static void Update(GameTime time)
        {
            //Set screen size. This is used by shaders.
            ScreenSize.X = (float)MainGameInstance.ActualWidth;
            ScreenSize.Y = (float)MainGameInstance.ActualHeight;

            //Force update camera this frame
            if (forceUpdateCamera && MainCamera.cameraInstance != null && !SceneManager.IsPlaying)
            {
                MainCamera.UpdateCameraAnimation(false);
                forceUpdateCamera = false;
            }

            if (IsPlaying)
                SystemTime.X += 0.0166f; //Hardcoded timestep (1 second / 60 frames)

            //Code for raising the DelayedUpdate event. This event fires off after a configurable interval and is used by some performance heavy UI operations.
            if (DelayedUpdateTimer >= SettingsManager.Instance.Settings.XenoKit_DelayedUpdateFrameInterval)
            {
                DelayedUpdate?.Invoke(null, EventArgs.Empty);
                DelayedUpdateTimer = 0;
            }
            else
            {
                DelayedUpdateTimer++;
            }

            //A slow update timer for cleaning up dead objects. This needs to be done regularly, but not so often
            if (SlowUpdateTimer >= SlowUpdateTimerAmount)
            {
                SlowUpdate?.Invoke(null, EventArgs.Empty);
                SlowUpdateTimer = 0;
            }
            else
            {
                SlowUpdateTimer++;
            }

        }

        #endregion


        public static void SetBacTimeScale(float timeScale, bool reset)
        {
            if (reset) BacTimeScale = 1f;
            BacTimeScale *= timeScale;
        }

        public static void Play()
        {
            if (IsPlaying) return;

            if (CurrentSceneState == EditorTabs.Action)
            {
                //Resimulate bac entry (if loaded)
                for (int i = 0; i < Actors.Length; i++)
                {
                    if (Actors[i] != null)
                        Actors[i].ActionControl.Resume();
                }
            }
            else if (CurrentSceneState == EditorTabs.Animation)
            {
                for (int i = 0; i < Actors.Length; i++)
                {
                    if (Actors[i] != null)
                        Actors[i].AnimationPlayer.Resume();
                }
            }
            else if (CurrentSceneState == EditorTabs.Camera)
            {
                MainCamera.Resume();
            }

            IsPlaying = true;
            PlayStateChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void Pause()
        {
            IsPlaying = false;
            PlayStateChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void Stop()
        {
            if (MainGameInstance != null)
            {
                MainGameInstance.VfxManager.StopEffects();
                MainGameInstance.AudioEngine.StopCues();
                MainGameInstance.camera.Stop();
                MainGameInstance.DestroyAllEntities();
            }

            //Goto first frame of animation
            for (int i = 0; i < Actors.Length; i++)
            {
                if (Actors[i] != null)
                {
                    Actors[i].AnimationPlayer.FirstFrame();
                    Actors[i].ActionControl.Stop();
                }
            }

            IsPlaying = false;
            PlayStateChanged?.Invoke(null, EventArgs.Empty);
        }

        #region SceneControl
        /// <summary>
        /// Plays an animation with default settings.
        /// </summary>
        public static void PlayAnimation(EAN_File eanFile, int eanIndex, int charIndex, bool forceAutoPlay)
        {
            ResetSceneCheck();
            EnsureActorIsSet(charIndex);

            Actors[charIndex].AnimationPlayer.PlayPrimaryAnimation(eanFile, eanIndex, 0, ushort.MaxValue, 1, 0, 0, false, 1f, true);

            if (forceAutoPlay)
                IsPlaying = true;
        }

        public static void PlayBacEntry(BAC_File bacFile, BAC_Entry bacEntry, Move move, int charIndex, bool resetPosition)
        {
            ResetSceneCheck();
            EnsureActorIsSet(charIndex);
            ResetState(true);

            if (resetPosition && !RetainActionMovement)
                Actors[charIndex].ResetPosition();
            else if (RetainActionMovement)
                Actors[charIndex].MergeTransforms();

            Actors[charIndex].ActionControl.PreviewBacEntry(bacFile, bacEntry, move, Actors[charIndex]);
            IsPlaying = AutoPlay;
        }

        public static void PlayCameraAnimation(EAN_File eanFile, EAN_Animation animation, BAC_Type10 bacCamEntry, int targetCharaIndex, bool autoTerminate = true)
        {
            MainGameInstance.camera.PlayCameraAnimation(eanFile, animation, bacCamEntry, targetCharaIndex, autoTerminate);
        }

        /// <summary>
        /// Plays a camera with default settings, focused on Actor[0].
        /// </summary>
        /// <param name="camera"></param>
        public static void PlayCameraAnimation(EAN_File eanFile, EAN_Animation camera)
        {
            ResetSceneCheck();
            EnsureActorIsSet(0);
            MainGameInstance.camera.PlayCameraAnimation(eanFile, camera, null, 0, false, false);

            if (AutoPlay)
                IsPlaying = true;
        }

        public static void ForceStopBacPlayer()
        {
            foreach (var chara in Actors)
                if (chara != null) chara.ActionControl.ClearBacPlayer();
        }

        public static bool IsOnTab(params EditorTabs[] tabs)
        {
            if (tabs == null) return false;
            return tabs.Contains(CurrentSceneState);
        }
        #endregion

        #region CameraControl
        public static void CameraSelectionChanged(EAN_File eanFile, EAN_Animation camera)
        {
            if (AutoPlay)
                MainGameInstance.camera.PlayCameraAnimation(eanFile, camera, null, 0, false);
        }

        public static void CameraChangeCurrentFrame(int frame)
        {
            MainGameInstance.camera.SkipToFrame(frame);
        }

        /// <summary>
        /// Ensures the camera gets updated this frame, even if it is currently paused.
        /// </summary>
        public static void UpdateCameraAnimation()
        {
            forceUpdateCamera = true;
        }
        #endregion


    }


    public delegate void ActorChangedEventHandler(object source, ActorChangedEventArgs e);

    public class ActorChangedEventArgs : EventArgs
    {
        public Actor Actor { get; private set; }
        public int ActorIndex { get; private set; }

        public ActorChangedEventArgs(Actor actor, int actorIndex)
        {
            Actor = actor;
            ActorIndex = actorIndex;
        }
    }
}
