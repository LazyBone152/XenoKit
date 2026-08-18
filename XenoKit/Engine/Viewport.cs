using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using MonoGame.Framework.WpfInterop.Input;
using Xv2CoreLib.SPM;
using Xv2CoreLib.Resource.App;
using XenoKit.Engine.View;
using XenoKit.Engine.Objects;
using XenoKit.Engine.Gizmo;
using XenoKit.Engine.Audio;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Vfx;
using XenoKit.Engine.Text;
using XenoKit.Windows;
using XenoKit.Editor;
using XenoKit.Engine.Model;
using XenoKit.Engine.Rendering;
using XenoKit.Inspector;
using XenoKit.Engine.Stage;
using XenoKit.Engine.Lighting;
using XenoKit.Engine.Pool;
using XenoKit.Engine.Scripting;
using XenoKit.Engine.Scripting.BSA;
using XenoKit.Engine.Textures;
using Xv2CoreLib.Resource;

namespace XenoKit.Engine
{
    public class Viewport : WpfGame
    {
		private static Viewport _instance = null;
        public static Viewport Instance => _instance;

        #region Events
        /// <summary>
        /// Invoked every frame.
        /// </summary>
        public static event EventHandler UpdateEvent;
        /// <summary>
        /// Invoked periodically after a set amount of frames has passed (user configurable between 10 - 60). This is intended for updating UI at a reduced, but still reasonably fast rate.
        /// </summary>
        public static event EventHandler DelayedEventUpdateEvent;
        /// <summary>
        /// Invoked every few minutes. This is intended for periodic background update events that don't need to run often. 
        /// </summary>
        public static event EventHandler SlowUpdateEvent;

        private static int DelayedUpdateTimer = 0;
        private static int SlowUpdateTimer = 0;
        private const int SlowUpdateTimerAmount = 60 * 60 * 3; //Every few minutes
        #endregion

        #region Fields
        private WpfGraphicsDeviceService _graphicsDeviceManager;
		private WpfKeyboard _keyboard;
		private WpfMouse _mouse;
		private SpriteBatch spriteBatch;
        public bool ViewportIsFocused => (IsActive && IsMouseOver) || IsFullScreen;

        //Scene
        public bool IsPlaying = false;
        public bool RenderCharacters = true;
        public bool WireframeMode = false;
        public bool IsBlackVoid = false;

        //Rendering
        private InfiniteWorldGrid WorldGrid;
        private RenderTargetWrapper MainRenderTarget;
        private RenderTargetWrapper AxisCorrectionRenderTarget;
        private FullscreenWindow FullscreenWindow = null;

        //Stage
        protected bool _isDefaultStageActive = true;
        protected Xv2Stage _defaultStage;
        protected Xv2Stage _setStage;
        public Xv2Stage CurrentStage { get; private set; }

        //Other
        public static Vector4 SystemTime; //Seconds elsapsed while "IsPlaying". For use in DBXV2 Shaders.

        public static Color ViewportBackgroundColor = new Color(20, 20, 20, 255);
        public static Color ScreenshotBackgroundColor = new Color(20, 20, 20, 255);

        private int DelayedTimer = 0;
        protected int HotkeyCooldown = 0;
        private bool isCameraUpdateForced = false;

        public bool DrawThisFrame {  get; private set; }
        #endregion

        #region Properties
        public GameTime GameTime { get; protected set; }
        /// <summary>
        /// The number of frames that has passed since the start of the application.
        /// </summary>
        public ulong Tick { get; private set; }
        public Camera Camera { get; protected set; }
        public AudioEngine AudioEngine { get; protected set; }
		public VfxPreview VfxPreview { get; protected set; }
		public ShaderManager ShaderManager { get; private set; }
        public DirLight LightSource { get; private set; }
        public SunLight SunLight { get; private set; }
        public Input Input { get; private set; }
        public TextRenderer TextRenderer { get; private set; }
        public VfxManager VfxManager { get; private set; }
        public RenderSystem RenderSystem { get; private set; }
        public CompiledObjectManager CompiledObjectManager { get; private set; } = new CompiledObjectManager();
        public ObjectPoolManager ObjectPoolManager { get; private set; }
        public Simulation Simulation { get; private set; }
        public FrameRateCounter FrameRate { get; private set; } = new FrameRateCounter();

        //Gizmos
        public AnimatorGizmo AnimatorGizmo { get; private set; }
        public BoneScaleGizmo BoneScaleGizmo { get; private set; }
        public BacMatrixGizmo BacMatrixGizmo { get; private set; }
        public HitboxGizmo BacHitboxGizmo { get; private set; }
        public ModelGizmo ModelGizmo { get; private set; }
        public EntityTransformGizmo EntityTransformGizmo { get; private set; }

