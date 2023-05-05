using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.Shapes;
using XenoKit.Engine.View;

namespace XenoKit.Engine.Objects
{
    public class VisualBone : Entity
    {
        private Sphere sphere;
        private BoundingSphere boundingSphere;
        public Matrix world;

        public bool IsVisible = false;

        //Mesh settings
        private Color DefaultColor = Color.Blue;
        private Color SelectedColor = Color.Red;
        private const float MeshSize = 0.01f;

        public VisualBone(GameBase gameBase) : base(gameBase)
        {
            sphere = new Sphere(gameBase, MeshSize, true);
        }

        public void Draw(Matrix world, bool isSelected)
        {
            if (IsVisible)
            {
                this.world = world;
                sphere.Draw(world, CameraBase.ViewMatrix, CameraBase.ProjectionMatrix, (isSelected) ? SelectedColor : DefaultColor);
            }
        }

        public bool IsMouseOver()
        {
            boundingSphere = new BoundingSphere(Vector3.Zero, MeshSize);
            boundingSphere = boundingSphere.Transform(world);

            float? value = EngineUtils.IntersectDistance(boundingSphere, Input.MousePosition, GameBase);

            return (value != null && !float.IsNaN(value.Value));
        }
    
    }
}
