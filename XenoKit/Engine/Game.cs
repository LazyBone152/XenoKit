using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.View;
using XenoKit.Engine.Objects;
using XenoKit.Engine.Gizmo;
using XenoKit.Engine.Audio;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Vfx;
using XenoKit.Engine.Text;
using XenoKit.Editor;
using XenoKit.Engine.Model;
using XenoKit.Engine.Rendering;
using XenoKit.Inspector;
using XenoKit.Windows;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine
{
    public class Game : GameBase
    {
        public override ICameraBase ActiveCameraBase { get => camera; }

        //Engine Features:
        public Camera camera;
        public AudioEngine AudioEngine;
        public VfxPreview VfxPreview;
        public FrameRateCounter FrameRate { get; private set; } = new FrameRateCounter();

        //Engine Values:
        public override bool IsMainInstance => true;
        public override int SuperSamplingFactor => IsFullScreen ? 1 : SettingsManager.settings.XenoKit_SuperSamplingFactor;

        //Gizmos
        public GizmoBase CurrentGizmo = null;
        public AnimatorGizmo AnimatorGizmo;
        public BoneScaleGizmo BoneScaleGizmo;
        public BacMatrixGizmo BacMatrixGizmo;
        public HitboxGizmo BacHitboxGizmo;
        public EntityTransformGizmo EntityTransformGizmo;
        private InfiniteWorldGrid WorldGrid;


        //Stage. Not final design just a quick hacky way to get stages into the application.
        public ManualFiles ActiveStage = null;

        private RenderTargetWrapper MainRenderTarget;
        private RenderTargetWrapper AxisCorrectionRenderTarget;

        //Fullscreen
        private FullscreenWindow fullscreenWindow = null;

        protected override void Initialize()
        {
            //Initalize all base elements first
            base.Initialize();

            //Set game instace in SceneManager - this is required for most objects to function correctly
            SceneManager.MainGameInstance = this;

            //Now initialize objects
            AnimatorGizmo = new AnimatorGizmo(this);
            BoneScaleGizmo = new  BoneScaleGizmo(this);
            BacMatrixGizmo = new BacMatrixGizmo(this);
            BacHitboxGizmo = new HitboxGizmo(this);
            EntityTransformGizmo = new EntityTransformGizmo(this);
            CurrentGizmo = AnimatorGizmo;

            camera = new Camera(this);
            AudioEngine = new AudioEngine();
            VfxManager = new VfxManager(this);
            RenderSystem = new RenderSystem(this, true);
            VfxPreview = new VfxPreview(this);

            MainRenderTarget = new RenderTargetWrapper(RenderSystem, 1, SurfaceFormat.Color, true, "MainRenderTarget");
            AxisCorrectionRenderTarget = new RenderTargetWrapper(RenderSystem, 1, SurfaceFormat.Color, true, "AxisCorrectionRenderTarget");
            RenderSystem.RegisterRenderTarget(MainRenderTarget);
            RenderSystem.RegisterRenderTarget(AxisCorrectionRenderTarget);

            //Set viewport background color
            if (LocalSettings.Instance.SerializedBackgroundColor != null)
            {
                SceneManager.ViewportBackgroundColor = LocalSettings.Instance.SerializedBackgroundColor.ToColor();
            }

            if (LocalSettings.Instance.CustomScreenshotBackgroundColor != null)
            {
                SceneManager.ScreenshotBackgroundColor = LocalSettings.Instance.CustomScreenshotBackgroundColor.ToColor();
            }
        }

        protected override void LoadContent()
        {
            //AddEntity(new WorldAxis(this), false);
            //AddEntity(new ObjectAxis(true, this), false);
            //AddEntity(new WorldGrid(this), false);
            WorldGrid = new InfiniteWorldGrid(this);

            base.LoadContent();
        }

        protected override void Update(GameTime time)
        {
            FrameRate.Update((float)time.ElapsedGameTime.TotalSeconds);
            base.Update(time);
            ShaderManager.Update();
            CurrentGizmo.Update();
            BacHitboxGizmo.Update();
            EntityTransformGizmo.Update();

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
                    VfxManager.Update();
                }

                //Stage
                if (ActiveStage != null)
                {
                    foreach (StageModel model in ActiveStage.StageModels)
                    {
                        model.Update();
                    }
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
            camera.Update(time);

            if (GameUpdate != null)
                GameUpdate.Invoke(this, null);

            SceneManager.Update(time);
        }

        protected override void DelayedUpdate()
        {
            base.DelayedUpdate();
            ShaderManager.DelayedUpdate();
            RenderSystem.DelayedUpdate();
            CurrentGizmo.DelayedUpdate();
            BacHitboxGizmo.DelayedUpdate();

            for (int i = 0; i < SceneManager.Actors.Length; i++)
            {
                if (SceneManager.ActorsEnable[i] && SceneManager.Actors[i] != null)
                    SceneManager.Actors[i].DelayedUpdate();
            }

            camera.DelayedUpdate();

            //Exit fullscreen state if the window is closed by some other means
            if(fullscreenWindow != null)
            {
                if (IsFullScreen && !fullscreenWindow.IsActive)
                {
                    DisableFullscreen();
                }
            }
        }

        protected override void Draw(GameTime time)
        {
            ShaderManager.SetAllGlobalSamplers();

            //RenderSystem goes first
            RenderSystem.Draw();

            //Next, render everything else (gizmos, grid)
            GraphicsDevice.SetRenderTarget(MainRenderTarget.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            GraphicsDevice.SetDepthBuffer(RenderSystem.DepthBuffer.RenderTarget);

            //ShaderManager.Instance.SetAllGlobalSamplers();
            base.Draw(time);

            if (SceneManager.IsOnEffectTab)
            {
                VfxPreview.Draw();
            }
            else
            {
                VfxManager.Draw();
            }

            //Draw MainRenderTarget onto screen
            GraphicsDevice.SetRenderTarget(AxisCorrectionRenderTarget.RenderTarget);
            GraphicsDevice.Clear(SceneManager.ViewportBackgroundColor);
            GraphicsDevice.SetDepthBuffer(RenderSystem.DepthBuffer.RenderTarget);

            //Merge RTs
            RenderSystem.DisplayRenderTarget(RenderSystem.GetFinalRenderTarget());
            RenderSystem.DisplayRenderTarget(MainRenderTarget.RenderTarget);

            //Draw last and over everything else
            TextRenderer.Draw();
            CurrentGizmo.Draw();
            BacHitboxGizmo.Draw();
            EntityTransformGizmo.Draw();
            WorldGrid.Draw();

            //Now apply axis correction
            GraphicsDevice.SetRenderTarget(MainRenderTarget.RenderTarget);
            RenderSystem.SetTextures(AxisCorrectionRenderTarget.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            RenderSystem.YBS.ApplyAxisCorrection();

            //Present on screen
            GraphicsDevice.SetRenderTarget(InternalRenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            RenderSystem.DisplayRenderTarget(MainRenderTarget.RenderTarget, true);

        }

        public void ResetState(bool resetAnims = true, bool resetCamPos = false)
        {
            if (resetAnims)
                camera.ClearCameraAnimation();
            
            if (resetCamPos)
                camera.ResetCamera();

            for (int i = 0; i < SceneManager.Actors.Length; i++)
            {
                if (SceneManager.Actors[i] != null)
                    SceneManager.Actors[i].ResetState(resetAnims);
            }

            VfxManager.StopEffects();
        }

        public AnimatorGizmo GetAnimatorGizmo()
        {
            CurrentGizmo = AnimatorGizmo;
            return AnimatorGizmo;
        }

        public BoneScaleGizmo GetBoneScaleGizmo()
        {
            CurrentGizmo = BoneScaleGizmo;
            return BoneScaleGizmo;
        }

        public BacMatrixGizmo GetBacMatrixGizmo()
        {
            CurrentGizmo = BacMatrixGizmo;
            return BacMatrixGizmo;
        }

        public void SetActiveStage(ManualFiles stage)
        {
            if(ActiveStage != null)
            {
                RenderSystem.RemoveRenderEntity(ActiveStage.StageModels);
            }

            if(stage != null)
            {
                RenderSystem.AddRenderEntity(stage.StageModels);
            }

            ActiveStage = stage;
        }

        public void EnableFullscreen()
        {
            IsFullScreen = true;
            _graphicsDeviceManager.IsFullScreen = true;

            try
            {
                fullscreenWindow = new FullscreenWindow();
                fullscreenWindow.Show();
                alternateInputElement = fullscreenWindow;
                _mouse.SetAlternateFocusElement(fullscreenWindow);
                RenderSystem.RecreateRenderTargetsNextFrames = 2;
            }
            catch
            {
                //Dont really need to do anything here. Just want to prevent crashing when opening fullscreen mode (rare)
            }
        }

        public void DisableFullscreen()
        {
            if(fullscreenWindow != null)
            {
                fullscreenWindow.Close();
                fullscreenWindow = null;
                alternateInputElement = null;
            }

            IsFullScreen = false;
            _graphicsDeviceManager.IsFullScreen = false;
            _mouse.SetAlternateFocusElement(null);
            RenderSystem.RecreateRenderTargetsNextFrames = 2;
        }

        protected override void CheckHotkeys()
        {
            if(HotkeyCooldown == 0)
            {
                if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Space))
                {
                    IsPlaying = !IsPlaying;
                    HotkeyCooldown = 10;
                }
                else if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) && Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.O))
                {
                    SettingsManager.settings.XenoKit_UseOutlinePostEffect = !SettingsManager.settings.XenoKit_UseOutlinePostEffect;
                    SetHotkeyCooldown();
                }
                else if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F11) || (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape) && IsFullScreen))
                {
                    SetHotkeyCooldown();
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
            }

            base.CheckHotkeys();
        }


        #region UiButtons
        public void StartPlayback()
        {
            IsPlaying = true;

        }

        public void StopPlayback()
        {
            IsPlaying = false;
        }

        public void PauseAnimation()
        {
            IsPlaying = false;

        }

        public void PrevFrame()
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
                    if (SceneManager.Actors[0] != null)
                        SceneManager.Actors[0].AnimationPlayer.PrevFrame();
                    break;
                case EditorTabs.Camera:
                    camera.PrevFrame();
                    break;
                case EditorTabs.Action:
                    if (SceneManager.Actors[0] != null)
                        SceneManager.Actors[0].ActionControl.SeekPrevFrame();
                    break;
            }
        }

        public void NextFrame()
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
                    if (SceneManager.Actors[0] != null)
                        SceneManager.Actors[0].AnimationPlayer.NextFrame();
                    break;
                case EditorTabs.Camera:
                    camera.NextFrame();
                    break;
                case EditorTabs.Action:
                    if (SceneManager.Actors[0] != null)
                    {
                        SceneManager.Actors[0].ActionControl.SeekNextFrame();
                    }
                    break;
            }
        }

        #endregion

        #region Events
        public static EventHandler GameUpdate;

        #endregion

    }

}