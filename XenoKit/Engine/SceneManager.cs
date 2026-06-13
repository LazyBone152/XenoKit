using Microsoft.Xna.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using XenoKit.Editor;
using XenoKit.Engine.Model;
using XenoKit.Engine.Scripting.BSA;
using Xv2CoreLib.BAC;
using Xv2CoreLib.EAN;
using Xv2CoreLib.Resource.App;
using Xv2CoreLib.SPM;

namespace XenoKit.Engine
{
    public enum MainEditorTabs
    {
        //Must match up with Tab Index!
        Nothing = -1,
        BCS = 0,
        Animation = 1,
        Camera = 2,
        FPF = 3,
        Action = 4, //bac
        State = 5, //bcm
        Projectile = 6,
        Effect = 7,
        Audio = 8,
        System = 9,
        CAC = 10,
        Inspector = 11,
        InspectorAnimation = 12,
        Stage = 13,
        DynamicTab
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
        Effect,
        Effect_PBIND,
        Effect_TBIND,
        Effect_CBIND,
        Effect_LIGHT,
        Effect_EMO,
        Audio_VOX,
        Audio_SE,
        System,
        CAC,
        Inspector,
        InspectorAnimation,
        FPF
    }

    public enum DynamicTabs
    {
        None,
        ModelScene
    }

    public static class SceneManager
    {
        #region SceneState
        public static EditorTabs PrevSceneState = EditorTabs.Nothing;
        public static EditorTabs CurrentSceneState = 0;
        public static DynamicTabs CurrentDynamicTab = DynamicTabs.None;
        public static bool IsOnEffectTab = false;
        public static bool IsOnInspectorTab = false;
        public static bool UseRenderScene = false;
        public static string DebugTestValue = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mainTabIdx"></param>
        /// <returns></returns>
        public static async Task<bool> SetSceneState(int mainTabIdx, int bcsTabIdx, int audioTabIdx, int effectTabIdx)
        {
            EditorTabs prevTab = CurrentSceneState;
            MainEditorTabs mainTab = (MainEditorTabs)mainTabIdx;
            BcsEditorTabs bcsTab = (BcsEditorTabs)bcsTabIdx;

            IsOnEffectTab = false;
            IsOnInspectorTab = false;
            CurrentDynamicTab = DynamicTabs.None;
            UseRenderScene = false;
            Viewport.Instance?.RenderSystem.SetRenderScene(null);

            //Set default actor values
            ActorsEnable[0] = true;
            ActorsEnable[1] = false;
            ActorsEnable[2] = false;

            if(mainTabIdx >= (int)MainEditorTabs.DynamicTab)
            {
                ActorsEnable[0] = false;

                //On a dynamic tab
                DynamicTab dynamicTab = TabManager.GetSelectedDynamicTab();

                if(dynamicTab != null)
                {
                    if(dynamicTab.Context is ModelScene modelScene)
                    {
                        CurrentDynamicTab = DynamicTabs.ModelScene;
                        UseRenderScene = true;
                        Viewport.Instance.RenderSystem.SetRenderScene(modelScene);
                    }
                }
                else
                {
                    Log.Add("Could not find the DynamicTab", LogType.Warning);
                }
            }
            else
            {
                switch (mainTab)
                {
                    case MainEditorTabs.Action:
                        CurrentSceneState = EditorTabs.Action;
                        ActorsEnable[1] = true;
                        break;
                    case MainEditorTabs.Animation:
                        CurrentSceneState = EditorTabs.Animation;
                        break;
                    case MainEditorTabs.Audio:
                        CurrentSceneState = audioTabIdx > 0 ? EditorTabs.Audio_VOX : EditorTabs.Audio_SE;
                        ActorsEnable[0] = false;
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
                    case MainEditorTabs.FPF:
                        CurrentSceneState = EditorTabs.FPF;
                        break;
                    case MainEditorTabs.Effect:
                        IsOnEffectTab = true;

                        switch (effectTabIdx)
                        {
                            case 0:
                                CurrentSceneState = EditorTabs.Effect;
                                break;
                            case 1:
                                CurrentSceneState = EditorTabs.Effect_PBIND;
                                ActorsEnable[0] = false;
                                break;
                            case 2:
                                CurrentSceneState = EditorTabs.Effect_TBIND;
                                ActorsEnable[0] = false;
                                break;
                            case 3:
                                CurrentSceneState = EditorTabs.Effect_CBIND;
                                break;
                            case 4:
                                CurrentSceneState = EditorTabs.Effect_EMO;
                                ActorsEnable[0] = false;
                                break;
                            case 5:
                                CurrentSceneState = EditorTabs.Effect_LIGHT;
                                break;
                        }
                        break;
                    case MainEditorTabs.Projectile:
                        CurrentSceneState = EditorTabs.Projectile;
                        ActorsEnable[0] = false;
                        break;
                    case MainEditorTabs.State:
                        CurrentSceneState = EditorTabs.State;
                        break;
                    case MainEditorTabs.System:
                        CurrentSceneState = EditorTabs.System;
                        break;
                    case MainEditorTabs.CAC:
                        CurrentSceneState = EditorTabs.CAC;
                        break;
                    case MainEditorTabs.Inspector:
                        CurrentSceneState = EditorTabs.Inspector;
                        IsOnInspectorTab = true;
                        break;
                    case MainEditorTabs.InspectorAnimation:
                        CurrentSceneState = EditorTabs.InspectorAnimation;
                        IsOnInspectorTab = true;
                        break;
                }

            }

            if (CurrentSceneState == EditorTabs.Action || CurrentSceneState == EditorTabs.Projectile)
            {
                await AsyncEnsureActorIsSet(0);

                if (CurrentSceneState == EditorTabs.Projectile && Actors[0] != null)
                    Actors[0].IsVisible = false;
            }
            else if (Actors[0] != null)
            {
                Actors[0].IsVisible = true;
            }

            //Needed because for SOME reason the tab changed event gets fired randomly when no change actually occured...
            if (prevTab == CurrentSceneState && CurrentDynamicTab == DynamicTabs.None)
            {
                return false;
            }

            //Changing tabs with an active bac entry will put the simulation in a bad state, best to stop it
            if (prevTab == EditorTabs.Action && Viewport.Instance.IsPlaying)
            {
                Stop();
                Viewport.Instance.IsPlaying = true;
            }

            EditorTabChanged?.Invoke(null, EventArgs.Empty);

            //Return false if state hasn't actually changed
            if (Viewport.Instance == null || (PrevSceneState == CurrentSceneState)) return false;

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
            Viewport.Instance.ResetState(resetAnims);
        }
        #endregion