        #endregion

        protected override void Initialize()
        {
            _instance = this;
            // must be initialized. required by Content loading and rendering (will add itself to the Services)
            // note that MonoGame requires this to be initialized in the constructor, while WpfInterop requires it to
            // be called inside Initialize (before base.Initialize())
            _graphicsDeviceManager = new WpfGraphicsDeviceService(this);
            _graphicsDeviceManager.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            spriteBatch = new SpriteBatch(_graphicsDeviceManager.GraphicsDevice);

            // wpf and keyboard need reference to the host control in order to receive input
            // this means every WpfGame control will have it's own keyboard & mouse manager which will only react if the mouse is in the control
            _keyboard = new WpfKeyboard(this);
            _mouse = new WpfMouse(this);

            _defaultStage = Xv2Stage.CreateDefaultStage();
            CurrentStage = _defaultStage;
            Input = new Input();
            LightSource = new DirLight();
            SunLight = new SunLight();
            ObjectPoolManager = new ObjectPoolManager();
            ShaderManager = new ShaderManager();
            Simulation = new Simulation();

            // must be called after the WpfGraphicsDeviceService instance was created
            base.Initialize();

            Xv2Texture.InitDefaultTexture();
            DefaultShaders.InitDefaultShaders();
            ModelInstanceData.InitDefaultInstanceData();

            CollisionMesh.CreateResources(GraphicsDevice);

            //Now initialize objects
            AnimatorGizmo = new AnimatorGizmo();
            BoneScaleGizmo = new  BoneScaleGizmo();
            BacMatrixGizmo = new BacMatrixGizmo();
            BacHitboxGizmo = new HitboxGizmo();
            EntityTransformGizmo = new EntityTransformGizmo();
            ModelGizmo = new ModelGizmo();

            Camera = new Camera();
            AudioEngine = new AudioEngine();
            VfxManager = new VfxManager();
            RenderSystem = new RenderSystem(spriteBatch, true);
            TextRenderer = new TextRenderer();
            VfxPreview = new VfxPreview();

            MainRenderTarget = new RenderTargetWrapper(RenderSystem, 1, SurfaceFormat.Color, true, "MainRenderTarget");
            AxisCorrectionRenderTarget = new RenderTargetWrapper(RenderSystem, 1, SurfaceFormat.Color, true, "AxisCorrectionRenderTarget");
            RenderSystem.RegisterRenderTarget(MainRenderTarget);
            RenderSystem.RegisterRenderTarget(AxisCorrectionRenderTarget);

            //Set viewport background color
            if (LocalSettings.Instance.SerializedBackgroundColor != null)
            {
                Viewport.ViewportBackgroundColor = LocalSettings.Instance.SerializedBackgroundColor.ToColor();
            }

            if (LocalSettings.Instance.CustomScreenshotBackgroundColor != null)
            {
                Viewport.ScreenshotBackgroundColor = LocalSettings.Instance.CustomScreenshotBackgroundColor.ToColor();
            }

            WorldGrid = new InfiniteWorldGrid();
        }

