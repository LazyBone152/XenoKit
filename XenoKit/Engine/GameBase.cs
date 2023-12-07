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
using XenoKit.Engine.Pool;
using XenoKit.Engine.Rendering;
using XenoKit.Engine.Shader;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine
{
    public class GameBase : WpfGame
    {
        protected WpfGraphicsDeviceService _graphicsDeviceManager;
        public WpfKeyboard _keyboard;
        public WpfMouse _mouse;
        public SpriteBatch spriteBatch;
        public GameTime GameTime { get; protected set; }

        //Engine Features:
        public ShaderManager ShaderManager { get; private set; }
        public virtual ICameraBase ActiveCameraBase { get; }
        public DirLight LightSource { get; private set; }
        public Input Input { get; private set; } = new Input();
        public TextRenderer TextRenderer { get; private set; }
        public VfxManager VfxManager { get; protected set; }
        public RenderSystem RenderSystem { get; protected set; }
        public CompiledObjectManager CompiledObjectManager { get; private set; } = new CompiledObjectManager();
        public ObjectPoolManager ObjectPoolManager { get; private set; }

        //Engine Values:
        public virtual bool IsMainInstance => false;
        public bool IsPlaying = false;
        public bool RenderCharacters = true;
        public bool WireframeMode = false;
        public float BacTimeScale = 1f;
        public float AnimationTimeScale = 1f;
        public float ActiveTimeScale
        {
            get
            {
                if (IsMainInstance)
                {
                    return (SceneManager.CurrentSceneState == EditorTabs.Action) ? BacTimeScale * AnimationTimeScale : 1f;
                }
                return 1f; //No instance other than the main uses a time scale currently
            }
        }

        //Entity
        protected List<Entity> Entities = new List<Entity>(1000);

        //Other
        public virtual Color BackgroundColor { get; set; } = new Color(20, 20, 20, 255);
        private int DelayedTimer = 0;
        protected int HotkeyCooldown = 0;

        protected override void Initialize()
        {
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

            // must be called after the WpfGraphicsDeviceService instance was created
            base.Initialize();

            //Load font
            TextRenderer = new TextRenderer(GraphicsDevice, spriteBatch);

            LightSource = new DirLight(this);
            ObjectPoolManager = new ObjectPoolManager(this);
            ShaderManager = new ShaderManager(this);
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
            CheckHotkeys();

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

            if(DelayedTimer >= 60)
            {
                DelayedTimer = 0;
                DelayedUpdate();
            }
            else
            {
                DelayedTimer++;
            }
        }

        protected virtual void DelayedUpdate()
        {
            ObjectPoolManager.DelayedUpdate();

            for(int i = 0; i < Entities.Count; i++)
            {
                Entities[i].DelayedUpdate();
            }
        }

        protected override void Draw(GameTime time)
        {
            //GraphicsDevice.Clear(BackgroundColor);

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

        public void SetBacTimeScale(float timeScale, bool reset)
        {
            if (reset) BacTimeScale = 1f;
            BacTimeScale *= timeScale;
        }
    
        protected virtual void CheckHotkeys()
        {
            if(HotkeyCooldown == 0)
            {
                if(Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) && Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.W))
                {
                    WireframeMode = !WireframeMode;
                    CompiledObjectManager.ForceShaderUpdate();
                    SetHotkeyCooldown();
                }
                else if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) && Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.G))
                {
                    SceneManager.ShowWorldAxis = !SceneManager.ShowWorldAxis;
                    SetHotkeyCooldown();
                }
                else if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) && Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.L))
                {
                    SettingsManager.settings.XenoKit_EnableDynamicLighting = !SettingsManager.settings.XenoKit_EnableDynamicLighting;
                    SetHotkeyCooldown();
                }
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
    }
}
