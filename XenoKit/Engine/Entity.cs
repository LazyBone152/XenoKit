using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.Text;
using XenoKit.Engine.View;
using XenoKit.Engine.Vfx;
using XenoKit.Engine.Pool;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Rendering;

namespace XenoKit.Engine
{
    /// <summary>
    /// Base class for all engine objects. Object instance cannot be shared accross different GameBase instances.
    /// </summary>
    public abstract class Entity : IDisposable
    {
        public GameBase GameBase { get; protected set; }
        public virtual EntityType EntityType => EntityType.Undefined;

        //Exposed Properties
        public GraphicsDevice GraphicsDevice => GameBase.GraphicsDevice;
        public ShaderManager ShaderManager => GameBase.ShaderManager;
        public RenderSystem RenderSystem => GameBase.RenderSystem;
        public Input Input => GameBase.Input;
        public TextRenderer TextRenderer => GameBase.TextRenderer;
        public ICameraBase CameraBase => GameBase.ActiveCameraBase;
        /// <summary>
        /// Exposes GameBase.IsActive. Determines whether the game view is currently focused (mouse is over it).
        /// </summary>
        public bool GameIsFocused => GameBase.IsActive || GameBase.IsFullScreen;
        /// <summary>
        /// GameTime for the current frame.
        /// </summary>
        public GameTime ElapsedTime => GameBase.GameTime;
        public VfxManager VfxManager => GameBase.VfxManager;
        public CompiledObjectManager CompiledObjectManager => GameBase.CompiledObjectManager;
        public ObjectPoolManager ObjectPoolManager => GameBase.ObjectPoolManager;

        public virtual string Name { get; set; }
        public virtual Matrix AbsoluteTransform { get; protected set; }
        public virtual Matrix Transform { get; set; } = Matrix.Identity;
        /// <summary>
        /// (Mostly just used by RenderDepthSystem to enable it to skip drawing objects that haven't been updated this frame)
        /// </summary>
        public virtual bool DrawThisFrame { get; protected set; }
        public virtual int AlphaBlendType => -1;
        public virtual int LowRezMode => 0;

        //Entity Settings
        public bool IsDestroyable { get; set; } = true;
        public bool IsDestroyed { private set; get; }

        public Entity(GameBase gameBase)
        {
            GameBase = gameBase;
        }

        public Entity() { }

        /// <summary>
        /// Invoked every frame, before Draw. This is where logic for updating the objects state should be.
        /// (Entity must be registered via <see cref="GameBase.AddEntity(Entity, bool)"/>)
        /// </summary>
        public virtual void Update()
        {

        }

        /// <summary>
        /// Invoked every second (60 frames).
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

        public virtual void DrawPass(bool normalPass)
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

        /// <summary>
        /// Mark the Entity as alive once again (Changes Entity.IsDestroyed to false).
        /// </summary>
        public void Reclaim()
        {
            IsDestroyed = false;
        }

        /// <summary>
        /// If the GameBase instance wasn't set in the constructor, it can be set by this method after the Entity has been created.
        /// </summary>
        public void SetGameBaseInstance(GameBase game)
        {
            if(GameBase != null)
            {
                throw new InvalidOperationException($"Entity.SetGameBaseInstance: GameBase has already been set!");
            }

            GameBase = game;
        }
    }

    public enum EntityType
    {
        Undefined,
        Model,
        Actor,
        Stage,
        VFX
    }
}
