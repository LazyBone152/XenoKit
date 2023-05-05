using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.View;

namespace XenoKit.Engine
{
    /// <summary>
    /// Base class for all objects.
    /// </summary>
    public abstract class Entity : IEntity
    {
        protected GraphicsDevice graphicsDevice;
        
        public virtual string Name { get; set; }
        public virtual Matrix Transform { get; set; }

        public abstract void Update(GameTime gameTime);

        public abstract void Draw(Camera camera);
    }

    public interface IEntity
    {
        string Name { get; set; }
        Matrix Transform { get; set; }

        void Update(GameTime gameTime);

        void Draw(Camera camera);
    }

}
