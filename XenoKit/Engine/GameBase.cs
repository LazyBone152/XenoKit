using System;
using System.IO;
using System.Timers;
using System.Collections.Generic;
using SpriteFontPlus;
using XenoKit.Editor;
using XenoKit.Engine.View;
using XenoKit.Engine.Objects;
using XenoKit.Engine.Gizmo;
using XenoKit.Engine.Audio;
using XenoKit.Engine.Text;
using XenoKit.Engine.Lighting;
using XenoKit.Engine.Vfx;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using MonoGame.Framework.WpfInterop.Input;
using System.Threading.Tasks;

namespace XenoKit.Engine
{
    public class GameBase : WpfGame
    {
        private IGraphicsDeviceService _graphicsDeviceManager;
        public WpfKeyboard _keyboard;
        public WpfMouse _mouse;
        public SpriteBatch spriteBatch;
        public GameTime GameTime { get; protected set; }

        //Custom Features:
        public DirLight LightSource { get; private set; }
        public Input Input { get; private set; } = new Input();
        public TextRenderer TextRenderer { get; private set; }
        public VfxManager VfxManager { get; protected set; }
        public CompiledObjectManager CompiledObjectManager { get; private set; } = new CompiledObjectManager();

        //Entity
        protected List<Entity> Entities = new List<Entity>(1000);

        //Other
        public virtual Color BackgroundColor { get; set; } = new Color(20, 20, 20, 255);
        public virtual ICameraBase ActiveCameraBase { get; }
        private Timer DelayedTimer = new Timer(1000);
        public bool RenderCharacters = true;

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

            //Load font
            TextRenderer = new TextRenderer(GraphicsDevice, spriteBatch);

            //Set up DelayedTimer so we can do a DelayedUpdate on all Entities
            DelayedTimer.Elapsed += DelayedTimer_Elapsed;
            DelayedTimer.AutoReset = true;
            DelayedTimer.Start();

            LightSource = new DirLight(this);

        }

        private void DelayedTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            DelayedUpdate();
        }

        protected override void LoadContent()
        {
            base.LoadContent();
        }


        protected override void Update(GameTime time)
        {
            GameTime = time;
            Input.Update(_mouse, _keyboard);

            LightSource.Update();

            //Entities
            for (int i = Entities.Count - 1; i >= 0; i--)
            {
                if (Entities[i].IsDestroyed)
                {
                    Entities.RemoveAt(i);
                    continue;
                }

                Entities[i].Update();
            }

        }

        protected virtual void DelayedUpdate()
        {
            for(int i = 0; i < Entities.Count; i++)
            {
                Entities[i].DelayedUpdate();
            }
        }

        protected override void Draw(GameTime time)
        {
            GraphicsDevice.Clear(BackgroundColor);

            //Entities
            for (int i = 0; i < Entities.Count; i++)
            {
                Entities[i].Draw();
            }
        }


        #region Entity
        public void AddEntity(Entity entity, bool isDestroyable = true)
        {
            entity.IsDestroyable = isDestroyable;

            if (!Entities.Contains(entity))
                Entities.Add(entity);
        }

        public void DestroyEntity(Entity entity)
        {
            entity.Dispose();
            Entities.Remove(entity);
        }

        public void DestroyAllEntities()
        {
            for (int i = Entities.Count - 1; i >= 0; i--)
            {
                if (Entities[i].IsDestroyable)
                {
                    Entities[i].Dispose();
                    Entities.RemoveAt(i);
                }
            }
        }

        #endregion
    }
}
