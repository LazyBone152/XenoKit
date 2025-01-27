using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.Shader;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Objects
{
    public class InfiniteWorldGrid : Entity
    {
        public readonly VertexPosition[] Vertices;
        private readonly PostShaderEffect Shader;

        private float Near = 0.02f;
        private float Far = 10f;
        private Vector4 GridColor = new Vector4(0.2f, 0.2f, 0.2f, 1f);

        public InfiniteWorldGrid(GameBase game) : base(game)
        {
            Shader = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetExtShaderProgram("LB_WorldGrid"), game);

            Vertices = new VertexPosition[6]
            {
                 new VertexPosition(new Vector3(1, 1, 0)),  new VertexPosition(new Vector3(-1, -1, 0)), new VertexPosition(new Vector3(-1, 1, 0)),
                 new VertexPosition(new Vector3(-1, -1, 0)),  new VertexPosition(new Vector3(1, 1, 0)),  new VertexPosition(new Vector3(1, -1, 0))
            };
            
            Shader.Parameters["near"]?.SetValue(Near);
            Shader.Parameters["far"]?.SetValue(Far);
            Shader.Parameters["gridColor"]?.SetValue(GridColor);
        }

        public override void Draw()
        {
            if (SceneManager.ShowWorldAxis)
            {
                Shader.Parameters["g_mV_VS"]?.SetValue(GameBase.ActiveCameraBase.ViewMatrix);
                Shader.Parameters["g_mP_VS"]?.SetValue(GameBase.ActiveCameraBase.ProjectionMatrix);
                Shader.Parameters["near"]?.SetValue(Near);
                Shader.Parameters["far"]?.SetValue(Far);
                Shader.Parameters["gridColor"]?.SetValue(GridColor);
                Shader.Parameters["supersampleFactor"]?.SetValue(RenderSystem.SuperSampleFactor);

                foreach (var pass in Shader.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
                    GraphicsDevice.RasterizerState = RasterizerState.CullNone;
                    GraphicsDevice.BlendState = BlendState.Additive;

                    GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, Vertices, 0, 2);
                }
            }
        }
    }
}