        #region Main Loop
        protected override void Update(GameTime time)
        {
            DrawThisFrame = !(SettingsManager.settings.XenoKit_HalfRenderRate && !MathHelpers.IsEven(Tick));
            IsBlackVoid = false;
            GameTime = time;
            Input.Update(_mouse, _keyboard);
            Camera.EarlyUpdate();
            CheckHotkeys();
            CurrentGizmo.Update();

            SunLight.Update();
            LightSource.Update();
            CurrentStage?.Update();

            FrameRate.Update((float)time.ElapsedGameTime.TotalSeconds);
            ShaderManager.Update();
            BacHitboxGizmo.Update();

            AudioEngine.Update();

            if (SceneManager.IsOnInspectorTab)
            {
                InspectorMode.Instance.Update();
            }
            else
            {
                if (SceneManager.IsOnEffectTab)
                {
                    VfxPreview.Update();
                }
                else
                {
                    if (SceneManager.IsOnTab(EditorTabs.Projectile))
                        BsaEffectPreviewController.Instance.Update();

                    VfxManager.Update();
                }

                //Actors
                for (int i = 0; i < SceneManager.Actors.Length; i++)
                {
                    //Skip updating the victim actor if it is disabled. This will also cause it to not be rendered as well.
                    if (i == 1 && !SceneManager.VictimEnabled) continue;

                    if (SceneManager.ActorsEnable[i] && SceneManager.Actors[i] != null)
                    {
                        SceneManager.Actors[i].Update();
                    }
                }

                Simulation.Update();
            }

            RenderSystem.Update();

            //Update camera last - this way it has the lowest priority for mouse click events
            Camera.Update(time);

            UpdateEvent?.Invoke(this, null);

            //Force update camera this frame
            if (isCameraUpdateForced && Camera.cameraInstance != null && !IsPlaying)
            {
                Camera.UpdateCameraAnimation(false);
                isCameraUpdateForced = false;
            }

            if (IsPlaying)
                SystemTime.X += 0.0166f; //Hardcoded timestep (1 second / 60 frames)

            if (Files.Instance.SelectedItem != null)
            {
                Files.Instance.SelectedItem.Update();
            }

            //Handle delayed/slow updates:
            //DelayedUpdate (every 60 frames)
            if (DelayedTimer >= 60)
            {
                DelayedTimer = 0;
                DelayedUpdate();
            }
            else
            {
                DelayedTimer++;
            }

            //SlowUpdate (every 5 minutes)
            if (SlowUpdateTimer >= SlowUpdateTimerAmount)
            {
                SlowUpdateTimer = 0;
                SlowUpdateEvent?.Invoke(this, EventArgs.Empty);
                SlowUpdate();
            }
            else
            {
                SlowUpdateTimer++;
            }

            //DelayedEventUpdate (user configurable between 10 - 60 frames)
            //Similar name, but different from DelayedUpdate. This is mainly used for updating UI elements after a short delay, and is event-only
            if (DelayedUpdateTimer >= SettingsManager.Instance.Settings.XenoKit_DelayedUpdateFrameInterval)
            {
                DelayedUpdateTimer = 0;
                DelayedEventUpdateEvent?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                DelayedUpdateTimer++;
            }

            Tick++;
        }

        protected void DelayedUpdate()
        {
            ObjectPoolManager.DelayedUpdate();

            ShaderManager.DelayedUpdate();
            RenderSystem.DelayedUpdate();

            for (int i = 0; i < SceneManager.Actors.Length; i++)
            {
                if (SceneManager.ActorsEnable[i] && SceneManager.Actors[i] != null)
                    SceneManager.Actors[i].DelayedUpdate();
            }

            Camera.DelayedUpdate();

            //Exit fullscreen state if the window is closed by some other means
            if(FullscreenWindow != null && IsFullScreen && !FullscreenWindow.IsActive)
            {
                DisableFullscreen();
            }
        }

        protected void SlowUpdate()
        {
            CompiledObjectManager.SlowUpdate();
            RenderSystem.SlowUpdate();
        }

