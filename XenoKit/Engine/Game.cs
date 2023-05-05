using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using MonoGame.Framework.WpfInterop.Input;
using System;
using XenoKit.Engine.View;
using XenoKit.Engine.Objects;
using XenoKit.Engine.Gizmo;
using XenoKit.Engine.Audio;

namespace XenoKit.Engine
{

    public class Game : WpfGame
    {
        private IGraphicsDeviceService _graphicsDeviceManager;
        public WpfKeyboard _keyboard;
        public WpfMouse _mouse;

        //Scene Objects:
        public Camera camera;
        public AudioEngine audioEngine;
        public AnimatorGizmo animGizmo;

        //Axis stuff:
        WorldAxis worldAxis;
        ObjectAxis worldAxis_b;
        Grid worldGrid;

        //Scene Settings:
        public bool loopAnimations = false;
        public bool loopCameras = false;
        public Color backgroundColor = Color.Black;

        public SpriteBatch spriteBatch;

        protected override void Initialize()
        { 
            // must be initialized. required by Content loading and rendering (will add itself to the Services)
            // note that MonoGame requires this to be initialized in the constructor, while WpfInterop requires it to
            // be called inside Initialize (before base.Initialize())
            _graphicsDeviceManager = new WpfGraphicsDeviceService(this);
            spriteBatch = new SpriteBatch(_graphicsDeviceManager.GraphicsDevice);

            // wpf and keyboard need reference to the host control in order to receive input
            // this means every WpfGame control will have it's own keyboard & mouse manager which will only react if the mouse is in the control
            _keyboard = new WpfKeyboard(this);
            _mouse = new WpfMouse(this);

            // must be called after the WpfGraphicsDeviceService instance was created
            base.Initialize();

            //Set game instace in SceneManager - this is required for most objects to function correctly
            SceneManager.gameInstance = this;

            //Now initialize objects
            animGizmo = new AnimatorGizmo();
            camera = new Camera(GraphicsDevice, this);
            audioEngine = new AudioEngine();

        }

        protected override void LoadContent()
        {
            worldAxis = new WorldAxis(GraphicsDevice);
            worldAxis_b = new ObjectAxis(GraphicsDevice);
            worldGrid = new Grid(GraphicsDevice);

            base.LoadContent();
        }

        protected override void Update(GameTime time)
        {
            Input.Update(_mouse, _keyboard);
            animGizmo.Update(time);

            //Audio
            audioEngine.Update();

            //Actors
            for (int i = 0; i < SceneManager.Actors.Length; i++)
            {
                if(SceneManager.ActorsEnable[i])
                    UpdateCharacter(i, time);
            }

            //Entities
            for (int i = 0; i < SceneManager.Entities.Count; i++)
            {
                SceneManager.Entities[i].Update(time);
            }
            
            //Update camera last - this way it has the lowest priority for mouse click events
            camera.Update(time, _mouse.GetState(), _keyboard.GetState());

            if (GameUpdate != null)
                GameUpdate.Invoke(this, null);

            SceneManager.Update(time);
        }

        protected override void Draw(GameTime time)
        {
            GraphicsDevice.Clear(backgroundColor);

            DrawWorldAxis();

            //Actors
            for (int i = 0; i < SceneManager.Actors.Length; i++)
            {
                if (SceneManager.ActorsEnable[i])
                    DrawCharacter(i);
            }

            //Entities
            for (int i = 0; i < SceneManager.Entities.Count; i++)
            {
                SceneManager.Entities[i].Draw(camera);
            }

            //Draw last and over everything else
            animGizmo.Draw();
        }

        private void DrawWorldAxis()
        {
            if (SceneManager.ShowWorldAxis)
            {
                worldAxis.Draw(camera);
                worldAxis_b.Draw(GraphicsDevice, camera, Matrix.Identity);
                worldGrid.Draw(camera);
            }
        }

        private void UpdateCharacter(int charIndex, GameTime time)
        {
            if(SceneManager.Actors[charIndex] != null)
            {
                SceneManager.Actors[charIndex].Update(time);
            }
        }

        private void DrawCharacter(int charIndex)
        {
            if (SceneManager.Actors[charIndex] != null)
            {
                SceneManager.Actors[charIndex].Draw(camera);
            }
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
        }

        //UI Interaction
        #region UiButtons
        public void StartPlayback()
        {
            SceneManager.IsPlaying = true;

        }

        public void StopPlayback()
        {
            SceneManager.IsPlaying = false;
        }

        public void PauseAnimation()
        {
            SceneManager.IsPlaying = false;

        }

        public void PrevFrame()
        {
            switch (SceneManager.CurrentSceneState)
            {
                case EditorTabs.Animation:
                    if (SceneManager.Actors[0] != null)
                        SceneManager.Actors[0].animationPlayer.PrevFrame();
                    break;
                case EditorTabs.Camera:
                    camera.PrevFrame();
                    break;
                case EditorTabs.Action:
                    if (SceneManager.Actors[0] != null)
                        SceneManager.Actors[0].bacPlayer.SeekPrevFrame();
                    break;
            }
        }

        public void NextFrame()
        {
            switch (SceneManager.CurrentSceneState)
            {
                case EditorTabs.Animation:
                    if (SceneManager.Actors[0] != null)
                        SceneManager.Actors[0].animationPlayer.NextFrame();
                    break;
                case EditorTabs.Camera:
                    camera.NextFrame();
                    break;
                case EditorTabs.Action:
                    if (SceneManager.Actors[0] != null)
                    {
                        SceneManager.Actors[0].bacPlayer.SeekNextFrame();
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