        #region Settings

        public static bool ShowDebugBones = false;
        public static bool ShowWorldAxis = true;
        /// <summary>
        /// Movement that occurs in a BAC entry is reverted by default when the entry ends. With this setting, that is disabled.
        /// </summary>
        public static bool RetainActionMovement = false;
        public static bool ShowModelEditorHighlights = true;

        //Frustum/Culling
        public static bool FrustumUpdateEnabled = true;
        public static bool FrustumCullEnabled = true;
        public static bool BoundingBoxVisible = false;

        //Stage
        public static bool StageGeometryVisible = true;
        public static bool CollisionMeshVisible = false;

        public static bool Loop => SettingsManager.settings.XenoKit_Loop;
        public static bool AutoPlay => SettingsManager.settings.XenoKit_AutoPlay;
        public static bool UseCameras => SettingsManager.settings.XenoKit_EnableCameraAnimations;
        public static bool ShowVisualSkeleton => SettingsManager.settings.XenoKit_EnableVisualSkeleton;
        public static float BattleDamageScratches = 0f;
        public static float BattleDamageBlood = 0f;

        //Simulation Parameters
        private static bool _victimIsFacingPrimary = true;
        private static float _victimDistance = 2f;
        public static bool VictimEnabled { get; set; } = false;
        public static float VictimDistance
        {
            get => _victimDistance;
            set
            {
                if (_victimDistance != value)
                {
                    _victimDistance = MathHelper.Clamp(value, -15f, 15f);
                    Actors[1]?.ResetPosition();
                }
            }
        }
        public static bool VictimIsFacingPrimary
        {
            get => _victimIsFacingPrimary;
            set
            {
                if (_victimIsFacingPrimary != value)
                {
                    _victimIsFacingPrimary = value;
                    Actors[1]?.ResetPosition();
                }
            }
        }
        public static bool VictimIsGuarding { get; set; }

        public static bool AllowBacLoop { get; set; }

        public static PivotPoint PivotPoint = PivotPoint.Center;
        public static ViewportSelectionMode ViewportSelectionMode = ViewportSelectionMode.Model;

        #endregion

        #region Actors
        public const int NumActors = 3;
        /// <summary>
        /// All characters active in the current scene. (0 = Primary, 1 = Victim, 2 = Partner)
        /// </summary>
        public readonly static Actor[] Actors = new Actor[NumActors];

