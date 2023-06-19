using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using MonoGame.Framework.WpfInterop.Input;
using SpriteFontPlus;
using XenoKit.Engine.View;
using XenoKit.Engine.Objects;
using XenoKit.Engine.Gizmo;
using XenoKit.Engine.Audio;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Vfx;
using XenoKit.Engine.Text;
using XenoKit.Editor;
using XenoKit.Engine.Model;
using XenoKit.Engine.Vfx.Particle;

namespace XenoKit.Engine
{
    public class Game : GameBase
    {
        public override ICameraBase ActiveCameraBase { get => camera; }

        //Engine Features:
        public Camera camera;
        public AudioEngine AudioEngine;
        public VfxPreview VfxPreview;

        //Engine Values:
        public override bool IsMainInstance => true;

        //Gizmos
        public GizmoBase CurrentGizmo = null;
        public AnimatorGizmo AnimatorGizmo;
        public BoneScaleGizmo BoneScaleGizmo;
        public BacMatrixGizmo BacMatrixGizmo;
        public HitboxGizmo BacHitboxGizmo;

        //Stage. Not final design just a quick hacky way to get stages into the application.
        public ManualFiles ActiveStage = null;

        protected override void Initialize()
        {
            //Initalize all base elements first
            base.Initialize();

            //Set game instace in SceneManager - this is required for most objects to function correctly
            SceneManager.MainGameInstance = this;

            //Required for global samplers, currently. Everything else uses Entity.GraphicsDevice. Will eventually remove entirely
            SceneManager.GraphicsDeviceRef = GraphicsDevice;

            //Now initialize objects
            AnimatorGizmo = new AnimatorGizmo(this);
            BoneScaleGizmo = new  BoneScaleGizmo(this);
            BacMatrixGizmo = new BacMatrixGizmo(this);
            BacHitboxGizmo = new HitboxGizmo(this);
            CurrentGizmo = AnimatorGizmo;

            camera = new Camera(this);
            AudioEngine = new AudioEngine();
            VfxManager = new VfxManager(this);
            RenderDepthSystem = new RenderDepthSystem(this);
            VfxPreview = new VfxPreview(this);
        }

        protected override void LoadContent()
        {
            AddEntity(new WorldAxis(this), false);
            AddEntity(new ObjectAxis(true, this), false);
            AddEntity(new WorldGrid(this), false);

            base.LoadContent();
        }

        protected override void Update(GameTime time)
        {
            base.Update(time);
            CurrentGizmo.Update();
            BacHitboxGizmo.Update();

            AudioEngine.Update();

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
                if(SceneManager.ActorsEnable[i] && SceneManager.Actors[i] != null)
                {
                    SceneManager.Actors[i].Update();
                }
            }
            
            //Update camera last - this way it has the lowest priority for mouse click events
            camera.Update(time);


            if (GameUpdate != null)
                GameUpdate.Invoke(this, null);

            SceneManager.Update(time);

        }

        protected override void DelayedUpdate()
        {
            base.DelayedUpdate();
            CurrentGizmo.DelayedUpdate();
            BacHitboxGizmo.DelayedUpdate();


            for (int i = 0; i < SceneManager.Actors.Length; i++)
            {
                if (SceneManager.ActorsEnable[i] && SceneManager.Actors[i] != null)
                    SceneManager.Actors[i].DelayedUpdate();
            }

            camera.DelayedUpdate();
        }

        protected override void Draw(GameTime time)
        {
            //ShaderManager.Instance.SetAllGlobalSamplers();
            base.Draw(time);

            //Stage:
            if (ActiveStage != null)
            {
                foreach(StageModel model in ActiveStage.StageModels)
                {
                    model.Draw();
                }
            }

            //Draw Particles, Actors... 
            RenderDepthSystem.Draw();

            //TODO: move other effects into RenderDepthSystem
            if (SceneManager.IsOnEffectTab)
            {
                VfxPreview.Draw();
            }
            else
            {
                VfxManager.Draw();
            }

            //Sprite:
            TextRenderer.Draw();

            //Draw last and over everything else
            CurrentGizmo.Draw();
            BacHitboxGizmo.Draw();
        }

        public void ResetState(bool resetAnims = true, bool resetCamPos = false)
        {
            BacTimeScale = 1f;

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
            switch (SceneManager.CurrentSceneState)
            {
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
            switch (SceneManager.CurrentSceneState)
            {
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