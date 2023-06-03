using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.Text;
using XenoKit.Engine.View;
using XenoKit.Engine.Vfx;

namespace XenoKit.Engine
{
    /// <summary>
    /// Base class for all engine objects. Object instance cannot be shared accross different GameBase instances.
    /// </summary>
    public abstract class Entity : IDisposable
    {
        protected GameBase GameBase;

        //Exposed Properties
        public GraphicsDevice GraphicsDevice => GameBase.GraphicsDevice;
        public Input Input => GameBase.Input;
        public TextRenderer TextRenderer => GameBase.TextRenderer;
        public ICameraBase CameraBase => GameBase.ActiveCameraBase;
        /// <summary>
        /// Exposes GameBase.IsActive. Determines whether the game view is currently focused (mouse is over it).
        /// </summary>
        public bool GameIsFocused => GameBase.IsActive;
        /// <summary>
        /// GameTime for the current frame.
        /// </summary>
        public GameTime ElapsedTime => GameBase.GameTime;
        public VfxManager VfxManager => GameBase.VfxManager;
        public CompiledObjectManager CompiledObjectManager => GameBase.CompiledObjectManager;

        public virtual string Name { get; set; }
        public virtual Matrix Transform { get; set; } = Matrix.Identity;

        //Entity Settings
        public bool IsDestroyable { get; set; } = true;
        public bool IsDestroyed { private set; get; }

        public Entity(GameBase gameBase)
        {
            GameBase = gameBase;
        }

        /// <summary>
        /// Invoked every frame, before Draw. This is where logic for updating the objects state should be.
        /// (Entity must be registered via <see cref="GameBase.AddEntity(Entity, bool)"/>)
        /// </summary>
        public virtual void Update()
        {

        }

        /// <summary>
        /// Invoked every second (60 frames).
        /// (Entity must be registered via <see cref="GameBase.AddEntity(Entity, bool)"/>)
        /// </summary>
        public virtual void DelayedUpdate()
        {

        }

        /// <summary>
        /// Invoked every frame. This is where the code for rendering the object should be, if any.
        /// (Entity must be registered via <see cref="GameBase.AddEntity(Entity, bool)"/>)
        /// </summary>
        public virtual void Draw()
        {

        }

        /// <summary>
        /// Invoked upon the Entity's destruction/>
        /// </summary>
        public virtual void Dispose()
        {

        }

        /// <summary>
        /// Destroy the Entity on the next update. Automatically invokes the <see cref="Dispose"/> method.
        /// (Entity must be registered via <see cref="GameBase.AddEntity(Entity, bool)"/>)
        /// </summary>
        public virtual void Destroy()
        {
            Dispose();
            IsDestroyed = true;
        }
    }
}