        public readonly static bool[] ActorsEnable = new bool[3] { true, true, false };
        private readonly static bool[] ActorIsLoading = new bool[NumActors];


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
                    SetActor(characters[0], actorSlot);
                }
                else
                {
                    Log.Add($"{GetActorName(actorSlot)} Actor was not set. Loading the default...", LogType.Info);

                    //Select a character based on idx. This way we dont populate the scene with multiple Gokus.
                    int charId = 0;

                    switch (actorSlot)
                    {
                        case 1: //Victim
                            charId = 16;
                            break;
                    }

                    try
                    {
                        //Load character
                        Actor defaultActor = Files.Instance.LoadCharacter(charId, 0, null, true);
                        SetActor(defaultActor, actorSlot);
                    }
                    catch (Exception ex)
                    {
                        Log.Add("Actor Set Error: " + ex.Message, ex.ToString(), LogType.Error);
                    }
                }
            }
        }

        public static async Task AsyncEnsureActorIsSet(int actorSlot = 0)
        {
            if (actorSlot >= Actors.Length) throw new InvalidOperationException($"SceneManager.AsyncEnsureActorIsSet: idx {actorSlot} is greater than the maximum amount of Actors.");
            
            if (Actors[actorSlot] == null)
            {
                if (ActorIsLoading[actorSlot]) return;
                ActorIsLoading[actorSlot] = true;

                var characters = Files.Instance.GetLoadedCharacters();
                Actor chara = characters.FirstOrDefault(x => !Actors.Contains(x));

                if (chara != null)
                {
                    SetActor(characters[0], actorSlot);
                }
                else
                {
                    //Select a character based on idx. This way we dont populate the scene with multiple Gokus.
                    int charId = 0;

                    switch (actorSlot)
                    {
                        case 1: //Victim
                            charId = 16;
                            break;
                    }

                    try
                    {
                        //Load character
                        Actor defaultActor = await Files.Instance.AsyncLoadCharacter(charId, 0, true);
                        SetActor(defaultActor, actorSlot);
                    }
                    catch (Exception ex)
                    {
                        Log.Add("Actor Set Error: " + ex.Message, ex.ToString(), LogType.Error);
                    }
                }
            }

            ActorIsLoading[actorSlot] = false;
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
                {
                    if(i == 0 && CurrentSceneState == EditorTabs.Action)
                    {
                        Log.Add("Cannot change this actor while on the Action tab.", LogType.Error);
                        return;
                    }

                    Actors[i] = null;
                }
            }

            //Remove previous actor from RenderDepthSystem
            if (Actors[actorSlot] != null)
                Viewport.Instance.RenderSystem.RemoveRenderEntity(Actors[actorSlot]);

            //Set new actor there
            Viewport.Instance.RenderSystem.AddRenderEntity(character);

            Actors[actorSlot] = character;
            character.ActorSlot = actorSlot;
            character.ResetPosition();
            Stop();

            Log.Add($"{character.Name} set as the {GetActorName(actorSlot)} actor.");

            if (actorSlot == 1)
            {
                VictimEnabled = true;
            }

            ActorChanged?.Invoke(character, new ActorChangedEventArgs(character, actorSlot));
        }

        public static int UnsetActor(Actor actor)
        {
            if (actor == null) return -1;
            int actorSlot = IndexOfCharacter(actor, true);

            if (actorSlot != -1)
            {
                Actors[actorSlot] = null;

                //Remove actor from RenderDepthSystem
                Viewport.Instance.RenderSystem.RemoveRenderEntity(actor);

                Log.Add($"{actor.Name} removed as the {GetActorName(actorSlot)} actor.");

                if(actorSlot == 1)
                {
                    VictimEnabled = false;
                }

                ActorChanged?.Invoke(null, new ActorChangedEventArgs(null, actorSlot));
            }

            return actorSlot;
        }

        public static void FocusActor(Actor actor)
        {
            Viewport.Instance.Camera.CameraState.SetFocus(actor);
        }

        private static string GetActorName(int charaIdx)
        {
            switch (charaIdx)
            {
                case 0:
                    return "Primary";
                case 1:
                    return "Victim";
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

        public static void Play()
        {
            if (Viewport.Instance.IsPlaying) return;

            if (CurrentSceneState == EditorTabs.Action)
            {
                //Resimulate bac entry (if loaded)
                if (Actors[0] != null)
                {
                    Actors[0].ActionControl.Resume();
                }
            }
            else if (CurrentSceneState == EditorTabs.Animation || CurrentSceneState == EditorTabs.FPF)
            {
                for (int i = 0; i < Actors.Length; i++)
                {
                    if (Actors[i] != null)
                        Actors[i].AnimationPlayer.Resume();
                }
            }
            else if (IsOnInspectorTab)
            {
                Inspector.InspectorMode.Instance.ActiveSkinnedEntity?.AnimationPlayer?.Resume();
            }
            else if (CurrentSceneState == EditorTabs.Camera)
            {
                Viewport.Instance.Camera.Resume();
            }

            Viewport.Instance.IsPlaying = true;
            PlayStateChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void Pause()
        {
            Viewport.Instance.IsPlaying = false;
            PlayStateChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void Stop()
        {
            if (Viewport.Instance != null)
            {
                Viewport.Instance.AudioEngine.StopCues();
                Viewport.Instance.Camera.Stop();

                if (IsOnTab(EditorTabs.Effect))
                {
                    Viewport.Instance.VfxPreview.Stop();
                }
                else
                {
                    Viewport.Instance.VfxManager.StopEffects();
                }

                BsaEffectPreviewController.Instance.Stop();

            }

            if (Actors[0] != null)
            {
                Actors[0].AnimationPlayer.FirstFrame();
                Actors[0].ActionControl.Stop();
                Actors[0].ShaderParameters.ShaderPath = Shader.ActorShaderPath.Default;
            }

            if (Actors[1] != null)
            {
                Actors[1].ResetState();
            }

            Inspector.InspectorMode.Instance.ActiveSkinnedEntity?.AnimationPlayer?.FirstFrame();

            Viewport.Instance.IsPlaying = false;
            PlayStateChanged?.Invoke(null, EventArgs.Empty);
        }

        #region SceneControl
        /// <summary>
        /// Plays an animation with default settings.
        /// </summary>
        public static async void PlayAnimation(EAN_File eanFile, int eanIndex, int charIndex, bool forceAutoPlay)
        {
            ResetSceneCheck();
            await AsyncEnsureActorIsSet(charIndex);

            Actors[charIndex].AnimationPlayer.PlayPrimaryAnimation(eanFile, eanIndex, 0, ushort.MaxValue, 1, 0, 0, false, 1f, true);

            if (forceAutoPlay)
                Viewport.Instance.IsPlaying = true;
        }

        public static async void PlayBacEntry(BAC_File bacFile, BAC_Entry bacEntry, Move move, int charIndex, bool resetPosition)
        {
            ResetSceneCheck();
            await AsyncEnsureActorIsSet(charIndex);
            ResetState(true);

            if (resetPosition && !RetainActionMovement)
                Actors[charIndex].ResetPosition();
            else if (RetainActionMovement)
                Actors[charIndex].MergeTransforms();

            Actors[charIndex].ActionControl.PreviewBacEntry(bacFile, bacEntry, move, Actors[charIndex]);
            Viewport.Instance.IsPlaying = AutoPlay;
        }

        public static void PlayCameraAnimation(EAN_File eanFile, EAN_Animation animation, BAC_Type10 bacCamEntry, Actor actor, int targetCharaIndex, bool autoTerminate = true)
        {
            Viewport.Instance.Camera.PlayCameraAnimation(eanFile, animation, bacCamEntry, actor, targetCharaIndex, autoTerminate);
        }

        /// <summary>
        /// Plays a camera with default settings, focused on Actor[0].
        /// </summary>
        /// <param name="camera"></param>
        public static async void PlayCameraAnimation(EAN_File eanFile, EAN_Animation camera)
        {
            ResetSceneCheck();
            await AsyncEnsureActorIsSet(0);
            Viewport.Instance.Camera.PlayCameraAnimation(eanFile, camera, null, Actors[0], 0, false, false);

            if (AutoPlay)
                Viewport.Instance.IsPlaying = true;
        }

        public static void ForceStopBacPlayer()
        {
            foreach (var chara in Actors)
                if (chara != null) chara.ActionControl.ClearBacPlayer();
        }

        public static bool IsOnTab(params EditorTabs[] tabs)
        {
            if (tabs == null || CurrentDynamicTab != DynamicTabs.None) return false;
            return tabs.Contains(CurrentSceneState);
        }
        #endregion

        #region CameraControl
        public static void CameraSelectionChanged(EAN_File eanFile, EAN_Animation camera)
        {
            if (AutoPlay)
                Viewport.Instance.Camera.PlayCameraAnimation(eanFile, camera, null, Actors[0], 0, false);
        }

        public static void CameraChangeCurrentFrame(int frame)
        {
            Viewport.Instance.Camera.SkipToFrame(frame);
        }

        #endregion

        public static void SetDefaultSpm(SPM_File spmFile)
        {
            Viewport.Instance?.SetDefaultSpm(spmFile);
        }

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
