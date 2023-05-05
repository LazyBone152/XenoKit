using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop.Input;
using XenoKit.Editor;
using XenoKit.Engine.Animation;
using XenoKit.Engine.Shapes;
using XenoKit.Engine.View;
using Xv2CoreLib.EAN;

namespace XenoKit.Engine.Objects
{
    public class VisualBone
    {
        private Sphere sphere;
        private BoundingSphere boundingSphere;
        private Matrix world;

        public bool IsVisible = false;

        //Mesh settings
        private Color DefaultColor = Color.Blue;
        private Color SelectedColor = Color.Red;
        private const float MeshSize = 0.01f;

        public VisualBone()
        {
            sphere = new Sphere(MeshSize, true);
        }

        public void Draw(Camera camera, Matrix world, bool isSelected)
        {
            if (IsVisible)
            {
                this.world = world;
                sphere.Draw(world, camera.ViewMatrix, camera.ProjectionMatrix, (isSelected) ? SelectedColor : DefaultColor);
            }
        }

        public bool IsMouseOver()
        {
            boundingSphere = new BoundingSphere(Vector3.Zero, MeshSize);
            boundingSphere = boundingSphere.Transform(world);

            float? value = EngineUtils.IntersectDistance(boundingSphere, Input.MousePosition);

            return (value != null && !float.IsNaN(value.Value));
        }
    
    }
}
