using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.View;
using XenoKit.Engine.Vfx;
using XenoKit.Engine.Pool;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Rendering;
using XenoKit.Engine.Lighting;

namespace XenoKit.Engine
{
    /// <summary>
    /// Base class for all engine objects.
    /// </summary>
    public abstract class EngineObject
    {
        public Viewport ViewportInstance => Viewport.Instance;
        public virtual EngineObjectTypeEnum EngineObjectType => EngineObjectTypeEnum.Undefined;

        //Exposed Properties
        public GraphicsDevice GraphicsDevice => ViewportInstance.GraphicsDevice;
        public ShaderManager ShaderManager => ViewportInstance.ShaderManager;
        public RenderSystem RenderSystem => ViewportInstance.RenderSystem;
        public Input Input => ViewportInstance.Input;
        public Camera Camera => ViewportInstance.Camera;
        /// <summary>
        /// True if the viewport has focus (mouse is over, recieving input)
        /// </summary>
        public bool ViewportIsFocused => ViewportInstance.IsActive || ViewportInstance.IsFullScreen;
        public VfxManager VfxManager => ViewportInstance.VfxManager;
        public CompiledObjectManager CompiledObjectManager => ViewportInstance.CompiledObjectManager;
        public ObjectPoolManager ObjectPoolManager => ViewportInstance.ObjectPoolManager;
        public LightSystem LightSystem => ViewportInstance.LightSystem;

        public virtual string Name { get; set; }
        public virtual System.Numerics.Matrix4x4 Transform { get; set; } = System.Numerics.Matrix4x4.Identity;

        public EngineObject()
        {
        }

        public virtual void Update()
        {

        }

        public virtual void DelayedUpdate()
        {

        }

        public virtual void Draw()
        {

        }

        public virtual void DrawPass(bool normalPass)
        {

        }

    }

    public enum EngineObjectTypeEnum
    {
        Undefined,
        Model,
        Actor,
        Stage,
        VFX
    }
}