        protected override void Draw(GameTime time)
        {
            if (!DrawThisFrame) return;

            ShaderManager.SetAllGlobalSamplers();

            //RenderSystem goes first
            RenderSystem.Draw();

            //Next, render everything else (gizmos, grid)
            GraphicsDevice.SetRenderTarget(MainRenderTarget.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            GraphicsDevice.SetDepthBuffer(RenderSystem.DepthBuffer.RenderTarget);

            //ShaderManager.Instance.SetAllGlobalSamplers();

            if (SceneManager.IsOnEffectTab)
            {
                VfxPreview.Draw();
            }
            else
            {
                VfxManager.Draw();
            }

            if (SceneManager.IsOnTab(EditorTabs.Projectile))
                BsaEffectPreviewController.Instance.Draw();

            if (RenderSystem.HasPendingScreenshot())
            {
                RenderSystem.ProcessPendingScreenshot(RenderSystem.GetFinalRenderTarget(), MainRenderTarget.RenderTarget);
            }

            //Draw MainRenderTarget onto screen
            GraphicsDevice.SetRenderTarget(AxisCorrectionRenderTarget.RenderTarget);
            GraphicsDevice.Clear(IsBlackVoid || !_isDefaultStageActive ? Color.Black : Viewport.ViewportBackgroundColor);
            GraphicsDevice.SetDepthBuffer(RenderSystem.DepthBuffer.RenderTarget);

            //Merge RTs
            RenderSystem.DisplayRenderTarget(RenderSystem.GetFinalRenderTarget());
            RenderSystem.DisplayRenderTarget(MainRenderTarget.RenderTarget);

            //Draw last and over everything else
            CurrentGizmo.Draw();
            BacHitboxGizmo.Draw();
            WorldGrid.Draw();
            TextRenderer.Draw();

            //Now apply axis correction
            GraphicsDevice.SetRenderTarget(MainRenderTarget.RenderTarget);
            RenderSystem.SetTextures(AxisCorrectionRenderTarget.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            RenderSystem.YBS.ApplyAxisCorrection();

            //Merge TextRenderer with the rendered scene
            if (TextRenderer.TextDrawThisFrame > 0)
            {
                RenderSystem.DisplayRenderTarget(TextRenderer.GetRenderTarget());
            }

            RenderSystem.CreateSmallScene(MainRenderTarget.RenderTarget);

            //Present on screen
            GraphicsDevice.SetRenderTarget(InternalRenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            RenderSystem.DisplayRenderTarget(MainRenderTarget.RenderTarget, true);
            //RenderSystem.DisplayRenderTarget(RenderSystem.GetReflectionRT().RenderTarget, true);

        }

        #endregion

        #region Helpers

        public void ResetState(bool resetAnims = true, bool resetCamPos = false)
        {
            if (resetAnims)
                Camera.ClearCameraAnimation();

            if (resetCamPos)
                Camera.ResetCamera();

            for (int i = 0; i < SceneManager.Actors.Length; i++)
            {
                if (SceneManager.Actors[i] != null)
                    SceneManager.Actors[i].ResetState(resetAnims);
            }

            VfxManager.StopEffects();
        }

        public AnimatorGizmo GetAnimatorGizmo()
        {
            return AnimatorGizmo;
        }

        public BoneScaleGizmo GetBoneScaleGizmo()
        {
            return BoneScaleGizmo;
        }

        public BacMatrixGizmo GetBacMatrixGizmo()
        {
            return BacMatrixGizmo;
        }

        public void EnableFullscreen()
        {
            IsFullScreen = true;
            _graphicsDeviceManager.IsFullScreen = true;

            try
            {
                FullscreenWindow = new FullscreenWindow();
                FullscreenWindow.Show();
                alternateInputElement = FullscreenWindow;
                _mouse.SetAlternateFocusElement(FullscreenWindow);
                RenderSystem.RecreateRenderTargetsNextFrames = 2;
            }
            catch
            {
                //Dont really need to do anything here. Just want to prevent crashing when opening fullscreen mode (rare)
            }
        }

        public void DisableFullscreen()
        {
            if (FullscreenWindow != null)
            {
                FullscreenWindow.Close();
                FullscreenWindow = null;
                alternateInputElement = null;
            }

            IsFullScreen = false;
            _graphicsDeviceManager.IsFullScreen = false;
            _mouse.SetAlternateFocusElement(null);
            RenderSystem.RecreateRenderTargetsNextFrames = 2;
        }

        protected void CheckHotkeys()
        {
            if (HotkeyCooldown == 0)
            {
                if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) && Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.L))
                {
                    WireframeMode = !WireframeMode;
                    CompiledObjectManager.ForceShaderUpdate();
                    Input.ExclusiveKeyDown(Microsoft.Xna.Framework.Input.Keys.L);
                    SetHotkeyCooldown();
                }
                else if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) && Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.G))
                {
                    SceneManager.ShowWorldAxis = !SceneManager.ShowWorldAxis;
                    Input.ExclusiveKeyDown(Microsoft.Xna.Framework.Input.Keys.G);
                    SetHotkeyCooldown();
                }
                else if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Space))
                {
                    IsPlaying = !IsPlaying;
                    HotkeyCooldown = 10;
                }
                else if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) && Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.O))
                {
                    SettingsManager.settings.XenoKit_UseOutlinePostEffect = !SettingsManager.settings.XenoKit_UseOutlinePostEffect;
                    Input.ExclusiveKeyDown(Microsoft.Xna.Framework.Input.Keys.O);
                    SetHotkeyCooldown();
                }
                else if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) && Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F))
                {
                    LocalSettings.Instance.EnableFog = !LocalSettings.Instance.EnableFog;
                    Input.ExclusiveKeyDown(Microsoft.Xna.Framework.Input.Keys.F);
                    SetHotkeyCooldown();
                }
                else if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F11) || (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape) && IsFullScreen))
                {
                    SetHotkeyCooldown();
                    Input.ExclusiveKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape);
                    if (IsFullScreen)
                    {
                        DisableFullscreen();
                    }
                    else
                    {
                        EnableFullscreen();
                    }
                }
                else if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F12) && Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl))
                {
                    SetHotkeyCooldown();
                    RenderSystem.RequestScreenshot(ScreenshotType.TransparentBackground);
                }
                else if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F12))
                {
                    SetHotkeyCooldown();
                    RenderSystem.RequestScreenshot(ScreenshotType.CustomBackgroundColor);
                }
#if DEBUG
                else if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.N) && Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl))
                {
                    //Toggle frustum culling
                    SetHotkeyCooldown();
                    SceneManager.FrustumCullEnabled = !SceneManager.FrustumCullEnabled;
                    Log.Add(SceneManager.FrustumCullEnabled ? "Frustum Culling enabled" : "Frustum Culling disabled");
                }
                else if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.M) && Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl))
                {
                    //Toggle frustum update
                    SetHotkeyCooldown();
                    SceneManager.FrustumUpdateEnabled = !SceneManager.FrustumUpdateEnabled;
                    Log.Add(SceneManager.FrustumUpdateEnabled ? "Frustum Update enabled" : "Frustum Update disabled");
                }
                else if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.B) && Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl))
                {
                    SetHotkeyCooldown();
                    SceneManager.BoundingBoxVisible = !SceneManager.BoundingBoxVisible;
                }
                else if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.P) && Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl))
                {
                    SetHotkeyCooldown();
                    SceneManager.StageGeometryVisible = !SceneManager.StageGeometryVisible;
                }
                else if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.O) && Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl))
                {
                    SetHotkeyCooldown();
                    SceneManager.CollisionMeshVisible = !SceneManager.CollisionMeshVisible;
                }

#endif
            }
            else
            {
                HotkeyCooldown -= 1;
            }
        }

        protected void SetHotkeyCooldown()
        {
            HotkeyCooldown = 20;
        }

        public void SetDefaultSpm(SPM_File spmFile)
        {
            _defaultStage?.SetSpmFile(spmFile);
        }

        public void SetActiveStage(Xv2Stage stage)
        {
            if (CurrentStage != null)
            {
                RenderSystem.RemoveRenderEntity(CurrentStage);
                RenderSystem.RemoveReflectionRenderEntity(CurrentStage);
                CurrentStage.UnsetActiveStage();
            }

            if (stage != null)
            {
                RenderSystem.AddRenderEntity(stage);
                RenderSystem.AddReflectionRenderEntity(stage);
            }

            _isDefaultStageActive = stage == null;
            CurrentStage = stage != null ? stage : _defaultStage;
            CurrentStage.SetActiveStage();
        }

        public void ForceCameraUpdate()
        {
            isCameraUpdateForced = true;
        }
        #endregion

        public void SeekPrevFrame()
        {
            if (SceneManager.IsOnEffectTab)
            {
                VfxPreview.SeekPrev();
                return;
            }

            switch (SceneManager.CurrentSceneState)
            {
                case EditorTabs.Inspector:
                case EditorTabs.InspectorAnimation:
                    InspectorMode.Instance.ActiveSkinnedEntity?.AnimationPlayer?.PrevFrame();
                    break;
                case EditorTabs.Animation:
                case EditorTabs.FPF:
                    if (SceneManager.Actors[0] != null)
                        SceneManager.Actors[0].AnimationPlayer.PrevFrame();
                    break;
                case EditorTabs.Camera:
                    Camera.PrevFrame();
                    break;
                case EditorTabs.Action:
                    if (SceneManager.Actors[0] != null)
                        SceneManager.Actors[0].ActionControl.SeekPrevFrame();
                    break;
                case EditorTabs.Projectile:
                    BsaEffectPreviewController.Instance.SeekPrevFrame();
                    break;
            }
        }

        public void SeekNextFrame()
        {
            if (SceneManager.IsOnEffectTab)
            {
                VfxPreview.SeekNext();
                return;
            }

            switch (SceneManager.CurrentSceneState)
            {
                case EditorTabs.Inspector:
                case EditorTabs.InspectorAnimation:
                    InspectorMode.Instance.ActiveSkinnedEntity?.AnimationPlayer?.NextFrame();
                    break;
                case EditorTabs.Animation:
                case EditorTabs.FPF:
                    if (SceneManager.Actors[0] != null)
                        SceneManager.Actors[0].AnimationPlayer.NextFrame();
                    break;
                case EditorTabs.Camera:
                    Camera.NextFrame();
                    break;
                case EditorTabs.Action:
                    if (SceneManager.Actors[0] != null)
                    {
                        SceneManager.Actors[0].ActionControl.SeekNextFrame();
                    }
                    break;
                case EditorTabs.Projectile:
                    BsaEffectPreviewController.Instance.SeekNextFrame();
                    break;
            }
        }

    }
}
